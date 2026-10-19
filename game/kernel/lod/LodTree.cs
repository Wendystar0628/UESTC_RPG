using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Lod;

/// <summary>One whole-map L4 root, then the fixed 512/256/128/64 metre quadtree.</summary>
public sealed class LodTree
{
    private readonly Dictionary<StreamTile, StreamTile[]> _children = [];
    private readonly Dictionary<StreamTile, Box3> _bounds = [];
    private readonly HashSet<StreamTile> _replacements = [];
    private readonly StreamSettings _settings;
    public LodTree(Box3 mapBounds, StreamSettings settings)
    {
        if (mapBounds.Size.X <= 0 || mapBounds.Size.Z <= 0) throw new ArgumentException("Map bounds must have positive XZ extent.");
        _settings = settings;
        _bounds.Add(StreamTile.Root, mapBounds);
        var roots = new List<StreamTile>();
        var minX = (int)MathF.Floor(mapBounds.Min.X / 512);
        var minZ = (int)MathF.Floor(mapBounds.Min.Z / 512);
        var maxX = (int)MathF.Ceiling(mapBounds.Max.X / 512) - 1;
        var maxZ = (int)MathF.Ceiling(mapBounds.Max.Z / 512) - 1;
        for (var x = minX; x <= maxX; x++)
            for (var z = minZ; z <= maxZ; z++)
            {
                var tile = new StreamTile(new(x, z), LodLevel.L3);
                roots.Add(tile);
                Add(tile, mapBounds);
            }
        _children.Add(StreamTile.Root, roots.ToArray());
    }
    public IReadOnlyList<StreamTile> Children(StreamTile tile) => _children.TryGetValue(tile, out var children) ? children : [];
    public Box3 Bounds(StreamTile tile) => _bounds[tile];
    public bool Contains(StreamTile tile) => _bounds.ContainsKey(tile);

    private void Add(StreamTile tile, Box3 mapBounds)
    {
        var min = tile.Min; var max = tile.Max;
        var bounds = new Box3(new(min.X, mapBounds.Min.Y, min.Y), new(max.X, mapBounds.Max.Y, max.Y));
        _bounds.Add(tile, bounds);
        if (tile.Level == LodLevel.L0) return;
        var children = new List<StreamTile>(4);
        for (var dx = 0; dx < 2; dx++)
            for (var dz = 0; dz < 2; dz++)
            {
                var child = new StreamTile(new(tile.Tile.X * 2 + dx, tile.Tile.Z * 2 + dz), tile.Level - 1);
                // Only real map quadrants participate in the all-children-ready barrier.
                if (child.Max.X <= mapBounds.Min.X || child.Min.X >= mapBounds.Max.X ||
                    child.Max.Y <= mapBounds.Min.Z || child.Min.Y >= mapBounds.Max.Z) continue;
                children.Add(child); Add(child, mapBounds);
            }
        _children.Add(tile, children.ToArray());
    }

    public void Select(Vector3 camera, float speed, float height, float fovDegrees, LodLevel minimum,
        HashSet<StreamTile> required, HashSet<StreamTile> refined)
    {
        required.Clear(); refined.Clear(); required.Add(StreamTile.Root);
        var fov = Math.Clamp(fovDegrees, 1, 179) * MathF.PI / 180;
        var reference = _settings.ReferenceFov * MathF.PI / 180;
        var scale = Math.Max(1, height) / _settings.ReferenceHeight *
            MathF.Tan(reference * .5f) / MathF.Tan(fov * .5f) *
            StreamSettings.DefaultBaseErrorPixels / (_settings.BaseErrorPixels * (1 + Math.Max(0, speed) / 30));
        Visit(StreamTile.Root, camera, minimum, scale, required, refined);
    }
    private void Visit(StreamTile tile, Vector3 camera, LodLevel minimum, float scale,
        HashSet<StreamTile> required, HashSet<StreamTile> refined)
    {
        if (tile.Level <= minimum || tile.Level == LodLevel.L0) return;
        var threshold = _settings.Distances[(int)tile.Level - 1] * scale;
        if (Distance(camera, _bounds[tile]) >= threshold) return;
        refined.Add(tile);
        foreach (var child in Children(tile))
        {
            required.Add(child);
            Visit(child, camera, minimum, scale, required, refined);
        }
    }
    public static float Distance(Vector3 position, Box3 bounds) => Vector3.Distance(position, Vector3.Clamp(position, bounds.Min, bounds.Max));

    /// <summary>A parent disappears only when every real child has complete, pipeline-ready coverage.</summary>
    public void ResolveVisible(IReadOnlySet<StreamTile> refined, Func<StreamTile, bool> ready, HashSet<StreamTile> visible)
    {
        visible.Clear(); _replacements.Clear();
        if (Cover(StreamTile.Root, refined, ready)) Resolve(StreamTile.Root, visible);
    }
    private bool Cover(StreamTile tile, IReadOnlySet<StreamTile> refined, Func<StreamTile, bool> ready)
    {
        var children = Children(tile);
        if (refined.Contains(tile) && children.Count > 0)
        {
            var complete = true;
            foreach (var child in children)
                if (!Cover(child, refined, ready)) complete = false;
            if (complete) { _replacements.Add(tile); return true; }
        }
        // Hidden ancestors need no material proof when their entire replacement frontier is ready.
        // An incomplete replacement still requires this tile's own ready geometry as coverage.
        return ready(tile);
    }
    private void Resolve(StreamTile tile, HashSet<StreamTile> visible)
    {
        if (_replacements.Contains(tile))
            foreach (var child in Children(tile)) Resolve(child, visible);
        else visible.Add(tile);
    }
}
