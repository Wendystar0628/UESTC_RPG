using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>How a vertical drop between two walking levels is connected (WLD-TERRAIN §5.1).</summary>
public enum DropMethod
{
    /// <summary>≤ 0.04 m: absorbed by the terrain (slope ≤ 5%).</summary>
    Absorb,
    /// <summary>0.04–0.15 m: a 1:12 sloped curb, no step.</summary>
    SlopedCurb,
    /// <summary>Ramp runs in the height field, rails and curbs from <see cref="RampPart"/> variation 2.</summary>
    Ramp,
    /// <summary>Flights of <see cref="StepsPart"/> with level pads between them.</summary>
    Steps,
    /// <summary>> 4 m: terraces or a hill path, not generated; goes to the main session for review.</summary>
    Terraces,
}

/// <summary>A level pad of the height field between two parts, from Start to End along the climb, at height Y.</summary>
public readonly record struct DropLanding(float Start, float End, float Y);

/// <summary>A ramp run of the height field from (Start, Y0) to (End, Y1).</summary>
public readonly record struct DropRampRun(float Start, float End, float Y0, float Y1);

/// <summary>
/// Plan in a local frame: origin at the foot of the connection on the centre line, +Z climbing, y from the lower level.
/// <see cref="Parts"/> are ready to place (Steps flights or ramp rails and curbs); <see cref="Landings"/> and
/// <see cref="RampRuns"/> are what the height field must carry. <see cref="Fits"/> is false when the rules need more run
/// than was available. <see cref="ParallelRampNeeded"/> marks drops whose steps need a second, gentler route (§5.1).
/// </summary>
public sealed record DropPlan(DropMethod Method, float Rise, float Run, bool Fits)
{
    public int Risers { get; init; }
    public float Riser { get; init; }
    public float Tread { get; init; }
    public float ClearWidth { get; init; }
    public float Grade { get; init; }
    public bool ParallelRampNeeded { get; init; }
    public IReadOnlyList<PlacedPart> Parts { get; init; } = [];
    public IReadOnlyList<DropLanding> Landings { get; init; } = [];
    public IReadOnlyList<DropRampRun> RampRuns { get; init; } = [];
}

/// <summary>
/// WLD-TERRAIN §5.1–§5.3 as code: risers 0.12–0.16 m nearest 0.15, tread clamp(0.65 − 2R, 0.30, 0.40), a pad every
/// ≤ 1.5 m of rise at least max(1.2 m, clear width) long, ramps ≤ 1:12 with a ≥ 1.5 m pad every 0.75 m of rise, clear
/// widths ≥ 2.4 m on the main network and ≥ 1.5 m elsewhere. WP-15 calls it for the drops it derives (§5.8).
/// </summary>
public static class TerrainDrop
{
    public static DropPlan Plan(float rise, float available, bool mainNetwork, float? clearWidth = null)
    {
        if (!float.IsFinite(rise) || rise < 0) throw new ArgumentOutOfRangeException(nameof(rise));
        if (!float.IsFinite(available) || available < 0) throw new ArgumentOutOfRangeException(nameof(available));
        var clear = Clear(mainNetwork, clearWidth);
        if (rise <= 0.04f) return new(DropMethod.Absorb, rise, 0, true) { ClearWidth = clear };
        if (rise <= 0.15f)
        {
            var run = rise / S.RampMaxGrade;
            return new(DropMethod.SlopedCurb, rise, run, run <= available + 1e-3f) { ClearWidth = clear, Grade = S.RampMaxGrade };
        }
        if (rise > 4f) return new(DropMethod.Terraces, rise, 0, false) { ClearWidth = clear };
        if (rise <= S.FlightMaxRise && available >= rise / S.RampMaxGrade) return RampPlan(rise, available, clear);
        return StepsPlan(rise, available, clear);
    }

    /// <summary>Riser count by §5.1: up to 0.35 m, n = ⌈ΔH / 0.15⌉; above, n ≥ 3 with R in 0.12–0.16 nearest 0.15.</summary>
    public static int RiserCount(float rise)
    {
        if (rise <= 0.35f) return Math.Max(2, (int)Math.Ceiling(rise / S.RiserTarget - 1e-4));
        var best = 3; var bestError = float.MaxValue;
        for (var n = Math.Max(3, (int)Math.Ceiling(rise / S.RiserMax - 1e-4)); n <= (int)Math.Floor(rise / S.RiserMin + 1e-4); n++)
        {
            var error = Math.Abs(rise / n - S.RiserTarget);
            if (error < bestError - 1e-6f) { best = n; bestError = error; }
        }
        return best;
    }

    public static float TreadFor(float riser) => Math.Clamp(S.TreadBase - 2 * riser, S.TreadMin, S.TreadMax);

    /// <summary>
    /// The steps half of "两条路" (§5.1): steps for <paramref name="rise"/> even when a ramp would fit, for places that
    /// pair short steps with a parallel ramp (台口、品学楼台基出入口). Rises of 0.15–4 m.
    /// </summary>
    public static DropPlan PlanSteps(float rise, bool mainNetwork, float? clearWidth = null)
    {
        if (!(rise > 0.15f && rise <= 4f)) throw new ArgumentOutOfRangeException(nameof(rise), "Steps connect drops of 0.15–4 m.");
        return StepsPlan(rise, float.MaxValue, Clear(mainNetwork, clearWidth));
    }

    /// <summary>The ramp half of "两条路": 1:12 or gentler runs of ≤ 0.75 m with ≥ 1.5 m pads, as gentle as the space allows.</summary>
    public static DropPlan PlanRamp(float rise, float available, bool mainNetwork, float? clearWidth = null)
    {
        if (!(rise > 0.04f && rise <= 4f)) throw new ArgumentOutOfRangeException(nameof(rise), "Ramps connect drops of 0.04–4 m.");
        return RampPlan(rise, available, Clear(mainNetwork, clearWidth));
    }

    private static float Clear(bool mainNetwork, float? clearWidth)
    {
        var minimum = mainNetwork ? S.MainNetworkWidth : S.OtherWidth;
        return Math.Max(clearWidth ?? minimum, minimum);
    }

    private static DropPlan StepsPlan(float rise, float available, float clear)
    {
        var n = RiserCount(rise);
        var r = rise / n; var t = TreadFor(r);
        // Enough flights that the tallest one (risers split as evenly as possible) still rises ≤ 1.5 m.
        var flights = Math.Max(1, (int)Math.Ceiling(rise / S.FlightMaxRise - 1e-4));
        while ((n + flights - 1) / flights * r > S.FlightMaxRise + 1e-4f) flights++;
        var landing = Math.Max(S.LandingMin, clear);
        var pad = Math.Max(0, landing - t);
        var width = clear + 2 * S.CheekWidth;
        var parts = new List<PlacedPart>(); var landings = new List<DropLanding>();
        float s = 0, y = 0;
        for (var f = 0; f < flights; f++)
        {
            var m = n / flights + (f < n % flights ? 1 : 0);
            var depth = m * t;
            var p = new StairParams(width, m * r, depth, m);
            parts.Add(new(new(PartCatalog.Steps), p, Matrix4x4.CreateTranslation(0, y, s + depth * 0.5f)));
            s += depth; y += m * r;
            if (f + 1 < flights) { landings.Add(new(s, s + pad, y)); s += pad; }
        }
        return new(DropMethod.Steps, rise, s, s <= available + 1e-3f)
        {
            Risers = n,
            Riser = r,
            Tread = t,
            ClearWidth = clear,
            Parts = parts,
            Landings = landings,
            ParallelRampNeeded = rise > S.FlightMaxRise,
        };
    }

    private static DropPlan RampPlan(float rise, float available, float clear)
    {
        var runs = Math.Max(1, (int)Math.Ceiling(rise / S.RampMaxSegmentRise - 1e-4));
        var landings = (runs - 1) * S.RampLandingMin;
        // As gentle as the space allows, within 5%–8.33% (§5.3).
        var grade = Math.Clamp(rise / Math.Max(available - landings, 1e-3f), S.RampMinGrade, S.RampMaxGrade);
        var each = rise / runs; var length = each / grade;
        var width = clear + 2 * S.RampCurbWidth;
        var parts = new List<PlacedPart>(); var pads = new List<DropLanding>(); var ramps = new List<DropRampRun>();
        float s = 0, y = 0;
        for (var i = 0; i < runs; i++)
        {
            ramps.Add(new(s, s + length, y, y + each));
            parts.Add(new(new(PartCatalog.Ramp), new RampParams(width, each, length) { Variation = 2 }, Matrix4x4.CreateTranslation(0, y, s + length * 0.5f)));
            s += length; y += each;
            if (i + 1 < runs) { pads.Add(new(s, s + S.RampLandingMin, y)); s += S.RampLandingMin; }
        }
        return new(DropMethod.Ramp, rise, s, s <= available + 1e-3f) { ClearWidth = clear, Grade = grade, Parts = parts, Landings = pads, RampRuns = ramps };
    }
}
