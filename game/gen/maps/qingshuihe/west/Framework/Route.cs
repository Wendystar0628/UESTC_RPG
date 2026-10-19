using Campus.Contracts;
using System.Numerics;
using Campus.Gen.Geometry;

namespace Campus.Gen.Maps.Qingshuihe.West.Framework;

/// <summary>Boardwalk design route; measurement and sampling use the shared geometry layer.</summary>
public sealed class Route(IReadOnlyList<Vector2> points)
{
    public Vector2[] Points { get; } = [.. points];
    public float Length { get; } = (float)PlanLines.Length(points);

    /// <summary>Two Chaikin corner-cutting passes per <paramref name="passes"/>, keeping both end points.</summary>
    public static Vector2[] Smooth(IReadOnlyList<Vector2> points, int passes)
    {
        var p = points.ToArray();
        for (var k = 0; k < passes; k++)
        {
            var q = new List<Vector2> { p[0] };
            for (var i = 0; i + 1 < p.Length; i++)
            {
                Vector2 a = p[i], b = p[i + 1];
                if (i > 0) q.Add(a * 0.75f + b * 0.25f);
                if (i + 2 < p.Length) q.Add(a * 0.25f + b * 0.75f);
            }
            q.Add(p[^1]);
            p = [.. q];
        }
        return p;
    }

    public (Vector2 P, Vector2 T) At(float station)
    {
        var sample = PlanLines.Sample(Points, station);
        return (sample.Point, sample.Tangent);
    }

    public static Vector2 Right(Vector2 tangent) => new(tangent.Y, -tangent.X);

    public (float S, float Lateral) Project(Vector2 point)
    {
        var projection = PlanLines.Project(Points, point);
        return ((float)projection.Station, Vector2.Dot(point - projection.Point, Right(projection.Tangent)));
    }

    public Vector2[] Sub(float from, float to) => PlanLines.Subline(Points, from, to);
}
