using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.Walkable;

/// <summary>Development/CI-only deterministic ground walk bake. Runtime code consumes the boot product.</summary>
public static class WalkableBaker
{
    public static WalkBakeData Build(IWorldModel world, MapId map, MapRect rect, PolygonRings boundary, string inputHash)
        => Build(world, map, rect, boundary, inputHash, [], []);

    public static WalkBakeData Build(IWorldModel world, MapId map, MapRect rect, PolygonRings boundary, string inputHash,
        IEnumerable<Footprint2D> occupancy, IEnumerable<RampCollider> bridgeSupports)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(boundary);
        ValidateRect(rect);
        if (boundary.Outer.Count < 3) throw new ArgumentException("A campus boundary polygon is required.", nameof(boundary));
        var features = world.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal)
            .SelectMany(GeometryParts).ToArray();
        var bridges = new WalkBridgeSurfaces(world, occupancy, bridgeSupports);
        var fine = Classify(world, features, rect, boundary, 1f, bridges);
        var runtime = Classify(world, features, rect, boundary, 2f, bridges);
        var flags = new byte[runtime.Classes.Length];
        runtime.MarkNearBoundary(boundary, flags);
        var supports = SampleSupports(world.Spatial, runtime, fine, bridges);
        var quantizationOrigin = new Vector3((rect.Min.X + rect.Max.X) * .5f, 0f, (rect.Min.Y + rect.Max.Y) * .5f);
        var exclusions = features.Where(feature => feature.Kind.Value is "building" or "water" or "viewOnly" or "seal")
            .SelectMany(feature => Polygons(feature.Geometry)).Concat(bridges.Footprints.Select(item => item.Rings)).ToArray();
        var safe = WalkSafePoints.Build(runtime, fine, boundary, world.Spatial, quantizationOrigin, exclusions);
        var walls = WalkWalls.Extract(fine, world.Spatial, HeightAt);
        foreach (var feature in features)
        {
            if (feature.Kind.Value is not ("boundary" or "seal") || feature.Geometry is not PolylineGeometry line) continue;
            for (var i = 1; i < line.Line.Points.Count; i++)
            {
                var a = line.Line.Points[i - 1];
                var b = line.Line.Points[i];
                if (Vector2.DistanceSquared(a, b) <= .000001f) continue;
                var tag = feature.Kind.Value == "boundary" ? SurfaceTag.Boundary : SurfaceTag.DoorClosed;
                walls.Add(WalkWalls.Create(a, b, world.Spatial, tag, 2.2f, HeightAt));
            }
        }
        var product = new WalkBakeData(WalkBakeData.CurrentVersion, map, inputHash,
            new WalkGrid(runtime.Origin, runtime.Width, runtime.Height, runtime.Classes, flags, supports),
            new SafePointGrid(runtime.Origin, runtime.Width / 4, runtime.Height / 4, quantizationOrigin, safe), walls);
        product.Validate();
        return product;

        float HeightAt(Vector2 point) => bridges.TrySupport(point, out var y, out _) ? y : world.Spatial.GroundY(point.X, point.Y);
    }

    private static WalkRaster Classify(IWorldModel world, IReadOnlyList<WorldFeature> features, MapRect rect, PolygonRings boundary, float step,
        WalkBridgeSurfaces bridges)
    {
        var raster = new WalkRaster(rect, step);
        raster.Polygon(boundary, WalkClass.Walk, campus: true);
        foreach (var feature in features)
        {
            if (feature.Kind.Value == "water")
            {
                Apply(feature, WalkClass.Water);
                // Water holes are islands; they need an explicit open-region override to become walkable.
                foreach (var polygon in Polygons(feature.Geometry))
                    foreach (var hole in polygon.Holes) raster.Polygon(new PolygonRings(hole), WalkClass.Forbidden);
            }
        }
        foreach (var waterway in world.Network.Waterways.OrderBy(line => line.Id.Value, StringComparer.Ordinal))
            raster.Line(waterway.Line, waterway.Width, WalkClass.Water);
        foreach (var bridge in world.Network.Bridges.OrderBy(line => line.Id.Value, StringComparer.Ordinal))
            if (!bridges.ReplacedLines.Contains(bridge.Id)) raster.Line(bridge.Line, bridge.Width, WalkClass.Walk);
        foreach (var footprint in bridges.Footprints) raster.Polygon(footprint.Rings, WalkClass.Walk);
        foreach (var feature in features)
            if (feature.Kind.Value == "main") Apply(feature, WalkClass.Walk);
        foreach (var feature in features)
            if (feature.Kind.Value == "building") Apply(feature, WalkClass.Building);
        // Declared ground-level openings override building footprints; denied areas take final precedence.
        foreach (var kind in new[] { "interior", "door", "wade", "viewOnly", "seal" })
            foreach (var feature in features)
            {
                if (feature.Kind.Value != kind) continue;
                var value = kind switch
                {
                    "interior" or "door" => WalkClass.Interior,
                    "wade" => WalkClass.Wade,
                    "viewOnly" or "seal" => WalkClass.Forbidden,
                    _ => throw new InvalidOperationException("Unsupported walk region.")
                };
                Apply(feature, value);
            }
        return raster;

        void Apply(WorldFeature feature, WalkClass value)
        {
            foreach (var polygon in Polygons(feature.Geometry)) raster.Polygon(polygon, value);
            // Seal line geometry is a physical closure, not a guessed navigable strip.
            if (feature.Kind.Value == "seal" && feature.Geometry is PolylineGeometry seal)
                raster.Line(seal.Line, .3f, value);
        }
    }

    private static IEnumerable<PolygonRings> Polygons(OverlayGeometry geometry)
    {
        if (geometry is PolygonGeometry polygon) yield return polygon.Rings;
        if (geometry is MultiPolygonGeometry multi)
            foreach (var rings in multi.Polygons) yield return rings;
    }

    private static IEnumerable<WorldFeature> GeometryParts(WorldFeature feature)
    {
        if (feature.Parts.Count == 0)
        {
            yield return feature;
            yield break;
        }
        // Entity envelopes may mix buildings, park ground and POIs. Only each source part's own
        // geometry/kind may classify the ground; never paint the aggregate envelope a second time.
        foreach (var part in feature.Parts.OrderBy(part => part.SourceId, StringComparer.Ordinal))
        {
            var kind = feature.Kind.Value;
            if (part.Props is { ValueKind: JsonValueKind.Object } props &&
                props.TryGetProperty("kind", out var partKind) && partKind.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(partKind.GetString()))
                kind = partKind.GetString()!;
            yield return feature with
            {
                Kind = new DesignKind(kind),
                Geometry = part.Geometry,
                Parts = [],
                Height = part.Height,
                Storeys = part.Storeys,
                Props = part.Props
            };
        }
    }

    private static float[] SampleSupports(ISpatialIndex spatial, WalkRaster runtime, WalkRaster fine, WalkBridgeSurfaces bridges)
    {
        var heights = new float[runtime.Classes.Length];
        for (var z = 0; z < runtime.Height; z++)
            for (var x = 0; x < runtime.Width; x++)
            {
                var value = runtime.At(x, z);
                if (value is not (WalkClass.Walk or WalkClass.Interior or WalkClass.Wade)) continue;
                var point = runtime.Center(x, z);
                if (value == WalkClass.Walk && bridges.TrySupport(point, out var bridgeY, out var bridgeSlope))
                {
                    if (bridgeSlope > .25f)
                    {
                        runtime.Classes[z * runtime.Width + x] = (byte)WalkClass.Steep;
                        for (var fz = z * 2; fz < z * 2 + 2; fz++)
                            for (var fx = x * 2; fx < x * 2 + 2; fx++)
                                if (fine.At(fx, fz) == WalkClass.Walk) fine.Classes[fz * fine.Width + fx] = (byte)WalkClass.Steep;
                    }
                    else heights[z * runtime.Width + x] = bridgeY;
                    continue;
                }
                var y = spatial.GroundY(point.X, point.Y);
                var x0 = spatial.GroundY(point.X - 1f, point.Y);
                var x1 = spatial.GroundY(point.X + 1f, point.Y);
                var z0 = spatial.GroundY(point.X, point.Y - 1f);
                var z1 = spatial.GroundY(point.X, point.Y + 1f);
                if (!float.IsFinite(y) || !float.IsFinite(x0) || !float.IsFinite(x1) || !float.IsFinite(z0) || !float.IsFinite(z1))
                    throw new InvalidOperationException("Walkable terrain must have finite ground heights.");
                var dx = (x1 - x0) * .5f;
                var dz = (z1 - z0) * .5f;
                if (dx * dx + dz * dz > 1f)
                {
                    runtime.Classes[z * runtime.Width + x] = (byte)WalkClass.Steep;
                    for (var fz = z * 2; fz < z * 2 + 2; fz++)
                        for (var fx = x * 2; fx < x * 2 + 2; fx++)
                            if (fine.At(fx, fz) is WalkClass.Walk or WalkClass.Interior or WalkClass.Wade)
                                fine.Classes[fz * fine.Width + fx] = (byte)WalkClass.Steep;
                }
                else heights[z * runtime.Width + x] = y;
            }
        return heights;
    }

    private static void ValidateRect(MapRect rect)
    {
        ArgumentNullException.ThrowIfNull(rect);
        if (!float.IsFinite(rect.Min.X) || !float.IsFinite(rect.Min.Y) || !float.IsFinite(rect.Max.X) || !float.IsFinite(rect.Max.Y) ||
            rect.Max.X <= rect.Min.X || rect.Max.Y <= rect.Min.Y ||
            rect.Min.X % 8f != 0f || rect.Min.Y % 8f != 0f || rect.Max.X % 8f != 0f || rect.Max.Y % 8f != 0f)
            throw new ArgumentException("Walk bake extent must be finite and aligned to the 8 m safe grid.", nameof(rect));
        if ((double)(rect.Max.X - rect.Min.X) * (rect.Max.Y - rect.Min.Y) > 64_000_000d)
            throw new ArgumentException("Walk bake exceeds the supported 64 million fine cells.", nameof(rect));
    }
}
