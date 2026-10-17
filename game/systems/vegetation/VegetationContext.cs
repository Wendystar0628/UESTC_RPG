using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;

namespace Campus.Systems.Vegetation;

/// <summary>
/// Where the vegetation worker gets its <see cref="BuildContext"/>. When the registered <see cref="IBakeContextSource"/> is the
/// D0 stub (no prepared production catalog yet, WP-02a) the runtime takes the same inputs the kernel's map loading takes:
/// the map's frozen <see cref="MapLoadInput"/> (base files, every overlay and the source snapshot) and the registered claims,
/// then <see cref="MapWorldLoading.LoadPrepared"/> and <see cref="MapWorldLoading.CreateContext"/>. With any other source the
/// prepared context is used as is. A source that cannot answer fails explicitly; there is no fallback to another world.
/// </summary>
internal static class VegetationContext
{
    /// <summary>True when the registry's chosen <see cref="IBakeContextSource"/> is a stub (the D0 stage).</summary>
    public static bool SourceIsD0Stub(ServiceRegistry registry) => registry.Entries.Any(entry =>
        entry.Kind == "service" && entry.Id == typeof(IBakeContextSource).FullName && entry.IsStub);

    /// <summary>
    /// Main thread: decides the branch and reads whatever the branch needs. The returned function runs on the worker.
    /// <paramref name="readMapInputs"/> is called only on the D0 stub branch.
    /// </summary>
    public static Func<BuildContext> Resolve(IMapContext map, string moduleId, IBakeContextSource source, bool sourceIsD0Stub,
        PaletteCatalog palette, BuildInputSnapshot global, IWorldModelLoader loader,
        Func<(MapLoadInput Input, MapClaimSnapshot? Claims)> readMapInputs)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(readMapInputs);
        if (!sourceIsD0Stub)
        {
            var prepared = source.GetPreparedContext(map, moduleId);
            return () => prepared;
        }
        var (input, claims) = readMapInputs();
        return () =>
        {
            var loaded = MapWorldLoading.LoadPrepared(loader, input, claims);
            var effective = MapWorldLoading.CreateContext(input.Entry, loaded, input.SourceInputs);
            var ledger = claims?.Build(effective.World);
            return new BuildContext
            {
                World = effective.World,
                Map = effective.Descriptor,
                WorldSeed = effective.Descriptor.Seed,
                GlobalInputs = global,
                SourceInputs = effective.SourceInputs.Entries,
                Catalog = palette,
                ClaimedBy = ledger is null ? (_, _) => null : ledger.ClaimedBy
            };
        };
    }

}
