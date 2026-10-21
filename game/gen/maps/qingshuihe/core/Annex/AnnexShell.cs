using Campus.Gen.Geometry;
using Campus.Gen.Sites;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using S = Campus.Gen.Maps.Qingshuihe.Core.CoreStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.Annex;

/// <summary>
/// One of the library's north blocks (16e, MAP-QSH-CORE, WLD-INTERIORS: closed, shell plus window cards). The
/// blocks continue the library's own language so the group reads as one building: beige stone walls on the library's
/// floor lines, the light stone plinth band and a string course at every floor line, parapets with the same chamfered
/// coping, reveals and sills of the library's openings. The court blocks (8406, 8408) and the north-west link (8409,
/// 8410) carry the library's ribbon windows; the north annex (8403, offices and stacks) has punched windows stacked on one
/// bay grid, rainwater pipes, roof units and one closed glass door with a stone canopy on its long street face. The low
/// block's roof is the 3F G terrace (MAP-QSH-INTERIORS-LIBRARY), paved like the library's 2F terrace.
/// <para>Windows: one grid <see cref="WindowKey"/> per block, <see cref="Columns"/> × storeys, on the library's channel.</para>
/// </summary>
public sealed class AnnexShell : ZoneSite, IWindowedPiece
{
    public const string WindowKey = "annex-shell";

    private readonly IReadOnlyList<WallPiece> _pieces;
    private readonly IReadOnlyList<Opening> _openings;
    private readonly Block _top;
    private readonly WallPiece? _roofUnitsFace;

    private AnnexShell(FeatureId feature, AnnexKind kind, BuildingFrame frame, IReadOnlyList<WallPiece> pieces, WallPiece? door, WallPiece? roofUnitsFace)
    {
        Feature = feature;
        Kind = kind;
        Frame = frame;
        _pieces = pieces;
        _roofUnitsFace = roofUnitsFace;
        _top = pieces.Select(p => p.Block).First();
        Rows = frame.StoreyHeights.Count;
        var next = new int[Rows];
        int Allocate(int cells, int row) { var c = next[row]; next[row] += cells; return c; }
        var openings = new List<Opening>();
        foreach (var piece in pieces) openings.AddRange(Layout(piece, ReferenceEquals(piece, door), Allocate));
        _openings = openings;
        Columns = Math.Max(1, next.Max());
    }

    public static IEnumerable<AnnexShell> From(IWorldModel world, AnnexSite? site, Func<FeatureId, bool>? includes = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (site is null) yield break;
        foreach (var (feature, kind) in AnnexSite.Members)
        {
            if (includes is not null && !includes(feature)) continue;
            if (!site.Annexes.TryGetValue(feature.Value, out var entry)) continue;
            var pieces = site.PiecesOf(feature.Value).ToArray();
            if (pieces.Length == 0) continue;
            yield return new AnnexShell(feature, kind, entry.Frame, pieces, kind == AnnexKind.NorthAnnex
                ? CoreContours.Wall(world, CoreIds.AnnexDoorFace, pieces.Where(p => p.Y0 <= entry.Frame.WallBaseY + 0.01f && p.Y1 >= entry.Frame.StoreyY(1))) : null,
                kind == AnnexKind.NorthAnnex ? CoreContours.Wall(world, CoreIds.AnnexDoorFace, pieces.Where(p => p.Top)) : null);
        }
    }

    public FeatureId Feature { get; }
    public AnnexKind Kind { get; }
    public BuildingFrame Frame { get; }
    public int Rows { get; }
    public int Columns { get; }
    public IReadOnlyList<WallPiece> Pieces => _pieces;
    public IReadOnlyList<Opening> Openings => _openings;

    public override string Id => "annex-" + Kind.ToString().ToLowerInvariant();
    public Vector2 Anchor => Frame.Centroid;
    public static IReadOnlyList<Claim> DeclaredClaims { get; } = AnnexSite.Members
        .Select(member => (Claim)new FeatureClaim(member.Feature, Aspects.Massing | Aspects.Facade | Aspects.Roof)).ToArray();

    public override IReadOnlyList<Claim> Claims => [new FeatureClaim(Feature, Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    private float Top => _top.Y1;
    private bool Terrace => Kind == AnnexKind.LowBlock;
    private IEnumerable<Block> RoofBlocks => _pieces.Select(piece => piece.Block).Distinct();

    public override IReadOnlyList<OutdoorAccessArea> DescribeOutdoorAccess(BuildContext context) => Terrace
        ? [new("library.terrace-g", Feature, RoofBlocks.Select(block =>
            new StoreyInfo(Rows + 1, block.Y1, Frame.StoreyHeights[^1], block.Ring)).ToArray())] : [];

    private byte Card => Kind switch
    {
        AnnexKind.NorthAnnex => AnnexStyle.CardOffice,
        AnnexKind.CourtWing or AnnexKind.LowBlock => LibraryStyle.CardReading,
        AnnexKind.Link or AnnexKind.Joint => AnnexStyle.CardCorridor,
        _ => throw new InvalidOperationException($"Unknown annex kind {Kind}."),
    };

    /// <summary>The closed door bay of the north annex (门厅卡 behind its glass); null for the other blocks.</summary>
    public Opening? Door => _openings.FirstOrDefault(o => o.Kind == OpeningKind.Entry);

    // ------------------------------------------------------------------ layout

    /// <summary>The north annex's longest ground face: its door goes to the bay nearest the face's middle.</summary>

    private IEnumerable<Opening> Layout(WallPiece piece, bool door, Func<int, int, int> allocate)
    {
        var f = Face.Of(piece);
        var length = piece.Length;
        if (length < LibraryStyle.MinWindowFace) yield break;
        for (var k = 0; k < Rows; k++)
        {
            var floor = Frame.StoreyY(k);
            if (floor < piece.Y0 - 0.05f) continue;
            if (Kind == AnnexKind.NorthAnnex)
            {
                float y0 = floor + (k == 0 ? AnnexStyle.AnnexGroundSill : AnnexStyle.AnnexSill), y1 = y0 + (k == 0 ? AnnexStyle.AnnexGroundHeight : AnnexStyle.AnnexWindowHeight);
                if (y1 > piece.Y1 - 0.3f) continue;
                var bays = (int)MathF.Floor((length - 2 * AnnexStyle.AnnexMargin) / AnnexStyle.AnnexBay);
                if (bays < 1) continue;
                var doorBay = door && k == 0 ? bays / 2 : -1;
                for (var i = 0; i < bays; i++)
                {
                    var u = length * 0.5f + (i - (bays - 1) * 0.5f) * AnnexStyle.AnnexBay;
                    if (i == doorBay)
                    {
                        var half = AnnexStyle.AnnexDoorWidth * 0.5f;
                        float top = floor + AnnexStyle.AnnexDoorHeight;
                        yield return new Opening(f, OpeningKind.Entry, new(u - half, u + half, floor, top), 2, [(floor, top, k)], allocate(2, k), Card)
                        { Door = (0, 2, floor + AnnexStyle.AnnexDoorTop) };
                        continue;
                    }
                    var w = AnnexStyle.AnnexWindow * 0.5f;
                    yield return new Opening(f, k == 0 ? OpeningKind.Podium : OpeningKind.Square, new(u - w, u + w, y0, y1), 2, [(y0, y1, k)], allocate(2, k), Card);
                }
                continue;
            }
            var ground = k == 0;
            float sill = ground ? LibraryStyle.PodiumSill : LibraryStyle.RibbonSill, height = ground ? LibraryStyle.PodiumHeight : LibraryStyle.RibbonHeight, module = ground ? LibraryStyle.PodiumModule : LibraryStyle.RibbonModule;
            float a = floor + sill, b = a + height;
            if (b > piece.Y1 - 0.3f || length < 2 * LibraryStyle.RibbonMargin + module) continue;
            var cells = Math.Max(1, (int)MathF.Round((length - 2 * LibraryStyle.RibbonMargin) / module));
            yield return new Opening(f, ground ? OpeningKind.Podium : OpeningKind.Ribbon, new(LibraryStyle.RibbonMargin, length - LibraryStyle.RibbonMargin, a, b), cells, [(a, b, k)], allocate(cells, k), Card);
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
                    list.Add(new WindowDecl(allocation.IdAt(o.Column + c, row), allocation.BaseId, Columns, LibraryStyle.LibraryWindowChannel,
                        o.Face.At((a + b) * 0.5f, -o.Depth, (g + y1) * 0.5f), -o.Face.T, Vector3.UnitY, b - a, y1 - g));
                }
        return list;
    }

    // ------------------------------------------------------------------ build

    public void Build(CoreBuild build, LodLevel lod)
    {
        var mesh = build.Mesh;
        int? windowBase = build.WindowNumbering is { } numbering ? Allocation(numbering).BaseId : null;
        var glass = build.Glass(S.WindowGlass, windowBase, Columns, LibraryStyle.LibraryWindowChannel);
        var wall = S.Wall(LibraryStyle.LibraryWall);
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
    private void Bands(GeometrySink mesh)
    {
        var trim = S.Trim(LibraryStyle.LibraryTrim);
        const float ext = 0.03f;
        foreach (var piece in _pieces)
        {
            var f = Face.Of(piece);
            if (piece.Y0 <= Frame.WallBaseY + 0.01f)
            {
                var u = -ext;
                foreach (var hole in _openings.Where(o => o.Door is not null && o.Face == f).Select(o => o.Hole).OrderBy(h => h.U0))
                {
                    LibraryFacade.Box(mesh, trim, f, u, hole.U0, 0f, LibraryStyle.PlinthProject, piece.Y0, Frame.PlinthTopY);
                    u = hole.U1;
                }
                LibraryFacade.Box(mesh, trim, f, u, piece.Length + ext, 0f, LibraryStyle.PlinthProject, piece.Y0, Frame.PlinthTopY);
            }
            for (var k = 1; k < Rows; k++)
            {
                var y = Frame.StoreyY(k);
                if (y < piece.Y0 + 0.3f || y > piece.Y1 - 0.3f) continue;
                LibraryFacade.Box(mesh, trim, f, -ext, piece.Length + ext, 0f, LibraryStyle.CourseProject, y - LibraryStyle.CourseHeight * 0.5f, y + LibraryStyle.CourseHeight * 0.5f);
            }
        }
    }

    /// <summary>Parapet with the library's chamfered coping along every top edge no other block stands against.</summary>
    private void Tops(GeometrySink mesh)
    {
        var tops = _pieces.Where(p => p.Top).Select(p => (p.A, p.B)).ToList();
        if (tops.Count == 0) return;
        var (chains, closed) = LibraryFacade.Chain(tops);
        float height = LibraryStyle.ParapetHeight, thick = LibraryStyle.ParapetThick;
        LibraryFacade.Sweep(mesh, S.Wall(LibraryStyle.LibraryWall), chains, closed, new([new(-thick, 0), new(0, 0), new(0, height - LibraryStyle.CopingHeight), new(-thick, height - LibraryStyle.CopingHeight)]), Top);
        float t = thick, c = LibraryStyle.CopingHeight, o = LibraryStyle.CopingOverhang, b = LibraryStyle.CopingBevel;
        LibraryFacade.Sweep(mesh, S.Trim(LibraryStyle.LibraryTrim), chains, closed,
            new([new(-t - o, 0), new(o, 0), new(o, c - b), new(o - b, c), new(-t - o + b, c), new(-t - o, c - b)]), Top + height - LibraryStyle.CopingHeight);
    }

    /// <summary>The roof: dark grey flat roof, or the low block's G terrace in the library terrace's light paving.</summary>
    private void Cap(GeometrySink mesh)
    {
        var paint = Terrace ? S.Roof(LibraryStyle.LibraryTerrace) : S.Roof(LibraryStyle.LibraryRoof);
        foreach (var block in RoofBlocks)
        {
            var polygon = PolygonTriangulator.Triangulate(new PolygonRings(block.Ring));
            var v = polygon.Vertices.Select(p => new Vector3(p.X, block.Y1, p.Y)).ToArray();
            for (var i = 0; i < polygon.Indices.Length; i += 3)
                mesh.Tri(paint, v[polygon.Indices[i]], v[polygon.Indices[i + 1]], v[polygon.Indices[i + 2]], Vector3.UnitY);
        }
    }

    /// <summary>The closed glass door (game/gen/parts/openings/, leaves shut) in its bay, the granite step-free threshold and a stone canopy.</summary>
    private void DoorBay(CoreBuild build, Opening door, LodLevel lod)
    {
        var f = door.Face;
        var h = door.Hole;
        var trim = S.Trim(LibraryStyle.LibraryTrim);
        float mid = (h.U0 + h.U1) * 0.5f, cw = AnnexStyle.AnnexCanopyWidth * 0.5f, y = h.Y1 + AnnexStyle.AnnexCanopyGap;
        LibraryFacade.Box(build.Mesh, trim, f, mid - cw, mid + cw, 0f, AnnexStyle.AnnexCanopyDepth, y, y + AnnexStyle.AnnexCanopyThick);
        if (lod <= LodLevel.L2)
            LibraryFacade.Box(build.Mesh, S.Roof(LibraryStyle.LibraryTerrace), f, h.U0 - 0.4f, h.U1 + 0.4f, 0f, 1.4f, Frame.PadY - S.Sink, h.Y0 + 0.01f);
        if (lod > LodLevel.L1) return;
        var origin = f.At(mid, -door.Depth, h.Y0);
        build.Parts.Add(new PlacedPart(new PartId(PartCatalog.DoorGlass),
            new DoorParams(h.U1 - h.U0, door.Door!.Value.Top - h.Y0, EntranceStyle.DoorDepth) { Foot = FootMat.Glass },
            OpeningPlacement.Frame(origin, f.N)));
    }

    /// <summary>
    /// Stone pilasters every few bays on the full-height faces of the north annex: they cut its long street fronts into
    /// library-scaled bays (the ring's corner towers read the same way), the string courses dying into them.
    /// </summary>
    private void Pilasters(GeometrySink mesh)
    {
        var wall = S.Wall(LibraryStyle.LibraryWall);
        foreach (var piece in _pieces.Where(p => p.Y0 <= Frame.WallBaseY + 0.01f && p.Top))
        {
            var bays = (int)MathF.Floor((piece.Length - 2 * AnnexStyle.AnnexMargin) / AnnexStyle.AnnexBay);
            if (bays < 2 * AnnexStyle.AnnexPilasterEvery) continue;
            var f = Face.Of(piece);
            var door = _openings.FirstOrDefault(o => o.Door is not null && o.Face == f);
            var at = new List<float>();
            for (var i = AnnexStyle.AnnexPilasterEvery; i < bays; i += AnnexStyle.AnnexPilasterEvery) at.Add(piece.Length * 0.5f + (i - bays * 0.5f) * AnnexStyle.AnnexBay);
            // The door bay is framed by a pair of its own, so the entrance reads from down the street.
            if (door is not null)
            {
                var mid = (door.Hole.U0 + door.Hole.U1) * 0.5f;
                at.RemoveAll(u => MathF.Abs(u - mid) < AnnexStyle.AnnexBay * 1.4f);
                at.Add(mid - AnnexStyle.AnnexBay * 0.5f);
                at.Add(mid + AnnexStyle.AnnexBay * 0.5f);
            }
            float h = AnnexStyle.AnnexPilaster * 0.5f;
            foreach (var u in at.Order())
                LibraryFacade.Box(mesh, wall, f, u - h, u + h, 0f, AnnexStyle.AnnexPilasterProject, piece.Y0, Top + LibraryStyle.ParapetHeight - LibraryStyle.CopingHeight);
        }
    }

    /// <summary>Rainwater pipes at both ends of every long ground face, from the plinth to under the coping (Q5).</summary>
    private void Pipes(GeometrySink mesh)
    {
        var metal = S.Metal(LibraryStyle.LibraryFrame);
        foreach (var piece in _pieces.Where(p => p.Y0 <= Frame.WallBaseY + 0.01f && p.Top && p.Length >= AnnexStyle.PipeMinFace))
        {
            var f = Face.Of(piece);
            foreach (var u in new[] { AnnexStyle.PipeInset, piece.Length - AnnexStyle.PipeInset })
            {
                float a = u - AnnexStyle.PipeSize * 0.5f, b = u + AnnexStyle.PipeSize * 0.5f;
                LibraryFacade.Box(mesh, metal, f, a, b, AnnexStyle.PipeOff, AnnexStyle.PipeOff + AnnexStyle.PipeSize, Frame.PlinthTopY, Top + LibraryStyle.ParapetHeight - 0.35f);
                LibraryFacade.Box(mesh, metal, f, a - 0.03f, b + 0.03f, 0f, AnnexStyle.PipeOff + AnnexStyle.PipeSize + 0.03f, Top + LibraryStyle.ParapetHeight - 0.5f, Top + LibraryStyle.ParapetHeight - 0.3f);
            }
        }
    }

    /// <summary>A short row of roof units behind the registered street parapet, seen from the 3F terrace (北辅楼屋顶设备).</summary>
    private void RoofUnits(CoreBuild build)
    {
        var face = _roofUnitsFace;
        if (face is null) return;
        var f = Face.Of(face);
        for (var i = 0; i < AnnexStyle.AnnexRoofUnits; i++)
        {
            var u = face.Length * 0.5f + (i - (AnnexStyle.AnnexRoofUnits - 1) * 0.5f) * (AnnexStyle.AnnexUnitLength + 1.4f);
            var p = f.At(u, -4.5f, Top);
            build.Parts.Add(new PlacedPart(new PartId(PartCatalog.HeatPump), new EquipmentParams(AnnexStyle.AnnexUnitLength, AnnexStyle.AnnexUnitDepth, AnnexStyle.AnnexUnitHeight),
                CoreShapes.Frame(p, Vector3.UnitY, f.N)));
        }
    }

    // ------------------------------------------------------------------ collision

    /// <summary>
    /// Ground walls block to 300 m (roofs are out of reach, MAP-QSH-CORE); the closed door's bay is a DoorClosed wall.
    /// The low block's G terrace is walkable from the library's 3F: a stone slab, its parapet blocking to 1.5 m above it.
    /// </summary>
    public IReadOnlyList<Collider> Collide()
    {
        var list = new List<Collider>();
        var high = Terrace ? Top + WalkRules.BarrierHeight : Frame.PadY + WalkRules.BuildingCollisionHeight;
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
            foreach (var block in RoofBlocks)
                list.Add(new SlabCollider([.. block.Ring], Top, 0.3f, SurfaceTag.Ground) { Foot = FootMat.StonePaving });
            foreach (var piece in _pieces.Where(p => p.Top))
                list.Add(Wall(piece.A, piece.B, Top, Top + WalkRules.BarrierHeight, SurfaceTag.Barrier));
        }
        return list;
    }

    private static WallCollider Wall(Vector2 a, Vector2 b, float y0, float y1, SurfaceTag tag) => new(a, b, 0.15f, y0, y1, tag) { Foot = FootMat.StonePaving };
    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        CoreRanges.ForFeature(world, Feature) is { } range ? [range] : [];

    public override void Build(SiteBuilder builder)
    {
        if (!builder.Includes(Anchor)) return;
        var build = CoreBuild.For(builder.Context);
        Build(build, builder.Lod);
        var output = build.Finish(builder.Lod);
        foreach (var mesh in output.Meshes) builder.Add(mesh, System.Numerics.Matrix4x4.Identity, Anchor);
        foreach (var part in output.Parts) builder.Place(part, Anchor);
        foreach (var emitter in output.Emitters ?? []) builder.Light(emitter, Anchor);
    }

    public override void Collide(SiteColliders colliders)
    {
        if (!colliders.Includes(Anchor)) return;
        foreach (var collider in Collide()) colliders.Add(collider, Anchor);
    }

    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel lod)
    {
        var build = CoreBuild.For(context);
        Build(build, lod);
        yield return new($"core/{Id}", Feature, build.Finish(lod), Collide(), []);
    }

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) =>
        WindowLayout.Grids.Select(grid => (WindowLayout.Feature, grid));

    public override void DescribeWindows(BuildContext context, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        foreach (var window in Windows(context.WindowNumbering!)) windows.Add(window);
    }

}
