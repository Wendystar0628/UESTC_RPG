using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// "安全出口 EXIT" sign (WLD-INTERIORS: Chinese above English, never 非常口): white housing, green face, white lettering
/// and a door-and-arrow symbol (no running figure: nothing that reads as a person, CORE-INDEX). The face and lettering
/// carry <see cref="ToonSpec.Emissive"/> = sign.signal (always on); placers also hand <see cref="Glow"/> to the lamp inventory
/// for the night halo. Variation 0: door symbol. 1: arrow pointing left. 2: arrow pointing right. Back on +Z; visual only.
/// </summary>
[Part(PartCatalog.ExitSign)]
public sealed class ExitSignPart() : MepPart<SignParams>(new(900, 60))
{
    protected override Vector3 Minimum => new(0.24f, 0.09f, 0.015f);

    /// <summary>Lamp-inventory marker for a placed sign: a small always-on green glow just in front of the face.</summary>
    public static LightEmitter Glow(SignParams p, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(p);
        return new(MepStyle.SignalChannel, Xf.Point(at, new(0, p.Height * 0.5f, -p.Depth * 0.5f - 0.02f)), MepStyle.SignalGlow,
            MepStyle.ExitGlowEnergy, MepStyle.ExitGlowRange);
    }

    private protected override void Compose(PartBuilder b, SignParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var e = MepStyle.ExitBorder;
        var face = -hz + MepStyle.Relief;
        b.AddBox(new(-hx, 0, face + MepStyle.ExitFaceProud), new(hx, p.Height, hz), MepStyle.SmallBevel, PartMaterials.Casing(MepStyle.White), f);
        b.AddBox(new(-hx + e, e, face), new(hx - e, p.Height - e, face + MepStyle.ExitFaceProud + 0.002f), 0, MepStyle.Lit(MepStyle.ExitGreen), f);
        if (!near) return;
        var ink = MepStyle.Lit(MepStyle.White);   // symbol and lettering positions below are face coordinates (u = viewer's right)
        var ih = p.Height - 2 * e;
        var symbol = ih * 0.9f; var gap = ih * 0.12f;
        var x0 = -hx + e + gap * 0.6f; var x1 = hx - e - gap * 0.6f;
        float sx0, sx1, tx0, tx1;
        if (v == 2) { sx1 = x1; sx0 = x1 - symbol; tx0 = x0; tx1 = sx0 - gap; }
        else { sx0 = x0; sx1 = x0 + symbol; tx0 = sx1 + gap; tx1 = x1; }
        var stroke = ih * 0.11f;
        var ym = e + ih * 0.5f;
        switch (v)
        {
            case 0:
                {
                    // Door frame open at the floor, an arrow walking into it.
                    var dx0 = sx0 + symbol * 0.42f; var dx1 = sx1 - symbol * 0.05f;
                    var dy0 = e + ih * 0.12f; var dy1 = e + ih * 0.88f;
                    StrokeFont.Segment(b, new(dx0, dy0), new(dx0, dy1), stroke, face, ink);
                    StrokeFont.Segment(b, new(dx0, dy1), new(dx1, dy1), stroke, face, ink);
                    StrokeFont.Segment(b, new(dx1, dy1), new(dx1, dy0), stroke, face, ink);
                    Arrow(b, sx0 + symbol * 0.05f, dx0 + symbol * 0.2f, ym - ih * 0.08f, ih * 0.2f, stroke, face, ink);
                    break;
                }
            case 1:
                Arrow(b, sx1 - symbol * 0.08f, sx0 + symbol * 0.08f, ym, ih * 0.3f, stroke * 1.2f, face, ink);
                break;
            case 2:
                Arrow(b, sx0 + symbol * 0.08f, sx1 - symbol * 0.08f, ym, ih * 0.3f, stroke * 1.2f, face, ink);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(p), v, "Unknown exit-sign variation.");
        }
        var width = tx1 - tx0; var cx = (tx0 + tx1) * 0.5f;
        var hc = Math.Min(ih * 0.4f, width / StrokeText.Width(MepStyle.ExitChinese, 1));
        var hl = Math.Min(ih * 0.25f, width / StrokeText.Width(MepStyle.ExitLatin, 1));
        StrokeFont.Write(b, MepStyle.ExitChinese, cx, e + ih * 0.7f, face, hc, ink, MepStyle.ExitWeight);
        StrokeFont.Write(b, MepStyle.ExitLatin, cx, e + ih * 0.24f, face, hl, ink, MepStyle.ExitWeight);
    }

    /// <summary>Arrow from <paramref name="from"/> to <paramref name="to"/> along X at height y, with a two-stroke head.</summary>
    private static void Arrow(PartBuilder b, float from, float to, float y, float head, float stroke, float face, ToonSpec ink)
    {
        var dir = Math.Sign(to - from);
        StrokeFont.Segment(b, new(from, y), new(to - dir * stroke * 0.5f, y), stroke, face, ink);
        StrokeFont.Segment(b, new(to, y), new(to - dir * head, y + head), stroke, face, ink);
        StrokeFont.Segment(b, new(to, y), new(to - dir * head, y - head), stroke, face, ink);
    }
}

/// <summary>
/// Door plate (门牌) beside or above a room door. Variation 0: white plate with a dark band on the left and dark numerals.
/// 1: brushed aluminium plate with a dark top rule. 2: warm wood plate with white numerals. The catalogue path has no text
/// field yet, so it draws the blank plate; placers that know the room number call <see cref="Build"/> (numerals and '-' only,
/// "区-层-序号", WLD-INTERIORS). Back on +Z; visual only.
/// </summary>
[Part(PartCatalog.RoomSign)]
public sealed class RoomSignPart() : MepPart<SignParams>(new(500, 60))
{
    private static readonly RoomSignPart Shared = new();

    protected override Vector3 Minimum => new(0.12f, 0.06f, 0.008f);

    /// <summary>The plate with <paramref name="number"/> on it; LOD levels other than L0 drop the numerals.</summary>
    public static PartGeometry Build(SignParams p, string number, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(number);
        var bounds = Shared.Bounds(p);
        var builder = new PartBuilder();
        Shared.Compose(builder, p, lod == LodLevel.L0);
        if (lod == LodLevel.L0 && number.Length > 0) Numerals(builder, p, number);
        return builder.Finish(bounds);
    }

    private protected override void Compose(PartBuilder b, SignParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var plate = v switch { 0 => PartMaterials.Casing(MepStyle.White), 1 => PartMaterials.Metal(MepStyle.Aluminium), _ => PartMaterials.Casing(MepStyle.Wood) };
        var face = -hz + MepStyle.Relief;
        b.AddBox(new(-hx, 0, face), new(hx, p.Height, hz), Math.Min(MepStyle.SmallBevel, p.Depth * 0.3f), plate, f);
        var dark = PartMaterials.Grille(MepStyle.Dark);
        // The band sits on the viewer's left, which is +X for a plate facing −Z.
        if (v == 0) b.AddBox(new(hx - p.Width * MepStyle.RoomSignHeader, 0.008f, face - 0.0015f), new(hx - 0.008f, p.Height - 0.008f, face + 0.001f), 0, dark, f);
        else if (v == 1 && near) b.AddBox(new(-hx + 0.012f, p.Height - 0.022f, face - 0.0015f), new(hx - 0.012f, p.Height - 0.016f, face + 0.001f), 0, dark, f);
    }

    private static void Numerals(PartBuilder b, SignParams p, string number)
    {
        var v = Variant(p);
        var hx = p.Width * 0.5f; var face = -p.Depth * 0.5f + MepStyle.Relief;
        var left = v == 0 ? -hx + p.Width * MepStyle.RoomSignHeader : -hx;   // face u: the band takes the viewer's left
        var room = hx - left - 0.03f;
        var h = Math.Min(p.Height * MepStyle.RoomSignTextRatio, room / StrokeText.Width(number, 1));
        var ink = PartMaterials.Grille(v == 2 ? MepStyle.White : MepStyle.Dark);
        StrokeFont.Write(b, number, (left + hx) * 0.5f, p.Height * (v == 1 ? 0.44f : 0.5f), face, h, ink);
    }
}
