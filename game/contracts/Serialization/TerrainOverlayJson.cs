using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Campus.Contracts;

/// <summary>Strict, source-generated, memory-only terrain wire boundary. World/reference validation remains the evaluator's responsibility.</summary>
public static class TerrainOverlayJson
{
    private static readonly TerrainOverlayJsonContext Context = CreateContext();

    private static TerrainOverlayJsonContext CreateContext()
    {
        var options = new JsonSerializerOptions(ContractJson.Options)
        {
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectRequiredConstructorParameters = true,
            RespectNullableAnnotations = true,
            AllowOutOfOrderMetadataProperties = false,
            NumberHandling = JsonNumberHandling.Strict
        };
        // Exact local vocabularies precede the repo-wide enum converter: non-Flags comma lists must not
        // be OR-combined into a different valid terrain kind (for example soft | steps becomes wetland).
        options.Converters.Insert(0, new TerrainEnumConverter<TerrainProfile>(("hermite", TerrainProfile.Hermite)));
        options.Converters.Insert(0, new TerrainEnumConverter<TerrainBankKind>(("hard", TerrainBankKind.Hard),
            ("soft", TerrainBankKind.Soft), ("steps", TerrainBankKind.Steps), ("wetland", TerrainBankKind.Wetland),
            ("swale", TerrainBankKind.Swale)));
        options.Converters.Insert(0, new TerrainEnumConverter<TerrainWeirKind>(("weir", TerrainWeirKind.Weir),
            ("gate", TerrainWeirKind.Gate), ("culvert", TerrainWeirKind.Culvert)));
        options.Converters.Insert(0, new TerrainEnumConverter<TerrainEdgeKind>(("edgeBand", TerrainEdgeKind.EdgeBand),
            ("terrace", TerrainEdgeKind.Terrace)));
        options.Converters.Insert(0, new TerrainBankConverter());
        options.Converters.Insert(0, new TerrainCrossingConverter());
        return new(options);
    }

    private sealed class TerrainBankConverter : JsonConverter<TerrainBank>
    {
        public override TerrainBank Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var fields = Fields(document.RootElement, "kind", "slope", "inner", "outer");
            if (!fields.TryGetValue("kind", out var kindElement)) throw new JsonException("Bank kind is required.");
            var kind = JsonSerializer.Deserialize(kindElement, Context.TerrainBankKind);
            TerrainBank bank;
            switch (kind)
            {
                case TerrainBankKind.Swale:
                    if (fields.ContainsKey("slope") || !fields.TryGetValue("inner", out var inner) || inner.ValueKind != JsonValueKind.String)
                        throw new JsonException("Swale requires inner and forbids slope.");
                    string? outer = null;
                    if (fields.TryGetValue("outer", out var value))
                    {
                        if (value.ValueKind == JsonValueKind.String) outer = value.GetString();
                        else if (value.ValueKind != JsonValueKind.Number || value.GetDouble() != 0)
                            throw new JsonException("Swale outer must be a positive 1:n ratio, numeric zero, or omitted.");
                    }
                    bank = new(kind) { Inner = inner.GetString(), Outer = outer };
                    break;
                case TerrainBankKind.Hard:
                case TerrainBankKind.Soft:
                case TerrainBankKind.Steps:
                case TerrainBankKind.Wetland:
                    if (fields.ContainsKey("inner") || fields.ContainsKey("outer") ||
                        !fields.TryGetValue("slope", out var slope) || slope.ValueKind != JsonValueKind.String)
                        throw new JsonException("Wet-bank kinds require slope and forbid inner/outer.");
                    bank = new(kind, slope.GetString());
                    break;
                default: throw new JsonException("Unknown bank kind.");
            }
            try { Bank(bank); }
            catch (ArgumentException error) { throw new JsonException(error.Message, error); }
            return bank;
        }

        public override void Write(Utf8JsonWriter writer, TerrainBank value, JsonSerializerOptions options)
        {
            try { Bank(value); }
            catch (ArgumentException error) { throw new JsonException(error.Message, error); }
            writer.WriteStartObject();
            writer.WritePropertyName("kind"); JsonSerializer.Serialize(writer, value.Kind, Context.TerrainBankKind);
            switch (value.Kind)
            {
                case TerrainBankKind.Swale:
                    writer.WriteString("inner", value.Inner);
                    if (value.Outer is not null) writer.WriteString("outer", value.Outer);
                    break;
                case TerrainBankKind.Hard:
                case TerrainBankKind.Soft:
                case TerrainBankKind.Steps:
                case TerrainBankKind.Wetland:
                    writer.WriteString("slope", value.Slope);
                    break;
                default: throw new JsonException("Unknown bank kind.");
            }
            writer.WriteEndObject();
        }
    }

    private sealed class TerrainCrossingConverter : JsonConverter<TerrainCrossing>
    {
        public override TerrainCrossing Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var fields = Fields(document.RootElement, "road", "at", "endBefore");
            if (fields.Count != 2 || !fields.TryGetValue("road", out var road) || road.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(road.GetString())) throw new JsonException("A crossing needs a real road ID and exactly one locator form.");
            var id = new FeatureId(road.GetString()!);
            if (fields.TryGetValue("at", out var at))
            {
                if (at.ValueKind != JsonValueKind.Array || at.GetArrayLength() != 2 ||
                    at[0].ValueKind != JsonValueKind.Number || at[1].ValueKind != JsonValueKind.Number)
                    throw new JsonException("Crossing at must be one [x,z] point.");
                var point = new Vector2(at[0].GetSingle(), at[1].GetSingle());
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new JsonException("Crossing at must be finite.");
                return new TerrainUnderpassCrossing(id, point);
            }
            if (fields.TryGetValue("endBefore", out var endBefore) && endBefore.ValueKind == JsonValueKind.True)
                return new TerrainEndBeforeCrossing(id);
            throw new JsonException("Crossing must be {road,at} or {road,endBefore:true}, never both.");
        }

        public override void Write(Utf8JsonWriter writer, TerrainCrossing value, JsonSerializerOptions options)
        {
            try { Crossing(value); }
            catch (ArgumentException error) { throw new JsonException(error.Message, error); }
            writer.WriteStartObject(); writer.WriteString("road", value.Road.Value);
            switch (value)
            {
                case TerrainUnderpassCrossing underpass:
                    writer.WritePropertyName("at"); writer.WriteStartArray();
                    writer.WriteNumberValue(underpass.At.X); writer.WriteNumberValue(underpass.At.Y); writer.WriteEndArray();
                    break;
                case TerrainEndBeforeCrossing:
                    writer.WriteBoolean("endBefore", true); break;
                default: throw new JsonException("Unknown crossing form.");
            }
            writer.WriteEndObject();
        }
    }

    private static Dictionary<string, JsonElement> Fields(JsonElement element, params string[] allowed)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new JsonException("Expected a terrain object.");
        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var field in element.EnumerateObject())
            if (!allowed.Contains(field.Name, StringComparer.Ordinal) || !fields.TryAdd(field.Name, field.Value))
                throw new JsonException($"Unknown or duplicate terrain field: {field.Name}");
        return fields;
    }

    private sealed class TerrainEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private readonly Dictionary<string, T> _read = new(StringComparer.Ordinal);
        private readonly Dictionary<T, string> _write = [];
        public TerrainEnumConverter(params (string Name, T Value)[] values)
        {
            foreach (var (name, value) in values) { _read.Add(name, value); _write.Add(value, name); }
        }
        public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String || !_read.TryGetValue(reader.GetString()!, out var value))
                throw new JsonException($"Expected one exact {typeof(T).Name} token.");
            return value;
        }
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (!_write.TryGetValue(value, out var name)) throw new JsonException($"Unknown {typeof(T).Name} value.");
            writer.WriteStringValue(name);
        }
    }

    public static TerrainOverlayFile Parse(ReadOnlySpan<byte> utf8)
    {
        try
        {
            var data = JsonSerializer.Deserialize(utf8, Context.TerrainOverlayFile) ?? throw new JsonException("Null terrain overlay.");
            Validate(data);
            return data;
        }
        catch (ArgumentException error) { throw new JsonException(error.Message, error); }
        // System.Text.Json reports a missing discriminator for an abstract base as NotSupportedException.
        // Keep one public input-error boundary without weakening rejection or hiding the original exception.
        catch (NotSupportedException error) { throw new JsonException(error.Message, error); }
    }

    public static byte[] ToUtf8Bytes(TerrainOverlayFile data)
    {
        Validate(data);
        return JsonSerializer.SerializeToUtf8Bytes(data, Context.TerrainOverlayFile);
    }

    /// <summary>Checks DTO shape, numeric boundaries and same-channel chained-weir spacing, not entity existence,
    /// world topology, slope feasibility, structural clearances or zone geometry.</summary>
    public static void Validate(TerrainOverlayFile data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Text(data.Zone.Value, "zone");
        if (data.Map is { } map) Text(map.Value, "map");
        ArgumentNullException.ThrowIfNull(data.Ops);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var op in data.Ops)
        {
            ArgumentNullException.ThrowIfNull(op);
            if (op.Id is null || !Regex.IsMatch(op.Id, "\\Adesign:terrain:[a-z0-9-]+\\z", RegexOptions.CultureInvariant))
                throw new ArgumentException("Invalid terrain operation id.");
            if (!ids.Add(op.Id)) throw new ArgumentException($"Duplicate terrain operation id: {op.Id}");
            Text(op.Reason, "reason");
            if (op.Zone != data.Zone) throw new ArgumentException($"{op.Id}: operation zone does not match its file.");
            if (op.Priority < 0) throw new ArgumentException($"{op.Id}: priority must be a non-negative int32.");
            try { ValidateOperation(op); }
            catch (ArgumentException error) { throw new ArgumentException($"{op.Id}: {error.Message}", nameof(data), error); }
        }
    }

    /// <summary>Checks explicit map-level parameters for automatic operations; does not validate world geometry.</summary>
    public static void ValidateTerrainDefaults(Maps.TerrainDefaults data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Ratio(data.LakeBankSlope, "terrainDefaults.lakeBankSlope");
        Positive(data.LakeMaxDepth, "terrainDefaults.lakeMaxDepth");
        Ratio(data.ChannelBankSlope, "terrainDefaults.channelBankSlope");
        Positive(data.ChannelDepth, "terrainDefaults.channelDepth");
        Positive(data.CorridorShoulder, "terrainDefaults.corridorShoulder");
    }

    private static void ValidateOperation(TerrainOp op)
    {
        switch (op)
        {
            case TerrainPlatformOp p:
                Polygon(p.Rings); Elevation(p.Y, "y"); Side(p.Side); Minimum(p.Fillet, 2, "fillet"); break;
            case TerrainMoundOp m:
                Shape(m.Shape); Positive(m.Height, "height"); Profile(m.Profile); break;
            case TerrainBasinOp b:
                Shape(b.Shape); Positive(b.Height, "height"); Profile(b.Profile); break;
            case TerrainSlopeOp s:
                ValidateSlope(s);
                break;
            case TerrainTerraceOp t:
                Line(t.Front); Point(t.Inward);
                if (t.Inward == Vector2.Zero) throw new ArgumentException("Terrace inward direction cannot be zero.");
                Elevation(t.FrontY, "frontY"); Count(t.Tiers, "tiers"); Positive(t.Riser, "riser"); Maximum(t.Riser, 1, "riser"); Positive(t.Tread, "tread");
                ArgumentNullException.ThrowIfNull(t.StairAt);
                foreach (var station in t.StairAt) Minimum(station, 0, "stairAt");
                break;
            case TerrainChannelOp c:
                ValidateChannel(c);
                break;
            case TerrainLakeOp l:
                Reference(l.Water, "water"); Elevation(l.WaterY, "waterY"); Bank(l.Bank); ArgumentNullException.ThrowIfNull(l.Bed);
                if (l.Bank.Kind == TerrainBankKind.Swale) throw new ArgumentException("A lake cannot use swale bank geometry.");
                Range(l.Bed.MaxDepth, 1.2, 1.8, "bed.maxDepth"); Ratio(l.Bed.Shelf, "bed.shelf"); ArgumentNullException.ThrowIfNull(l.Wade);
                foreach (var polygon in l.Wade) Polygon(polygon);
                break;
            case TerrainPadOp p:
                Reference(p.Feature, "feature"); Positive(p.Grade, "grade"); Maximum(p.Grade, 6, "grade"); Minimum(p.Apron, 0, "apron");
                if (p.PadY is { } padY) Elevation(padY, "padY");
                if (p.Edge is { } edge)
                {
                    Text(edge.StructureId, "edge.structureId"); Positive(edge.Riser, "edge.riser"); Maximum(edge.Riser, 1, "edge.riser"); Positive(edge.Tread, "edge.tread"); Count(edge.Tiers, "edge.tiers");
                    _ = edge.Kind switch { TerrainEdgeKind.EdgeBand or TerrainEdgeKind.Terrace => 0, _ => throw new ArgumentException("Unknown edge kind.") };
                }
                break;
            case TerrainCorridorOp c:
                ValidateCorridor(c);
                break;
            default: throw new ArgumentException("Unknown terrain operation type.");
        }
    }

    private static void ValidateSlope(TerrainSlopeOp s)
    {
        Elevation(s.Y0, "y0"); Elevation(s.Y1, "y1"); Finite(s.CrossFall, "crossFall"); Side(s.Side); Minimum(s.Fillet, 2, "fillet");
        switch (s.Geometry)
        {
            case PolygonGeometry polygon:
                Polygon(polygon.Rings);
                if (s.From is not { } from || s.To is not { } to || from == to || s.Width is not null)
                    throw new ArgumentException("Polygon slope requires distinct from/to points and no width.");
                Point(from); Point(to); break;
            case PolylineGeometry polyline:
                Line(polyline.Line);
                if (s.Width is not { } width || s.From is not null || s.To is not null)
                    throw new ArgumentException("Polyline slope requires width and no from/to points.");
                Positive(width, "width"); break;
            default: throw new ArgumentException("Stage 1 slopes accept only polygon or polyline geometry.");
        }
    }

    private static void ValidateChannel(TerrainChannelOp c)
    {
        Positive(c.Depth, "depth"); Bank(c.Bank);
        if (c.Line is not null) { Line(c.Line); if (c.Width is null) throw new ArgumentException("An explicit channel line requires width."); }
        if (c.Width is { } channelWidth) Positive(channelWidth, "width");
        ArgumentNullException.ThrowIfNull(c.Weirs);
        ArgumentNullException.ThrowIfNull(c.Wade);
        if (c.Dry && c.Wade.Count != 0) throw new ArgumentException("A dry channel cannot register wade polygons.");
        foreach (var polygon in c.Wade) Polygon(polygon);
        if (c.Dry)
        {
            if (c.Water is not null || c.WaterY is not null || c.Line is null || c.Width is null || c.Weirs.Count != 0)
                throw new ArgumentException("Explicit dry channels require null water/waterY, line/width, and no water-level weirs.");
            if (c.Bank.Kind != TerrainBankKind.Swale) throw new ArgumentException("Stage 1 dry channels require a swale bank.");
        }
        else
        {
            if (c.Water is not { } water || c.WaterY is not { } waterY)
                throw new ArgumentException("A wet channel needs a real water ID and constant waterY; null does not imply a dry channel.");
            Reference(water, "water"); Elevation(waterY, "waterY");
            if (c.Bank.Kind == TerrainBankKind.Swale) throw new ArgumentException("A wet channel cannot use swale bank geometry.");
        }
        foreach (var weir in c.Weirs)
        {
            ArgumentNullException.ThrowIfNull(weir);
            Minimum(weir.S, 0, "weirs.s"); Range(weir.Drop, 0.3, 0.6, "weirs.drop");
            Reference(weir.Structure, "weirs.structure"); Reference(weir.Upstream, "weirs.upstream"); Reference(weir.Downstream, "weirs.downstream");
            if (weir.Upstream == weir.Downstream || weir.Structure == weir.Upstream || weir.Structure == weir.Downstream ||
                c.Water != weir.Upstream && c.Water != weir.Downstream)
                throw new ArgumentException("A weir needs a distinct structure and two distinct water IDs, including this channel's water ID.");
            _ = weir.Kind switch { TerrainWeirKind.Weir or TerrainWeirKind.Gate or TerrainWeirKind.Culvert => 0, _ => throw new ArgumentException("Unknown weir kind.") };
        }
        for (var i = 0; i < c.Weirs.Count; i++)
            for (var j = i + 1; j < c.Weirs.Count; j++)
            {
                var a = c.Weirs[i]; var b = c.Weirs[j];
                if (a.Kind != TerrainWeirKind.Weir || b.Kind != TerrainWeirKind.Weir ||
                    a.Downstream != b.Upstream && b.Downstream != a.Upstream) continue;
                if (a.Structure == b.Structure)
                    throw new ArgumentException("Chained weirs require distinct structures.");
                if (Math.Abs(a.S - b.S) < 1.5)
                    throw new ArgumentException("Chained weirs require at least 1.5 m absolute station separation on this channel's centreline.");
            }
        ArgumentNullException.ThrowIfNull(c.Crossings);
        if (!c.Dry && c.Crossings.Count != 0) throw new ArgumentException("Stage 1 crossings are dry-channel-only; wet channels use their real network structures.");
        foreach (var crossing in c.Crossings) Crossing(crossing);
    }

    private static void ValidateCorridor(TerrainCorridorOp c)
    {
        Reference(c.Road, "road"); Range(c.CrossFall, -0.02, 0.02, "crossFall"); Range(c.Shoulder, 0.5, 1, "shoulder");
        double firstStation = 0;
        if (c.Range is { } stationRange)
        {
            Minimum(stationRange.S0, 0, "range.s0"); Finite(stationRange.S1, "range.s1");
            if (stationRange.S1 <= stationRange.S0)
                throw new ArgumentException("Corridor range must be a nonempty interval with s1 > s0.");
            firstStation = stationRange.S0;
        }
        ArgumentNullException.ThrowIfNull(c.Anchors);
        if (c.Anchors.Count < 2 || c.Anchors[0] is null || c.Anchors[0].S != firstStation)
            throw new ArgumentException("Corridor needs at least two anchors, starting at range.s0 or zero when range is absent.");
        double previous = -1;
        foreach (var anchor in c.Anchors)
        {
            ArgumentNullException.ThrowIfNull(anchor); Minimum(anchor.S, 0, "anchors.s"); Elevation(anchor.Y, "anchors.y");
            if (anchor.S <= previous) throw new ArgumentException("Corridor anchor stations must strictly increase.");
            previous = anchor.S;
        }
        if (c.Range is { } authoredRange && c.Anchors[^1].S != authoredRange.S1)
            throw new ArgumentException("Corridor last anchor must equal range.s1.");
        ArgumentNullException.ThrowIfNull(c.Steps);
        double previousEnd = -1;
        foreach (var step in c.Steps)
        {
            ArgumentNullException.ThrowIfNull(step); Reference(step.Structure, "steps.structure");
            Minimum(step.S0, 0, "steps.s0"); Finite(step.S1, "steps.s1");
            if (step.S1 <= step.S0 || step.S0 < firstStation || step.S1 > c.Anchors[^1].S || step.S0 < previousEnd)
                throw new ArgumentException("Step intervals must be ordered, nonoverlapping and inside the authored corridor span.");
            previousEnd = step.S1;
        }
    }

    private static void Shape(TerrainShape shape)
    {
        switch (shape)
        {
            case TerrainCircle c: Point(c.Center); Positive(c.Radius, "shape.radius"); break;
            case TerrainEllipse e:
                Point(e.Center); Point(e.Radii); Positive(e.Radii.X, "shape.radii.x"); Positive(e.Radii.Y, "shape.radii.z"); Finite(e.YawDeg, "shape.yawDeg"); break;
            default: throw new ArgumentException("Stage 1 supports circle/ellipse hills and basins only.");
        }
    }
    private static void Profile(TerrainProfile profile) => _ = profile switch
    {
        TerrainProfile.Hermite => 0,
        _ => throw new ArgumentException("Unknown terrain profile.")
    };
    private static void Side(TerrainSide side) { ArgumentNullException.ThrowIfNull(side); Ratio(side.Fill, "side.fill"); Ratio(side.Cut, "side.cut"); }
    private static void Bank(TerrainBank bank)
    {
        ArgumentNullException.ThrowIfNull(bank);
        switch (bank.Kind)
        {
            case TerrainBankKind.Swale:
                if (bank.Slope is not null) throw new ArgumentException("Swale uses inner/outer, not slope.");
                Ratio(bank.Inner, "bank.inner");
                if (bank.Outer is not null) Ratio(bank.Outer, "bank.outer");
                break;
            case TerrainBankKind.Hard:
            case TerrainBankKind.Soft:
            case TerrainBankKind.Steps:
            case TerrainBankKind.Wetland:
                if (bank.Inner is not null || bank.Outer is not null) throw new ArgumentException("Wet banks use slope, not inner/outer.");
                Ratio(bank.Slope, "bank.slope");
                break;
            default: throw new ArgumentException("Unknown bank kind.");
        }
    }
    private static void Crossing(TerrainCrossing crossing)
    {
        ArgumentNullException.ThrowIfNull(crossing); Reference(crossing.Road, "crossings.road");
        switch (crossing)
        {
            case TerrainUnderpassCrossing underpass: Point(underpass.At); break;
            case TerrainEndBeforeCrossing: break;
            default: throw new ArgumentException("Unknown crossing form.");
        }
    }
    private static void Polygon(PolygonRings polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon); Ring(polygon.Outer); ArgumentNullException.ThrowIfNull(polygon.Holes);
        foreach (var hole in polygon.Holes) Ring(hole);
    }
    private static void Ring(IReadOnlyList<Vector2> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3 || points[0] == points[^1]) throw new ArgumentException("Rings need at least three vertices and no repeated closing vertex.");
        foreach (var point in points) Point(point);
    }
    private static void Line(Polyline line)
    {
        ArgumentNullException.ThrowIfNull(line); ArgumentNullException.ThrowIfNull(line.Points);
        if (line.Points.Count < 2) throw new ArgumentException("A line requires at least two points.");
        for (var i = 0; i < line.Points.Count; i++)
        {
            Point(line.Points[i]);
            if (i > 0 && line.Points[i] == line.Points[i - 1]) throw new ArgumentException("A line cannot contain a zero-length segment.");
        }
    }
    private static void Ratio(string? value, string field)
    {
        if (value is null || !Regex.IsMatch(value, "\\A1:(?!0+(?:\\.0+)?\\z)[0-9]+(?:\\.[0-9]+)?\\z", RegexOptions.CultureInvariant) ||
            !double.TryParse(value.AsSpan(2), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var denominator) ||
            !double.IsFinite(denominator) || denominator <= 0) throw new ArgumentException($"{field} must be a finite positive 1:n ratio.");
    }
    private static void Reference(FeatureId id, string field) => Text(id.Value, field);
    private static void Text(string value, string field) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{field} is required."); }
    private static void Point(Vector2 p) { if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) throw new ArgumentException("Coordinates must be finite."); }
    private static void Finite(double value, string field) { if (!double.IsFinite(value)) throw new ArgumentException($"{field} must be finite."); }
    private static void Positive(double value, string field) { Finite(value, field); if (value <= 0) throw new ArgumentException($"{field} must be positive."); }
    private static void Minimum(double value, double minimum, string field) { Finite(value, field); if (value < minimum) throw new ArgumentException($"{field} is below {minimum}."); }
    private static void Maximum(double value, double maximum, string field) { Finite(value, field); if (value > maximum) throw new ArgumentException($"{field} exceeds {maximum}."); }
    private static void Range(double value, double minimum, double maximum, string field) { Minimum(value, minimum, field); Maximum(value, maximum, field); }
    private static void Elevation(double value, string field) => Range(value, TerrainSampling.MinimumHeight, TerrainSampling.MaximumHeight, field);
    private static void Count(int value, string field) { if (value <= 0) throw new ArgumentException($"{field} must be positive."); }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(TerrainOverlayFile))]
[JsonSerializable(typeof(TerrainBankKind))]
internal partial class TerrainOverlayJsonContext : JsonSerializerContext { }
