using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.Water.Tests;

using static GdUnit4.Assertions;

/// <summary>Meshes the real water polygons of every map export and checks coverage and the water triangle budget.</summary>
[TestSuite]
public sealed class WaterMapDataTests
{
    [TestCase]
    public void EveryMapStaysInsideTheWaterBudget()
    {
        var maps = Path.Combine(FindRepository(), "game", "data", "maps");
        var checkedMaps = 0;
        foreach (var file in Directory.EnumerateFiles(maps, "campus.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var bodies = Read(file);
            var meshes = bodies.Select(b => WaterMesher.Build(b, bodies)).ToArray();
            var triangles = meshes.Sum(m => m.TriangleCount);
            Console.WriteLine($"{file}: {bodies.Count} water bodies, {meshes.Sum(m => m.VertexCount)} vertices, {triangles} triangles");
            AssertThat(triangles > 50_000).OverrideFailureMessage($"water uses {triangles} triangles (budget 50 000)").IsFalse();
            foreach (var m in meshes)
            {
                var area = 0f;
                for (var t = 0; t < m.TriangleCount; t++)
                {
                    int a = m.Index[3 * t], b = m.Index[3 * t + 1], c = m.Index[3 * t + 2];
                    area += MathF.Abs((m.Position[3 * b + 2] - m.Position[3 * a + 2]) * (m.Position[3 * c] - m.Position[3 * a]) -
                        (m.Position[3 * b] - m.Position[3 * a]) * (m.Position[3 * c + 2] - m.Position[3 * a + 2])) * 0.5f;
                }
                AssertThat(MathF.Abs(area - m.Body.Area) > MathF.Max(1f, m.Body.Area * 2e-3f)).OverrideFailureMessage($"{m.Body.Id.Value}: mesh area {area} != polygon area {m.Body.Area}").IsFalse();
            }
            checkedMaps++;
        }
        AssertThat(checkedMaps == 0).OverrideFailureMessage("no map export found").IsFalse();
    }

    private static List<WaterBody> Read(string file)
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
        var bodies = new List<WaterBody>();
        foreach (var f in doc.RootElement.GetProperty("features").EnumerateArray())
        {
            if (!f.TryGetProperty("kind", out var kind) || kind.GetString() != "water") continue;
            foreach (var polygon in f.GetProperty("polygons").EnumerateArray())
            {
                var rings = polygon.EnumerateArray().Select(r => (IReadOnlyList<Vector2>)r.EnumerateArray()
                    .Select(p => new Vector2(p[0].GetSingle(), p[1].GetSingle())).ToList()).ToList();
                foreach (var r in rings)
                    if (r.Count > 1 && r[0] == r[^1]) ((List<Vector2>)r).RemoveAt(r.Count - 1);
                var shape = new PolygonRings(rings[0], rings.Skip(1).ToList());
                var area = WaterCatalog.Area(shape);
                if (area < 1f) continue;
                var min = rings[0].Aggregate(Vector2.Min);
                var max = rings[0].Aggregate(Vector2.Max);
                var len = 0f;
                foreach (var r in rings)
                    for (int i = 0, j = r.Count - 1; i < r.Count; j = i++) len += Vector2.Distance(r[i], r[j]);
                var kindOf = len * len / (4f * float.Pi * area) > WaterParams.Default.ChannelElongation ? WaterKind.Channel : WaterKind.Lake;
                bodies.Add(new(new FeatureId(f.GetProperty("id").GetString()!), kindOf, shape, 0f, -1, min, max, area));
            }
        }
        return bodies;
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }
}
