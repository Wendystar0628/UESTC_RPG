using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Zones.DesignBoundary.Fence;
using Campus.Gen.Zones.DesignBoundary.Framework;
using F = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Fence;
using L = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Look;
using R = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Grate;
using SG = Campus.Gen.Zones.DesignBoundary.BoundaryTune.SideGate;

namespace Campus.Gen.Zones.DesignBoundary.Ring;

/// <summary>
/// The campus boundary fence all the way round (30b; WLD-OUTSKIRTS §7.1, MAP-QSH-ADDITIONS §2.11): every run of
/// <see cref="BoundaryPlan"/> in the north cluster fences' iron language with its hedge pieces, the closed side gates where
/// other roads cross the ring, and the river grates where water does (WLD-OUTSKIRTS §7.3). The gates themselves are
/// <see cref="Gates.GateSite"/>s. Every collider here is Boundary.
/// </summary>
public sealed class RingFenceSite : BoundarySite
{
    public override string Id => "30b.ring";

    public override void Build(SiteBuilder b)
    {
        if (BoundaryPlan.Of(b.Context.World) is not { } plan) return;
        foreach (var run in plan.Runs) BoundaryFence.Build(b, run);
        foreach (var o in plan.Openings)
        {
            switch (o.Kind)
            {
                case OpeningKind.Gate: break;
                case OpeningKind.SideGate: SideGate(b, plan, o); break;
                case OpeningKind.Grate: Grate(b, plan, o); break;
                default: throw new InvalidOperationException($"Unknown opening kind {o.Kind}.");
            }
        }
    }

    public override void Collide(SiteColliders c)
    {
        if (BoundaryPlan.Of(c.Context.World) is not { } plan) return;
        foreach (var run in plan.Runs) BoundaryFence.Collide(c, run);
        foreach (var o in plan.Openings)
        {
            switch (o.Kind)
            {
                case OpeningKind.Gate: break;
                case OpeningKind.SideGate:
                    {
                        var (a, d) = plan.Ends(o);
                        var y = MathF.Min(c.Ground(a), c.Ground(d));
                        c.Wall(a, d, 0.15f, y - 0.5f, y + F.ColliderTop, SurfaceTag.Boundary, FootMat.Metal);
                        break;
                    }
                case OpeningKind.Grate:
                    {
                        var (a, d) = plan.Ends(o);
                        var (bank, bed) = Levels(c.Ground, a, d);
                        c.Wall(a, d, 0.12f, bed - 0.5f, bank + R.Top, SurfaceTag.Boundary, FootMat.Metal);
                        break;
                    }
                default: throw new InvalidOperationException($"Unknown opening kind {o.Kind}.");
            }
        }
    }

    /// <summary>Bank top (the higher end) and the lowest ground between the two ends less the grate's foot.</summary>
    private static (float Bank, float Bed) Levels(Func<Vector2, float> ground, Vector2 a, Vector2 d)
    {
        var bank = MathF.Max(ground(a), ground(d));
        var low = bank;
        for (var k = 0; k <= 8; k++) low = MathF.Min(low, ground(a + (d - a) * (k / 8f)));
        return (bank, low - R.Foot);
    }

    /// <summary>
    /// A closed side gate (常闭侧门) between the two end pillars: iron leaves in the fence's language — stiles, bottom, middle and
    /// top rails, 25 mm bars at the fence's pitch — two leaves meeting in the middle, one for a footway.
    /// </summary>
    private static void SideGate(SiteBuilder b, BoundaryPlan plan, Opening o)
    {
        var (pa, pd) = plan.Ends(o);
        var span = Vector2.Distance(pa, pd);
        if (span < 1f) return;
        var u = (pd - pa) / span;
        var face = F.EndPillarSize * 0.5f + 0.02f;
        var a = pa + u * face;
        var d = pd - u * face;
        var y = MathF.Min(b.Ground(pa), b.Ground(pd));
        if (o.Width <= SG.FootwayWidth) Leaf(b, a, d, u, y);
        else
        {
            var mid = (a + d) * 0.5f;
            Leaf(b, a, mid - u * (SG.Seam * 0.5f), u, y);
            Leaf(b, mid + u * (SG.Seam * 0.5f), d, u, y);
        }
    }

    private static void Leaf(SiteBuilder b, Vector2 a, Vector2 d, Vector2 u, float y)
    {
        var top = y + F.TopRail + F.RailSize;
        b.Run(L.Steel, a, d, SG.Rail, y + SG.Bottom, y + SG.Bottom + SG.Rail);
        b.Run(L.Steel, a, d, SG.Rail, y + F.TopRail, top);
        if (b.Detail == SiteDetail.Mass) return;
        b.Run(L.Steel, a, d, SG.Rail, y + SG.MidRail, y + SG.MidRail + SG.Rail);
        b.Box(L.Steel, a + u * (SG.Stile * 0.5f), u, SG.Stile, SG.Stile, y + SG.Bottom, top);
        b.Box(L.Steel, d - u * (SG.Stile * 0.5f), u, SG.Stile, SG.Stile, y + SG.Bottom, top);
        var pitch = b.Detail == SiteDetail.Full ? F.BarPitch : F.BarPitch * 4;
        var s0 = SG.Stile;
        var s1 = Vector2.Distance(a, d) - SG.Stile;
        var count = (int)MathF.Floor((s1 - s0) / pitch);
        if (count < 1) return;
        var start = s0 + (s1 - s0 - (count - 1) * pitch) * 0.5f;
        for (var k = 0; k < count; k++)
            b.Box(L.SteelBar, a + u * (start + k * pitch), u, F.BarSize, F.BarSize, y + SG.Bottom + SG.Rail, y + F.BarTop, caps: false, noShadow: true);
    }

    /// <summary>
    /// River grate (拦污格栅) between the end pillars on the two bank tops: deep grey flat bars across the whole channel from
    /// below the bed to 1.5 m above the bank, a top beam and a waist rail just above the bank line.
    /// </summary>
    private static void Grate(SiteBuilder b, BoundaryPlan plan, Opening o)
    {
        var (pa, pd) = plan.Ends(o);
        var span = Vector2.Distance(pa, pd);
        if (span < 1f) return;
        var u = (pd - pa) / span;
        var face = F.EndPillarSize * 0.5f;
        var a = pa + u * face;
        var d = pd - u * face;
        var (bank, bed) = Levels(b.Ground, pa, pd);
        var top = bank + R.Top;
        b.Run(L.Steel, a, d, R.BeamDepth, top - R.BeamHeight, top);
        b.Run(L.Steel, a, d, R.BeamDepth * 0.7f, bank + R.WaistY, bank + R.WaistY + R.WaistHeight);
        if (b.Detail == SiteDetail.Mass) return;
        var pitch = b.Detail == SiteDetail.Full ? R.BarPitch : R.BarPitch * 3;
        var length = Vector2.Distance(a, d);
        var count = (int)MathF.Floor((length - 0.1f) / pitch);
        if (count < 1) return;
        var start = (length - (count - 1) * pitch) * 0.5f;
        for (var k = 0; k < count; k++)
            b.Box(L.SteelBar, a + u * (start + k * pitch), u, R.BarWidth, R.BarDepth, bed, top - R.BeamHeight, caps: false, noShadow: true);
    }
}
