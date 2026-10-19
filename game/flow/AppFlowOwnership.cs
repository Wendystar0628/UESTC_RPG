using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;

namespace Campus.Flow;

public sealed partial class AppFlowService : IDisposable
{
    private IFlowLease? _titleFlow;
    private IFlowLease? _loadingFlow;
    private IMapArrival? _progressArrival;
    private int _arrivalCompleted;
    private int _arrivalTotal;
    private bool _worldComplete;
    private bool _saveComplete;
    private bool _saveNeeded;
    private bool _trackingLoading;
    private bool _loadingCompleted;

    private void HoldTitleFlow() => _titleFlow ??= control.AcquireFlow(FlowPriority.Pause, 0);
    private void HoldLoadingFlow()
    {
        if (_loadingFlow is null) _loadingFlow = control.AcquireFlow(FlowPriority.AppFlow, 0);
        else _loadingFlow.SetRate(0);
    }
    private void ReleaseLoadingFlow()
    {
        var lease = _loadingFlow; _loadingFlow = null; lease?.Dispose();
    }
    private void ReleaseFlow()
    {
        try { ReleaseLoadingFlow(); }
        finally { var lease = _titleFlow; _titleFlow = null; lease?.Dispose(); }
    }
    private async Task BeginLoading(EnterRequest? request, bool save)
    {
        StopTrackingLoading();
        _worldComplete = false; _saveComplete = false; _saveNeeded = save;
        _arrivalCompleted = 0; _arrivalTotal = 0; _loadingCompleted = false;
        _trackingLoading = true;
        mapRuntime.ArrivalChanged += ObserveArrival;
        await loading.Show(request);
        if (request is not null) titlePresentation.SetVisible(false);
        loading.Report(0, 1);
    }
    private void ObserveArrival(IMapArrival? arrival)
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
    private void CompleteWorldPreparation() { _worldComplete = true; ReportLoading(); }
    private void CompleteSavePreparation() { _saveComplete = true; ReportLoading(); }
    private void ReportLoading()
    {
        // Arrival supplies its real item count before any positive fraction is published.
        // This includes arrivals started inside NewGame/CompleteResume rather than duplicating them.
        if (!_trackingLoading || _arrivalTotal == 0) return;
        var completed = _arrivalCompleted + (_worldComplete ? 1 : 0) + (_saveNeeded && _saveComplete ? 1 : 0);
        var total = _arrivalTotal + 1 + (_saveNeeded ? 1 : 0);
        loading.Report(completed, total);
        if (completed == total && !_loadingCompleted)
        {
            _loadingCompleted = true; MarkPhase("loading-complete");
        }
    }
    private void CompleteLoadingProgress()
    {
        ReportArrival();
        if (!_worldComplete || _saveNeeded && !_saveComplete || _progressArrival is not { } arrival ||
            !arrival.Done.IsCompletedSuccessfully || _arrivalCompleted != _arrivalTotal)
            throw new InvalidOperationException("Loading cannot complete before every preparation item is ready.");
        ReportLoading();
    }
    private async Task FinishLoading()
    {
        CompleteLoadingProgress();
        await loading.Hide();
        StopTrackingLoading();
    }
    private void StopTrackingLoading()
    {
        mapRuntime.ArrivalChanged -= ObserveArrival;
        if (_progressArrival is { } arrival) arrival.ProgressChanged -= ReportArrival;
        _progressArrival = null; _trackingLoading = false;
    }
    private void MarkPhase(string phase) => PhaseReached?.Invoke(phase);
    public void Dispose()
    {
        try { StopTrackingLoading(); ReleaseFlow(); }
        finally { _input?.Dispose(); _input = null; }
    }
}
