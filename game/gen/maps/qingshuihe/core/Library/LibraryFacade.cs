using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using S = Campus.Gen.Maps.Qingshuihe.Core.CoreStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.Library;

/// <summary>
/// A vertical wall plane in face coordinates: u along <see cref="Along"/> from <see cref="A"/>, s outward along
/// <see cref="Out"/> (the wall face is s = 0, reveals are negative), y up in world metres.
/// </summary>
public readonly record struct Face(Vector2 A, Vector2 Along, Vector2 Out, float Length)
{
    public static Face Of(WallPiece piece) => new(piece.A, piece.Along, piece.Out, piece.Length);

    public Vector3 At(float u, float s, float y)
    {
        var p = A + Along * u + Out * s;
        return new(p.X, y, p.Y);
    }

    public Vector3 N => new(Out.X, 0, Out.Y);
    public Vector3 T => new(Along.X, 0, Along.Y);
}

/// <summary>Rectangle in a face: u from U0 to U1, y from Y0 to Y1.</summary>
public readonly record struct Rect(float U0, float U1, float Y0, float Y1);

/// <summary>What kind of opening a window run is (it decides frame, sill and glass); Entry is the 1F cafe door bay.</summary>
public enum OpeningKind { Ribbon, Podium, Square, Slit, Slot, Mech, Entry }

/// <summary>
/// One glazed opening of the shell: its hole, its glass cells (Cells across, one window row per entry of Rows) and the
/// first column it takes in the building's window grid (columns are allocated per row, so rows share column ranges).
/// </summary>
public sealed record Opening(Face Face, OpeningKind Kind, Rect Hole, int Cells, IReadOnlyList<(float Y0, float Y1, int Row)> Rows,
    int Column, byte Card)
{
    public float Depth => Kind == OpeningKind.Slot ? S.SlotDepth : S.Reveal;

    /// <summary>
    /// A door bay (16c front door in the slot, 16d cafe door): cells First … First + Count − 1 of the first row are open
    /// below Top (the door transom); their glass is only the light above it. Null for every other opening.
    /// </summary>
    public (int First, int Count, float Top)? Door { get; init; }

    /// <summary>The glass span of cell <paramref name="cell"/> in row entry (y0, y1): above the door transom in the door bay.</summary>
    public float GlassBottom(int cell, float y0) =>
        Door is { } d && y0 <= Hole.Y0 + 1e-3f && cell >= d.First && cell < d.First + d.Count ? d.Top : y0;
}

/// <summary>Paints of a glazed opening: reveal (the wall's), sill (trim) and frame bars.</summary>
public readonly record struct FacadePaints(Paint Wall, Paint Trim, Paint Frame)
{
    public static FacadePaints Library => new(S.Wall(S.LibraryWall), S.Trim(S.LibraryTrim), S.Metal(S.LibraryFrame));
}

/// <summary>Pure writers for walls, bands and glazed openings on a <see cref="Face"/>.</summary>
public static class LibraryFacade
{
    /// <summary>A vertical quad in the plane s, facing <paramref name="normal"/>.</summary>
    public static void Panel(GeometrySink mesh, in Paint paint, Face f, float u0, float u1, float s, float y0, float y1, Vector3 normal)
    {
        if (u1 - u0 < 1e-4f || y1 - y0 < 1e-4f) return;
        mesh.Quad(paint, f.At(u0, s, y0), f.At(u1, s, y0), f.At(u1, s, y1), f.At(u0, s, y1), normal);
    }

    /// <summary>A box in face coordinates: front, top, bottom and the two ends (the back stands against the wall).</summary>
    public static void Box(GeometrySink mesh, in Paint paint, Face f, float u0, float u1, float s0, float s1, float y0, float y1)
    {
        if (u1 - u0 < 1e-4f || s1 - s0 < 1e-4f || y1 - y0 < 1e-4f) return;
        Panel(mesh, paint, f, u0, u1, s1, y0, y1, f.N);
        mesh.Quad(paint, f.At(u0, s0, y1), f.At(u1, s0, y1), f.At(u1, s1, y1), f.At(u0, s1, y1), Vector3.UnitY);
        mesh.Quad(paint, f.At(u0, s0, y0), f.At(u1, s0, y0), f.At(u1, s1, y0), f.At(u0, s1, y0), -Vector3.UnitY);
        mesh.Quad(paint, f.At(u0, s0, y0), f.At(u0, s1, y0), f.At(u0, s1, y1), f.At(u0, s0, y1), -f.T);
        mesh.Quad(paint, f.At(u1, s0, y0), f.At(u1, s1, y0), f.At(u1, s1, y1), f.At(u1, s0, y1), f.T);
    }

    /// <summary>
    /// The wall face from u0 to u1 and y0 to y1 with rectangular holes: cut into horizontal bands at every hole edge,
    /// each band filled between the holes that span it.
    /// </summary>
    public static void Wall(GeometrySink mesh, in Paint paint, Face f, float u0, float u1, float y0, float y1, IReadOnlyList<Rect> holes)
    {
        var ys = new List<float> { y0, y1 };
        foreach (var h in holes)
        {
            if (h.Y0 > y0 && h.Y0 < y1) ys.Add(h.Y0);
            if (h.Y1 > y0 && h.Y1 < y1) ys.Add(h.Y1);
        }
        ys.Sort();
        for (var k = 0; k + 1 < ys.Count; k++)
        {
            float ya = ys[k], yb = ys[k + 1];
            if (yb - ya < 1e-4f) continue;
            var mid = (ya + yb) * 0.5f;
            var u = u0;
            foreach (var h in holes.Where(h => h.Y0 <= mid && h.Y1 >= mid).OrderBy(h => h.U0))
            {
                Panel(mesh, paint, f, u, MathF.Max(u, h.U0), 0f, ya, yb, f.N);
                u = MathF.Max(u, h.U1);
            }
            Panel(mesh, paint, f, u, u1, 0f, ya, yb, f.N);
        }
    }

    /// <summary>
    /// A glazed opening at one level. L0 and L1: reveal, sill and glass on the glass plane; L0 also the frame ring,
    /// mullions between the cells and transoms between the rows; L1 the frame ring and mullions. L2 and coarser: the
    /// wall stays closed and the glass lies a little proud of it, one pane per cell (L2) or per row (L3, L4).
    /// </summary>
    public static void Window(GeometrySink mesh, GlassSink glass, Opening o, LodLevel lod) => Window(mesh, glass, o, lod, FacadePaints.Library);

    /// <summary>The same opening in another building's paints (16d-2: the museum's granite reveals and dark frames).</summary>
    public static void Window(GeometrySink mesh, GlassSink glass, Opening o, LodLevel lod, in FacadePaints paints)
    {
        var f = o.Face;
        var h = o.Hole;
        var trim = paints.Trim;
        var wall = paints.Wall;
        var frame = paints.Frame;
        if (lod >= LodLevel.L2)
        {
            const float proud = 0.015f;
            var cells = lod == LodLevel.L2 ? o.Cells : 1;
            foreach (var (y0, y1, row) in o.Rows)
                for (var c = 0; c < cells; c++)
                {
                    float a = h.U0 + (h.U1 - h.U0) * c / cells, b = h.U0 + (h.U1 - h.U0) * (c + 1) / cells;
                    glass.Pane(f.At(a, proud, y0), f.At(b, proud, y0), f.At(b, proud, y1), f.At(a, proud, y1), f.N, o.Column + c * o.Cells / cells, row, o.Card);
                }
            return;
        }
        var depth = -o.Depth;
        // Reveal: jambs face into the opening, the head faces down, the bottom is the sill's top surface.
        mesh.Quad(wall, f.At(h.U0, 0f, h.Y0), f.At(h.U0, depth, h.Y0), f.At(h.U0, depth, h.Y1), f.At(h.U0, 0f, h.Y1), f.T);
        mesh.Quad(wall, f.At(h.U1, 0f, h.Y0), f.At(h.U1, depth, h.Y0), f.At(h.U1, depth, h.Y1), f.At(h.U1, 0f, h.Y1), -f.T);
        mesh.Quad(wall, f.At(h.U0, 0f, h.Y1), f.At(h.U1, 0f, h.Y1), f.At(h.U1, depth, h.Y1), f.At(h.U0, depth, h.Y1), -Vector3.UnitY);
        mesh.Quad(o.Kind == OpeningKind.Slot ? wall : trim, f.At(h.U0, 0f, h.Y0), f.At(h.U1, 0f, h.Y0), f.At(h.U1, depth, h.Y0), f.At(h.U0, depth, h.Y0), Vector3.UnitY);
        if (o.Kind is OpeningKind.Ribbon or OpeningKind.Podium or OpeningKind.Square)
            Box(mesh, trim, f, h.U0 - S.SillProject, h.U1 + S.SillProject, 0f, S.SillProject, h.Y0 - S.SillHeight, h.Y0);
        foreach (var (y0, y1, row) in o.Rows)
            for (var c = 0; c < o.Cells; c++)
            {
                float a = h.U0 + (h.U1 - h.U0) * c / o.Cells, b = h.U0 + (h.U1 - h.U0) * (c + 1) / o.Cells, g = o.GlassBottom(c, y0);
                glass.Pane(f.At(a, depth, g), f.At(b, depth, g), f.At(b, depth, y1), f.At(a, depth, y1), f.N, o.Column + c, row, o.Card);
            }
        // Frame ring and mullions stand proud of the glass by the frame depth; the door bay has no sill bar and its
        // inner mullions start at the door transom (the door part frames the opening below). L0 builds each bar as a box;
        // L1 (mid range, where a 7 cm bar is an outline stroke) keeps only its front face.
        float s0 = depth, s1 = depth + S.FrameDepth, w = S.FrameFace, m = S.MullionFace * 0.5f;
        float U(int c) => h.U0 + (h.U1 - h.U0) * c / o.Cells;
        void Bar(float u0, float u1, float front, float y0, float y1)
        {
            if (lod == LodLevel.L0) Box(mesh, frame, f, u0, u1, s0, front, y0, y1);
            else Panel(mesh, frame, f, u0, u1, front, y0, y1, f.N);
        }
        Bar(h.U0, h.U0 + w, s1, h.Y0, h.Y1);
        Bar(h.U1 - w, h.U1, s1, h.Y0, h.Y1);
        Bar(h.U0 + w, h.U1 - w, s1, h.Y1 - w, h.Y1);
        if (o.Door is { } door)
        {
            Bar(h.U0 + w, U(door.First), s1, h.Y0, h.Y0 + w);
            Bar(U(door.First + door.Count), h.U1 - w, s1, h.Y0, h.Y0 + w);
        }
        else
            Bar(h.U0 + w, h.U1 - w, s1, h.Y0, h.Y0 + w);
        for (var c = 1; c < o.Cells; c++)
        {
            var bottom = MathF.Min(o.GlassBottom(c - 1, h.Y0), o.GlassBottom(c, h.Y0));
            Bar(U(c) - m, U(c) + m, s1 + (o.Kind == OpeningKind.Slot ? 0.08f : 0f), bottom > h.Y0 ? bottom : h.Y0 + w, h.Y1 - w);
        }
        if (lod != LodLevel.L0) return;
        foreach (var y in Transoms(o))
            for (var c = 0; c < o.Cells; c++)
            {
                float a = h.U0 + (h.U1 - h.U0) * c / o.Cells + (c == 0 ? w : m), b = h.U0 + (h.U1 - h.U0) * (c + 1) / o.Cells - (c == o.Cells - 1 ? w : m);
                Box(mesh, frame, f, a, b, s0, s1, y - m, y + m);
            }
    }

    /// <summary>Transom heights: between stacked window rows, the podium's fixed top light and every door bay's transom.</summary>
    private static IEnumerable<float> Transoms(Opening o)
    {
        for (var r = 1; r < o.Rows.Count; r++) yield return o.Rows[r].Y0;
        if (o.Kind == OpeningKind.Podium) yield return o.Hole.Y0 + S.PodiumTransom;
        if (o.Door is { } door) yield return door.Top;
    }

    // ------------------------------------------------------------------ swept edges

    /// <summary>
    /// Sweeps a section along chained wall pieces (each chain one polyline, closed when it comes back to its start);
    /// lateral +X of the section is the pieces' outward side. A chain the kernel cannot miter is swept piece by piece.
    /// </summary>
    public static void Sweep(GeometrySink mesh, in Paint paint, IReadOnlyList<IReadOnlyList<Vector2>> chains, IReadOnlyList<bool> closed,
        SweepProfile profile, float baseY)
    {
        var spec = S.Spec(paint.Material, paint.Family);
        for (var i = 0; i < chains.Count; i++)
        {
            var path = chains[i];
            try
            {
                mesh.Add(paint, PartGeometryKernel.Sweep(profile, path, spec, closed[i], baseY), Matrix4x4.Identity);
            }
            catch (ArgumentException)
            {
                var count = closed[i] ? path.Count : path.Count - 1;
                for (var k = 0; k < count; k++)
                {
                    Vector2 a = path[k], b = path[(k + 1) % path.Count];
                    if (Vector2.Distance(a, b) < 0.02f) continue;
                    mesh.Add(paint, PartGeometryKernel.Sweep(profile, [a, b], spec, false, baseY), Matrix4x4.Identity);
                }
            }
        }
    }

    /// <summary>
    /// Chains pieces in their ring order into polylines (a new chain wherever one piece does not start where the last
    /// ended); the last chain joins the first when the ring closes, and a single chain round the whole ring is closed.
    /// </summary>
    public static (IReadOnlyList<IReadOnlyList<Vector2>> Chains, IReadOnlyList<bool> Closed) Chain(IReadOnlyList<(Vector2 A, Vector2 B)> pieces)
    {
        const float join = 0.05f;
        var chains = new List<List<Vector2>>();
        foreach (var (a, b) in pieces)
        {
            if (chains.Count > 0 && Vector2.Distance(chains[^1][^1], a) < join) chains[^1].Add(b);
            else chains.Add([a, b]);
        }
        var closed = new List<bool>();
        if (chains.Count > 1 && Vector2.Distance(chains[^1][^1], chains[0][0]) < join)
        {
            chains[^1].AddRange(chains[0].Skip(1));
            chains.RemoveAt(0);
        }
        foreach (var c in chains)
        {
            var loop = c.Count > 3 && Vector2.Distance(c[^1], c[0]) < join;
            if (loop) c.RemoveAt(c.Count - 1);
            closed.Add(loop);
            // Drop near-duplicate points the miter could not resolve.
            for (var k = c.Count - 1; k > 0; k--)
                if (Vector2.Distance(c[k], c[k - 1]) < join) c.RemoveAt(k);
        }
        return (chains, closed);
    }

    /// <summary>Parapet with a chamfered, overhanging coping; X = 0 is the wall face, +X outward.</summary>
    public static SweepProfile Parapet(float height, float thick)
    {
        float t = thick, h = height - S.CopingHeight, c = S.CopingHeight, o = S.CopingOverhang, b = S.CopingBevel;
        return new([new(-t, 0), new(0, 0), new(0, h), new(o, h), new(o, h + c - b), new(o - b, h + c), new(-t - o + b, h + c),
            new(-t - o, h + c - b), new(-t - o, h), new(-t, h)]);
    }

    /// <summary>The canted metal eave over the set-back wall: low outer edge with a fascia, leaning back up over the roof.</summary>
    public static SweepProfile Eave() =>
        new([new(S.EaveOverhang, -S.EaveDrop - S.EaveThick), new(S.EaveOverhang, -S.EaveDrop), new(-S.EaveRun, S.EaveRise),
            new(-S.EaveRun, S.EaveRise - S.EaveThick)]);
}
