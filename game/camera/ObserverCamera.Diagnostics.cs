using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;

namespace Campus.Camera;

public sealed record InputLayerDiagnostic(string Owner, int Priority);
public sealed record InputLockDiagnostic(InputLockReason Reason, int Count);
public sealed record CameraInputDiagnostics(
    AppState? AppState,
    ViewMode Mode,
    bool IsPlaying,
    bool InterfaceMode,
    bool InputLocked,
    IReadOnlyList<InputLayerDiagnostic> Layers,
    IReadOnlyList<InputLockDiagnostic> Locks,
    IReadOnlyList<string> HeldActions,
    Vector2 Move,
    float Vertical,
    bool Sprint,
    MapId? Map,
    bool BoundsReady,
    bool PoseLegal,
    Pose Pose,
    float Speed,
    float EdgeFog,
    TransitKind? ActiveTransit,
    long InputEvents,
    long UnhandledInputEvents)
{
    public string? LastRoutedAction { get; init; }
    public bool LastRoutedPressed { get; init; }
    public string? LastConsumer { get; init; }
}

public partial class ObserverCamera
{
    private long _inputEventCount;
    private long _unhandledInputEventCount;
    private string? _lastRoutedAction;
    private bool _lastRoutedPressed;
    private string? _lastInputConsumer;

    /// <summary>On-demand engineering evidence; never changes input, locks, pose, or world state.</summary>
    public CameraInputDiagnostics CaptureInputDiagnostics() => new(
        _flow?.State, Mode, IsPlaying, InterfaceMode, _locks.Locked,
        _layers.Select(layer => new InputLayerDiagnostic(layer.Owner, layer.Priority)).ToArray(),
        Enum.GetValues<InputLockReason>().Select(reason => new InputLockDiagnostic(reason, _locks.Count(reason))).ToArray(),
        _input.CaptureHeldActions(), _input.Move, _input.Vertical, _input.Sprint,
        _map?.Descriptor.Id, _bounds is not null, IsLegal(Pose), Pose, Speed, EdgeFog,
        ActiveTransit?.Kind, _inputEventCount, _unhandledInputEventCount)
    {
        LastRoutedAction = _lastRoutedAction,
        LastRoutedPressed = _lastRoutedPressed,
        LastConsumer = _lastInputConsumer
    };
}
