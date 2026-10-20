using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Audio;
using Campus.Gen.World;

namespace Campus.Gen.Collision;

public sealed record CollisionInterior(string Id, IInteriorProvider Provider, IReadOnlyList<FeatureId> FeatureIds);
public sealed record CollisionBuildingFootprint(string SourceId, PolygonRings Rings, float? Height, int? Storeys);

/// <summary>Combines the registered pure generators. Call only from the bake worker.</summary>
public sealed class CollisionTileBuilder(
    IReadOnlyList<PreparedContentModule> modules,
    Func<PlacedPart, IReadOnlyList<Collider>> partCollision)
{
    public IReadOnlyList<Collider> Build(BuildContext context, TileKey tile)
    {
        var interiors = CollisionInteriorProviders.Resolve(context);
        var world = new List<Collider>();
        foreach (var entry in modules.OrderBy(m => m.Id, StringComparer.Ordinal))
        {
            if (!entry.Tiles.Contains(tile)) continue;
            var moduleContext = entry.Context.Memo(context.World, () => entry.Context.WithWorld(context.World));
            world.AddRange(entry.Module.Collide(moduleContext, tile));
            foreach (var part in entry.Module.Build(moduleContext, tile, LodLevel.L0).Parts)
                world.AddRange(partCollision(part).Select(c => CollisionGeometry.Transform(c, part.At)));
        }
        foreach (var feature in context.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            var entry = context.World.Interiors.Get(feature.Id);
            foreach (var footprint in BuildingFootprints(feature, entry is { Open: true }))
            {
                var origin = TileGrid.Origin(tile);
                var minimum = footprint.Rings.Outer.Aggregate(Vector2.Min) - new Vector2(.3f);
                var maximum = footprint.Rings.Outer.Aggregate(Vector2.Max) + new Vector2(.3f);
                if (maximum.X < origin.X || minimum.X > origin.X + TileGrid.BaseEdge ||
                    maximum.Y < origin.Y || minimum.Y > origin.Y + TileGrid.BaseEdge) continue;
                world.AddRange(BuildingWalls(footprint.Rings, context.World.Spatial, entry, footprint.Height)
                    .Where(w => CollisionGeometry.IntersectsTile(w, tile)));
            }
        }
        // The mainline uses floor batches inside C tiles; room-cluster residency is deferred.
        foreach (var provider in interiors.OrderBy(i => i.Id, StringComparer.Ordinal))
            foreach (var feature in provider.FeatureIds)
            {
                if (!provider.Provider.Walkable) continue;
                var layout = provider.Provider.Layout(context, feature);
                var tileOrigin = TileGrid.Origin(tile);
                if (layout.Bounds.Max.X < tileOrigin.X || layout.Bounds.Min.X > tileOrigin.X + TileGrid.BaseEdge ||
                    layout.Bounds.Max.Z < tileOrigin.Y || layout.Bounds.Min.Z > tileOrigin.Y + TileGrid.BaseEdge) continue;
                foreach (var storey in layout.OpenStoreys.Order())
                {
                    world.AddRange(provider.Provider.CollisionStorey(context, feature, storey)
                        .Where(c => CollisionGeometry.IntersectsTile(c, tile)));
                    foreach (var part in provider.Provider.BuildStorey(context, feature, storey, InteriorDetail.Full).Parts)
                        world.AddRange(partCollision(part).Select(c => CollisionGeometry.Transform(c, part.At))
                            .Where(c => CollisionGeometry.IntersectsTile(c, tile)));
                }
            }
        return CollisionGeometry.Localize(world.Where(c => !IsGroundTrim(c, context.World.Terrain)), tile);
    }

    private static bool IsGroundTrim(Collider source, ITerrain terrain)
    {
        if (source is HeightFieldCollider || source.Tag is not (SurfaceTag.Ground or SurfaceTag.Prop)) return false;
        var bounds = CollisionGeometry.Bounds(source);
        var size = bounds.Max - bounds.Min;
        var width = source is BoxCollider box ? 2 * MathF.Min(
            box.Half.X * new Vector3(box.At.M11, box.At.M12, box.At.M13).Length(),
            box.Half.Z * new Vector3(box.At.M31, box.At.M32, box.At.M33).Length()) : MathF.Min(size.X, size.Z);
        if (size.Y > WalkRules.StepHeight || width > .5f) return false;
        var center = (bounds.Min + bounds.Max) / 2;
        var ground = terrain.HeightAt(center.X, center.Z);
        // Only low narrow trim resting on the ground is omitted. Elevated thin floors and stairs remain solid.
        return bounds.Min.Y <= ground + .05f && bounds.Max.Y >= ground - .05f && bounds.Max.Y <= ground + WalkRules.StepHeight;
    }

    /// <summary>Parts replace the aggregate query envelope, including for mixed POI parents.</summary>
    public static IReadOnlyList<CollisionBuildingFootprint> BuildingFootprints(WorldFeature feature, bool exteriorOnly = false)
    {
        var result = new List<CollisionBuildingFootprint>();
        if (feature.Parts.Count > 0)
        {
            foreach (var part in feature.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
            {
                var kind = part.Props.TryText("kind") ?? feature.Kind.Value;
                if (kind == "building") Add(part.SourceId, part.Geometry, part.Height, part.Storeys);
            }
        }
        else if (feature.Kind.Value == "building")
            Add(feature.Id.Value, feature.Geometry, feature.Height, feature.Storeys);
        if (!exteriorOnly || result.Count < 2) return result;
        // Open interiors connect their overlapping/adjacent massing blocks. Only the union perimeter is an exterior wall.
        var exterior = TerrainUnion.Build(result.Select(part => new TerrainRegion([part.Rings])));
        return exterior.Polygons.Select(rings => new CollisionBuildingFootprint(feature.Id.Value, rings,
            result.Max(part => part.Height), result.Max(part => part.Storeys))).ToArray();

        void Add(string sourceId, WorldGeometry geometry, float? height, int? storeys)
        {
            switch (geometry)
            {
                case PolygonGeometry polygon:
                    result.Add(new(sourceId, polygon.Rings, height, storeys));
                    break;
                case MultiPolygonGeometry multi:
                    foreach (var polygon in multi.Polygons) result.Add(new(sourceId, polygon, height, storeys));
                    break;
            }
        }
    }

    /// <summary>Wall thickness goes inside each ring; declared open entrances cut their absolute doorway range.</summary>
    public static IReadOnlyList<WallCollider> BuildingWalls(PolygonRings polygon, ISpatialIndex spatial, InteriorIndexEntry? interior, float? buildingHeight = null)
    {
        const float halfThickness = 0.075f;
        if (buildingHeight is { } height && (!float.IsFinite(height) || height < 0))
            throw new ArgumentException("Building height must be finite and non-negative.", nameof(buildingHeight));
        // SYS-WALK requires a roughly 300 m wall to keep closed roofs inaccessible.
        // A taller source part may extend it; the parent envelope's height is never substituted.
        var collisionHeight = MathF.Max(WalkRules.BuildingCollisionHeight, buildingHeight ?? 0f);
        var result = new List<WallCollider>();
        AddRing(polygon.Outer, false);
        foreach (var hole in polygon.Holes) AddRing(hole, true);
        return result;

        void AddRing(IReadOnlyList<Vector2> ring, bool hole)
        {
            if (ring.Count < 3) return;
            var area = PlanGeometry.SignedArea(ring);
            for (var i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                var length = Vector2.Distance(a, b);
                if (length < 0.001f) continue;
                var direction = (b - a) / length;
                var inward = new Vector2(-direction.Y, direction.X) * ((area > 0) != hole ? 1 : -1);
                var edgeGround = MathF.Min(spatial.GroundY(a.X, a.Y), spatial.GroundY(b.X, b.Y));
                var bottom = MinimumExteriorGround(a, b, inward, spatial) - 2f;
                var top = edgeGround + collisionHeight;
                var cuts = new List<EntranceWallOpening>();
                foreach (var door in interior?.Entrances ?? [])
                {
                    var center = new Vector2(door.Center.X, door.Center.Z);
                    var t = Vector2.Dot(center - a, direction);
                    var projected = a + direction * Math.Clamp(t, 0, length);
                    if (Vector2.Distance(center, projected) > 0.5f || door.ClearWidth <= 0) continue;
                    var start = Math.Clamp(t - door.ClearWidth / 2, 0, length);
                    var end = Math.Clamp(t + door.ClearWidth / 2, 0, length);
                    var head = door.HeadY != 0f ? door.HeadY : door.Center.Y + WalkRules.UnknownEntranceCollisionHeight;
                    if (end > start) cuts.Add(new(start, end, door.Center.Y, head, EntranceWallOpenings.IsOpen(interior, door)));
                }
                result.AddRange(EntranceWallOpenings.Cut(new WallCollider(a + inward * halfThickness, b + inward * halfThickness,
                    halfThickness, bottom, top, SurfaceTag.Wall) { Foot = FootMat.Concrete }, cuts, halfThickness, halfThickness));
            }
        }
    }

    private static float MinimumExteriorGround(Vector2 a, Vector2 b, Vector2 inward, ISpatialIndex spatial)
    {
        // GroundY is the map's authoritative terrain query. Sample its L0 one-metre lattice in the exterior 3 m band.
        const float width = 3f;
        var direction = Vector2.Normalize(b - a);
        var length = Vector2.Distance(a, b);
        var minimum = MathF.Min(spatial.GroundY(a.X, a.Y), spatial.GroundY(b.X, b.Y));
        var min = Vector2.Min(a, b) - new Vector2(width);
        var max = Vector2.Max(a, b) + new Vector2(width);
        for (var z = (int)MathF.Floor(min.Y); z <= (int)MathF.Ceiling(max.Y); z++)
            for (var x = (int)MathF.Floor(min.X); x <= (int)MathF.Ceiling(max.X); x++)
            {
                var offset = new Vector2(x, z) - a;
                var along = Vector2.Dot(offset, direction);
                var outside = -Vector2.Dot(offset, inward);
                var closest = a + direction * Math.Clamp(along, 0, length);
                if (outside < 0 || Vector2.DistanceSquared(new(x, z), closest) > width * width) continue;
                var height = spatial.GroundY(x, z);
                if (!float.IsFinite(height)) throw new InvalidOperationException("Building collision band has non-finite terrain.");
                minimum = MathF.Min(minimum, height);
            }
        return minimum;
    }
}
