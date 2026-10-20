using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.BakeInput;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Water.Tests;

using static GdUnit4.Assertions;

/// <summary>Meshes the real water polygons of every map export and checks polygon coverage.</summary>
[TestSuite]
public sealed class WaterMapDataTests
{
    [TestCase, TestCategory(TestReason.Smoke)]
    [TestCategory("Slow")]
    public void EveryMapWaterMeshCoversItsPolygon()
    {
        var index = MapCatalog.ReadIndex(File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "maps", "index.json")));
        var checkedMaps = 0;
        foreach (var map in index.Maps.OrderBy(map => map.Id.Value, StringComparer.Ordinal))
        {
            var entry = RealMapFixture.Entry(map.Id);
            var bodies = WaterCatalog.Build(RealMapFixture.Load(map.Id == index.DefaultMap ? null : map.Id).World, entry.Descriptor);
            var meshes = bodies.Select(b => WaterMesher.Build(b, bodies)).ToArray();
            var triangles = meshes.Sum(m => m.TriangleCount);
            Console.WriteLine($"{map.Id.Value}: {bodies.Count} water bodies, {meshes.Sum(m => m.VertexCount)} vertices, {triangles} triangles");
            foreach (var m in meshes)
            {
                var area = 0f;
                for (var t = 0; t < m.TriangleCount; t++)
                {
                    int a = m.Index[3 * t], b = m.Index[3 * t + 1], c = m.Index[3 * t + 2];
                    area += MathF.Abs((m.Position[3 * b + 2] - m.Position[3 * a + 2]) * (m.Position[3 * c] - m.Position[3 * a]) -
                        (m.Position[3 * b] - m.Position[3 * a]) * (m.Position[3 * c + 2] - m.Position[3 * a + 2])) * 0.5f;
                }
                AssertThat(MathF.Abs(area - m.Body.Area) > MathF.Max(MathF.Pow(20 * TestTolerance.Layout, 2), m.Body.Area * 20 * TestTolerance.FloatRelative)).OverrideFailureMessage($"{m.Body.Id.Value}: mesh area {area} != polygon area {m.Body.Area}").IsFalse();
            }
            checkedMaps++;
        }
        AssertThat(checkedMaps == 0).OverrideFailureMessage("no map export found").IsFalse();
    }

}
