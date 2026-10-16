using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

/// <summary>The far stand-in, the pool cross-fade policy and their mirror constants in veg_card / veg_trunk (max-011 23c2c).</summary>
[TestSuite]
public sealed class VegFadeTests
{
    private static readonly Lazy<SpeciesTable> Table = new(() =>
        SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(FindRepository(), "game", "data", "vegetation", "species.json"))));

    [TestCase]
    public void FarStandInIsLightTaggedAndShapedLikeL1()
    {
        var palette = MotherModelTests.Palette;
        foreach (var s in Table.Value.Species)
            for (var v = 0; v < TreeForms.Variants; v++)
            {
                var (wood, crown) = MotherModels.FarStandIn(s, v, palette);
                var tris = (wood.Index.Length + crown.Index.Length) / 3;
                if (tris is <= 0 or > 300) throw new InvalidOperationException($"{s.Id}/{v}: {tris} triangles (ENG-PERF §9 L2 ≤ 300)");
                if (crown.Index.Length == 0) throw new InvalidOperationException($"{s.Id}/{v}: no crown");
                for (var i = 3; i < crown.Custom2!.Length; i += 4)
                    if (crown.Custom2[i] != MotherModels.FarMassTag) throw new InvalidOperationException($"{s.Id}/{v}: mass tag {crown.Custom2[i]}");
                for (var i = 3; i < wood.Custom2!.Length; i += 4)
                    if (wood.Custom2[i] != MotherModels.FarWoodTag) throw new InvalidOperationException($"{s.Id}/{v}: wood tag {wood.Custom2[i]}");
                // Same vertex layout and material as the near crown and wood, so the far pool shares their warmed pipelines.
                var l1 = MotherModels.Build(s, v, VegLevel.L1, palette);
                if (crown.Material != l1.Crown.Material || (crown.Uv is null) != (l1.Crown.Uv is null) || crown.Flags?[0] != l1.Crown.Flags?[0])
                    throw new InvalidOperationException($"{s.Id}/{v}: crown layout differs from L1");
                if (wood.Index.Length > 0 && (wood.Material != l1.Wood.Material || (wood.Uv is null) != (l1.Wood.Uv is null)))
                    throw new InvalidOperationException($"{s.Id}/{v}: wood layout differs from L1");
                if (TreeForms.IsBall(s.Form) != (wood.Index.Length == 0)) throw new InvalidOperationException($"{s.Id}/{v}: wood presence");
                // The stand-in stays within the L1 crown's bulk (the masses are the same lumpy surfaces at a coarser sampling).
                var (lo, hi) = Extent(crown.Position);
                var (l1lo, l1hi) = Extent(l1.Crown.Position);
                // A palm's stand-in is a swollen head ball standing in for the fronds: only its size is checked.
                if (s.Form != "palm" && (hi.Y > l1hi.Y + 0.05f || lo.Y < l1lo.Y - 0.05f)) throw new InvalidOperationException($"{s.Id}/{v}: stand-in crown outgrows L1");
                if (hi.Y < l1hi.Y - 0.25f * (l1hi.Y - l1lo.Y)) throw new InvalidOperationException($"{s.Id}/{v}: stand-in crown much smaller than L1");
            }
        // Near models keep the near tags (wood 0, masses −1).
        var near = MotherModels.Build(Table.Value.Get("ginkgo"), 0, VegLevel.L1, palette);
        if (near.Wood.Custom2!.Where((_, i) => i % 4 == 3).Any(w => w != 0f)) throw new InvalidOperationException("near wood tagged");
    }

    [TestCase]
    public void PoolsOverlapOnlyInTheBand()
    {
        Expect(VegFade.Pools(60f), (true, false), "60 m");
        Expect(VegFade.Pools(134f), (true, false), "134 m");
        Expect(VegFade.Pools(150f), (true, true), "150 m");
        Expect(VegFade.Pools(166f), (false, true), "166 m");
        // No crown clouds yet: the far stand-in stays at any distance.
        Expect(VegFade.Pools(1500f), (false, !VegFade.CloudReady || 1500f < VegFade.CloudFadeEnd), "1500 m");
        // The detail factor scales the band.
        Expect(VegFade.Pools(120f, 0.8f), (true, true), "120 m at 0.8");
        if (VegFade.FarShare(130f) != 0f || VegFade.FarShare(170f) != 1f || Math.Abs(VegFade.FarShare(150f) - 0.5f) > 1e-4f)
            throw new InvalidOperationException("far share");
        for (var d = 130f; d < 170f; d += 1f)
            if (VegFade.FarShare(d + 1f) < VegFade.FarShare(d)) throw new InvalidOperationException("far share not monotone");
    }

    [TestCase]
    public void ShadersMirrorTheFadeConstants()
    {
        foreach (var name in new[] { "veg_card.gdshader", "veg_trunk.gdshader" })
        {
            var text = File.ReadAllText(Path.Combine(FindRepository(), "game", "style", "shaders", name));
            ExpectVec2(text, "VEG_POOL_FADE", VegFade.NearFadeStart, VegFade.NearFadeEnd, name);
            ExpectVec2(text, "VEG_CLOUD_FADE", VegFade.CloudFadeStart, VegFade.CloudFadeEnd, name);
            ExpectVec2(text, "VEG_UNDER_FADE", Understorey.FadeStart, Understorey.DrawDistance, name);
            if (!Regex.IsMatch(text, $@"const bool VEG_CLOUD_READY = {(VegFade.CloudReady ? "true" : "false")};"))
                throw new InvalidOperationException($"{name}: VEG_CLOUD_READY differs from VegFade.CloudReady");
            if (!Regex.IsMatch(text, $@"const int VEG_VARIANT_UNDER = {Understorey.UnderVariantBit};"))
                throw new InvalidOperationException($"{name}: VEG_VARIANT_UNDER differs from Understorey.UnderVariantBit");
        }
    }

    [TestCase]
    public void UnderstoreyPlantsCarryTheFadeBit()
    {
        var s = Table.Value.Get("loropetalum");
        var plant = new GroundPlant("k", s, 0, default, 0f, System.Numerics.Vector3.One, UnderKind.Shrub, 0.3f, 0.5f);
        var code = (int)MathF.Round(plant.Custom.Y);
        if (code / 16 != s.Row || (code % 16 & Understorey.UnderVariantBit) == 0) throw new InvalidOperationException($"custom.y {code}");
        if ((Understorey.UnderVariantBit & 1) != 0) throw new InvalidOperationException("bit 0 is the whitewash");
    }

    private static void Expect((bool, bool) got, (bool, bool) want, string label)
    {
        if (got != want) throw new InvalidOperationException($"{label}: pools {got}, expected {want}");
    }

    private static void ExpectVec2(string text, string name, float a, float b, string file)
    {
        var m = Regex.Match(text, $@"const vec2 {name} = vec2\(([-0-9.]+), ([-0-9.]+)\);");
        if (!m.Success) throw new InvalidOperationException($"{file}: {name} not found");
        var x = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var y = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        if (x != a || y != b) throw new InvalidOperationException($"{file}: {name} = ({x}, {y}), expected ({a}, {b})");
    }

    private static (System.Numerics.Vector3 Lo, System.Numerics.Vector3 Hi) Extent(float[] p)
    {
        var lo = new System.Numerics.Vector3(float.MaxValue);
        var hi = new System.Numerics.Vector3(float.MinValue);
        for (var i = 0; i < p.Length; i += 3)
        {
            var v = new System.Numerics.Vector3(p[i], p[i + 1], p[i + 2]);
            lo = System.Numerics.Vector3.Min(lo, v);
            hi = System.Numerics.Vector3.Max(hi, v);
        }
        return (lo, hi);
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
