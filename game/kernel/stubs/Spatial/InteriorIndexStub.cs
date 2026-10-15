using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(IInteriorIndex), Owner = "WP-00", IsStub = true)]
public sealed class InteriorIndexStub : IInteriorIndex
{
    private InteriorIndexEntry[] _entries = [];
    public void Configure(IEnumerable<InteriorIndexEntry> entries) =>
        _entries = entries.OrderBy(e => e.Feature.Value, StringComparer.Ordinal).ToArray();
    public InteriorIndexEntry? Get(FeatureId id) => Array.Find(_entries, e => e.Feature == id);
    public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r)
    {
        if (!float.IsFinite(r) || r < 0) throw new ArgumentOutOfRangeException(nameof(r));
        float squared = r * r;
        return _entries.SelectMany(e => e.Entrances)
            .Where(e => Vector3.DistanceSquared(e.Center, p) <= squared).ToArray();
    }
}
