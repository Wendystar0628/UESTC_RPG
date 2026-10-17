using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Zones.Core.Framework;
using S = Campus.Gen.Zones.Core.CoreStyle;
using Shapes = Campus.Gen.Zones.Core.Framework.Shapes;

namespace Campus.Gen.Zones.Core.Library;

/// <summary>
/// 图书馆外壳 (16b, MAP-QSH-CORE): the beige-stone octagonal ring with its four corner towers, the front square tower
/// with the full-height glass slot, the one-storey podium whose roof is the 2F terrace, the set-back 5F under a canted
/// metal eave, and the roof with the octagonal skylight, curved metal roofs and the mechanical box. Interiors are
/// WP-17's; the grand stair (8398) and the door presentation are 16c's.
/// <para>
/// Faces: towers carry the vertical slits, every other face one long ribbon per storey (MAP-QSH-CORE initial value).
/// Window glass is VF-B (toon_glass with cards); the skylight and the slot are clear glass with their steel in their
/// own bundles, so they stay separate instances (MAP-QSH-CORE fog exemption).
/// </para>
/// <para>
/// Windows: one grid <see cref="WindowKey"/> for the whole entity, <see cref="Columns"/> × 6 (rows are storeys 0–5,
/// columns allocated per row by the immutable opening layout below). The glass and the window declarations both read
/// the map's allocation of that grid; nothing here numbers windows itself.
/// </para>
/// </summary>
public sealed class LibraryShell : ICorePiece, IWindowedPiece
{
    /// <summary>Key of the library's single window grid in the map's WindowNumberingSnapshot.</summary>
    public const string WindowKey = "library-shell";
    public const int Rows = 6;
    private readonly IReadOnlyList<Opening> _openings;
    private readonly IReadOnlyList<(Face Face, Rect Hole, int Column)> _mech;

    private LibraryShell(LibraryPlan plan)
    {
        Plan = plan;
        var next = new int[Rows];
        int Allocate(int cells, IEnumerable<int> rows)
        {
            var list = rows.ToArray();
            var column = list.Max(r => next[r]);
            foreach (var r in list) next[r] = column + cells;
            return column;
        }
        var openings = new List<Opening>();
        foreach (var piece in plan.Pieces)
            openings.AddRange(Layout(piece, Allocate));
        _openings = openings;
        SkylightColumn = Allocate(1, [5]);
        _mech = LibraryRoof.MechWindows(plan).Select(w => (w.Face, w.Hole, Allocate(1, [5]))).ToArray();
        Columns = next.Max();
    }

    public static LibraryShell? From(IWorldModel world)
    {
        var plan = LibraryPlan.From(world);
        if (plan is null) return null;
        var shell = new LibraryShell(plan) { _ground = world.Terrain.HeightAt };
        shell.CafeWalk = shell.WalkFrom(world);
        return shell;
    }

    /// <summary>Ground height under a plan point (the world's terrain; WP-08's evaluator or the flat stub).</summary>
    private Surface _ground = (_, _) => 0f;

    /// <summary>
    /// The paved link from the cafe apron to the nearest footway (16d-2, review of 16d): from inside the apron's front edge
    /// to the walk's edge, <see cref="CoreStyle.CafeWalkWidth"/> wide; null when the apron already meets a walk or none is in reach.
    /// </summary>
    public (Vector2 A, Vector2 B)? CafeWalk { get; private set; }

    private (Vector2 A, Vector2 B)? WalkFrom(IWorldModel world)
    {
        if (_openings.FirstOrDefault(o => o.Kind == OpeningKind.Entry) is not { } entry) return null;
        var (u0, u1, depth) = Apron(entry);
        var front = entry.Face.At((u0 + u1) * 0.5f, depth, 0f);
        var p = new Vector2(front.X, front.Z);
        (Vector2 Q, float Half, float Gap)? best = null;
        foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
        {
            if (Highway(road.Props) is not ("footway" or "path" or "pedestrian")) continue;
            var half = (road.Width > 0f ? road.Width : S.CafeWalkFallbackWidth) * 0.5f;
            var line = road.Line.Points;
            for (var k = 1; k < line.Count; k++)
            {
                Vector2 a = line[k - 1], b = line[k], ab = b - a;
                var t = Math.Clamp(Vector2.Dot(p - a, ab) / MathF.Max(ab.LengthSquared(), 1e-9f), 0f, 1f);
                var q = a + ab * t;
                var gap = Vector2.Distance(p, q) - half;
                if (best is null || gap < best.Value.Gap) best = (q, half, gap);
            }
        }
        if (best is not { } hit || hit.Gap < 0.3f || hit.Gap > S.CafeWalkReach) return null;
        var d = Vector2.Normalize(hit.Q - p);
        return (p - d * S.CafeWalkInset, hit.Q - d * (hit.Half - S.CafeWalkOverlap));
    }

    private static string? Highway(JsonElement? props) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("highway", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>
    /// The cafe walk as WP-15 paves a footway: pavers at the paving level (terrain + 0.15, 5 cm over the lawn), a granite
    /// edging band along each side and the 5 cm edge face down to the lawn.
    /// </summary>
    private void CafeWalkMesh(CoreMesh mesh)
    {
        if (CafeWalk is not { } w) return;
        var along = Vector2.Normalize(w.B - w.A);
        var side = new Vector2(-along.Y, along.X);
        float half = S.CafeWalkWidth * 0.5f, band = S.CafeWalkEdging, lift = S.PavingLift + ApronLift * 0.5f;
        Shapes.FlatStrip(mesh, S.Roof(S.CafeWalkPaving), w.A, w.B, S.CafeWalkWidth - 2 * band, _ground, lift);
        foreach (var sign in new[] { -1f, 1f })
        {
            var o = side * (sign * (half - band * 0.5f));
            Shapes.FlatStrip(mesh, S.Roof(S.CafeWalkEdge), w.A + o, w.B + o, band, _ground, lift);
            var e = side * (sign * half);
            Vector2 a = w.A + e, b = w.B + e;
            var n = new Vector3(side.X * sign, 0f, side.Y * sign);
            mesh.Quad(S.Roof(S.CafeWalkEdge), new(a.X, _ground(a.X, a.Y) + S.LawnLift - 0.02f, a.Y), new(b.X, _ground(b.X, b.Y) + S.LawnLift - 0.02f, b.Y),
                new(b.X, _ground(b.X, b.Y) + lift, b.Y), new(a.X, _ground(a.X, a.Y) + lift, a.Y), n);
        }
    }

    public string Id => "library-shell";
    public Vector2 Anchor => Plan.Centre;
    public IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(CoreFeatures.Library, Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    public LibraryPlan Plan { get; }
    public IReadOnlyList<Opening> Openings => _openings;
    /// <summary>Columns of the library's window grid (rows are storeys 0–5).</summary>
    public int Columns { get; }
    public int SkylightColumn { get; }
    public int WindowCount => Columns * Rows;

    /// <summary>
    /// The front door bay in the glass slot (16c places the door there): bottom centre on the slot's glass plane, the
    /// horizontal outward normal, and the bay's width and height up to the door transom. Null when the slot has no door.
    /// </summary>
    public (Vector3 Origin, Vector3 Outward, float Width, float Height, Face Face, float Depth)? EntranceDoor =>
        Bay(_openings.FirstOrDefault(x => x.Kind == OpeningKind.Slot && x.Door is not null));

    /// <summary>The 1F cafe door bay (lib.cafe, 16d) in the same terms; the shell places its glass door itself.</summary>
    public (Vector3 Origin, Vector3 Outward, float Width, float Height, Face Face, float Depth)? CafeDoor =>
        Bay(_openings.FirstOrDefault(x => x.Kind == OpeningKind.Entry && x.Door is not null));

    private static (Vector3 Origin, Vector3 Outward, float Width, float Height, Face Face, float Depth)? Bay(Opening? o)
    {
        if (o?.Door is not { } d) return null;
        var (a, b) = DoorSpan(o);
        return (o.Face.At((a + b) * 0.5f, -o.Depth, o.Hole.Y0), o.Face.N, b - a, d.Top - o.Hole.Y0, o.Face, o.Depth);
    }

    /// <summary>The door cells' span along the face (u from, u to).</summary>
    private static (float A, float B) DoorSpan(Opening o)
    {
        var d = o.Door!.Value;
        return (o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * d.First / o.Cells, o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * (d.First + d.Count) / o.Cells);
    }

    // ------------------------------------------------------------------ windows

    public ClaimedWindowLayout WindowLayout => new(CoreFeatures.Library, [new WindowGridSpec(WindowKey, Columns, Rows)]);

    /// <summary>The library grid's allocation; its size must be the one this layout declared.</summary>
    public WindowAllocation Allocation(WindowNumberingSnapshot numbering)
    {
        ArgumentNullException.ThrowIfNull(numbering);
        var allocation = numbering.Get(CoreFeatures.Library, WindowKey);
        if (allocation.Columns != Columns || allocation.Rows != Rows)
            throw new InvalidOperationException($"Library window grid is {Columns} × {Rows}, the allocation {allocation.Columns} × {allocation.Rows}.");
        return allocation;
    }

    public IReadOnlyList<WindowDecl> Windows(WindowNumberingSnapshot numbering)
    {
        var allocation = Allocation(numbering);
        return Cells().Select(c => new WindowDecl(allocation.IdAt(c.Column, c.Row), allocation.BaseId, Columns, S.LibraryWindowChannel,
            c.Centre, c.Right, c.Up, c.Width, c.Height)).ToArray();
    }

    /// <summary>
    /// Every logical window cell on its glass plane: the openings' cells row by row, the skylight, the mechanical box.
    /// Right × Up points outdoors (WindowDecl, OpeningGlass.Declare).
    /// </summary>
    public IEnumerable<(int Column, int Row, Vector3 Centre, Vector3 Right, Vector3 Up, float Width, float Height)> Cells()
    {
        var up = Vector3.UnitY;
        foreach (var o in _openings)
            foreach (var (y0, y1, row) in o.Rows)
                for (var c = 0; c < o.Cells; c++)
                {
                    float a = o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * c / o.Cells, b = o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * (c + 1) / o.Cells, g = o.GlassBottom(c, y0);
                    yield return (o.Column + c, row, o.Face.At((a + b) * 0.5f, -o.Depth, (g + y1) * 0.5f), -o.Face.T, up, b - a, y1 - g);
                }
        var sky = (LibraryRoof.SkylightBaseY(Plan) + LibraryRoof.SkylightApexY(Plan)) * 0.5f;
        yield return (SkylightColumn, 5, new Vector3(Plan.Centre.X, sky, Plan.Centre.Y), Vector3.UnitX, -Vector3.UnitZ, S.SkylightAcross, S.SkylightAcross);
        foreach (var (face, hole, column) in _mech)
            yield return (column, 5, face.At((hole.U0 + hole.U1) * 0.5f, -S.Reveal, (hole.Y0 + hole.Y1) * 0.5f), -face.T, up, hole.U1 - hole.U0, hole.Y1 - hole.Y0);
    }

    // ------------------------------------------------------------------ layout

    private IEnumerable<Opening> Layout(WallPiece piece, Func<int, IEnumerable<int>, int> allocate)
    {
        var f = Face.Of(piece);
        var length = piece.Length;
        if (length < S.MinWindowFace) yield break;
        var plan = Plan;
        switch (piece.Block.Kind)
        {
            case BlockKind.Podium:
                {
                    float y0 = plan.FloorY + S.PodiumSill, y1 = y0 + S.PodiumHeight;
                    if (piece.Y0 > plan.FloorY + 0.01f || piece.Y1 < y1 + 0.3f || length < 2 * S.RibbonMargin + S.PodiumModule) yield break;
                    float u0 = S.RibbonMargin, u1 = length - S.RibbonMargin;
                    // lib.cafe (16d): the end of the ribbon at the store front becomes a door bay from the floor up, the door
                    // open to the ribbon's transom line, its light above; a pier, then the rest of the ribbon.
                    if (plan.CafeFace is { } cafe && ReferenceEquals(cafe.Piece, piece) && length > 2 * S.RibbonMargin + (S.CafeDoorCells + 2) * S.PodiumModule + S.CafeDoorPier)
                    {
                        var width = S.CafeDoorCells * S.PodiumModule;
                        var bay = cafe.AtA ? new Rect(u0, u0 + width, plan.FloorY, y1) : new Rect(u1 - width, u1, plan.FloorY, y1);
                        yield return new Opening(f, OpeningKind.Entry, bay, S.CafeDoorCells, [(plan.FloorY, y1, 0)], allocate(S.CafeDoorCells, [0]), S.CardReading)
                        { Door = (0, S.CafeDoorCells, y0 + S.PodiumTransom) };
                        if (cafe.AtA) u0 = bay.U1 + S.CafeDoorPier; else u1 = bay.U0 - S.CafeDoorPier;
                    }
                    var cells = Math.Max(1, (int)MathF.Round((u1 - u0) / S.PodiumModule));
                    yield return new(f, OpeningKind.Podium, new(u0, u1, y0, y1), cells, [(y0, y1, 0)], allocate(cells, [0]), S.CardReading);
                    break;
                }
            case BlockKind.Wing:
                if (length < 2 * S.RibbonMargin + S.RibbonModule) yield break;
                for (var k = 0; k < S.LibraryStoreys.Length - 1; k++)
                {
                    float y0 = plan.StoreyY(k) + S.RibbonSill, y1 = y0 + S.RibbonHeight;
                    if (plan.StoreyY(k) < piece.Y0 - 0.05f || y1 > piece.Y1 - 0.3f) continue;
                    var cells = Math.Max(1, (int)MathF.Round((length - 2 * S.RibbonMargin) / S.RibbonModule));
                    yield return new(f, OpeningKind.Ribbon, new(S.RibbonMargin, length - S.RibbonMargin, y0, y1), cells, [(y0, y1, k)], allocate(cells, [k]), S.CardReading);
                }
                break;
            case BlockKind.Setback:
                {
                    var row = S.LibraryStoreys.Length - 1;
                    var count = (int)MathF.Floor((length - 1.8f + (S.SquarePitch - S.SquareSize)) / S.SquarePitch);
                    float y0 = plan.StoreyY(row) + S.SquareSill, y1 = y0 + S.SquareSize;
                    for (var i = 0; i < count; i++)
                    {
                        var u = length * 0.5f + (i - (count - 1) * 0.5f) * S.SquarePitch;
                        yield return new(f, OpeningKind.Square, new(u - S.SquareSize * 0.5f, u + S.SquareSize * 0.5f, y0, y1), 1, [(y0, y1, row)], allocate(1, [row]), S.CardReading);
                    }
                    break;
                }
            case BlockKind.Tower when length >= 8f && piece.Y1 - piece.Y0 > 6f:
                {
                    var count = Math.Min(S.TowerSlits, (int)MathF.Floor((length - 2f) / S.SlitPitch) + 1);
                    float y0 = MathF.Max(piece.Y0, plan.SecondFloorY) + 0.9f, y1 = piece.Y1 - S.SlitTopGap;
                    var rows = StoreyRows(y0, y1);
                    for (var i = 0; i < count; i++)
                    {
                        var u = length * 0.5f + (i - (count - 1) * 0.5f) * S.SlitPitch;
                        yield return new(f, OpeningKind.Slit, new(u - S.SlitWidth * 0.5f, u + S.SlitWidth * 0.5f, y0, y1), 1, rows, allocate(1, rows.Select(r => r.Row)), S.CardStair);
                    }
                    break;
                }
            case BlockKind.FrontTower when length >= 8f && piece.Y1 - piece.Y0 > 6f:
                {
                    float y0 = MathF.Max(piece.Y0, plan.SecondFloorY), y1 = piece.Y1 - S.SlotTopGap;
                    var rows = StoreyRows(y0, y1);
                    var cells = S.SlotMullions + 1;
                    // The 2F front door (16c) opens the slot's middle cells below the door transom.
                    var door = MathF.Abs(y0 - plan.SecondFloorY) < 0.01f ? ((cells - S.DoorCells) / 2, S.DoorCells, y0 + S.SlotDoorHeight) : ((int, int, float)?)null;
                    var slot = new Opening(f, OpeningKind.Slot, new((length - S.SlotWidth) * 0.5f, (length + S.SlotWidth) * 0.5f, y0, y1), cells, rows,
                        allocate(cells, rows.Select(r => r.Row)), S.CardReading);
                    yield return slot with { Door = door };
                    break;
                }
            case BlockKind.Tower or BlockKind.FrontTower or BlockKind.Core:
                break;
            case BlockKind.Annex or BlockKind.Neighbour:
                throw new InvalidOperationException("The north blocks (16e) are not part of the library plan.");
            default:
                throw new ArgumentOutOfRangeException(nameof(piece), piece.Block.Kind, "Unknown block kind.");
        }
    }

    /// <summary>The span y0…y1 cut at the storey floors it crosses, each part tagged with its storey (window row).</summary>
    private IReadOnlyList<(float Y0, float Y1, int Row)> StoreyRows(float y0, float y1)
    {
        var list = new List<(float, float, int)>();
        for (var k = 0; k < Rows; k++)
        {
            float a = MathF.Max(y0, Plan.StoreyY(k)), b = MathF.Min(y1, Plan.StoreyY(k + 1));
            if (b - a > 0.3f) list.Add((a, b, k));
        }
        return list;
    }

    // ------------------------------------------------------------------ build

    public void Build(CoreBuild build, LodLevel lod)
    {
        var mesh = build.Mesh;
        int? windowBase = build.WindowNumbering is { } numbering ? Allocation(numbering).BaseId : null;
        var glass = build.Glass(S.WindowGlass, windowBase, Columns, S.LibraryWindowChannel);
        var clear = build.Glass(S.ClearGlass, windowBase, Columns, S.LibraryWindowChannel);
        var landmark = new CoreMesh(build.Palette);
        Walls(mesh, lod);
        foreach (var o in _openings)
            LibraryFacade.Window(o.Kind == OpeningKind.Slot ? landmark : mesh, o.Kind == OpeningKind.Slot ? clear : glass, o, lod);
        if (lod <= LodLevel.L1) Bands(mesh);
        Tops(mesh);
        Caps(mesh);
        LibraryRoof.Skylight(Plan, mesh, landmark, clear, SkylightColumn, lod);
        if (lod <= LodLevel.L2) LibraryRoof.Vaults(Plan, mesh, lod);
        if (lod <= LodLevel.L1) LibraryRoof.Plant(Plan, build);
        if (lod <= LodLevel.L1 && CafeDoor is { } cafe)
            build.Parts.Add(new PlacedPart(new PartId(PartCatalog.DoorGlass),
                new DoorParams(cafe.Width, cafe.Height, S.DoorDepth, S.DoorOpenDeg, S.DoorLeaves) { Foot = FootMat.Glass },
                OpeningPlacement.Frame(cafe.Origin, cafe.Outward)));
        if (lod <= LodLevel.L2 && _openings.FirstOrDefault(o => o.Kind == OpeningKind.Entry) is { } entry)
        {
            var (u0, u1, depth) = Apron(entry);
            LibraryFacade.Box(mesh, S.Roof(S.LibraryTerrace), entry.Face, u0, u1, 0f, depth, Plan.PadY - S.Sink, entry.Hole.Y0 + ApronLift);
            CafeWalkMesh(mesh);
        }
        Mech(mesh, glass, lod);
        build.Bundles.AddRange(landmark.Finish());
    }

    private void Walls(CoreMesh mesh, LodLevel lod)
    {
        var wall = S.Wall(S.LibraryWall);
        var byPiece = _openings.ToLookup(o => o.Face);
        foreach (var piece in Plan.Pieces)
        {
            var f = Face.Of(piece);
            var holes = lod <= LodLevel.L1 ? byPiece[f].Select(o => o.Hole).ToArray() : [];
            LibraryFacade.Wall(mesh, wall, f, 0f, piece.Length, piece.Y0, piece.Y1, holes);
        }
    }

    /// <summary>Plinth band on every ground-standing piece and a string course at each inner floor line of the wings.</summary>
    private void Bands(CoreMesh mesh)
    {
        var trim = S.Trim(S.LibraryTrim);
        foreach (var piece in Plan.Pieces)
        {
            var f = Face.Of(piece);
            const float ext = 0.03f;
            if (piece.Y0 <= Plan.WallBaseY + 0.01f)
            {
                // The plinth band breaks at a ground-floor door bay (lib.cafe).
                var u = -ext;
                foreach (var hole in _openings.Where(o => o.Door is not null && o.Face == f && o.Hole.Y0 < Plan.PlinthTopY).Select(o => o.Hole).OrderBy(h => h.U0))
                {
                    LibraryFacade.Box(mesh, trim, f, u, hole.U0, 0f, S.PlinthProject, piece.Y0, Plan.PlinthTopY);
                    u = hole.U1;
                }
                LibraryFacade.Box(mesh, trim, f, u, piece.Length + ext, 0f, S.PlinthProject, piece.Y0, Plan.PlinthTopY);
            }
            if (piece.Block.Kind != BlockKind.Wing) continue;
            for (var k = 1; k < S.LibraryStoreys.Length - 1; k++)
            {
                var y = Plan.StoreyY(k);
                if (y < piece.Y0 + 0.3f || y > piece.Y1 - 0.3f) continue;
                LibraryFacade.Box(mesh, trim, f, -ext, piece.Length + ext, 0f, S.CourseProject, y - S.CourseHeight * 0.5f, y + S.CourseHeight * 0.5f);
            }
        }
    }

    /// <summary>Parapets with copings on the podium, wings and towers; the canted eave on the set-back 5F.</summary>
    private void Tops(CoreMesh mesh)
    {
        var wall = S.Wall(S.LibraryWall);
        var trim = S.Trim(S.LibraryTrim);
        foreach (var block in Plan.Blocks)
        {
            var tops = Plan.Pieces.Where(p => ReferenceEquals(p.Block, block) && p.Top).Select(p => (p.A, p.B)).ToList();
            if (tops.Count == 0) continue;
            if (block.Kind == BlockKind.Podium) tops = [.. tops.SelectMany(t => Split(t, Plan.Landing)).Where(s => !s.Cut).Select(s => (s.A, s.B))];
            var (chains, closed) = LibraryFacade.Chain(tops);
            switch (block.Kind)
            {
                case BlockKind.Setback:
                    LibraryFacade.Sweep(mesh, S.Sheet(S.LibraryEave), chains, closed, LibraryFacade.Eave(), block.Y1);
                    break;
                case BlockKind.Podium or BlockKind.Wing or BlockKind.Tower or BlockKind.FrontTower:
                    {
                        var height = block.Kind is BlockKind.Podium or BlockKind.Wing ? S.ParapetHeight : S.TowerCrown;
                        LibraryFacade.Sweep(mesh, wall, chains, closed, Body(height - S.CopingHeight, S.ParapetThick), block.Y1);
                        LibraryFacade.Sweep(mesh, trim, chains, closed, Coping(S.ParapetThick), block.Y1 + height - S.CopingHeight);
                        break;
                    }
                case BlockKind.Core:
                    break;
                case BlockKind.Annex or BlockKind.Neighbour:
                    throw new InvalidOperationException("The north blocks (16e) are not part of the library plan.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(block), block.Kind, "Unknown block kind.");
            }
        }
    }

    private static SweepProfile Body(float height, float thick) => new([new(-thick, 0), new(0, 0), new(0, height), new(-thick, height)]);

    private static SweepProfile Coping(float thick)
    {
        float t = thick, c = S.CopingHeight, o = S.CopingOverhang, b = S.CopingBevel;
        return new([new(-t - o, 0), new(o, 0), new(o, c - b), new(o - b, c), new(-t - o + b, c), new(-t - o, c - b)]);
    }

    /// <summary>A segment split where <paramref name="cuts"/> lie along it: the spans in order, each marked cut or not.</summary>
    private static IEnumerable<(Vector2 A, Vector2 B, bool Cut)> Split((Vector2 A, Vector2 B) segment, IReadOnlyList<(Vector2 A, Vector2 B)> cuts)
    {
        var length = Vector2.Distance(segment.A, segment.B);
        var d = (segment.B - segment.A) / length;
        var spans = new List<(float Lo, float Hi)>();
        foreach (var (a, b) in cuts)
            if (LibraryPlan.Overlap(segment.A, segment.B, a, b) is (var lo, var hi)) spans.Add((lo, hi));
        var t = 0f;
        foreach (var (lo, hi) in spans.Where(s => s.Hi - s.Lo > 0.1f).OrderBy(s => s.Lo))
        {
            if (lo - t > 0.1f) yield return (segment.A + d * t, segment.A + d * lo, false);
            if (hi > t) yield return (segment.A + d * MathF.Max(t, lo), segment.A + d * hi, true);
            t = MathF.Max(t, hi);
        }
        if (length - t > 0.1f) yield return (segment.A + d * t, segment.B, false);
    }

    /// <summary>Top surfaces: the podium terrace in light paving, every other roof dark grey, the core open over the atrium.</summary>
    private void Caps(CoreMesh mesh)
    {
        foreach (var block in Plan.Blocks)
        {
            var paint = block.Kind == BlockKind.Podium ? S.Roof(S.LibraryTerrace) : S.Roof(S.LibraryRoof);
            IReadOnlyList<IReadOnlyList<Vector2>> holes = [];
            if (block.Kind == BlockKind.Core)
            {
                var hole = new Vector2[8];
                for (var k = 0; k < 8; k++) { var v = LibraryRoof.Vertex(Plan, 7 - k, Plan.SkylightRadius, 0); hole[k] = new(v.X, v.Z); }
                holes = [hole];
            }
            var polygon = PolygonTriangulator.Triangulate(new PolygonRings(block.Ring, holes));
            var v3 = polygon.Vertices.Select(p => new Vector3(p.X, block.Y1, p.Y)).ToArray();
            for (var i = 0; i < polygon.Indices.Length; i += 3)
                mesh.Tri(paint, v3[polygon.Indices[i]], v3[polygon.Indices[i + 1]], v3[polygon.Indices[i + 2]], Vector3.UnitY);
        }
    }

    /// <summary>The raised mechanical box north of the skylight: walls with two rows of small windows, coping, roof.</summary>
    private void Mech(CoreMesh mesh, GlassSink glass, LodLevel lod)
    {
        var wall = S.Wall(S.LibraryWall);
        float y0 = Plan.RoofY - 0.02f, y1 = Plan.RoofY + S.MechHeight;
        var faces = LibraryRoof.MechFaces(Plan);
        var windows = lod <= LodLevel.L2
            ? _mech.Select(m => new Opening(m.Face, OpeningKind.Mech, m.Hole, 1, [(m.Hole.Y0, m.Hole.Y1, 5)], m.Column, S.CardMech)).ToArray()
            : [];
        foreach (var f in faces)
            LibraryFacade.Wall(mesh, wall, f, 0f, f.Length, y0, y1, lod <= LodLevel.L1 ? windows.Where(o => o.Face == f).Select(o => o.Hole).ToArray() : []);
        foreach (var o in windows) LibraryFacade.Window(mesh, glass, o, lod);
        var ring = new[] { faces[0].A, faces[0].A + faces[0].Along * faces[0].Length, faces[1].A, faces[1].A + faces[1].Along * faces[1].Length };
        LibraryFacade.Sweep(mesh, S.Trim(S.LibraryTrim), [ring], [true], Coping(0.25f), y1);
        mesh.Quad(S.Roof(S.LibraryRoof), new(ring[0].X, y1, ring[0].Y), new(ring[1].X, y1, ring[1].Y), new(ring[2].X, y1, ring[2].Y), new(ring[3].X, y1, ring[3].Y), Vector3.UnitY);
    }

    // ------------------------------------------------------------------ collision

    /// <summary>
    /// Walls standing on the ground block from their foot to 300 m (the roof is out of reach, MAP-QSH-CORE), except
    /// the podium, whose walls stop 1.5 m above its terrace; walls rising from the terrace block from there up. The
    /// terrace is a stone slab (16c's stair lands on it), its parapet left open where the stair base stands.
    /// <para>
    /// Door bays (the 2F front door, the 1F cafe door) are left open in the face line: the recess has its jambs, a floor
    /// level with the door's, the glass plane beside the door and the door's two fixed outer leaves as walls, and a lintel
    /// over the two open leaves, so the clear way is the open leaves' width (WLD-INTERIORS).
    /// </para>
    /// </summary>
    public IReadOnlyList<Collider> Collide()
    {
        var list = new List<Collider>();
        var top = Plan.PadY + 300f;
        foreach (var piece in Plan.Pieces)
        {
            var onGround = piece.Y0 <= Plan.WallBaseY + 0.01f;
            var onTerrace = MathF.Abs(piece.Y0 - Plan.SecondFloorY) < 0.01f;
            if (!onGround && !onTerrace) continue;
            var face = Face.Of(piece);
            var doors = _openings.Where(o => o.Door is not null && o.Face == face).ToArray();
            if (piece.Block.Kind == BlockKind.Podium)
            {
                foreach (var (a, b, landing) in Split((piece.A, piece.B), Plan.Landing))
                    foreach (var (p, q) in Open(face, a, b, doors))
                        list.Add(Wall(p, q, piece.Y0, landing ? Plan.SecondFloorY : Plan.SecondFloorY + 1.5f));
                foreach (var o in doors) list.AddRange(DoorBay(o, Plan.SecondFloorY + 1.5f));
                foreach (var o in doors.Where(o => o.Kind == OpeningKind.Entry))
                {
                    var (u0, u1, depth) = Apron(o);
                    Vector2 P(float u, float s) { var p = o.Face.At(u, s, 0f); return new(p.X, p.Z); }
                    list.Add(new SlabCollider([P(u0, 0f), P(u1, 0f), P(u1, depth), P(u0, depth)], o.Hole.Y0 + ApronLift, 0.3f, SurfaceTag.Ground) { Foot = FootMat.StonePaving });
                }
                if (CafeWalk is { } w && doors.Any(o => o.Kind == OpeningKind.Entry))
                {
                    var along = Vector2.Normalize(w.B - w.A);
                    var side = new Vector2(-along.Y, along.X) * (S.CafeWalkWidth * 0.5f);
                    var mid = (w.A + w.B) * 0.5f;
                    list.Add(new SlabCollider([w.A - side, w.B - side, w.B + side, w.A + side], _ground(mid.X, mid.Y) + S.PavingLift, 0.3f, SurfaceTag.Ground) { Foot = FootMat.StonePaving });
                }
                continue;
            }
            foreach (var (p, q) in Open(face, piece.A, piece.B, doors)) list.Add(Wall(p, q, piece.Y0, top));
            foreach (var o in doors) list.AddRange(DoorBay(o, top));
        }
        foreach (var podium in Plan.Blocks.Where(b => b.Kind == BlockKind.Podium))
            list.Add(new SlabCollider([.. podium.Ring], Plan.SecondFloorY, 0.3f, SurfaceTag.Ground) { Foot = FootMat.StonePaving });
        return list;
    }

    private static WallCollider Wall(Vector2 a, Vector2 b, float y0, float y1) => new(a, b, 0.15f, y0, y1, SurfaceTag.Wall) { Foot = FootMat.StonePaving };

    /// <summary>The apron's top stands this little proud of the threshold, clear of WP-15's paving at the same level.</summary>
    private const float ApronLift = 0.01f;

    /// <summary>The granite apron in front of the cafe door bay: its span along the face and its depth out from it.</summary>
    private static (float U0, float U1, float Depth) Apron(Opening entry) =>
        (entry.Hole.U0 - S.CafeApronMargin, entry.Hole.U1 + S.CafeApronMargin, S.CafeApronDepth);

    /// <summary>The segment a → b of a face line without the holes of its door bays.</summary>
    private static IEnumerable<(Vector2 A, Vector2 B)> Open(Face face, Vector2 a, Vector2 b, IReadOnlyList<Opening> doors)
    {
        float ua = Vector2.Dot(a - face.A, face.Along), ub = Vector2.Dot(b - face.A, face.Along);
        var (lo, hi) = (MathF.Min(ua, ub), MathF.Max(ua, ub));
        var u = lo;
        foreach (var o in doors.OrderBy(o => o.Hole.U0))
        {
            if (o.Hole.U1 <= u || o.Hole.U0 >= hi) continue;
            if (o.Hole.U0 - u > 0.05f) yield return (face.A + face.Along * u, face.A + face.Along * o.Hole.U0);
            u = MathF.Max(u, o.Hole.U1);
        }
        if (hi - u > 0.05f) yield return (face.A + face.Along * u, face.A + face.Along * hi);
    }

    /// <summary>Colliders of one door bay up to <paramref name="top"/> (see <see cref="Collide"/>).</summary>
    private static IEnumerable<Collider> DoorBay(Opening o, float top)
    {
        var f = o.Face;
        var h = o.Hole;
        float s = -o.Depth, y = h.Y0, doorTop = o.Door!.Value.Top;
        var (d0, d1) = DoorSpan(o);
        var leaf = (d1 - d0) / S.DoorLeaves;
        Vector2 P(float u, float at) { var p = f.At(u, at, 0f); return new(p.X, p.Z); }
        yield return new SlabCollider([P(h.U0, 0f), P(h.U1, 0f), P(h.U1, s), P(h.U0, s)], y, 0.3f, SurfaceTag.Ground) { Foot = FootMat.StonePaving };
        yield return Wall(P(h.U0, 0f), P(h.U0, s), y, top);
        yield return Wall(P(h.U1, 0f), P(h.U1, s), y, top);
        yield return Wall(P(h.U0, s), P(d0 + leaf, s), y, top);
        yield return Wall(P(d1 - leaf, s), P(h.U1, s), y, top);
        yield return Wall(P(d0 + leaf, s), P(d1 - leaf, s), doorTop, top);
    }

}
