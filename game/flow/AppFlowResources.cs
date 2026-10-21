using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;

namespace Campus.Flow;

internal sealed class AppFlowResources(ICameraRig camera, IWorldRestore restore, IWorldControl control,
    IAppLoadingPresentation loading, IAppFrontispiecePresentation frontispiece, IMapRuntime mapRuntime,
    Action<string> phaseReached) : IDisposable
{
    private IDisposable? _input;
    private IFlowLease? _titleFlow;
    private IFlowLease? _loadingFlow;
    private IMapArrival? _progressArrival;
    private int _arrivalCompleted;
    private int _arrivalTotal;
    private bool _worldComplete;
    private bool _saveComplete;
    private bool _saveNeeded;
    private bool _requiresArrival;
    private bool _trackingLoading;
    private bool _loadingCompleted;
    private IAppLoadingLease? _loadingLease;

    internal void HoldTitleFlow() => _titleFlow ??= control.AcquireFlow(FlowPriority.Pause, 0);
    internal void HoldLoadingFlow()
    {
        if (_loadingFlow is null) _loadingFlow = control.AcquireFlow(FlowPriority.AppFlow, 0);
        else if (!restore.RestoreFaulted) _loadingFlow.SetRate(0);
    }
    internal void ReleaseLoadingFlow()
    {
        var lease = _loadingFlow; _loadingFlow = null; lease?.Dispose();
    }
    internal void ReleaseFlow()
    {
        try { ReleaseLoadingFlow(); }
        finally { var lease = _titleFlow; _titleFlow = null; lease?.Dispose(); }
    }
    internal async Task BeginLoading(EnterRequest? request, bool save)
    {
        StopTrackingLoading();
        _worldComplete = false; _saveComplete = false; _saveNeeded = save; _requiresArrival = request is not null;
        _arrivalCompleted = 0; _arrivalTotal = 0; _loadingCompleted = false;
        _trackingLoading = true;
        mapRuntime.ArrivalChanged += ObserveArrival;
        var previous = _loadingLease;
        var lease = loading.Show(request);
        _loadingLease = lease;
        await lease.Covered;
        if (previous is not null) await previous.Hide();
        if (request is not null) await frontispiece.Hide(0);
        lease.Report(0, 1);
    }
    internal void ObserveArrival(IMapArrival? arrival)
    {
        if (_progressArrival is { } previous) previous.ProgressChanged -= ReportArrival;
        _progressArrival = arrival;
        if (arrival is not null) arrival.ProgressChanged += ReportArrival;
        ReportArrival();
    }
    private void ReportArrival()
    {
        if (_progressArrival is not { } arrival || !_trackingLoading) return;
        _arrivalTotal = arrival.Total; _arrivalCompleted = arrival.Completed;
        ReportLoading();
    }
    internal async Task ShowTitle()
    {
        await frontispiece.Show();
        if (_progressArrival is { } arrival) arrival.ProgressChanged -= ReportArrival;
        _progressArrival = null; _arrivalCompleted = 0; _arrivalTotal = 0; _requiresArrival = false;
        CompleteWorldPreparation();
    }
    internal void CompleteWorldPreparation() { _worldComplete = true; ReportLoading(); }
    internal void CompleteSavePreparation() { _saveComplete = true; ReportLoading(); }
    private void ReportLoading()
    {
        // Arrival supplies its real item count before any positive fraction is published.
        // This includes arrivals started inside NewGame/Load rather than duplicating them.
        if (!_trackingLoading) return;
        var completed = _arrivalCompleted + (_worldComplete ? 1 : 0) + (_saveNeeded && _saveComplete ? 1 : 0);
        var total = _arrivalTotal + 1 + (_saveNeeded ? 1 : 0);
        _loadingLease?.Report(completed, total);
        if (completed == total && !_loadingCompleted)
        {
            _loadingCompleted = true; phaseReached("loading-complete");
        }
    }
    internal void CompleteLoadingProgress()
    {
        ReportArrival();
        if (!_worldComplete || _saveNeeded && !_saveComplete || _requiresArrival && _progressArrival is null ||
            _progressArrival is { } arrival && (!arrival.Done.IsCompletedSuccessfully || _arrivalCompleted != _arrivalTotal))
            throw new InvalidOperationException("Loading cannot complete before every preparation item is ready.");
        ReportLoading();
    }
    internal async Task FinishLoading()
    {
        CompleteLoadingProgress();
        var lease = _loadingLease;
        _loadingLease = null;
        if (lease is not null) await lease.Hide();
        StopTrackingLoading();
    }
    private void StopTrackingLoading()
    {
        mapRuntime.ArrivalChanged -= ObserveArrival;
        if (_progressArrival is { } arrival) arrival.ProgressChanged -= ReportArrival;
        _progressArrival = null; _trackingLoading = false;
    }
    internal void HoldInput() => _input ??= camera.LockInput(InputLockReason.AppFlow);
    internal void ReleaseInput()
    {
        if (restore.RestoreFaulted) return;
        _input?.Dispose(); _input = null;
    }
    public void Dispose()
    {
        try { StopTrackingLoading(); ReleaseFlow(); }
        finally { _input?.Dispose(); _input = null; }
    }
}
