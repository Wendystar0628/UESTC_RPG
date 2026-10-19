using System.Collections.Concurrent;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Bake;

/// <summary>Lazy fingerprint samples owned by one frozen MapBuild, never by gameplay terrain queries.</summary>
internal sealed class BakeFingerprintSamples(IWorldModel world)
{
    internal readonly record struct PointSample(float Height, TerrainFacts Facts, float? Ground, float WalkDistance);

    private readonly ConcurrentDictionary<(int X, int Z), Lazy<PointSample>> _points = new();
    private readonly ConcurrentDictionary<(TileKey Tile, int Level), Lazy<short[]>> _tiles = new();

    internal PointSample At(Vector2 point) => _points.GetOrAdd(
        (BitConverter.SingleToInt32Bits(point.X), BitConverter.SingleToInt32Bits(point.Y)),
        _ => new(() => Read(world, point), LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    internal short[] Tile(TileKey tile, int level) => _tiles.GetOrAdd((tile, level),
        key => new(() =>
        {
            var heights = new short[TerrainSampling.SampleCount(key.Level)];
            world.Terrain.FillTile(key.Tile, key.Level, heights);
            return heights;
        }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    private static PointSample Read(IWorldModel world, Vector2 point)
    {
        var height = world.Terrain.HeightAt(point.X, point.Y);
        var facts = world.Terrain.FactsAt(point.X, point.Y);
        var fidelity = world.Fidelity;
        return new(height, facts, fidelity is null ? null : (float)fidelity.GroundAt(point.X, point.Y),
            fidelity?.WalkDistanceAt(point.X, point.Y) ?? 0);
    }
}
