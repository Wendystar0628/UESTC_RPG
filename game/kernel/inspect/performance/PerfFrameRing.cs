using Campus.Contracts;

namespace Campus.Kernel.Inspect;

internal readonly record struct PerfGap(long FirstSequence, long LastSequence, long Rows,
    ulong? FirstCollectionFrame, ulong? LastCollectionFrame, double FromMs, double ToMs)
{
    public PerfGap Add(long sequence, ulong? frame, double time) => Rows == 0
        ? new(sequence, sequence, 1, frame, frame, time, time)
        : this with { LastSequence = sequence, LastCollectionFrame = frame, ToMs = time, Rows = Rows + 1 };
}

internal sealed class PerfFrameSlot(int viewportCapacity)
{
    public FrameSample Sample;
    public ViewportFrameSample[] Viewports = new ViewportFrameSample[viewportCapacity];
    public int ViewportCount;
    public long Sequence;
    public PerfGap Gap;
}

/// <summary>ENG-PERF-DATA: a Channel cannot copy borrowed spans without allocating a row on the frame path.
/// One main-thread producer and one writer own opposite cursors; a slot remains borrowed until Release.</summary>
internal sealed class PerfFrameRing
{
    private readonly PerfFrameSlot[] _slots;
    private long _read, _write, _sequence;
    private int _viewportCapacity, _requestedCapacity;
    private PerfGap _pendingGap;
    public long DroppedFrames { get; private set; }
    public PerfGap TailGap => _pendingGap;

    public PerfFrameRing(int viewportCapacity, int capacity = 512)
    {
        _viewportCapacity = Math.Max(1, viewportCapacity);
        _slots = Enumerable.Range(0, capacity).Select(_ => new PerfFrameSlot(_viewportCapacity)).ToArray();
    }

    public bool TryWrite(in FrameSample sample, ReadOnlySpan<ViewportFrameSample> viewports)
    {
        long sequence = ++_sequence;
        long write = _write;
        if (viewports.Length > _viewportCapacity || write - Volatile.Read(ref _read) == _slots.Length)
        {
            _requestedCapacity = Math.Max(_requestedCapacity, viewports.Length);
            _pendingGap = _pendingGap.Add(sequence, sample.CollectionFrame, sample.MonotonicMilliseconds);
            DroppedFrames++;
            return false;
        }
        var slot = _slots[(int)(write % _slots.Length)];
        slot.Sample = sample;
        viewports.CopyTo(slot.Viewports);
        slot.ViewportCount = viewports.Length;
        slot.Sequence = sequence;
        slot.Gap = _pendingGap;
        _pendingGap = default;
        Volatile.Write(ref _write, write + 1);
        return true;
    }

    // Host calls outside SamplePublished. The consumer releases its last borrowed slot before Empty.
    public void PrepareViewportLayout()
    {
        if (_requestedCapacity <= _viewportCapacity || !Empty) return;
        _viewportCapacity = _requestedCapacity;
        foreach (var slot in _slots) slot.Viewports = new ViewportFrameSample[_viewportCapacity];
    }

    public bool Empty => Volatile.Read(ref _write) == Volatile.Read(ref _read);
    public PerfFrameSlot? Peek() => Empty ? null : _slots[(int)(_read % _slots.Length)];
    public void Release() => Volatile.Write(ref _read, _read + 1);
}
