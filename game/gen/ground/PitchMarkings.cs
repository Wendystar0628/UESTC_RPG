using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Football pitch lines (WP-15): the same flat decal quads as the road markings, laid on the turf. The Laws-of-the-Game
/// layout of a 105 x 68 m pitch is scaled to the rectangle inside the outer line: boundary, halfway line, centre circle
/// and spot, and at both ends the penalty area, goal area, penalty spot, penalty arc and corner arcs.
/// </summary>
internal sealed partial class RoadMarkings
{
    private void AddPitches(GroundLayout layout, GroundSection section)
    {
        foreach (var shape in layout.Shapes)
        {
            // Turf is the surface of football pitches and of pitches that name no sport (GroundClassifier). Lines are drawn
            // only for a record that names its sport (Marked), and only on a plain quadrilateral of football proportions
            // (FitPitch): an untagged pitch, even a football-shaped rectangle, a ring or a small field stays plain grass.
            if (shape.Kind != SurfaceKind.Turf || !shape.Marked || SportGround.FitPitch(shape, section.Pitch) is not { } pitch) continue;
            AddPitch(layout, section, pitch);
        }
    }

    private void AddPitch(GroundLayout layout, GroundSection section, PitchFrame pitch)
    {
        var spec = section.Pitch;
        var width = spec.LineWidth;
        // The outer line lies inside the edging band that runs along the boundary of every paved surface.
        var inset = section.EdgingWidth + width * 0.5f + spec.EdgeClear;
        var hl = pitch.Length * 0.5f - inset;
        var hw = pitch.Width * 0.5f - inset;
        var su = 2f * hl / spec.RefLength;
        var sv = 2f * hw / spec.RefWidth;
        var mean = (su + sv) * 0.5f;
        var level = section.PavingY;

        Vector2 At(float u, float v) => pitch.Centre + pitch.Along * u + pitch.Across * v;

        // A straight line from local (u0, v0) to (u1, v1), lengthened by half a line width at both ends so lines that
        // meet at a corner close it.
        void Line(float u0, float v0, float u1, float v1)
        {
            var a = At(u0, v0);
            var b = At(u1, v1);
            var along = b - a;
            if (along.LengthSquared() < 1e-6f) return;
            along = Vector2.Normalize(along) * (width * 0.5f);
            Strip(layout, a - along, b + along, width, level);
        }

        // A curve through local points, as chords.
        void Curve(List<Vector2> points)
        {
            for (var k = 0; k + 1 < points.Count; k++) Strip(layout, At(points[k].X, points[k].Y), At(points[k + 1].X, points[k + 1].Y), width, level);
        }

        void Spot(float u, float v)
        {
            var half = pitch.Along * (spec.SpotSize * 0.5f);
            var at = At(u, v);
            Strip(layout, at - half, at + half, spec.SpotSize, level);
        }

        // Boundary and halfway line.
        Line(-hl, -hw, hl, -hw);
        Line(-hl, hw, hl, hw);
        Line(-hl, -hw, -hl, hw);
        Line(hl, -hw, hl, hw);
        Line(0f, -hw, 0f, hw);

        // Centre circle and kick-off spot.
        var radius = spec.CircleRadius * mean;
        Curve(Arc(0f, 0f, radius, 0f, MathF.Tau, 1f, 1f, spec.Chord));
        Spot(0f, 0f);

        foreach (var g in new[] { -1f, 1f })
        {
            // g is the end: the goal line lies at u = g * hl and u grows toward it.
            var penaltyEdge = g * (hl - spec.PenaltyDepth * su);
            var penaltyHalf = spec.PenaltyWidth * 0.5f * sv;
            Line(penaltyEdge, -penaltyHalf, penaltyEdge, penaltyHalf);
            Line(g * hl, -penaltyHalf, penaltyEdge, -penaltyHalf);
            Line(g * hl, penaltyHalf, penaltyEdge, penaltyHalf);

            var goalEdge = g * (hl - spec.GoalDepth * su);
            var goalHalf = spec.GoalWidth * 0.5f * sv;
            Line(goalEdge, -goalHalf, goalEdge, goalHalf);
            Line(g * hl, -goalHalf, goalEdge, -goalHalf);
            Line(g * hl, goalHalf, goalEdge, goalHalf);

            // Penalty spot and the part of its circle that lies outside the penalty area.
            var spotU = g * (hl - spec.SpotDistance * su);
            Spot(spotU, 0f);
            var reach = MathF.Abs(spotU - penaltyEdge) / radius;
            if (reach < 1f)
            {
                var span = DetMath.Acos(reach);
                Curve(Arc(spotU, 0f, radius, -span, span, -g, 1f, spec.Chord));
            }

            // Corner arcs.
            foreach (var h in new[] { -1f, 1f })
                Curve(Arc(g * hl, h * hw, spec.CornerRadius * mean, 0f, MathF.PI * 0.5f, -g, -h, spec.Chord));
        }
    }

    /// <summary>
    /// Points of the arc of the given radius around local (cu, cv), from angle <paramref name="from"/> to
    /// <paramref name="to"/>; (<paramref name="ku"/>, <paramref name="kv"/>) mirror the cosine and sine directions.
    /// </summary>
    private static List<Vector2> Arc(float cu, float cv, float radius, float from, float to, float ku, float kv, float chord)
    {
        var steps = Math.Max(4, (int)MathF.Ceiling(MathF.Abs(to - from) * radius / chord));
        var points = new List<Vector2>(steps + 1);
        for (var k = 0; k <= steps; k++)
        {
            var angle = from + (to - from) * k / steps;
            points.Add(new(cu + ku * radius * DetMath.Cos(angle), cv + kv * radius * DetMath.Sin(angle)));
        }
        return points;
    }

    /// <summary>One line segment, kept only if it lies wholly on visible turf.</summary>
    private void Strip(GroundLayout layout, Vector2 a, Vector2 b, float width, float level)
    {
        if (!OnTurf(layout, (a + b) * 0.5f) || !OnTurf(layout, a) || !OnTurf(layout, b)) return;
        AddStrip(a, b, width, GroundColor.Marking, level);
    }

    /// <summary>Turf is drawn unless a track, water, a carriageway or a car park lies over it.</summary>
    private static bool OnTurf(GroundLayout layout, Vector2 p) =>
        layout.SignedDistance(SurfaceKind.Turf, p, 1f) < -0.05f &&
        layout.SignedDistance(SurfaceKind.Track, p, 1f) > 0.1f &&
        layout.SignedDistance(SurfaceKind.Water, p, 1f) > 0.1f &&
        layout.SignedDistance(SurfaceKind.Carriageway, p, 1f) > 0.1f &&
        layout.SignedDistance(SurfaceKind.Parking, p, 1f) > 0.1f;
}
