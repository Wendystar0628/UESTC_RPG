using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Contracts.Maps;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Inspect;

/// <summary>Application-level housekeeping; map shutdown deliberately does not stop the recorder.</summary>
[KernelNode("performance-recording")]
public sealed partial class PerformanceRecordingHost : Node, IMapAware, IRegistryExitParticipant
{
    private PerformanceRecordingService _recorder = null!;
    private Task<PerfRecordingSnapshot>? _initialization;

    public override void _Ready()
    {
        _recorder = KernelServices.Get<PerformanceRecordingService>();
        _recorder.AttachViewport(GetViewport());
        _initialization = _recorder.InitializeAsync(AppBoot.Options);
    }

    public override void _Process(double delta)
    {
        _recorder.Poll();
        if (_initialization is { IsCompleted: true } initialization)
        {
            // The service receives request failures before completing this task; only observe it here.
            _ = initialization.Exception;
            _initialization = null;
        }
    }

    public void OnMapLoaded(IMapContext context) => _recorder.MapLoaded();
    public void OnMapUnloading(MapId from) => _recorder.MapUnloading();
    public void PrepareForExit() => _recorder.PrepareForExit();
    public async Task DrainForExitAsync()
    {
        if (_initialization is not { } initialization) return;
        await ((Task)initialization).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
        _initialization = null;
    }
}
