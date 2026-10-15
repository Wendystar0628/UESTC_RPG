using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;

namespace Campus.Pipeline.Map;

public static class AtlasLabelData
{
    internal static JsonSerializerOptions JsonOptions { get; } = new(ContractJson.Options)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static AtlasLabels Parse(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > 4 * 1024 * 1024) throw new InvalidDataException("Atlas labels exceed 4 MiB.");
        var labels = JsonSerializer.Deserialize<AtlasLabels>(utf8, JsonOptions)
            ?? throw new InvalidDataException("Atlas labels are missing.");
        Validate(labels);
        return labels;
    }

    public static void Validate(AtlasLabels data)
    {
        if (data.Version != 1 || data.Regions is null || data.Places is null || data.Labels is null)
            throw new InvalidDataException("Unsupported or incomplete atlas labels.");
        if (data.Regions.Count + data.Places.Count > 255 || data.Labels.Count > 4096)
            throw new InvalidDataException("Atlas label or region limit exceeded.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var regionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var region in data.Regions)
        {
            ValidateRegion(region, ids);
            if (region.Parent is not null) throw new InvalidDataException("A top-level region cannot have a parent.");
            regionIds.Add(region.Id);
        }
        foreach (var place in data.Places)
        {
            ValidateRegion(place, ids);
            if (place.Parent is null || !regionIds.Contains(place.Parent))
                throw new InvalidDataException($"Place {place.Id} has an unknown parent.");
        }
        var labelIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var label in data.Labels)
        {
            if (string.IsNullOrWhiteSpace(label.Id) || !labelIds.Add(label.Id)) throw new InvalidDataException("Duplicate/empty label id.");
            _ = LabelLayouter.MinBand(label.Kind);
            if (label.At is null && (label.Line is null || label.Line.Count < 2)) throw new InvalidDataException($"Label {label.Id} has no position.");
            if (label.At is { } at) ValidatePoint(at);
            if (label.Line is { } line) foreach (var point in line) ValidatePoint(point);
            if (label.Kind is "region" or "place" or "landmark")
            {
                if (label.Name is not null || string.IsNullOrWhiteSpace(label.Ref))
                    throw new InvalidDataException($"Label {label.Id} must refer to its canonical name.");
                if ((label.Kind is "region" or "place") && !ids.Contains(label.Ref!))
                    throw new InvalidDataException($"Label {label.Id} refers to an unknown region/place.");
            }
            else if (label.Name is null && label.Ref is null) throw new InvalidDataException($"Label {label.Id} has no name.");
        }
        foreach (var id in ids.Order(StringComparer.Ordinal))
            if (!data.Labels.Any(label => label.Ref == id && label.At is not null))
                throw new InvalidDataException($"Region/place {id} has no registered seed.");
    }

    internal static Vector2 Anchor(AtlasLabel label)
    {
        if (label.At is { } at) return at;
        var points = label.Line!;
        return points[points.Count / 2];
    }

    private static void ValidateRegion(AtlasRegion region, HashSet<string> ids)
    {
        if (string.IsNullOrWhiteSpace(region.Id) || !ids.Add(region.Id) ||
            string.IsNullOrWhiteSpace(region.Name.Zh) || string.IsNullOrWhiteSpace(region.Name.En))
            throw new InvalidDataException("Invalid region/place name or duplicate id.");
    }

    internal static void ValidatePoint(Vector2 p)
    {
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) throw new InvalidDataException("Non-finite atlas coordinate.");
    }
}

internal sealed class RegionSampler
{
    private sealed record Seed(string Id, string? Parent, Vector2 At, byte Index);
    private readonly Seed[] _regions;
    private readonly Seed[] _places;
    private readonly PolygonRings _boundary;
    private readonly MapRegionArea[] _areas;
    internal string[] Ids { get; }

    internal RegionSampler(AtlasLabels labels, PolygonRings boundary, IReadOnlyList<MapRegionArea> areas)
    {
        _boundary = boundary;
        var records = labels.Regions.Concat(labels.Places).OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
        Ids = ["", .. records.Select(r => r.Id)];
        var seeds = records.Select((r, i) => new Seed(r.Id, r.Parent,
            labels.Labels.Where(l => l.Ref == r.Id && l.At is not null).OrderBy(l => l.Id, StringComparer.Ordinal).First().At!.Value,
            checked((byte)(i + 1)))).ToArray();
        _regions = seeds.Where(s => s.Parent is null).ToArray();
        _places = seeds.Where(s => s.Parent is not null).ToArray();
        _areas = areas.OrderBy(a => a.Id, StringComparer.Ordinal).ToArray();
        if (_areas.Select(a => a.Id).Distinct(StringComparer.Ordinal).Count() != _areas.Length)
            throw new InvalidDataException("Duplicate atlas region area.");
        foreach (var area in _areas)
        {
            if (!_regions.Any(r => r.Id == area.Id)) throw new InvalidDataException($"Unknown atlas area {area.Id}.");
            foreach (var polygon in area.Polygons)
            {
                MapDistance.ValidateRing(polygon.Outer);
                foreach (var hole in polygon.Holes) MapDistance.ValidateRing(hole);
            }
        }
    }

    internal byte At(double x, double z, double dilation = 0)
    {
        if (_regions.Length == 0 || MapDistance.Polygon(_boundary, x, z) > dilation) return 0;
        var area = _areas.FirstOrDefault(a => a.Polygons.Any(p => MapDistance.Polygon(p, x, z) <= 0));
        if (area is null) return 0;
        var region = _regions.First(r => r.Id == area.Id);
        return (Nearest(_places, region.Id, x, z) ?? region).Index;
    }

    internal MapRegionGrid Build(MapRect rect, float cellMetres, CancellationToken cancellationToken)
    {
        int width = checked((int)Math.Ceiling((rect.Max.X - rect.Min.X) / cellMetres));
        int height = checked((int)Math.Ceiling((rect.Max.Y - rect.Min.Y) / cellMetres));
        var cells = new byte[checked(width * height)];
        for (int z = 0; z < height; z++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = 0; x < width; x++)
                cells[z * width + x] = At(rect.Min.X + (x + 0.5) * cellMetres, rect.Min.Y + (z + 0.5) * cellMetres);
        }
        return new(rect, width, height, cellMetres, Ids, cells);
    }

    private static Seed? Nearest(Seed[] seeds, string? parent, double x, double z)
    {
        Seed? nearest = null;
        double best = double.MaxValue;
        foreach (var seed in seeds)
        {
            if (seed.Parent != parent) continue;
            double dx = x - seed.At.X, dz = z - seed.At.Y;
            double distance = dx * dx + dz * dz;
            if (distance < best) { best = distance; nearest = seed; }
        }
        return nearest;
    }
}
