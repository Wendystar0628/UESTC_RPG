using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.North.Framework;
using B = Campus.Gen.Zones.North.NorthTune.Bank;
using L = Campus.Gen.Zones.North.NorthTune.Look;

namespace Campus.Gen.Zones.North.Canal;

/// <summary>
/// The canal-bank railing on the 商业街 side of 红光右支渠北段 (20a-4): a light steel railing along the east water edge that
/// carries on from the north-gate plaza's railing (WP-30 30a) to the far end of the street. Same railing language as the
/// plaza's, so the two read as one run: square posts at most <see cref="B.PostPitch"/> apart, a handrail and a bottom rail, 20 mm
/// bars between them, an end post a size heavier with a cap (收头) at the far end. The plaza's end post at
/// <see cref="B.JointT"/> is shared: the first rail panel starts flush on its campus-side face and the first collider overlaps its
/// centre. Frame: t along the bank (the run heads south into the campus, toward negative t), n off the water edge toward the road (<see cref="B"/>).
/// </summary>
public sealed class CanalRailSite : NorthSite
{
    public override string Id => "20a4.canal-rail";

    /// <summary>The bank frame: origin on the straight water edge, X along the bank, +Z off the water.</summary>
    public static PlanFrame Frame { get; } = PlanFrame.Along(B.EdgeOrigin, B.EdgeDirection);

    /// <summary>Panels from the joint post to the end post, at most <see cref="B.PostPitch"/> apart.</summary>
    public static int Panels => (int)MathF.Ceiling((B.JointT - B.EndT) / B.PostPitch);

    /// <summary>Post k's centre on the rail line; k = 0 is the plaza's end post, k = <see cref="Panels"/> the end post of this run.</summary>
    public static Vector2 Post(int k) => Frame.P(B.JointT + (B.EndT - B.JointT) * k / Panels, B.RailN);

    /// <summary>Where panel k's handrail starts: post centre, except at the joint, where it starts on the shared post's campus-side face.</summary>
    private static Vector2 PanelStart(int k) => k == 0 ? Frame.P(B.JointT - B.JointPost * 0.5f, B.RailN) : Post(k);

    public override void Build(SiteBuilder b)
    {
        var panels = Panels;
        var u = Frame.U;
        var y = new float[panels];
        for (var k = 0; k < panels; k++) y[k] = MathF.Min(b.Ground(Post(k)), b.Ground(Post(k + 1))) + B.PavingTop;
        for (var k = 0; k < panels; k++)
            b.Run(L.LightSteel, PanelStart(k), Post(k + 1), B.HandRail, y[k] + B.Height - B.HandRail * 0.7f, y[k] + B.Height);
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        for (var k = 0; k < panels; k++)
            b.Run(L.LightSteel, PanelStart(k), Post(k + 1), 0.04f, y[k] + B.BottomRail, y[k] + B.BottomRail + 0.04f);
        for (var k = 1; k <= panels; k++)
        {
            var at = Post(k);
            var end = k == panels;
            var size = end ? B.PostSize + 0.02f : B.PostSize;
            var base0 = end ? y[k - 1] : MathF.Min(y[k - 1], y[k]);
            b.Box(L.LightSteel, at, u, size, size, base0 - 0.02f, base0 + B.Height - 0.01f);
            if (end && b.Detail == SiteDetail.Full)
                b.Add(PartGeometryKernel.BeveledBox(size + 0.04f, 0.04f, size + 0.04f, 0.008f, L.LightSteel), PlanFrame.Along(at, u).At(0, base0 + B.Height, 0));
        }
        if (b.Detail == SiteDetail.Mass) return;
        var pitch = b.Detail == SiteDetail.Full ? B.BarPitch : B.BarPitch * 3;
        var length = B.JointT - B.EndT;
        for (var k = 0; k < panels; k++)
        {
            // Bars stand between the posts' faces; distances run from the joint along the run.
            var s0 = length * k / panels + B.PostSize;
            var s1 = length * (k + 1) / panels - B.PostSize;
            var count = (int)MathF.Floor((s1 - s0) / pitch);
            if (count < 1) continue;
            var start = s0 + (s1 - s0 - (count - 1) * pitch) * 0.5f;
            for (var i = 0; i < count; i++)
                b.Box(L.LightBar, Frame.P(B.JointT - (start + i * pitch), B.RailN), u, B.BarSize, B.BarSize,
                    y[k] + B.BottomRail + 0.04f, y[k] + B.Height - B.HandRail * 0.7f, caps: false, noShadow: true);
        }
    }

    public override void Collide(SiteColliders c)
    {
        // An ordinary waterside barrier, not the boundary (WLD-OUTSKIRTS §7.1); one wall per panel, the first from the joint
        // post's centre so it overlaps the plaza's last collider instead of leaving a seam.
        for (var k = 0; k < Panels; k++)
        {
            Vector2 a = k == 0 ? Post(0) : Post(k), d = Post(k + 1);
            var y = MathF.Min(c.Ground(a), c.Ground(d)) + B.PavingTop;
            c.Wall(a, d, B.ColliderHalf, y - 0.6f, y + B.ColliderTop, SurfaceTag.Barrier, FootMat.Metal);
        }
    }
}
