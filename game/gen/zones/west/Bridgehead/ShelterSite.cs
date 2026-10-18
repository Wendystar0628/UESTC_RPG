using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using StreetStyle = Campus.Gen.Parts.Street.StreetStyle;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.West.WestTune.Look;
using H = Campus.Gen.Zones.West.WestTune.Shelter;

namespace Campus.Gen.Zones.West.Bridgehead;

/// <summary>
/// 西门桥头候车亭 (MAP-QSH-WEST): built on the OSM shelter footprint qsh:way:604832425, which this site claims so WP-14
/// does not raise a 3.6 m box there. A light, clean shelter: a raised granite platform with a pale kerb, five slim aluminium
/// posts along the back, a thin grey-white flat roof cantilevered toward the road with a front and a back eave (the rain
/// curtains of §11 天气要点 hang from both, clear of the platform), a tech-blue lip under the front fascia, a glass back
/// screen and two short end screens, a timber bench, and a frosted downlight strip whose street-grade markers light the
/// platform at night (候车亭顶灯, §11). Snow lies on the roof by normal. The stop sign and its light box are WP-13's.
/// Frame: origin at the footprint's south-west corner, local X north along the back line, local +Z toward the road.
/// </summary>
public sealed class ShelterSite : ZoneSite, ILightMetadataSource
{
    public override string Id => "19c.shelter";

    public override IReadOnlyList<Claim> Claims => [new FeatureClaim(new(H.Entity), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    internal static PlanFrame Frame => PlanFrame.Toward(H.BackA, H.BackB);

    internal static float Length => Vector2.Distance(H.BackA, H.BackB);

    /// <summary>Post positions along the back (local x), inset from the ends.</summary>
    internal static float[] PostsX => [.. Enumerable.Range(0, H.Posts).Select(i => 0.3f + i * (Length - 0.6f) / (H.Posts - 1))];

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod) =>
        [new(Frame.Rect(-H.PlatformMargin, -0.25f, Length + H.PlatformMargin, H.Depth - 0.22f)),
         new(Frame.Rect(-H.PlatformMargin, H.Depth - 0.24f, Length + H.PlatformMargin, H.Depth)),
         new(Frame.Rect(-0.45f, -H.RoofBack, Length + 0.45f, H.Depth + H.RoofFront))];

    private const float PostZ = 0.25f;

    internal static float Ground(Func<float, float, float> ground)
    {
        var c = Frame.P(Length * 0.5f, H.Depth * 0.5f);
        return ground(c.X, c.Y);
    }

    public override void Build(SiteBuilder b)
    {
        var f = Frame;
        var y = Ground(b.Ground);
        var len = Length;
        var top = y + H.PlatformRise;
        var under = top + H.Clear;
        var fine = b.Detail == SiteDetail.Full;
        var bevel = b.Detail == SiteDetail.Mass ? 0f : 0.03f;
        // Platform and its pale front kerb (收边 to the road).
        Box(b, f, -H.PlatformMargin, len + H.PlatformMargin, y - 0.12f, top, -0.25f, H.Depth - 0.22f, L.ShelterSlab, bevel);
        Box(b, f, -H.PlatformMargin, len + H.PlatformMargin, y - 0.12f, top + 0.005f, H.Depth - 0.24f, H.Depth, L.Trim, bevel);
        // Roof slab with both eaves past the platform, and the blue lip under its front fascia.
        float back = -H.RoofBack, front = H.Depth + H.RoofFront;
        Box(b, f, -0.45f, len + 0.45f, under, under + H.RoofThick, back, front, L.ShelterRoof, bevel);
        if (b.Detail == SiteDetail.Mass)
        {
            Box(b, f, 0.2f, len - 0.2f, top, under, 0.2f, 0.3f, L.Glass, 0);
            return;
        }
        Box(b, f, -0.4f, len + 0.4f, under - H.Band, under, front - 0.14f, front - 0.04f, L.ShelterBand, 0.01f);
        var xs = PostsX;
        foreach (var x in xs)
        {
            Box(b, f, x - H.PostSize * 0.5f, x + H.PostSize * 0.5f, top - 0.02f, under + 0.01f, PostZ - H.PostSize * 0.5f, PostZ + H.PostSize * 0.5f, L.Metal, fine ? 0.012f : 0);
            // A small base plate where each post meets the platform (ART-MODELING: contact reads).
            if (fine) Box(b, f, x - 0.11f, x + 0.11f, top, top + 0.025f, PostZ - 0.11f, PostZ + 0.11f, L.Metal, 0.008f);
        }
        // Glass back screen between the posts, framed by an aluminium sill and head rail along the whole back.
        for (var i = 0; i + 1 < xs.Length; i++)
            Box(b, f, xs[i] + H.PostSize * 0.5f, xs[i + 1] - H.PostSize * 0.5f, top + H.ScreenFoot, top + H.ScreenTop, PostZ - 0.015f, PostZ + 0.015f, L.Glass, 0);
        foreach (var rail in new[] { top + H.ScreenFoot, top + H.ScreenTop })
            Box(b, f, xs[0], xs[^1], rail - 0.04f, rail + 0.04f, PostZ - 0.035f, PostZ + 0.035f, L.Metal, fine ? 0.01f : 0);
        // End screens: short glass returns at both ends, each closed by a slim upright.
        foreach (var x in new[] { xs[0], xs[^1] })
        {
            Box(b, f, x - 0.015f, x + 0.015f, top + H.ScreenFoot, top + H.ScreenTop, PostZ + H.PostSize * 0.5f, H.EndScreen, L.Glass, 0);
            Box(b, f, x - 0.035f, x + 0.035f, top, top + H.ScreenTop + 0.04f, H.EndScreen, H.EndScreen + 0.07f, L.Metal, fine ? 0.01f : 0);
            Box(b, f, x - 0.035f, x + 0.035f, top + H.ScreenTop - 0.04f, top + H.ScreenTop + 0.04f, PostZ, H.EndScreen, L.Metal, 0);
        }
        // Timber bench over the two middle bays, on aluminium brackets.
        float s0 = xs[1] - 1.4f, s1 = xs[^2] + 1.4f, z0 = PostZ + H.SeatOff + 0.06f, z1 = z0 + H.SeatDepth;
        Box(b, f, s0, s1, top + H.SeatHeight - 0.055f, top + H.SeatHeight, z0, z1, L.Deck, fine ? 0.015f : 0);
        for (var x = s0 + 0.3f; x <= s1 - 0.25f; x += (s1 - s0 - 0.6f) / 3f)
            Box(b, f, x - 0.03f, x + 0.03f, top, top + H.SeatHeight - 0.055f, z0 + 0.04f, z1 - 0.06f, L.Metal, 0);
        // Downlight strip under the roof and its markers, one per bay (contract-12).
        Box(b, f, xs[0] + 0.4f, xs[^1] - 0.4f, under - 0.03f, under, H.Depth * 0.45f - 0.05f, H.Depth * 0.45f + 0.05f, L.LampLens, 0);
        foreach (var light in Lights(under)) b.Light(light);
    }

    public IReadOnlyList<LightEmitter> DescribeLights(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var top = Ground(context.World.Terrain.HeightAt) + H.PlatformRise;
        return Lights(top + H.Clear);
    }

    private static IReadOnlyList<LightEmitter> Lights(float under)
    {
        var f = Frame;
        var xs = PostsX;
        var lights = new List<LightEmitter>();
        for (var i = 0; i + 1 < xs.Length; i++)
        {
            var p = f.P((xs[i] + xs[i + 1]) * 0.5f, H.Depth * 0.45f);
            lights.Add(new(StreetStyle.StreetGroup, new(p.X, under - 0.08f, p.Y), StreetStyle.LampWarm, H.LightEnergy, H.LightRange));
        }
        return lights;
    }

    private static void Box(SiteBuilder b, PlanFrame f, float x0, float x1, float y0, float y1, float z0, float z1, ToonSpec material, float bevel) =>
        GridFacade.Box(b, f, x0, x1, y0, y1, z0, z1, material, bevel);

    public override void Collide(SiteColliders c)
    {
        var f = Frame;
        var y = Ground(c.Ground);
        var len = Length;
        var top = y + H.PlatformRise;
        var under = top + H.Clear;
        // Platform (a 0.18 m step up, auto-stepped), roof slab, posts as cylinders, the glass screens as walls, the bench.
        c.Box(f.At(len * 0.5f, (y - 0.12f + top) * 0.5f, (H.Depth - 0.25f) * 0.5f), new(len * 0.5f + H.PlatformMargin, (top - y + 0.12f) * 0.5f, (H.Depth + 0.25f) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        c.Box(f.At(len * 0.5f, under + H.RoofThick * 0.5f, (H.Depth + H.RoofFront - H.RoofBack) * 0.5f),
            new(len * 0.5f + 0.45f, H.RoofThick * 0.5f, (H.Depth + H.RoofFront + H.RoofBack) * 0.5f), SurfaceTag.Wall, FootMat.Metal);
        var xs = PostsX;
        foreach (var x in xs) c.Add(new PostCollider(f.P(x, PostZ), H.PostSize * 0.6f, top, under, SurfaceTag.Prop) { Foot = FootMat.Metal });
        c.Wall(f.P(xs[0], PostZ), f.P(xs[^1], PostZ), 0.03f, top, top + H.ScreenTop, SurfaceTag.Wall, FootMat.Glass);
        foreach (var x in new[] { xs[0], xs[^1] })
            c.Wall(f.P(x, PostZ), f.P(x, H.EndScreen + 0.07f), 0.03f, top, top + H.ScreenTop, SurfaceTag.Wall, FootMat.Glass);
        float s0 = xs[1] - 1.4f, s1 = xs[^2] + 1.4f, z0 = PostZ + H.SeatOff + 0.06f;
        c.Box(f.At((s0 + s1) * 0.5f, top + H.SeatHeight * 0.5f, z0 + H.SeatDepth * 0.5f), new((s1 - s0) * 0.5f, H.SeatHeight * 0.5f, H.SeatDepth * 0.5f), SurfaceTag.Prop, FootMat.WoodDeck);
    }
}
