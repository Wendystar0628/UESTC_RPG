using Campus.Contracts;
using Campus.Contracts.Content;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Bake;

[KernelService(typeof(IMapContentSource))]
public sealed partial class BakeService : IMapContentSource
{
    public T GetModule<T>(IMapContext context, string moduleId) where T : class, IContentModule
    {
        ArgumentNullException.ThrowIfNull(context);
        lock (_sync)
        {
            if (!_contexts.TryGetValue(context.Descriptor.Id, out var map))
                throw new InvalidOperationException("The requested map has no prepared bake context.");
            // Reuse the same preparation/identity checks as worker context publication.
            _ = map.GetPreparedContext(context, moduleId);
            return map.Modules[moduleId].Value as T ??
                throw new InvalidOperationException($"Content module '{moduleId}' does not provide {typeof(T).Name}.");
        }
    }
}
