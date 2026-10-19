using Campus.Contracts;
using Campus.Contracts.Vegetation;
using Campus.Kernel.Instances;
using Godot;
using N = System.Numerics;

namespace Campus.Systems.Vegetation;

/// <summary>
/// One grass style's near pool (WLD-VEGETATION §3.5, ENG-PERF): the clumps are filed in <see cref="Cell"/> m cells; at most at the
/// vegetation runtime's 10 Hz, once the eye has moved <see cref="Step"/> m, the cells within <see cref="Reach"/> of the eye are
/// written as the buffer's prefix and the rest is hidden. veg_grass sinks the blades into the lawn before the near radius
/// (global quality.z, 20 m at its widest) ends, and drops the clumps beyond the density (quality.y) in the shader, so the lawn's own colour is the far grass and nothing pops at the reach. Layer 6, no shadow (ENG-PERF).
/// Each pool belongs to one owner tile; capacity is the most local clumps any eye position can select.
/// </summary>
internal sealed class GrassPool
{
    /// <summary>Key prefix of grass entries in IVegetationRuntimeSource.PrepareGrass (= Campus.Gen.Vegetation.GrassClumps.KeyPrefix).</summary>
    public const string KeyPrefix = "grass/";

    private const uint Layer = 1u << 5;   // layer 6: grass, leaves, particles (ENG-PERF)
    private const float Cell = 8f;
    internal const float Reach = 24f;      // Capacity envelope covering all preset grass ranges plus clump sway; visibility reads quality.z.
    private const float Step = 2f;
    private const int Stride = InstanceStreamLayout.FloatsPerInstance;

    private static readonly Aabb Parked = new(new Vector3(0f, -100000f, 0f), Vector3.One);

    private readonly VegetationMotherData _data;
    private readonly string _tileId;
    public bool Ready => _pool is { IsReady: true };
    private readonly float[] _records;
    private readonly Dictionary<(int X, int Z), (List<int> Items, Box3 Box)> _cells = [];
    private readonly float[] _scratch;
    private IInstancePool? _pool;
    private N.Vector2? _at;

    public GrassPool(VegetationMotherData data, string tileId)
    {
        _data = data;
        _tileId = tileId;
        _records = data.Records.ToArray();
        for (var i = 0; i < data.Bounds.Count; i++)
        {
            var b = data.Bounds[i];
            var key = KeyOf(new N.Vector2((b.Min.X + b.Max.X) * 0.5f, (b.Min.Z + b.Max.Z) * 0.5f));
            if (_cells.TryGetValue(key, out var cell)) _cells[key] = (cell.Items, new Box3(N.Vector3.Min(cell.Box.Min, b.Min), N.Vector3.Max(cell.Box.Max, b.Max)));
            else _cells[key] = ([], b);
            _cells[key].Items.Add(i);
        }
        Capacity = Math.Max(1, MostInReach());
        _scratch = new float[Capacity * Stride];
    }

    public int Count => _data.Bounds.Count;
    public int Capacity { get; }
    public int Visible => _pool?.VisibleCount ?? 0;

    public void Mount(IInstancePoolHost host, MapId map)
    {
        _pool = host.Mount(new GeometryInstancePoolSpec($"vegetation.{_tileId}.{_data.Key}", _data.Owner.Value, _data.Prototypes[0], Capacity, Parked,
            Layer, GeometryInstance3D.ShadowCastingSetting.Off) { Map = map });
        _pool.WriteBuffer(_scratch);
        _at = null;
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
        foreach (var (key, cell) in _cells)
        {
            var dx = MathF.Max(0f, MathF.Max(key.X * Cell - at.X, at.X - (key.X + 1) * Cell));
            var dz = MathF.Max(0f, MathF.Max(key.Z * Cell - at.Y, at.Y - (key.Z + 1) * Cell));
            if (dx * dx + dz * dz > Reach * Reach) continue;
            var (items, box) = cell;
            foreach (var i in items)
            {
                if (n == Capacity) break;
                _records.AsSpan(i * Stride, Stride).CopyTo(_scratch.AsSpan(n * Stride, Stride));
                n++;
            }
            min = N.Vector3.Min(min, box.Min);
            max = N.Vector3.Max(max, box.Max);
        }
        pool.VisibleCount = 0;
        if (n == 0)
        {
            pool.SetAabb(Parked);
            return;
        }
        _scratch.AsSpan(n * Stride).Clear();
        pool.WriteBuffer(_scratch);
        var size = max - min;
        pool.SetAabb(new Aabb(new Vector3(min.X, min.Y, min.Z), new Vector3(size.X, size.Y, size.Z)));
        pool.VisibleCount = n;
    }

    public void Free()
    {
        _pool?.Free();
        _pool = null;
        _at = null;
    }

    private static (int X, int Z) KeyOf(N.Vector2 p) => ((int)MathF.Floor(p.X / Cell), (int)MathF.Floor(p.Y / Cell));

    /// <summary>Cells whose square comes within <paramref name="reach"/> of the point.</summary>
    private IEnumerable<(List<int> Items, Box3 Box)> InReach(N.Vector2 at, float reach = Reach)
    {
        var r = (int)MathF.Ceiling(reach / Cell);
        var c = KeyOf(at);
        for (var z = c.Z - r; z <= c.Z + r; z++)
            for (var x = c.X - r; x <= c.X + r; x++)
            {
                if (!_cells.TryGetValue((x, z), out var cell)) continue;
                var dx = MathF.Max(0f, MathF.Max(x * Cell - at.X, at.X - (x + 1) * Cell));
                var dz = MathF.Max(0f, MathF.Max(z * Cell - at.Y, at.Y - (z + 1) * Cell));
                if (dx * dx + dz * dz <= reach * reach) yield return cell;
            }
    }

    /// <summary>
    /// An upper bound on any selection: every eye in a cell lies within half a cell diagonal of its centre, so its selection is
    /// inside the cells within <see cref="Reach"/> plus that of the centre. Taken over every cell round the filled ones.
    /// </summary>
    private int MostInReach()
    {
        var r = (int)MathF.Ceiling(Reach / Cell) + 1;
        var best = 0;
        var seen = new HashSet<(int, int)>();
        foreach (var key in _cells.Keys)
            for (var z = key.Z - r; z <= key.Z + r; z++)
                for (var x = key.X - r; x <= key.X + r; x++)
                    if (seen.Add((x, z)))
                        best = Math.Max(best, InReach(new N.Vector2((x + 0.5f) * Cell, (z + 0.5f) * Cell), Reach + Cell * 0.71f).Sum(c => c.Items.Count));
        return best;
    }
}
