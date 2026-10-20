using Campus.Contracts;

namespace Campus.Hud;

/// <summary>
/// 界面的只读入口（UI-NAV）：每次访问直接转发权威源，绘制刷新由界面根负责。
/// 界面只读 IWorldState、ICameraRig 的速度与模式，写入一律经 IWorldControl、IViewControl。
/// </summary>
[KernelService(typeof(HudModel))]
public sealed class HudModel(IWorldState world, ICameraRig rig, IRuleset ruleset)
{
    public IWorldState World => world;
    public IRuleset Ruleset => ruleset;
    public double Clock => world.Clock;
    public Season Season => world.SeasonTarget;
    public int FlowRate => world.BaseFlow.Rate;
    public WeatherSel Weather => world.Weather;
    public ViewMode Mode => rig.Mode;
    public float Speed => rig.Speed;
    public WorldTransition Transition => world.Transition;

    /// <summary>世界控件是否开放（规则集的 WorldControls，ENG-ROADMAP 第 1 条）。</summary>
    public bool Allows(WorldControlMask control) => (ruleset.Current.WorldControls & control) == control;
}
