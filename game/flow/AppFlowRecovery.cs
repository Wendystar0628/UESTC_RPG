using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;

namespace Campus.Flow;

internal sealed class AppFlowRecovery(ISaveService saves, IMapService maps,
    AppFlowContinuation continuation, AppFlowResources resources, Func<bool> isQuitting,
    Action<AppState> changeState, Action<string, Exception> diagnostic)
{
    internal const string RestoreErrorKey = "save.error.restore_world";
    private Task? _faultHold;
    private string? _errorOverride;
    internal Exception? Failure { get; private set; }
    internal string? LastError => _errorOverride ?? Failure?.Message;
    internal void ResetHold() => _faultHold = null;
    internal void BeginEntry() { Failure = null; _errorOverride = null; ResetHold(); }
    internal void CaptureFailure([FailureHandler("SYS-TITLE")] Exception error, AppState state)
    {
        Failure = error; _errorOverride = null;
        diagnostic($"[game/flow/][map:{maps.Current.Value}][flow:{state}]", error);
    }
    internal void RetainRestoreError() => _errorOverride = RestoreErrorKey;
    internal Task HoldRestoreFailure(bool keepBoot = false)
    {
        if (_faultHold is not null) return _faultHold;
        var done = new TaskCompletionSource(); _faultHold = done.Task;
        _ = HoldRestoreFailureCore(done, keepBoot);
        return done.Task;
    }
    private async Task HoldRestoreFailureCore(TaskCompletionSource done, bool keepBoot)
    {
        try
        {
            _errorOverride = RestoreErrorKey;
            continuation.ResetFaultedWorld();
            resources.HoldInput(); resources.HoldTitleFlow(); resources.HoldLoadingFlow();
            await resources.BeginLoading(null, save: false);
            var result = await saves.RestoreTitlePreview();
            if (!AppFlowContinuation.Loaded(result.Status))
            {
                // Default data failed: retain cover for boot/close handling.
                if (!isQuitting() && !keepBoot) changeState(AppState.Title);
                done.TrySetResult(); return;
            }
            continuation.RequireHealthyWorld(); continuation.InvalidateTitleDestination();
            resources.CompleteWorldPreparation();
            await continuation.PrepareTitleDestination();
            await resources.FinishLoading(); resources.ReleaseLoadingFlow();
            if (!isQuitting() && !keepBoot) changeState(AppState.Title);
            done.TrySetResult();
        }
        catch (Exception error) { done.TrySetException(error); }
    }
    internal async Task<IMapArrival?> RecoverTitle()
    {
        resources.HoldInput(); resources.HoldTitleFlow(); resources.HoldLoadingFlow();
        if (isQuitting()) return null;
        await resources.BeginLoading(null, save: false);
        resources.CompleteWorldPreparation();
        var arrival = await continuation.PrepareTitleDestination();
        await resources.FinishLoading(); resources.ReleaseLoadingFlow(); changeState(AppState.Title);
        return arrival;
    }
}
