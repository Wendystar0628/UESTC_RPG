using System.Numerics;
using Campus.Camera.Cinematic;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Cinematic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class CinematicPlaybackTests
{
    [TestCase]
    public void DelayedCameraAndCapturedCurrentPreserveStartPose()
    {
        var pose = new Pose(new(10, 40, 50), 170, -20, 62);
        var anchors = Anchors(pose, default, 70);
        var definition = Timeline() with { Camera = [Hold(0.5, 2)] };
        CinematicValidation.Validate(definition, definition.Map);
        using var track = new CinematicTrack(definition, anchors, new TestWorldModel());
        var changedCamera = pose with { Position = Vector3.One, YawDeg = 0 };
        if (track.Sample(0.25, changedCamera, 0.1) != pose) throw new InvalidOperationException("$current was reread during a delayed camera track.");
        if (anchors.Resolve(new FovKey(0, Anchor: "$dest")) != 70) throw new InvalidOperationException("Missing saved FOV did not fall back to the setting.");
    }
    [TestCase]
    public void WorldReplacementRejectsFadeRampsAndLetterbox()
    {
        var definition = Timeline() with { World = [new(0.5, WorldCueKind.NewTimeline)] };
        var letterboxRejected = false;
        try { CinematicValidation.Validate(definition with { Masks = [new(0, 1, MaskKind.Letterbox)] }, definition.Map); }
        catch (InvalidDataException) { letterboxRejected = true; }
        AssertThat(letterboxRejected).OverrideFailureMessage("Invalid timeline was accepted.").IsTrue();
        var fadeRampRejected = false;
        try { CinematicValidation.Validate(definition with { Masks = [new MaskSeg(0, 2, MaskKind.Veil) { FadeIn = 1 }] }, definition.Map); }
        catch (InvalidDataException) { fadeRampRejected = true; }
        AssertThat(fadeRampRejected).OverrideFailureMessage("Invalid timeline was accepted.").IsTrue();
        CinematicValidation.Validate(definition with { Masks = [new(0, 1, MaskKind.Veil)] }, definition.Map);
        var shortVeilRejected = false;
        try { CinematicValidation.Validate(definition with { Masks = [new(0, 0.5, MaskKind.Veil)] }, definition.Map); }
        catch (InvalidDataException) { shortVeilRejected = true; }
        AssertThat(shortVeilRejected).OverrideFailureMessage("Invalid timeline was accepted.").IsTrue();
    }
    [TestCase]
    public async Task CoverageRequiresARealPostDrawAfterTheRequest()
    {
        var rig = new FlowTestRig();
        var veil = rig.Veil; var presentation = rig.Presentation; var playback = rig.Playback;
        await veil.Cover(VeilReason.Cinematic, 0);
        var covered = playback.WaitForPresentedCoverage();
        playback.Tick(0);
        if (covered.IsCompleted) throw new InvalidOperationException("Numeric opacity incorrectly counted as a presented cover.");
        playback.FramePresented(); playback.Tick(0);
        if (covered.IsCompleted) throw new InvalidOperationException("An uncovered frame authorized warmup.");
        presentation.Present(new(1, VeilContent.Fog, null)); playback.Tick(0);
        if (covered.IsCompleted) throw new InvalidOperationException("Presentation state without a draw authorized warmup.");
        playback.FramePresented(); playback.Tick(0); await covered;
        var nextPose = playback.WaitForPresentedCoverage(); playback.Tick(0);
        if (nextPose.IsCompleted) throw new InvalidOperationException("An old covered frame authorized a later camera pose.");
        playback.FramePresented(); playback.Tick(0); await nextPose;
    }
    [TestCase]
    public void ClearCancelsInfiniteWaitsAndPlaybackWithoutCancelingTheirWorldJob()
    {
        var rig = new FlowTestRig();
        var camera = rig.Camera; var playback = rig.Playback;
        var worldJob = new TaskCompletionSource();
        var definition = Timeline() with
        {
            World = [new(0, WorldCueKind.NewTimeline)],
            Masks = [new(0, 1, MaskKind.Veil)],
        };
        var playing = playback.Play(definition, Start(camera.Pose, default, 70), new CinematicSpawnMapStub(), new TestWorldModel(), AppState.Entering,
            (_, _) => worldJob.Task);
        playback.Tick(0);
        var predicateCalls = 0;
        var waiting = playback.WaitUntil(() => { predicateCalls++; return false; }, double.PositiveInfinity, "Old wait.");
        playback.Clear();
        if (!playing.IsCanceled || !waiting.IsCanceled || worldJob.Task.IsCompleted)
            throw new InvalidOperationException("Clear failed to cancel its own work or canceled the underlying world transaction.");
        worldJob.SetResult(); playback.Tick(10);
        if (predicateCalls != 1)
            throw new InvalidOperationException("A canceled wait was polled again after Clear.");
    }
    [TestCase]
    public async Task ReentrantClearDuringPlaybackCancellationPreservesTheNextPlaybackAndWait()
    {
        var rig = new FlowTestRig();
        var camera = rig.Camera; var playback = rig.Playback;
        var playing = playback.Play(Timeline(), Start(camera.Pose, default, 70), new CinematicSpawnMapStub(), new TestWorldModel(), AppState.Entering,
            (_, _) => Task.CompletedTask);
        var oldPredicateCalls = 0;
        var oldWait = playback.WaitUntil(() => { oldPredicateCalls++; return false; }, double.PositiveInfinity, "Old wait.");
        Task? nextPlaying = null; Task? nextWait = null; var nextReady = false;
        var restart = RestartAfterCancellation();
        playback.Clear();
        if (!restart.IsCompletedSuccessfully || !oldWait.IsCanceled || nextPlaying is null || nextWait is null ||
            nextPlaying.IsCompleted || nextWait.IsCompleted || !playback.IsPlaying)
            throw new InvalidOperationException("Old cancellation cleared the replacement playback or its wait.");
        nextReady = true; playback.Tick(0.25); await nextWait; await restart;
        if (oldPredicateCalls != 1 || nextPlaying.IsCompleted || playback.Time != 0.25)
            throw new InvalidOperationException("Reentrant Clear retained an old wait or prevented the replacement from advancing.");
        playback.Clear();
        if (!nextPlaying.IsCanceled) throw new InvalidOperationException("Replacement playback did not retain its own cancellation lifetime.");

        async Task RestartAfterCancellation()
        {
            try { await playing; throw new InvalidOperationException("The old playback should be canceled."); }
            catch (OperationCanceledException)
            {
                // EnterCore also clears playback synchronously when its awaited task is canceled.
                playback.Clear();
                nextWait = playback.WaitUntil(() => nextReady, double.PositiveInfinity, "Replacement wait.");
                nextPlaying = playback.Play(Timeline(), Start(camera.Pose, default, 70), new CinematicSpawnMapStub(), new TestWorldModel(), AppState.Entering,
                    (_, _) => Task.CompletedTask);
            }
        }
    }
    [TestCase]
    public async Task TickDoesNotPollCanceledOrNewWaitsAfterACompletionClearsPlayback()
    {
        var rig = new FlowTestRig();
        var camera = rig.Camera; var playback = rig.Playback;
        var oldPredicateCalls = 0;
        var oldWait = playback.WaitUntil(() => { oldPredicateCalls++; return false; }, double.PositiveInfinity, "Old wait.");
        var completeReady = false;
        var completing = playback.WaitUntil(() => completeReady, double.PositiveInfinity, "Completing wait.");
        var nextPredicateCalls = 0; var nextReady = false; Task? nextWait = null;
        var replace = ReplaceWhenCompleted();
        completeReady = true; playback.Tick(0.1);
        if (!replace.IsCompletedSuccessfully || !oldWait.IsCanceled || oldPredicateCalls != 1 ||
            nextWait is null || nextWait.IsCompleted || nextPredicateCalls != 1)
            throw new InvalidOperationException("A completion mutated the current wait pass or consumed a replacement wait.");
        nextReady = true; playback.Tick(0); await nextWait; await replace;
        if (oldPredicateCalls != 1 || nextPredicateCalls != 2)
            throw new InvalidOperationException("The next tick did not isolate old cancellation from the replacement wait.");

        async Task ReplaceWhenCompleted()
        {
            await completing;
            playback.Clear();
            nextWait = playback.WaitUntil(() => { nextPredicateCalls++; return nextReady; }, double.PositiveInfinity, "Replacement wait.");
        }
    }
    [TestCase]
    public async Task OldDoneCancellationCannotClearPlaybackStartedByAnOldWaitCancellation()
    {
        var rig = new FlowTestRig();
        var camera = rig.Camera; var playback = rig.Playback;
        var playing = playback.Play(Timeline(), Start(camera.Pose, default, 70), new CinematicSpawnMapStub(), new TestWorldModel(), AppState.Entering,
            (_, _) => Task.CompletedTask);
        var waiting = playback.WaitUntil(() => false, double.PositiveInfinity, "Old wait.");
        Task? nextPlaying = null; Task? nextWait = null; var nextReady = false;
        var restart = RestartWhenWaitIsCanceled();
        var clearAgain = ClearWhenPlaybackIsCanceled();
        playback.Clear();
        if (!restart.IsCompletedSuccessfully || !clearAgain.IsCompletedSuccessfully || nextPlaying is null ||
            nextWait is null || nextPlaying.IsCompleted || nextWait.IsCompleted || !playback.IsPlaying)
            throw new InvalidOperationException("A later old cancellation cleared the playback started by an earlier old cancellation.");
        nextReady = true; playback.Tick(0.25); await nextWait; await restart; await clearAgain;
        if (nextPlaying.IsCompleted || playback.Time != 0.25)
            throw new InvalidOperationException("The replacement playback did not retain its own lifetime.");
        playback.Clear();

        async Task RestartWhenWaitIsCanceled()
        {
            try { await waiting; throw new InvalidOperationException("The old wait should be canceled."); }
            catch (OperationCanceledException)
            {
                nextPlaying = playback.Play(Timeline(), Start(camera.Pose, default, 70), new CinematicSpawnMapStub(), new TestWorldModel(), AppState.Entering,
                    (_, _) => Task.CompletedTask);
                nextWait = playback.WaitUntil(() => nextReady, double.PositiveInfinity, "Replacement wait.");
            }
        }
        async Task ClearWhenPlaybackIsCanceled()
        {
            try { await playing; throw new InvalidOperationException("The old playback should be canceled."); }
            catch (OperationCanceledException) { playback.Clear(); }
        }
    }
    [TestCase]
    public async Task SkippingJoinsInFlightRestoreAndDrainsEachCueOnce()
    {
        var rig = new FlowTestRig();
        var camera = rig.Camera; var playback = rig.Playback;
        var blocked = new TaskCompletionSource(); var observed = new List<WorldCueKind>();
        var definition = Timeline() with
        {
            World = [new(0, WorldCueKind.NewTimeline), new(0.5, WorldCueKind.FlowTo, 17.5, 60), new(2, WorldCueKind.SetFlow, Rate: 1)],
            Masks = [new(0, 1, MaskKind.Veil)],
        };
        var done = playback.Play(definition, Start(camera.Pose, default, 70), new CinematicSpawnMapStub(), new TestWorldModel(), AppState.Entering,
            (cue, skipped) => { observed.Add(cue.Cue); return cue.Cue == WorldCueKind.NewTimeline ? blocked.Task : Task.CompletedTask; });
        playback.Tick(0.1);
        var finish = playback.FinishWorld();
        if (finish.IsCompleted || observed.Count != 1) throw new InvalidOperationException("Skip raced the in-flight world restore.");
        blocked.SetResult(); await finish; await done;
        if (!observed.SequenceEqual([WorldCueKind.NewTimeline, WorldCueKind.FlowTo, WorldCueKind.SetFlow])) throw new InvalidOperationException("Skip lost or duplicated a world cue.");
        await playback.FinishWorld();
        if (observed.Count != 3) throw new InvalidOperationException("Repeated skip re-executed world cues.");
        playback.Clear();
    }
    [TestCase]
    public void VisibleOrientationUsesTheShortRouteAcrossNorth()
    {
        if (Math.Abs(CinematicPoseMath.AngleDelta(179, -179) - 2) > 0.0001f || Math.Abs(CinematicPoseMath.AngleDelta(-179, 179) + 2) > 0.0001f)
            throw new InvalidOperationException("Camera yaw takes the long rotation path.");
    }
    [TestCase]
    public void OrbitAzimuthUsesTheSameForwardConventionAsPose()
    {
        var definition = Timeline() with { Camera = [new(0, 2, CameraPath.Orbit, [], new(CameraLookMode.Target, Target: Vector3.Zero), [new(0, 60)], Orbit: new(Vector3.Zero, 10, 10, 0, 90, 5, 5))] };
        using var track = new CinematicTrack(definition, Anchors(default, default, 60), new TestWorldModel());
        if (Vector3.Distance(track.PositionAt(0), new(0, 5, -10)) > 0.001f || Vector3.Distance(track.PositionAt(2), new(10, 5, 0)) > 0.001f)
            throw new InvalidOperationException("Orbit reflected the path along Z.");
    }
    private static CameraSeg Hold(double from, double to) => new(from, to, CameraPath.Hold,
        [new(Anchor: "$current")], new(CameraLookMode.Fixed), [new(from, 70)]);
    private static CinematicDef Timeline() => new(1, "test", new("test"), CinematicKind.EnterNew, 2,
        [Hold(0, 2)], [], [], [], [], [], new(new(Anchor: "$current")));
    private static CinematicPlaybackStart Start(Pose current, Pose destination, float fov) =>
        new(default, current, destination, fov, CinematicSpawnMapStub.Map);
    private static CinematicAnchorSet Anchors(Pose current, Pose destination, float fov) =>
        new(default, current, destination, fov, new CinematicSpawnMapStub(), CinematicSpawnMapStub.Map);
}
