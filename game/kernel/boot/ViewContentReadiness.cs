using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Boot;

/// <summary>Snapshots the completed stage's producers once; polling reads their real mounted/uploaded state.</summary>
public sealed class ViewContentReadiness
{
    private readonly IViewContentReadiness[] _producers;
    // The lifetime owner polls this same snapshot; readiness queries do not collect failures.
    internal ReadOnlySpan<IViewContentReadiness> Producers => _producers;
    private ViewContentReadiness(IViewContentReadiness[] producers) => _producers = producers;

    public static ViewContentReadiness Capture(Node stage) => new(Descendants(stage).OfType<IViewContentReadiness>().ToArray());

    /// <summary>Production arrival already owns complete destination visuals and collision.</summary>
    public async Task WaitForReadyAsync(Node host, IMapArrival arrival, Func<bool> contentReady,
        CancellationToken cancellationToken = default)
    {
        var runtime = KernelServices.Get<IMapRuntime>();
        var rig = KernelServices.Get<ICameraRig>();
        var world = KernelServices.Get<IWorldState>();
        var weather = KernelServices.Get<IWeatherProcess>();
        var bake = KernelServices.Get<IBakeService>();
        using var cancellation = cancellationToken.Register(arrival.Cancel);
        var progress = (bake.Progress, arrival.Done.Status);
        var timeout = TimeSpan.FromSeconds(BootTuning.StalledSeconds);
        using var deadline = new CancellationTokenSource(timeout);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        var content = false;
        var producersReady = false;
        var scrubbing = false;
        var transition = WorldTransition.None;
        var weatherTransitioning = false;
        Pose? lastPose = null;
        var completed = 0;
        var total = 0;
        try
        {
            while (true)
            {
                await FrameAwaiter.ProcessAsync(host, lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                if (!host.IsInsideTree()) throw new OperationCanceledException("Stage left the tree during arrival.");
                if (runtime.Failure is { } failure)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
                if (arrival.Done.IsCompleted) await arrival.Done;
                if (!ReferenceEquals(runtime.Arrival, arrival)) throw new OperationCanceledException("Stage arrival was replaced.");
                content = contentReady();
                lastPose = rig.Pose;
                producersReady = arrival.Done.IsCompletedSuccessfully && content && IsReady(lastPose.Value);
                scrubbing = world.Scrubbing;
                transition = world.Transition;
                weatherTransitioning = weather.Transitioning;
                completed = arrival.Completed;
                total = arrival.Total;
                if (producersReady && !scrubbing && transition == WorldTransition.None && !weatherTransitioning) return;
                var nextProgress = (bake.Progress, arrival.Done.Status);
                if (nextProgress != progress) deadline.CancelAfter(timeout);
                progress = nextProgress;
            }
        }
        catch (OperationCanceledException error) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Stage arrival stalled; arrival={progress.Item2},completed={completed},total={total},bake={progress.Item1}," +
                $"content={content},producersReady={producersReady},scrubbing={scrubbing},worldTransition={transition}," +
                $"weatherTransitioning={weatherTransitioning},pose={lastPose}", error);
        }
    }

    public void PrepareCapture(Pose pose)
    {
        foreach (var producer in _producers.OfType<IViewCapturePreparation>()) producer.PrepareCapture(pose);
    }

    public bool IsReady(Pose pose)
    {
        var ready = true;
        foreach (var producer in _producers)
            ready &= producer.IsReady(pose);
        return ready;
    }

    public string Describe(Pose pose) => string.Join(',', _producers.Select(producer =>
        producer.GetType().Name + "=" + producer.IsReady(pose)));

    private static IEnumerable<Node> Descendants(Node host)
    {
        yield return host;
        foreach (var child in host.GetChildren())
            foreach (var descendant in Descendants(child)) yield return descendant;
    }
}
