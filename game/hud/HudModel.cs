using Campus.Contracts;

namespace Campus.Hud;

/// <summary>
/// 界面的数据层（UI-NAV）：以 10 Hz 采样世界状态，值变了才发 <see cref="Changed"/>；采样间隔按帧间隔累加，不用 Timer。
/// 界面只读 IWorldState、ICameraRig 的速度与模式，写入一律经 IWorldControl、IViewControl。
/// </summary>
[KernelService(typeof(HudModel), Owner = "WP-35")]
public sealed class HudModel(IWorldState world, ICameraRig rig, IRuleset ruleset)
{
    private const double Interval = 0.1;
    private double _accum = Interval;
    private long _worldRev = -1;
    private float _speed = -1;
    private ViewMode _mode;
    private WorldTransition _transition;

    public IWorldState World => world;
    public IRuleset Ruleset => ruleset;
    public double Clock { get; private set; }
    public Season Season { get; private set; }
    public int FlowRate => world.BaseFlow.Rate;
    public WeatherSel Weather { get; private set; }
    public ViewMode Mode => _mode;
    public float Speed => _speed;
    public WorldTransition Transition => _transition;
    public long Rev { get; private set; }
    public event Action? Changed;

    /// <summary>世界控件是否开放（规则集的 WorldControls，ENG-ROADMAP 第 1 条）。</summary>
    public bool Allows(WorldControlMask control) => (ruleset.Current.WorldControls & control) == control;

    public void Tick(double delta)
    {
        _accum += delta;
        if (_accum < Interval) return;
        _accum = 0;
        var changed = world.Rev != _worldRev || Math.Abs(rig.Speed - _speed) > 0.05f || rig.Mode != _mode || world.Transition != _transition
                      || Math.Abs(world.Clock - Clock) >= 1 / 60.0;
        if (!changed) return;
        _worldRev = world.Rev;
        _speed = rig.Speed;
        _mode = rig.Mode;
        _transition = world.Transition;
        Clock = world.Clock;
        Season = world.SeasonTarget;
        Weather = world.Weather;
        Rev++;
        Changed?.Invoke();
    }

    /// <summary>立刻重采样（操作之后马上刷新读数，不等下一个 0.1 s）。</summary>
    public void Poke() => _accum = Interval;
}
