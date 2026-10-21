using System.Numerics;
using Campus.Gen.Geometry;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings.Build;

/// <summary>Entrance surrounds, canopies and landings, pilasters, rain and condensate pipes and grille shelves.</summary>
internal sealed class BuildingDetailMesher(BuildingPlan plan, LodLevel lod, GeometrySink sink,
    BuildingSurfaces surfaces, BuildingSkin shell, FacadeFittings fittings)
{
    public void Build()
    {
        Entrances();
        Pilasters();
        if (shell.Full) Downpipes();
        if (plan.Profile.AcGrilles) Grilles();
    }

    // ---------------------------------------------------------------------------------------------
    // Entrances: surround, canopy, and the game/gen/parts/circulation/ landing with its steps or slope (EntranceKit) (L0/L1).
    // ---------------------------------------------------------------------------------------------
    private void Entrances()
    {
        foreach (var e in plan.Entrances) Entrance(e);
    }

    private void Entrance(PlanEntrance e)
    {
        var fl = plan.Facades[e.Facade];
        var skin = plan.SkinAt(fl.Ring);
        var f = EdgeFrame.Of(skin, fl.Edge);
        var door = plan.DoorOf(e);
        var uMin = 0.15f;
        var uMax = skin.Length[fl.Edge] - 0.15f;
        var w = 0.22f;
        // Door surround (stone), 70 mm proud of the wall.
        f.Box(sink, surfaces.Stone, door.U0 - w, door.U0, 0f, 0.07f, door.Y0, door.Y1 + w);
        f.Box(sink, surfaces.Stone, door.U1, door.U1 + w, 0f, 0.07f, door.Y0, door.Y1 + w);
        f.Box(sink, surfaces.Stone, door.U0, door.U1, 0f, 0.07f, door.Y1, door.Y1 + w);
        // A door onto a deck or porch of another package stands alone: that package roofs and floors its landing.
        if (!e.Landing) return;
        // Canopy: white aluminium edge, 0.3 m thick, ~2.2 m deep.
        var cu0 = MathF.Max(uMin, door.U0 - 1.0f);
        var cu1 = MathF.Min(uMax, door.U1 + 1.0f);
        var cy = e.CanopyY;
        var depth = plan.Small ? 1.2f : 2.2f;
        var canopy = shell.Full
            ? Profile.Of([new(0f, cy, 0f, 0.35f), new(depth, cy), new(depth, cy + 0.28f), new(depth - 0.02f, cy + 0.3f), new(0f, cy + 0.3f)], 3)
            : Profile.Of([new(0f, cy, 0f, 0.35f), new(depth, cy), new(depth, cy + 0.3f), new(0f, cy + 0.3f)]);
        f.Bar(sink, surfaces.Trim with { Family = Families.WallWarmWhite }, cu0, cu1, canopy);
        // Landing at the threshold and the steps or slope down to the outside ground: game/gen/parts/circulation/ parts.
        EntranceKit.Build(sink, EntranceKit.Pieces(plan, e), lod);
    }

    // ---------------------------------------------------------------------------------------------
    // Vertical members: pilasters on piers, downpipes, AC grilles.
    // ---------------------------------------------------------------------------------------------
    private void Pilasters()
    {
        var every = plan.Profile.PilasterEvery;
        if (every <= 0) return;
        var top = plan.Profile.RoofEdge == Rules.RoofEdge.Cornice ? plan.WallTopY : plan.RoofY - 0.25f;
        foreach (var (fi, f, layout, skin) in shell.Facades())
        {
            if (layout.Bays < 3 || layout.Kinds.Any(k => k == BayKind.Blank)) continue;
            var pier = layout.BayWidth - layout.Width;
            if (pier < 0.85f) continue;
            for (var m = every; m < layout.Bays; m += every)
            {
                var uc = layout.BoundarySkin(m);
                // None beside a door: not on its bay's boundaries, nor within 0.6 m of an off-centre anchor door.
                if (plan.Entrances.Any(e => e.Facade == fi && (m == e.Bay || m == e.Bay + 1 ||
                    (uc > plan.DoorOf(e).U0 - 0.6f && uc < plan.DoorOf(e).U1 + 0.6f)))) continue;
                Pilaster(f, uc - 0.28f, uc + 0.28f, plan.PlinthTopY, top);
            }
        }
    }

    private void Pilaster(EdgeFrame f, float u0, float u1, float y0, float y1)
    {
        const float d = 0.14f;
        var c = shell.Full ? 0.03f : 0f;
        var o = f.Out;
        var a = f.Along;
        var front = (o + Vector3.Zero);
        f.Panel(sink, surfaces.Trim, u0 + c, u1 - c, d, y0, y1, front, new(u0, y0), new(u1, y1));
        // sides
        sink.Quad(surfaces.Trim, new V(f.At(u0, 0f, y0), -a, new(0f, y0)), new V(f.At(u0, d - c, y0), -a, new(d, y0)), new V(f.At(u0, d - c, y1), -a, new(d, y1)), new V(f.At(u0, 0f, y1), -a, new(0f, y1)));
        sink.Quad(surfaces.Trim, new V(f.At(u1, 0f, y0), a, new(0f, y0)), new V(f.At(u1, d - c, y0), a, new(d, y0)), new V(f.At(u1, d - c, y1), a, new(d, y1)), new V(f.At(u1, 0f, y1), a, new(0f, y1)));
        if (c > 0f)
        {
            sink.Quad(surfaces.Trim, new V(f.At(u0, d - c, y0), -a, default), new V(f.At(u0 + c, d, y0), o, default), new V(f.At(u0 + c, d, y1), o, default), new V(f.At(u0, d - c, y1), -a, default));
            sink.Quad(surfaces.Trim, new V(f.At(u1, d - c, y0), a, default), new V(f.At(u1 - c, d, y0), o, default), new V(f.At(u1 - c, d, y1), o, default), new V(f.At(u1, d - c, y1), a, default));
        }
        if (plan.Profile.RoofEdge != Rules.RoofEdge.Cornice)
            sink.Quad(surfaces.Trim, new V(f.At(u0, 0f, y1), Vector3.UnitY, default), new V(f.At(u1, 0f, y1), Vector3.UnitY, default),
                new V(f.At(u1, d, y1), Vector3.UnitY, default), new V(f.At(u0, d, y1), Vector3.UnitY, default));
    }

    /// <summary>Rain downpipes every ~20 m on the outer ring, and condensate drains beside the niche strips (game/gen/parts/roof/ parts, FacadeFittings).</summary>
    private void Downpipes()
    {
        var top = plan.Profile.RoofEdge == Rules.RoofEdge.Cornice ? plan.WallTopY : plan.RoofY + 0.15f;
        var rain = new ToonSpec(Materials.Lit(plan.Profile.Trim), Materials.Shade(plan.Profile.Trim));
        var drainColour = new ToonSpec(Materials.Lit(FittingRules.DrainFamily), Materials.Shade(FittingRules.DrainFamily));
        foreach (var (fi, f, layout, skin) in shell.Facades())
        {
            if (layout.Ring != 0 || skin.Length[layout.Edge] < 10f) continue;
            var openings = plan.Openings[fi];
            var slots = fittings.Slots(fi, layout, skin);
            var drains = fittings.Drains(fi, layout, skin).ToArray();
            foreach (var (u, drainTop) in drains) fittings.Downpipe(f, u, plan.SunkY, drainTop, true, drainColour);
            var spots = new List<float> { 0.2f };
            for (var x = 20f; x < skin.Length[layout.Edge] - 6f; x += 20f)
            {
                // nearest bay boundary, skipped when a pilaster stands there
                var m = (int)MathF.Round((x + layout.SkinShift) / layout.BayWidth);
                if (plan.Profile.PilasterEvery > 0 && m % plan.Profile.PilasterEvery == 0) m++;
                if (m <= 0 || m >= layout.Bays) continue;
                spots.Add(layout.BoundarySkin(m) + (plan.Profile.AcGrilles ? FittingRules.RainBesideGrille : 0f));
            }
            foreach (var u in spots)
            {
                // Clear of every opening, niche and drain of the facade.
                var near = FittingRules.PipeHalf + FittingRules.PipeClear;
                if (!FacadeFittings.Clear(openings, u) || slots.Any(s => s.U0 - near < u && u < s.U1 + near) ||
                    drains.Any(d => MathF.Abs(d.U - u) < 2f * FittingRules.PipeHalf)) continue;
                fittings.Downpipe(f, u, plan.SunkY, top, false, rain);
            }
        }
    }

    /// <summary>The unit grilles: a game/gen/parts/roof/ cage on a chamfered shelf beside each window, in vertical strips.</summary>
    private void Grilles()
    {
        foreach (var (fi, f, layout, skin) in shell.Facades())
            foreach (var slot in fittings.Slots(fi, layout, skin))
            {
                Shelf(f, slot);
                fittings.Grille(f, slot);
            }
    }

    /// <summary>Shelf under a niche, from the wall to just past the cage front; chamfered front top edge at L0, AO under it.</summary>
    private void Shelf(EdgeFrame f, GrilleSlot slot)
    {
        var u0 = slot.U0 - FittingRules.ShelfSide;
        var u1 = slot.U1 + FittingRules.ShelfSide;
        var depth = FittingRules.Grille.Depth + FittingRules.ShelfOverhang;
        var top = slot.Y0;
        var bottom = top - FittingRules.ShelfThickness;
        if (!shell.Full)
        {
            f.Box(sink, surfaces.Trim, u0, u1, 0f, depth, bottom, top, aoBottom: FittingRules.ShelfAo);
            return;
        }
        var c = FittingRules.ShelfChamfer;
        f.Bar(sink, surfaces.Trim, u0, u1, Profile.Of([new(0f, bottom, 0f, FittingRules.ShelfAo), new(depth, bottom), new(depth, top - c), new(depth - c, top), new(0f, top)], 2));
    }
}
