using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的来源平面、楼层样式、立面定位与柱位；外观、碰撞和室内共用。</summary>
internal static class SixLayout
{
    /// <summary>One stone block: its entity, its source part, its window grid key and, for C only, its own floors above the base.</summary>
    internal sealed record Block(string Entity, string Part, string Key, int? Fixed = null)
    {
        /// <summary>Floors above the glass base: the entity's storeys in the world model less the base (DECISION-239), or C's own count.</summary>
        public int Upper(IWorldModel world)
        {
            if (Fixed is { } floors) return floors;
            return world.Features.TryGetValue(new(Entity), out var feature) && feature.Storeys is { } storeys
                ? storeys - S.BaseStoreys
                : throw new InvalidOperationException($"六号科研楼 entity {Entity} has no storeys in the world model.");
        }
    }

    internal static readonly IReadOnlyList<Block> Blocks =
        [new(S.EntityA, S.PartA, "a"), new(S.EntityB, S.PartB, "b"), new(S.EntityC, S.PartC, "c", S.UpperC)];

    /// <summary>The blocks' plan frame, X along <see cref="S.Axis"/>.</summary>
    internal static readonly PlanFrame Frame = PlanFrame.Along(Vector2.Zero, S.Axis);

    internal static GridStyle Style(int upper) => new(L.Stone, L.Trim, L.Stone, L.Glass, L.Metal, L.RoofSlab)
    {
        Bay = S.Bay,
        Pier = S.Pier,
        PierDepth = S.PierDepth,
        BandDepth = S.PierDepth - 0.15f,
        GroundStorey = S.Storey,
        Storey = S.Storey,
        Storeys = upper,
        GroundSill = S.Sill,
        GroundWindow = S.Window,
        Sill = S.Sill,
        Window = S.Window,
        PlinthHeight = 0.6f,
        Cornice = 0.8f,
        CorniceProjection = 0.45f,
        Parapet = 0.9f,
        Band = L.DarkGrey,
        BandHeight = 1.0f,
        BandLift = 0.25f,
        BandProud = 0.2f,
        Louvres = 4,
    };

    internal static PolygonRings Plan(IWorldModel world, string entity, string part)
    {
        return world.Feature(entity).ContourGeometry(part) is PolygonGeometry polygon
            ? polygon.Rings : throw new InvalidOperationException($"{part} is not a polygon.");
    }

    internal static IReadOnlyList<PolygonRings> Volumes(IWorldModel world) =>
        [.. Blocks.Select(k => Plan(world, k.Entity, k.Part)), Plan(world, S.EntityC, S.LinkAB), Plan(world, S.EntityC, S.Tower)];

    internal static bool In(PolygonRings plan, Vector2 p) => PlanRings.InsideByCrossings(plan.Outer, p) && !plan.Holes.Any(h => PlanRings.InsideByCrossings(h, p));

    internal static float BaseY(BuildContext ctx, string entity) => ctx.World.Terrain.PadY(new(entity));

    internal static GridEdge Dress(GridEdgeInfo e, IReadOnlyList<PolygonRings> volumes, float setBack = 0f)
    {
        if (new[] { 0.1f, 0.5f, 0.9f }.All(t => volumes.Any(r => In(r, Vector2.Lerp(e.A, e.B, t) + e.Frame.W * (setBack + 0.6f)))))
            return new(EdgeKind.Hidden);
        return new(e.Length < 4.5f ? EdgeKind.Blank : EdgeKind.Plain);
    }

    internal static Vector2 EastDoor(IWorldModel world)
    {
        var entrance = world.Interiors.Get(new(S.EntityC))?.Entrances.FirstOrDefault(e => e.Id == S.EastDoorId)
            ?? throw new InvalidOperationException($"Entrance {S.EastDoorId} is missing from the world model.");
        return new(entrance.Center.X, entrance.Center.Z);
    }

    internal static (GridEdgeInfo Face, float X) EastFace(IWorldModel world, Vector2 door)
    {
        var face = SelectedFace(world, WestIds.SixEastFace);
        return (face, face.Frame.Local(door).X);
    }

    internal static GridEdgeInfo SelectedFace(IWorldModel world, string selection)
    {
        var line = world.ContourSegment(selection);
        var frame = PlanFrame.Toward(line[^1], line[0]);
        return new(0, line[^1], line[0], frame, Vector2.Distance(line[0], line[^1]));
    }

    internal static Vector2[] Inset(IReadOnlyList<Vector2> ring, float by, bool hole) =>
        PlanOffsets.MitreOffset(ring, PlanRings.SignedArea(ring) > 0 != hole ? -by : by, closed: true);

    internal static PolygonRings BasePlan(PolygonRings plan) =>
        new(Inset(plan.Outer, S.Recess, false), [.. plan.Holes.Select(h => (IReadOnlyList<Vector2>)Inset(h, S.Recess, true))]);

    internal static IEnumerable<(float X, PlanFrame Frame)> Columns(PolygonRings plan, GridStyle style, IReadOnlyList<PolygonRings> volumes, Vector2 eastDoor, IWorldModel world)
    {
        var doors = S.ShutDoors(world).Append(eastDoor).ToArray();
        foreach (var run in GridFacade.Layout(plan, style, e => Dress(e, volumes)).Runs)
        {
            if (run.Edge.Kind == EdgeKind.Hidden) continue;
            var f = run.Info.Frame;
            var xs = run.Bays == 0
                ? new[] { S.ColumnSize * 0.5f, run.Info.Length - S.ColumnSize * 0.5f }
                : Enumerable.Range(0, run.Bays + 1).Where(i => i % S.ColumnEvery == 0 || i == run.Bays).Select(i => i * run.Pitch + S.Pier * 0.5f).ToArray();
            foreach (var x in xs)
                if (!doors.Any(d => Vector2.Distance(f.P(x, 0), d) < S.VestibuleWidth * 0.5f + 0.8f)) yield return (x, f);
        }
    }

    internal static string ShutEntity(IWorldModel world, Vector2 at)
    {
        var index = Array.IndexOf(S.ShutDoors(world), at);
        return world.Feature(WestIds.SixShutFaces[index]).Text("parent");
    }

    internal static (GridEdgeInfo Face, float X, float Y) ShutFace(BuildContext ctx, Vector2 at)
    {
        var index = Array.IndexOf(S.ShutDoors(ctx.World), at);
        var face = SelectedFace(ctx.World, WestIds.SixShutFaces[index]);
        return (face, face.Frame.Local(at).X, BaseY(ctx, ShutEntity(ctx.World, at)));
    }
}
