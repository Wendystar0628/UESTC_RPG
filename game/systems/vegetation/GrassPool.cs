using Campus.Contracts;
using Campus.Contracts.Vegetation;
using Campus.Kernel.Instances;
using Godot;
using N = System.Numerics;

namespace Campus.Systems.Vegetation;

/// <summary>
/// One grass style's near pool (WLD-VEGETATION, ENG-PERF): the clumps are filed in <see cref="GrassPoolIndex.CellSize"/> m cells; at most at the
/// vegetation runtime's 10 Hz, once the eye has moved <see cref="Step"/> m, the cells within <see cref="GrassPoolIndex.Reach"/> of the eye are
/// written as the buffer's prefix and the rest is hidden. veg_grass sinks the blades into the lawn before the near radius
/// (global quality.z, 20 m at its widest) ends, and drops the clumps beyond the density (quality.y) in the shader, so the lawn's own colour is the far grass and nothing pops at the reach. Layer 6, no shadow (ENG-PERF).
/// Each pool belongs to one owner tile; capacity is the most local clumps any eye position can select.
/// </summary>
internal sealed class GrassPool
{
    /// <summary>Key prefix of grass entries in IVegetationRuntimeSource.PrepareGrass (= Campus.Gen.Vegetation.GrassClumps.KeyPrefix).</summary>
    public const string KeyPrefix = "grass/";

    private const uint Layer = RenderLayers.GrassAndEffects;   // layer 6: grass, leaves, particles (ENG-PERF)
    private const float Step = 2f;
    private const int Stride = InstanceStreamLayout.FloatsPerInstance;

    private static readonly Aabb Parked = new(new Vector3(0f, -100000f, 0f), Vector3.One);

    private readonly VegetationMotherData _data;
    private readonly string _tileId;
    public bool Ready => _pool is { IsReady: true };
    private readonly ReadOnlyMemory<float> _records;
    private readonly GrassPoolIndex _index;
    private float[] _scratch = [];
    private IInstancePool? _pool;
    private N.Vector2? _at;
    private N.Vector2? _selectionAt;

    public GrassPool(VegetationMotherData data, string tileId)
    {
        _data = data;
        _tileId = tileId;
        _records = data.Records;
        _index = data.GrassIndex ?? throw new ArgumentException("Grass data has no baked pool index.", nameof(data));
    }

    public int Count => _data.Bounds.Count;
    public int Capacity => _index.Capacity;
    public int Visible => _pool?.VisibleCount ?? 0;

    public void Mount(IInstancePoolHost host, MapId map)
    {
        _scratch = new float[Capacity * Stride];
        _pool = host.Mount(new GeometryInstancePoolSpec($"vegetation.{_tileId}.{_data.Key}", _data.Prototypes[0], Capacity, Parked,
            Layer, GeometryInstance3D.ShadowCastingSetting.Off) { Map = map });
        _pool.WriteBuffer(_scratch);
        _at = null;
        _selectionAt = null;
    }

    public void Refresh(N.Vector3 eye, bool reset)
    {
        if (_pool is not { } pool) return;
        var at = new N.Vector2(eye.X, eye.Z);
        if (!reset && _at is { } last && N.Vector2.Distance(last, at) < Step) return;
        _at = at;
        var n = 0;
        var min = new N.Vector3(float.MaxValue);
        var max = new N.Vector3(float.MinValue);
        var changed = false;
        for (var c = 0; c < _index.Cells.Count; c++)
        {
            var cell = _index.Cells[c];
            var selected = Contains(cell, at);
            // The frozen directory fixes records and order; compare selected cells, including equal-count replacements.
            changed |= selected != (_selectionAt is { } previous && Contains(cell, previous));
            if (!selected) continue;
            foreach (var i in _index.Members.Span.Slice(cell.Start, cell.Count))
            {
                _records.Span.Slice(i * Stride, Stride).CopyTo(_scratch.AsSpan(n * Stride, Stride));
                n++;
            }
            min = N.Vector3.Min(min, cell.Bounds.Min);
            max = N.Vector3.Max(max, cell.Bounds.Max);
        }
        if (n == 0)
        {
            if (pool.VisibleCount != 0) pool.VisibleCount = 0;
            if (changed) pool.SetAabb(Parked);
            _selectionAt = at;
            return;
        }
        if (changed)
        {
            _scratch.AsSpan(n * Stride).Clear();
            pool.WriteBuffer(_scratch);
            var size = max - min;
            pool.SetAabb(new Aabb(new Vector3(min.X, min.Y, min.Z), new Vector3(size.X, size.Y, size.Z)));
        }
        if (pool.VisibleCount != n) pool.VisibleCount = n;
        _selectionAt = at;
    }

    private static bool Contains(GrassPoolCell cell, N.Vector2 at)
    {
        var dx = MathF.Max(0f, MathF.Max(cell.X * GrassPoolIndex.CellSize - at.X, at.X - (cell.X + 1) * GrassPoolIndex.CellSize));
        var dz = MathF.Max(0f, MathF.Max(cell.Z * GrassPoolIndex.CellSize - at.Y, at.Y - (cell.Z + 1) * GrassPoolIndex.CellSize));
        return !(dx * dx + dz * dz > GrassPoolIndex.Reach * GrassPoolIndex.Reach);
    }

    public void Hide()
    {
        if (_pool is { VisibleCount: > 0 }) _pool.VisibleCount = 0;
        _at = null;
    }

    public void Free()
    {
        _pool?.Free();
        _pool = null;
        _at = null;
        _selectionAt = null;
    }

}
