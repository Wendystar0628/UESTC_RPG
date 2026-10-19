using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.North.Identity;

/// <summary>The existing two-storey link becomes the transparent entrance hall, without adding another block.</summary>
public sealed class ActivityLobbySite : ZoneSite
{
    public override string Id => "20c.activity-lobby";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(NorthFeatureIds.ActivityLobby.Entity), new Campus.Contracts.Parts.PanelParams().Depth),
            SiteRanges.Bounds(world.Feature(NorthFeatureIds.ActivityLobbyDoor), world.Feature(NorthFeatureIds.ActivityLobbyDoor).Number("canopyDepth") + new Campus.Contracts.Parts.PanelParams().Depth)];
    public override IReadOnlyList<Claim> Claims =>
        [new FeatureClaim(new(NorthFeatureIds.ActivityLobby.Entity), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
        => [new(world.Feature(NorthFeatureIds.ActivityLobby.Entity).Contour())];

    public override void Build(SiteBuilder b)
    {
        var feature = b.Context.World.Feature(NorthFeatureIds.ActivityLobby.Entity);
        var ring = feature.Contour();
        var y = b.Context.World.Terrain.PadY(feature.Id);
        var height = feature.Height!.Value;
        var wall = new Campus.Contracts.Parts.PanelParams().Depth;
        b.Add(PartGeometryKernel.Extrude(new(ring), wall, NorthTune.Look.Trim, y + height), Matrix4x4.Identity);
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var z = ring[(i + 1) % ring.Count];
            var f = PlanFrame.Toward((a + z) * .5f, z);
            var length = Vector2.Distance(a, z);
            // One registered door notch is used by both the facade and its collision.
            var door = b.Context.World.Feature(NorthFeatureIds.ActivityLobbyDoor);
            var local = f.Local(door.Point());
            var onDoorSide = MathF.Abs(local.Y) < wall && MathF.Abs(local.X) < length * .5f;
            if (onDoorSide)
            {
                var opening = door.Number("width"); var head = door.Number("height");
                foreach (var side in new[] { -1f, 1f })
                {
                    var x0 = side < 0 ? -length * .5f : local.X + opening * .5f;
                    var x1 = side < 0 ? local.X - opening * .5f : length * .5f;
                    NorthIdentityGeometry.Box(b, f, (x0 + x1) * .5f, y, 0, x1 - x0, height, wall, NorthTune.Look.Glass);
                }
                NorthIdentityGeometry.Box(b, f, local.X, y + head, 0, opening, height - head, wall, NorthTune.Look.Glass);
                NorthIdentityGeometry.Box(b, f, local.X, y + head + wall, door.Number("canopyDepth") * .5f,
                    opening + wall * 2, wall, door.Number("canopyDepth"), NorthTune.Look.Trim);
            }
            else NorthIdentityGeometry.Box(b, f, 0, y, 0, length, height, wall, NorthTune.Look.Glass);
            foreach (var x in new[] { -length * .5f, length * .5f })
                NorthIdentityGeometry.Box(b, f, x, y, 0, wall, height, wall, NorthTune.Look.Steel);
            NorthIdentityGeometry.Box(b, f, 0, y + height * .5f, 0, length, wall, wall * 2, NorthTune.Look.Steel);
        }
    }

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        var feature = world.Feature(NorthFeatureIds.ActivityLobby.Entity);
        var ring = feature.Contour();
        var y = world.Terrain.PadY(feature.Id);
        var door = world.Feature(NorthFeatureIds.ActivityLobbyDoor);
        var width = door.Number("width");
        var depth = new Campus.Contracts.Parts.PanelParams().Depth;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var z = ring[(i + 1) % ring.Count];
            var f = PlanFrame.Toward((a + z) * .5f, z); var len = Vector2.Distance(a, z);
            var at = f.Local(door.Point());
            if (MathF.Abs(at.Y) < depth && MathF.Abs(at.X) < len * .5f)
            {
                c.Wall(a, f.P(at.X - width * .5f, 0), depth * .5f, y, y + feature.Height!.Value, SurfaceTag.Wall, FootMat.Concrete);
                c.Wall(f.P(at.X + width * .5f, 0), z, depth * .5f, y, y + feature.Height!.Value, SurfaceTag.Wall, FootMat.Concrete);
            }
            else c.Wall(a, z, depth * .5f, y, y + feature.Height!.Value, SurfaceTag.Wall, FootMat.Concrete);
        }
    }
}
