using Campus.Contracts.Audio;
using Campus.Contracts.Settings;
using Godot;

namespace Campus.Audio.Core;

/// <summary>Audio's view of the audio section in user://settings.json; debounces writes to the registered settings store.</summary>
internal sealed class AudioSettingsStore(ISettingsStore store)
{
    private const string Section = "audio";
    private AudioSettings? _pending;
    private float _remaining;
    private int _attempts;

    public AudioSettings Load()
    {
        var section = store.Section(Section);
        var defaults = new AudioSettings();
        return new AudioSettings(
            section.Float("master", defaults.Master, 0, 1), section.Float("ambience", defaults.Ambience, 0, 1),
            section.Float("foley", defaults.Foley, 0, 1), section.Float("ui", defaults.Ui, 0, 1), section.Float("music", defaults.Music, 0, 1),
            section.Bool("ui_sounds", true), section.Bool("hover_sounds", false), section.Bool("quiet_mode", false), section.Bool("mono", false),
            (SuddenSoundMode)section.Int("sudden", 0, 0, 2), (BackgroundBehavior)section.Int("background", 0, 0, 2),
            section.String("output_device") is { } device && !string.IsNullOrWhiteSpace(device) ? device : null);
    }

    public void Queue(AudioSettings settings)
    {
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
        var section = store.Section(Section);
        section.Set("master", settings.Master); section.Set("ambience", settings.Ambience);
        section.Set("foley", settings.Foley); section.Set("ui", settings.Ui); section.Set("music", settings.Music);
        section.Set("ui_sounds", settings.UiSounds); section.Set("hover_sounds", settings.HoverSounds);
        section.Set("quiet_mode", settings.QuietMode); section.Set("mono", settings.Mono);
        section.Set("sudden", (int)settings.Sudden); section.Set("background", (int)settings.Background);
        section.Set("output_device", settings.OutputDevice ?? "");
        if (store.TrySave()) { _pending = null; return; }
        // Retries advance on real frame delta; never block the main thread for 50 ms.
        _attempts++;
        _remaining = _attempts <= 3 ? .05f : float.PositiveInfinity;
        if (_attempts > 3) GD.PushWarning("Audio settings remain in memory; disk write failed.");
    }
}
