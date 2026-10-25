using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Kernel.Frame;

namespace Campus.Kernel.Inspect;

/// <summary>Pure heartbeat state machine; Godot callbacks cannot detect a blocked main thread.</summary>
internal sealed class PerfHangDetector(double timeoutMilliseconds)
{
    private FrameStageSnapshot? _last;
    private bool _hung;
    public void Observe(FrameStageSnapshot? snapshot, double now, Action<PerfCapturedEvent> emit)
    {
        if (snapshot is not { } stage) return;
        if (_last != snapshot)
        {
            if (_hung) emit(new("hang.end", stage.CollectionFrame, now,
                new { boundary = _last!.Value.Boundary.ToString(), stalledSinceMs = _last.Value.MonotonicMilliseconds,
                    resumedBoundary = stage.Boundary.ToString(), reason = "heartbeatResumed" }));
            _hung = false;
            _last = stage;
        }
        if (!_hung && now - stage.MonotonicMilliseconds >= timeoutMilliseconds)
        {
            emit(new("hang.begin", stage.CollectionFrame, now,
                new { boundary = stage.Boundary.ToString(), stalledSinceMs = stage.MonotonicMilliseconds }));
            _hung = true;
        }
    }
    public void Close(double now, Action<PerfCapturedEvent> emit)
    {
        if (!_hung || _last is not { } stage) return;
        emit(new("hang.end", stage.CollectionFrame, now,
            new { boundary = stage.Boundary.ToString(), stalledSinceMs = stage.MonotonicMilliseconds,
                resumedBoundary = (string?)null, reason = "recordingStopped" }));
        _hung = false;
    }
}

internal sealed class PerfHangWatchdog
{
    private readonly CancellationTokenSource _stop = new();
    private readonly PerfValueChannel _output;
    private readonly Thread _thread;
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Completion => _completion.Task;
    public PerfHangWatchdog(IFrameStatistics frames, PerfValueChannel output, PerfRecordingSession session)
    {
        _output = output;
        _thread = new Thread(() =>
        {
            var detector = new PerfHangDetector(KernelBudgets.PerfHangWatchdogSeconds * 1000);
            void Emit(PerfCapturedEvent item) => output.TryEvent(item);
            try
            {
                while (!_stop.IsCancellationRequested)
                {
                    detector.Observe(frames.StageSnapshot, FrameClock.MonotonicMilliseconds, Emit);
                    output.Flush();
                    _stop.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(KernelBudgets.PerfHangWatchdogSeconds / 10));
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
            catch (Exception error)
            {
                session.AddFailure(PerfFaultCode.ProducerFailed, error);
                try
                {
                    output.TryEvent(new("producer.failure", null, FrameClock.MonotonicMilliseconds,
                        new { producer = "watchdog", exceptionType = error.GetType().FullName }));
                }
                catch (Exception eventError) { session.AddFailure(PerfFaultCode.ProducerFailed, eventError); }
            }
            finally
            {
                try { detector.Close(FrameClock.MonotonicMilliseconds, Emit); }
                catch (Exception error) { session.AddFailure(PerfFaultCode.ProducerFailed, error); }
                finally { output.Stop(); _completion.SetResult(); }
            }
        }) { IsBackground = true, Name = "Performance heartbeat watchdog" };
        try { _thread.Start(); }
        catch { _stop.Dispose(); throw; }
    }
    public void Stop() => _stop.Cancel();
    public void Release() { _thread.Join(); _output.Stop(); _stop.Dispose(); }
}
