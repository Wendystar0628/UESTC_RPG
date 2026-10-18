using Campus.Contracts;

namespace Campus.Flow;

public sealed partial class AppFlowService : IDisposable
{
    private IFlowLease? _titleFlow;
    private IFlowLease? _cinematicFlow;
    private IVeilLease? _cover;

    private IFlowLease CinematicFlow => _cinematicFlow ??
        throw new InvalidOperationException("The application flow has no cinematic flow lease.");

    private void HoldTitleFlow() => _titleFlow ??= control.AcquireFlow(FlowPriority.Pause, 0);
    private void HoldCinematicFlow()
    {
        if (_cinematicFlow is null) _cinematicFlow = control.AcquireFlow(FlowPriority.Cinematic, 0);
        else _cinematicFlow.SetRate(0);
    }
    private void ReleaseCinematicFlow()
    {
        var lease = _cinematicFlow; _cinematicFlow = null;
        lease?.Dispose();
    }
    private void ReleaseFlow()
    {
        try { ReleaseCinematicFlow(); }
        finally { var lease = _titleFlow; _titleFlow = null; lease?.Dispose(); }
    }
    private void CommitEntryFlow(bool newGame)
    {
        if (_definition?.End.Flow is { } rate) control.SetFlow(rate);
        else if (newGame) control.SetFlow(1);
    }
    // Flow owns endpoint, skip and fault coverage independently of playback masks.
    private Task CoverFlow(float seconds) => (_cover ??= veil.Cover(VeilReason.Cinematic, seconds)).Covered;
    private async Task RevealFlow()
    {
        var lease = _cover; _cover = null;
        if (lease is null) return;
        using (lease) await lease.Reveal(FlowTuning.RevealSeconds);
    }
    public void Dispose()
    {
        try { ReleaseFlow(); }
        finally
        {
            var cover = _cover; _cover = null; cover?.Dispose();
            _input?.Dispose(); _input = null;
        }
    }
}
