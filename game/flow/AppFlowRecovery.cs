using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;

namespace Campus.Flow;

public sealed partial class AppFlowService
{
    private const string RestoreErrorKey = "save.error.restore_world";
    private Task? _faultHold;

    private void RequireHealthyWorld()
    {
        if (restore.RestoreFaulted) throw new InvalidOperationException(RestoreErrorKey);
    }
    private Task HoldRestoreFailure(bool keepBoot = false)
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
            _preview = null; _livePreview = false; _newTimeline = false; _resumeEndpoint = null;
            HoldInput(); HoldTitleFlow(); HoldLoadingFlow();
            await BeginLoading(null, save: false);
            var result = await saves.RestoreTitlePreview();
            if (!Loaded(result.Status))
            {
                // Default data itself failed: retain the covered fault for boot/close handling.
                if (!_quitting && !keepBoot) Change(AppState.Title);
                done.TrySetResult(); return;
            }
            RequireHealthyWorld(); _titleArrival = null;
            CompleteWorldPreparation();
            await PrepareTitleDestination();
            await FinishLoading(); ReleaseLoadingFlow();
            if (!_quitting && !keepBoot) Change(AppState.Title);
            done.TrySetResult();
        }
        catch (Exception error) { done.TrySetException(error); }
    }
    private async Task RecoverTitle()
    {
        HoldInput(); HoldTitleFlow(); HoldLoadingFlow();
        if (_quitting) return;
        await BeginLoading(null, save: false);
        CompleteWorldPreparation();
        await PrepareTitleDestination();
        await FinishLoading(); ReleaseLoadingFlow(); Change(AppState.Title);
    }
    private async Task<bool> QuitFaultedWorld()
    {
        await HoldRestoreFailure(); Change(AppState.Quitting);
        try
        {
            var result = await saves.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(FlowTuning.SaveTimeoutSeconds));
            if (CanLeave(result.Status)) return true;
        }
        catch (Exception error) when (error is not OutOfMemoryException) { CaptureFailure(error); LastError = RestoreErrorKey; }
        _quitting = false; Change(AppState.Title);
        return false;
    }
}
