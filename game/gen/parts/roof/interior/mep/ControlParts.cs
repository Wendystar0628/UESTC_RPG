using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// 86-type switch plate (开关面板): a white plate with one, two or three rocker keys (Variation 0, 1, 2), each key with a fine
/// grey rocker line. Every room has one by the door (WLD-INTERIORS). Back on +Z; visual only.
/// </summary>
[Part(PartCatalog.SwitchPanel)]
public sealed class SwitchPanelPart() : MepPart<PanelParams>(new(260, 16))
{
    protected override Vector3 Minimum => new(0.06f, 0.06f, 0.008f);

    private protected override void Compose(PartBuilder b, PanelParams p, bool near)
    {
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var white = PartMaterials.Casing(MepStyle.White);
        var face = -hz + 0.002f;
        b.AddBox(new(-hx, 0, face), new(hx, p.Height, hz), near ? Math.Min(0.003f, p.Depth * 0.3f) : 0, white, f);
        if (!near) return;
        var keys = Variant(p) + 1;
        var margin = Math.Min(0.012f, p.Width * 0.14f);
        var span = p.Width - 2 * margin;
        var kw = (span - (keys - 1) * MepStyle.KeyGap) / keys;
        for (var i = 0; i < keys; i++)
        {
            var x0 = -hx + margin + i * (kw + MepStyle.KeyGap);
            b.AddBox(new(x0, margin, -hz), new(x0 + kw, p.Height - margin, face + 0.002f), 0.0015f, white, f);
            b.AddBox(new(x0 + kw * 0.3f, p.Height * 0.5f - 0.0006f, -hz - 0.0004f), new(x0 + kw * 0.7f, p.Height * 0.5f + 0.0006f, -hz + 0.001f), 0, PartMaterials.Grille(MepStyle.Grey), f);
        }
    }
}

/// <summary>
/// Door-side access reader (刷卡器) and the dorm desk face tablet (人脸平板, WLD-INTERIORS). Variation 0: white reader with a
/// dark card window and a card mark. 1: dark reader with a small screen and a 3 × 4 keypad. 2: face tablet: dark screen in a
/// thin aluminium bezel with a camera dot. Back on +Z; visual only.
/// </summary>
[Part(PartCatalog.CardReader)]
public sealed class CardReaderPart() : MepPart<EquipmentParams>(new(300, 40))
{
    protected override Vector3 Minimum => new(0.07f, 0.1f, 0.015f);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var h = p.Height;
        // Body front sits back far enough that a window plus its strokes stay inside the footprint.
        var face = -hz + MepStyle.Relief + 0.0015f;
        var dark = PartMaterials.Grille(MepStyle.Dark);
        var bodyColour = v switch { 0 => PartMaterials.Casing(MepStyle.White), 1 => PartMaterials.Casing(MepStyle.Dark), _ => PartMaterials.Metal(MepStyle.Aluminium) };
        b.AddBox(new(-hx, 0, face), new(hx, h, hz), near ? Math.Min(MepStyle.SmallBevel * 2, p.Depth * 0.3f) : 0, bodyColour, f);
        var m = Math.Min(0.012f, p.Width * 0.12f);
        switch (v)
        {
            case 0:
                {
                    b.AddBox(new(-hx + m, h * 0.42f, face - 0.0015f), new(hx - m, h - m, face + 0.001f), 0, dark, f);
                    if (!near) return;
                    // Card mark: a small rounded card outline in white strokes on the dark window.
                    var cw = (hx - m) * 0.9f; var cy = h * 0.42f + (h * 0.58f - m) * 0.5f; var ch = Math.Min(cw * 0.62f, (h * 0.58f - m) * 0.6f);
                    var ink = PartMaterials.Grille(MepStyle.White); var s = ch * 0.1f; var z = face - 0.0015f;
                    StrokeFont.Segment(b, new(-cw * 0.5f, cy - ch * 0.5f), new(cw * 0.5f, cy - ch * 0.5f), s, z, ink);
                    StrokeFont.Segment(b, new(cw * 0.5f, cy - ch * 0.5f), new(cw * 0.5f, cy + ch * 0.5f), s, z, ink);
                    StrokeFont.Segment(b, new(cw * 0.5f, cy + ch * 0.5f), new(-cw * 0.5f, cy + ch * 0.5f), s, z, ink);
                    StrokeFont.Segment(b, new(-cw * 0.5f, cy + ch * 0.5f), new(-cw * 0.5f, cy - ch * 0.5f), s, z, ink);
                    StrokeFont.Segment(b, new(-cw * 0.32f, cy + ch * 0.12f), new(-cw * 0.08f, cy + ch * 0.12f), s, z, ink);
                    b.AddDisc(0, h * 0.24f, -hz, face + 0.001f, Math.Min(0.006f, hx * 0.15f), 8, PartMaterials.Casing(MepStyle.Grey), f);
                    break;
                }
            case 1:
                {
                    b.AddBox(new(-hx + m, h * 0.68f, face - 0.0015f), new(hx - m, h - m, face + 0.001f), 0, PartMaterials.Grille(MepStyle.Glass), f);
                    if (!near) return;
                    var kw = (p.Width - 2 * m - 2 * 0.004f) / 3; var kh = Math.Min(kw * 0.7f, (h * 0.62f - m) / 4.6f);
                    for (var r = 0; r < 4; r++)
                        for (var c = 0; c < 3; c++)
                        {
                            var x0 = -hx + m + c * (kw + 0.004f); var y1 = h * 0.62f - r * (kh + 0.005f);
                            b.AddBox(new(x0, y1 - kh, -hz), new(x0 + kw, y1, face + 0.001f), 0, PartMaterials.Casing(MepStyle.Grey), f);
                        }
                    break;
                }
            case 2:
                {
                    b.AddBox(new(-hx + m * 0.6f, m * 1.6f, face - 0.0015f), new(hx - m * 0.6f, h - m * 1.6f, face + 0.001f), 0, PartMaterials.Grille(MepStyle.Glass), f);
                    if (!near) return;
                    b.AddDisc(0, h - m * 0.8f, -hz, face + 0.001f, Math.Min(0.0045f, m * 0.4f), 8, dark, f);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(p), v, "Unknown card-reader variation.");
        }
    }
}
