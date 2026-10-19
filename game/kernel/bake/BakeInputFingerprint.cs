using System.Buffers.Binary;
using System.IO.Hashing;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.Vegetation;

namespace Campus.Kernel.Bake;

/// <summary>One input stamp for the effective world and resources read by a requested product.</summary>
internal static class BakeInputFingerprint
{
    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadInputs(IMapContext map)
    {
        var snapshot = map.SourceInputs;
        if (snapshot is null || snapshot.Entries.Count == 0 || !snapshot.Entries.TryGetValue("world/geo.json", out var geo) || geo.IsEmpty)
            throw new InvalidDataException($"Map {map.Descriptor.Id.Value} has no frozen world/geo.json input. Publish MapLoadInput.SourceInputs when creating IMapContext.");
        return snapshot.Entries;
    }

    public static string Identity(BakeKey key, string stamp) => $"{BakeOptions.Abi}/{key.Map.Value}/{key.Module}/{key.Detail}/{stamp}";

    public static string Compute(BuildContext context, BakeKey key, string runtimeCode, BakeFingerprintSamples samples,
        IReadOnlyDictionary<string, string>? dependencyCodes = null)
    {
        if (string.IsNullOrWhiteSpace(context.CodeHash) || string.IsNullOrWhiteSpace(runtimeCode))
            throw new InvalidDataException($"Missing generated code fingerprint for {key.Module}.");
        var input = new InputHash();
        input.Add(new { BakeOptions.Abi, key, context.CodeHash, runtimeCode, context.WorldSeed });
        if (dependencyCodes is not null) input.Add(dependencyCodes.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray());
        var collision = key.Detail.StartsWith("C/", StringComparison.Ordinal);
        if (!collision) input.Add(new { PaletteLayout.EncodingVersion, PaletteLayout.LayoutId, entries = context.Catalog.Entries.OrderBy(entry => entry.Index.Value).ToArray() });
        input.Add(new { context.Map.DataRect, context.Map.Bounds });
        if (key.Module == "diorama")
        {
            input.File(context.SourceInputs, context.Map.Env.Backdrop);
            input.Add(context.World.Outskirts?.CutLine);
            return input.Finish();
        }

        var area = Area(context, key);
        var features = context.World.Features.Values.Where(feature => Intersects(feature.Bounds, area) && UsesFeature(key.Module, feature))
            .OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        input.Add(features);
        foreach (var feature in features)
        {
            input.Add(new
            {
                feature.Id,
                claims = Enum.GetValues<Aspects>().Select(aspect => new { aspect, owner = context.ClaimedBy(feature.Id, aspect) }).ToArray(),
                interior = context.World.Interiors.Get(feature.Id),
                windows = UsesWindows(key.Module) ? context.WindowNumbering?.Allocations.Where(allocation => allocation.Feature == feature.Id)
                    .OrderBy(allocation => allocation.Key, StringComparer.Ordinal).ToArray() : null
            });
            AddPad(context.World.Terrain, input, feature.Id);
            input.Add(context.World.Terrain.WaterY(feature.Id));
            if (context.World.Fidelity is { } fidelity && key.Module is "buildings" or "map")
            {
                input.Add(fidelity.Facades(feature.Id).Select(facade => new
                    { facade.Building, facade.Index, facade.A, facade.B, facade.Zone, facade.GroundBand }).ToArray());
                input.Add(fidelity.Roof(feature.Id).Zone);
            }
            foreach (var point in Points(feature.Geometry)) Sample(input, point, samples);
            foreach (var part in feature.Parts)
                foreach (var point in Points(part.Geometry)) Sample(input, point, samples);
            Sample(input, new(feature.Bounds.Center.X, feature.Bounds.Center.Z), samples);
        }

        if (key.Module != "buildings" && !key.Module.StartsWith("interior.", StringComparison.Ordinal))
        {
            AddLines(context.World.Network.Roads);
            AddLines(context.World.Network.Waterways);
            AddLines(context.World.Network.Bridges);
            AddLines(context.World.Network.Culverts);
            if (key.Module is "vegetation" or "furniture" or "collision.world" or "map")
                input.Add(context.World.Network.Trees.Where(tree => Contains(area, new(tree.Position.X, tree.Position.Z)))
                    .OrderBy(tree => tree.Id.Value, StringComparer.Ordinal).ToArray());
        }
        AddBoundary(input, context.World.Boundary, area);
        AddTerrain(key, input, area, samples);
        var vegetation = key.Module == "vegetation" || key.Module == "collision.world" && dependencyCodes?.ContainsKey("vegetation") == true;
        if (vegetation) AddVegetation(context, input, area, samples);
        if (key.Module == "furniture" || key.Module == "collision.world" && dependencyCodes?.ContainsKey("furniture") == true)
            AddFurniturePlanting(context, input, area);
        if (vegetation)
            input.File(context.GlobalInputs.Entries, "vegetation/species.json");
        if (!collision && (key.Module is "furniture" or "zone.north" or "zone.center-east" or "zone.west"))
            input.File(context.GlobalInputs.Entries, SceneTextInputs.AtlasKey);
        if (!collision && (key.Module is "ground" or "buildings"))
            input.File(context.SourceInputs, SurfaceMaterialCatalog.SourcePath);
        if (key.Module == "water") input.Add(context.Map.Env.ReflectLakes);
        if (key.Module == "map")
        {
            foreach (var file in new[] { "atlas/labels.json", "atlas/markers.json", "waypoints.json" }) input.File(context.SourceInputs, file);
            input.Add(context.Map.Env.ReflectLakes);
            input.Add(context.World.Outskirts?.Blocks);
        }
        return input.Finish();

        void AddLines(IReadOnlyList<NetworkLine> lines)
        {
            var selected = lines.Where(line => Intersects(Bounds(line.Line.Points, line.Width * .5f), area))
                .OrderBy(line => line.Id.Value, StringComparer.Ordinal).ToArray();
            input.Add(selected);
            foreach (var line in selected)
                foreach (var point in line.Line.Points) Sample(input, point, samples);
        }
    }

    private static void AddVegetation(BuildContext context, InputHash input, MapRect area, BakeFingerprintSamples samples)
    {
        if (!context.SourceInputs.TryGetValue("vegetation/planting.json", out var bytes)) { input.Add(false); return; }
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        var rules = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var key in new[] { "roads", "osmTrees", "groves", "pits" })
            if (root.TryGetProperty(key, out var value)) rules.Add(key, value);
        input.Add(rules);
        var wallReach = context.World.Features.Values.Any(feature => feature.Kind.Value == "sportGroup")
            ? Planting.SportWallReach(SpeciesTable.Parse(context.GlobalInputs.Entries["vegetation/species.json"].Span)) : 0f;

        // A parcel's accepted count can change a later candidate anywhere in that parcel. Crown spacing also
        // couples neighbouring parcels, transitively. Keep whole entities and their connected neighbourhood,
        // rather than clipping vertices or candidate positions to the requested cell.
        var regions = new List<MapRect> { area };
        // A long pit is held if any earlier tree stands anywhere in it; design trees also suppress later
        // design trees. Include these complete entities before resolving their upstream grove neighbourhood.
        var remaining = context.World.Features.Values.Where(feature => HasKind(feature, "green") ||
            feature.Kind.Value is "tree-pit" or "tree" or "sportGroup")
            .OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToList();
        for (var changed = true; changed;)
        {
            changed = false;
            for (var i = remaining.Count - 1; i >= 0; i--)
            {
                var box = VegetationBounds(remaining[i], wallReach);
                if (!regions.Any(region => Intersects(box, region))) continue;
                // Spaced reads the adjacent 32 m occupation buckets: their full reach fits this guard band.
                regions.Add(Expand(box, TileGrid.BaseEdge));
                remaining.RemoveAt(i);
                changed = true;
            }
        }

        // OSM duplicates are suppressed in ID order. An earlier point can suppress another earlier point,
        // so follow that chain as well as the trees directly beside the relevant parcels.
        var trees = context.World.Network.Trees.OrderBy(tree => tree.Id.Value, StringComparer.Ordinal).ToArray();
        var selectedTrees = new bool[trees.Length];
        for (var i = trees.Length - 1; i >= 0; i--)
        {
            var point = new Vector2(trees[i].Position.X, trees[i].Position.Z);
            if (!selectedTrees[i] && !regions.Any(region => Contains(region, point))) continue;
            selectedTrees[i] = true;
            for (var j = 0; j < i; j++)
                if (Vector2.Distance(point, new(trees[j].Position.X, trees[j].Position.Z)) < Planting.SameTree)
                    selectedTrees[j] = true;
        }
        var selected = trees.Where((_, index) => selectedTrees[index]).ToArray();
        foreach (var tree in selected)
        {
            var point = new Vector2(tree.Position.X, tree.Position.Z);
            regions.Add(Expand(new(point, point), TileGrid.BaseEdge));
        }
        input.Add(selected);

        string[] kinds = ["building", "water", "sport", "sportGroup", "parking", "green", "road", "tree-pit", "tree"];
        var features = context.World.Features.Values.Where(feature => kinds.Any(kind => HasKind(feature, kind)) &&
            regions.Any(region => Intersects(VegetationBounds(feature, wallReach), region)))
            .OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        input.Add(features);

        // PlantRoads chains all segments of a rule's name and uses the chain ordinal in every tree key.
        // Even a disconnected earlier chain can change that ordinal: its complete road input remains shared.
        var roadNames = root.TryGetProperty("roads", out var roads) ? roads.EnumerateArray()
            .Select(road => road.GetProperty("name").GetString()).ToHashSet(StringComparer.Ordinal) : [];
        input.Add(context.World.Network.Roads.Where(road => roadNames.Contains(RoadName(road.Props)) ||
                regions.Any(region => Intersects(Bounds(road.Line.Points, road.Width * .5f), region)))
            .OrderBy(road => road.Id.Value, StringComparer.Ordinal).ToArray());

        if (root.TryGetProperty("groves", out var groves))
            foreach (var key in new[] { "areas", "washNear" })
                if (groves.TryGetProperty(key, out var places))
                    foreach (var place in places.EnumerateArray())
                    {
                        var name = place.GetProperty("name").GetString();
                        var named = context.World.Features.Values.Where(feature => feature.Name == name).ToArray();
                        if (named.Length == 0) continue;
                        var envelope = new MapRect(
                            named.Aggregate(new Vector2(float.MaxValue), (min, feature) => Vector2.Min(min, new(feature.Bounds.Min.X, feature.Bounds.Min.Z))),
                            named.Aggregate(new Vector2(float.MinValue), (max, feature) => Vector2.Max(max, new(feature.Bounds.Max.X, feature.Bounds.Max.Z))));
                        if (regions.Any(region => Intersects(Expand(envelope, place.GetProperty("radius").GetSingle()), region)))
                            input.Add(new { name, envelope });
                    }
        var outer = context.World.Boundary.Outer;
        input.Add(new { windMin = outer.Count > 0 ? outer.Min(point => point.X) : -1000f,
            windMax = outer.Count > 0 ? outer.Max(point => point.X) : 1000f, context.Map.TerrainDefaults });
        foreach (var feature in features)
        {
            AddPad(context.World.Terrain, input, feature.Id);
            input.Add(context.World.Terrain.WaterY(feature.Id));
            foreach (var point in Points(feature.Geometry)) Sample(input, point, samples);
            foreach (var part in feature.Parts)
                foreach (var point in Points(part.FacadeGeometry ?? part.Geometry)) Sample(input, point, samples);
            Sample(input, new(feature.Bounds.Center.X, feature.Bounds.Center.Z), samples);
        }
        foreach (var tree in selected) Sample(input, new(tree.Position.X, tree.Position.Z), samples);
        var wallGroups = features.Where(feature => feature.Kind.Value == "sportGroup").Select(feature => feature.Id).ToHashSet();
        if (wallGroups.Count > 0)
            foreach (var wall in Planting.SportWallPoints(context.World))
                if (wallGroups.Contains(wall.Feature)) Sample(input, wall.Position, samples);
        foreach (var region in regions) AddBoundary(input, context.World.Boundary, region);
        // Terrain is analytic at candidate positions; retain its authored program rather than treating metre
        // samples as an exact substitute. This is independent of the removed map-wide grove quota.
        foreach (var file in context.SourceInputs.Keys.Where(path => path.StartsWith("world/", StringComparison.Ordinal) && path.EndsWith(".terrain.json", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
            input.File(context.SourceInputs, file);
    }

    private static bool HasKind(WorldFeature feature, string kind) => feature.Kind.Value == kind || feature.Parts.Any(part =>
        part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var value) && value.GetString() == kind);

    private static MapRect VegetationBounds(WorldFeature feature, float wallReach)
    {
        var points = Points(feature.Geometry).Concat(feature.Parts.SelectMany(part => Points(part.FacadeGeometry ?? part.Geometry)))
            .Append(new(feature.Bounds.Min.X, feature.Bounds.Min.Z)).Append(new(feature.Bounds.Max.X, feature.Bounds.Max.Z)).ToArray();
        var margin = 0f;
        foreach (var props in feature.Parts.Select(part => part.Props).Prepend(feature.Props))
            foreach (var key in new[] { "radius", "size", "width" })
                if (props is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty(key, out var number) &&
                    number.ValueKind == JsonValueKind.Number && number.TryGetSingle(out var n))
                    margin = MathF.Max(margin, key == "radius" ? n : n * .5f);
        if (feature.Kind.Value == "sportGroup" && feature.Props is { ValueKind: JsonValueKind.Object } wall)
        {
            // A shared row can stand halfway into the permitted gap; crowns extend beyond either row.
            var offset = wall.GetProperty("treeOffset").GetSingle();
            var shared = wall.GetProperty("sharedGapBelow").GetSingle() * .5f;
            margin = MathF.Max(margin, MathF.Max(offset, shared) + wallReach);
        }
        return Bounds(points, margin);
    }

    private static string? RoadName(JsonElement? props)
    {
        if (props is not { ValueKind: JsonValueKind.Object } value) return null;
        foreach (var key in new[] { "name:zh", "name" })
            if (value.TryGetProperty(key, out var name) && name.ValueKind == JsonValueKind.String) return name.GetString();
        return null;
    }

    private static void AddFurniturePlanting(BuildContext context, InputHash input, MapRect area)
    {
        if (!context.SourceInputs.TryGetValue("vegetation/planting.json", out var bytes)) { input.Add(false); return; }
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        var roadNames = context.World.Network.Roads.Where(road => Intersects(Bounds(road.Line.Points, road.Width * .5f), area))
            .Select(road => Name(road.Props)).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var roads = root.TryGetProperty("roads", out var roadList) ? roadList.EnumerateArray()
            .Where(road => Name(road) is { } name && roadNames.Contains(name)).ToArray() : [];
        input.Add(roads.Select(road => new { name = Name(road),
            rows = road.TryGetProperty("rows", out var rows) ? rows.EnumerateArray().Select(row => Property(row, "offset")).ToArray() : null,
            band = road.TryGetProperty("band", out var band) ? Property(band, "offset") : null }).ToArray());

        static JsonElement? Property(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : null;
        static string? Name(JsonElement? element)
        {
            if (element is not { ValueKind: JsonValueKind.Object } value) return null;
            foreach (var key in new[] { "name:zh", "name" })
                if (value.TryGetProperty(key, out var name) && name.ValueKind == JsonValueKind.String) return name.GetString();
            return null;
        }
    }

    internal static MapRect Area(BuildContext context, BakeKey key)
    {
        if (key.Module.StartsWith("interior.", StringComparison.Ordinal))
        {
            var feature = new FeatureId(key.Detail.Split('/')[0]);
            if (!context.World.Features.TryGetValue(feature, out var source)) throw new InvalidDataException("Interior source feature is absent.");
            return Expand(new(new(source.Bounds.Min.X, source.Bounds.Min.Z), new(source.Bounds.Max.X, source.Bounds.Max.Z)), TileGrid.BaseEdge);
        }
        if (key.Detail.StartsWith("C/", StringComparison.Ordinal) || key.Detail.StartsWith('L'))
        {
            var request = BakeRequestKeys.Parse(key);
            // L4/0/0 is explicitly the current whole-map distant product, unlike the L0-L3 grid.
            if (request.Level == LodLevel.L4) return context.Map.DataRect;
            var edge = TileGrid.Edge(request.Level);
            var min = TileGrid.Origin(request.Tile, request.Level);
            return Expand(new(min, min + new Vector2(edge)), TileGrid.BaseEdge);
        }
        return context.Map.DataRect; // Atlas and other non-cell products.
    }

    private static bool UsesWindows(string module) => module == "buildings" || module == "core" || module.StartsWith("zone.", StringComparison.Ordinal);

    private static bool UsesFeature(string module, WorldFeature feature) => module switch
    {
        "buildings" => feature.Kind.Value == "building" || feature.Height.HasValue || feature.Storeys.HasValue ||
            feature.Parts.Any(part => part.Height.HasValue || part.Storeys.HasValue),
        "water" => feature.Kind.Value == "water" || feature.Parts.Any(part => part.Props is { ValueKind: JsonValueKind.Object } props &&
            props.TryGetProperty("kind", out var kind) && kind.GetString() == "water"),
        _ => true
    };

    private static void AddTerrain(BakeKey key, InputHash input, MapRect area, BakeFingerprintSamples samples)
    {
        if (key.Detail.StartsWith("C/", StringComparison.Ordinal) || key.Detail.StartsWith('L'))
        {
            var request = BakeRequestKeys.Parse(key);
            var heights = samples.Tile(request.Tile, (int)request.Level);
            input.Add(heights);
        }
        // Canonical metre samples include the shared border; coarser requests retain their actual terrain spacing.
        var spacing = key.Detail.StartsWith('L') ? 1 << (int)BakeRequestKeys.Parse(key).Level : 1;
        if (key.Module == "map") spacing = 8;
        for (var z = area.Min.Y; z <= area.Max.Y; z += spacing)
            for (var x = area.Min.X; x <= area.Max.X; x += spacing) Sample(input, new(x, z), samples);
    }

    private static void AddPad(ITerrain terrain, InputHash input, FeatureId feature)
    {
        var present = terrain.TryGetPadY(feature, out var y);
        input.Add(present);
        if (present) input.Number(y);
    }

    private static void Sample(InputHash input, Vector2 point, BakeFingerprintSamples samples)
    {
        var sample = samples.At(point);
        input.Number(sample.Height);
        var facts = sample.Facts;
        input.Number(facts.Grade); input.Number((float)facts.Class); input.Number(facts.CrestDist); input.Number(facts.ToeDist);
        if (sample.Ground is { } ground)
        {
            input.Number(ground);
            input.Number(sample.WalkDistance);
        }
    }

    private static void AddBoundary(InputHash input, PolygonRings rings, MapRect area)
    {
        // Point classification handles tiles entirely inside/outside a ring; only nearby edges carry geometry bytes.
        input.Add(new { inside = Inside(rings.Outer, area.Min) && !rings.Holes.Any(hole => Inside(hole, area.Min)) });
        AddRing(rings.Outer);
        foreach (var hole in rings.Holes) AddRing(hole);
        void AddRing(IReadOnlyList<Vector2> ring)
        {
            for (var index = 0; index < ring.Count; index++)
            {
                var a = ring[index]; var b = ring[(index + 1) % ring.Count];
                if (Intersects(Bounds([a, b]), area)) input.Add(new { a, b });
            }
        }
    }

    private static bool Inside(IReadOnlyList<Vector2> ring, Vector2 point)
    {
        var inside = false;
        for (int a = 0, b = ring.Count - 1; a < ring.Count; b = a++)
            if ((ring[a].Y > point.Y) != (ring[b].Y > point.Y) && point.X <
                (ring[b].X - ring[a].X) * (point.Y - ring[a].Y) / (ring[b].Y - ring[a].Y) + ring[a].X) inside = !inside;
        return inside;
    }

    private static MapRect Expand(MapRect box, float margin) => new(box.Min - new Vector2(margin), box.Max + new Vector2(margin));
    private static bool Contains(MapRect box, Vector2 point) => point.X >= box.Min.X && point.X <= box.Max.X && point.Y >= box.Min.Y && point.Y <= box.Max.Y;
    private static bool Intersects(Box3 box, MapRect area) => box.Min.X <= area.Max.X && box.Max.X >= area.Min.X && box.Min.Z <= area.Max.Y && box.Max.Z >= area.Min.Y;
    private static bool Intersects(MapRect box, MapRect area) => box.Min.X <= area.Max.X && box.Max.X >= area.Min.X && box.Min.Y <= area.Max.Y && box.Max.Y >= area.Min.Y;
    private static MapRect Bounds(IReadOnlyList<Vector2> points, float margin = 0) => Expand(new(
        points.Aggregate(new Vector2(float.MaxValue), Vector2.Min), points.Aggregate(new Vector2(float.MinValue), Vector2.Max)), margin);
    private static IEnumerable<Vector2> Points(WorldGeometry geometry) => geometry switch
    {
        PointGeometry point => [new(point.Position.X, point.Position.Z)],
        PolylineGeometry line => line.Line.Points,
        PolygonGeometry polygon => polygon.Rings.Outer.Concat(polygon.Rings.Holes.SelectMany(hole => hole)),
        MultiPolygonGeometry multi => multi.Polygons.SelectMany(polygon => polygon.Outer.Concat(polygon.Holes.SelectMany(hole => hole))),
        _ => throw new InvalidDataException("Unknown feature geometry.")
    };

    private sealed class InputHash
    {
        private readonly XxHash128 _hash = new();
        public void Number(float value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
            _hash.Append(bytes);
        }
        public void Add<T>(T value)
        {
            using var bytes = new MemoryStream();
            using (var writer = new Utf8JsonWriter(bytes)) Write(writer, JsonSerializer.SerializeToElement(value, ContractJson.Options));
            Span<byte> length = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(length, checked((int)bytes.Length));
            _hash.Append(length);
            _hash.Append(bytes.GetBuffer().AsSpan(0, checked((int)bytes.Length)));
        }
        public void File(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, string? path)
        {
            if (path is null) return;
            Add(path);
            if (!files.TryGetValue(path, out var bytes)) { Add(false); return; }
            using var parsed = JsonDocument.Parse(bytes);
            Add(parsed.RootElement);
        }
        public string Finish() => Convert.ToHexStringLower(_hash.GetCurrentHash());
        private static void Write(Utf8JsonWriter writer, JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    // These are the authored explanation fields. Names and displayed text remain effective inputs.
                    if (property.Name is "note" or "notes" or "reason") continue;
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }
                writer.WriteEndObject();
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) Write(writer, item);
                writer.WriteEndArray();
            }
            else if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var integer)) writer.WriteNumberValue(integer);
            else if (element.ValueKind == JsonValueKind.Number && element.TryGetUInt64(out var unsigned)) writer.WriteNumberValue(unsigned);
            else if (element.ValueKind == JsonValueKind.Number) writer.WriteNumberValue(element.GetDouble());
            else element.WriteTo(writer);
        }
    }
}
