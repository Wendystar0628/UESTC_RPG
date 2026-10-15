using System.Text;
using System.Text.RegularExpressions;
using Campus.Contracts.Quality;
using Campus.Kernel.Quality.Adaptive;
using Godot;

namespace Campus.Kernel.Quality.Settings;

public sealed record StoredGraphics(QualityProfile Profile, DeviceTier Tier, string DeviceKey, bool UserSelected);

// WP-32's local storage seam: the service can be exercised without initializing Godot.
public interface IGraphicsSettingsStore
{
    StoredGraphics? Read();
    Task Write(QualityProfile profile, GraphicsDevice device, DeviceTier tier, string key, bool userSelected);
}

/// <summary>Owns only graphics.cfg and engine_override.cfg; no world/save-slot data enters these files.</summary>
public sealed class GraphicsSettingsStore(string directory, QualityCatalog catalog) : IGraphicsSettingsStore
{
    private readonly SemaphoreSlim _writer = new(1, 1);
    private readonly string _graphics = Path.Combine(directory, "graphics.cfg");
    private readonly string _engine = Path.Combine(directory, "engine_override.cfg");

    public StoredGraphics? Read()
    {
        // Absence of the primary file means an intentional reset; do not resurrect its old .bak.
        if (!File.Exists(_graphics)) return null;
        foreach (var path in new[] { _graphics, _graphics + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                if (new FileInfo(path).Length > 256 * 1024) throw new InvalidDataException("Settings file is too large.");
                var text = File.ReadAllText(path, Encoding.UTF8);
                if (!SafeText(text)) throw new InvalidDataException("Settings contain an object/resource constructor.");
                using var config = new ConfigFile();
                if (config.Parse(text) != Error.Ok) throw new InvalidDataException("Settings parsing failed.");
                var schema = Integer(config, "meta", "schema", 0);
                if (schema is < 0 or > 1) throw new InvalidDataException("Unsupported graphics settings schema.");
                while (schema < 1) schema = Migrate0To1(config);
                var presetName = Text(config, "profile", "preset", "balanced");
                var preset = presetName switch
                {
                    "smooth" => QualityPreset.Smooth,
                    "balanced" => QualityPreset.Balanced,
                    "fine" => QualityPreset.Fine,
                    "ultra" => QualityPreset.Ultra,
                    _ => QualityPreset.Balanced,
                };
                var overrides = new Dictionary<QualityKnob, float>();
                foreach (var knob in QualityCatalog.All)
                {
                    var name = QualityCatalog.Name(knob);
                    if (knob == QualityKnob.Adaptive || !config.HasSectionKey("profile", name)) continue;
                    using var value = config.GetValue("profile", name);
                    if (value.VariantType is not (Variant.Type.Int or Variant.Type.Float)) continue;
                    var number = (float)value.AsDouble();
                    if (catalog.IsValid(knob, number)) overrides[knob] = number;
                    else GD.PushWarning($"[WP-32] Ignoring invalid setting {name}.");
                }
                var tierValue = Integer(config, "device", "tier", (int)DeviceTier.MinSpec);
                var tier = tierValue is >= 0 and <= (int)DeviceTier.DgpuHigh ? (DeviceTier)tierValue : DeviceTier.MinSpec;
                return new(new(preset, overrides, Boolean(config, "profile", "adaptive", true)), tier,
                    Text(config, "device", "key", ""), Boolean(config, "profile", "user_selected", false));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                GD.PushWarning($"[WP-32] Cannot use {Path.GetFileName(path)}: {error.Message}");
            }
        }
        return null;
    }

    public async Task Write(QualityProfile profile, GraphicsDevice device, DeviceTier tier, string key, bool userSelected)
    {
        // Encode on the Godot main thread before doing filesystem work.
        using var config = new ConfigFile();
        config.SetValue("meta", "schema", 1);
        config.SetValue("meta", "gpu", device.Name);
        config.SetValue("meta", "vendor", device.Vendor);
        config.SetValue("meta", "driver", device.Driver);
        config.SetValue("device", "key", key);
        config.SetValue("device", "tier", (int)tier);
        config.SetValue("profile", "preset", profile.Preset.ToString().ToLowerInvariant());
        config.SetValue("profile", "adaptive", profile.Adaptive);
        config.SetValue("profile", "user_selected", userSelected);
        foreach (var pair in profile.Overrides.OrderBy(pair => pair.Key))
            config.SetValue("profile", QualityCatalog.Name(pair.Key), pair.Value);
        var graphicsText = config.EncodeToText();
        using var engine = new ConfigFile();
        var api = profile.Overrides.GetValueOrDefault(QualityKnob.GraphicsApi, catalog.PresetValue(profile.Preset, QualityKnob.GraphicsApi));
        engine.SetValue("rendering", "rendering_device/driver.windows", api == 0 ? "d3d12" : "vulkan");
        var engineText = engine.EncodeToText();
        await _writer.WaitAsync();
        try
        {
            await AtomicWrite(_graphics, graphicsText);
            await AtomicWrite(_engine, engineText);
        }
        finally { _writer.Release(); }
    }

    public static bool SafeText(string text) => !Regex.IsMatch(text,
        @"\b(?:Object|Resource|SubResource|ExtResource)\s*\(", RegexOptions.CultureInvariant);

    private static async Task AtomicWrite(string path, string text)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var bytes = Encoding.UTF8.GetBytes(text);
                using (var stream = new FileStream(path + ".tmp", FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
                else File.Move(path + ".tmp", path);
                return;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                if (attempt == 3) { GD.PushWarning($"[WP-32] Graphics settings remain in memory; write failed: {error.Message}"); return; }
                await Task.Delay(50);
            }
        }
    }

    private static int Migrate0To1(ConfigFile config)
    {
        if (!config.HasSectionKey("profile", "adaptive")) config.SetValue("profile", "adaptive", true);
        config.SetValue("meta", "schema", 1);
        return 1;
    }
    private static string Text(ConfigFile config, string section, string key, string fallback)
    {
        using var value = config.GetValue(section, key, fallback);
        return value.VariantType == Variant.Type.String ? value.AsString() : fallback;
    }
    private static int Integer(ConfigFile config, string section, string key, int fallback)
    {
        using var value = config.GetValue(section, key, fallback);
        if (value.VariantType != Variant.Type.Int) return fallback;
        var number = value.AsInt64();
        return number is >= int.MinValue and <= int.MaxValue ? (int)number : fallback;
    }
    private static bool Boolean(ConfigFile config, string section, string key, bool fallback)
    {
        using var value = config.GetValue(section, key, fallback);
        return value.VariantType == Variant.Type.Bool ? value.AsBool() : fallback;
    }
}
