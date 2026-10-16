using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using S = Campus.Gen.Zones.Core.CoreStyle;

namespace Campus.Gen.Zones.Core.Annex;

/// <summary>
/// One of the library's north blocks (16e, MAP-QSH-CORE §1.1, WLD-INTERIORS §17: closed, shell plus window cards). The
/// blocks continue the library's own language so the group reads as one building: beige stone walls on the library's
/// floor lines, the light stone plinth band and a string course at every floor line, parapets with the same chamfered
/// coping, reveals and sills of the library's openings. The court blocks (8406, 8408) and the north-west link (8409,
/// 8410) carry the library's ribbon windows; the north annex (8403, offices and stacks) has punched windows stacked on one
/// bay grid, rainwater pipes, roof units and one closed glass door with a stone canopy on its long street face. The low
/// block's roof is the 3F G terrace (MAP-QSH-LIBRARY §9), paved like the library's 2F terrace.
/// <para>Windows: one grid <see cref="WindowKey"/> per block, <see cref="Columns"/> × storeys, on the library's channel.</para>
/// </summary>
public sealed class AnnexShell : ICorePiece, IWindowedPiece
{
    public const string WindowKey = "annex-shell";

    private readonly IReadOnlyList<WallPiece> _pieces;
    private readonly IReadOnlyList<Opening> _openings;
    private readonly Block _top;
    private Surface _ground = (_, _) => 0f;

    private AnnexShell(FeatureId feature, AnnexKind kind, BuildingFrame frame, IReadOnlyList<WallPiece> pieces)
    {
        Feature = feature;
        Kind = kind;
        Frame = frame;
        _pieces = pieces;
        _top = pieces.Select(p => p.Block).First();
        Rows = frame.StoreyHeights.Count;
        var next = new int[Rows];
        int Allocate(int cells, int row) { var c = next[row]; next[row] += cells; return c; }
        var door = kind == AnnexKind.NorthAnnex ? DoorPiece(pieces, frame) : null;
        var openings = new List<Opening>();
        foreach (var piece in pieces) openings.AddRange(Layout(piece, ReferenceEquals(piece, door), Allocate));
        _openings = openings;
        Columns = Math.Max(1, next.Max());
    }

    public static IEnumerable<AnnexShell> From(IWorldModel world, AnnexSite? site)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (site is null) yield break;
        foreach (var (feature, kind) in AnnexSite.Members)
        {
            if (!site.Annexes.TryGetValue(feature.Value, out var entry)) continue;
            var pieces = site.PiecesOf(feature.Value).ToArray();
            if (pieces.Length == 0) continue;
            yield return new AnnexShell(feature, kind, entry.Frame, pieces) { _ground = world.Terrain.HeightAt };
        }
    }

    public FeatureId Feature { get; }
    public AnnexKind Kind { get; }
    public BuildingFrame Frame { get; }
    public int Rows { get; }
    public int Columns { get; }
    public IReadOnlyList<WallPiece> Pieces => _pieces;
    public IReadOnlyList<Opening> Openings => _openings;

    public string Id => "annex-" + Kind.ToString().ToLowerInvariant();
    public Vector2 Anchor => Frame.Centroid;
    public IReadOnlyList<Claim> Claims => [new FeatureClaim(Feature, Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    private float Top => _top.Y1;
    private bool Terrace => Kind == AnnexKind.LowBlock;

    private byte Card => Kind switch
    {
        AnnexKind.NorthAnnex => S.CardOffice,
        AnnexKind.CourtWing or AnnexKind.LowBlock => S.CardReading,
        AnnexKind.Link or AnnexKind.Joint => S.CardCorridor,
        _ => throw new InvalidOperationException($"Unknown annex kind {Kind}."),
    };

    /// <summary>The closed door bay of the north annex (门厅卡 behind its glass); null for the other blocks.</summary>
    public Opening? Door => _openings.FirstOrDefault(o => o.Kind == OpeningKind.Entry);

    // ------------------------------------------------------------------ layout

    /// <summary>The north annex's longest ground face: its door goes to the bay nearest the face's middle.</summary>
    private static WallPiece? DoorPiece(IReadOnlyList<WallPiece> pieces, BuildingFrame frame) =>
        pieces.Where(p => p.Y0 <= frame.WallBaseY + 0.01f && p.Y1 >= frame.StoreyY(1)).OrderByDescending(p => p.Length).FirstOrDefault();

    private IEnumerable<Opening> Layout(WallPiece piece, bool door, Func<int, int, int> allocate)
    {
        var f = Face.Of(piece);
        var length = piece.Length;
        if (length < S.MinWindowFace) yield break;
        for (var k = 0; k < Rows; k++)
        {
            var floor = Frame.StoreyY(k);
            if (floor < piece.Y0 - 0.05f) continue;
            if (Kind == AnnexKind.NorthAnnex)
            {
                float y0 = floor + (k == 0 ? S.AnnexGroundSill : S.AnnexSill), y1 = y0 + (k == 0 ? S.AnnexGroundHeight : S.AnnexWindowHeight);
                if (y1 > piece.Y1 - 0.3f) continue;
                var bays = (int)MathF.Floor((length - 2 * S.AnnexMargin) / S.AnnexBay);
                if (bays < 1) continue;
                var doorBay = door && k == 0 ? bays / 2 : -1;
                for (var i = 0; i < bays; i++)
                {
                    var u = length * 0.5f + (i - (bays - 1) * 0.5f) * S.AnnexBay;
                    if (i == doorBay)
                    {
                        var half = S.AnnexDoorWidth * 0.5f;
                        float top = floor + S.AnnexDoorHeight;
                        yield return new Opening(f, OpeningKind.Entry, new(u - half, u + half, floor, top), 2, [(floor, top, k)], allocate(2, k), Card)
                        { Door = (0, 2, floor + S.AnnexDoorTop) };
                        continue;
                    }
                    var w = S.AnnexWindow * 0.5f;
                    yield return new Opening(f, k == 0 ? OpeningKind.Podium : OpeningKind.Square, new(u - w, u + w, y0, y1), 2, [(y0, y1, k)], allocate(2, k), Card);
                }
                continue;
            }
            var ground = k == 0;
            float sill = ground ? S.PodiumSill : S.RibbonSill, height = ground ? S.PodiumHeight : S.RibbonHeight, module = ground ? S.PodiumModule : S.RibbonModule;
            float a = floor + sill, b = a + height;
            if (b > piece.Y1 - 0.3f || length < 2 * S.RibbonMargin + module) continue;
            var cells = Math.Max(1, (int)MathF.Round((length - 2 * S.RibbonMargin) / module));
            yield return new Opening(f, ground ? OpeningKind.Podium : OpeningKind.Ribbon, new(S.RibbonMargin, length - S.RibbonMargin, a, b), cells, [(a, b, k)], allocate(cells, k), Card);
        }
    }

    // ------------------------------------------------------------------ windows

    public ClaimedWindowLayout WindowLayout => new(Feature, [new WindowGridSpec(WindowKey, Columns, Rows)]);

    public WindowAllocation Allocation(WindowNumberingSnapshot numbering)
    {
        ArgumentNullException.ThrowIfNull(numbering);
        var allocation = numbering.Get(Feature, WindowKey);
        if (allocation.Columns != Columns || allocation.Rows != Rows)
            throw new InvalidOperationException($"{Feature.Value} window grid is {Columns} × {Rows}, the allocation {allocation.Columns} × {allocation.Rows}.");
        return allocation;
    }

    public IReadOnlyList<WindowDecl> Windows(WindowNumberingSnapshot numbering)
    {
        var allocation = Allocation(numbering);
        var list = new List<WindowDecl>();
        foreach (var o in _openings)
            foreach (var (y0, y1, row) in o.Rows)
                for (var c = 0; c < o.Cells; c++)
                {
                    float a = o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * c / o.Cells, b = o.Hole.U0 + (o.Hole.U1 - o.Hole.U0) * (c + 1) / o.Cells, g = o.GlassBottom(c, y0);
                    list.Add(new WindowDecl(allocation.IdAt(o.Column + c, row), allocation.BaseId, Columns, S.LibraryWindowChannel,
                        o.Face.At((a + b) * 0.5f, -o.Depth, (g + y1) * 0.5f), -o.Face.T, Vector3.UnitY, b - a, y1 - g));
                }
        return list;
    }

    // ------------------------------------------------------------------ build

    public void Build(CoreBuild build, LodLevel lod)
    {
        var mesh = build.Mesh;
        int? windowBase = build.WindowNumbering is { } numbering ? Allocation(numbering).BaseId : null;
        var glass = build.Glass(S.WindowGlass, windowBase, Columns, S.LibraryWindowChannel);
        var wall = S.Wall(S.LibraryWall);
        var byFace = _openings.ToLookup(o => o.Face);
        foreach (var piece in _pieces)
        {
            var f = Face.Of(piece);
            LibraryFacade.Wall(mesh, wall, f, 0f, piece.Length, piece.Y0, piece.Y1, lod <= LodLevel.L1 ? byFace[f].Select(o => o.Hole).ToArray() : []);
        }
        foreach (var o in _openings) LibraryFacade.Window(mesh, glass, o, lod);
        if (lod <= LodLevel.L1) Bands(mesh);
        Tops(mesh);
        Cap(mesh);
        if (Door is { } door) DoorBay(build, door, lod);
        if (Kind == AnnexKind.NorthAnnex && lod <= LodLevel.L2) Pilasters(mesh);
        if (Kind == AnnexKind.NorthAnnex && lod <= LodLevel.L1)
        {
            Pipes(mesh);
            RoofUnits(build);
        }
    }

    /// <summary>Plinth band on the ground pieces (broken at the door) and a string course at each floor line, as the library.</summary>
    private void Bands(CoreMesh mesh)
    {
        var trim = S.Trim(S.LibraryTrim);
        const float ext = 0.03f;
        foreach (var piece in _pieces)
        {
            var f = Face.Of(piece);
            if (piece.Y0 <= Frame.WallBaseY + 0.01f)
            {
                var u = -ext;
                foreach (var hole in _openings.Where(o => o.Door is not null && o.Face == f).Select(o => o.Hole).OrderBy(h => h.U0))
                {
                    LibraryFacade.Box(mesh, trim, f, u, hole.U0, 0f, S.PlinthProject, piece.Y0, Frame.PlinthTopY);
                    u = hole.U1;
                }
                LibraryFacade.Box(mesh, trim, f, u, piece.Length + ext, 0f, S.PlinthProject, piece.Y0, Frame.PlinthTopY);
            }
            for (var k = 1; k < Rows; k++)
            {
                var y = Frame.StoreyY(k);
                if (y < piece.Y0 + 0.3f || y > piece.Y1 - 0.3f) continue;
                LibraryFacade.Box(mesh, trim, f, -ext, piece.Length + ext, 0f, S.CourseProject, y - S.CourseHeight * 0.5f, y + S.CourseHeight * 0.5f);
            }
        }
    }

    /// <summary>Parapet with the library's chamfered coping along every top edge no other block stands against.</summary>
    private void Tops(CoreMesh mesh)
    {
        var tops = _pieces.Where(p => p.Top).Select(p => (p.A, p.B)).ToList();
        if (tops.Count == 0) return;
        var (chains, closed) = LibraryFacade.Chain(tops);
        float height = S.ParapetHeight, thick = S.ParapetThick;
        LibraryFacade.Sweep(mesh, S.Wall(S.LibraryWall), chains, closed, new([new(-thick, 0), new(0, 0), new(0, height - S.CopingHeight), new(-thick, height - S.CopingHeight)]), Top);
        float t = thick, c = S.CopingHeight, o = S.CopingOverhang, b = S.CopingBevel;
        LibraryFacade.Sweep(mesh, S.Trim(S.LibraryTrim), chains, closed,
            new([new(-t - o, 0), new(o, 0), new(o, c - b), new(o - b, c), new(-t - o + b, c), new(-t - o, c - b)]), Top + height - S.CopingHeight);
    }

    /// <summary>The roof: dark grey flat roof, or the low block's G terrace in the library terrace's light paving.</summary>
    private void Cap(CoreMesh mesh)
    {
        var paint = Terrace ? S.Roof(S.LibraryTerrace) : S.Roof(S.LibraryRoof);
        foreach (var block in _pieces.Select(p => p.Block).Distinct())
        {
            var polygon = PolygonTriangulator.Triangulate(new PolygonRings(block.Ring));
            var v = polygon.Vertices.Select(p => new Vector3(p.X, block.Y1, p.Y)).ToArray();
            for (var i = 0; i < polygon.Indices.Length; i += 3)
                mesh.Tri(paint, v[polygon.Indices[i]], v[polygon.Indices[i + 1]], v[polygon.Indices[i + 2]], Vector3.UnitY);
        }
    }

    /// <summary>The closed glass door (WP-10, leaves shut) in its bay, the granite step-free threshold and a stone canopy.</summary>
    private void DoorBay(CoreBuild build, Opening door, LodLevel lod)
    {
        var f = door.Face;
        var h = door.Hole;
        var trim = S.Trim(S.LibraryTrim);
        float mid = (h.U0 + h.U1) * 0.5f, cw = S.AnnexCanopyWidth * 0.5f, y = h.Y1 + S.AnnexCanopyGap;
        LibraryFacade.Box(build.Mesh, trim, f, mid - cw, mid + cw, 0f, S.AnnexCanopyDepth, y, y + S.AnnexCanopyThick);
        if (lod <= LodLevel.L2)
            LibraryFacade.Box(build.Mesh, S.Roof(S.LibraryTerrace), f, h.U0 - 0.4f, h.U1 + 0.4f, 0f, 1.4f, Frame.PadY - S.Sink, h.Y0 + 0.01f);
        if (lod > LodLevel.L1) return;
        var origin = f.At(mid, -door.Depth, h.Y0);
        build.Parts.Add(new PlacedPart(new PartId(PartCatalog.DoorGlass),
            new DoorParams(h.U1 - h.U0, door.Door!.Value.Top - h.Y0, S.DoorDepth) { Foot = FootMat.Glass },
            OpeningPlacement.Frame(origin, f.N)));
    }

    /// <summary>
    /// Stone pilasters every few bays on the full-height faces of the north annex: they cut its long street fronts into
    /// library-scaled bays (the ring's corner towers read the same way), the string courses dying into them.
    /// </summary>
    private void Pilasters(CoreMesh mesh)
    {
        var wall = S.Wall(S.LibraryWall);
        foreach (var piece in _pieces.Where(p => p.Y0 <= Frame.WallBaseY + 0.01f && p.Top))
        {
            var bays = (int)MathF.Floor((piece.Length - 2 * S.AnnexMargin) / S.AnnexBay);
            if (bays < 2 * S.AnnexPilasterEvery) continue;
            var f = Face.Of(piece);
            var door = _openings.FirstOrDefault(o => o.Door is not null && o.Face == f);
            var at = new List<float>();
            for (var i = S.AnnexPilasterEvery; i < bays; i += S.AnnexPilasterEvery) at.Add(piece.Length * 0.5f + (i - bays * 0.5f) * S.AnnexBay);
            // The door bay is framed by a pair of its own, so the entrance reads from down the street.
            if (door is not null)
            {
                var mid = (door.Hole.U0 + door.Hole.U1) * 0.5f;
                at.RemoveAll(u => MathF.Abs(u - mid) < S.AnnexBay * 1.4f);
                at.Add(mid - S.AnnexBay * 0.5f);
                at.Add(mid + S.AnnexBay * 0.5f);
            }
            float h = S.AnnexPilaster * 0.5f;
            foreach (var u in at.Order())
                LibraryFacade.Box(mesh, wall, f, u - h, u + h, 0f, S.AnnexPilasterProject, piece.Y0, Top + S.ParapetHeight - S.CopingHeight);
        }
    }

    /// <summary>Rainwater pipes at both ends of every long ground face, from the plinth to under the coping (Q5).</summary>
    private void Pipes(CoreMesh mesh)
    {
        var metal = S.Metal(S.LibraryFrame);
        foreach (var piece in _pieces.Where(p => p.Y0 <= Frame.WallBaseY + 0.01f && p.Top && p.Length >= S.PipeMinFace))
        {
            var f = Face.Of(piece);
            foreach (var u in new[] { S.PipeInset, piece.Length - S.PipeInset })
            {
                float a = u - S.PipeSize * 0.5f, b = u + S.PipeSize * 0.5f;
                LibraryFacade.Box(mesh, metal, f, a, b, S.PipeOff, S.PipeOff + S.PipeSize, Frame.PlinthTopY, Top + S.ParapetHeight - 0.35f);
                LibraryFacade.Box(mesh, metal, f, a - 0.03f, b + 0.03f, 0f, S.PipeOff + S.PipeSize + 0.03f, Top + S.ParapetHeight - 0.5f, Top + S.ParapetHeight - 0.3f);
            }
        }
    }

    /// <summary>A short row of roof units behind the longest parapet, seen from the 3F terrace (北辅楼屋顶设备).</summary>
    private void RoofUnits(CoreBuild build)
    {
        var face = _pieces.Where(p => p.Top).OrderByDescending(p => p.Length).FirstOrDefault();
        if (face is null) return;
        var f = Face.Of(face);
        for (var i = 0; i < S.AnnexRoofUnits; i++)
        {
            var u = face.Length * 0.5f + (i - (S.AnnexRoofUnits - 1) * 0.5f) * (S.AnnexUnitLength + 1.4f);
            var p = f.At(u, -4.5f, Top);
            build.Parts.Add(new PlacedPart(new PartId(PartCatalog.HeatPump), new EquipmentParams(S.AnnexUnitLength, S.AnnexUnitDepth, S.AnnexUnitHeight),
                Shapes.Frame(p, Vector3.UnitY, f.N)));
        }
    }

    // ------------------------------------------------------------------ collision

    /// <summary>
    /// Ground walls block to 300 m (roofs are out of reach, MAP-QSH-CORE §8.2); the closed door's bay is a DoorClosed wall.
    /// The low block's G terrace is walkable from the library's 3F: a stone slab, its parapet blocking to 1.5 m above it.
    /// </summary>
    public IReadOnlyList<Collider> Collide()
    {
        var list = new List<Collider>();
        var high = Frame.PadY + 300f;
        foreach (var piece in _pieces.Where(p => p.Y0 <= Frame.WallBaseY + 0.01f))
        {
            var f = Face.Of(piece);
            var door = _openings.FirstOrDefault(o => o.Door is not null && o.Face == f);
            if (door is null) { list.Add(Wall(piece.A, piece.B, piece.Y0, high, SurfaceTag.Wall)); continue; }
            Vector2 P(float u) { var p = f.At(u, 0f, 0f); return new(p.X, p.Z); }
            list.Add(Wall(piece.A, P(door.Hole.U0), piece.Y0, high, SurfaceTag.Wall));
            list.Add(Wall(P(door.Hole.U0), P(door.Hole.U1), piece.Y0, high, SurfaceTag.DoorClosed));
            list.Add(Wall(P(door.Hole.U1), piece.B, piece.Y0, high, SurfaceTag.Wall));
        }
        // Walls rising from the G terrace (the low block's roof is the 3F floor line) keep the terrace closed.
        if (!Terrace && Rows > 2)
            foreach (var piece in _pieces.Where(p => MathF.Abs(p.Y0 - Frame.StoreyY(2)) < 0.05f))
                list.Add(Wall(piece.A, piece.B, piece.Y0, high, SurfaceTag.Wall));
        if (Terrace)
        {
            foreach (var block in _pieces.Select(p => p.Block).Distinct())
                list.Add(new SlabCollider([.. block.Ring], Top, 0.3f, SurfaceTag.Ground) { Foot = FootMat.StonePaving });
            foreach (var piece in _pieces.Where(p => p.Top))
                list.Add(Wall(piece.A, piece.B, Top, Top + 1.5f, SurfaceTag.Barrier));
        }
        return list;
    }

    private static WallCollider Wall(Vector2 a, Vector2 b, float y0, float y1, SurfaceTag tag) => new(a, b, 0.15f, y0, y1, tag) { Foot = FootMat.StonePaving };
}
