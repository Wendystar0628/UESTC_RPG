using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;

namespace Campus.Gen.Ground;

/// <summary>Sidewalk tactile paving, utility covers and parking lines use the existing draped marking stream.</summary>
internal sealed partial class RoadMarkings
{
    private readonly List<Vector2> _manholes = [];

    private void AddGroundDetails(GroundLayout layout, GroundSection section, List<RoadGraph.Junction> junctions)
    {
        var mainRoads = layout.Roads.Where(r => r.Carriageway && r.Width >= section.Markings.CentreLineMinWidth).ToArray();
        var spec = section.Details;
        var level = section.PavingY - section.RoadY;
        var covers = _manholes;
        foreach (var walk in DetailWalks(layout))
        {
            var length = (float)PlanLines.Length(walk.Points);
            if (length < spec.TactileWarningDepth || walk.Width < spec.TactileWidth + 2f * spec.TactileEdgeClear) continue;
            var rng = new Rng(StableHash.Hash32(0u, "ground-details", walk.Id.Value));
            var nextCover = spec.ManholeSpacingMin * rng.NextFloat();
            for (var distance = 0f; distance < length; distance += 1f)
            {
                var end = Math.Min(distance + 1f, length);
                var mid = PlanLines.Sample(walk.Points, (distance + end) * 0.5f);
                RoadLine? main = null;
                var nearest = float.PositiveInfinity;
                foreach (var road in mainRoads)
                {
                    var d = float.PositiveInfinity;
                    for (var k = 0; k + 1 < road.Points.Length; k++)
                        d = Math.Min(d, (float)PlanLines.SegmentDistance(mid.Point.X, mid.Point.Y, road.Points[k], road.Points[k + 1]));
                    d -= road.Width * 0.5f;
                    if (d >= 0f && d < nearest) (main, nearest) = (road, d);
                }
                if (main is null || nearest > walk.Width + spec.TactileEdgeClear) continue;
                var side = new Vector2(-mid.Tangent.Y, mid.Tangent.X);
                var outer = walk.Width * 0.5f - spec.TactileEdgeClear - spec.TactileWidth * 0.5f;
                // The side away from the carriageway is the planting/tree-pit side.
                if (layout.SignedDistance(SurfaceKind.Carriageway, mid.Point + side * outer, 16f) <
                    layout.SignedDistance(SurfaceKind.Carriageway, mid.Point - side * outer, 16f)) side = -side;
                var a = PlanLines.Sample(walk.Points, distance).Point + side * outer;
                var b = PlanLines.Sample(walk.Points, end).Point + side * outer;
                if (OnWalk(layout, a, spec.TactileWidth * 0.5f) && OnWalk(layout, b, spec.TactileWidth * 0.5f) &&
                    OnWalk(layout, (a + b) * 0.5f, spec.TactileWidth * 0.5f))
                {
                    // At a road crossing the final 0.6 m is a warning pad; otherwise the three ribs follow travel.
                    var warning = _crossings.Any(p => Vector2.Distance(p, mid.Point) < main.Width * 0.5f + spec.TactileWarningDepth) ||
                        NearJunction(junctions, mid.Point, spec.TactileWarningDepth);
                    if (warning && layout.SignedDistance(SurfaceKind.Carriageway, a, 16f) <
                        layout.SignedDistance(SurfaceKind.Carriageway, b, 16f)) (a, b) = (b, a);
                    AddTactile(a, b, side, warning, spec, level, section.DecalLift);
                }
                if (distance < nextCover) continue;
                nextCover = distance + spec.ManholeSpacingMin + (spec.ManholeSpacingMax - spec.ManholeSpacingMin) * rng.NextFloat();
                var cover = mid.Point - side * outer;
                if (!OnWalk(layout, cover, spec.ManholeDiameter * 0.5f) ||
                    covers.Any(p => Vector2.DistanceSquared(p, cover) < spec.ManholeDiameter * spec.ManholeDiameter)) continue;
                AddManhole(cover, spec, level, section.DecalLift);
                covers.Add(cover);
            }
        }
        AddParking(layout, section);
    }

    public IReadOnlyList<GeometryBundle> EmitManholeText(BuildContext context, GroundTile tile, GroundSection section, TerrainGrid terrain)
    {
        var centres = _manholes.Where(p => p.X >= tile.X0 && p.X < tile.X0 + tile.Size &&
            p.Y >= tile.Z0 && p.Y < tile.Z0 + tile.Size).ToArray();
        if (centres.Length == 0) return [];
        var atlas = new SceneTextAtlas(context.GlobalInputs.Require(SceneTextInputs.AtlasKey));
        var ink = GroundPalette.Spec(GroundMaterial.Marking) with
        {
            Lit = GroundPalette.Lit(GroundColor.Concrete), Shade = GroundPalette.Shade(GroundColor.Concrete)
        };
        var background = context.Catalog.Resolve(GroundPalette.Lit(GroundColor.UtilityIron));
        var diameter = section.Details.ManholeDiameter;
        var result = new List<GeometryBundle>();
        var placed = new PartBuilder();
        foreach (var centre in centres)
        {
            var index = _manholes.IndexOf(centre);
            var label = new PartBuilder();
            Lettering.Write(label, atlas.Layout(index % 2 == 0 ? "雨水" : "污水"), background,
                0f, -diameter * 0.3f, 0f, false, diameter * 0.12f, ink);
            var (min, max) = label.Measure();
            placed.Add(label.Finish(new Box3(min, max)), Matrix4x4.CreateRotationX(MathF.PI * 0.5f) *
                Matrix4x4.CreateTranslation(centre.X, section.PavingY + section.DecalLift, centre.Y));
        }
        var bounds = placed.Measure();
        foreach (var mesh in placed.Finish(new Box3(bounds.Min, bounds.Max)).Meshes)
        {
            var positions = mesh.Position;
            for (var v = 0; v < positions.Length; v += 3)
                positions[v + 1] += terrain.At(positions[v], positions[v + 2]);
            result.Add(mesh);
        }
        return result;
    }

    private static IEnumerable<RoadLine> DetailWalks(GroundLayout layout)
    {
        var network = layout.Roads.Select(r => r.Id).ToHashSet();
        foreach (var walk in layout.Roads)
            if (!walk.Carriageway && walk.Type is "footway" or "path") yield return walk;
        // Area sidewalks are not in the centre-line network. Recover their long axis from the existing footprint;
        // exact walk-distance checks below keep the strip inside the authored area, including its holes.
        foreach (var shape in layout.Shapes)
        {
            if (shape.Kind != SurfaceKind.Walk || network.Contains(shape.Id) || shape.Rings is not { Length: > 0 } rings) continue;
            if (DetailFrame(rings[0]) is not { } frame) continue;
            var (axis, across, minU, maxU, minV, maxV) = frame;
            if (maxU - minU < 2f * (maxV - minV)) continue;
            var centreV = (minV + maxV) * 0.5f;
            yield return new(shape.Id, [axis * minU + across * centreV, axis * maxU + across * centreV], maxV - minV, false, "footway");
        }
    }

    private void AddTactile(Vector2 a, Vector2 b, Vector2 side, bool warning, GroundDetailSpec spec, float level, float lift)
    {
        var length = Vector2.Distance(a, b);
        if (warning && length > spec.TactileWarningDepth)
        {
            var split = b - Vector2.Normalize(b - a) * spec.TactileWarningDepth;
            AddTactile(a, split, side, false, spec, level, lift);
            a = split;
            length = spec.TactileWarningDepth;
        }
        AddStrip(a, b, spec.TactileWidth, GroundColor.MarkingYellow, level);
        var insetLevel = level + lift;
        if (warning)
        {
            var along = Vector2.Normalize(b - a);
            var step = spec.TactileWidth / 3f;
            for (var d = step * 0.5f; d < length; d += step)
                for (var row = -1; row <= 1; row++)
                {
                    var p = a + along * d + side * (row * step);
                    AddStrip(p - along * (step * 0.15f), p + along * (step * 0.15f), step * 0.3f, GroundColor.UtilityIron, insetLevel);
                }
        }
        else
        {
            for (var rib = -1; rib <= 1; rib++)
            {
                var offset = side * (rib * spec.TactileWidth / 3f);
                AddStrip(a + offset, b + offset, spec.TactileWidth / 15f, GroundColor.UtilityIron, insetLevel);
            }
        }
    }

    private void AddManhole(Vector2 centre, GroundDetailSpec spec, float level, float lift)
    {
        var radius = spec.ManholeDiameter * 0.5f;
        var arc = Arc(centre.X, centre.Y, radius, 0f, MathF.Tau, 1f, 1f, spec.ManholeDiameter / 8f);
        for (var k = 0; k + 1 < arc.Count; k++)
            AddQuad(new(centre, arc[k], arc[k + 1], centre, GroundColor.UtilityIron, level), centre);
        // A circuit ring and a geometric lightning emblem are original, without reusing the school crest.
        var inner = Arc(centre.X, centre.Y, radius * 0.8f, 0f, MathF.Tau, 1f, 1f, spec.ManholeDiameter / 8f);
        for (var k = 0; k + 1 < inner.Count; k++)
            AddStrip(inner[k], inner[k + 1], radius * 0.05f, GroundColor.Concrete, level + lift);
        Vector2[] bolt = [new(-0.05f, -0.20f), new(-0.13f, 0.02f), new(0.08f, -0.02f), new(0.01f, 0.20f)];
        for (var k = 0; k + 1 < bolt.Length; k++)
            AddStrip(centre + bolt[k] * spec.ManholeDiameter, centre + bolt[k + 1] * spec.ManholeDiameter,
                radius * 0.08f, GroundColor.Concrete, level + lift);
    }

    private void AddParking(GroundLayout layout, GroundSection section)
    {
        var spec = section.Details;
        foreach (var shape in layout.Shapes)
        {
            if (shape.Kind != SurfaceKind.Parking || shape.Rings is not { Length: > 0 } rings) continue;
            if (DetailFrame(rings[0]) is not { } frame) continue;
            var (axis, across, minU, maxU, minV, maxV) = frame;
            var margin = spec.ParkingLine;
            foreach (var side in new[] { -1f, 1f })
            {
                var back = side < 0f ? minV + margin : maxV - margin;
                var front = back - side * spec.ParkingDepth;
                // Keep the central aisle open; a shallow parking area gets only its first row.
                if (front < minV + margin || front > maxV - margin ||
                    side > 0f && maxV - minV < 2f * spec.ParkingDepth + spec.ParkingWidth) continue;
                for (var u = minU + margin; u + spec.ParkingWidth < maxU - margin; u += spec.ParkingWidth)
                {
                    var a = axis * u + across * back;
                    var b = axis * (u + spec.ParkingWidth) + across * back;
                    var c = axis * (u + spec.ParkingWidth) + across * front;
                    var d = axis * u + across * front;
                    if (!OnParking(layout, shape, a, margin) || !OnParking(layout, shape, b, margin) ||
                        !OnParking(layout, shape, c, margin) || !OnParking(layout, shape, d, margin) ||
                        !OnParking(layout, shape, (a + b) * 0.5f, margin) || !OnParking(layout, shape, (a + d) * 0.5f, margin) ||
                        !OnParking(layout, shape, (b + c) * 0.5f, margin)) continue;
                    AddStrip(a, b, spec.ParkingLine, GroundColor.Marking);
                    AddStrip(a, d, spec.ParkingLine, GroundColor.Marking);
                    AddStrip(b, c, spec.ParkingLine, GroundColor.Marking);
                }
            }
        }
    }

    private static (Vector2 Axis, Vector2 Across, float MinU, float MaxU, float MinV, float MaxV)? DetailFrame(Vector2[] ring)
    {
        var axis = Vector2.Zero;
        for (var k = 0; k < ring.Length; k++)
        {
            var edge = ring[(k + 1) % ring.Length] - ring[k];
            if (edge.LengthSquared() > axis.LengthSquared()) axis = edge;
        }
        if (axis.LengthSquared() < 1f) return null;
        axis = Vector2.Normalize(axis);
        var across = new Vector2(-axis.Y, axis.X);
        return (axis, across, ring.Min(p => Vector2.Dot(p, axis)), ring.Max(p => Vector2.Dot(p, axis)),
            ring.Min(p => Vector2.Dot(p, across)), ring.Max(p => Vector2.Dot(p, across)));
    }

    private static bool OnWalk(GroundLayout layout, Vector2 p, float margin) =>
        layout.SignedDistance(SurfaceKind.Walk, p, margin + 1f) < -margin &&
        layout.SignedDistance(SurfaceKind.Carriageway, p, margin + 1f) > margin &&
        layout.SignedDistance(SurfaceKind.Parking, p, margin + 1f) > margin &&
        layout.SignedDistance(SurfaceKind.Pit, p, margin + 1f) > margin &&
        layout.SignedDistance(SurfaceKind.Building, p, margin + 1f) > margin &&
        layout.SignedDistance(SurfaceKind.Water, p, margin + 1f) > margin;

    private static bool OnParking(GroundLayout layout, GroundShape shape, Vector2 p, float margin) =>
        Sdf.Shape(shape, p, margin + 1f) < -margin * 0.5f &&
        layout.SignedDistance(SurfaceKind.Carriageway, p, margin + 1f) > margin &&
        layout.SignedDistance(SurfaceKind.Building, p, margin + 1f) > margin &&
        layout.SignedDistance(SurfaceKind.Water, p, margin + 1f) > margin;
}
