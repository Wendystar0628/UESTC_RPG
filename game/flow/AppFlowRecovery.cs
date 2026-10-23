using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;

namespace Campus.Flow;

internal sealed class AppFlowRecovery(IMapService maps, IWorldRestore restore, IMapRuntime mapRuntime,
    AppFlowResources resources, Func<bool> isQuitting,
    Action<AppState> changeState, Action<string, Exception> diagnostic)
{
    internal const string RestoreErrorKey = SaveErrorKeys.RestoreWorld;
    private Task? _faultHold;
    private string? _errorOverride;
    internal Exception? Failure { get; private set; }
    internal string? LastError => _errorOverride ?? (Failure is null ? null : "title.enter_failed");
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
            await RecoverTitle(keepBoot);
            done.TrySetResult();
        }
        catch (Exception error) { done.TrySetException(error); }
    }
    internal async Task RecoverTitle(bool keepBoot = false)
    {
        resources.HoldInput();
        if (!restore.RestoreFaulted) { resources.HoldTitleFlow(); resources.HoldLoadingFlow(); }
        if (isQuitting()) return;
        await resources.BeginLoading(null, save: false);
        mapRuntime.ClearDestination(); restore.EndInstance();
        await resources.ShowTitle();
        if (!keepBoot) changeState(AppState.Title);
        await resources.FinishLoading(); resources.ReleaseLoadingFlow();
    }
}
