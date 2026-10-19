using Campus.Gen.Geometry;
using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.South.Framework;
using Campus.Gen.World;
using T = Campus.Gen.Zones.South.SouthTune.Gate;
using L = Campus.Gen.Zones.South.SouthTune.Look;
using W = Campus.Gen.Zones.South.SouthTune.Windows;

namespace Campus.Gen.Zones.South.Gate;

/// <summary>
/// South gate (MAP-QSH-SOUTH): an open gateway with no gatehouse arch. Two flat-roofed grey stone guard houses, a stone
/// pillar on each traffic island beside the carriageway, barrier arms down across the carriageway, QR turnstiles closed in
/// the two pedestrian lanes, the low granite name wall to the left seen from the street, and the forecourt's stone spheres,
/// bollards and yellow no-parking grid. In walk mode the gate is the campus boundary: every gap is under 0.5 m and the
/// closure is a 2.2 m Boundary collider (§1.2, WLD-OUTSKIRTS). At night the guard-house windows are the gate's two small
/// lights (§11): they declare one grid on the all-night channel.
/// The OSM relation is a "building" in the source data, so this site claims it and WP-14 must not extrude it.
/// </summary>
public sealed class SouthGateSite : ZoneSite
{
    public static readonly string Feature = SouthIds.Gate.Entity;
    /// <summary>The guard houses' window grid (contract-29): three watch windows per house, one row per house.</summary>
    public const string HouseKey = "guard-houses";
    private const int HouseColumns = 3;

    /// <summary>
    /// The gate's plan, read from the relation's named source parts: west/east guard houses and traffic islands.
    /// Frame: origin midway between the houses, X from the west house to the east one, +Z outward to the street.
    /// </summary>
    internal sealed record Layout(PlanFrame Frame, float HouseX, float IslandWestX, float IslandEastX, float IslandZ, float IslandWidth, float IslandLength)
    {
        public Vector2 HouseWest => Frame.P(-HouseX, 0);

        public Vector2 HouseEast => Frame.P(HouseX, 0);

        public float CarriageA => IslandWestX + IslandWidth * 0.5f;

        public float CarriageB => IslandEastX - IslandWidth * 0.5f;

        /// <summary>Pedestrian lane between a guard house and its island, local x range; side −1 west, +1 east.</summary>
        public (float A, float B) Lane(int side) => side < 0
            ? (-HouseX + T.HouseSize * 0.5f, IslandWestX - IslandWidth * 0.5f)
            : (IslandEastX + IslandWidth * 0.5f, HouseX - T.HouseSize * 0.5f);
    }

    private static readonly ConditionalWeakTable<IWorldModel, Layout> Layouts = new();

    internal static Layout LayoutOf(IWorldModel world) => Layouts.GetValue(world, Read);

    private static Layout Read(IWorldModel world)
    {
        var gate = world.Feature(Feature);
        IReadOnlyList<Vector2> Ring(string sourceId) => gate.Contour(sourceId);
        Vector2[] houses = [PlanGeometry.Centroid(Ring(SouthIds.WestHouse.Part), meanIfDegenerate: true), PlanGeometry.Centroid(Ring(SouthIds.EastHouse.Part), meanIfDegenerate: true)];
        var frame = PlanFrame.Along((houses[0] + houses[1]) * 0.5f, houses[1] - houses[0]);
        IReadOnlyList<Vector2>[] islands = [Ring(SouthIds.WestIsland.Part), Ring(SouthIds.EastIsland.Part)];
        var at = islands.Select(r => frame.Local(PlanGeometry.Centroid(r, meanIfDegenerate: true))).ToArray();
        // An island is a thin rotated rectangle: its width and length are the means of its short and long sides.
        static float[] Sides(IReadOnlyList<Vector2> r) => [.. Enumerable.Range(0, r.Count).Select(i => Vector2.Distance(r[i], r[(i + 1) % r.Count])).Order()];
        var sides = islands.Select(Sides).ToArray();
        return new(frame, Vector2.Distance(houses[0], houses[1]) * 0.5f, at[0].X, at[1].X, (at[0].Y + at[1].Y) * 0.5f,
            sides.Average(e => (e[0] + e[1]) * 0.5f), sides.Average(e => (e[^1] + e[^2]) * 0.5f));
    }

    public override string Id => "18a.gate";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var g = LayoutOf(world);
        var houseHalf = T.HouseSize * .5f + T.HouseEave;
        var minX = MathF.Min(T.NameWallX0 - .1f, -g.HouseX - houseHalf);
        var maxX = MathF.Max(T.GridX1, g.HouseX + houseHalf);
        var minZ = MathF.Min(-houseHalf, g.IslandZ - g.IslandLength * .5f);
        var maxZ = MathF.Max(T.GridZ1, MathF.Max(T.NameWallZ + T.NameWallThickness,
            g.IslandZ + g.IslandLength * .5f + .8f));
        return [SiteRanges.Bounds(g.Frame.Rect(minX, minZ, maxX, maxZ), .15f)];
    }

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(Feature), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    public override IReadOnlyList<InspectionProbe> Probes =>
        // Q2: the west pedestrian lane, 1 m behind the turnstile row, looking out to the street (a stand, not map data).
        [Probe("Q2", Feature, new(-527.33f, WalkRules.StandingEye, 720.70f), new(-0.583f, 0.812f), -2f, 1f)];

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
    {
        var g = LayoutOf(world);
        return [.. new[] { -1, 1 }.Select(side => new PolygonRings(g.Frame.Shift(side * g.HouseX, 0).Rect(
            -T.HouseSize * 0.5f, -T.HouseSize * 0.5f, T.HouseSize * 0.5f, T.HouseSize * 0.5f)))];
    }

    public override void Build(SiteBuilder b)
    {
        var g = LayoutOf(b.Context.World);
        var y = b.Ground(g.Frame.Origin);
        foreach (var side in new[] { -1, 1 }) GuardHouse(b, g, side, y);
        foreach (var x in new[] { g.IslandWestX, g.IslandEastX }) Island(b, g, x, y);
        Barriers(b, g, y);
        foreach (var side in new[] { -1, 1 }) Turnstiles(b, g, side, y);
        NameWall(b, g, y);
        Forecourt(b, g, y);
    }

    private static void GuardHouse(SiteBuilder b, Layout g, int side, float y)
    {
        var f = g.Frame.Shift(side * g.HouseX, 0);
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
        if (numbered is { } allocation) WindowNumbers.Panes(b, Openings(g, y).Where(o => o.Row == (side < 0 ? 0 : 1)), allocation, W.AllNight, W.Office);
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
    internal static GlazedOpening[] Openings(Layout g, float y) =>
    [
        .. new[] { -1, 1 }.SelectMany((side, row) => Panes(side).Select((p, column) =>
        {
            var f = g.Frame.Shift(side * g.HouseX, 0);
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

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var g = LayoutOf(ctx.World);
        WindowNumbers.Declare(windows, Openings(g, ctx.World.Terrain.HeightAt(g.Frame.Origin.X, g.Frame.Origin.Y)), Allocation(ctx, true)!.Value, W.AllNight);
    }

    /// <summary>A window on a house face: dark glass in an aluminium surround over a stone sill. Turned panes sit on ±X faces.</summary>
    private static void Pane(SiteBuilder b, PlanFrame f, (float X, float Z, bool Turned, float Width) pane, float y, bool glass)
    {
        var (x, z, turned, width) = pane;
        float y0 = y + T.HouseWindowSill, y1 = y + T.HouseWindowHead, h = y1 - y0;
        var outward = turned ? MathF.Sign(x) : MathF.Sign(z);
        Matrix4x4 At(float along, float yy, float off) => turned
            ? f.AtTurned(x + outward * off, yy, z + along)
            : f.At(x + along, yy, z + outward * off);
        if (glass) b.Add(PartGeometryKernel.BeveledBox(width, h, 0.04f, 0, ZoneLooks.Glass), At(0, y0, 0.01f));
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
    private static void Island(SiteBuilder b, Layout g, float x, float y)
    {
        b.Add(PartGeometryKernel.BeveledBox(g.IslandWidth, T.IslandRise + 0.1f, g.IslandLength, 0.03f, L.Granite), g.Frame.At(x, y - 0.1f, g.IslandZ));
        var top = y + T.IslandRise;
        b.Add(PartGeometryKernel.BeveledBox(T.PillarSize + 0.3f, 0.5f, T.PillarSize + 0.3f, 0.03f, L.Granite), g.Frame.At(x, top, T.PillarZ));
        b.Add(PartGeometryKernel.BeveledBox(T.PillarSize, T.PillarHeight - 0.9f, T.PillarSize, 0.04f, L.Stone), g.Frame.At(x, top + 0.5f, T.PillarZ));
        b.Add(PartGeometryKernel.BeveledBox(T.PillarSize + 0.24f, 0.4f, T.PillarSize + 0.24f, 0.03f, L.StoneTrim), g.Frame.At(x, top + T.PillarHeight - 0.4f, T.PillarZ));
        if (b.Detail == SiteDetail.Mass) return;
        // Two shallow joint bands keep the pillar from reading as one bare extrusion (ART-FIDELITY).
        foreach (var k in new[] { 0.36f, 0.68f })
            b.Add(PartGeometryKernel.BeveledBox(T.PillarSize + 0.04f, 0.05f, T.PillarSize + 0.04f, 0.01f, L.StoneTrim), g.Frame.At(x, top + 0.5f + (T.PillarHeight - 0.9f) * k, T.PillarZ));
    }

    /// <summary>Barrier cabinets on the islands' inner edges, arms down across the carriageway, meeting with a small gap.</summary>
    private static void Barriers(SiteBuilder b, Layout g, float y)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var top = y + T.IslandRise;
        var mid = (g.CarriageA + g.CarriageB) * 0.5f;
        foreach (var side in new[] { -1, 1 })
        {
            var cabinet = side < 0 ? g.CarriageA - 0.3f : g.CarriageB + 0.3f;
            b.Add(PartGeometryKernel.BeveledBox(0.36f, 1.05f, 0.36f, 0.02f, L.Metal), g.Frame.At(cabinet, top, T.BarrierZ));
            var tip = mid + side * T.BarrierGap * 0.5f;
            var root = cabinet + side * -0.18f;
            var length = MathF.Abs(tip - root);
            b.Add(PartGeometryKernel.BeveledBox(length, 0.1f, 0.07f, 0.012f, L.WhiteSteel), g.Frame.At((tip + root) * 0.5f, y + T.BarrierHeight, T.BarrierZ));
            // A drop foot under the tip keeps the arm level.
            b.Add(PartGeometryKernel.BeveledBox(0.06f, T.BarrierHeight, 0.06f, 0.01f, L.Metal), g.Frame.At(tip + side * 0.15f, y, T.BarrierZ));
        }
    }

    /// <summary>Flap turnstiles: cabinets at both lane edges and between passages, glass flaps closed (gaps under 0.5 m).</summary>
    private static void Turnstiles(SiteBuilder b, Layout g, int side, float y)
    {
        var (a, c) = g.Lane(side);
        var cabinets = new float[T.TurnstilePassages + 1];
        var first = a + 0.1f + T.CabinetWidth * 0.5f;
        var last = c - 0.1f - T.CabinetWidth * 0.5f;
        for (var i = 0; i <= T.TurnstilePassages; i++) cabinets[i] = first + (last - first) * i / T.TurnstilePassages;
        b.Add(PartGeometryKernel.BeveledBox(c - a, 0.06f, T.CabinetDepth + 0.6f, 0.015f, L.Granite), g.Frame.At((a + c) * 0.5f, y - 0.04f, T.TurnstileZ));
        foreach (var x in cabinets)
        {
            b.Add(PartGeometryKernel.BeveledBox(T.CabinetWidth, T.CabinetHeight, T.CabinetDepth, 0.03f, L.Metal), g.Frame.At(x, y, T.TurnstileZ));
            if (b.Detail == SiteDetail.Full)
                b.Add(PartGeometryKernel.BeveledBox(T.CabinetWidth - 0.04f, 0.02f, 0.5f, 0, ZoneLooks.Glass), g.Frame.At(x, y + T.CabinetHeight, T.TurnstileZ + 0.35f));
        }
        if (b.Detail == SiteDetail.Mass) return;
        for (var i = 0; i < T.TurnstilePassages; i++)
        {
            var x0 = cabinets[i] + T.CabinetWidth * 0.5f;
            var x1 = cabinets[i + 1] - T.CabinetWidth * 0.5f;
            var flap = (x1 - x0 - 0.08f) * 0.5f;
            b.Add(PartGeometryKernel.BeveledBox(flap, 0.55f, 0.02f, 0, ZoneLooks.Glass), g.Frame.At(x0 + flap * 0.5f, y + 0.38f, T.TurnstileZ));
            b.Add(PartGeometryKernel.BeveledBox(flap, 0.55f, 0.02f, 0, ZoneLooks.Glass), g.Frame.At(x1 - flap * 0.5f, y + 0.38f, T.TurnstileZ));
        }
    }

    /// <summary>
    /// Name wall (§1.3): a low granite wall with one square slab per character on its street face. The characters
    /// (Guo Moruo's hand, ink green) need a glyph decal and a registered colour; the slabs stand blank until then.
    /// </summary>
    private static void NameWall(SiteBuilder b, Layout g, float y)
    {
        var length = T.NameWallX1 - T.NameWallX0;
        var cx = (T.NameWallX0 + T.NameWallX1) * 0.5f;
        float t = T.NameWallThickness, h = T.NameWallHeight;
        b.Add(PartGeometryKernel.BeveledBox(length + 0.12f, 0.35f, t + 0.16f, 0.02f, L.Granite), g.Frame.At(cx, y - 0.1f, T.NameWallZ));
        b.Add(PartGeometryKernel.BeveledBox(length, h - 0.25f - 0.1f, t, 0.03f, L.Stone), g.Frame.At(cx, y + 0.25f, T.NameWallZ));
        b.Add(PartGeometryKernel.BeveledBox(length + 0.1f, 0.1f, t + 0.1f, 0.02f, L.StoneTrim), g.Frame.At(cx, y + h - 0.1f, T.NameWallZ));
        if (b.Detail == SiteDetail.Mass) return;
        var slabY = y + 0.25f + (h - 0.35f - T.NameSlab) * 0.5f;
        for (var i = 0; i < T.NameCharacters; i++)
        {
            var x = cx + (i - (T.NameCharacters - 1) * 0.5f) * T.NameSlabPitch;
            b.Add(PartGeometryKernel.BeveledBox(T.NameSlab, T.NameSlab, 0.06f, 0.012f, L.StoneTrim), g.Frame.At(x, slabY, T.NameWallZ + t * 0.5f + 0.03f));
        }
    }

    /// <summary>
    /// Forecourt (§1.4): stone spheres before the guard houses, yellow-and-black crash bollards off the island noses (WP-13's
    /// variation 1 in its own colours: a tint or material would paint the black stripe yellow too), yellow grid lines.
    /// </summary>
    private static void Forecourt(SiteBuilder b, Layout g, float y)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var sphere = new FurnitureParams(T.SphereSize, T.SphereSize, T.SphereSize) { Material = L.Granite };
        foreach (var side in new[] { -1, 1 })
            for (var k = 0; k < 4; k++)
            {
                var x = side * (g.HouseX + T.HouseSize * 0.5f - 0.3f - k * T.SpherePitch);
                b.Place(PartCatalog.StoneSphere, sphere, g.Frame.At(x, y, T.SphereRowZ));
            }
        var bollard = new ColumnParams(0.18f, 0.8f, 0.18f) { Variation = 1 };
        foreach (var x in new[] { g.IslandWestX, g.IslandEastX })
            foreach (var d in new[] { -0.6f, 0f, 0.6f })
                b.Place(PartCatalog.Bollard, bollard, g.Frame.At(x + d, y, g.IslandZ + g.IslandLength * 0.5f + 0.7f));
        GridLines(b, g, y);
    }

    private static void GridLines(SiteBuilder b, Layout g, float y)
    {
        float x0 = T.GridX0, x1 = T.GridX1, z0 = T.GridZ0, z1 = T.GridZ1, w = T.GridLine;
        Line(b, g, new(x0, z0), new(x1, z0), y, w);
        Line(b, g, new(x1, z0), new(x1, z1), y, w);
        Line(b, g, new(x1, z1), new(x0, z1), y, w);
        Line(b, g, new(x0, z1), new(x0, z0), y, w);
        var step = T.GridPitch * MathF.Sqrt(2f);
        // x − z = c (rising diagonals) and x + z = c (falling diagonals), clipped to the box.
        for (var c = x0 - z1 + step * 0.5f; c < x1 - z0; c += step)
        {
            float za = MathF.Max(z0, x0 - c), zb = MathF.Min(z1, x1 - c);
            if (zb - za > 0.3f) Line(b, g, new(za + c, za), new(zb + c, zb), y, w * 0.8f);
        }
        for (var c = x0 + z0 + step * 0.5f; c < x1 + z1; c += step)
        {
            float za = MathF.Max(z0, c - x1), zb = MathF.Min(z1, c - x0);
            if (zb - za > 0.3f) Line(b, g, new(c - za, za), new(c - zb, zb), y, w * 0.8f);
        }
    }

    /// <summary>A painted strip between two local points, 2 cm proud of the paving.</summary>
    private static void Line(SiteBuilder b, Layout g, Vector2 p, Vector2 q, float y, float width)
    {
        var a = g.Frame.P(p.X, p.Y);
        var c = g.Frame.P(q.X, q.Y);
        var strip = PlanFrame.Along((a + c) * 0.5f, c - a);
        b.Add(PartGeometryKernel.BeveledBox(Vector2.Distance(a, c), 0.02f, width, 0, L.Marking), strip.At(0, y, 0));
    }

    public override void Collide(SiteColliders c)
    {
        var g = LayoutOf(c.Context.World);
        var y = c.Ground(g.Frame.Origin.X, g.Frame.Origin.Y);
        // Guard houses: their four walls are part of the closure.
        foreach (var side in new[] { -1, 1 })
        {
            var ring = g.Frame.Shift(side * g.HouseX, 0).Rect(-T.HouseSize * 0.5f, -T.HouseSize * 0.5f, T.HouseSize * 0.5f, T.HouseSize * 0.5f);
            for (var i = 0; i < 4; i++)
                c.Wall(ring[i], ring[(i + 1) % 4], 0.15f, y, y + T.HouseHeight + T.HouseRoof, SurfaceTag.Boundary, FootMat.StonePaving);
        }
        // Islands with their pillars, the carriageway behind the barrier arms, and the two turnstile rows.
        foreach (var x in new[] { g.IslandWestX, g.IslandEastX })
            c.Box(g.Frame.At(x, y + WalkRules.BoundaryHeight * 0.5f, g.IslandZ), new(g.IslandWidth * 0.5f, WalkRules.BoundaryHeight * 0.5f, g.IslandLength * 0.5f), SurfaceTag.Boundary, FootMat.StonePaving);
        c.Box(g.Frame.At((g.CarriageA + g.CarriageB) * 0.5f, y + WalkRules.BoundaryHeight * 0.5f, T.BarrierZ), new((g.CarriageB - g.CarriageA) * 0.5f + 0.05f, WalkRules.BoundaryHeight * 0.5f, 0.15f), SurfaceTag.Boundary, FootMat.Metal);
        foreach (var side in new[] { -1, 1 })
        {
            var (a, b) = g.Lane(side);
            c.Box(g.Frame.At((a + b) * 0.5f, y + WalkRules.BoundaryHeight * 0.5f, T.TurnstileZ), new((b - a) * 0.5f + 0.05f, WalkRules.BoundaryHeight * 0.5f, T.CabinetDepth * 0.5f), SurfaceTag.Boundary, FootMat.Metal);
        }
        // Name wall: an ordinary wall outside the closure.
        c.Wall(g.Frame.P(T.NameWallX0, T.NameWallZ), g.Frame.P(T.NameWallX1, T.NameWallZ), T.NameWallThickness * 0.5f, y, y + T.NameWallHeight, SurfaceTag.Wall, FootMat.StonePaving);
    }
}
