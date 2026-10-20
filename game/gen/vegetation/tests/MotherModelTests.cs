using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

[TestSuite]
public sealed class MotherModelTests
{
    private static readonly Lazy<(SpeciesTable Table, PaletteCatalog Palette)> Data = new(Load);

    internal static PaletteCatalog Palette => Data.Value.Palette;

    private static (SpeciesTable, PaletteCatalog) Load()
    {
        var root = RepositoryFixture.Root;
        var table = SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json")));
        return (table, Campus.Contracts.Tests.PaletteFixture.Catalog);
    }

    private static readonly ConcurrentDictionary<(string Species, int Variant, VegLevel Level), MotherModel> Mothers = new();

    /// <summary>A mother model built once per species, variant and level; the tests only read it.</summary>
    internal static MotherModel Mother(SpeciesInfo species, int variant, VegLevel level) =>
        Mothers.GetOrAdd((species.Id, variant, level), key => MotherModels.Build(species, key.Variant, key.Level, Palette));

    [TestCase, TestCategory(TestReason.Contract)]
    public void SpeciesTableCoversTheVegetationDoc()
    {
        var (table, palette) = Data.Value;
        if (table.Species.Count == 0) throw new InvalidOperationException($"only {table.Species.Count} species");
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
        // Bark has its own registered pair.
        _ = palette.Resolve(MotherModels.Bark);
        _ = palette.Resolve(MotherModels.BarkShade);
    }

    [TestCase, TestCategory(TestReason.Contract)]
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

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
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

    [TestCase, TestCategory(TestReason.Contract), TestCategory("Slow")]
    public void LevelsAreNested()
    {
        var table = Data.Value.Table;
        foreach (var s in table.Species)
            for (var v = 0; v < TreeForms.Variants; v++)
            {
                var near = Mother(s, v, VegLevel.L0Plus).Triangles;
                var mid = Mother(s, v, VegLevel.L0).Triangles;
                var far = Mother(s, v, VegLevel.L1).Triangles;
                if (!(near >= mid && mid >= far)) throw new InvalidOperationException($"{s.Id}/{v}: levels not nested {near}/{mid}/{far}");
            }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void LowerLevelCardsArePrefixes()
    {
        var table = Data.Value.Table;
        foreach (var id in new[] { "ginkgo", "osmanthus", "nanmu" })
        {
            var cards = new[] { VegLevel.L0Plus, VegLevel.L0, VegLevel.L1 }
                .Select(level => Mother(table.Get(id), 0, level))
                .Select(m => m.Crown.Custom0!.Take((m.Cards - m.UnderCards) * 4 * 4).ToArray()).ToArray();
            for (var i = 1; i < cards.Length; i++)
                if (cards[i].Length > cards[i - 1].Length || !cards[i].SequenceEqual(cards[i - 1].Take(cards[i].Length)))
                    throw new InvalidOperationException($"{id}: level {i} cards are not a prefix");
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void UnderCardsOnlyNearAndUnderTallCrowns()
    {
        // max-011 23c3: crowns one can stand under carry an underside layer at L0+ and, for card and mixed crowns, its leading
        // share at L0 (a prefix of whole clusters); L1, low crowns and palms carry none. The layer faces down.
        var table = Data.Value.Table;
        foreach (var (id, throughL0) in new[] { ("ginkgo", true), ("camphor", true), ("koelreuteria", false) })
        {
            var s = table.Get(id);
            var near = Mother(s, 0, VegLevel.L0Plus);
            var mid = Mother(s, 0, VegLevel.L0);
            var far = Mother(s, 0, VegLevel.L1);
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
            if (Mother(table.Get(id), 0, VegLevel.L0Plus).UnderCards != 0)
                throw new InvalidOperationException($"{id}: a low crown has underside cards");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CardsHaveBranchAnchorsAndDistinctFacings()
    {
        var table = Data.Value.Table;
        foreach (var s in table.Species.Where(s => s.Deciduous))
        {
            var model = Mother(s, 0, VegLevel.L0Plus);
            var anchors = Vectors(model.Crown.Custom1!).Take(model.Cards * 4);
            var tips = model.BranchTips;
            // Every card anchor is within 0.4 m of a branch tip that some crown mass hangs from (WLD-VEGETATION).
            foreach (var a in anchors)
                if (tips.Min(t => Vector3.Distance(t, a)) > 0.4f) throw new InvalidOperationException($"{s.Id}: card anchor floats");
            var normals = Vectors(model.Crown.Normal, 3).Take(model.Cards * 4).Select(n => (MathF.Round(n.X), MathF.Round(n.Z))).Distinct().Count();
            if (normals < 3) throw new InvalidOperationException($"{s.Id}: fewer than three card facings");
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CardsHugTheCrown()
    {
        // max-011: cards sit on the outer masses and reach at most 5% of the crown diameter beyond them; no floating bits.
        var table = Data.Value.Table;
        foreach (var s in table.Species.Where(s => TreeForms.Get(s.Form).Structure != TreeStructure.TerminalHead))
        {
            var model = Mother(s, 0, VegLevel.L0Plus);
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void CardsComeInPressedClusters()
    {
        // max-011 review ④: cards come 3–5 to a cluster at every level (a level keeps whole clusters), the cards of a
        // cluster share their rank and h2, and the cluster's first card (its centre) sits inside a crown mass (measured on
        // the L0+ masses, the finest outline; lower levels draw the same masses with fewer vertices).
        var table = Data.Value.Table;
        foreach (var s in table.Species.Where(s => TreeForms.Get(s.Form).Structure != TreeStructure.TerminalHead))
            foreach (var level in new[] { VegLevel.L0Plus, VegLevel.L0, VegLevel.L1 })
            {
                var model = Mother(s, 0, level);
                var masses = MassSpheres(Mother(s, 0, VegLevel.L0Plus));
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void CrownSharesOneShellNormal()
    {
        // max-011: where two masses meet on the outside of the crown, their vertices carry nearly the same normal (the
        // shell's), so masses never shade apart. Hidden vertices deep inside the crown (AO pushed down) are skipped.
        var table = Data.Value.Table;
        foreach (var id in new[] { "ginkgo", "cherry", "camphor", "osmanthus" })
        {
            var model = Mother(table.Get(id), 0, VegLevel.L0);
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void TallCrownsHaveNoHoles()
    {
        // max-011: metasequoia and ginkgo crowns showed sky through gaps between masses. Look through the crown at its
        // axis from 12 sides and 10 heights over the middle 80% of the crown: every sight line must meet a mass.
        var table = Data.Value.Table;
        foreach (var id in new[] { "metaseq", "ginkgo" })
            for (var v = 0; v < TreeForms.Variants; v++)
            {
                var model = Mother(table.Get(id), v, VegLevel.L0);
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
                        var dir = new Vector3(MathF.Cos(a), 0f, MathF.Sin(a));
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void TreesStandOnTheirRootsAndCarryTheirCrown()
    {
        var table = Data.Value.Table;
        foreach (var s in table.Species)
        {
            var m = Mother(s, 0, VegLevel.L0);
            if (MathF.Abs(m.Bounds.Min.Y + MotherModels.Burial) > TestTolerance.Layout && TreeForms.Get(s.Form).Structure != TreeStructure.StemlessClusters)
                throw new InvalidOperationException($"{s.Id}: trunk not buried {m.Bounds.Min.Y}");
            // Clipped shrub balls sit a little lower than their nominal height (no stem, flattened top cluster).
            if (m.Bounds.Max.Y < m.Height * ((TreeForms.Get(s.Form).Structure == TreeStructure.StemlessClusters) ? 0.8f : 0.85f) || m.Bounds.Max.Y > m.Height * 1.35f)
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

}
