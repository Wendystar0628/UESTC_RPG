using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using A = Campus.Gen.Zones.South.SouthTune.Axis;
using L = Campus.Gen.Zones.South.SouthTune.Look;

namespace Campus.Gen.Zones.South.MainBuilding;

/// <summary>
/// The entrance axis from the south gate to the main building (§2.5): the lawns, paving and parterres are WP-15 ground and
/// WP-23 planting; this site builds the national flagpole at the centre of the front lawn on a three-tier granite base.
/// The flag itself waits for the wind field (WP-26) and its cloth material.
/// </summary>
public sealed class EntranceAxisSite : ZoneSite
{
    private static PlanFrame FrameOf(IWorldModel world) => new(world.Feature(SouthIds.Flagpole).Point(), A.FlagpoleFacing);

    public override string Id => "18a.entrance-axis";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(FrameOf(world).Rect(-2.1f, -2.1f, 2.1f, 2.1f))];

    public override void Build(SiteBuilder b)
    {
        var frame = FrameOf(b.Context.World);
        var y = b.Ground(frame.Origin);
        float[] tiers = [4.2f, 3.0f, 1.7f];
        float[] rises = [0.3f, 0.3f, 0.55f];
        var top = y - 0.1f;
        for (var i = 0; i < tiers.Length; i++)
        {
            var h = rises[i] + (i == 0 ? 0.1f : 0f);
            b.Add(PartGeometryKernel.BeveledBox(tiers[i], h, tiers[i], 0.025f, i == 2 ? L.StoneTrim : L.Granite), frame.At(0, top, 0));
            top += h;
        }
        var segments = b.Detail == SiteDetail.Full ? 20 : 8;
        b.Add(Shapes.Shaft(0.24f, 0.22f, 0.35f, segments, L.Metal), frame.At(0, top, 0));
        b.Add(Shapes.Shaft(A.PoleFootRadius, A.PoleTopRadius, A.PoleHeight, segments, L.Metal), frame.At(0, top, 0));
        if (b.Detail == SiteDetail.Mass) return;
        var head = top + A.PoleHeight;
        b.Add(Shapes.Shaft(0.05f, 0.13f, 0.14f, segments, L.Metal), frame.At(0, head, 0));
        b.Add(Shapes.Shaft(0.13f, 0.02f, 0.22f, segments, L.Metal), frame.At(0, head + 0.14f, 0));
    }

    public override void Collide(SiteColliders c)
    {
        var frame = FrameOf(c.Context.World);
        var y = c.Ground(frame.Origin.X, frame.Origin.Y);
        c.Box(frame.At(0, y + 0.1f, 0), new(2.1f, 0.2f, 2.1f), SurfaceTag.Prop, FootMat.StonePaving);
        c.Box(frame.At(0, y + 0.4f, 0), new(1.5f, 0.2f, 1.5f), SurfaceTag.Prop, FootMat.StonePaving);
        c.Box(frame.At(0, y + 0.85f, 0), new(0.85f, 0.275f, 0.85f), SurfaceTag.Prop, FootMat.StonePaving);
        c.Add(new PostCollider(frame.Origin, 0.2f, y + 1.15f, y + 1.15f + A.PoleHeight, SurfaceTag.Prop) { Foot = FootMat.Metal });
    }
}
