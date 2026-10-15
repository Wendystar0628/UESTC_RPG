using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Kernel.Stubs.AudioUi;

/// <summary>D0 silent audio service. Playback is supplied by WP-53.</summary>
[KernelService(typeof(IAudioService), Owner = "WP-00", IsStub = true)]
public sealed class AudioServiceStub : IAudioService
{
    public AudioSettings Settings { get; private set; } = new();
    public event Action<AudioSettings>? SettingsChanged;
    public AudioSnapshot Snapshot => new(0, 0, 0, 0, 0, -80f, 0);

    public void ApplySettings(AudioSettings next)
    {
        ArgumentNullException.ThrowIfNull(next);
        if (next == Settings) return;
        Settings = next;
        SettingsChanged?.Invoke(next);
    }

    public void PlayUi(UiCue cue) { }
    public void SetMusic(MusicCue cue, float fadeSeconds = 1.5f) { }
    public void Cine(string cue, float seconds, float db = 0f) { }
    public void Duck(DuckReason reason, float db, float attackSeconds, float releaseSeconds) { }
    public void ReleaseDuck(DuckReason reason) { }
}

[KernelService(typeof(IAmbienceService), Owner = "WP-00", IsStub = true)]
public sealed class AmbienceServiceStub : IAmbienceService
{
    private int _nextHandle;
    public AmbienceSnapshot Now => default;
    public void Cue(in AmbienceCue cue) { }
    public void CancelCues(uint group) { }
    public int AddDynamic(in AmbientEmitterDecl decl) => checked(++_nextHandle);
    public void MoveDynamic(int handle, System.Numerics.Vector3 position) { }
    public void RemoveDynamic(int handle) { }
}

[KernelService(typeof(IFoleyService), Owner = "WP-00", IsStub = true)]
public sealed class FoleyServiceStub : IFoleyService
{
    public FoleyStats Stats => default;
    public IReadOnlyList<FoleyEvent> RecentLog => Array.Empty<FoleyEvent>();
    public void Play(in FoleyEvent e) { }
    public void SetLoop(FoleyLoop loop, float level01, float param01) { }
}

[KernelService(typeof(IGroundCover), Owner = "WP-00", IsStub = true)]
public sealed class GroundCoverStub : IGroundCover
{
    public GroundCover Sample(float x, float z) => default;
}

[KernelService(typeof(IWalkTelemetry), Owner = "WP-00", IsStub = true)]
public sealed class WalkTelemetryStub : IWalkTelemetry
{
    public event Action<FootfallInfo> Footfall { add { } remove { } }
    public event Action<ulong, float> Jumped { add { } remove { } }
    public event Action<ulong, float, System.Numerics.Vector3> Landed { add { } remove { } }
    public event Action<ulong, float> StepUp { add { } remove { } }
    public event Action<ulong, float> StepDown { add { } remove { } }
    public event Action<ulong, float> Skid { add { } remove { } }
    public event Action<ContactInfo> WallContact { add { } remove { } }
    public event Action<ulong, float> WaterEnter { add { } remove { } }
    public event Action<ulong> WaterExit { add { } remove { } }
    public event Action<ulong> Sink { add { } remove { } }
    public event Action<ulong, float> TurnInPlace { add { } remove { } }
    public event Action<ulong> ModeTouchdown { add { } remove { } }
    public event Action<ulong> ModeTakeoff { add { } remove { } }
    public event Action<ulong> Teleport { add { } remove { } }
    public event Action<ulong, float> ElevatorDoor { add { } remove { } }
    public float StepPhase => 0f;
}
