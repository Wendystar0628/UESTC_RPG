using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>Owns the LOD street section, contour thresholds and surface/height interpretation.</summary>
internal sealed class GroundSurfaceProfile
{
    internal readonly record struct Threshold(int Field, float Iso)
    {
        // A building owns its boundary; its zero-valued corner samples must not classify an interior triangle as lawn.
        public bool Contains(float value) => Field == TileField.Building ? value <= Iso : value < Iso;
    }

    private const int TWater = 0, TRoad = 1, TBevel = 2, TCurb = 3, TCar = 4, TPaved = 5, TEdge = 6,
        TTrack = 7, TTurf = 8, TCourt = 9, TPlaza = 10, TPlazaBorder = 11, TLawnEdge = 12, TLawnLow = 13, TLawnHigh = 14,
        TPit = 15, TPitTop = 16, TPitCoreBevel = 17, TPitCore = 18, TBuilding = 19;

    private readonly TileField _f;

    private readonly GroundSection _s;

    private readonly bool _bevel;

    private readonly bool _bands;

    private readonly bool _pits;

    private readonly Threshold[] _t;

    private readonly int _enabled;

    private readonly float _grassY;

    private readonly float _pavingY;

    public GroundSurfaceProfile(TileField field, GroundSection section, LodLevel lod)
    {
        _f = field;
        _s = section;
        _bevel = GroundLod.HasBevel(lod);
        // Steps per level: L0 full cross-section, L1 lawns flush with paving, L2 and coarser one flat surface.
        var steps = GroundLod.Steps(lod);
        _pavingY = steps == 0 ? section.RoadY : section.PavingY;
        _grassY = steps == 2 ? section.GrassY : _pavingY;
        _bands = GroundLod.HasBands(lod);
        _pits = _bands;   // the frame and the soil are resolved where kerbs are (L0, L1); beyond that a pit is under a pixel
        _t =
        [
            new(TileField.W, 0f),
            new(TileField.A, 0f),
            new(TileField.A, _bevel ? section.CurbBevel : 0f),
            new(TileField.A, _bands ? section.CurbWidth : 0f),
            new(TileField.Car, 0f),
            new(TileField.P, 0f),
            new(TileField.P, _bands ? -section.EdgingWidth : 0f),
            new(TileField.Track, 0f),
            new(TileField.Turf, 0f),
            new(TileField.Court, 0f),
            new(TileField.Plaza, 0f),
            new(TileField.Plaza, _bands ? -section.PlazaBorder : 0f),
            new(TileField.LawnEdge, section.Lawn.EdgeBand),
            new(TileField.Tone, section.Lawn.Low),
            new(TileField.Tone, section.Lawn.High),
            new(TileField.Pit, 0f),
            new(TileField.Pit, _bevel ? -section.CurbBevel : 0f),
            new(TileField.Core, _bevel ? section.CurbBevel : 0f),
            new(TileField.Core, 0f),
            new(TileField.Building, 0f)
        ];
        // Disabled thresholds are never split on: a duplicate contour would steal the face tag of the real one.
        _enabled = (1 << _t.Length) - 1;
        if (!_bevel) _enabled &= ~(1 << TBevel);
        if (!_bands) _enabled &= ~((1 << TCurb) | (1 << TEdge) | (1 << TPlazaBorder));
        if (!_pits) _enabled &= ~((1 << TPit) | (1 << TPitTop) | (1 << TPitCoreBevel) | (1 << TPitCore));
        if (!_bevel) _enabled &= ~((1 << TPitTop) | (1 << TPitCoreBevel));
        if (!(section.Lawn.EdgeBand > 0f)) _enabled &= ~(1 << TLawnEdge);
        if (field.ToneMacro == 0) _enabled &= ~((1 << TLawnEdge) | (1 << TLawnLow) | (1 << TLawnHigh));
        if (lod != LodLevel.L0) _enabled &= ~(1 << TLawnEdge);
    }

    public Threshold[] Thresholds => _t;
    public int Enabled => _enabled;

    /// <summary>Height of the surface at the lattice point (i, j).</summary>
    public float LatticeHeight(GroundSurface surface, int i, int j) => HeightOf(surface, _f.V, (j * _f.S + i) * TileField.Count);

    /// <summary>
    /// Base height plus the surface's offset in the street cross-section, from the field values that start at
    /// <paramref name="offset"/> in <paramref name="v"/> (a lattice row or one vertex). A pit's frame and soil take their
    /// heights from the pit's own values; the chamfers slope 45 degrees over the bevel width from the frame top down to
    /// its edge, outside and inside.
    /// </summary>
    public float HeightOf(GroundSurface surface, float[] v, int offset = 0)
    {
        var h = v[offset + TileField.H];
        var bevel = _s.CurbBevel;
        return h + surface switch
        {
            // Terrain already evaluates the lake/channel bed; do not carve it a second time here.
            GroundSurface.Water => 0f,
            GroundSurface.Asphalt or GroundSurface.Parking => _s.RoadY,
            GroundSurface.Bevel => _s.PavingY - bevel + Math.Clamp(v[offset + TileField.A], 0f, bevel),
            GroundSurface.Curb or GroundSurface.Edging or GroundSurface.Track or GroundSurface.Turf or GroundSurface.Court or
                GroundSurface.Granite or GroundSurface.PlazaBorder or GroundSurface.Paver => _pavingY,
            GroundSurface.Grass or GroundSurface.GrassLight or GroundSurface.GrassDark =>
                _grassY + (_pavingY - _grassY) * (1f - Math.Clamp(v[offset + TileField.LawnEdge] / _s.LawnEdgeRise, 0f, 1f)),
            GroundSurface.PitRim => v[offset + TileField.PitRim],
            GroundSurface.PitSoil => v[offset + TileField.PitSoil],
            GroundSurface.PitBevel => v[offset + TileField.PitRim] - bevel +
                MathF.Min(Math.Clamp(-v[offset + TileField.Pit], 0f, bevel), Math.Clamp(v[offset + TileField.Core], 0f, bevel)),
            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown ground surface.")
        };
    }

    // ---------------------------------------------------------------- classification

    public int CodeAt(float[] values, int offset)
    {
        var code = 0;
        for (var t = 0; t < _t.Length; t++)
            if ((_enabled & (1 << t)) != 0 && _t[t].Contains(values[offset + _t[t].Field])) code |= 1 << t;
        return code;
    }

    public int CodeOf(float[] values) => CodeAt(values, 0);

    // Buildings replace visible lawn; surface/height queries keep the ground below the building's own floor.
    public static bool CoveredLawn(int code, GroundSurface surface) =>
        (code & (1 << TBuilding)) != 0 && GroundPalette.IsLawn(surface);

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
}
