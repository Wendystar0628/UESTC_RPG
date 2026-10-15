using System.Numerics;
using Campus.Contracts;

namespace Campus.Systems.Nightlight.Schedule;

/// <summary>一帧的输入：都取自 IWorldState、IColorScriptFrame 与真实帧间隔，不读回任何全局参数。</summary>
public readonly record struct ScheduleInput(
    double Clock, float SunAltDeg, float Overcast, float Precip, float PoolGain, double RealDt, long Instance);

/// <summary>过场"初灯追镜头"的覆盖：前沿位置与软边都是相对路线全长的比例，目标高度是覆盖期间判开灯用的 h_eff。</summary>
public readonly record struct CueOverride(float Front, float Band, float TargetSunDeg);

/// <summary>
/// 32 个灯光通道的每帧求值（ENG-PERF §10.3）。只算数：结果交给 WP-03 的通道写入接口。
/// 目标值只由钟点、h_eff 与季节决定（可复现）；过渡宽度 Δ 随通道的变化速率自适应，
/// 世界实例变了（IWorldState.Instance）就直接落到新值，不带历史。
/// </summary>
public sealed class ScheduleEvaluator
{
    private readonly LightingScheduleFile _file;
    private readonly ChannelSpec[] _bySlot = new ChannelSpec[LightingScheduleFile.ChannelCount];
    private readonly Dictionary<ChannelId, int> _slotById = [];
    private readonly ThresholdGroup?[] _groupBySlot = new ThresholdGroup?[LightingScheduleFile.ChannelCount];
    private readonly float[] _prev = new float[LightingScheduleFile.ChannelCount];
    private readonly float[] _delta = new float[LightingScheduleFile.ChannelCount];
    private long _instance = long.MinValue;

    public ScheduleEvaluator(LightingScheduleFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        Validate(file);
        _file = file;
        foreach (var spec in file.Channels)
        {
            _bySlot[spec.Slot] = spec;
            _slotById.Add(spec.Id, spec.Slot);
            if (spec.Kind == ChannelClass.Lamp) _groupBySlot[spec.Slot] = file.Groups[spec.Group!];
        }
        CueSlot = _slotById[file.Cue.Slot];
        CueSysSlot = _slotById[file.Cue.SysSlot];
        PoolGainSlot = file.Channels.Single(c => c.Kind == ChannelClass.PoolGain).Slot;
    }

    public LightingScheduleFile File => _file;
    public int CueSlot { get; }
    public int CueSysSlot { get; }
    public int PoolGainSlot { get; }
    public CueOverride? Cue { get; set; }

    public int SlotOf(ChannelId id) => _slotById.TryGetValue(id, out var slot) ? slot :
        throw new KeyNotFoundException($"Lighting channel '{id.Value}' is not in lighting-schedule.json.");
    public bool TrySlotOf(ChannelId id, out int slot) => _slotById.TryGetValue(id, out slot);
    public ChannelSpec Spec(int slot) => _bySlot[slot];
    /// <summary>灯具类槽位的开灯阈值分组；不是灯具类时为 null。</summary>
    public ThresholdGroup? GroupOf(int slot) => _groupBySlot[slot];

    public float EffectiveSun(in ScheduleInput input) =>
        ScheduleMath.EffectiveSun(input.SunAltDeg, input.Overcast, input.Precip, _file.Heff);

    /// <summary>不含过渡宽度的目标值：窗灯类为亮窗比例，灯具类为保留比例（0–1），系统槽位按各自约定。</summary>
    public float Target(int slot, in ScheduleInput input)
    {
        var spec = _bySlot[slot];
        switch (spec.Kind)
        {
            case ChannelClass.Window:
                return WindowTarget(spec, input);
            case ChannelClass.Lamp:
                if (slot == CueSlot && Cue is { } cue) return Math.Clamp(cue.Front, 0f, 1f);
                return Math.Clamp(ScheduleMath.Curve(spec.Curve, input.Clock), 0f, 1f);
            case ChannelClass.Cue:
                return Cue is null ? 0f : 1f;
            case ChannelClass.PoolGain:
                return float.IsFinite(input.PoolGain) && input.PoolGain > 0f ? input.PoolGain : 1f;
            case ChannelClass.Reserved:
                return 0f;
            default:
                throw new InvalidOperationException($"Unknown channel class {spec.Kind}.");
        }
    }

    /// <summary>求本帧 32 个通道的 (L, Δ)。</summary>
    public void Evaluate(in ScheduleInput input, Span<Vector2> output)
    {
        if (output.Length != LightingScheduleFile.ChannelCount)
            throw new ArgumentException($"Expected {LightingScheduleFile.ChannelCount} channel slots.", nameof(output));
        var reset = input.Instance != _instance;
        _instance = input.Instance;
        var deltaSpec = _file.Delta;
        var dt = Math.Max(input.RealDt, 0.0);
        var decay = dt > 0 ? (float)Math.Exp(-dt / Math.Max(deltaSpec.Seconds, 1e-3)) : 1f;
        for (var slot = 0; slot < LightingScheduleFile.ChannelCount; slot++)
        {
            var spec = _bySlot[slot];
            var target = Target(slot, input);
            float delta;
            if (reset || dt <= 0)
            {
                delta = reset ? deltaSpec.Min : _delta[slot];
            }
            else
            {
                var rate = Math.Abs(target - _prev[slot]) / (float)Math.Max(dt, 1e-4);
                var wanted = Math.Clamp(rate * deltaSpec.Seconds, deltaSpec.Min, deltaSpec.Max);
                delta = Math.Clamp(Math.Max(wanted, _delta[slot] * decay), deltaSpec.Min, deltaSpec.Max);
            }
            _prev[slot] = target;
            _delta[slot] = delta;
            output[slot] = spec.Kind switch
            {
                ChannelClass.Window => new Vector2(target, delta),
                ChannelClass.Lamp when slot == CueSlot && Cue is { } cue =>
                    new Vector2(Math.Clamp(cue.Front, 0f, 1f) * (1f + cue.Band), Math.Max(cue.Band, deltaSpec.Min)),
                ChannelClass.Lamp => new Vector2(target * (1f + delta), delta),
                ChannelClass.Cue => Cue is { } c ? new Vector2(1f, c.TargetSunDeg) : Vector2.Zero,
                ChannelClass.PoolGain => new Vector2(target, 0f),
                ChannelClass.Reserved => new Vector2(0f, deltaSpec.Min),
                _ => throw new InvalidOperationException($"Unknown channel class {spec.Kind}."),
            };
        }
    }

    private float WindowTarget(ChannelSpec spec, in ScheduleInput input)
    {
        var heff = EffectiveSun(input);
        var dark = spec.Dark is { } d ? ScheduleMath.SmoothStep(d.X, d.Y, heff) : 1f;
        var occ = Math.Clamp(ScheduleMath.Curve(spec.Curve, input.Clock), 0f, 1f);
        var value = Math.Clamp(dark * occ, 0f, spec.Cap);
        if (spec.DayFloor) value = Math.Max(value, Math.Min(spec.Cap, DayFloor(occ, input)));
        return value;
    }

    private float DayFloor(float occupancy, in ScheduleInput input)
    {
        var f = _file.DayFloor;
        var o = f.OvercastRamp ?? new Vector2(0.45f, 0.9f);
        var r = f.RainRamp ?? new Vector2(0.08f, 0.4f);
        var u = f.OccupancyRamp ?? new Vector2(0.1f, 0.45f);
        var floor = Math.Max(f.Overcast * ScheduleMath.SmoothStep(o.X, o.Y, input.Overcast),
            f.Rain * ScheduleMath.SmoothStep(r.X, r.Y, input.Precip));
        return floor * ScheduleMath.SmoothStep(u.X, u.Y, occupancy);
    }

    private static void Validate(LightingScheduleFile file)
    {
        if (file.Format != LightingScheduleFile.CurrentFormat)
            throw new InvalidDataException($"lighting-schedule.json format {file.Format}; expected {LightingScheduleFile.CurrentFormat}.");
        if (file.Channels.Count != LightingScheduleFile.ChannelCount)
            throw new InvalidDataException($"lighting-schedule.json must register all {LightingScheduleFile.ChannelCount} slots.");
        var slots = new bool[LightingScheduleFile.ChannelCount];
        var ids = new HashSet<ChannelId>();
        foreach (var spec in file.Channels)
        {
            if (spec.Slot is < 0 or >= LightingScheduleFile.ChannelCount || slots[spec.Slot])
                throw new InvalidDataException($"Channel slot {spec.Slot} is out of range or duplicated.");
            slots[spec.Slot] = true;
            if (string.IsNullOrWhiteSpace(spec.Id.Value) || !ids.Add(spec.Id))
                throw new InvalidDataException($"Channel id '{spec.Id.Value}' is empty or duplicated.");
            for (var i = 0; i < spec.Curve.Count; i++)
            {
                var key = spec.Curve[i];
                if (!float.IsFinite(key.X) || !float.IsFinite(key.Y) || key.X < 0f || key.X >= 24f || (i > 0 && key.X <= spec.Curve[i - 1].X))
                    throw new InvalidDataException($"Channel '{spec.Id.Value}' curve keys must be increasing hours in [0, 24).");
            }
            switch (spec.Kind)
            {
                case ChannelClass.Window:
                    if (spec.Curve.Count == 0 || spec.Cap is <= 0f or > 1f)
                        throw new InvalidDataException($"Window channel '{spec.Id.Value}' needs a curve and a cap in (0, 1].");
                    break;
                case ChannelClass.Lamp:
                    if (spec.Curve.Count == 0 || spec.Group is null || !file.Groups.ContainsKey(spec.Group))
                        throw new InvalidDataException($"Lamp channel '{spec.Id.Value}' needs a curve and a known group.");
                    break;
                case ChannelClass.Cue:
                case ChannelClass.PoolGain:
                case ChannelClass.Reserved:
                    break;
                default:
                    throw new InvalidDataException($"Unknown channel class {spec.Kind}.");
            }
        }
        if (file.Channels.Count(c => c.Kind == ChannelClass.Cue) != 1 || file.Channels.Count(c => c.Kind == ChannelClass.PoolGain) != 1)
            throw new InvalidDataException("lighting-schedule.json needs exactly one cue slot and one pool-gain slot.");
        var cueSlot = file.Channels.FirstOrDefault(c => c.Id == file.Cue.Slot);
        var sysSlot = file.Channels.FirstOrDefault(c => c.Id == file.Cue.SysSlot);
        if (cueSlot is not { Kind: ChannelClass.Lamp } || sysSlot is not { Kind: ChannelClass.Cue })
            throw new InvalidDataException("cue.slot must name a lamp channel and cue.sysSlot the cue channel.");
        foreach (var target in file.Cue.Groups.Values)
            if (!file.Channels.Any(c => c.Id == target && c.Kind == ChannelClass.Lamp))
                throw new InvalidDataException($"cue group maps to unknown lamp channel '{target.Value}'.");
        foreach (var (name, group) in file.Groups)
            if (!float.IsFinite(group.FromDeg) || !float.IsFinite(group.ToDeg) || group.ToDeg > group.FromDeg)
                throw new InvalidDataException($"Threshold group '{name}' needs toDeg <= fromDeg.");
    }
}
