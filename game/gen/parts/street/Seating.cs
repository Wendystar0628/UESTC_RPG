using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Street;

/// <summary>Which side of a box's top face is the sitting edge, in the box's own frame (−Z is the front, as for parts).</summary>
public enum SeatSide { Front, Back, Both }

/// <summary>
/// Contract seat declarations (<see cref="SeatDecl"/>, contract-33) from collider boxes, for the bench and for anyone whose
/// sittable top is a <see cref="BoxCollider"/>: stone tables and tree-pool rims (game/gen/maps/qingshuihe/public/, MAP-QSH-NORTH), grass seat tiers (game/gen/parts/circulation/
/// StepsAlong, one box per tier). A part returns them from <see cref="IPartDef{TParams}.Seats"/>, computed from the same
/// collider it returns from Collision(), so the seat edge and the standable top face can never drift apart (WLD-LIFE-STREET,
/// "与碰撞同源"). The sitting height is the edge's From.Y and To.Y (equal for a level box).
/// </summary>
public static class Seating
{
    /// <summary>
    /// Seat edges on the top face of <paramref name="box"/>. The box may turn about Y only (as part and step colliders do).
    /// Faces shorter than one place, or too high or low to sit on (0.3–0.8 m above its own base), yield nothing.
    /// </summary>
    public static IReadOnlyList<SeatDecl> FromBox(BoxCollider box, SeatSide side, bool back = false, float baseY = 0f)
    {
        ArgumentNullException.ThrowIfNull(box);
        var at = box.At; var h = box.Half;
        if (Math.Abs(at.M12) > 1e-4f || Math.Abs(at.M32) > 1e-4f || Math.Abs(at.M21) > 1e-4f || Math.Abs(at.M23) > 1e-4f)
            throw new ArgumentException("A seat box may only turn about Y.", nameof(box));
        var top = at.M42 + h.Y;
        if (top - baseY is < 0.3f or > 0.8f) return [];
        var length = 2 * h.X - 2 * StreetStyle.SeatEdgeMargin;
        var places = (int)Math.Floor(length / StreetStyle.SeatPitch + 1e-4f);
        if (places < 1) return [];
        var seats = new List<SeatDecl>(2);
        foreach (var sign in side switch { SeatSide.Front => new[] { -1f }, SeatSide.Back => [1f], SeatSide.Both => [-1f, 1f], _ => throw new ArgumentOutOfRangeException(nameof(side)) })
        {
            var a = Xf.Point(at, new(-length * 0.5f, h.Y, sign * h.Z));
            var b = Xf.Point(at, new(length * 0.5f, h.Y, sign * h.Z));
            var facing = Xf.Direction(at, new(0, 0, sign));
            // Seen from the sitter, From is on the left: a sitter facing −Z has −X on the left, one facing +Z has +X.
            seats.Add(sign < 0 ? new(a with { Y = top }, b with { Y = top }, facing, 2 * h.Z, back, places)
                : new(b with { Y = top }, a with { Y = top }, facing, 2 * h.Z, back, places));
        }
        return seats.AsReadOnly();
    }

    /// <summary>Sitting positions spread along the edge: the centres of <see cref="SeatDecl.Places"/> equal cells.</summary>
    public static IReadOnlyList<Vector3> Positions(SeatDecl seat)
    {
        ArgumentNullException.ThrowIfNull(seat);
        var result = new Vector3[seat.Places];
        for (var i = 0; i < seat.Places; i++)
        {
            var t = (i + 0.5f) / seat.Places;
            result[i] = new(seat.From.X + (seat.To.X - seat.From.X) * t, seat.From.Y + (seat.To.Y - seat.From.Y) * t, seat.From.Z + (seat.To.Z - seat.From.Z) * t);
        }
        return result;
    }
}
