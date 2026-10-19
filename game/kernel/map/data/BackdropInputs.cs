using Campus.Gen.World;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Maps;

/// <summary>Map-load boundary for optional backdrop bytes and their authored camera extent.</summary>
public static class BackdropInputs
{
    public static BuildInputSnapshot Freeze(MapDescriptor map, string dataRoot,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, Func<string, bool> exists,
        Func<string, ReadOnlyMemory<byte>> read, string version)
    {
        var inputs = new Dictionary<string, ReadOnlyMemory<byte>>(files, StringComparer.Ordinal);
        AddOptional(map, dataRoot, inputs, exists, read);
        return BuildInputSnapshot.Freeze(inputs, version);
    }

    /// <summary>Borrow already captured bytes; explicitly read the authored path even when its extension is not .json.</summary>
    public static void AddOptional(MapDescriptor map, string dataRoot,
        IDictionary<string, ReadOnlyMemory<byte>> inputs, Func<string, bool> exists,
        Func<string, ReadOnlyMemory<byte>> read)
    {
        var key = map.Env.Backdrop;
        MapCatalog.ValidateRelative(key);
        if (!dataRoot.StartsWith("res://", StringComparison.Ordinal) || !dataRoot.EndsWith('/'))
            throw new InvalidDataException("Backdrop data root must be an absolute res:// directory.");
        MapCatalog.ValidateRelative(dataRoot[6..].TrimEnd('/'));
        if (inputs.TryGetValue(key, out var captured))
        {
            if (captured.IsEmpty) throw new InvalidDataException($"Empty backdrop input: {key}.");
            return;
        }
        // Absence is optional; an existing empty/unreadable file is a load failure.
        if (exists(dataRoot + key))
        {
            var bytes = read(dataRoot + key);
            if (bytes.IsEmpty) throw new InvalidDataException($"Empty backdrop input: {key}.");
            inputs[key] = bytes;
        }
    }

    public static float CameraFar(MapDescriptor map, BuildInputSnapshot inputs, float current, float near)
    {
        var key = map.Env.Backdrop;
        MapCatalog.ValidateRelative(key);
        if (!inputs.Entries.TryGetValue(key, out var bytes)) return current;
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("farClip", out var property) ||
                property.ValueKind != JsonValueKind.Number || !property.TryGetSingle(out var value) ||
                !float.IsFinite(value) || value <= near)
                throw new InvalidDataException($"Backdrop '{key}' requires finite farClip greater than camera Near ({near}).");
            return value;
        }
        catch (JsonException error)
        { throw new InvalidDataException($"Invalid backdrop JSON: {key}.", error); }
    }
}
