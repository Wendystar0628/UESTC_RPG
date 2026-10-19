using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.North.Identity;

/// <summary>Locked exterior stair, in two straight flights with a middle landing.</summary>
public sealed class ChaoyangStairSite : ZoneSite
{
    private static readonly StairOpenPart Stair = new();
    private static readonly IronGatePart Gate = new();
    public override string Id => "20c.chaoyang-stair";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var record = world.Feature(NorthFeatureIds.ChaoyangStair);
        return [SiteRanges.Bounds(record, record.Number("width") * .5f + new DoorParams().Depth),
            SiteRanges.Bounds([record.Pair("frontFrom"), record.Pair("frontTo")], new RoofParams().Depth + new ColumnParams().Width)];
    }

    public override void Build(SiteBuilder b)
    {
        var record = b.Context.World.Feature(NorthFeatureIds.ChaoyangStair);
        var (start, end) = record.PointPair();
        var length = Vector2.Distance(start, end);
        var axis = Vector2.Normalize(end - start);
        var frame = new PlanFrame(start, new(axis.Y, -axis.X));
        var y = b.Context.World.Terrain.PadY(new(NorthFeatureIds.Chunhui.Entity)) + record.Number("floorBase");
        var rise = record.Number("rise");
        var width = record.Number("width");
        var landing = record.Number("landing");
        var flightRun = (length - landing * 2) * .5f;
        var steps = (int)MathF.Ceiling(rise * .5f / CirculationStyle.RiserTarget);
        var p = new StairParams(width, rise * .5f, flightRun, steps)
            { Variation = 2, Material = NorthTune.Look.Trim, Foot = FootMat.Concrete };
        b.Add(Stair.Lods[b.Lod](p), frame.At(0, y, flightRun * .5f));
        NorthIdentityGeometry.Box(b, frame, 0, y + rise * .5f - CirculationStyle.LandingThickness,
            flightRun + landing * .5f, width, CirculationStyle.LandingThickness, landing, NorthTune.Look.Trim);
        b.Add(Stair.Lods[b.Lod](p), frame.At(0, y + rise * .5f, flightRun * 1.5f + landing));
        NorthIdentityGeometry.Box(b, frame, 0, y + rise - CirculationStyle.LandingThickness,
            length - landing * .5f, width, CirculationStyle.LandingThickness, landing, NorthTune.Look.Trim);
        var door = new DoorParams(width, WalkRules.DefaultDoorHeight, new DoorParams().Depth)
            { Material = NorthTune.Look.Steel, Foot = FootMat.Metal };
        b.Add(Gate.Lods[b.Lod](door), frame.At(0, y, 0));
        var frontA = record.Pair("frontFrom"); var frontZ = record.Pair("frontTo");
        var facade = PlanFrame.Toward((frontA + frontZ) * .5f, frontZ);
        var facadeWidth = Vector2.Distance(frontA, frontZ);
        var canopy = new RoofParams();
        var height = b.Context.World.Feature(NorthFeatureIds.Chunhui.Entity).Height!.Value;
        NorthIdentityGeometry.Box(b, facade, 0, y + height - canopy.Height,
            canopy.Depth * .5f, facadeWidth, canopy.Height, canopy.Depth,
            ZoneLooks.S("base.warmBrown", ToonFx.BevelLift));
        var fin = new PanelParams();
        var pitch = new ColumnParams().Width * 2;
        var count = Math.Max(1, (int)MathF.Ceiling(canopy.Depth / pitch));
        for (var i = 0; i <= count; i++)
            NorthIdentityGeometry.Box(b, facade, facadeWidth * .5f, y,
                canopy.Depth * i / count, fin.Depth, height - canopy.Height, new ColumnParams().Width,
                NorthTune.Look.Trim);
    }

    public override void Collide(SiteColliders c)
    {
        var record = c.Context.World.Feature(NorthFeatureIds.ChaoyangStair);
        var (start, end) = record.PointPair();
        var direction = Vector2.Normalize(end - start);
        var side = new Vector2(-direction.Y, direction.X) * record.Number("width") * .5f;
        var y = c.Context.World.Terrain.PadY(new(NorthFeatureIds.Chunhui.Entity)) + record.Number("floorBase");
        c.Wall(start - side, start + side, new DoorParams().Depth, y, y + WalkRules.BarrierHeight,
            SurfaceTag.DoorClosed, FootMat.Metal);
        // The exterior flight is visible but intentionally inaccessible from the walking network.
    }
}
