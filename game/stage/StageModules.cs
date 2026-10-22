using System.Reflection;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Registry;

namespace Campus.Stage;

/// <summary>Loads module types from the build-generated assembly list for stage registration.</summary>
internal static class StageModules
{
    internal static IEnumerable<Type> LoadModuleTypes()
    {
        foreach (var name in BuildInfo.ModuleAssemblies)
        {
            Type[] types;
            try { types = Assembly.Load(new AssemblyName(name)).GetTypes(); }
            catch (Exception error)
            { throw new AppStartupException($"Cannot load module assembly: {name}.", error); }
            foreach (var type in types) yield return type;
        }
    }
}
