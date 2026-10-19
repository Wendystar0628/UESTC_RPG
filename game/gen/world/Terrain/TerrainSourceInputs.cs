using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

/// <summary>Pure source preparation. Bounds and dependency closure never construct or evaluate terrain.</summary>
internal static class TerrainSourceInputs
{
    private sealed record Source(string Key, string Stamp, MapRect Support, bool ReadsTerrain,
        IReadOnlyList<string> References);

    internal static TerrainInputSummary Prepare(TerrainCompileContext context, IReadOnlyList<TerrainSource> files)
    {
        var sources = new List<Source>();
        var references = new TerrainReferences(context);
        var operations = files.OrderBy(file => file.File, StringComparer.Ordinal).SelectMany(file => file.Data.Ops).ToArray();
        var range = TerrainCompiler.SourceRangeBound(operations);
        var waterGrade = new[] { context.Defaults?.LakeBankGrade ?? 1, context.Defaults?.ChannelBankGrade ?? 1 }
            .Concat(operations.OfType<TerrainLakeOp>().Select(op => TerrainReferences.Grade(op.Bank.Slope!)))
            .Concat(operations.OfType<TerrainChannelOp>().Where(op => !op.Dry).Select(op => TerrainReferences.Grade(op.Bank.Slope!))).Min();
        var waterDepth = new[] { context.Defaults?.LakeDepth ?? 0, context.Defaults?.ChannelDepth ?? 0 }
            .Concat(operations.OfType<TerrainLakeOp>().Select(op => op.Bed.MaxDepth))
            .Concat(operations.OfType<TerrainChannelOp>().Where(op => !op.Dry).Select(op => op.Depth)).Max();
        var waterReach = (range + TerrainSampling.MaximumHeight + waterDepth) / waterGrade + 4;
        var pads = operations.OfType<TerrainPadOp>().Select(op => op.Feature).ToHashSet();
        var waters = operations.Select(op => op switch
        { TerrainLakeOp lake => (FeatureId?)lake.Water, TerrainChannelOp channel => channel.Water, _ => null })
            .OfType<FeatureId>().ToHashSet();
        foreach (var feature in context.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
        {
            sources.Add(new("feature/" + feature.Id.Value, Stamp(feature), FeatureBounds(feature), false, []));
            if (!pads.Contains(feature.Id) && references.StructuralPadArea(feature) is { } pad)
                sources.Add(new("auto-pad/" + feature.Id.Value, Stamp(feature), Expand(pad.Bounds, TerrainCompiler.AutomaticPadApron + TerrainCompiler.AutomaticPadGrade), true, ["feature/" + feature.Id.Value]));
            if (waters.Contains(feature.Id)) continue;
            var polygons = WorldFeatureKinds.Parts(feature, "water").SelectMany(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)).ToArray();
            if (polygons.Length == 0) continue;
            // Includes the complete pre-water shoreline, connected water level solve and bank envelope.
            var support = Expand(new TerrainRegion(polygons).Bounds, waterReach);
            sources.Add(new("auto-water/" + feature.Id.Value, Stamp(feature), support, true, ["feature/" + feature.Id.Value]));
        }
        AddLines("road", context.Network.Roads);
        AddLines("waterway", context.Network.Waterways);
        AddLines("bridge", context.Network.Bridges);
        AddLines("culvert", context.Network.Culverts);
        foreach (var road in references.AutomaticRoads())
        {
            var authored = operations.OfType<TerrainCorridorOp>().Where(op => references.Road(op.Road, "world", op.Id).Source == road.Source).ToArray();
            var shoulder = Math.Max(context.Defaults?.CorridorShoulder ?? 0, authored.Select(op => op.Shoulder).DefaultIfEmpty(0).Max());
            var cross = Math.Max(TerrainCompiler.AutomaticRoadCrossFall, authored.Select(op => Math.Abs(op.CrossFall)).DefaultIfEmpty(0).Max());
            sources.Add(new("auto-road/" + road.Source.Value, Stamp(new { road.Source, road.Path.Line, road.Width, road.Kind, footprint = road.Footprint?.Polygons }),
                RoadBounds(road, shoulder, cross), true, ["road/" + road.Source.Value]));
        }
        foreach (var op in operations)
        {
            var refs = new List<string>();
            MapRect support;
            var reads = true;
            switch (op)
            {
                case TerrainMoundOp mound: support = ShapeBounds(mound.Shape); reads = false; break;
                case TerrainBasinOp basin: support = ShapeBounds(basin.Shape); reads = false; break;
                case TerrainPlatformOp platform:
                    support = Constraint(new TerrainRegion([platform.Rings]).Bounds, platform.Side, platform.Fillet); reads = false; break;
                case TerrainSlopeOp slope:
                    support = Constraint(Expand(GeometryBounds(slope.Geometry), (slope.Width ?? 0) / 2), slope.Side, slope.Fillet, slope.CrossFall); reads = false; break;
                case TerrainTerraceOp terrace:
                    support = TerrainCompiler.ConstraintBounds(range, Expand(Bounds(terrace.Front.Points), terrace.Tiers * terrace.Tread), .5, .5, 2); reads = false; break;
                case TerrainPadOp pad:
                    support = Expand(references.PadArea(pad.Feature, "world", pad.Id).Bounds, pad.Apron + pad.Grade);
                    refs.Add("feature/" + pad.Feature.Value);
                    if (pad.Edge is { } edge) refs.Add("feature/" + edge.StructureId);
                    break;
                case TerrainCorridorOp corridor:
                    var road = references.Road(corridor.Road, "world", corridor.Id);
                    // Ranged intervals share one full-road solve and its maximum shoulder/crossfall.
                    var siblings = operations.OfType<TerrainCorridorOp>().Where(other => references.Road(other.Road, "world", other.Id).Source == road.Source).ToArray();
                    support = RoadBounds(road, siblings.Max(other => other.Shoulder), siblings.Max(other => Math.Abs(other.CrossFall)));
                    refs.Add("feature/" + corridor.Road.Value); refs.Add("road/" + road.Source.Value);
                    refs.AddRange(corridor.Steps.Select(step => "feature/" + step.Structure.Value));
                    break;
                case TerrainLakeOp lake:
                    support = Expand(references.Area(lake.Water, "water", "world", lake.Id).Bounds, waterReach);
                    refs.Add("feature/" + lake.Water.Value); break;
                case TerrainChannelOp channel:
                    if (channel.Dry)
                    {
                        var radius = channel.Width!.Value / 2 + channel.Depth / TerrainReferences.Grade(channel.Bank.Inner!);
                        support = Expand(Bounds(channel.Line!.Points), radius + (channel.Bank.Outer is { } outer ? range / TerrainReferences.Grade(outer) : 0));
                    }
                    else
                    {
                        var region = references.Area(channel.Water!.Value, "water", "world", channel.Id);
                        var (path, width) = channel.Line is { } line ? (new TerrainPath(line), channel.Width!.Value) : references.WaterLine(channel.Water.Value, "world", channel.Id);
                        var grade = TerrainReferences.Grade(channel.Bank.Slope!);
                        support = Union(Expand(region.Bounds, waterReach),
                            TerrainCompiler.ConstraintBounds(range, Expand(path.Bounds, width / 2), grade, grade, 2));
                        refs.Add("feature/" + channel.Water.Value.Value);
                    }
                    foreach (var weir in channel.Weirs)
                    {
                        refs.Add("feature/" + weir.Structure.Value); refs.Add("feature/" + weir.Upstream.Value); refs.Add("feature/" + weir.Downstream.Value);
                    }
                    foreach (var crossing in channel.Crossings)
                    {
                        refs.Add("feature/" + crossing.Road.Value);
                        refs.Add("road/" + references.Road(crossing.Road, "world", channel.Id).Source.Value);
                    }
                    break;
                default: throw new ArgumentException("Unknown terrain source operation.");
            }
            sources.Add(new("operation/" + op.Id, Stamp<TerrainOp>(op), support, reads, refs));
        }

        // A pad median reads its whole source footprint; road fits and water groups read whole connected
        // sources. Follow those source dependencies transitively, including explicit remote structures.
        var dependencies = sources.Select(source => source.ReadsTerrain || source.References.Count != 0
            ? sources.Select((candidate, index) => (candidate, index))
                .Where(pair => source.ReadsTerrain && (Intersects(source.Support, pair.candidate.Support) ||
                    pair.candidate.ReadsTerrain && source.References.Any(reference => pair.candidate.References.Contains(reference))) ||
                    source.References.Contains(pair.candidate.Key))
                .Select(pair => pair.index).ToArray() : []).ToArray();
        var closures = new string[sources.Count];
        // Overlapping whole-source readers form one component. Resolve each component once; raw entities
        // and compact regional operations are leaves, so they never join unrelated readers by themselves.
        for (var index = 0; index < sources.Count; index++)
        {
            if (closures[index] is not null) continue;
            if (!sources[index].ReadsTerrain) { closures[index] = Stamp(new { sources[index].Key, sources[index].Stamp, sources[index].Support }); continue; }
            var readers = new HashSet<int>(); var leaves = new HashSet<int>(); var pending = new Stack<int>(); pending.Push(index);
            while (pending.TryPop(out var next))
            {
                if (!readers.Add(next)) continue;
                foreach (var dependency in dependencies[next])
                    if (sources[dependency].ReadsTerrain) pending.Push(dependency); else leaves.Add(dependency);
            }
            var stamp = Stamp(readers.Concat(leaves).Select(next => sources[next]).OrderBy(source => source.Key, StringComparer.Ordinal)
                .Select(source => new { source.Key, source.Stamp, source.Support }).ToArray());
            foreach (var reader in readers) closures[reader] = stamp;
        }
        var cells = new Dictionary<TileKey, List<string>>();
        var coverage = Expand(context.DataRect, TileGrid.BaseEdge);
        for (var index = 0; index < sources.Count; index++)
        {
            var support = sources[index].Support;
            if (!Intersects(support, coverage)) continue;
            var min = Vector2.Max(support.Min, coverage.Min); var max = Vector2.Min(support.Max, coverage.Max);
            var lo = TileGrid.At(new Vector3(min.X, 0, min.Y)); var hi = TileGrid.At(new Vector3(max.X, 0, max.Y));
            for (var z = lo.Z; z <= hi.Z; z++) for (var x = lo.X; x <= hi.X; x++)
            {
                var cell = new TileKey(x, z);
                if (!cells.TryGetValue(cell, out var stamps)) cells.Add(cell, stamps = []);
                stamps.Add(closures[index]);
            }
        }
        return new(Stamp(new { context.Map, context.DataRect, context.Boundary, context.EntityPrefix, context.Defaults }),
            cells.Select(pair => new TerrainInputCell(pair.Key, Stamp(pair.Value.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()))).ToArray());

        void AddLines(string kind, IReadOnlyList<NetworkLine> lines)
        {
            foreach (var line in lines.OrderBy(line => line.Id.Value, StringComparer.Ordinal))
                sources.Add(new(kind + "/" + line.Id.Value, Stamp(line), Expand(Bounds(line.Line.Points), line.Width / 2), false, []));
        }
        MapRect Constraint(MapRect box, TerrainSide side, double fillet, double cross = 0) =>
            TerrainCompiler.ConstraintBounds(range, box, TerrainReferences.Grade(side.Fill), TerrainReferences.Grade(side.Cut), fillet, cross);
        MapRect RoadBounds(TerrainRoad road, double shoulder, double cross)
        {
            var width = road.Width / 2 + shoulder;
            var reach = TerrainCompiler.CorridorReach(range, width, cross);
            return Union(Expand(road.Path.Bounds, reach), road.Footprint?.Bounds ?? road.Path.Bounds);
        }
    }

    private static MapRect FeatureBounds(WorldFeature feature) => new(new(feature.Bounds.Min.X, feature.Bounds.Min.Z), new(feature.Bounds.Max.X, feature.Bounds.Max.Z));
    private static MapRect ShapeBounds(TerrainShape shape) => shape switch
    {
        TerrainCircle circle => Expand(new(circle.Center, circle.Center), circle.Radius),
        TerrainEllipse ellipse => Expand(new(ellipse.Center, ellipse.Center), Math.Max(ellipse.Radii.X, ellipse.Radii.Y)),
        _ => throw new ArgumentException("Unknown terrain source shape.")
    };
    private static MapRect GeometryBounds(WorldGeometry geometry) => geometry switch
    {
        PointGeometry point => new(new(point.Position.X, point.Position.Z), new(point.Position.X, point.Position.Z)),
        PolylineGeometry line => Bounds(line.Line.Points),
        PolygonGeometry polygon => Bounds(polygon.Rings.Outer),
        MultiPolygonGeometry multi => Bounds(multi.Polygons.SelectMany(polygon => polygon.Outer)),
        _ => throw new ArgumentException("Unknown terrain source geometry.")
    };
    private static MapRect Bounds(IEnumerable<Vector2> points) => new(points.Aggregate(new Vector2(float.MaxValue), Vector2.Min), points.Aggregate(new Vector2(float.MinValue), Vector2.Max));
    private static MapRect Expand(MapRect box, double reach) => new(
        new(MathF.BitDecrement((float)(box.Min.X - reach)), MathF.BitDecrement((float)(box.Min.Y - reach))),
        new(MathF.BitIncrement((float)(box.Max.X + reach)), MathF.BitIncrement((float)(box.Max.Y + reach))));
    private static MapRect Union(MapRect a, MapRect b) => new(Vector2.Min(a.Min, b.Min), Vector2.Max(a.Max, b.Max));
    private static bool Intersects(MapRect a, MapRect b) => a.Min.X <= b.Max.X && a.Max.X >= b.Min.X && a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y;

    private static string Stamp<T>(T value)
    {
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes)) Write(writer, JsonSerializer.SerializeToElement(value, ContractJson.Options));
        var text = Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length));
        return string.Concat(Enumerable.Range(0, 4).Select(lane => StableHash.Hash32((uint)lane, text).ToString("x8", System.Globalization.CultureInfo.InvariantCulture)));
    }
    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                if (property.Name is "note" or "notes" or "reason") continue;
                writer.WritePropertyName(property.Name); Write(writer, property.Value);
            }
            writer.WriteEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray(); foreach (var item in element.EnumerateArray()) Write(writer, item); writer.WriteEndArray();
        }
        else element.WriteTo(writer);
    }
}
