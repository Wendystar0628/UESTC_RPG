using Campus.Contracts.Atlas;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.BakeInput;

public static class MapBaker
{
    public const string Module = "map";
    public const string Detail = "base";

    /// <summary>Call on a kernel-owned worker. Produces CPU bytes only; no IO, threads or engine objects.</summary>
    public static MapBakeProduct Build(MapBakeInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input);
        var world = input.World;
        var rect = input.Map.DataRect;
        var options = input.Options;
        var warnings = new SortedSet<string>(StringComparer.Ordinal)
        {
            "landmark-glyphs-deferred"
        };
        if (input.RegionAreas.Count == 0) warnings.Add("region-boundaries-missing-region-query-returns-unknown");
        var shapes = Collect(world, input.Labels, warnings);
        var boundary = world.Boundary;
        MapDistance.ValidateRing(boundary.Outer);
        foreach (var hole in boundary.Holes) MapDistance.ValidateRing(hole);
        var campusRect = CampusRect(boundary, options.CampusMarginMetres);
        var sampler = new RegionSampler(input.Labels, boundary, input.RegionAreas);
        double classDilation = 2 * Math.Max((campusRect.Max.X - campusRect.Min.X) / options.ClassSize, (campusRect.Max.Y - campusRect.Min.Y) / options.ClassSize);
        var inside = new MapBuckets(shapes.Where(s => s.Channel <= MapChannel.Plaza), options.BucketMetres, Math.Max(8, classDilation), campusRect);
        var outside = new MapBuckets(shapes.Where(s => s.Channel >= MapChannel.Block), options.BucketMetres, 8, input.AtlasRect);
        List<MapTexture> textures =
        [
            Raster("map_sdf_a", options.MainSize, campusRect, (x, z, data, offset) =>
            {
                Span<double> distances = stackalloc double[5];
                distances.Fill(8);
                foreach (var shape in inside.At(x, z))
                {
                    int channel = (int)shape.Channel;
                    distances[channel] = Math.Min(distances[channel], shape.Distance(x, z));
                }
                for (int c = 0; c < 4; c++) data[offset + c] = MapDistance.Encode(distances[c]);
            }, cancellationToken),
            Raster("map_sdf_b", options.ClassSize, campusRect, (x, z, data, offset) =>
            {
                double plaza = 8, closestClass = classDilation;
                var kind = MapFeatureClass.None;
                foreach (var shape in inside.At(x, z))
                {
                    if (shape.Class == MapFeatureClass.None && shape.Channel != MapChannel.Plaza) continue;
                    double distance = shape.Distance(x, z);
                    if (shape.Channel == MapChannel.Plaza) plaza = Math.Min(plaza, distance);
                    if (shape.Class != MapFeatureClass.None && distance <= classDilation &&
                        (distance < closestClass || (distance == closestClass && shape.Class > kind)))
                    { closestClass = distance; kind = shape.Class; }
                }
                data[offset] = sampler.At(x, z, classDilation);
                data[offset + 1] = (byte)kind;
                data[offset + 2] = MapDistance.Encode(plaza);
                data[offset + 3] = MapDistance.Encode(MapDistance.Polygon(boundary, x, z));
            }, cancellationToken),
            RasterOutside("map_sdf_out", options.OutskirtsSize, rect, outside, cancellationToken),
            RasterOutside("map_sdf_ring", options.RingSize, input.AtlasRect, outside, cancellationToken)
        ];
        var regions = sampler.Build(rect, options.RegionCellMetres, cancellationToken);
        var labels = LabelLayouter.Build(input.Labels, input.LabelInputHash, input.LabelLayout);
        if (!labels.Ready) warnings.Add("label-layout-awaits-runtime-font-measurements");
        var manifest = new MapManifest(1, input.Map.Id.Value, input.WorldModelHash, input.CodeHash,
            input.LabelInputHash, rect, input.AtlasRect, input.Labels.Version,
            textures.Select(t => new MapTextureInfo(t.Name, t.Name + ".rgba8", t.Width, t.Height, t.Rect,
                new((t.Rect.Max.X - t.Rect.Min.X) / t.Width, (t.Rect.Max.Y - t.Rect.Min.Y) / t.Height),
                Channels(t.Name), Hash(t.Rgba8))).ToArray(),
            new("regions.u8", regions.Rect, regions.Width, regions.Height, regions.CellMetres, regions.Ids, Hash(regions.Cells)),
            warnings.ToArray());
        return new(manifest, textures, regions, labels);
    }

    internal static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static MapRect CampusRect(PolygonRings boundary, float margin)
    {
        float minX = boundary.Outer.Min(p => p.X), maxX = boundary.Outer.Max(p => p.X);
        float minZ = boundary.Outer.Min(p => p.Y), maxZ = boundary.Outer.Max(p => p.Y);
        float centerX = (minX + maxX) / 2, centerZ = (minZ + maxZ) / 2;
        float half = Math.Max(maxX - minX, maxZ - minZ) / 2 + margin;
        var rect = new MapRect(new(centerX - half, centerZ - half), new(centerX + half, centerZ + half));
        ValidateRect(rect);
        return rect;
    }

    private static MapTexture RasterOutside(string name, int size, MapRect rect, MapBuckets buckets, CancellationToken token) =>
        Raster(name, size, rect, (x, z, data, offset) =>
        {
            Span<double> distances = stackalloc double[4];
            distances.Fill(8);
            foreach (var shape in buckets.At(x, z))
            {
                int channel = (int)shape.Channel - (int)MapChannel.Block;
                distances[channel] = Math.Min(distances[channel], shape.Distance(x, z));
            }
            for (int c = 0; c < 4; c++) data[offset + c] = MapDistance.Encode(distances[c]);
        }, token);

    private static MapTexture Raster(string name, int size, MapRect rect, Action<double, double, byte[], int> pixel, CancellationToken token)
    {
        byte[] bytes = new byte[checked(size * size * 4)];
        double stepX = ((double)rect.Max.X - rect.Min.X) / size, stepZ = ((double)rect.Max.Y - rect.Min.Y) / size;
        for (int z = 0; z < size; z++)
        {
            token.ThrowIfCancellationRequested();
            double worldZ = rect.Min.Y + (z + 0.5) * stepZ;
            for (int x = 0; x < size; x++) pixel(rect.Min.X + (x + 0.5) * stepX, worldZ, bytes, (z * size + x) * 4);
        }
        return new(name, size, size, rect, bytes);
    }

    private static List<MapShape> Collect(IWorldModel world, AtlasLabels labels, SortedSet<string> warnings)
    {
        List<MapShape> shapes = [];
        var networkIds = world.Network.Roads.Concat(world.Network.Waterways).Concat(world.Network.Bridges)
            .Select(line => line.Id).ToHashSet();
        var landmarks = labels.Labels.Where(l => l.Kind == "landmark" && l.Ref is not null).Select(l => l.Ref!).ToHashSet(StringComparer.Ordinal);
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Parts.Count == 0)
            {
                AddGeometry(feature, feature.Geometry, feature.Kind.Value, feature.Props, feature.Id.Value);
                continue;
            }
            // The aggregate is a query envelope: a POI campus can contain both grounds and buildings.
            // Never paint it in addition to its independently classified source parts.
            foreach (var part in feature.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
            {
                string kind = feature.Kind.Value;
                if (part.Props is { ValueKind: JsonValueKind.Object } props &&
                    props.TryGetProperty("kind", out var partKind) && partKind.ValueKind == JsonValueKind.String)
                    kind = partKind.GetString()!;
                AddGeometry(feature, part.Geometry, kind, part.Props ?? feature.Props, feature.Id.Value + "/" + part.SourceId);
            }
        }
        AddNetwork(world.Network.Roads, MapChannel.Road);
        AddNetwork(world.Network.Waterways, MapChannel.Water);
        AddNetwork(world.Network.Bridges, MapChannel.Plaza, MapFeatureClass.Bridge);
        if (world.Outskirts is { } outskirts)
        {
            foreach (var block in outskirts.Blocks.OrderBy(b => b.Id, StringComparer.Ordinal))
                if (block.Band is OutskirtsBand.O0 or OutskirtsBand.O1)
                    shapes.Add(new(new PolygonRings(block.Footprint), MapChannel.Block));
            if (outskirts.CutLine.Count >= 3) shapes.Add(new(new PolygonRings(outskirts.CutLine), MapChannel.Edge));
            else warnings.Add("outskirts-cut-line-missing");
            warnings.Add("outskirts-road-and-water-model-fields-not-yet-available");
        }
        else warnings.Add("outskirts-model-missing");
        return shapes;

        void AddGeometry(WorldFeature feature, WorldGeometry geometry, string kind, JsonElement? properties, string diagnosticId)
        {
            if (Classify(kind) is not { } c) return;
            if (c.Class == MapFeatureClass.Building && landmarks.Contains(feature.Id.Value)) c.Class = MapFeatureClass.Landmark;
            switch (geometry)
            {
                case PolygonGeometry polygon:
                    shapes.Add(new(polygon.Rings, c.Channel, c.Class));
                    break;
                case MultiPolygonGeometry multi:
                    foreach (var polygon in multi.Polygons) shapes.Add(new(polygon, c.Channel, c.Class));
                    break;
                case PolylineGeometry line when !networkIds.Contains(feature.Id):
                    // Width must come from the merged world model; no guessed map-only road width.
                    if (properties is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("width", out var width) &&
                        width.TryGetSingle(out var value) && value > 0)
                        AddLine(shapes, line.Line, value, c.Channel, c.Class);
                    else warnings.Add("line-without-width:" + diagnosticId);
                    break;
            }
        }

        void AddNetwork(IReadOnlyList<NetworkLine> network, MapChannel channel, MapFeatureClass featureClass = MapFeatureClass.None)
        {
            foreach (var line in network.OrderBy(l => l.Id.Value, StringComparer.Ordinal))
            {
                if (!float.IsFinite(line.Width) || line.Width <= 0) { warnings.Add("line-without-width:" + line.Id.Value); continue; }
                AddLine(shapes, line.Line, line.Width, channel, featureClass);
            }
        }
    }

    private static void AddLine(List<MapShape> shapes, Polyline line, float width, MapChannel channel, MapFeatureClass featureClass)
    {
        for (int i = 1; i < line.Points.Count; i++) shapes.Add(new(line.Points[i - 1], line.Points[i], width / 2d, channel, featureClass));
    }

    private static (MapChannel Channel, MapFeatureClass Class)? Classify(string kind) => kind switch
    {
        "water" or "lake" or "river" => (MapChannel.Water, MapFeatureClass.None),
        "green" or "grass" or "garden" => (MapChannel.Green, MapFeatureClass.None),
        "road" or "path" => (MapChannel.Road, MapFeatureClass.None),
        "building" => (MapChannel.Building, MapFeatureClass.Building),
        "landmark" => (MapChannel.Building, MapFeatureClass.Landmark),
        "plaza" or "paving" or "parking" => (MapChannel.Plaza, MapFeatureClass.Plaza),
        "sport" or "pitch" or "track" => (MapChannel.Plaza, MapFeatureClass.Sport),
        "bridge" or "boardwalk" => (MapChannel.Plaza, MapFeatureClass.Bridge),
        _ => null
    };

    private static string[] Channels(string name) => name switch
    {
        "map_sdf_a" => ["water-sdf", "green-sdf", "road-sdf", "building-sdf"],
        "map_sdf_b" => ["region-index", "feature-class", "plaza-sport-sdf", "campus-boundary-sdf"],
        "map_sdf_out" or "map_sdf_ring" => ["outskirts-block-sdf", "outskirts-road-sdf", "outskirts-water-sdf", "cut-line-sdf"],
        _ => throw new ArgumentException("Unknown map texture.")
    };

    private static void Validate(MapBakeInput input)
    {
        if (string.IsNullOrWhiteSpace(input.WorldModelHash) || string.IsNullOrWhiteSpace(input.CodeHash) || string.IsNullOrWhiteSpace(input.LabelInputHash))
            throw new ArgumentException("Bake identity requires world, code and label input hashes.");
        ValidateRect(input.Map.DataRect);
        ValidateRect(input.AtlasRect);
        var data = input.Map.DataRect;
        if (input.AtlasRect.Min.X > data.Min.X || input.AtlasRect.Min.Y > data.Min.Y || input.AtlasRect.Max.X < data.Max.X || input.AtlasRect.Max.Y < data.Max.Y)
            throw new ArgumentException("Atlas rectangle must contain the data rectangle.");
        foreach (int size in new[] { input.Options.MainSize, input.Options.ClassSize, input.Options.OutskirtsSize, input.Options.RingSize })
            if (size is < 1 or > 2048) throw new ArgumentOutOfRangeException(nameof(input), "Map texture size outside 1..2048.");
        if (!float.IsFinite(input.Options.BucketMetres) || input.Options.BucketMetres < 8 || input.Options.BucketMetres > 256 ||
            !float.IsFinite(input.Options.CampusMarginMetres) || input.Options.CampusMarginMetres < 0 || input.Options.CampusMarginMetres > 256 ||
            !float.IsFinite(input.Options.RegionCellMetres) || input.Options.RegionCellMetres < 1 || input.Options.RegionCellMetres > 64)
            throw new ArgumentException("Invalid map sampling settings.");
        double gridCells = Math.Ceiling((data.Max.X - data.Min.X) / input.Options.RegionCellMetres) *
            Math.Ceiling((data.Max.Y - data.Min.Y) / input.Options.RegionCellMetres);
        if (gridCells > 4 * 1024 * 1024) throw new ArgumentException("Region grid exceeds its cell limit.");
        AtlasLabelData.Validate(input.Labels);
    }

    internal static void ValidateRect(MapRect rect)
    {
        ArgumentNullException.ThrowIfNull(rect);
        AtlasLabelData.ValidatePoint(rect.Min); AtlasLabelData.ValidatePoint(rect.Max);
        if (rect.Max.X <= rect.Min.X || rect.Max.Y <= rect.Min.Y || rect.Max.X - rect.Min.X > 8192 || rect.Max.Y - rect.Min.Y > 8192)
            throw new ArgumentException("Invalid map rectangle.");
    }
}
