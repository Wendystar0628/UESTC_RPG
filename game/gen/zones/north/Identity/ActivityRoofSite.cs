using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.North.Identity;

/// <summary>One small renewed roof pergola and planted court on the activity centre's existing roof.</summary>
public sealed class ActivityRoofSite : ZoneSite
{
    public override string Id => "20c.activity-roof";
    public override void Build(SiteBuilder b)
    {
        var record = b.Context.World.Feature(NorthFeatureIds.ActivityRoofGarden);
        var feature = b.Context.World.Feature(NorthFeatureIds.Activity.Entity);
        var y = b.Context.World.Terrain.PadY(feature.Id) + feature.Part(record.Text("sourceId")).Height!.Value;
        var f = PlanFrame.Along(record.Point(), record.Pair("direction"));
        var roof = new RoofParams(); var column = new ColumnParams(); var height = new DoorParams().Height;
        foreach (var x in new[] { -1f, 1f })
            foreach (var z in new[] { -1f, 1f })
                NorthIdentityGeometry.Box(b, f, x * roof.Width * .5f, y, z * roof.Depth * .5f,
                    column.Width, height, column.Depth, NorthTune.Look.Trim);
        foreach (var z in new[] { -1f, 1f })
            NorthIdentityGeometry.Box(b, f, 0, y + height, z * roof.Depth * .5f,
                roof.Width + column.Width, roof.Height, column.Depth, NorthTune.Look.Wood);
        var count = Math.Max(1, (int)MathF.Ceiling(roof.Width / column.Width));
        for (var i = 0; i <= count; i++)
            NorthIdentityGeometry.Box(b, f, -roof.Width * .5f + roof.Width * i / count, y + height + roof.Height,
                0, column.Width * .5f, roof.Height * .5f, roof.Depth + column.Depth, NorthTune.Look.Wood);
        var planter = new FurnitureParams();
        NorthIdentityGeometry.Box(b, f, 0, y, roof.Depth, planter.Width, planter.Height, planter.Depth, NorthTune.Look.Trim);
        NorthIdentityGeometry.Box(b, f, 0, y + planter.Height, roof.Depth, planter.Width * .8f,
            NorthTune.Deck.Shrub, planter.Depth * .8f, NorthTune.Look.Hedge);
    }
}
