using System.Text;
using System.Text.RegularExpressions;
using Campus.Contracts.Audio;
using Godot;

namespace Campus.Audio.Core;

/// <summary>The only audio.cfg reader/writer. Never passes object syntax to ConfigFile.</summary>
internal sealed class AudioSettingsStore(bool fixedSettings)
{
    private readonly string _path = ProjectSettings.GlobalizePath("user://audio.cfg");
    private AudioSettings? _pending;
    private float _remaining;
    private int _attempts;

    public AudioSettings Load()
    {
        if (fixedSettings) return new();
        foreach (var path in new[] { _path, _path + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                if (new FileInfo(path).Length > 65536) throw new InvalidDataException("Settings file exceeds 64 KiB.");
                var text = File.ReadAllText(path, Encoding.UTF8);
                if (AudioSettingsText.ContainsObjectConstruction(text))
                    throw new InvalidDataException("Object/resource syntax is forbidden in settings.");
                using var config = new ConfigFile();
                if (config.Parse(text) != Error.Ok) throw new InvalidDataException("Invalid audio settings text.");
                var defaults = new AudioSettings();
                using var schema = Read(config, "schema");
                if (schema.VariantType != Variant.Type.Nil && (schema.VariantType != Variant.Type.Int || schema.AsInt64() != 1))
                    throw new InvalidDataException("Unsupported audio settings schema.");
                return new AudioSettings(
                    Number(config, "master", defaults.Master), Number(config, "ambience", defaults.Ambience),
                    Number(config, "foley", defaults.Foley), Number(config, "ui", defaults.Ui), Number(config, "music", defaults.Music),
                    Bool(config, "ui_sounds", true), Bool(config, "hover_sounds", false), Bool(config, "quiet_mode", false), Bool(config, "mono", false),
                    (SuddenSoundMode)Int(config, "sudden", 0, 0, 2), (BackgroundBehavior)Int(config, "background", 0, 0, 2),
                    Text(config, "output_device"));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
            { GD.PushWarning($"Audio settings rejected; trying backup/defaults: {error.Message}"); }
        }
        return new();
    }

    private static Variant Read(ConfigFile config, string key) => config.HasSectionKey("audio", key) ? config.GetValue("audio", key) : default;
    private static float Number(ConfigFile config, string key, float fallback)
    {
        using var value = Read(config, key);
        if (value.VariantType is not (Variant.Type.Float or Variant.Type.Int)) return fallback;
        var number = value.AsDouble();
        return double.IsFinite(number) && number is >= 0 and <= 1 ? (float)number : fallback;
    }
    private static bool Bool(ConfigFile config, string key, bool fallback)
    { using var value = Read(config, key); return value.VariantType == Variant.Type.Bool ? value.AsBool() : fallback; }
    private static int Int(ConfigFile config, string key, int fallback, int min, int max)
    {
        using var value = Read(config, key);
        if (value.VariantType != Variant.Type.Int) return fallback;
        var number = value.AsInt64();
        return number >= min && number <= max ? (int)number : fallback;
    }
    private static string? Text(ConfigFile config, string key)
    {
        using var value = Read(config, key);
        return value.VariantType == Variant.Type.String && !string.IsNullOrWhiteSpace(value.AsString()) ? value.AsString() : null;
    }

    public void Queue(AudioSettings settings)
    {
        if (fixedSettings) return;
        _pending = settings; _remaining = .5f; _attempts = 0;
    }

    public void Step(float dt)
    {
        if (_pending is null) return;
        _remaining -= dt;
        if (_remaining <= 0f) WritePending();
    }

    public void Flush() { if (_pending is not null) WritePending(); }

    private void WritePending()
    {
        var settings = _pending!;
        try
        {
            using var config = new ConfigFile();
            config.SetValue("audio", "schema", settings.Schema);
            config.SetValue("audio", "master", settings.Master); config.SetValue("audio", "ambience", settings.Ambience);
            config.SetValue("audio", "foley", settings.Foley); config.SetValue("audio", "ui", settings.Ui); config.SetValue("audio", "music", settings.Music);
            config.SetValue("audio", "ui_sounds", settings.UiSounds); config.SetValue("audio", "hover_sounds", settings.HoverSounds);
            config.SetValue("audio", "quiet_mode", settings.QuietMode); config.SetValue("audio", "mono", settings.Mono);
            config.SetValue("audio", "sudden", (int)settings.Sudden); config.SetValue("audio", "background", (int)settings.Background);
            config.SetValue("audio", "output_device", settings.OutputDevice ?? "");
            var bytes = Encoding.UTF8.GetBytes(config.EncodeToText());
            using (var stream = new FileStream(_path + ".tmp", FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
            { stream.Write(bytes); stream.Flush(true); }
            if (File.Exists(_path)) File.Replace(_path + ".tmp", _path, _path + ".bak");
            else File.Move(_path + ".tmp", _path);
            _pending = null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Retries advance on real frame delta; never block the main thread for 50 ms.
            _attempts++;
            _remaining = _attempts <= 3 ? .05f : float.PositiveInfinity;
            if (_attempts > 3) GD.PushWarning($"Audio settings remain in memory; disk write failed: {error.Message}");
        }
    }
}

/// <summary>Text-only guard executed before ConfigFile can instantiate Variant constructors.</summary>
public static class AudioSettingsText
{
    private static readonly Regex Constructors = new(@"\b(?:Object|Resource|SubResource|ExtResource)\s*\(",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public static bool ContainsObjectConstruction(string text) => Constructors.IsMatch(text);
}
