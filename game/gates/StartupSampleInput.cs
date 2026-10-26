using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Startup;
using Campus.Kernel.Frame;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Shares native input injection and stage-owned waits across startup samples.</summary>
internal sealed class StartupSampleInput(IStageGateHost host)
{
    public async Task WaitForPlayingWalk()
    {
        var camera = KernelServices.Get<ICameraRig>();
        var walk = KernelServices.Get<IWalkController>();
        var observer = KernelServices.Get<IObserverState>();
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
    }

    public async Task WaitForRelease(StringName action)
    {
        var camera = KernelServices.Get<ICameraRig>();
        var walk = KernelServices.Get<IWalkController>();
        // Consume release on a later real frame before reading the controller's speed.
        await NextFrame();
        var progress = (camera.Pose.Position, camera.Speed, walk.Velocity);
        await Wait(() => !Input.IsActionPressed(action) && camera.Speed < .0001f && (!walk.Active || walk.IsSettled),
            "The startup movement release did not settle the production controller.", () =>
            {
                var next = (camera.Pose.Position, camera.Speed, walk.Velocity);
                var changed = next != progress;
                progress = next;
                return changed;
            });
    }

    public async Task Wait(Func<bool> ready, string failure, Func<bool>? madeProgress = null)
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

    public async Task NextFrame()
    {
        host.CheckActive();
        await FrameAwaiter.ProcessAsync(host.Stage, host.Lifetime);
        host.CheckActive();
    }

    public async Task Press(Key key, bool physical, Func<Task> whilePressed, Func<Task>? afterRelease = null)
    {
        Exception? failure = null;
        try
        {
            SendKey(key, true, physical);
            await whilePressed();
        }
        catch (Exception error) { failure = error; throw; }
        finally
        {
            try
            {
                SendKey(key, false, physical);
                // A cancelled stage still receives key-up; its shutdown owns controller teardown.
                if (!host.Lifetime.IsCancellationRequested && afterRelease is not null) await afterRelease();
            }
            catch (Exception release) when (failure is not null)
            { throw new AggregateException("Startup input and its release both failed.", failure, release); }
        }
    }

    public static void SendKey(Key key, bool pressed, bool physical)
    {
        using var input = new InputEventKey { Pressed = pressed, PhysicalKeycode = key };
        if (!physical) input.Keycode = key;
        Input.ParseInputEvent(input);
    }

    public async Task ReleaseKeys(Key[] keys, bool[] held, Exception? movementFailure, Func<Task> afterRelease)
    {
        try
        {
            for (var index = 0; index < held.Length; index++)
                if (held[index]) { SendKey(keys[index], false, true); held[index] = false; }
            if (host.Lifetime.IsCancellationRequested) return;
            await NextFrame();
            await afterRelease();
        }
        catch (Exception release) when (movementFailure is not null)
        { throw new AggregateException("Authored route and input release both failed.", movementFailure, release); }
    }
}
