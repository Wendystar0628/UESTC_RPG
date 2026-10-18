using Campus.Contracts;
using Godot;

namespace Campus.Hud.Motion;

public enum MotionLevel : byte { Full, Reduced, Instant }

/// <summary>
/// "减少动态效果"的唯一来源（UI-VISUAL）：实现契约 <see cref="IMotionSettings"/>，3D 侧只读它；
/// 界面组件的时长一律经这里取，组件里不写判断。--motion=full|reduced|instant 优先于设置；Instant 给门禁和截图用。
/// </summary>
[KernelService(typeof(IMotionSettings), Owner = "WP-35")]
[KernelService(typeof(MotionPolicy), Owner = "WP-35")]
public sealed class MotionPolicy : IMotionSettings
{
    private readonly HudSettings _settings;
    private MotionLevel? _forced;
    private MotionLevel _level;

    public MotionPolicy(HudSettings settings)
    {
        _settings = settings;
        _level = Evaluate();
        settings.Changed += Refresh;
    }

    public MotionLevel Level => _level;
    public bool ReduceMotion => _level != MotionLevel.Full;
    public event Action? Changed;

    /// <summary>系统设置可能在运行中变化：HudRoot 失焦回来时调用一次。</summary>
    public void Refresh()
    {
        var next = Evaluate();
        if (next == _level) return;
        _level = next;
        Changed?.Invoke();
    }

    /// <summary>入场类时长：完整时照原值；减少动态效果时只留透明度，最长 0.2 s；Instant 为 0。</summary>
    public float Fade(float seconds) => _level switch
    {
        MotionLevel.Full => seconds,
        MotionLevel.Reduced => Math.Min(seconds, 0.2f),
        MotionLevel.Instant => 0f,
        _ => throw new InvalidOperationException("Unknown motion level."),
    };

    /// <summary>位移、扫描、量化等空间运动：只在完整档出现。</summary>
    public bool Spatial => _level switch
    {
        MotionLevel.Full => true,
        MotionLevel.Reduced or MotionLevel.Instant => false,
        _ => throw new InvalidOperationException("Unknown motion level."),
    };

    private MotionLevel Evaluate()
    {
        if (_forced is { } forced) return forced;
        return _settings.ReduceMotion switch
        {
            ReduceMotionMode.On => MotionLevel.Reduced,
            ReduceMotionMode.Off => MotionLevel.Full,
            ReduceMotionMode.System => DisplayServer.AccessibilityShouldReduceAnimation() > 0 ? MotionLevel.Reduced : MotionLevel.Full,
            _ => throw new InvalidOperationException("Unknown reduce-motion mode."),
        };
    }

    /// <summary>启动参数到位（BeforeMap）后由 HudRoot 调用一次：--motion 优先，其次 --ui=1 的截图默认 instant（UI-VISUAL）。</summary>
    public void Apply(HudArguments args)
    {
        _forced = args.Motion ?? (args.Ui == "1" ? MotionLevel.Instant : null);
        Refresh();
    }
}
