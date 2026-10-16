using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Parked car (停着的轿车, WLD-LIFE §3.3–§3.4; Opus review 13c2b 21:05): a plain family car in a low-saturation body colour, the body
/// rounded at nose and tail and chamfered along its shoulders, cut by wheel arches over 0.65 m wheels with light hubs, its underside
/// 0.15 m off the ground (no skirt); a glasshouse set in behind a ledge at the belt, its windows inside body-colour pillars, sill strip
/// and roof rail; clear head lamps and red tail lamps, never lit, wing mirrors (<see cref="CarBody"/>). Width × Height × Depth is the
/// car, its length along Z and its nose at −Z. Variation 0 is a saloon, 1 a hatchback (<see cref="StreetStyle.Saloon"/>,
/// <see cref="StreetStyle.Hatch"/>). The body colour is the caller's Material when it sets one, else <see cref="StreetStyle.CarBodies"/>[0].
/// Collision (WLD-LIFE §5): one box, Metal; the roof is out of jumping reach.
/// </summary>
[Part(PartCatalog.ParkedCar, Owner = "WP-13")]
public sealed class ParkedCarPart : IPartDef<VehicleParams>
{
    public ParkedCarPart() => Lods = Levels.Of<VehicleParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<VehicleParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(1200, 360);

    public Box3 Bounds(VehicleParams p)
    {
        Validate(p);
        // The bumpers and the intake stand a little proud of the nose and the tail.
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f - 0.025f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f + 0.025f));
    }

    public IReadOnlyList<Collider> Collision(VehicleParams p)
    {
        Validate(p);
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f - 0.05f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Prop)
            { Foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot }];
    }

    private PartGeometry Build(VehicleParams p, LodLevel lod)
    {
        Validate(p);
        return CarBody.Build(p, lod, Bounds(p));
    }

    private static void Validate(VehicleParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown car variation.");
        if (p.Width < 1.5f || p.Height < 1.3f || p.Depth < 3.6f || p.Depth > 5.2f) throw new ArgumentOutOfRangeException(nameof(p), "A car is at least 1.5 × 1.3 × 3.6 m and at most 5.2 m long.");
    }
}

/// <summary>
/// Anonymous shared bicycle (匿名共享单车, WLD-LIFE §3.5, D-066): a low-step milk-white frame with the electronic blue on the down
/// tube and the number plate only, a front basket and a rear lock box in dark grey, solid mudguard, dark tyres on light solid
/// wheels. No brand, no colours of a real scheme. Width × Height × Depth is the bike (handlebar width, bar height, wheel to wheel),
/// its front at −Z. The ride's contact points are part sockets shared with WP-54b's riding clips: seat, left and right grips,
/// left and right pedals, front and rear axles (the sitter faces −Z, so the left grip is at −X). Parked bikes have no collision and
/// make no sound (WLD-LIFE §5, SYS-AUDIO §9.8); the number on the plate waits for the scene-text atlas and WP-55's bay data.
/// </summary>
[Part(PartCatalog.ParkedBicycle, Owner = "WP-13")]
public sealed class ParkedBicyclePart : IPartDef<VehicleParams>
{
    public ParkedBicyclePart() => Lods = Levels.Of<VehicleParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<VehicleParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(960, 130);

    public Box3 Bounds(VehicleParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    public IReadOnlyDictionary<string, Matrix4x4> Sockets => SocketsAt(new VehicleParams(0.6f, 1.1f, 1.8f));

    /// <summary>The contact points for a bike of these sizes (part-local, Y up, front −Z).</summary>
    public static IReadOnlyDictionary<string, Matrix4x4> SocketsAt(VehicleParams p)
    {
        var k = Frame(p);
        return new SortedDictionary<string, Matrix4x4>(StringComparer.Ordinal)
        {
            ["seat"] = Xf.Move(0, k.Saddle.Y + 0.03f, k.Saddle.X),
            ["grip.left"] = Xf.Move(-p.Width * 0.5f + 0.05f, k.Bar.Y, k.Bar.X),
            ["grip.right"] = Xf.Move(p.Width * 0.5f - 0.05f, k.Bar.Y, k.Bar.X),
            ["pedal.left"] = Xf.Move(-0.1f, k.Crank.Y, k.Crank.X),
            ["pedal.right"] = Xf.Move(0.1f, k.Crank.Y, k.Crank.X),
            ["axle.front"] = Xf.Move(0, k.Front.Y, k.Front.X),
            ["axle.rear"] = Xf.Move(0, k.Rear.Y, k.Rear.X),
        };
    }

    private PartGeometry Build(VehicleParams p, LodLevel lod)
    {
        var k = Frame(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var white = S.Pole(S.BikeWhite); var blue = S.Pole(S.BikeBlue); var dark = S.Casing(S.BikeDark);
        var r = k.Front.Y; var t = S.BikeTyre;
        // Wheels: a light solid disc in a dark tyre ring (the ring only near); the disc stands in for the tyre beyond.
        foreach (var axle in new[] { k.Front, k.Rear })
        {
            var at = Xf.XUp.Then(Xf.Move(0, axle.Y, axle.X));
            if (near)
            {
                var ring = new Vector2[WheelRingSegments];
                for (var i = 0; i < ring.Length; i++)
                {
                    var a = 2.0 * Math.PI * i / ring.Length;
                    ring[i] = new((float)((r - t) * DetMath.Cos(a)), (float)((r - t) * DetMath.Sin(a)));
                }
                b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(2 * t, 2 * t, 0), ring, S.Pole(S.Tyre), closed: true, baseY: -t), at);
                b.AddLathe(Shapes.Creased(new(0, -0.012f), new(r - 2 * t + 0.004f, -0.012f), new(r - 2 * t + 0.004f, 0.012f), new(0, 0.012f)), WheelRingSegments, S.Pole(S.GreyMetal), at);
            }
            else b.AddLathe(Shapes.Creased(new(0, -0.025f), new(r, -0.025f), new(r, 0.025f), new(0, 0.025f)), 6, S.Pole(S.Tyre), at);
        }
        var tube = S.BikeTube; var seg = near ? 5 : 4;
        Vector3 P(Vector2 zy, float x = 0) => new(x, zy.Y, zy.X);
        var hw = p.Width * 0.5f;
        var basket = new Vector2(k.Front.X - 0.02f, k.Bar.Y - 0.19f);
        if (!near)
        {
            // Beyond the near level (ART-MODELING §7.8): the blue down tube, seat tube, head and bar, the saddle and the basket.
            b.AddPipe(P(k.HeadLow), P(k.Crank), tube * 1.25f, seg, blue);
            b.AddPipe(P(k.Crank), P(k.Saddle), tube, seg, white);
            b.AddPipe(P(k.HeadLow), P(k.Bar), tube * 1.2f, seg, white);
            b.AddPipe(P(k.Bar, -hw), P(k.Bar, hw), tube, seg, S.Pole(S.GreyMetal));
            b.AddBox(new(-0.075f, k.Saddle.Y, k.Saddle.X - 0.08f), new(0.075f, k.Saddle.Y + 0.055f, k.Saddle.X + 0.16f), 0, S.Casing(S.Tyre));
            b.AddBox(new(-0.15f, basket.Y - 0.1f, basket.X - 0.13f), new(0.15f, basket.Y + 0.08f, basket.X + 0.13f), 0, dark);
            return b.Finish(Bounds(p));
        }
        // Low-step frame: the blue down tube curves under the knee into the bottom bracket; seat tube, stays and fork in white.
        var knee = new Vector2((k.HeadLow.X + k.Crank.X) * 0.5f, k.Crank.Y + 0.06f);
        b.AddPipe(P(k.HeadLow), P(knee), tube * 1.25f, seg, blue);
        b.AddPipe(P(knee), P(k.Crank), tube * 1.25f, seg, blue);
        b.AddPipe(P(k.Crank), P(k.SeatTop), tube, seg, white);
        b.AddPipe(P(k.HeadLow), P(k.HeadTop), tube * 1.2f, seg, white);
        b.AddPipe(P(k.HeadLow), P(k.Front), tube * 0.9f, seg, white);
        {
            foreach (var x in new[] { -0.045f, 0.045f })
            {
                b.AddPipe(P(k.Crank, x * 0.6f), P(k.Rear, x), tube * 0.7f, seg, white);
                b.AddPipe(P(k.SeatTop - new Vector2(-0.02f, 0.06f), x * 0.4f), P(k.Rear, x), tube * 0.6f, seg, white);
            }
            // Cranks and pedals.
            foreach (var x in new[] { -1f, 1f })
                b.AddBox(new(x * 0.1f - 0.04f, k.Crank.Y - 0.012f, k.Crank.X - 0.05f), new(x * 0.1f + 0.04f, k.Crank.Y + 0.012f, k.Crank.X + 0.05f), 0, dark, VertexFlag.NoShadow);
            b.AddPipe(P(k.Crank, -0.1f), P(k.Crank, 0.1f), 0.016f, 4, S.Pole(S.GreyMetal), VertexFlag.NoShadow);
        }
        // Seat post and saddle; stem and handlebar with dark grips.
        b.AddPipe(P(k.SeatTop), P(k.Saddle), tube * 0.8f, seg, S.Pole(S.GreyMetal));
        b.AddBox(new(-0.075f, k.Saddle.Y, k.Saddle.X - 0.08f), new(0.075f, k.Saddle.Y + 0.055f, k.Saddle.X + 0.16f), near ? 0.015f : 0, S.Casing(S.Tyre));
        b.AddPipe(P(k.HeadTop), P(k.Bar), tube, seg, white);
        b.AddPipe(P(k.Bar, -hw + 0.12f), P(k.Bar, hw - 0.12f), tube * 0.8f, seg, S.Pole(S.GreyMetal));
        foreach (var x in new[] { -1f, 1f })
            b.AddPipe(P(k.Bar, x * (hw - 0.13f)), P(k.Bar, x * hw), 0.018f, 4, S.Pole(S.Tyre));
        // Front basket over the front wheel, rear lock box at the hub, mudguard over the rear wheel.
        b.AddBox(new(-0.15f, basket.Y - 0.1f, basket.X - 0.13f), new(0.15f, basket.Y + 0.08f, basket.X + 0.13f), 0.015f, dark);
        b.AddBox(new(-0.06f, k.Rear.Y + 0.02f, k.Rear.X - 0.11f), new(0.06f, k.Rear.Y + 0.16f, k.Rear.X + 0.07f), 0, dark);
        b.AddBox(new(-0.04f, k.Rear.Y + r + 0.012f, k.Rear.X - 0.22f), new(0.04f, k.Rear.Y + r + 0.03f, k.Rear.X + r * 0.9f), 0, S.Casing(S.BikeWhite), VertexFlag.NoShadow);
        // The number plate in front of the bar (blue, its number waits for the scene-text atlas).
        b.AddBox(new(-0.08f, k.Bar.Y - 0.13f, k.HeadTop.X - 0.05f), new(0.08f, k.Bar.Y - 0.04f, k.HeadTop.X - 0.04f), 0, S.Board(S.BikeBlue), VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    /// <summary>Side-view points (z, y) of the frame for a bike of these sizes: axles, bottom bracket, seat, head tube, bar.</summary>
    private readonly record struct BikeFrame(Vector2 Front, Vector2 Rear, Vector2 Crank, Vector2 SeatTop, Vector2 Saddle, Vector2 HeadLow, Vector2 HeadTop, Vector2 Bar);

    /// <summary>L0 tyre ring and hub disc segments (Opus review 13c2c: a round wheel up close); L1 and below keep their own.</summary>
    private const int WheelRingSegments = 24;

    private static BikeFrame Frame(VehicleParams p)
    {
        Validate(p);
        var r = Math.Min(S.BikeWheelRadius, p.Depth * 0.18f);
        var half = p.Depth * 0.5f - r;
        var bar = Math.Min(S.BikeBarY, p.Height - 0.03f);
        var saddle = Math.Min(S.BikeSaddleY, bar - 0.1f);
        return new(new(-half, r), new(half, r), new(0.12f * half / 0.57f, 0.29f), new(0.22f * half / 0.57f, saddle - 0.12f), new(0.26f * half / 0.57f, saddle),
            new(-0.42f * half / 0.57f, 0.7f), new(-0.38f * half / 0.57f, bar - 0.1f), new(-0.34f * half / 0.57f, bar));
    }

    private static void Validate(VehicleParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 0.45f || p.Height < 0.95f || p.Depth < 1.5f || p.Depth > 2f) throw new ArgumentOutOfRangeException(nameof(p), "A bicycle is 1.5–2 m long, at least 0.95 m to the bar and 0.45 m across it.");
    }
}
