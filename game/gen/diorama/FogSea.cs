using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Diorama;

/// <summary>
/// The fog sea (WLD-DIORAMA): one opaque, double-sided ring of flat panels round the diorama, its top below the
/// campus ground, out to <see cref="FogSeaData.OuterRadius"/>. The inner edge follows the ground's edge: the cut line when
/// the map has outskirts (IOutskirtsModel.CutLine), otherwise the map's data rectangle (the extent of the ground). Along that
/// edge the surface can climb into a fog bank (<see cref="EdgeBankData"/>) whose crest wanders with a slow noise, so the
/// world's edge sinks into fog instead of ending in a line. Colour, drift streaks, deep-night glow and the fog-weather rise
/// are the shader's (UV = distance outward from the edge, rise top).
/// </summary>
public static class FogSea
{
    // Radial samples outward from the edge (as shares of the bank's outside width), then distances to the outer radius.
    private static readonly float[] BankSteps = [-1.7f, -1.35f, -1f, -0.8f, -0.6f, -0.35f, -0.15f, 0f, 0.2f, 0.45f, 0.75f, 1f];
    private const float BankFootY = -3f;          // the mound starts this far under the ground, so the fog meets the ground in a soft shoreline
    private const float BankCrestAt = -0.35f;     // crest position, share of the inside width
    private static readonly float[] FarSteps = [1.6f, 2.6f, 4.2f, 6.5f];

    public static GeometryBundle Build(FogSeaData fog, Vector2 centre, IReadOnlyList<Vector2>? cutLine, MapRectLike rect, uint seed)
    {
        var mesh = new MeshWriter(DioramaVertex.KindFogSea);
        var segments = fog.Segments;
        var bank = fog.EdgeBank;
        var noiseSeed = StableHash.Hash32(seed, "diorama.fog", "bank");
        var radii = new List<float>();
        var grid = new int[segments][];
        for (var s = 0; s < segments; s++)
        {
            var theta = s * (2f * MathF.PI / segments);
            var dir = new Vector2(DetMath.Sin(theta), -DetMath.Cos(theta));
            var edge = EdgeDistance(centre, dir, cutLine, rect);
            // Bank crest wanders along the perimeter (cells of waveLength metres, periodic round the ring).
            var perimeterCells = MathF.Max(1f, MathF.Round(2f * MathF.PI * edge / bank.WaveLength));
            var t = s / (float)segments * perimeterCells;
            var crest = bank.Height * (1f + bank.Jitter * DioramaNoise.Value(t, noiseSeed, (int)perimeterCells));
            // The fog shoreline wanders in and out across the ground's edge, so the straight edge never shows as a line.
            var wander = bank.Wander * DioramaNoise.Value(t * 0.5f, noiseSeed ^ 0x68e31da4u, (int)MathF.Max(1f, MathF.Round(perimeterCells * 0.5f)));
            radii.Clear();
            foreach (var k in BankSteps)
                radii.Add(edge + (k < 0f ? k * bank.Inside : k * bank.Outside));
            foreach (var k in FarSteps)
            {
                var r = edge + bank.Outside * k * k;
                if (r < fog.OuterRadius - 50f && r > radii[^1] + 50f) radii.Add(r);
            }
            if (radii[^1] < fog.OuterRadius) radii.Add(fog.OuterRadius);
            grid[s] = new int[radii.Count];
            for (var k = 0; k < radii.Count; k++)
            {
                var d = radii[k] - edge;
                var y = bank.Height > 0f ? Bank(d + wander, crest, fog.TopY, bank) : fog.TopY;
                var p = centre + dir * radii[k];
                grid[s][k] = mesh.Vertex(new Vector3(p.X, y, p.Y), Vector3.UnitY, new(d, fog.RiseTopY), Vector2.Zero);
            }
        }
        for (var s = 0; s < segments; s++)
        {
            var a = grid[s];
            var b = grid[(s + 1) % segments];
            var count = Math.Min(a.Length, b.Length);
            for (var k = 0; k < count - 1; k++) mesh.Quad(a[k], b[k], b[k + 1], a[k + 1]);
            // Rows differ only near the outer radius; close the longer side with a fan to its last vertex.
            for (var k = count - 1; k < a.Length - 1; k++) mesh.Triangle(a[k], b[count - 1], a[k + 1]);
            for (var k = count - 1; k < b.Length - 1; k++) mesh.Triangle(a[count - 1], b[k + 1], b[k]);
        }
        return mesh.Bundle(DioramaPalette.FogSea);
    }

    /// <summary>The bank profile across the edge: a mound rising out of the ground inside, its crest, then down to the sea top.</summary>
    private static float Bank(float d, float crest, float top, EdgeBankData bank)
    {
        var crestAt = BankCrestAt * bank.Inside;
        if (d < crestAt) return BankFootY + (crest - BankFootY) * SmoothStep(-bank.Inside, crestAt, d);
        return top + (crest - top) * (1f - SmoothStep(crestAt, bank.Outside, d));
    }

    /// <summary>Distance from the centre to the ground's edge along a direction: the cut line if given, else the rectangle.</summary>
    public static float EdgeDistance(Vector2 centre, Vector2 dir, IReadOnlyList<Vector2>? cutLine, MapRectLike rect)
    {
        if (cutLine is { Count: >= 3 })
        {
            var best = 0f;
            for (var i = 0; i < cutLine.Count; i++)
            {
                var a = cutLine[i] - centre;
                var b = cutLine[(i + 1) % cutLine.Count] - centre;
                if (Ray(dir, a, b) is { } t && t > best) best = t;
            }
            if (best > 0f) return best;
        }
        var tx = dir.X > 1e-6f ? (rect.Max.X - centre.X) / dir.X : dir.X < -1e-6f ? (rect.Min.X - centre.X) / dir.X : float.MaxValue;
        var tz = dir.Y > 1e-6f ? (rect.Max.Y - centre.Y) / dir.Y : dir.Y < -1e-6f ? (rect.Min.Y - centre.Y) / dir.Y : float.MaxValue;
        return MathF.Min(tx, tz);
    }

    private static float? Ray(Vector2 dir, Vector2 a, Vector2 b)
    {
        var e = b - a;
        var den = dir.X * e.Y - dir.Y * e.X;
        if (MathF.Abs(den) < 1e-9f) return null;
        var t = (a.X * e.Y - a.Y * e.X) / den;
        var u = (a.X * dir.Y - a.Y * dir.X) / den;
        return t > 0f && u is >= 0f and <= 1f ? t : null;
    }

    private static float SmoothStep(float a, float b, float x)
    {
        var t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}

/// <summary>Axis-aligned xz rectangle (the map's data rectangle) without depending on the maps contract types here.</summary>
public readonly record struct MapRectLike(Vector2 Min, Vector2 Max);
