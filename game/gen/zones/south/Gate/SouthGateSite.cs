using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.South.Framework;
using T = Campus.Gen.Zones.South.SouthTune.Gate;
using L = Campus.Gen.Zones.South.SouthTune.Look;
using W = Campus.Gen.Zones.South.SouthTune.Windows;

namespace Campus.Gen.Zones.South.Gate;

/// <summary>
/// South gate (MAP-QSH-SOUTH §1): an open gateway with no gatehouse arch. Two flat-roofed grey stone guard houses, a stone
/// pillar on each traffic island beside the carriageway, barrier arms down across the carriageway, QR turnstiles closed in
/// the two pedestrian lanes, the low granite name wall to the left seen from the street, and the forecourt's stone spheres,
/// bollards and yellow no-parking grid. In walk mode the gate is the campus boundary: every gap is under 0.5 m and the
/// closure is a 2.2 m Boundary collider (§1.2, WLD-OUTSKIRTS §7.1). At night the guard-house windows are the gate's two small
/// lights (§11): they declare one grid on the all-night channel.
/// The OSM relation is a "building" in the source data, so this site claims it and WP-14 must not extrude it.
/// </summary>
public sealed class SouthGateSite : SouthSite
{
    public const string Feature = "qsh:relation:8407525";
    /// <summary>The guard houses' window grid (contract-29): three watch windows per house, one row per house.</summary>
    public const string HouseKey = "guard-houses";
    private const int HouseColumns = 3;

    private static readonly PlanFrame Frame = PlanFrame.Along((T.HouseWest + T.HouseEast) * 0.5f, T.HouseEast - T.HouseWest);
    private static readonly float HouseX = Vector2.Distance(T.HouseWest, T.HouseEast) * 0.5f;

    public override string Id => "18a.gate";

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(Feature), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            var (a, b) = Lane(-1);
            var eye = Frame.P((a + b) * 0.5f, T.TurnstileZ - T.CabinetDepth * 0.5f - 1f);
            return [Probe("Q2", Feature, new(eye.X, 1.62f, eye.Y), Frame.W, -2f, 1f)];
        }
    }

    /// <summary>Pedestrian lane between a guard house and its island, local x range; side −1 west, +1 east.</summary>
    internal static (float A, float B) Lane(int side) => side < 0
        ? (-HouseX + T.HouseSize * 0.5f, T.IslandWestX - T.IslandWidth * 0.5f)
        : (T.IslandEastX + T.IslandWidth * 0.5f, HouseX - T.HouseSize * 0.5f);

    private static float CarriageA => T.IslandWestX + T.IslandWidth * 0.5f;
    private static float CarriageB => T.IslandEastX - T.IslandWidth * 0.5f;

    public override void Build(SiteBuilder b)
    {
        var y = b.Ground(Frame.Origin);
        foreach (var side in new[] { -1, 1 }) GuardHouse(b, side, y);
        foreach (var x in new[] { T.IslandWestX, T.IslandEastX }) Island(b, x, y);
        Barriers(b, y);
        foreach (var side in new[] { -1, 1 }) Turnstiles(b, side, y);
        NameWall(b, y);
        Forecourt(b, y);
    }

    private static void GuardHouse(SiteBuilder b, int side, float y)
    {
        var f = Frame.Shift(side * HouseX, 0);
        float s = T.HouseSize, h = T.HouseHeight;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.14f, T.HousePlinth + 0.1f, s + 0.14f, 0.02f, L.Granite), f.At(0, y - 0.1f, 0));
        b.Add(PartGeometryKernel.BeveledBox(s, h - T.HousePlinth, s, 0.03f, L.Stone), f.At(0, y + T.HousePlinth, 0));
        var roof = s + 2 * T.HouseEave;
        b.Add(PartGeometryKernel.BeveledBox(roof, T.HouseRoof, roof, 0.03f, L.StoneTrim), f.At(0, y + h, 0));
        if (b.Detail != SiteDetail.Mass)
            b.Add(PartGeometryKernel.BeveledBox(roof - 0.5f, 0.04f, roof - 0.5f, 0.01f, L.RoofSlab), f.At(0, y + h + T.HouseRoof, 0));
        if (b.Detail == SiteDetail.Mass) return;
        // Glass: numbered panes once the map's window numbering is in the context (lit all night), plain glass without one.
        var numbered = Allocation(b.Context);
        foreach (var pane in Panes(side)) Pane(b, f, pane, y, numbered is null);
        if (numbered is { } allocation) WindowNumbers.Panes(b, Openings(y).Where(o => o.Row == (side < 0 ? 0 : 1)), allocation, W.AllNight, W.Office);
        Door(b, f, -side * 1.6f, -T.HouseSize * 0.5f, y);
    }

    /// <summary>
    /// The watch windows of one guard house in grid-column order: street face, lane face (turned onto the ±X face toward the
    /// lanes), campus face beside the door. Local to the house's frame.
    /// </summary>
    private static (float X, float Z, bool Turned, float Width)[] Panes(int side)
    {
        var half = T.HouseSize * 0.5f;
        return [(0, half, false, 3.6f), (-side * half, 0, true, 3.0f), (side * 0.9f, -half, false, 2.0f)];
    }

    /// <summary>
    /// The guard-house windows as the gate's one grid (contract-29): row 0 the west house, row 1 the east one, columns as in
    /// <see cref="Panes"/>; each pane's bottom centre on the glass plane just proud of the stone face.
    /// </summary>
    internal static GlazedOpening[] Openings(float y) =>
    [
        .. new[] { -1, 1 }.SelectMany((side, row) => Panes(side).Select((p, column) =>
        {
            var f = Frame.Shift(side * HouseX, 0);
            var outward = p.Turned ? MathF.Sign(p.X) : MathF.Sign(p.Z);
            var at = p.Turned ? f.P(p.X + outward * PaneZ, p.Z) : f.P(p.X, p.Z + outward * PaneZ);
            var normal = (p.Turned ? f.U : f.W) * outward;
            return new GlazedOpening(column, row, new(at.X, y + T.HouseWindowSill, at.Y), new(normal.X, 0, normal.Y), p.Width, T.HouseWindowHead - T.HouseWindowSill);
        })),
    ];

    /// <summary>Numbered panes stand this far in front of the stone face; the plain glass box sits between them and the wall.</summary>
    private const float PaneZ = 0.03f;

    /// <summary>The guard-house grid's allocation (null without a snapshot), checked against its six windows.</summary>
    private static WindowAllocation? Allocation(BuildContext ctx, bool required = false)
    {
        var feature = new FeatureId(Feature);
        var allocation = required ? WindowNumbers.Required(ctx, feature, HouseKey) : WindowNumbers.Allocation(ctx, feature, HouseKey);
        if (allocation is { } a) WindowNumbers.Fit(HouseColumns, 2, feature, HouseKey, a);
        return allocation;
    }

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) => [(new(Feature), new(HouseKey, HouseColumns, 2))];

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows) =>
        WindowNumbers.Declare(windows, Openings(ctx.World.Terrain.HeightAt(Frame.Origin.X, Frame.Origin.Y)), Allocation(ctx, true)!.Value, W.AllNight);

    /// <summary>A window on a house face: dark glass in an aluminium surround over a stone sill. Turned panes sit on ±X faces.</summary>
    private static void Pane(SiteBuilder b, PlanFrame f, (float X, float Z, bool Turned, float Width) pane, float y, bool glass)
    {
        var (x, z, turned, width) = pane;
        float y0 = y + T.HouseWindowSill, y1 = y + T.HouseWindowHead, h = y1 - y0;
        var outward = turned ? MathF.Sign(x) : MathF.Sign(z);
        Matrix4x4 At(float along, float yy, float off) => turned
            ? f.AtTurned(x + outward * off, yy, z + along)
            : f.At(x + along, yy, z + outward * off);
        if (glass) b.Add(PartGeometryKernel.BeveledBox(width, h, 0.04f, 0, L.Glass), At(0, y0, 0.01f));
        b.Add(PartGeometryKernel.BeveledBox(width + 0.12f, 0.06f, 0.1f, 0.01f, L.Metal), At(0, y1, 0.04f));
        b.Add(PartGeometryKernel.BeveledBox(width + 0.12f, 0.06f, 0.1f, 0.01f, L.Metal), At(0, y0 - 0.06f, 0.04f));
        b.Add(PartGeometryKernel.BeveledBox(width + 0.3f, 0.07f, 0.18f, 0.015f, L.StoneTrim), At(0, y0 - 0.13f, 0.08f));
        foreach (var u in new[] { -0.5f, 0f, 0.5f })
            b.Add(PartGeometryKernel.BeveledBox(0.06f, h, 0.08f, 0.01f, L.Metal), At(u * (width + 0.06f), y0, 0.04f));
    }

    private static void Door(SiteBuilder b, PlanFrame f, float x, float z, float y)
    {
        var outward = MathF.Sign(z);
        b.Add(PartGeometryKernel.BeveledBox(1.0f, 2.2f, 0.05f, 0, L.Metal), f.At(x, y + T.HousePlinth, z + outward * 0.02f));
        b.Add(PartGeometryKernel.BeveledBox(1.2f, 0.08f, 0.1f, 0.01f, L.StoneTrim), f.At(x, y + T.HousePlinth + 2.2f, z + outward * 0.05f));
        b.Add(PartGeometryKernel.BeveledBox(1.3f, 0.15f, 0.5f, 0.02f, L.Granite), f.At(x, y - 0.05f, z + outward * 0.25f));
    }

    /// <summary>Traffic island with its stone pillar on the street end and the barrier cabinet on the campus end.</summary>
    private static void Island(SiteBuilder b, float x, float y)
    {
        b.Add(PartGeometryKernel.BeveledBox(T.IslandWidth, T.IslandRise + 0.1f, T.IslandLength, 0.03f, L.Granite), Frame.At(x, y - 0.1f, T.IslandZ));
        var top = y + T.IslandRise;
        b.Add(PartGeometryKernel.BeveledBox(T.PillarSize + 0.3f, 0.5f, T.PillarSize + 0.3f, 0.03f, L.Granite), Frame.At(x, top, T.PillarZ));
        b.Add(PartGeometryKernel.BeveledBox(T.PillarSize, T.PillarHeight - 0.9f, T.PillarSize, 0.04f, L.Stone), Frame.At(x, top + 0.5f, T.PillarZ));
        b.Add(PartGeometryKernel.BeveledBox(T.PillarSize + 0.24f, 0.4f, T.PillarSize + 0.24f, 0.03f, L.StoneTrim), Frame.At(x, top + T.PillarHeight - 0.4f, T.PillarZ));
        if (b.Detail == SiteDetail.Mass) return;
        // Two shallow joint bands keep the pillar from reading as one bare extrusion (ART-FIDELITY §0).
        foreach (var k in new[] { 0.36f, 0.68f })
            b.Add(PartGeometryKernel.BeveledBox(T.PillarSize + 0.04f, 0.05f, T.PillarSize + 0.04f, 0.01f, L.StoneTrim), Frame.At(x, top + 0.5f + (T.PillarHeight - 0.9f) * k, T.PillarZ));
    }

    /// <summary>Barrier cabinets on the islands' inner edges, arms down across the carriageway, meeting with a small gap.</summary>
    private static void Barriers(SiteBuilder b, float y)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var top = y + T.IslandRise;
        var mid = (CarriageA + CarriageB) * 0.5f;
        foreach (var side in new[] { -1, 1 })
        {
            var cabinet = side < 0 ? CarriageA - 0.3f : CarriageB + 0.3f;
            b.Add(PartGeometryKernel.BeveledBox(0.36f, 1.05f, 0.36f, 0.02f, L.Metal), Frame.At(cabinet, top, T.BarrierZ));
            var tip = mid + side * T.BarrierGap * 0.5f;
            var root = cabinet + side * -0.18f;
            var length = MathF.Abs(tip - root);
            b.Add(PartGeometryKernel.BeveledBox(length, 0.1f, 0.07f, 0.012f, L.WhiteSteel), Frame.At((tip + root) * 0.5f, y + T.BarrierHeight, T.BarrierZ));
            // A drop foot under the tip keeps the arm level.
            b.Add(PartGeometryKernel.BeveledBox(0.06f, T.BarrierHeight, 0.06f, 0.01f, L.Metal), Frame.At(tip + side * 0.15f, y, T.BarrierZ));
        }
    }

    /// <summary>Flap turnstiles: cabinets at both lane edges and between passages, glass flaps closed (gaps under 0.5 m).</summary>
    private static void Turnstiles(SiteBuilder b, int side, float y)
    {
        var (a, c) = Lane(side);
        var cabinets = new float[T.TurnstilePassages + 1];
        var first = a + 0.1f + T.CabinetWidth * 0.5f;
        var last = c - 0.1f - T.CabinetWidth * 0.5f;
        for (var i = 0; i <= T.TurnstilePassages; i++) cabinets[i] = first + (last - first) * i / T.TurnstilePassages;
        b.Add(PartGeometryKernel.BeveledBox(c - a, 0.06f, T.CabinetDepth + 0.6f, 0.015f, L.Granite), Frame.At((a + c) * 0.5f, y - 0.04f, T.TurnstileZ));
        foreach (var x in cabinets)
        {
            b.Add(PartGeometryKernel.BeveledBox(T.CabinetWidth, T.CabinetHeight, T.CabinetDepth, 0.03f, L.Metal), Frame.At(x, y, T.TurnstileZ));
            if (b.Detail == SiteDetail.Full)
                b.Add(PartGeometryKernel.BeveledBox(T.CabinetWidth - 0.04f, 0.02f, 0.5f, 0, L.Glass), Frame.At(x, y + T.CabinetHeight, T.TurnstileZ + 0.35f));
        }
        if (b.Detail == SiteDetail.Mass) return;
        for (var i = 0; i < T.TurnstilePassages; i++)
        {
            var x0 = cabinets[i] + T.CabinetWidth * 0.5f;
            var x1 = cabinets[i + 1] - T.CabinetWidth * 0.5f;
            var flap = (x1 - x0 - 0.08f) * 0.5f;
            b.Add(PartGeometryKernel.BeveledBox(flap, 0.55f, 0.02f, 0, L.Glass), Frame.At(x0 + flap * 0.5f, y + 0.38f, T.TurnstileZ));
            b.Add(PartGeometryKernel.BeveledBox(flap, 0.55f, 0.02f, 0, L.Glass), Frame.At(x1 - flap * 0.5f, y + 0.38f, T.TurnstileZ));
        }
    }

    /// <summary>
    /// Name wall (§1.3): a low granite wall with one square slab per character on its street face. The characters
    /// (Guo Moruo's hand, ink green) need a glyph decal and a registered colour; the slabs stand blank until then.
    /// </summary>
    private static void NameWall(SiteBuilder b, float y)
    {
        var length = T.NameWallX1 - T.NameWallX0;
        var cx = (T.NameWallX0 + T.NameWallX1) * 0.5f;
        float t = T.NameWallThickness, h = T.NameWallHeight;
        b.Add(PartGeometryKernel.BeveledBox(length + 0.12f, 0.35f, t + 0.16f, 0.02f, L.Granite), Frame.At(cx, y - 0.1f, T.NameWallZ));
        b.Add(PartGeometryKernel.BeveledBox(length, h - 0.25f - 0.1f, t, 0.03f, L.Stone), Frame.At(cx, y + 0.25f, T.NameWallZ));
        b.Add(PartGeometryKernel.BeveledBox(length + 0.1f, 0.1f, t + 0.1f, 0.02f, L.StoneTrim), Frame.At(cx, y + h - 0.1f, T.NameWallZ));
        if (b.Detail == SiteDetail.Mass) return;
        var slabY = y + 0.25f + (h - 0.35f - T.NameSlab) * 0.5f;
        for (var i = 0; i < T.NameCharacters; i++)
        {
            var x = cx + (i - (T.NameCharacters - 1) * 0.5f) * T.NameSlabPitch;
            b.Add(PartGeometryKernel.BeveledBox(T.NameSlab, T.NameSlab, 0.06f, 0.012f, L.StoneTrim), Frame.At(x, slabY, T.NameWallZ + t * 0.5f + 0.03f));
        }
    }

    /// <summary>Forecourt (§1.4): stone spheres before the guard houses, bollards off the island noses, yellow grid lines.</summary>
    private static void Forecourt(SiteBuilder b, float y)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var sphere = new FurnitureParams(T.SphereSize, T.SphereSize, T.SphereSize) { Material = L.Granite };
        foreach (var side in new[] { -1, 1 })
            for (var k = 0; k < 4; k++)
            {
                var x = side * (HouseX + T.HouseSize * 0.5f - 0.3f - k * T.SpherePitch);
                b.Place(PartCatalog.StoneSphere, sphere, Frame.At(x, y, T.SphereRowZ));
            }
        var bollard = new ColumnParams(0.18f, 0.8f, 0.18f) { Material = L.Marking };
        foreach (var x in new[] { T.IslandWestX, T.IslandEastX })
            foreach (var d in new[] { -0.6f, 0f, 0.6f })
                b.Place(PartCatalog.Bollard, bollard, Frame.At(x + d, y, T.IslandZ + T.IslandLength * 0.5f + 0.7f), new PaletteToken("ground.markingYellow.lit"));
        GridLines(b, y);
    }

    private static void GridLines(SiteBuilder b, float y)
    {
        float x0 = T.GridX0, x1 = T.GridX1, z0 = T.GridZ0, z1 = T.GridZ1, w = T.GridLine;
        Line(b, new(x0, z0), new(x1, z0), y, w);
        Line(b, new(x1, z0), new(x1, z1), y, w);
        Line(b, new(x1, z1), new(x0, z1), y, w);
        Line(b, new(x0, z1), new(x0, z0), y, w);
        var step = T.GridPitch * MathF.Sqrt(2f);
        // x − z = c (rising diagonals) and x + z = c (falling diagonals), clipped to the box.
        for (var c = x0 - z1 + step * 0.5f; c < x1 - z0; c += step)
        {
            float za = MathF.Max(z0, x0 - c), zb = MathF.Min(z1, x1 - c);
            if (zb - za > 0.3f) Line(b, new(za + c, za), new(zb + c, zb), y, w * 0.8f);
        }
        for (var c = x0 + z0 + step * 0.5f; c < x1 + z1; c += step)
        {
            float za = MathF.Max(z0, c - x1), zb = MathF.Min(z1, c - x0);
            if (zb - za > 0.3f) Line(b, new(c - za, za), new(c - zb, zb), y, w * 0.8f);
        }
    }

    /// <summary>A painted strip between two local points, 2 cm proud of the paving.</summary>
    private static void Line(SiteBuilder b, Vector2 p, Vector2 q, float y, float width)
    {
        var a = Frame.P(p.X, p.Y);
        var c = Frame.P(q.X, q.Y);
        var strip = PlanFrame.Along((a + c) * 0.5f, c - a);
        b.Add(PartGeometryKernel.BeveledBox(Vector2.Distance(a, c), 0.02f, width, 0, L.Marking), strip.At(0, y, 0));
    }

    public override void Collide(SiteColliders c)
    {
        var y = c.Ground(Frame.Origin.X, Frame.Origin.Y);
        // Guard houses: their four walls are part of the closure.
        foreach (var side in new[] { -1, 1 })
        {
            var ring = Frame.Shift(side * HouseX, 0).Rect(-T.HouseSize * 0.5f, -T.HouseSize * 0.5f, T.HouseSize * 0.5f, T.HouseSize * 0.5f);
            for (var i = 0; i < 4; i++)
                c.Wall(ring[i], ring[(i + 1) % 4], 0.15f, y, y + T.HouseHeight + T.HouseRoof, SurfaceTag.Boundary, FootMat.StonePaving);
        }
        // Islands with their pillars, the carriageway behind the barrier arms, and the two turnstile rows.
        foreach (var x in new[] { T.IslandWestX, T.IslandEastX })
            c.Box(Frame.At(x, y + T.BoundaryTop * 0.5f, T.IslandZ), new(T.IslandWidth * 0.5f, T.BoundaryTop * 0.5f, T.IslandLength * 0.5f), SurfaceTag.Boundary, FootMat.StonePaving);
        c.Box(Frame.At((CarriageA + CarriageB) * 0.5f, y + T.BoundaryTop * 0.5f, T.BarrierZ), new((CarriageB - CarriageA) * 0.5f + 0.05f, T.BoundaryTop * 0.5f, 0.15f), SurfaceTag.Boundary, FootMat.Metal);
        foreach (var side in new[] { -1, 1 })
        {
            var (a, b) = Lane(side);
            c.Box(Frame.At((a + b) * 0.5f, y + T.BoundaryTop * 0.5f, T.TurnstileZ), new((b - a) * 0.5f + 0.05f, T.BoundaryTop * 0.5f, T.CabinetDepth * 0.5f), SurfaceTag.Boundary, FootMat.Metal);
        }
        // Name wall: an ordinary wall outside the closure.
        c.Wall(Frame.P(T.NameWallX0, T.NameWallZ), Frame.P(T.NameWallX1, T.NameWallZ), T.NameWallThickness * 0.5f, y, y + T.NameWallHeight, SurfaceTag.Wall, FootMat.StonePaving);
    }
}
