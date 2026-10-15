using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Cinematic;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(ILightInventory), Owner = "WP-00", IsStub = true)]
public sealed class LightInventoryStub : ILightInventory
{
    public long Rev => 0;
    public IReadOnlyList<LightEmitter> Lamps => [];
    public IReadOnlyList<WindowFacadeDecl> Facades => [];
    public IReadOnlyList<WindowDecl> Windows => [];
}

[KernelService(typeof(ILightChannelSink), Owner = "WP-00", IsStub = true)]
public sealed class LightChannelSinkStub : ILightChannelSink
{
    public void Submit(ReadOnlySpan<Vector2> lDelta)
    {
        if (lDelta.Length != LightChannelCatalog.Count)
            throw new ArgumentException($"Expected {LightChannelCatalog.Count} light-channel pairs.", nameof(lDelta));
        for (var slot = 0; slot < lDelta.Length; slot++)
            if (!float.IsFinite(lDelta[slot].X) || !float.IsFinite(lDelta[slot].Y))
                throw new ArgumentException($"Light-channel slot {slot} must contain finite values.", nameof(lDelta));
        // The real WP-03 service owns GPU upload; the D0 stub only enforces the input contract.
    }
}

[KernelService(typeof(ILampCueHook), Owner = "WP-00", IsStub = true)]
public sealed class LampCueHookStub : ILampCueHook
{
    public bool Begin(string cueRef, float targetSunAltDeg) => false;
    public void Seek(double timelineSeconds) { }
    public void End() { }
}
