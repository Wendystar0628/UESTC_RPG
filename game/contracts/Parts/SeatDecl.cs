using System.Numerics;

namespace Campus.Contracts;

/// <summary>A declared seat edge in metres. From/To are the front edge at seat height, with From on the sitter's left.
/// Facing is the horizontal unit direction the sitter faces. Depth extends backwards from the front edge;
/// Back indicates a backrest, and Places is the number of evenly distributed seats along the edge.</summary>
/// <remarks>Coordinates are part-local when returned by IPartDef.Seats, and world coordinates when declared
/// directly in ContentOutput.Seats. The baker applies PlacedPart.At to part-local declarations exactly once;
/// world declarations are already placed. Seat declarations come from the same source as collision geometry,
/// never from render triangles. This record contains no occupancy, character handle or save identity.</remarks>
public sealed record SeatDecl(Vector3 From, Vector3 To, Vector3 Facing, float Depth, bool Back, int Places);
