using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>Schedules world controls on Movie Maker's fixed engine frames; Godot owns all recording.</summary>
internal static class MovieCapture
{
    internal sealed record Segment(ulong First, int Count, int Fps, string Pattern, string NativePath);

    internal static int RequireMovieMaker()
    {
        if (string.IsNullOrWhiteSpace(Engine.GetWriteMoviePath()))
            throw new ArgumentException("--movie-frames requires Godot --write-movie and --fixed-fps before --.");
        var arguments = OS.GetCmdlineArgs();
        var index = Array.IndexOf(arguments, "--fixed-fps");
        if (index < 0 || index + 1 >= arguments.Length ||
            !int.TryParse(arguments[index + 1], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var fps) || fps <= 0)
            throw new ArgumentException("--movie-frames requires a positive Godot --fixed-fps before --.");
        return fps;
    }

    internal static async Task<Segment> RunAsync(Node host, CaptureBootOptions options, Pose pose,
        Func<bool> contentReady, string diagnosticId, CancellationToken cancellationToken)
    {
        var fps = RequireMovieMaker();
        var nativePath = Engine.GetWriteMoviePath();
        var pattern = Path.GetExtension(nativePath).Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(nativePath) + "%08d.png" : Path.GetFileName(nativePath);
        var limit = options.MovieFrames ?? throw new ArgumentException("Movie capture requires --movie-frames.");
        var tree = host.GetTree();
        var rig = KernelServices.Get<ICameraRig>();
        var runtime = KernelServices.Get<IMapRuntime>();
        var stream = KernelServices.Get<IMapStreamState>();
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        var control = KernelServices.Get<IWorldControl>();
        var world = KernelServices.Get<IWorldState>();
        var arrival = runtime.Arrival ?? throw new InvalidOperationException("Movie capture requires a prepared destination.");
        var completion = new TaskCompletionSource<Segment>();
        ulong? firstFrame = null;
        var nextEvent = 0;
        tree.ProcessFrame += BeforeProcess;
        host.TreeExiting += OnExit;
        using var cancellation = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try { return await completion.Task; }
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
                    GD.Print($"[{diagnosticId}][phase=movie-complete] firstMovieFrame={first} frames={limit}");
                    completion.TrySetResult(new(first, limit, fps, pattern, nativePath));
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
                    // Metadata retains this native offset; event frames below remain zero-based in the clip.
                    GD.Print($"[{diagnosticId}][phase=movie-start] firstMovieFrame={engineFrame} frames={limit}");
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
                GD.Print(FormattableString.Invariant($"[{diagnosticId}][phase=movie-event] frame={frame} movieFrame={engineFrame} season={change.Season} clock={change.Clock} weather={(change.AutomaticWeather ? "auto" : change.Weather?.ToString())} intensity={change.Intensity}"));
            }
            catch (Exception error) { completion.TrySetException(error); }
        }
    }
}
