using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.West.WestTune.Look;
using S = Campus.Gen.Zones.West.WestTune.Six;
using W = Campus.Gen.Zones.West.WestTune.Windows;

namespace Campus.Gen.Zones.West.Six;

/// <summary>六号科研楼的版式：窗格样式、各楼平面与体量、立面边的处理、东门定位与窗格声明。</summary>
public sealed partial class SixSite
{
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

    // ---- Plan ----

    internal static PolygonRings Plan(IWorldModel world, string entity, string part)
    {
        return world.Feature(entity).ContourGeometry(part) is PolygonGeometry polygon
            ? polygon.Rings : throw new InvalidOperationException($"{part} is not a polygon.");
    }

    /// <summary>Plans of every closed volume of the building (blocks, A–B link, tower), for hiding faces that touch another.</summary>
    internal static IReadOnlyList<PolygonRings> Volumes(IWorldModel world) =>
        [.. Blocks.Select(k => Plan(world, k.Entity, k.Part)), Plan(world, S.EntityC, S.LinkAB), Plan(world, S.EntityC, S.Tower)];

    /// <summary>Inside a plan's outer ring and outside its courtyard holes.</summary>
    internal static bool In(PolygonRings plan, Vector2 p) => PlanGeometry.InsideByCrossings(plan.Outer, p) && !plan.Holes.Any(h => PlanGeometry.InsideByCrossings(h, p));

    internal static float BaseY(BuildContext ctx, string entity) => ctx.World.Terrain.PadY(new(entity));

    /// <summary>
    /// Hidden where a face lies against another volume; Blank when too short for a bay; otherwise plain windows. A base edge
    /// (set back by <paramref name="setBack"/>) looks past the stone line.
    /// </summary>
    internal static GridEdge Dress(GridEdgeInfo e, IReadOnlyList<PolygonRings> volumes, float setBack = 0f)
    {
        if (new[] { 0.1f, 0.5f, 0.9f }.All(t => volumes.Any(r => In(r, Vector2.Lerp(e.A, e.B, t) + e.Frame.W * (setBack + 0.6f)))))
            return new(EdgeKind.Hidden);
        return new(e.Length < 4.5f ? EdgeKind.Blank : EdgeKind.Plain);
    }

    /// <summary>The C east door's centre, from C's entrance record in the world model (interiors.json).</summary>
    internal static Vector2 EastDoor(IWorldModel world)
    {
        var entrance = world.Interiors.Get(new(S.EntityC))?.Entrances.FirstOrDefault(e => e.Id == S.EastDoorId)
            ?? throw new InvalidOperationException($"Entrance {S.EastDoorId} is missing from the world model.");
        return new(entrance.Center.X, entrance.Center.Z);
    }

    /// <summary>The outer face carrying the east door, and the door's local x on it.</summary>
    internal static (GridEdgeInfo Face, float X) EastFace(IWorldModel world, Vector2 door)
    {
        var face = SelectedFace(world, WestIds.SixEastFace);
        return (face, face.Frame.Local(door).X);
    }

    private static GridEdgeInfo SelectedFace(IWorldModel world, string selection)
    {
        var line = world.ContourSegment(selection);
        var frame = PlanFrame.Toward(line[^1], line[0]);
        return new(0, line[^1], line[0], frame, Vector2.Distance(line[0], line[^1]));
    }

    // ---- Window grids: one layout for geometry, collision and declarations ----

    private static GridNumbering Numbering(Block k) => new(new(k.Entity), k.Key, 0, W.Research, W.Lab) { AltCard = W.Office, AltShare = W.OfficeShare };

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        var volumes = Volumes(world);
        foreach (var k in Blocks)
        {
            var layout = GridFacade.Layout(Plan(world, k.Entity, k.Part), Style(k.Upper(world)), e => Dress(e, volumes));
            if (layout.Columns > 0) yield return (new(k.Entity), new(k.Key, layout.Columns, layout.Rows));
        }
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var volumes = Volumes(ctx.World);
        foreach (var k in Blocks)
        {
            var plan = Plan(ctx.World, k.Entity, k.Part);
            var style = Style(k.Upper(ctx.World));
            if (GridFacade.Layout(plan, style, e => Dress(e, volumes)).Columns == 0) continue;
            GridFacade.Declare(facades, plan, BaseY(ctx, k.Entity) + S.BaseHeight, style, e => Dress(e, volumes), Numbering(k),
                WindowNumbers.Required(ctx, new(k.Entity), k.Key));
        }
    }
}
