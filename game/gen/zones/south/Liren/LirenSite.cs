using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.South.Framework;
using T = Campus.Gen.Zones.South.SouthTune.Liren;
using L = Campus.Gen.Zones.South.SouthTune.Look;
using W = Campus.Gen.Zones.South.SouthTune.Windows;

namespace Campus.Gen.Zones.South.Liren;

/// <summary>
/// Liren building (MAP-QSH-SOUTH). Blocks A and B stay with WP-14 (4 storeys, white family set in overlay/south.d/);
/// this site claims the connector entity, whose OSM parts WP-14 would extrude solid and so close the corner gateway:
/// the two four-storey pier blocks either side of it, the fourth-storey bridge over the three-storey void, held on square
/// columns at both open faces (§6.3), and the two-storey east link between A and B. The void stays open from the paved
/// plaza into the courtyard; its floor is paved.
/// </summary>
public sealed class LirenSite : SouthSite
{
    public const string Connector = "qsh:relation:11185938";
    public const string BlockA = "qsh:relation:11185936", BlockB = "qsh:way:814133732";
    public const string PierPart = "osm:way:814133734", BridgePart = "osm:way:814133733", LinkPart = "osm:way:814133736";
    /// <summary>Window grids (contract-29): both pier blocks in one grid (in part order), the bridge storey and the east link.</summary>
    public const string PierKey = "piers", BridgeKey = "bridge", LinkKey = "east-link";

    /// <summary>Pier blocks: storeys and cornice line as WP-14 builds A and B at 15.6 m (wall top 15.1, coping 16.57).</summary>
    internal static readonly GridStyle PierStyle = new(L.White, L.WhiteTrim, L.GreyPlinth, L.Glass, L.Metal, L.RoofSlab)
    {
        Bay = 3.6f,
        Pier = 1.6f,
        PierDepth = 0.4f,
        BandDepth = 0.3f,
        GroundStorey = T.GroundStorey,
        Storey = T.Storey,
        Storeys = 4,
        GroundSill = 0.9f,
        GroundWindow = 2.3f,
        Sill = 0.9f,
        Window = 2.1f,
        PlinthHeight = 0.6f,
        Cornice = 0.6f,
        CorniceProjection = 0.34f,
        Parapet = T.Parapet,
    };

    /// <summary>The bridge storey: built from the bridge floor, its "plinth" is the fascia beam over the void.</summary>
    internal static readonly GridStyle BridgeStyle = PierStyle with
    {
        GroundStorey = PierStyle.WallTop - T.BridgeFloor,
        Storeys = 1,
        GroundSill = 0.9f,
        GroundWindow = 2.0f,
        PlinthHeight = 0.8f,
        Plinth = L.WhiteTrim,
    };

    /// <summary>The east link: two glazed storeys between A and B.</summary>
    internal static readonly GridStyle LinkStyle = PierStyle with
    {
        Bay = 3.0f,
        Pier = 0.7f,
        PierDepth = 0.35f,
        Storeys = 2,
        GroundWindow = 2.5f,
        Window = 2.5f,
        Cornice = 0.4f,
        Parapet = 0.5f,
    };

    public override string Id => "18b.liren";

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(Connector), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    /// <summary>The connector's parts and, for each, the volumes its faces may lie against (the void under the bridge is open).</summary>
    internal sealed record Footprints(IReadOnlyList<IReadOnlyList<Vector2>> Piers, IReadOnlyList<Vector2> Bridge, IReadOnlyList<Vector2> Link,
        IReadOnlyList<IReadOnlyList<Vector2>> A, IReadOnlyList<IReadOnlyList<Vector2>> B)
    {
        public IReadOnlyList<IReadOnlyList<Vector2>> PierNeighbours => [.. A, .. B, Link];
        public IReadOnlyList<IReadOnlyList<Vector2>> LinkNeighbours => [.. A, .. B];
        public IReadOnlyList<IReadOnlyList<Vector2>> BridgeNeighbours => Piers;
    }

    internal static Footprints Rings(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(Connector), out var connector))
            throw new InvalidOperationException($"Liren connector {Connector} is missing from the world model.");
        // The facade outline (WP-08): wall hiding, columns, collision and window declarations all share this one layout.
        IReadOnlyList<IReadOnlyList<Vector2>> Polygons(string source) => connector.Parts.FirstOrDefault(p => p.SourceId == source) is { } part
            ? WorldFeatureGeometry.FacadeGeometryOf(part) switch
            {
                PolygonGeometry g => [g.Rings.Outer],
                MultiPolygonGeometry m => m.Polygons.Select(r => r.Outer).ToArray(),
                _ => throw new InvalidOperationException($"Liren part {source} is missing."),
            }
            : throw new InvalidOperationException($"Liren part {source} is missing.");
        var bridge = Polygons(BridgePart)[0];
        var piers = Polygons(PierPart);
        var link = Polygons(LinkPart)[0];
        return new(piers, bridge, link, Block(world, BlockA), Block(world, BlockB));
    }

    /// <summary>
    /// A neighbouring block's footprint: the outer rings of its own source parts. An entity's Geometry is only its aggregate
    /// query envelope (a bounding rectangle in some world models), which would swallow the connector and hide every face.
    /// </summary>
    internal static IReadOnlyList<IReadOnlyList<Vector2>> Block(IWorldModel world, string id)
    {
        if (!world.Features.TryGetValue(new(id), out var f)) throw new InvalidOperationException($"Liren block {id} is missing.");
        static IEnumerable<IReadOnlyList<Vector2>> Outers(OverlayGeometry? g) => g switch
        {
            PolygonGeometry p => [p.Rings.Outer],
            MultiPolygonGeometry m => m.Polygons.Select(r => r.Outer),
            _ => [],
        };
        var rings = f.Parts.Count > 0 ? f.Parts.SelectMany(p => Outers(WorldFeatureGeometry.FacadeGeometryOf(p))).ToArray() : Outers(f.Geometry).ToArray();
        return rings.Length > 0 ? rings : throw new InvalidOperationException($"Liren block {id} has no polygon footprint.");
    }

    /// <summary>An edge lying against one of the given volumes is not dressed.</summary>
    internal static GridEdge Dress(GridEdgeInfo e, IReadOnlyList<IReadOnlyList<Vector2>> neighbours)
    {
        var probe = (e.A + e.B) * 0.5f + e.Frame.W * 0.8f;
        if (neighbours.Any(ring => PlanGeometry.InsideByCrossings(ring, probe))) return new(EdgeKind.Hidden);
        return new(e.Length < 4f ? EdgeKind.Blank : EdgeKind.Plain);
    }

    /// <summary>Every gridded block of the connector with its place in the numbering, in grid order; y is the bridge storey's lift.</summary>
    internal static IEnumerable<(IReadOnlyList<Vector2> Ring, GridStyle Style, Func<GridEdgeInfo, GridEdge> Dress, float Lift, GridNumbering Numbering)> Blocks(Footprints f)
    {
        var feature = new FeatureId(Connector);
        var offset = 0;
        foreach (var pier in f.Piers)
        {
            Func<GridEdgeInfo, GridEdge> dress = e => Dress(e, f.PierNeighbours);
            yield return (pier, PierStyle, dress, 0f, new(feature, PierKey, offset, W.Teaching, W.Classroom));
            offset += GridFacade.Layout(pier, PierStyle, dress).Columns;
        }
        yield return (f.Link, LinkStyle, e => Dress(e, f.LinkNeighbours), 0f, new(feature, LinkKey, 0, W.Teaching, W.Corridor));
        yield return (f.Bridge, BridgeStyle, e => Dress(e, f.BridgeNeighbours), T.BridgeFloor, new(feature, BridgeKey, 0, W.Teaching, W.Corridor));
    }

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        // Blocks sharing a key (the two piers) add their columns; every block of a key has the same storeys.
        var grids = new SortedDictionary<string, (int Columns, int Rows)>(StringComparer.Ordinal);
        foreach (var (ring, style, dress, _, numbering) in Blocks(Rings(world)))
        {
            var layout = GridFacade.Layout(ring, style, dress);
            grids[numbering.Key] = grids.TryGetValue(numbering.Key, out var g) ? (g.Columns + layout.Columns, g.Rows) : (layout.Columns, layout.Rows);
        }
        return [.. grids.Select(p => (new FeatureId(Connector), new WindowGridSpec(p.Key, p.Value.Columns, p.Value.Rows)))];
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var f = Rings(ctx.World);
        var centre = PlanMath.Centroid(f.Bridge);
        var y = ctx.World.Terrain.HeightAt(centre.X, centre.Y);
        foreach (var (ring, style, dress, lift, numbering) in Blocks(f))
            GridFacade.Declare(facades, ring, y + lift, style, dress, numbering, WindowNumbers.Required(ctx, numbering.Feature, numbering.Key));
    }

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
    {
        var f = Rings(world);
        var blocks = Blocks(f).Select(b => new PolygonRings(b.Ring));
        if (lod is LodLevel.L3 or LodLevel.L4) return [.. blocks];
        var half = (T.ColumnSize + 0.3f) * 0.5f;
        return [.. blocks, .. Columns(f).Select(c => new PolygonRings(new PlanFrame(c.Point, c.Along).Rect(-half, -half, half, half)))];
    }

    public override void Build(SiteBuilder b)
    {
        var f = Rings(b.Context.World);
        var y = b.Ground(PlanMath.Centroid(f.Bridge));
        // Piers, the east link and the bridge storey over the void; the soffit slab under it reads the void's ceiling at three storeys.
        foreach (var (ring, style, dress, lift, numbering) in Blocks(f)) GridFacade.Build(b, ring, y + lift, style, dress, numbering);
        var bridge = new PolygonRings(f.Bridge);
        b.Add(PartGeometryKernel.Extrude(bridge, T.BridgeFloor - T.Soffit, L.White, y + T.Soffit), Matrix4x4.Identity);
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        b.Add(PartGeometryKernel.Extrude(bridge, 0.04f, L.Paving, y - 0.01f), Matrix4x4.Identity);
        var bevel = b.Detail == SiteDetail.Full ? 0.03f : 0f;
        foreach (var at in Columns(f))
        {
            var col = new PlanFrame(at.Point, at.Along);
            b.Add(PartGeometryKernel.BeveledBox(T.ColumnSize + 0.3f, 0.45f, T.ColumnSize + 0.3f, bevel, L.GreyPlinth), col.At(0, y - 0.05f, 0));
            b.Add(PartGeometryKernel.BeveledBox(T.ColumnSize, T.Soffit - 0.4f, T.ColumnSize, bevel, L.White), col.At(0, y + 0.4f, 0));
            if (b.Detail != SiteDetail.Mass)
                b.Add(PartGeometryKernel.BeveledBox(T.ColumnSize + 0.25f, 0.35f, T.ColumnSize + 0.25f, bevel, L.WhiteTrim), col.At(0, y + T.Soffit - 0.35f, 0));
        }
    }

    /// <summary>Two columns on each open face of the void, at its thirds, set in under the bridge.</summary>
    internal static IEnumerable<(Vector2 Point, Vector2 Along)> Columns(Footprints f)
    {
        foreach (var (e, _, _) in GridFacade.Edges(f.Bridge))
        {
            if (Dress(e, f.BridgeNeighbours).Kind == EdgeKind.Hidden) continue;
            foreach (var t in new[] { 1f / 3f, 2f / 3f })
                yield return (e.Frame.P(e.Length * t, -T.ColumnInset), e.Frame.U);
        }
    }

    public override void Collide(SiteColliders c)
    {
        var f = Rings(c.Context.World);
        var centre = PlanMath.Centroid(f.Bridge);
        var y = c.Ground(centre.X, centre.Y);
        foreach (var pier in f.Piers) GridFacade.Collide(c, pier, y, PierStyle, e => Dress(e, f.PierNeighbours), FootMat.Concrete);
        GridFacade.Collide(c, f.Link, y, LinkStyle, e => Dress(e, f.LinkNeighbours), FootMat.Concrete);
        foreach (var (point, along) in Columns(f))
            c.Box(new PlanFrame(point, along).At(0, y + T.Soffit * 0.5f, 0), new(T.ColumnSize * 0.5f + 0.05f, T.Soffit * 0.5f, T.ColumnSize * 0.5f + 0.05f), SurfaceTag.Wall, FootMat.Concrete);
    }
}
