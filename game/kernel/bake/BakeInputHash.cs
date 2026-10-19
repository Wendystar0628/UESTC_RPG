using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Kernel.Bake;

internal sealed class BakeInputHash
{
    private readonly XxHash128 _hash = new();
    public void Add<T>(T value)
    {
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes)) Write(writer, JsonSerializer.SerializeToElement(value, ContractJson.Options));
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(length, checked((int)bytes.Length));
        _hash.Append(length);
        _hash.Append(bytes.GetBuffer().AsSpan(0, checked((int)bytes.Length)));
    }
    public void File(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, string? path)
    {
        if (path is null) return;
        Add(path);
        if (!files.TryGetValue(path, out var bytes)) { Add(false); return; }
        using var parsed = JsonDocument.Parse(bytes);
        Add(parsed.RootElement);
    }
    public string Finish() => Convert.ToHexStringLower(_hash.GetCurrentHash());
    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                // These are the authored explanation fields. Names and displayed text remain effective inputs.
                if (property.Name is "note" or "notes" or "reason") continue;
                writer.WritePropertyName(property.Name);
                Write(writer, property.Value);
            }
            writer.WriteEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in element.EnumerateArray()) Write(writer, item);
            writer.WriteEndArray();
        }
        else if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var integer)) writer.WriteNumberValue(integer);
        else if (element.ValueKind == JsonValueKind.Number && element.TryGetUInt64(out var unsigned)) writer.WriteNumberValue(unsigned);
        else if (element.ValueKind == JsonValueKind.Number) writer.WriteNumberValue(element.GetDouble());
        else element.WriteTo(writer);
    }
}
