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
        // Publish the task before Clear: cancelling playback can synchronously
        // reenter this failure handler through the awaiting entry operation.
        var done = new TaskCompletionSource(); _faultHold = done.Task;
        _ = HoldRestoreFailureCore(done, keepBoot);
        return done.Task;
    }
    private async Task HoldRestoreFailureCore(TaskCompletionSource done, bool keepBoot)
    {
        try
        {
            LastError = RestoreErrorKey;
            _preview = null; _livePreview = false; _previewNeedsVeil = true;
            _flowTarget = null; _newTimeline = false;
            _resumeEndpoint = null;
            HoldInput();
            await CoverFlow(0);
            playback.Clear();
            // Runtime recovery can use the title menu; a failed boot remains Boot
            // for the stage's failure handler, without claiming a prepared title.
            // The partially restored world and camera must remain completely hidden.
            if (!_quitting && !keepBoot) Change(AppState.Title);
            done.TrySetResult();
        }
        catch (Exception error) { done.TrySetException(error); }
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

    private async Task<EnterResult> RecoverEntry(bool newGame)
    {
        _path = EnterPath.Veil; _skipped = true;
        _preview = null; _livePreview = false;
        await CoverFlow(0);
        // These are the only operations allowed to clear the fault. Save performs
        // World -> map travel -> Content/Pose even when Context was previously null.
        if (newGame) await StartNewTimeline(); else await RestoreRequested();
        RequireHealthyWorld();
        var context = Context();
        if (newGame)
        {
            _definition = new CinematicCatalog(context).Find("enterNew");
            await AdvanceToDefault();
        }
        _finishing = true;
        await FinishEntry(newGame, underVeil: true);
        LastError = null;
        return EnterResult.Entered;
    }
}
