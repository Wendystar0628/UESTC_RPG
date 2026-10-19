using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Sport;
using Campus.Gen.Parts.Street;
using Campus.Gen.Zones.Common;
using S = Campus.Gen.Parts.Sport.SportsStyle;

namespace Campus.Gen.Zones.CenterEast.Sports;

/// <summary>East-facing aluminium seats on the moon terrace and the equipment room's ordinary service entrance.</summary>
public sealed class SportsFurnitureSite : ZoneSite
{
    public override string Id => "21d.sports-furniture";
    private static PlanFrame Seats(IWorldModel world)
    {
        var record = world.Feature(CenterEastIds.Sports.TrackSeats);
        return PlanFrame.Along(record.Point(), record.Pair("axis"));
    }
    private static GridEdgeInfo EquipmentDoor(IWorldModel world) => GridFacade.Edges(world.Feature(CenterEastIds.Sports.Equipment.Entity).Contour())
        .OrderByDescending(e => e.Info.Length).First().Info;

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var seats = Seats(world);
        var length = world.Feature(CenterEastIds.Sports.TrackSeats).Number("length");
        b.Add(SportsParts.Seats(length, b.Detail == SiteDetail.Full), seats.At(0, b.Ground(seats.Origin), 0));
        if (b.Detail == SiteDetail.Mass) return;
        var door = EquipmentDoor(world);
        var defaults = (DoorParams)PartCatalog.Find(new(PartCatalog.DoorRoller))!.Defaults;
        var y = world.Terrain.PadY(new(CenterEastIds.Sports.Equipment.Entity));
        b.Place(PartCatalog.DoorRoller, defaults, door.Frame.AtReversed(door.Length * 0.5f, y, S.Cover));
        var canopy = (RoofParams)PartCatalog.Find(new(PartCatalog.Canopy))!.Defaults;
        canopy = canopy with { Depth = MathF.Min(canopy.Depth, S.Cover * 2) };
        b.Place(PartCatalog.Canopy, canopy, door.Frame.At(door.Length * 0.5f, y + defaults.Height + S.Slab, canopy.Depth * 0.5f));
        var atlas = new SceneTextAtlas(b.Context.GlobalInputs.Require(SceneTextInputs.AtlasKey));
        foreach (var sign in world.Features.Values.Where(f => f.Kind.Value == "sportsSign").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            var f = PlanFrame.Along(sign.Point(), sign.Pair("direction"));
            var width = sign.Number("width"); var height = sign.Number("height"); var depth = sign.Number("depth");
            var board = new PartBuilder();
            board.Add(PartGeometryKernel.BeveledBox(width, height, depth, S.Bevel, S.Silver));
            if (sign.OptionalNumber("standing") == 1) board.AddColumn(0, -sign.Number("base"), 0, 0, S.Post, 6, S.Steel);
            var run = atlas.Layout(sign.RequiredProps().Text("text"));
            Lettering.Write(board, run, b.Context.Catalog.Resolve(S.Silver.Lit), 0, height * 0.5f, -depth * 0.5f, false,
                MathF.Min(height * 0.7f, width * 0.9f / run.Width), S.Green);
            var (min, max) = board.Measure();
            b.Add(board.Finish(new(min, max)), f.At(0, b.Ground(sign.Point()) + sign.Number("base"), 0));
        }
    }

    public override void Collide(SiteColliders c)
    {
        var record = c.Context.World.Feature(CenterEastIds.Sports.TrackSeats);
        var f = Seats(c.Context.World);
        var y = c.Ground(f.Origin);
        for (var row = 0; row < S.SeatRows; row++)
            c.Box(f.At(0, y + (row + 1) * S.SeatRise + S.Rail, row * S.SeatDepth),
                new(record.Number("length") * 0.5f, S.Rail, S.SeatDepth * 0.5f), SurfaceTag.Prop, FootMat.Metal);
    }
}
