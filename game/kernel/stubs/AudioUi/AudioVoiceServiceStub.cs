using System.Numerics;
using Campus.Audio.Core.Stubs;
using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Kernel.Stubs.AudioUi;

/// <summary>D0 exposes bus routing but never allocates or plays a voice.</summary>
[KernelService(typeof(IAudioVoiceService), Owner = "WP-00", IsStub = true)]
public sealed class AudioVoiceServiceStub : IAudioVoiceService
{
    public int BusIndex(string name) => D0AudioNode.IndexOfBus(name);
    public AudioVoiceHandle Play(in AudioVoiceRequest request) => AudioVoiceHandle.Invalid;
    public bool IsPlaying(AudioVoiceHandle handle) => false;
    public void Update(AudioVoiceHandle handle, Vector3 position, float gainDb, float pitchScale = 1f) { }
    public void Stop(AudioVoiceHandle handle, float fadeSeconds = 0.1f) { }
}
