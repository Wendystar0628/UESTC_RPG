using System.Numerics;
using Campus.Gen.Geometry;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings.Build;

/// <summary>Pressed-in simple window frames and bars, in one facade frame and glass plane.</summary>
internal sealed class SimpleWindowMesher(GeometrySink sink, EdgeFrame f, LodLevel lod, float sG)
{
    private readonly float c = lod == LodLevel.L0 ? OpeningRules.SimpleChamfer : 0f;
    private readonly float e = OpeningRules.SimplePressIn;
    private readonly float face = OpeningRules.SimpleFrameFace;
    private readonly float bar = OpeningRules.SimpleBarFace;

    /// <summary>
    /// Simple window of K3 and beyond: a frame ring pressed into the reveal, with continuous mullions and a transom
    /// between them (the game/gen/parts/openings/ casement light layout). Each member is a front face, a single 45° chamfer on its inner
    /// edges and returns back to the glass: at L0 24 triangles for the ring and 10 for each bar; at L1 the ring alone
    /// without chamfer (16). Nothing behind the glass. Metal of the game/gen/parts/openings/ frames, no shadow.
    /// </summary>
    public void Build(Opening o, int columns, int rows)
    {
        var sF = sG + OpeningRules.SimpleFrameProud;
        float iu0 = o.U0 + face, iu1 = o.U1 - face, iy0 = o.Y0 + face, iy1 = o.Y1 - face;
        // Ring: corner loops for the outer edge (front), the chamfer's outer edge (front) and the clear light (back).
        (float U, float Y)[] Loop(float u0, float y0, float u1, float y1) => [(u0, y0), (u1, y0), (u1, y1), (u0, y1)];
        var outer = Loop(o.U0 - e, o.Y0 - e, o.U1 + e, o.Y1 + e);
        var bevel = Loop(iu0 - c, iy0 - c, iu1 + c, iy1 + c);
        var light = Loop(iu0, iy0, iu1, iy1);
        var a = f.Along;
        var up = Vector3.UnitY;
        Vector3[] inward = [up, -a, -up, a];   // bottom, right, top, left: towards the light
        for (var k = 0; k < 4; k++)
        {
            var n = (k + 1) % 4;
            Band(outer[k], outer[n], sF, bevel[n], bevel[k], sF, f.Out);
            if (c > 0f) Band(bevel[k], bevel[n], sF, light[n], light[k], sF - c, VectorMath.Unit(f.Out + inward[k]));
            Band(light[k], light[n], sF - c, light[n], light[k], sG, inward[k]);
        }
        // Mullions run through between the ring's chamfers; transoms sit a step behind and span the whole light.
        if (lod > OpeningRules.SimpleBarsUpTo) return;
        var lightWidth = (iu1 - iu0 - (columns - 1) * bar) / columns;
        for (var k = 1; k < columns; k++)
        {
            var m0 = iu0 + k * lightWidth + (k - 1) * bar;
            Bar(m0, m0 + bar, iy0 - c, iy1 + c, sF, vertical: true);
        }
        if (rows < 2) return;
        var top = Math.Min(Math.Clamp((o.Y1 - o.Y0) * OpeningRules.TransomShare, OpeningRules.TransomMin, OpeningRules.TransomMax), (iy1 - iy0) * 0.5f);
        Bar(iu0 - c, iu1 + c, iy1 - top - bar, iy1 - top, sF - OpeningRules.SimpleTransomStep, vertical: false);
    }

    private void Band((float U, float Y) p0, (float U, float Y) p1, float s01, (float U, float Y) p2, (float U, float Y) p3, float s23, Vector3 normal) =>
        sink.Quad(SimpleMetal,
            new V(f.At(p0.U, s01, p0.Y), normal, new(p0.U, p0.Y)), new V(f.At(p1.U, s01, p1.Y), normal, new(p1.U, p1.Y)),
            new V(f.At(p2.U, s23, p2.Y), normal, new(p2.U, p2.Y)), new V(f.At(p3.U, s23, p3.Y), normal, new(p3.U, p3.Y)));

    // A bar from (u0, y0) to (u1, y1) with its front at sFront: front face, chamfers and returns along its length.
    private void Bar(float u0, float u1, float y0, float y1, float sFront, bool vertical)
    {
        var a = f.Along;
        var up = Vector3.UnitY;
        if (vertical)
        {
            Band((u0 + c, y0), (u1 - c, y0), sFront, (u1 - c, y1), (u0 + c, y1), sFront, f.Out);
            if (c > 0f)
            {
                Band((u0, y0), (u0, y1), sFront - c, (u0 + c, y1), (u0 + c, y0), sFront, VectorMath.Unit(f.Out - a));
                Band((u1 - c, y0), (u1 - c, y1), sFront, (u1, y1), (u1, y0), sFront - c, VectorMath.Unit(f.Out + a));
            }
            Band((u0, y0), (u0, y1), sFront - c, (u0, y1), (u0, y0), sG, -a);
            Band((u1, y0), (u1, y1), sFront - c, (u1, y1), (u1, y0), sG, a);
        }
        else
        {
            Band((u0, y0 + c), (u1, y0 + c), sFront, (u1, y1 - c), (u0, y1 - c), sFront, f.Out);
            if (c > 0f)
            {
                Band((u0, y0), (u1, y0), sFront - c, (u1, y0 + c), (u0, y0 + c), sFront, VectorMath.Unit(f.Out - up));
                Band((u0, y1 - c), (u1, y1 - c), sFront, (u1, y1), (u0, y1), sFront - c, VectorMath.Unit(f.Out + up));
            }
            Band((u0, y0), (u1, y0), sFront - c, (u1, y0), (u0, y0), sG, -up);
            Band((u0, y1), (u1, y1), sFront - c, (u1, y1), (u0, y1), sG, up);
        }
    }

    private static readonly Face SimpleMetal = new(MaterialKind.Metal, Families.FrameAlu, VertexFlag.NoShadow);
}
