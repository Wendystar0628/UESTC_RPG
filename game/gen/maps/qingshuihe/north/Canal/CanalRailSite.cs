using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using B = Campus.Gen.Maps.Qingshuihe.North.NorthTune.Bank;
using L = Campus.Gen.Maps.Qingshuihe.North.NorthTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.North.Canal;

/// <summary>
/// The canal-bank railing on the 商业街 side of 红光右支渠北段 (20a-4): a light steel railing along the east water edge that
/// carries on from the north-gate plaza's railing (WP-30 30a) to the far end of the street. Same railing language as the
/// plaza's, so the two read as one run: square posts at most <see cref="B.PostPitch"/> apart, a handrail and a bottom rail, 20 mm
/// bars between them, an end post a size heavier with a cap (收头) at the far end. The plaza's end post at
/// <see cref="B.JointT"/> is shared: the first rail panel starts flush on its campus-side face and the first collider overlaps its
/// centre. Frame: t along the bank (the run heads south into the campus, toward negative t), n off the water edge toward the road (<see cref="B"/>).
/// </summary>
public sealed class CanalRailSite : ZoneSite
{
    public override string Id => "20a4.canal-rail";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        FrameOf(world) is { } frame ? [SiteRanges.Bounds([Post(world, frame, 0), Post(world, frame, Panels(world))], B.ColliderHalf)] : [];

    /// <summary>
    /// The registered bank frame: origin at the selected edge's north end, X toward that end, +Z off the water.
    /// Null when the bank selection is not in the map.
    /// </summary>
    public static PlanFrame? FrameOf(IWorldModel world)
    {
        if (!world.Features.ContainsKey(new(Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryFeatureIds.NorthBankContour))) return null;
        return world.ContourFrame(Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryFeatureIds.NorthBankContour, originAtEnd: false, forward: false);
    }

    /// <summary>Panels from the joint post to the end post, at most <see cref="B.PostPitch"/> apart.</summary>
    public static int Panels(IWorldModel world) => (int)MathF.Ceiling((B.JointT(world) - B.EndT) / B.PostPitch);

    /// <summary>Post k's centre on the rail line; k = 0 is the plaza's end post, k = <see cref="Panels"/> the end post of this run.</summary>
    public static Vector2 Post(IWorldModel world, PlanFrame frame, int k) => frame.P(B.JointT(world) + (B.EndT - B.JointT(world)) * k / Panels(world), B.RailN(world));

    /// <summary>Where panel k's handrail starts: post centre, except at the joint, where it starts on the shared post's campus-side face.</summary>
    private static Vector2 PanelStart(IWorldModel world, PlanFrame frame, int k) => k == 0 ? frame.P(B.JointT(world) - B.JointPost(world) * 0.5f, B.RailN(world)) : Post(world, frame, k);

    private static Campus.Contracts.Maps.MapRect PanelRange(IWorldModel world, PlanFrame frame, int k, float padding = B.ColliderHalf) =>
        SiteRanges.Bounds([Post(world, frame, k), Post(world, frame, k + 1)], padding);

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        if (FrameOf(b.Context.World) is not { } frame) return;
        var panels = Panels(world);
        var u = frame.U;
        var y = new float[panels];
        for (var k = 0; k < panels; k++)
            if (b.Includes(PanelRange(world, frame, k, B.PostPitch + B.ColliderHalf)))
                y[k] = MathF.Min(b.Ground(Post(world, frame, k)), b.Ground(Post(world, frame, k + 1))) + GroundLevels.Paving;
        for (var k = 0; k < panels; k++)
            if (b.Includes(PanelRange(world, frame, k))) b.Run(L.LightSteel, PanelStart(world, frame, k), Post(world, frame, k + 1), B.HandRail, y[k] + B.Height - B.HandRail * 0.7f, y[k] + B.Height);
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        for (var k = 0; k < panels; k++)
            if (b.Includes(PanelRange(world, frame, k))) b.Run(L.LightSteel, PanelStart(world, frame, k), Post(world, frame, k + 1), 0.04f, y[k] + B.BottomRail, y[k] + B.BottomRail + 0.04f);
        for (var k = 1; k <= panels; k++)
        {
            var at = Post(world, frame, k);
            if (!b.Includes(SiteRanges.Point(at, B.ColliderHalf))) continue;
            var end = k == panels;
            var size = end ? B.PostSize + 0.02f : B.PostSize;
            var base0 = end ? y[k - 1] : MathF.Min(y[k - 1], y[k]);
            b.Box(L.LightSteel, at, u, size, size, base0 - 0.02f, base0 + B.Height - 0.01f);
            if (end && b.Detail == SiteDetail.Full)
                b.Add(PartSolids.BeveledBox(size + 0.04f, 0.04f, size + 0.04f, 0.008f, L.LightSteel), PlanFrame.Along(at, u).At(0, base0 + B.Height, 0));
        }
        if (b.Detail == SiteDetail.Mass) return;
        var pitch = b.Detail == SiteDetail.Full ? B.BarPitch : B.BarPitch * 3;
        var length = B.JointT(world) - B.EndT;
        for (var k = 0; k < panels; k++)
        {
            if (!b.Includes(PanelRange(world, frame, k))) continue;
            // Bars stand between the posts' faces; distances run from the joint along the run.
            var s0 = length * k / panels + B.PostSize;
            var s1 = length * (k + 1) / panels - B.PostSize;
            var count = (int)MathF.Floor((s1 - s0) / pitch);
            if (count < 1) continue;
            var start = s0 + (s1 - s0 - (count - 1) * pitch) * 0.5f;
            for (var i = 0; i < count; i++)
                b.Box(L.LightBar, frame.P(B.JointT(world) - (start + i * pitch), B.RailN(world)), u, B.BarSize, B.BarSize,
                    y[k] + B.BottomRail + 0.04f, y[k] + B.Height - B.HandRail * 0.7f, caps: false, noShadow: true);
        }
    }

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        // An ordinary waterside barrier, not the boundary (WLD-OUTSKIRTS); one wall per panel, the first from the joint
        // post's centre so it overlaps the plaza's last collider instead of leaving a seam.
        if (FrameOf(c.Context.World) is not { } frame) return;
        for (var k = 0; k < Panels(world); k++)
        {
            if (!c.Includes(PanelRange(world, frame, k))) continue;
            Vector2 a = Post(world, frame, k), d = Post(world, frame, k + 1);
            var y = MathF.Min(c.Ground(a), c.Ground(d)) + GroundLevels.Paving;
            c.Wall(a, d, B.ColliderHalf, y - 0.6f, y + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.Metal);
        }
    }
}
