using Campus.Contracts;
using Campus.Contracts.Perf;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Inspect;

/// <summary>Application-level housekeeping; map shutdown deliberately does not stop the recorder.</summary>
[KernelNode("performance-recording")]
public sealed partial class PerformanceRecordingHost : Node
{
    private PerformanceRecordingService _recorder = null!;
    private Task<PerfRecordingSnapshot>? _initialization;

    public override void _Ready()
    {
        _recorder = KernelServices.Get<PerformanceRecordingService>();
        _initialization = _recorder.InitializeAsync(AppBoot.Options);
    }

    public override void _Process(double delta)
    {
        _recorder.Poll();
        if (_initialization is { IsCompleted: true })
        {
            _initialization.GetAwaiter().GetResult();
            _initialization = null;
        }
    }
}
