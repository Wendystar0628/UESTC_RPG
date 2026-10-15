using Campus.Contracts.Cinematic;
using Campus.Systems.Nightlight.Lamps;
using Campus.Systems.Nightlight.Schedule;

namespace Campus.Systems.Nightlight.Cue;

/// <summary>
/// 过场"初灯沿路推进"的灯光提示（SYS-TITLE §3.3）：只改先后、不改终值。
/// 覆盖期间，lamp.cue 那一组灯（路线带宽以内的初灯组，κ = 沿路线的弧长比例）按前沿依次亮起，
/// 判开灯用目标钟点的太阳高度（17:30 本该亮着的那些灯才亮，其余照旧不亮）；撤销后各值与不覆盖时完全相同。
/// 只在 AppState.Entering 生效，由调用方（NightlightNode）把关；这里只按时间轴求前沿。
/// </summary>
public sealed class LampCue
{
    private readonly LampCueDef _def;
    private readonly float _length;

    public LampCue(LampCueDef def, float targetSunDeg)
    {
        ArgumentNullException.ThrowIfNull(def);
        if (def.Route.Count < 2) throw new ArgumentException("A lamp cue needs a route of at least two points.", nameof(def));
        if (def.Front.Count == 0) throw new ArgumentException("A lamp cue needs at least one front key.", nameof(def));
        if (!float.IsFinite(targetSunDeg)) throw new ArgumentOutOfRangeException(nameof(targetSunDeg));
        _def = def;
        _length = LampSetBuilder.RouteLength(def.Route);
        if (_length <= 0f) throw new ArgumentException("A lamp cue route must have a positive length.", nameof(def));
        TargetSunDeg = targetSunDeg;
    }

    public string Ref => _def.Ref;
    public float TargetSunDeg { get; }
    public float RouteLength => _length;

    /// <summary>时间轴秒数处的覆盖值：前沿弧长按 Front 的关键帧线性插值，换成路线全长的比例；软边 = Band / 全长。</summary>
    public CueOverride At(double timelineSeconds)
    {
        var keys = _def.Front;
        float arc;
        var t = (float)timelineSeconds;
        if (t <= keys[0].X) arc = keys[0].Y;
        else if (t >= keys[^1].X) arc = keys[^1].Y;
        else
        {
            arc = keys[^1].Y;
            for (var i = 1; i < keys.Count; i++)
            {
                if (t > keys[i].X) continue;
                var a = keys[i - 1];
                var b = keys[i];
                var span = b.X - a.X;
                arc = span <= 0f ? b.Y : a.Y + (b.Y - a.Y) * ((t - a.X) / span);
                break;
            }
        }
        var band = Math.Clamp(_def.Band / _length, 0.005f, 0.5f);
        return new CueOverride(Math.Clamp(arc / _length, 0f, 1f), band, TargetSunDeg);
    }

    /// <summary>前沿已经扫过全程：这之后撤销覆盖不会有任何一盏灯变化（终值相同）。</summary>
    public bool Finished(double timelineSeconds) => At(timelineSeconds).Front >= 1f;
}
