using System;
using System.IO;
using System.Linq;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

/// <summary>Far stand-in geometry and the generated understorey fade bit.</summary>
[TestSuite]
public sealed class VegFadeTests
{
    private static readonly Lazy<SpeciesTable> Table = new(() =>
        SpeciesTable.Parse(File.ReadAllBytes(Path.Combine(RepositoryFixture.Root, "game", "data", "vegetation", "species.json"))));

    [TestCase, TestCategory(TestReason.Contract)]
    public void FarStandInIsLightTaggedAndShapedLikeL1()
    {
        var palette = MotherModelTests.Palette;
        foreach (var s in Table.Value.Species)
            for (var v = 0; v < TreeForms.Variants; v++)
            {
                var (wood, crown) = MotherModels.FarStandIn(s, v, palette);
                var tris = (wood.Index.Length + crown.Index.Length) / 3;
                if (tris <= 0) throw new InvalidOperationException($"{s.Id}/{v}: no triangles");
                if (crown.Index.Length == 0) throw new InvalidOperationException($"{s.Id}/{v}: no crown");
                for (var i = 3; i < crown.Custom2!.Length; i += 4)
                    if (crown.Custom2[i] != MotherModels.FarMassTag) throw new InvalidOperationException($"{s.Id}/{v}: mass tag {crown.Custom2[i]}");
                for (var i = 3; i < wood.Custom2!.Length; i += 4)
                    if (wood.Custom2[i] != MotherModels.FarWoodTag) throw new InvalidOperationException($"{s.Id}/{v}: wood tag {wood.Custom2[i]}");
                // Same vertex layout and material as the near crown and wood, so the far pool shares their warmed pipelines.
                var l1 = MotherModelTests.Mother(s, v, VegLevel.L1);
                if (crown.Material != l1.Crown.Material || (crown.Uv is null) != (l1.Crown.Uv is null) || crown.Flags?[0] != l1.Crown.Flags?[0])
                    throw new InvalidOperationException($"{s.Id}/{v}: crown layout differs from L1");
                if (wood.Index.Length > 0 && (wood.Material != l1.Wood.Material || (wood.Uv is null) != (l1.Wood.Uv is null)))
                    throw new InvalidOperationException($"{s.Id}/{v}: wood layout differs from L1");
                if (TreeForms.IsBall(s.Form) != (wood.Index.Length == 0)) throw new InvalidOperationException($"{s.Id}/{v}: wood presence");
                // The stand-in stays within the L1 crown's bulk (the masses are the same lumpy surfaces at a coarser sampling).
                var (lo, hi) = Extent(crown.Position);
                var (l1lo, l1hi) = Extent(l1.Crown.Position);
                // A palm's stand-in is a swollen head ball standing in for the fronds: only its size is checked.
                if (s.Form != "palm" && (hi.Y > l1hi.Y + TestTolerance.Layout || lo.Y < l1lo.Y - TestTolerance.Layout)) throw new InvalidOperationException($"{s.Id}/{v}: stand-in crown outgrows L1");
                if (hi.Y < l1hi.Y - 0.25f * (l1hi.Y - l1lo.Y)) throw new InvalidOperationException($"{s.Id}/{v}: stand-in crown much smaller than L1");
            }
        // Near models keep the near tags (wood 0, masses −1).
        var near = MotherModelTests.Mother(Table.Value.Get("ginkgo"), 0, VegLevel.L1);
        if (near.Wood.Custom2!.Where((_, i) => i % 4 == 3).Any(w => w != 0f)) throw new InvalidOperationException("near wood tagged");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void UnderstoreyPlantsCarryTheFadeBit()
    {
        var s = Table.Value.Get("loropetalum");
        var plant = new GroundPlant("k", s, 0, default, 0f, System.Numerics.Vector3.One, UnderKind.Shrub, 0.3f, 0.5f);
        var code = (int)MathF.Round(plant.Custom.Y);
        if (code / 16 != s.Row || (code % 16 & Understorey.UnderVariantBit) == 0) throw new InvalidOperationException($"custom.y {code}");
        if ((Understorey.UnderVariantBit & 1) != 0) throw new InvalidOperationException("bit 0 is the whitewash");
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

}
