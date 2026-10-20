using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Water;

/// <summary>A nonzero water-ring edge with its scan-line interval and exact row intersections.</summary>
internal readonly record struct WaterEdge(Vector2 A, Vector2 B)
{
    public float ZMin => MathF.Min(A.Y, B.Y);
    public float ZMax => MathF.Max(A.Y, B.Y);
    public bool Spans(float z) => (A.Y <= z && z < B.Y) || (B.Y <= z && z < A.Y);
    public float XAt(float z)
    {
        if (z == A.Y) return A.X;
        if (z == B.Y) return B.X;
        return A.X + (z - A.Y) * (B.X - A.X) / (B.Y - A.Y);
    }
}

internal static class WaterEdges
{
    internal static List<WaterEdge> Edges(WaterBody body)
    {
        var edges = new List<WaterEdge>();
        AddRing(body.Rings.Outer);
        foreach (var hole in body.Rings.Holes) AddRing(hole);
        return edges;

        void AddRing(IReadOnlyList<Vector2> ring)
        {
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
                if (ring[i] != ring[j]) edges.Add(new(ring[j], ring[i]));
        }
    }

}
