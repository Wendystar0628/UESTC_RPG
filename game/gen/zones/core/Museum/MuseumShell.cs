using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using S = Campus.Gen.Zones.Core.CoreStyle;
using Shapes = Campus.Gen.Zones.Core.Framework.Shapes;

namespace Campus.Gen.Zones.Core.Museum;

/// <summary>
/// 电子科技博物馆 (16d-2, MAP-QSH-CORE): one entity, the hall body 8401 inside the colonnade ring 8402. The granite
/// gallery storey (带横槽的浅灰花岗岩外墙) fills 8401 under a flat roof that runs out over the ring to a white eave band on
/// square columns (柱廊环, walkable and dry in rain); the white central hall (序厅, 约 28×26 m) rises from the middle with a
/// clerestory ribbon and a low pyramid roof (白色体量，低坡锥形屋顶). The south colonnade carries the portico (方柱门廊):
/// four larger columns, a raised head block with the name field, and the main door; the lake face (朝湖立面) is
/// floor-to-head dark glass with the side door to the boardwalk. Interiors are WP-17's; the entrance kit (mat, sign,
/// door lamp) WP-13's from the interior index.
/// <para>
/// Plan: every ring is the hall body octagon offset per edge (the colonnade outer line from 8402's parallel edge) and
/// chamfered at its corners, so the walls, eave and platform keep 8401's shape and orientation and no fold sits near 45°.
/// Windows: one grid <see cref="WindowKey"/>, <see cref="Columns"/> × 2 (row 0 the gallery storey, row 1 the clerestory),
/// on the public-interior channel.
/// </para>
/// </summary>
public sealed class MuseumShell : ICorePiece, IWindowedPiece
{
    public const string WindowKey = "museum-shell";
    public const int Rows = 2;

    private readonly IReadOnlyList<Opening> _openings;
    private readonly IReadOnlyList<Opening> _clerestory;

    private MuseumShell(BuildingFrame frame, IReadOnlyList<Vector2> body, IReadOnlyList<float> ring)
    {
        Frame = frame;
        Body = body;
        var widths = ring.ToArray();
        Platform = MuseumGeometry.Chamfer(MuseumGeometry.Offset(body, widths), S.MuseumCornerChamfer);
        ColumnLine = MuseumGeometry.Offset(body, widths.Select(w => w - S.ColumnInset).ToArray());
        Eave = MuseumGeometry.Chamfer(MuseumGeometry.Offset(body, widths.Select(w => w - S.ColumnInset + S.ColumnSize * 0.5f + S.MuseumEaveOverhang).ToArray()), S.MuseumCornerChamfer);
        Gallery = MuseumGeometry.Chamfer(body, S.MuseumCornerChamfer);
        Hall = MuseumGeometry.Chamfer(MuseumGeometry.Scale(body, S.MuseumHallLength, S.MuseumHallWidth), S.MuseumCornerChamfer);
        var normals = Enumerable.Range(0, body.Count).Select(i => MuseumGeometry.Normal(body, i)).ToArray();
        South = Array.IndexOf(normals, normals.MaxBy(n => n.Y));
        West = Array.IndexOf(normals, normals.MinBy(n => n.X));
        North = Array.IndexOf(normals, normals.MinBy(n => n.Y));
        East = Array.IndexOf(normals, normals.MaxBy(n => n.X));

        var next = new int[Rows];
        int Allocate(int cells, int row) { var c = next[row]; next[row] += cells; return c; }
        var openings = new List<Opening>();
        for (var i = 0; i < body.Count; i++)
        {
            var f = GalleryFace(i);
            float y0 = frame.FloorY, mid = f.Length * 0.5f;
            if (i == South)
            {
                var half = S.EntryCells * S.EntryCell * 0.5f;
                openings.Add(new Opening(f, OpeningKind.Entry, new(mid - half, mid + half, y0, y0 + S.EntryHeight), S.EntryCells, [(y0, y0 + S.EntryHeight, 0)],
                    Allocate(S.EntryCells, 0), S.CardLecture)
                { Door = ((S.EntryCells - S.EntryDoorCells) / 2, S.EntryDoorCells, y0 + S.EntryTransom) });
            }
            if (i == West)
            {
                float u0 = S.LakePier, u1 = f.Length - S.LakePier;
                var cells = Math.Max(2, (int)MathF.Round((u1 - u0) / S.LakeModule));
                // The side door's cell counts from the face's north end (smaller z).
                var fromNorth = (int)MathF.Round(S.LakeDoorAt * (cells - 1));
                var northAtA = f.At(0, 0, 0).Z < f.At(f.Length, 0, 0).Z;
                var door = northAtA ? fromNorth : cells - 1 - fromNorth;
                openings.Add(new Opening(f, OpeningKind.Entry, new(u0, u1, y0, y0 + S.EntryHeight), cells, [(y0, y0 + S.EntryHeight, 0)],
                    Allocate(cells, 0), S.CardLecture)
                { Door = (door, 1, y0 + S.EntryTransom) });
            }
            if (i == North || i == East || i == South)
            {
                var clear = i == South ? S.EntryCells * S.EntryCell * 0.5f + 1.5f : -1f;
                var count = (int)MathF.Floor((f.Length - 2 * S.MuseumSlitMargin) / S.MuseumSlitPitch) + 1;
                for (var k = 0; k < count; k++)
                {
                    var u = mid + (k - (count - 1) * 0.5f) * S.MuseumSlitPitch;
                    if (MathF.Abs(u - mid) < clear) continue;
                    float a = y0 + S.MuseumSlitSill, b = y0 + S.EntryHeight;
                    openings.Add(new Opening(f, OpeningKind.Square, new(u - S.MuseumSlitWidth * 0.5f, u + S.MuseumSlitWidth * 0.5f, a, b), 1, [(a, b, 0)],
                        Allocate(1, 0), S.CardLecture));
                }
            }
        }
        _openings = openings;
        var clerestory = new List<Opening>();
        for (var i = 0; i < body.Count; i++)
        {
            var f = HallFace(i);
            if (f.Length < 2 * S.ClerestoryMargin + S.ClerestoryModule) continue;
            var cells = Math.Max(1, (int)MathF.Round((f.Length - 2 * S.ClerestoryMargin) / S.ClerestoryModule));
            float a = RoofY + S.ClerestorySill, b = a + S.ClerestoryHeight;
            clerestory.Add(new Opening(f, OpeningKind.Ribbon, new(S.ClerestoryMargin, f.Length - S.ClerestoryMargin, a, b), cells, [(a, b, 1)],
                Allocate(cells, 1), S.CardVenue));
        }
        _clerestory = clerestory;
        Columns = next.Max();
    }

    /// <summary>The museum, or null when the entity or its hall body is not in this world.</summary>
    public static MuseumShell? From(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Features.TryGetValue(CoreFeatures.Museum, out var museum)) return null;
        var hall = museum.Parts.FirstOrDefault(p => p.SourceId == CoreFeatures.MuseumHall);
        if (hall is null) return null;
        var body = Plan.Polygons(PlanGeometry.FacadeGeometryOf(hall)).FirstOrDefault()?.Outer;
        if (body is null || body.Count < 4) return null;
        body = MuseumGeometry.Positive(body);
        var outer = museum.Parts.Where(p => p.SourceId == CoreFeatures.MuseumRing).SelectMany(p => Plan.Polygons(PlanGeometry.FacadeGeometryOf(p))).FirstOrDefault()?.Outer;
        var ring = Enumerable.Range(0, body.Count).Select(i => outer is null ? S.ColonnadeFallback : MuseumGeometry.ParallelDistance(body, i, outer, S.ColonnadeFallback)).ToArray();
        var frame = BuildingFrame.Of(world, CoreFeatures.Museum, S.MuseumStoreys);
        return new MuseumShell(frame, body, ring);
    }

    public string Id => "museum";
    public Vector2 Anchor => Plan.Centroid([Body]);
    public IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(CoreFeatures.Museum, Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    public BuildingFrame Frame { get; }
    /// <summary>The hall body octagon (8401), counterclockwise in (x, z) so each edge's outward side is (dz, −dx).</summary>
    public IReadOnlyList<Vector2> Body { get; }
    /// <summary>Chamfered rings: gallery wall line, hall wall line, eave outer face, platform edge.</summary>
    public IReadOnlyList<Vector2> Gallery { get; }
    public IReadOnlyList<Vector2> Hall { get; }
    public IReadOnlyList<Vector2> Eave { get; }
    public IReadOnlyList<Vector2> Platform { get; }
    /// <summary>Column centre line: the body octagon offset, corners not chamfered (a column stands on each).</summary>
    public IReadOnlyList<Vector2> ColumnLine { get; }
    /// <summary>Body edges facing south (portico, main door), west (the lake front), north and east.</summary>
    public int South { get; }
    public int West { get; }
    public int North { get; }
    public int East { get; }
    public IReadOnlyList<Opening> Openings => _openings;
    public IReadOnlyList<Opening> Clerestory => _clerestory;
    public int Columns { get; }

    public float FloorY => Frame.FloorY;
    public float SoffitY => FloorY + S.MuseumSoffit;
    public float RoofY => Frame.EavesY;
    public float HallEavesY => FloorY + S.MuseumHallEaves;

    /// <summary>Main face of body edge <paramref name="i"/> on a chamfered ring (faces alternate corner, main).</summary>
    private static Face MainFace(IReadOnlyList<Vector2> ring16, int i) => MuseumGeometry.FaceOf(ring16, 2 * i + 1);
    public Face GalleryFace(int i) => MainFace(Gallery, i);
    public Face HallFace(int i) => MainFace(Hall, i);

    /// <summary>The two door bays: the main door under the portico and the lake side door.</summary>
    public Opening MainDoor => _openings.First(o => o.Door is not null && o.Face == GalleryFace(South));
    public Opening LakeDoor => _openings.First(o => o.Door is not null && o.Face == GalleryFace(West));

    /// <summary>
    /// Door centre on its glass plane at the floor, outward normal and clear width of each open entrance, for the interior
    /// index (EntranceDecl: outside landing 2.5 m out, inside 3.0 m in). The clear width is the open leaves' span: the main
    /// door's outer two leaves are fixed side lights, both lake door leaves open.
    /// </summary>
    public IEnumerable<(string Id, Vector3 Centre, Vector3 Outward, float ClearWidth)> Entrances()
    {
        foreach (var (id, o, leaves) in new[] { ("museum.main", MainDoor, S.MuseumDoorLeaves), ("museum.lake", LakeDoor, S.LakeDoorLeaves) })
        {
            var (a, b) = DoorSpan(o);
            var open = leaves > 2 ? (b - a) * (leaves - 2) / leaves : b - a;
            yield return (id, o.Face.At((a + b) * 0.5f, -o.Depth, o.Hole.Y0), o.Face.N, open);
        }
    }

    private static (float A, float B) DoorSpan(Opening o)
    {
        var d = o.Door!.Value;
        return (o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * d.First / o.Cells, o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * (d.First + d.Count) / o.Cells);
    }

    // ------------------------------------------------------------------ windows

    public ClaimedWindowLayout WindowLayout => new(CoreFeatures.Museum, [new WindowGridSpec(WindowKey, Columns, Rows)]);

    public WindowAllocation Allocation(WindowNumberingSnapshot numbering)
    {
        ArgumentNullException.ThrowIfNull(numbering);
        var allocation = numbering.Get(CoreFeatures.Museum, WindowKey);
        if (allocation.Columns != Columns || allocation.Rows != Rows)
            throw new InvalidOperationException($"Museum window grid is {Columns} × {Rows}, the allocation {allocation.Columns} × {allocation.Rows}.");
        return allocation;
    }

    public IReadOnlyList<WindowDecl> Windows(WindowNumberingSnapshot numbering)
    {
        var allocation = Allocation(numbering);
        return Cells().Select(c => new WindowDecl(allocation.IdAt(c.Column, c.Row), allocation.BaseId, Columns, S.MuseumWindowChannel,
            c.Centre, c.Right, c.Up, c.Width, c.Height)).ToArray();
    }

    /// <summary>Every logical window cell on its glass plane, row by row; Right × Up points outdoors.</summary>
    public IEnumerable<(int Column, int Row, Vector3 Centre, Vector3 Right, Vector3 Up, float Width, float Height)> Cells()
    {
        foreach (var o in _openings.Concat(_clerestory))
            foreach (var (y0, y1, row) in o.Rows)
                for (var c = 0; c < o.Cells; c++)
                {
                    float a = o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * c / o.Cells, b = o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * (c + 1) / o.Cells, g = o.GlassBottom(c, y0);
                    yield return (o.Column + c, row, o.Face.At((a + b) * 0.5f, -o.Depth, (g + y1) * 0.5f), -o.Face.T, Vector3.UnitY, b - a, y1 - g);
                }
    }

    // ------------------------------------------------------------------ build

    public void Build(CoreBuild build, LodLevel lod)
    {
        var mesh = build.Mesh;
        int? windowBase = build.WindowNumbering is { } numbering ? Allocation(numbering).BaseId : null;
        var glass = build.Glass(S.WindowGlass, windowBase, Columns, S.MuseumWindowChannel);
        var granite = new FacadePaints(S.Wall(S.MuseumGranite), S.Trim(S.MuseumTrim), S.Metal(S.MuseumFrame));
        var white = new FacadePaints(S.Wall(S.MuseumWhite), S.Trim(S.MuseumTrim), S.Metal(S.MuseumFrame));
        PlatformTop(mesh);
        GalleryWalls(mesh, lod);
        foreach (var o in _openings) LibraryFacade.Window(mesh, glass, o, lod, granite);
        RoofRing(mesh, lod);
        HallBody(mesh, glass, white, lod);
        Portico(mesh, lod);
        if (lod <= LodLevel.L2) Colonnade(mesh, lod);
        if (lod <= LodLevel.L1) Doors(build);
    }

    /// <summary>The colonnade floor: light granite from the gallery wall to a dark curb at the platform edge, its edge face to the ground.</summary>
    private void PlatformTop(CoreMesh mesh)
    {
        var curb = MuseumGeometry.Offset(Platform, Enumerable.Repeat(-S.MuseumCurb, Platform.Count).ToArray());
        Ring(mesh, S.Roof(S.MuseumFloor), Gallery, curb, FloorY, Vector3.UnitY);
        Ring(mesh, S.Roof(S.MuseumDark), curb, Platform, FloorY, Vector3.UnitY);
        var bottom = Frame.PadY - S.Sink;
        for (var k = 0; k < Platform.Count; k++)
        {
            var f = MuseumGeometry.FaceOf(Platform, k);
            LibraryFacade.Panel(mesh, S.Trim(S.MuseumDark), f, 0f, f.Length, 0f, bottom, FloorY, f.N);
        }
    }

    /// <summary>A flat band between two corresponding rings (same vertex count) at height y, one quad per side.</summary>
    private static void Ring(CoreMesh mesh, in Paint paint, IReadOnlyList<Vector2> inner, IReadOnlyList<Vector2> outer, float y, Vector3 normal)
    {
        for (var k = 0; k < inner.Count; k++)
        {
            var j = (k + 1) % inner.Count;
            mesh.Quad(paint, new(inner[k].X, y, inner[k].Y), new(inner[j].X, y, inner[j].Y), new(outer[j].X, y, outer[j].Y), new(outer[k].X, y, outer[k].Y), normal);
        }
    }

    /// <summary>Granite gallery walls up to the colonnade ceiling: grooves (L0, L1), plinth band (L0, L1), holes at L0 and L1.</summary>
    private void GalleryWalls(CoreMesh mesh, LodLevel lod)
    {
        var wall = S.Wall(S.MuseumGranite);
        var groove = S.Trim(S.MuseumGroove);
        var plinth = S.Trim(S.MuseumDark);
        for (var k = 0; k < Gallery.Count; k++)
        {
            var f = MuseumGeometry.FaceOf(Gallery, k);
            var holes = lod <= LodLevel.L1 ? _openings.Where(o => o.Face == f).Select(o => o.Hole).ToArray() : [];
            var grooves = lod <= LodLevel.L1 ? S.MuseumGrooves.Select(g => new Rect(0f, f.Length, FloorY + g - S.GrooveHeight * 0.5f, FloorY + g + S.GrooveHeight * 0.5f)).ToArray() : [];
            LibraryFacade.Wall(mesh, wall, f, 0f, f.Length, Frame.WallBaseY, SoffitY, [.. holes, .. grooves]);
            foreach (var g in grooves)
            {
                var mid = (g.Y0 + g.Y1) * 0.5f;
                foreach (var (a, b) in Spans(0f, f.Length, holes.Where(h => h.Y0 <= mid && h.Y1 >= mid)))
                {
                    LibraryFacade.Panel(mesh, groove, f, a, b, -S.GrooveDepth, g.Y0, g.Y1, f.N);
                    mesh.Quad(groove, f.At(a, 0f, g.Y1), f.At(b, 0f, g.Y1), f.At(b, -S.GrooveDepth, g.Y1), f.At(a, -S.GrooveDepth, g.Y1), -Vector3.UnitY);
                    mesh.Quad(groove, f.At(a, 0f, g.Y0), f.At(b, 0f, g.Y0), f.At(b, -S.GrooveDepth, g.Y0), f.At(a, -S.GrooveDepth, g.Y0), Vector3.UnitY);
                    if (a <= 1e-3f) mesh.Quad(groove, f.At(a, 0f, g.Y0), f.At(a, -S.GrooveDepth, g.Y0), f.At(a, -S.GrooveDepth, g.Y1), f.At(a, 0f, g.Y1), f.T);
                    if (b >= f.Length - 1e-3f) mesh.Quad(groove, f.At(b, 0f, g.Y0), f.At(b, -S.GrooveDepth, g.Y0), f.At(b, -S.GrooveDepth, g.Y1), f.At(b, 0f, g.Y1), -f.T);
                }
            }
            if (lod > LodLevel.L1) continue;
            // Plinth band (勒脚), broken where a bay reaches the floor (the doors, the lake glass).
            const float ext = 0.03f;
            var low = holes.Where(h => h.Y0 < Frame.PlinthTopY).ToArray();
            foreach (var (a, b) in Spans(-ext, f.Length + ext, low))
                LibraryFacade.Box(mesh, plinth, f, a, b, 0f, S.PlinthProject, Frame.WallBaseY, Frame.PlinthTopY);
        }
    }

    /// <summary>The parts of u0…u1 not covered by <paramref name="holes"/> (by their u range).</summary>
    private static IEnumerable<(float A, float B)> Spans(float u0, float u1, IEnumerable<Rect> holes)
    {
        var u = u0;
        foreach (var h in holes.OrderBy(h => h.U0))
        {
            if (h.U1 <= u) continue;
            if (h.U0 - u > 1e-3f) yield return (u, h.U0);
            u = MathF.Max(u, h.U1);
        }
        if (u1 - u > 1e-3f) yield return (u, u1);
    }

    /// <summary>
    /// The flat roof from the hall out to the eave, its white soffit over the colonnade, and the eave band: a white fascia
    /// rising <see cref="CoreStyle.MuseumUpstand"/> above the roof with a light coping (L0–L2; L3 and L4 the band only).
    /// </summary>
    private void RoofRing(CoreMesh mesh, LodLevel lod)
    {
        var inner = MuseumGeometry.Offset(Eave, Enumerable.Repeat(-S.MuseumFasciaThick, Eave.Count).ToArray());
        Flat(mesh, S.Roof(S.MuseumFlatRoof), inner, Hall, RoofY, Vector3.UnitY);
        Flat(mesh, S.Wall(S.MuseumWhite), inner, Gallery, SoffitY, -Vector3.UnitY);
        var top = RoofY + S.MuseumUpstand;
        var coping = lod <= LodLevel.L2 ? S.CopingHeight : 0f;
        Sweep(mesh, S.Wall(S.MuseumWhite), Eave, Box(-S.MuseumFasciaThick, 0f, top - coping - SoffitY), SoffitY);
        if (coping > 0f) Sweep(mesh, S.Trim(S.MuseumTrim), Eave, Coping(S.MuseumFasciaThick), top - coping);
    }

    /// <summary>A flat polygon between an outer ring and one hole ring at height y.</summary>
    private static void Flat(CoreMesh mesh, in Paint paint, IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> hole, float y, Vector3 normal)
    {
        var polygon = PolygonTriangulator.Triangulate(new PolygonRings(outer, [hole.Reverse().ToArray()]));
        var v = polygon.Vertices.Select(p => new Vector3(p.X, y, p.Y)).ToArray();
        for (var i = 0; i < polygon.Indices.Length; i += 3)
            mesh.Tri(paint, v[polygon.Indices[i]], v[polygon.Indices[i + 1]], v[polygon.Indices[i + 2]], normal);
    }

    private static void Sweep(CoreMesh mesh, in Paint paint, IReadOnlyList<Vector2> ring, SweepProfile profile, float baseY) =>
        LibraryFacade.Sweep(mesh, paint, [ring], [true], profile, baseY);

    /// <summary>A rectangle section from lateral x0 to x1 (+x outward), 0 to h up.</summary>
    private static SweepProfile Box(float x0, float x1, float h) => new([new(x0, 0), new(x1, 0), new(x1, h), new(x0, h)]);

    private static SweepProfile Coping(float thick)
    {
        float t = thick, c = S.CopingHeight, o = S.CopingOverhang, b = S.CopingBevel;
        return new([new(-t - o, 0), new(o, 0), new(o, c - b), new(o - b, c), new(-t - o + b, c), new(-t - o, c - b)]);
    }

    /// <summary>
    /// The white central hall: walls from the flat roof to the eaves with the clerestory ribbon (L0, L1 holes and frames;
    /// L2 on proud glass), a dark base band where it rises from the roof, the eave slab and the low pyramid roof.
    /// </summary>
    private void HallBody(CoreMesh mesh, GlassSink glass, in FacadePaints paints, LodLevel lod)
    {
        var wall = paints.Wall;
        for (var k = 0; k < Hall.Count; k++)
        {
            var f = MuseumGeometry.FaceOf(Hall, k);
            var holes = lod <= LodLevel.L1 ? _clerestory.Where(o => o.Face == f).Select(o => o.Hole).ToArray() : [];
            LibraryFacade.Wall(mesh, wall, f, 0f, f.Length, RoofY - 0.02f, HallEavesY, holes);
            if (lod <= LodLevel.L1)
                LibraryFacade.Box(mesh, S.Trim(S.MuseumDark), f, -0.03f, f.Length + 0.03f, 0f, S.PlinthProject, RoofY - 0.02f, RoofY + S.MuseumHallBase);
        }
        foreach (var o in _clerestory) LibraryFacade.Window(mesh, glass, o, lod, paints);
        var sheet = S.Sheet(S.MuseumRoof);
        Sweep(mesh, sheet, Hall, Box(-0.3f, S.MuseumHallEaveOut, S.MuseumHallEaveThick), HallEavesY);
        // Pyramid faces from the eave slab's outer top edge to the small flat top (planar trapezoids: each top edge is its
        // eave edge scaled about the hall's centre).
        var eave = MuseumGeometry.Offset(Hall, Enumerable.Repeat(S.MuseumHallEaveOut, Hall.Count).ToArray());
        var centre = Plan.Centroid([Hall]);
        float y0 = HallEavesY + S.MuseumHallEaveThick, y1 = y0 + S.MuseumRoofRise;
        var apex = eave.Select(p => centre + (p - centre) * S.MuseumApexScale).ToArray();
        for (var k = 0; k < eave.Count; k++)
        {
            var j = (k + 1) % eave.Count;
            Vector3 a = new(eave[k].X, y0, eave[k].Y), b = new(eave[j].X, y0, eave[j].Y), c = new(apex[j].X, y1, apex[j].Y), d = new(apex[k].X, y1, apex[k].Y);
            var n = Shapes.Normalize(Vector3.Cross(b - a, d - a));
            if (n.Y < 0) n = -n;
            mesh.Quad(sheet, a, b, c, d, n);
        }
        var cap = PolygonTriangulator.Triangulate(new PolygonRings(apex));
        for (var i = 0; i < cap.Indices.Length; i += 3)
        {
            Vector2 p = cap.Vertices[cap.Indices[i]], q = cap.Vertices[cap.Indices[i + 1]], r = cap.Vertices[cap.Indices[i + 2]];
            mesh.Tri(sheet, new(p.X, y1, p.Y), new(q.X, y1, q.Y), new(r.X, y1, r.Y), Vector3.UnitY);
        }
    }

    /// <summary>
    /// The portico's head block (门头) over the south colonnade bay: white, from just under the colonnade ceiling to
    /// <see cref="CoreStyle.PorticoTop"/>, back on the gallery wall and standing proud of the eave band, with a recessed granite
    /// name field on its face (the calligraphy waits for the scene text atlas) and a coping.
    /// </summary>
    private void Portico(CoreMesh mesh, LodLevel lod)
    {
        var f = GalleryFace(South);
        var u = f.Length * 0.5f;
        float u0 = u - S.PorticoHalf, u1 = u + S.PorticoHalf;
        var front = EaveDistance(South) + S.PorticoProject;
        float y0 = SoffitY - 0.03f, y1 = FloorY + S.PorticoTop;
        var white = S.Wall(S.MuseumWhite);
        mesh.Quad(white, f.At(u0, 0f, y1), f.At(u1, 0f, y1), f.At(u1, front, y1), f.At(u0, front, y1), Vector3.UnitY);
        mesh.Quad(white, f.At(u0, 0f, y0), f.At(u1, 0f, y0), f.At(u1, front, y0), f.At(u0, front, y0), -Vector3.UnitY);
        mesh.Quad(white, f.At(u0, 0f, y0), f.At(u0, front, y0), f.At(u0, front, y1), f.At(u0, 0f, y1), -f.T);
        mesh.Quad(white, f.At(u1, 0f, y0), f.At(u1, front, y0), f.At(u1, front, y1), f.At(u1, 0f, y1), f.T);
        LibraryFacade.Panel(mesh, white, f, u0, u1, 0f, RoofY, y1, -f.N);
        var face = new Face(f.A + f.Out * front, f.Along, f.Out, f.Length);
        if (lod <= LodLevel.L1)
        {
            var field = new Rect(u0 + S.NameSideMargin, u1 - S.NameSideMargin, y0 + S.NameMargin, y1 - S.NameMargin);
            LibraryFacade.Wall(mesh, white, face, u0, u1, y0, y1, [field]);
            var d = -S.NameDepth;
            LibraryFacade.Panel(mesh, S.Wall(S.MuseumNameField), face, field.U0, field.U1, d, field.Y0, field.Y1, f.N);
            mesh.Quad(white, face.At(field.U0, 0f, field.Y0), face.At(field.U0, d, field.Y0), face.At(field.U0, d, field.Y1), face.At(field.U0, 0f, field.Y1), f.T);
            mesh.Quad(white, face.At(field.U1, 0f, field.Y0), face.At(field.U1, d, field.Y0), face.At(field.U1, d, field.Y1), face.At(field.U1, 0f, field.Y1), -f.T);
            mesh.Quad(white, face.At(field.U0, 0f, field.Y1), face.At(field.U1, 0f, field.Y1), face.At(field.U1, d, field.Y1), face.At(field.U0, d, field.Y1), -Vector3.UnitY);
            mesh.Quad(white, face.At(field.U0, 0f, field.Y0), face.At(field.U1, 0f, field.Y0), face.At(field.U1, d, field.Y0), face.At(field.U0, d, field.Y0), Vector3.UnitY);
        }
        else
            LibraryFacade.Panel(mesh, white, face, u0, u1, 0f, y0, y1, f.N);
        if (lod > LodLevel.L2) return;
        const float o = 0.05f;
        var trim = S.Trim(S.MuseumTrim);
        var c = S.CopingHeight;
        LibraryFacade.Box(mesh, trim, f, u0 - o, u1 + o, -o, front + o, y1, y1 + c);
        LibraryFacade.Panel(mesh, trim, f, u0 - o, u1 + o, -o, y1, y1 + c, -f.N);
    }

    /// <summary>Distance out from the gallery wall line of body edge <paramref name="i"/> to the eave's outer face.</summary>
    private float EaveDistance(int i)
    {
        var f = GalleryFace(i);
        var e = MuseumGeometry.FaceOf(Eave, 2 * i + 1);
        return Vector2.Dot(e.A - f.A, f.Out);
    }

    // ------------------------------------------------------------------ colonnade

    /// <summary>
    /// Column positions on the column line: a column on every corner (squared to the corner's bisector) and bays of at
    /// most <see cref="CoreStyle.ColumnPitch"/> between; on the south edge the four portico columns replace the bays in front
    /// of the door. Each entry: centre, facing direction, size, base size, portico.
    /// </summary>
    public IEnumerable<(Vector2 C, Vector2 Along, float Size, float Base)> ColumnPositions()
    {
        var n = ColumnLine.Count;
        for (var i = 0; i < n; i++)
        {
            Vector2 a = ColumnLine[i], b = ColumnLine[(i + 1) % n], p = ColumnLine[(i + n - 1) % n];
            var length = Vector2.Distance(a, b);
            var along = (b - a) / length;
            var bisector = Vector2.Normalize(along + Vector2.Normalize(a - p));
            yield return (a, bisector, S.ColumnSize, S.ColumnBase);
            if (i != South)
            {
                var bays = Math.Max(1, (int)MathF.Ceiling(length / S.ColumnPitch));
                for (var k = 1; k < bays; k++) yield return (a + along * (length * k / bays), along, S.ColumnSize, S.ColumnBase);
                continue;
            }
            // The door axis: the gallery face's middle projected onto this edge.
            var f = GalleryFace(i);
            var mid = new Vector2(f.At(f.Length * 0.5f, 0, 0).X, f.At(f.Length * 0.5f, 0, 0).Z);
            var axis = Vector2.Dot(mid - a, along);
            float left = axis - S.PorticoOuter, right = axis + S.PorticoOuter;
            var bl = Math.Max(1, (int)MathF.Ceiling(left / S.ColumnPitch));
            for (var k = 1; k < bl; k++) yield return (a + along * (left * k / bl), along, S.ColumnSize, S.ColumnBase);
            foreach (var w in new[] { -S.PorticoOuter, -S.PorticoInner, S.PorticoInner, S.PorticoOuter })
                yield return (a + along * (axis + w), along, S.PorticoColumn, S.PorticoColumnBase);
            var br = Math.Max(1, (int)MathF.Ceiling((length - right) / S.ColumnPitch));
            for (var k = 1; k < br; k++) yield return (a + along * (right + (length - right) * k / br), along, S.ColumnSize, S.ColumnBase);
        }
    }

    /// <summary>Square columns from the floor to the colonnade ceiling: L0 base, chamfered shaft and capital; L1 base and shaft; L2 the shaft.</summary>
    private void Colonnade(CoreMesh mesh, LodLevel lod)
    {
        var white = S.Wall(S.MuseumWhite);
        var dark = S.Trim(S.MuseumDark);
        var spec = S.Spec(CoreMaterial.Wall, S.MuseumWhite);
        float baseH = S.ColumnBaseHeight, capH = S.ColumnCapHeight;
        foreach (var (c, along, size, plinth) in ColumnPositions())
        {
            Matrix4x4 At(float y) => Shapes.Facing(new(c.X, y, c.Y), along);
            var shaftY = lod <= LodLevel.L1 ? FloorY + baseH - 0.01f : FloorY - 0.05f;
            var shaftTop = lod == LodLevel.L0 ? SoffitY - capH + 0.01f : SoffitY + 0.02f;
            var bevel = lod == LodLevel.L0 ? S.ColumnBevel : 0f;
            mesh.Add(white, PartGeometryKernel.BeveledBox(size, shaftTop - shaftY, size, bevel, spec), At(shaftY));
            if (lod <= LodLevel.L1)
                mesh.Add(dark, PartGeometryKernel.BeveledBox(plinth, baseH + 0.05f, plinth, lod == LodLevel.L0 ? 0.02f : 0f, spec), At(FloorY - 0.05f));
            if (lod == LodLevel.L0)
            {
                var cap = size + (S.ColumnCap - S.ColumnSize);
                mesh.Add(S.Trim(S.MuseumTrim), PartGeometryKernel.BeveledBox(cap, capH + 0.02f, cap, 0.02f, spec), At(SoffitY - capH));
            }
        }
    }

    // ------------------------------------------------------------------ doors

    /// <summary>WP-10 glass doors in the two bays: the main door's inner two leaves and both lake door leaves fixed open at 90°.</summary>
    private void Doors(CoreBuild build)
    {
        foreach (var (o, leaves) in new[] { (MainDoor, S.MuseumDoorLeaves), (LakeDoor, S.LakeDoorLeaves) })
        {
            var (a, b) = DoorSpan(o);
            var origin = o.Face.At((a + b) * 0.5f, -o.Depth, o.Hole.Y0);
            build.Parts.Add(new PlacedPart(new PartId(PartCatalog.DoorGlass),
                new DoorParams(b - a, o.Door!.Value.Top - o.Hole.Y0, S.DoorDepth, S.DoorOpenDeg, leaves) { Foot = FootMat.Glass },
                OpeningPlacement.Frame(origin, o.Face.N)));
        }
    }

    // ------------------------------------------------------------------ collision

    /// <summary>
    /// Gallery walls from their foot to 300 m (the roofs are out of reach, MAP-QSH-CORE), open at the two door bays
    /// (jambs, floor, the glass plane and fixed leaves beside the open leaves, lintel); each column from the floor to the
    /// ceiling; the colonnade floor as stone slabs round the ring.
    /// </summary>
    public IReadOnlyList<Collider> Collide()
    {
        var list = new List<Collider>();
        var top = Frame.PadY + WalkRules.BuildingCollisionHeight;
        var doors = new[] { (MainDoor, S.MuseumDoorLeaves), (LakeDoor, S.LakeDoorLeaves) };
        for (var k = 0; k < Gallery.Count; k++)
        {
            var f = MuseumGeometry.FaceOf(Gallery, k);
            var bays = doors.Where(d => d.Item1.Face == f).ToArray();
            foreach (var (a, b) in Spans(0f, f.Length, bays.Select(d => d.Item1.Hole)))
                list.Add(Wall(f.At(a, 0, 0), f.At(b, 0, 0), Frame.WallBaseY, top));
            foreach (var (o, leaves) in bays) list.AddRange(DoorBay(o, leaves, top));
        }
        foreach (var (c, along, size, _) in ColumnPositions())
            list.Add(new WallCollider(c - along * (size * 0.5f), c + along * (size * 0.5f), size * 0.5f, FloorY - 0.1f, SoffitY, SurfaceTag.Wall) { Foot = FootMat.StonePaving });
        for (var k = 0; k < Gallery.Count; k++)
        {
            var j = (k + 1) % Gallery.Count;
            list.Add(new SlabCollider([Gallery[k], Gallery[j], Platform[j], Platform[k]], FloorY, 0.3f, SurfaceTag.Ground) { Foot = FootMat.StonePaving });
        }
        return list;
    }

    private static WallCollider Wall(Vector3 a, Vector3 b, float y0, float y1) =>
        new(new(a.X, a.Z), new(b.X, b.Z), 0.15f, y0, y1, SurfaceTag.Wall) { Foot = FootMat.StonePaving };

    private static IEnumerable<Collider> DoorBay(Opening o, int leaves, float top)
    {
        var f = o.Face;
        var h = o.Hole;
        float s = -o.Depth, y = h.Y0, doorTop = o.Door!.Value.Top;
        var (d0, d1) = DoorSpan(o);
        var fixedLeaf = leaves > 2 ? (d1 - d0) / leaves : 0f;
        Vector2 P(float u, float at) { var p = f.At(u, at, 0f); return new(p.X, p.Z); }
        WallCollider W(Vector2 a, Vector2 b, float y0) => new(a, b, 0.15f, y0, top, SurfaceTag.Wall) { Foot = FootMat.StonePaving };
        yield return new SlabCollider([P(h.U0, 0f), P(h.U1, 0f), P(h.U1, s), P(h.U0, s)], y, 0.3f, SurfaceTag.Ground) { Foot = FootMat.StonePaving };
        yield return W(P(h.U0, 0f), P(h.U0, s), y);
        yield return W(P(h.U1, 0f), P(h.U1, s), y);
        if (d0 + fixedLeaf - h.U0 > 0.05f) yield return W(P(h.U0, s), P(d0 + fixedLeaf, s), y);
        if (h.U1 - (d1 - fixedLeaf) > 0.05f) yield return W(P(d1 - fixedLeaf, s), P(h.U1, s), y);
        yield return W(P(d0 + fixedLeaf, s), P(d1 - fixedLeaf, s), doorTop);
    }
}
