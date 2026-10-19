using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Street;
using Campus.Gen.Zones.Common;
using P = Campus.Gen.Zones.South.SouthTune.Plaza;

namespace Campus.Gen.Zones.South.Gate;

/// <summary>
/// The plaza inside the south gate (MAP-QSH-SOUTH, design:south-a01-plaza): the first stop after the gate and the foot of
/// the A01 atlas kiosk, in the corner north of the crossroad on the north-west side of the main axis. The paving, its border band,
/// kerbs and edging are WP-15 ground drawn from the overlay polygon; this site furnishes it with a planter at the far end and two
/// benches in front of it, their backs to the shrubs, facing back across the plaza toward the gate. The kiosk is WP-13's on WP-48c's
/// anchor A01, which sits on this paving.
/// </summary>
public sealed class GatePlazaSite : ZoneSite
{
    public const string Feature = "design:south-a01-plaza";

    private static readonly BenchPart Bench = new();
    private static readonly PlanterPart Planter = new();

    public override string Id => "18c3.gate-plaza";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        FrameOf(world) is { } plaza ? [.. Items(plaza.Frame).Select(i => ItemRange(i.Params, i.At))] : [];

    private static Campus.Contracts.Maps.MapRect ItemRange(FurnitureParams p, PlanFrame at) =>
        SiteRanges.Bounds(at.Rect(-p.Width * .5f, -p.Depth * .5f, p.Width * .5f, p.Depth * .5f));

    /// <summary>
    /// The plaza's frame from its overlay rectangle, whatever the ring's start and winding: origin at the centre, Z along the long
    /// sides toward the campus (the gate's inward direction), X across toward the north-west lawn. Null when the map has no plaza.
    /// </summary>
    internal static (PlanFrame Frame, float HalfAcross, float HalfAlong)? FrameOf(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(Feature), out var plaza)) return null;
        var axis = world.ContourSegment(SouthIds.PlazaAxis);
        var gate = SouthGateSite.LayoutOf(world);
        var inward = new Vector2(gate.HouseEast.Y - gate.HouseWest.Y, gate.HouseWest.X - gate.HouseEast.X);
        var along = axis[^1] - axis[0];
        if (Vector2.Dot(along, inward) < 0) along = -along;
        var rectangle = plaza.Rectangle(new(along.Y, -along.X));
        return (rectangle.Frame, rectangle.Width * 0.5f, rectangle.Depth * 0.5f);
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
            if (b.Includes(ItemRange(p, at))) b.Place(part, p, at.At(0, b.Ground(at.Origin) + P.PavingTop, 0));
    }

    public override void Collide(SiteColliders c)
    {
        if (FrameOf(c.Context.World) is not { } plaza) return;
        foreach (var (part, p, at) in Items(plaza.Frame))
        {
            if (!c.Includes(ItemRange(p, at))) continue;
            var local = part == PartCatalog.Bench ? Bench.Collision(p) : Planter.Collision(p);
            c.Add(local, at.At(0, c.Ground(at.Origin.X, at.Origin.Y) + P.PavingTop, 0));
        }
    }
}
