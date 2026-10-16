using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

/// <summary>Logical palette index 0..511. Default is the existing index zero, not an unknown-index sentinel.</summary>
[JsonConverter(typeof(PaletteIndexJsonConverter))]
public readonly record struct PaletteIndex
{
    public PaletteIndex(int value)
    {
        if (value is < 0 or >= PaletteLayout.Capacity)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Palette index must be in [0, 511].");
        Value = (ushort)value;
    }

    public ushort Value { get; }

    /// <summary>Explicit legacy conversion. Extended indices fail instead of truncating to another colour.</summary>
    public byte ToLegacyByte() => Value < PaletteLayout.LegacyCapacity ? (byte)Value
        : throw new InvalidOperationException("An extended palette index cannot use a legacy byte API.");
}

// Scoped to the new type; the shared ContractJson options and all legacy representations remain unchanged.
internal sealed class PaletteIndexJsonConverter : JsonConverter<PaletteIndex>
{
    public PaletteIndexJsonConverter() { }

    public override PaletteIndex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value) ||
            value is < 0 or >= PaletteLayout.Capacity)
            throw new JsonException("Expected an integer palette index in [0, 511].");
        return new(value);
    }

    public override void Write(Utf8JsonWriter writer, PaletteIndex value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
