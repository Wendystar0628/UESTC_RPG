using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

public static partial class FurniturePlanner
{
    // ---------------------------------------------------------------------------------------------------------
    // Common.
    // ---------------------------------------------------------------------------------------------------------

    private static FurnitureItem Item(string key, FurnitureKind kind, string part, BoxPartParams p, Matrix4x4 at, IReadOnlyList<Collider> local, IReadOnlyList<LightEmitter> emitters) =>
        new(key, kind, new PlacedPart(new(part), p, at)) { Emitters = emitters, Colliders = local.Select(c => Placed(c, at)).ToArray() };

    /// <summary>
    /// Rigid frame whose local −Z (every part's front) points along <paramref name="facing"/> and whose origin is (p.x, y, p.z).
    /// Rows are the images of local X, Y, Z (System.Numerics row vectors).
    /// </summary>
    public static Matrix4x4 Frame(Vector2 p, Vector2 facing, float y)
    {
        var f = Vector2.Normalize(facing);
        return new(-f.Y, 0, f.X, 0, 0, 1, 0, 0, -f.X, 0, -f.Y, 0, p.X, y, p.Y, 1);
    }

    /// <summary>Plan direction of a yaw in degrees: 0 = north (−Z), 90 = east (+X), the convention of anchors and entrances.</summary>
    public static Vector2 Facing(float yawDeg)
    {
        var a = yawDeg * (float.Pi / 180f);
        return new(DetMath.Sin(a), -DetMath.Cos(a));
    }

    /// <summary>Moves a part-local collider by a rigid placement matrix.</summary>
    public static Collider Placed(Collider collider, Matrix4x4 at) => collider switch
    {
        BoxCollider box => box with { At = box.At.Then(at) },
        PostCollider post => post with
        {
            C = new(post.C.X * at.M11 + post.C.Y * at.M31 + at.M41, post.C.X * at.M13 + post.C.Y * at.M33 + at.M43),
            Y0 = post.Y0 + at.M42,
            Y1 = post.Y1 + at.M42,
        },
        RampCollider ramp => ramp with { P0 = Xf.Point(at, ramp.P0), P1 = Xf.Point(at, ramp.P1), P2 = Xf.Point(at, ramp.P2), P3 = Xf.Point(at, ramp.P3) },
        _ => throw new ArgumentException("Unsupported collider kind for a placed street part.", nameof(collider)),
    };

    /// <summary>0, +step, −step, +2·step, … up to ±<paramref name="max"/>.</summary>
    private static IEnumerable<float> Shifts(float max)
    {
        yield return 0f;
        for (var d = S.ShiftStep; d <= max + 1e-3f; d += S.ShiftStep)
        {
            yield return d;
            yield return -d;
        }
    }

    private static float Extent(IReadOnlyList<Vector2> ring, Vector2 axis)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var p in ring)
        {
            var d = Vector2.Dot(p, axis);
            min = MathF.Min(min, d);
            max = MathF.Max(max, d);
        }
        return max - min;
    }

    private static Vector2 Anchor(WorldFeature feature)
    {
        if (feature.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("anchor", out var a) &&
            a.ValueKind == JsonValueKind.Array && a.GetArrayLength() >= 3)
            return new(a[0].GetSingle(), a[2].GetSingle());
        return new((feature.Bounds.Min.X + feature.Bounds.Max.X) * 0.5f, (feature.Bounds.Min.Z + feature.Bounds.Max.Z) * 0.5f);
    }

    private static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static float? Number(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : null;

    /// <summary>The yaw in degrees (0 = north, 90 = east, [0, 360)) of a plan direction: the inverse of <see cref="Facing"/>.</summary>
    private static float YawDeg(Vector2 facing)
    {
        var deg = DetMath.Atan2(facing.X, -facing.Y) * (180f / float.Pi);
        return deg < 0f ? deg + 360f : deg;
    }

    private static string Fixed(float v) => v.ToString("F2", CultureInfo.InvariantCulture);

    private static string Side(float side) => side < 0f ? "r" : "l";

    private static string Num(int k) => k.ToString(CultureInfo.InvariantCulture);

    private static string Num(float v) => v.ToString(CultureInfo.InvariantCulture);
}
