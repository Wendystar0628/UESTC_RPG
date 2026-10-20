using System.Collections.ObjectModel;
using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

/// <summary>Deterministic terrain in prepared regions. The scheduler prepares regions before querying; sampling performs no IO.</summary>
public interface ITerrain
{
    /// <summary>Frozen source-only dependency stamps, prepared without evaluating terrain.</summary>
    TerrainInputSummary SourceInputs { get; }
    /// <summary>True only if H is zero across the entire 64 m L0 cell, including its shared boundary.</summary>
    /// <remarks>For a coarser cell, test every covered L0 cell before taking the zero-height fast path.</remarks>
    bool IsFlat(TileKey tile);
    /// <summary>Centimetre lattice height, interpolated on the (1,0)-(0,1) diagonal. An unprepared region is a caller error.</summary>
    float HeightAt(float x, float z);
    /// <summary>Height and facts from the same prepared metre lattice.</summary>
    (float Height, TerrainFacts Facts) SampleAt(float x, float z);
    /// <summary>Fills exactly 65 * 65 samples for levels 0-3, or exactly 81 * 81 samples for level 4.</summary>
    /// <remarks>
    /// Row-major index is z * width + x, including both boundary rows and columns.
    /// At levels 0-3, tile is the level's index, floor(worldXZ / (64 &lt;&lt; level)), including negative coordinates;
    /// origin is tile * (64 &lt;&lt; level), spacing is 1 &lt;&lt; level metres. It is not an unscaled L0 tile index.
    /// At level 4 tile is ignored: sample this terrain's full map DataRect from Min to Max in 80 intervals per axis.
    /// The L4 spacing is extent / 80 (32 m only for a 2560 m extent). Both endpoints are included.
    /// Evaluate the same H at world coordinates and round H * 100 using MidpointRounding.ToEven.
    /// TerrainSampling.HoleCm is reserved for a missing height; it is never an ordinary height value.
    /// Reject an invalid level or a destination with a different length. No allocation is required per call.
    /// </remarks>
    void FillTile(TileKey tile, int level, Span<short> cm);
    /// <summary>Absolute pad elevation for a building, plaza or other supported feature.</summary>
    /// <exception cref="KeyNotFoundException">The feature has no pad in this terrain.</exception>
    float PadY(FeatureId feature);
    /// <summary>Returns the same absolute pad elevation as PadY when present; false means the feature has no pad.</summary>
    bool TryGetPadY(FeatureId feature, out float y);
    /// <summary>Logical constant water level. Unknown features and non-water features return null.</summary>
    float? WaterY(FeatureId water);
    TerrainFacts FactsAt(float x, float z);
}

/// <summary>Source inputs affecting one 64 m world cell, including upstream source dependencies.</summary>
public readonly record struct TerrainInputCell(TileKey Cell, string Stamp);

/// <summary>The common source stamp covers map-wide parameters; Cells contains spatial source dependencies.</summary>
public sealed class TerrainInputSummary
{
    public string CommonStamp { get; }
    public IReadOnlyList<TerrainInputCell> Cells { get; }
    public TerrainInputSummary(string commonStamp, IReadOnlyList<TerrainInputCell> cells)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commonStamp);
        ArgumentNullException.ThrowIfNull(cells);
        var ordered = cells.OrderBy(cell => cell.Cell.X).ThenBy(cell => cell.Cell.Z).ToArray();
        if (ordered.Any(cell => string.IsNullOrWhiteSpace(cell.Stamp)) || ordered.Select(cell => cell.Cell).Distinct().Count() != ordered.Length)
            throw new ArgumentException("Terrain source cells require unique keys and nonempty stamps.", nameof(cells));
        CommonStamp = commonStamp;
        Cells = Array.AsReadOnly(ordered);
    }
}

public enum TerrainClass : byte { Flat, Slope, Transition }

/// <summary>Grade is absolute rise / horizontal distance; crest and toe distances are signed metres.</summary>
/// <remarks>PositiveInfinity means there is no such line. GPU attribute writers must map absent lines to a finite sentinel.</remarks>
public readonly record struct TerrainFacts(float Grade, TerrainClass Class, float CrestDist, float ToeDist);

/// <summary>Sampling positions, independent of terrain evaluation and rendering.</summary>
public readonly record struct TerrainSampleGrid(Vector2 Origin, Vector2 Step, int Width, int Height)
{
    public int SampleCount => checked(Width * Height);
}

public static class TerrainSampling
{
    public const int TileQuads = TileGrid.BaseEdge;
    public const int TileSamples = 65;
    public const int WholeMapSamples = 81;
    public const short HoleCm = short.MinValue;
    public const float MinimumHeight = -5f;
    public const float MaximumHeight = 10f;
    public const float Centimetre = 0.01f;

    /// <summary>Integer nodes read by the common triangle sampler, including both axes for fractional queries.</summary>
    public static TerrainSampleGrid QueryGrid(Vector2 point)
    {
        if (!VectorMath.Finite(point)) throw new ArgumentOutOfRangeException(nameof(point));
        var origin = new Vector2(MathF.Floor(point.X), MathF.Floor(point.Y));
        var width = point == origin ? 1 : 2;
        return new(origin, Vector2.One, width, width);
    }

    /// <summary>Required destination length; the optional 33x33 render optimization never changes FillTile's contract.</summary>
    public static int SampleCount(int level) => level switch
    {
        0 or 1 or 2 or 3 => TileSamples * TileSamples,
        4 => WholeMapSamples * WholeMapSamples,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    /// <summary>Returns the common world-coordinate grid used by every ITerrain producer and consumer.</summary>
    public static TerrainSampleGrid Grid(TileKey tile, int level, MapRect dataRect)
    {
        _ = SampleCount(level);
        ArgumentNullException.ThrowIfNull(dataRect);
        if (!VectorMath.Finite(dataRect.Min) || !VectorMath.Finite(dataRect.Max) || dataRect.Max.X <= dataRect.Min.X || dataRect.Max.Y <= dataRect.Min.Y)
            throw new ArgumentException("Terrain map rectangle must be finite and have positive extent.", nameof(dataRect));
        if (level == 4)
            return new(dataRect.Min, (dataRect.Max - dataRect.Min) / (WholeMapSamples - 1), WholeMapSamples, WholeMapSamples);
        int step = 1 << level;
        var origin = TileGrid.Origin(tile, (LodLevel)level);
        return new(origin, new Vector2(step), TileSamples, TileSamples);
    }

    /// <summary>HeightFieldCollider decoding: a hole becomes NaN for the physics boundary; other samples are centimetres.</summary>
    public static float DecodeHeight(short cm) => cm == HoleCm ? float.NaN : cm * Centimetre;

}

/// <summary>Immutable, pure flat fallback for maps whose terrain evaluator is not attached yet.</summary>
/// <remarks>Water levels are a caller-supplied classified snapshot. No feature IDs, names, geometry or files are guessed here.</remarks>
public sealed class FlatTerrain : ITerrain
{
    public static FlatTerrain Default { get; } = new();
    private readonly IReadOnlyDictionary<FeatureId, float> _waterLevels;
    public TerrainInputSummary SourceInputs { get; }

    public FlatTerrain(IReadOnlyDictionary<FeatureId, float>? waterLevels = null)
    {
        var snapshot = new Dictionary<FeatureId, float>();
        foreach (var pair in waterLevels ?? new Dictionary<FeatureId, float>())
        {
            if (string.IsNullOrWhiteSpace(pair.Key.Value) || !float.IsFinite(pair.Value)
                || pair.Value < TerrainSampling.MinimumHeight || pair.Value > TerrainSampling.MaximumHeight)
                throw new ArgumentException("Flat terrain water levels require feature IDs and finite in-range heights.", nameof(waterLevels));
            snapshot.Add(pair.Key, pair.Value);
        }
        _waterLevels = new ReadOnlyDictionary<FeatureId, float>(snapshot);
        SourceInputs = new("flat/" + System.Text.Json.JsonSerializer.Serialize(snapshot.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal).ToArray(), ContractJson.Options), []);
    }

    public bool IsFlat(TileKey tile) => true;
    public float HeightAt(float x, float z) => 0f;
    public (float Height, TerrainFacts Facts) SampleAt(float x, float z) => (0f, FactsAt(x, z));
    public void FillTile(TileKey tile, int level, Span<short> cm)
    {
        if (cm.Length != TerrainSampling.SampleCount(level))
            throw new ArgumentException("Terrain sample span does not match the selected level.", nameof(cm));
        cm.Clear();
    }
    public float PadY(FeatureId feature) => 0f;
    public bool TryGetPadY(FeatureId feature, out float y) { y = 0f; return true; }
    public float? WaterY(FeatureId water) => _waterLevels.TryGetValue(water, out float level) ? level : null;
    public TerrainFacts FactsAt(float x, float z) => new(0, TerrainClass.Flat, float.PositiveInfinity, float.PositiveInfinity);
}

/// <summary>Per-zone terrain input. Bytes are read by the kernel and interpreted by the WP-08 evaluator.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TerrainOverlayFile(ZoneId Zone, IReadOnlyList<TerrainOp> Ops)
{
    public MapId? Map { get; init; }
}

/// <summary>Shared terrain operation header. Concrete operation types supply their domain priority.</summary>
/// <remarks>Priority is a non-negative int32 and sorts only inside the fixed composition group, then ordinal Id.
/// Regional operations share one group: there is no hidden type sort after priority. Height is relative amplitude;
/// Y, Y0/Y1, FrontY, PadY and WaterY are absolute world elevations.</remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "op", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(TerrainPlatformOp), "platform")]
[JsonDerivedType(typeof(TerrainMoundOp), "mound")]
[JsonDerivedType(typeof(TerrainBasinOp), "basin")]
[JsonDerivedType(typeof(TerrainSlopeOp), "slope")]
[JsonDerivedType(typeof(TerrainTerraceOp), "terrace")]
[JsonDerivedType(typeof(TerrainChannelOp), "channel")]
[JsonDerivedType(typeof(TerrainLakeOp), "lake")]
[JsonDerivedType(typeof(TerrainPadOp), "pad")]
[JsonDerivedType(typeof(TerrainCorridorOp), "corridor")]
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public abstract record TerrainOp(string Id, string Reason, int Priority)
{
    public required ZoneId Zone { get; init; }
}
