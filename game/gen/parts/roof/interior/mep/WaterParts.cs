using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Electric water boiler (电开水器): brushed stainless cabinet under a full-width lid, a dark control panel, taps with red
/// hot-water levers over a perforated drip tray, and the white "小心烫伤" sticker with red lettering (WLD-INTERIORS).
/// Variation 0: floor cabinet on feet, two taps. 1: wider cabinet on a kick plinth, three taps. 2: wall-hung tank, one tap
/// at the bottom (the placer mounts it about 1 m up). Front is −Z.
/// </summary>
[Part(PartCatalog.Boiler)]
public sealed class BoilerPart() : MepPart<EquipmentParams>(new(1300, 160))
{
    private const float Lid = 0.035f;

    protected override Vector3 Minimum => new(0.36f, 0.5f, 0.25f);

    public override IReadOnlyList<Collider> Collision(EquipmentParams p) => Variant(p) == 2 ? [] : PropBox(p);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var steel = PartMaterials.Casing(MepStyle.Stainless);
        var metal = PartMaterials.Metal(MepStyle.Aluminium);
        var dark = PartMaterials.Grille(MepStyle.Dark);
        var hx = p.Width * 0.5f; var back = p.Depth * 0.5f;
        var front = -p.Depth * 0.5f + MepStyle.BoilerTapReach;
        var y0 = v == 2 ? 0 : MepStyle.BoilerFoot;
        b.AddBox(new(-hx + 0.01f, y0, front), new(hx - 0.01f, p.Height - Lid + 0.003f, back), near ? MepStyle.Bevel : 0, steel, f);
        b.AddBox(new(-hx, p.Height - Lid, front - 0.012f), new(hx, p.Height, back), near ? 0.008f : 0, steel, f);
        // Base: four feet, or a recessed kick plinth.
        if (v == 0)
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                {
                    var cx = sx * (hx - 0.05f); var cz = sz > 0 ? back - 0.05f : front + 0.05f;
                    b.AddBox(new(cx - 0.025f, 0, cz - 0.025f), new(cx + 0.025f, y0 + 0.003f, cz + 0.025f), 0, metal, f);
                }
        else if (v == 1)
            b.AddBox(new(-hx + 0.03f, 0, front + 0.03f), new(hx - 0.03f, y0 + 0.003f, back - 0.02f), 0, dark, f);

        // Control panel under the lid.
        var px = Math.Min(hx * 0.62f, 0.2f);
        var py1 = p.Height - Lid - 0.035f; var py0 = py1 - Math.Min(MepStyle.BoilerPanelHeight, (p.Height - y0) * 0.18f);
        b.AddBox(new(-px, py0, front - MepStyle.Relief), new(px, py1, front + 0.002f), near ? MepStyle.SmallBevel : 0, dark, f);
        if (near)
        {
            var white = PartMaterials.Casing(MepStyle.White);
            var gy = (py0 + py1) * 0.5f;
            b.AddBox(new(-px + 0.02f, py0 + 0.025f, front - MepStyle.Relief - 0.002f), new(-px * 0.1f, py1 - 0.025f, front - MepStyle.Relief + 0.001f), 0, PartMaterials.Grille(MepStyle.Grey), f);
            for (var i = 0; i < 3; i++)
                b.AddDisc(px * (0.2f + 0.27f * i), gy, front - MepStyle.Relief - 0.005f, front - MepStyle.Relief + 0.001f, 0.012f, 10, white, f);
        }

        // Taps over the tray.
        var taps = v switch { 0 => 2, 1 => 3, _ => 1 };
        var yt = v == 2 ? Math.Max(0.16f, p.Height * 0.2f)
            : Math.Max(y0 + 0.12f, Math.Min(Math.Max(y0 + 0.32f, p.Height * MepStyle.BoilerTapRatio), py0 - 0.22f));
        var tw = Math.Min(hx - 0.04f, 0.08f * taps + 0.06f);
        var trayTop = v == 2 ? 0.03f : Math.Max(y0 + 0.06f, yt - MepStyle.BoilerTrayDrop);
        b.AddBox(new(-tw, trayTop - 0.03f, -p.Depth * 0.5f + 0.004f), new(tw, trayTop, front + 0.003f), near ? MepStyle.SmallBevel : 0, metal, f);
        // Dark back plate framing the taps.
        b.AddBox(new(-tw, trayTop, front - 0.002f), new(tw, yt + 0.11f, front + 0.002f), 0, dark, f);
        var pitch = Math.Min(0.16f, 2 * tw / taps);
        var red = PartMaterials.Casing(MepStyle.FireRed);
        for (var i = 0; i < taps; i++)
        {
            var x = (i - (taps - 1) * 0.5f) * pitch;
            b.AddBox(new(x - 0.018f, yt, front - 0.05f), new(x + 0.018f, yt + 0.05f, front - 0.001f), near ? 0.004f : 0, metal, f);
            if (!near) continue;
            b.AddColumn(x, yt - 0.04f, yt + 0.003f, front - 0.034f, 0.009f, 8, PartMaterials.Pipe(MepStyle.Aluminium), f);
            b.AddBox(new(x - 0.011f, yt + 0.047f, front - 0.068f), new(x + 0.011f, yt + 0.068f, front - 0.012f), 0.004f, red, f);
        }
        if (!near) return;
        // Drain slots in the tray top.
        for (var k = 0; k < 3; k++)
        {
            var z = -p.Depth * 0.5f + 0.02f + k * (front - (-p.Depth * 0.5f) - 0.03f) / 2.5f;
            b.AddBox(new(-tw + 0.025f, trayTop - 0.003f, z), new(tw - 0.025f, trayTop + 0.0015f, z + 0.008f), 0, dark, f);
        }
        // Scald warning: white sticker, red lettering.
        var h = Math.Min(MepStyle.BoilerWarningHeight, (2 * hx - 0.08f) / StrokeText.Width(MepStyle.BoilerWarning, 1));
        var sy = Math.Min(yt + 0.11f + 0.02f + h * 0.8f, py0 - h);
        var sw = StrokeText.Width(MepStyle.BoilerWarning, h) * 0.5f + 0.015f;
        b.AddBox(new(-sw, sy - h * 0.8f, front - 0.0015f), new(sw, sy + h * 0.8f, front + 0.002f), 0, PartMaterials.Grille(MepStyle.White), f);
        StrokeFont.Write(b, MepStyle.BoilerWarning, 0, sy, front - 0.0015f, h, PartMaterials.Grille(MepStyle.FireRed));
    }
}

/// <summary>
/// Water dispenser (饮水机 / 直饮机). Variation 0: white bottled unit with an inverted aqua bottle. 1: white bottom-loading
/// cabinet with a top control strip. 2: stainless purifier with a dark display and three taps. All have a dark tap niche with a
/// red hot lever and a drip tray. Floor-standing; front is −Z.
/// </summary>
[Part(PartCatalog.WaterDispenser)]
public sealed class WaterDispenserPart() : MepPart<EquipmentParams>(new(700, 200))
{
    private const float Reach = 0.035f;

    protected override Vector3 Minimum => new(0.26f, 0.6f, 0.26f);

    public override IReadOnlyList<Collider> Collision(EquipmentParams p) => PropBox(p);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var back = p.Depth * 0.5f;
        var front = -p.Depth * 0.5f + Reach;
        var body = PartMaterials.Casing(v == 2 ? MepStyle.Stainless : MepStyle.White);
        var dark = PartMaterials.Grille(MepStyle.Dark);
        var metal = PartMaterials.Metal(MepStyle.Aluminium);
        var bottle = v == 0 ? Math.Min(MepStyle.DispenserBottleMax, p.Height * MepStyle.DispenserBottleRatio) : 0;
        var top = p.Height - bottle;
        b.AddBox(new(-hx, 0, front), new(hx, top, back), near ? MepStyle.Bevel : 0, body, f);
        if (v == 0) AddBottle(b, Math.Min(hx, (back - front) * 0.5f), top, p.Height, (front + back) * 0.5f, near);
        else if (near)
        {
            // Top control strip (1) or display (2).
            var band = v == 1 ? PartMaterials.Grille(MepStyle.Grey) : dark;
            b.AddBox(new(-hx * 0.7f, top - 0.11f, front - MepStyle.Relief), new(hx * 0.7f, top - 0.04f, front + 0.002f), MepStyle.SmallBevel, band, f);
        }

        // Tap niche: dark back plate, taps, drip tray.
        var n0 = top * (v == 0 ? 0.48f : 0.5f); var n1 = top * (v == 0 ? 0.86f : 0.74f);
        var nx = hx - MepStyle.DispenserNiche;
        b.AddBox(new(-nx, n0, front - 0.002f), new(nx, n1, front + 0.002f), 0, dark, f);
        b.AddBox(new(-nx + 0.01f, n0, -p.Depth * 0.5f + 0.004f), new(nx - 0.01f, n0 + 0.025f, front + 0.003f), near ? MepStyle.SmallBevel : 0, metal, f);
        var taps = v == 2 ? 3 : 2;
        var pitch = 2 * nx / (taps + 0.4f);
        var yt = n1 - 0.07f;
        for (var i = 0; i < taps; i++)
        {
            var x = (i - (taps - 1) * 0.5f) * pitch;
            var lever = PartMaterials.Casing(i == 0 ? MepStyle.FireRed : v == 2 ? MepStyle.Aluminium : MepStyle.White);
            b.AddBox(new(x - 0.016f, yt - 0.02f, front - 0.03f), new(x + 0.016f, yt + 0.03f, front - 0.001f), near ? 0.004f : 0, lever, f);
            if (near) b.AddColumn(x, yt - 0.045f, yt - 0.017f, front - 0.016f, 0.007f, 8, PartMaterials.Pipe(MepStyle.Aluminium), f);
        }
        if (!near) return;
        // Lower door: a seam line and a recessed pull, so the cabinet does not read as a blank block.
        b.AddBox(new(-hx + 0.015f, n0 - 0.06f, front - 0.0015f), new(hx - 0.015f, n0 - 0.05f, front + 0.002f), 0, metal, f);
        b.AddBox(new(-0.04f, n0 - 0.12f, front - 0.002f), new(0.04f, n0 - 0.1f, front + 0.002f), 0, dark, f);
        if (v == 2)
            for (var k = 0; k < 4; k++)
                b.AddBox(new(-hx * 0.6f, 0.06f + k * 0.03f, front - 0.0015f), new(hx * 0.6f, 0.072f + k * 0.03f, front + 0.002f), 0, dark, f);
    }

    /// <summary>Inverted 18.9 L bottle: neck down into the cabinet top, shoulder, ribbed wall, rounded top.</summary>
    private static void AddBottle(PartBuilder b, float maxRadius, float y0, float y1, float zc, bool near)
    {
        var f = MepStyle.Flags;
        var r = Math.Min(maxRadius * 0.86f, 0.15f); var neck = r * 0.24f;
        var h = y1 - y0;
        var shoulder = y0 + Math.Min(0.12f, h * 0.28f);
        var segments = near ? RoofStyle.RoundSegmentsFine : 8;
        var water = PartMaterials.Curved(MepStyle.Water);
        b.AddColumn(0, y0 - 0.003f, y0 + 0.022f, zc, r * 0.78f, segments, PartMaterials.Casing(MepStyle.Grey), f);
        LatheBand[] bands =
        [
            new(0, y0 + 0.02f, neck, y0 + 0.02f, new(0, -1), new(0, -1)),
            new(neck, y0 + 0.02f, neck, y0 + 0.06f, new(1, 0), new(1, 0)),
            new(neck, y0 + 0.06f, r, shoulder, new(0.6f, -0.8f), new(0.6f, -0.8f)),
            new(r, shoulder, r, y1 - 0.06f, new(1, 0), new(1, 0)),
            new(r, y1 - 0.06f, r * 0.7f, y1, new(1, 0), new(0, 1)),
            new(r * 0.7f, y1, 0, y1, new(0, 1), new(0, 1)),
        ];
        b.Add(PartShapes.Lathe(bands, segments, water), Xf.Move(0, 0, zc), f);
        if (!near) return;
        // Two moulded rings on the wall catch the light like a real bottle.
        foreach (var t in new[] { 0.38f, 0.66f })
        {
            var y = shoulder + (y1 - 0.06f - shoulder) * t;
            b.AddColumn(0, y - 0.008f, y + 0.008f, zc, r + 0.003f, segments, water, f);
        }
    }
}
