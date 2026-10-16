using System.Reflection;
using Campus.Contracts;
using Campus.Kernel.Registry;

namespace Campus.Kernel.Boot;

internal static class RuntimeComposition
{
    public static object Resolve(ServiceRegistry registry, Registration entry)
    {
        if (entry.Kind != "service") return registry.Create(entry.Type);
        // Respect composition-supplied instances such as IAppExit; do not construct an unused fallback.
        var service = entry.Type.GetCustomAttributes<KernelServiceAttribute>()
            .Single(attribute => attribute.Service.FullName == entry.Id).Service;
        return registry.Get(service);
    }
}
