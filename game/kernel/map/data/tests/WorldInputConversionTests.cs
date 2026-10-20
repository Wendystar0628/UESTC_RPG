using System.Numerics;
using System.Text;
using Campus.Contracts;
using Campus.Kernel.Maps;
using Campus.Kernel.Renderer;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class WorldInputConversionTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void GodotAndNumericsTransformsKeepOrientationAndTranslation()
    {
        var matrix = Matrix4x4.CreateScale(2, 3, 4) * Matrix4x4.CreateRotationY(.6f) * Matrix4x4.CreateTranslation(14, 6, -2);
        if (matrix.ToGodot().ToNumerics() != matrix)
            throw new InvalidOperationException("Row/column conversion lost matrix values.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WorldIntegerStoreysAcceptDecimalAndExponentEncoding()
    {
        using var stream = typeof(WorldInputConversionTests).Assembly.GetManifestResourceStream("Campus.Tests.world-levels-integer.json")!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        foreach (var encoded in new[] { "2.0", "2e0" })
        {
            var bytes = Encoding.UTF8.GetBytes(json.Replace("\"storeys\": 2.0", "\"storeys\": " + encoded, StringComparison.Ordinal));
            var data = Campus.Gen.World.WorldModelLoader.Load(new("integer-levels"),
                new Dictionary<string, ReadOnlyMemory<byte>>
                {
                    ["world/geo.json"] = Encoding.UTF8.GetBytes("""
                        {"origin":[0,0],"crs":"EPSG:32648","bounds":[0,0,10,10],"entityPrefix":"fixture",
                         "boundary":{"outer":[[0,0],[10,0],[10,10],[0,10]],"holes":[]}}
                        """),
                    ["world/fixture.json"] = bytes
                }, new() { Terrain = FlatTerrain.Default });
            if (data.World.Features.Single().Value.Storeys != 2)
                throw new InvalidOperationException("Integral world storey encoding was lost.");
        }
    }

}
