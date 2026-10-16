using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Stubs.Foundation;

/// <summary>D0 has no prepared production catalog; never substitute an incomplete empty context.</summary>
[KernelService(typeof(IBakeContextSource), Owner = "WP-00", IsStub = true)]
public sealed class BakeContextSourceStub : IBakeContextSource
{
    public BuildContext GetPreparedContext(IMapContext context, string moduleId) =>
        throw new NotSupportedException("A prepared production build context requires WP-02a.");
}
