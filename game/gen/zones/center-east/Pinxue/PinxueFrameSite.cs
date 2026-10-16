using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.CenterEast.Framework;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;

namespace Campus.Gen.Zones.CenterEast.Pinxue;

/// <summary>
/// 品学楼 gate frame and link bridges (MAP-QSH-CENTER-EAST §2.2). The 品学楼 entity's own parts are the frame bar between
/// A and B and the two bridges to C; OSM records all three as 16 m buildings standing on the ground, which would block
/// the passage and the paths under the bridges, so this site claims the entity and builds them itself: four square
/// columns carrying a two-storey beam bridge with the inscription plaque on its forecourt face (storeys 1–2 open as the
/// passage), and the A–C, B–C bridges over storeys 2–3. The wings themselves stay with WP-14 on the +1.5 m platform.
/// </summary>
public sealed class PinxueFrameSite : CenterEastSite
{
    /// <summary>Frame of the bar: X south along it from A's wall, +Z west out to the forecourt.</summary>
    internal static readonly PlanFrame F = PlanFrame.Toward(P.FrameNorthWest, P.FrameSouthWest);
    internal static float Length => Vector2.Distance(P.FrameNorthWest, P.FrameSouthWest);
    internal static float Axis => Length * 0.5f;

    public override string Id => "21a.pinxue-frame";

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(P.Complex), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            // Q1 (walk, K1): under the calligraphy beam's forecourt edge, looking up at the soffit and the plaque.
            var eye = F.P(Axis - 4f, 3.5f);
            return [Probe("Q1", P.Complex, new(eye.X, P.PlatformY + 1.62f, eye.Y), -F.W, 26f, 5f)];
        }
    }

    internal static float BaseY(BuildContext ctx) => ctx.World.Terrain.PadY(new(P.Complex));

    public override void Build(SiteBuilder b)
    {
        var y = BaseY(b.Context);
        Passage(b, y);
        foreach (var x in P.ColumnX) Column(b, x, y);
        Beam(b, y);
        foreach (var source in new[] { P.BridgeAC, P.BridgeBC }) LinkBridge(b, Bar(b.Context.World, source), y);
        SideDoor(b, y);
    }

    // ---- Passage floor and columns ----

    /// <summary>Paving under the frame: the bar is a building in OSM, so the ground leaves its footprint unpaved.</summary>
    private static void Passage(SiteBuilder b, float y)
    {
        var d = P.FrameDepth;
        b.Add(PartGeometryKernel.BeveledBox(Length, 0.14f, d, 0.01f, L.Paving), F.At(Axis, y - 0.12f, -d * 0.5f));
    }

    private static void Column(SiteBuilder b, float x, float y)
    {
        float s = P.ColumnSize, z = -P.FrameDepth * 0.5f;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.3f, P.ColumnPlinth + 0.1f, s + 0.3f, 0.03f, L.DarkGranite), F.At(x, y - 0.1f, z));
        // A shadow joint where the column meets the soffit: the beam reads as carried, not welded on.
        var joint = b.Detail == SiteDetail.Mass ? 0f : 0.12f;
        b.Add(PartGeometryKernel.BeveledBox(s, P.BeamBottom - P.ColumnPlinth - joint, s, 0.05f, L.Wall), F.At(x, y + P.ColumnPlinth, z));
        if (joint > 0) b.Add(PartGeometryKernel.BeveledBox(s - 0.16f, joint, s - 0.16f, 0.01f, L.Metal), F.At(x, y + P.BeamBottom - joint, z));
    }

    // ---- Beam bridge (storeys 3–4) with the inscription plaque ----

    private static void Beam(SiteBuilder b, float y)
    {
        var len = Length + 0.4f; // the ends sink into A's and B's walls
        Bridge(b, F, Axis - len * 0.5f, Axis + len * 0.5f, -P.FrameDepth, 0f, y + P.BeamBottom, y + P.BeamTop, frontSolid: true);
        if (b.Detail == SiteDetail.Mass) return;
        // Plaque: a slightly proud stone panel with a fine frame; the calligraphy "求实求真 大气大为" waits for the
        // scene text atlas (contract-26 follow-up) and is not painted on as a texture here.
        var mid = y + (P.BeamBottom + P.BeamTop) * 0.5f + 0.2f;
        b.Add(PartGeometryKernel.BeveledBox(P.PlaqueWidth + 0.3f, P.PlaqueHeight + 0.3f, P.PlaqueProud, 0.01f, L.Trim), F.At(Axis, mid - (P.PlaqueHeight + 0.3f) * 0.5f, P.PlaqueProud * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(P.PlaqueWidth, P.PlaqueHeight, 0.04f, 0.005f, L.Stone), F.At(Axis, mid - P.PlaqueHeight * 0.5f, P.PlaqueProud + 0.02f));
        if (b.Detail != SiteDetail.Full) return;
        // Two rows of soffit downlights over the passage, skipping the columns.
        for (var x = P.DownlightPitch * 0.5f; x < Length; x += P.DownlightPitch)
        {
            if (P.ColumnX.Any(c => MathF.Abs(c - x) < P.ColumnSize * 0.5f + 0.4f)) continue;
            foreach (var z in new[] { -P.FrameDepth * 0.25f, -P.FrameDepth * 0.75f })
                b.Add(Shapes.Shaft(P.DownlightRadius, P.DownlightRadius, 0.05f, 12, L.Metal), F.At(x, y + P.BeamBottom - 0.05f, z));
        }
    }

    /// <summary>
    /// A two-storey bridge box between x0 and x1 (local), z0 (back) to z1 (front): soffit, walls 0.45 m thick, roof and
    /// coping. Faces carry one ribbon window per storey with aluminium mullions; a solid front keeps the face plain.
    /// </summary>
    internal static void Bridge(SiteBuilder b, PlanFrame f, float x0, float x1, float z0, float z1, float yBottom, float yTop, bool frontSolid)
    {
        float len = x1 - x0, xc = (x0 + x1) * 0.5f, depth = z1 - z0, wall = 0.45f, soffit = 0.6f;
        b.Add(PartGeometryKernel.BeveledBox(len, soffit, depth, 0.03f, L.Wall), f.At(xc, yBottom, (z0 + z1) * 0.5f));
        var h = yTop - yBottom - soffit;
        b.Add(PartGeometryKernel.BeveledBox(len, 0.4f, depth - 2 * wall, 0.01f, L.RoofSlab), f.At(xc, yTop - 0.4f, (z0 + z1) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(len, 0.35f, depth + 0.2f, 0.03f, L.Trim), f.At(xc, yTop, (z0 + z1) * 0.5f));
        if (frontSolid) b.Add(PartGeometryKernel.BeveledBox(len, h, wall, 0.03f, L.Wall), f.At(xc, yBottom + soffit, z1 - wall * 0.5f));
        else WindowWall(b, f, xc, len, z1 - wall * 0.5f, yBottom, h, outward: 1f);
        WindowWall(b, f, xc, len, z0 + wall * 0.5f, yBottom, h, outward: -1f);
        if (b.Detail == SiteDetail.Mass) return;
        // Soffit edge band: a crisp horizontal line under the bridge on both faces.
        foreach (var (z, sign) in new[] { (z1, 1f), (z0, -1f) })
            b.Add(PartGeometryKernel.BeveledBox(len, 0.18f, 0.12f, 0.01f, L.Trim), f.At(xc, yBottom, z + sign * 0.05f));
    }

    /// <summary>A wall slab with one ribbon window per 4 m storey; glass set 0.3 m back; mullions every 1.8 m.</summary>
    private static void WindowWall(SiteBuilder b, PlanFrame f, float xc, float len, float zc, float yBottom, float h, float outward)
    {
        const float wall = 0.45f, soffit = 0.6f, inset = 1.2f;
        var storeys = Math.Max(1, (int)MathF.Round((h + soffit) / P.StoreyHeight));
        var openings = new List<IReadOnlyList<Vector2>>();
        float half = len * 0.5f - inset;
        var bands = new List<(float Sill, float Head)>();
        for (var k = 0; k < storeys; k++)
        {
            float sill = k * P.StoreyHeight + 1.0f - soffit, head = k * P.StoreyHeight + 3.1f - soffit;
            if (head > h - 0.3f) break;
            bands.Add((sill, head));
            openings.Add([new(-half, sill), new(half, sill), new(half, head), new(-half, head)]);
        }
        var y = yBottom + soffit;
        b.Add(PartGeometryKernel.WallPanel(len, h, wall, openings, L.Wall), f.At(xc, y, zc));
        if (b.Detail == SiteDetail.Mass) return;
        var glassZ = zc - outward * 0.12f;
        foreach (var (sill, head) in bands)
        {
            b.Add(PartGeometryKernel.BeveledBox(2 * half, head - sill, 0.04f, 0.005f, L.Glass), f.At(xc, y + sill, glassZ));
            b.Add(PartGeometryKernel.BeveledBox(2 * half + 0.1f, 0.08f, 0.32f, 0.01f, L.Trim), f.At(xc, y + sill - 0.08f, zc + outward * 0.12f));
            if (b.Detail != SiteDetail.Full) continue;
            var n = Math.Max(1, (int)MathF.Round(2 * half / 1.8f));
            for (var i = 1; i < n; i++)
                b.Add(PartGeometryKernel.BeveledBox(0.08f, head - sill, 0.1f, 0.01f, L.Metal), f.At(xc - half + 2 * half * i / n, y + sill, glassZ + outward * 0.05f));
            b.Add(PartGeometryKernel.BeveledBox(2 * half, 0.07f, 0.1f, 0.01f, L.Metal), f.At(xc, y + sill + (head - sill) * 0.72f, glassZ + outward * 0.05f));
        }
    }

    // ---- Link bridges A–C, B–C (storeys 2–3) ----

    /// <summary>Plan frame and local extents of a four-sided bar footprint: X along its longest edge.</summary>
    internal static (PlanFrame Frame, float X0, float X1, float Z0, float Z1) Bar(IWorldModel world, string sourceId)
    {
        var part = world.Features[new(P.Complex)].Parts.First(p => p.SourceId == sourceId);
        var ring = part.Geometry is PolygonGeometry polygon ? polygon.Rings.Outer
            : throw new InvalidOperationException($"Bridge part {sourceId} is not a single polygon.");
        var best = 0;
        for (var i = 1; i < ring.Count; i++)
            if (Vector2.Distance(ring[i], ring[(i + 1) % ring.Count]) > Vector2.Distance(ring[best], ring[(best + 1) % ring.Count])) best = i;
        var f = PlanFrame.Toward(ring[best], ring[(best + 1) % ring.Count]);
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        foreach (var p in ring)
        {
            var l = f.Local(p);
            x0 = MathF.Min(x0, l.X); x1 = MathF.Max(x1, l.X); z0 = MathF.Min(z0, l.Y); z1 = MathF.Max(z1, l.Y);
        }
        return (f, x0, x1, z0, z1);
    }

    private static void LinkBridge(SiteBuilder b, (PlanFrame Frame, float X0, float X1, float Z0, float Z1) bar, float y)
    {
        var (f, x0, x1, z0, z1) = bar;
        Bridge(b, f, x0 - 0.3f, x1 + 0.3f, z0, z1, y + P.BridgeBottom, y + P.BridgeTop, frontSolid: false);
        var segments = b.Detail == SiteDetail.Full ? 16 : 8;
        foreach (var (x, z) in BridgeColumns(bar))
        {
            b.Add(PartGeometryKernel.BeveledBox(0.8f, 0.35f, 0.8f, 0.02f, L.DarkGranite), f.At(x, y - 0.1f, z));
            b.Add(Shapes.Shaft(P.BridgeColumnRadius, P.BridgeColumnRadius * 0.9f, P.BridgeBottom - 0.25f, segments, L.Wall), f.At(x, y + 0.25f, z));
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

    // ---- Side door into A under the passage: entrance presentation (WLD-INTERIORS §4); the doorway is WP-31b's ----

    private static PlanFrame SideDoorFrame => new(F.P(0, P.SideDoorZ), -F.W);

    private static void SideDoor(SiteBuilder b, float y)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var d = SideDoorFrame;
        var floor = y + 0.15f;
        b.Place(PartCatalog.EntranceMat, new PanelParams(2.0f, 0.03f, 1.3f), d.At(0, y, 1.0f));
        b.Place(PartCatalog.OpenSign, new SignParams(0.42f, 0.24f, 0.02f), d.At(1.55f, floor + 1.45f, 0.3f));
        b.Place(PartCatalog.EntranceLamp, new LampParams(0.3f, 0.3f, 0.2f), d.At(0, floor + 2.9f, 0.3f));
    }

    public override void Collide(SiteColliders c)
    {
        var y = BaseY(c.Context);
        var z = -P.FrameDepth * 0.5f;
        foreach (var x in P.ColumnX)
            c.Box(F.At(x, y + P.BeamBottom * 0.5f, z), new(P.ColumnSize * 0.5f + 0.05f, P.BeamBottom * 0.5f, P.ColumnSize * 0.5f + 0.05f), SurfaceTag.Wall, FootMat.StonePaving);
        c.Box(F.At(Axis, y + (P.BeamBottom + P.BeamTop) * 0.5f, z), new(Length * 0.5f, (P.BeamTop - P.BeamBottom) * 0.5f, P.FrameDepth * 0.5f), SurfaceTag.Wall, FootMat.Concrete);
        foreach (var source in new[] { P.BridgeAC, P.BridgeBC })
        {
            var bar = Bar(c.Context.World, source);
            var (f, x0, x1, z0, z1) = bar;
            c.Box(f.At((x0 + x1) * 0.5f, y + (P.BridgeBottom + P.BridgeTop) * 0.5f, (z0 + z1) * 0.5f),
                new((x1 - x0) * 0.5f, (P.BridgeTop - P.BridgeBottom) * 0.5f, (z1 - z0) * 0.5f), SurfaceTag.Wall, FootMat.Concrete);
            foreach (var (x, zc) in BridgeColumns(bar))
                c.Add(new PostCollider(f.P(x, zc), P.BridgeColumnRadius + 0.05f, y, y + P.BridgeBottom, SurfaceTag.Prop) { Foot = FootMat.Concrete });
        }
    }
}
