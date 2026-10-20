using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>A contour vertex with interpolated fields and the outgoing edge threshold.</summary>

internal sealed class GroundContourVertex
{
    public required double X { get; init; }
    public required double Z { get; init; }
    public required float[] Vals { get; init; }
    public int Tag { get; set; } = -1;   // threshold that created the edge leaving this vertex; -1 = cell edge
}
