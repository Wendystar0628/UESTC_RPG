using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Sport.SportsStyle;

namespace Campus.Gen.Parts.Sport;

/// <summary>Scaled court markings and regulation-height equipment; all rings and extrusions use the part kernel.</summary>
public static class CourtEquipment
{
    public static PartGeometry Surface(float length, float width, bool tennis, bool practice)
    {
        var b = new PartBuilder();
        var hl = MathF.Min(length * 0.45f, (tennis ? S.TennisLength : S.BasketballLength) * 0.5f);
        var hw = MathF.Min(width * 0.45f, (tennis ? S.TennisWidth : S.BasketballWidth) * 0.5f);
        b.Add(PartSolids.BeveledBox(hl * 2, S.Marking * 0.1f, hw * 2, 0, tennis ? S.PlayBlue : S.Court));
        void Line(float x0, float z0, float x1, float z1)
        {
            var a = new Vector2(x0, z0); var end = new Vector2(x1, z1);
            b.Add(PartSweeps.Sweep(PartSweeps.BeveledRectangleProfile(S.Marking, S.Marking * 0.1f, 0), [a, end], S.White, baseY: S.Marking * 0.12f), VertexFlag.NoShadow);
        }
        Line(-hl, -hw, hl, -hw); Line(hl, -hw, hl, hw); Line(hl, hw, -hl, hw); Line(-hl, hw, -hl, -hw);
        Line(0, -hw, 0, hw);
        if (tennis && !practice)
        {
            var single = hw * (8.23f / 10.97f); var service = MathF.Min(6.4f, hl * 0.6f);
            Line(-hl, -single, hl, -single); Line(-hl, single, hl, single);
            Line(-service, -single, -service, single); Line(service, -single, service, single); Line(-service, 0, service, 0);
        }
        if (!tennis)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var keyLength = MathF.Min(5.8f, hl * 0.5f); var keyHalf = MathF.Min(2.45f, hw * 0.5f);
                b.Add(PartSolids.BeveledBox(keyLength, S.Marking * 0.1f, keyHalf * 2, 0, S.PlayRed), Xf.Move(side * (hl - keyLength * 0.5f), S.Marking * 0.12f, 0));
                Line(side * (hl - keyLength), -keyHalf, side * (hl - keyLength), keyHalf);
                Line(side * (hl - keyLength), -keyHalf, side * hl, -keyHalf); Line(side * (hl - keyLength), keyHalf, side * hl, keyHalf);
            }
            var radius = MathF.Min(1.8f, hw * 0.3f);
            var circle = PartShapes.Arc(radius, 0, 0, 360, 32).ToArray();
            foreach (var arc in circle) Line(arc.R0, arc.Y0, arc.R1, arc.Y1);
        }
        var (min, max) = b.Measure(); return b.Finish(new(min, max));
    }

    public static PartGeometry Hoop(bool fine)
    {
        var b = new PartBuilder();
        b.AddColumn(0, 0, S.HoopHeight, 0, S.Post * 2, 8, S.Green);
        b.AddPipe(new(0, S.HoopHeight, 0), new(S.Cover, S.HoopHeight, 0), S.Post, 8, S.Steel);
        b.Add(PartSolids.BeveledBox(S.Rail * 2, S.BoardHeight, S.BoardWidth, S.Bevel, S.White),
            Xf.Move(S.Cover, S.HoopHeight, 0));
        if (fine)
        {
            var bands = PartShapes.Arc(S.HoopRadius, 0, 0, 360, 20).ToArray();
            foreach (var arc in bands)
                b.AddPipe(new(S.Cover + S.HoopRadius + arc.R0, S.HoopHeight, arc.Y0), new(S.Cover + S.HoopRadius + arc.R1, S.HoopHeight, arc.Y1), S.Wire, 4, S.PlayRed, VertexFlag.NoShadow);
        }
        var (min, max) = b.Measure(); return b.Finish(new(min, max));
    }

    public static PartGeometry Net(float width, bool fine)
    {
        var b = new PartBuilder();
        foreach (var z in new[] { -width * 0.5f, width * 0.5f }) b.AddColumn(0, 0, S.TennisPost, z, S.Post, 6, S.Green);
        b.AddPipe(new(0, S.TennisNet, -width * 0.5f), new(0, S.TennisNet, width * 0.5f), S.Wire, 4, S.White, VertexFlag.NoShadow);
        if (fine)
            for (var z = -width * 0.5f; z <= width * 0.5f; z += S.WirePitch)
                b.AddPipe(new(0, 0, z), new(0, S.TennisNet, z), S.Wire * 0.5f, 4, S.Green, VertexFlag.NoShadow);
        var (min, max) = b.Measure(); return b.Finish(new(min, max));
    }
}
