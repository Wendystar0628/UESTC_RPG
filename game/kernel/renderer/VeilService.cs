using Campus.Contracts;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Renderer;

[KernelService(typeof(IVeilService), Owner = "WP-01")]
[KernelService(typeof(IVeilState), Owner = "WP-01")]
[KernelNode("veil-service", Owner = "WP-01")]
public partial class VeilService : Node, IVeilService
{
    private readonly VeilTimeline _timeline = new();
    private IVeilPresentation? _presentation;
    private Texture2D? _frontispiece;
    public float Opacity => _timeline.Opacity;
    public VeilReason Reason => _timeline.Reason;
    public bool Waiting => _timeline.Waiting;
    public bool Busy => _timeline.Busy;
    public Exception? Failure => _timeline.Failure;
    public event Action<Exception>? Failed;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        ProcessMode = ProcessModeEnum.Always;
        _presentation = Services.Get<IVeilPresentation>();
        _timeline.Changed += Present;
        _timeline.Failed += error => Failed?.Invoke(error);
        Present();
    }
    public override void _Process(double delta) => _timeline.Advance(delta);
    public Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog) => _timeline.Cover(reason, seconds, content);
    public void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds) => _timeline.HoldUntil(reason, ready, maxWaitSeconds);
    public Task Reveal(VeilReason reason, float seconds) => _timeline.Reveal(reason, seconds);
    public IDisposable ObserveReadinessWait(VeilWaitKind kind = VeilWaitKind.Readiness)
    {
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Begin readiness observation on the Godot main thread.");
        return _timeline.ObserveReadinessWait(kind);
    }
    public override void _ExitTree()
    {
        _timeline.Changed -= Present;
        _timeline.StopObservingReadiness();
    }
    public void SetFrontispiece(Texture2D? texture)
    {
        _frontispiece = texture;
        Present();
    }
    private void Present() => _presentation?.Present(new VeilFrame(Opacity, _timeline.Content, _frontispiece));
}
