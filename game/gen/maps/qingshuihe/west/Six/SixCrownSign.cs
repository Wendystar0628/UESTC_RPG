using System.Numerics;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>C 区东立面的院名冠标与红色徽记。</summary>
internal static class SixCrownSign
{
    private static Matrix4x4 Upright(PlanFrame f, float x, float y, float z)
    {
        var p = f.P(x, z);
        return new(f.U.X, 0, f.U.Y, 0, f.W.X, 0, f.W.Y, 0, 0, -1, 0, 0, p.X, y, p.Y, 1);
    }

    internal static void Build(SiteBuilder b, GridEdgeInfo face, float top)
    {
        var f = face.Frame;
        var x = face.Length * 0.5f;
        float hw = S.SignWidth * 0.5f, y0 = top - S.SignDrop, y1 = top + S.SignRise, z1 = S.SignProud;
        var bevel = b.Detail == SiteDetail.Full ? 0.05f : 0f;
        GridFacade.Box(b, f, x - hw, x + hw, y0, y1, 0, z1, L.Stone, bevel);
        GridFacade.Box(b, f, x - hw - 0.12f, x + hw + 0.12f, y1, y1 + 0.22f, -0.1f, z1 + 0.12f, L.Trim, bevel);
        GridFacade.Box(b, f, x - hw - 0.08f, x + hw + 0.08f, y0 - 0.25f, y0, -0.1f, z1 + 0.08f, L.Trim, bevel);
        if (b.Detail == SiteDetail.Mass) return;
        // The emblem: a thin outer ring, an octagon and circuit traces ending in round pads, single colour (楼顶红).
        var r = S.EmblemDiameter * 0.5f;
        var centre = Upright(f, x, y1 - 0.45f - r, z1);
        var d = S.EmblemDepth;
        b.Add(Shapes.Ring(Vector2.Zero, r - 0.09f, 0.18f, d, 48, L.Red), centre);
        b.Add(Shapes.Ring(Vector2.Zero, r * 0.66f, 0.15f, d, 8, L.Red), centre);
        b.Add(Shapes.Ring(Vector2.Zero, r * 0.17f, 0.12f, d, 16, L.Red), centre);
        for (var i = 0; i < 6; i++)
        {
            var a = MathF.PI / 6f + i * MathF.PI / 3f;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            var length = r * 0.38f;
            var mid = r * (0.23f + 0.19f);
            var at = new Matrix4x4(c, 0, s, 0, 0, 1, 0, 0, -s, 0, c, 0, c * mid, 0, s * mid, 1);
            b.Add(PartGeometryKernel.BeveledBox(length, d, 0.1f, 0, L.Red), GeometryPlacement.Multiply(at, centre));
            var pad = new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, c * r * 0.62f, 0, s * r * 0.62f, 1);
            b.Add(Shapes.Shaft(0.11f, 0.11f, d, 12, L.Red), GeometryPlacement.Multiply(pad, centre));
        }
        Framework.WestLettering.Write(b, "基础与前沿研究院", S.CharacterSize, L.Silver, L.Stone,
            f.At(x, y0 + 1.8f, z1 + .015f));
    }
}
