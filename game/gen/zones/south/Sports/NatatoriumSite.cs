using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.South.Framework;
using Campus.Gen.Zones.Common;
using S = Campus.Gen.Zones.South.SouthTune.Sports;
using L = Campus.Gen.Zones.South.SouthTune.Look;
using W = Campus.Gen.Zones.South.SouthTune.Windows;

namespace Campus.Gen.Zones.South.Sports;

/// <summary>
/// Natatorium (MAP-QSH-SOUTH; WLD-INTERIORS): a low white elliptical dome, which the facade rules cannot make, so this
/// site claims it. A short white skirt, the dome, its dark glass band near the top as a clerestory drum under a white cap
/// (at night the band carries the pool's glow on the venue curve), a light concrete apron round the foot, and the
/// platform-storey entrance hall on the platform side, closed until WP-31a registers the pool's entrance.
/// The ellipse is fitted to the footprint's area and second moments. The glass band is one grid (contract-29), a column per facet.
/// </summary>
public sealed class NatatoriumSite : ZoneSite
{
    public static readonly string Feature = SouthIds.Natatorium.Entity;
    private static readonly string Part = SouthIds.Natatorium.Part;
    public const string GlassKey = "glass-band";

    private const float PaneZ = 0.03f;

    public override string Id => "18c.natatorium";

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(Feature), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    /// <summary>The dome's LOD-free plan: the fitted ellipse (local X along the major axis), the glass band's facet count and the platform storey's height (the platform feature's, D-239).</summary>
    internal sealed record Plan(PlanFrame Frame, float A, float B, int Bays, float DeckTop)
    {
        /// <summary>Plan point at parameter t (radians) on the ellipse scaled by rho.</summary>
        public Vector2 At(float t, float rho) => Frame.P(A * rho * MathF.Cos(t), B * rho * MathF.Sin(t));

        /// <summary>Facet i of the glass drum: frame on the chord's midpoint (+Z outward), and the chord's length.</summary>
        public (PlanFrame Frame, float Width) Facet(int i)
        {
            var (a, c) = (At(2f * MathF.PI * i / Bays, S.PoolDrum), At(2f * MathF.PI * (i + 1) / Bays, S.PoolDrum));
            var d = c - a;
            var length = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
            // The ellipse runs counter-clockwise in the local frame, so the outward normal is the chord turned clockwise.
            var u = new Vector2(-d.X / length, -d.Y / length);
            return (new((a + c) * 0.5f, u), length);
        }
    }

    internal static Plan PlanOf(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(Feature), out var feature))
            throw new InvalidOperationException($"Natatorium {Feature} is missing from the world model.");
        var ring = feature.Contour(Part);
        var centre = PlanGeometry.Centroid(ring, meanIfDegenerate: true);
        // Second moments about the centroid; a filled ellipse has variance (a/2)² and (b/2)² along its axes.
        double area = 0, sxx = 0, szz = 0, sxz = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            double x0 = ring[i].X - centre.X, z0 = ring[i].Y - centre.Y;
            double x1 = ring[(i + 1) % ring.Count].X - centre.X, z1 = ring[(i + 1) % ring.Count].Y - centre.Y;
            var cross = x0 * z1 - x1 * z0;
            area += cross * 0.5;
            sxx += cross * (x0 * x0 + x0 * x1 + x1 * x1) / 12.0;
            szz += cross * (z0 * z0 + z0 * z1 + z1 * z1) / 12.0;
            sxz += cross * (x0 * z1 + 2 * x0 * z0 + 2 * x1 * z1 + x1 * z0) / 24.0;
        }
        sxx /= area; szz /= area; sxz /= area;
        double mean = (sxx + szz) * 0.5, spread = Math.Sqrt((sxx - szz) * (sxx - szz) * 0.25 + sxz * sxz);
        var theta = 0.5f * MathF.Atan2((float)(2 * sxz), (float)(sxx - szz));
        float a = 2f * (float)Math.Sqrt(mean + spread), b = 2f * (float)Math.Sqrt(mean - spread);
        // Keep the footprint's area: π·a·b = |area|.
        var scale = MathF.Sqrt((float)Math.Abs(area) / (MathF.PI * a * b));
        a *= scale; b *= scale;
        var drum = MathF.PI * (3 * (a + b) - MathF.Sqrt((3 * a + b) * (a + 3 * b))) * S.PoolDrum;
        var deckTop = world.Feature(PlatformSite.Feature).Height ?? throw new InvalidOperationException($"Platform {PlatformSite.Feature} has no height.");
        return new(new(centre, new(MathF.Cos(theta), MathF.Sin(theta))), a, b, (int)MathF.Round(drum / S.PoolBay), deckTop);
    }

    internal static GlazedOpening[] Openings(Plan p, float y)
    {
        var list = new GlazedOpening[p.Bays];
        for (var i = 0; i < p.Bays; i++)
        {
            var (f, width) = p.Facet(i);
            var o = f.P(0, PaneZ);
            list[i] = new(i, 0, new(o.X, y + S.PoolGlassFoot + 0.12f, o.Y), new(f.W.X, 0, f.W.Y), width - 0.14f, S.PoolGlassTop - S.PoolGlassFoot - 0.24f);
        }
        return list;
    }

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world) =>
        [(new(Feature), new(GlassKey, PlanOf(world).Bays, 1))];

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var p = PlanOf(ctx.World);
        var c = p.Frame.Origin;
        WindowNumbers.Declare(windows, Openings(p, ctx.World.Terrain.HeightAt(c.X, c.Y)), Allocation(ctx, p, true)!.Value, W.Venue);
    }

    private static WindowAllocation? Allocation(BuildContext ctx, Plan p, bool required = false)
    {
        var feature = new FeatureId(Feature);
        var allocation = required ? WindowNumbers.Required(ctx, feature, GlassKey) : WindowNumbers.Allocation(ctx, feature, GlassKey);
        if (allocation is { } a) WindowNumbers.Fit(p.Bays, 1, feature, GlassKey, a);
        return allocation;
    }

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
    {
        var p = PlanOf(world);
        var count = lod switch { LodLevel.L0 => 120, LodLevel.L1 => 80, LodLevel.L2 => 56, _ => 36 };
        var body = new PolygonRings(Enumerable.Range(0, count).Select(i => p.At(2f * MathF.PI * i / count, 1f)).ToArray());
        if (lod is not (LodLevel.L0 or LodLevel.L1)) return [body];
        var entry = EntryFrame(p, out var reach);
        var half = S.PoolEntryWidth * 0.5f;
        return [body, new(entry.Rect(-half, reach - 3f, half, reach + S.PoolEntryDepth - 0.5f))];
    }

    public override void Build(SiteBuilder b)
    {
        var p = PlanOf(b.Context.World);
        var y = b.Ground(p.Frame.Origin);
        var at = p.Frame.At(0, y, 0);
        var segments = b.Detail switch { SiteDetail.Full => 120, SiteDetail.Plain => 80, _ => b.Lod is LodLevel.L2 ? 56 : 36 };
        // Profiles are in fractions of the semi-axes: the lathe scales them by (A, B).
        b.Add(Solids.Lathe(S.PoolDomeProfile, segments, L.DomeWhite, p.A, p.B, capBottom: false, capTop: false), at);
        b.Add(Solids.Lathe(S.PoolCapProfile, segments, L.DomeWhite, p.A, p.B, capBottom: true), at);
        // The glass band: a faceted drum just inside the cap's rim, its foot buried in the dome's crown.
        for (var i = 0; i < p.Bays; i++)
        {
            var (f, width) = p.Facet(i);
            b.Add(PartGeometryKernel.WallPanel(width + 0.02f, S.PoolGlassTop - S.PoolGlassFoot + 0.4f, 0.06f, [], ZoneLooks.Glass), f.At(0, y + S.PoolGlassFoot - 0.3f, -0.03f));
        }
        if (Allocation(b.Context, p) is { } allocation) WindowNumbers.Panes(b, Openings(p, y), allocation, W.Venue, W.Pool);
        // The crown inside the drum, so the band never shows sky through its back.
        b.Add(Solids.Lathe([new(S.PoolDrum, S.PoolGlassFoot - 0.2f), new(S.PoolDrum * 0.6f, S.PoolGlassFoot + 0.2f), new(0, S.PoolGlassFoot + 0.3f)], segments / 2, L.DomeWhite, p.A, p.B, capBottom: false), at);
        if (b.Detail == SiteDetail.Mass) return;
        // Plinth line, the skirt's cornice and the light concrete apron round the foot (§7.2's ochre-red ring read as a running
        // track beside the stadium; 18c2 review).
        var e = 1f / p.A;
        b.Add(Solids.Lathe([new(1f + 0.25f * e, -0.1f), new(1f + 0.25f * e, 0.35f), new(1f, 0.45f)], segments, L.Granite with { Normal = ToonNormal.Smooth }, p.A, p.B, capBottom: false, capTop: false), at);
        b.Add(Solids.Lathe([new(1f + 0.3f * e, 1.55f), new(1f + 0.3f * e, 1.85f), new(0.99f, 2.0f)], segments, L.StoneTrim with { Normal = ToonNormal.Smooth }, p.A, p.B, capBottom: true, capTop: false), at);
        b.Add(Solids.Lathe([new(1f + S.PoolRing, 0.03f), new(1f + 0.25f * e, 0.03f)], segments, L.Apron, p.A, p.B, capBottom: false, capTop: false), at);
        Meridians(b, p, y, b.Detail == SiteDetail.Full ? S.PoolRibs : S.PoolRibs / 2);
        // Mullions between the glass facets and a slim sill ring.
        for (var i = 0; i < p.Bays; i++)
        {
            var (f, width) = p.Facet(i);
            GridFacade.Box(b, f, width * 0.5f - 0.07f, width * 0.5f + 0.07f, y + S.PoolGlassFoot, y + S.PoolGlassTop, -0.02f, 0.12f, L.DarkFrame, 0);
        }
        Entry(b, p, y);
    }

    private static readonly ToonSpec Rib = L.Baluster with { Normal = ToonNormal.Flat };

    /// <summary>
    /// Meridian ribs over the dome, from the skirt's cornice up to the glass band, one slim fin per profile step: the white
    /// dome reads as a built shell with structure rather than one bare surface (ART-FIDELITY).
    /// </summary>
    private static void Meridians(SiteBuilder b, Plan p, float y, int count)
    {
        var profile = S.PoolDomeProfile;
        for (var k = 0; k < count; k++)
        {
            var t = 2f * MathF.PI * (k + 0.5f) / count;
            float c = MathF.Cos(t), s = MathF.Sin(t);
            // The ellipse's outward plan normal at t (gradient of x²/A² + z²/B²), in world.
            var g = p.Frame.U * (c / p.A) + p.Frame.W * (s / p.B);
            var outward = g / MathF.Sqrt(g.X * g.X + g.Y * g.Y);
            for (var j = 1; j + 1 < profile.Length; j++)
            {
                Vector3 P(Vector2 q)
                {
                    var at = p.At(t, q.X) + outward * 0.04f;
                    return new(at.X, y + q.Y, at.Y);
                }
                var (a, e) = (P(profile[j]), P(profile[j + 1]));
                b.Add(PartGeometryKernel.BeveledBox(0.2f, Vector3.Distance(a, e) + 0.05f, 0.12f, 0, Rib), LandmarkSolids.Along(a, e, new(outward.X, 0.6f, outward.Y)));
            }
        }
    }

    /// <summary>The entrance hall's frame: on the major-axis ellipse direction of the bearing, +Z outward.</summary>
    internal static PlanFrame EntryFrame(Plan p, out float reach)
    {
        var d = Angles.Heading(S.PoolDoorBearing);
        var local = p.Frame.Local(p.Frame.Origin + d);
        // Distance from the centre to the dome's face along d at the platform storey.
        var rho = Rho(p.DeckTop);
        var k = 1f / MathF.Sqrt(local.X * local.X / (p.A * p.A) + local.Y * local.Y / (p.B * p.B));
        reach = k * rho;
        return new(p.Frame.Origin, new(d.Y, -d.X));
    }

    /// <summary>The dome profile's radius fraction at a height.</summary>
    private static float Rho(float h)
    {
        var profile = S.PoolDomeProfile;
        for (var k = 0; k + 1 < profile.Length; k++)
            if (h >= profile[k].Y && h <= profile[k + 1].Y)
                return profile[k].X + (profile[k + 1].X - profile[k].X) * (h - profile[k].Y) / (profile[k + 1].Y - profile[k].Y);
        return profile[^1].X;
    }

    /// <summary>
    /// The platform-storey entrance hall (WLD-INTERIORS): a white box pushed out of the dome on the platform side, glazed
    /// front with closed doors, a flat canopy slab. It reads as the pool's door; it opens once WP-31a registers the entrance.
    /// </summary>
    private static void Entry(SiteBuilder b, Plan p, float y)
    {
        var f = EntryFrame(p, out var reach);
        float floor = y + p.DeckTop, hw = S.PoolEntryWidth * 0.5f, h = S.PoolEntryHeight, front = reach + S.PoolEntryDepth, back = reach - 3f;
        var face = front - 0.5f;
        GridFacade.Box(b, f, -hw, hw, floor - 0.3f, floor + h, back, face, L.White, 0.04f);
        // Cheeks either side frame the glass in a 0.5 m reveal; the canopy slab runs out over the landing.
        GridFacade.Box(b, f, -hw, -hw + 0.6f, floor - 0.3f, floor + h, face - 0.01f, front, L.White, 0.03f);
        GridFacade.Box(b, f, hw - 0.6f, hw, floor - 0.3f, floor + h, face - 0.01f, front, L.White, 0.03f);
        GridFacade.Box(b, f, -hw - 0.4f, hw + 0.4f, floor + h, floor + h + 0.45f, back, front + 0.9f, L.StoneTrim, 0.03f);
        // Glazed front: panels between dark mullions, the middle pair the closed doors, a transom and a metal head panel.
        GridFacade.Box(b, f, -hw + 0.6f, hw - 0.6f, floor, floor + h - 0.5f, face, face + 0.05f, ZoneLooks.Glass, 0);
        foreach (var x in new[] { -1.4f, 0f, 1.4f })
            GridFacade.Box(b, f, x - 0.06f, x + 0.06f, floor, floor + h - 0.5f, face, face + 0.14f, L.DarkFrame, 0);
        GridFacade.Box(b, f, -hw + 0.6f, hw - 0.6f, floor + 2.7f, floor + 2.8f, face, face + 0.14f, L.DarkFrame, 0);
        GridFacade.Box(b, f, -hw + 0.6f, hw - 0.6f, floor + h - 0.5f, floor + h, face, face + 0.08f, L.Metal, 0);
    }

    public override void Collide(SiteColliders c)
    {
        var p = PlanOf(c.Context.World);
        var y = c.Ground(p.Frame.Origin.X, p.Frame.Origin.Y);
        const int segments = 40;
        for (var k = 0; k < segments; k++)
        {
            var (a, e) = (p.At(2f * MathF.PI * k / segments, 1f), p.At(2f * MathF.PI * (k + 1) / segments, 1f));
            c.Wall(a, e, 0.2f, y, y + S.PoolGlassTop, SurfaceTag.Wall, FootMat.Concrete);
        }
        // The entrance hall's glazed front on the platform storey: closed for now.
        var f = EntryFrame(p, out var reach);
        float floor = y + p.DeckTop, hw = S.PoolEntryWidth * 0.5f, front = reach + S.PoolEntryDepth - 0.45f;
        c.Wall(f.P(-hw, front), f.P(hw, front), 0.12f, floor, floor + S.PoolEntryHeight, SurfaceTag.DoorClosed, FootMat.Glass);
        foreach (var side in new[] { -hw, hw })
            c.Wall(f.P(side, reach - 1f), f.P(side, front), 0.12f, floor, floor + S.PoolEntryHeight, SurfaceTag.Wall, FootMat.Concrete);
    }
}
