using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using Campus.Pipeline.Map;

namespace Campus.Kernel.Bake;

/// <summary>Owns the shared local product store and prepares terrain before pure readers receive it.</summary>
[KernelService(typeof(TerrainCacheService), Owner = "WP-02a")]
[KernelService(typeof(ITerrainPreparation), Owner = "WP-02a")]
public sealed class TerrainCacheService : ITerrainPreparation, IDisposable
{
    private sealed class Session(MapId map, WorldModelPreparation preparation, TerrainFrozenData data)
    {
        public MapId Map { get; } = map;
        public WorldModelPreparation Preparation { get; } = preparation;
        public TerrainFrozenData Data { get; } = data;
        public object Sync { get; } = new();
        public ITerrainAssetBuilder? Assets;
        public IWorldModel? AssetWorld;
        public int Sampling;
        public int Sealing;
    }
    private readonly ConcurrentDictionary<ITerrain, Session> _sessions = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentDictionary<(ITerrain Terrain, TileKey Block), Lazy<Task>> _preparing = new();
    private readonly BakeOptions _options;
    internal BakeCacheStore Store { get; }
    public TerrainCacheService() : this(BakeOptions.FromApplication()) { }
    internal TerrainCacheService(BakeOptions options) { _options = options; Store = new(options.CacheRoot); }

    internal WorldModelLoadResult Load(MapId map, WorldModelPreparation preparation)
    {
        var key = new BakeKey(map, "terrain", "meta");
        var identity = TerrainStamp(preparation.SourceInputs);
        var data = ReadJson<TerrainFrozenData>(key, identity);
        ITerrainAssetBuilder? builder = null;
        if (data is null)
        {
            RequireMiss(key);
            builder = preparation.CreateAssets();
            data = builder.Freeze();
            WriteJson(key, identity, data);
        }
        var loaded = preparation.Complete(data);
        _sessions[loaded.World.Terrain] = new(map, preparation, data) { Assets = builder };
        return loaded;
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
                    Interlocked.Increment(ref session.Sampling);
                    try
                    {
                    var key = new BakeKey(session.Map, "terrain", $"block/{block.X}/{block.Z}");
                    var stamp = TerrainStamp(session.Data.SourceInputs, new(new(block.X * TerrainRasterBlock.Edge, block.Z * TerrainRasterBlock.Edge),
                        new((block.X + 1) * TerrainRasterBlock.Edge, (block.Z + 1) * TerrainRasterBlock.Edge)));
                    var bytes = Read(key, stamp);
                    TerrainRasterBlock value;
                    TerrainRasterBlock? cached = null;
                    if (bytes is not null)
                    {
                        try { cached = TerrainCacheCodec.DecodeBlock(bytes); }
                        catch (Exception error) when (error is IOException or ArgumentException) { Store.Remove(key); }
                    }
                    if (cached is not null) value = cached;
                    else
                    {
                        RequireMiss(key);
                        lock (session.Sync) value = Assets(session).BuildBlock(block, prepared);
                        Write(key, stamp, TerrainCacheCodec.EncodeBlock(value));
                    }
                    prepared.Publish(value);
                    }
                    finally { Interlocked.Decrement(ref session.Sampling); }
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
            Interlocked.Increment(ref session.Sampling);
            try
            {
            cancellation.ThrowIfCancellationRequested();
            var key = new BakeKey(session.Map, "terrain", "points/" + product);
            var stamp = Stamp(new { terrain = TerrainStamp(session.Data.SourceInputs), points = snapshot });
            var bytes = Read(key, stamp);
            TerrainRasterPoint[] values;
            TerrainRasterPoint[]? cached = null;
            if (bytes is not null)
            {
                try { cached = TerrainCacheCodec.DecodePoints(bytes); }
                catch (Exception error) when (error is IOException or ArgumentException) { Store.Remove(key); }
            }
            if (cached is not null) values = cached;
            else
            {
                RequireMiss(key);
                lock (session.Sync) values = Assets(session).BuildPoints(snapshot);
                Write(key, stamp, TerrainCacheCodec.EncodePoints(values));
            }
            cancellation.ThrowIfCancellationRequested();
            prepared.Publish(values);
            }
            finally { Interlocked.Decrement(ref session.Sampling); }
        }, cancellation);
    }

    internal IWorldModel AssetWorld(IWorldModel world)
    {
        if (world.Terrain is not IPreparedTerrain) return world;
        var session = RequireSession(world.Terrain);
        lock (session.Sync) return session.AssetWorld ??= session.Preparation.WithTerrain(world, Assets(session));
    }
    internal TileKey[] SaveResidentPoints(IWorldModel world, string stamp)
    {
        if (world.Terrain is not IPreparedTerrain prepared) return [];
        var session = RequireSession(prepared);
        TerrainRasterPoint[] points;
        lock (session.Sync) points = Assets(session).SnapshotPoints();
        var groups = points.GroupBy(point => new TileKey(Block(point.X), Block(point.Z))).OrderBy(group => group.Key.X).ThenBy(group => group.Key.Z).ToArray();
        foreach (var group in groups)
            Write(new(session.Map, "terrain", $"points/resident/{group.Key.X}/{group.Key.Z}"), stamp, TerrainCacheCodec.EncodePoints(group.ToArray()));
        prepared.Publish(points);
        return groups.Select(group => group.Key).ToArray();
    }
    internal bool RestoreResidentPoints(IWorldModel world, string stamp, IReadOnlyList<TileKey> blocks)
    {
        if (world.Terrain is not IPreparedTerrain prepared) return true;
        var session = RequireSession(prepared);
        foreach (var block in blocks)
        {
            var key = new BakeKey(session.Map, "terrain", $"points/resident/{block.X}/{block.Z}");
            var bytes = Read(key, stamp);
            if (bytes is null) return false;
            try { prepared.Publish(TerrainCacheCodec.DecodePoints(bytes)); }
            catch (Exception error) when (error is IOException or ArgumentException) { Store.Remove(key); return false; }
        }
        return true;
    }
    internal void ReleaseAssets(IWorldModel world)
    {
        if (!_sessions.TryGetValue(world.Terrain, out var session)) return;
        if (session.Assets is null || Volatile.Read(ref session.Sampling) != 0 || Interlocked.CompareExchange(ref session.Sealing, 1, 0) != 0) return;
        _ = Task.Run(() =>
        {
            try
            {
                lock (session.Sync)
                {
                    if (Volatile.Read(ref session.Sampling) != 0) return;
                    if (world.Terrain is IPreparedTerrain prepared) session.Assets?.Seal(prepared);
                    session.Assets = null; session.AssetWorld = null;
                }
            }
            finally { Volatile.Write(ref session.Sealing, 0); }
        });
    }
    internal void Unload(IWorldModel world)
    {
        _sessions.TryRemove(world.Terrain, out _);
        foreach (var key in _preparing.Keys.Where(key => ReferenceEquals(key.Terrain, world.Terrain))) _preparing.TryRemove(key, out _);
    }
    private Session RequireSession(ITerrain terrain) => _sessions.TryGetValue(terrain, out var session) ? session
        : throw new InvalidOperationException("Prepared terrain is not attached to its source load session.");
    private static ITerrainAssetBuilder Assets(Session session) => session.Assets ??= session.Preparation.CreateAssets();
    private static int Block(float coordinate) => checked((int)MathF.Floor(coordinate / TerrainRasterBlock.Edge));
    internal static string Stamp<T>(T value) => Convert.ToHexStringLower(XxHash128.Hash(JsonSerializer.SerializeToUtf8Bytes(value, ContractJson.Options)));
    private static string TerrainStamp(TerrainInputSummary inputs, MapRect? area = null) => Stamp(new
    {
        BakeOptions.Abi, code = GenerationInputAssembly.CodeVersion(typeof(WorldModelLoader)),
        runtime = typeof(TerrainCacheService).Module.ModuleVersionId, inputs.CommonStamp,
        cells = inputs.Cells.Where(cell => area is null || (cell.Cell.X + 1) * TileGrid.BaseEdge >= area.Min.X && cell.Cell.X * TileGrid.BaseEdge <= area.Max.X &&
            (cell.Cell.Z + 1) * TileGrid.BaseEdge >= area.Min.Y && cell.Cell.Z * TileGrid.BaseEdge <= area.Max.Y).ToArray()
    });
    internal void RequireMiss(BakeKey key)
    { if (_options.Strict && !_options.Live) throw new InvalidDataException($"Strict cache miss: {key.Map.Value}/{key.Module}/{key.Detail}"); }
    internal T? ReadJson<T>(BakeKey key, string identity) where T : class
    {
        var bytes = Read(key, identity);
        if (bytes is null) return null;
        try { return JsonSerializer.Deserialize<T>(bytes, ContractJson.Options) ?? throw new JsonException("Empty cached product."); }
        catch (Exception error) when (error is JsonException or ArgumentException) { Store.Remove(key); return null; }
    }
    internal void WriteJson<T>(BakeKey key, string identity, T value) => Write(key, identity, JsonSerializer.SerializeToUtf8Bytes(value, ContractJson.Options));
    private byte[]? Read(BakeKey key, string identity)
    {
        if (_options.Live) return null;
        var maximumPayload = key.Detail == "meta" && (key.Module is "terrain" or "resident")
            ? BakeCacheStore.MaximumProductBytes : BakeCacheStore.MaximumPayloadBytes;
        var bytes = Store.TryRead(key, maximumPayload);
        if (bytes is null) return null;
        try
        {
            using var reader = new BinaryReader(new MemoryStream(bytes));
            if (reader.ReadString() != identity) { Store.Remove(key); return null; }
            return reader.ReadBytes(checked((int)(reader.BaseStream.Length - reader.BaseStream.Position)));
        }
        catch (IOException) { Store.Remove(key); return null; }
    }
    private void Write(BakeKey key, string identity, byte[] bytes)
    {
        if (_options.Live) return;
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)) { writer.Write(identity); writer.Write(bytes); }
        Store.Write(key, stream.ToArray());
    }
    public void Dispose() { Store.Stop(); _sessions.Clear(); _preparing.Clear(); }
}
