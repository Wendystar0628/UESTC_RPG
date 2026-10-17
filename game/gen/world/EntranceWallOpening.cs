using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>A doorway measured along a wall, with absolute world heights.</summary>
public readonly record struct EntranceWallOpening(float Start, float End, float Bottom, float Top, bool Open);

/// <summary>Shared entrance state and wall subtraction for generated facades and collision envelopes.</summary>
public static class EntranceWallOpenings
{
    public static bool IsOpen(InteriorIndexEntry? entry, EntranceDecl? door) => entry is { Open: true } && door is { Open: true };

    public static IReadOnlyList<WallCollider> Cut(WallCollider wall, IEnumerable<EntranceWallOpening> openings,
        float startExtension = 0f, float endExtension = 0f)
    {
        var length = Vector2.Distance(wall.A, wall.B);
        if (length < 0.001f) return [];
        var direction = (wall.B - wall.A) / length;
        var cuts = openings.Select(c => c with { Start = Math.Clamp(c.Start, 0f, length), End = Math.Clamp(c.End, 0f, length) })
            .Where(c => c.End > c.Start).ToArray();
        var breaks = cuts.SelectMany(c => new[] { c.Start, c.End }).Append(0f).Append(length).Distinct().Order().ToArray();
        var result = new List<WallCollider>();
        for (var i = 0; i < breaks.Length - 1; i++)
        {
            var lo = breaks[i]; var hi = breaks[i + 1]; var midpoint = (lo + hi) * 0.5f;
            var doors = cuts.Where(c => midpoint >= c.Start && midpoint <= c.End).ToArray();
            var start = wall.A + direction * (lo == 0f ? -startExtension : lo);
            var end = wall.A + direction * (hi == length ? length + endExtension : hi);
            var heights = doors.SelectMany(d => new[] { Math.Clamp(d.Bottom, wall.Y0, wall.Y1), Math.Clamp(d.Top, wall.Y0, wall.Y1) })
                .Append(wall.Y0).Append(wall.Y1).Distinct().Order().ToArray();
            for (var j = 0; j < heights.Length - 1; j++)
            {
                var bottom = heights[j]; var top = heights[j + 1]; var middle = (bottom + top) * 0.5f;
                var atHeight = doors.Where(d => middle >= d.Bottom && middle <= d.Top).ToArray();
                if (atHeight.Any(d => !d.Open)) Add(bottom, top, SurfaceTag.DoorClosed);
                else if (!atHeight.Any(d => d.Open)) Add(bottom, top, SurfaceTag.Wall);
            }

            void Add(float y0, float y1, SurfaceTag tag)
            {
                if (y1 > y0) result.Add(wall with { A = start, B = end, Y0 = y0, Y1 = y1, Tag = tag });
            }
        }
        return result;
    }
}
