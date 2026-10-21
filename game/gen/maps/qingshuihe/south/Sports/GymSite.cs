using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.South.Framework;
using Campus.Gen.Sites;
using S = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Sports;
using L = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Look;
using W = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Windows;

namespace Campus.Gen.Maps.Qingshuihe.South.Sports;

/// <summary>
/// Gym (MAP-QSH-SOUTH; WLD-INTERIORS), a 26 m round drum that no facade rule can extrude, so this site claims it:
/// - an ochre-brown base band up to the platform storey (the field level behind it is closed, three service doors);
/// - the ring hall's tea glass, faceted one bay per pane, with slanted dark mullions, a transom and brown louvre blades;
/// - a ring of bulging silver ribs whose rolled edge turns down over the glass;
/// - the silver-grey roof, broad and almost flat in the middle, radial seams and two rings of light blue-grey vent caps;
/// - the open public entrance on the platform storey (gym.deck in interiors.json), with WLD-INTERIORS's presentation.
/// The glass is one grid (contract-29): a column per facet, the platform storey and the upper hall as rows, the door cell skipped.
/// The tea glass uses its registered colour (ART-STYLE).
/// </summary>
public sealed class GymSite : ZoneSite
{
    public static readonly string Feature = SouthIds.Gym.Entity;
    private static readonly string Part = SouthIds.Gym.Part;
    public const string GlassKey = "ring-hall";

    /// <summary>Numbered panes stand this far in front of the glass core.</summary>
    private const float PaneZ = 0.03f;

    public override string Id => "18c.gym";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var p = PlanOf(world);
        return [SiteRanges.Point(p.Centre, MathF.Max(p.Radius + S.GymRibProfile.Max(v => v.X) + S.GymRibDepth,
            p.Radius * S.GymRoofProfile.Max(v => v.X)))];
    }

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(Feature), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    /// <summary>
    /// The drum's LOD-free plan: centre and radius fitted to the footprint, the glass facets (column i spans angles
    /// Phase + 2πi/Bays … Phase + 2π(i+1)/Bays, angle from +x toward +z), column 0 centred on the platform-storey door.
    /// DeckTop is the platform storey's height (the platform feature's, DECISION-239), Top the drum's own height from the map data.
    /// </summary>
    internal sealed record Plan(Vector2 Centre, float Radius, float Phase, int Bays, float DeckTop, float Top)
    {
        public float GlassRadius => Radius - S.GymGlassInset;

        public float Angle(float i) => Phase + 2f * MathF.PI * i / Bays;

        public Vector2 At(float angle, float r) => new(Centre.X + r * MathF.Cos(angle), Centre.Y + r * MathF.Sin(angle));

        /// <summary>A frame on the plan circle of radius r at an angle: +Z outward, X along the circle.</summary>
        public PlanFrame Frame(float angle, float r)
        {
            var n = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            return new(Centre + n * r, new(n.Y, -n.X));
        }

        /// <summary>Facet i of the glass ring: its frame on the chord's midpoint and the chord's length.</summary>
        public (PlanFrame Frame, float Width) Facet(int i)
        {
            var half = MathF.PI / Bays;
            return (Frame(Angle(i) + half, GlassRadius * MathF.Cos(half)), 2f * GlassRadius * MathF.Sin(half));
        }
    }

    internal static Plan PlanOf(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(Feature), out var feature))
            throw new InvalidOperationException($"Gym {Feature} is missing from the world model.");
        var ring = feature.Contour(Part);
        var radius = MathF.Sqrt((float)Math.Abs(PlanRings.SignedArea(ring)) / MathF.PI);
        var bays = (int)MathF.Round(2f * MathF.PI * (radius - S.GymGlassInset) / S.GymBay);
        var door = Angles.Heading(S.GymDoorBearing);
        var deckTop = world.Feature(PlatformSite.Feature).Height ?? throw new InvalidOperationException($"Platform {PlatformSite.Feature} has no height.");
        var top = feature.Height ?? throw new InvalidOperationException($"Gym {Feature} has no height.");
        return new(PlanPolygons.Centroid(ring), radius, MathF.Atan2(door.Y, door.X) - MathF.PI / bays, bays, deckTop, top);
    }

    /// <summary>Row sills and heights of the ring hall's glass, above the ground: the platform storey, then the upper hall over the transom.</summary>
    private static (float Sill, float Height)[] Rows(Plan p) =>
        [(p.DeckTop + 0.05f, S.GymTransom - p.DeckTop - 0.1f), (S.GymTransom + 0.05f, S.GymGlassTop - S.GymTransom - 0.15f)];

    private const int RowCount = 2;

    /// <summary>The ring hall's panes in grid order, one per facet and row; the door cell (0, 0) carries the open doorway instead.</summary>
    internal static GlazedOpening[] Openings(Plan p, float y)
    {
        var list = new List<GlazedOpening>();
        var rows = Rows(p);
        for (var row = 0; row < rows.Length; row++)
            for (var i = 0; i < p.Bays; i++)
            {
                if (i == 0 && row == 0) continue;
                var (f, width) = p.Facet(i);
                var o = f.P(0, PaneZ);
                list.Add(new(i, row, new(o.X, y + rows[row].Sill, o.Y), new(f.W.X, 0, f.W.Y), width - 0.02f, rows[row].Height));
            }
        return [.. list];
    }

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) =>
        [(new(Feature), new(GlassKey, PlanOf(world).Bays, RowCount))];

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var p = PlanOf(ctx.World);
        WindowNumbers.Declare(windows, Openings(p, ctx.World.Terrain.HeightAt(p.Centre.X, p.Centre.Y)), Allocation(ctx, p), W.Venue);
    }

    private static WindowAllocation Allocation(BuildContext ctx, Plan p)
    {
        var feature = new FeatureId(Feature);
        var allocation = WindowNumbers.Allocation(ctx, feature, GlassKey);
        WindowNumbers.Fit(p.Bays, RowCount, feature, GlassKey, allocation);
        return allocation;
    }

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
    {
        var p = PlanOf(world);
        var count = lod switch { LodLevel.L0 => 144, LodLevel.L1 => 96, LodLevel.L2 => 64, _ => 40 };
        return [new(Enumerable.Range(0, count).Select(i => p.At(2f * MathF.PI * i / count, p.Radius - S.GymBaseInset)).ToArray())];
    }

    public override void Build(SiteBuilder b)
    {
        var p = PlanOf(b.Context.World);
        var y = b.Ground(p.Centre);
        var at = Matrix4x4.CreateTranslation(p.Centre.X, y, p.Centre.Y);
        var (round, ribs) = b.Detail switch { SiteDetail.Full => (144, S.GymRibs), SiteDetail.Plain => (96, S.GymRibs / 2), _ => (b.Lod is LodLevel.L2 ? 64 : 40, 0) };
        Base(b, p, y, at, round);
        Glass(b, p, y, at, round);
        Ribs(b, p, y, at, round, ribs);
        Roof(b, p, y, at, round);
        if (b.Detail == SiteDetail.Mass) return;
        Entrance(b, p, y);
        foreach (var bearing in S.GymServiceDoors) ServiceDoor(b, p, y, bearing);
    }

    private static ToonSpec Round(ToonSpec spec) => spec with { Normal = ToonNormal.Smooth };

    // ---- Base band to the platform storey: plinth, ochre band, stone coping flush with the platform ----

    private static void Base(SiteBuilder b, Plan p, float y, Matrix4x4 at, int segments)
    {
        float r = p.Radius - S.GymBaseInset, top = p.DeckTop;
        b.Add(Solids.Lathe([new(r + 0.12f, -0.1f), new(r + 0.12f, 0.55f)], segments, Round(L.Granite)), at);
        b.Add(Solids.Lathe([new(r, 0.5f), new(r, top - 0.28f)], segments, L.Ochre), at);
        // The coping is the ring hall's outer floor edge: its top is the platform storey.
        b.Add(Solids.Lathe([new(r + 0.2f, top - 0.3f), new(r + 0.2f, top), new(p.GlassRadius - 0.2f, top)], segments, Round(L.StoneTrim), capTop: false), at);
        if (b.Detail == SiteDetail.Mass) return;
        // Two shallow joint bands keep the ochre band from reading as one bare extrusion (ART-FIDELITY).
        foreach (var h in new[] { 1.75f, 2.95f })
            b.Add(Solids.Lathe([new(r + 0.04f, h), new(r + 0.04f, h + 0.06f)], segments, Round(L.StoneTrim), capBottom: false, capTop: false), at);
    }

    // ---- Ring hall glass: faceted core (the doorway left open), numbered panes, transom, slanted mullions, louvres ----

    private static void Glass(SiteBuilder b, Plan p, float y, Matrix4x4 at, int segments)
    {
        float y0 = y + p.DeckTop - 0.3f, height = S.GymGlassTop - p.DeckTop + 0.4f;
        for (var i = 0; i < p.Bays; i++)
        {
            var (f, width) = p.Facet(i);
            var half = width * 0.5f + 0.01f;
            IReadOnlyList<Vector2> outline = i == 0
                ? [new(-half, 0), new(-S.GymDoorWidth * 0.5f, 0), new(-S.GymDoorWidth * 0.5f, 0.3f + S.GymDoorHeight), new(S.GymDoorWidth * 0.5f, 0.3f + S.GymDoorHeight),
                    new(S.GymDoorWidth * 0.5f, 0), new(half, 0), new(half, height), new(-half, height)]
                : [new(-half, 0), new(half, 0), new(half, height), new(-half, height)];
            b.Add(PartSolids.WallPanel(new PolygonRings(outline), 0.06f, L.TeaGlass), f.At(0, y0, -0.03f));
        }
        WindowNumbers.Panes(b, Openings(p, y), Allocation(b.Context, p), W.Venue, W.Gym, L.TeaGlass);
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        var rg = p.GlassRadius;
        // Transom and head: dark rings just proud of the glass.
        foreach (var h in new[] { S.GymTransom - 0.06f, S.GymGlassTop - 0.14f })
            b.Add(Solids.Lathe([new(rg + 0.16f, h), new(rg + 0.16f, h + 0.12f)], segments, Round(L.DarkFrame), capBottom: false, capTop: false), at);
        if (b.Detail == SiteDetail.Mass) return;
        // Brown louvre blades under the rib ring (§7.1): thin rings standing off the glass.
        foreach (var h in S.GymLouvres)
            b.Add(Solids.Lathe([new(rg + 0.25f, h), new(rg + 0.75f, h), new(rg + 0.75f, h + 0.06f), new(rg + 0.25f, h + 0.06f), new(rg + 0.25f, h)], segments, L.Louvre,
                capBottom: false, capTop: false), at);
        // Slanted dark mullions: one per bay, leaning round the drum by GymMullionLean over the glass height; none across the door.
        var lean = S.GymMullionLean / rg;
        var r = rg + 0.1f;
        for (var i = 1; i < p.Bays; i++)
        {
            var a0 = p.Angle(i);
            var (lo, hi) = (p.At(a0, r), p.At(a0 + lean, r));
            var n = new Vector3(MathF.Cos(a0 + lean * 0.5f), 0, MathF.Sin(a0 + lean * 0.5f));
            var foot = new Vector3(lo.X, y + p.DeckTop, lo.Y);
            var head = new Vector3(hi.X, y + S.GymGlassTop - 0.1f, hi.Y);
            b.Add(PartSolids.BeveledBox(0.14f, Vector3.Distance(foot, head), 0.16f, 0.02f, L.DarkFrame), LandmarkSolids.Along(foot, head, n));
        }
    }

    // ---- Rib ring: one curved metal band over the glass top, slim ribs over its bulge; its rolled edge turns down ----

    private static readonly ToonSpec RibFin = L.Silver with { Line = LineClass.Fine, Normal = ToonNormal.Flat };

    private static void Ribs(SiteBuilder b, Plan p, float y, Matrix4x4 at, int segments, int ribs)
    {
        var profile = S.GymRibProfile.Select(v => new Vector2(p.Radius + v.X, v.Y)).ToArray();
        b.Add(Solids.Lathe(profile, segments, L.Silver), at);
        if (ribs == 0) return;
        // Each rib follows the band from just above the tucked foot over the bulge into the roll, a fin per profile step.
        var bevel = b.Detail == SiteDetail.Full ? 0.02f : 0;
        for (var k = 0; k < ribs; k++)
        {
            var angle = p.Phase + 2f * MathF.PI * k / ribs;
            var d = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            for (var j = 1; j + 2 < profile.Length; j++)
            {
                Vector2 q0 = profile[j], q1 = profile[j + 1];
                var lift = S.GymRibDepth * 0.5f - 0.02f;
                // Offset along the profile normal (outward and up), so the fin stands proud of the band.
                var t = Vector2.Normalize(q1 - q0);
                var n = new Vector2(t.Y, -t.X);
                Vector3 P(Vector2 q, float e) => new(p.Centre.X + d.X * (q.X + n.X * e), y + q.Y + n.Y * e, p.Centre.Y + d.Y * (q.X + n.X * e));
                var (a, c) = (P(q0, lift), P(q1, lift));
                var normal = new Vector3(d.X * n.X, n.Y, d.Y * n.X);
                b.Add(PartSolids.BeveledBox(S.GymRibWidth, Vector3.Distance(a, c) + 0.06f, S.GymRibDepth, bevel, RibFin), LandmarkSolids.Along(a - (c - a) * (0.03f / Vector3.Distance(a, c)), c, normal));
            }
        }
    }

    // ---- Roof dish: broad, almost flat middle; radial seams; two rings of vent caps ----

    private static void Roof(SiteBuilder b, Plan p, float y, Matrix4x4 at, int segments)
    {
        var profile = S.GymRoofProfile.Select(v => new Vector2(p.Radius * v.X, v.Y)).ToArray();
        b.Add(Solids.Lathe(profile, segments, L.Silver, capBottom: false), at);
        if (b.Detail == SiteDetail.Mass) return;
        float Height(float r)
        {
            for (var k = 0; k + 1 < profile.Length; k++)
                if (r <= profile[k].X && r >= profile[k + 1].X)
                    return profile[k + 1].Y + (profile[k].Y - profile[k + 1].Y) * (r - profile[k + 1].X) / (profile[k].X - profile[k + 1].X);
            return profile[^1].Y;
        }
        // Radial seams: dark standing strips from the roll in to the flat middle, one piece per profile step.
        if (b.Detail == SiteDetail.Full)
            for (var k = 0; k < S.GymSeams; k++)
            {
                var angle = p.Phase + 2f * MathF.PI * (k + 0.5f) / S.GymSeams;
                var d = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                for (var j = 0; j + 2 < profile.Length; j++)
                {
                    float r0 = profile[j].X - 0.4f, r1 = profile[j + 1].X;
                    var a = new Vector3(p.Centre.X + d.X * r0, y + Height(r0) + 0.02f, p.Centre.Y + d.Y * r0);
                    var c = new Vector3(p.Centre.X + d.X * r1, y + Height(r1) + 0.02f, p.Centre.Y + d.Y * r1);
                    var rise = profile[j + 1].Y - profile[j].Y;
                    var up = new Vector3(d.X * rise, profile[j].X - profile[j + 1].X, d.Y * rise);
                    b.Add(PartSolids.BeveledBox(0.12f, Vector3.Distance(a, c), 0.1f, 0, L.DarkFrame), LandmarkSolids.Along(a, c, up));
                }
            }
        // Vent caps: a short drum under a mushroom hood, the inner ring staggered half a step.
        Vector2[] hood = [new(0.38f, 0), new(0.38f, 0.55f), new(0.8f, 0.6f), new(0.8f, 0.74f), new(0.55f, 0.92f), new(0, 1.02f)];
        foreach (var (ring, count) in S.GymVents)
            for (var k = 0; k < count; k++)
            {
                var angle = p.Phase + 2f * MathF.PI * (k + (ring < 0.5f ? 0.5f : 0f)) / count;
                var r = p.Radius * ring;
                var c = p.At(angle, r);
                b.Add(Solids.Lathe(hood, b.Detail == SiteDetail.Full ? 14 : 8, L.Vent), Matrix4x4.CreateTranslation(c.X, y + Height(r) - 0.05f, c.Y));
            }
    }

    // ---- Entrances ----

    /// <summary>
    /// The platform-storey public entrance in facet 0 (gym.deck): an aluminium portal with the leaves fixed open against the
    /// jambs (SYS-WALK), a glass lobby screen inside and a flat canopy over the landing.
    /// </summary>
    private static void Entrance(SiteBuilder b, Plan p, float y)
    {
        var (f, bay) = p.Facet(0);
        float floor = y + p.DeckTop, w = S.GymDoorWidth, h = S.GymDoorHeight, head = y + S.GymTransom;
        // Portal: jambs to the transom, a head bar, a metal panel filling the bay between the door head and the transom.
        foreach (var side in new[] { -1f, 1f })
            GridFacade.Box(b, f, side * (w * 0.5f + 0.06f) - 0.09f, side * (w * 0.5f + 0.06f) + 0.09f, floor, head, -0.1f, 0.2f, L.DarkFrame, 0.015f);
        GridFacade.Box(b, f, -bay * 0.5f, bay * 0.5f, floor + h, floor + h + 0.18f, -0.1f, 0.22f, L.DarkFrame, 0.015f);
        GridFacade.Box(b, f, -bay * 0.5f + 0.05f, bay * 0.5f - 0.05f, floor + h + 0.18f, head - 0.06f, -0.05f, 0.05f, L.Metal, 0);
        // Leaves swung in against the reveals, and the lobby's inner glass screen 2 m inside.
        foreach (var side in new[] { -1f, 1f })
            GridFacade.Box(b, f, side * (w * 0.5f - 0.03f) - 0.03f, side * (w * 0.5f - 0.03f) + 0.03f, floor, floor + h - 0.05f, -1.45f, -0.05f, L.Metal, 0.01f);
        GridFacade.Box(b, f, -w * 0.5f - 0.2f, w * 0.5f + 0.2f, floor, floor + h, -2.1f, -2.04f, L.TeaGlass, 0);
        foreach (var side in new[] { -1f, 1f })
            GridFacade.Box(b, f, side * (w * 0.5f + 0.2f) - 0.06f, side * (w * 0.5f + 0.2f) + 0.06f, floor, floor + h, -2.1f, -0.1f, L.StoneTrim, 0.01f);
        // Flat canopy over the landing on two slim posts.
        GridFacade.Box(b, f, -w * 0.5f - 1.4f, w * 0.5f + 1.4f, floor + h + 0.6f, floor + h + 0.85f, 0.1f, 2.6f, L.StoneTrim, 0.02f);
        foreach (var side in new[] { -1f, 1f })
            GridFacade.Box(b, f, side * (w * 0.5f + 1.1f) - 0.08f, side * (w * 0.5f + 1.1f) + 0.08f, floor, floor + h + 0.6f, 2.3f, 2.46f, L.Metal, 0.01f);
        // The mat, open sign and door lamp are game/gen/furniture/'s, placed on the gym.deck entrance anchor (WLD-INTERIORS).
    }

    private static float BearingAngle(float bearing)
    {
        var d = Angles.Heading(bearing);
        return MathF.Atan2(d.Y, d.X);
    }

    /// <summary>A closed field-level service door in the ochre band (WLD-INTERIORS), under a small stone hood.</summary>
    private static void ServiceDoor(SiteBuilder b, Plan p, float y, float bearing)
    {
        var f = p.Frame(BearingAngle(bearing), p.Radius - S.GymBaseInset);
        GridFacade.Box(b, f, -1.0f, 1.0f, y, y + 2.4f, -0.05f, 0.06f, L.Metal, 0.01f);
        GridFacade.Box(b, f, -1.2f, 1.2f, y + 2.4f, y + 2.62f, -0.05f, 0.5f, L.StoneTrim, 0.02f);
        GridFacade.Box(b, f, -1.15f, 1.15f, y - 0.1f, y + 0.12f, 0, 0.9f, L.Granite, 0.02f);
    }

    // ---- Collision ----

    /// <summary>The drum's wall ring at the base band: the doorway open on the platform storey, the service doors closed.</summary>
    public override void Collide(SiteColliders c)
    {
        var p = PlanOf(c.Context.World);
        var y = c.Ground(p.Centre.X, p.Centre.Y);
        const int segments = 48;
        var r = p.Radius - S.GymBaseInset + 0.1f;
        var door = p.Angle(0.5f);
        var services = S.GymServiceDoors.Select(BearingAngle).ToArray();
        for (var k = 0; k < segments; k++)
        {
            float a0 = door + 2f * MathF.PI * (k - 0.5f) / segments, a1 = door + 2f * MathF.PI * (k + 0.5f) / segments;
            var (pa, pb) = (p.At(a0, r), p.At(a1, r));
            if (k == 0)
            {
                c.Wall(pa, pb, 0.25f, y, y + p.DeckTop, SurfaceTag.Wall, FootMat.Concrete);
                c.Wall(pa, pb, 0.25f, y + p.DeckTop + S.GymDoorHeight, y + p.Top, SurfaceTag.Wall, FootMat.Concrete);
                continue;
            }
            var mid = (a0 + a1) * 0.5f;
            if (services.Any(s =>
            {
                var delta = Angles.SignedDelta(0f, (s - mid) * Angles.RadiansToDegrees);
                return MathF.Abs(delta) < 180f / segments;
            }))
            {
                c.Wall(pa, pb, 0.25f, y, y + 2.4f, SurfaceTag.DoorClosed, FootMat.Concrete);
                c.Wall(pa, pb, 0.25f, y + 2.4f, y + p.Top, SurfaceTag.Wall, FootMat.Concrete);
                continue;
            }
            c.Wall(pa, pb, 0.25f, y, y + p.Top, SurfaceTag.Wall, FootMat.Concrete);
        }
    }
}
