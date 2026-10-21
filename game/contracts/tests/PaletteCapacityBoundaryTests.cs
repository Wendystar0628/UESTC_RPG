using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

// Pure index, encoding and data-contract checks only. Native COLOR readback and shader/texture readiness remain integration checks.
[TestSuite]
public sealed class PaletteCapacityBoundaryTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LogicalIndexAndLayoutKeepBank0AndSeasonalDomains()
    {
        AssertThat(default(PaletteIndex).Value == 0 && new PaletteIndex(255).Value == 255).OverrideFailureMessage("Bank-0 index range changed.").IsTrue();
        AssertThrown(() => new PaletteIndex(-1)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => new PaletteIndex(512)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThat(PaletteLayout.FrameTexel(new(255), 3) == (255, 3)).OverrideFailureMessage("Bank-0 final frame-row texel moved.").IsTrue();
        AssertThat(PaletteLayout.FrameTexel(new(256), 0) == (0, 64)).OverrideFailureMessage("First extended texel is incorrect.").IsTrue();
        AssertThat(PaletteLayout.FrameTexel(new(511), 3) == (255, 67)).OverrideFailureMessage("Last extended texel is incorrect.").IsTrue();
        AssertThat(PaletteLayout.Bank0ByteCount == 65536 && PaletteLayout.ByteCount == 69632).OverrideFailureMessage("Approved texture extent changed.").IsTrue();
        for (var row = 0; row < 4; row++)
            for (var id = 0; id < 256; id++)
                AssertThat(PaletteLayout.FrameTexel(new(id), row) == (id, row)).OverrideFailureMessage("An existing colour byte offset moved.").IsTrue();
        AssertThat(PaletteLayout.SeasonalTexel(0, 4) == (0, 4) && PaletteLayout.SeasonalTexel(255, 63) == (255, 63)).OverrideFailureMessage("Seasonal endpoints moved.").IsTrue();
        AssertThrown(() => PaletteLayout.FrameTexel(new(256), 4)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => PaletteLayout.SeasonalTexel(256, 4)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => PaletteLayout.SeasonalTexel(0, 64)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThat(JsonSerializer.Serialize(new PaletteIndex(511), ContractJson.Options) == "511").OverrideFailureMessage("Index must be a JSON integer, not an object/string.").IsTrue();
        foreach (var invalid in new[] { "-1", "512", "1.5", "\"256\"", "null" })
        {
            bool rejected = false;
            try { JsonSerializer.Deserialize<PaletteIndex>(invalid, ContractJson.Options); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CatalogUsesOneSourceFormatAndPreservesBankRules()
    {
        var bank0 = Bank0();
        var bank0Json = JsonSerializer.Serialize(new PaletteRegistryData(bank0.Entries), ContractJson.Options);
        var extensions = new List<PaletteEntry> { Entry(256), Entry(257), Entry(510), Entry(511) };
        var json = JsonSerializer.Serialize(new PaletteRegistryData(extensions), ContractJson.Options);
        // Exercise the real property converter: the global enum converter would OR these strings into valid values.
        foreach (var (canonical, invalid) in new[]
        {
            ("shade", "lit,shade"), ("lit", "lit,lit"), ("shade", "shade,shade"),
            ("lit", "LIT"), ("lit", " lit"), ("lit", "lit ")
        })
        {
            var badJson = json.Replace($"\"role\":\"{canonical}\"", $"\"role\":\"{invalid}\"", StringComparison.Ordinal);
            AssertThat(badJson != json).OverrideFailureMessage("The role JSON regression did not modify a real input property.").IsTrue();
            {
                bool rejected = false;
                try { JsonSerializer.Deserialize<PaletteRegistryData>(badJson, ContractJson.Options); }
                catch (JsonException) { rejected = true; }
                AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
            }
        }
        // The shared metadata converter supports every existing bank-0 role.
        foreach (var role in Enum.GetValues<PaletteRole>())
        {
            var entryJson = JsonSerializer.Serialize(Entry(256) with { Role = role }, ContractJson.Options);
            var entryRoundTrip = JsonSerializer.Deserialize<PaletteEntry>(entryJson, ContractJson.Options) ?? throw new JsonException();
            AssertThat(entryRoundTrip.Role == role).OverrideFailureMessage("The shared metadata converter cannot represent a bank-0 role.").IsTrue();
        }
        var data = JsonSerializer.Deserialize<PaletteRegistryData>(json, ContractJson.Options) ?? throw new JsonException();
        var catalog = PaletteCatalog.FromData(new(bank0.Entries.Concat(data.Entries).ToArray()));
        var copied = new PaletteCatalog(bank0, extensions);
        extensions.Clear();
        AssertThat(copied.ExtensionCount == 4 && catalog.Resolve(new("ext.510")).Value == 510).OverrideFailureMessage("Catalog retained a mutable input list or truncated an index.").IsTrue();
        AssertThat(catalog.Get(new PaletteIndex(64)).Albedo == Vector3.One).OverrideFailureMessage("Bank-0 metadata changed.").IsTrue();
        AssertThat(bank0Json == JsonSerializer.Serialize(new PaletteRegistryData(bank0.Entries), ContractJson.Options)).OverrideFailureMessage("Bank-0 metadata changed.").IsTrue();
        AssertThat(json.Contains("\"index\":256", StringComparison.Ordinal)).OverrideFailureMessage("Every source must use a numeric index field.").IsTrue();
        AssertThrown(() => ((IList<PaletteEntry>)catalog.Entries).Clear()).IsInstanceOf<NotSupportedException>();
        AssertThrown(() => bank0.Get(new PaletteToken("ext.256"))).IsInstanceOf<KeyNotFoundException>();
        catalog.Validate(new(new("ext.256"), new("ext.257")));
        AssertThrown(() => catalog.Validate(new(new("ext.256"), new("ext.511")))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PaletteCatalog(bank0, [Entry(256)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PaletteCatalog(bank0, [Entry(256), Entry(257), Entry(256) with { Token = new("duplicate-index") }])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PaletteCatalog(bank0, [Entry(256) with { Token = new("old.lit") }, Entry(257)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PaletteCatalog(bank0, [Entry(256) with { Seasonal = true }, Entry(257)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PaletteCatalog(bank0, [Entry(256) with { Row = 4 }, Entry(257)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PaletteCatalog(bank0, [Entry(256) with { Role = PaletteRole.Emissive }, Entry(257)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PaletteCatalog(bank0, [Entry(256) with { Albedo = new(float.NaN, 0, 0) }, Entry(257)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new PaletteCatalog(bank0, [Entry(254), Entry(255)])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PaletteCatalog.FromData(new([], Schema: 1))).IsInstanceOf<ArgumentException>();
        {
            bool rejected = false;
            try { JsonSerializer.Deserialize<PaletteRegistryData>("{\"entries\":[]}", ContractJson.Options); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        {
            bool rejected = false;
            try { JsonSerializer.Deserialize<PaletteRegistryData>("{\"schema\":2,\"entries\":[],\"unknown\":1}", ContractJson.Options); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
        {
            bool rejected = false;
            try { JsonSerializer.Deserialize<PaletteRegistryData>(json.Replace("\"index\":256,", "", StringComparison.Ordinal), ContractJson.Options); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void GeometryIndicesRetainWidthAndHaveUnambiguousDecalSemantics()
    {
        var old = Geometry() with { ColorIndices = [64, 64, 64], Reserved = [1, 8, 15] };
        old.ValidateVertexData();
        var next = Geometry() with
        {
            ColorIndices = [256, 256, 256],
            DecalBaseColorIndices = [510, 510, 510],
            Flags = [4, 4, 4]
        };
        next.ValidateVertexData();
        var json = JsonSerializer.Serialize(next, ContractJson.Options);
        var restored = JsonSerializer.Deserialize<GeometryBundle>(json, ContractJson.Options) ?? throw new JsonException();
        restored.ValidateVertexData();
        AssertThat(restored.ColorIndices is [256, 256, 256] && restored.DecalBaseColorIndices is [510, 510, 510] &&
            restored.Reserved is null).OverrideFailureMessage("The new arrays lost width or gained a competing byte source.").IsTrue();
        var oldRestored = JsonSerializer.Deserialize<GeometryBundle>(JsonSerializer.Serialize(old, ContractJson.Options), ContractJson.Options)!;
        oldRestored.ValidateVertexData();
        AssertThat(oldRestored.ColorIndices is [64, 64, 64] && oldRestored.Reserved is [1, 8, 15]).OverrideFailureMessage("Old glass bytes were repurposed.").IsTrue();
        AssertThrown(() => (next with { Reserved = [0, 0, 0] }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (next with { Flags = [4, 0, 4] }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (next with { Flags = [20, 20, 20] }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (next with { ColorIndices = [512, 256, 256] }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (next with { DecalBaseColorIndices = [510, 510] }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ColorEncodingPreservesBank0BytesAndKeepsForegroundAndDecalBanksIndependent()
    {
        byte[] reservedValues = [0, 1, 8, 15, 128, 255];
        for (var sample = 0; sample < TestSamples.PerProperty; sample++)
        {
            var id = sample * byte.MaxValue / (TestSamples.PerProperty - 1);
            var flags = sample % 16;
            var reserved = reservedValues[sample % reservedValues.Length];
            var original = ((byte)id, (byte)(byte.MaxValue - id), (byte)flags, reserved);
            AssertThat(PaletteEncoding.EncodeColor(new(id), original.Item2, (byte)flags, reserved) == original).OverrideFailureMessage("A bank0 COLOR byte changed, including a pre-existing A payload.").IsTrue();
        }
        AssertThat(PaletteEncoding.EncodeColor(new(511), 77, 12, decalBase: new(256)) == ((byte)255, (byte)77, (byte)60, (byte)0)).OverrideFailureMessage("Both banks must fit independently.").IsTrue();
        AssertThat(PaletteEncoding.EncodeColor(new(256), 255, 4, decalBase: new(255)) == ((byte)0, (byte)255, (byte)20, (byte)255)).OverrideFailureMessage("Extended foreground aliased its bank0 base.").IsTrue();
        AssertThat(PaletteEncoding.EncodeColor(new(255), 255, 4, decalBase: new(256)) == ((byte)255, (byte)255, (byte)36, (byte)0)).OverrideFailureMessage("Extended base aliased its bank0 foreground.").IsTrue();
        AssertThat(PaletteEncoding.EncodeColor(new(256), 255, 1, 15) == ((byte)0, (byte)255, (byte)17, (byte)15)).OverrideFailureMessage("Non-decal reserved byte changed.").IsTrue();
        AssertThat(PaletteEncoding.DecodeColor(255, 60).Value == 511 && PaletteEncoding.DecodeDecalBase(0, 60).Value == 256).OverrideFailureMessage("Packed banks decoded incorrectly.").IsTrue();
        AssertThrown(() => PaletteEncoding.EncodeColor(new(256), 255, 16)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PaletteEncoding.EncodeColor(new(256), 255, 0, decalBase: new(256))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PaletteEncoding.EncodeColor(new(256), 255, 4, 1, new(256))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PaletteEncoding.DecodeColor(0, 64)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PaletteEncoding.DecodeColor(0, 32)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => PaletteEncoding.DecodeDecalBase(15, 1)).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void LampEncodingKeepsBank0FloatBitsAndRejectsUnassignedPackedValues()
    {
        for (var sample = 0; sample < TestSamples.PerProperty; sample++)
        {
            var id = sample * (PaletteLayout.Capacity - 1) / (TestSamples.PerProperty - 1);
            var percent = sample * 100 / (TestSamples.PerProperty - 1);
            var packed = PaletteEncoding.PackLampColor(new(id), percent / 100f);
            var decoded = PaletteEncoding.UnpackLampColor(packed);
            AssertThat(decoded.Index.Value == id && decoded.Energy == percent / 100f).OverrideFailureMessage("Lamp index/energy changed during round trip.").IsTrue();
            if (id < 256)
                AssertThat(BitConverter.SingleToInt32Bits(packed) == BitConverter.SingleToInt32Bits(id + 256f * percent)).OverrideFailureMessage("Bank-0 lamp bits changed.").IsTrue();
        }
        // Oracle is the existing PackColor formula, including clamping and midpoint rounding.
        foreach (var energy in new[] { -1f, 0.005f, 0.015f, 0.333f, 0.755f, 2f })
        {
            var bank0Packed = 255f + 256f * MathF.Round(Math.Clamp(energy, 0f, 1f) * 100f);
            AssertThat(BitConverter.SingleToInt32Bits(PaletteEncoding.PackLampColor(new(255), energy)) == BitConverter.SingleToInt32Bits(bank0Packed)).OverrideFailureMessage("Bank-0 non-grid energy changed.").IsTrue();
        }
        AssertThat(PaletteEncoding.PackLampColor(new(511), 1f) == 58623f).OverrideFailureMessage("The largest approved packed value changed.").IsTrue();
        AssertThrown(() => PaletteEncoding.PackLampColor(new(256), float.NaN)).IsInstanceOf<ArgumentOutOfRangeException>();
        foreach (var invalid in new[] { -1f, 0.5f, 25856f, 58624f, float.NaN, float.PositiveInfinity })
            AssertThrown(() => PaletteEncoding.UnpackLampColor(invalid)).IsInstanceOf<ArgumentOutOfRangeException>();
    }

    private static PaletteRegistry Bank0() => new([
        new(new("old.lit"), new(64), Vector3.One, "fixture", false, PaletteRole.Lit),
        new(new("old.shade"), new(65), new(.5f), "fixture", false, PaletteRole.Shade)]);
    private static PaletteEntry Entry(int index) => new(new("ext." + index), new(index), Vector3.One,
        "fixture", false, (index & 1) == 0 ? PaletteRole.Lit : PaletteRole.Shade);
    private static GeometryBundle Geometry() => new(new(new(new("old.lit"), new("old.shade"))),
        [0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2]);
}
