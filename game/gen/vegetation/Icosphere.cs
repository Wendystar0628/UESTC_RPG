using System.Numerics;

namespace Campus.Gen.Vegetation;

/// <summary>Unit icospheres: level 0 = 20 triangles, 1 = 80, 2 = 320 (ENG-PERF §9 cloud balls). Triangles wind counter-clockwise seen from outside.</summary>
internal sealed class Icosphere
{
    private static readonly Lazy<Icosphere>[] Levels =
    [
        new(() => Create(0)),
        new(() => Create(1)),
        new(() => Create(2)),
    ];

    private Icosphere(Vector3[] vertices, int[] index)
    {
        Vertices = vertices;
        Index = index;
    }

    public Vector3[] Vertices { get; }
    public int[] Index { get; }

    public static Icosphere Get(int level) => level is >= 0 and <= 2 ? Levels[level].Value
        : throw new ArgumentOutOfRangeException(nameof(level), level, "Icosphere levels are 0–2.");

    private static Icosphere Create(int level)
    {
        var t = (1f + MathF.Sqrt(5f)) * 0.5f;
        var vertices = new List<Vector3>
        {
            new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
            new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
            new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1),
        };
        for (var i = 0; i < vertices.Count; i++) vertices[i] = Vector3.Normalize(vertices[i]);
        var faces = new List<int>
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };
        for (var l = 0; l < level; l++)
        {
            var midpoints = new Dictionary<long, int>();
            var next = new List<int>(faces.Count * 4);
            for (var f = 0; f < faces.Count; f += 3)
            {
                var a = faces[f];
                var b = faces[f + 1];
                var c = faces[f + 2];
                var ab = Midpoint(vertices, midpoints, a, b);
                var bc = Midpoint(vertices, midpoints, b, c);
                var ca = Midpoint(vertices, midpoints, c, a);
                next.AddRange([a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca]);
            }
            faces = next;
        }
        // Normalise the winding: counter-clockwise seen from outside.
        for (var f = 0; f < faces.Count; f += 3)
        {
            var a = vertices[faces[f]];
            var b = vertices[faces[f + 1]];
            var c = vertices[faces[f + 2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f) (faces[f + 1], faces[f + 2]) = (faces[f + 2], faces[f + 1]);
        }
        return new Icosphere([.. vertices], [.. faces]);
    }

    private static int Midpoint(List<Vector3> vertices, Dictionary<long, int> cache, int a, int b)
    {
        var key = ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
        if (cache.TryGetValue(key, out var index)) return index;
        vertices.Add(Vector3.Normalize((vertices[a] + vertices[b]) * 0.5f));
        cache.Add(key, vertices.Count - 1);
        return vertices.Count - 1;
    }
}
