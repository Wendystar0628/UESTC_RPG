using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.South.Framework;
using P = Campus.Gen.Zones.South.SouthTune.Platform;
using S = Campus.Gen.Zones.South.SouthTune.Sports;
using L = Campus.Gen.Zones.South.SouthTune.Look;
using W = Campus.Gen.Zones.South.SouthTune.Windows;

namespace Campus.Gen.Zones.South.Sports;

/// <summary>
/// The sports platform (大平台, MAP-QSH-SOUTH, OSM "未命名 0526"), which WP-14 would extrude as a plain box, so this site
/// claims it:
/// - a 5.0 m deck (the gym's and natatorium's platform storey) with a white fascia, a light joint grid on its paving and 1.1 m
///   parapet panels along every free edge; edges that meet the gym, the natatorium or the stand stay open;
/// - under the deck, a round-column street arcade along the free edges in front of a closed shop block (§7.4, WLD-INTERIORS
///   §5.2) with glazed shopfronts, blank sign boards and closed glass doors; the service roads keep running under the deck;
/// - the grand steps in the south edge's notch and the OSM steps at the north tip, both catalogue flights (PartCatalog.Steps).
/// The shopfront glass is one grid (contract-29), a column per bay round the shop block.
/// </summary>
public sealed class PlatformSite : SouthSite
{
    public const string Feature = "qsh:way:601600526";
    private const string Part = "osm:way:601600526";
    public const string ShopKey = "arcade-shops";
    /// <summary>The venues the deck meets: their footprints close the deck's edge.</summary>
    private static readonly string[] Neighbours = [GymSite.Feature, NatatoriumSite.Feature, "qsh:way:217855276"];

    private const float PaneZ = 0.06f;

    public override string Id => "18c.platform";

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(Feature), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    /// <summary>A deck edge: its ends, outward unit normal and whether it is free (no venue or deck beyond it).</summary>
    internal readonly record struct Edge(Vector2 A, Vector2 B, Vector2 Out, bool Free)
    {
        public float Length => Vector2.Distance(A, B);
        public Vector2 Dir => Vector2.Normalize(B - A);
        public Vector2 Mid => (A + B) * 0.5f;
        /// <summary>A frame on the edge's midpoint whose local +Z points outward.</summary>
        public PlanFrame Frame => new(Mid, new(Out.Y, -Out.X));
    }

    /// <summary>One shopfront bay on the shop block: frame on the bay centre (+Z out of the block), width, door or not.</summary>
    internal readonly record struct Bay(PlanFrame Frame, float Width, bool Door);

    /// <summary>A stair down from the deck: frame on the top centre of the deck edge, +Z outward and downhill.</summary>
    internal readonly record struct Stair(PlanFrame Frame, float Width);

    /// <summary>The LOD-free plan every output shares.</summary>
    internal sealed record Plan(float Y, IReadOnlyList<Vector2[]> Decks, IReadOnlyList<Edge> Edges, Vector2[] Core, IReadOnlyList<Bay> Bays,
        IReadOnlyList<Vector2> Columns, IReadOnlyList<Stair> Stairs)
    {
        public float Top => Y + S.PlatformTop;
        public float Soffit => Top - P.Slab;
    }

    internal static Plan PlanOf(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(Feature), out var feature))
            throw new InvalidOperationException($"Platform {Feature} is missing from the world model.");
        var deck = Ccw(OuterOf(feature.Parts.FirstOrDefault(p => p.SourceId == Part) is { } src ? PlanGeometry.FacadeGeometryOf(src) : feature.Geometry)
            ?? throw new InvalidOperationException($"Platform {Feature} is not a polygon."));
        var decks = new List<Vector2[]> { deck };
        var y = world.Terrain.HeightAt(PlanMath.Centroid(deck).X, PlanMath.Centroid(deck).Y);

        var closed = new List<Vector2[]>(decks);
        foreach (var id in Neighbours)
            if (world.Features.TryGetValue(new(id), out var n) && OuterOf(n.Parts.Count > 0 ? PlanGeometry.FacadeGeometryOf(n.Parts[0]) : n.Geometry) is { } ring) closed.Add(ring);
        var edges = new List<Edge>();
        foreach (var ring in decks)
            for (var i = 0; i < ring.Length; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Length];
                if (Vector2.Distance(a, b) < 0.05f) continue;
                var d = Vector2.Normalize(b - a);
                var out_ = new Vector2(d.Y, -d.X);
                var probe = (a + b) * 0.5f + out_ * P.AbutProbe;
                edges.Add(new(a, b, out_, !closed.Any(r => PlanGeometry.InsideByCrossings(r, probe))));
            }

        var stairs = new List<Stair>();
        if (Nearest(edges, P.GrandStairAt) is { } grand) stairs.Add(new(grand.Frame.Shift(grand.Frame.Local(P.GrandStairAt).X, 0), P.GrandStairWidth));
        var down = Vector2.Normalize(P.NorthStairDown);
        stairs.Add(new(new(P.NorthStairTop, new(down.Y, -down.X)), P.NorthStairWidth));

        var core = Ccw(P.Core);
        var bays = new List<Bay>();
        for (var i = 0; i < core.Length; i++)
        {
            Vector2 a = core[i], b = core[(i + 1) % core.Length];
            var length = Vector2.Distance(a, b);
            if (length < 3.5f) continue;
            var d = (b - a) / length;
            var n = Math.Max(1, (int)MathF.Round(length / P.ShopBay));
            for (var k = 0; k < n; k++)
            {
                var c = a + d * (length * (k + 0.5f) / n);
                bays.Add(new(new(c, -d), length / n, bays.Count % 2 == 1 && length / n >= P.DoorWidth + 1.6f));
            }
        }

        var roads = Carriageways(world);
        var columns = new List<Vector2>();
        foreach (var e in edges.Where(e => e.Free && e.Length >= 4f))
        {
            var n = Math.Max(1, (int)MathF.Ceiling((e.Length - 2f * P.ColumnEnd) / P.ColumnPitch));
            for (var k = 0; k <= n; k++)
            {
                var c = Vector2.Lerp(e.A, e.B, (P.ColumnEnd + (e.Length - 2f * P.ColumnEnd) * k / n) / e.Length) - e.Out * P.ColumnInset;
                if (PlanGeometry.InsideByCrossings(core, c) || !decks.Any(r => PlanGeometry.InsideByCrossings(r, c)) || columns.Any(o => Vector2.Distance(o, c) < 3f)) continue;
                if (roads.Any(r => Distance(r.Line, c) < r.HalfWidth + P.ColumnRadius + 0.3f)) continue;
                if (stairs.Any(s => InStair(s, c, 1.0f))) continue;
                columns.Add(c);
            }
        }
        return new(y, decks, edges, core, bays, columns, stairs);
    }

    // ---- Window grid: one pane per shopfront bay (the transom light over a door bay) ----

    internal static GlazedOpening[] Openings(Plan p)
    {
        var floor = p.Y + SouthTune.Plaza.PavingTop;
        var list = new GlazedOpening[p.Bays.Count];
        for (var i = 0; i < p.Bays.Count; i++)
        {
            var bay = p.Bays[i];
            var o = bay.Frame.P(0, PaneZ);
            var outward = new Vector3(bay.Frame.W.X, 0, bay.Frame.W.Y);
            list[i] = bay.Door
                ? new(i, 0, new(o.X, floor + P.DoorHeight + 0.12f, o.Y), outward, P.DoorWidth, P.ShopHead - P.DoorHeight - 0.12f)
                : new(i, 0, new(o.X, floor + P.ShopSill, o.Y), outward, bay.Width - P.Pilaster - 0.04f, P.ShopHead - P.ShopSill);
        }
        return list;
    }

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) =>
        [(new(Feature), new(ShopKey, PlanOf(world).Bays.Count, 1))];

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var p = PlanOf(ctx.World);
        WindowNumbers.Declare(windows, Openings(p), Allocation(ctx, p, true)!.Value, W.General);
    }

    private static WindowAllocation? Allocation(BuildContext ctx, Plan p, bool required = false)
    {
        var feature = new FeatureId(Feature);
        var allocation = required ? WindowNumbers.Required(ctx, feature, ShopKey) : WindowNumbers.Allocation(ctx, feature, ShopKey);
        if (allocation is { } a) WindowNumbers.Fit(p.Bays.Count, 1, feature, ShopKey, a);
        return allocation;
    }

    // ---- Geometry ----

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
    {
        var p = PlanOf(world);
        return [.. p.Decks.Select(r => new PolygonRings(r)), new(p.Core)];
    }

    public override void Build(SiteBuilder b)
    {
        var p = PlanOf(b.Context.World);
        Deck(b, p);
        Core(b, p);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var c in p.Columns) Column(b, p, c);
        Shopfronts(b, p);
        foreach (var e in p.Edges.Where(e => e.Free)) Parapet(b, p, e);
        foreach (var s in p.Stairs) Flights(b, p, s);
        if (b.Detail == SiteDetail.Full) Joints(b, p);
    }

    /// <summary>The deck: a white slab under the top paving, a fascia band on every free edge.</summary>
    private static void Deck(SiteBuilder b, Plan p)
    {
        foreach (var ring in p.Decks)
        {
            b.Add(PartGeometryKernel.Extrude(new PolygonRings(ring), P.Slab - 0.15f, L.White, p.Soffit), Matrix4x4.Identity);
            b.Add(PartGeometryKernel.Extrude(new PolygonRings(ring), 0.15f, L.DeckPaving, p.Top - 0.15f), Matrix4x4.Identity);
        }
        var bevel = b.Detail == SiteDetail.Full ? 0.03f : 0;
        foreach (var e in p.Edges.Where(e => e.Free))
            b.Add(PartGeometryKernel.BeveledBox(e.Length + 0.24f, P.Fascia, 0.4f, bevel, L.White), e.Frame.At(0, p.Top - P.Fascia + 0.02f, P.FasciaProud - 0.2f));
    }

    /// <summary>The closed shop block under the deck, up to the soffit.</summary>
    private static void Core(SiteBuilder b, Plan p) =>
        b.Add(PartGeometryKernel.Extrude(new PolygonRings(p.Core), p.Soffit - p.Y + 0.05f, L.Sand, p.Y - 0.1f), Matrix4x4.Identity);

    private static void Column(SiteBuilder b, Plan p, Vector2 c)
    {
        var floor = p.Y + SouthTune.Plaza.PavingTop;
        var full = b.Detail == SiteDetail.Full;
        var at = Matrix4x4.CreateTranslation(c.X, 0, c.Y);
        b.Add(PartGeometryKernel.BeveledBox(1.0f, 0.4f, 1.0f, full ? 0.03f : 0, L.Granite), at * Matrix4x4.CreateTranslation(0, floor - 0.1f, 0));
        b.Add(Shapes.Shaft(P.ColumnRadius, P.ColumnRadius * 0.94f, p.Soffit - floor - 0.6f, full ? 20 : 12, L.DomeWhite),
            at * Matrix4x4.CreateTranslation(0, floor + 0.3f, 0));
        b.Add(PartGeometryKernel.BeveledBox(0.95f, 0.32f, 0.95f, full ? 0.03f : 0, L.StoneTrim), at * Matrix4x4.CreateTranslation(0, p.Soffit - 0.32f, 0));
    }

    /// <summary>
    /// Shopfronts on the shop block: a pilaster at each bay end, a stone sill, glass (numbered when the snapshot is there), a
    /// blank sign board above; door bays carry a closed glass door (PartCatalog.DoorGlass) with side lights and a transom.
    /// </summary>
    private static void Shopfronts(SiteBuilder b, Plan p)
    {
        var floor = p.Y + SouthTune.Plaza.PavingTop;
        var full = b.Detail == SiteDetail.Full;
        foreach (var bay in p.Bays)
        {
            var f = bay.Frame;
            float hw = bay.Width * 0.5f, inner = hw - P.Pilaster * 0.5f;
            GridFacade.Box(b, f, -hw, -hw + P.Pilaster, floor - 0.1f, p.Soffit, -0.05f, 0.16f, L.Sand, full ? 0.02f : 0);
            GridFacade.Box(b, f, -inner, inner, p.Y + P.SignBottom, p.Y + P.SignTop, -0.05f, 0.2f, L.White, full ? 0.02f : 0);
            if (bay.Door)
            {
                var side = inner - P.DoorWidth * 0.5f - 0.1f;
                foreach (var s in new[] { -1f, 1f })
                {
                    var x0 = s < 0 ? -inner : P.DoorWidth * 0.5f + 0.1f;
                    GridFacade.Box(b, f, x0, x0 + side, floor - 0.1f, floor + P.ShopSill, -0.05f, 0.12f, L.Granite, full ? 0.01f : 0);
                    GridFacade.Box(b, f, x0, x0 + side, floor + P.ShopSill, floor + P.ShopHead, 0, 0.05f, L.Glass, 0);
                }
                GridFacade.Box(b, f, -P.DoorWidth * 0.5f - 0.1f, P.DoorWidth * 0.5f + 0.1f, floor + P.DoorHeight, floor + P.ShopHead, 0, 0.05f, L.Glass, 0);
                b.Place(PartCatalog.DoorGlass, new DoorParams(P.DoorWidth, P.DoorHeight, 0.12f, 0, 2) { Foot = FootMat.Glass }, f.AtReversed(0, floor, 0.1f));
                GridFacade.Box(b, f, -inner, inner, floor + P.DoorHeight, floor + P.DoorHeight + 0.1f, 0, 0.12f, L.Metal, 0);
            }
            else
            {
                GridFacade.Box(b, f, -inner, inner, floor - 0.1f, floor + P.ShopSill, -0.05f, 0.12f, L.Granite, full ? 0.01f : 0);
                GridFacade.Box(b, f, -inner, inner, floor + P.ShopSill, floor + P.ShopHead, 0, 0.05f, L.Glass, 0);
                // Two slim mullions split the window into three lights.
                foreach (var x in new[] { -inner / 3f, inner / 3f })
                    GridFacade.Box(b, f, x - 0.04f, x + 0.04f, floor + P.ShopSill, floor + P.ShopHead, 0, 0.1f, L.Metal, 0);
            }
            GridFacade.Box(b, f, -inner, inner, floor + P.ShopHead, floor + P.ShopHead + 0.1f, 0, 0.12f, L.Metal, 0);
        }
        // The last bay's far pilaster closes each run: one per block corner.
        for (var i = 0; i < p.Core.Length; i++)
        {
            var c = p.Core[i];
            GridFacade.Box(b, new(c, new(1, 0)), -0.3f, 0.3f, floor - 0.1f, p.Soffit, -0.3f, 0.3f, L.Sand, full ? 0.02f : 0);
        }
        if (Allocation(b.Context, p) is { } allocation) WindowNumbers.Panes(b, Openings(p), allocation, W.General, W.Office);
    }

    /// <summary>Parapet panels (PartCatalog.Railing, solid infill) along a free edge, open where a stair lands.</summary>
    private static void Parapet(SiteBuilder b, Plan p, Edge e)
    {
        foreach (var (rail, at) in ParapetPieces(p, e)) b.Place(PartCatalog.Railing, rail, at);
    }

    private static IEnumerable<(RailingParams Rail, Matrix4x4 At)> ParapetPieces(Plan p, Edge e)
    {
        var f = e.Frame;
        var half = e.Length * 0.5f;
        var cuts = new List<(float U0, float U1)>();
        foreach (var s in p.Stairs)
        {
            var l = f.Local(s.Frame.Origin);
            if (MathF.Abs(l.Y) < 1.0f) cuts.Add((l.X - s.Width * 0.5f - 0.15f, l.X + s.Width * 0.5f + 0.15f));
        }
        foreach (var (u0, u1) in GridFacade.Open(-half, half, cuts))
        {
            var length = u1 - u0;
            if (length < 0.3f) continue;
            var n = (int)MathF.Ceiling(length / 6f);
            for (var k = 0; k < n; k++)
            {
                var rail = new RailingParams(length / n, P.Parapet, P.ParapetDepth, 1.5f) { Variation = 1, Material = L.White, Foot = FootMat.Concrete };
                yield return (rail, f.At(u0 + length * (k + 0.5f) / n, p.Top, -P.ParapetInset));
            }
        }
    }

    private static readonly StepsPart Steps = new();

    private static StairParams Flight(float width) =>
        new(width, S.PlatformTop * 0.5f, P.RisersPerFlight * P.StairTread, P.RisersPerFlight) { Variation = 0, Material = L.Granite, Foot = FootMat.StonePaving };

    private static float FlightDepth => P.RisersPerFlight * P.StairTread;

    /// <summary>
    /// Two catalogue flights and a landing down from the deck edge, outward along the stair frame's +Z; a stone mass carries the
    /// upper flight and the landing.
    /// </summary>
    private static void Flights(SiteBuilder b, Plan p, Stair s)
    {
        var f = s.Frame;
        var flight = Flight(s.Width);
        float d = FlightDepth, half = S.PlatformTop * 0.5f;
        var full = b.Detail == SiteDetail.Full;
        b.Place(PartCatalog.Steps, flight, f.AtReversed(0, p.Y + half, d * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(s.Width, half, d + P.StairLanding, full ? 0.03f : 0, L.Granite), f.At(0, p.Y - 0.1f, (d + P.StairLanding) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(s.Width, 0.1f, P.StairLanding, full ? 0.02f : 0, L.Granite), f.At(0, p.Y + half - 0.1f, d + P.StairLanding * 0.5f));
        foreach (var x in new[] { -s.Width * 0.5f + 0.15f, s.Width * 0.5f - 0.15f })
            b.Add(PartGeometryKernel.BeveledBox(0.3f, half + 1.0f, P.StairLanding, full ? 0.02f : 0, L.StoneTrim), f.At(x, p.Y - 0.1f, d + P.StairLanding * 0.5f));
        b.Place(PartCatalog.Steps, flight, f.AtReversed(0, p.Y, d + P.StairLanding + d * 0.5f));
    }

    /// <summary>A light joint grid on the deck paving, every <see cref="P.JointPitch"/> m in world x and z, clipped to each deck ring.</summary>
    private static void Joints(SiteBuilder b, Plan p)
    {
        foreach (var ring in p.Decks)
            foreach (var turned in new[] { false, true })
            {
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var v in ring) { var c = turned ? v.Y : v.X; lo = MathF.Min(lo, c); hi = MathF.Max(hi, c); }
                for (var line = MathF.Ceiling(lo / P.JointPitch) * P.JointPitch; line < hi; line += P.JointPitch)
                {
                    var hits = new List<float>();
                    for (var i = 0; i < ring.Length; i++)
                    {
                        Vector2 a = ring[i], c = ring[(i + 1) % ring.Length];
                        float ca = turned ? a.Y : a.X, cc = turned ? c.Y : c.X;
                        if ((ca - line) * (cc - line) >= 0) continue;
                        var t = (line - ca) / (cc - ca);
                        hits.Add(turned ? a.X + (c.X - a.X) * t : a.Y + (c.Y - a.Y) * t);
                    }
                    hits.Sort();
                    for (var k = 0; k + 1 < hits.Count; k += 2)
                    {
                        float s0 = hits[k] + 0.6f, s1 = hits[k + 1] - 0.6f;
                        if (s1 - s0 < 1f) continue;
                        var mid = (s0 + s1) * 0.5f;
                        var at = turned ? new PlanFrame(new(mid, line), new(1, 0)) : new PlanFrame(new(line, mid), new(0, 1));
                        b.Add(PartGeometryKernel.BeveledBox(s1 - s0, 0.012f, P.JointWidth, 0, L.DeckJoint), at.At(0, p.Top, 0));
                    }
                }
            }
    }

    // ---- Collision ----

    public override void Collide(SiteColliders c)
    {
        var p = PlanOf(c.Context.World);
        foreach (var ring in p.Decks) c.Add(new SlabCollider(ring, p.Top, P.Slab, SurfaceTag.Ground) { Foot = FootMat.StonePaving });
        var railing = new RailingPart();
        foreach (var e in p.Edges.Where(e => e.Free))
            foreach (var (rail, at) in ParapetPieces(p, e)) c.Add(railing.Collision(rail), at);
        foreach (var col in p.Columns) c.Add(new PostCollider(col, P.ColumnRadius + 0.05f, p.Y, p.Soffit, SurfaceTag.Wall) { Foot = FootMat.Concrete });
        var floor = p.Y + SouthTune.Plaza.PavingTop;
        foreach (var bay in p.Bays)
        {
            var f = bay.Frame;
            var hw = bay.Width * 0.5f;
            if (!bay.Door)
            {
                c.Wall(f.P(-hw, 0), f.P(hw, 0), 0.15f, p.Y - 0.2f, p.Soffit, SurfaceTag.Wall, FootMat.Concrete);
                continue;
            }
            var dw = P.DoorWidth * 0.5f + 0.1f;
            c.Wall(f.P(-hw, 0), f.P(-dw, 0), 0.15f, p.Y - 0.2f, p.Soffit, SurfaceTag.Wall, FootMat.Concrete);
            c.Wall(f.P(-dw, 0), f.P(dw, 0), 0.15f, p.Y - 0.2f, floor + P.DoorHeight, SurfaceTag.DoorClosed, FootMat.Glass);
            c.Wall(f.P(-dw, 0), f.P(dw, 0), 0.15f, floor + P.DoorHeight, p.Soffit, SurfaceTag.Wall, FootMat.Concrete);
            c.Wall(f.P(dw, 0), f.P(hw, 0), 0.15f, p.Y - 0.2f, p.Soffit, SurfaceTag.Wall, FootMat.Concrete);
        }
        // Short core edges carry no bays: close them as plain wall.
        for (var i = 0; i < p.Core.Length; i++)
        {
            Vector2 a = p.Core[i], e = p.Core[(i + 1) % p.Core.Length];
            if (Vector2.Distance(a, e) < 3.5f) c.Wall(a, e, 0.15f, p.Y - 0.2f, p.Soffit, SurfaceTag.Wall, FootMat.Concrete);
        }
        float d = FlightDepth, half = S.PlatformTop * 0.5f;
        foreach (var s in p.Stairs)
        {
            var f = s.Frame;
            var fl = Flight(s.Width);
            c.Add(Steps.Collision(fl), f.AtReversed(0, p.Y + half, d * 0.5f));
            c.Box(f.At(0, p.Y + half - 0.15f, d + P.StairLanding * 0.5f), new(s.Width * 0.5f, 0.15f, P.StairLanding * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
            c.Add(Steps.Collision(fl), f.AtReversed(0, p.Y, d + P.StairLanding + d * 0.5f));
            // Cheeks: no stepping off the sides of the upper flight and the landing.
            foreach (var x in new[] { -s.Width * 0.5f, s.Width * 0.5f })
                c.Wall(f.P(x, 0), f.P(x, d + P.StairLanding), 0.15f, p.Y, p.Top + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.StonePaving);
        }
    }

    // ---- Plan helpers ----

    private static Vector2[]? OuterOf(OverlayGeometry geometry) => geometry switch
    {
        PolygonGeometry g => Clean(g.Rings.Outer),
        MultiPolygonGeometry m => Clean(m.Polygons.OrderByDescending(r => MathF.Abs(GymSite.Area(r.Outer))).First().Outer),
        _ => null,
    };

    /// <summary>The ring without a repeated closing vertex.</summary>
    private static Vector2[] Clean(IReadOnlyList<Vector2> ring)
    {
        var list = ring.ToList();
        if (list.Count > 1 && Vector2.Distance(list[0], list[^1]) < 1e-4f) list.RemoveAt(list.Count - 1);
        return [.. list];
    }

    /// <summary>The ring counter-clockwise in (x, z) (positive shoelace area), so every edge's outward normal is (dz, −dx).</summary>
    private static Vector2[] Ccw(IReadOnlyList<Vector2> ring) => GymSite.Area(ring) >= 0 ? [.. ring] : [.. ring.Reverse()];

    private static Edge? Nearest(IEnumerable<Edge> edges, Vector2 at)
    {
        Edge? best = null;
        var bestDistance = float.MaxValue;
        foreach (var e in edges.Where(e => e.Free))
        {
            var d = Distance([e.A, e.B], at);
            if (d < bestDistance) { best = e; bestDistance = d; }
        }
        return bestDistance < 3f ? best : null;
    }

    private static bool InStair(Stair s, Vector2 c, float margin)
    {
        var l = s.Frame.Local(c);
        return MathF.Abs(l.X) < s.Width * 0.5f + margin && l.Y > -margin && l.Y < 2f * FlightDepth + P.StairLanding + margin;
    }

    private readonly record struct Road(IReadOnlyList<Vector2> Line, float HalfWidth);

    /// <summary>Carriageway centre lines of the network: columns keep off them.</summary>
    private static List<Road> Carriageways(IWorldModel world)
    {
        var list = new List<Road>();
        foreach (var line in world.Network.Roads)
        {
            var type = line.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("roadType", out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : null;
            if (type is "footway" or "path" or "steps" or "pedestrian" or "cycleway" or "track" or "corridor") continue;
            list.Add(new(line.Line.Points, (line.Width > 0 ? line.Width : 5f) * 0.5f));
        }
        return list;
    }

    private static float Distance(IReadOnlyList<Vector2> line, Vector2 p)
    {
        var best = float.MaxValue;
        for (var i = 0; i + 1 < line.Count; i++)
        {
            var ab = line[i + 1] - line[i];
            var t = Math.Clamp(Vector2.Dot(p - line[i], ab) / MathF.Max(Vector2.Dot(ab, ab), 1e-6f), 0f, 1f);
            best = MathF.Min(best, Vector2.Distance(p, line[i] + ab * t));
        }
        return best;
    }
}
