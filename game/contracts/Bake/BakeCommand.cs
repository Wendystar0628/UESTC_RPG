using Campus.Contracts.Startup;

namespace Campus.Contracts.Bake;

/// <summary>CPU batch entry, selected before interactive scene composition. Nonzero means a failed requested build or verification.</summary>
public interface IBakeCommand
{
    Task<int> Run(BootContext context);
}
