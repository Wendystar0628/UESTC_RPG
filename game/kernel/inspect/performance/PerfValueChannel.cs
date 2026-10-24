using System.Threading.Channels;

namespace Campus.Kernel.Inspect;

internal sealed record PerfCapturedRow(string Stream, ulong? Frame, double Time, IReadOnlyDictionary<string, object?> Values);
internal sealed record PerfValueMessage(PerfCapturedEvent? Event, PerfCapturedRow? Row, PerfGap Gap, long Sequence = 0,
    (NvmlIdentity Identity, NvmlFastBatch Values)? FastBatch = null);

/// <summary>One producer per channel; only the session writer consumes captured values.</summary>
internal sealed class PerfValueChannel(string source, string stream = "events")
{
    private readonly Channel<PerfValueMessage> _channel = Channel.CreateBounded<PerfValueMessage>(
        new BoundedChannelOptions(256) { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
    private long _attempts;
    private PerfGap _gap;
    public string Source { get; } = source;
    public string Stream { get; } = stream;
    public long Dropped { get; private set; }
    public PerfGap TailGap => _gap;
    public bool Complete => _channel.Reader.Completion.IsCompleted;
    public bool TryEvent(PerfCapturedEvent value) => TryWrite(new(value, null, default), value.Frame, value.Time);
    public bool TryRow(PerfCapturedRow value) => TryWrite(new(null, value, default), value.Frame, value.Time);
    public bool TryFastBatch(NvmlIdentity identity, NvmlFastBatch batch) =>
        // Empty queries still produce one metadata row through FastRows.
        TryWrite(new(null, null, default, FastBatch: (identity, batch)), null, batch.QueryEndMs, Math.Max(1, batch.Samples.Count));
    private bool TryWrite(PerfValueMessage value, ulong? frame, double time, int rows = 1)
    {
        long sequence = _attempts + 1;
        _attempts += rows;
        if (_channel.Writer.TryWrite(value with { Gap = _gap, Sequence = sequence })) { _gap = default; return true; }
        Dropped += rows;
        _gap = _gap.Add(sequence, frame, time) with { LastSequence = _attempts, Rows = _gap.Rows + rows };
        return false;
    }
    public bool TryRead(out PerfValueMessage? value) => _channel.Reader.TryRead(out value);
    public void Stop() => _channel.Writer.TryComplete();
}
