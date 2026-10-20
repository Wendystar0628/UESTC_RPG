using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;

namespace Campus.Flow;

internal sealed class AppFlowRecovery(ISaveService saves, IMapService maps,
    AppFlowContinuation continuation, AppFlowResources resources, Func<bool> isQuitting,
    Action<AppState> changeState, Action<string> diagnostic)
{
    internal const string RestoreErrorKey = "save.error.restore_world";
    private Task? _faultHold;
    internal string? LastError { get; private set; }
    internal void ResetHold() => _faultHold = null;
    internal void BeginEntry() { LastError = null; ResetHold(); }
    internal void CaptureFailure(Exception error, AppState state)
    {
        LastError = error.Message;
        diagnostic($"[WP-49a][map:{maps.Current.Value}][flow:{state}] {error.ToString()}");
    }
    internal void RetainRestoreError() => LastError = RestoreErrorKey;
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
            LastError = RestoreErrorKey;
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
    internal async Task RecoverTitle()
    {
        resources.HoldInput(); resources.HoldTitleFlow(); resources.HoldLoadingFlow();
        if (isQuitting()) return;
        await resources.BeginLoading(null, save: false);
        resources.CompleteWorldPreparation();
        await continuation.PrepareTitleDestination();
        await resources.FinishLoading(); resources.ReleaseLoadingFlow(); changeState(AppState.Title);
    }
}
