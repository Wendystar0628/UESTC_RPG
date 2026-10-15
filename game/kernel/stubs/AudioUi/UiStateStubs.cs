using System.Numerics;
using Campus.Contracts;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace Campus.Kernel.Stubs.AudioUi;

[KernelService(typeof(IMotionSettings), Owner = "WP-00", IsStub = true)]
public sealed class MotionSettingsStub : IMotionSettings
{
    public bool ReduceMotion { get; private set; } = DisplayServer.AccessibilityShouldReduceAnimation() > 0;
    public event Action? Changed;

    /// <summary>Call on the main thread so a Windows accessibility change is reflected in D0.</summary>
    public void Refresh()
    {
        var next = DisplayServer.AccessibilityShouldReduceAnimation() > 0;
        if (next == ReduceMotion) return;
        ReduceMotion = next;
        Changed?.Invoke();
    }
}

[KernelService(typeof(IPaletteProbe), Owner = "WP-00", IsStub = true)]
public sealed class PaletteProbeStub : IPaletteProbe
{
    private static readonly Vector3[] AutumnHorizon =
    [
        new(.12f, .16f, .25f), new(.11f, .15f, .24f), new(.11f, .15f, .24f),
        new(.12f, .16f, .25f), new(.18f, .22f, .32f), new(.37f, .38f, .47f),
        new(.77f, .64f, .56f), new(.89f, .78f, .66f), new(.76f, .83f, .87f),
        new(.73f, .83f, .90f), new(.74f, .84f, .91f), new(.77f, .86f, .92f),
        new(.80f, .87f, .92f), new(.81f, .87f, .91f), new(.83f, .86f, .88f),
        new(.88f, .85f, .80f), new(.94f, .82f, .68f), new(.96f, .75f, .56f),
        new(.74f, .58f, .57f), new(.39f, .39f, .52f), new(.23f, .28f, .42f),
        new(.17f, .22f, .34f), new(.14f, .19f, .29f), new(.12f, .17f, .26f)
    ];

    public void SampleHorizon(Season season, WeatherVec weather, Span<Vector3> srgb24)
    {
        if (srgb24.Length != 24) throw new ArgumentException("Exactly 24 hourly samples are required.", nameof(srgb24));
        AutumnHorizon.AsSpan().CopyTo(srgb24);
    }
}

[KernelService(typeof(ILightningFeed), Owner = "WP-00", IsStub = true)]
public sealed class LightningFeedStub : ILightningFeed
{
    public event Action<LightningFlash>? Flash { add { } remove { } }
}

[KernelService(typeof(IChannelReader), Owner = "WP-00", IsStub = true)]
public sealed class ChannelReaderStub : IChannelReader
{
    public float Value(ChannelId id) => 0f;
}
