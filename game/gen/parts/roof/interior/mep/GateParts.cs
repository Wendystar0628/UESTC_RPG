using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Access gate cabinet (闸机), one side of a lane: a stainless cabinet on a dark toe under a cap, Width across the lane, Depth along it
/// (the passer walks along Z), the lane on its +X side. Two cabinets face each other across the passage. Variation 0: a wing gate, its
/// glass wing drawn back into a dark slot on the lane side, a green indicator. 1: a swing gate, its glass leaf standing open along the
/// lane on two hinge blocks, green. 2: a flap gate, two slots for retracted flaps, a red indicator (closed). The indicator is a plain
/// colour here; the always-on glow is the lamp table's (WLD-INTERIORS-ACCESS). Floor-standing: one prop box.
/// </summary>
[Part(PartCatalog.Turnstile, Owner = "WP-12")]
public sealed class TurnstilePart() : MepPart<EquipmentParams>(new(1000, 200))
{
    private const float Side = 0.03f;

    protected override Vector3 Minimum => new(0.2f, 0.8f, 0.8f);

    public override IReadOnlyList<Collider> Collision(EquipmentParams p) => PropBox(p);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var h = p.Height;
        var steel = MepStyle.Casing(MepStyle.Stainless);
        var dark = MepStyle.Insert(MepStyle.Dark);
        var bx = hx - Side;   // the cabinet's lane-side face; the leaf and the slots stand between it and the footprint's edge
        b.AddBox(new(-hx + 0.02f, 0, -hz + 0.04f), new(bx - 0.02f, 0.05f + MepStyle.Relief, hz - 0.04f), 0, dark, f);
        b.AddBox(new(-hx, 0.05f, -hz), new(bx, h - 0.05f + MepStyle.Relief, hz), near ? MepStyle.Bevel : 0, steel, f);
        b.AddBox(new(-hx, h - 0.05f, -hz), new(bx, h - 0.006f, hz), near ? 0.01f : 0, steel, f);
        // Indicator window near the entry end, on the cap: green arrow lens, or the red one of the closed gate.
        var lens = MepStyle.Insert(v == 2 ? MepStyle.FireRed : MepStyle.ExitGreen);
        b.AddBox(new(-bx * 0.5f - 0.04f, h - 0.012f, -hz + 0.08f), new(-bx * 0.5f + 0.04f, h, -hz + 0.2f), 0, lens, f);
        if (!near && v == 0) return;
        switch (v)
        {
            case 0:
                // Wing slot on the lane face, with the edge of the glass wing showing in it.
                b.AddBox(new(bx - MepStyle.Relief, 0.62f, -hz * 0.55f), new(bx + MepStyle.Relief + 0.001f, 0.9f, hz * 0.55f), 0, dark, f);
                if (near) b.AddBox(new(bx, 0.7f, -hz * 0.45f), new(bx + 0.012f, 0.84f, hz * 0.45f), 0, MepStyle.Insert(MepStyle.Glass), f);
                break;
            case 1:
                // The leaf stands open along the lane on two hinge blocks.
                b.AddBox(new(bx, 0.2f, -hz * 0.3f), new(bx + 0.016f, 0.85f, hz * 0.55f), 0, MepStyle.Insert(MepStyle.Glass), f);
                foreach (var z in new[] { -hz * 0.3f + 0.03f, hz * 0.55f - 0.03f })
                    b.AddBox(new(bx - MepStyle.Relief, 0.3f, z - 0.025f), new(bx + 0.02f, 0.4f, z + 0.025f), 0, dark, f);
                break;
            default:
                // Two flap slots, the flaps drawn back.
                foreach (var y in new[] { 0.45f, 0.75f })
                    b.AddBox(new(bx - MepStyle.Relief, y, -hz * 0.5f), new(bx + MepStyle.Relief + 0.001f, y + 0.02f, hz * 0.5f), 0, dark, f);
                break;
        }
        if (near && v != 2)
            // A card reader plate on the cap, past the indicator.
            b.AddBox(new(-bx * 0.5f - 0.025f, h - 0.012f, -hz + 0.3f), new(-bx * 0.5f + 0.025f, h, -hz + 0.4f), 0.003f, MepStyle.Casing(MepStyle.White), f);
    }
}

/// <summary>
/// Switchgear cabinet (配电柜) against a wall: a plinth, a light grey body and doors with handles on the front (−Z). Variation 0: two
/// doors with louvres low and an indicator panel with three lamps above. 1: three narrow doors with a dark meter window each. 2: one
/// large dark door with hinges and a red warning plate. Floor-standing: one prop box.
/// </summary>
[Part(PartCatalog.Switchgear, Owner = "WP-12")]
public sealed class SwitchgearPart() : MepPart<EquipmentParams>(new(1100, 220))
{
    protected override Vector3 Minimum => new(0.5f, 1.2f, 0.4f);

    public override IReadOnlyList<Collider> Collision(EquipmentParams p) => PropBox(p);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var h = p.Height;
        var dark = MepStyle.Insert(MepStyle.Dark);
        var shell = MepStyle.Casing(v == 2 ? MepStyle.Dark : MepStyle.Grey);
        var door = MepStyle.Casing(v == 2 ? MepStyle.Dark : MepStyle.White);
        const float plinth = 0.08f; const float skin = 0.03f;
        b.AddBox(new(-hx + 0.02f, 0, -hz + 0.02f), new(hx - 0.02f, plinth + MepStyle.Relief, hz - 0.02f), 0, dark, f);
        b.AddBox(new(-hx, plinth, -hz + skin), new(hx, h, hz), near ? MepStyle.Bevel : 0, shell, f);
        // Doors stand 15 mm off the body, handles another 15 mm out; the cabinet's front plane is -hz.
        var doors = v switch { 0 => 2, 1 => 3, _ => 1 };
        var inner = p.Width - 0.1f; var gap = 0.01f;
        var dw = (inner - (doors - 1) * gap) / doors;
        var y0 = 0.16f; var y1 = h - (v == 0 ? 0.28f : 0.1f);
        for (var i = 0; i < doors; i++)
        {
            var x0 = -inner * 0.5f + i * (dw + gap);
            b.AddBox(new(x0, y0, -hz + 0.015f), new(x0 + dw, y1, -hz + skin + MepStyle.Relief), near ? 0.008f : 0, door, f);
            if (!near) continue;
            // Handle on the meeting side (the door nearest the middle), hinges on the other for the single door.
            var hxp = doors == 1 ? x0 + dw - 0.05f : (i < doors / 2.0f ? x0 + dw - 0.04f : x0 + 0.04f);
            b.AddBox(new(hxp - 0.012f, (y0 + y1) * 0.5f - 0.08f, -hz), new(hxp + 0.012f, (y0 + y1) * 0.5f + 0.08f, -hz + 0.015f + MepStyle.Relief), 0.004f, MepStyle.Metal(MepStyle.Aluminium), f);
            if (v == 0)
                for (var k = 0; k < 4; k++)
                    b.AddBox(new(x0 + 0.06f, y0 + 0.08f + k * 0.045f, -hz + 0.011f), new(x0 + dw - 0.06f, y0 + 0.08f + k * 0.045f + 0.02f, -hz + 0.015f + MepStyle.Relief), 0, dark, f);
            if (v == 1)
                b.AddBox(new(x0 + 0.04f, y1 - 0.22f, -hz + 0.011f), new(x0 + dw - 0.04f, y1 - 0.08f, -hz + 0.015f + MepStyle.Relief), 0, MepStyle.Insert(MepStyle.Glass), f);
            if (v == 2)
                foreach (var y in new[] { y0 + 0.2f, y1 - 0.2f })
                    b.AddBox(new(x0 + 0.005f, y - 0.05f, -hz + 0.011f), new(x0 + 0.03f, y + 0.05f, -hz + 0.015f + MepStyle.Relief), 0, MepStyle.Metal(MepStyle.Aluminium), f);
        }
        if (v == 0)
        {
            // Indicator panel above the doors with three lamps.
            b.AddBox(new(-hx * 0.6f, h - 0.22f, -hz + 0.015f), new(hx * 0.6f, h - 0.06f, -hz + skin + MepStyle.Relief), 0, dark, f);
            if (near)
                for (var i = 0; i < 3; i++)
                    b.AddDisc((i - 1) * hx * 0.35f, h - 0.14f, -hz + 0.006f, -hz + 0.02f, 0.014f, 10, MepStyle.Insert(i == 1 ? MepStyle.FireRed : MepStyle.ExitGreen), f);
        }
        else if (v == 2 && near)
            b.AddBox(new(-0.1f, h - 0.3f, -hz + 0.011f), new(0.1f, h - 0.18f, -hz + 0.015f + MepStyle.Relief), 0, MepStyle.Insert(MepStyle.FireRed), f);
    }
}
