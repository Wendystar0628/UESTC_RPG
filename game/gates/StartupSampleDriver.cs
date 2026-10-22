using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Startup;
using Campus.Kernel.Frame;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Runs normal player input through Godot; the stage owns and drains the complete sample task.</summary>
internal sealed class StartupSampleDriver(IStageGateHost host)
{
    private static readonly StringName ForwardAction = "move_forward";
    private const double IdleMilliseconds = 10_000;
    private const double ForwardHoldMilliseconds = 20_000;

    public async Task Run(StartupScenario scenario)
    {
        try { await RunScenario(scenario); }
        catch (OperationCanceledException error) when (!host.Lifetime.IsCancellationRequested || error.CancellationToken != host.Lifetime)
        { throw new InvalidOperationException("Startup input sampling was cancelled outside its owning lifetime.", error); }
    }

    private async Task RunScenario(StartupScenario scenario)
    {
        switch (scenario)
        {
            case StartupScenario.Title: return;
            case StartupScenario.Immediate:
            case StartupScenario.Playing: break;
            case StartupScenario.Dwell:
                var boot = KernelServices.Get<IAppBootFlow>();
                var bake = KernelServices.Get<IBakeService>();
                var progress = (boot.MapReady.Status, boot.BackgroundReady.Status, bake.Progress);
                await Wait(() =>
                {
                    if (boot.BackgroundReady.IsCompleted) boot.BackgroundReady.GetAwaiter().GetResult();
                    return boot.NewGamePreloadReady;
                }, "New-game preload stopped making progress.", () =>
                {
                    var next = (boot.MapReady.Status, boot.BackgroundReady.Status, bake.Progress);
                    var changed = next != progress;
                    progress = next;
                    return changed;
                });
                break;
            default: throw new InvalidOperationException("Unknown startup sampling scenario.");
        }
        host.CheckActive();
        if (host.Flow.State != AppState.Title)
            throw new InvalidOperationException("Startup input requires the real title menu.");
        await Press(Key.Enter, false, "title-enter-press", "title-enter-release", NextFrame);
        await Wait(() => host.Flow.State != AppState.Title, "The normal title menu did not accept Enter.");
        if (scenario == StartupScenario.Playing) await SamplePlaying();
    }

    private async Task SamplePlaying()
    {
        var camera = KernelServices.Get<ICameraRig>();
        var walk = KernelServices.Get<IWalkController>();
        var observer = KernelServices.Get<IObserverState>();
        var input = KernelServices.Get<IInputRouter>();
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        var bake = KernelServices.Get<IBakeService>();
        var progress = (host.Flow.State, host.Ready, bake.Progress, walk.BodyPose.Position, walk.Velocity);
        await Wait(() => host.Ready && host.Flow.State == AppState.Playing &&
            observer.Mode == ViewMode.Walk && walk.Active && !walk.ExitRequested &&
            walk.Grounded && walk.CollisionReady && walk.IsSettled &&
            camera.ActiveTransit is null && !loading.Active,
            "Playing did not release a settled, collision-ready Walk controller.", () =>
            {
                var next = (host.Flow.State, host.Ready, bake.Progress, walk.BodyPose.Position, walk.Velocity);
                var changed = next != progress;
                progress = next;
                return changed;
            });
        var physicalKey = (Key)input.PhysicalKey("move_forward");
        GD.Print($"STARTUP_INPUT action=move_forward physicalKey={(int)physicalKey}");
        if (Input.IsActionPressed(ForwardAction))
            throw new InvalidOperationException("The startup sample inherited a held forward action.");
        Trace("playing-idle-start", IdleMilliseconds);
        await HoldFor(IdleMilliseconds, observer, walk);
        Trace("playing-idle-end");
        await Press(physicalKey, true, "playing-forward-start", "playing-forward-end",
            () => HoldFor(ForwardHoldMilliseconds, observer, walk), ForwardHoldMilliseconds);
        // Wait a real later frame so the controller consumes its release, rather than trusting raw action state alone.
        await NextFrame();
        var releaseProgress = (walk.BodyPose.Position, walk.Velocity);
        await Wait(() => !Input.IsActionPressed(ForwardAction) && camera.Speed < .0001f && walk.IsSettled,
            "The startup movement release did not settle the production controller.", () =>
            {
                var next = (walk.BodyPose.Position, walk.Velocity);
                var changed = next != releaseProgress;
                releaseProgress = next;
                return changed;
            });
        Trace("playing-release-observed");
    }

    private async Task HoldFor(double milliseconds, IObserverState observer, IWalkController walk)
    {
        var started = FrameClock.MonotonicMilliseconds;
        do
        {
            await NextFrame();
            if (host.Flow.State != AppState.Playing || observer.Mode != ViewMode.Walk || !walk.Active || walk.ExitRequested)
                throw new InvalidOperationException("The startup sample left the active Playing Walk controller.");
        } while (FrameClock.MonotonicMilliseconds - started < milliseconds);
    }

    private async Task Wait(Func<bool> ready, string failure, Func<bool>? madeProgress = null)
    {
        var progressAt = FrameClock.MonotonicMilliseconds;
        while (!ready())
        {
            host.CheckActive();
            if (madeProgress?.Invoke() == true) progressAt = FrameClock.MonotonicMilliseconds;
            if (FrameClock.MonotonicMilliseconds - progressAt > BootTuning.StalledSeconds * 1000)
                throw new TimeoutException(failure);
            await NextFrame();
        }
        host.CheckActive();
    }

    private async Task NextFrame()
    {
        host.CheckActive();
        await FrameAwaiter.ProcessAsync(host.Stage, host.Lifetime);
        host.CheckActive();
    }

    private static async Task Press(Key key, bool physical, string start, string end, Func<Task> whilePressed,
        double? targetMilliseconds = null)
    {
        Exception? failure = null;
        var pressed = false;
        try
        {
            Trace(start, targetMilliseconds);
            pressed = true;
            SendKey(key, true, physical);
            await whilePressed();
        }
        catch (Exception error) { failure = error; throw; }
        finally
        {
            if (pressed)
                try { SendKey(key, false, physical); Trace(end); }
                catch (Exception release) when (failure is not null)
                { throw new AggregateException("Startup input and its release both failed.", failure, release); }
        }
    }

    private static void SendKey(Key key, bool pressed, bool physical = false)
    {
        using var input = new InputEventKey { Pressed = pressed };
        if (physical) input.PhysicalKeycode = key;
        else { input.Keycode = key; input.PhysicalKeycode = key; }
        Input.ParseInputEvent(input);
    }

    private static void Trace(string phase, double? targetMilliseconds = null)
    {
        var target = targetMilliseconds is { } milliseconds
            ? FormattableString.Invariant($" targetMilliseconds={milliseconds:R}") : "";
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE {phase} {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()}{target}"));
    }
}
