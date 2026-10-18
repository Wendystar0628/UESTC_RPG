using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Bench (长椅, WLD-LIFE): three wood slats on two grey steel side frames, seat top at 0.45 m (a human anchor, never
/// scaled). Width runs along X; the sitter faces −Z. Variation 0 has a two-slat backrest leaning back towards +Z and Height is
/// its top; variation 1 is backless, sittable from both sides, and Height is ignored (the seat is the top). Depth is the
/// footprint: a backed bench's seat front lies on −Depth/2, a backless seat is centred.
/// Collision (WLD-LIFE): the seat block, jumpable and standable, plus a thin block behind it for the backrest. The seat
/// declaration (<see cref="IPartDef{TParams}.Seats"/>, contract-33) is read off that same seat block (<see cref="Seating.FromBox"/>).
/// </summary>
[Part(PartCatalog.Bench, Owner = "WP-13")]
public sealed class BenchPart : IPartDef<FurnitureParams>
{
    public BenchPart() => Lods = Levels.Of<FurnitureParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<FurnitureParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(420, 80);

    public Box3 Bounds(FurnitureParams p)
    {
        Validate(p);
        var back = p.Variation == 1 ? p.Depth * 0.5f : Math.Max(p.Depth * 0.5f, Back(p).MaxZ);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, Top(p), back));
    }

    public IReadOnlyList<Collider> Collision(FurnitureParams p)
    {
        var seat = SeatBlock(p);
        if (p.Variation == 1) return [seat];
        var back = Back(p);
        var z0 = back.FootZ - S.SlatThickness;
        var at = Xf.Move(0, (S.SeatHeight + p.Height) * 0.5f, (z0 + back.MaxZ) * 0.5f);
        return [seat, new BoxCollider(at, new(p.Width * 0.5f, (p.Height - S.SeatHeight) * 0.5f, (back.MaxZ - z0) * 0.5f), SurfaceTag.Prop) { Foot = seat.Foot }];
    }

    public IReadOnlyList<SeatDecl> Seats(FurnitureParams p) =>
        Seating.FromBox(SeatBlock(p), p.Variation == 1 ? SeatSide.Both : SeatSide.Front, p.Variation != 1);

    /// <summary>The standable seat block: full width, from the ground to the slat tops, over the slat depth.</summary>
    private static BoxCollider SeatBlock(FurnitureParams p)
    {
        Validate(p);
        var (front, depth) = Seat(p);
        var foot = p.Foot == FootMat.Auto ? S.BenchFoot : p.Foot;
        return new(Xf.Move(0, S.SeatHeight * 0.5f, front + depth * 0.5f), new(p.Width * 0.5f, S.SeatHeight * 0.5f, depth * 0.5f), SurfaceTag.Prop) { Foot = foot };
    }

    private PartGeometry Build(FurnitureParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var wood = S.Slat(S.Wood);
        var steel = S.Metal(S.GreyMetal);
        var (front, depth) = Seat(p);
        var hx = p.Width * 0.5f;
        var top = S.SeatHeight; var under = top - S.SlatThickness;
        if (near)
        {
            var slat = (depth - (S.SeatSlats - 1) * S.SlatGap) / S.SeatSlats;
            for (var k = 0; k < S.SeatSlats; k++)
            {
                var z = front + k * (slat + S.SlatGap);
                b.AddBox(new(-hx, under, z), new(hx, top, z + slat), S.SlatBevel, wood);
            }
        }
        else b.AddBox(new(-hx, under, front), new(hx, top, front + depth), 0, wood);
        // Side frames: front leg, bearer under the slats, and a rear leg or the leaning back post.
        var legX = hx - Math.Min(S.BenchLegInset, hx * 0.3f);
        var leg = S.BenchLeg; var bearer = under - 0.045f;
        var backed = p.Variation == 0;
        var frame = backed ? Back(p) : default;
        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * legX;
            b.AddBox(new(x - leg * 0.5f, 0, front + 0.04f), new(x + leg * 0.5f, under + S.Joint, front + 0.04f + leg), 0, steel);
            // The bearer is a little narrower than the legs, so no two steel faces are coplanar (no depth fighting).
            if (near) b.AddBox(new(x - leg * 0.5f + 0.004f, bearer, front + 0.02f), new(x + leg * 0.5f - 0.004f, under + S.Joint, front + depth - 0.02f), 0, steel);
            if (!backed)
                b.AddBox(new(x - leg * 0.5f, 0, front + depth - 0.04f - leg), new(x + leg * 0.5f, under + S.Joint, front + depth - 0.04f), 0, steel);
            else
                b.AddCentredBox(new(leg, frame.PostLength, leg), Xf.RotX(S.BackTiltDeg).Then(Xf.Move(x, frame.PostMid.X, frame.PostMid.Y)), steel);
        }
        if (!backed) return b.Finish(Bounds(p));
        // Backrest slats on the leaning plane through the back foot, just in front of the posts.
        var count = near ? S.BackSlats : 1;
        var height = near ? S.BackSlatHeight : frame.Span;
        for (var k = 0; k < count; k++)
        {
            var along = near ? frame.Span - height * 0.5f - k * (height + 0.05f) : frame.Span * 0.5f;
            var centre = new Vector2(frame.FootY + along * frame.Cos, frame.FootZ + along * frame.Sin);
            b.AddCentredBox(new(p.Width, height, S.SlatThickness), Xf.RotX(S.BackTiltDeg).Then(Xf.Move(0, centre.X, centre.Y)), wood, near ? S.SlatBevel : 0);
        }
        return b.Finish(Bounds(p));
    }

    private static float Top(FurnitureParams p) => p.Variation == 1 ? S.SeatHeight : p.Height;

    /// <summary>Seat front edge and slat depth. Backless benches centre the seat; backed benches keep it at the front.</summary>
    private static (float Front, float Depth) Seat(FurnitureParams p)
    {
        if (p.Variation == 0) return (-p.Depth * 0.5f, S.SeatDepth);
        var depth = Math.Min(p.Depth, S.SeatDepth + 0.08f);
        return (-depth * 0.5f, depth);
    }

    /// <summary>
    /// The leaning back: its plane passes through (FootY, FootZ) just behind the seat and rises along (Cos, Sin). Span is the slat
    /// run along the plane up to 1 cm under the top; the posts' centre lines sit behind the slats and run from the ground to the top.
    /// PostMid is (Y, Z) of a post's centre; MaxZ the furthest point of the backrest.
    /// </summary>
    private readonly record struct BackFrame(float FootY, float FootZ, float Sin, float Cos, float Span, float PostLength, Vector2 PostMid, float MaxZ);

    private static BackFrame Back(FurnitureParams p)
    {
        var (front, depth) = Seat(p);
        var tilt = S.BackTiltDeg * Angles.DegreesToRadians;
        var s = MathF.Sin(tilt); var c = MathF.Cos(tilt); var t = s / c;
        var footY = S.SeatHeight + S.BackRise; var footZ = front + depth + 0.005f;
        var span = (p.Height - 0.01f - footY) / c;
        // Post centre line: half a slat plus half a post behind the slat plane (measured horizontally).
        var offset = (S.SlatThickness * 0.5f + S.BenchLeg * 0.5f - 0.002f) / c;
        float PostZ(float y) => footZ + (y - footY) * t + offset;
        var half = S.BenchLeg * 0.5f;
        var y0 = half * s + 0.001f; var y1 = p.Height - half * s - 0.001f;
        var mid = (y0 + y1) * 0.5f;
        var maxZ = PostZ(y1) + half * c;
        return new(footY, footZ, s, c, span, (y1 - y0) / c, new(mid, PostZ(mid)), maxZ);
    }

    private static void Validate(FurnitureParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown bench variation.");
        if (p.Width < 0.8f) throw new ArgumentOutOfRangeException(nameof(p), "A bench is at least 0.8 m long.");
        if (p.Variation == 0 && (p.Height < S.SeatHeight + S.BackRise + 0.2f || p.Depth < S.SeatDepth + 0.05f))
            throw new ArgumentOutOfRangeException(nameof(p), "A backed bench needs room above and behind the seat.");
        if (p.Variation == 1 && p.Depth < 0.3f) throw new ArgumentOutOfRangeException(nameof(p), "A backless bench is at least 0.3 m deep.");
    }
}
