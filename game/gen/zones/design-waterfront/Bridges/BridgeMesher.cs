using Campus.Gen.Zones.Common;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using T = Campus.Gen.Zones.DesignWaterfront.WaterfrontTune;

namespace Campus.Gen.Zones.DesignWaterfront.Bridges;

/// <summary>
/// Geometry of one bridge in world coordinates (ART-MODELING 收边与倒角, §7.4, §7.6):
/// a concrete slab with a flat soffit; asphalt on the carriageway, paving on the walking strips, granite kerbs between them,
/// all at the WP-15 ground levels so the approaches meet flush; a continuous white-stone parapet base (地栿) that overhangs
/// the fascia as its drip line; square stone posts with plain caps every ~2 m between taller bridge-head posts; two
/// stainless rails that cast no shadow; abutment walls at the banks. Levels: L0 everything; L1 without the rails;
/// L2/L3 the deck, base and head posts; L4 nothing (the deck is below a pixel there).
/// </summary>
internal static class BridgeMesher
{
    private const float Joint = 0.003f;

    public static PartGeometry? Build(BridgePlan plan, LodLevel lod)
    {
        if (lod == LodLevel.L4) return null;
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var top = plan.RoadY; var walk = plan.WalkY;
        var slabTop = (plan.HasCarriage ? top : walk) - T.Surfacing;
        var soffit = top - T.SlabDepth;

        // Slab, fascia faces at the outer edges.
        b.Add(PartGeometryKernel.BeveledExtrude(plan.Footprint, slabTop + Joint - soffit, near ? 0.03f : 0f, T.ConcreteSpec, soffit));
        foreach (var band in plan.Bands)
        {
            if (band.Kind == BandKind.Carriage)
            {
                b.Add(PartGeometryKernel.BeveledExtrude(plan.Strip(band.U0, band.U1), top - slabTop, 0f, T.AsphaltSpec, slabTop));
                continue;
            }
            // Walking strip; on a carriage bridge its inner edge is a kerb stone flush with the paving.
            float u0 = band.U0, u1 = band.U1;
            if (plan.HasCarriage)
            {
                var inner = band.U0 < 0 ? band.U1 : band.U0;
                var k0 = band.U0 < 0 ? inner - T.KerbWidth : inner;
                b.Add(PartGeometryKernel.BeveledExtrude(plan.Strip(k0, k0 + T.KerbWidth), walk - slabTop, near ? T.KerbBevel : 0f, T.KerbSpec, slabTop));
                if (band.U0 < 0) u1 -= T.KerbWidth - Joint; else u0 += T.KerbWidth - Joint;
            }
            b.Add(PartGeometryKernel.BeveledExtrude(plan.Strip(u0, u1), walk - slabTop + Joint, 0f, T.PaverSpec, slabTop - Joint));
        }

        // Parapet bases, overhanging the fascia.
        var baseTop = walk + T.BaseRise;
        foreach (var (inner, outer) in Sides(plan))
        {
            var lo = Math.Min(inner, outer); var hi = Math.Max(inner, outer);
            b.Add(PartGeometryKernel.BeveledExtrude(plan.Strip(lo, hi), baseTop - slabTop, near ? T.BaseBevel : 0f, T.StoneSpec, slabTop));
        }

        // Posts, head posts and rails.
        foreach (var (inner, outer) in Sides(plan))
        {
            var u = (inner + outer) * 0.5f;
            float s0 = plan.StartS(u), s1 = plan.EndS(u);
            var h0 = s0 + T.HeadSize * 0.5f; var h1 = s1 - T.HeadSize * 0.5f;
            Post(b, plan, h0, u, T.HeadSize, baseTop, walk + T.HeadTop, T.HeadCap, T.HeadCapHeight, near);
            Post(b, plan, h1, u, T.HeadSize, baseTop, walk + T.HeadTop, T.HeadCap, T.HeadCapHeight, near);
            if (lod is LodLevel.L2 or LodLevel.L3) continue;
            var run = h1 - h0;
            var count = Math.Max(1, (int)MathF.Round(run / T.PostSpacing));
            for (var i = 1; i < count; i++)
                Post(b, plan, h0 + run * i / count, u, T.PostSize, baseTop, walk + T.PostTop, T.CapSize, T.CapHeight, near);
            if (!near) continue;
            foreach (var y in new[] { walk + T.RailLow, walk + T.RailHigh })
                b.AddPipe(plan.P(h0, u, y), plan.P(h1, u, y), T.RailRadius, T.RailSegments, T.RailSpec, T.NoShadow);
        }

        // Abutments under the deck ends, from below any bed up into the slab.
        foreach (var a in plan.Abutments)
            b.Add(PartGeometryKernel.BeveledExtrude(PlanGeometry.OpenRing([a.A, a.B, a.C, a.D]).ToArray(), soffit + Joint * 10f - (plan.RoadY + T.AbutmentBottom), 0f, T.ConcreteSpec, plan.RoadY + T.AbutmentBottom));

        var foot = plan.Footprint;
        float minX = foot.Min(p => p.X) - 1f, maxX = foot.Max(p => p.X) + 1f, minZ = foot.Min(p => p.Y) - 1f, maxZ = foot.Max(p => p.Y) + 1f;
        return b.Finish(new(new(minX, plan.RoadY + T.AbutmentBottom, minZ), new(maxX, walk + T.HeadTop + 0.1f, maxZ)));
    }

    /// <summary>Each side's parapet strip as (inner edge, outer edge including the overhang): left then right.</summary>
    internal static IEnumerable<(float Inner, float Outer)> Sides(BridgePlan plan)
    {
        yield return (plan.Left + Joint, plan.OuterLeft - T.BaseOverhang);
        yield return (plan.Right - Joint, plan.OuterRight + T.BaseOverhang);
    }

    /// <summary>Square post turned to the bridge axis, standing on the base, with a plain square cap.</summary>
    private static void Post(PartBuilder b, BridgePlan plan, float s, float u, float size, float bottom, float topY, float cap, float capHeight, bool near)
    {
        var at = new PlanFrame(plan.P(s, u), plan.Axis).At(0, 0, 0);
        var shaft = topY - capHeight + Joint - (bottom - Joint);
        b.Add(PartGeometryKernel.BeveledBox(size, shaft, size, near ? T.PostBevel : 0f, T.StoneSpec), Xf.Move(0, bottom - Joint, 0).Then(at));
        b.Add(PartGeometryKernel.BeveledBox(cap, capHeight, cap, near ? T.CapBevel : 0f, T.StoneSpec), Xf.Move(0, topY - capHeight, 0).Then(at));
    }

}
