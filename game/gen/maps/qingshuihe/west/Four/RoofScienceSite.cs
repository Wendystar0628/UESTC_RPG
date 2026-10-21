using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.West.Four;

/// <summary>Authored C-wing roof equipment, using game/gen/parts/roof/'s catalogue geometry.</summary>
public sealed class RoofScienceSite : ZoneSite
{
    public override string Id => "19b.four-roof-science";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(WestIds.FourRoofScience), 2f)];

    public override void Build(SiteBuilder b)
    {
        var layout = b.Context.World.Feature(WestIds.FourRoofScience);
        var parent = b.Context.World.Feature(layout.Text("parent"));
        var height = parent.Part(layout.Text("sourceId")).Levels().Height;
        var roof = b.Context.World.Terrain.PadY(parent.Id) + height;
        var points = layout.Line();
        if (b.Includes(SiteRanges.Point(points[0], 1f))) b.Place(PartCatalog.TestTower, new EquipmentParams(2f, 6f, 2f), Matrix4x4.CreateTranslation(points[0].X, roof, points[0].Y));
        // The catalogue dish faces -Z; reverse it to face south (+Z).
        foreach (var at in points.Skip(1).Where(at => b.Includes(SiteRanges.Point(at, 1.4f))))
            b.Place(PartCatalog.Antenna, new EquipmentParams(1.4f, 2.2f, 1.4f),
                Matrix4x4.CreateRotationY(MathF.PI) * Matrix4x4.CreateTranslation(at.X, roof, at.Y));
    }
}
