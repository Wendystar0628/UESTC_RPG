using Campus.Contracts;

namespace Campus.Gen.Interior.Shell.Box;

[InteriorProviderFactory("interior.box.")]
public sealed class BoxInteriorProviderFactory : IInteriorProviderFactory
{
    public IInteriorProvider Create(string providerId) => new BoxInteriorProvider();
}
