using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// One indoor flight in the game/gen/interior/core/ convention: over a run of Risers × Tread the nosing line passes (0, 0) and
/// (run, rise); riser k stands at u = k·Tread, the strip u ∈ [0, Tread] is the lower level and the last riser is the edge
/// of the upper slab or landing. The flight's RampCollider is therefore exactly game/gen/interior/core/'s (WLD-INTERIORS).
/// </summary>
internal readonly record struct Flight(int Risers, float Riser, float Tread)
{
    internal float Rise => Risers * Riser;
    internal float Run => Risers * Tread;
    internal float Slope => Riser / Tread;
    /// <summary>Vertical thickness of the waist slab below the inner-corner line.</summary>
    internal float Waist => S.WaistThickness * (float)Math.Sqrt(1 + (double)Slope * Slope);
    internal float NosingY(float u) => u * Slope;
    internal float TreadY(float u) => Math.Clamp((int)Math.Floor(u / Tread + 1e-4), 0, Risers) * Riser;

    /// <summary>
    /// Side profile in flight coordinates (u up the flight, y from the lower level), with its cap triangulation. With a
    /// landing foot the lower strip is solid down to the landing soffit so the flight continues an adjoining landing.
    /// The underside is the waist slab's soffit, floored at the base. The bottom chain is split under every riser so each
    /// column under a tread is convex and fans from its top-right inner corner (the inner corners are collinear, which
    /// is exactly what makes a general ear clipper emit slivers).
    /// </summary>
    internal (List<Vector2> Ring, List<bool> Chamfer, List<int> Triangles) Profile(float bevel, bool landingFoot, float topJoint)
    {
        if (Risers < 3) throw new ArgumentOutOfRangeException(nameof(Risers), "An indoor flight has 3 to 18 risers (game/gen/interior/core/).");
        var ring = new List<Vector2>(); var chamfer = new List<bool>(); var triangles = new List<int>();
        int Add(float u, float y, bool bevelNext = false) { ring.Add(new(u, y)); chamfer.Add(bevelNext); return ring.Count - 1; }
        var n = Risers;
        var baseY = landingFoot ? -S.LandingThickness : 0;
        // The last tread runs a joint's depth into the next slab, whose edge is the top riser.
        var end = Run + topJoint;
        var soffitStart = Tread + (baseY + Waist) / Slope;
        var left = landingFoot ? -S.Joint : Tread;
        var soffit = soffitStart > left + 0.05f && soffitStart < end - 0.05f && (n - 1) * Riser - Waist > baseY + 0.02f;
        var slope = Slope; var tread = Tread; var waist = Waist;
        float Bottom(float u) => soffit ? Math.Max(baseY, (u - tread) * slope - waist) : baseY;

        // Top chain, left to right: inner[k] is the corner at ((k + 1)·T, k·R); inner[0] is the foot of riser 1.
        var topLeft = landingFoot ? Add(-S.Joint, 0) : -1;
        var inner = new int[n]; var ca = new int[n]; var cb = new int[n];
        inner[0] = Add(Tread, 0);
        for (var k = 1; k < n; k++)
        {
            var u = k * Tread; var y = k * Riser;
            if (bevel > 0) { ca[k] = Add(u, y - bevel, true); cb[k] = Add(u + bevel, y); }
            else ca[k] = cb[k] = Add(u, y);
            inner[k] = Add(k == n - 1 ? end : (k + 1) * Tread, y);
        }
        // Bottom chain, right to left: below every riser, plus the kink where the soffit meets the base.
        var below = new int[n + 1]; var kink = new int[n];
        Array.Fill(kink, -1);
        below[n] = Add(end, Bottom(end));
        for (var k = n - 1; k >= 1; k--)
        {
            var u = k * Tread; var right = k == n - 1 ? end : (k + 1) * Tread;
            if (soffit && soffitStart > u && soffitStart < right) kink[k] = Add(soffitStart, baseY);
            below[k] = k == 1 && !landingFoot ? inner[0] : Add(u, Bottom(u));
        }
        var bottomLeft = -1;
        if (landingFoot)
        {
            if (soffit && soffitStart > -S.Joint && soffitStart < Tread) kink[0] = Add(soffitStart, baseY);
            bottomLeft = Add(-S.Joint, baseY);
        }

        void Fan(int apex, List<int> rim)
        {
            for (var i = 0; i + 1 < rim.Count; i++) { triangles.Add(apex); triangles.Add(rim[i]); triangles.Add(rim[i + 1]); }
        }
        if (landingFoot)
        {
            var rim = new List<int> { topLeft, bottomLeft };
            if (kink[0] >= 0) rim.Add(kink[0]);
            rim.Add(below[1]);
            Fan(inner[0], rim);
        }
        for (var k = 1; k < n; k++)
        {
            var rim = new List<int> { cb[k] };
            if (ca[k] != cb[k]) rim.Add(ca[k]);
            if (inner[k - 1] != below[k]) rim.Add(inner[k - 1]);
            rim.Add(below[k]);
            if (kink[k] >= 0) rim.Add(kink[k]);
            rim.Add(below[k + 1]);
            Fan(inner[k], rim);
        }
        return (ring, chamfer, triangles);
    }
}

/// <summary>Placement of one flight: flight u runs along local z from z0 in direction dir (±1), y from y0, across x0..x1.</summary>
internal readonly record struct FlightLane(Flight Flight, float X0, float X1, float Z0, float Dir, float Y0)
{
    internal float Z(float u) => Z0 + Dir * u;
    internal Vector2 ZY(float u, float y) => new(Z(u), Y0 + y);

    internal void Solid(MeshBuilder mesh, ToonSpec material, Detail detail, bool landingFoot, float topJoint, Frame frame)
    {
        var bevel = detail == Detail.Full ? S.NosingBevel : 0;
        var flight = detail >= Detail.Coarse ? Merge(Flight) : Flight;
        var (ring, chamfer, triangles) = flight.Profile(bevel, landingFoot, topJoint);
        var zy = new List<Vector2>(ring.Count);
        foreach (var p in ring) zy.Add(ZY(p.X, p.Y));
        mesh.Extrude(material, zy, chamfer, X0, X1, frame, triangles);
    }

    /// <summary>Far levels merge pairs of steps; total rise and run stay (ART-MODELING "台阶").</summary>
    private static Flight Merge(Flight f)
    {
        if (f.Risers < 6) return f;
        var risers = (f.Risers + 1) / 2;
        return new(risers, f.Rise / risers, f.Run / risers);
    }

    /// <summary>
    /// Skirt over an open side face, from above the nosings down past the soffit. Its outer face is at outerX; the flight
    /// solid must stop at least <see cref="StringerCover"/> inside it so the stair profile never shows.
    /// </summary>
    internal void Stringer(MeshBuilder mesh, ToonSpec material, float outerX, float outward, Detail detail, Frame frame)
    {
        if (detail == Detail.Mass) return;
        var f = Flight;
        var cos = 1 / (float)Math.Sqrt(1 + (double)f.Slope * f.Slope);
        var depth = (f.Riser + f.Waist) * cos + 0.01f;
        // A short level run at the top ends the skirt with a vertical cut at the slab edge instead of a tilted cap.
        var level = Math.Min(0.12f, f.Tread * 0.5f);
        List<Vector2> path = [ZY(f.Tread * 0.5f, f.NosingY(f.Tread * 0.5f)), ZY(f.Run - level, f.NosingY(f.Run - level)), ZY(f.Run, f.NosingY(f.Run - level))];
        // Positive lateral lies below a path that rises toward +z; a path toward −z flips the sides.
        var (lat0, lat1) = Dir > 0 ? (-S.StringerRise, depth) : (-depth, S.StringerRise);
        var x0 = outward > 0 ? outerX - S.StringerWidth : outerX;
        mesh.VerticalSweep(material, Profiles.Rect(lat0, lat1, 0, S.StringerWidth, detail == Detail.Full ? S.PostBevel : 0), path, x0, frame);
    }

    /// <summary>How far inside a stringer's outer face the flight solid stops.</summary>
    internal const float StringerCover = 0.01f;

    /// <summary>Handrail centreline 0.9 m over the nosing line, optionally continued level past the top.</summary>
    internal List<Vector2> RailPath(float from, float to, float levelBefore = 0, float levelAfter = 0)
    {
        var f = Flight;
        var path = new List<Vector2>();
        if (levelBefore > 0) path.Add(ZY(from - levelBefore, f.NosingY(from) + S.HandrailHeight));
        path.Add(ZY(from, f.NosingY(from) + S.HandrailHeight));
        path.Add(ZY(to, f.NosingY(to) + S.HandrailHeight));
        if (levelAfter > 0) path.Add(ZY(to + levelAfter, f.NosingY(to) + S.HandrailHeight));
        if (Dir < 0) path.Reverse();
        return path;
    }

    /// <summary>One baluster per tread on the stringer line.</summary>
    internal IEnumerable<(float Z, float Foot)> BalusterStations()
    {
        var f = Flight;
        for (var k = 1; k < f.Risers; k++)
        {
            var u = (k + 0.5f) * f.Tread;
            yield return (Z(u), Y0 + f.NosingY(u) + S.StringerRise * 0.5f);
        }
    }

    /// <summary>RampCollider through every nosing, top flush with the upper level, bottom buried 0.2 m (WLD-INTERIORS).</summary>
    internal RampCollider Ramp(float x0, float x1)
    {
        var f = Flight;
        var bury = -S.RampBury / f.Slope;
        return new RampCollider(new(x0, Y0 - S.RampBury, Z(bury)), new(x1, Y0 - S.RampBury, Z(bury)),
            new(x1, Y0 + f.Rise, Z(f.Run)), new(x0, Y0 + f.Rise, Z(f.Run)), S.WaistThickness, SurfaceTag.Stair)
        { Stair = new(f.Riser, f.Tread, f.Risers) };
    }

    /// <summary>Barrier on an open side: a thin wall whose top is 1.5 m over the nosing line (SlopedWallCollider, WLD-INTERIORS).</summary>
    internal SlopedWallCollider OpenSide(float x)
    {
        var f = Flight;
        return new SlopedWallCollider(new(x, Z(0)), new(x, Z(f.Run)), S.BarrierMinHalfThickness,
            Y0 - S.RampBury, Y0 + WalkRules.BarrierHeight, Y0 + (f.Risers - 1) * f.Riser - f.Waist, Y0 + f.Rise + WalkRules.BarrierHeight, SurfaceTag.Barrier);
    }
}
