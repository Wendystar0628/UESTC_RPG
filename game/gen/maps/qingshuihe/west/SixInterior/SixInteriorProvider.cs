using Campus.Gen.Interior.Shell;
using Campus.Gen.Interior;
using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Roof.Mep;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.West.Six;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.West.SixInterior;

/// <summary>The C east hall, its formula stair and the authored northern route to B's short public bay.</summary>
public abstract class SixInteriorProvider : IInteriorProvider, IInteriorCatalogConfigurable
{
    public BakeInputDependencies BakeInputs { get; } = new(BakeInputKinds.World, [], [SceneTextInputs.AtlasKey]);
    public BakeInputDependencies CollisionInputs => BakeInputDependencies.World;

    private FeatureId[] _features = [];
    private IReadOnlyList<InteriorOpenPart> _openParts = [];
    private IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> _entrances =
        new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>();
    private static readonly DormitoryPlanOptions Rules = new();
    internal static readonly ToonSpec FloorMaterial = CirculationStyle.StairBody;
    internal static readonly ToonSpec WallMaterial = PartMaterials.Trim("wall.greyWhite");
    // Existing campus light-wood family (also used by the library's light timber).
    internal static readonly ToonSpec WoodMaterial = PartMaterials.Trim("panel.brown");
    public IReadOnlyList<FeatureId> FeatureIds => _features;
    public int Priority => 100;
    public bool Walkable => true;

    public void Configure(InteriorCatalogConfiguration configuration)
    {
        var (features, openParts, entrances, buildingType, seals) = configuration;
        if (buildingType is not null || seals.Count != 0)
            throw new ArgumentException("This authored provider does not consume generated building configuration or catalog seals.", nameof(configuration));
        _features = features.Distinct().OrderBy(f => f.Value, StringComparer.Ordinal).ToArray();
        _openParts = openParts;
        _entrances = entrances;
    }

    public InteriorLayout Layout(BuildContext ctx, FeatureId id)
    {
        Require(id);
        var p = SixInteriorPlan.Of(ctx);
        var isC = id.Value == S.EntityC;
        var floors = InteriorOpenPart.IndoorFloorsFor(_openParts, id).Where(f => (f is 1 or 2) && (isC || f == 2)).ToArray();
        var footprint = isC ? p.CBase : p.PublicB;
        var storeys = floors.Select(f => new StoreyInfo(f, f == 1 ? p.FloorY : p.UpperY,
            f == 1 ? p.StairRise : S.BaseStorey, footprint.Outer)).ToArray();
        var zones = floors.Select(f => new ZoneVolume(f, f * 100, f == 1 ? ZoneKind.Lobby : ZoneKind.Corridor,
            (isC ? f == 1 ? p.Hall : p.UpperC : p.PublicB).Outer,
            new("interior.main"), StableHash.Hash32(ctx.WorldSeed, id.Value, "six.hall", f.ToString(CultureInfo.InvariantCulture)), true)).ToList();
        var portals = new List<PortalDecl>();
        var entrances = _entrances.GetValueOrDefault(id) ?? [];
        foreach (var entrance in entrances.Where(e => e.Open))
        {
            var frame = SixSite.EastFace(ctx.World, SixSite.EastDoor(ctx.World)).Face.Frame;
            var c = new Vector2(entrance.Center.X, entrance.Center.Z);
            portals.Add(new(1, -1, 100, V(c - frame.U * entrance.ClearWidth * 0.5f, p.FloorY),
                V(c + frame.U * entrance.ClearWidth * 0.5f, p.FloorY), S.DoorHeight, PortalKind.Door));
        }
        if (isC)
            portals.Add(new(2, 100, 200, V(p.FormulaFrame.P(-p.FormulaWidth * 0.5f, p.FormulaRun * 0.5f), p.UpperY),
                V(p.FormulaFrame.P(p.FormulaWidth * 0.5f, p.FormulaRun * 0.5f), p.UpperY), S.DoorHeight, PortalKind.StairOpening));
        var doors = new List<DoorLeafDecl>();
        if (isC)
            foreach (var floor in floors)
            {
                var returning = ReturnDoor(p, floor);
                var hinge = returning.Hinge;
                var y = floor == 1 ? p.FloorY : p.UpperY;
                zones.Add(new(floor, floor * 100 + 10, ZoneKind.Stair, p.ReturnOpening.Outer, new("interior.main"),
                    StableHash.Hash32(ctx.WorldSeed, id.Value, "six.return", floor.ToString(CultureInfo.InvariantCulture)), true));
                portals.Add(new(floor, floor * 100, floor * 100 + 10, V(hinge, y),
                    V(hinge + p.ReturnFrame.U * Rules.DoorClearWidth, y), Rules.DoorHeight, PortalKind.Door));
                var yaw = MathF.Atan2(-p.ReturnFrame.U.Y, p.ReturnFrame.U.X) * (180f / float.Pi);
                doors.Add(new($"six.return/{floor}", floor, floor * 100, floor * 100 + 10,
                    V(hinge, floor == 1 ? p.FloorY : p.UpperY), yaw, 90, Rules.DoorClearWidth, Rules.DoorHeight, DoorLeafKind.FireDoor));
            }
        var extent = isC ? footprint.Outer.Concat(SixSite.Plan(ctx.World, S.EntityC, S.LinkBC).Outer)
            .Concat(SixSite.Bridges(ctx.World).SelectMany(b => new[] { b.From, b.To })).ToArray() : footprint.Outer.ToArray();
        return new(id, storeys, zones, portals, entrances, [], floors, isC ? floors : [],
            new(new(extent.Min(v => v.X) - (isC ? SixSite.BridgeHalfWidth : 0), p.GroundY, extent.Min(v => v.Y) - (isC ? SixSite.BridgeHalfWidth : 0)),
                new(extent.Max(v => v.X) + (isC ? SixSite.BridgeHalfWidth : 0), p.GroundY + S.BaseHeight, extent.Max(v => v.Y) + (isC ? SixSite.BridgeHalfWidth : 0))),
            StableHash.Hash32(ctx.WorldSeed, id.Value, "six.interior.v1")) { Doors = doors };
    }

    public ContentOutput BuildStorey(BuildContext ctx, FeatureId id, int storey, InteriorDetail detail)
    {
        Require(id);
        var full = detail switch
        {
            InteriorDetail.Full => true,
            InteriorDetail.Proxy or InteriorDetail.Shell => false,
            _ => throw new ArgumentOutOfRangeException(nameof(detail))
        };
        if (!Open(id, storey)) return ContentOutput.Empty;
        var sink = new InteriorGeometry(ctx.Catalog);
        Compose(ctx, id, storey, sink, full);
        return sink.Finish();
    }

    public IReadOnlyList<Collider> CollisionStorey(BuildContext ctx, FeatureId id, int storey)
    {
        Require(id);
        if (!Open(id, storey)) return [];
        var sink = new InteriorGeometry(ctx.Catalog) { Draw = false };
        Compose(ctx, id, storey, sink, false);
        return sink.Colliders;
    }

    public NavFloorGraph Nav(BuildContext ctx, FeatureId id, int storey)
    {
        Require(id);
        if (!Open(id, storey)) return NavFloorGraph.Empty(id, storey);
        var p = SixInteriorPlan.Of(ctx);
        var nodes = new List<NavNode>(); var edges = new List<NavEdge>();
        NavNode Node(string entity, int floor, string role, Vector2 point, float y)
        {
            var node = new NavNode(new(StableHash.Hash32(ctx.WorldSeed, entity, floor.ToString(CultureInfo.InvariantCulture), "six/" + role)),
                V(point, y), new(new FeatureId(entity), (short)floor), role);
            nodes.Add(node); return node;
        }
        void Link(NavNode a, NavNode b, NavEdgeKind kind, float width) => edges.Add(new(a.Id, b.Id, kind,
            Vector3.Distance(a.P, b.P), width, b.P.Y - a.P.Y, NavFlags.Indoor | NavFlags.Covered, "six.interior"));
        if (id.Value == S.EntityB)
        {
            var bridge = SixSite.BridgeDoors(ctx.World).Single(d => d.Entity == S.EntityB);
            var door = Node(S.EntityB, 2, "bridge", bridge.Centre, p.UpperY);
            Link(door, Node(S.EntityB, 2, "public", PlanGeometry.Centroid(p.PublicB), p.UpperY), NavEdgeKind.Corridor, S.DoorWidth);
        }
        else if (storey == 1)
        {
            var entrance = _entrances[id].Single(e => e.Id == S.EastDoorId);
            var entry = Node(S.EntityC, 1, "entry", new(entrance.Inside.X, entrance.Inside.Z), p.FloorY);
            var foot = Node(S.EntityC, 1, "formula.foot", p.FormulaFrame.P(0, -p.FormulaRun * 0.5f), p.FloorY);
            Link(entry, foot, NavEdgeKind.Room, p.FormulaWidth);
            Link(foot, Node(S.EntityC, 2, "formula.top", p.FormulaFrame.P(0, p.FormulaRun * 0.5f), p.UpperY), NavEdgeKind.Stair, p.FormulaWidth);
            var returning = Node(S.EntityC, 1, "return.lower", ReturnDoor(p, 1).Centre, p.FloorY);
            Link(entry, returning, NavEdgeKind.Room, Rules.DoorClearWidth);
            Link(returning, Node(S.EntityC, 2, "return.upper", ReturnDoor(p, 2).Centre, p.UpperY), NavEdgeKind.Stair, Rules.StairFlightWidth);
        }
        else
        {
            var previous = Node(S.EntityC, 2, "formula.top", p.FormulaFrame.P(0, p.FormulaRun * 0.5f), p.UpperY);
            for (var i = 0; i < p.CRoute.Count; i++)
            {
                var next = Node(S.EntityC, 2, "route." + i.ToString(CultureInfo.InvariantCulture), p.CRoute[i], p.UpperY);
                Link(previous, next, NavEdgeKind.Corridor, Rules.CorridorClearWidth); previous = next;
                if (i == 1)
                    Link(next, Node(S.EntityC, 2, "return.upper", ReturnDoor(p, 2).Centre, p.UpperY), NavEdgeKind.Door, Rules.DoorClearWidth);
            }
            var bridges = SixSite.Bridges(ctx.World).ToArray();
            foreach (var point in new[] { bridges[1].From, PlanGeometry.Centroid(SixSite.Plan(ctx.World, S.EntityC, S.LinkBC)), bridges[0].From })
            {
                var next = Node(S.EntityC, 2, "bridge." + nodes.Count.ToString(CultureInfo.InvariantCulture), point, p.UpperY);
                Link(previous, next, NavEdgeKind.Bridge, S.DoorWidth); previous = next;
            }
            Link(previous, Node(S.EntityB, 2, "bridge", bridges[0].To, p.UpperY), NavEdgeKind.Bridge, S.DoorWidth);
        }
        return new(id, (short)storey, nodes, edges);
    }

    private static void Compose(BuildContext ctx, FeatureId id, int storey, InteriorGeometry sink, bool full)
    {
        var p = SixInteriorPlan.Of(ctx);
        if (id.Value == S.EntityB)
        {
            sink.Floor(p.PublicB, p.UpperY, SixInteriorPlan.SlabThickness, FloorMaterial, FootMat.Tile);
            sink.Plate(p.PublicB, p.CeilingY, SixInteriorPlan.SlabThickness, WallMaterial);
            Enclose(ctx, sink, p, p.PublicB, p.BBase, p.UpperY, false);
            if (full) SixInteriorFurnishing.PublicBay(ctx, sink, p);
            return;
        }
        if (storey == 1)
        {
            sink.Floor(p.Hall, p.FloorY, SixInteriorPlan.SlabThickness, FloorMaterial, FootMat.Terrazzo);
            var (face, x) = SixSite.EastFace(ctx.World, SixSite.EastDoor(ctx.World));
            sink.Floor(new(face.Frame.Rect(x - S.VestibuleWidth * 0.5f, -S.Recess,
                x + S.VestibuleWidth * 0.5f, 0)), p.FloorY, SixInteriorPlan.SlabThickness, L.Granite, FootMat.StonePaving);
            sink.Plate(p.Hall, p.CeilingY, SixInteriorPlan.SlabThickness, WallMaterial);
            Enclose(ctx, sink, p, p.Hall, p.CBase, p.FloorY, false);
            Formula(sink, p);
            ReturnStair(sink, p, true);
            if (full) SixInteriorFurnishing.Hall(ctx, sink, p);
        }
        else
        {
            foreach (var floor in p.UpperFloors)
                sink.Floor(floor, p.UpperY, SixInteriorPlan.SlabThickness, FloorMaterial, FootMat.Tile);
            sink.Plate(p.UpperC, p.CeilingY, SixInteriorPlan.SlabThickness, WallMaterial);
            Enclose(ctx, sink, p, p.UpperC, p.CBase, p.UpperY, true);
            ReturnStair(sink, p, false);
            BridgeSupport(ctx, sink, p);
        }
    }

    private static void Formula(InteriorGeometry sink, SixInteriorPlan p)
    {
        var run = p.FormulaFlightRun;
        var part = new StairOpenPart();
        var parameters = new StairParams(p.FormulaWidth, p.StairRise * 0.5f, run, 15)
        { Material = WoodMaterial, Foot = FootMat.WoodFloor };
        sink.Part(part, parameters, p.FormulaFrame.At(0, p.FloorY, -(p.FormulaLanding + run) * 0.5f), true);
        sink.Floor(new(p.FormulaFrame.Rect(-p.FormulaWidth * 0.5f, -p.FormulaLanding * 0.5f,
            p.FormulaWidth * 0.5f, p.FormulaLanding * 0.5f)), p.LandingY,
            SixInteriorPlan.SlabThickness, WoodMaterial, FootMat.WoodFloor);
        sink.Part(part, parameters, p.FormulaFrame.At(0, p.LandingY,
            (p.FormulaLanding + run) * 0.5f), true);
    }

    private static void ReturnStair(InteriorGeometry sink, SixInteriorPlan p, bool lower)
    {
        var frame = p.ReturnFrame;
        var thick = Rules.PartitionThickness;
        var z0 = -p.ReturnDepth * 0.5f; var z1 = p.ReturnDepth * 0.5f;
        var x0 = -p.ReturnWidth * 0.5f; var x1 = p.ReturnWidth * 0.5f;
        if (lower)
            sink.Part(new StairDoublePart(), new StairParams(p.ReturnWidth, p.StairRise, p.ReturnDepth, 15)
            { Foot = FootMat.Terrazzo }, frame.At(0, p.FloorY, 0), true);
        var y = lower ? p.FloorY : p.UpperY;
        // The upper floor is the end of the return stair; its room is capped here, with no continuation upstairs.
        foreach (var (a, b) in new[] { (frame.P(x0, z0), frame.P(x0, z1)), (frame.P(x0, z1), frame.P(x1, z1)),
            (frame.P(x1, z1), frame.P(x1, z0)) })
            sink.Wall(a, b, y, lower ? p.UpperY : p.CeilingY, thick, WallMaterial);
        var doorX = ReturnDoor(p, lower ? 1 : 2).X;
        var halfDoor = Rules.DoorClearWidth * 0.5f;
        foreach (var (a, b) in GridFacade.Open(x0, x1, [(doorX - halfDoor, doorX + halfDoor)]))
            sink.Wall(frame.P(a, z0), frame.P(b, z0), y, lower ? p.UpperY : p.CeilingY, thick, WallMaterial);
        sink.Wall(frame.P(doorX - halfDoor, z0), frame.P(doorX + halfDoor, z0), y + Rules.DoorHeight,
            lower ? p.UpperY : p.CeilingY, thick, WallMaterial);
        sink.Part(new ExitSignPart(), new SignParams(0.4f, 0.15f, 0.04f), frame.At(doorX, y + Rules.DoorHeight, z0 - thick));
        if (!lower) sink.Plate(p.ReturnOpening, p.CeilingY, SixInteriorPlan.SlabThickness, WallMaterial);
    }

    private static void Enclose(BuildContext ctx, InteriorGeometry sink, SixInteriorPlan p, PolygonRings area,
        PolygonRings source, float y, bool gallery)
    {
        foreach (var (a, b, atrium) in p.InteriorEdges(area, source, gallery))
        {
            // The northern route ends on the real bridge opening; that threshold remains open.
            if (SixSite.BridgeDoors(ctx.World).Any(d => PlanLines.SegmentDistanceSquared(d.Centre, a, b) < 0.04f)) continue;
            if (gallery && atrium) sink.GlassGuard(a, b, y, 1.2f);
            else if (y == p.FloorY)
                foreach (var span in p.LowerWallSegments(a, b))
                    sink.Wall(span.A, span.B, y, span.Top, Rules.PartitionThickness, WallMaterial, SurfaceTag.DoorClosed);
            else
                sink.Wall(a, b, y, p.CeilingY, Rules.PartitionThickness, WallMaterial, SurfaceTag.DoorClosed);
        }
    }

    private static void BridgeSupport(BuildContext ctx, InteriorGeometry sink, SixInteriorPlan p)
    {
        var link = SixSite.Plan(ctx.World, S.EntityC, S.LinkBC);
        sink.Support(link, p.GroundY + S.LinkDeck, 0.7f, FootMat.StonePaving);
        foreach (var (edge, _, _) in GridFacade.Edges(link))
            foreach (var (x0, x1) in GridFacade.Open(0, edge.Length, SixSite.LinkOpenings(ctx.World, edge)))
                sink.Colliders.Add(new WallCollider(edge.Frame.P(x0, 0), edge.Frame.P(x1, 0), CirculationStyle.BarrierMinHalfThickness,
                    p.UpperY, p.UpperY + WalkRules.BarrierHeight, SurfaceTag.Barrier) { Foot = FootMat.Glass });
        foreach (var bridge in SixSite.Bridges(ctx.World))
        {
            var f = PlanFrame.Toward(bridge.From, bridge.To);
            var length = Vector2.Distance(bridge.From, bridge.To);
            sink.Support(new(f.Rect(0, -SixSite.BridgeHalfWidth, length, SixSite.BridgeHalfWidth)), p.GroundY + S.LinkDeck, 0.7f, FootMat.StonePaving);
            foreach (var side in new[] { -1.6f, 1.6f })
                sink.Colliders.Add(new WallCollider(f.P(0, side), f.P(length, side), CirculationStyle.BarrierMinHalfThickness,
                    p.UpperY, p.UpperY + WalkRules.BarrierHeight, SurfaceTag.Barrier) { Foot = FootMat.Glass });
        }
    }

    private static (float X, Vector2 Centre, Vector2 Hinge) ReturnDoor(SixInteriorPlan p, int floor)
    {
        var flight = (p.ReturnWidth - CirculationStyle.WellWidth) * 0.5f;
        var x = (floor == 1 ? -1 : 1) * (CirculationStyle.WellWidth + flight) * 0.5f;
        return (x, p.ReturnFrame.P(x, -p.ReturnDepth * 0.5f),
            p.ReturnFrame.P(x - Rules.DoorClearWidth * 0.5f, -p.ReturnDepth * 0.5f));
    }
    private bool Open(FeatureId id, int floor) => InteriorOpenPart.IndoorFloorsFor(_openParts, id).Contains(floor) && (floor is 1 or 2) && (id.Value == S.EntityC || floor == 2);
    private void Require(FeatureId id)
    {
        if (!_features.Contains(id)) throw new ArgumentException("Feature is not assigned to the six interior.", nameof(id));
    }
    internal static Vector3 V(Vector2 p, float y) => new(p.X, y, p.Y);
}
