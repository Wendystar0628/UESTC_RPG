using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using N = Campus.Gen.Zones.South.SouthTune.Lawn;
using L = Campus.Gen.Zones.South.SouthTune.Look;

namespace Campus.Gen.Zones.South.Innovation;

/// <summary>
/// The west lawn by the innovation centre (MAP-QSH-ADDITIONS R6; design:south-west-lawn-seat): the low grass mound is a
/// terrain operation in south.terrain.json, this site adds the stop on its crest, a granite seat-wall arc facing north-east.
/// Each chord follows the ground, so the seat rides the mound once the terrain evaluator lands and stands level on the flat stub.
/// </summary>
public sealed class WestLawnSite : ZoneSite
{
    public override string Id => "18c.west-lawn";

    /// <summary>The seat-wall arc's chords, bearings SeatFrom…SeatTo around the crest.</summary>
    internal static IEnumerable<(Vector2 A, Vector2 B)> SeatArc()
    {
        Vector2 At(int k) => N.Crest + PlanMath.Heading(N.SeatFrom + (N.SeatTo - N.SeatFrom) * k / N.SeatChords) * N.SeatRadius;
        for (var k = 0; k < N.SeatChords; k++) yield return (At(k), At(k + 1));
    }

    public override void Build(SiteBuilder b)
    {
        foreach (var (a, e) in SeatArc())
        {
            var seg = PlanFrame.Along((a + e) * 0.5f, e - a);
            var ground = b.Ground(seg.Origin);
            var length = Vector2.Distance(a, e) + 0.03f;
            b.Add(PartGeometryKernel.BeveledBox(length, N.SeatHeight + 0.02f, N.SeatThickness, 0.025f, L.Granite), seg.At(0, ground - 0.12f, 0));
            if (b.Detail == SiteDetail.Mass) continue;
            // A slightly proud stone seat slab: the horizontal face that takes the season's leaves, petals and snow.
            b.Add(PartGeometryKernel.BeveledBox(length, 0.1f, N.SeatThickness + 0.08f, 0.02f, L.StoneTrim), seg.At(0, ground + N.SeatHeight - 0.1f, 0));
        }
    }

    public override void Collide(SiteColliders c)
    {
        foreach (var (a, e) in SeatArc())
        {
            var seg = PlanFrame.Along((a + e) * 0.5f, e - a);
            var ground = c.Ground(seg.Origin.X, seg.Origin.Y);
            c.Box(seg.At(0, ground + N.SeatHeight * 0.5f, 0), new(Vector2.Distance(a, e) * 0.5f, N.SeatHeight * 0.5f, N.SeatThickness * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
        }
    }
}
