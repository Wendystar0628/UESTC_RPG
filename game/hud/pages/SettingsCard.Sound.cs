using Campus.Contracts.Audio;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public partial class SettingsCard
{
    private System.Collections.IEnumerable BuildSound(Control page)
    {
        var audio = Svc<IAudioService>();
        var volume = _kit.Layout.SettingGroup(page, UiKit.T("sound.group.volume"));
        Slider(volume, AudioSettingDefinitions.Master, audio.Settings.Master, value => audio.ApplySettings(audio.Settings with { Master = value }), Percent);
        yield return null;
        Slider(volume, AudioSettingDefinitions.Ambience, audio.Settings.Ambience, value => audio.ApplySettings(audio.Settings with { Ambience = value }), Percent);
        yield return null;
        Slider(volume, AudioSettingDefinitions.Foley, audio.Settings.Foley, value => audio.ApplySettings(audio.Settings with { Foley = value }), Percent);
        yield return null;
        Slider(volume, AudioSettingDefinitions.Ui, audio.Settings.Ui, value => audio.ApplySettings(audio.Settings with { Ui = value }), Percent);
        yield return null;
        Slider(volume, AudioSettingDefinitions.Music, audio.Settings.Music, value => audio.ApplySettings(audio.Settings with { Music = value }), Percent);
        yield return null;
        var calm = _kit.Layout.SettingGroup(page, UiKit.T("sound.group.calm"));
        Toggle(calm, AudioSettingDefinitions.UiSounds, audio.Settings.UiSounds, value => audio.ApplySettings(audio.Settings with { UiSounds = value }));
        yield return null;
        Toggle(calm, AudioSettingDefinitions.HoverSounds, audio.Settings.HoverSounds, value => audio.ApplySettings(audio.Settings with { HoverSounds = value }));
        yield return null;
        Toggle(calm, AudioSettingDefinitions.QuietMode, audio.Settings.QuietMode, value => audio.ApplySettings(audio.Settings with { QuietMode = value }));
        yield return null;
        Toggle(calm, AudioSettingDefinitions.Mono, audio.Settings.Mono, value => audio.ApplySettings(audio.Settings with { Mono = value }));
        yield return null;
        Choice(calm, AudioSettingDefinitions.Sudden, (int)audio.Settings.Sudden, value => audio.ApplySettings(audio.Settings with { Sudden = (SuddenSoundMode)value }));
        yield return null;
        Choice(calm, AudioSettingDefinitions.Background, (int)audio.Settings.Background, value => audio.ApplySettings(audio.Settings with { Background = (BackgroundBehavior)value }));
        yield return null;
    }

    private static string Percent(float value) => $"{value * 100:0}%";
}
