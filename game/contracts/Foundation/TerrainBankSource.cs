using System.Numerics;

namespace Campus.Contracts;

/// <summary>Optional immutable capability of the same ITerrain instance; this is not a separately registered service.</summary>
/// <remarks>
/// WP-08 resolves and freezes spans with its terrain operations and road profiles. Consumers use that result,
/// rather than deriving hard-bank extents again. Banks has unique ordinal IDs and stable ordinal ID order.
/// An empty list means this compiled terrain has no exported spans; an absent capability makes no such claim.
/// Publishing another terrain version must never change a retained source, span, or BankTopY result.
/// </remarks>
public interface ITerrainBankSource
{
    IReadOnlyList<TerrainBankSpan> Banks { get; }

    /// <summary>Absolute world Y of the resolved bank top, excluding content-side kerbs and railings.</summary>
    /// <remarks>
    /// Station is metres along the span's final exported Shore, in [0, Length], including transition portions.
    /// Use the same double segment accumulation as TerrainBankSpan.Length. Both endpoints are included;
    /// a closed span's stations 0 and Length identify the same position and elevation.
    /// Query the frozen evaluator/profiles without IO or mutable callbacks. The returned height is finite.
    /// Reject malformed IDs with ArgumentException, unknown IDs with KeyNotFoundException, and non-finite
    /// or out-of-range stations with ArgumentOutOfRangeException. Do not clamp, wrap, or substitute terrain height.
    /// </remarks>
    float BankTopY(string id, double station);
}

/// <summary>A resolved bank interval and its provenance, defensively copied once when terrain is compiled.</summary>
/// <remarks>
/// Shore points are final world (X,Z) coordinates, ordered with water to the mathematical left:
/// for tangent (dx,dz), left is (-dz,dx). The producer checks this against water topology; this value never
/// guesses a water side or reverses points. Open spans include both end transitions. A complete Hard ring
/// repeats its first point once at the end and has core [0, Length] and zero transition.
/// Partial spans crossing the original ring seam are exported as one ordered open polyline.
/// No engine resources, evaluation callbacks, material rules or mutable authoring state are retained.
/// </remarks>
public sealed class TerrainBankSpan
{
    public TerrainBankSpan(string id, IReadOnlyList<FeatureId> waters, IReadOnlyList<FeatureId> roads,
        IReadOnlyList<FeatureId> bridges, Polyline shore, double coreFrom, double coreTo,
        double transitionLength, TerrainBankKind kind)
    {
        ValidateId(id, nameof(id));
        _ = kind switch
        {
            TerrainBankKind.Hard or TerrainBankKind.Soft or TerrainBankKind.Steps or
                TerrainBankKind.Wetland or TerrainBankKind.Swale => kind,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), "Unknown terrain bank kind.")
        };
        var waterSnapshot = CopyIds(waters, nameof(waters));
        if (waterSnapshot.Count == 0) throw new ArgumentException("A bank span must identify at least one water feature.", nameof(waters));
        var roadSnapshot = CopyIds(roads, nameof(roads));
        var bridgeSnapshot = CopyIds(bridges, nameof(bridges));
        ArgumentNullException.ThrowIfNull(shore);
        ArgumentNullException.ThrowIfNull(shore.Points);
        var points = shore.Points.ToArray();
        if (points.Length < 2) throw new ArgumentException("A shore requires at least two points.", nameof(shore));

        double length = 0;
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                throw new ArgumentException("Shore points must be finite world XZ coordinates.", nameof(shore));
            if (i == 0) continue;
            // Convert before subtracting; Vector2 subtraction/distance would round in float first.
            double dx = (double)point.X - points[i - 1].X;
            double dz = (double)point.Y - points[i - 1].Y;
            if (dx == 0 && dz == 0)
                throw new ArgumentException("Consecutive repeated shore points are not allowed.", nameof(shore));
            double next = length + Math.Sqrt(dx * dx + dz * dz);
            if (!double.IsFinite(next) || next <= length)
                throw new ArgumentException("Every shore segment must advance its finite double station.", nameof(shore));
            length = next;
        }
        bool closed = points[0] == points[^1];
        if (closed && points.Distinct().Count() < 3)
            throw new ArgumentException("A closed shore requires at least three distinct points.", nameof(shore));
        if (!double.IsFinite(coreFrom) || !double.IsFinite(coreTo) || coreFrom < 0 || coreFrom >= coreTo || coreTo > length)
            throw new ArgumentOutOfRangeException(nameof(coreFrom), "Core must be a finite, nonempty interval inside [0, Length].");
        if (!double.IsFinite(transitionLength) || transitionLength < 0)
            throw new ArgumentOutOfRangeException(nameof(transitionLength));
        bool wholeHardRing = closed && kind == TerrainBankKind.Hard && coreFrom == 0 && coreTo == length && transitionLength == 0;
        if (!wholeHardRing && (closed || coreFrom < transitionLength || coreTo > length - transitionLength))
            throw new ArgumentException("An open core must have room for its transition at both ends; a closed span must be a complete Hard ring with zero transition.", nameof(transitionLength));

        Id = id; Waters = waterSnapshot; Roads = roadSnapshot; Bridges = bridgeSnapshot;
        Shore = new Polyline(Array.AsReadOnly(points));
        CoreFrom = coreFrom; CoreTo = coreTo; TransitionLength = transitionLength; Kind = kind; Length = length;
    }

    public string Id { get; }
    public IReadOnlyList<FeatureId> Waters { get; }
    public IReadOnlyList<FeatureId> Roads { get; }
    public IReadOnlyList<FeatureId> Bridges { get; }
    public Polyline Shore { get; }
    public double CoreFrom { get; }
    public double CoreTo { get; }
    /// <summary>Transition metres at each end outside the core; may be zero on an open span or a complete Hard ring.</summary>
    public double TransitionLength { get; }
    public TerrainBankKind Kind { get; }
    /// <summary>Ordered sum of sqrt(dx*dx+dz*dz), with differences and accumulation in double on the final exported points.</summary>
    public double Length { get; }

    private static IReadOnlyList<FeatureId> CopyIds(IReadOnlyList<FeatureId> source, string parameter)
    {
        ArgumentNullException.ThrowIfNull(source, parameter);
        var ids = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var feature in source)
        {
            ValidateId(feature.Value, parameter);
            ids.Add(feature.Value);
        }
        return Array.AsReadOnly(ids.Select(value => new FeatureId(value)).ToArray());
    }

    private static void ValidateId(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Any(char.IsControl))
            throw new ArgumentException("IDs must be nonempty, trimmed and free of control characters.", parameter);
    }
}
