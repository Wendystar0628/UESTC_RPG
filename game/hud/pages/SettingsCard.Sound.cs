using Campus.Contracts.Audio;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

// 声音页（SYS-AUDIO：只调 IAudioService.ApplySettings）
public partial class SettingsCard
{
    private float BuildSound(Control page, float width)
    {
        var audio = Svc<IAudioService>();
        float y = 8;
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("sound.group.volume"));
        y += 24;
        (string Key, Func<AudioSettings, float> Get, Func<AudioSettings, float, AudioSettings> Set)[] volumes =
        [
            ("sound.master", s => s.Master, (s, v) => s with { Master = v }),
            ("sound.ambience", s => s.Ambience, (s, v) => s with { Ambience = v }),
            ("sound.foley", s => s.Foley, (s, v) => s with { Foley = v }),
            ("sound.ui", s => s.Ui, (s, v) => s with { Ui = v }),
            ("sound.music", s => s.Music, (s, v) => s with { Music = v }),
        ];
        foreach (var (key, get, set) in volumes)
        {
            Row(page, y, UiKit.T(key));
            var fader = Fader.Create(_kit, page, new Vector2(ControlX + 6, y + 2), 200, 0, 1, 0.05f, get(audio.Settings), false, v => $"{v * 100:0}%");
            fader.Changed += v => audio.ApplySettings(set(audio.Settings, v));
            y += 40;
        }
        y += 12;
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("sound.group.calm"));
        y += 24;
        (string Key, bool Value, Func<AudioSettings, bool, AudioSettings> Set)[] toggles =
        [
            ("sound.ui_sounds", audio.Settings.UiSounds, (s, v) => s with { UiSounds = v }),
            ("sound.hover", audio.Settings.HoverSounds, (s, v) => s with { HoverSounds = v }),
            ("sound.quiet", audio.Settings.QuietMode, (s, v) => s with { QuietMode = v }),
            ("sound.mono", audio.Settings.Mono, (s, v) => s with { Mono = v }),
        ];
        foreach (var (key, value, set) in toggles)
        {
            Row(page, y, UiKit.T(key));
            var sw = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), value, false);
            sw.Toggled += v => audio.ApplySettings(set(audio.Settings, v));
            y += 40;
        }
        Row(page, y, UiKit.T("sound.sudden"));
        var sudden = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [72, 72, 72],
            [UiKit.T("sound.sudden.normal"), UiKit.T("sound.sudden.softer"), UiKit.T("ui.common.off")], false, selected: (int)audio.Settings.Sudden);
        sudden.Changed += i => audio.ApplySettings(audio.Settings with { Sudden = (SuddenSoundMode)i });
        y += 40;
        Row(page, y, UiKit.T("sound.background"));
        var background = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [72, 72, 72],
            [UiKit.T("sound.background.keep"), UiKit.T("sound.background.lower"), UiKit.T("sound.background.mute")], false, selected: (int)audio.Settings.Background);
        background.Changed += i => audio.ApplySettings(audio.Settings with { Background = (BackgroundBehavior)i });
        return y + 56;
    }
}
