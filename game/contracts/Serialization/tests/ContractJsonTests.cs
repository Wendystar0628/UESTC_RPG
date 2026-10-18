using System.Numerics;
using System.Text.Json;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class ContractJsonTests
{
    [TestCase]
    public void SceneTransformsAndIdentifiersKeepTheirWireShapes()
    {
        var matrix = Matrix4x4.CreateRotationY(.7f) * Matrix4x4.CreateTranslation(10, 2, -3);
        var text = JsonSerializer.Serialize(matrix, ContractJson.Options);
        if (!text.StartsWith('[') || JsonSerializer.Deserialize<Matrix4x4>(text, ContractJson.Options) != matrix)
            throw new InvalidOperationException("Transform must round-trip as a row-major 16-value array.");
        if (JsonSerializer.Serialize(new MapId("example"), ContractJson.Options) != "\"example\"")
            throw new InvalidOperationException("String identifiers must not become objects.");
        if (JsonSerializer.Serialize(Season.Autumn, ContractJson.Options) != "\"autumn\"")
            throw new InvalidOperationException("Enums must remain named camelCase strings.");
    }

    [TestCase]
    public void VectorShapeRejectsMissingOrTrailingCoordinates()
    {
        foreach (var text in new[] { "[1,2]", "[1,2,3,4]", "{}" })
        {
            try { JsonSerializer.Deserialize<Vector3>(text, ContractJson.Options); }
            catch (JsonException) { continue; }
            throw new InvalidOperationException("Malformed vector was accepted: " + text);
        }
    }
}
