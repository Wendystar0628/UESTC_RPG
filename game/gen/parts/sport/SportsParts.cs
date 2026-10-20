using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Parts.Sport.SportsStyle;

namespace Campus.Gen.Parts.Sport;

/// <summary>Sports fittings composed from the common kernel; no scene nodes or independent geometry framework.</summary>
public static class SportsParts
{
    public static PartGeometry Fence(float length, float height, bool fine, bool gate = false)
    {
        var b = new PartBuilder();
        var bays = Math.Max(1, (int)MathF.Ceiling(length / S.PostPitch));
        for (var i = 0; i <= bays; i++)
            b.AddColumn(length * i / bays, 0, height, 0, S.Post, 6, S.Green, VertexFlag.NoShadow);
        foreach (var y in new[] { S.Rail, height - S.Rail })
            b.AddPipe(new(0, y, 0), new(length, y, 0), S.Rail, 6, S.Green, VertexFlag.NoShadow);
        // Mesh gets coarser with LOD; at mass levels only the structural frame survives.
        if (fine)
        {
            var count = Math.Max(1, (int)MathF.Ceiling(length / S.WirePitch));
            for (var i = 1; i < count; i++)
                b.AddPipe(new(length * i / count, 0, 0), new(length * i / count, height, 0), S.Wire * 0.5f, 4, S.Green, VertexFlag.NoShadow);
            for (var y = S.WirePitch; y < height; y += S.WirePitch)
                b.AddPipe(new(0, y, 0), new(length, y, 0), S.Wire * 0.5f, 4, S.Green, VertexFlag.NoShadow);
        }
        if (gate)
        {
            b.AddPipe(new(length * 0.5f, 0, 0), new(length * 0.5f, height, 0), S.Rail, 6, S.Green);
            b.Add(PartSolids.BeveledBox(S.Post * 2, S.Post * 2, S.Post * 2, S.Bevel, S.Steel), Xf.Move(length * 0.5f, height * 0.45f, S.Post));
        }
        return b.Finish(new(new(-S.Post, 0, -S.Post), new(length + S.Post, height, S.Post * 3)));
    }

    public static PartGeometry Floodlight(float height, bool fine)
    {
        var b = new PartBuilder();
        b.AddColumn(0, 0, StreetStyle.LampSleeveHeight, 0, StreetStyle.LampSleeveRadius, 8, S.Steel);
        b.AddColumn(0, 0, height, 0, S.PoleRadius, fine ? 12 : 6, S.Steel);
        b.AddPipe(new(-S.Cover, height, 0), new(S.Cover, height, 0), S.Rail, 6, S.Steel);
        foreach (var x in new[] { -S.Cover, S.Cover })
        {
            b.Add(PartSolids.BeveledBox(StreetStyle.ArmHeadLength, StreetStyle.DiscHeadThickness, StreetStyle.ArmHeadWidth, S.Bevel, S.Silver), Xf.Move(x, height, 0));
            b.Add(PartSolids.BeveledBox(StreetStyle.ArmHeadLength - S.Rail, S.Wire, StreetStyle.ArmHeadWidth - S.Rail, 0, S.Glass), Xf.Move(x, height - S.Wire, 0));
        }
        var (min, max) = b.Measure();
        return b.Finish(new(min, max));
    }

    public static PartGeometry PoolLadder(bool fine)
    {
        var b = new PartBuilder();
        foreach (var x in new[] { -S.Cover * 0.5f, S.Cover * 0.5f })
        {
            b.AddPipe(new(x, -S.Cover * 2, S.Cover), new(x, S.Cover, S.Cover), S.Rail, fine ? 8 : 6, S.Steel);
            b.AddPipe(new(x, S.Cover, 0), new(x, S.Cover, S.Cover), S.Rail, 6, S.Steel);
            b.AddPipe(new(x, 0, 0), new(x, S.Cover, 0), S.Rail, 6, S.Steel);
        }
        for (var y = -S.Cover * 2; y < 0; y += S.Slab)
            b.AddPipe(new(-S.Cover * 0.5f, y, S.Cover), new(S.Cover * 0.5f, y, S.Cover), S.Rail, 6, S.Steel);
        var (min, max) = b.Measure();
        return b.Finish(new(min, max));
    }

    public static PartGeometry Seats(float length, bool fine)
    {
        var b = new PartBuilder();
        for (var row = 0; row < S.SeatRows; row++)
        {
            var y = (row + 1) * S.SeatRise;
            b.Add(PartSolids.BeveledBox(length, S.Rail * 2, S.SeatDepth, fine ? S.Bevel : 0, S.Silver), Xf.Move(0, y, row * S.SeatDepth));
            foreach (var x in new[] { -length * 0.45f, 0, length * 0.45f })
                b.AddColumn(x, 0, y, row * S.SeatDepth, S.Post, 6, S.Steel);
        }
        var back = S.SeatRows * S.SeatRise + S.CanopyBack;
        var front = S.SeatRows * S.SeatRise + S.CanopyFront;
        foreach (var x in new[] { -length * 0.45f, length * 0.45f })
            b.AddColumn(x, 0, back, S.SeatRows * S.SeatDepth, S.Post * 2, 8, S.Steel);
        // A sloping closed extrusion provides the single-plane canopy and visible edge thickness.
        Vector2[] profile = [new(-S.SeatDepth, front), new(S.SeatRows * S.SeatDepth, back),
            new(S.SeatRows * S.SeatDepth, back + S.Rail * 2), new(-S.SeatDepth, front + S.Rail * 2)];
        b.Add(PartSweeps.Sweep(new(profile), [new(-length * 0.5f, 0), new(length * 0.5f, 0)], S.Silver));
        var (min, max) = b.Measure();
        return b.Finish(new(min, max));
    }
}
