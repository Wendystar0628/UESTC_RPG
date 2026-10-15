using System.Text.Json;
using Campus.Contracts;

namespace Campus.Systems.Sky;

/// <summary>Map-dependent sky parameters read from <c>&lt;DataRoot&gt;sky.json</c> (ENG-WP §2, WP-40).</summary>
/// <param name="CloudDrift">Prevailing cloud drift for f(world time) (SYS-WEATHER §7).</param>
public sealed record SkyMapData(CloudDrift CloudDrift)
{
    public const string FileName = "sky.json";

    /// <summary>Used when a map ships no sky.json: a slow eastward drift.</summary>
    public static SkyMapData Fallback { get; } = new(new CloudDrift(90f, 6f));

    public static SkyMapData Parse(ReadOnlySpan<byte> json)
    {
        var data = JsonSerializer.Deserialize<SkyMapData>(json, ContractJson.Options)
            ?? throw new InvalidDataException("sky.json is empty.");
        if (data.CloudDrift is null) throw new InvalidDataException("sky.json needs cloudDrift.");
        data.CloudDrift.Validate();
        return data;
    }
}

/// <param name="TowardDeg">Azimuth the clouds move toward (0 = north, 90 = east, CORE-INDEX).</param>
/// <param name="SpeedMps">World-time speed in metres per world second; fast flow turns it into a time-lapse.</param>
public sealed record CloudDrift(float TowardDeg, float SpeedMps)
{
    public void Validate()
    {
        if (!float.IsFinite(TowardDeg) || TowardDeg < 0f || TowardDeg >= 360f)
            throw new InvalidDataException("cloudDrift.towardDeg must be in [0, 360).");
        if (!float.IsFinite(SpeedMps) || SpeedMps < 0f || SpeedMps > 40f)
            throw new InvalidDataException("cloudDrift.speedMps must be in [0, 40].");
    }
}
