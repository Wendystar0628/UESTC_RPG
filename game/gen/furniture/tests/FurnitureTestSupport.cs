using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using static GdUnit4.Assertions;

namespace Campus.Gen.Furniture.Tests;

/// <summary>The real world, the layout builder and the fixtures shared by the furniture test suites.</summary>
internal static class FurnitureTestSupport
{
    internal static readonly Lazy<IWorldModel> World = new(() => RealMapFixture.Load().World);

    internal static readonly Lazy<byte[]> PlantingBytes = new(() => RealMapFixture.ReadFiles()[FurnitureStyle.PlantingKey].ToArray());

    /// <summary>The map's frozen source inputs as the loader supplies them: the real planting rules.</summary>
    internal static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Inputs(byte[]? planting = null) =>
        new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { [FurnitureStyle.PlantingKey] = planting ?? PlantingBytes.Value };

    internal static FurnitureLayout Build(IWorldModel world, Func<FeatureId, Aspects, WpId?>? claimedBy = null, IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs = null) =>
        FurniturePlanner.Build(world, claimedBy, inputs ?? Inputs());

    /// <summary>The real planting rules, edited by <paramref name="change"/>, as the bytes the loader would supply.</summary>
    internal static byte[] Edit(Action<JsonNode> change)
    {
        var root = JsonNode.Parse(PlantingBytes.Value)!;
        change(root);
        return JsonSerializer.SerializeToUtf8Bytes(root);
    }

    internal static readonly Vector2[] Compass = [new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f), new(0.7f, 0.7f), new(-0.7f, 0.7f), new(0.7f, -0.7f), new(-0.7f, -0.7f)];

    /// <summary>The first spot (scanning west to east, north to south) with open ground all round: no outline, tree, road or paving near, inside the campus.</summary>
    internal static Vector2 OpenGround(IWorldModel world)
    {
        const float Clear = 30f;
        var site = new StreetSite(world);
        var lines = world.Network.Roads.Concat(world.Network.Bridges).ToList();
        for (var x = -1200f; x <= 1200f; x += 24f)
            for (var z = -1200f; z <= 1200f; z += 24f)
            {
                var p = new Vector2(x, z);
                if (!PlanGeometry.Contains(world.Boundary, p) || site.Blocker(p, 14f) is not null || site.PavingEdgeFacing(p) is not null) continue;
                if (!Compass.All(d => PlanGeometry.Contains(world.Boundary, p + d * Clear))) continue;
                if (lines.Any(l => (float)PlanGeometry.Project(l.Line.Points, p).Distance < Half(l) + Clear)) continue;
                return p;
            }
        throw new InvalidOperationException("The map has no open ground for a made-up walk.");
    }

    internal static (float Yaw, Vector2 Centre)? Turned(FurnitureLayout layout, string id)
    {
        var line = layout.Diagnostics.Where(d => d.Feature?.Value == id && d.Code == "wp13.anchor.turned").Select(d => d.Message).SingleOrDefault();
        if (line is null) return null;
        var m = System.Text.RegularExpressions.Regex.Match(line, @"yawDeg (-?\d+\.\d+) \(data -?\d+\.\d+\), centre \((-?\d+\.\d+), (-?\d+\.\d+)\) \(data");
        AssertThat(m.Success).OverrideFailureMessage("unreadable diagnostic message: " + line).IsTrue();
        float F(int g) => float.Parse(m.Groups[g].Value, System.Globalization.CultureInfo.InvariantCulture);
        return (F(1), new(F(2), F(3)));
    }

    /// <summary>The facing an anchor has in the layout: its data's yaw, or the yaw its "turned onto paving" diagnostic message gives.</summary>
    internal static Vector2 ExpectedFacing(FurnitureLayout layout, WorldFeature feature) =>
        Angles.Heading(Turned(layout, feature.Id.Value)?.Yaw ?? feature.Props!.Value.GetProperty("yawDeg").GetSingle());

    /// <summary>A copy of an anchor's props with one number replaced.</summary>
    internal static JsonElement Edit(JsonElement props, string name, float value)
    {
        var node = JsonNode.Parse(props.GetRawText())!;
        node[name] = value;
        return JsonSerializer.SerializeToElement(node);
    }

    internal static Vector2 Front(Matrix4x4 at) => Vector2.Normalize(new(-at.M31, -at.M33));

    internal static string? Highway(NetworkLine line) => RoadType(line.Props);

    internal static string? RoadType(JsonElement? props) =>
        props is { ValueKind: JsonValueKind.Object } p && (p.TryGetProperty("roadType", out var type) || p.TryGetProperty("highway", out type))
            ? WorldProps.TryText(type) : null;

    internal static float Half(NetworkLine line) => MathF.Max(line.Width, 2f) * 0.5f;

    internal static IEnumerable<Vector2> Corners(FurnitureItem item)
    {
        var p = (FurnitureParams)item.Part.Params;
        for (var i = -1; i <= 1; i += 2)
            for (var k = -1; k <= 1; k += 2)
            {
                var q = Xf.Point(item.Part.At, new(i * p.Width * 0.5f, 0, k * p.Depth * 0.5f));
                yield return new(q.X, q.Z);
            }
    }

    /// <summary>The corners of a box part's footprint and of the standing room <paramref name="front"/> ahead of it (local −Z).</summary>
    internal static IEnumerable<Vector2> Corners(FurnitureItem item, float front)
    {
        var p = (BoxPartParams)item.Part.Params;
        foreach (var i in new[] { -1f, 1f })
            foreach (var z in new[] { p.Depth * 0.5f, -p.Depth * 0.5f - front })
            {
                var q = Xf.Point(item.Part.At, new(i * p.Width * 0.5f, 0, z));
                yield return new(q.X, q.Z);
            }
    }

    internal static IEnumerable<PolygonRings> Polygons(WorldFeature feature) =>
        (feature.Parts.Count == 0 ? [feature.Geometry] : feature.Parts.Where(p => (p.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var k) ? k.GetString() : feature.Kind.Value) == feature.Kind.Value).Select(p => p.Geometry))
        .SelectMany(g => g switch { PolygonGeometry p => [p.Rings], MultiPolygonGeometry m => m.Polygons, _ => Array.Empty<PolygonRings>() });

    /// <summary>The world with made-up network lines, trees and features added (a feature with an existing id replaces that one).</summary>
    internal static FakeWorld MadeWorld(IWorldModel inner, IReadOnlyList<NetworkLine> lines, IReadOnlyList<WorldFeature> features, IReadOnlyList<NetworkTree>? trees = null)
    {
        var all = new Dictionary<FeatureId, WorldFeature>(inner.Features);
        foreach (var f in features) all[f.Id] = f;
        return new(inner)
        {
            Features = all,
            Network = inner.Network with { Roads = [.. inner.Network.Roads, .. lines], Trees = [.. inner.Network.Trees, .. trees ?? Array.Empty<NetworkTree>()] },
        };
    }
}
