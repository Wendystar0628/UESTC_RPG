using Campus.Contracts;
using Campus.Contracts.Audio;
using Godot;

namespace Campus.Audio.Core.Stubs;

/// <summary>Creates the SYS-AUDIO 3.1 bus tree only. No sources or effects are instantiated.</summary>
[KernelNode("d0-audio", Owner = "WP-00", IsStub = true)]
public partial class D0AudioNode : Node
{
    private static readonly (string Name, string? Parent)[] Buses =
    [
        (AudioBuses.Master, null),
        (AudioBuses.Music, AudioBuses.Master),
        (AudioBuses.Ui, AudioBuses.Master),
        (AudioBuses.World, AudioBuses.Master),
        (AudioBuses.Out, AudioBuses.World),
        (AudioBuses.AmbBed, AudioBuses.Out),
        (AudioBuses.AmbWeather, AudioBuses.Out),
        (AudioBuses.AmbFar, AudioBuses.Out),
        (AudioBuses.AmbTrace, AudioBuses.Out),
        (AudioBuses.AmbEmit, AudioBuses.Out),
        (AudioBuses.OutEcho, AudioBuses.Out),
        (AudioBuses.InBed, AudioBuses.World),
        (AudioBuses.InEmit, AudioBuses.World),
        (AudioBuses.NearDry, AudioBuses.World),
        (AudioBuses.SlotAEr, AudioBuses.World),
        (AudioBuses.SlotARev, AudioBuses.World),
        (AudioBuses.SlotBEr, AudioBuses.World),
        (AudioBuses.SlotBRev, AudioBuses.World),
        (AudioBuses.Air, AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Canteen) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Gym) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Pool) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Hall) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Atrium) + ".Rev", AudioBuses.World),
        (AudioBuses.ZonePrefix + nameof(AcousticClass.Auditorium) + ".Rev", AudioBuses.World)
    ];

    internal static int DryBusIndex { get; } = Array.FindIndex(Buses, bus => bus.Name == AudioBuses.NearDry);

    public override void _Ready() => InitializeBusTree();

    public static void InitializeBusTree()
    {
        AudioServer.BusCount = Buses.Length;
        for (var index = 0; index < Buses.Length; index++)
        {
            AudioServer.SetBusName(index, Buses[index].Name);
            if (Buses[index].Parent is { } parent) AudioServer.SetBusSend(index, parent);
        }
    }
}
