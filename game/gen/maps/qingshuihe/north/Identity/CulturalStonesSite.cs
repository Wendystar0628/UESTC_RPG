using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.North.Identity;

/// <summary>Two small uninscribed landscape markers at the existing cultural POIs.</summary>
public sealed class CulturalStonesSite : ZoneSite
{
    public override string Id => "20c.cultural-stones";
    private static readonly FurnitureParams Stone = new();

    private static Campus.Contracts.Maps.MapRect Range(IWorldModel world, string id) =>
        SiteRanges.Bounds(world.Feature(id), MathF.Max(Stone.Width, Stone.Depth) * .5f);

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [Range(world, NorthFeatureIds.Culture1.Entity), Range(world, NorthFeatureIds.Culture2.Entity)];

    public override void Build(SiteBuilder b)
    {
        foreach (var id in new[] { NorthFeatureIds.Culture1.Entity, NorthFeatureIds.Culture2.Entity })
        {
            if (!b.Includes(Range(b.Context.World, id))) continue;
            var p = b.Context.World.Feature(id).Point();
            var y = b.Ground(p);
            var frame = PlanFrame.Along(p, System.Numerics.Vector2.UnitX);
            var baseH = new RoofParams().Height;
            NorthIdentityGeometry.Box(b, frame, 0, y - baseH, 0, Stone.Width, baseH * 2, Stone.Depth, NorthTune.Look.Granite);
            b.Add(PartGeometryKernel.BeveledBox(Stone.Width * .8f, Stone.Height, Stone.Depth * .8f,
                PartStyleDefaults.WallCornerBevel, NorthTune.Look.Granite), frame.At(0, y + baseH, 0));
        }
    }

    public override void Collide(SiteColliders c)
    {
        foreach (var id in new[] { NorthFeatureIds.Culture1.Entity, NorthFeatureIds.Culture2.Entity })
        {
            if (!c.Includes(Range(c.Context.World, id))) continue;
            var p = c.Context.World.Feature(id).Point();
            var y = c.Ground(p); var baseH = new RoofParams().Height;
            var frame = PlanFrame.Along(p, System.Numerics.Vector2.UnitX);
            c.Box(frame.At(0, y + (baseH + Stone.Height) * .5f, 0),
                new(Stone.Width * .5f, (baseH + Stone.Height) * .5f, Stone.Depth * .5f), SurfaceTag.Prop, FootMat.Concrete);
        }
    }
}
