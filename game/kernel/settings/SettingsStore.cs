using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Settings;
using Godot;

namespace Campus.Kernel.Settings;

/// <summary>
/// The only reader/writer of the profile-level settings file user://settings.json (SYS-SAVE-FORMAT).
/// Top-level sections: ui, input, graphics, audio. Each system reads and writes only its own section.
/// JSON parsing never creates objects from file content; wrong-typed or out-of-range keys fall back to
/// the caller's default, unknown keys are ignored, missing sections read as defaults.
/// </summary>
[KernelService(typeof(ISettingsStore))]
public sealed class SettingsStore : ISettingsStore
{
    private const int MaxBytes = 256 * 1024;

    private readonly string _path;
    private readonly object _gate = new();      // guards _root
    private readonly object _fileGate = new();  // serializes disk writes
    private readonly JsonObject _root;

    public SettingsStore() : this(ProjectSettings.GlobalizePath("user://settings.json")) { }

    internal SettingsStore(string path)
    {
        _path = path;
        _root = Load(path);
    }

    public ISettingsSection Section(string name) => new SettingsSection(this, [name]);

    /// <summary>One write attempt: temp file, flush, replace keeping a .bak. False when the disk refused; the memory copy stays valid.</summary>
    public bool TrySave()
    {
        lock (_fileGate)
        {
            try
            {
                WriteFile(Encode());
                return true;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                GD.PushWarning($"settings.json not written: {error.Message}");
                return false;
            }
        }
    }

    /// <summary>Retrying write for callers that can wait; gives up after four attempts.</summary>
    public async Task SaveAsync()
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            if (TrySave()) return;
            await Task.Delay(50);
        }
    }

    private string Encode()
    {
        // Encoded inside the file lock from the current document, so a later write never restores older values.
        lock (_gate)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) _root.WriteTo(writer);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    private void WriteFile(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
        {
            stream.Write(Encoding.UTF8.GetBytes(text));
            stream.Flush(true);
        }
        if (File.Exists(_path)) File.Replace(temp, _path, _path + ".bak");
        else File.Move(temp, _path);
    }

    /// <summary>Missing file = defaults. Unreadable file falls back to .bak, then to defaults with a log line.</summary>
    private static JsonObject Load(string path)
    {
        if (!File.Exists(path)) return [];   // deleting the file is the reset; do not resurrect an old .bak
        foreach (var candidate in new[] { path, path + ".bak" })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                if (new FileInfo(candidate).Length > MaxBytes) throw new InvalidDataException("Settings file is too large.");
                var node = JsonNode.Parse(File.ReadAllText(candidate, Encoding.UTF8), null, new JsonDocumentOptions { MaxDepth = 8 });
                if (node is JsonObject root) return root;
                throw new InvalidDataException("Settings root is not an object.");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
            {
                GD.PushWarning($"{Path.GetFileName(candidate)} rejected; trying backup/defaults: {error.Message}");
            }
        }
        return [];
    }

    internal T Read<T>(string[] path, Func<JsonObject?, T> read)
    {
        lock (_gate) return read(Find(path, false));
    }

    internal void Write(string[] path, Action<JsonObject> write)
    {
        lock (_gate) write(Find(path, true)!);
    }

    private JsonObject? Find(string[] path, bool create)
    {
        var node = _root;
        foreach (var name in path)
        {
            if (node[name] is JsonObject child) { node = child; continue; }
            if (!create) return null;
            child = [];
            node[name] = child;
            node = child;
        }
        return node;
    }
}

/// <summary>One object inside settings.json. Readers take the caller's fallback and range; writers set a single primitive.</summary>
public sealed class SettingsSection : ISettingsSection
{
    private readonly SettingsStore _store;
    private readonly string[] _path;

    internal SettingsSection(SettingsStore store, string[] path)
    {
        _store = store;
        _path = path;
    }

    public ISettingsSection Child(string name) => new SettingsSection(_store, [.. _path, name]);

    public bool Has(string key) => _store.Read(_path, node => node?[key] is not null);

    public IReadOnlyList<string> Keys() => _store.Read<IReadOnlyList<string>>(_path, node => node?.Select(pair => pair.Key).ToArray() ?? []);

    public bool Bool(string key, bool fallback) =>
        _store.Read(_path, node => node?[key] is JsonValue value && value.GetValueKind() is JsonValueKind.True or JsonValueKind.False
            ? value.GetValue<bool>() : fallback);

    public int Int(string key, int fallback, int min, int max) =>
        _store.Read(_path, node =>
        {
            if (!TryNumber(node, key, out var number) || number != Math.Floor(number) || number < min || number > max) return fallback;
            return (int)number;
        });

    public float Float(string key, float fallback, float min, float max) =>
        _store.Read(_path, node => TryNumber(node, key, out var number) && number >= min && number <= max ? (float)number : fallback);

    /// <summary>The value when it is a finite number, so the caller can apply its own validity rule.</summary>
    public bool TryFloat(string key, out float value)
    {
        var found = _store.Read(_path, node => TryNumber(node, key, out var number) ? number : (double?)null);
        value = found is { } n ? (float)n : 0f;
        return found is not null;
    }

    public string? String(string key) =>
        _store.Read(_path, node => node?[key] is JsonValue value && value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : null);

    public void Set(string key, bool value) => _store.Write(_path, node => node[key] = JsonValue.Create(value));
    public void Set(string key, int value) => _store.Write(_path, node => node[key] = JsonValue.Create(value));
    public void Set(string key, string value) => _store.Write(_path, node => node[key] = JsonValue.Create(value));
    public void Set(string key, float value) =>
        _store.Write(_path, node => node[key] = JsonValue.Create(double.Parse(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture)));

    public void Remove(string key) => _store.Write(_path, node => node.Remove(key));

    public void Clear() => _store.Write(_path, node => node.Clear());

    private static bool TryNumber(JsonObject? node, string key, out double number)
    {
        number = 0;
        if (node?[key] is not JsonValue value || value.GetValueKind() != JsonValueKind.Number) return false;
        number = value.GetValue<double>();
        return double.IsFinite(number);
    }
}
