using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Look;
using P = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Plaza;

namespace Campus.Gen.Zones.DesignBoundary.Waterfront;

/// <summary>
/// The waterside plaza inside the north gate (design:boundary-north-a05-plaza): the foot of the A05 atlas kiosk and the first
/// stop of R4 after the gate (MAP-QSH-NORTH R4, SYS-ATLAS). The paving, its kerbs and edging are WP-15 ground drawn from
/// the overlay polygon; this site adds a light steel railing along the water edge and two benches whose sitters face the
/// channel and the north gate bridge. Frame: t along the bank, n off the water edge toward the road (<see cref="P"/>).
/// </summary>
public sealed class NorthPlazaSite : ZoneSite
{
    private static readonly BenchPart Bench = new();

    public override string Id => "30a.north-plaza";

    /// <summary>The bank frame: origin on the straight water edge, X along the bank, +Z off the water.</summary>
    public static PlanFrame Frame { get; } = PlanFrame.Along(P.EdgeOrigin, P.EdgeDirection);

    private static bool Present(IWorldModel world) => world.Features.ContainsKey(new(P.Feature));

    public override void Build(SiteBuilder b)
    {
        if (!Present(b.Context.World)) return;
        Railing(b);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var at in P.Benches)
        {
            var bench = Frame.Shift(at.X, at.Y);
            b.Place(PartCatalog.Bench, P.BenchSize, bench.At(0, b.Ground(bench.Origin) + P.PavingTop, 0));
        }
    }

    /// <summary>
    /// Railing on the water side: square posts at most <see cref="P.PostPitch"/> apart, a handrail and a bottom rail, 20 mm bars
    /// between them; the two end posts are a size heavier, the gate-side one with a cap (收头); the campus-side one carries on
    /// into WP-20's canal-bank railing (20a-4) and keeps no cap. Stands on the paving.
    /// </summary>
    private static void Railing(SiteBuilder b)
    {
        var length = P.RailT1 - P.RailT0;
        var panels = (int)MathF.Ceiling(length / P.PostPitch);
        var u = Frame.U;
        var a = Frame.P(P.RailT0, P.RailN);
        var c = Frame.P(P.RailT1, P.RailN);
        var y = MathF.Min(b.Ground(a), b.Ground(c)) + P.PavingTop;
        b.Run(L.LightSteel, a, c, P.HandRail, y + P.RailHeight - P.HandRail * 0.7f, y + P.RailHeight);
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        b.Run(L.LightSteel, a, c, 0.04f, y + P.BottomRail, y + P.BottomRail + 0.04f);
        for (var k = 0; k <= panels; k++)
        {
            var at = Frame.P(P.RailT0 + length * k / panels, P.RailN);
            var end = k == 0 || k == panels;
            var size = end ? P.PostSize + 0.02f : P.PostSize;
            b.Box(L.LightSteel, at, u, size, size, y - 0.02f, y + P.RailHeight - 0.01f);
            // The campus-side end post (k = 0) is shared with WP-20's canal-bank railing, which carries on from it: no cap there.
            if (end && k == panels && b.Detail == SiteDetail.Full)
                b.Add(PartGeometryKernel.BeveledBox(size + 0.04f, 0.04f, size + 0.04f, 0.008f, L.LightSteel), PlanFrame.Along(at, u).At(0, y + P.RailHeight, 0));
        }
        if (b.Detail == SiteDetail.Mass) return;
        var pitch = b.Detail == SiteDetail.Full ? P.BarPitch : P.BarPitch * 3;
        for (var k = 0; k < panels; k++)
        {
            var t0 = P.RailT0 + length * k / panels + P.PostSize;
            var t1 = P.RailT0 + length * (k + 1) / panels - P.PostSize;
            var count = (int)MathF.Floor((t1 - t0) / pitch);
            var start = t0 + (t1 - t0 - (count - 1) * pitch) * 0.5f;
            for (var i = 0; i < count; i++)
                b.Box(L.LightBar, Frame.P(start + i * pitch, P.RailN), u, P.BarSize, P.BarSize, y + P.BottomRail + 0.04f, y + P.RailHeight - P.HandRail * 0.7f, caps: false, noShadow: true);
        }
    }

    public override void Collide(SiteColliders c)
    {
        if (!Present(c.Context.World)) return;
        var a = Frame.P(P.RailT0, P.RailN);
        var d = Frame.P(P.RailT1, P.RailN);
        var y = MathF.Min(c.Ground(a), c.Ground(d)) + P.PavingTop;
        // Inside the campus: an ordinary waterside barrier, not the boundary (WLD-OUTSKIRTS), top 1.5 m (SYS-WALK).
        c.Wall(a, d, 0.08f, y - 0.6f, y + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.Metal);
        foreach (var at in P.Benches)
        {
            var bench = Frame.Shift(at.X, at.Y);
            c.Add(Bench.Collision(P.BenchSize), bench.At(0, c.Ground(bench.Origin) + P.PavingTop, 0));
        }
    }
}
