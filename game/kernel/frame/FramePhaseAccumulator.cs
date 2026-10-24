using Campus.Contracts;

namespace Campus.Kernel.Frame;

/// <summary>ENG-PERF-DATA: closes the previous B1-to-B1 interval, never reuses missing boundaries.</summary>
internal sealed class FramePhaseAccumulator(bool safeThreadModel)
{
    private double? _b0, _b1, _b2, _b3, _b4;
    private bool _ordered = true;
    private int _physicsSteps;
    private ulong? _b1Cycles, _b3Cycles, _drawCycles;

    internal double? FrameMilliseconds { get; private set; }
    internal FramePhases Phases { get; private set; }
    internal ulong? MainCycles { get; private set; }
    internal ulong? DrawCycles { get; private set; }

    internal void Physics(double now)
    {
        if (_physicsSteps++ == 0) _b0 = now;
        if (_b1.HasValue && !_b4.HasValue) _ordered = false;
    }

    internal void Begin(double now, ulong? cycles)
    {
        FrameMilliseconds = _b1 is { } previous ? now - previous : null;
        MainCycles = Difference(_b1Cycles, cycles);
        var b0 = _b0 ?? now;
        var valid = safeThreadModel && _ordered &&
            _b1 is { } b1 && _b2 is { } b2 && _b3 is { } b3 && _b4 is { } b4 &&
            b1 <= b2 && b2 <= b3 && b3 <= b4 && b4 <= b0 && b0 <= now;
        Phases = valid
            ? new(FramePhaseValidity.All, now - b0, _b2 - _b1, _b3 - _b2, _b4 - _b3, b0 - _b4, _physicsSteps)
            : new(FramePhaseValidity.None, null, null, null, null, null, _physicsSteps);
        DrawCycles = valid ? _drawCycles : null;
        _b0 = _b2 = _b3 = _b4 = null;
        _b1 = now;
        _b1Cycles = cycles;
        _b3Cycles = _drawCycles = null;
        _physicsSteps = 0;
        _ordered = true;
    }

    internal void ProcessEnd(double now)
    {
        if (_b2.HasValue || !_b1.HasValue) _ordered = false;
        _b2 = now;
    }

    internal void PreDraw(double now, ulong? cycles)
    {
        if (_b3.HasValue || !_b2.HasValue) _ordered = false;
        _b3 = now;
        _b3Cycles = cycles;
    }

    internal void PostDraw(double now, ulong? cycles)
    {
        if (_b4.HasValue || !_b3.HasValue) _ordered = false;
        _b4 = now;
        _drawCycles = Difference(_b3Cycles, cycles);
    }

    private static ulong? Difference(ulong? before, ulong? after) =>
        before is { } first && after is { } last && last >= first ? last - first : null;
}
