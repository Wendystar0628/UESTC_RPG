using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using Campus.Gen.Sites;
using K = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Wenke;
using L = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Look;
using W = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Windows;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Wenke;

/// <summary>
/// 文科楼群 (MAP-QSH-CENTER-EAST), claimed whole from WP-14 and built by this zone: the 15-storey tower on its
/// rounded-triangle plan in the east wing, three podium wings stepping back storey by storey toward their tips, and the
/// central hub carrying the glass-vaulted atrium. Each block is a stack of rounded storeys, one pure layout per block:
/// a white-grey floor band at every floor standing proud of a continuous dark glass ribbon cut into numbered cells by
/// mullions; set-backs are planted terraces behind bar rails; podium roofs are planted, the tower roof carries a penthouse
/// and fenced plant. Faces where another block stands at that storey are left out, so the group reads as one body.
/// The yellow-green panels are a strip on the tower's south face over the atrium door and the door's two-storey porch.
/// Closed (§3.3): the porch's glass doors are shut (DoorClosed), the lobby card is WP-51's; no hoarding or scaffold.
/// </summary>
public sealed class WenkeSite : ZoneSite
{

    public override string Id => "21c.wenke";

    private static Campus.Contracts.Maps.MapRect PorchRange(IWorldModel world) =>
        SiteRanges.Bounds(Door(world).Rect(-K.PorchHalfWidth - K.PorchJamb - 0.15f, -0.4f,
            K.PorchHalfWidth + K.PorchJamb + 0.15f, K.PorchDepth + 0.15f));

    private static Campus.Contracts.Maps.MapRect AtriumRange(IWorldModel world)
    {
        var (frame, half) = AtriumOf(world);
        return SiteRanges.Bounds(frame.Rect(-half.X, -half.Y, half.X, half.Y), K.AtriumCurb);
    }

    private static Campus.Contracts.Maps.MapRect BlockRange(IWorldModel world, string part) =>
        SiteRanges.Bounds(world.Feature(CenterEastIds.Wenke.Entity).Contour(part), K.BandOut * 2.5f + K.ParapetThickness);

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [.. K.Blocks.Select(block => BlockRange(world, block.Part)), PorchRange(world), AtriumRange(world)];

    public override IReadOnlyList<Claim> Claims => [new FeatureClaim(new(CenterEastIds.Wenke.Entity), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod) =>
        [.. Footprints(world).Blocks.SelectMany(block => block.Outlines).Select(outline => new PolygonRings(outline))];

    // ---- Layout: one pure plan for geometry, collision and the window declarations ----

    internal enum CellKind { Window, Hidden, Panel }

    /// <summary>One glazing cell of a storey: its column in the block's grid, its path on the glass line and what fills it.</summary>
    internal sealed record Cell(int Column, int Row, Vector2[] Path, CellKind Kind);

    /// <summary>A block: its grid key, its canonical rounded outline per storey (the glass line) and every storey's cells.</summary>
    internal sealed record Block(string Key, string Part, int Storeys, Vector2[][] Outlines, Cell[][] Rows)
    {
        public int Columns => Rows.Max(r => r.Length);
        public bool Windowed => Rows.Any(r => r.Any(c => c.Kind == CellKind.Window));
    }

    internal sealed record Plan(Block[] Blocks);

    /// <summary>Floor level of storey k above the pad (k = storeys is the roof).</summary>
    internal static float Floor(int k) => k == 0 ? 0f : K.GroundStorey + (k - 1) * K.Storey;

    internal static float GlassBottom(int k) => k == 0 ? K.Plinth : Floor(k) + K.Upstand;

    internal static float GlassTop(int k) => Floor(k + 1) - K.SlabBelow;

    internal static float BaseY(BuildContext ctx) => ctx.World.Terrain.PadY(new(CenterEastIds.Wenke.Entity));

    /// <summary>
    /// The door frame: origin at the door on the hub's south-east face, +Z out to the forecourt, +X toward the tower. The door's
    /// spot and facing are registered in the forecourt feature's props.
    /// </summary>
    internal static PlanFrame Door(IWorldModel world)
    {
        var props = world.Feature(CenterEastIds.Wenke.ForecourtId).RequiredProps();
        return PlanMath.Facing(props.Pair("door"), props.Number("doorFacing"));
    }

    internal static Plan Footprints(IWorldModel world) => Compute(world);

    internal static Plan Footprints(BuildContext context) =>
        context.Memo(typeof(Plan), () => Footprints(context.World));

    private static Plan Compute(IWorldModel world)
    {
        var feature = world.Feature(CenterEastIds.Wenke.Entity);
        IReadOnlyList<Vector2> Ring(string part) => feature.Contour(part);
        var layout = world.Feature(CenterEastIds.Wenke.AtriumRoofId);
        var setbacks = layout.RequiredProps().GetProperty("setbacks").EnumerateArray().ToArray();
        var panelStrip = layout.Pair("panelStrip");
        // Storeys per block: the zone's own terraced counts, and the tower's from its part's OSM levels.
        var counts = K.Blocks.Select(b => b.Storeys ?? world.Feature(CenterEastIds.Wenke.Entity).Part(b.Part).Levels().Storeys).ToArray();
        // Clipped (set-back) outlines first, then the rounding, which keeps sharp the corners where another block meets.
        var clipped = K.Blocks.Select((b, i) => Enumerable.Range(0, counts[i]).Select(k =>
        {
            var ring = PlanRings.CleanRing(Ring(b.Part), minEdge: 0.05f, collinearTolerance: 1e-4f) ?? throw new InvalidOperationException("Building part has no contour.");
            foreach (var s in setbacks)
                if (s.Text("part") == b.Part && k >= s.Number("from")) ring = Contour.Clip(ring, s.Pair("at"), s.Pair("toward"), s.Number("keep"));
            return ring;
        }).ToArray()).ToArray();
        var outlines = new Vector2[K.Blocks.Length][][];
        for (var i = 0; i < K.Blocks.Length; i++)
        {
            outlines[i] = new Vector2[counts[i]][];
            for (var k = 0; k < counts[i]; k++)
            {
                var others = Enumerable.Range(0, K.Blocks.Length).Where(j => j != i && k < counts[j]).Select(j => clipped[j][k]).ToArray();
                outlines[i][k] = Contour.Fillet(clipped[i][k], K.CornerRadius, K.MinTurn, q => others.Any(o => Near(o, q, 0.6f)));
            }
        }
        var blocks = new Block[K.Blocks.Length];
        for (var i = 0; i < K.Blocks.Length; i++)
        {
            var part = K.Blocks[i].Part;
            var storeys = counts[i];
            var rows = new Cell[storeys][];
            for (var k = 0; k < storeys; k++)
            {
                var others = Enumerable.Range(0, K.Blocks.Length).Where(j => j != i && k < counts[j]).Select(j => outlines[j][k]).ToArray();
                var cells = Contour.Cells(outlines[i][k], K.MullionPitch);
                rows[k] = [.. cells.Select((path, c) => new Cell(c, k, path, Kind(part, k, path, others, panelStrip)))];
            }
            blocks[i] = new(Key(part), part, storeys, outlines[i], rows);
        }
        return new(blocks);
    }

    private static string Key(string part) => part switch
    {
        var id when id == CenterEastIds.Wenke.Tower => "tower",
        var id when id == CenterEastIds.Wenke.Hub => "hub",
        var id when id == CenterEastIds.Wenke.South => "south",
        var id when id == CenterEastIds.Wenke.North => "north",
        var id when id == CenterEastIds.Wenke.West => "west",
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Not a 文科楼 part."),
    };

    private static CellKind Kind(string part, int k, Vector2[] path, Vector2[][] others, Vector2 panelStrip)
    {
        var (mid, outward) = Contour.Middle(path);
        if (others.Any(o => PlanRings.InsideByCrossings(o, mid + outward * 0.8f))) return CellKind.Hidden;
        if (part == CenterEastIds.Wenke.Tower && k >= K.PanelStripFrom && Vector2.Distance(mid, panelStrip) <= K.PanelStripHalf) return CellKind.Panel;
        return CellKind.Window;
    }

    private static bool Near(IReadOnlyList<Vector2> ring, Vector2 p, float distance) =>
        PlanRings.DistanceToRing(p, ring) < distance;

    // ---- Window grids (contract-29): one per block, columns = the most cells any storey has, rows = storeys ----

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        foreach (var block in Footprints(world).Blocks)
            if (block.Windowed) yield return (new(CenterEastIds.Wenke.Entity), new(block.Key, block.Columns, block.Storeys));
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var y = BaseY(ctx);
        foreach (var block in Footprints(ctx).Blocks.Where(b => b.Windowed))
        {
            var allocation = WindowNumbers.Required(ctx, new(CenterEastIds.Wenke.Entity), block.Key);
            WindowNumbers.Fit(block.Columns, block.Storeys, new(CenterEastIds.Wenke.Entity), block.Key, allocation);
            WindowNumbers.Declare(windows, Openings(block, y), allocation, W.Teaching);
        }
    }

    /// <summary>Each window cell as one opening across its chord (a curved cell's panes share its number).</summary>
    internal static IEnumerable<GlazedOpening> Openings(Block block, float y)
    {
        foreach (var row in block.Rows)
            foreach (var cell in row.Where(c => c.Kind == CellKind.Window))
            {
                Vector2 a = cell.Path[0], b = cell.Path[^1];
                var mid = (a + b) * 0.5f;
                var outward = PlanOffsets.RightUnitNormal(a, b);
                yield return new(cell.Column, cell.Row, new(mid.X, y + GlassBottom(cell.Row), mid.Y), new(outward.X, 0, outward.Y),
                    Vector2.Distance(a, b), GlassTop(cell.Row) - GlassBottom(cell.Row));
            }
    }

    /// <summary>Window card of a cell: classrooms with a share of offices, a fixed hash of its ID (WLD-INTERIORS).</summary>
    private static byte CardOf(int id) => (uint)id * 2654435761u % 10u < (uint)W.WenkeOfficeShare ? W.Office : W.Classroom;

    // ---- Build ----

    public override void Build(SiteBuilder b)
    {
        var plan = Footprints(b.Context);
        var y = BaseY(b.Context);
        foreach (var block in plan.Blocks.Where(block => b.Includes(BlockRange(b.Context.World, block.Part)))) BuildBlock(b, plan, block, y);
        if (b.Includes(PorchRange(b.Context.World))) Porch(b, Door(b.Context.World), y);
        if (b.Includes(AtriumRange(b.Context.World))) Atrium(b, AtriumOf(b.Context.World), y);
        if (b.Includes(BlockRange(b.Context.World, CenterEastIds.Wenke.Tower))) TowerRoof(b, plan.Blocks.Single(k => k.Part == CenterEastIds.Wenke.Tower), y);
    }

    private static void BuildBlock(SiteBuilder b, Plan plan, Block block, float y)
    {
        var detail = b.Detail;
        var chamfer = detail == SiteDetail.Full ? K.BandChamfer : 0f;
        var n = block.Storeys;
        var allocation = block.Windowed ? WindowNumbers.Allocation(b.Context, new(CenterEastIds.Wenke.Entity), block.Key) : null;
        if (allocation is { } fit) WindowNumbers.Fit(block.Columns, block.Storeys, new(CenterEastIds.Wenke.Entity), block.Key, fit);
        for (var k = 0; k < n; k++)
        {
            var outline = block.Outlines[k];
            // The band at floor k runs over the storey below (it is that storey's roof edge and this storey's sill).
            if (k == 0)
                b.Add(Contour.Slab(PlanOffsets.MitreOffset(outline, K.BandOut, closed: true, limit: 2.5f), y - 0.1f, y + K.Plinth, L.DarkGranite, chamfer), Matrix4x4.Identity);
            else
            {
                var below = block.Outlines[k - 1];
                b.Add(Contour.Slab(PlanOffsets.MitreOffset(below, K.BandOut, closed: true, limit: 2.5f), y + Floor(k) - K.SlabBelow, y + Floor(k) + K.Upstand, L.Wall, chamfer), Matrix4x4.Identity);
                if (Terraces(plan, block, k).Any())
                {
                    b.Add(Contour.Slab(PlanOffsets.MitreOffset(below, K.BandOut - K.TerraceRim, closed: true, limit: 2.5f), y + Floor(k) + K.Upstand, y + Floor(k) + K.Upstand + K.TerraceSoil, L.Bed), Matrix4x4.Identity);
                    if (detail != SiteDetail.Mass) Rails(b, plan, block, k, y);
                }
            }
            Glass(b, block, k, y, allocation);
            // A dark core just inside the glass line, so the seams where blocks meet never show through the shell.
            b.Add(Contour.Slab(PlanOffsets.MitreOffset(outline, -K.CoreInset, closed: true, limit: 2.5f), y + GlassBottom(k) - 0.05f, y + GlassTop(k) + 0.05f, L.DarkFrame, 0f, caps: false), Matrix4x4.Identity);
        }
        // Roof: the top band, a parapet ring on it, and the roof inside (planted on the podium, slab on the tower).
        var top = PlanOffsets.MitreOffset(block.Outlines[n - 1], K.BandOut, closed: true, limit: 2.5f);
        var roof = y + Floor(n);
        b.Add(Contour.Slab(top, roof - K.SlabBelow, roof + K.RoofBand, L.Wall, chamfer), Matrix4x4.Identity);
        if (b.Lod is not (LodLevel.L3 or LodLevel.L4))
            b.Add(Contour.Wall(top, K.ParapetThickness, roof + K.RoofBand, roof + K.Parapet, L.Wall), Matrix4x4.Identity);
        var inside = PlanOffsets.MitreOffset(block.Outlines[n - 1], K.BandOut - K.ParapetThickness - 0.02f, closed: true, limit: 2.5f);
        b.Add(Contour.Slab(inside, roof + K.RoofBand, roof + K.RoofBand + K.TerraceSoil, block.Part == CenterEastIds.Wenke.Tower ? L.RoofSlab : L.Bed), Matrix4x4.Identity);
    }

    /// <summary>Cells of storey k − 1 whose band edge is a terrace: open to the sky at storey k, and not against another block.</summary>
    internal static IEnumerable<Cell> Terraces(Plan plan, Block block, int k)
    {
        var above = block.Outlines[k];
        var others = plan.Blocks.Where(o => o != block && k < o.Storeys).Select(o => o.Outlines[k]).ToArray();
        foreach (var cell in block.Rows[k - 1])
        {
            if (cell.Kind == CellKind.Hidden) continue;
            var (mid, outward) = Contour.Middle(cell.Path);
            var inside = mid - outward * 1.2f;
            if (!PlanRings.InsideByCrossings(above, inside) && !others.Any(o => PlanRings.InsideByCrossings(o, inside))) yield return cell;
        }
    }

    /// <summary>Bar rails (WP-11) along each terrace cell, on the band just inside its face.</summary>
    private static void Rails(SiteBuilder b, Plan plan, Block block, int k, float y)
    {
        var level = y + Floor(k) + K.Upstand;
        foreach (var cell in Terraces(plan, block, k))
        {
            Vector2 a = cell.Path[0], e = cell.Path[^1];
            var outward = PlanOffsets.RightUnitNormal(a, e);
            var shift = outward * (K.BandOut - 0.15f);
            var length = Vector2.Distance(a, e);
            if (length < 0.3f) continue;
            var rail = new RailingParams(length, K.RailHeight, 0.06f, K.RailPosts) { Variation = 2 };
            b.Place(PartCatalog.Railing, rail, PlanFrame.Toward(a + shift, e + shift).At(length * 0.5f, level, 0));
        }
    }

    /// <summary>Storey k's ribbon: numbered glass (or plain glass without a numbering), yellow-green panels, and mullions at L0.</summary>
    private static void Glass(SiteBuilder b, Block block, int k, float y, WindowAllocation? allocation)
    {
        float y0 = y + GlassBottom(k), y1 = y + GlassTop(k), h = y1 - y0;
        var cells = block.Rows[k];
        foreach (var cell in cells)
        {
            switch (cell.Kind)
            {
                case CellKind.Hidden:
                    continue;
                case CellKind.Panel:
                    for (var i = 0; i + 1 < cell.Path.Length; i++)
                    {
                        Vector2 p = cell.Path[i], q = cell.Path[i + 1];
                        var length = Vector2.Distance(p, q);
                        if (length < 0.02f) continue;
                        b.Add(PartSolids.BeveledBox(length, h, 0.14f, 0, L.YellowGreen), PlanFrame.Toward(p, q).At(length * 0.5f, y0, 0.05f));
                    }
                    break;
                case CellKind.Window:
                    for (var i = 0; i + 1 < cell.Path.Length; i++)
                    {
                        Vector2 p = cell.Path[i], q = cell.Path[i + 1];
                        var length = Vector2.Distance(p, q);
                        if (length < 0.02f) continue;
                        var mid = (p + q) * 0.5f;
                        var outward = PlanOffsets.RightUnitNormal(p, q);
                        var at = OpeningPlacement.Frame(new(mid.X, y0, mid.Y), new(outward.X, 0, outward.Y));
                        if (allocation is { } numbers)
                        {
                            var id = numbers.IdAt(cell.Column, cell.Row);
                            b.Add(OpeningGlass.Grid(length, h, 1, 1, numbers.BaseId, cell.Column, cell.Row, numbers.Columns, W.Teaching, CardOf(id)), at);
                        }
                        else b.Add(Pane(length, h), at);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cell), cell.Kind, "Unknown cell kind.");
            }
        }
        if (b.Detail != SiteDetail.Full) return;
        // A mullion where a cell starts, whenever either side of it is built glass or panel.
        for (var c = 0; c < cells.Length; c++)
        {
            var before = cells[(c + cells.Length - 1) % cells.Length];
            if (!Built(cells[c]) && !Built(before)) continue;
            var path = cells[c].Path;
            var direction = path[^1] - path[0];
            if (direction.LengthSquared() < 1e-6f) continue;
            b.Add(PartSolids.BeveledBox(K.MullionWidth, h, K.MullionDepth, 0, L.DarkFrame), PlanFrame.Along(path[0], direction).At(0, y0, 0));
        }

        static bool Built(Cell c) => c.Kind is CellKind.Window or CellKind.Panel;
    }

    /// <summary>
    /// A one-sided glass quad in the opening frame (plane z = 0, facing −Z) for builds without a window numbering, its UV one
    /// window cell like a numbered pane's.
    /// </summary>
    private static GeometryBundle Pane(float width, float height)
    {
        var m = new Shapes.Writer(ZoneLooks.Glass);
        var n = -Vector3.UnitZ;
        const float e = 0.002f;
        m.Quad(m.Vertex(new(-width * 0.5f, 0, 0), n, new(e, e)), m.Vertex(new(width * 0.5f, 0, 0), n, new(1 - e, e)),
            m.Vertex(new(width * 0.5f, height, 0), n, new(1 - e, 1 - e)), m.Vertex(new(-width * 0.5f, height, 0), n, new(e, 1 - e)), n);
        return m.Finish().Meshes[0];
    }

    // ---- The atrium porch (closed entrance) ----

    private static float PorchTop => GlassTop(1);

    /// <summary>
    /// Yellow-green jambs and head standing <see cref="K.PorchDepth"/> out from the hub's glass line over two storeys, a
    /// granite threshold, and on the porch's glass plane the shut glass doors, side lights, a transom beam and the upper
    /// glazing. The threshold stands 3 cm over the forecourt paving (no steps).
    /// </summary>
    private static void Porch(SiteBuilder b, PlanFrame f, float y)
    {
        float hw = K.PorchHalfWidth, j = K.PorchJamb, d = K.PorchDepth, top = y + PorchTop, floor = y + K.PavingTop + 0.03f;
        var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        GridFacade.Box(b, f, -hw - j, -hw, y - 0.1f, top, -0.4f, d, L.YellowGreen, bevel);
        GridFacade.Box(b, f, hw, hw + j, y - 0.1f, top, -0.4f, d, L.YellowGreen, bevel);
        GridFacade.Box(b, f, -hw - j - 0.1f, hw + j + 0.1f, top - K.PorchHead, top, -0.4f, d + 0.1f, L.YellowGreen, bevel);
        GridFacade.Box(b, f, -hw - j - 0.15f, hw + j + 0.15f, top, top + 0.12f, -0.4f, d + 0.15f, L.Wall, bevel * 0.5f);
        GridFacade.Box(b, f, -hw, hw, y - 0.1f, floor, -0.4f, d, L.Granite, bevel * 0.5f);
        var glass = d - 0.3f;
        if (b.Detail == SiteDetail.Mass)
        {
            GridFacade.Box(b, f, -hw, hw, floor, top - K.PorchHead, glass - 0.05f, glass, ZoneLooks.Glass, 0);
            return;
        }
        var head = floor + K.DoorHeight;
        float band0 = y + Floor(1) - 0.25f, band1 = band0 + 0.5f, lintel = top - K.PorchHead;
        b.Place(PartCatalog.DoorGlass, new DoorParams(K.DoorWidth, K.DoorHeight, 0.12f, 0, 2) { Foot = FootMat.Glass }, f.AtReversed(0, floor, glass + 0.06f));
        b.Place(PartCatalog.CurtainWall, new WindowParams(K.DoorWidth, band0 - head, 0.12f, 2, 1), f.AtReversed(0, head, glass));
        var side = hw - K.DoorWidth * 0.5f;
        foreach (var s in new[] { -1f, 1f })
            b.Place(PartCatalog.CurtainWall, new WindowParams(side, band0 - floor, 0.12f, 1, 2), f.AtReversed(s * (K.DoorWidth * 0.5f + side * 0.5f), floor, glass));
        GridFacade.Box(b, f, -hw, hw, band0, band1, glass - 0.1f, glass + 0.12f, L.Wall, 0);
        b.Place(PartCatalog.CurtainWall, new WindowParams(2 * hw, lintel - band1, 0.12f, 4, 2), f.AtReversed(0, band1, glass));
    }

    // ---- The atrium's glass vault ----

    /// <summary>Vault section from the curb to the crown: (radius as a share of the ellipse, height in metres).</summary>
    internal static Vector2[] VaultProfile =>
        [new(1f, 0f), new(0.96f, 0.28f * K.AtriumRise), new(0.86f, 0.52f * K.AtriumRise), new(0.7f, 0.74f * K.AtriumRise),
            new(0.48f, 0.9f * K.AtriumRise), new(0.24f, 0.98f * K.AtriumRise), new(0f, K.AtriumRise)];

    /// <summary>The vault's frame (centre, long axis) and semi-axes, registered in the atrium roof feature's props.</summary>
    internal static (PlanFrame Frame, Vector2 Half) AtriumOf(IWorldModel world)
    {
        var props = world.Feature(CenterEastIds.Wenke.AtriumRoofId).RequiredProps();
        return (PlanFrame.Along(props.Pair("centre"), props.Pair("axis")), props.Pair("half"));
    }

    /// <summary>The hub roof the vault stands on (its top band).</summary>
    internal static float AtriumBase(float y) => y + Floor(K.HubStoreys) + K.RoofBand;

    private static Vector2[] Ellipse((PlanFrame Frame, Vector2 Half) atrium, float scale, int segments)
    {
        var f = atrium.Frame;
        var points = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = 2f * MathF.PI * i / segments;
            points[i] = f.P(MathF.Cos(a) * atrium.Half.X * scale, MathF.Sin(a) * atrium.Half.Y * scale);
        }
        return points;
    }

    private static void Atrium(SiteBuilder b, (PlanFrame Frame, Vector2 Half) atrium, float y)
    {
        var f = atrium.Frame;
        var floor = AtriumBase(y);
        var segments = b.Detail switch { SiteDetail.Full => 64, SiteDetail.Plain => 40, _ => 24 };
        var curb = PartSweeps.BeveledRectangleProfile(0.5f, K.AtriumCurb, b.Detail == SiteDetail.Full ? 0.04f : 0f);
        b.Add(PartSweeps.Sweep(curb, Ellipse(atrium, 1f, segments), L.Wall, closed: true), Matrix4x4.CreateTranslation(0, floor, 0));
        var spring = floor + K.AtriumCurb;
        b.Add(Solids.Lathe(VaultProfile, segments, ZoneLooks.Glass, atrium.Half.X, atrium.Half.Y), f.At(0, spring, 0));
        if (b.Detail == SiteDetail.Mass) return;
        // Ribs: meridians from the curb to the crown, and two horizontal rings, in the aluminium frame colour.
        var profile = VaultProfile;
        for (var r = 0; r < K.AtriumRibs; r++)
        {
            var a = 2f * MathF.PI * r / K.AtriumRibs;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            for (var k = 0; k + 1 < profile.Length; k++)
            {
                Vector3 Point(Vector2 q)
                {
                    var p = f.P(c * q.X * atrium.Half.X, s * q.X * atrium.Half.Y);
                    return new(p.X, spring + q.Y + 0.04f, p.Y);
                }
                Strut(b, Point(profile[k]), Point(profile[k + 1]), K.AtriumRib, L.Metal);
            }
        }
        if (b.Detail != SiteDetail.Full) return;
        var bar = PartSweeps.BeveledRectangleProfile(K.AtriumRib, K.AtriumRib, 0);
        foreach (var k in new[] { 2, 4 })
            b.Add(PartSweeps.Sweep(bar, Ellipse(atrium, profile[k].X + 0.004f, segments), L.Metal, closed: true), Matrix4x4.CreateTranslation(0, spring + profile[k].Y, 0));
    }

    /// <summary>A square bar between two world points.</summary>
    internal static void Strut(SiteBuilder b, Vector3 from, Vector3 to, float size, ToonSpec material)
    {
        var axis = to - from;
        var length = axis.Length();
        if (length < 0.01f) return;
        b.Add(PartSolids.BeveledBox(size, length, size, 0, material), Along(from, axis / length));
    }

    /// <summary>Rigid placement whose local Y runs along a unit direction from a point.</summary>
    internal static Matrix4x4 Along(Vector3 at, Vector3 y)
    {
        var reference = MathF.Abs(y.Y) < 0.95f ? Vector3.UnitY : Vector3.UnitX;
        var x = VectorMath.Unit(Vector3.Cross(y, reference), Vector3.UnitY, minimumLength: 1e-12f);
        var z = Vector3.Cross(x, y);
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, at.X, at.Y, at.Z, 1);
    }

    // ---- Tower roof: penthouse and fenced plant ----

    private static void TowerRoof(SiteBuilder b, Block tower, float y)
    {
        var roof = y + Floor(tower.Storeys) + K.RoofBand + K.TerraceSoil;
        var ring = tower.Outlines[^1];
        var centre = PlanPolygons.Centroid(ring);
        // The penthouse runs along the tower's south face, the plant fenced on its north side.
        // Local +Z of this frame points north, away from the south face and its yellow-green strip.
        var f = PlanFrame.Along(centre, new(-0.932f, -0.362f));
        var size = K.PenthouseSize;
        var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        b.Add(PartSolids.BeveledBox(size.X, size.Y, size.Z, bevel, L.Wall), f.At(0, roof - 0.05f, 0));
        b.Add(PartSolids.BeveledBox(size.X + 0.4f, 0.2f, size.Z + 0.4f, bevel * 0.5f, L.LightTrim), f.At(0, roof + size.Y - 0.05f, 0));
        if (b.Detail == SiteDetail.Mass) return;
        b.Place(PartCatalog.DoorFire, new DoorParams(1.0f, 2.1f, 0.12f), f.AtReversed(-size.X * 0.3f, roof, size.Z * 0.5f));
        var plant = f.Shift(0, size.Z * 0.5f + 1.2f + K.PlantSize.Y * 0.5f);
        float hx = K.PlantSize.X * 0.5f, hz = K.PlantSize.Y * 0.5f;
        foreach (var s in new[] { -1f, 1f })
        {
            b.Place(PartCatalog.EquipmentFence, new RailingParams(K.PlantSize.X, K.PlantFence, 0.06f, 1.4f) { Variation = s > 0 ? 1 : 0 }, plant.At(0, roof, s * hz));
            b.Place(PartCatalog.EquipmentFence, new RailingParams(K.PlantSize.Y, K.PlantFence, 0.06f, 1.4f), plant.AtTurned(s * hx, roof, 0));
        }
        foreach (var dx in new[] { -2.6f, 0f, 2.6f })
            b.Place(PartCatalog.HeatPump, new EquipmentParams(1.6f, 1.9f, 0.9f), plant.At(dx, roof, 0));
    }

    // ---- Collision: ground-storey walls outside the porch, the porch's jambs, threshold and shut glass ----

    public override void Collide(SiteColliders c)
    {
        var plan = Footprints(c.Context);
        var y = BaseY(c.Context);
        foreach (var block in plan.Blocks.Where(block => c.Includes(BlockRange(c.Context.World, block.Part))))
        {
            var top = y + Floor(block.Storeys) + K.Parapet;
            foreach (var cell in block.Rows[0].Where(cell => cell.Kind != CellKind.Hidden))
            {
                Vector2 a = cell.Path[0], e = cell.Path[^1];
                var shift = PlanOffsets.RightUnitNormal(a, e) * (K.BandOut * 0.5f);
                c.Wall(a + shift, e + shift, K.BandOut * 0.5f + 0.1f, y, top, SurfaceTag.Wall, FootMat.Glass);
            }
        }
        if (!c.Includes(PorchRange(c.Context.World))) return;
        var f = Door(c.Context.World);
        float hw = K.PorchHalfWidth, j = K.PorchJamb, d = K.PorchDepth, porchTop = y + PorchTop;
        foreach (var s in new[] { -1f, 1f })
            c.Box(f.At(s * (hw + j * 0.5f), (y + porchTop) * 0.5f, (d - 0.4f) * 0.5f), new(j * 0.5f, (porchTop - y) * 0.5f, (d + 0.4f) * 0.5f), SurfaceTag.Wall, FootMat.StonePaving);
        var sill = K.PavingTop + 0.03f;
        c.Box(f.At(0, y + (sill - 0.1f) * 0.5f, (d - 0.4f) * 0.5f), new(hw, (sill + 0.1f) * 0.5f, (d + 0.4f) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        c.Wall(f.P(-hw, d - 0.3f), f.P(hw, d - 0.3f), 0.15f, y, porchTop, SurfaceTag.DoorClosed, FootMat.Glass);
    }

}
