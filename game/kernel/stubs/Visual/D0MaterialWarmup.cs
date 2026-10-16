using Campus.Contracts;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stubs.AudioUi;
using Godot;

namespace Campus.Kernel.Stubs.Visual;

/// <summary>D0-only composition: a real opaque veil draw, production warm draws, then a sample-removal draw.</summary>
public partial class D0MaterialWarmup(MaterialWarmup warmup, VeilServiceStub veil,
    IToonMaterialProvider materials, IVeilPresentation presentation) : Node
{
    private enum Phase { Cover, Warming, Clearing, Ready, Failed }
    private Phase _phase;
    private bool _automaticReveal;
    private ulong _clearedFrame;
    private readonly TaskCompletionSource _initialReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool ReadyForReveal => _phase == Phase.Ready && materials.CapturePendingWarm().Materials.Count == 0;
    public string? Error { get; private set; }

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        veil.ConfigureMaterialReadiness(() => ReadyForReveal);
        RenderingServer.Singleton.FramePostDraw += OnFrameDrawn;
        GD.Print($"[D0][phase=material-warmup] provider={materials.GetType().FullName} presentation={presentation.GetType().FullName}");
    }

    public async Task WaitUntilReady()
    {
        await _initialReady.Task;
        _automaticReveal = true;
    }

    public override void _Process(double delta)
    {
        if (_phase == Phase.Failed) return;
        if (_phase == Phase.Ready && !ReadyForReveal)
        {
            _phase = Phase.Cover;
            GD.Print($"[D0][phase=material-warmup] event=late-pending frame={Engine.GetProcessFrames()}");
        }
        veil.Advance(delta);
        presentation.Present(new VeilFrame(veil.Opacity, veil.Content, null));
    }

    private void OnFrameDrawn()
    {
        try
        {
            switch (_phase)
            {
                case Phase.Cover:
                    if (veil.Opacity < 1f || !presentation.IsCovered) return;
                    var pending = materials.CapturePendingWarm();
                    GD.Print($"[D0][phase=material-warmup] event=cover-drawn pending={pending.Materials.Count} layouts={pending.Materials.Sum(item => item.Layouts.Count)} frame={Engine.GetProcessFrames()}");
                    warmup.Begin();
                    _phase = Phase.Warming;
                    break;
                case Phase.Warming:
                    if (warmup.Error is { } error) throw new InvalidOperationException(error);
                    if (!warmup.IsReady || !warmup.SamplesReleased) return;
                    warmup.End();
                    _clearedFrame = Engine.GetProcessFrames();
                    _phase = Phase.Clearing;
                    break;
                case Phase.Clearing:
                    // A further rendered frame contains no retiring sample nodes or borrowed mesh resources.
                    if (Engine.GetProcessFrames() <= _clearedFrame) return;
                    if (materials.CapturePendingWarm().Materials.Count > 0) { _phase = Phase.Cover; return; }
                    if (veil.Opacity < 1f || !presentation.IsCovered)
                        throw new InvalidOperationException("D0 warmup lost its viewport cover before the removal draw.");
                    _phase = Phase.Ready;
                    GD.Print($"[D0][phase=material-warmup] event=ready-after-removal-draw frame={Engine.GetProcessFrames()}");
                    _initialReady.TrySetResult();
                    // Re-read the latest explicit intent: a Cover issued during warming still belongs to its caller.
                    if (_automaticReveal && veil.WantsRevealed && !veil.IsTransitioning)
                        _ = veil.Reveal(veil.Reason, .3f);
                    break;
                case Phase.Ready:
                case Phase.Failed:
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception error)
        {
            Error = error.GetBaseException().Message;
            _phase = Phase.Failed;
            warmup.End();
            _initialReady.TrySetException(error);
            GD.PushError("[D0][phase=material-warmup] " + Error);
            GetTree().Quit(2);
        }
    }

    public override void _ExitTree()
    {
        RenderingServer.Singleton.FramePostDraw -= OnFrameDrawn;
        veil.ConfigureMaterialReadiness(null);
        warmup.End();
    }
}
