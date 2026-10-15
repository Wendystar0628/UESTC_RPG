using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Interior;

/// <summary>Shared facade/opening dimensions in metres; dormitory bays default to 3.6 m.</summary>
public sealed record FacadeGridOptions
{
    public float TargetBayWidth { get; init; } = 3.6f;
    public float MinimumBayWidth { get; init; } = 2.8f;
    public float MaximumBayWidth { get; init; } = 4.2f;
    public float WindowWidth { get; init; } = 1.8f;
    public float WindowHeight { get; init; } = 1.5f;
    public float SillHeight { get; init; } = 0.9f;
    public float JambClearance { get; init; } = 0.25f;
}

/// <summary>Window-card classification; the room plan refines the ordinary room default.</summary>
public enum FacadeWindowKind : byte { Room, Corridor, Stair, Service, Blank }

/// <summary>
/// Plan points use world X/Z metres. A/B are bay boundaries on the glass plane;
/// OpeningA/B delimit the window on that same plane. Inward points into the building.
/// Index is contiguous across all rings, independent of storeys and input winding/start vertex.
/// </summary>
public sealed record FacadeBay(int Index, int Ring, int Edge, int Bay,
    Vector2 A, Vector2 B, Vector2 OpeningA, Vector2 OpeningB, Vector2 Inward,
    float SillHeight, float WindowHeight, FacadeWindowKind WindowKind)
{
    public string Key => FormattableString.Invariant($"r{Ring}/e{Edge}/b{Bay}");
    public Vector2 Center => PlanGeometry.Lerp(A, B, 0.5d);
    public float Width => PlanGeometry.Length(B - A);
    /// <summary>The caller adds its building allocation base; ordinal is zero-based, not a floor label.</summary>
    public int WindowIndex(int storeyOrdinal, int baysPerStorey)
    {
        if (storeyOrdinal < 0 || baysPerStorey <= Index) throw new ArgumentOutOfRangeException(nameof(storeyOrdinal));
        return checked(storeyOrdinal * baysPerStorey + Index);
    }
}

public sealed record FacadeGrid(PolygonRings Footprint, IReadOnlyList<FacadeBay> Bays, uint GridHash);

/// <summary>Pure common source for exterior windows and interior partition boundaries; no world lookup or IO.</summary>
public static class FacadeGridGenerator
{
    public static FacadeGrid Generate(PolygonRings footprint, FacadeGridOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(footprint);
        options ??= new();
        Validate(options);
        Vector2[] outer = PlanGeometry.CanonicalRing(footprint.Outer, true);
        var holes = footprint.Holes.Select(h => PlanGeometry.CanonicalRing(h, false))
            .OrderBy(h => h[0].X).ThenBy(h => h[0].Y).ToArray();
        ValidateHoles(outer, holes);
        var rings = new List<IReadOnlyList<Vector2>> { outer };
        rings.AddRange(holes);
        var bays = new List<FacadeBay>();
        for (int ringIndex = 0; ringIndex < rings.Count; ringIndex++)
        {
            IReadOnlyList<Vector2> ring = rings[ringIndex];
            for (int edge = 0; edge < ring.Count; edge++)
            {
                Vector2 a = ring[edge], b = ring[(edge + 1) % ring.Count];
                Vector2 tangent = PlanGeometry.Unit(b - a);
                Vector2 inward = new(-tangent.Y, tangent.X);
                float length = PlanGeometry.Length(b - a);
                int count = BayCount(length, options);
                if (bays.Count + count > 65536)
                    throw new ArgumentException("A single footprint exceeds the supported facade bay count.", nameof(footprint));
                for (int bay = 0; bay < count; bay++)
                {
                    Vector2 start = PlanGeometry.Lerp(a, b, (double)bay / count);
                    Vector2 end = PlanGeometry.Lerp(a, b, (double)(bay + 1) / count);
                    float width = PlanGeometry.Length(end - start);
                    float openingWidth = MathF.Min(options.WindowWidth, width - 2f * options.JambClearance);
                    bool blank = width < options.MinimumBayWidth || openingWidth < 0.5f;
                    Vector2 center = PlanGeometry.Lerp(start, end, 0.5d);
                    Vector2 halfOpening = tangent * (blank ? 0f : openingWidth * 0.5f);
                    bays.Add(new(bays.Count, ringIndex, edge, bay, start, end,
                        PlanGeometry.Quantize(center - halfOpening), PlanGeometry.Quantize(center + halfOpening), inward,
                        PlanGeometry.Quantize(options.SillHeight), PlanGeometry.Quantize(options.WindowHeight),
                        blank ? FacadeWindowKind.Blank : FacadeWindowKind.Room));
                }
            }
        }
        var hashParts = new List<string> { "interior.facade.v1" };
        foreach (FacadeBay bay in bays)
        {
            hashParts.Add(bay.Key);
            hashParts.Add(PlanGeometry.Key(bay.A));
            hashParts.Add(PlanGeometry.Key(bay.B));
            hashParts.Add(PlanGeometry.Key(bay.OpeningA));
            hashParts.Add(PlanGeometry.Key(bay.OpeningB));
            hashParts.Add(PlanGeometry.Mm(bay.SillHeight));
            hashParts.Add(PlanGeometry.Mm(bay.WindowHeight));
            hashParts.Add(bay.WindowKind.ToString());
        }
        return new(new(outer, holes), bays.ToArray(), StableHash.Hash32(0, hashParts.ToArray()));
    }

    internal static int BayCount(float length, FacadeGridOptions options)
    {
        int minimumCount = Math.Max(1, (int)Math.Ceiling(length / options.MaximumBayWidth));
        int maximumCount = Math.Max(1, (int)Math.Floor(length / options.MinimumBayWidth));
        if (minimumCount > maximumCount) return minimumCount;
        return Math.Clamp((int)Math.Round(length / options.TargetBayWidth, MidpointRounding.ToEven), minimumCount, maximumCount);
    }

    private static void Validate(FacadeGridOptions o)
    {
        float[] values = [o.TargetBayWidth, o.MinimumBayWidth, o.MaximumBayWidth, o.WindowWidth, o.WindowHeight, o.JambClearance];
        if (values.Any(v => !float.IsFinite(v) || v <= 0f) ||
            !float.IsFinite(o.SillHeight) || o.SillHeight < 0f || o.MinimumBayWidth < 0.5f ||
            o.MinimumBayWidth > o.TargetBayWidth || o.TargetBayWidth > o.MaximumBayWidth ||
            o.MaximumBayWidth > 30f || o.WindowHeight > 30f)
            throw new ArgumentException("Invalid facade grid dimensions.", nameof(o));
    }

    private static void ValidateHoles(Vector2[] outer, Vector2[][] holes)
    {
        for (int i = 0; i < holes.Length; i++)
        {
            if (!PlanGeometry.Contains(outer, holes[i][0], false) || RingsTouch(outer, holes[i]))
                throw new ArgumentException("A footprint hole must lie strictly inside its outer ring.", nameof(holes));
            for (int j = 0; j < i; j++)
                if (RingsTouch(holes[i], holes[j]) || PlanGeometry.Contains(holes[i], holes[j][0]) || PlanGeometry.Contains(holes[j], holes[i][0]))
                    throw new ArgumentException("Footprint holes may not intersect, touch or nest.", nameof(holes));
        }
    }

    private static bool RingsTouch(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b)
    {
        for (int i = 0; i < a.Count; i++)
            for (int j = 0; j < b.Count; j++)
                if (PlanGeometry.SegmentsTouch(a[i], a[(i + 1) % a.Count], b[j], b[(j + 1) % b.Count])) return true;
        return false;
    }
}
