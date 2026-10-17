using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Street;
using Campus.Gen.Zones.South.Framework;
using P = Campus.Gen.Zones.South.SouthTune.Plaza;
using T = Campus.Gen.Zones.South.SouthTune.Gate;

namespace Campus.Gen.Zones.South.Gate;

/// <summary>
/// The plaza inside the south gate (MAP-QSH-SOUTH §12.6, design:south-a01-plaza): the first stop after the gate and the foot of
/// the A01 atlas kiosk, in the corner north of the crossroad on the north-west side of the main axis. The paving, its border band,
/// kerbs and edging are WP-15 ground drawn from the overlay polygon; this site furnishes it with a planter at the far end and two
/// benches in front of it, their backs to the shrubs, facing back across the plaza toward the gate. The kiosk is WP-13's on WP-48c's
/// anchor; <see cref="SouthTune.Plaza.KioskSpot"/> is the spot the plaza keeps free for it.
/// </summary>
public sealed class GatePlazaSite : SouthSite
{
    public const string Feature = "design:south-a01-plaza";

    private static readonly BenchPart Bench = new();
    private static readonly PlanterPart Planter = new();

    public override string Id => "18c3.gate-plaza";

    /// <summary>
    /// The plaza's frame from its overlay rectangle, whatever the ring's start and winding: origin at the centre, Z along the long
    /// sides toward the campus (the gate's inward direction), X across toward the north-west lawn. Null when the map has no plaza.
    /// </summary>
    internal static (PlanFrame Frame, float HalfAcross, float HalfAlong)? FrameOf(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(Feature), out var plaza)) return null;
        if (plaza.Geometry is not PolygonGeometry { Rings.Outer: { Count: 4 } ring })
            throw new InvalidOperationException($"{Feature} is expected to be a four-cornered polygon.");
        var centre = (ring[0] + ring[1] + ring[2] + ring[3]) * 0.25f;
        var (a, b) = Vector2.Distance(ring[0], ring[1]) >= Vector2.Distance(ring[1], ring[2]) ? (ring[1] - ring[0], ring[2] - ring[1]) : (ring[2] - ring[1], ring[1] - ring[0]);
        var inward = new Vector2(T.HouseEast.Y - T.HouseWest.Y, T.HouseWest.X - T.HouseEast.X);
        var along = Vector2.Dot(a, inward) >= 0 ? a : -a;
        // X is Z turned a quarter clockwise in plan, so local +Z (PlanFrame.W) runs along the axis.
        return (PlanFrame.Along(centre, new(along.Y, -along.X)), b.Length() * 0.5f, a.Length() * 0.5f);
    }

    /// <summary>Planter and benches as (part, parameters, plan frame at the item's foot); a bench's sitter faces back along −Z.</summary>
    private static IEnumerable<(string Part, FurnitureParams Params, PlanFrame At)> Items(PlanFrame f)
    {
        yield return (PartCatalog.Planter, P.PlanterSize, f.Shift(0, P.PlanterAlong));
        foreach (var x in P.BenchAcross) yield return (PartCatalog.Bench, P.BenchSize, f.Shift(x, P.BenchAlong));
    }

    public override void Build(SiteBuilder b)
    {
        if (b.Detail == SiteDetail.Mass || FrameOf(b.Context.World) is not { } plaza) return;
        foreach (var (part, p, at) in Items(plaza.Frame))
            b.Place(part, p, at.At(0, b.Ground(at.Origin) + P.PavingTop, 0));
    }

    public override void Collide(SiteColliders c)
    {
        if (FrameOf(c.Context.World) is not { } plaza) return;
        foreach (var (part, p, at) in Items(plaza.Frame))
        {
            var local = part == PartCatalog.Bench ? Bench.Collision(p) : Planter.Collision(p);
            c.Add(local, at.At(0, c.Ground(at.Origin.X, at.Origin.Y) + P.PavingTop, 0));
        }
    }
}
