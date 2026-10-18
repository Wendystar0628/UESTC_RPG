namespace Campus.Contracts;

/// <summary>Parsed new-world weather choices. Loaded worlds restore their weather save section instead.</summary>
public sealed record WeatherBoot(
    uint Seed,                                  // --weather-seed; ISaveService.NewGame chooses entropy when omitted.
    WeatherKind? Kind = null,                   // --weather; auto has no explicit kind.
    float? Intensity = null, float? Wet = null,  // --weather-intensity, --wet.
    double? RemainingMinutes = null);           // --weather-remaining.
