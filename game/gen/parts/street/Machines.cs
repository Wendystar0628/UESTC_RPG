using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Vending machine (售货机, WLD-LIFE, MAP-QSH-ADDITIONS): a white cabinet on a dark toe under a small grey rain canopy;
/// on the front (−Z) a display window of three shelves of goods in a few clean colours in front of a self-lit back, a self-lit
/// header strip, and below them the blue pick-up panel with its flap and a coin plate. Variation 1 is the one low-saturation
/// odd machine a group may hold (its panel white). No brand and, on the mainline, no lettering. Width × Height × Depth is the
/// cabinet; the canopy reaches <see cref="StreetStyle.VendingCanopyOut"/> beyond the front. The window and header glow cold white at
/// night on the shop-sign channel (WLD-LIFE). Sound: the compressor's hum within 4 m (WLD-LIFE). Collision: one box, Metal.
/// </summary>
[Part(PartCatalog.VendingMachine)]
public sealed class VendingMachinePart : IPartDef<EquipmentParams>
{
    public VendingMachinePart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(520, 120);

    public Box3 Bounds(EquipmentParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f - 0.02f, 0, -p.Depth * 0.5f - S.VendingCanopyOut), new(p.Width * 0.5f + 0.02f, p.Height, p.Depth * 0.5f + 0.01f));
    }

    public IReadOnlyList<Collider> Collision(EquipmentParams p)
    {
        Validate(p);
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Prop)
            { Foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot }];
    }

    /// <summary>The compressor at the back of the toe, heard within 4 m (SYS-AUDIO); the set is registered by WP-53b.</summary>
    public IReadOnlyList<SoundSocketDecl> Sounds(EquipmentParams p)
    {
        Validate(p);
        return [new SoundSocketDecl("compressor", new(S.VendingHumSet), Xf.Move(0, 0.3f, p.Depth * 0.5f - 0.12f), S.VendingHumDb, S.VendingHumRadius)];
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var odd = p.Variation == 1;
        var body = S.Casing(odd ? S.VendingOdd : S.VendingWhite);
        var dark = S.Casing(S.DarkSteel);
        var top = p.Height - S.VendingCanopy;
        if (near) b.AddBox(new(-hx + 0.03f, 0, -hz + 0.03f), new(hx - 0.03f, S.VendingPlinth + S.Joint, hz - 0.03f), 0, dark, VertexFlag.NoShadow);
        b.AddBox(new(-hx, near ? S.VendingPlinth : 0, -hz), new(hx, top + S.Joint, hz), near ? 0.02f : 0, body);
        b.AddBox(new(-hx - 0.02f, top, -hz - S.VendingCanopyOut), new(hx + 0.02f, p.Height, hz + 0.01f), near ? 0.012f : 0, S.Casing(S.GreyMetal));
        // Front, from the top: header strip, window, pick-up panel; all proud of the face so their edges draw.
        var face = -hz; var fx = hx - 0.07f;
        var window0 = p.Height * 0.54f; var window1 = top - 0.2f;
        var lit = S.Lens(S.ScreenWhite, S.ShopGroup);
        b.AddBox(new(-fx, window1 + 0.04f, face - 0.006f), new(fx, top - 0.05f, face + S.Joint), 0, lit, VertexFlag.NoShadow);
        b.AddBox(new(-fx, window0, face - 0.005f), new(fx, window1, face + S.Joint), 0, lit, VertexFlag.NoShadow);
        var panel = S.Board(odd ? S.VendingWhite : S.StreetBlue);
        b.AddBox(new(-fx, 0.12f, face - 0.006f), new(fx, window0 - 0.03f, face + S.Joint), 0, panel, VertexFlag.NoShadow);
        if (!near) return b.Finish(Bounds(p));
        // Window frame and shelves of goods.
        var frame = S.Metal(S.GreyMetal); var e = 0.025f; var proud = face - 0.04f;
        b.AddBox(new(-fx - e, window1, proud), new(fx + e, window1 + e, face + S.Joint), 0, frame, VertexFlag.NoShadow);
        b.AddBox(new(-fx - e, window0 - e, proud), new(fx + e, window0, face + S.Joint), 0, frame, VertexFlag.NoShadow);
        b.AddBox(new(-fx - e, window0 - 0.002f, proud), new(-fx, window1 + 0.002f, face + S.Joint), 0, frame, VertexFlag.NoShadow);
        b.AddBox(new(fx, window0 - 0.002f, proud), new(fx + e, window1 + 0.002f, face + S.Joint), 0, frame, VertexFlag.NoShadow);
        var row = (window1 - window0) / S.VendingShelves;
        var pitch = 2 * fx / S.VendingGoods;
        for (var k = 0; k < S.VendingShelves; k++)
        {
            var y = window0 + k * row;
            if (k > 0) b.AddBox(new(-fx + 0.003f, y - 0.008f, proud + 0.004f), new(fx - 0.003f, y + 0.004f, face), 0, frame, VertexFlag.NoShadow);
            for (var i = 0; i < S.VendingGoods; i++)
            {
                var x = -fx + (i + 0.5f) * pitch;
                var colour = S.Board(S.Goods[(i + 2 * k) % S.Goods.Length]);
                // Bottles and cans: narrower than tall.
                b.AddBox(new(x - pitch * 0.2f, y + 0.012f, proud + 0.01f), new(x + pitch * 0.2f, y + row * 0.72f, face - 0.006f), 0, colour, VertexFlag.NoShadow);
            }
        }
        // Pick-up flap and coin plate on the panel.
        b.AddBox(new(-hx * 0.62f, 0.2f, face - 0.03f), new(hx * 0.62f, 0.38f, face - 0.004f), 0.008f, dark, VertexFlag.NoShadow);
        b.AddBox(new(-fx + 0.04f, window0 - 0.3f, face - 0.02f), new(-fx + 0.18f, window0 - 0.1f, face - 0.004f), 0.006f, dark, VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    private static void Validate(EquipmentParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown vending machine variation.");
        if (p.Width < 0.7f || p.Height < 1.5f || p.Depth < 0.5f) throw new ArgumentOutOfRangeException(nameof(p), "A vending machine is at least 0.7 × 1.5 × 0.5 m.");
    }
}

/// <summary>
/// Delivery locker (外卖柜, WLD-LIFE): a grey steel cabinet on a dark toe under a dark cap, three banks of locker doors on the
/// front (−Z), the middle bank holding a small screen and a keypad. The screen glows cold white at night on the shop-sign channel.
/// Width × Height × Depth is the cabinet. Silent (WLD-LIFE). Collision: one box, Metal (2 m, out of jumping reach).
/// </summary>
[Part(PartCatalog.DeliveryLocker)]
public sealed class DeliveryLockerPart : IPartDef<EquipmentParams>
{
    public DeliveryLockerPart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(380, 100);

    public Box3 Bounds(EquipmentParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f - 0.03f, 0, -p.Depth * 0.5f - 0.03f), new(p.Width * 0.5f + 0.03f, p.Height, p.Depth * 0.5f + 0.03f));
    }

    public IReadOnlyList<Collider> Collision(EquipmentParams p)
    {
        Validate(p);
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Prop)
            { Foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot }];
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var dark = S.Casing(S.DarkSteel);
        const float toe = 0.08f, cap = 0.06f;
        if (near) b.AddBox(new(-hx + 0.04f, 0, -hz + 0.04f), new(hx - 0.04f, toe + S.Joint, hz - 0.04f), 0, dark, VertexFlag.NoShadow);
        b.AddBox(new(-hx, near ? toe : 0, -hz), new(hx, p.Height - cap + S.Joint, hz), near ? 0.015f : 0, S.Casing(S.LightSteel));
        b.AddBox(new(-hx - 0.03f, p.Height - cap, -hz - 0.03f), new(hx + 0.03f, p.Height, hz + 0.03f), near ? 0.012f : 0, dark);
        // Banks of doors, left to right as seen from the front; the middle bank's upper rows give way to the screen and keypad.
        var face = -hz; const float edge = 0.05f, gap = 0.014f;
        var bank = (p.Width - 2 * edge - (S.LockerColumns - 1) * 0.03f) / S.LockerColumns;
        var y0 = toe + 0.06f; var y1 = p.Height - cap - 0.06f;
        var door = S.Casing(S.GreyMetal);
        var screen = (Y0: y0 + (y1 - y0) * 0.56f, Y1: y0 + (y1 - y0) * 0.78f);
        for (var c = 0; c < S.LockerColumns; c++)
        {
            var x1 = hx - edge - c * (bank + 0.03f); var x0 = x1 - bank;
            var middle = c == S.LockerColumns / 2;
            if (!near)
            {
                b.AddBox(new(x0, y0, face - 0.006f), new(x1, y1, face + S.Joint), 0, door, VertexFlag.NoShadow);
                continue;
            }
            // Rows from the bottom: a tall door, then equal ones.
            var tall = (y1 - y0) * 0.28f;
            var small = (y1 - y0 - tall) / (S.LockerRows - 1);
            for (var k = 0; k < S.LockerRows; k++)
            {
                var d0 = k == 0 ? y0 : y0 + tall + (k - 1) * small;
                var d1 = (k == 0 ? y0 + tall : d0 + small) - gap;
                if (middle && d1 > screen.Y0 && d0 < screen.Y1) continue;
                b.AddBox(new(x0, d0, face - 0.008f), new(x1, d1, face + S.Joint), 0, door, VertexFlag.NoShadow);
            }
            if (!middle) continue;
            b.AddBox(new(x0 + 0.02f, screen.Y0 + 0.02f, face - 0.02f), new(x1 - 0.02f, screen.Y1 - 0.02f, face + S.Joint), 0.008f, dark, VertexFlag.NoShadow);
            var sh = (screen.Y1 - screen.Y0) * 0.5f;
            b.AddBox(new(x0 + 0.06f, screen.Y1 - 0.04f - sh, face - 0.024f), new(x1 - 0.06f, screen.Y1 - 0.04f, face - 0.016f), 0, S.Lens(S.ScreenWhite, S.ShopGroup), VertexFlag.NoShadow);
            b.AddBox(new(x0 + bank * 0.3f, screen.Y0 + 0.05f, face - 0.03f), new(x1 - bank * 0.3f, screen.Y0 + 0.05f + sh * 0.6f, face - 0.016f), 0, S.Casing(S.LightSteel), VertexFlag.NoShadow);
        }
        if (!near) b.AddBox(new(-bank * 0.4f, screen.Y0, face - 0.012f), new(bank * 0.4f, screen.Y1, face - 0.004f), 0, S.Lens(S.ScreenWhite, S.ShopGroup), VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    private static void Validate(EquipmentParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 1.2f || p.Height < 1.6f || p.Depth < 0.4f) throw new ArgumentOutOfRangeException(nameof(p), "A delivery locker is at least 1.2 × 1.6 × 0.4 m.");
    }
}

/// <summary>
/// Drinking fountain (直饮水台, MAP-QSH-ADDITIONS): a brushed-steel cabinet with a lipped top, on the front (−Z) a dark
/// recess with two push spouts over a drip tray, the push buttons above, and a green indicator light between them (the one small
/// lit point at night, signal channel); a white plate printed "直饮水" on its left side (+X). Width × Height × Depth is the cabinet
/// (the top at 1.1 m). Silent. Collision: one box, Metal; the top is jumpable (MAP-QSH-ADDITIONS).
/// </summary>
[Part(PartCatalog.DrinkingFountain)]
public sealed class DrinkingFountainPart : IPartDef<EquipmentParams>
{
    public DrinkingFountainPart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(360, 80);

    public Box3 Bounds(EquipmentParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f - 0.015f, 0, -p.Depth * 0.5f - 0.07f), new(p.Width * 0.5f + 0.015f, p.Height, p.Depth * 0.5f + 0.015f));
    }

    public IReadOnlyList<Collider> Collision(EquipmentParams p)
    {
        Validate(p);
        return [new BoxCollider(Xf.Move(0, p.Height * 0.5f, 0), new(p.Width * 0.5f, p.Height * 0.5f, p.Depth * 0.5f), SurfaceTag.Prop)
            { Foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot }];
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var steel = S.Casing(S.Stainless); var dark = S.Casing(S.DarkSteel);
        var top = p.Height - S.FountainTop;
        if (near) b.AddBox(new(-hx + 0.025f, 0, -hz + 0.025f), new(hx - 0.025f, 0.05f + S.Joint, hz - 0.025f), 0, dark, VertexFlag.NoShadow);
        b.AddBox(new(-hx, near ? 0.05f : 0, -hz), new(hx, top + S.Joint, hz), near ? 0.02f : 0, steel);
        b.AddBox(new(-hx - 0.015f, top, -hz - 0.015f), new(hx + 0.015f, p.Height, hz + 0.015f), near ? 0.012f : 0, steel);
        // The dark recess with the drip tray along its foot.
        var face = -hz; var rx = hx - 0.07f;
        var r0 = p.Height * 0.62f; var r1 = top - 0.12f;
        b.AddBox(new(-rx, r0, face - 0.004f), new(rx, r1, face + S.Joint), 0, dark, VertexFlag.NoShadow);
        b.AddBox(new(-rx - 0.01f, r0 - 0.03f, face - 0.07f), new(rx + 0.01f, r0 + 0.01f, face + S.Joint), near ? 0.008f : 0, dark, VertexFlag.NoShadow);
        if (!near) return b.Finish(Bounds(p));
        foreach (var sx in new[] { -1f, 1f })
        {
            var x = sx * rx * 0.5f;
            b.AddPipe(new(x, r1 - 0.06f, face - 0.002f), new(x, r1 - 0.06f, face - 0.055f), 0.012f, 6, S.Pole(S.GreyMetal), VertexFlag.NoShadow);
            b.AddPipe(new(x, r1 - 0.05f, face - 0.05f), new(x, r1 - 0.09f, face - 0.05f), 0.01f, 6, S.Pole(S.GreyMetal), VertexFlag.NoShadow);
            b.AddBox(new(x - 0.035f, r1 + 0.03f, face - 0.012f), new(x + 0.035f, r1 + 0.07f, face + S.Joint), 0, dark, VertexFlag.NoShadow);
        }
        b.AddBox(new(-0.012f, r1 + 0.04f, face - 0.008f), new(0.012f, r1 + 0.064f, face + S.Joint), 0, S.Lens(S.SignGreen, S.Signal), VertexFlag.NoShadow);
        // The "直饮水" plate on the +X side: printed on a front-facing plane, then turned onto the side (−Z onto +X).
        var plate = (Y: p.Height * 0.66f, H: 0.11f, W: 0.27f);
        b.AddBox(new(hx - S.Joint, plate.Y - plate.H * 0.5f, -plate.W * 0.5f), new(hx + 0.006f, plate.Y + plate.H * 0.5f, plate.W * 0.5f), 0, S.Board(S.PaintWhite), VertexFlag.NoShadow);
        var print = new PartBuilder();
        Lettering.Write(print, p.PrintedText, p.PrintedTextBaseColor, 0, plate.Y, 0, false, plate.H * 0.62f, S.Ink(S.StreetBlue));
        b.Add(print.Finish(new(new(-plate.W, 0, -0.01f), new(plate.W, p.Height, 0.01f))), Xf.RotY(-90f).Then(Xf.Move(hx + 0.006f, 0, 0)), VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    private static void Validate(EquipmentParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 0.4f || p.Height < 0.85f || p.Height > 1.25f || p.Depth < 0.35f) throw new ArgumentOutOfRangeException(nameof(p), "A drinking fountain is at least 0.4 × 0.35 m with its top at 0.85–1.25 m.");
    }
}
