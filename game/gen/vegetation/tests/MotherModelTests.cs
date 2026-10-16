using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

[TestSuite]
public sealed class MotherModelTests
{
    private static readonly Lazy<(SpeciesTable Table, PaletteRegistry Palette)> Data = new(Load);

    private static (SpeciesTable, PaletteRegistry) Load()
    {
        var root = FindRepository();
        var table = SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json")));
        var entries = new List<PaletteEntry>();
        var palette = Path.Combine(root, "game", "style", "palette");
        var files = new[] { Path.Combine(palette, "core.json") }
            .Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "WP-*.json").Order(StringComparer.Ordinal));
        foreach (var file in files)
            entries.AddRange(JsonSerializer.Deserialize<PaletteRegistryData>(File.ReadAllBytes(file), ContractJson.Options)!.Entries);
        return (table, new PaletteRegistry(entries));
    }

    [TestCase]
    public void SpeciesTableCoversTheVegetationDoc()
    {
        var (table, palette) = Data.Value;
        if (table.Species.Count < 21) throw new InvalidOperationException($"only {table.Species.Count} species");
        foreach (var s in table.Species)
        {
            palette.Validate(MotherModels.CrownSpec(s));
            if (palette.Get(s.LeafLit).Row is null) throw new InvalidOperationException($"{s.Id}: leaf colour has no seasonal row");
            if (s.BloomLit is { } bloom) _ = palette.Get(bloom);
            if (s.Deciduous == s.LeafAmount.All(a => a >= 1f)) throw new InvalidOperationException($"{s.Id}: deciduous flag disagrees with A");
        }
        palette.Validate(MotherModels.WoodSpec());
        _ = palette.Get(MotherModels.Whitewash);
        _ = palette.Get(MotherModels.Twig);
        // Bark has its own pair in the WP-23 extension range (max-011 review: 252–253).
        if (palette.Get(MotherModels.Bark).Id != 252 || palette.Get(MotherModels.BarkShade).Id != 253)
            throw new InvalidOperationException("tree.bark must sit at colour IDs 252–253");
    }

    [TestCase]
    public void PackedTableCarriesEveryRow()
    {
        var (table, palette) = Data.Value;
        var data = table.Pack(palette);
        if (data.Length != SpeciesTable.Width * SpeciesTable.Rows * 4) throw new InvalidOperationException("table size");
        foreach (var s in table.Species)
        {
            float At(int x, int c) => data[(s.Row * SpeciesTable.Width + x) * 4 + c];
            if (At(7, 3) != SpeciesTable.Tag) throw new InvalidOperationException($"{s.Id}: tag missing");
            if (At(0, 2) != s.LeafAmount[2] || At(1, 0) != s.BloomAmount[0]) throw new InvalidOperationException($"{s.Id}: anchors");
            if ((int)At(2, 1) != palette.Get(s.LeafLit).Row) throw new InvalidOperationException($"{s.Id}: leaf row");
        }
        // Unused rows must not look written: an unset texture reads as all zeros or all ones.
        var used = table.Species.Select(s => s.Row).ToHashSet();
        for (var row = 0; row < SpeciesTable.Rows; row++)
            if (!used.Contains(row) && data[(row * SpeciesTable.Width + 7) * 4 + 3] == SpeciesTable.Tag)
                throw new InvalidOperationException($"row {row} tagged");
    }

    [TestCase]
    public void MothersAreDeterministic()
    {
        var (table, palette) = Data.Value;
        foreach (var id in new[] { "ginkgo", "cherry", "camphor", "palm" })
        {
            var a = MotherModels.Build(table.Get(id), 3, VegLevel.L0Plus, palette);
            var b = MotherModels.Build(table.Get(id), 3, VegLevel.L0Plus, palette);
            if (!a.Wood.Position.SequenceEqual(b.Wood.Position) || !a.Crown.Custom1!.SequenceEqual(b.Crown.Custom1!)
                || !a.Crown.Normal.SequenceEqual(b.Crown.Normal) || !a.Wood.Index.SequenceEqual(b.Wood.Index))
                throw new InvalidOperationException($"{id}: two builds differ");
        }
    }

    [TestCase]
    public void TriangleBudgetsHold()
    {
        var (table, palette) = Data.Value;
        foreach (var s in table.Species)
            for (var v = 0; v < TreeForms.Variants; v++)
            {
                var near = MotherModels.Build(s, v, VegLevel.L0Plus, palette).Triangles;
                var mid = MotherModels.Build(s, v, VegLevel.L0, palette).Triangles;
                var far = MotherModels.Build(s, v, VegLevel.L1, palette).Triangles;
                // ENG-PERF §9 and the max-011 review: cloud L0 2.5k raised to the 3.5k allowance for the outline, card and
                // mixed L0 3–4k, L1 target 0.9k. Only the metasequoia exceeds 4k at L0 (up to 4.5k) and 0.9k at L1 (up to
                // 1.1k); these ceilings hold it there.
                var midLimit = s.Crown == CrownType.Cloud ? MotherModels.CloudTrianglesL0Outline : 4600;
                if (near > MotherModels.MaxTrianglesL0Plus) throw new InvalidOperationException($"{s.Id}/{v}: L0+ {near}");
                if (mid > midLimit) throw new InvalidOperationException($"{s.Id}/{v}: L0 {mid}");
                if (far > 1100) throw new InvalidOperationException($"{s.Id}/{v}: L1 {far}");
                if (!(near >= mid && mid >= far)) throw new InvalidOperationException($"{s.Id}/{v}: levels not nested {near}/{mid}/{far}");
            }
    }

    [TestCase]
    public void LowerLevelCardsArePrefixes()
    {
        var (table, palette) = Data.Value;
        foreach (var id in new[] { "ginkgo", "osmanthus", "nanmu" })
        {
            var cards = new[] { VegLevel.L0Plus, VegLevel.L0, VegLevel.L1 }
                .Select(level => MotherModels.Build(table.Get(id), 0, level, palette))
                .Select(m => m.Crown.Custom0!.Take((m.Cards - m.UnderCards) * 4 * 4).ToArray()).ToArray();
            for (var i = 1; i < cards.Length; i++)
                if (cards[i].Length > cards[i - 1].Length || !cards[i].SequenceEqual(cards[i - 1].Take(cards[i].Length)))
                    throw new InvalidOperationException($"{id}: level {i} cards are not a prefix");
        }
    }

    [TestCase]
    public void UnderCardsOnlyNearAndUnderTallCrowns()
    {
        // max-011 23c3: crowns one can stand under carry an underside layer at L0+ and, for card and mixed crowns, its leading
        // share at L0 (a prefix of whole clusters); L1, low crowns and palms carry none. The layer faces down.
        var (table, palette) = Data.Value;
        foreach (var (id, throughL0) in new[] { ("ginkgo", true), ("camphor", true), ("koelreuteria", false) })
        {
            var s = table.Get(id);
            var near = MotherModels.Build(s, 0, VegLevel.L0Plus, palette);
            var mid = MotherModels.Build(s, 0, VegLevel.L0, palette);
            var far = MotherModels.Build(s, 0, VegLevel.L1, palette);
            if (near.UnderCards < MotherModels.ClusterMin * 8) throw new InvalidOperationException($"{id}: {near.UnderCards} underside cards at L0+");
            if (far.UnderCards != 0) throw new InvalidOperationException($"{id}: underside cards at L1");
            if (throughL0 ? mid.UnderCards == 0 || mid.UnderCards > near.UnderCards * 0.6f : mid.UnderCards != 0)
                throw new InvalidOperationException($"{id}: {mid.UnderCards} of {near.UnderCards} underside cards at L0");
            var first = near.Cards - near.UnderCards;
            if (Vectors(near.Crown.Normal, 3).Skip(first * 4).Take(near.UnderCards * 4).Any(n => n.Y > -0.1f))
                throw new InvalidOperationException($"{id}: an underside card does not face down");
            if ((int)near.Crown.Custom2![first * 16 + 3] != (throughL0 ? 1 : 2))
                throw new InvalidOperationException($"{id}: underside rank tier");
            var a = near.Crown.Custom0!.Skip(first * 16).Take(mid.UnderCards * 16);
            var b = mid.Crown.Custom0!.Skip((mid.Cards - mid.UnderCards) * 16).Take(mid.UnderCards * 16);
            if (!a.SequenceEqual(b)) throw new InvalidOperationException($"{id}: L0 underside cards are not a prefix");
        }
        foreach (var id in new[] { "cherry", "photinia", "palm" })
            if (MotherModels.Build(table.Get(id), 0, VegLevel.L0Plus, palette).UnderCards != 0)
                throw new InvalidOperationException($"{id}: a low crown has underside cards");
    }

    [TestCase]
    public void CardsHaveBranchAnchorsAndDistinctFacings()
    {
        var (table, palette) = Data.Value;
        foreach (var s in table.Species.Where(s => s.Deciduous))
        {
            var model = MotherModels.Build(s, 0, VegLevel.L0Plus, palette);
            var anchors = Vectors(model.Crown.Custom1!).Take(model.Cards * 4);
            var tips = model.BranchTips;
            // Every card anchor is within 0.4 m of a branch tip that some crown mass hangs from (WLD-VEGETATION §1.8).
            foreach (var a in anchors)
                if (tips.Min(t => Vector3.Distance(t, a)) > 0.4f) throw new InvalidOperationException($"{s.Id}: card anchor floats");
            var normals = Vectors(model.Crown.Normal, 3).Take(model.Cards * 4).Select(n => (MathF.Round(n.X), MathF.Round(n.Z))).Distinct().Count();
            if (normals < 3) throw new InvalidOperationException($"{s.Id}: fewer than three card facings");
        }
    }

    [TestCase]
    public void CardsHugTheCrown()
    {
        // max-011: cards sit on the outer masses and reach at most 5% of the crown diameter beyond them; no floating bits.
        var (table, palette) = Data.Value;
        foreach (var s in table.Species.Where(s => s.Form != "palm"))
        {
            var model = MotherModels.Build(s, 0, VegLevel.L0Plus, palette);
            var masses = MassSpheres(model);
            var corners = Vectors(model.Crown.Position, 3).Take(model.Cards * 4);
            var limit = MotherModels.CardReach * model.CrownDiameter;
            foreach (var p in corners)
            {
                var outside = masses.Min(m => Vector3.Distance(p, m.Center) - m.Radius);
                if (outside > limit) throw new InvalidOperationException($"{s.Id}: card corner {outside:F2} m outside the crown (limit {limit:F2} m)");
            }
        }
    }

    [TestCase]
    public void CardsComeInPressedClusters()
    {
        // max-011 review ④: cards come 3–5 to a cluster at every level (a level keeps whole clusters), the cards of a
        // cluster share their rank and h2, and the cluster's first card (its centre) sits inside a crown mass (measured on
        // the L0+ masses, the finest outline; lower levels draw the same masses with fewer vertices).
        var (table, palette) = Data.Value;
        foreach (var s in table.Species.Where(s => s.Form != "palm"))
            foreach (var level in new[] { VegLevel.L0Plus, VegLevel.L0, VegLevel.L1 })
            {
                var model = MotherModels.Build(s, 0, level, palette);
                var masses = MassSpheres(MotherModels.Build(s, 0, VegLevel.L0Plus, palette));
                var c0 = model.Crown.Custom0!;
                var c2 = model.Crown.Custom2!;
                var clusters = Enumerable.Range(0, model.Cards).GroupBy(i => c2[i * 16 + 3]).ToList();
                foreach (var cluster in clusters)
                {
                    var cards = cluster.ToList();
                    if (cards.Count is < MotherModels.ClusterMin or > MotherModels.ClusterMax)
                        throw new InvalidOperationException($"{s.Id}/{level}: a cluster of {cards.Count} cards");
                    if (cards.Select(i => c0[i * 16 + 3]).Distinct().Count() != 1)
                        throw new InvalidOperationException($"{s.Id}/{level}: cluster cards differ in h2");
                    var centre = new Vector3(c0[cards[0] * 16], c0[cards[0] * 16 + 1], c0[cards[0] * 16 + 2]);
                    var outside = masses.Min(m => Vector3.Distance(centre, m.Center) - m.Radius);
                    if (outside > 0f)
                        throw new InvalidOperationException($"{s.Id}/{level}: cluster centre {outside:F3} m outside every crown mass (diameter {model.CrownDiameter:F1})");
                }
            }
    }

    [TestCase]
    public void CrownSharesOneShellNormal()
    {
        // max-011: where two masses meet on the outside of the crown, their vertices carry nearly the same normal (the
        // shell's), so masses never shade apart. Hidden vertices deep inside the crown (AO pushed down) are skipped.
        var (table, palette) = Data.Value;
        foreach (var id in new[] { "ginkgo", "cherry", "camphor", "osmanthus" })
        {
            var model = MotherModels.Build(table.Get(id), 0, VegLevel.L0, palette);
            var first = model.Cards * 4;
            var p = Vectors(model.Crown.Position, 3).Skip(first).ToArray();
            var n = Vectors(model.Crown.Normal, 3).Skip(first).ToArray();
            var ao = model.Crown.Ao!.Skip(first).ToArray();
            var owner = Vectors(model.Crown.Custom0!).Skip(first).ToArray();
            var worst = 1f;
            var radius = 0.015f * model.CrownDiameter;
            for (var i = 0; i < p.Length; i++)
                for (var j = i + 1; j < p.Length; j++)
                    if (owner[i] != owner[j] && ao[i] > 0.97f && ao[j] > 0.97f && Vector3.Distance(p[i], p[j]) < radius)
                        worst = MathF.Min(worst, Vector3.Dot(n[i], n[j]));
            if (worst < 0.9f) throw new InvalidOperationException($"{id}: masses meet with normals at dot {worst:F2}");
        }
    }

    [TestCase]
    public void TallCrownsHaveNoHoles()
    {
        // max-011: metasequoia and ginkgo crowns showed sky through gaps between masses. Look through the crown at its
        // axis from 12 sides and 10 heights over the middle 80% of the crown: every sight line must meet a mass.
        var (table, palette) = Data.Value;
        foreach (var id in new[] { "metaseq", "ginkgo" })
            for (var v = 0; v < TreeForms.Variants; v++)
            {
                var model = MotherModels.Build(table.Get(id), v, VegLevel.L0, palette);
                var masses = MassSpheres(model);
                var low = masses.Min(m => m.Center.Y - m.Radius);
                var high = masses.Max(m => m.Center.Y + m.Radius);
                var axis = masses.Aggregate(Vector3.Zero, (a, m) => a + m.Center) / masses.Count;
                for (var k = 0; k < 10; k++)
                {
                    var y = low + (high - low) * (0.1f + 0.8f * (k + 0.5f) / 10f);
                    for (var j = 0; j < 12; j++)
                    {
                        var a = j * 6.2831853f / 12f;
                        var dir = new Vector3(DetMath.Cos(a), 0f, DetMath.Sin(a));
                        var through = new Vector3(axis.X, y, axis.Z);
                        // A sight line through the axis point meets a sphere when the sphere comes within its radius of the line.
                        bool Meets((Vector3 Center, float Radius) m)
                        {
                            var rel = m.Center - through;
                            return (rel - dir * Vector3.Dot(rel, dir)).Length() < m.Radius;
                        }
                        if (!masses.Any(Meets)) throw new InvalidOperationException($"{id}/{v}: sky through the crown at y {y:F1}, azimuth {j * 30}°");
                    }
                }
            }
    }

    /// <summary>Each crown mass as a sphere: centre from CUSTOM0, radius from its farthest vertex.</summary>
    private static List<(Vector3 Center, float Radius)> MassSpheres(MotherModel model)
    {
        var first = model.Cards * 4;
        var p = Vectors(model.Crown.Position, 3).Skip(first).ToArray();
        var c = Vectors(model.Crown.Custom0!).Skip(first).ToArray();
        var byCenter = new Dictionary<Vector3, float>();
        for (var i = 0; i < p.Length; i++)
            byCenter[c[i]] = MathF.Max(byCenter.GetValueOrDefault(c[i]), Vector3.Distance(p[i], c[i]));
        return byCenter.OrderBy(e => e.Key.X).ThenBy(e => e.Key.Y).ThenBy(e => e.Key.Z).Select(e => (e.Key, e.Value)).ToList();
    }

    [TestCase]
    public void TreesStandOnTheirRootsAndCarryTheirCrown()
    {
        var (table, palette) = Data.Value;
        foreach (var s in table.Species)
        {
            var m = MotherModels.Build(s, 0, VegLevel.L0, palette);
            if (MathF.Abs(m.Bounds.Min.Y + MotherModels.Burial) > 0.05f && s.Form != "shrub")
                throw new InvalidOperationException($"{s.Id}: trunk not buried {m.Bounds.Min.Y}");
            // Clipped shrub balls sit a little lower than their nominal height (no stem, flattened top cluster).
            if (m.Bounds.Max.Y < m.Height * (s.Form == "shrub" ? 0.8f : 0.85f) || m.Bounds.Max.Y > m.Height * 1.35f)
                throw new InvalidOperationException($"{s.Id}: top {m.Bounds.Max.Y} for height {m.Height}");
            if (m.TrunkRadius <= 0f || m.BranchHeight <= 0f) throw new InvalidOperationException($"{s.Id}: collider data");
        }
    }

    private static Vector3[] Vectors(float[] data, int stride = 4)
    {
        var result = new Vector3[data.Length / stride];
        for (var i = 0; i < result.Length; i++) result[i] = new(data[i * stride], data[i * stride + 1], data[i * stride + 2]);
        return result;
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }
}
