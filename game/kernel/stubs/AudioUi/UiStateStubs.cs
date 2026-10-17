using Campus.Contracts;

namespace Campus.Kernel.Stubs.AudioUi;

[KernelService(typeof(ILightningFeed), Owner = "WP-00", IsStub = true)]
public sealed class LightningFeedStub : ILightningFeed
{
    public event Action<LightningFlash>? Flash { add { } remove { } }
}
