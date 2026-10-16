using System.Text.Json;

namespace Campus.Gen.Diorama;

/// <summary>
/// The map's backdrop parameters (WLD-DIORAMA "分工"; SYS-MAPS §2.1): fog sea, the two city rings, the far ring with its hills
/// and snow mountains, and the seasonal snow table. Read from the bytes of <c>env.backdrop</c> (清水河: backdrop.json) that the
/// caller preloads into <c>BuildContext.SourceInputs</c>; the generator performs no IO and keeps no map constants in code.
/// Azimuths are compass bearings in degrees (0 north, 90 east); world north is −z, east is +x (CORE-CONVENTIONS §1).
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

    public static BackdropData Parse(ReadOnlySpan<byte> json)
    {
        using var document = JsonDocument.Parse(json.ToArray());
        var root = document.RootElement;
        if (Int(root, "schema") != Schema) throw new InvalidDataException($"backdrop: schema must be {Schema}.");
        var fog = Obj(root, "fogSea");
        var bank = Obj(fog, "edgeBank");
        var fogSea = new FogSeaData(Num(fog, "topY"), Num(fog, "outerRadius"), Num(fog, "riseTopY"), Int(fog, "segments"),
            new EdgeBankData(Num(bank, "height"), Num(bank, "inside"), Num(bank, "outside"), Num(bank, "jitter"), Num(bank, "waveLength"), Num(bank, "wander")));
        var rings = Arr(root, "cityRings").Select(r => new CityRingData(Str(r, "id"), Num(r, "radius"), Num(r, "parallax"),
            Num(r, "haze"), Num(r, "rowHaze"), Num(r, "depth"), Int(r, "rows"), Num(r, "baseY"))).ToArray();
        var sectors = Arr(root, "sectors").Select(s => new SectorData(Num(s, "from"), Num(s, "to"),
            Fill(Obj(s, "near")), Fill(Obj(s, "mid")))).ToArray();
        var far = Obj(root, "farRing");
        var hills = Arr(far, "hills").Select(h => new HillKey(Num(h, "azimuth"), Num(h, "elevationDeg"))).ToArray();
        var mountains = Arr(far, "mountains").Select(m => new MountainData(Str(m, "name"), Num(m, "azimuth"), Num(m, "distanceKm"),
            Num(m, "trueElevationDeg"), Arr(m, "peaks").Select(p => new PeakData(Num(p, "offsetDeg"), Num(p, "height"), Num(p, "halfWidthDeg"))).ToArray())).ToArray();
        var farRing = new FarRingData(Num(far, "radius"), Num(far, "parallax"), Num(far, "depth"), Int(far, "rows"), Num(far, "skirtY"),
            Num(far, "stepDeg"), Num(far, "fineStepDeg"), Num(far, "visualScale"), hills, mountains);
        var snow = Obj(root, "snow");
        var data = new BackdropData(Num(root, "farClip"), fogSea, rings, sectors, farRing,
            new SnowData(Four(snow, "visibility"), Four(snow, "snowline")));
        data.Validate();
        return data;

        static SectorFill Fill(JsonElement e) => new(Num(e, "fill"), Num(e, "towerShare"), Pair(e, "low"), Pair(e, "high"));
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
        Check(FarRing.Parallax == 1f, "the far ring sits at infinity: parallax 1 (WLD-DIORAMA §5.4)");
        Check(FarRing.Rows is >= 2 and <= 16 && FarRing.Depth > 0f && FarRing.Depth < FarRing.Radius * 0.5f, "far ring rows and depth");
        Check(FarRing.SkirtY < FogSea.TopY, "far ring skirt must sink below the fog sea");
        Check(FarRing.StepDeg is > 0.05f and <= 2f && FarRing.FineStepDeg > 0.02f && FarRing.FineStepDeg <= FarRing.StepDeg, "far ring steps");
        Check(FarRing.VisualScale is >= 1f and <= 4f, "visual scale 1-4");
        Check(FarRing.Hills.Count >= 2, "at least two hill keys");
        foreach (var m in FarRing.Mountains)
            Check(m.Peaks.Count > 0 && m.Peaks.All(p => p.Height > 0f && p.HalfWidthDeg > 0f) && m.TrueElevationDeg > 0f, $"mountain {m.Name}");
        Check(Snow.Visibility.All(v => v is >= 0f and <= 1f) && Snow.Snowline.All(v => v is >= 0f and <= 1f), "snow table values 0-1");
    }

    private static JsonElement Obj(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Object ? v : throw new InvalidDataException($"backdrop: missing object '{name}'.");
    private static IEnumerable<JsonElement> Arr(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().ToArray() : throw new InvalidDataException($"backdrop: missing array '{name}'.");
    private static float Num(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Number) throw new InvalidDataException($"backdrop: missing number '{name}'.");
        var f = v.GetSingle();
        return float.IsFinite(f) ? f : throw new InvalidDataException($"backdrop: '{name}' is not finite.");
    }
    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : throw new InvalidDataException($"backdrop: missing integer '{name}'.");
    private static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : throw new InvalidDataException($"backdrop: missing string '{name}'.");
    private static (float Min, float Max) Pair(JsonElement e, string name)
    {
        var a = Arr(e, name).ToArray();
        if (a.Length != 2) throw new InvalidDataException($"backdrop: '{name}' needs two numbers.");
        return (a[0].GetSingle(), a[1].GetSingle());
    }
    private static float[] Four(JsonElement e, string name)
    {
        var a = Arr(e, name).Select(x => x.GetSingle()).ToArray();
        return a.Length == 4 ? a : throw new InvalidDataException($"backdrop: '{name}' needs four seasons (spring, summer, autumn, winter).");
    }
}

/// <param name="TopY">Clear-weather top of the fog sea (m), below the campus ground (WLD-DIORAMA §4.2).</param>
/// <param name="RiseTopY">Highest top in fog weather, about 1 m under the cut edge (§4.3).</param>
public sealed record FogSeaData(float TopY, float OuterRadius, float RiseTopY, int Segments, EdgeBankData EdgeBank);

/// <summary>
/// The fog bank along the inner edge: the fog surface climbs to <see cref="Height"/> just inside the ground's edge so the edge
/// of the world sinks into fog. Height 0 or below keeps the inner edge flat under the ground (for a map whose cut-line section
/// is drawn, WLD-DIORAMA §2). Wander shifts the bank in and out along the edge (m), Jitter scales its crest.
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

/// <summary>Seasonal snow table (WLD-DIORAMA §6.3), each array spring, summer, autumn, winter.</summary>
public sealed record SnowData(IReadOnlyList<float> Visibility, IReadOnlyList<float> Snowline);
