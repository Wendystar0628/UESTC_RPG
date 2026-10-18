using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using S = Campus.Gen.Zones.Core.LibraryInterior.LibraryInteriorStyle;

namespace Campus.Gen.Zones.Core.LibraryInterior;

/// <summary>
/// 图书馆室内 (WP-17, MAP-QSH-LIBRARY), laid out on the shell's own plan so walls, floors, windows and the skylight agree.
/// Every open storey has the shell interior and the atrium rings; 1F carries the hall and 5F the west aisle and reading band.
/// The other catalogue features of the library entry (the north-east connector and the cafe store front) get plain
/// floor plates and wall linings until their rooms are built.
/// </summary>
[InteriorProvider("interior.library", Owner = "WP-17")]
public sealed class LibraryInteriorProvider : IInteriorProvider, IInteriorCatalogConfigurable
{
    private sealed record CatalogState(FeatureId[] Features, int[] Floors, IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> Entrances);

    private CatalogState _catalog = new([], [], new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>());

    public IReadOnlyList<FeatureId> FeatureIds => _catalog.Features;
    public int Priority => 0;
    public bool Walkable => true;

    /// <summary>Boot supplies the authoritative interiors.json fields; generators perform no file IO.</summary>
    public void Configure(IEnumerable<FeatureId> features, IEnumerable<int> openFloors,
        IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> entrances)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(openFloors);
        ArgumentNullException.ThrowIfNull(entrances);
        var ids = features.Distinct().OrderBy(x => x.Value, StringComparer.Ordinal).ToArray();
        var floors = openFloors.Distinct().Order().ToArray();
        if (ids.Length == 0 || floors.Length == 0) throw new ArgumentException("The library interior needs features and open floors.");
        if (!ids.Contains(CoreIds.Library)) throw new ArgumentException("The library interior must include the library entity.", nameof(features));
        if (floors.Any(f => f < 1 || f > CoreStyle.LibraryStoreys.Length))
            throw new ArgumentOutOfRangeException(nameof(openFloors), "Library storeys run from 1 to the shell's storey count.");
        _catalog = new(ids, floors, entrances);
    }

    public InteriorLayout Layout(BuildContext ctx, FeatureId id)
    {
        var catalog = Require(ctx, id);
        var entrances = catalog.Entrances.GetValueOrDefault(id) ?? [];
        return id == CoreIds.Library ? LibraryLayout(ctx, id, catalog, entrances) : PlateLayout(ctx, id, catalog, entrances);
    }

    public ContentOutput BuildStorey(BuildContext ctx, FeatureId id, int storey, InteriorDetail detail)
    {
        var full = detail switch
        {
            InteriorDetail.Full => true,
            InteriorDetail.Shell or InteriorDetail.Proxy => false,
            _ => throw new ArgumentOutOfRangeException(nameof(detail)),
        };
        var catalog = Require(ctx, id);
        if (!catalog.Floors.Contains(storey)) return ContentOutput.Empty;
        var sink = new InteriorSink(ctx.Catalog) { Full = full };
        if (id == CoreIds.Library) Compose(ctx, id, storey, sink);
        else Plates(ctx, id, storey, sink);
        return sink.Finish();
    }

    public IReadOnlyList<Collider> CollisionStorey(BuildContext ctx, FeatureId id, int storey)
    {
        var catalog = Require(ctx, id);
        if (id != CoreIds.Library || !catalog.Floors.Contains(storey)) return [];
        var sink = new InteriorSink(ctx.Catalog) { Draw = false };
        Compose(ctx, id, storey, sink);
        return sink.Colliders.ToArray();
    }

    public NavFloorGraph Nav(BuildContext ctx, FeatureId id, int storey)
    {
        var catalog = Require(ctx, id);
        if (id != CoreIds.Library || !catalog.Floors.Contains(storey)) return NavFloorGraph.Empty(id, storey);
        var f = LibraryFrame.Of(ctx.World);
        var nodes = new List<NavNode>();
        var edges = new List<NavEdge>();
        NavNode Node(int floor, string role, Vector2 p)
        {
            var node = new NavNode(new(StableHash.Hash32(ctx.WorldSeed, id.Value, floor.ToString(CultureInfo.InvariantCulture), role)),
                LibraryFrame.V(p, f.FloorY(floor)), new(id, (short)floor), role);
            nodes.Add(node);
            return node;
        }
        void Link(NavNode a, NavNode b, NavEdgeKind kind, float width) => edges.Add(new(a.Id, b.Id, kind,
            Vector3.Distance(a.P, b.P), width, b.P.Y - a.P.Y, NavFlags.Indoor, "library.interior"));

        var radius = (f.VoidApothem + f.GalleryApothem) * 0.5f;
        var ring = Enumerable.Range(0, 8).Select(j => Node(storey, "ring." + j.ToString(CultureInfo.InvariantCulture), f.At(j, radius, 0f))).ToArray();
        for (var j = 0; j < 8; j++) Link(ring[j], ring[(j + 1) % 8], NavEdgeKind.Corridor, 2f);
        if (storey == 1)
        {
            var centre = Node(1, "centre", f.At(0, 6.5f, 0f));
            Link(centre, ring[0], NavEdgeKind.Room, 3f);
            foreach (var j in S.StairFaces)
            {
                // Round the exhibition boards at the centre on the stair's own side.
                var key = j.ToString(CultureInfo.InvariantCulture);
                var side = Node(1, "stair.side." + key, f.At(j < 4 ? j - 1 : j + 1, 3f, 0f));
                var foot = Node(1, "stair." + key, f.At(j, TwinStair.FootRadius(f) - 0.8f, 0f));
                Link(centre, side, NavEdgeKind.Room, 3f);
                Link(side, foot, NavEdgeKind.Room, 3f);
                if (catalog.Floors.Contains(2))
                    Link(foot, Node(2, "ring." + j.ToString(CultureInfo.InvariantCulture), f.At(j, radius, 0f)), NavEdgeKind.Stair, S.StairWidth);
            }
            foreach (var j in new[] { 1, 7 })
                Link(ring[j], Node(1, "sector." + j.ToString(CultureInfo.InvariantCulture), f.At(j, f.GalleryApothem + 3.5f, 0f)), NavEdgeKind.Room, 3f);
            for (var j = 0; j < 8; j += 2)
                Link(ring[j], Node(1, "mouth." + j.ToString(CultureInfo.InvariantCulture), f.At(j, f.GalleryApothem + 1.5f, 0f)), NavEdgeKind.Corridor, S.CorridorWidth);
        }
        if (storey == f.Storeys)
        {
            var end = WestBand.AisleEnd(f, WestBand.Wall(f));
            var aisle = Node(storey, "aisle", f.At(3, (f.GalleryApothem + end) * 0.5f, 0f));
            var band = Node(storey, "band", f.At(3, end + S.BandDepth * 0.5f, 0f));
            Link(ring[3], aisle, NavEdgeKind.Corridor, S.AisleWidth);
            Link(aisle, band, NavEdgeKind.Corridor, S.AisleWidth);
            foreach (var t in new[] { -10f, 10f })
                Link(band, Node(storey, "band." + (t < 0 ? "a" : "b"), f.At(3, end + S.BandDepth * 0.5f, t)), NavEdgeKind.Room, 2f);
        }
        return new(id, (short)storey, nodes, edges);
    }

    private static void Compose(BuildContext ctx, FeatureId id, int storey, InteriorSink sink)
    {
        var f = LibraryFrame.Of(ctx.World);
        var rng = new Rng(StableHash.Hash32(ctx.WorldSeed, id.Value, "library.interior", storey.ToString(CultureInfo.InvariantCulture)));
        ShellInterior.Build(f, storey, sink);
        Atrium.Build(f, storey, sink, ref rng);
        if (storey == 1) Hall.Build(f, sink, ref rng);
        if (storey == f.Storeys) WestBand.Build(f, sink, ref rng);
    }

    private CatalogState Require(BuildContext ctx, FeatureId id)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var catalog = _catalog;
        if (!catalog.Features.Contains(id)) throw new ArgumentException("Feature is not assigned to this provider.", nameof(id));
        if (!ctx.World.Features.ContainsKey(id)) throw new KeyNotFoundException(id.Value);
        return catalog;
    }

    private static InteriorLayout LibraryLayout(BuildContext ctx, FeatureId id, CatalogState catalog, IReadOnlyList<EntranceDecl> entrances)
    {
        var f = LibraryFrame.Of(ctx.World);
        var storeys = Enumerable.Range(1, f.Storeys).Select(k => new StoreyInfo(k, f.FloorY(k), f.TopY(k) - f.FloorY(k), f.Core.Ring)).ToArray();
        var zones = new List<ZoneVolume>();
        ZoneVolume Zone(int storey, int index, ZoneKind kind, IReadOnlyList<Vector2> ring) => new(storey, storey * 100 + index, kind, ring,
            S.LightChannel, StableHash.Hash32(ctx.WorldSeed, id.Value, "library.light", storey.ToString(CultureInfo.InvariantCulture),
                index.ToString(CultureInfo.InvariantCulture)), true);
        foreach (var k in catalog.Floors)
        {
            zones.Add(Zone(k, 0, k == 1 ? ZoneKind.Lobby : ZoneKind.Atrium, f.Octagon(f.GalleryApothem)));
            if (k == 1)
            {
                var t = Atrium.SectorHalf(f);
                foreach (var j in new[] { 1, 7 })
                    zones.Add(Zone(k, j, ZoneKind.Room, Quad(f, j, f.GalleryApothem, f.GalleryApothem + S.SectorDepth, t)));
            }
            if (k == f.Storeys)
            {
                var end = WestBand.AisleEnd(f, WestBand.Wall(f));
                zones.Add(Zone(k, 1, ZoneKind.Corridor, Quad(f, 3, f.GalleryApothem, end, S.AisleWidth * 0.5f)));
                zones.Add(Zone(k, 2, ZoneKind.Room, Quad(f, 3, end, end + S.BandDepth, 15f)));
            }
        }
        var portals = new List<PortalDecl>();
        foreach (var e in entrances.Where(x => x.Open))
        {
            var storey = storeys.Where(s => s.FloorY <= e.Center.Y + 0.3f).Select(s => s.Index).DefaultIfEmpty(1).Max();
            if (!catalog.Floors.Contains(storey)) continue;
            var yaw = e.YawDeg * (MathF.PI / 180f);
            var right = new Vector3(MathF.Cos(yaw), 0f, MathF.Sin(yaw)) * (e.ClearWidth * 0.5f);
            portals.Add(new(storey, -1, storey * 100, e.Center - right, e.Center + right,
                e.HeadY > e.Center.Y ? e.HeadY - e.Center.Y : WalkRules.UnknownEntranceCollisionHeight, PortalKind.Door));
        }
        if (catalog.Floors.Contains(1) && catalog.Floors.Contains(2))
            foreach (var j in S.StairFaces)
                portals.Add(new(2, 100, 200, LibraryFrame.V(f.At(j, f.VoidApothem, -S.StairWidth * 0.5f), f.FloorY(2)),
                    LibraryFrame.V(f.At(j, f.VoidApothem, S.StairWidth * 0.5f), f.FloorY(2)), f.TopY(1) - f.FloorY(1), PortalKind.StairOpening));
        foreach (var k in catalog.Floors.Where(n => catalog.Floors.Contains(n + 1)))
            portals.Add(new(k + 1, k * 100, (k + 1) * 100, LibraryFrame.V(f.At(2, f.VoidApothem, 0f), f.FloorY(k + 1)),
                LibraryFrame.V(f.At(6, f.VoidApothem, 0f), f.FloorY(k + 1)), f.TopY(k + 1) - f.FloorY(k + 1), PortalKind.AtriumOpening));
        var points = f.Plan.Blocks.SelectMany(b => b.Ring).ToArray();
        var bounds = new Box3(new(points.Min(p => p.X), f.FloorY(1) - S.SlabThick, points.Min(p => p.Y)),
            new(points.Max(p => p.X), f.Plan.Blocks.Max(b => b.Y1), points.Max(p => p.Y)));
        return new(id, storeys, zones, portals, entrances, [], catalog.Floors, catalog.Floors, bounds,
            StableHash.Hash32(ctx.WorldSeed, id.Value, "library.interior.v1"));
    }

    /// <summary>A rectangle along Dir(j) from r0 to r1, half-width t across.</summary>
    private static IReadOnlyList<Vector2> Quad(LibraryFrame f, int j, float r0, float r1, float t) =>
        [f.At(j, r0, -t), f.At(j, r1, -t), f.At(j, r1, t), f.At(j, r0, t)];

    // ------------------------------------------------------------------ the entry's other features

    /// <summary>A neighbour block's polygons, storey height, first floor and top, from its own data and the library's floor offset.</summary>
    private static (IReadOnlyList<IReadOnlyList<Vector2>> Rings, float Height, float Floor, float Top, int Levels) PlateSection(BuildContext ctx, FeatureId id)
    {
        var feature = ctx.World.Features[id];
        var rings = WorldFeatureGeometry.FacadePolygonsOf(feature, "building").Select(p => PlanGeometry.OpenRing(p.Outer)).ToArray();
        if (rings.Length == 0) throw new InvalidOperationException($"Library interior feature {id.Value} has no building footprint.");
        var levels = Math.Max(1, feature.Storeys ?? feature.Parts.Select(p => p.Storeys ?? 0).DefaultIfEmpty(0).Max());
        var height = feature.Height ?? feature.Parts.Select(p => p.Height ?? 0f).DefaultIfEmpty(0f).Max();
        if (height <= 0f) height = levels * WorldModelLoader.DefaultStoreyHeight;
        var plan = LibraryFrame.Of(ctx.World).Plan;
        var pad = ctx.World.Terrain.PadY(id);
        return (rings, height / levels, pad + (plan.FloorY - plan.PadY), pad + height, levels);
    }

    private static float PlateY(float floor, float storeyHeight, float top, int storey) =>
        MathF.Min(floor + (storey - 1) * storeyHeight, top - S.SlabThick - 0.1f);

    private static InteriorLayout PlateLayout(BuildContext ctx, FeatureId id, CatalogState catalog, IReadOnlyList<EntranceDecl> entrances)
    {
        var (rings, height, floor, top, _) = PlateSection(ctx, id);
        var storeys = catalog.Floors.Select(k => new StoreyInfo(k, PlateY(floor, height, top, k), height, rings[0])).ToArray();
        var feature = ctx.World.Features[id];
        return new(id, storeys, [], [], entrances, [], catalog.Floors, [], feature.Bounds,
            StableHash.Hash32(ctx.WorldSeed, id.Value, "library.plates.v1"));
    }

    /// <summary>Floor plate, its soffit and the wall lining of one storey of a neighbour block (simplified interior, no collision).</summary>
    private static void Plates(BuildContext ctx, FeatureId id, int storey, InteriorSink sink)
    {
        var (rings, height, floor, top, levels) = PlateSection(ctx, id);
        var y = PlateY(floor, height, top, storey);
        foreach (var ring in rings)
        {
            var polygon = new PolygonRings(ring);
            sink.Plate(S.Stone(S.Floor), polygon, y, true);
            sink.Plate(S.Wall(S.CeilingWhite), polygon, y - S.SlabThick, false);
            if (storey > levels) continue;
            var y1 = MathF.Min(y + height, top - S.CeilingGap);
            for (var i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                if (Vector2.Distance(a, b) < 0.1f) continue;
                var n = PlanGeometry.RightUnitNormal(a, b);
                sink.Prism(S.Wall(S.WallWhite), [a - n * S.LiningOut, b - n * S.LiningOut, b - n * S.LiningIn, a - n * S.LiningIn], y, y1);
            }
            if (storey == levels) sink.Plate(S.Wall(S.CeilingWhite), polygon, top - S.CeilingGap, false);
        }
    }
}
