using Campus.Contracts;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

// 设置卡的共用部分：服务取用、输入占用、行标签、键名（各页的文件见 SettingsCard.*.cs）
public partial class SettingsCard
{
    private static T Svc<T>() where T : class => Campus.Kernel.Registry.Kernel.Get<T>();

    private static string Key(SettingsPage page) => page switch
    {
        SettingsPage.Keys => "keys",
        SettingsPage.General => "general",
        SettingsPage.Graphics => "graphics",
        SettingsPage.Sound => "sound",
        SettingsPage.Rebind => "rebind",
        SettingsPage.Licenses => "licenses",
        _ => throw new ArgumentOutOfRangeException(nameof(page)),
    };

    private static IDisposable Lock(InputLockReason reason) => Svc<ICameraRig>().LockInput(reason);

    // ---------------- 行 ----------------
    private float Row(Control page, float y, string name, bool hint = false)
    {
        _kit.Text(page, name, "body", "text.1", new Vector2(8, y + 8));
        return y;
    }

    private const float ControlX = 216;

    /// <summary>键名按当前键盘布局显示（UI-NAV）：物理键码经 KeyboardGetLabelFromPhysical 换成布局上的字。</summary>
    public string KeyLabel(string action)
    {
        var physical = _router.PhysicalKey(action);
        return physical == 0 ? "—" : KeyName((Key)physical);
    }

    private static string KeyName(Key code)
    {
        var label = DisplayServer.KeyboardGetLabelFromPhysical(code);
        var text = OS.GetKeycodeString(label != Godot.Key.None ? label : code);
        return text switch { "Escape" => "Esc", "Period" => ".", "Space" => "Space", _ => text };
    }
}
