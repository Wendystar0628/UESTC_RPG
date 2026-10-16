using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings.Build;

/// <summary>
/// Generates one building at one detail level (ART-MODELING §7.8 minimum components):
/// chamfered corners with custom normals, plinth, string courses, openings (reveal with AO and sill from the
/// wall; WP-10 frames, doors and curtains with OpeningGlass glass, see FacadeOpenings), cornice or parapet with
/// the WP-12 coping and lightning strip, roof with stair-core penthouses and a fenced WP-12 plant enclosure (see
/// RoofParts), entrances (surround, canopy, the WP-11 landing with its slope or steps, see EntranceKit), pilasters,
/// downpipes and AC grilles. Surfaces that persist across
/// levels keep their normals, colours and material classes.
/// </summary>
public sealed class BuildingMesher
{
    private const float ParapetInner = -0.24f; // parapet inner face, relative to the wall plane

    private readonly BuildingPlan _p;
    private readonly LodLevel _lod;
    private readonly MeshSink _sink;
    private readonly FacadeProfile _prof;
    private readonly FacadeOpenings _openings;
    private readonly RoofParts _roofParts;
    private readonly bool _parapetOk;
    private readonly Face _wall, _trim, _plinth, _roof, _metal, _grille, _grilleBlade, _accent, _stone;

    public BuildingMesher(BuildingPlan plan, WindowBlock block, LodLevel lod, MeshSink sink)
    {
        _p = plan;
        _lod = lod;
        _sink = sink;
        _prof = plan.Profile;
        _openings = new FacadeOpenings(plan, block, lod, sink);
        _roofParts = new RoofParts(plan, lod, sink);
        _wall = new Face(MaterialKind.Wall, _prof.Wall);
        _accent = new Face(MaterialKind.Wall, _prof.Accent ?? _prof.Wall);
        _trim = new Face(MaterialKind.Trim, _prof.Trim);
        _plinth = new Face(MaterialKind.Trim, _prof.Plinth);
        _stone = new Face(MaterialKind.Trim, Families.StoneBeige);
        _roof = new Face(MaterialKind.Roof, _prof.Roof);
        _metal = new Face(MaterialKind.Metal, Families.FrameAlu, VertexFlags.NoShadow);
        _grille = new Face(MaterialKind.Grille, Families.FrameAlu);
        _grilleBlade = new Face(MaterialKind.Grille, Families.RoofGrey, VertexFlags.NoShadow);
        _parapetOk = plan.SkinRings.All(r => r.Offset(ParapetInner - 0.06f) is not null);
    }

    private bool Detailed => _lod <= LodLevel.L1;
    private bool Full => _lod == LodLevel.L0;
    private float Chamfer => Full || _lod == LodLevel.L1 ? BuildingPlan.WallChamfer : 0f;

    public void Build()
    {
        switch (_lod)
        {
            case LodLevel.L0:
            case LodLevel.L1:
                Walls();
                Openings();
                Plinth();
                StringCourses();
                RoofEdge();
                Roof();
                Penthouses();
                Entrances();
                Pilasters();
                if (Full) Downpipes();
                if (_prof.AcGrilles) Grilles();
                break;
            case LodLevel.L2:
                MassWalls(withEdge: true);
                FlatWindows();
                Penthouses();
                break;
            case LodLevel.L3:
                MassWalls(withEdge: false);
                FlatWindows();
                Penthouses();
                break;
            case LodLevel.L4:
                MassWalls(withEdge: false);
                Penthouses();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_lod), _lod, "Unknown LOD.");
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Walls (L0/L1): one grid per facade on shared rows, chamfer strips at outside corners.
    // ---------------------------------------------------------------------------------------------
    private void Walls()
    {
        var rows = _p.WallRows();
        var c = Chamfer;
        var fi = 0;
        for (var r = 0; r < _p.RingCount; r++)
        {
            var skin = _p.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++)
            {
                var frame = EdgeFrame.Of(skin, e);
                var next = (e + 1) % skin.Count;
                var uS = Prims.Chamferable(skin, e, c) ? c : 0f;
                var uE = skin.Length[e] - (Prims.Chamferable(skin, next, c) ? c : 0f);
                var openings = _p.Openings[fi];
                var us = new List<float> { uS, uE };
                foreach (var o in openings) { us.Add(o.U0); us.Add(o.U1); }
                var cols = Breaks(us, uS, uE);
                // Window columns for accent spandrels (between storeys, below the roof line).
                for (var i = 0; i + 1 < cols.Length; i++)
                {
                    var u0 = cols[i];
                    var u1 = cols[i + 1];
                    var uc = (u0 + u1) * 0.5f;
                    var accentColumn = _prof.AccentSpandrels && _prof.Accent is not null &&
                        openings.Any(o => o.Kind == BayKind.Window && o.U0 <= uc && uc <= o.U1);
                    for (var j = 0; j + 1 < rows.Length; j++)
                    {
                        var y0 = rows[j];
                        var y1 = rows[j + 1];
                        var yc = (y0 + y1) * 0.5f;
                        if (openings.Any(o => o.U0 <= uc && uc <= o.U1 && o.Y0 <= yc && yc <= o.Y1)) continue;
                        // Apron only: the panel from the floor to the sill of a window of the column, the lintel stays wall.
                        var accent = accentColumn && yc > _p.PlinthTopY && yc < _p.RoofY - 0.3f &&
                            (!_prof.AccentApron || openings.Any(o => o.Kind == BayKind.Window && o.U0 <= uc && uc <= o.U1 && _p.FloorY(o.Row) <= yc && yc <= o.Y0));
                        var face = accent ? _accent : _wall;
                        frame.Panel(_sink, face, u0, u1, 0f, y0, y1, frame.Out, new(u0, y0), new(u1, y1));
                    }
                }
            }
            // Chamfer strips: from the end of edge e-1 to the start of edge e, normals interpolate between them.
            for (var i = 0; i < skin.Count; i++)
            {
                if (!Prims.Chamferable(skin, i, c)) continue;
                var prev = (i + skin.Count - 1) % skin.Count;
                var a = skin.Points[i] - skin.Dir[prev] * c;
                var b = skin.Points[i] + skin.Dir[i] * c;
                var na = Prims.W3(skin.Normal[prev]);
                var nb = Prims.W3(skin.Normal[i]);
                for (var j = 0; j + 1 < rows.Length; j++)
                    _sink.Quad(_wall,
                        new V(Prims.W(a, rows[j]), na, new(0f, rows[j])), new V(Prims.W(b, rows[j]), nb, new(c, rows[j])),
                        new V(Prims.W(b, rows[j + 1]), nb, new(c, rows[j + 1])), new V(Prims.W(a, rows[j + 1]), na, new(0f, rows[j + 1])));
            }
        }
    }

    private static float[] Breaks(List<float> values, float min, float max)
    {
        values.Sort();
        var result = new List<float>();
        foreach (var v in values)
        {
            var x = Math.Clamp(v, min, max);
            if (result.Count == 0 || x - result[^1] > 0.01f) result.Add(x);
        }
        if (result[^1] < max - 1e-4f) result.Add(max);
        result[^1] = max;
        return [.. result];
    }

    // ---------------------------------------------------------------------------------------------
    // Openings (L0/L1): reveal (AO) and sill here; frame, door, curtain and glass from WP-10 parts.
    // ---------------------------------------------------------------------------------------------
    private void Openings()
    {
        var fi = 0;
        for (var r = 0; r < _p.RingCount; r++)
        {
            var skin = _p.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++)
            {
                var frame = EdgeFrame.Of(skin, e);
                var layout = _p.Facades[fi];
                var openings = _p.Openings[fi];
                for (var k = 0; k < openings.Length; k++) Opening(frame, layout, openings[k], _p.ZoneOf(fi, k));
            }
        }
    }

    private void Opening(EdgeFrame f, FacadeLayout layout, Opening o, FidelityZone zone)
    {
        var isDoor = o.Kind == BayKind.Door;
        Reveal(f, o.U0, o.U1, o.Y0, o.Y1, -_p.Reveal, isDoor);
        if (!isDoor) Sill(f, o.U0, o.U1, o.Y0);
        _openings.Fill(f, layout, o, zone);
    }

    private void Reveal(EdgeFrame f, float u0, float u1, float y0, float y1, float sG, bool door)
    {
        const float aoOut = 0.12f;
        const float aoIn = 0.55f;
        var a = f.Along;
        var up = Vector3.UnitY;
        // Jambs, head and bottom run from the wall plane (s = 0) to the glass plane.
        _sink.Quad(_wall, new V(f.At(u0, 0f, y0), a, new(0f, y0), aoOut), new V(f.At(u0, sG, y0), a, new(sG, y0), aoIn),
            new V(f.At(u0, sG, y1), a, new(sG, y1), aoIn), new V(f.At(u0, 0f, y1), a, new(0f, y1), aoOut));
        _sink.Quad(_wall, new V(f.At(u1, 0f, y0), -a, new(0f, y0), aoOut), new V(f.At(u1, sG, y0), -a, new(sG, y0), aoIn),
            new V(f.At(u1, sG, y1), -a, new(sG, y1), aoIn), new V(f.At(u1, 0f, y1), -a, new(0f, y1), aoOut));
        _sink.Quad(_wall, new V(f.At(u0, 0f, y1), -up, new(u0, 0f), aoOut), new V(f.At(u1, 0f, y1), -up, new(u1, 0f), aoOut),
            new V(f.At(u1, sG, y1), -up, new(u1, sG), aoIn), new V(f.At(u0, sG, y1), -up, new(u0, sG), aoIn));
        var bottom = door ? _stone : _trim;
        _sink.Quad(bottom, new V(f.At(u0, 0f, y0), up, new(u0, 0f), aoOut), new V(f.At(u1, 0f, y0), up, new(u1, 0f), aoOut),
            new V(f.At(u1, sG, y0), up, new(u1, sG), aoIn), new V(f.At(u0, sG, y0), up, new(u0, sG), aoIn));
    }

    /// <summary>Sill: 70 mm projection, 140 mm thick, chamfered front edge (L0), 70 mm ears.</summary>
    private void Sill(EdgeFrame f, float u0, float u1, float y)
    {
        if (Full)
        {
            var section = Profile.Of([new(0f, y - 0.14f), new(0.07f, y - 0.14f), new(0.07f, y - 0.016f), new(0.054f, y), new(0f, y)], 2);
            f.Bar(_sink, _trim, u0 - 0.07f, u1 + 0.07f, section);
        }
        else
        {
            f.Box(_sink, _trim, u0 - 0.07f, u1 + 0.07f, 0f, 0.07f, y - 0.14f, y);
        }
    }

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
    // corner (B7, ART-MODELING §2; the plant and mast are WP-12 parts, RoofParts).
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
            if (PlanRing.SignedArea(corners) < 0f) Array.Reverse(corners);
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
            var spot = ring.Points[i] + Vector2.Normalize(inward) * RoofRules.MastInset;
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

    // ---------------------------------------------------------------------------------------------
    // Entrances: surround, canopy, and the WP-11 landing with its steps or slope (EntranceKit) (L0/L1).
    // ---------------------------------------------------------------------------------------------
    private void Entrances()
    {
        foreach (var e in _p.Entrances) Entrance(e);
    }

    private void Entrance(PlanEntrance e)
    {
        var fl = _p.Facades[e.Facade];
        var skin = _p.SkinAt(fl.Ring);
        var f = EdgeFrame.Of(skin, fl.Edge);
        var door = _p.DoorOf(e);
        var uMin = 0.15f;
        var uMax = skin.Length[fl.Edge] - 0.15f;
        var w = 0.22f;
        // Door surround (stone), 70 mm proud of the wall.
        f.Box(_sink, _stone, door.U0 - w, door.U0, 0f, 0.07f, door.Y0, door.Y1 + w);
        f.Box(_sink, _stone, door.U1, door.U1 + w, 0f, 0.07f, door.Y0, door.Y1 + w);
        f.Box(_sink, _stone, door.U0, door.U1, 0f, 0.07f, door.Y1, door.Y1 + w);
        // A door onto a deck or porch of another package stands alone: that package roofs and floors its landing.
        if (!e.Landing) return;
        // Canopy: white aluminium edge, 0.3 m thick, ~2.2 m deep.
        var cu0 = MathF.Max(uMin, door.U0 - 1.0f);
        var cu1 = MathF.Min(uMax, door.U1 + 1.0f);
        var cy = e.CanopyY;
        var depth = _p.Small ? 1.2f : 2.2f;
        var canopy = Full
            ? Profile.Of([new(0f, cy, 0f, 0.35f), new(depth, cy), new(depth, cy + 0.28f), new(depth - 0.02f, cy + 0.3f), new(0f, cy + 0.3f)], 3)
            : Profile.Of([new(0f, cy, 0f, 0.35f), new(depth, cy), new(depth, cy + 0.3f), new(0f, cy + 0.3f)]);
        f.Bar(_sink, _trim with { Family = Families.WallWarmWhite }, cu0, cu1, canopy);
        // Landing at the threshold and the steps or slope down to the outside ground: WP-11 parts.
        EntranceKit.Build(_sink, EntranceKit.Pieces(_p, e), _lod);
    }

    // ---------------------------------------------------------------------------------------------
    // Vertical members: pilasters on piers, downpipes, AC grilles.
    // ---------------------------------------------------------------------------------------------
    private IEnumerable<(int Fi, EdgeFrame F, FacadeLayout L, PlanRing Skin)> FacadeFrames()
    {
        var fi = 0;
        for (var r = 0; r < _p.RingCount; r++)
        {
            var skin = _p.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++) yield return (fi, EdgeFrame.Of(skin, e), _p.Facades[fi], skin);
        }
    }

    private void Pilasters()
    {
        var every = _prof.PilasterEvery;
        if (every <= 0) return;
        var top = _prof.RoofEdge == Rules.RoofEdge.Cornice ? _p.WallTopY : _p.RoofY - 0.25f;
        foreach (var (fi, f, layout, skin) in FacadeFrames())
        {
            if (layout.Bays < 3 || layout.Kinds.Any(k => k == BayKind.Blank)) continue;
            var pier = layout.BayWidth - layout.Width;
            if (pier < 0.85f) continue;
            for (var m = every; m < layout.Bays; m += every)
            {
                var uc = layout.BoundarySkin(m);
                // None beside a door: not on its bay's boundaries, nor within 0.6 m of an off-centre anchor door.
                if (_p.Entrances.Any(e => e.Facade == fi && (m == e.Bay || m == e.Bay + 1 ||
                    (uc > _p.DoorOf(e).U0 - 0.6f && uc < _p.DoorOf(e).U1 + 0.6f)))) continue;
                Pilaster(f, uc - 0.28f, uc + 0.28f, _p.PlinthTopY, top);
            }
        }
    }

    private void Pilaster(EdgeFrame f, float u0, float u1, float y0, float y1)
    {
        const float d = 0.14f;
        var c = Full ? 0.03f : 0f;
        var o = f.Out;
        var a = f.Along;
        var front = (o + Vector3.Zero);
        f.Panel(_sink, _trim, u0 + c, u1 - c, d, y0, y1, front, new(u0, y0), new(u1, y1));
        // sides
        _sink.Quad(_trim, new V(f.At(u0, 0f, y0), -a, new(0f, y0)), new V(f.At(u0, d - c, y0), -a, new(d, y0)), new V(f.At(u0, d - c, y1), -a, new(d, y1)), new V(f.At(u0, 0f, y1), -a, new(0f, y1)));
        _sink.Quad(_trim, new V(f.At(u1, 0f, y0), a, new(0f, y0)), new V(f.At(u1, d - c, y0), a, new(d, y0)), new V(f.At(u1, d - c, y1), a, new(d, y1)), new V(f.At(u1, 0f, y1), a, new(0f, y1)));
        if (c > 0f)
        {
            _sink.Quad(_trim, new V(f.At(u0, d - c, y0), -a, default), new V(f.At(u0 + c, d, y0), o, default), new V(f.At(u0 + c, d, y1), o, default), new V(f.At(u0, d - c, y1), -a, default));
            _sink.Quad(_trim, new V(f.At(u1, d - c, y0), a, default), new V(f.At(u1 - c, d, y0), o, default), new V(f.At(u1 - c, d, y1), o, default), new V(f.At(u1, d - c, y1), a, default));
        }
        if (_prof.RoofEdge != Rules.RoofEdge.Cornice)
            _sink.Quad(_trim, new V(f.At(u0, 0f, y1), Vector3.UnitY, default), new V(f.At(u1, 0f, y1), Vector3.UnitY, default),
                new V(f.At(u1, d, y1), Vector3.UnitY, default), new V(f.At(u0, d, y1), Vector3.UnitY, default));
    }

    private void Downpipes()
    {
        var top = _prof.RoofEdge == Rules.RoofEdge.Cornice ? _p.WallTopY : _p.RoofY + 0.15f;
        foreach (var (fi, f, layout, skin) in FacadeFrames())
        {
            if (layout.Ring != 0 || skin.Length[layout.Edge] < 10f) continue;
            var openings = _p.Openings[fi];
            var spots = new List<float> { 0.2f };
            for (var x = 20f; x < skin.Length[layout.Edge] - 6f; x += 20f)
            {
                // nearest bay boundary, skipped when a pilaster stands there
                var m = (int)MathF.Round((x + layout.SkinShift) / layout.BayWidth);
                if (_prof.PilasterEvery > 0 && m % _prof.PilasterEvery == 0) m++;
                if (m <= 0 || m >= layout.Bays) continue;
                spots.Add(layout.BoundarySkin(m) + (_prof.AcGrilles ? 0.45f : 0f));
            }
            foreach (var u in spots)
            {
                var u0 = u - 0.065f;
                var u1 = u + 0.065f;
                if (openings.Any(o => o.U0 - 0.05f < u1 && u0 < o.U1 + 0.05f)) continue;
                f.Box(_sink, _metal, u0, u1, 0.02f, 0.15f, _p.PlinthTopY, top, bottom: true);
            }
        }
    }

    private void Grilles()
    {
        foreach (var (fi, f, layout, skin) in FacadeFrames())
        {
            var openings = _p.Openings[fi];
            var uEnd = skin.Length[layout.Edge] - 0.2f;
            foreach (var o in openings)
            {
                if (o.Kind != BayKind.Window || o.Row == 0) continue;
                var u0 = o.U1 + 0.14f;
                var u1 = u0 + 0.95f;
                if (u1 > uEnd || openings.Any(p => p.Row == o.Row && p.U0 < u1 + 0.1f && p.U1 > u0)) continue;
                var y0 = _p.FloorY(o.Row) + 0.2f;
                const float depth = 0.38f;
                f.Box(_sink, _trim, u0 - 0.03f, u1 + 0.03f, 0f, depth + 0.04f, y0 - 0.08f, y0, aoBottom: 0.3f);
                f.Box(_sink, _grille, u0, u1, 0f, depth, y0, y0 + 0.8f, bottom: false);
                if (Full)
                    for (var k = 1; k <= 3; k++)
                    {
                        // Louvre blades: read as a perforated grille rather than a closed box.
                        var yb = y0 + 0.2f * k;
                        f.Box(_sink, _grilleBlade, u0 + 0.04f, u1 - 0.04f, depth, depth + 0.025f, yb - 0.018f, yb + 0.018f);
                    }
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // L2–L4: massing with colour bands; windows as flat panes (same numbers and hashes as near).
    // ---------------------------------------------------------------------------------------------
    private void MassWalls(bool withEdge)
    {
        var o = _prof.PlinthProjection;
        var top = _p.PlinthTopY;
        ProfilePoint Z(float s, float y) => new(s, y);
        var plinth = Profile.Of([Z(o, _p.SunkY), Z(o, top), Z(0f, top)]);
        for (var r = 0; r < _p.RingCount; r++)
            Prims.Sweep(_sink, _plinth, _p.SkinAt(r), plinth);
        if (withEdge)
        {
            var yr = _p.RoofY;
            if (_prof.RoofEdge == Rules.RoofEdge.Cornice)
            {
                var proj = _p.Small ? 0.22f : 0.34f;
                var ctop = yr + (_p.Small ? 0.05f : 0.10f);
                SweepAll(_wall, Profile.Of([Z(0f, top), Z(0f, _p.WallTopY)]));
                SweepAll(_trim, Profile.Of([Z(0f, _p.WallTopY), Z(proj, _p.WallTopY), Z(proj, ctop), Z(0.12f, ctop)]));
                SweepAll(_wall, Profile.Of([Z(0.12f, ctop), Z(0.12f, _p.EdgeTopY - 0.15f)]));
                EdgeCap(0.12f);
            }
            else
            {
                SweepAll(_wall, Profile.Of([Z(0f, top), Z(0f, _p.WallTopY)]));
                EdgeCap(0f);
            }
        }
        else
        {
            SweepAll(_wall, Profile.Of([Z(0f, top), Z(0f, _p.EdgeTopY)]));
            Prims.Cap(_sink, _roof, _p.SkinOuter, _p.SkinHoles, 0f, 0f, _p.EdgeTopY, up: true);
        }
    }

    /// <summary>L2 roof edge: the same coping as near (the part's far level) and the roof slab.</summary>
    private void EdgeCap(float s0)
    {
        Coping(s0, _p.EdgeTopY - 0.15f, _p.EdgeTopY);
        Roof();
    }

    private void FlatWindows()
    {
        foreach (var (fi, f, layout, _) in FacadeFrames())
            foreach (var o in _p.Openings[fi])
                _openings.FlatGlass(f, layout, o, 0.012f);
    }
}
