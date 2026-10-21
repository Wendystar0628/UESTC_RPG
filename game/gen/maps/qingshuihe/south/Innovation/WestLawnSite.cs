using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.South.Framework;
using N = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Lawn;
using L = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.South.Innovation;

/// <summary>
/// The west lawn by the innovation centre (MAP-QSH-ADDITIONS R6; design:south-west-lawn-seat): the low grass mound is a
/// terrain operation in south.terrain.json, this site adds the stop on its crest, a granite seat-wall arc facing north-east.
/// Each chord follows the ground, so the seat rides the mound once the terrain evaluator lands and stands level on the flat stub.
/// </summary>
public sealed class WestLawnSite : ZoneSite
{
    public const string Seat = "design:south-west-lawn-seat";

    public override string Id => "18c.west-lawn";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var wall = WallOf(world);
        return [SiteRanges.Bounds(SeatArc(wall).SelectMany(s => new[] { s.A, s.B }), wall.Thickness * .5f + .05f)];
    }

    /// <summary>The seat wall from its overlay feature (DECISION-239): the crest, the arc's radius and bearing range, its height and thickness.</summary>
    internal readonly record struct SeatWall(Vector2 Crest, float Radius, float From, float To, float Height, float Thickness);

    internal static SeatWall WallOf(IWorldModel world)
    {
        var f = world.Feature(Seat);
        var bearings = f.Numbers("bearings");
        return new(f.Point(), f.Number("radius"), bearings[0], bearings[1],
            f.Height ?? throw new InvalidOperationException($"{Seat} has no height."), f.Number("thickness"));
    }

    /// <summary>The seat-wall arc's chords, bearings From…To around the crest.</summary>
    internal static IEnumerable<(Vector2 A, Vector2 B)> SeatArc(SeatWall wall)
    {
        Vector2 At(int k) => wall.Crest + Angles.Heading(wall.From + (wall.To - wall.From) * k / N.SeatChords) * wall.Radius;
        for (var k = 0; k < N.SeatChords; k++) yield return (At(k), At(k + 1));
    }

    public override void Build(SiteBuilder b)
    {
        var wall = WallOf(b.Context.World);
        foreach (var (a, e) in SeatArc(wall))
        {
            if (!b.Includes(SiteRanges.Bounds([a, e], wall.Thickness * .5f + .05f))) continue;
            var seg = PlanFrame.Along((a + e) * 0.5f, e - a);
            var ground = b.Ground(seg.Origin);
            var length = Vector2.Distance(a, e) + 0.03f;
            b.Add(PartSolids.BeveledBox(length, wall.Height + 0.02f, wall.Thickness, 0.025f, L.Granite), seg.At(0, ground - 0.12f, 0));
            if (b.Detail == SiteDetail.Mass) continue;
            // A slightly proud stone seat slab: the horizontal face that takes the season's leaves, petals and snow.
            b.Add(PartSolids.BeveledBox(length, 0.1f, wall.Thickness + 0.08f, 0.02f, L.StoneTrim), seg.At(0, ground + wall.Height - 0.1f, 0));
        }
    }

    public override void Collide(SiteColliders c)
    {
        var wall = WallOf(c.Context.World);
        foreach (var (a, e) in SeatArc(wall))
        {
            if (!c.Includes(SiteRanges.Bounds([a, e], wall.Thickness * .5f + .05f))) continue;
            var seg = PlanFrame.Along((a + e) * 0.5f, e - a);
            var ground = c.Ground(seg.Origin.X, seg.Origin.Y);
            c.Box(seg.At(0, ground + wall.Height * 0.5f, 0), new(Vector2.Distance(a, e) * 0.5f, wall.Height * 0.5f, wall.Thickness * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
        }
    }
}
