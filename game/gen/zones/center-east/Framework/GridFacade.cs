using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;

namespace Campus.Gen.Zones.CenterEast.Framework;

/// <summary>How one footprint edge of a gridded block is dressed.</summary>
public enum EdgeKind
{
    /// <summary>Piers, spandrel bands and windows.</summary>
    Plain,
    /// <summary>As Plain, plus a giant-order pilaster on every Nth pier above the ground storey (the windows read in groups).</summary>
    Front,
    /// <summary>A plain wall with plinth, cornice and parapet: edges too short for a window bay.</summary>
    Blank,
    /// <summary>Nothing: the edge lies against another volume.</summary>
    Hidden,
}

/// <summary>One edge as the block sees it: endpoints in build order, its frame (local x 0…Length, +Z outward) and index.</summary>
public readonly record struct GridEdgeInfo(int Index, Vector2 A, Vector2 B, PlanFrame Frame, float Length)
{
    /// <summary>Outward bearing in degrees (0 north, 90 east).</summary>
    public float Facing => CenterEastSite.Bearing(Frame.W);
}

/// <summary>
/// An edge's dressing; Cuts are ranges along it (local x) where nothing is built below Top, a height above the block base (an
/// entrance portal stands there).
/// </summary>
public sealed record GridEdge(EdgeKind Kind)
{
    public IReadOnlyList<(float U0, float U1, float Top)> Cuts { get; init; } = [];
}

/// <summary>
/// One footprint edge in a block's pure layout: its dressing, corner convexity and window bays. A windowed edge owns the grid
/// columns ColumnStart … ColumnStart + Bays − 1 of the block (canonical ring order); blank and hidden edges own none.
/// </summary>
public sealed record GridRun(GridEdgeInfo Info, GridEdge Edge, bool Convex0, bool Convex1, int Bays, float Pitch, int ColumnStart);

/// <summary>A gridded block's layout: every edge in ring order, the block's window columns and its storey rows. Independent of LOD and terrain.</summary>
public sealed record GridLayout(IReadOnlyList<GridRun> Runs, int Columns, int Rows);

/// <summary>
/// Where a gridded block's windows sit in the map-wide numbering: its named grid, the block's first column there, the light
/// and the window card; AltShare tenths of the windows (by window ID) take AltCard instead (a card mix such as W-LAB : W-OFF).
/// </summary>
public sealed record GridNumbering(FeatureId Feature, string Key, int ColumnOffset, ChannelId Channel, byte Card)
{
    public byte AltCard { get; init; }
    public int AltShare { get; init; }

    /// <summary>The card of one window, a fixed integer hash of its ID so every level and rebuild agrees.</summary>
    public byte CardOf(int windowId) => AltShare > 0 && (uint)windowId * 2654435761u % 10u < (uint)AltShare ? AltCard : Card;
}

/// <summary>Proportions and materials of a gridded block (see <see cref="GridFacade"/>). Heights are above the block base.</summary>
public sealed record GridStyle(ToonSpec Wall, ToonSpec Trim, ToonSpec Plinth, ToonSpec Glass, ToonSpec Frame, ToonSpec Roof)
{
    public float Bay { get; init; } = 3.9f;
    public float Pier { get; init; } = 1.2f;
    public float PierDepth { get; init; } = 0.45f;
    public float BandDepth { get; init; } = 0.35f;
    public float GroundStorey { get; init; } = 5f;
    public float Storey { get; init; } = 4.2f;
    public int Storeys { get; init; } = 5;
    public float GroundSill { get; init; } = 1.3f;
    public float GroundWindow { get; init; } = 3.0f;
    public float Sill { get; init; } = 0.9f;
    public float Window { get; init; } = 2.4f;
    public float PlinthHeight { get; init; } = 1.0f;
    public float Cornice { get; init; } = 0.7f;
    public float CorniceProjection { get; init; } = 0.6f;
    public float Parapet { get; init; } = 0.6f;
    /// <summary>Front edges: a giant-order pilaster on every Nth pier above the ground storey (0 = none).</summary>
    public int PilasterEvery { get; init; }

    /// <summary>Top of the wall grid (cornice soffit).</summary>
    public float WallTop => GroundStorey + (Storeys - 1) * Storey;
    /// <summary>Top of the parapet coping: the block's height.</summary>
    public float Top => WallTop + Cornice + Parapet;
    /// <summary>Top of the roof slab inside the parapet, where roof structures stand.</summary>
    public float RoofTop => WallTop + 0.35f;

    internal float FloorOf(int k) => k == 0 ? 0 : GroundStorey + (k - 1) * Storey;
    public float SillOf(int k) => FloorOf(k) + (k == 0 ? GroundSill : Sill);
    public float HeadOf(int k) => SillOf(k) + (k == 0 ? GroundWindow : Window);
}

/// <summary>
/// A gridded masonry block on a plan ring: a glass core, a stone frame of full-height piers and storey-high spandrel bands
/// in front of it (the window reveals are the frame's depth), plinth, a two-step cornice, a parapet with coping and a
/// roof slab. The zone dresses the buildings it claims from WP-14 with it (the research family, MAP-QSH-CENTER-EAST);
/// it keeps the ART-MODELING minimum set: corner piers, plinth, string courses, sills, recessed glass with frames,
/// cornice and coping. Bands and copings wrap convex corners; reflex corners simply interpenetrate. One pure layout
/// (<see cref="Layout"/>) feeds the geometry, the collision and the window declarations. Same rules as the south zone's grid
/// blocks (Xuanbang), kept in this assembly because zones do not reference each other (G4).
/// </summary>
public static class GridFacade
{
    /// <summary>Numbered panes stand this far in front of the glass core (and of the stone core at L3–L4).</summary>
    private const float PaneZ = 0.03f;

    /// <summary>The block's layout: the one source for geometry, collision and the window declarations.</summary>
    public static GridLayout Layout(IReadOnlyList<Vector2> ring, GridStyle s, Func<GridEdgeInfo, GridEdge> dress)
    {
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(dress);
        var runs = new List<GridRun>();
        var column = 0;
        foreach (var (info, convex0, convex1) in Edges(ring))
        {
            var edge = dress(info);
            var bays = 0;
            var pitch = 0f;
            if (edge.Kind is EdgeKind.Plain or EdgeKind.Front && Bays(info.Length, s, out var n, out var p)) (bays, pitch) = (n, p);
            runs.Add(new(info, edge, convex0, convex1, bays, pitch, column));
            column += bays;
        }
        return new(runs, column, s.Storeys);
    }

    /// <summary>True when cell (bay, storey) of a windowed run lies under a cut (the portal), so it carries no window.</summary>
    public static bool Skipped(GridRun run, int bay, int storey, GridStyle s)
    {
        float x0 = bay * run.Pitch + s.Pier, x1 = (bay + 1) * run.Pitch;
        return run.Edge.Cuts.Any(c => x1 > c.U0 && x0 < c.U1 && c.Top > s.SillOf(storey));
    }

    public static void Build(SiteBuilder b, IReadOnlyList<Vector2> ring, float y0, GridStyle s, Func<GridEdgeInfo, GridEdge> dress, GridNumbering? numbering = null)
    {
        ArgumentNullException.ThrowIfNull(ring);
        var detail = b.Detail;
        var coarse = b.Lod is LodLevel.L3 or LodLevel.L4;
        var layout = Layout(ring, s, dress);
        // Glass core (stone at the coarse levels, where window bands no longer read) and the roof slab inside the parapet.
        var footprint = new PolygonRings(ring);
        b.Add(PartGeometryKernel.Extrude(footprint, s.WallTop + 0.1f, coarse ? s.Wall : s.Glass, y0 - 0.1f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Extrude(footprint, 0.25f, s.Roof, y0 + s.WallTop + 0.1f), Matrix4x4.Identity);
        if (numbering is not null && WindowNumbers.Allocation(b.Context, numbering.Feature, numbering.Key) is { } allocation)
            Panes(b, layout, y0, s, numbering, allocation);
        foreach (var lr in layout.Runs)
        {
            var (info, edge) = (lr.Info, lr.Edge);
            if (edge.Kind == EdgeKind.Hidden) continue;
            var run = new Edge(f: info.Frame, l: info.Length, cuts: edge.Cuts, c0: lr.Convex0, c1: lr.Convex1, y0: y0);
            var bevel = detail == SiteDetail.Full;
            // Plinth, cornice (fascia and crown), parapet and coping: horizontal runs wrapping convex corners.
            run.Run(b, 0, s.PierDepth + 0.08f, y0 - 0.1f, s.PlinthHeight + 0.1f, s.Plinth, bevel);
            var top = y0 + s.WallTop;
            run.Run(b, 0, s.PierDepth + 0.2f, top, 0.3f, s.Trim, bevel);
            run.Run(b, 0, s.PierDepth + s.CorniceProjection, top + 0.3f, s.Cornice - 0.3f, s.Trim, bevel);
            if (coarse) continue;
            var face = s.PierDepth;
            run.Run(b, face - 0.32f, face, top + s.Cornice, s.Parapet - 0.12f, s.Wall, false);
            if (detail != SiteDetail.Mass)
                run.Run(b, face - 0.38f, face + 0.06f, top + s.Cornice + s.Parapet - 0.12f, 0.12f, s.Trim, bevel);
            if (lr.Bays == 0)
            {
                run.Run(b, 0, s.PierDepth, y0 + s.PlinthHeight, s.WallTop - s.PlinthHeight, s.Wall, bevel, ignoreHeight: true);
                continue;
            }
            // Piers; the last one is carried round a convex corner (the next edge starts flush, so nothing doubles).
            var f = info.Frame;
            var (n, pitch) = (lr.Bays, lr.Pitch);
            for (var i = 0; i <= n; i++)
            {
                var x0 = i * pitch;
                var x1 = i * pitch + s.Pier + (i == n && lr.Convex1 ? s.PierDepth : 0);
                var from = Clear(edge, x0, x1, y0 + s.PlinthHeight, y0);
                if (from >= top - 0.2f) continue;
                Box(b, f, x0, x1, from, top, 0, s.PierDepth, s.Wall, bevel ? 0.035f : 0);
            }
            // Spandrel bands between the window rows, each with a sill strip along its top.
            var rows = new List<(float Y0, float Y1, bool Sill)> { (s.PlinthHeight, s.SillOf(0), true) };
            for (var k = 0; k + 1 < s.Storeys; k++) rows.Add((s.HeadOf(k), s.SillOf(k + 1), true));
            rows.Add((s.HeadOf(s.Storeys - 1), s.WallTop, false));
            foreach (var (r0, r1, sill) in rows)
            {
                if (r1 - r0 < 0.05f) continue;
                run.Run(b, 0, s.BandDepth, y0 + r0, r1 - r0, s.Wall, false);
                if (sill && detail != SiteDetail.Mass && r0 > s.PlinthHeight - 0.01f)
                    run.Run(b, 0, s.BandDepth + 0.06f, y0 + r1 - 0.08f, 0.08f, s.Trim, false);
            }
            if (detail == SiteDetail.Mass) continue;
            Windows(b, f, edge, n, pitch, y0, s, detail);
            if (edge.Kind == EdgeKind.Front) Pilasters(b, f, edge, n, pitch, y0, s, detail);
        }
    }

    /// <summary>Wall colliders along every built edge, outside the cut ranges (whoever builds there collides there).</summary>
    public static void Collide(SiteColliders c, IReadOnlyList<Vector2> ring, float y0, GridStyle s, Func<GridEdgeInfo, GridEdge> dress, FootMat foot)
    {
        foreach (var run in Layout(ring, s, dress).Runs)
        {
            if (run.Edge.Kind == EdgeKind.Hidden) continue;
            var z = s.PierDepth * 0.5f;
            foreach (var (u0, u1) in Open(0, run.Info.Length, run.Edge.Cuts.Select(cut => (cut.U0, cut.U1))))
                c.Wall(run.Info.Frame.P(u0, z), run.Info.Frame.P(u1, z), z + 0.1f, y0, y0 + s.Top, SurfaceTag.Wall, foot);
        }
    }

    /// <summary>
    /// The block's windows as facade declarations, one per windowed run, from the same layout as the panes: origin at the
    /// lower-left corner of the run's first window on the outer facade surface (the pier face), Right along the edge (Right × Up
    /// points out), one sill per storey; cells under a cut are skipped. Height is the upper storeys' window height.
    /// </summary>
    public static void Declare(ICollection<WindowFacadeDecl> facades, IReadOnlyList<Vector2> ring, float y0, GridStyle s, Func<GridEdgeInfo, GridEdge> dress,
        GridNumbering numbering, WindowAllocation allocation)
    {
        var layout = Layout(ring, s, dress);
        WindowNumbers.Fit(layout, numbering, allocation);
        var sills = Enumerable.Range(0, s.Storeys).Select(k => s.SillOf(k) - s.SillOf(0)).ToArray();
        foreach (var run in layout.Runs.Where(r => r.Bays > 0))
        {
            var f = run.Info.Frame;
            var origin = f.P(s.Pier, s.PierDepth);
            var skip = new List<int>();
            for (var k = 0; k < s.Storeys; k++)
                for (var i = 0; i < run.Bays; i++)
                    if (Skipped(run, i, k, s)) skip.Add(k * run.Bays + i);
            var decl = new WindowFacadeDecl(allocation.BaseId, allocation.Columns, numbering.Channel, numbering.ColumnOffset + run.ColumnStart, run.Bays,
                s.Storeys, new(origin.X, y0 + s.SillOf(0), origin.Y), new(f.U.X, 0, f.U.Y), Vector3.UnitY, run.Pitch, run.Pitch - s.Pier, s.Window, sills);
            facades.Add(decl with { Skip = skip });
        }
    }

    /// <summary>One numbered VF-B pane per window cell (WP-10 OpeningGlass), just proud of the core, at every level.</summary>
    private static void Panes(SiteBuilder b, GridLayout layout, float y0, GridStyle s, GridNumbering numbering, WindowAllocation allocation)
    {
        WindowNumbers.Fit(layout, numbering, allocation);
        foreach (var run in layout.Runs.Where(r => r.Bays > 0))
        {
            var f = run.Info.Frame;
            var outward = new Vector3(f.W.X, 0, f.W.Y);
            var width = run.Pitch - s.Pier;
            for (var k = 0; k < s.Storeys; k++)
                for (var i = 0; i < run.Bays; i++)
                {
                    if (Skipped(run, i, k, s)) continue;
                    var column = numbering.ColumnOffset + run.ColumnStart + i;
                    var p = f.P(s.Pier + (i + 0.5f) * run.Pitch - s.Pier * 0.5f, PaneZ);
                    var pane = OpeningGlass.Grid(width, s.HeadOf(k) - s.SillOf(k), 1, 1, allocation.BaseId, column, k,
                        allocation.Columns, numbering.Channel, numbering.CardOf(allocation.IdAt(column, k)));
                    b.Add(pane, OpeningPlacement.Frame(new(p.X, y0 + s.SillOf(k), p.Y), outward));
                }
        }
    }

    /// <summary>The ring's edges oriented so each frame's +Z points out of the block, with the convexity of both ends.</summary>
    public static IEnumerable<(GridEdgeInfo Info, bool Convex0, bool Convex1)> Edges(IReadOnlyList<Vector2> ring)
    {
        var count = ring.Count;
        var ccw = PlanMath.SignedArea2(ring) > 0;
        bool Convex(int i)
        {
            Vector2 p = ring[(i + count - 1) % count], q = ring[i], r = ring[(i + 1) % count];
            var cross = (q.X - p.X) * (r.Y - q.Y) - (q.Y - p.Y) * (r.X - q.X);
            return ccw ? cross > 0 : cross < 0;
        }
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            // PlanFrame's +Z is the left normal; a counter-clockwise ring has its outside on the right, so walk it backwards.
            var (a, bEnd, ca, cb) = ccw ? (ring[j], ring[i], Convex(j), Convex(i)) : (ring[i], ring[j], Convex(i), Convex(j));
            var length = MathF.Sqrt((bEnd.X - a.X) * (bEnd.X - a.X) + (bEnd.Y - a.Y) * (bEnd.Y - a.Y));
            if (length < 0.05f) continue;
            yield return (new GridEdgeInfo(i, a, bEnd, PlanFrame.Toward(a, bEnd), length), ca, cb);
        }
    }

    /// <summary>Bay count and pitch: piers at 0, pitch, …, n·pitch (their left edges); false when no window fits.</summary>
    private static bool Bays(float l, GridStyle s, out int n, out float pitch)
    {
        n = Math.Max(1, (int)MathF.Round((l - s.Pier) / s.Bay));
        pitch = (l - s.Pier) / n;
        return pitch - s.Pier >= 0.9f;
    }

    private static void Windows(SiteBuilder b, PlanFrame f, GridEdge edge, int n, float pitch, float y0, GridStyle s, SiteDetail detail)
    {
        if (detail != SiteDetail.Full) return;
        var width = pitch - s.Pier;
        for (var i = 0; i < n; i++)
        {
            var xc = i * pitch + s.Pier + width * 0.5f;
            for (var k = 0; k < s.Storeys; k++)
            {
                float sill = y0 + s.SillOf(k), head = y0 + s.HeadOf(k);
                if (Clear(edge, xc - width * 0.5f, xc + width * 0.5f, sill, y0) > sill) continue;
                // A mullion and a transom in the aluminium frame colour, just proud of the glass.
                Box(b, f, xc - 0.045f, xc + 0.045f, sill, head, 0.02f, 0.1f, s.Frame, 0);
                Box(b, f, xc - width * 0.5f, xc + width * 0.5f, sill + (head - sill) * 0.68f, sill + (head - sill) * 0.68f + 0.08f, 0.02f, 0.1f, s.Frame, 0);
            }
        }
    }

    /// <summary>Front edges: giant-order pilasters on every Nth pier from the ground storey up to the cornice.</summary>
    private static void Pilasters(SiteBuilder b, PlanFrame f, GridEdge edge, int n, float pitch, float y0, GridStyle s, SiteDetail detail)
    {
        if (s.PilasterEvery <= 0) return;
        float p0 = y0 + s.GroundStorey, p1 = y0 + s.WallTop;
        for (var i = s.PilasterEvery; i < n; i += s.PilasterEvery)
        {
            float x0 = i * pitch - 0.15f, x1 = i * pitch + s.Pier + 0.15f;
            if (Clear(edge, x0, x1, p0, y0) > p0) continue;
            var d = s.PierDepth + 0.22f;
            var bevel = detail == SiteDetail.Full ? 0.03f : 0;
            Box(b, f, x0 - 0.08f, x1 + 0.08f, p0 - 0.35f, p0 + 0.2f, 0, d + 0.06f, s.Trim, bevel);
            Box(b, f, x0, x1, p0 + 0.2f, p1, 0, d, s.Wall, bevel);
        }
    }

    /// <summary>
    /// One built edge: horizontal runs between depths z0…z1 (outward) wrap a convex corner by the rule "the edge ending
    /// there reaches out to z1, the edge starting there reaches back to z0", so a corner is filled exactly once.
    /// </summary>
    private readonly struct Edge(PlanFrame f, float l, IReadOnlyList<(float U0, float U1, float Top)> cuts, bool c0, bool c1, float y0)
    {
        /// <summary>A run from y to y + h (absolute), split around cuts that reach above y (all cuts when ignoreHeight).</summary>
        public void Run(SiteBuilder b, float z0, float z1, float y, float h, ToonSpec material, bool bevel, bool ignoreHeight = false)
        {
            var above = y - y0 + 0.01f;
            var open = cuts.Where(c => ignoreHeight || c.Top > above).Select(c => (c.U0, c.U1));
            foreach (var (u0, u1) in Open(c0 ? -z0 : 0, l + (c1 ? z1 : 0), open))
                Box(b, f, u0, u1, y, y + h, z0, z1, material, bevel ? MathF.Min(0.03f, h * 0.3f) : 0);
        }
    }

    /// <summary>The lowest y (absolute) at which an element spanning [x0, x1] may start: above every cut it overlaps.</summary>
    private static float Clear(GridEdge edge, float x0, float x1, float y, float y0)
    {
        foreach (var cut in edge.Cuts)
            if (x1 > cut.U0 && x0 < cut.U1) y = MathF.Max(y, y0 + cut.Top);
        return y;
    }

    /// <summary>[a, b] minus the given ranges, pieces shorter than 5 cm dropped.</summary>
    internal static IEnumerable<(float U0, float U1)> Open(float a, float b, IEnumerable<(float U0, float U1)> cuts)
    {
        var pieces = new List<(float, float)> { (a, b) };
        foreach (var (c0, c1) in cuts)
        {
            var next = new List<(float, float)>();
            foreach (var (p0, p1) in pieces)
            {
                if (c1 <= p0 || c0 >= p1) { next.Add((p0, p1)); continue; }
                if (c0 > p0) next.Add((p0, c0));
                if (c1 < p1) next.Add((c1, p1));
            }
            pieces = next;
        }
        return pieces.Where(p => p.Item2 - p.Item1 > 0.05f);
    }

    /// <summary>A box in the edge frame spanning x0…x1, y0…y1 (absolute) and z0…z1 (outward).</summary>
    internal static void Box(SiteBuilder b, PlanFrame f, float x0, float x1, float y0, float y1, float z0, float z1, ToonSpec material, float bevel)
    {
        float w = x1 - x0, h = y1 - y0, d = z1 - z0;
        if (w < 0.01f || h < 0.01f || d < 0.01f) return;
        bevel = MathF.Min(bevel, MathF.Min(w, MathF.Min(h, d)) * 0.45f);
        b.Add(PartGeometryKernel.BeveledBox(w, h, d, bevel, material), f.At((x0 + x1) * 0.5f, y0, (z0 + z1) * 0.5f));
    }
}
