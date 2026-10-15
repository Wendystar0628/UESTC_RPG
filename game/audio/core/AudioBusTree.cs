using Campus.Contracts.Audio;
using Godot;

namespace Campus.Audio.Core;

internal sealed class AudioBusTree
{
    internal static readonly (string Name, string? Parent)[] Layout =
    [
        (AudioBuses.Master, null), (AudioBuses.Music, AudioBuses.Master), (AudioBuses.Ui, AudioBuses.Master),
        (AudioBuses.World, AudioBuses.Master), (AudioBuses.Out, AudioBuses.World),
        (AudioBuses.AmbBed, AudioBuses.Out), (AudioBuses.AmbWeather, AudioBuses.Out),
        (AudioBuses.AmbFar, AudioBuses.Out), (AudioBuses.AmbTrace, AudioBuses.Out),
        (AudioBuses.AmbEmit, AudioBuses.Out), (AudioBuses.OutEcho, AudioBuses.Out),
        (AudioBuses.InBed, AudioBuses.World), (AudioBuses.InEmit, AudioBuses.World),
        (AudioBuses.NearDry, AudioBuses.World), (AudioBuses.SlotAEr, AudioBuses.World),
        (AudioBuses.SlotARev, AudioBuses.World), (AudioBuses.SlotBEr, AudioBuses.World),
        (AudioBuses.SlotBRev, AudioBuses.World), (AudioBuses.Air, AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Canteen) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Gym) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Pool) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Hall) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Atrium) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Auditorium) + ".Rev", AudioBuses.World)
    ];
    private AudioEffectStereoEnhance? _stereo;

    public static int Index(string name)
    {
        var index = Array.FindIndex(Layout, bus => bus.Name == name);
        return index >= 0 ? index : throw new ArgumentException("Unknown audio bus: " + name, nameof(name));
    }

    public void Initialize(bool limiter)
    {
        AudioServer.BusCount = Layout.Length;
        for (var i = 0; i < Layout.Length; i++)
        {
            while (AudioServer.GetBusEffectCount(i) > 0) AudioServer.RemoveBusEffect(i, 0);
            AudioServer.SetBusName(i, Layout[i].Name);
            if (Layout[i].Parent is { } parent) AudioServer.SetBusSend(i, parent);
        }
        // Neutral until Mono is selected. No effect enable/disable during active playback.
        _stereo = new AudioEffectStereoEnhance { PanPullout = 1f, TimePulloutMs = 0f, Surround = 0f };
        AudioServer.AddBusEffect(0, _stereo);
        if (limiter) AudioServer.AddBusEffect(0, new AudioEffectHardLimiter { CeilingDb = -1f });
        AudioServer.AddBusEffect(Index(AudioBuses.World), new AudioEffectLowPassFilter { CutoffHz = 20500f });
        AudioServer.AddBusEffect(Index(AudioBuses.Out), new AudioEffectLowPassFilter { CutoffHz = 20500f });
        AudioServer.AddBusEffect(Index(AudioBuses.AmbTrace), new AudioEffectLowPassFilter { CutoffHz = 2500f });
        foreach (var name in new[] { AudioBuses.SlotAEr, AudioBuses.SlotBEr })
            AudioServer.AddBusEffect(Index(name), new AudioEffectDelay { Dry = 0f, Tap1Active = true, Tap1DelayMs = 18f, Tap1LevelDb = -12f, Tap2Active = false });
        for (var i = 0; i < Layout.Length; i++)
            if (Layout[i].Name.EndsWith(".Rev", StringComparison.Ordinal))
                AudioServer.AddBusEffect(i, new AudioEffectReverb { Dry = 0f, Wet = 1f });
        Validate();
    }

    public void Mono(bool mono) { if (_stereo is not null) _stereo.PanPullout = mono ? 0f : 1f; }

    public static void Validate()
    {
        if (AudioServer.BusCount != Layout.Length) throw new InvalidOperationException("Audio bus count differs from contract.");
        for (var i = 0; i < Layout.Length; i++)
        {
            if (AudioServer.GetBusName(i) != Layout[i].Name ||
                Layout[i].Parent is { } parent && (Index(parent) >= i || AudioServer.GetBusSend(i).ToString() != parent))
                throw new InvalidOperationException("Audio bus routing differs from contract: " + Layout[i].Name);
        }
    }
}
