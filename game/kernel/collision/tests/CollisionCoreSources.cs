using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Gen.Buildings;
using Campus.Gen.Collision;
using Campus.Gen.Vegetation;
using Campus.Kernel.Bake;
using Campus.Kernel.Registry;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Collision.Tests;

public sealed partial class CollisionCoreTests
{
    private IReadOnlyDictionary<string, Func<object, (PartGeometry Geometry, IReadOnlyList<Collider> Colliders)>> _parts =
        new Dictionary<string, Func<object, (PartGeometry, IReadOnlyList<Collider>)>>();

    private void CaptureParts(MapId map)
    {
        _parts = Services.Services.ForMap(map.Value).Where(entry => entry.Kind == "part")
            .ToDictionary(entry => entry.Id, entry =>
            {
                var instance = Services.Services.Create(entry.Type);
                var api = entry.Type.GetInterfaces().Single(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPartDef<>));
                return (Func<object, (PartGeometry, IReadOnlyList<Collider>)>)typeof(CollisionCoreTests)
                    .GetMethod(nameof(Part), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(api.GenericTypeArguments[0])
                    .Invoke(null, [instance])!;
            }, StringComparer.Ordinal);
    }
    private static Func<object, (PartGeometry, IReadOnlyList<Collider>)> Part<T>(IPartDef<T> part) where T : notnull =>
        value => (part.Lods[LodLevel.L0]((T)value), part.Collision((T)value));

    private void CheckSourceExpansion(BakeService.MapBuild map)
    {
        var claimed = map.Context.World.Features.Values.Where(feature =>
            (map.ClaimLookup(feature.Id, Aspects.Massing) ?? map.ClaimLookup(feature.Id, Aspects.Facade)) is not null
            && CollisionTileBuilder.BuildingFootprints(feature).Count != 0)
            .ToDictionary(feature => feature.Id, feature => feature);
        var measuredClaims = new HashSet<FeatureId>();
        var measured = 0;
        foreach (var pair in map.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var module = pair.Value.Value;
            // Ground has no placed parts or expansion samples; CheckGround already covers its complete formal C/L0 output.
            if (module is Campus.Gen.Ground.GroundModule) continue;
            Godot.GD.Print($"G15Core expansion module={pair.Key}");
            var context = map.CreateBuildContext(pair.Value.CodeHash);
            foreach (var tile in module.Tiles(context).Distinct().OrderBy(tile => tile.Z).ThenBy(tile => tile.X))
            {
                if (module is BuildingsModule buildings)
                {
                    foreach (var source in buildings.TraceSources(context, tile, LodLevel.L0))
                        Measure(source.SourceId, source.Output, source.Colliders, [source.OccupiedFootprint]);
                    continue;
                }
                if (module is VegetationModule vegetation)
                {
                    foreach (var source in vegetation.TraceSources(context, tile, LodLevel.L0))
                        Measure(source.SourceId, source.Output, source.Colliders);
                    continue;
                }
                var output = module.Build(context, tile, LodLevel.L0);
                foreach (var placed in output.Parts)
                {
                    var part = _parts[placed.Part.Value](placed.Params);
                    CheckExpansion($"part/{pair.Key}/{tile}/{placed.Part.Value}",
                        part.Geometry.Meshes.SelectMany(mesh => Triangles(mesh, placed.At)).ToArray(),
                        part.Colliders.Select(collider => CollisionGeometry.Transform(collider, placed.At)).ToArray());
                    if (part.Colliders.Count != 0) measured++;
                }
                var direct = module.Collide(context, tile);
                if (direct.Count == 0) continue;
                // Claimed modules must expose their actual source output and directed contours before
                // they can be measured. Neither a tile AABB nor an untransformed world footprint proves
                // ownership or the final visible contour; the missing-source assertion below stays red.
                if (module.Claims.Count == 0)
                    Measure($"module/{pair.Key}/{tile}", output, direct);
            }
        }
        foreach (var missing in claimed.Keys.Where(id => !measuredClaims.Contains(id)))
            Fail("No actual claimed-source expansion sample (source geometry and directed contour required): " + missing.Value);
        Require(measured > 0, "G15 measured no source-paired collision expansion.");
        Godot.GD.Print($"G15Core expansionSources={measured} claimedSources={measuredClaims.Count}/{claimed.Count}");

        void Measure(string id, ContentOutput output, IReadOnlyList<Collider> colliders, IReadOnlyList<PolygonRings>? occupied = null)
        {
            if (colliders.Count == 0) return;
            CheckExpansion(id, OutputTriangles(output).ToArray(), colliders, occupied); measured++;
        }
    }

    private IEnumerable<Triangle> OutputTriangles(ContentOutput output)
    {
        foreach (var mesh in output.Meshes)
            foreach (var triangle in Triangles(mesh, Matrix4x4.Identity)) yield return triangle;
        foreach (var part in output.Parts)
            foreach (var mesh in _parts[part.Part.Value](part.Params).Geometry.Meshes)
                foreach (var triangle in Triangles(mesh, part.At)) yield return triangle;
        foreach (var stream in output.Instances)
            for (var instance = 0; instance < stream.Count; instance++)
            {
                var data = stream.Data.Slice(instance * InstanceStreamLayout.FloatsPerInstance).ToArray();
                var at = new Matrix4x4(data[0], data[4], data[8], 0, data[1], data[5], data[9], 0,
                    data[2], data[6], data[10], 0, data[3], data[7], data[11], 1);
                foreach (var surface in stream.Prototype.Surfaces)
                {
                    var position = surface.Position.ToArray(); var index = surface.Index.ToArray();
                    Vector3 Point(int vertex) => Vector3.Transform(new(position[vertex * 3], position[vertex * 3 + 1], position[vertex * 3 + 2]), at);
                    for (var triangle = 0; triangle < index.Length; triangle += 3)
                        yield return new(Point(index[triangle]), Point(index[triangle + 1]), Point(index[triangle + 2]));
                }
            }
    }

    private void CheckExpansion(string source, IReadOnlyList<Triangle> visible, IReadOnlyList<Collider> colliders,
        IReadOnlyList<PolygonRings>? occupied = null)
    {
        foreach (var collider in colliders.Where(collider => collider is not HeightFieldCollider && collider.Tag is not (SurfaceTag.Ground or SurfaceTag.Invisible or SurfaceTag.Water)))
        {
            if (visible.Count == 0)
            { Fail($"source={source} has collision without matching visible geometry."); continue; }
            var maximum = MaximumOutwardExpansion(visible, collider, occupied);
            if (maximum > .3f) Fail($"source={source} {collider.GetType().Name} horizontal expansion={maximum:R}m > .3m.");
        }
    }

    private static float MaximumOutwardExpansion(IReadOnlyList<Triangle> visible, Collider collider,
        IReadOnlyList<PolygonRings>? occupied = null) =>
        HorizontalSamples(collider).Max(point => MathF.Sqrt(OutwardDistanceSquared(point, visible, occupied)));

    // Visible triangles already describe their occupied XZ projection, including any triangulated holes.
    // A one-sided building facade additionally needs its generator's directed solid footprint: filling
    // that material side removes inward wall thickness, while courtyard rings retain their air side.
    private static float OutwardDistanceSquared(Vector2 point, IReadOnlyList<Triangle> visible,
        IReadOnlyList<PolygonRings>? occupied)
    {
        var minimum = float.PositiveInfinity;
        foreach (var polygon in occupied ?? [])
        {
            if (ContainsPoint(polygon.Outer, point) && !polygon.Holes.Any(hole => ContainsPoint(hole, point))) return 0;
            foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                for (var edge = 0; edge < ring.Count; edge++)
                    minimum = MathF.Min(minimum, SegmentDistanceSquared(point, ring[edge], ring[(edge + 1) % ring.Count]));
        }
        foreach (var triangle in visible)
        {
            minimum = MathF.Min(minimum, DistanceSquared(point, triangle));
            if (minimum == 0) break;
        }
        return minimum;
    }

    private static bool ContainsPoint(IReadOnlyList<Vector2> ring, Vector2 point)
    {
        var inside = false;
        for (var edge = 0; edge < ring.Count; edge++)
        {
            var a = ring[edge]; var b = ring[(edge + 1) % ring.Count];
            if ((a.Y > point.Y) != (b.Y > point.Y)
                && point.X < a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y)) inside = !inside;
        }
        return inside;
    }

    private static IEnumerable<Vector2> HorizontalSamples(Collider collider)
    {
        Vector2[] ring;
        switch (collider)
        {
            case PostCollider post:
                for (var i = 0; i < 32; i++)
                { var angle = i * (2 * float.Pi / 32); yield return post.C + post.R * new Vector2(MathF.Cos(angle), MathF.Sin(angle)); }
                yield break;
            case WallCollider wall:
                ring = WallRing(wall.A, wall.B, wall.HalfT); break;
            case SlopedWallCollider wall:
                ring = WallRing(wall.A, wall.B, wall.HalfT); break;
            case SlabCollider slab:
                ring = slab.Ring.ToArray(); break;
            case RampCollider ramp:
                ring = [new(ramp.P0.X, ramp.P0.Z), new(ramp.P1.X, ramp.P1.Z), new(ramp.P2.X, ramp.P2.Z), new(ramp.P3.X, ramp.P3.Z)]; break;
            case BoxCollider box:
                var corners = Enumerable.Range(0, 8).Select(i => Vector3.Transform(new Vector3(
                    (i & 1) == 0 ? -box.Half.X : box.Half.X, (i & 2) == 0 ? -box.Half.Y : box.Half.Y,
                    (i & 4) == 0 ? -box.Half.Z : box.Half.Z), box.At)).ToArray();
                foreach (var point in corners) yield return new(point.X, point.Z);
                for (var i = 0; i < 8; i++)
                    foreach (var bit in new[] { 1, 2, 4 })
                        if ((i & bit) == 0) { var midpoint = (corners[i] + corners[i | bit]) / 2; yield return new(midpoint.X, midpoint.Z); }
                yield break;
            default: throw new NotSupportedException("No horizontal support samples for " + collider.GetType().Name);
        }
        for (var i = 0; i < ring.Length; i++) { yield return ring[i]; yield return (ring[i] + ring[(i + 1) % ring.Length]) / 2; }
    }
    private static Vector2[] WallRing(Vector2 a, Vector2 b, float half)
    {
        var direction = Vector2.Normalize(b - a); var offset = new Vector2(-direction.Y, direction.X) * half;
        return [a - offset, b - offset, b + offset, a + offset];
    }
}
