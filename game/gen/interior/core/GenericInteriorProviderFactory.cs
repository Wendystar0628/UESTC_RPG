using Campus.Contracts;

namespace Campus.Gen.Interior;

[InteriorProviderFactory("interior.plan.")]
public sealed class GenericInteriorProviderFactory : IInteriorProviderFactory
{
    public IInteriorProvider Create(string providerId) => new GenericInteriorProvider();
}
