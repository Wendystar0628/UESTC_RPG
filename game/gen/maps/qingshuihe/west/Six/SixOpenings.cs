using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Sites;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixLayout;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixConnections;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的桥口与底座开口表；玻璃、饰条、碰撞和室内读取同一布局。</summary>
internal static class SixOpenings
{
    internal const float BridgeHalfWidth = 1.7f;
    internal sealed record BaseOpening(float From, float To, float Bottom, float Top);

    internal static IEnumerable<(string Entity, Vector2 Centre, PlanFrame Face)> BridgeDoors(IWorldModel world)
    {
        var bridges = Bridges(world).ToArray();
        for (var i = 0; i < WestIds.SixBridges.Count; i++)
        {
            var target = WestIds.SixBridges[i].End;
            var line = world.ContourSegment(target);
            var frame = PlanFrame.Toward(line[^1], line[0]);
            yield return (world.Feature(target).Text("parent"), bridges[i].To, frame);
        }
    }

    internal static IReadOnlyList<(float From, float To)> LinkOpenings(IWorldModel world, GridEdgeInfo edge) =>
        Bridges(world).Select(b => edge.Frame.Local(b.From))
            .Where(p => MathF.Abs(p.Y) < 0.02f && p.X > 0 && p.X < edge.Length)
            .Select(p => (MathF.Max(0, p.X - BridgeHalfWidth), MathF.Min(edge.Length, p.X + BridgeHalfWidth))).ToArray();

    internal static IReadOnlyList<BaseOpening> BaseOpenings(IWorldModel world, string entity, GridEdgeInfo edge)
    {
        var result = new List<BaseOpening>();
        void Add(Vector2 p, float width, float bottom, float height)
        {
            var local = edge.Frame.Local(p);
            if (MathF.Abs(local.Y) > 0.02f || local.X <= 0 || local.X >= edge.Length) return;
            result.Add(new(MathF.Max(0, local.X - width * 0.5f), MathF.Min(edge.Length, local.X + width * 0.5f), bottom, bottom + height));
        }
        if (entity == S.EntityC)
        {
            var (face, x) = EastFace(world, EastDoor(world));
            Add(face.Frame.P(x, -S.Recess), S.VestibuleWidth, 0, S.VestibuleHeight);
        }
        foreach (var door in BridgeDoors(world).Where(d => d.Entity == entity))
            Add(door.Centre, S.DoorWidth, S.LinkDeck, S.DoorHeight);
        return result;
    }
}
