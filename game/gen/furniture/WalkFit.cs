using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Furniture;

/// <summary>
/// A small entity standing at a walk's edge, facing the walk, with the bay that joins it to the walk (or on a plaza, on the paving
/// itself: no bay where the paving is level enough), and the height it stands at.
/// </summary>
internal readonly record struct WalkFit(Vector2 Centre, Vector2 Facing, (Vector2 A, Vector2 B, float R) Ends, Pad? Bay, float Top);
