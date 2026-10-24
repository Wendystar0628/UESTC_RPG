namespace Campus.Kernel.Inspect;

/// <summary>Pure value accounting, independent of NVML loading and Godot's CLR host.</summary>
internal sealed class NvmlCounterTracker
{
    private ulong? _previous;
    private ulong? _reference;
    private double? _readEndMs;
    private bool _readGap;

    public NvmlCounterInterval Observe(NvmlValue<ulong> reading, double startMs, double endMs,
        ulong? driverReferenceUs = null)
    {
        ulong? value = reading.Value;
        bool reset = value.HasValue && _previous.HasValue &&
            (value < _previous || driverReferenceUs.HasValue && _reference.HasValue && driverReferenceUs < _reference);
        ulong? delta = value.HasValue && _previous.HasValue && !reset ? value.Value - _previous.Value : null;
        var interval = new NvmlCounterInterval
        {
            Cumulative = value, Delta = delta, IntervalStartMs = _readEndMs, ReadStartMs = startMs,
            ReadEndMs = endMs, Reset = reset, ReadGap = _readGap,
            DriverReferenceUs = driverReferenceUs, Status = reading.Status
        };
        if (value.HasValue)
        {
            _previous = value;
            _reference = driverReferenceUs;
            _readEndMs = endMs;
            _readGap = false;
        }
        else _readGap = true;
        return interval;
    }
}

internal sealed class NvmlSampleCursor
{
    private double? _successfulReadMs;
    private double? _largestObservedIntervalMs;
    private double? _observedBufferSpanMs;
    private bool _readFailure;
    public ulong LastSeenUs { get; private set; }

    public NvmlFastBatch Observe(NvmlMetric metric, NvmlStatus status, NvmlNative.Sample[] raw,
        double startMs, double endMs)
    {
        ulong before = LastSeenUs;
        double? previous = _successfulReadMs;
        var reasons = new List<string>();
        var points = new List<NvmlFastPoint>();
        if (status.IsSuccess)
        {
            Array.Sort(raw, (left, right) => left.TimestampUs.CompareTo(right.TimestampUs));
            if (raw.Length > 0 && before > 0 && raw[^1].TimestampUs < before)
            {
                reasons.Add("driverTimestampRegressed: cursor reset, cross-boundary interval unknown");
                LastSeenUs = 0;
                _largestObservedIntervalMs = null;
                _observedBufferSpanMs = null;
            }
            if (LastSeenUs == 0 && raw.Length > 1)
                _observedBufferSpanMs = (raw[^1].TimestampUs - raw[0].TimestampUs) / 1000d;
            if (before == 0) reasons.Add("initialBufferedWindow: session boundary is not driver clock origin");
            if (_readFailure) reasons.Add("queryFailureSinceLastSuccessfulRead: buffer continuity unconfirmed");
            if (previous.HasValue && _observedBufferSpanMs is > 0 && endMs - previous > _observedBufferSpanMs)
                reasons.Add("readSpanExceedsObservedBufferSpan: possible gap, retention not guaranteed");
            foreach (var sample in raw)
            {
                if (sample.TimestampUs <= LastSeenUs) continue;
                double? interval = LastSeenUs == 0 ? null : (sample.TimestampUs - LastSeenUs) / 1000d;
                if (points.Count == 0 && interval.HasValue && _largestObservedIntervalMs.HasValue &&
                    interval > _largestObservedIntervalMs)
                    reasons.Add("boundaryIntervalExceedsObservedRange: possible gap, lost count unknown");
                if (interval.HasValue)
                    _largestObservedIntervalMs = Math.Max(_largestObservedIntervalMs ?? interval.Value, interval.Value);
                points.Add(new(sample.TimestampUs, ConvertValue(metric, sample.Value.UnsignedInt), interval));
                LastSeenUs = sample.TimestampUs;
            }
            _successfulReadMs = endMs;
            _readFailure = false;
        }
        else if (status.NativeCode != NvmlNative.NotFound)
        {
            _readFailure = true;
            reasons.Add("queryUnavailable: no inferred sample count");
        }
        return new()
        {
            Metric = metric, Status = status, QueryStartMs = startMs, QueryEndMs = endMs,
            PreviousSuccessfulReadMs = previous,
            SuccessfulReadSpanMs = previous.HasValue ? endMs - previous.Value : null,
            CursorBeforeUs = before, CursorAfterUs = LastSeenUs,
            ObservedBufferSpanMs = _observedBufferSpanMs,
            Samples = points.ToArray(), ContinuityReasons = reasons.ToArray()
        };
    }

    private static double ConvertValue(NvmlMetric metric, uint value) => metric switch
    {
        NvmlMetric.Power => value / 1000d,
        NvmlMetric.GpuUtilization or NvmlMetric.MemoryUtilization => value / 100d,
        _ => throw new ArgumentOutOfRangeException(nameof(metric))
    };
}
