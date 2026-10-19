using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.North.Identity;

/// <summary>Chengdian Hall's red drum, pale belt and recessed ribbon glazing (MAP-QSH-NORTH).</summary>
public sealed class AuditoriumSite : ZoneSite
{
    public override string Id => "20c.auditorium";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var record = world.Feature(NorthFeatureIds.DrumDesign);
        return [SiteRanges.Point(record.Point(), record.Number("diameter") * .5f + record.Number("wallThickness")),
            SiteRanges.Bounds(world.Feature(record.Text("backstage")), record.Number("wallThickness"))];
    }
    public override IReadOnlyList<Claim> Claims =>
        [new FeatureClaim(new(NorthFeatureIds.Auditorium.Entity), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod)
        => [new(world.Feature(NorthFeatureIds.Auditorium.Entity).Contour())];

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var record = world.Feature(NorthFeatureIds.DrumDesign);
        var center = record.Point();
        var radius = record.Number("diameter") * .5f;
        var height = record.Number("height");
        var y = world.Terrain.PadY(new(NorthFeatureIds.Auditorium.Entity));
        var beltY = record.Number("beltBase");
        var beltH = record.Number("beltHeight");
        var windowH = record.Number("windowHeight");
        var segments = (int)record.Number(b.Detail == SiteDetail.Mass ? "segmentsFar" : "segments");
        var wall = ZoneLooks.S(record.Text("wall"));
        var trim = ZoneLooks.S(record.Text("trim"));
        var roof = ZoneLooks.S(record.Text("roof"));
        var f = PlanFrame.Along(center, record.Pair("direction"));
        // The drum is hollow: the open auditorium's interior owns floors and seating.
        var thickness = record.Number("wallThickness");
        Ring(b, f, radius, thickness, y, beltY - windowH, segments, wall);
        var ribbon = PartShapes.Lathe(PartShapes.Loop(new(radius - thickness, 0), new(radius, 0),
            new(radius, windowH), new(radius - thickness, windowH)), segments, NorthTune.Look.Glass);
        foreach (var mesh in ribbon.Meshes)
            foreach (var rear in new[] { false, true })
            {
                var indices = new List<int>();
                for (var i = 0; i < mesh.Index.Length; i += 3)
                {
                    var x = mesh.Position[mesh.Index[i] * 3] + mesh.Position[mesh.Index[i + 1] * 3]
                        + mesh.Position[mesh.Index[i + 2] * 3];
                    if ((x > 0) == rear) indices.AddRange(mesh.Index.Skip(i).Take(3));
                }
                if (indices.Count > 0) b.Add(mesh with { Index = indices.ToArray(),
                    Material = rear ? new ToonRef(wall) : mesh.Material }, f.At(0, y + beltY - windowH, 0));
            }
        Ring(b, f, radius + thickness, thickness * 2, y + beltY, beltH, segments, trim);
        Ring(b, f, radius, thickness, y + beltY + beltH, height - beltY - beltH, segments, wall);
        b.Add(PartShapes.Lathe(PartShapes.Creased(new(0, 0), new(radius, 0),
            new(radius, thickness), new(0, thickness)), segments, roof), f.At(0, y + height - thickness, 0));
        var backstage = world.Feature(record.Text("backstage"));
        b.Add(PartGeometryKernel.Extrude(new(backstage.Outer()), backstage.Number("height"),
            ZoneLooks.S(backstage.Text("wall")), y), Matrix4x4.Identity);
    }

    private static void Ring(SiteBuilder b, PlanFrame frame, float radius, float thickness,
        float y, float height, int segments, ToonSpec material)
    {
        var profile = PartShapes.Loop(new(radius - thickness, 0), new(radius, 0),
            new(radius, height), new(radius - thickness, height));
        b.Add(PartShapes.Lathe(profile, segments, material), frame.At(0, y, 0));
    }

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        var record = world.Feature(NorthFeatureIds.DrumDesign);
        var center = record.Point();
        var radius = record.Number("diameter") * .5f;
        var height = record.Number("height");
        var y = world.Terrain.PadY(new(NorthFeatureIds.Auditorium.Entity));
        var count = (int)record.Number("segmentsFar");
        var ring = Enumerable.Range(0, count).Select(i => center + radius * new Vector2(
            (float)Math.Cos(Math.Tau * i / count), (float)Math.Sin(Math.Tau * i / count))).ToArray();
        for (var i = 0; i < count; i++)
            c.Wall(ring[i], ring[(i + 1) % count], record.Number("wallThickness"), y, y + height,
                SurfaceTag.Wall, FootMat.Concrete);
        var backstage = world.Feature(record.Text("backstage"));
        var outline = backstage.Outer();
        for (var i = 0; i < outline.Count; i++)
            c.Wall(outline[i], outline[(i + 1) % outline.Count], record.Number("wallThickness"), y,
                y + backstage.Number("height"), SurfaceTag.Wall, FootMat.Concrete);
    }
}
