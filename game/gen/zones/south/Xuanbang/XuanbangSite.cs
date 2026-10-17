using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.South.Framework;
using X = Campus.Gen.Zones.South.SouthTune.Xuanbang;
using L = Campus.Gen.Zones.South.SouthTune.Look;
using W = Campus.Gen.Zones.South.SouthTune.Windows;

namespace Campus.Gen.Zones.South.Xuanbang;

/// <summary>
/// Xuanbang (MAP-QSH-SOUTH §5), the tallest group on the skyline, on the east bank of the east lake. Its entity carries three
/// building parts of different heights and the clock tower cannot be a generic extrusion, so this site claims the whole
/// entity and builds all three parts (contract request for per-part control in the 18b report):
/// - the 8-storey main wing and south annex (35.7 m) as a gridded beige-stone block, with giant pilasters and round-headed
///   ground-floor windows on the lake fronts, the pediment portico on the main front, small gables on the two projecting
///   end blocks and the atrium skylight on the roof;
/// - the 18-storey tower (73.85 m) as a stone frame with vertical ribs over blue-grey glass;
/// - the clock tower (66.65 m, <see cref="ClockTower"/>).
/// Footprints are read from the world model; proportions live in SouthTune.Xuanbang.
/// </summary>
public sealed class XuanbangSite : SouthSite
{
    public const string Feature = "qsh:way:844548980";
    public const string MainPart = "osm:way:844548980", TowerPart = "osm:way:987943106", ClockPart = "osm:way:1157144625";
    /// <summary>
    /// Window grids (contract-29): main wing with the south annex, tower, clock-tower slits and portals, and the portico's back
    /// wall (the hall glass behind the colonnade, which the main grid skips under its cut).
    /// </summary>
    public const string MainKey = "main", TowerKey = "tower", ShaftKey = "clock-shaft", PortalKey = "clock-portals", PorticoKey = "portico";

    /// <summary>Main wing and annex: 5.0 m ground storey, seven of 4.2 m, cornice and parapet to 35.7 m.</summary>
    internal static readonly GridStyle MainStyle = new(L.Beige, L.BeigeTrim, L.Granite, L.Glass, L.DarkFrame, L.RoofSlab)
    {
        Bay = X.MainBay,
        Pier = X.MainPier,
        GroundStorey = X.MainGround,
        Storey = X.MainStorey,
        Storeys = 8,
        GroundSill = 1.3f,
        GroundWindow = 3.2f,
        Sill = 0.9f,
        Window = 2.5f,
        PilasterEvery = 2,
        ArchedGround = true,
    };

    /// <summary>Tower: close piers and a rib in every window, thin spandrels, a tall parapet to 73.85 m.</summary>
    internal static readonly GridStyle TowerStyle = new(L.Beige, L.BeigeTrim, L.Granite, L.Glass, L.DarkFrame, L.RoofSlab)
    {
        Bay = X.TowerBay,
        Pier = X.TowerPier,
        PierDepth = 0.6f,
        BandDepth = 0.3f,
        GroundStorey = X.TowerGround,
        Storey = X.TowerStorey,
        Storeys = 18,
        GroundSill = 0.8f,
        GroundWindow = 3.6f,
        Sill = 0.7f,
        Window = 2.7f,
        PlinthHeight = 0.8f,
        Cornice = 0.9f,
        CorniceProjection = 0.5f,
        Parapet = X.TowerParapet,
        Ribs = true,
    };

    private static readonly StepsPart Steps = new();

    public override string Id => "18b.xuanbang";

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(Feature), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    internal sealed record Footprints(IReadOnlyList<Vector2> Main, IReadOnlyList<Vector2> Tower, IReadOnlyList<Vector2> Clock);

    internal static Footprints Rings(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(Feature), out var feature))
            throw new InvalidOperationException($"Xuanbang {Feature} is missing from the world model.");
        IReadOnlyList<Vector2> Ring(string source) => feature.Parts.FirstOrDefault(p => p.SourceId == source) is { } src && PlanGeometry.FacadeGeometryOf(src) is PolygonGeometry g
            ? g.Rings.Outer
            : throw new InvalidOperationException($"Xuanbang part {source} is missing or not a single polygon.");
        return new(Ring(MainPart), Ring(TowerPart), Ring(ClockPart));
    }

    /// <summary>Height of the portico's entablature top above the ground: the main front's grid is cut below it.</summary>
    internal static float PorticoTop => X.PodiumRise + X.ColumnHeight + X.EntablatureHeight;

    internal static GridEdge MainEdge(GridEdgeInfo e, Footprints f)
    {
        var probe = (e.A + e.B) * 0.5f + e.Frame.W;
        if (PlanMath.Inside(f.Tower, probe) || PlanMath.Inside(f.Clock, probe)) return new(EdgeKind.Hidden);
        if (e.Length < 4f) return new(EdgeKind.Blank);
        if (MathF.Abs(PlanMath.Turn(e.Facing - X.FrontFacing)) > 20f) return new(EdgeKind.Plain);
        if (e.Length < X.MainFrontMin) return new(EdgeKind.Front);
        var c = e.Length * 0.5f;
        return new(EdgeKind.Front) { Cuts = [(c - X.PorticoHalfWidth, c + X.PorticoHalfWidth, PorticoTop)] };
    }

    private static GridNumbering MainNumbering => new(new(Feature), MainKey, 0, W.Research, W.Lab);
    private static GridNumbering TowerNumbering => new(new(Feature), TowerKey, 0, W.Research, W.Lab);

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        var f = Rings(world);
        var main = GridFacade.Layout(f.Main, MainStyle, e => MainEdge(e, f));
        var tower = GridFacade.Layout(f.Tower, TowerStyle, _ => new(EdgeKind.Plain));
        FeatureId id = new(Feature);
        var grids = new List<(FeatureId, WindowGridSpec)>
        {
            (id, new(MainKey, main.Columns, main.Rows)), (id, new(TowerKey, tower.Columns, tower.Rows)),
            (id, new(ShaftKey, ClockTower.ShaftColumns, X.SlitRows)), (id, new(PortalKey, ClockTower.PortalColumns, 1)),
        };
        var porticoes = Porticoes(f).Count;
        if (porticoes > 0) grids.Add((id, new(PorticoKey, PorticoColumns * porticoes, 2)));
        return grids;
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var f = Rings(ctx.World);
        float Ground(Vector2 p) => ctx.World.Terrain.HeightAt(p.X, p.Y);
        var y0 = Ground(PlanMath.Centroid(f.Main));
        GridFacade.Declare(facades, f.Main, y0, MainStyle, e => MainEdge(e, f), MainNumbering,
            WindowNumbers.Required(ctx, MainNumbering.Feature, MainKey));
        GridFacade.Declare(facades, f.Tower, Ground(PlanMath.Centroid(f.Tower)), TowerStyle, _ => new(EdgeKind.Plain), TowerNumbering,
            WindowNumbers.Required(ctx, TowerNumbering.Feature, TowerKey));
        var y = Ground(PlanMath.Centroid(f.Clock));
        WindowNumbers.Declare(windows, ClockTower.Slits(f.Clock, y), ClockAllocation(ctx, ShaftKey, true)!.Value, W.Research);
        WindowNumbers.Declare(windows, ClockTower.Portals(f.Clock, y), ClockAllocation(ctx, PortalKey, true)!.Value, W.Research);
        var porticoes = Porticoes(f);
        if (porticoes.Count == 0) return;
        var hall = PorticoAllocation(ctx, porticoes.Count, true)!.Value;
        for (var i = 0; i < porticoes.Count; i++) WindowNumbers.Declare(windows, PorticoOpenings(porticoes[i], y0 + X.PodiumRise, i), hall, W.Hall);
    }

    /// <summary>Three doors below, three arched windows above, per portico.</summary>
    private const int PorticoColumns = 3;

    /// <summary>The pediment porticoes, one centred on every main front of the main wing (the layout's one source for them).</summary>
    internal static IReadOnlyList<PlanFrame> Porticoes(Footprints f) =>
    [
        .. GridFacade.Edges(f.Main).Select(e => e.Info)
            .Where(e => MainEdge(e, f).Kind == EdgeKind.Front && e.Length >= X.MainFrontMin)
            .Select(e => e.Frame.Shift(e.Length * 0.5f, 0)),
    ];

    /// <summary>
    /// The glass of one portico's back wall in its grid cells: row 0 the left closed door, the open middle door's fanlight and the
    /// right closed door; row 1 the three arched windows above. Each pane is the opening's bounding rectangle on the glass plane;
    /// the wall around the arch hides its corners.
    /// </summary>
    internal static GlazedOpening[] PorticoOpenings(PlanFrame p, float floor, int portico)
    {
        var outward = new Vector3(p.W.X, 0, p.W.Y);
        GlazedOpening At(int column, int row, float x, float sill, float width, float height)
        {
            var q = p.P(x, PorticoGlassZ);
            return new(portico * PorticoColumns + column, row, new(q.X, floor + sill, q.Y), outward, width, height);
        }
        var doors = X.Doors.Select((x, i) => MathF.Abs(x) < 0.01f
            ? At(i, 0, x, X.DoorSpring - 0.01f, X.DoorWidth, X.DoorWidth * 0.5f + 0.01f)
            : At(i, 0, x, 0, X.DoorWidth, X.DoorSpring + X.DoorWidth * 0.5f));
        var windows = X.Doors.Select((x, i) => At(i, 1, x, X.UpperWindowSill, X.UpperWindowWidth, X.UpperWindowSpring + X.UpperWindowWidth * 0.5f));
        return [.. doors, .. windows];
    }

    /// <summary>The glass plane in the portico frame: just proud of the block's glass core (the facade line), 0.45 m reveals.</summary>
    private const float PorticoGlassZ = 0.05f;

    /// <summary>The portico grid's allocation (null without a snapshot), checked against the porticoes' openings.</summary>
    private static WindowAllocation? PorticoAllocation(BuildContext ctx, int porticoes, bool required = false)
    {
        var feature = new FeatureId(Feature);
        var allocation = required ? WindowNumbers.Required(ctx, feature, PorticoKey) : WindowNumbers.Allocation(ctx, feature, PorticoKey);
        if (allocation is { } a) WindowNumbers.Fit(PorticoColumns * porticoes, 2, feature, PorticoKey, a);
        return allocation;
    }

    /// <summary>A clock-tower grid's allocation (null without a snapshot), checked against the tower's openings.</summary>
    internal static WindowAllocation? ClockAllocation(BuildContext ctx, string key, bool required = false)
    {
        var feature = new FeatureId(Feature);
        var allocation = required ? WindowNumbers.Required(ctx, feature, key) : WindowNumbers.Allocation(ctx, feature, key);
        var (columns, rows) = key == ShaftKey ? (ClockTower.ShaftColumns, X.SlitRows) : (ClockTower.PortalColumns, 1);
        if (allocation is { } a) WindowNumbers.Fit(columns, rows, feature, key, a);
        return allocation;
    }

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
    {
        var f = Rings(world);
        var (clock, hx, hz) = ClockTower.FrameOf(f.Clock);
        return [new(f.Main), new(f.Tower), new(clock.Rect(-hx, -hz, hx, hz)),
            .. Porticoes(f).Select(p => new PolygonRings(p.Rect(-X.PorticoHalfWidth - 0.5f, -0.3f,
                X.PorticoHalfWidth + 0.5f, X.PorticoDepth + 0.3f)))];
    }

    public override void Build(SiteBuilder b)
    {
        var f = Rings(b.Context.World);
        var y = b.Ground(PlanMath.Centroid(f.Main));
        GridFacade.Build(b, f.Main, y, MainStyle, e => MainEdge(e, f), MainNumbering);
        foreach (var (e, _, _) in GridFacade.Edges(f.Main))
            if (MainEdge(e, f).Kind == EdgeKind.Front && e.Length is > X.EndBlockMin and < X.EndBlockMax) EndGable(b, e, y);
        var porticoes = Porticoes(f);
        var hall = porticoes.Count > 0 ? PorticoAllocation(b.Context, porticoes.Count) : null;
        for (var i = 0; i < porticoes.Count; i++) Portico(b, porticoes[i], y, i, hall);
        Skylight(b, y);
        GridFacade.Build(b, f.Tower, b.Ground(PlanMath.Centroid(f.Tower)), TowerStyle, _ => new(EdgeKind.Plain), TowerNumbering);
        ClockTower.Build(b, f.Clock, b.Ground(PlanMath.Centroid(f.Clock)));
    }

    // ---- Pediment portico (§5.6): four round columns, entablature with dentils, pediment with the name panel ----

    private static void Portico(SiteBuilder b, PlanFrame p, float y, int index, WindowAllocation? hall)
    {
        float hw = X.PorticoHalfWidth, d = X.PorticoDepth, floor = y + X.PodiumRise;
        var top = floor + X.ColumnHeight;
        // Podium from the facade out past the colonnade, three steps down to the lakeside walk (no per-riser outline).
        b.Add(PartGeometryKernel.BeveledBox(2 * hw + 1f, X.PodiumRise + 0.1f, d + 0.6f, 0.03f, L.Granite), p.At(0, y - 0.1f, d * 0.5f));
        var flight = Flight();
        b.Add(Steps.Lods[b.Lod](flight), p.AtReversed(0, y, d + 0.3f + flight.Depth * 0.5f));
        foreach (var x in X.PorticoColumns) Column(b, p, x, d - 1f, floor);
        // Entablature: architrave, frieze, cornice; dentils under the cornice.
        b.Add(PartGeometryKernel.BeveledBox(2 * hw, 0.8f, d + 0.3f, 0.03f, L.Beige), p.At(0, top, (d - 0.3f) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(2 * hw, 0.7f, d + 0.15f, 0.02f, L.Beige), p.At(0, top + 0.8f, (d - 0.45f) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(2 * hw + 0.6f, 0.45f, d + 0.6f, 0.03f, L.BeigeTrim), p.At(0, top + 1.5f, d * 0.5f));
        if (b.Detail == SiteDetail.Full)
            for (var x = -hw + 0.35f; x <= hw - 0.3f; x += 0.5f)
                b.Add(PartGeometryKernel.BeveledBox(0.22f, 0.24f, 0.2f, 0, L.BeigeTrim), p.At(x, top + 1.26f, d - 0.05f));
        // Pediment: the tympanum sits 0.2 m behind the architrave face inside a proud raking frame.
        var pb = top + X.EntablatureHeight;
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings([new(-hw - 0.3f, 0), new(hw + 0.3f, 0), new(0, X.PedimentRise)]), d - 0.2f + 0.3f, L.Beige),
            p.At(0, pb, (d - 0.2f - 0.3f) * 0.5f));
        if (b.Detail != SiteDetail.Mass)
        {
            var outer = new Vector2[] { new(-hw - 0.6f, -0.02f), new(hw + 0.6f, -0.02f), new(0, X.PedimentRise + 0.45f) };
            // The inner edge runs parallel to the outer one, so the raking members keep one thickness up to the apex.
            var slope = (X.PedimentRise + 0.47f) / (hw + 0.6f);
            var inner = new Vector2[] { new(-hw + 1.1f, 0.4f), new(hw - 1.1f, 0.4f), new(0, 0.4f + slope * (hw - 1.1f)) };
            b.Add(PartGeometryKernel.WallPanel(new PolygonRings(outer, [inner]), 0.6f, L.BeigeTrim), p.At(0, pb, d - 0.1f));
            // Name panel in the tympanum: "宣邦" over "Xuanbang"; the glyphs wait for the scene-text atlas (contract-26).
            b.Add(PartGeometryKernel.BeveledBox(2.8f, 1.3f, 0.12f, 0.02f, L.BeigeTrim), p.At(0, pb + 1.1f, d - 0.2f + 0.06f));
        }
        BackWall(b, p, floor, index, hall);
    }

    internal static StairParams Flight() =>
        new(2 * X.PorticoHalfWidth - 4f, X.PodiumRise, 3 * 0.4f, 3) { Variation = 2, Material = L.StepStone, Foot = FootMat.StonePaving };

    private static void Column(SiteBuilder b, PlanFrame p, float x, float z, float floor)
    {
        var r = X.ColumnRadius;
        var segments = b.Detail switch { SiteDetail.Full => 24, SiteDetail.Plain => 16, _ => 10 };
        b.Add(PartGeometryKernel.BeveledBox(2 * r + 0.5f, 0.35f, 2 * r + 0.5f, 0.03f, L.BeigeTrim), p.At(x, floor, z));
        b.Add(Solids.Lathe([new(r + 0.16f, 0), new(r + 0.16f, 0.12f), new(r + 0.05f, 0.3f), new(r, 0.36f)], segments, L.BeigeTrim), p.At(x, floor + 0.35f, z));
        var shaft = X.ColumnHeight - 0.35f - 0.36f - 0.9f;
        b.Add(Shapes.Shaft(r, r * 0.86f, shaft, segments, L.Beige), p.At(x, floor + 0.71f, z));
        var capital = floor + 0.71f + shaft;
        b.Add(Solids.Lathe([new(r * 0.86f, 0), new(r * 0.95f, 0.15f), new(r + 0.22f, 0.45f), new(r + 0.22f, 0.5f)], segments, L.BeigeTrim), p.At(x, capital, z));
        b.Add(PartGeometryKernel.BeveledBox(2 * r + 0.6f, 0.4f, 2 * r + 0.6f, 0.03f, L.BeigeTrim), p.At(x, capital + 0.5f, z));
    }

    /// <summary>
    /// The facade behind the portico: three round-arched doors (the middle one open, WLD-INTERIORS §2.4) with arched windows
    /// above, responds behind the columns. The wall stands on the podium from the facade line (z −0.3…0.5). Its glass is the
    /// numbered hall glass once the map's window numbering is in the context, plain arched glass without one.
    /// </summary>
    private static void BackWall(SiteBuilder b, PlanFrame p, float floor, int index, WindowAllocation? hall)
    {
        var hw = X.PorticoHalfWidth;
        var h = X.ColumnHeight;
        var segments = b.Detail == SiteDetail.Full ? 14 : 8;
        var outer = new List<Vector2> { new(-hw, 0) };
        foreach (var x in X.Doors)
        {
            var door = Shapes.RoundArch(x, 0, X.DoorWidth, X.DoorSpring, segments);
            outer.Add(door[0]);
            for (var i = door.Length - 1; i >= 2; i--) outer.Add(door[i]);
            outer.Add(door[1]);
        }
        outer.AddRange([new(hw, 0), new(hw, h), new(-hw, h)]);
        var windows = X.Doors.Select(x => (IReadOnlyList<Vector2>)Shapes.RoundArch(x, X.UpperWindowSill, X.UpperWindowWidth, X.UpperWindowSpring, segments)).ToArray();
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(outer, windows), 0.8f, L.Beige), p.At(0, floor, 0.1f));
        foreach (var x in X.PorticoColumns)
            b.Add(PartGeometryKernel.BeveledBox(1.3f, h - 0.05f, 0.25f, 0.02f, L.Beige), p.At(x, floor, 0.62f));
        if (b.Detail == SiteDetail.Mass) return;
        // Glass just proud of the block's glass core (the facade line), frames in front of it: 0.45 m reveals.
        const float glass = PorticoGlassZ, frame = 0.12f;
        var plain = hall is null;
        if (hall is { } allocation) WindowNumbers.Panes(b, PorticoOpenings(p, floor, index), allocation, W.Hall, W.Corridor);
        foreach (var x in X.Doors)
        {
            if (plain)
                b.Add(PartGeometryKernel.WallPanel(new PolygonRings(Shapes.RoundArch(x, X.UpperWindowSill, X.UpperWindowWidth, X.UpperWindowSpring, segments)), 0.05f, L.Glass), p.At(0, floor, glass));
            b.Add(PartGeometryKernel.BeveledBox(X.UpperWindowWidth + 0.3f, 0.12f, 0.3f, 0.02f, L.BeigeTrim), p.At(x, floor + X.UpperWindowSill - 0.12f, 0.6f));
            var open = MathF.Abs(x) < 0.01f;
            if (!open)
            {
                // Closed side doors: glazed, a stile and a transom bar at the springing.
                if (plain)
                    b.Add(PartGeometryKernel.WallPanel(new PolygonRings(Shapes.RoundArch(x, 0, X.DoorWidth, X.DoorSpring, segments)), 0.05f, L.Glass), p.At(0, floor, glass));
                b.Add(PartGeometryKernel.BeveledBox(0.1f, X.DoorSpring, 0.1f, 0, L.DarkFrame), p.At(x, floor, frame));
                b.Add(PartGeometryKernel.BeveledBox(X.DoorWidth, 0.1f, 0.1f, 0, L.DarkFrame), p.At(x, floor + X.DoorSpring - 0.1f, frame));
                continue;
            }
            // Open middle door: fanlight glass above the springing, leaves swung out against the reveals (SYS-WALK §8).
            var fan = Shapes.RoundArch(x, X.DoorSpring - 0.01f, X.DoorWidth, 0.01f, segments);
            if (plain) b.Add(PartGeometryKernel.WallPanel(new PolygonRings(fan), 0.05f, L.Glass), p.At(0, floor, glass));
            b.Add(PartGeometryKernel.BeveledBox(X.DoorWidth, 0.12f, 0.12f, 0, L.DarkFrame), p.At(x, floor + X.DoorSpring - 0.06f, frame));
            foreach (var side in new[] { -1f, 1f })
                b.Add(PartGeometryKernel.BeveledBox(0.06f, X.DoorSpring - 0.1f, 1.5f, 0.012f, L.DarkFrame), p.At(x + side * (X.DoorWidth * 0.5f - 0.05f), floor, 0.55f));
            // The mat, open sign and door lamp are WP-13's, placed on the entrance anchor (WLD-INTERIORS §0).
        }
    }

    // ---- Small gables on the projecting end blocks (§5.7) ----

    private static void EndGable(SiteBuilder b, GridEdgeInfo e, float y)
    {
        var g = e.Frame.Shift(e.Length * 0.5f, 0);
        var s = MainStyle;
        var baseY = y + s.WallTop + s.Cornice;
        var half = e.Length * 0.5f + 0.2f;
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings([new(-half, 0), new(half, 0), new(0, X.EndGableRise)]), 0.9f, L.Beige), g.At(0, baseY, s.PierDepth - 0.45f));
        if (b.Detail == SiteDetail.Mass) return;
        var outer = new Vector2[] { new(-half - 0.3f, -0.02f), new(half + 0.3f, -0.02f), new(0, X.EndGableRise + 0.32f) };
        var inner = new Vector2[] { new(-half + 0.75f, 0.3f), new(half - 0.75f, 0.3f), new(0, X.EndGableRise - 0.22f) };
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(outer, [inner]), 0.3f, L.BeigeTrim), g.At(0, baseY, s.PierDepth + 0.05f));
    }

    // ---- Atrium skylight on the main wing (WLD-INTERIORS §16) ----

    private static void Skylight(SiteBuilder b, float y)
    {
        var f = PlanFrame.Along(X.Skylight, X.SlabAxis);
        var roof = y + MainStyle.WallTop + 0.35f;
        var size = X.SkylightSize;
        b.Add(PartGeometryKernel.BeveledBox(size + 0.6f, 0.6f, size + 0.6f, 0.03f, L.BeigeTrim), f.At(0, roof, 0));
        b.Add(Shapes.Hip(size, size, X.SkylightRise, 3f, b.Lod is LodLevel.L3 or LodLevel.L4 ? L.RoofSlab : L.Glass), f.At(0, roof + 0.6f, 0));
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.BeveledBox(3.2f, 0.16f, 0.3f, 0.02f, L.DarkFrame), f.At(0, roof + 0.6f + X.SkylightRise - 0.04f, 0));
    }

    // ---- Collision ----

    public override void Collide(SiteColliders c)
    {
        var f = Rings(c.Context.World);
        var centre = PlanMath.Centroid(f.Main);
        var y = c.Ground(centre.X, centre.Y);
        GridFacade.Collide(c, f.Main, y, MainStyle, e => MainEdge(e, f), FootMat.StonePaving);
        var t = PlanMath.Centroid(f.Tower);
        GridFacade.Collide(c, f.Tower, c.Ground(t.X, t.Y), TowerStyle, _ => new(EdgeKind.Plain), FootMat.StonePaving);
        foreach (var p in Porticoes(f)) PorticoCollide(c, p, y);
        var k = PlanMath.Centroid(f.Clock);
        ClockTower.Collide(c, f.Clock, c.Ground(k.X, k.Y));
    }

    private static void PorticoCollide(SiteColliders c, PlanFrame p, float y)
    {
        float hw = X.PorticoHalfWidth, d = X.PorticoDepth, floor = y + X.PodiumRise;
        c.Box(p.At(0, floor - 0.15f, d * 0.5f), new(hw + 0.5f, 0.15f, d * 0.5f + 0.3f), SurfaceTag.Ground, FootMat.StonePaving);
        var flight = Flight();
        c.Add(Steps.Collision(flight), p.AtReversed(0, y, d + 0.3f + flight.Depth * 0.5f));
        foreach (var x in X.PorticoColumns)
            c.Add(new PostCollider(p.P(x, d - 1f), X.ColumnRadius + 0.1f, floor, floor + X.ColumnHeight, SurfaceTag.Wall) { Foot = FootMat.StonePaving });
        // Back wall: solid between the doors, the side doors closed, the middle doorway open into WP-31a's atrium.
        var edges = new List<float> { -hw };
        foreach (var x in X.Doors) { edges.Add(x - X.DoorWidth * 0.5f); edges.Add(x + X.DoorWidth * 0.5f); }
        edges.Add(hw);
        for (var i = 0; i + 1 < edges.Count; i++)
        {
            var door = i % 2 == 1;
            var mid = (edges[i] + edges[i + 1]) * 0.5f;
            if (door && MathF.Abs(mid) < 0.01f) continue;
            c.Wall(p.P(edges[i], 0.1f), p.P(edges[i + 1], 0.1f), 0.4f, floor, floor + X.ColumnHeight, door ? SurfaceTag.DoorClosed : SurfaceTag.Wall, FootMat.StonePaving);
        }
    }
}
