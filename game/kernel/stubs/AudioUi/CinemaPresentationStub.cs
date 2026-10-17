using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Cinematic;

namespace Campus.Kernel.Stubs.AudioUi;

[KernelService(typeof(ICinematicSettings), Owner = "WP-00", IsStub = true)]
public sealed class CinematicSettingsStub : ICinematicSettings
{
    public CinematicMode Mode => CinematicMode.Full;
    public event Action? Changed { add { } remove { } }
}

/// <summary>D0 bookkeeping only. Creates no real presentation or interface.</summary>
[KernelService(typeof(ICinemaPresenter), Owner = "WP-00", IsStub = true)]
public sealed class CinemaPresenterStub : ICinemaPresenter
{
    public bool Active { get; private set; }
    public void Begin(CinematicDef def, string? placeKey) => Active = true;
    public void Present(double timelineSeconds, float skipProgress, bool skipHintVisible, double waitingSeconds) { }
    public void End(CinematicEnd end, bool skipped) => Active = false;
}
