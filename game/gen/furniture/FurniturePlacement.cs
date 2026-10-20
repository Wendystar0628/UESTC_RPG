using System.Text.Json;
using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlanner;

namespace Campus.Gen.Furniture;

/// <summary>Creates placed items and stable placement keys; shared plan-space measurements.</summary>
internal static class FurniturePlacement
{
    internal static FurnitureItem Item(string key, FurnitureKind kind, string part, BoxPartParams p, Matrix4x4 at, IReadOnlyList<Collider> local, IReadOnlyList<LightEmitter> emitters) =>
        new(key, kind, new PlacedPart(new(part), p, at)) { Emitters = emitters, Colliders = local.Select(c => FurniturePlanner.Placed(c, at)).ToArray() };

    /// <summary>0, +step, −step, +2·step, … up to ±<paramref name="max"/>.</summary>
    internal static IEnumerable<float> Shifts(float max)
    {
        yield return 0f;
        for (var d = S.ShiftStep; d <= max + 1e-3f; d += S.ShiftStep)
        {
            yield return d;
            yield return -d;
        }
    }

    internal static float Extent(IReadOnlyList<Vector2> ring, Vector2 axis)
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

    internal static Vector2 Anchor(WorldFeature feature)
    {
        if (feature.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("anchor", out var a) &&
            a.ValueKind == JsonValueKind.Array && a.GetArrayLength() >= 3)
            return new(a[0].GetSingle(), a[2].GetSingle());
        return new((feature.Bounds.Min.X + feature.Bounds.Max.X) * 0.5f, (feature.Bounds.Min.Z + feature.Bounds.Max.Z) * 0.5f);
    }

    internal static string Fixed(float v) => v.ToString("F2", CultureInfo.InvariantCulture);

    internal static string Side(float side) => side < 0f ? "r" : "l";

    internal static string Num(int k) => k.ToString(CultureInfo.InvariantCulture);

    internal static string Num(float v) => v.ToString(CultureInfo.InvariantCulture);

    internal static FurnitureItem Fitted(string key, FurnitureKind kind, string part, BoxPartParams p, WalkFit fit, IReadOnlyList<Collider> local)
    {
        var item = Item(key, kind, part, p, Frame(fit.Centre, fit.Facing, fit.Top), local, []);
        return fit.Bay is { } bay ? item with { Pad = bay, Colliders = [.. item.Colliders, bay.Collider()] } : item;
    }
}
