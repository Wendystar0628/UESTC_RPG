using System.Threading.Channels;

namespace Campus.Kernel.Inspect;

internal sealed record PerfCapturedRow(string Stream, ulong? Frame, double Time, IReadOnlyDictionary<string, object?> Values);
internal sealed record PerfValueMessage(PerfCapturedEvent? Event, PerfCapturedRow? Row, PerfGap Gap, long Sequence = 0,
    (NvmlIdentity Identity, NvmlFastBatch Values)? FastBatch = null);

/// <summary>One producer reserves a bounded slot for each publication step, then seals its detached values.
/// BCL Channel supplies handoff; batching keeps a synchronous source burst from consuming one slot per row.</summary>
internal sealed class PerfValueChannel(string source, string stream = "events")
{
    private readonly Channel<Publication> _channel = Channel.CreateBounded<Publication>(
        new BoundedChannelOptions(256) { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
    private Publication? _writing, _reading;
    private int _readIndex;
    private bool _stopped;
    private long _attempts;
    private PerfGap _gap;
    public string Source { get; } = source;
    public string Stream { get; } = stream;
    public long Dropped { get; private set; }
    public PerfGap TailGap => _gap;
    public bool Complete => _reading is null && _channel.Reader.Completion.IsCompleted;
    public bool TryEvent(PerfCapturedEvent value) => TryWrite(new(value, null, default), value.Frame, value.Time);
    public bool TryRow(PerfCapturedRow value) => TryWrite(new(null, value, default), value.Frame, value.Time);
    public bool TryFastBatch(NvmlIdentity identity, NvmlFastBatch batch) =>
        // Empty queries still produce one metadata row through FastRows.
        TryWrite(new(null, null, default, FastBatch: (identity, batch)), null, batch.QueryEndMs, Math.Max(1, batch.Samples.Count));
    private bool TryWrite(PerfValueMessage value, ulong? frame, double time, int rows = 1)
    {
        long sequence = _attempts + 1;
        _attempts += rows;
        var captured = value with { Gap = _gap, Sequence = sequence };
        if (!_stopped)
        {
            if (_writing is { } publication)
            {
                publication.Values.Add(captured);
                _gap = default;
                return true;
            }
            var next = new Publication(captured);
            if (_channel.Writer.TryWrite(next))
            {
                _writing = next;
                _gap = default;
                return true;
            }
        }
        Dropped += rows;
        _gap = _gap.Add(sequence, frame, time) with { LastSequence = _attempts, Rows = _gap.Rows + rows };
        return false;
    }
    public void Flush()
    {
        if (_writing is not { } publication) return;
        _writing = null;
        publication.Seal();
    }

    public bool TryRead(out PerfValueMessage? value)
    {
        value = null;
        if (_reading is null)
        {
            // A producer may be stalled. Leave its mutable batch queued and let IO drain other producers.
            if (!_channel.Reader.TryPeek(out var next) || !next.Sealed || !_channel.Reader.TryRead(out _reading)) return false;
            _readIndex = 0;
        }
        value = _reading.Values[_readIndex++];
        if (_readIndex == _reading.Values.Count) _reading = null;
        return true;
    }

    public void Stop()
    {
        _stopped = true;
        Flush();
        _channel.Writer.TryComplete();
    }

    private sealed class Publication(PerfValueMessage first)
    {
        private int _sealed;
        internal List<PerfValueMessage> Values { get; } = [first];
        internal bool Sealed => Volatile.Read(ref _sealed) != 0;
        internal void Seal() => Volatile.Write(ref _sealed, 1);
    }
}
