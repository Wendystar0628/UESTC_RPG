using Campus.Contracts;
using Campus.Hud.Motion;
using Godot;

namespace Campus.Hud.Style;

/// <summary>
/// 晶白、深屏两套主题的锁存与交班扫描（UI-VISUAL）：按真实太阳高度带回差判定（下穿 −0.5° 切深屏，
/// 上穿 +0.5° 切晶白），快进进行中不判定、结束后判一次；切换时全局参数 ui_night 在 0.6 s 真实时间内线性走完
/// （它是扫描前沿的位置，不是颜色插值量），减少动态效果时一帧走完。只在切换期间写 ui_night。
/// </summary>
public partial class UiThemeClock : Node
{
    private readonly IWorldState _world;
    private readonly HudSettings _settings;
    private readonly MotionPolicy _motion;
    private readonly float _toDark, _toLight, _sweep;
    private UiThemeMode? _forced;
    private bool _dark;
    private float _front;
    private float _target;
    private long _instance;

    public UiThemeClock(IWorldState world, HudSettings settings, MotionPolicy motion, UiTokens tokens)
    {
        _world = world;
        _settings = settings;
        _motion = motion;
        _toDark = tokens.F("themeSwitch", "toDarkBelowDeg");
        _toLight = tokens.F("themeSwitch", "toLightAboveDeg");
        _sweep = tokens.F("themeSwitch", "sweepMs") / 1000f;
        ProcessPriority = FramePriority.Hud;
        Name = "UiThemeClock";
    }

    /// <summary>当前（或正在切往的）主题是深屏。</summary>
    public bool Dark => _dark;
    public float Front => _front;
    /// <summary>主题切换开始时发出（参数为新主题是否深屏），界面据此决定要不要出交班音。</summary>
    public event Action<bool>? Switched;

    public override void _EnterTree()
    {
        _instance = _world.Instance;
        _dark = Decide(initial: true);
        _front = _target = _dark ? 1 : 0;
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.UiNight, _front);
        _settings.Changed += OnSettingsChanged;
    }

    public override void _ExitTree() => _settings.Changed -= OnSettingsChanged;

    private void OnSettingsChanged() => Judge(force: true);

    public override void _Process(double delta)
    {
        if (_world.Instance != _instance)
        {
            // 换了世界实例（读档、新游戏）：直接落到新值，不扫
            _instance = _world.Instance;
            _dark = Decide(initial: true);
            Snap();
            return;
        }
        if ((_world.Transition & WorldTransition.Time) == 0) Judge(force: false);
        if (_front == _target) return;
        var step = _sweep <= 0 || !_motion.Spatial ? 1f : (float)delta / _sweep;
        _front = _target > _front ? Math.Min(_target, _front + step) : Math.Max(_target, _front - step);
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.UiNight, _front);
    }

    private void Judge(bool force)
    {
        var next = Decide(initial: force);
        if (next == _dark) return;
        _dark = next;
        _target = _dark ? 1 : 0;
        Switched?.Invoke(_dark);
        if (!_motion.Spatial) Snap();
    }

    private void Snap()
    {
        _front = _target = _dark ? 1 : 0;
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.UiNight, _front);
    }

    private bool Decide(bool initial)
    {
        var mode = _forced ?? _settings.Theme;
        switch (mode)
        {
            case UiThemeMode.Light: return false;
            case UiThemeMode.Dark: return true;
            case UiThemeMode.Auto:
                var h = _world.Sun.AltDeg;
                if (initial) return h < 0;
                return _dark ? !(h > _toLight) : h < _toDark;
            default: throw new InvalidOperationException("Unknown UI theme mode.");
        }
    }

    /// <summary>启动参数到位（BeforeMap）后由 HudRoot 调用一次：--ui-theme 压过设置，直接落到该主题，不扫。</summary>
    public void Apply(HudArguments args)
    {
        _forced = args.UiTheme;
        if (_forced is null || !IsInsideTree()) return;
        _dark = Decide(initial: true);
        Snap();
    }
}
