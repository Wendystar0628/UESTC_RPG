using System.Numerics;
using System.Text.Json;
using GdUnit4;

namespace Campus.Contracts.Tests;

// Pure boundary/compatibility checks only. Native COLOR readback and shader/texture readiness remain integration checks.
[TestSuite]
public sealed class PaletteCapacityBoundaryTests
{
    [TestCase]
    public void LogicalIndexAndLayoutKeepTheLegacyPrefixAndSeasonalDomain()
    {
        Check(default(PaletteIndex).Value == 0 && new PaletteIndex(255).ToLegacyByte() == 255, "Legacy index range changed.");
        Expect<ArgumentOutOfRangeException>(() => new PaletteIndex(-1));
        Expect<ArgumentOutOfRangeException>(() => new PaletteIndex(512));
        Expect<InvalidOperationException>(() => new PaletteIndex(256).ToLegacyByte());
        Check(PaletteLayout.FrameTexel(new(255), 3) == (255, 3), "Legacy final frame-row texel moved.");
        Check(PaletteLayout.FrameTexel(new(256), 0) == (0, 64), "First extended texel is incorrect.");
        Check(PaletteLayout.FrameTexel(new(511), 3) == (255, 67), "Last extended texel is incorrect.");
        Check(PaletteLayout.LegacyByteCount == 65536 && PaletteLayout.ByteCount == 69632, "Approved texture extent changed.");
        for (var row = 0; row < 4; row++)
            for (var id = 0; id < 256; id++)
                Check(PaletteLayout.FrameTexel(new(id), row) == (id, row), "An existing colour byte offset moved.");
        Check(PaletteLayout.SeasonalTexel(0, 4) == (0, 4) && PaletteLayout.SeasonalTexel(255, 63) == (255, 63), "Seasonal endpoints moved.");
        Expect<ArgumentOutOfRangeException>(() => PaletteLayout.FrameTexel(new(256), 4));
        Expect<ArgumentOutOfRangeException>(() => PaletteLayout.SeasonalTexel(256, 4));
        Expect<ArgumentOutOfRangeException>(() => PaletteLayout.SeasonalTexel(0, 64));
        Check(JsonSerializer.Serialize(new PaletteIndex(511), ContractJson.Options) == "511", "Index must be a JSON integer, not an object/string.");
        foreach (var invalid in new[] { "-1", "512", "1.5", "\"256\"", "null" })
            Expect<JsonException>(() => JsonSerializer.Deserialize<PaletteIndex>(invalid, ContractJson.Options));
    }

    [TestCase]
    public void CatalogIsAnImmutableUnionAndSchemaTwoCannotRedefineLegacyOrSeasonalEntries()
    {
        var legacy = Legacy();
        var legacyJson = JsonSerializer.Serialize(new PaletteRegistryData(legacy.Entries), ContractJson.Options);
        var extensions = new List<PaletteEntryV2> { Entry(256), Entry(257), Entry(510), Entry(511) };
        var json = JsonSerializer.Serialize(new PaletteExtensionData(extensions), ContractJson.Options);
        // Exercise the real property converter: the global enum converter would OR these strings into valid values.
        foreach (var (canonical, invalid) in new[]
        {
            ("shade", "lit,shade"), ("lit", "lit,lit"), ("shade", "shade,shade"),
            ("lit", "LIT"), ("lit", " lit"), ("lit", "lit ")
        })
        {
            var badJson = json.Replace($"\"role\":\"{canonical}\"", $"\"role\":\"{invalid}\"", StringComparison.Ordinal);
            Check(badJson != json, "The role JSON regression did not modify a real input property.");
            Expect<JsonException>(() => JsonSerializer.Deserialize<PaletteExtensionData>(badJson, ContractJson.Options));
        }
        // The V2 metadata view also represents legacy entries, so its local converter supports all existing roles.
        foreach (var role in Enum.GetValues<PaletteRole>())
        {
            var entryJson = JsonSerializer.Serialize(Entry(256) with { Role = role }, ContractJson.Options);
            var entryRoundTrip = JsonSerializer.Deserialize<PaletteEntryV2>(entryJson, ContractJson.Options) ?? throw new JsonException();
            Check(entryRoundTrip.Role == role, "The V2 metadata role converter cannot represent a legacy role.");
        }
        var oldCaseJson = legacyJson.Replace("\"role\":\"lit\"", "\"role\":\"LIT\"", StringComparison.Ordinal);
        var oldCaseData = JsonSerializer.Deserialize<PaletteRegistryData>(oldCaseJson, ContractJson.Options) ?? throw new JsonException();
        Check(oldCaseJson != legacyJson && PaletteRegistry.FromData(oldCaseData).Get((byte)64).Role == PaletteRole.Lit,
            "Local V2 validation broke legacy role casing compatibility.");
        var data = JsonSerializer.Deserialize<PaletteExtensionData>(json, ContractJson.Options) ?? throw new JsonException();
        var catalog = PaletteCatalog.FromData(legacy, data);
        var copied = new PaletteCatalog(legacy, extensions);
        extensions.Clear();
        Check(copied.ExtensionCount == 4 && catalog.Resolve(new("ext.510")).Value == 510, "Catalog retained a mutable input list or truncated an index.");
        Check(ReferenceEquals(catalog.LegacyRegistry, legacy) && catalog.Get(new PaletteIndex(64)).Albedo == Vector3.One, "Legacy metadata changed.");
        Check(legacyJson == JsonSerializer.Serialize(new PaletteRegistryData(legacy.Entries), ContractJson.Options), "Schema-1 serialization changed.");
        Check(json.Contains("\"index\":256", StringComparison.Ordinal), "Schema-2 must use a numeric index field.");
        Expect<NotSupportedException>(() => ((IList<PaletteEntryV2>)catalog.Entries).Clear());
        Expect<KeyNotFoundException>(() => legacy.Get(new PaletteToken("ext.256")));
        var fallback = PaletteCatalog.FromLegacy(legacy);
        Check(fallback.ExtensionCount == 0 && fallback.Count == legacy.Count && !fallback.TryGet(new PaletteIndex(256), out _), "Legacy fallback invented extension metadata.");
        catalog.Validate(new(new("ext.256"), new("ext.257")));
        Expect<ArgumentException>(() => catalog.Validate(new(new("ext.256"), new("ext.511"))));
        Expect<ArgumentException>(() => new PaletteCatalog(legacy, [Entry(256)]));
        Expect<ArgumentException>(() => new PaletteCatalog(legacy, [Entry(256), Entry(257), Entry(256) with { Token = new("duplicate-index") }]));
        Expect<ArgumentException>(() => new PaletteCatalog(legacy, [Entry(256) with { Token = new("old.lit") }, Entry(257)]));
        Expect<ArgumentException>(() => new PaletteCatalog(legacy, [Entry(256) with { Seasonal = true }, Entry(257)]));
        Expect<ArgumentException>(() => new PaletteCatalog(legacy, [Entry(256) with { Row = 4 }, Entry(257)]));
        Expect<ArgumentException>(() => new PaletteCatalog(legacy, [Entry(256) with { Role = PaletteRole.Emissive }, Entry(257)]));
        Expect<ArgumentException>(() => new PaletteCatalog(legacy, [Entry(256) with { Albedo = new(float.NaN, 0, 0) }, Entry(257)]));
        Expect<ArgumentException>(() => new PaletteCatalog(legacy, [Entry(254), Entry(255)]));
        Expect<ArgumentException>(() => PaletteCatalog.FromData(legacy, new([], Schema: 1)));
        Expect<JsonException>(() => JsonSerializer.Deserialize<PaletteExtensionData>("{\"entries\":[]}", ContractJson.Options));
        Expect<JsonException>(() => JsonSerializer.Deserialize<PaletteExtensionData>("{\"schema\":2,\"entries\":[],\"unknown\":1}", ContractJson.Options));
        Expect<JsonException>(() => JsonSerializer.Deserialize<PaletteExtensionData>(json.Replace("\"index\":256,", "", StringComparison.Ordinal), ContractJson.Options));
    }

    [TestCase]
    public void NewGeometryArraysRequireExplicitSupportAndHaveUnambiguousDecalSemantics()
    {
        var old = Geometry() with { ColorIds = [64, 64, 64], Reserved = [1, 8, 15] };
        old.ValidateVertexData();
        var next = Geometry() with
        {
            ColorIndices = [256, 256, 256],
            DecalBaseColorIndices = [510, 510, 510],
            Flags = [4, 4, 4]
        };
        Expect<NotSupportedException>(next.ValidateVertexData);
        next.ValidateVertexData(PaletteLayout.EncodingVersion);
        var json = JsonSerializer.Serialize(next, ContractJson.Options);
        var restored = JsonSerializer.Deserialize<GeometryBundle>(json, ContractJson.Options) ?? throw new JsonException();
        restored.ValidateVertexData(PaletteLayout.EncodingVersion);
        Check(restored.ColorIndices is [256, 256, 256] && restored.DecalBaseColorIndices is [510, 510, 510] &&
            restored.ColorIds is null && restored.Reserved is null, "The new arrays lost width or gained a competing byte source.");
        var oldRestored = JsonSerializer.Deserialize<GeometryBundle>(JsonSerializer.Serialize(old, ContractJson.Options), ContractJson.Options)!;
        oldRestored.ValidateVertexData();
        Check(oldRestored.ColorIndices is null && oldRestored.Reserved is [1, 8, 15], "Old glass bytes were repurposed.");
        Expect<ArgumentException>(() => (next with { ColorIds = [0, 0, 0] }).ValidateVertexData(2));
        Expect<ArgumentException>(() => (next with { Reserved = [0, 0, 0] }).ValidateVertexData(2));
        Expect<ArgumentException>(() => (next with { Flags = [4, 0, 4] }).ValidateVertexData(2));
        Expect<ArgumentException>(() => (next with { Flags = [20, 20, 20] }).ValidateVertexData(2));
        Expect<ArgumentException>(() => (next with { ColorIndices = [512, 256, 256] }).ValidateVertexData(2));
        Expect<ArgumentException>(() => (next with { DecalBaseColorIndices = [510, 510] }).ValidateVertexData(2));
        Expect<ArgumentOutOfRangeException>(() => next.ValidateVertexData(3));
    }

    [TestCase]
    public void ColorEncodingPreservesLegacyBytesAndKeepsForegroundAndDecalBanksIndependent()
    {
        foreach (var reserved in new byte[] { 0, 1, 8, 15, 128, 255 })
            for (var flags = 0; flags < 16; flags++)
                for (var id = 0; id < 256; id++)
                {
                    var original = ((byte)id, (byte)(255 - id), (byte)flags, reserved);
                    Check(PaletteEncoding.EncodeColor(new(id), original.Item2, (byte)flags, reserved) == original,
                        "A legacy COLOR byte changed, including a pre-existing A payload.");
                }
        Check(PaletteEncoding.EncodeColor(new(511), 77, 12, decalBase: new(256)) == ((byte)255, (byte)77, (byte)60, (byte)0), "Both banks must fit independently.");
        Check(PaletteEncoding.EncodeColor(new(256), 255, 4, decalBase: new(255)) == ((byte)0, (byte)255, (byte)20, (byte)255), "Extended foreground aliased its legacy base.");
        Check(PaletteEncoding.EncodeColor(new(255), 255, 4, decalBase: new(256)) == ((byte)255, (byte)255, (byte)36, (byte)0), "Extended base aliased its legacy foreground.");
        Check(PaletteEncoding.EncodeColor(new(256), 255, 1, 15) == ((byte)0, (byte)255, (byte)17, (byte)15), "Non-decal reserved byte changed.");
        Check(PaletteEncoding.DecodeColor(255, 60).Value == 511 && PaletteEncoding.DecodeDecalBase(0, 60).Value == 256, "Packed banks decoded incorrectly.");
        Expect<ArgumentException>(() => PaletteEncoding.EncodeColor(new(256), 255, 16));
        Expect<ArgumentException>(() => PaletteEncoding.EncodeColor(new(256), 255, 0, decalBase: new(256)));
        Expect<ArgumentException>(() => PaletteEncoding.EncodeColor(new(256), 255, 4, 1, new(256)));
        Expect<ArgumentException>(() => PaletteEncoding.DecodeColor(0, 64));
        Expect<ArgumentException>(() => PaletteEncoding.DecodeColor(0, 32));
        Expect<ArgumentException>(() => PaletteEncoding.DecodeDecalBase(15, 1));
    }

    [TestCase]
    public void LampEncodingKeepsLegacyFloatBitsAndRejectsUnassignedPackedValues()
    {
        for (var id = 0; id < 512; id++)
            for (var percent = 0; percent <= 100; percent++)
            {
                var packed = PaletteEncoding.PackLampColor(new(id), percent / 100f);
                var decoded = PaletteEncoding.UnpackLampColor(packed);
                Check(decoded.Index.Value == id && decoded.Energy == percent / 100f, "Lamp index/energy changed during round trip.");
                if (id < 256)
                    Check(BitConverter.SingleToInt32Bits(packed) == BitConverter.SingleToInt32Bits(id + 256f * percent), "Legacy lamp bits changed.");
            }
        // Oracle is the existing WP-39 PackColor formula, including clamping and midpoint rounding.
        foreach (var energy in new[] { -1f, 0.005f, 0.015f, 0.333f, 0.755f, 2f })
        {
            var legacy = 255f + 256f * MathF.Round(Math.Clamp(energy, 0f, 1f) * 100f);
            Check(BitConverter.SingleToInt32Bits(PaletteEncoding.PackLampColor(new(255), energy)) == BitConverter.SingleToInt32Bits(legacy), "Legacy non-grid energy changed.");
        }
        Check(PaletteEncoding.PackLampColor(new(511), 1f) == 58623f, "The largest approved packed value changed.");
        Expect<ArgumentOutOfRangeException>(() => PaletteEncoding.PackLampColor(new(256), float.NaN));
        foreach (var invalid in new[] { -1f, 0.5f, 25856f, 58624f, float.NaN, float.PositiveInfinity })
            Expect<ArgumentOutOfRangeException>(() => PaletteEncoding.UnpackLampColor(invalid));
    }

    private static PaletteRegistry Legacy() => new([
        new(new("old.lit"), 64, Vector3.One, "fixture", false, PaletteRole.Lit),
        new(new("old.shade"), 65, new(.5f), "fixture", false, PaletteRole.Shade)]);
    private static PaletteEntryV2 Entry(int index) => new(new("ext." + index), new(index), Vector3.One,
        "fixture", false, (index & 1) == 0 ? PaletteRole.Lit : PaletteRole.Shade);
    private static GeometryBundle Geometry() => new(new(new(new("old.lit"), new("old.shade"))),
        [0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2]);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
