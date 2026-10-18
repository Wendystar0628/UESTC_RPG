using Campus.Contracts.Audio;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public partial class SettingsCard
{
    private void BuildSound(Control page)
    {
        var audio = Svc<IAudioService>();
        var volume = _kit.Layout.SettingGroup(page, UiKit.T("sound.group.volume"));
        Slider(volume, AudioSettingDefinitions.Master, audio.Settings.Master, value => audio.ApplySettings(audio.Settings with { Master = value }), Percent);
        Slider(volume, AudioSettingDefinitions.Ambience, audio.Settings.Ambience, value => audio.ApplySettings(audio.Settings with { Ambience = value }), Percent);
        Slider(volume, AudioSettingDefinitions.Foley, audio.Settings.Foley, value => audio.ApplySettings(audio.Settings with { Foley = value }), Percent);
        Slider(volume, AudioSettingDefinitions.Ui, audio.Settings.Ui, value => audio.ApplySettings(audio.Settings with { Ui = value }), Percent);
        Slider(volume, AudioSettingDefinitions.Music, audio.Settings.Music, value => audio.ApplySettings(audio.Settings with { Music = value }), Percent);
        var calm = _kit.Layout.SettingGroup(page, UiKit.T("sound.group.calm"));
        Toggle(calm, AudioSettingDefinitions.UiSounds, audio.Settings.UiSounds, value => audio.ApplySettings(audio.Settings with { UiSounds = value }));
        Toggle(calm, AudioSettingDefinitions.HoverSounds, audio.Settings.HoverSounds, value => audio.ApplySettings(audio.Settings with { HoverSounds = value }));
        Toggle(calm, AudioSettingDefinitions.QuietMode, audio.Settings.QuietMode, value => audio.ApplySettings(audio.Settings with { QuietMode = value }));
        Toggle(calm, AudioSettingDefinitions.Mono, audio.Settings.Mono, value => audio.ApplySettings(audio.Settings with { Mono = value }));
        Choice(calm, AudioSettingDefinitions.Sudden, (int)audio.Settings.Sudden, value => audio.ApplySettings(audio.Settings with { Sudden = (SuddenSoundMode)value }));
        Choice(calm, AudioSettingDefinitions.Background, (int)audio.Settings.Background, value => audio.ApplySettings(audio.Settings with { Background = (BackgroundBehavior)value }));
    }

    private static string Percent(float value) => $"{value * 100:0}%";
}
