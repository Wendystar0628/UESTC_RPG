using Campus.Contracts.Maps;
using Campus.Contracts;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud;

public partial class HudRoot
{
    private string ResetName()
    {
        return _dependencies.Maps.Context?.Descriptor.World.DefaultName.Zh ?? "";
    }

    // ---------------- 布局：四角贴角，中间留空（UI-VISUAL） ----------------
    private void Layout()
    {
        ApplyScale();
        foreach (var corner in new[] { _topLeft, _topRight, _bottomLeft, _bottomRight })
            corner.CustomMaximumSize = new Vector2(_safeFrame.Size.X / 2, _safeFrame.Size.Y);
    }

    // ---------------- 四角的常驻件：进入漫游时建，回到开始页时释放（开始页上它们本来就不显示，不该占实例参数槽） ----------------
    private sealed record Resident(SpeedBar Speed, WeatherStrip Weather, TimeCard Time, TimeStrip Strip, Control TopRight)
    {
        public Control[] Controls { get; } = [Speed, Weather, Time, Strip, TopRight];
    }

    private void BuildResident()
    {
        if (_resident is not null) return;
        var speed = SpeedBar.Create(_kit, _bottomLeft, _model);
        speed.ModePicked += mode => _ = SetMode(mode);
        var weather = WeatherStrip.Create(_kit, _bottomRight, _model, _control);
        weather.Toast += text => _toast.Show(text, "weather_auto");
        var time = TimeCard.Create(_kit, _bottomRight, _model, _control, _dependencies.PaletteProbe, ResetName);
        time.Toast += text => _toast.Show(text);
        var strip = TimeStrip.Create(_kit, _bottomRight, _model);
        strip.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) ToggleTime(true); };
        _resident = new Resident(speed, weather, time, strip, BuildTopRight());
        ResetIdleFade();
        Layout();
    }

    private void ReleaseResident()
    {
        if (_resident is not { } resident) return;
        foreach (var child in resident.Controls)
        {
            child.GetParent().RemoveChild(child);
            child.QueueFree();
        }
        _resident = null;
        _weatherHeld = false;
    }

    // ---------------- 性能信息：在右上角单独显隐，不跟随普通 HUD 淡出 ----------------
    private void SyncPerf()
    {
        var want = _settings.ShowPerf && Playing && !_clean && !_hidden;
        if (want == (_perf is not null)) return;
        if (!want) { FreePerf(); return; }
        _perf = PerfReadout.Create(_kit, _topRight);
    }

    private void FreePerf()
    {
        if (_perf is not { } perf) return;
        _perf = null;
        perf.GetParent().RemoveChild(perf);   // 立即移出场景树，视口渲染耗时测量随之关闭
        perf.QueueFree();
    }

    private Control BuildTopRight()
    {
        var topRight = _kit.Layout.Row(_topRight);
        topRight.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        (string Icon, string Tip, Action Act)[] buttons =
        [
            ("save", "hud.button.saves", () => OpenSaves(fromTitle: false)),
            ("help", "hud.button.help", () => ToggleSettings(SettingsPage.Keys)),
            ("settings", "hud.button.settings", () => ToggleSettings(SettingsPage.Graphics)),
        ];
        for (var i = 0; i < buttons.Length; i++)
        {
            var (icon, tip, act) = buttons[i];
            var b = _kit.Button(topRight, ButtonStyle.Icon, "", icon: icon, pin: true, pressed: act);
            b.TooltipText = UiKit.T(tip);
            b.BaseFill = "surface.panel";
            b.Refresh();
        }
        return topRight;
    }

    private void FadeResidents(float alpha, bool interactive)
    {
        if (_resident is not { } resident) return;
        foreach (var child in resident.Controls)
        {
            child.Modulate = new Color(1, 1, 1, alpha);
            UiLayout.SetInteractive(child, interactive && child.Visible);
        }
    }
}
