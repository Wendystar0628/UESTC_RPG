using System.Numerics;

namespace Campus.Contracts.Audio;

/// <summary>
/// Opaque voice identity owned by WP-53a. Zero is invalid/refused; handles are not reused for later voices.
/// A finished, stopped or unloaded voice becomes invalid and IsPlaying returns false.
/// </summary>
public readonly record struct AudioVoiceHandle(long Value)
{
    public static AudioVoiceHandle Invalid => default;
    public bool IsValid => Value > 0;
}

/// <summary>
/// A request from the ambience/foley pipelines to the audio core, not a public world-content playback API.
/// ResourcePath must name a licensed resource beneath res://assets/audio/. No Godot objects cross this boundary.
/// Group volume, master volume, ducking and background behavior are applied by WP-53a on the player.
/// </summary>
/// <param name="ResourcePath">Licensed resource path beneath res://assets/audio/.</param>
/// <param name="Group">The settings volume group, applied by the core.</param>
/// <param name="BusIndex">A cached index from BusIndex(name), ListenerRouting.DryBus or SendTap.BusIndex.
/// It remains fixed for the lifetime of the voice; each additional send needs a separate voice.</param>
/// <param name="Spatial">True for a 3D voice; false for non-spatial playback.</param>
/// <param name="Loop">Whether to loop the licensed source. The core owns and configures the playback resource.</param>
/// <param name="GainDb">Source/send gain before core-owned group/master/duck multipliers.</param>
/// <param name="PitchScale">Positive playback pitch multiplier; random selection belongs to the requesting pipeline.</param>
/// <param name="Position">World-space position in metres; ignored for non-spatial voices.</param>
/// <param name="UnitSize">Spatial attenuation reference distance in metres.</param>
/// <param name="MaxDistance">Spatial audibility limit in metres.</param>
/// <param name="PanningStrength">Spatial stereo panning strength.</param>
/// <param name="AreaMask">Spatial reverb-area mask; zero disables automatic area routing.</param>
/// <param name="Priority">Voice arbitration priority, using the existing audio declaration convention.</param>
/// <param name="MapScoped">The core stops this voice during map unload; false is for map-independent playback.</param>
public readonly record struct AudioVoiceRequest(
    string ResourcePath,
    AudioGroup Group,
    int BusIndex,
    bool Spatial = false,
    bool Loop = false,
    float GainDb = 0f,
    float PitchScale = 1f,
    Vector3 Position = default,
    float UnitSize = 12f,
    float MaxDistance = 144f,
    float PanningStrength = 0.7f,
    uint AreaMask = 0,
    byte Priority = 4,
    bool MapScoped = true);

/// <summary>
/// WP-53a's main-thread voice pool boundary, consumed by WP-53b/WP-53d.
/// Only the core creates players. World content continues to use IAudioService, IAmbienceService or IFoleyService.
/// No separate boot protocol: the audio core participates in the shared startup readiness mechanism.
/// </summary>
public interface IAudioVoiceService
{
    /// <summary>Resolve/cache named buses during initialization, never on each playback. Unknown names throw ArgumentException.</summary>
    int BusIndex(string name);

    /// <summary>Starts a voice immediately. Returns Invalid if the core is not ready or arbitration refuses the request.</summary>
    AudioVoiceHandle Play(in AudioVoiceRequest request);

    /// <summary>False for an invalid/stale handle, including after one-shot completion or map-scoped unload.</summary>
    bool IsPlaying(AudioVoiceHandle handle);

    /// <summary>Updates only a live voice's position, source/send gain and pitch; stale handles are ignored.</summary>
    void Update(AudioVoiceHandle handle, Vector3 position, float gainDb, float pitchScale = 1f);

    /// <summary>Fades out and releases a live voice; zero seconds stops immediately. Stale handles are ignored.</summary>
    void Stop(AudioVoiceHandle handle, float fadeSeconds = 0.1f);
}
