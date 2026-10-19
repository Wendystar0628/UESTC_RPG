using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Look;
using R = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Research;
using W = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Windows;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Research;

/// <summary>
/// One research building of 中部科研区 (MAP-QSH-CENTER-EAST), claimed from WP-14 and built by this zone: its
/// block parts as five-storey <see cref="GridFacade"/> blocks in the renovated research family, its lens hall as a glass
/// hall with slab bands and an arc front, one entrance (a portal on a block face or doors in the hall's arc), stair
/// penthouses and fenced plant on the roofs. Subclasses add each building's own roof features. Footprints come from the
/// world model; every value lives in <see cref="R"/>. The buildings stay closed (WLD-INTERIORS): doors are shut and
/// collide as DoorClosed. Each gridded block is one named window grid (contract-29) sized from the same layout as its
/// geometry; the lens halls' glass and the entrance glazing stay unnumbered.
/// </summary>
public abstract class ResearchSite : ZoneSite
{
    /// <summary>The shared research frame R: X along the blocks' long faces, +Z roughly south.</summary>
    internal static readonly PlanFrame Frame = PlanFrame.Along(Vector2.Zero, R.Axis);

    internal static readonly GridStyle Style = new(L.Stone, L.LightTrim, L.DarkGranite, ZoneLooks.Glass, L.Metal, L.RoofSlab)
    {
        Bay = R.Bay,
        Pier = R.Pier,
        PierDepth = 0.45f,
        BandDepth = 0.3f,
        GroundStorey = R.GroundStorey,
        Storey = R.Storey,
        Storeys = R.Storeys,
        GroundSill = R.GroundSill,
        GroundWindow = R.GroundWindow,
        Sill = R.Sill,
        Window = R.Window,
        PlinthHeight = 0.9f,
        Cornice = 0.7f,
        CorniceProjection = 0.5f,
        Parapet = 0.6f,
        PilasterEvery = R.PilasterEvery,
    };

    private static readonly StepsPart Steps = new();

    /// <summary>The claimed entity.</summary>
    internal abstract string Entity { get; }
    /// <summary>Its block parts, built as gridded blocks, with the key of each block's window grid.</summary>
    internal abstract IReadOnlyList<(string Source, string Key)> Blocks { get; }
    /// <summary>Entities whose blocks touch this one's (their shared faces are hidden, facing faces read as a courtyard).</summary>
    internal virtual IReadOnlyList<string> Neighbours => [];
    internal abstract string HallId { get; }
    internal abstract string HallBackId { get; }
    /// <summary>The hall's part, whose OSM levels are the hall's storeys.</summary>
    internal abstract string HallPart { get; }
    /// <summary>Local door point on a block face, or null when the door is in the hall's arc.</summary>
    internal abstract string? BlockDoorId { get; }
    internal abstract IReadOnlyList<string> StairIds { get; }
    internal abstract IReadOnlyList<string> PlantIds { get; }

    private string RoofId => Entity == CenterEastIds.Research.KeA ? CenterEastIds.Research.KeARoofId
        : Entity == CenterEastIds.Research.KeB ? CenterEastIds.Research.KeBRoofId : CenterEastIds.Research.KeCGardenId;

    private Campus.Contracts.Maps.MapRect HallRange(IWorldModel world) =>
        SiteRanges.Bounds(world.ContourSegment(HallBackId), world.Feature(HallId).Number("sagitta") + R.HallCanopyDepth + 3 * R.TreadDepth);

    private static float BlockPadding => R.PortalHalfWidth + R.PortalJamb + R.PortalDepth + 3 * R.TreadDepth;

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(Entity), BlockPadding), HallRange(world),
         SiteRanges.Bounds(world.Feature(RoofId), R.KeATankSize.X),
         .. StairIds.Select(id => SiteRanges.Point(world.Feature(id).Point(), R.StairSize.Length() * 0.5f)),
         .. PlantIds.Select(id => SiteRanges.Point(world.Feature(id).Point(), R.PlantSize.Length() * 0.5f))];

    public override IReadOnlyList<Claim> Claims => [new FeatureClaim(new(Entity), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
    {
        var plan = Footprints(world);
        return [.. GridBlocks(plan).Select(block => new PolygonRings(block.Ring)), new(plan.Hall)];
    }

    // ---- Plan ----

    /// <summary>
    /// World rings of this building's blocks, of every block that may hide or face them, and of its lens hall with the hall's
    /// storeys, and the ring of a raised block over them when the building has one (科 B's attic).
    /// </summary>
    internal sealed record Plan(IReadOnlyList<IReadOnlyList<Vector2>> Blocks, IReadOnlyList<IReadOnlyList<Vector2>> AllBlocks, Vector2[] Hall,
        int HallStoreys, IReadOnlyList<Vector2>? Raised, Vector2[] Back, float Sagitta, Vector2? Door);

    internal Plan Footprints(IWorldModel world)
    {
        var own = Blocks.Select(block => Ring(world, Entity, block.Source)).ToArray();
        var all = own.Concat(Neighbours.SelectMany(n => NeighbourBlocks(world, n))).ToArray();
        var (_, hallStoreys) = world.Feature(Entity).Part(HallPart).Levels();
        var back = world.ContourSegment(HallBackId);
        var sagitta = world.Feature(HallId).Number("sagitta");
        return new(own, all, HallRing(back, sagitta, 16), hallStoreys, RaisedRing(world), back, sagitta,
            BlockDoorId is null ? null : world.Feature(BlockDoorId).Point());
    }

    /// <summary>The ring of a block raised over the main ones, in world plan, or null.</summary>
    internal virtual IReadOnlyList<Vector2>? RaisedRing(IWorldModel world) => null;

    internal static IReadOnlyList<Vector2> Ring(IWorldModel world, string entity, string source)
    {
        return world.Feature(entity).Contour(source);
    }

    private static IEnumerable<IReadOnlyList<Vector2>> NeighbourBlocks(IWorldModel world, string entity) => entity switch
    {
        var id when id == CenterEastIds.Research.KeB => new[] { CenterEastIds.Research.KeBMain, CenterEastIds.Research.KeBMiddle, CenterEastIds.Research.KeBWest }.Select(source => Ring(world, CenterEastIds.Research.KeB, source)),
        var id when id == CenterEastIds.Research.KeC => new[] { CenterEastIds.Research.KeCBar, CenterEastIds.Research.KeCLink }.Select(source => Ring(world, CenterEastIds.Research.KeC, source)),
        _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Not a research building."),
    };

    /// <summary>A lens hall's world outline: the local back polyline closed by an arc bulging away from it, outside on the right.</summary>
    internal static Vector2[] HallRing(IReadOnlyList<Vector2> back, float sagitta, int segments)
    {
        var points = back.ToList();
        Vector2 a = points[^1], b = points[0];
        var centre = points.Aggregate(Vector2.Zero, (s, p) => s + p) / points.Count;
        var chord = Vector2.Normalize(b - a);
        var side = new Vector2(-chord.Y, chord.X);
        if (Vector2.Dot(side, (a + b) * 0.5f - centre) < 0) side = -side;
        var arc = Shapes.Arc(a, b, sagitta, side, segments);
        points.AddRange(arc.Skip(1).Take(arc.Length - 2));
        return PlanMath.OutsideRight(points);
    }

    /// <summary>The hall's arc apex and its outward direction (the C hall's doors stand there).</summary>
    internal (Vector2 Apex, Vector2 Out) HallApex(Plan plan)
    {
        Vector2 a = plan.Back[^1], b = plan.Back[0];
        var centre = plan.Back.Aggregate(Vector2.Zero, (s, p) => s + p) / plan.Back.Length;
        var chord = Vector2.Normalize(b - a);
        var side = new Vector2(-chord.Y, chord.X);
        if (Vector2.Dot(side, (a + b) * 0.5f - centre) < 0) side = -side;
        return ((a + b) * 0.5f + side * plan.Sagitta, side);
    }

    internal float BaseY(BuildContext ctx) => ctx.World.Terrain.PadY(new(Entity));

    // ---- Edge dressing ----

    /// <summary>
    /// Hidden where the face lies against another block; Blank when too short for a bay; Front (pilasters) on long faces
    /// that do not look into a courtyard; the door's face is cut for the portal up to the second-floor sill.
    /// </summary>
    internal GridEdge Dress(GridEdgeInfo e, Plan plan)
    {
        // Hidden only when the whole face is covered: a link strip touching the middle of a long face leaves it dressed.
        var mid = (e.A + e.B) * 0.5f;
        if (new[] { 0.1f, 0.5f, 0.9f }.All(t => plan.AllBlocks.Any(r => PlanGeometry.InsideByCrossings(r, Vector2.Lerp(e.A, e.B, t) + e.Frame.W * 0.6f))))
            return new(EdgeKind.Hidden);
        if (e.Length < 4.5f) return new(EdgeKind.Blank);
        var courtyard = false;
        for (var d = 3f; d <= R.CourtyardReach && !courtyard; d += 3f)
            courtyard = plan.AllBlocks.Any(r => PlanGeometry.InsideByCrossings(r, mid + e.Frame.W * d));
        var kind = e.Length >= R.FrontMin && !courtyard ? EdgeKind.Front : EdgeKind.Plain;
        if (DoorOn(e, plan) is not { } x) return new(kind);
        var half = R.PortalHalfWidth + R.PortalJamb;
        return new(kind) { Cuts = [(x - half, x + half, PortalTop)] };
    }

    /// <summary>Local x of the block door on this face, when the door point lies on it.</summary>
    internal float? DoorOn(GridEdgeInfo e, Plan plan)
    {
        if (plan.Door is not { } door) return null;
        var local = e.Frame.Local(door);
        var half = R.PortalHalfWidth + R.PortalJamb;
        return MathF.Abs(local.Y) < 0.6f && local.X > half && local.X < e.Length - half ? local.X : null;
    }

    // ---- Gridded blocks and their window grids: one layout for geometry, collision and declarations ----

    /// <summary>
    /// The building's gridded blocks: its block parts on the pad, plus any raised block (科 B's attic) with its lift above the
    /// pad. Each is one named window grid of the entity, numbered from column 0.
    /// </summary>
    internal virtual IEnumerable<(string Key, IReadOnlyList<Vector2> Ring, GridStyle Style, float Lift, Func<GridEdgeInfo, GridEdge> Dress)> GridBlocks(Plan plan)
    {
        for (var i = 0; i < Blocks.Count; i++)
            yield return (Blocks[i].Key, plan.Blocks[i], Style, 0f, e => Dress(e, plan));
    }

    private GridNumbering Numbering(string key) =>
        new(new(Entity), key, 0, W.Research, W.Lab) { AltCard = W.Office, AltShare = W.OfficeShare };

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        foreach (var (key, ring, style, _, dress) in GridBlocks(Footprints(world)))
        {
            var layout = GridFacade.Layout(ring, style, dress);
            if (layout.Columns > 0) yield return (new(Entity), new(key, layout.Columns, layout.Rows));
        }
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var y = BaseY(ctx);
        foreach (var (key, ring, style, lift, dress) in GridBlocks(Footprints(ctx.World)))
        {
            if (GridFacade.Layout(ring, style, dress).Columns == 0) continue;
            GridFacade.Declare(facades, ring, y + lift, style, dress, Numbering(key), WindowNumbers.Required(ctx, new(Entity), key));
        }
    }

    // ---- Build ----

    public override void Build(SiteBuilder b)
    {
        var plan = Footprints(b.Context.World);
        var y = BaseY(b.Context);
        foreach (var (key, ring, style, lift, dress) in GridBlocks(plan))
        {
            if (!b.Includes(SiteRanges.Bounds(ring, BlockPadding))) continue;
            var windowed = GridFacade.Layout(ring, style, dress).Columns > 0;
            GridFacade.Build(b, ring, y + lift, style, dress, windowed ? Numbering(key) : null);
        }
        foreach (var ring in plan.Blocks)
            foreach (var (e, _, _) in GridFacade.Edges(ring))
                if (DoorOn(e, plan) is { } x && b.Includes(SiteRanges.Point(e.Frame.P(x, 0), BlockPadding))) Portal(b, e.Frame, x, y);
        if (b.Includes(HallRange(b.Context.World))) Hall(b, plan, y);
        var roof = y + Style.RoofTop;
        foreach (var id in StairIds.Where(id => b.Includes(SiteRanges.Point(b.Context.World.Feature(id).Point(), R.StairSize.Length() * 0.5f)))) Penthouse(b, Frame.Local(b.Context.World.Feature(id).Point()), roof);
        foreach (var id in PlantIds.Where(id => b.Includes(SiteRanges.Point(b.Context.World.Feature(id).Point(), R.PlantSize.Length() * 0.5f)))) Plant(b, Frame.Local(b.Context.World.Feature(id).Point()), roof);
        if (b.Includes(SiteRanges.Bounds(b.Context.World.Feature(RoofId), R.KeATankSize.X))) Roof(b, plan, y);
    }

    /// <summary>Each building's own roof features (fence and tank, attic and antennas, garden).</summary>
    private protected abstract void Roof(SiteBuilder b, Plan plan, float y);

    // ---- Entrance portal on a block face ----

    private static float PortalTop => Style.SillOf(R.PortalStoreys);

    private static StairParams Flight(float width) =>
        new(width, R.FloorLevel, 3 * R.TreadDepth, 3) { Variation = 1, Material = L.Granite, Foot = FootMat.StonePaving };

    /// <summary>
    /// Jambs and head project <see cref="R.PortalDepth"/> from the glass plane; inside them a landing at floor level, the
    /// glass doors and mullioned side lights and transom on the glass plane; three risers out to the pad.
    /// </summary>
    private static void Portal(SiteBuilder b, PlanFrame f, float x, float y)
    {
        float hw = R.PortalHalfWidth, j = R.PortalJamb, d = R.PortalDepth, top = y + PortalTop, floor = y + R.FloorLevel;
        var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        GridFacade.Box(b, f, x - hw - j, x - hw, y - 0.1f, top, -0.3f, d, L.LightTrim, bevel);
        GridFacade.Box(b, f, x + hw, x + hw + j, y - 0.1f, top, -0.3f, d, L.LightTrim, bevel);
        GridFacade.Box(b, f, x - hw - j - 0.1f, x + hw + j + 0.1f, top - R.PortalHead, top, -0.3f, d + 0.1f, L.LightTrim, bevel);
        GridFacade.Box(b, f, x - hw - j - 0.15f, x + hw + j + 0.15f, top, top + 0.1f, -0.3f, d + 0.15f, L.Stone, bevel * 0.5f);
        GridFacade.Box(b, f, x - hw, x + hw, y - 0.1f, floor, 0, d, L.Granite, bevel * 0.5f);
        if (b.Detail == SiteDetail.Mass) return;
        // Glazing on the glass plane (the block's glass core): the doors, a transom over them and two side lights.
        var head = floor + R.DoorHeight;
        var lintel = top - R.PortalHead;
        b.Place(PartCatalog.Door, new DoorParams(R.DoorWidth, R.DoorHeight, 0.14f, 0, 2) { Foot = FootMat.Glass }, f.AtReversed(x, floor, 0.07f));
        // Lower storey: doors, a transom light over them and a side light either side, up to the upper floor's slab
        // band; upper storey: one four-light band of glazing up to the head.
        float band0 = y + Style.FloorOf(1) - 0.2f, band1 = band0 + 0.55f;
        b.Place(PartCatalog.CurtainWall, new WindowParams(R.DoorWidth, band0 - head, 0.12f, 2, 1), f.AtReversed(x, head, 0));
        var side = hw - R.DoorWidth * 0.5f;
        foreach (var s in new[] { -1f, 1f })
            b.Place(PartCatalog.CurtainWall, new WindowParams(side, band0 - floor, 0.12f, 1, 2), f.AtReversed(x + s * (R.DoorWidth * 0.5f + side * 0.5f), floor, 0));
        GridFacade.Box(b, f, x - hw, x + hw, band0, band1, 0, 0.2f, L.LightTrim, 0);
        b.Place(PartCatalog.CurtainWall, new WindowParams(2 * hw, lintel - band1, 0.12f, 4, 2), f.AtReversed(x, band1, 0));
        var flight = Flight(2 * hw);
        b.Place(PartCatalog.Steps, flight, f.AtReversed(x, y, d + flight.Depth * 0.5f));
    }

    private static void PortalCollide(SiteColliders c, PlanFrame f, float x, float y)
    {
        float hw = R.PortalHalfWidth, j = R.PortalJamb, d = R.PortalDepth, top = y + PortalTop;
        foreach (var s in new[] { -1f, 1f })
            c.Box(f.At(x + s * (hw + j * 0.5f), (y + top) * 0.5f, (d - 0.3f) * 0.5f), new(j * 0.5f, (top - y) * 0.5f, (d + 0.3f) * 0.5f), SurfaceTag.Wall, FootMat.StonePaving);
        c.Box(f.At(x, y + R.FloorLevel * 0.5f, d * 0.5f), new(hw, R.FloorLevel * 0.5f, d * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        c.Wall(f.P(x - hw, 0.1f), f.P(x + hw, 0.1f), 0.15f, y, top, SurfaceTag.DoorClosed, FootMat.Glass);
        var flight = Flight(2 * hw);
        c.Add(Steps.Collision(flight), f.AtReversed(x, y, d + flight.Depth * 0.5f));
    }

    // ---- Lens hall ----

    private static float HallHeight(Plan plan) => plan.HallStoreys * R.HallStorey + R.HallAttic;

    private static SweepProfile Section(float in0, float out1, float height) =>
        new([new(in0, 0), new(out1, 0), new(out1, height), new(in0, height)]);

    private void Hall(SiteBuilder b, Plan plan, float y)
    {
        var ring = plan.Hall;
        var h = HallHeight(plan);
        var glassTop = y + h - R.HallAttic;
        var coarse = b.Lod is LodLevel.L3 or LodLevel.L4;
        b.Add(PartGeometryKernel.Extrude(new PolygonRings(ring), glassTop - y + 0.1f, coarse ? L.LightTrim : ZoneLooks.Glass, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Extrude(new PolygonRings(ring), 0.3f, L.RoofSlab, y + h - 0.3f), Matrix4x4.Identity);
        Band(b, ring, Section(-0.4f, 0.08f, R.FloorLevel + 0.1f), L.DarkGranite, y - 0.1f);
        for (var k = 1; k < plan.HallStoreys; k++)
            Band(b, ring, Section(-0.4f, 0.16f, 0.5f), L.LightTrim, y + R.FloorLevel + k * R.HallStorey - 0.25f);
        Band(b, ring, Section(-0.4f, 0.12f, R.HallAttic), L.Wall, glassTop);
        if (b.Detail != SiteDetail.Mass) Band(b, ring, Section(-0.5f, 0.22f, 0.14f), L.Stone, y + h);
        var door = plan.Door is null ? HallApex(plan) : ((Vector2, Vector2)?)null;
        if (b.Detail == SiteDetail.Full) Mullions(b, plan, ring, y + R.FloorLevel, glassTop, door);
        if (door is { } dd) HallDoor(b, dd.Item1, dd.Item2, y);
    }

    private static void Band(SiteBuilder b, Vector2[] ring, SweepProfile section, ToonSpec material, float baseY) =>
        b.Add(PartGeometryKernel.Sweep(section, ring, material, closed: true), Matrix4x4.CreateTranslation(0, baseY, 0));

    /// <summary>Hall edges that are not against a block, as (start, end) with the outside on the right.</summary>
    private static IEnumerable<(Vector2 A, Vector2 B)> HallFaces(Plan plan, Vector2[] ring)
    {
        for (var i = 0; i < ring.Length; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Length];
            var t = Vector2.Normalize(b - a);
            var outward = new Vector2(-t.Y, t.X) * -1f;
            if (plan.AllBlocks.Any(r => PlanGeometry.InsideByCrossings(r, (a + b) * 0.5f + outward * 0.6f))) continue;
            yield return (a, b);
        }
    }

    /// <summary>Slim aluminium mullions every ≤ 1.5 m on the free faces, a transom per storey; clear of the door.</summary>
    private void Mullions(SiteBuilder b, Plan plan, Vector2[] ring, float y0, float y1, (Vector2 Apex, Vector2 Out)? door)
    {
        foreach (var (a, e) in HallFaces(plan, ring))
        {
            var length = Vector2.Distance(a, e);
            var f = PlanFrame.Toward(a, e);
            // PlanFrame's +Z is the left normal; the outside is on the right, so the mullions sit at local z < 0.
            var n = Math.Max(1, (int)MathF.Ceiling(length / 1.5f));
            for (var i = 0; i < n; i++)
            {
                var x = length * i / n;
                if (door is { } d && Vector2.Distance(f.P(x, 0), d.Apex) < R.HallDoorWidth * 0.5f + 0.4f) continue;
                b.Add(PartGeometryKernel.BeveledBox(0.07f, y1 - y0, 0.12f, 0, L.Metal), f.At(x, y0, -0.06f));
            }
            for (var k = 0; k < plan.HallStoreys; k++)
                b.Add(PartGeometryKernel.BeveledBox(length, 0.07f, 0.1f, 0, L.Metal), f.At(length * 0.5f, y0 + k * R.HallStorey + 3.0f, -0.05f));
        }
    }

    /// <summary>The hall's frame at its door: X along the arc tangent, +Z outward.</summary>
    private static PlanFrame DoorFrame(Vector2 apex, Vector2 outward) => new(apex, new(outward.Y, -outward.X));

    private static void HallDoor(SiteBuilder b, Vector2 apex, Vector2 outward, float y)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var f = DoorFrame(apex, outward);
        var floor = y + R.FloorLevel;
        var width = R.HallDoorWidth + 1.6f;
        b.Place(PartCatalog.Door, new DoorParams(R.HallDoorWidth, R.HallDoorHeight, 0.14f, 0, 2) { Foot = FootMat.Glass }, f.AtReversed(0, floor, 0.04f));
        b.Place(PartCatalog.Canopy, new RoofParams(width, 0.25f, R.HallCanopyDepth) { Variation = 1 }, f.AtReversed(0, floor + R.HallDoorHeight + 0.35f, R.HallCanopyDepth * 0.5f));
        var flight = Flight(width);
        b.Place(PartCatalog.Steps, flight, f.AtReversed(0, y, 0.12f + flight.Depth * 0.5f));
    }

    // ---- Roof: stair penthouses and plant enclosures ----

    private static void Penthouse(SiteBuilder b, Vector2 at, float roof)
    {
        var s = R.StairSize;
        var h = R.StairHeight;
        var bevel = b.Detail == SiteDetail.Full ? 0.035f : 0f;
        b.Add(PartGeometryKernel.BeveledBox(s.X, h + 0.05f, s.Y, bevel, L.Stone), Frame.At(at.X, roof - 0.05f, at.Y));
        b.Add(PartGeometryKernel.BeveledBox(s.X + 0.3f, 0.18f, s.Y + 0.3f, bevel * 0.5f, L.LightTrim), Frame.At(at.X, roof + h, at.Y));
        if (b.Detail == SiteDetail.Mass) return;
        // A shut steel door on the south face; the roof stays closed (MAP-QSH-CENTER-EAST 楼顶).
        b.Place(PartCatalog.DoorFire, new DoorParams(1.0f, 2.1f, 0.12f), Frame.AtReversed(at.X - s.X * 0.25f, roof, at.Y + s.Y * 0.5f));
    }

    private static void Plant(SiteBuilder b, Vector2 at, float roof)
    {
        if (b.Detail == SiteDetail.Mass) return;
        float hx = R.PlantSize.X * 0.5f, hz = R.PlantSize.Y * 0.5f;
        foreach (var s in new[] { -1f, 1f })
        {
            b.Place(PartCatalog.EquipmentFence, new RailingParams(R.PlantSize.X, R.PlantFence, 0.06f, 1.4f) { Variation = s > 0 ? 1 : 0 }, Frame.At(at.X, roof, at.Y + s * hz));
            b.Place(PartCatalog.EquipmentFence, new RailingParams(R.PlantSize.Y, R.PlantFence, 0.06f, 1.4f), Frame.AtTurned(at.X + s * hx, roof, at.Y));
        }
        foreach (var dx in new[] { -2.4f, 0f, 2.4f })
            b.Place(PartCatalog.HeatPump, new EquipmentParams(1.6f, 1.9f, 0.9f), Frame.At(at.X + dx, roof, at.Y));
    }

    // ---- Collision ----

    public override void Collide(SiteColliders c)
    {
        var plan = Footprints(c.Context.World);
        var y = BaseY(c.Context);
        foreach (var ring in plan.Blocks)
        {
            if (!c.Includes(SiteRanges.Bounds(ring, BlockPadding))) continue;
            GridFacade.Collide(c, ring, y, Style, e => Dress(e, plan), FootMat.StonePaving);
            foreach (var (e, _, _) in GridFacade.Edges(ring))
                if (DoorOn(e, plan) is { } x) PortalCollide(c, e.Frame, x, y);
        }
        // Hall faces at the glass, 0.15 m inside it; the two arc segments meeting at the apex are the shut doors.
        if (!c.Includes(HallRange(c.Context.World))) return;
        var door = plan.Door is null ? HallApex(plan) : ((Vector2, Vector2)?)null;
        foreach (var (a, e) in HallFaces(plan, plan.Hall))
        {
            var atDoor = door is { } dd && MathF.Min(Vector2.Distance(a, dd.Item1), Vector2.Distance(e, dd.Item1)) < 0.01f;
            var t = Vector2.Normalize(e - a);
            var inward = new Vector2(-t.Y, t.X) * 0.15f;
            c.Wall(a + inward, e + inward, 0.15f, y, y + HallHeight(plan), atDoor ? SurfaceTag.DoorClosed : SurfaceTag.Wall, FootMat.Glass);
        }
        if (door is not { } d) return;
        var f = DoorFrame(d.Item1, d.Item2);
        var flight = Flight(R.HallDoorWidth + 1.6f);
        c.Add(Steps.Collision(flight), f.AtReversed(0, y, 0.12f + flight.Depth * 0.5f));
    }
}
