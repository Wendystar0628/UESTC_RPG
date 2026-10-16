namespace Campus.Kernel.Registry;

/// <summary>Frozen production module names for pure managed inspection without engine startup.</summary>
public static class MapDeclarationCatalog
{
    private static readonly IReadOnlyList<string> Names =
        Array.AsReadOnly((string[])BuildInfo.ModuleAssemblies.Clone());

    public static IReadOnlyList<string> ModuleAssemblyNames => Names;
}
