using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

internal enum TerrainGroup { Regional, Linear, Water, Pad, Corridor }
internal sealed record TerrainSource(string File, TerrainOverlayFile Data);
internal sealed record TerrainAutoSettings(double LakeBankGrade, double LakeDepth, double ChannelBankGrade,
    double ChannelDepth, double CorridorShoulder);
internal sealed record TerrainCompileContext(MapId Map, MapRect DataRect, PolygonRings Boundary,
    IReadOnlyDictionary<FeatureId, WorldFeature> Features, WorldNetwork Network, string EntityPrefix,
    TerrainAutoSettings? Defaults, IReadOnlyDictionary<ZoneId, IReadOnlyList<PolygonRings>>? Zones);

internal sealed record TerrainNode(string Id, string File, ZoneId Zone, TerrainGroup Group, int Priority,
    MapRect Support, Func<TerrainEvaluation, double, double, TerrainEvaluation> Apply, bool Automatic = false, bool WaterBed = false)
{
    public IReadOnlyList<MapRect>? IndexPatches { get; init; }
    public Func<double, double, string>? ExplainPoint { get; init; }
    public bool HasSeparateAuthorValidation { get; init; }
}

internal sealed record TerrainWaterRegion(FeatureId Feature, TerrainRegion Region, double Level,
    TerrainRegion[] Wade, bool Channel, TerrainBridgeBanks? BridgeBanks = null, double ShoreTop = 0, bool Underground = false);

internal sealed record TerrainBankInterval(int Polygon, int Ring, double From, double To, double Length,
    IReadOnlyList<FeatureId> Roads, IReadOnlyList<FeatureId> Bridges);
internal sealed record ResolvedTerrainBank(TerrainBankSpan Span, TerrainNode Water, TerrainProgram BeforeWater)
{
    public float TopAt(double station)
    {
        var points = Span.Shore.Points;
        if (station == Span.Length) return Evaluate(points[^1].X, points[^1].Y);
        var cumulative = 0d;
        for (var i = 1; i < points.Count; i++)
        {
            var a = points[i - 1]; var c = points[i]; var dx = (double)c.X - a.X; var dz = (double)c.Y - a.Y;
            var length = Math.Sqrt(dx * dx + dz * dz); var end = cumulative + length;
            if (station <= end)
            {
                var t = (station - cumulative) / length;
                return Evaluate(a.X + dx * t, a.Y + dz * t);
            }
            cumulative = end;
        }
        throw new ArgumentOutOfRangeException(nameof(station));

        float Evaluate(double x, double z)
        {
            var value = Water.Apply(BeforeWater.Evaluate(x, z), x, z).Surface.Height;
            if (!double.IsFinite(value) || Math.Abs(value) > float.MaxValue) throw new InvalidOperationException("Resolved bank top is not finite.");
            return (float)value;
        }
    }
}

internal sealed class TerrainProgram
{
    private readonly TerrainNode[] _nodes;
    private readonly Dictionary<TileKey, int[]> _buckets;
    private readonly Func<double, double, TerrainDifferential>? _boundaryWeight;
    public IReadOnlyList<TerrainNode> Nodes => _nodes;

    public TerrainProgram(IEnumerable<TerrainNode> nodes, Func<double, double, TerrainDifferential>? boundaryWeight = null)
    {
        _boundaryWeight = boundaryWeight;
        _nodes = nodes.OrderBy(n => n.Group).ThenBy(n => n.Priority).ThenBy(n => n.Id, StringComparer.Ordinal).ToArray();
        var buckets = new Dictionary<TileKey, List<int>>();
        for (var i = 0; i < _nodes.Length; i++)
        {
            foreach (var bounds in _nodes[i].IndexPatches ?? [_nodes[i].Support])
            {
                if (!VectorMath.Finite(bounds.Min) || !VectorMath.Finite(bounds.Max))
                    throw new WorldDataException(_nodes[i].File, _nodes[i].Id, "Non-finite terrain influence bound.");
                var x0 = Cell(bounds.Min.X); var z0 = Cell(bounds.Min.Y); var x1 = Cell(bounds.Max.X); var z1 = Cell(bounds.Max.Y);
                if ((long)(x1 - x0 + 1) * (z1 - z0 + 1) > 1_000_000)
                    throw new WorldDataException(_nodes[i].File, _nodes[i].Id, "Terrain influence bound exceeds the supported map-sized index.");
                for (var z = z0; z <= z1; z++)
                    for (var x = x0; x <= x1; x++)
                    {
                        var key = new TileKey(x, z);
                        if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = [];
                        if (list.Count == 0 || list[^1] != i) list.Add(i);
                    }
            }
        }
        _buckets = buckets.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }

    public TerrainEvaluation Evaluate(double x, double z, int exclusiveEnd = int.MaxValue)
        => EvaluateCore(x, z, exclusiveEnd, null);

    public TerrainEvaluation EvaluateExcept(double x, double z, TerrainNode excluded)
        => EvaluateCore(x, z, int.MaxValue, excluded);

    private TerrainEvaluation EvaluateCore(double x, double z, int exclusiveEnd, TerrainNode? excluded)
    {
        var value = new TerrainEvaluation(TerrainDifferential.Constant(0), TerrainClass.Flat) { BoundaryWeight = _boundaryWeight?.Invoke(x, z) };
        if (!_buckets.TryGetValue(new(Cell(x), Cell(z)), out var candidates)) return value;
        foreach (var index in candidates)
        {
            if (index >= exclusiveEnd) break;
            var node = _nodes[index]; var box = node.Support;
            if (ReferenceEquals(node, excluded)) continue;
            if (x < box.Min.X || x > box.Max.X || z < box.Min.Y || z > box.Max.Y) continue;
            value = node.Apply(value, x, z);
            if (!double.IsFinite(value.Surface.Height) || !double.IsFinite(value.Surface.Dx) || !double.IsFinite(value.Surface.Dz))
                throw new WorldDataException(node.File, node.Id, $"Non-finite terrain result at ({x}, {z}).");
        }
        return value;
    }

    public IEnumerable<TileKey> AffectedTiles() => _buckets.Keys.OrderBy(k => k.Z).ThenBy(k => k.X);
    public IEnumerable<TerrainNode> CandidatesAt(double x, double z)
    {
        if (!_buckets.TryGetValue(new(Cell(x), Cell(z)), out var candidates)) yield break;
        foreach (var index in candidates)
        {
            var node = _nodes[index];
            if (x >= node.Support.Min.X && x <= node.Support.Max.X && z >= node.Support.Min.Y && z <= node.Support.Max.Y) yield return node;
        }
    }

    public string Explain(double x, double z)
    {
        var value = new TerrainEvaluation(TerrainDifferential.Constant(0), TerrainClass.Flat) { BoundaryWeight = _boundaryWeight?.Invoke(x, z) };
        var changes = new List<string>();
        foreach (var node in _nodes)
        {
            if (x < node.Support.Min.X || x > node.Support.Max.X || z < node.Support.Min.Y || z > node.Support.Max.Y) continue;
            var next = node.Apply(value, x, z);
            if (Math.Abs(next.Surface.Height - value.Surface.Height) > 0.0000001)
                changes.Add($"{node.Id} ({node.File}): {value.Surface.Height:R}->{next.Surface.Height:R}" + (node.ExplainPoint is { } detail ? "; " + detail(x, z) : ""));
            value = next;
        }
        return string.Join("; ", changes);
    }
    private static int Cell(double value) => checked((int)Math.Floor(value / TerrainSampling.TileQuads));
}

internal sealed class CompiledTerrain : ITerrain, ITerrainBankSource, ITerrainWaterSource
{
    private readonly TerrainHeightField _field;
    private readonly TerrainWaterRegion[] _water;
    public IReadOnlyList<string> Diagnostics { get; }
    public bool ZonesValidated { get; }
    private readonly IReadOnlyDictionary<string, ResolvedTerrainBank> _banks;
    public IReadOnlyList<TerrainBankSpan> Banks { get; }
    public IReadOnlyList<TerrainWaterBody> Waters { get; }

    public CompiledTerrain(MapRect dataRect, TerrainProgram program, IReadOnlyDictionary<FeatureId, float> pads,
        IEnumerable<TerrainWaterRegion> water, IReadOnlyList<string> diagnostics, bool zonesValidated,
        IReadOnlyList<ResolvedTerrainBank>? banks = null)
    {
        _water = water.OrderBy(w => w.Feature.Value, StringComparer.Ordinal).ToArray();
        Waters = Array.AsReadOnly(_water.Where(value => !value.Underground).Select(value => new TerrainWaterBody(value.Feature,
            CopyPolygons(value.Region.Polygons), CopyPolygons(value.Wade.SelectMany(area => area.Polygons)))).ToArray());
        _field = new(dataRect, (x, z) => program.Evaluate(x, z), program.Nodes.SelectMany(n => n.IndexPatches ?? [n.Support]).ToArray(), pads,
            _water.ToDictionary(w => w.Feature, w => (float)w.Level));
        Diagnostics = diagnostics.ToArray(); ZonesValidated = zonesValidated;
        var orderedBanks = (banks ?? []).OrderBy(bank => bank.Span.Id, StringComparer.Ordinal).ToArray();
        _banks = orderedBanks.ToDictionary(bank => bank.Span.Id, StringComparer.Ordinal);
        Banks = Array.AsReadOnly(orderedBanks.Select(bank => bank.Span).ToArray());
    }

    private static IReadOnlyList<PolygonRings> CopyPolygons(IEnumerable<PolygonRings> polygons) => Array.AsReadOnly(polygons.Select(polygon =>
        new PolygonRings(Array.AsReadOnly(polygon.Outer.ToArray()),
            Array.AsReadOnly(polygon.Holes.Select(hole => (IReadOnlyList<System.Numerics.Vector2>)Array.AsReadOnly(hole.ToArray())).ToArray()))).ToArray());

    public bool IsFlat(TileKey tile) => _field.IsFlat(tile);
    public float HeightAt(float x, float z) => _field.HeightAt(x, z);
    public void FillTile(TileKey tile, int level, Span<short> cm) => _field.FillTile(tile, level, cm);
    public float PadY(FeatureId feature) => _field.PadY(feature);
    public float? WaterY(FeatureId water) => _field.WaterY(water);
    public TerrainFacts FactsAt(float x, float z) => _field.FactsAt(x, z);
    public float BankTopY(string id, double station)
    {
        if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || id.Any(char.IsControl)) throw new ArgumentException("Malformed bank span ID.", nameof(id));
        if (!_banks.TryGetValue(id, out var bank)) throw new KeyNotFoundException($"Unknown bank span '{id}'.");
        if (!double.IsFinite(station) || station < 0 || station > bank.Span.Length) throw new ArgumentOutOfRangeException(nameof(station));
        return bank.TopAt(station);
    }
    public bool IsSurfaceWater(FeatureId id) => _water.Any(water => water.Feature == id && !water.Underground);

    public (FeatureId Id, float Surface, bool Wade)? WaterAt(float x, float z)
    {
        TerrainWaterRegion? first = null;
        foreach (var water in _water)
        {
            if (water.Underground) continue;
            if (!water.Region.Contains(x, z)) continue;
            first ??= water;
            foreach (var area in water.Wade)
                if (area.Contains(x, z)) return (water.Feature, (float)water.Level, true);
        }
        return first is null ? null : (first.Feature, (float)first.Level, false);
    }
}
