using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.World;

namespace Campus.Gen.Buildings.Build;

public sealed partial class BuildingMesher
{
    // ---------------------------------------------------------------------------------------------
    // Plinth, string courses and roof edge: profile sweeps around the skin rings.
    // ---------------------------------------------------------------------------------------------
    private float C(float s) => s == 0f ? Chamfer : s > 0.1f ? (Chamfer > 0f ? 0.08f : 0f) : s > 0f ? (Chamfer > 0f ? 0.04f : 0f) : 0f;

    private ProfilePoint P(float s, float y, float ao = 0f) => new(s, y, C(s), ao);

    private void SweepAll(in Face face, Profile profile)
    {
        foreach (var ring in _p.SkinRings) Prims.Sweep(_sink, face, ring, profile);
    }

    private void Plinth()
    {
        var o = _prof.PlinthProjection;
        var top = _p.PlinthTopY;
        var profile = Profile.Of([P(o, _p.SunkY), P(o, top - 0.02f), P(o - 0.02f, top), P(0f, top)], 1);
        for (var r = 0; r < _p.RingCount; r++)
            Prims.Sweep(_sink, _plinth, _p.SkinAt(r), profile, DoorGaps(r), new Vector2(0f, _p.SunkY));
    }

    /// <summary>The doors on ring r that cut the plinth (threshold below its top line), as gaps in its sweep (skin u).</summary>
    private SweepGap[] DoorGaps(int ring) =>
        [.. _p.Entrances.Where(e => _p.Facades[e.Facade].Ring == ring && _p.DoorOf(e).Y0 < _p.PlinthTopY)
            .Select(e => new SweepGap(_p.Facades[e.Facade].Edge, _p.DoorOf(e).U0, _p.DoorOf(e).U1))];

    private void StringCourses()
    {
        if (!_prof.StringCourses) return;
        for (var k = 1; k < _p.Storeys; k++)
        {
            var y = _p.FloorY(k);
            SweepAll(_trim, Profile.Of([P(0f, y - 0.09f, 0.3f), P(0.05f, y - 0.09f), P(0.05f, y + 0.07f), P(0.032f, y + 0.09f), P(0f, y + 0.09f)], 2));
        }
    }

    private void RoofEdge()
    {
        var yr = _p.RoofY;
        switch (_prof.RoofEdge)
        {
            case Rules.RoofEdge.Cornice:
                {
                    var yc0 = _p.WallTopY;
                    var proj = _p.Small ? 0.22f : 0.34f;
                    var top = yr + (_p.Small ? 0.05f : 0.10f);
                    // Cornice with a drip lip at the front of the soffit.
                    SweepAll(_trim, Profile.Of([P(0f, yc0, 0.55f), P(proj - 0.04f, yc0, 0.25f), P(proj - 0.04f, yc0 - 0.04f), P(proj, yc0 - 0.04f),
                    P(proj, top - 0.02f), P(proj - 0.02f, top), P(0.12f, top)], 4));
                    var copingBase = _p.EdgeTopY - 0.15f;
                    SweepAll(_wall, Profile.Of([P(0.12f, top), P(0.12f, copingBase)]));
                    Coping(0.12f, copingBase, _p.EdgeTopY);
                    break;
                }
            case Rules.RoofEdge.Parapet:
                {
                    // Top-floor band in the trim colour, then the coping on the parapet.
                    SweepAll(_trim, Profile.Of([P(0f, yr - 0.25f, 0.3f), P(0.06f, yr - 0.25f), P(0.06f, yr + 0.18f), P(0.04f, yr + 0.2f), P(0f, yr + 0.2f)], 2));
                    Coping(0f, _p.WallTopY, _p.EdgeTopY);
                    break;
                }
            default:
                throw new InvalidOperationException("Unknown roof edge.");
        }
    }

    /// <summary>
    /// Coping on the parapet from its outer face s0 to the inner face, y0 to top: the WP-12 parapet cap (60 mm lip each
    /// side, drip under the outer one near) with the lightning strip on it, and the parapet's inner face down to the roof.
    /// Without a valid inner offset, or where the part cannot sweep a ring, WP-14's own coping profile.
    /// </summary>
    private void Coping(float s0, float y0, float top)
    {
        if (_parapetOk && _roofParts.Coping(s0, ParapetInner, y0, top, Materials.Spec(MaterialKind.Trim, _prof.Trim)))
        {
            SweepAll(_wall, Profile.Of([P(ParapetInner, y0), P(ParapetInner, _p.RoofY)]));
            _roofParts.LightningStrip(s0, ParapetInner, top);
            return;
        }
        var lip = s0 + 0.06f;
        if (_parapetOk)
        {
            SweepAll(_trim, Profile.Of([P(s0, y0), P(lip, y0), P(lip, top - 0.015f), P(lip - 0.015f, top),
                P(ParapetInner - 0.06f, top), P(ParapetInner - 0.06f, top - 0.07f), P(ParapetInner, top - 0.07f)], 2));
            SweepAll(_wall, Profile.Of([P(ParapetInner, top - 0.07f), P(ParapetInner, _p.RoofY)]));
        }
        else
        {
            SweepAll(_trim, Profile.Of([P(s0, y0), P(lip, y0), P(lip, top - 0.015f), P(lip - 0.015f, top), P(0f, top)], 2));
        }
    }

    private void Roof()
    {
        if (_parapetOk) Prims.Cap(_sink, _roof, _p.SkinOuter, _p.SkinHoles, ParapetInner, 0f, _p.RoofY, up: true);
        else Prims.Cap(_sink, _roof, _p.SkinOuter, _p.SkinHoles, 0f, Chamfer, _p.EdgeTopY, up: true);
    }

    // ---------------------------------------------------------------------------------------------
    // Roof relief: stair-core penthouses every ~30 m, one fenced plant enclosure and on some tall roofs a mast at a
    // corner (B7, ART-MODELING; the plant and mast are WP-12 parts, RoofParts).
    // ---------------------------------------------------------------------------------------------
    private void Penthouses()
    {
        // Plant stands on the slab: the recessed roof inside the parapet, or the flat top where there is none.
        var plantY = _lod >= LodLevel.L3 || !_parapetOk ? _p.EdgeTopY : _p.RoofY;
        var boxes = RoofBoxes();
        foreach (var (ring, height, plant, centre) in boxes)
        {
            var y0 = _p.RoofY - 0.05f;
            if (_lod >= LodLevel.L3 || !_parapetOk) y0 = MathF.Min(y0, _p.EdgeTopY - 0.05f);
            var top = _p.RoofY + height;
            var c = Full && !plant ? BuildingPlan.WallChamfer : 0f;
            ProfilePoint Q(float s, float y) => new(s, y, s == 0f ? c : 0f);
            if (plant)
            {
                _roofParts.Enclosure(ring, centre, plantY);
                continue;
            }
            if (_lod <= LodLevel.L1)
            {
                Prims.Sweep(_sink, _wall, ring, Profile.Of([Q(0f, y0), Q(0f, top)]));
                var ct = top + 0.16f;
                Prims.Sweep(_sink, _trim, ring, Profile.Of([Q(0f, top), Q(0.05f, top), Q(0.05f, ct - 0.015f), Q(0.035f, ct), Q(-0.22f, ct)], 2));
                Prims.Cap(_sink, _roof, ring, [], -0.22f, 0f, ct, up: true);
                // Door on the long side facing the roof centre line.
                var f = EdgeFrame.Of(ring, 0);
                var mid = ring.Length[0] * 0.5f;
                f.Box(_sink, _metal with { Flags = 0 }, mid - 0.5f, mid + 0.5f, 0f, 0.04f, _p.RoofY, _p.RoofY + 2.05f);
            }
            else
            {
                Prims.Sweep(_sink, _wall, ring, Profile.Of([Q(0f, y0), Q(0f, top + 0.16f)]));
                Prims.Cap(_sink, _roof, ring, [], 0f, 0f, top + 0.16f, up: true);
            }
        }
        if (MastSpot(boxes) is { } mast) _roofParts.Mast(mast, plantY);
    }

    /// <summary>
    /// Deterministic roof boxes inside the parapet: (ring, height above roof, is the plant enclosure, centre). Penthouses
    /// first; the enclosure takes the first candidate slot along the axis that keeps RoofRules.RoofMargin to the parapet,
    /// courtyards and penthouses (its fence line is the ring; it has no height of its own).
    /// </summary>
    private List<(PlanRing Ring, float Height, bool Plant, Vector2 Centre)> RoofBoxes()
    {
        var list = new List<(PlanRing, float, bool, Vector2)>();
        if (_p.Small || _p.Storeys < 2 || _p.Area < 120f || _p.StoreyHeight > 6.5f) return list;
        var axis = _p.Axis;
        var perp = new Vector2(-axis.Y, axis.X);
        var pts = _p.Outer.Points;
        float amin = float.MaxValue, amax = float.MinValue;
        foreach (var q in pts)
        {
            var a = Vector2.Dot(q, axis);
            amin = MathF.Min(amin, a);
            amax = MathF.Max(amax, a);
        }
        var length = amax - amin;
        var count = Math.Max(1, (int)MathF.Round(length / 30f));
        // Accepted boxes as (axis coordinate, across centre, half sizes) for the clearance test.
        var taken = new List<(float A, float B, float HalfA, float HalfB)>();
        for (var i = 0; i < count; i++) TryBox(amin + length * (i + 0.5f) / count, new Vector2(2.4f, 2.1f), false);
        if (_p.Area > RoofRules.PlantFromArea && _roofParts.EnclosureHalf() is { } half)
            foreach (var t in new[] { count >= 2 ? 1f / count : 0.22f, 0.22f, 0.78f, 0.5f })
                if (TryBox(amin + length * t, half, true)) break;
        return list;

        bool TryBox(float a, Vector2 size, bool plant)
        {
            var span = Across(a, axis, perp);
            if (span is null) return false;
            var (b0, b1) = span.Value;
            var margin = plant ? RoofRules.RoofMargin : 0.6f;
            var halfA = size.X;
            var halfB = plant ? size.Y : MathF.Min(size.Y, (b1 - b0) * 0.5f - 1.0f);
            if (plant ? (b1 - b0) * 0.5f - margin < halfB : halfB < 1.2f) return false;
            var bc = (b0 + b1) * 0.5f;
            if (plant && taken.Any(t => MathF.Abs(t.A - a) < t.HalfA + halfA + margin && MathF.Abs(t.B - bc) < t.HalfB + halfB + margin)) return false;
            Vector2 At(float da, float db) => axis * (a + da) + perp * (bc + db);
            var corners = new[] { At(-halfA, -halfB), At(halfA, -halfB), At(halfA, halfB), At(-halfA, halfB) };
            if (PlanGeometry.SignedArea(corners) < 0) Array.Reverse(corners);
            var ok = corners.Append(At(0f, 0f)).All(q =>
                PlanRing.InsideFootprint(_p.Outer, _p.Holes, q) && PlanRing.DistanceToEdges(_p.Outer, _p.Holes, q) > margin);
            if (!ok) return false;
            var height = MathF.Min(2.9f, MathF.Max(2.4f, _p.StoreyHeight - 0.4f));
            list.Add((PlanRing.FromPoints(corners, false), height, plant, At(0f, 0f)));
            taken.Add((a, bc, halfA, halfB));
            return true;
        }
    }

    /// <summary>
    /// Mast spot on a tall roof (RoofRules.MastFromStoreys, every other building by hash): inset from one outside corner
    /// of the outer ring along its bisector, clear of the parapet and of every roof box. Null when none fits.
    /// </summary>
    private Vector2? MastSpot(List<(PlanRing Ring, float Height, bool Plant, Vector2 Centre)> boxes)
    {
        if (_p.Storeys < RoofRules.MastFromStoreys || (StableHash.Hash32(_p.Seed, "wp14.mast") & 1u) != 0u) return null;
        var ring = _p.Outer;
        var corners = Enumerable.Range(0, ring.Count).Where(i => ring.Convex[i] && ring.TurnCos[i] < 0.5f).ToArray();
        if (corners.Length == 0) return null;
        var first = (int)(StableHash.Hash32(_p.Seed, "wp14.mast.corner") % (uint)corners.Length);
        for (var k = 0; k < corners.Length; k++)
        {
            var i = corners[(first + k) % corners.Length];
            var inward = -(ring.Normal[(i + ring.Count - 1) % ring.Count] + ring.Normal[i]);
            if (inward.LengthSquared() < 1e-6f) continue;
            var spot = ring.Points[i] + VectorMath.Unit(inward) * RoofRules.MastInset;
            if (!PlanRing.InsideFootprint(_p.Outer, _p.Holes, spot) || PlanRing.DistanceToEdges(_p.Outer, _p.Holes, spot) < 0.9f) continue;
            if (boxes.Any(b => PlanRing.InsideFootprint(b.Ring, [], spot) || PlanRing.DistanceToEdges(b.Ring, [], spot) < 1.0f)) continue;
            return spot;
        }
        return null;
    }

    /// <summary>Widest interval of the footprint along perp at axis coordinate a.</summary>
    private (float, float)? Across(float a, Vector2 axis, Vector2 perp)
    {
        var hits = new List<float>();
        foreach (var ring in new[] { _p.Outer }.Concat(_p.Holes))
            for (var i = 0; i < ring.Count; i++)
            {
                var p0 = ring.Points[i];
                var p1 = ring.Points[(i + 1) % ring.Count];
                var a0 = Vector2.Dot(p0, axis);
                var a1 = Vector2.Dot(p1, axis);
                if ((a0 <= a) == (a1 <= a)) continue;
                var t = (a - a0) / (a1 - a0);
                hits.Add(Vector2.Dot(p0 + (p1 - p0) * t, perp));
            }
        hits.Sort();
        (float, float)? best = null;
        for (var i = 0; i + 1 < hits.Count; i += 2)
            if (best is null || hits[i + 1] - hits[i] > best.Value.Item2 - best.Value.Item1) best = (hits[i], hits[i + 1]);
        return best;
    }


}
