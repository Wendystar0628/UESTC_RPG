using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Sport.SportsStyle;

namespace Campus.Gen.Parts.Sport;

/// <summary>Barrel roof and glazed bow ends use the circulation package's single circular section sampler.</summary>
public static class TrainingRoof
{
    public static PartGeometry Build(float length, float width, bool fine)
    {
        var b = new PartBuilder();
        var arc = ArcSection.Sample(width, S.Rise, fine ? 48 : 24);
        var path = new Vector2[] { new(-length * 0.5f, 0), new(length * 0.5f, 0) };
        for (var i = 0; i + 1 < arc.Count; i++)
        {
            Vector2 p = arc[i], q = arc[i + 1];
            Vector2[] strip = [p, q, q + new Vector2(0, S.Rail * 2), p + new Vector2(0, S.Rail * 2)];
            var tp = q - arc[Math.Max(0, i - 1)]; var tq = arc[Math.Min(arc.Count - 1, i + 2)] - p;
            var np = VectorMath.Unit(new Vector2(-tp.Y, tp.X)); var nq = VectorMath.Unit(new Vector2(-tq.Y, tq.X));
            var glazing = MathF.Abs((p.X + q.X) * 0.5f) <= S.SkylightWidth * 0.5f;
            b.Add(PartGeometryKernel.Sweep(new(strip, [-np, -nq, nq, np]), path, glazing ? S.Glass : S.Silver, baseY: S.Spring));
        }
        // End bows are closed glazed extrusions above the annex roof, with silver vertical ribs.
        var bow = arc.Reverse().Select(p => new Vector2(p.X, p.Y + S.Spring)).ToArray();
        foreach (var x in new[] { -length * 0.5f, length * 0.5f })
        {
            b.Add(PartGeometryKernel.Sweep(new(bow), [new(x - S.Rail, 0), new(x + S.Rail, 0)], S.Glass));
            if (!fine) continue;
            for (var i = 1; i + 1 < arc.Count; i += 2)
                b.AddColumn(x, S.Spring, S.Spring + arc[i].Y, -arc[i].X, S.Rail, 6, S.Silver, VertexFlag.NoShadow);
        }
        if (fine)
            for (var x = -length * 0.5f; x <= length * 0.5f; x += S.PostPitch)
                for (var i = 0; i + 1 < arc.Count; i++)
                    b.AddPipe(new(x, S.Spring + arc[i].Y, -arc[i].X), new(x, S.Spring + arc[i + 1].Y, -arc[i + 1].X), S.Wire, 4, S.Silver, VertexFlag.NoShadow);
        var (min, max) = b.Measure();
        return b.Finish(new(min, max));
    }
}
