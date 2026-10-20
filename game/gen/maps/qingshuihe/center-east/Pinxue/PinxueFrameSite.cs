using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Look;
using P = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Pinxue;
using W = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Windows;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Pinxue;

/// <summary>
/// 品学楼 gate frame and link bridges (MAP-QSH-CENTER-EAST). The 品学楼 entity's own parts are the frame bar between
/// A and B and the two bridges to C; OSM records all three as 16 m buildings standing on the ground, which would block
/// the passage and the paths under the bridges, so this site claims the entity and builds them itself: four square
/// columns carrying a two-storey beam bridge with the inscription plaque on its forecourt face (storeys 1–2 open as the
/// passage), and the A–C, B–C bridges over storeys 2–3. The wings themselves stay with WP-14 on the +1.5 m platform.
/// </summary>
public sealed class PinxueFrameSite : ZoneSite
{
    /// <summary>
    /// The gate read from the world model (D-239): the frame of the bar (X south along its west edge from A's wall, +Z west out
    /// to the forecourt), its length and depth from the bar part, and the storey height and top from A's main body.
    /// </summary>
    internal sealed record Gate(PlanFrame F, float Length, float Depth, float StoreyHeight, float BeamTop)
    {
        public float Axis => Length * 0.5f;
        public float BeamBottom => P.PassageStoreys * StoreyHeight;
        public float BridgeBottom => (P.BridgeFromStorey - 1) * StoreyHeight;
        public float BridgeTop => P.BridgeToStorey * StoreyHeight;
    }

    internal static Gate GateOf(IWorldModel world)
    {
        var edge = world.ContourSegment(CenterEastIds.Layout.PinxueFrameEdge);
        var a = edge[^1];
        var b = edge[0];
        var f = world.ContourFrame(CenterEastIds.Layout.PinxueFrameEdge, originAtEnd: true, forward: false);
        var depth = world.Feature(CenterEastIds.Pinxue.Complex).Contour(CenterEastIds.Pinxue.FrameBar).Max(p => -f.Local(p).Y);
        var (height, storeys) = world.Feature(CenterEastIds.Pinxue.WingA).Part(CenterEastIds.Pinxue.WingAMain).Levels();
        return new(f, Vector2.Distance(a, b), depth, height / storeys, height);
    }

    public override string Id => "21a.pinxue-frame";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(CenterEastIds.Pinxue.Complex), P.ColumnSize)];

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(CenterEastIds.Pinxue.Complex), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
    {
        var g = GateOf(world);
        return [.. Bridges(world).Select(box => new PolygonRings(box.Frame.Rect(box.X0, box.Z0, box.X1, box.Z1))),
                .. P.ColumnX.Select(x => new PolygonRings(g.F.Rect(x - P.ColumnSize * 0.5f, -g.Depth * 0.5f - P.ColumnSize * 0.5f,
                    x + P.ColumnSize * 0.5f, -g.Depth * 0.5f + P.ColumnSize * 0.5f)))];
    }

    internal static float BaseY(BuildContext ctx) => ctx.World.Terrain.PadY(new(CenterEastIds.Pinxue.Complex));

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var g = GateOf(world);
        var y = BaseY(b.Context);
        if (b.Includes(SiteRanges.Bounds(g.F.Rect(0, -g.Depth, g.Length, 0)))) Passage(b, g, y);
        foreach (var x in P.ColumnX.Where(x => b.Includes(SiteRanges.Point(g.F.P(x, -g.Depth * 0.5f), P.ColumnSize)))) Column(b, g, x, y);
        var bridges = Bridges(world);
        foreach (var box in bridges.Where(box => b.Includes(SiteRanges.Bounds(box.Frame.Rect(box.X0, box.Z0, box.X1, box.Z1), 0.2f)))) Bridge(b, box, y, WindowNumbers.Allocation(b.Context, new(CenterEastIds.Pinxue.Complex), box.Key));
        if (b.Includes(SiteRanges.Bounds(g.F.Rect(0, -g.Depth, g.Length, P.PlaqueProud)))) Beam(b, g, y);
        foreach (var source in new[] { CenterEastIds.Pinxue.BridgeAC, CenterEastIds.Pinxue.BridgeBC }) LinkColumns(b, Bar(world, source), g, y);
    }

    // ---- Windows (contract-29) ----

    /// <summary>
    /// The 品学楼 entity's own windows are the ribbon windows of its three bridges: the beam's back face toward the inner
    /// plaza (its forecourt face carries the plaque) and both faces of the A–C and B–C bridges, one named grid per bridge,
    /// a column per mullion cell and face, a row per storey. The wings' classroom windows belong to A, B and C (qsh:relation:
    /// 11043352–11043354), which WP-14 builds and numbers.
    /// </summary>
    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        foreach (var box in Bridges(world))
        {
            var (bands, bays, faces) = Ribbon(box);
            yield return (new(CenterEastIds.Pinxue.Complex), new(box.Key, bays * faces, bands.Count));
        }
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var y = BaseY(ctx);
        foreach (var box in Bridges(ctx.World))
        {
            var allocation = WindowNumbers.Required(ctx, new(CenterEastIds.Pinxue.Complex), box.Key);
            var (bands, bays, faces) = Ribbon(box);
            WindowNumbers.Fit(bays * faces, bands.Count, new(CenterEastIds.Pinxue.Complex), box.Key, allocation);
            WindowNumbers.Declare(windows, Openings(box, y), allocation, W.Teaching);
        }
    }

    // ---- Bridges: one LOD-free description for geometry, collision and the window grids ----

    /// <summary>
    /// A two-storey bridge box between X0 and X1 (local), Z0 (back) to Z1 (front), from Bottom to Top above the platform;
    /// a solid front keeps the forecourt face plain for the plaque.
    /// </summary>
    internal sealed record BridgeBox(string Key, PlanFrame Frame, float X0, float X1, float Z0, float Z1, float Bottom, float Top, bool FrontSolid, float StoreyHeight);

    private const float BridgeWall = 0.45f, Soffit = 0.6f, RibbonInset = 1.2f, MullionPitch = 1.8f;

    /// <summary>The beam bridge over the passage and the A–C, B–C link bridges, read from the entity's parts.</summary>
    internal static IReadOnlyList<BridgeBox> Bridges(IWorldModel world)
    {
        var g = GateOf(world);
        var len = g.Length + 0.4f; // the ends sink into A's and B's walls
        var list = new List<BridgeBox> { new("beam", g.F, g.Axis - len * 0.5f, g.Axis + len * 0.5f, -g.Depth, 0f, g.BeamBottom, g.BeamTop, true, g.StoreyHeight) };
        foreach (var (key, source) in new[] { ("bridge-ac", CenterEastIds.Pinxue.BridgeAC), ("bridge-bc", CenterEastIds.Pinxue.BridgeBC) })
        {
            var (f, x0, x1, z0, z1) = Bar(world, source);
            list.Add(new(key, f, x0 - 0.3f, x1 + 0.3f, z0, z1, g.BridgeBottom, g.BridgeTop, false, g.StoreyHeight));
        }
        return list;
    }

    /// <summary>A bridge's ribbon windows: one band (sill, head above the soffit) per 4 m storey, mullion cells per band, windowed faces.</summary>
    internal static (IReadOnlyList<(float Sill, float Head)> Bands, int Bays, int Faces) Ribbon(BridgeBox box)
    {
        var h = box.Top - box.Bottom - Soffit;
        var storeys = Math.Max(1, (int)MathF.Round((h + Soffit) / box.StoreyHeight));
        var bands = new List<(float Sill, float Head)>();
        for (var k = 0; k < storeys; k++)
        {
            float sill = k * box.StoreyHeight + 1.0f - Soffit, head = k * box.StoreyHeight + 3.1f - Soffit;
            if (head > h - 0.3f) break;
            bands.Add((sill, head));
        }
        var half = (box.X1 - box.X0) * 0.5f - RibbonInset;
        return (bands, Math.Max(1, (int)MathF.Round(2 * half / MullionPitch)), box.FrontSolid ? 1 : 2);
    }

    /// <summary>The windowed faces in grid order (back first): outward sign along the frame's Z and the wall's centre line.</summary>
    private static IEnumerable<(float Outward, float Zc)> Faces(BridgeBox box)
    {
        yield return (-1f, box.Z0 + BridgeWall * 0.5f);
        if (!box.FrontSolid) yield return (1f, box.Z1 - BridgeWall * 0.5f);
    }

    /// <summary>One numbered pane per mullion cell, 1 cm in front of the ribbon glass; column = face × bays + cell, row = band.</summary>
    internal static IEnumerable<GlazedOpening> Openings(BridgeBox box, float y)
    {
        var (bands, n, _) = Ribbon(box);
        var f = box.Frame;
        float half = (box.X1 - box.X0) * 0.5f - RibbonInset, xc = (box.X0 + box.X1) * 0.5f, cell = 2 * half / n;
        var face = 0;
        foreach (var (outward, zc) in Faces(box))
        {
            var normal = new Vector3(f.W.X * outward, 0, f.W.Y * outward);
            var paneZ = zc - outward * 0.12f + outward * 0.03f;
            for (var k = 0; k < bands.Count; k++)
                for (var i = 0; i < n; i++)
                {
                    var p = f.P(xc - half + (i + 0.5f) * cell, paneZ);
                    yield return new(face * n + i, k, new(p.X, y + box.Bottom + Soffit + bands[k].Sill, p.Y), normal, cell, bands[k].Head - bands[k].Sill);
                }
            face++;
        }
    }

    // ---- Passage floor and columns ----

    /// <summary>Paving under the frame: the bar is a building in OSM, so the ground leaves its footprint unpaved.</summary>
    private static void Passage(SiteBuilder b, Gate g, float y)
    {
        var d = g.Depth;
        b.Add(PartGeometryKernel.BeveledBox(g.Length, 0.14f, d, 0.01f, L.Paving), g.F.At(g.Axis, y - 0.12f, -d * 0.5f));
    }

    private static void Column(SiteBuilder b, Gate g, float x, float y)
    {
        float s = P.ColumnSize, z = -g.Depth * 0.5f;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.3f, P.ColumnPlinth + 0.1f, s + 0.3f, 0.03f, L.DarkGranite), g.F.At(x, y - 0.1f, z));
        // A shadow joint where the column meets the soffit: the beam reads as carried, not welded on.
        var joint = b.Detail == SiteDetail.Mass ? 0f : 0.12f;
        b.Add(PartGeometryKernel.BeveledBox(s, g.BeamBottom - P.ColumnPlinth - joint, s, 0.05f, L.Wall), g.F.At(x, y + P.ColumnPlinth, z));
        if (joint > 0) b.Add(PartGeometryKernel.BeveledBox(s - 0.16f, joint, s - 0.16f, 0.01f, L.Metal), g.F.At(x, y + g.BeamBottom - joint, z));
    }

    // ---- Beam bridge (storeys 3–4) with the inscription plaque ----

    /// <summary>The beam bridge's plaque and soffit lights; the bridge box itself is one of <see cref="Bridges"/>.</summary>
    private static void Beam(SiteBuilder b, Gate g, float y)
    {
        if (b.Detail == SiteDetail.Mass) return;
        // Plaque: a slightly proud stone panel with a fine frame; the calligraphy "求实求真 大气大为" waits for the
        // scene text atlas (contract-26 follow-up) and is not painted on as a texture here.
        var mid = y + (g.BeamBottom + g.BeamTop) * 0.5f + 0.2f;
        b.Add(PartGeometryKernel.BeveledBox(P.PlaqueWidth + 0.3f, P.PlaqueHeight + 0.3f, P.PlaqueProud, 0.01f, L.Trim), g.F.At(g.Axis, mid - (P.PlaqueHeight + 0.3f) * 0.5f, P.PlaqueProud * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(P.PlaqueWidth, P.PlaqueHeight, 0.04f, 0.005f, L.Stone), g.F.At(g.Axis, mid - P.PlaqueHeight * 0.5f, P.PlaqueProud + 0.02f));
        if (b.Detail != SiteDetail.Full) return;
        // Two rows of soffit downlights over the passage, skipping the columns.
        for (var x = P.DownlightPitch * 0.5f; x < g.Length; x += P.DownlightPitch)
        {
            if (P.ColumnX.Any(c => MathF.Abs(c - x) < P.ColumnSize * 0.5f + 0.4f)) continue;
            foreach (var z in new[] { -g.Depth * 0.25f, -g.Depth * 0.75f })
                b.Add(Shapes.Shaft(P.DownlightRadius, P.DownlightRadius, 0.05f, 12, L.Metal), g.F.At(x, y + g.BeamBottom - 0.05f, z));
        }
    }

    /// <summary>
    /// A bridge box: soffit, walls 0.45 m thick, roof and coping. Each windowed face carries one ribbon window per storey
    /// with aluminium mullions; with a window allocation every mullion cell gets its numbered pane in front of the glass.
    /// </summary>
    private static void Bridge(SiteBuilder b, BridgeBox box, float y, WindowAllocation? allocation)
    {
        var f = box.Frame;
        float len = box.X1 - box.X0, xc = (box.X0 + box.X1) * 0.5f, depth = box.Z1 - box.Z0, zm = (box.Z0 + box.Z1) * 0.5f;
        float yBottom = y + box.Bottom, yTop = y + box.Top, h = box.Top - box.Bottom - Soffit;
        b.Add(PartGeometryKernel.BeveledBox(len, Soffit, depth, 0.03f, L.Wall), f.At(xc, yBottom, zm));
        b.Add(PartGeometryKernel.BeveledBox(len, 0.4f, depth - 2 * BridgeWall, 0.01f, L.RoofSlab), f.At(xc, yTop - 0.4f, zm));
        b.Add(PartGeometryKernel.BeveledBox(len, 0.35f, depth + 0.2f, 0.03f, L.Trim), f.At(xc, yTop, zm));
        if (box.FrontSolid) b.Add(PartGeometryKernel.BeveledBox(len, h, BridgeWall, 0.03f, L.Wall), f.At(xc, yBottom + Soffit, box.Z1 - BridgeWall * 0.5f));
        var (bands, n, _) = Ribbon(box);
        foreach (var (outward, zc) in Faces(box)) WindowWall(b, f, xc, len, zc, yBottom, h, outward, bands, n);
        if (allocation is { } a)
        {
            var (_, bays, faces) = Ribbon(box);
            WindowNumbers.Fit(bays * faces, bands.Count, new(CenterEastIds.Pinxue.Complex), box.Key, a);
            WindowNumbers.Panes(b, Openings(box, y), a, W.Teaching, W.Corridor);
        }
        if (b.Detail == SiteDetail.Mass) return;
        // Soffit edge band: a crisp horizontal line under the bridge on both faces.
        foreach (var (z, sign) in new[] { (box.Z1, 1f), (box.Z0, -1f) })
            b.Add(PartGeometryKernel.BeveledBox(len, 0.18f, 0.12f, 0.01f, L.Trim), f.At(xc, yBottom, z + sign * 0.05f));
    }

    /// <summary>A wall slab with one ribbon window per band; glass set 0.3 m back (at every level, so no band reads as a hole); mullions every cell.</summary>
    private static void WindowWall(SiteBuilder b, PlanFrame f, float xc, float len, float zc, float yBottom, float h, float outward,
        IReadOnlyList<(float Sill, float Head)> bands, int n)
    {
        var half = len * 0.5f - RibbonInset;
        var openings = bands.Select(band => (IReadOnlyList<Vector2>)[new(-half, band.Sill), new(half, band.Sill), new(half, band.Head), new(-half, band.Head)]).ToArray();
        var y = yBottom + Soffit;
        b.Add(PartGeometryKernel.WallPanel(len, h, BridgeWall, openings, L.Wall), f.At(xc, y, zc));
        var glassZ = zc - outward * 0.12f;
        foreach (var (sill, head) in bands)
        {
            b.Add(PartGeometryKernel.BeveledBox(2 * half, head - sill, 0.04f, b.Detail == SiteDetail.Mass ? 0f : 0.005f, ZoneLooks.Glass), f.At(xc, y + sill, glassZ));
            if (b.Detail == SiteDetail.Mass) continue;
            b.Add(PartGeometryKernel.BeveledBox(2 * half + 0.1f, 0.08f, 0.32f, 0.01f, L.Trim), f.At(xc, y + sill - 0.08f, zc + outward * 0.12f));
            if (b.Detail != SiteDetail.Full) continue;
            for (var i = 1; i < n; i++)
                b.Add(PartGeometryKernel.BeveledBox(0.08f, head - sill, 0.1f, 0.01f, L.Metal), f.At(xc - half + 2 * half * i / n, y + sill, glassZ + outward * 0.05f));
            b.Add(PartGeometryKernel.BeveledBox(2 * half, 0.07f, 0.1f, 0.01f, L.Metal), f.At(xc, y + sill + (head - sill) * 0.72f, glassZ + outward * 0.05f));
        }
    }

    // ---- Link bridges A–C, B–C (storeys 2–3) ----

    /// <summary>Bridge bounds along its explicitly selected source edge.</summary>
    internal static (PlanFrame Frame, float X0, float X1, float Z0, float Z1) Bar(IWorldModel world, string sourceId)
    {
        var selection = sourceId == CenterEastIds.Pinxue.BridgeAC ? CenterEastIds.Layout.PinxueBridgeAcEdge : CenterEastIds.Layout.PinxueBridgeBcEdge;
        var frame = world.ContourFrame(selection);
        var bounds = world.Feature(CenterEastIds.Pinxue.Complex).Rectangle(frame.U, sourceId);
        var centre = frame.Local(bounds.Frame.Origin);
        return (frame, centre.X - bounds.Width * 0.5f, centre.X + bounds.Width * 0.5f,
            centre.Y - bounds.Depth * 0.5f, centre.Y + bounds.Depth * 0.5f);
    }

    /// <summary>A link bridge's round columns; the bridge box itself is one of <see cref="Bridges"/>.</summary>
    private static void LinkColumns(SiteBuilder b, (PlanFrame Frame, float X0, float X1, float Z0, float Z1) bar, Gate g, float y)
    {
        var f = bar.Frame;
        if (!b.Includes(SiteRanges.Bounds(f.Rect(bar.X0, bar.Z0, bar.X1, bar.Z1), P.BridgeColumnRadius))) return;
        var segments = b.Detail == SiteDetail.Full ? 16 : 8;
        foreach (var (x, z) in BridgeColumns(bar))
        {
            b.Add(PartGeometryKernel.BeveledBox(0.8f, 0.35f, 0.8f, 0.02f, L.DarkGranite), f.At(x, y - 0.1f, z));
            b.Add(Shapes.Shaft(P.BridgeColumnRadius, P.BridgeColumnRadius * 0.9f, g.BridgeBottom - 0.25f, segments, L.Wall), f.At(x, y + 0.25f, z));
        }
    }

    /// <summary>Two pairs of round columns at the third points, 0.7 m inside the long faces; the path runs between them.</summary>
    internal static IEnumerable<(float X, float Z)> BridgeColumns((PlanFrame Frame, float X0, float X1, float Z0, float Z1) bar)
    {
        var (_, x0, x1, z0, z1) = bar;
        foreach (var t in new[] { 1f / 3f, 2f / 3f })
            foreach (var z in new[] { z0 + 0.7f, z1 - 0.7f })
                yield return (x0 + (x1 - x0) * t, z);
    }

    public override void Collide(SiteColliders c)
    {
        var g = GateOf(c.Context.World);
        var y = BaseY(c.Context);
        var z = -g.Depth * 0.5f;
        foreach (var x in P.ColumnX.Where(x => c.Includes(SiteRanges.Point(g.F.P(x, z), P.ColumnSize))))
            c.Box(g.F.At(x, y + g.BeamBottom * 0.5f, z), new(P.ColumnSize * 0.5f + 0.05f, g.BeamBottom * 0.5f, P.ColumnSize * 0.5f + 0.05f), SurfaceTag.Wall, FootMat.StonePaving);
        foreach (var box in Bridges(c.Context.World).Where(box => c.Includes(SiteRanges.Bounds(box.Frame.Rect(box.X0, box.Z0, box.X1, box.Z1), 0.2f))))
            c.Box(box.Frame.At((box.X0 + box.X1) * 0.5f, y + (box.Bottom + box.Top) * 0.5f, (box.Z0 + box.Z1) * 0.5f),
                new((box.X1 - box.X0) * 0.5f, (box.Top - box.Bottom) * 0.5f, (box.Z1 - box.Z0) * 0.5f), SurfaceTag.Wall, FootMat.Concrete);
        foreach (var source in new[] { CenterEastIds.Pinxue.BridgeAC, CenterEastIds.Pinxue.BridgeBC })
        {
            var bar = Bar(c.Context.World, source);
            if (!c.Includes(SiteRanges.Bounds(bar.Frame.Rect(bar.X0, bar.Z0, bar.X1, bar.Z1), P.BridgeColumnRadius))) continue;
            foreach (var (x, zc) in BridgeColumns(bar))
                c.Add(new PostCollider(bar.Frame.P(x, zc), P.BridgeColumnRadius + 0.05f, y, y + g.BridgeBottom, SurfaceTag.Prop) { Foot = FootMat.Concrete });
        }
    }
}
