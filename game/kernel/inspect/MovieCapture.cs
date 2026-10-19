using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>Schedules world controls on Movie Maker's fixed engine frames; Godot owns all recording.</summary>
internal static class MovieCapture
{
    internal static void RequireMovieMaker()
    {
        if (string.IsNullOrWhiteSpace(Engine.GetWriteMoviePath()))
            throw new ArgumentException("--movie-frames requires Godot --write-movie and --fixed-fps before --.");
    }

    internal static async Task RunAsync(Node host, CaptureBootOptions options, Pose pose,
        Func<bool> contentReady, CancellationToken cancellationToken)
    {
        RequireMovieMaker();
        var limit = options.MovieFrames ?? throw new ArgumentException("Movie capture requires --movie-frames.");
        var tree = host.GetTree();
        var rig = Services.Get<ICameraRig>();
        var runtime = Services.Get<IMapRuntime>();
        var stream = Services.Get<IMapStreamState>();
        var loading = Services.Get<IAppLoadingPresentation>();
        var control = Services.Get<IWorldControl>();
        var world = Services.Get<IWorldState>();
        var arrival = runtime.Arrival ?? throw new InvalidOperationException("Movie capture requires a prepared destination.");
        var completion = new TaskCompletionSource();
        ulong? firstFrame = null;
        var nextEvent = 0;
        tree.ProcessFrame += BeforeProcess;
        host.TreeExiting += OnExit;
        using var cancellation = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try { await completion.Task; }
        finally
        {
            if (GodotObject.IsInstanceValid(tree)) tree.ProcessFrame -= BeforeProcess;
            if (GodotObject.IsInstanceValid(host)) host.TreeExiting -= OnExit;
        }

        void OnExit() => completion.TrySetCanceled();

        // SceneTree emits this before every node's _Process, so controls precede WorldState.Tick.
        // A fixed, fully prepared camera requests no new cells. The synchronous destination barrier
        // must still hold every frame; a changed destination is an error, never a partial recorded frame.
        void BeforeProcess()
        {
            if (completion.Task.IsCompleted) return;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var engineFrame = Engine.GetProcessFrames();
                if (firstFrame is { } first && engineFrame - first >= (ulong)limit)
                {
                    GD.Print($"[WP-01][phase=movie-complete] firstMovieFrame={first} frames={limit}");
                    completion.TrySetResult();
                    return;
                }
                if (!host.IsInsideTree() || rig.Pose != pose || !ReferenceEquals(runtime.Arrival, arrival))
                    throw new OperationCanceledException("Movie capture destination was replaced.");
                if (!arrival.Done.IsCompletedSuccessfully || !stream.DestinationReady || loading.Active || !contentReady())
                    throw new InvalidOperationException("Movie frame destination is not ready.");
                if (!RenderingServer.RenderLoopEnabled)
                    throw new InvalidOperationException("Movie capture requires the production render loop.");
                if (firstFrame is null)
                {
                    firstFrame = engineFrame;
                    // The native writer starts at frame zero and writes once at each engine iteration's end.
                    // The script removes this many loading frames before naming the delivered first frame zero.
                    GD.Print($"[WP-01][phase=movie-start] firstMovieFrame={engineFrame} frames={limit}");
                }
                var frame = checked((int)(engineFrame - firstFrame.Value));
                if (nextEvent >= options.MovieEvents.Count || options.MovieEvents[nextEvent].Frame != frame) return;
                var change = options.MovieEvents[nextEvent++];
                if (change.Season is { } season) control.SetSeason(season);
                if (change.Clock is { } clock) control.SetClock(clock);
                if (change.AutomaticWeather) control.SetWeatherAuto();
                else if (change.Weather is { } weather)
                {
                    if (!world.IsNative(weather))
                        throw new ArgumentException($"Movie event weather {weather} is unavailable in {world.SeasonTarget}.");
                    control.SetWeather(weather, change.Intensity);
                }
                GD.Print(FormattableString.Invariant($"[WP-01][phase=movie-event] frame={frame} movieFrame={engineFrame} season={change.Season} clock={change.Clock} weather={(change.AutomaticWeather ? "auto" : change.Weather?.ToString())} intensity={change.Intensity}"));
            }
            catch (Exception error) { completion.TrySetException(error); }
        }
    }
}
