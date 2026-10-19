using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Cable tray (电缆桥架) run along X: Width is its length, Height the side height, Depth the tray's width; three cables lie in it.
/// Variation 0: a perforated galvanised tray (a U of floor and two sides, a row of dark slots in the floor). 1: a ladder tray, two rails
/// and rungs every 0.3 m. 2: a wire-basket tray, floor wires, cross wires and side wires. Hung from a ceiling or wall, so visual
/// only. The part is the tray itself; its hangers are the host's (they would leave the footprint).
/// </summary>
[Part(PartCatalog.CableTray, Owner = "WP-12")]
public sealed class CableTrayPart() : MepPart<TrimParams>(new(1100, 220))
{
    protected override Vector3 Minimum => new(0.5f, 0.05f, 0.12f);

    private protected override void Compose(PartBuilder b, TrimParams p, bool near)
    {
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var h = p.Height;
        var steel = PartMaterials.Casing(MepStyle.Stainless);
        const float t = 0.012f;
        var floor = t;
        switch (Variant(p))
        {
            case 0:
                {
                    // The floor sits between the sides and a joint into them, so no two faces share an edge.
                    b.AddBox(new(-hx, 0, -hz + t - MepStyle.Relief), new(hx, t, hz - t + MepStyle.Relief), 0, steel, f);
                    b.AddBox(new(-hx, 0, hz - t), new(hx, h, hz), near ? 0.003f : 0, steel, f);
                    b.AddBox(new(-hx, 0, -hz), new(hx, h, -hz + t), near ? 0.003f : 0, steel, f);
                    if (near)
                    {
                        var slot = PartMaterials.Grille(MepStyle.Dark);
                        for (var x = -hx + 0.15f; x < hx - 0.15f; x += 0.2f)
                            foreach (var z in new[] { -hz * 0.55f, hz * 0.55f })
                                b.AddBox(new(x - 0.05f, t - MepStyle.Relief, z - 0.008f), new(x + 0.05f, t + 0.002f, z + 0.008f), 0, slot, f);
                    }
                    break;
                }
            case 1:
                {
                    const float rail = 0.02f;
                    b.AddBox(new(-hx, 0, hz - rail), new(hx, h, hz), near ? 0.003f : 0, steel, f);
                    b.AddBox(new(-hx, 0, -hz), new(hx, h, -hz + rail), near ? 0.003f : 0, steel, f);
                    floor = 0.02f;
                    var rungs = near ? 0.3f : 0.6f;
                    for (var x = -hx + rungs * 0.5f; x < hx; x += rungs)
                        b.AddBox(new(x - 0.015f, 0, -hz + rail - MepStyle.Relief), new(x + 0.015f, floor, hz - rail + MepStyle.Relief), 0, steel, f);
                    break;
                }
            case 2:
                {
                    // A basket: lengthwise floor wires, cross wires over them, a top wire and a bottom wire on each side, ribs between.
                    const float wire = 0.008f;
                    var metal = PartMaterials.Metal(MepStyle.Stainless);
                    foreach (var z in new[] { -hz + wire, hz - wire })
                    {
                        b.AddBox(new(-hx, h - wire, z - wire * 0.5f), new(hx, h, z + wire * 0.5f), 0, metal, f);
                        b.AddBox(new(-hx, 0, z - wire * 0.5f), new(hx, wire, z + wire * 0.5f), 0, metal, f);
                    }
                    floor = 2 * wire;
                    if (!near)
                    {
                        b.AddBox(new(-hx, 0, -hz + wire), new(hx, wire, hz - wire), 0, PartMaterials.Grille(MepStyle.Dark), f);
                        break;
                    }
                    for (var x = -hx + 0.1f; x < hx; x += 0.2f)
                    {
                        b.AddBox(new(x - wire * 0.5f, wire - MepStyle.Relief, -hz + wire), new(x + wire * 0.5f, floor, hz - wire), 0, metal, f);
                        foreach (var z in new[] { -hz + wire, hz - wire })
                            b.AddBox(new(x - wire * 0.5f, wire - MepStyle.Relief, z - wire * 0.5f), new(x + wire * 0.5f, h - wire + MepStyle.Relief, z + wire * 0.5f), 0, metal, f);
                    }
                    for (var k = -2; k <= 2; k++)
                        b.AddBox(new(-hx, 0, k * hz * 0.4f - wire * 0.5f), new(hx, wire, k * hz * 0.4f + wire * 0.5f), 0, metal, f);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown cable-tray variation.");
        }
        // Three cables lying in the tray along its length.
        var r = Math.Min(0.012f, (h - floor) * 0.4f);
        var cable = new[] { PartMaterials.Pipe(MepStyle.Dark), PartMaterials.Pipe(MepStyle.Grey), PartMaterials.Pipe(MepStyle.Dark) };
        for (var i = 0; i < 3; i++)
        {
            var z = (i - 1) * hz * 0.42f;
            b.AddPipe(new(-hx + 0.02f, floor + r, z), new(hx - 0.02f, floor + r, z), r, near ? 8 : 5, cable[i], f);
        }
    }
}

/// <summary>
/// Floor or building directory (楼层索引牌): a plate with a header band and five rows, each a floor numeral (5 down to 1) on the viewer's
/// left and a bar standing for the floor's contents on the right; the contents' text waits for the scene-text atlas. Variation 0: white
/// plate, dark header. 1: brushed-aluminium plate with a dark face and white numerals. 2: warm-wood plate with a dark header. Back on +Z
/// (a wall); visual only.
/// </summary>
[Part(PartCatalog.DirectorySign, Owner = "WP-12")]
public sealed class DirectorySignPart() : MepPart<SignParams>(new(1500, 160))
{
    protected override Vector3 Minimum => new(0.3f, 0.4f, 0.015f);

    private protected override void Compose(PartBuilder b, SignParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var h = p.Height;
        var face = -hz + MepStyle.Relief;
        var plate = v switch { 0 => PartMaterials.Casing(MepStyle.White), 1 => PartMaterials.Metal(MepStyle.Aluminium), _ => PartMaterials.Casing(MepStyle.Wood) };
        b.AddBox(new(-hx, 0, face), new(hx, h, hz), near ? MepStyle.SmallBevel : 0, plate, f);
        var dark = PartMaterials.Grille(MepStyle.Dark);
        var header = Math.Min(0.14f, h * 0.16f); var margin = 0.03f;
        float rowTop;
        if (v == 1)
        {
            // A dark face inside the aluminium border carries everything.
            b.AddBox(new(-hx + margin, margin, face - 0.0015f), new(hx - margin, h - margin, face + 0.001f), 0, dark, f);
            rowTop = h - margin - 0.02f;
        }
        else
        {
            b.AddBox(new(-hx + margin, h - margin - header, face - 0.0015f), new(hx - margin, h - margin, face + 0.001f), 0, dark, f);
            rowTop = h - margin - header - 0.02f;
        }
        var rowH = (rowTop - margin - 0.02f) / 5;
        if (!near) return;
        var ink = PartMaterials.Grille(v == 1 || v == 2 ? MepStyle.White : MepStyle.Dark);
        var bar = PartMaterials.Grille(v == 1 ? MepStyle.Grey : MepStyle.Dark);
        for (var i = 0; i < 5; i++)
        {
            var cy = rowTop - (i + 0.5f) * rowH;
            // Face coordinates: u is the viewer's right, which is -X.
            StrokeFont.Write(b, (5 - i).ToString(System.Globalization.CultureInfo.InvariantCulture), -hx + margin + 0.06f, cy, face, rowH * 0.55f, ink);
            var u0 = -hx + margin + 0.14f; var u1 = hx - margin - 0.04f - (i % 2 == 0 ? 0.04f : 0.12f);
            b.AddBox(new(-u1, cy - rowH * 0.12f, face - 0.0015f), new(-u0, cy + rowH * 0.12f, face + 0.001f), 0, bar, f);
            if (i > 0) b.AddBox(new(-hx + margin, rowTop - i * rowH - 0.0015f, face - 0.001f), new(hx - margin, rowTop - i * rowH + 0.0015f, face + 0.001f), 0, PartMaterials.Grille(MepStyle.Grey), f);
        }
    }
}
