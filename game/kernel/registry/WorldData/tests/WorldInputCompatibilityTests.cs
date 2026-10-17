using System.Numerics;
using System.Text;
using Campus.Contracts;
using Campus.Kernel.Maps;
using Campus.Kernel.Renderer;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class WorldInputCompatibilityTests
{
    [TestCase]
    public void GodotAndNumericsTransformsKeepOrientationAndTranslation()
    {
        var matrix = Matrix4x4.CreateScale(2, 3, 4) * DetMath.RotationY(.6f) * Matrix4x4.CreateTranslation(14, 6, -2);
        if (matrix.ToGodot().ToNumerics() != matrix)
            throw new InvalidOperationException("Row/column conversion lost matrix values.");
    }

    [TestCase]
    public void CampusIntegerLevelsAcceptDecimalAndExponentEncoding()
    {
        using var stream = typeof(WorldInputCompatibilityTests).Assembly.GetManifestResourceStream("Campus.Tests.campus-levels-integer.json")!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        foreach (var encoded in new[] { "2.0", "2e0" })
        {
            var bytes = Encoding.UTF8.GetBytes(json.Replace("\"levels\": 2.0", "\"levels\": " + encoded, StringComparison.Ordinal));
            var data = new WorldModelLoaderAdapter().Load(new("integer-levels"),
                new Dictionary<string, ReadOnlyMemory<byte>> { ["campus.json"] = bytes });
            if (data.World.Features.Single().Value.Storeys != 2)
                throw new InvalidOperationException("Integral source level encoding was lost.");
        }
    }
}
