namespace Campus.Tests;

/// <summary>Why a test exists. Every test carries exactly one (STD-CODE).</summary>
internal static class TestReason
{
    /// <summary>Guards a public interface, data format or same-input-same-output rule.</summary>
    public const string Contract = "Contract";

    /// <summary>Guards the core experience: boot, walking, save.</summary>
    public const string Smoke = "Smoke";

    /// <summary>Guards a bug that has occurred before.</summary>
    public const string Regression = "Regression";
}

/// <summary>Additional test behavior tags, independent of the test's reason.</summary>
internal static class TestTag
{
    /// <summary>Compares results bit for bit; allows skipping <see cref="TestTolerance"/>.</summary>
    public const string Determinism = "Determinism";
}

/// <summary>Shared tolerances at demo precision. Assertions reference these instead of literals; <c>Determinism</c> tests compare bit for bit.</summary>
internal static class TestTolerance
{
    /// <summary>Metres: positions and lengths, including a position read back after save and restore. Seams and coplanar faces use the production geometry tolerance instead.</summary>
    public const float Position = 0.02f;

    /// <summary>Metres: authored alignment such as furniture, shoreline and entrances.</summary>
    public const float Layout = 0.05f;

    /// <summary>Degrees: angles.</summary>
    public const float AngleDegrees = 1f;

    /// <summary>Seconds: a few frames.</summary>
    public const float TimeSeconds = 0.05f;

    /// <summary>Relative error for float identities.</summary>
    public const float FloatRelative = 1e-4f;

    /// <summary>Absolute share error for proportion and distribution-mean checks with small samples.</summary>
    public const float Proportion = 0.1f;

    /// <summary>Hours: world time (3.6 s of world clock).</summary>
    public const double WorldHours = 1e-3;

    /// <summary>Budgets are set targets: a measurement fails only this many times over its budget (WF-RULE).</summary>
    public const float BudgetFactor = 10f;
}

/// <summary>Upper bounds on sample counts at demo precision. Tests loop at most this many times.</summary>
internal static class TestSamples
{
    /// <summary>Samples per property for property-style checks.</summary>
    public const int PerProperty = 256;

    /// <summary>Weather seeds swept by a weather test.</summary>
    public const int WeatherSeeds = 50;

    /// <summary>Iterations of a per-frame loop.</summary>
    public const int FrameLoop = 2000;

    /// <summary>Cells compared by a whole-zone determinism test, picked heaviest first.</summary>
    public const int HeaviestCells = 3;
}
