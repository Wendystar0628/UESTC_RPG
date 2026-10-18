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


}
