using Campus.Contracts.AppFlow;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;

namespace Campus.Contracts.Startup;

public enum BootRunMode : byte { Interactive, Capture, CpuBake }
public enum StartupScenario : byte { Immediate, Route }

/// <summary>Parsed startup choices. Map-dependent defaults are resolved after catalogue loading.</summary>
public sealed record BootRunOptions
{
    public BootRunMode Mode { get; init; }
    public bool Automated { get; init; }
    public bool FreezeAnimation { get; init; }
    public bool Title { get; init; } = true;
    /// <summary>Command-wide map selection for CPU bake or registry checks; Flow.Map holds only a concrete map ID.</summary>
    public bool AllMaps { get; init; }
    public string? SaveDirectory { get; init; }
    /// <summary>Explicit AI performance root; does not change interactive run behavior or author settings.</summary>
    public string? PerfOutputDirectory { get; init; }
    public PerfCollectionOptions? PerfCollection { get; init; }
    public IReadOnlyList<string> TravelSequence { get; init; } = [];
    /// <summary>In-process player input sampling; retains normal interactive startup defaults.</summary>
    public StartupScenario? StartupScenario { get; init; }
    public StartupRouteOptions? StartupRoute { get; init; }
    public BootFlowRequest Flow { get; init; } = new();
    public WorldBootOptions World { get; init; } = new();
    public UiBootOptions Ui { get; init; } = new();
    public AudioBootOptions Audio { get; init; } = new();
    public SaveBootOptions Save { get; init; } = new();
    public BakeBootOptions Bake { get; init; } = new();
    public CaptureBootOptions Capture { get; init; } = new();
    public QualityBootOptions Quality { get; init; } = new();
    public bool HasLoad => Flow.Slot is not null || Flow.LoadFile is not null;
    public bool CaptureMode => Mode == BootRunMode.Capture;
    public bool CpuBakeMode => Mode == BootRunMode.CpuBake;
}

/// <summary>A requested map waypoint and continuous formal input route; the driver verifies the actual start.</summary>
public sealed record StartupRouteOptions(string Start, ViewMode Mode, double DurationSeconds);

public sealed record WorldBootOptions
{
    public double? Clock { get; init; }
    public Season? Season { get; init; }
    public double? AnimationTime { get; init; }
    public uint? Seed { get; init; }
    public int? FlowRate { get; init; }
    public ViewMode? ViewMode { get; init; }
    public string? Pose { get; init; }
    public WeatherBootOverrides Weather { get; init; } = new();
}

public sealed record WeatherBootOverrides(uint? Seed = null, WeatherKind? Kind = null,
    float? Intensity = null, float? Wet = null, double? RemainingMinutes = null, bool Requested = false);

// UI uses explicit overrides, unlike effective capture canvas and audio choices.
public sealed record UiBootOptions(bool? Visible = null, float? Scale = null, string? Gallery = null,
    string? ExportTheme = null, MotionLevel? Motion = null, UiThemeMode? Theme = null);
public sealed record AudioBootOptions(bool Enabled = true, bool Fixed = false, bool Limiter = true);
public sealed record SaveBootOptions(bool Enabled = true, SlotId? WriteSlot = null,
    int? AtFrame = null, uint? WeatherSeed = null);
// Bake workers use explicit inputs; capture defaults do not change these choices.
public sealed record BakeBootOptions(bool Live = false, bool Strict = false,
    bool Background = true, bool MapOnly = false, uint? Seed = null);
public sealed record CaptureBootOptions
{
    public string? Gate { get; init; }
    public string? Inspect { get; init; }
    public string? Output { get; init; }
    public IReadOnlyList<string> Shots { get; init; } = [];
    public int? FrameLimit { get; init; }
    public int? MovieFrames { get; init; }
    public IReadOnlyList<CaptureMovieEvent> MovieEvents { get; init; } = [];
    public bool IncludeCanvas { get; init; } = true;
}
/// <summary>Zero-based frames after the fixed capture pose has been presented.</summary>
public sealed record CaptureMovieEvent(int Frame, Season? Season = null, double? Clock = null,
    WeatherKind? Weather = null, bool AutomaticWeather = false, float? Intensity = null);

public sealed record QualityBootOptions
{
    public QualityPreset? Preset { get; init; }
    public IReadOnlyDictionary<QualityKnob, float> Overrides { get; init; } = new Dictionary<QualityKnob, float>();
    public bool Requested => Preset is not null || Overrides.Count > 0;
}
