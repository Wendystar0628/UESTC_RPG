using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Vegetation;

namespace Campus.Kernel.Bake;

/// <summary>One immutable source index per MapBuild; product keys only combine prepared source-cell stamps.</summary>
internal sealed class BakeSourceIndex
{
    private readonly BuildContext _context;
    private readonly WorldFeature[] _features;
    private readonly NetworkLine[] _roads;
    private readonly NetworkTree[] _trees;
    private readonly JsonElement? _planting;
    private readonly string _plantingStamp;
    private readonly string _namedRoadStamp;
    private readonly float _wallReach;
    private readonly Dictionary<TileKey, string> _cells = [];
    private readonly Dictionary<TileKey, string> _windows = [];
    private readonly ConcurrentDictionary<TileKey, Lazy<string>> _vegetation = new();
    private readonly ConcurrentDictionary<TileKey, Lazy<string>> _furniture = new();
    private readonly ConcurrentDictionary<string, string> _files = new(StringComparer.Ordinal);
    public string CommonStamp { get; }
    public string PaletteStamp { get; }

    public BakeSourceIndex(BuildContext context)
    {
        _context = context;
        _features = context.World.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        _roads = context.World.Network.Roads.OrderBy(road => road.Id.Value, StringComparer.Ordinal).ToArray();
        _trees = context.World.Network.Trees.OrderBy(tree => tree.Id.Value, StringComparer.Ordinal).ToArray();
        if (context.SourceInputs.TryGetValue("vegetation/planting.json", out var planting))
        {
            using var document = JsonDocument.Parse(planting);
            _planting = document.RootElement.Clone();
        }
        _plantingStamp = Hash(_planting);
        var names = _planting is { } root && root.TryGetProperty("roads", out var roads)
            ? roads.EnumerateArray().Select(road => road.GetProperty("name").GetString()).ToHashSet(StringComparer.Ordinal) : [];
        _namedRoadStamp = Hash(_roads.Where(road => names.Contains(RoadName(road.Props))).ToArray());
        _wallReach = _features.Any(feature => feature.Kind.Value == "sportGroup")
            ? Planting.SportWallReach(SpeciesTable.Parse(context.GlobalInputs.Entries["vegetation/species.json"].Span)) : 0;
        var cells = new Dictionary<TileKey, List<string>>();
        var windows = new Dictionary<TileKey, List<string>>();
        foreach (var feature in _features)
        {
            var stamp = Hash(new { feature, claims = Enum.GetValues<Aspects>().Select(aspect =>
                new { aspect, owner = context.ClaimedBy(feature.Id, aspect) }).ToArray() });
            var box = VegetationBounds(feature, 0);
            Add(cells, box, stamp);
            if (context.WindowNumbering is { } numbering)
                Add(windows, box, Hash(numbering.Allocations.Where(allocation => allocation.Feature == feature.Id).OrderBy(allocation => allocation.Key, StringComparer.Ordinal).ToArray()));
        }
        foreach (var lines in new[] { _roads, context.World.Network.Waterways, context.World.Network.Bridges, context.World.Network.Culverts })
            foreach (var line in lines) Add(cells, Bounds(line.Line.Points, line.Width * .5f), Hash(line));
        foreach (var tree in _trees)
            Add(cells, new(new(tree.Position.X, tree.Position.Z), new(tree.Position.X, tree.Position.Z)), Hash(tree));
        var terrain = context.World.Terrain.SourceInputs;
        foreach (var cell in terrain.Cells)
        {
            if (!cells.TryGetValue(cell.Cell, out var stamps)) cells.Add(cell.Cell, stamps = []);
            stamps.Add(cell.Stamp);
        }
        foreach (var pair in cells) _cells.Add(pair.Key, Hash(pair.Value.Order(StringComparer.Ordinal).ToArray()));
        foreach (var pair in windows) _windows.Add(pair.Key, Hash(pair.Value.Order(StringComparer.Ordinal).ToArray()));
        CommonStamp = Hash(new { terrain.CommonStamp, context.World.Boundary, context.Map.TerrainDefaults,
            fidelity = FileStamp("fidelity.json"), interior = FileStamp("interiors.json") });
        PaletteStamp = Hash(new { PaletteLayout.EncodingVersion, PaletteLayout.LayoutId,
            entries = context.Catalog.Entries.OrderBy(entry => entry.Index.Value).ToArray() });

        static void Add(Dictionary<TileKey, List<string>> cells, MapRect box, string stamp)
        {
            foreach (var cell in Cells(box))
            {
                if (!cells.TryGetValue(cell, out var stamps)) cells.Add(cell, stamps = []);
                stamps.Add(stamp);
            }
        }
    }

    public string FileStamp(string? path, bool global = false)
    {
        if (path is null) return Hash<string?>(null);
        return _files.GetOrAdd((global ? "global/" : "map/") + path, _ =>
        {
            var input = new BakeInputHash(); input.File(global ? _context.GlobalInputs.Entries : _context.SourceInputs, path); return input.Finish();
        });
    }

    public void AddArea(BakeInputHash input, MapRect area, bool windows, bool vegetation, bool furniture)
    {
        AddCells(input, [area], windows);
        foreach (var cell in Cells(area))
        {
            if (vegetation) input.Add(_vegetation.GetOrAdd(cell, key => new(() =>
            { var hash = new BakeInputHash(); AddVegetation(hash, CellBounds(key)); return hash.Finish(); }, LazyThreadSafetyMode.ExecutionAndPublication)).Value);
            if (furniture) input.Add(_furniture.GetOrAdd(cell, key => new(() =>
            { var hash = new BakeInputHash(); AddFurniturePlanting(hash, CellBounds(key)); return hash.Finish(); }, LazyThreadSafetyMode.ExecutionAndPublication)).Value);
        }
    }

    private void AddCells(BakeInputHash input, IEnumerable<MapRect> regions, bool windows)
    {
        foreach (var cell in regions.SelectMany(Cells).Distinct().OrderBy(cell => cell.X).ThenBy(cell => cell.Z))
        {
            input.Add(new { cell, stamp = _cells.GetValueOrDefault(cell) });
            if (windows) input.Add(_windows.GetValueOrDefault(cell));
        }
    }
    private static IEnumerable<TileKey> Cells(MapRect box)
    {
        var lo = TileGrid.At(new Vector3(box.Min.X, 0, box.Min.Y)); var hi = TileGrid.At(new Vector3(box.Max.X, 0, box.Max.Y));
        for (var z = lo.Z; z <= hi.Z; z++) for (var x = lo.X; x <= hi.X; x++) yield return new(x, z);
    }
    private static MapRect CellBounds(TileKey cell)
    {
        var min = new Vector2(cell.X * TileGrid.BaseEdge, cell.Z * TileGrid.BaseEdge); return new(min, min + new Vector2(TileGrid.BaseEdge));
    }
    private static string Hash<T>(T value) { var hash = new BakeInputHash(); hash.Add(value); return hash.Finish(); }

    private void AddVegetation(BakeInputHash input, MapRect area)
    {
        if (_planting is not { } root) { input.Add(false); return; }
        input.Add(_plantingStamp);
        var wallReach = _wallReach;

        // A parcel's accepted count can change a later candidate anywhere in that parcel. Crown spacing also
        // couples neighbouring parcels, transitively. Keep whole entities and their connected neighbourhood,
        // rather than clipping vertices or candidate positions to the requested cell.
        var regions = new List<MapRect> { area };
        // A long pit is held if any earlier tree stands anywhere in it; design trees also suppress later
        // design trees. Include these complete entities before resolving their upstream grove neighbourhood.
        var remaining = _features.Where(feature => HasKind(feature, "green") ||
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
        var trees = _trees;
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

        // PlantRoads chains all segments of a rule's name and uses the chain ordinal in every tree key.
        // Even a disconnected earlier chain can change that ordinal: its complete road input remains shared.
        var roadNames = root.TryGetProperty("roads", out var roads) ? roads.EnumerateArray()
            .Select(road => road.GetProperty("name").GetString()).ToHashSet(StringComparer.Ordinal) : [];
        input.Add(_namedRoadStamp);
        foreach (var road in _roads)
            if (roadNames.Contains(RoadName(road.Props)) || regions.Any(region => Intersects(Bounds(road.Line.Points, road.Width * .5f), region)))
                regions.Add(Expand(Bounds(road.Line.Points, road.Width * .5f), TileGrid.BaseEdge));

        if (root.TryGetProperty("groves", out var groves))
            foreach (var key in new[] { "areas", "washNear" })
                if (groves.TryGetProperty(key, out var places))
                    foreach (var place in places.EnumerateArray())
                    {
                        var name = place.GetProperty("name").GetString();
                        var named = _features.Where(feature => feature.Name == name).ToArray();
                        if (named.Length == 0) continue;
                        var envelope = new MapRect(
                            named.Aggregate(new Vector2(float.MaxValue), (min, feature) => Vector2.Min(min, new(feature.Bounds.Min.X, feature.Bounds.Min.Z))),
                            named.Aggregate(new Vector2(float.MinValue), (max, feature) => Vector2.Max(max, new(feature.Bounds.Max.X, feature.Bounds.Max.Z))));
                        if (regions.Any(region => Intersects(Expand(envelope, place.GetProperty("radius").GetSingle()), region)))
                            input.Add(new { name, envelope });
                    }
        var outer = _context.World.Boundary.Outer;
        input.Add(new { windMin = outer.Count > 0 ? outer.Min(point => point.X) : -1000f,
            windMax = outer.Count > 0 ? outer.Max(point => point.X) : 1000f, _context.Map.TerrainDefaults });
        // These regions include full parcels, pits, earlier OSM trees, sport walls and named road chains.
        // Their cell stamps already carry complete terrain source dependencies.
        AddCells(input, regions, windows: false);
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

    private void AddFurniturePlanting(BakeInputHash input, MapRect area)
    {
        if (_planting is not { } root) { input.Add(false); return; }
        var roadNames = _roads.Where(road => Intersects(Bounds(road.Line.Points, road.Width * .5f), area))
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

    private static MapRect Expand(MapRect box, float margin) => new(box.Min - new Vector2(margin), box.Max + new Vector2(margin));
    private static bool Contains(MapRect box, Vector2 point) => point.X >= box.Min.X && point.X <= box.Max.X && point.Y >= box.Min.Y && point.Y <= box.Max.Y;
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

}
