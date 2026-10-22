using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts.Save;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class ContractJsonTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Regression)]
    public void RuntimeIdentifiersRoundTripAsStrings()
    {
        var slot = new SlotId("s1");
        var json = JsonSerializer.Serialize(slot, ContractJson.Options);
        if (json != "\"s1\"" || JsonSerializer.Deserialize<SlotId>(json, ContractJson.Options) != slot)
            throw new InvalidOperationException("Runtime contract identifiers must round-trip as strings.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void ExplicitStringValuesKeepTheirWireShapes()
    {
        const string text = "example";
        object[] values = [new PaletteToken(text), new BuildingFamily(text), new FacadePreset(text), new DesignKind(text)];
        var expected = JsonSerializer.Serialize(text, ContractJson.Options);
        foreach (var value in values)
        {
            var json = JsonSerializer.Serialize(value, ContractJson.Options);
            if (json != expected || !Equals(JsonSerializer.Deserialize(json, value.GetType(), ContractJson.Options), value))
                throw new InvalidOperationException("Explicit string values must keep their existing scalar wire shapes.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
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
