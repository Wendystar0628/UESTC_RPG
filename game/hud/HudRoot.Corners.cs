using Campus.Contracts.Maps;
using Campus.Contracts;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud;

public partial class HudRoot
{
    private string ResetName()
    {
        try { return Services.Get<IMapService>().Context?.Descriptor.World.DefaultName.Zh ?? ""; }
        catch (InvalidOperationException) { return ""; }
    }

    // ---------------- 布局：四角贴角，中间留空（UI-VISUAL） ----------------
    private void Layout()
    {
        var view = _root.Size;
        if (view.X <= 0) return;
        var edge = _kit.Tokens.Space("screenEdge");
        _minimapSlot.Position = new Vector2(edge, edge);
        if (_resident is not { } r) return;
        r.Speed.Position = new Vector2(edge, view.Y - 36 - r.Speed.Size.Y);
        r.Time.Position = new Vector2(view.X - edge - r.Time.Size.X, view.Y - 36 - r.Time.Size.Y);
        r.Strip.Position = new Vector2(view.X - edge - r.Strip.Size.X, view.Y - 36 - r.Strip.Size.Y);
        r.Weather.Position = new Vector2(r.Time.Position.X - 12 - r.Weather.Size.X, view.Y - 36 - r.Weather.Size.Y);
        r.TopRight.Position = new Vector2(view.X - edge - r.TopRight.Size.X, edge);
    }

    // ---------------- 四角的常驻件：进入漫游时建，回到开始页时释放（开始页上它们本来就不显示，不该占实例参数槽） ----------------
    private sealed record Resident(SpeedBar Speed, WeatherStrip Weather, TimeCard Time, TimeStrip Strip, Control TopRight);

    private void BuildResident()
    {
        if (_resident is not null) return;
        var speed = SpeedBar.Create(_kit, _corners, _model);
        speed.ModePicked += mode => _ = SetMode(mode);
        var weather = WeatherStrip.Create(_kit, _corners, _model, _control);
        weather.Toast += text => _toast.Show(text, "weather_auto");
        var time = TimeCard.Create(_kit, _corners, _model, _control, Services.Get<IPaletteProbe>(), ResetName);
        time.Toast += text => _toast.Show(text);
        var strip = TimeStrip.Create(_kit, _corners, _model);
        strip.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) ToggleTime(true); };
        _resident = new Resident(speed, weather, time, strip, BuildTopRight());
        Layout();
    }

    private void ReleaseResident()
    {
        if (_resident is null) return;
        foreach (var child in _corners.GetChildren())
        {
            _corners.RemoveChild(child);
            child.QueueFree();
        }
        _resident = null;
        _weatherHeld = false;
    }

    private Control BuildTopRight()
    {
        var size = _kit.Tokens.Size("hudIconButton");
        var topRight = new Control { Size = new Vector2(size * 3 + 16, size), MouseFilter = Control.MouseFilterEnum.Ignore };
        _corners.AddChild(topRight);
        (string Icon, string Tip, Action Act)[] buttons =
        [
            ("save", "hud.button.saves", () => OpenSaves(fromTitle: false)),
            ("help", "hud.button.help", () => ToggleSettings(SettingsPage.Keys)),
            ("settings", "hud.button.settings", () => ToggleSettings(SettingsPage.Graphics)),
        ];
        for (var i = 0; i < buttons.Length; i++)
        {
            var (icon, tip, act) = buttons[i];
            var b = _kit.Button(topRight, ButtonStyle.Icon, "", new Rect2(i * (size + 8), 0, size, size), padX: 8, icon: icon, iconMax: 24, pin: true, padY: 8,
                pressed: act);
            b.TooltipText = UiKit.T(tip);
            b.BaseFill = "surface.panel";
            b.Refresh();
        }
        return topRight;
    }
}
