using KernelServices = Campus.Kernel.Registry.Kernel;
using Campus.Contracts.Diagnostics;
using Campus.Contracts.Audio;
using Campus.Contracts.Settings;
using Godot;

namespace Campus.Audio.Core;

/// <summary>Audio's view of the audio section in user://settings.json; debounces writes to the registered settings store.</summary>
internal sealed class AudioSettingsStore(ISettingsStore store)
{
    private AudioSettings? _pending;
    private float _remaining;
    private int _attempts;

    public AudioSettings Load()
    {
        var section = store.Section(AudioSettingDefinitions.Section);
        return new AudioSettings(
            section.Get(AudioSettingDefinitions.Master), section.Get(AudioSettingDefinitions.Ambience),
            section.Get(AudioSettingDefinitions.Foley), section.Get(AudioSettingDefinitions.Ui), section.Get(AudioSettingDefinitions.Music),
            section.Get(AudioSettingDefinitions.UiSounds), section.Get(AudioSettingDefinitions.HoverSounds),
            section.Get(AudioSettingDefinitions.QuietMode), section.Get(AudioSettingDefinitions.Mono),
            (SuddenSoundMode)section.Get(AudioSettingDefinitions.Sudden), (BackgroundBehavior)section.Get(AudioSettingDefinitions.Background),
            section.Get(AudioSettingDefinitions.OutputDevice) is { } device && !string.IsNullOrWhiteSpace(device)
                ? device : AudioSettings.Default.OutputDevice);
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
        var section = store.Section(AudioSettingDefinitions.Section);
        section.Set(AudioSettingDefinitions.Master.Key, settings.Master); section.Set(AudioSettingDefinitions.Ambience.Key, settings.Ambience);
        section.Set(AudioSettingDefinitions.Foley.Key, settings.Foley); section.Set(AudioSettingDefinitions.Ui.Key, settings.Ui); section.Set(AudioSettingDefinitions.Music.Key, settings.Music);
        section.Set(AudioSettingDefinitions.UiSounds.Key, settings.UiSounds); section.Set(AudioSettingDefinitions.HoverSounds.Key, settings.HoverSounds);
        section.Set(AudioSettingDefinitions.QuietMode.Key, settings.QuietMode); section.Set(AudioSettingDefinitions.Mono.Key, settings.Mono);
        section.Set(AudioSettingDefinitions.Sudden.Key, (int)settings.Sudden); section.Set(AudioSettingDefinitions.Background.Key, (int)settings.Background);
        section.Set(AudioSettingDefinitions.OutputDevice.Key, settings.OutputDevice ?? "");
        if (store.TrySave()) { _pending = null; return; }
        // Retries advance on real frame delta; never block the main thread for 50 ms.
        _attempts++;
        _remaining = _attempts <= 3 ? .05f : float.PositiveInfinity;
        if (_attempts > 3) KernelServices.Get<IDiagnosticLog>().Warning("Audio", "Audio settings remain in memory; disk write failed.");
    }
}
