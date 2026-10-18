using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

internal sealed partial class GroundMesher
{
    // ---------------------------------------------------------------- emission

    private static float[] Mean(List<Vert> poly)
    {
        var mean = new float[TileField.Count];
        foreach (var v in poly)
            for (var f = 0; f < mean.Length; f++) mean[f] += v.Vals[f];
        for (var f = 0; f < mean.Length; f++) mean[f] /= poly.Count;
        return mean;
    }

    private void EmitPiece(List<Vert> poly)
    {
        var code = CodeOf(Mean(poly));
        var surface = Classify(code);
        if (surface != GroundSurface.Water)
        {
            var points = new Vector3[poly.Count];
            var normals = new Vector3[poly.Count];
            for (var k = 0; k < poly.Count; k++)
            {
                points[k] = Point(poly[k], surface);
                normals[k] = SurfaceNormal(surface, poly[k]);
            }
            for (var k = 0; k < poly.Count; k++) Skirt(surface, points[k], points[(k + 1) % poly.Count]);
            if (poly.Count == 3 || (_grid.IsFlat && !HasCollinear(poly)))
            {
                for (var k = 1; k + 1 < poly.Count; k++)
                    Top(surface, points[0], points[k], points[k + 1], normals[0], normals[k], normals[k + 1]);
            }
            else
            {
                // Fan from the centroid so that every outline edge is a triangle edge. A fan from a vertex drops the
                // degenerate triangles of collinear outline runs, which leaves T-junctions (pinholes) against the
                // neighbour's vertices, and on sloping ground makes vertical slivers.
                var mean = Mean(poly);
                var c = new Vert { X = poly.Average(v => v.X), Z = poly.Average(v => v.Z), Vals = mean };
                var pc = Point(c, surface);
                var nc = SurfaceNormal(surface, c);
                for (var k = 0; k < poly.Count; k++)
                {
                    var next = (k + 1) % poly.Count;
                    Top(surface, pc, points[k], points[next], nc, normals[k], normals[next]);
                }
            }
        }
        var ccw = Area(poly) > 0;
        for (var k = 0; k < poly.Count; k++)
        {
            var u = poly[k];
            if (u.Tag < 0) continue;
            var w = poly[(k + 1) % poly.Count];
            // Outward edge normal in the xz plane (positive shoelace area: interior on the left of u -> w in x-z axes).
            var ex = (float)(w.X - u.X);
            var ez = (float)(w.Z - u.Z);
            var outward = ccw ? new Vector3(ez, 0f, -ex) : new Vector3(-ez, 0f, ex);
            if (outward.LengthSquared() < 1e-14f) continue;
            Face(u, w, u.Tag, code, surface, Vector3.Normalize(outward));
        }
    }

    /// <summary>Emits the vertical face along a contour edge when this side is higher than the other.</summary>
    private void Face(Vert u, Vert w, int t, int code, GroundSurface surface, Vector3 outward)
    {
        if (surface == GroundSurface.Water) return;
        var mid = new float[TileField.Count];
        for (var f = 0; f < mid.Length; f++) mid[f] = 0.5f * (u.Vals[f] + w.Vals[f]);
        var other = (CodeOf(mid) & ~(1 << t)) | (~code & (1 << t));
        var neighbour = Classify(other);
        var hu = HeightOf(surface, u.Vals);
        var hw = HeightOf(surface, w.Vals);
        var lu = HeightOf(neighbour, u.Vals);
        var lw = HeightOf(neighbour, w.Vals);
        if (hu - lu < 1e-4f || hw - lw < 1e-4f) return;
        var field = _t[t].Field;
        // Outward from this (higher) side: up the field gradient when this side is inside (field below iso).
        var sign = (code & (1 << t)) != 0 ? 1f : -1f;
        var nu = FaceNormal(u, field, sign, outward);
        var nw = FaceNormal(w, field, sign, outward);
        // Water banks are stone revetment whatever lies above them; other steps take the upper surface's face colour.
        var color = neighbour == GroundSurface.Water ? GroundColor.Curb : GroundPalette.FaceColorOf(surface);
        var pu0 = new Vector3((float)u.X, lu, (float)u.Z);
        var pu1 = new Vector3((float)u.X, hu, (float)u.Z);
        var pw0 = new Vector3((float)w.X, lw, (float)w.Z);
        var pw1 = new Vector3((float)w.X, hw, (float)w.Z);
        _sink.Triangle(color, GroundPalette.FlagNoShadow, pu0, pw0, pw1, nu, nw, nw, outward, new(surface, GroundFaceRole.Vertical, _sourceContext));
        _sink.Triangle(color, GroundPalette.FlagNoShadow, pu0, pw1, pu1, nu, nw, nu, outward, new(surface, GroundFaceRole.Vertical, _sourceContext));
    }

    private Vector3 Point(Vert v, GroundSurface surface) =>
        new((float)v.X, HeightOf(surface, v.Vals), (float)v.Z);

    /// <summary>Height of the surface at the lattice point (i, j).</summary>
    private float LatticeHeight(GroundSurface surface, int i, int j) => HeightOf(surface, _f.V, (j * _f.S + i) * TileField.Count);

    /// <summary>
    /// Base height plus the surface's offset in the street cross-section, from the field values that start at
    /// <paramref name="offset"/> in <paramref name="v"/> (a lattice row or one vertex). A pit's frame and soil take their
    /// heights from the pit's own values; the chamfers slope 45 degrees over the bevel width from the frame top down to
    /// its edge, outside and inside.
    /// </summary>
    private float HeightOf(GroundSurface surface, float[] v, int offset = 0)
    {
        var h = v[offset + TileField.H];
        var bevel = _s.CurbBevel;
        return h + surface switch
        {
            GroundSurface.Water => _s.BankBottomY,
            GroundSurface.Asphalt or GroundSurface.Parking => _s.RoadY,
            GroundSurface.Bevel => _s.PavingY - bevel + Math.Clamp(v[offset + TileField.A], 0f, bevel),
            GroundSurface.Curb or GroundSurface.Edging or GroundSurface.Track or GroundSurface.Turf or GroundSurface.Court or
                GroundSurface.Granite or GroundSurface.PlazaBorder or GroundSurface.Paver => _pavingY,
            GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark => _grassY,
            GroundSurface.PitRim => v[offset + TileField.PitRim],
            GroundSurface.PitSoil => v[offset + TileField.PitSoil],
            GroundSurface.PitBevel => v[offset + TileField.PitRim] - bevel +
                MathF.Min(Math.Clamp(-v[offset + TileField.Pit], 0f, bevel), Math.Clamp(v[offset + TileField.Core], 0f, bevel)),
            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
        };
    }

    private Vector3 SurfaceNormal(GroundSurface surface, Vert v) => surface switch
    {
        GroundSurface.Bevel => BevelNormal(v),
        GroundSurface.PitBevel => PitBevelNormal(v),
        _ => Up
    };

    /// <summary>Normal of a pit frame's chamfer: tilted outward on the outer top edge, toward the soil on the inner one.</summary>
    private Vector3 PitBevelNormal(Vert v)
    {
        var g = Gradient(v, TileField.Pit);
        if (g == Vector2.Zero) return Up;
        var inner = v.Vals[TileField.Core] < -v.Vals[TileField.Pit];
        var sign = inner ? -1f : 1f;
        return Vector3.Normalize(Up + new Vector3(sign * g.X, 0f, sign * g.Y));
    }

    private Vector3 BevelNormal(Vert v)
    {
        var g = Gradient(v, TileField.A);
        if (g == Vector2.Zero) return Up;
        return Vector3.Normalize(Up + new Vector3(-g.X, 0f, -g.Y));
    }

    /// <summary>Smooth face normal from the field gradient; falls back to the edge normal where the gradient disagrees.</summary>
    private Vector3 FaceNormal(Vert v, int field, float sign, Vector3 outward)
    {
        var g = Gradient(v, field) * sign;
        var n = new Vector3(g.X, 0f, g.Y);
        return Vector3.Dot(n, outward) > 0.5f ? n : outward;
    }

    /// <summary>Unit horizontal gradient of a field at the vertex, from exact point queries (seam consistent).</summary>
    private Vector2 Gradient(Vert v, int field)
    {
        var x = (float)v.X;
        var z = (float)v.Z;
        if (_gradients.TryGetValue((x, z, field), out var cached)) return cached;
        const float h = 0.02f;
        var limit = _f.Limit;
        var p = new Vector2(x, z);
        var shapes = _layout.Near(p - new Vector2(limit + h), p + new Vector2(limit + h));
        var gx = Value(field, shapes, new(x + h, z), limit) - Value(field, shapes, new(x - h, z), limit);
        var gz = Value(field, shapes, new(x, z + h), limit) - Value(field, shapes, new(x, z - h), limit);
        var g = new Vector2(gx, gz);
        g = g.LengthSquared() > 1e-12f ? Vector2.Normalize(g) : Vector2.Zero;
        _gradients.Add((x, z, field), g);
        return g;
    }

    private static float Value(int field, List<GroundShape> shapes, Vector2 p, float limit)
    {
        float Of(SurfaceKind kind)
        {
            var best = limit;
            foreach (var shape in shapes)
                if (shape.Kind == kind) best = MathF.Min(best, Sdf.Shape(shape, p, limit));
            return best;
        }
        return field switch
        {
            TileField.W => Of(SurfaceKind.Water),
            TileField.A => MathF.Min(Of(SurfaceKind.Carriageway), Of(SurfaceKind.Parking)),
            TileField.Car => Of(SurfaceKind.Carriageway),
            TileField.P => MathF.Min(MathF.Min(MathF.Min(Of(SurfaceKind.Track), Of(SurfaceKind.Turf)),
                MathF.Min(Of(SurfaceKind.Court), Of(SurfaceKind.Plaza))), Of(SurfaceKind.Walk)),
            TileField.Track => Of(SurfaceKind.Track),
            TileField.Turf => Of(SurfaceKind.Turf),
            TileField.Court => Of(SurfaceKind.Court),
            TileField.Plaza => Of(SurfaceKind.Plaza),
            // Core is Pit plus the frame thickness of the nearest pit: the same gradient.
            TileField.Pit or TileField.Core => Of(SurfaceKind.Pit),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown field.")
        };
    }

    // ---------------------------------------------------------------- classification

    private int CodeAt(float[] values, int offset)
    {
        var code = 0;
        for (var t = 0; t < _t.Length; t++)
            if ((_enabled & (1 << t)) != 0 && values[offset + _t[t].Field] < _t[t].Iso) code |= 1 << t;
        return code;
    }

    private int CodeOf(float[] values) => CodeAt(values, 0);

    /// <summary>Priority: water, carriageway level, kerb bevel and kerb, paved surfaces (edging band first), lawn.</summary>
    public GroundSurface Classify(int code)
    {
        bool Has(int t) => (code & (1 << t)) != 0;
        if (Has(TWater)) return GroundSurface.Water;
        if (Has(TRoad)) return Has(TCar) ? GroundSurface.Asphalt : GroundSurface.Parking;
        if (_bevel && Has(TBevel)) return GroundSurface.Bevel;
        if (_bands && Has(TCurb)) return GroundSurface.Curb;
        if (_pits && Has(TPit))
        {
            // A tree pit or planting strip is carved out of whatever lies round it: nothing is paved or sown inside it.
            // Inside the outer face: a chamfer on the frame's outer top edge, the flat frame top, a chamfer on its inner
            // top edge, then the sunken soil.
            if (Has(TPitCore)) return GroundSurface.PitSoil;
            if (_bevel && (Has(TPitCoreBevel) || !Has(TPitTop))) return GroundSurface.PitBevel;
            return GroundSurface.PitRim;
        }
        if (!Has(TPaved))
        {
            if (Has(TLawnEdge) || Has(TLawnLow)) return GroundSurface.GrassDark;
            if ((_enabled & (1 << TLawnHigh)) == 0) return GroundSurface.Grass;
            return Has(TLawnHigh) ? GroundSurface.Grass : GroundSurface.GrassLight;
        }
        if (_bands && !Has(TEdge)) return GroundSurface.Edging;
        if (Has(TTrack)) return GroundSurface.Track;
        if (Has(TTurf)) return GroundSurface.Turf;
        if (Has(TCourt)) return GroundSurface.Court;
        if (!Has(TPlaza)) return GroundSurface.Paver;
        return _bands && !Has(TPlazaBorder) ? GroundSurface.PlazaBorder : GroundSurface.Granite;
    }

    /// <summary>Surface at a lattice point (used by the footstep and mask grids).</summary>
    public GroundSurface SurfaceAt(int i, int j) => Classify(_code[j * _f.S + i]);

    public float HeightAt(int i, int j) => LatticeHeight(SurfaceAt(i, j), i, j);
}
