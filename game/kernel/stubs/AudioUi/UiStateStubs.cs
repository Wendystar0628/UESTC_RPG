using Campus.Contracts;

namespace Campus.Kernel.Stubs.AudioUi;

[KernelService(typeof(ILightningFeed), IsStub = true)]
public sealed class LightningFeedStub : ILightningFeed
{
    public event Action<LightningFlash>? Flash { add { } remove { } }
}
