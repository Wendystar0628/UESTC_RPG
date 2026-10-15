using Campus.Contracts.Audio;
using Godot;
using NVector3 = System.Numerics.Vector3;

namespace Campus.Audio.Core;

/// <summary>Bounded reusable players. Handles are generations, never pool indices.</summary>
internal sealed class AudioVoicePool(Node parent, AudioClipLibrary library, AudioMix mix)
{
    private const int SpatialCapacity = 32;
    private const int FlatCapacity = 16;
    private readonly Voice[] _voices = new Voice[SpatialCapacity + FlatCapacity];
    private long _nextHandle;

    public void Initialize()
    {
        for (var i = 0; i < _voices.Length; i++)
        {
            var voice = new Voice();
            if (i < SpatialCapacity)
            {
                voice.Spatial = new AudioStreamPlayer3D
                {
                    Name = "Voice3D" + i,
                    VolumeDb = AudioMix.SilenceDb,
                    AreaMask = 0,
                    DopplerTracking = AudioStreamPlayer3D.DopplerTrackingEnum.Disabled,
                    AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance,
                    AttenuationFilterCutoffHz = 20500f,
                    MaxPolyphony = 1
                };
                parent.AddChild(voice.Spatial);
            }
            else
            {
                voice.Flat = new AudioStreamPlayer { Name = "Voice2D" + i, VolumeDb = AudioMix.SilenceDb, MaxPolyphony = 1 };
                parent.AddChild(voice.Flat);
            }
            _voices[i] = voice;
        }
    }

    public AudioVoiceHandle Play(in AudioVoiceRequest request)
    {
        Validate(request);
        var begin = request.Spatial ? 0 : SpatialCapacity;
        var end = request.Spatial ? SpatialCapacity : _voices.Length;
        Voice? slot = null;
        for (var i = begin; i < end; i++)
            if (_voices[i].Handle == 0) { slot = _voices[i]; break; }
        // Pool exhaustion is an explicit rejected request; never cut an active loop to reuse its bus.
        if (slot is null) return AudioVoiceHandle.Invalid;
        var stream = library.Load(request.ResourcePath, request.Loop);
        if (stream is null) return AudioVoiceHandle.Invalid;
        if (request.Spatial && stream is AudioStreamWav { Stereo: true })
            throw new ArgumentException("Spatial audio requires a mono asset.", nameof(request));
        slot.Handle = checked(++_nextHandle);
        slot.Group = request.Group; slot.GainDb = request.GainDb; slot.MapScoped = request.MapScoped;
        slot.Stopping = false; slot.StopGain = 1f; slot.StopRemaining = 0f; slot.Age = 0f;
        var db = AudioMix.ToDb(mix.Gain(request.Group) * AudioMix.FromDb(request.GainDb));
        var bus = AudioBusTree.Layout[request.BusIndex].Name;
        if (slot.Spatial is { } spatial)
        {
            spatial.Bus = bus; spatial.Stream = stream; spatial.GlobalPosition = Convert(request.Position);
            spatial.UnitSize = request.UnitSize; spatial.MaxDistance = request.MaxDistance;
            spatial.PanningStrength = request.PanningStrength; spatial.AreaMask = request.AreaMask;
            spatial.PitchScale = request.PitchScale; spatial.VolumeDb = db;
            spatial.Play();
        }
        else if (slot.Flat is { } flat)
        {
            flat.Bus = bus; flat.Stream = stream; flat.PitchScale = request.PitchScale; flat.VolumeDb = db;
            flat.Play();
        }
        return new(slot.Handle);
    }

    private static void Validate(in AudioVoiceRequest request)
    {
        AudioMix.GroupIndex(request.Group);
        if (request.Group == AudioGroup.Master || request.BusIndex <= 0 || request.BusIndex >= AudioBusTree.Layout.Length)
            throw new ArgumentException("Voice requires a concrete audio group and destination bus.", nameof(request));
        var bus = AudioBusTree.Layout[request.BusIndex].Name;
        if ((request.Group == AudioGroup.Ui && bus != AudioBuses.Ui) ||
            (request.Group == AudioGroup.Music && bus != AudioBuses.Music) ||
            ((request.Group is AudioGroup.Ambience or AudioGroup.Foley) && (bus is AudioBuses.Ui or AudioBuses.Music)))
            throw new ArgumentException("Voice group does not match bus routing.", nameof(request));
        ValidateUpdate(request.Position, request.GainDb, request.PitchScale);
        if (!float.IsFinite(request.UnitSize) || request.UnitSize <= 0 || !float.IsFinite(request.MaxDistance) || request.MaxDistance <= 0 ||
            !float.IsFinite(request.PanningStrength) || request.PanningStrength is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(request));
    }

    private static void ValidateUpdate(NVector3 position, float gainDb, float pitch)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z) ||
            !float.IsFinite(gainDb) || gainDb is < -80 or > 12 || !float.IsFinite(pitch) || pitch is < .25f or > 4f)
            throw new ArgumentOutOfRangeException(nameof(gainDb));
    }

    public bool IsPlaying(AudioVoiceHandle handle) => Find(handle) is { } voice && voice.Playing;

    public void Update(AudioVoiceHandle handle, NVector3 position, float db, float pitch)
    {
        if (Find(handle) is not { } voice) return;
        ValidateUpdate(position, db, pitch);
        voice.GainDb = db;
        if (voice.Spatial is { } spatial) { spatial.GlobalPosition = Convert(position); spatial.PitchScale = pitch; }
        else if (voice.Flat is { } flat) flat.PitchScale = pitch;
    }

    public void Stop(AudioVoiceHandle handle, float seconds)
    {
        if (Find(handle) is not { } voice) return;
        if (!float.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        voice.Stopping = true; voice.StopRemaining = seconds;
        if (seconds == 0f) voice.Clear();
    }

    public void StopMapVoices()
    { foreach (var voice in _voices) if (voice.Handle != 0 && voice.MapScoped) voice.Clear(); }

    public (int Active, int Spatial) Step(float dt)
    {
        var active = 0; var spatialCount = 0;
        foreach (var voice in _voices)
        {
            if (voice.Handle == 0) continue;
            voice.Age += dt;
            if (voice.Age > .1f && !voice.Playing) { voice.Clear(); continue; }
            if (voice.Stopping)
            {
                if (dt >= voice.StopRemaining) { voice.Clear(); continue; }
                voice.StopGain *= 1f - dt / voice.StopRemaining;
                voice.StopRemaining -= dt;
            }
            var db = AudioMix.ToDb(mix.Gain(voice.Group) * AudioMix.FromDb(voice.GainDb) * voice.StopGain);
            if (voice.Spatial is { } player) { player.VolumeDb = db; spatialCount++; }
            else if (voice.Flat is { } flat) flat.VolumeDb = db;
            active++;
        }
        return (active, spatialCount);
    }

    private Voice? Find(AudioVoiceHandle handle)
    {
        if (handle.Value <= 0) return null;
        foreach (var voice in _voices) if (voice.Handle == handle.Value) return voice;
        return null;
    }

    private static Vector3 Convert(NVector3 p) => new(p.X, p.Y, p.Z);

    private sealed class Voice
    {
        public AudioStreamPlayer3D? Spatial;
        public AudioStreamPlayer? Flat;
        public long Handle;
        public AudioGroup Group;
        public float GainDb, StopGain, StopRemaining, Age;
        public bool MapScoped, Stopping;
        public bool Playing => Spatial?.Playing ?? Flat?.Playing ?? false;
        public void Clear()
        {
            Spatial?.Stop(); Flat?.Stop();
            if (Spatial is not null) Spatial.Stream = null;
            if (Flat is not null) Flat.Stream = null;
            Handle = 0;
        }
    }
}
