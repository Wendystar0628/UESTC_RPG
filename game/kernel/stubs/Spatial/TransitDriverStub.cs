using Campus.Contracts;

namespace Campus.Kernel.Stubs;

/// <summary>D0 immediate-arrival stub only. It does not evaluate a slide, arc, veil, or streaming readiness.</summary>
/// <remarks>The production camera has a parameterless constructor and resolves this driver after registration
/// in its ready phase. It must not constructor-inject ITransitDriver, which would create a dependency cycle.</remarks>
[KernelService(typeof(ITransitDriver), Owner = "WP-00", IsStub = true)]
public sealed class TransitDriverStub(ICameraRig camera) : ITransitDriver
{
    public bool IsReady => false;
    public Pose? Destination { get; private set; }

    public ITransitHandle Start(Pose from, Pose to, TransitOptions? options = null)
    {
        TransitKind kind = (options?.Prefer ?? TransitPreference.Auto) switch
        {
            TransitPreference.Auto => TransitKind.Slide,
            TransitPreference.Teleport => TransitKind.Teleport,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        Destination = to;
        try { camera.SetPose(to); }
        finally { Destination = null; }
        return new ArrivedStubHandle(kind);
    }

    private sealed class ArrivedStubHandle(TransitKind kind) : ITransitHandle
    {
        public TransitKind Kind => kind;
        public float Progress => 1;
        public Task<TransitResult> Done { get; } = Task.FromResult(TransitResult.Arrived);
        public void Cancel() { }
    }
}
