using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using Campus.Kernel.Registry;

namespace Campus.Kernel.Bake;

/// <summary>Owns layered product reads and local writes, preparing terrain before pure readers receive it.</summary>
[KernelService(typeof(TerrainCacheService))]
[KernelService(typeof(ITerrainPreparation))]



public sealed class TerrainCacheService : ITerrainPreparation, IDisposable
{
    private sealed class Session(MapId map, WorldModelPreparation preparation, TerrainFrozenData data, IPreparedTerrain terrain)
    {
        public MapId Map { get; } = map;
        public WorldModelPreparation Preparation { get; } = preparation;
        public TerrainFrozenData Data { get; } = data;
        public IPreparedTerrain Terrain { get; } = terrain;
        public object Sync { get; } = new();
        // Retain the evaluator until this world unloads; an idle bake queue is not its lifetime.
        public ITerrainAssetBuilder? Assets;
        public IWorldModel? AssetWorld;
    }
    private readonly ConcurrentDictionary<ITerrain, Session> _sessions = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentDictionary<(ITerrain Terrain, TileKey Block), Lazy<Task>> _preparing = new();
    private readonly BakeOptions _options;
    internal BakeCacheStore Store { get; }
    internal BuildCodeIndex CodeFingerprints { get; }
    public TerrainCacheService() : this(BakeOptions.FromApplication()) { }
    internal TerrainCacheService(BakeOptions options) : this(options, BuildCodeIndex.Shared) { }
    internal TerrainCacheService(BakeOptions options, BuildCodeIndex codeFingerprints)
    { _options = options; CodeFingerprints = codeFingerprints; Store = new(options.CacheRoot, packagedRoot: options.PackagedCacheRoot); }

    internal WorldModelLoadResult Load(MapId map, WorldModelPreparation preparation)
    {
        var key = new BakeKey(map, "terrain", "meta");
        var identity = TerrainStamp(preparation.SourceInputs);
        var fidelityKey = new BakeKey(map, "terrain", "fidelity");
        var fidelityIdentity = Stamp(new { BakeOptions.Abi, code = CodeFingerprints.Chain(BakeCodeChains.Shared), preparation.FidelitySourceStamp });
        var fidelity = ReadProduct<FidelityFrozenData>(fidelityKey, fidelityIdentity);
        WorldModelLoadResult? loaded = null;
        var data = ReadProduct<TerrainFrozenData>(key, identity, value => loaded = preparation.Complete(value, ResolveFidelity));
        ITerrainAssetBuilder? builder = null;
        if (data is null)
        {
            RequireMiss(key);
            builder = preparation.CreateAssets();
            data = builder.Freeze();
            WriteProduct(key, identity, data);
        }
        loaded ??= preparation.Complete(data, ResolveFidelity);
        var terrain = (IPreparedTerrain)loaded.World.Terrain;
        builder?.Attach(terrain);
        _sessions[terrain] = new(map, preparation, data, terrain) { Assets = builder };
        return loaded;

        FidelityFrozenData ResolveFidelity(ITerrain source)
        {
            if (fidelity is not null) return fidelity;
            RequireMiss(fidelityKey);
            fidelity = preparation.CreateFidelityAssets(source);
            WriteProduct(fidelityKey, fidelityIdentity, fidelity);
            return fidelity;
        }
    }

    public bool IsPrepared(IWorldModel world, Vector2 point) => world.Terrain is not IPreparedTerrain prepared || prepared.IsPrepared(point);
    public Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default)
    {
        if (context.World.Terrain is not IPreparedTerrain prepared) return Task.CompletedTask;
        var session = RequireSession(prepared);
        var tasks = new List<Task>();
        var minX = Block(region.Min.X); var minZ = Block(region.Min.Y);
        var maxX = Block(region.Max.X); var maxZ = Block(region.Max.Y);
        for (var z = minZ; z <= maxZ; z++)
            for (var x = minX; x <= maxX; x++)
            {
                var block = new TileKey(x, z);
                if (prepared.HasBlock(block)) continue;
                tasks.Add(_preparing.GetOrAdd((prepared, block), _ => new(() => Task.Run(() =>
                {
                    var key = new BakeKey(session.Map, "terrain", $"block/{block.X}/{block.Z}");
                    var stamp = TerrainStamp(session.Data.SourceInputs, new(new(block.X * TerrainRasterBlock.Edge, block.Z * TerrainRasterBlock.Edge),
                        new((block.X + 1) * TerrainRasterBlock.Edge, (block.Z + 1) * TerrainRasterBlock.Edge)));
                    TerrainRasterBlock value;
                    var cached = Read(key, stamp, TerrainCacheCodec.DecodeBlock, BakePayloadClass.Tile);
                    if (cached is not null) value = cached;
                    else
                    {
                        RequireMiss(key);
                        lock (session.Sync) value = Assets(session).BuildBlock(block, prepared);
                        Write(key, stamp, TerrainCacheCodec.EncodeBlock(value), BakePayloadClass.Tile);
                    }
                    lock (session.Sync)
                    {
                        prepared.Publish(value);
                        session.Assets?.Attach(prepared);
                    }
                }), LazyThreadSafetyMode.ExecutionAndPublication)).Value);
            }
        return Task.WhenAll(tasks).WaitAsync(cancellation);
    }

    public Task PreparePoints(IMapContext context, string product, IReadOnlyList<Vector2> points, CancellationToken cancellation = default)
    {
        if (context.World.Terrain is not IPreparedTerrain prepared || points.All(prepared.IsPrepared)) return Task.CompletedTask;
        var session = RequireSession(prepared);
        var snapshot = points.ToArray();
        return Task.Run(() =>
        {
            cancellation.ThrowIfCancellationRequested();
            var key = new BakeKey(session.Map, "terrain", "points/" + product);
            var stamp = Stamp(new { terrain = TerrainStamp(session.Data.SourceInputs), points = snapshot });
            TerrainRasterPoint[] values;
            var cached = Read(key, stamp, TerrainCacheCodec.DecodePoints, BakePayloadClass.Tile);
            if (cached is not null) values = cached;
            else
            {
                RequireMiss(key);
                lock (session.Sync) values = Assets(session).BuildPoints(snapshot);
                Write(key, stamp, TerrainCacheCodec.EncodePoints(values), BakePayloadClass.Tile);
            }
            cancellation.ThrowIfCancellationRequested();
            lock (session.Sync)
            {
                prepared.Publish(values);
                session.Assets?.Attach(prepared);
            }
        }, cancellation);
    }

    internal IWorldModel AssetWorld(IWorldModel world)
    {
        if (world.Terrain is not IPreparedTerrain) return world;
        var session = RequireSession(world.Terrain);
        lock (session.Sync)
        {
            if (session.AssetWorld is null || !ReferenceEquals(session.AssetWorld.Interiors, world.Interiors))
                session.AssetWorld = session.Preparation.WithTerrain(world, Assets(session));
            return session.AssetWorld;
        }
    }
    internal TileKey[] SaveResidentPoints(IWorldModel world, string stamp)
    {
        if (world.Terrain is not IPreparedTerrain prepared) return [];
        var session = RequireSession(prepared);
        TerrainRasterPoint[] points;
        lock (session.Sync) points = Assets(session).SnapshotPoints();
        var groups = points.GroupBy(point => new TileKey(Block(point.X), Block(point.Z))).OrderBy(group => group.Key.X).ThenBy(group => group.Key.Z).ToArray();
        foreach (var group in groups)
            Write(new(session.Map, "terrain", $"points/resident/{group.Key.X}/{group.Key.Z}"), stamp, TerrainCacheCodec.EncodePoints(group.ToArray()), BakePayloadClass.Tile);
        lock (session.Sync)
        {
            prepared.Publish(points);
            session.Assets?.Attach(prepared);
        }
        return groups.Select(group => group.Key).ToArray();
    }
    internal bool RestoreResidentPoints(IWorldModel world, string stamp, IReadOnlyList<TileKey> blocks)
    {
        if (world.Terrain is not IPreparedTerrain prepared) return true;
        var session = RequireSession(prepared);
        foreach (var block in blocks)
        {
            var key = new BakeKey(session.Map, "terrain", $"points/resident/{block.X}/{block.Z}");
            var points = Read(key, stamp, TerrainCacheCodec.DecodePoints, BakePayloadClass.Tile);
            if (points is null) return false;
            lock (session.Sync)
            {
                prepared.Publish(points);
                session.Assets?.Attach(prepared);
            }
        }
        return true;
    }
    internal void Unload(IWorldModel world)
    {
        _sessions.TryRemove(world.Terrain, out _);
        foreach (var key in _preparing.Keys.Where(key => ReferenceEquals(key.Terrain, world.Terrain))) _preparing.TryRemove(key, out _);
    }
    private Session RequireSession(ITerrain terrain) => _sessions.TryGetValue(terrain, out var session) ? session
        : throw new InvalidOperationException("Prepared terrain is not attached to its source load session.");
    private static ITerrainAssetBuilder Assets(Session session)
    {
        if (session.Assets is { } assets) return assets;
        assets = session.Preparation.CreateAssets();
        assets.Attach(session.Terrain);
        return session.Assets = assets;
    }
    private static int Block(float coordinate) => checked((int)MathF.Floor(coordinate / TerrainRasterBlock.Edge));
    internal static string Stamp<T>(T value) => Convert.ToHexStringLower(XxHash128.Hash(JsonSerializer.SerializeToUtf8Bytes(value, ContractJson.Options)));
    private string TerrainStamp(TerrainInputSummary inputs, MapRect? area = null) => Stamp(new
    {
        BakeOptions.Abi, code = CodeFingerprints.Chain(BakeCodeChains.Shared), inputs.CommonStamp,
        cells = inputs.Cells.Where(cell => area is null || (cell.Cell.X + 1) * TileGrid.BaseEdge >= area.Min.X && cell.Cell.X * TileGrid.BaseEdge <= area.Max.X &&
            (cell.Cell.Z + 1) * TileGrid.BaseEdge >= area.Min.Y && cell.Cell.Z * TileGrid.BaseEdge <= area.Max.Y).ToArray()
    });
    internal void RequireMiss(BakeKey key)
    { if (_options.Strict && !_options.Live) throw new InvalidDataException($"Strict cache miss: {key.Map.Value}/{key.Module}/{key.Detail}"); }
    internal T? ReadProduct<T>(BakeKey key, string identity, Action<T>? validate = null) where T : class =>
        Read(key, identity, bytes => DecodeProduct(bytes, key, validate), BakePayloadClass.Product);
    internal IEnumerable<T> ReadProductCandidates<T>(BakeKey key, string identity, Action<T> validate) where T : class
    {
        if (_options.Live) yield break;
        foreach (var value in Store.ReadCandidates(key, BakePayloadClass.Product, bytes => Decode(bytes, identity, payload => DecodeProduct(payload, key, validate))))
            yield return value;
    }
    internal void WriteProduct<T>(BakeKey key, string identity, T value) where T : class => Write(key, identity, TileBundleCodec.EncodeCache(BakePayloadClass.Product, key, value), BakePayloadClass.Product);
    private T? Read<T>(BakeKey key, string identity, Func<byte[], T> decode, BakePayloadClass payload) where T : class
    {
        if (_options.Live) return null;
        return Store.TryRead(key, payload, bytes => Decode(bytes, identity, decode));
    }
    private static T Decode<T>(byte[] bytes, string identity, Func<byte[], T> decode)
    {
        using var reader = new BinaryReader(new MemoryStream(bytes));
        if (reader.ReadString() != identity) throw new InvalidDataException("Cached product identity mismatch.");
        return decode(reader.ReadBytes(checked((int)(reader.BaseStream.Length - reader.BaseStream.Position))));
    }
    private static T DecodeProduct<T>(byte[] bytes, BakeKey key, Action<T>? validate = null) where T : class
    {
        var value = TileBundleCodec.DecodeCache<T>(BakePayloadClass.Product, bytes, key);
        validate?.Invoke(value);
        return value;
    }
    private void Write(BakeKey key, string identity, byte[] bytes, BakePayloadClass payload)
    {
        if (_options.Live) return;
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)) { writer.Write(identity); writer.Write(bytes); }
        Store.Write(key, payload, stream.ToArray());
    }
    public void Dispose() { Store.Stop(); _sessions.Clear(); _preparing.Clear(); }
}
