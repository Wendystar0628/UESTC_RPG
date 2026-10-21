using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Gen.World;

namespace Campus.Gen.Diorama;

/// <summary>
/// The map's backdrop parameters (WLD-OUTSKIRTS-DIORAMA "分工"; SYS-MAPS): fog sea, the two city rings, the far ring with its hills
/// and snow mountains, and the seasonal snow table. Read from the bytes of <c>env.backdrop</c> (清水河: backdrop.json) that the
/// caller preloads into <c>BuildContext.SourceInputs</c>; the generator performs no IO and keeps no map constants in code.
/// Azimuths are compass bearings in degrees (0 north, 90 east); world north is −z, east is +x (STD-CONVENTIONS).
/// </summary>
public sealed record BackdropData(
    float FarClip,
    FogSeaData FogSea,
    IReadOnlyList<CityRingData> CityRings,
    IReadOnlyList<SectorData> Sectors,
    FarRingData FarRing,
    SnowData Snow)
{
    public const int Schema = 1;

    private static readonly BackdropJsonContext Context = CreateContext();

    public static BackdropData Parse(ReadOnlySpan<byte> json)
    {
        var file = WireJson.Read(json, Context.BackdropWire, "backdrop");
        if (file.Schema != Schema) throw new InvalidDataException($"backdrop: schema must be {Schema}.");
        if (file.Snow.Visibility.Count != 4 || file.Snow.Snowline.Count != 4)
            throw new InvalidDataException("backdrop: snow tables need four seasons (spring, summer, autumn, winter).");
        if (file.CityRings.Any(r => r is null) || file.Sectors.Any(s => s is null) || file.FarRing.Hills.Any(h => h is null)
            || file.FarRing.Mountains.Any(m => m is null || m.Peaks.Any(p => p is null)))
            throw new InvalidDataException("backdrop: list members must not be null.");
        var data = new BackdropData(file.FarClip, file.FogSea, file.CityRings,
            file.Sectors.Select(s => new SectorData(s.From, s.To, s.Near.ToData(), s.Mid.ToData())).ToArray(),
            file.FarRing, file.Snow);
        data.Validate();
        return data;
    }

    private static BackdropJsonContext CreateContext()
    {
        var options = new JsonSerializerOptions(WireJson.Options);
        options.Converters.Add(new BackdropFiniteSingleConverter());
        return new(options);
    }

    /// <summary>Range checks that keep the products inside the camera's far clip and the encodings exact.</summary>
    public void Validate()
    {
        static void Check(bool ok, string what)
        {
            if (!ok) throw new InvalidDataException("backdrop: " + what);
        }
        Check(FarClip > 0f, "farClip must be positive");
        Check(FogSea.TopY < 0f && FogSea.RiseTopY < 0f && FogSea.RiseTopY > FogSea.TopY, "fog sea tops must lie below ground, the fog-weather top above the clear one");
        Check(FogSea.OuterRadius > FarRing.Radius && FogSea.OuterRadius < FarClip, "fog sea must reach past the far ring and stay inside farClip");
        Check(FogSea.Segments is >= 32 and <= 2048, "fog sea segments 32-2048");
        Check(FogSea.EdgeBank.Inside >= 0f && FogSea.EdgeBank.Outside > 0f && FogSea.EdgeBank.WaveLength > 0f &&
            FogSea.EdgeBank.Wander >= 0f && FogSea.EdgeBank.Wander < FogSea.EdgeBank.Inside * 0.7f, "edge bank widths (wander < 0.7 × inside, so the bank always starts under the ground)");
        Check(CityRings.Count is >= 1 and <= 4, "one to four city rings");
        var previous = 0f;
        foreach (var ring in CityRings)
        {
            Check(ring.Radius > previous && ring.Radius < FarRing.Radius, $"city ring {ring.Id} radius must grow and stay inside the far ring");
            Check(ring.Parallax is >= 0f and < 1f, $"city ring {ring.Id} parallax 0-1");
            Check(ring.Haze is >= 0f and <= 1f && ring.RowHaze >= 0f, $"city ring {ring.Id} haze 0-1");
            Check(ring.Rows is >= 1 and <= 6 && ring.Depth >= 0f && ring.Depth < ring.Radius * 0.5f, $"city ring {ring.Id} rows and depth");
            Check(ring.BaseY < FogSea.TopY, $"city ring {ring.Id} must sink below the fog sea top");
            previous = ring.Radius;
        }
        Check(Sectors.Count > 0, "at least one sector");
        foreach (var s in Sectors)
            foreach (var f in new[] { s.Near, s.Mid })
                Check(f.Fill is >= 0f and <= 1f && f.TowerShare is >= 0f and <= 1f && f.Low.Min > 0f && f.Low.Max >= f.Low.Min &&
                    f.High.Min > 0f && f.High.Max >= f.High.Min, $"sector {s.From}-{s.To} fill and heights");
        Check(FarRing.Parallax == 1f, "the far ring sits at infinity: parallax 1 (WLD-OUTSKIRTS-DIORAMA)");
        Check(FarRing.Rows is >= 2 and <= 16 && FarRing.Depth > 0f && FarRing.Depth < FarRing.Radius * 0.5f, "far ring rows and depth");
        Check(FarRing.SkirtY < FogSea.TopY, "far ring skirt must sink below the fog sea");
        Check(FarRing.StepDeg is > 0.05f and <= 2f && FarRing.FineStepDeg > 0.02f && FarRing.FineStepDeg <= FarRing.StepDeg, "far ring steps");
        Check(FarRing.VisualScale is >= 1f and <= 4f, "visual scale 1-4");
        Check(FarRing.Hills.Count >= 2, "at least two hill keys");
        foreach (var m in FarRing.Mountains)
            Check(m.Peaks.Count > 0 && m.Peaks.All(p => p.Height > 0f && p.HalfWidthDeg > 0f) && m.TrueElevationDeg > 0f, $"mountain {m.Name}");
        Check(Snow.Visibility.All(v => v is >= 0f and <= 1f) && Snow.Snowline.All(v => v is >= 0f and <= 1f), "snow table values 0-1");
    }
}

/// <param name="TopY">Clear-weather top of the fog sea (m), below the campus ground (WLD-OUTSKIRTS-DIORAMA).</param>
/// <param name="RiseTopY">Highest top in fog weather, about 1 m under the cut edge (WLD-OUTSKIRTS-DIORAMA).</param>
public sealed record FogSeaData(float TopY, float OuterRadius, float RiseTopY, int Segments, EdgeBankData EdgeBank);

/// <summary>
/// The fog bank along the inner edge: the fog surface climbs to <see cref="Height"/> just inside the ground's edge so the edge
/// of the world sinks into fog. Height 0 or below keeps the inner edge flat under the ground (for a map whose cut-line section
/// is drawn, WLD-OUTSKIRTS-DIORAMA). Wander shifts the bank in and out along the edge (m), Jitter scales its crest.
/// </summary>
public sealed record EdgeBankData(float Height, float Inside, float Outside, float Jitter, float WaveLength, float Wander);

public sealed record CityRingData(string Id, float Radius, float Parallax, float Haze, float RowHaze, float Depth, int Rows, float BaseY);

/// <summary>A compass sector of the city rings: from → to clockwise (wrapping past 360).</summary>
public sealed record SectorData(float From, float To, SectorFill Near, SectorFill Mid);

/// <param name="Fill">Share of building slots that hold a building.</param>
/// <param name="TowerShare">Share of buildings that are residential towers (the rest are low blocks).</param>
public sealed record SectorFill(float Fill, float TowerShare, (float Min, float Max) Low, (float Min, float Max) High);

public sealed record HillKey(float Azimuth, float ElevationDeg);

public sealed record PeakData(float OffsetDeg, float Height, float HalfWidthDeg);

public sealed record MountainData(string Name, float Azimuth, float DistanceKm, float TrueElevationDeg, IReadOnlyList<PeakData> Peaks);

public sealed record FarRingData(float Radius, float Parallax, float Depth, int Rows, float SkirtY, float StepDeg, float FineStepDeg,
    float VisualScale, IReadOnlyList<HillKey> Hills, IReadOnlyList<MountainData> Mountains);

/// <summary>Seasonal snow table (WLD-OUTSKIRTS-DIORAMA), each array spring, summer, autumn, winter.</summary>
public sealed record SnowData(IReadOnlyList<float> Visibility, IReadOnlyList<float> Snowline);

// The wire alone carries schema/commentary and JSON's two-element height arrays.
internal sealed record BackdropWire(int Schema, float FarClip, FogSeaData FogSea, IReadOnlyList<CityRingData> CityRings,
    IReadOnlyList<BackdropSectorWire> Sectors, FarRingData FarRing, SnowData Snow,
    string? Doc = null, IReadOnlyList<string>? Notes = null);

internal sealed record BackdropSectorWire(float From, float To, BackdropFillWire Near, BackdropFillWire Mid);

internal sealed record BackdropFillWire(float Fill, float TowerShare, Vector2 Low, Vector2 High)
{
    // WireJson inherits ContractJson's finite, exactly-two-number vector reader.
    public SectorFill ToData() => new(Fill, TowerShare, (Low.X, Low.Y), (High.X, High.Y));
}

// Strict JSON numbers can still overflow Single. Keep all backdrop floats finite,
// including array members and fields whose domain checks do not constrain their range.
internal sealed class BackdropFiniteSingleConverter : JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetSingle(out var value) || !float.IsFinite(value))
            throw new JsonException("backdrop numbers must be finite Single values.");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}

[JsonSerializable(typeof(BackdropWire))]
internal partial class BackdropJsonContext : JsonSerializerContext { }
