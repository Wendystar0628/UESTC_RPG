using System.Numerics;
using Campus.Gen.Geometry;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Contracts;

namespace Campus.Gen.Buildings.Build;

/// <summary>Near walls, opening reveals and sills, the door-cut plinth and storey string courses.</summary>
internal sealed class BuildingFacadeMesher(BuildingPlan plan, GeometrySink sink, BuildingSurfaces surfaces,
    BuildingSkin shell, FacadeOpenings openingParts)
{
    public void Build()
    {
        Walls();
        Openings();
        Plinth();
        StringCourses();
    }

    // ---------------------------------------------------------------------------------------------
    // Walls (L0/L1): one grid per facade on shared rows, chamfer strips at outside corners.
    // ---------------------------------------------------------------------------------------------
    private void Walls()
    {
        var rows = plan.WallRows();
        var c = shell.Chamfer;
        var fi = 0;
        for (var r = 0; r < plan.RingCount; r++)
        {
            var skin = plan.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++)
            {
                var frame = EdgeFrame.Of(skin, e);
                var next = (e + 1) % skin.Count;
                var uS = Prims.Chamferable(skin, e, c) ? c : 0f;
                var uE = skin.Length[e] - (Prims.Chamferable(skin, next, c) ? c : 0f);
                var openings = plan.Openings[fi];
                var us = new List<float> { uS, uE };
                foreach (var o in openings) { us.Add(o.U0); us.Add(o.U1); }
                var cols = Breaks(us, uS, uE);
                // Window columns for accent spandrels (between storeys, below the roof line).
                for (var i = 0; i + 1 < cols.Length; i++)
                {
                    var u0 = cols[i];
                    var u1 = cols[i + 1];
                    var uc = (u0 + u1) * 0.5f;
                    var accentColumn = plan.Profile.AccentSpandrels && plan.Profile.Accent is not null &&
                        openings.Any(o => o.Kind == BayKind.Window && o.U0 <= uc && uc <= o.U1);
                    for (var j = 0; j + 1 < rows.Length; j++)
                    {
                        var y0 = rows[j];
                        var y1 = rows[j + 1];
                        var yc = (y0 + y1) * 0.5f;
                        if (openings.Any(o => o.U0 <= uc && uc <= o.U1 && o.Y0 <= yc && yc <= o.Y1)) continue;
                        // Apron only: the panel from the floor to the sill of a window of the column, the lintel stays wall.
                        var accent = accentColumn && yc > plan.PlinthTopY && yc < plan.RoofY - 0.3f &&
                            (!plan.Profile.AccentApron || openings.Any(o => o.Kind == BayKind.Window && o.U0 <= uc && uc <= o.U1 && plan.FloorY(o.Row) <= yc && yc <= o.Y0));
                        // Pattern UV: metres along the wall and up from the plinth top; the gate follows the precision zone (K4, K5 stay plain).
                        var face = (accent ? surfaces.Accent : surfaces.Wall) with { Uv2 = WallPatterns.Uv2Of(plan.WallZoneAt(fi, uc, yc < plan.GroundRowTopY)) };
                        var v0 = y0 - plan.PlinthTopY;
                        var v1 = y1 - plan.PlinthTopY;
                        frame.Panel(sink, face, u0, u1, 0f, y0, y1, frame.Out, new(u0, v0), new(u1, v1));
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
                    sink.Quad(surfaces.Wall,
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
    // Openings (L0/L1): reveal (AO) and sill here; frame, door, curtain and glass from game/gen/parts/openings/ parts.
    // ---------------------------------------------------------------------------------------------
    private void Openings()
    {
        var fi = 0;
        for (var r = 0; r < plan.RingCount; r++)
        {
            var skin = plan.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++)
            {
                var frame = EdgeFrame.Of(skin, e);
                var layout = plan.Facades[fi];
                var openings = plan.Openings[fi];
                for (var k = 0; k < openings.Length; k++) Opening(frame, layout, openings[k], plan.ZoneOf(fi, k));
            }
        }
    }

    private void Opening(EdgeFrame f, FacadeLayout layout, Opening o, FidelityZone zone)
    {
        var isDoor = o.Kind == BayKind.Door;
        Reveal(f, o.U0, o.U1, o.Y0, o.Y1, -plan.Reveal, isDoor);
        if (!isDoor) Sill(f, o.U0, o.U1, o.Y0);
        openingParts.Fill(f, layout, o, zone);
    }

    private void Reveal(EdgeFrame f, float u0, float u1, float y0, float y1, float sG, bool door)
    {
        const float aoOut = 0.12f;
        const float aoIn = 0.55f;
        var a = f.Along;
        var up = Vector3.UnitY;
        // Jambs, head and bottom run from the wall plane (s = 0) to the glass plane.
        sink.Quad(surfaces.Wall, new V(f.At(u0, 0f, y0), a, new(0f, y0), aoOut), new V(f.At(u0, sG, y0), a, new(sG, y0), aoIn),
            new V(f.At(u0, sG, y1), a, new(sG, y1), aoIn), new V(f.At(u0, 0f, y1), a, new(0f, y1), aoOut));
        sink.Quad(surfaces.Wall, new V(f.At(u1, 0f, y0), -a, new(0f, y0), aoOut), new V(f.At(u1, sG, y0), -a, new(sG, y0), aoIn),
            new V(f.At(u1, sG, y1), -a, new(sG, y1), aoIn), new V(f.At(u1, 0f, y1), -a, new(0f, y1), aoOut));
        sink.Quad(surfaces.Wall, new V(f.At(u0, 0f, y1), -up, new(u0, 0f), aoOut), new V(f.At(u1, 0f, y1), -up, new(u1, 0f), aoOut),
            new V(f.At(u1, sG, y1), -up, new(u1, sG), aoIn), new V(f.At(u0, sG, y1), -up, new(u0, sG), aoIn));
        var bottom = door ? surfaces.Stone : surfaces.Trim;
        sink.Quad(bottom, new V(f.At(u0, 0f, y0), up, new(u0, 0f), aoOut), new V(f.At(u1, 0f, y0), up, new(u1, 0f), aoOut),
            new V(f.At(u1, sG, y0), up, new(u1, sG), aoIn), new V(f.At(u0, sG, y0), up, new(u0, sG), aoIn));
    }

    /// <summary>Sill: 70 mm projection, 140 mm thick, chamfered front edge (L0), 70 mm ears.</summary>
    private void Sill(EdgeFrame f, float u0, float u1, float y)
    {
        if (shell.Full)
        {
            var section = Profile.Of([new(0f, y - 0.14f), new(0.07f, y - 0.14f), new(0.07f, y - 0.016f), new(0.054f, y), new(0f, y)], 2);
            f.Bar(sink, surfaces.Trim, u0 - 0.07f, u1 + 0.07f, section);
        }
        else
        {
            f.Box(sink, surfaces.Trim, u0 - 0.07f, u1 + 0.07f, 0f, 0.07f, y - 0.14f, y);
        }
    }

    private void Plinth()
    {
        var o = plan.Profile.PlinthProjection;
        var top = plan.PlinthTopY;
        var profile = Profile.Of([shell.Point(o, plan.SunkY), shell.Point(o, top - 0.02f), shell.Point(o - 0.02f, top), shell.Point(0f, top)], 1);
        for (var r = 0; r < plan.RingCount; r++)
            Prims.Sweep(sink, surfaces.Plinth, plan.SkinAt(r), profile, DoorGaps(r), new Vector2(0f, plan.SunkY));
    }

    /// <summary>The doors on ring r that cut the plinth (threshold below its top line), as gaps in its sweep (skin u).</summary>
    private SweepGap[] DoorGaps(int ring) =>
        [.. plan.Entrances.Where(e => plan.Facades[e.Facade].Ring == ring && plan.DoorOf(e).Y0 < plan.PlinthTopY)
            .Select(e => new SweepGap(plan.Facades[e.Facade].Edge, plan.DoorOf(e).U0, plan.DoorOf(e).U1))];

    private void StringCourses()
    {
        if (!plan.Profile.StringCourses) return;
        for (var k = 1; k < plan.Storeys; k++)
        {
            var y = plan.FloorY(k);
            shell.Sweep(surfaces.Trim, Profile.Of([shell.Point(0f, y - 0.09f, 0.3f), shell.Point(0.05f, y - 0.09f), shell.Point(0.05f, y + 0.07f), shell.Point(0.032f, y + 0.09f), shell.Point(0f, y + 0.09f)], 2));
        }
    }
}
