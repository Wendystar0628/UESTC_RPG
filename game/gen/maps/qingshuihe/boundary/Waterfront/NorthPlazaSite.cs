using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryTune.Look;
using P = Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryTune.Plaza;

namespace Campus.Gen.Maps.Qingshuihe.Boundary.Waterfront;

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

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        if (FrameOf(world) is not { } frame) return [];
        return [RailRange(world, frame), .. P.Benches.Select(at => SiteRanges.Point(frame.P(at.X, at.Y), P.BenchSize.Width))];
    }

    private static Campus.Contracts.Maps.MapRect RailRange(IWorldModel world, PlanFrame frame) =>
        SiteRanges.Bounds([frame.P(P.RailT0(world), P.RailN(world)), frame.P(P.RailT1, P.RailN(world))], P.EndPostSize(world) + 0.04f);

    /// <summary>
    /// The registered bank frame: origin at the selected edge's north end, X toward that end, +Z off the water.
    /// Null when the plaza or bank selection is not in the map.
    /// </summary>
    public static PlanFrame? FrameOf(IWorldModel world)
    {
        if (!world.Features.ContainsKey(new(BoundaryFeatureIds.NorthPlaza)) || !world.Features.ContainsKey(new(BoundaryFeatureIds.NorthBankContour))) return null;
        return world.ContourFrame(BoundaryFeatureIds.NorthBankContour, originAtEnd: false, forward: false);
    }

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        if (FrameOf(b.Context.World) is not { } frame) return;
        if (b.Includes(RailRange(world, frame))) Railing(b, frame);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var at in P.Benches)
        {
            var bench = frame.Shift(at.X, at.Y);
            if (!b.Includes(bench.Origin)) continue;
            b.Place(PartCatalog.Bench, P.BenchSize, bench.At(0, b.Ground(bench.Origin) + GroundLevels.Paving, 0));
        }
    }

    /// <summary>
    /// Railing on the water side: square posts at most <see cref="P.PostPitch"/> apart, a handrail and a bottom rail, 20 mm bars
    /// between them; the two end posts are a size heavier, the gate-side one with a cap (收头); the campus-side one carries on
    /// into WP-20's canal-bank railing (20a-4) and keeps no cap. Stands on the paving.
    /// </summary>
    private static void Railing(SiteBuilder b, PlanFrame frame)
    {
        var world = b.Context.World;
        var length = P.RailT1 - P.RailT0(world);
        var panels = (int)MathF.Ceiling(length / P.PostPitch);
        var u = frame.U;
        var a = frame.P(P.RailT0(world), P.RailN(world));
        var c = frame.P(P.RailT1, P.RailN(world));
        var y = MathF.Min(b.Ground(a), b.Ground(c)) + GroundLevels.Paving;
        b.Run(L.LightSteel, a, c, P.HandRail, y + P.RailHeight - P.HandRail * 0.7f, y + P.RailHeight);
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        b.Run(L.LightSteel, a, c, 0.04f, y + P.BottomRail, y + P.BottomRail + 0.04f);
        for (var k = 0; k <= panels; k++)
        {
            var at = frame.P(P.RailT0(world) + length * k / panels, P.RailN(world));
            var end = k == 0 || k == panels;
            var size = end ? P.EndPostSize(world) : P.PostSize;
            b.Box(L.LightSteel, at, u, size, size, y - 0.02f, y + P.RailHeight - 0.01f);
            // The campus-side end post (k = 0) is shared with WP-20's canal-bank railing, which carries on from it: no cap there.
            if (end && k == panels && b.Detail == SiteDetail.Full)
                b.Add(PartSolids.BeveledBox(size + 0.04f, 0.04f, size + 0.04f, 0.008f, L.LightSteel), PlanFrame.Along(at, u).At(0, y + P.RailHeight, 0));
        }
        if (b.Detail == SiteDetail.Mass) return;
        var pitch = b.Detail == SiteDetail.Full ? P.BarPitch : P.BarPitch * 3;
        for (var k = 0; k < panels; k++)
        {
            var t0 = P.RailT0(world) + length * k / panels + P.PostSize;
            var t1 = P.RailT0(world) + length * (k + 1) / panels - P.PostSize;
            var count = (int)MathF.Floor((t1 - t0) / pitch);
            var start = t0 + (t1 - t0 - (count - 1) * pitch) * 0.5f;
            for (var i = 0; i < count; i++)
                b.Box(L.LightBar, frame.P(start + i * pitch, P.RailN(world)), u, P.BarSize, P.BarSize, y + P.BottomRail + 0.04f, y + P.RailHeight - P.HandRail * 0.7f, caps: false, noShadow: true);
        }
    }

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        if (FrameOf(c.Context.World) is not { } frame) return;
        var a = frame.P(P.RailT0(world), P.RailN(world));
        var d = frame.P(P.RailT1, P.RailN(world));
        if (c.Includes(RailRange(world, frame)))
        {
            var y = MathF.Min(c.Ground(a), c.Ground(d)) + GroundLevels.Paving;
            // Inside the campus: an ordinary waterside barrier, not the boundary (WLD-OUTSKIRTS), top 1.5 m (SYS-WALK).
            c.Wall(a, d, 0.08f, y - 0.6f, y + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.Metal);
        }
        foreach (var at in P.Benches)
        {
            var bench = frame.Shift(at.X, at.Y);
            if (!c.Includes(SiteRanges.Point(bench.Origin, P.BenchSize.Width))) continue;
            c.Add(Bench.Collision(P.BenchSize), bench.At(0, c.Ground(bench.Origin) + GroundLevels.Paving, 0));
        }
    }
}
