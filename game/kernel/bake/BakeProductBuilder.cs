using Campus.Gen.BakeInput;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using static Campus.Kernel.Bake.BakeService;
using System.Globalization;

namespace Campus.Kernel.Bake;

internal sealed class BakeProductBuilder
{
    private readonly BakeOptions _options;
    private readonly BakeCacheStore _cache;
    private readonly TerrainCacheService _terrain;
    private readonly BakeTerrainPreparation _terrainPreparation;
    private readonly object _clearSync = new();
    private long _cacheGeneration;
    private Task? _clearing;
    private readonly BakeQueue _queue;

    internal BakeProductBuilder(BakeOptions options, TerrainCacheService terrain, BakeQueue queue)
    { _options = options; _terrain = terrain; _cache = terrain.Store; _terrainPreparation = new(terrain); _queue = queue; }

    internal void Clear()
    {
        lock (_clearSync)
        {
            if (_clearing is { IsCompleted: false }) return;
            _clearing?.GetAwaiter().GetResult();
            Interlocked.Increment(ref _cacheGeneration);
            _clearing = ObserveClear(Task.Run(_cache.Clear));
        }
    }

    private async Task ObserveClear(Task clearing)
    {
        try { await clearing.ConfigureAwait(false); }
        catch (Exception error) { _queue.RecordFailure(error); throw; }
    }

    internal void Drain()
    {
        Task? clearing;
        lock (_clearSync) clearing = _clearing;
        clearing?.GetAwaiter().GetResult();
    }

    internal object Build(MapBuild map, BakeKey key)
    {
        var cancellation = map.Lifetime.Token;
        cancellation.ThrowIfCancellationRequested();
        var generation = Interlocked.Read(ref _cacheGeneration);
        Task? clearing;
        lock (_clearSync) clearing = _clearing;
        clearing?.GetAwaiter().GetResult();
        cancellation.ThrowIfCancellationRequested();
        _ = map.Data.Value;
        cancellation.ThrowIfCancellationRequested();
        var module = map.Content.Modules.GetValueOrDefault(key.Module);
        var interior = map.Content.Interiors.GetValueOrDefault(key.Module);
        var matches = map.Content.Producers.Where(p => p.Value.CanBuild(key)).ToArray();
        if (matches.Length > 1 || (module is not null || interior is not null) && matches.Length != 0)
            throw new InvalidDataException("More than one producer claims this bake key.");
        if (module is null && interior is null && matches.Length == 0) throw new InvalidDataException("No bake producer is registered for this key.");
        var payloadClass = module is not null || interior is not null ? BakePayloadClass.Tile : matches[0].Value.PayloadClass;
        var context = map.Content.GetPreparedBuildContext(module is not null ? module.Value :
            interior is not null ? interior.Value : matches[0].Value);
        var sources = map.Inputs.SourceIndex(context);
        var inputHash = sources.TryProduct(key, out var preparedHash) ? preparedHash : map.Inputs.ProductInputHash(_terrain.CodeFingerprints, map, context, key);
        sources.RememberProduct(key, inputHash);
        var identity = BakeInputFingerprint.Identity(key, inputHash);
        _terrainPreparation.PrepareProduct(map, key);
        if (!_options.Live)
        {
            var cached = _cache.TryReadParts(key, payloadClass, payload =>
            {
                var product = Decode(payloadClass, identity, key, payload);
                RequireInteriorProduct(map, key, product);
                BakeAuxiliaryInputs.RequireProduct(map, key, product);
                return product;
            }, cancellation);
            if (cached is not null)
            {
                map.PipelineStatistics?.Add(cached, map.Content.Geometry);
                return cached;
            }
            _terrain.RequireMiss(key);
        }
        // Only a cache miss needs the evaluator; cached products keep the published read-only world.
        var buildContext = map.Content.CreateAssetContext(context);
        var built = module is not null ? map.Content.Geometry!.Build(key, buildContext, module.Value, diagnostics => map.Collect(key.Module, "geometry", diagnostics)) :
            interior is not null ? BuildInterior(map, key, buildContext, interior.Value) : matches[0].Value.Build(key, buildContext, inputHash);
        cancellation.ThrowIfCancellationRequested();
        var bytes = Encode(payloadClass, identity, built, cancellation);
        cancellation.ThrowIfCancellationRequested();
        // Validate exactly the bytes about to be written before publishing Ready; malformed producer output is terminal.
        var validated = Decode(payloadClass, identity, key, bytes);
        cancellation.ThrowIfCancellationRequested();
        RequireInteriorProduct(map, key, validated);
        BakeAuxiliaryInputs.RequireProduct(map, key, validated);
        map.PipelineStatistics?.Add(validated, map.Content.Geometry);
        if (!_options.Live && generation == Interlocked.Read(ref _cacheGeneration))
        {
            _cache.WriteParts(key, payloadClass, bytes, cancellation);
        }
        return validated;
    }

    private IReadOnlyList<byte[]> Encode(BakePayloadClass payloadClass, string identity, object product, CancellationToken cancellation) =>
        BakeProductCodec.EncodeParts(payloadClass, identity, product, bytes => _cache.Fits(payloadClass, bytes, cancellation));
    private static object Decode(BakePayloadClass payloadClass, string identity, BakeKey key, IReadOnlyList<byte[]> parts) => BakeProductCodec.DecodeParts(payloadClass, identity, key, parts);
    private static void RequireInteriorProduct(MapBuild map, BakeKey key, object product)
    {
        if (map.Content.Interiors.ContainsKey(key.Module) && (product is not TileBundle tile || tile.Stats.Surfaces == 0 || tile.Stats.Triangles == 0))
            throw new InvalidDataException($"Open interior product is empty: {key.Map.Value}/{key.Module}/{key.Detail}.");
    }
    private static TileBundle BuildInterior(MapBuild map, BakeKey key, BuildContext context, IInteriorProvider provider)
    {
        var fields = key.Detail.Split('/');
        if (fields.Length != 3 || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var storey))
            throw new ArgumentException("Invalid interior bake detail.");
        var feature = new FeatureId(fields[0]);
        var binding = map.Data.Value.InteriorBindings.SingleOrDefault(binding => "interior." + binding.ProviderId == key.Module);
        if (binding is null || !binding.FeatureIds.Contains(feature) || !provider.FeatureIds.Contains(feature))
            throw new InvalidDataException("Interior feature is not bound to this map's authoritative catalog.");
        var detail = fields[2] switch { "IF" => InteriorDetail.Full, "IS" => InteriorDetail.Shell, _ => throw new InvalidDataException("Interior detail must be IF or IS.") };
        if (!context.InteriorLayouts.TryGetValue(feature, out var layout))
            throw new InvalidDataException($"Interior layout is not prepared: {feature.Value}.");
        if (!layout.OpenStoreys.Contains(storey)) throw new InvalidDataException("Interior storey is not open.");
        var center = layout.Bounds.Center;
        var tile = TileGrid.At(center);
        return map.Content.Geometry!.BuildOutput(key, context, provider.BuildStorey(context, feature, storey, detail), tile, LodLevel.L0,
            diagnostics => map.Collect(key.Module, "interior-geometry", diagnostics, feature));
    }
}
