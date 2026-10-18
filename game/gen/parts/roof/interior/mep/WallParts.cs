using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Wall fan (壁扇) on a wall plate and a short arm: a motor, three pitched blades and a round guard of a rim, a mid ring, a hub and
/// eight spokes bellying forward. Variation 0: white body, aluminium guard. 1: dark industrial body and guard. 2: a flush exhaust fan,
/// a square plate with a dark round opening, four blades and a hub. The origin is the footprint's bottom centre; the guard hangs at the
/// top of the height. Front is −Z, back on +Z; visual only.
/// </summary>
[Part(PartCatalog.WallFan, Owner = "WP-12")]
public sealed class WallFanPart() : MepPart<EquipmentParams>(new(900, 220))
{
    protected override Vector3 Minimum => new(0.3f, 0.3f, 0.26f);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hz = p.Depth * 0.5f;
        if (v == 2) { Exhaust(b, p, near); return; }
        var segments = near ? 16 : 8;
        var r = Math.Min(p.Width * 0.5f - 0.01f, (p.Height - 0.06f) * 0.5f);   // 护圈外沿 r + 6 mm、辐条 4 mm，收进 1 cm 留在外框里
        var cy = p.Height - r - 0.01f;
        var body = MepStyle.Casing(v == 0 ? MepStyle.White : MepStyle.Grey);
        var guard = MepStyle.Metal(v == 0 ? MepStyle.Aluminium : MepStyle.Dark);
        // From the front: hub, blade plane, guard rim and the motor behind them; the wall plate and arm take the back.
        var zf = -hz + 0.015f; var zr = zf + 0.11f; var zb = zf + 0.07f;
        var motorFront = zb + 0.025f; var motorRear = motorFront + 0.1f;
        b.AddBox(new(-0.07f, cy - 0.09f, hz - 0.02f), new(0.07f, cy + 0.09f, hz), near ? MepStyle.Bevel : 0, body, f);
        b.AddPipe(new(0, cy, motorRear - 0.01f), new(0, cy, hz - 0.015f), 0.02f, near ? 8 : 6, MepStyle.Pipe(v == 0 ? MepStyle.White : MepStyle.Grey), f);
        // Motor: a rounded drum along Z (the lathe's Y runs to −Z once turned).
        var turn = Xf.RotX(-90f);
        b.AddLathe(PartShapes.Creased(new(0, 0), new(0.07f, 0), new(0.07f, 0.075f), new(0.04f, 0.1f), new(0, 0.1f)), segments, MepStyle.Curved(v == 0 ? MepStyle.White : MepStyle.Grey), turn.Then(Xf.Move(0, cy, motorRear)), f);
        // Guard: the rim, then (near) the mid ring, hub and spokes.
        b.AddLathe(PartShapes.Loop(new(r - 0.006f, 0), new(r + 0.006f, 0), new(r + 0.006f, 0.014f), new(r - 0.006f, 0.014f)), segments, guard, turn.Then(Xf.Move(0, cy, zr + 0.014f)), f);
        var hubR = 0.035f;
        b.AddLathe(PartShapes.Creased(new(0, 0), new(hubR, 0), new(hubR, 0.015f), new(0, 0.015f)), segments, body, turn.Then(Xf.Move(0, cy, zf + 0.015f)), f);
        if (!near)
        {
            b.AddLathe(PartShapes.Creased(new(0, 0), new(r * 0.9f, 0), new(r * 0.9f, 0.008f), new(0, 0.008f)), segments, MepStyle.Insert(MepStyle.Dark), turn.Then(Xf.Move(0, cy, zb + 0.004f)), f);
            return;
        }
        var rm = (r + hubR) * 0.5f;
        b.AddLathe(PartShapes.Loop(new(rm - 0.004f, 0), new(rm + 0.004f, 0), new(rm + 0.004f, 0.01f), new(rm - 0.004f, 0.01f)), segments, guard, turn.Then(Xf.Move(0, cy, (zr + zf) * 0.5f + 0.005f)), f);
        for (var i = 0; i < 8; i++)
        {
            var a = i * MathF.PI / 4;
            var (c, s) = (MathF.Cos(a), MathF.Sin(a));
            b.AddPipe(new(c * r, cy + s * r, zr), new(c * hubR, cy + s * hubR, zf), 0.004f, 4, guard, f);
        }
        // Three pitched blades on the hub plane.
        var len = r - hubR - 0.025f;
        for (var i = 0; i < 3; i++)
        {
            var at = Xf.RotX(14f).Then(Xf.Move(hubR + len * 0.5f, 0, 0)).Then(Matrix4x4.CreateRotationZ(i * MathF.PI * 2 / 3 + 0.3f)).Then(Xf.Move(0, cy, zb));
            b.AddCentredBox(new(len, 0.075f, 0.006f), at, body, 0.002f, f);
        }
    }

    /// <summary>Flush exhaust fan: a square plate on the wall, a dark round opening, four blades and a hub.</summary>
    private static void Exhaust(PartBuilder b, EquipmentParams p, bool near)
    {
        var f = MepStyle.Flags;
        var s = Math.Min(p.Width, p.Height) * 0.5f; var back = p.Depth * 0.5f;
        var cy = p.Height * 0.5f;
        var white = MepStyle.Casing(MepStyle.White);
        b.AddBox(new(-s, cy - s, back - 0.06f), new(s, cy + s, back), near ? MepStyle.Bevel : 0, white, f);
        var ro = s * 0.82f;
        b.AddDisc(0, cy, back - 0.066f, back - 0.05f, ro, near ? 20 : 10, MepStyle.Insert(MepStyle.Dark), f);
        if (!near) return;
        for (var i = 0; i < 4; i++)
        {
            var at = Xf.RotX(16f).Then(Xf.Move(0.04f + (ro - 0.06f) * 0.5f, 0, 0)).Then(Matrix4x4.CreateRotationZ(i * MathF.PI * 0.5f + 0.4f)).Then(Xf.Move(0, cy, back - 0.074f));
            b.AddCentredBox(new(ro - 0.06f, 0.07f, 0.006f), at, MepStyle.Casing(MepStyle.Grey), 0.002f, f);
        }
        b.AddDisc(0, cy, back - 0.084f, back - 0.07f, 0.035f, 12, white, f);
    }
}

/// <summary>
/// Surveillance camera (监控摄像头). Variation 0: ceiling dome, a white base disc under the ceiling and a smoked dome below it.
/// 1: bullet camera on a wall bracket, a white tube under a sun hood with a dark lens, pointing −Z. 2: a ball camera hung from a wall
/// arm, a white sphere with a smoked window band. The origin is the footprint's bottom centre; the part hangs from its top (the ceiling
/// or the wall bracket). Back on +Z; visual only.
/// </summary>
[Part(PartCatalog.SecurityCamera, Owner = "WP-12")]
public sealed class SecurityCameraPart() : MepPart<EquipmentParams>(new(700, 160))
{
    protected override Vector3 Minimum => new(0.1f, 0.1f, 0.2f);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var f = MepStyle.Flags;
        var segments = near ? 16 : 8;
        var white = MepStyle.Curved(MepStyle.White);
        var smoked = MepStyle.Curved(MepStyle.Dark);
        var h = p.Height; var hz = p.Depth * 0.5f;
        var rb = Math.Min(p.Width, 0.2f) * 0.5f;
        switch (Variant(p))
        {
            case 0:
                {
                    // Base disc at the top, the dome hanging under it.
                    var rd = rb * 0.9f; var cy = h - 0.025f;
                    b.AddLathe(PartShapes.Creased(new(0, cy - 0.004f), new(rb, cy - 0.004f), new(rb, h), new(0, h)), segments, white, Matrix4x4.Identity, f);
                    var dome = new List<LatheBand>(PartShapes.Arc(rd, cy, -90, 0, near ? 4 : 2)) { LatheBand.Flat(rd, cy, 0, cy) };
                    b.AddLathe(dome, segments, smoked, Matrix4x4.Identity, f);
                    break;
                }
            case 1:
                {
                    // Wall bracket, a short arm, then the tube along −Z under its hood.
                    var cy = h * 0.5f; var zRear = hz - 0.04f; var length = p.Depth - 0.044f;
                    var rt = Math.Min(0.032f, h * 0.3f);
                    b.AddBox(new(-0.025f, cy - 0.04f, hz - 0.02f), new(0.025f, cy + 0.04f, hz), near ? 0.004f : 0, MepStyle.Casing(MepStyle.White), f);
                    b.AddPipe(new(0, cy, hz - 0.015f), new(0, cy, zRear), 0.012f, 6, MepStyle.Pipe(MepStyle.Aluminium), f);
                    var turn = Xf.RotX(-90f).Then(Xf.Move(0, cy, zRear));
                    var body = length - 0.06f;
                    b.AddLathe(PartShapes.Creased(new(0, 0), new(rt, 0), new(rt, body), new(0, body)), segments, white, turn, f);
                    b.AddLathe(PartShapes.Loop(new(rt - 0.003f, body - 0.03f), new(rt + 0.008f, body - 0.03f), new(rt + 0.008f, length), new(rt - 0.003f, length)), segments, white, turn, f);
                    b.AddLathe(PartShapes.Creased(new(0, body - 0.004f), new(rt * 0.9f, body - 0.004f), new(rt * 0.9f, body + 0.004f), new(0, body + 0.004f)), segments, smoked, turn, f);
                    break;
                }
            case 2:
                {
                    // Wall arm, a stem, then the ball with a smoked band round it.
                    var rs = Math.Min(0.045f, (h - 0.05f) * 0.5f); var cy = rs + 0.003f; var ax = hz - 0.07f;
                    b.AddBox(new(-0.025f, h - 0.07f, hz - 0.02f), new(0.025f, h - 0.005f, hz), near ? 0.004f : 0, MepStyle.Casing(MepStyle.White), f);
                    b.AddPipe(new(0, h - 0.035f, hz - 0.015f), new(0, h - 0.035f, ax), 0.012f, 6, MepStyle.Pipe(MepStyle.Aluminium), f);
                    b.AddPipe(new(0, h - 0.035f, ax), new(0, cy + rs * 0.8f, ax), 0.012f, 6, MepStyle.Pipe(MepStyle.Aluminium), f);
                    var at = Xf.Move(0, cy, ax);
                    b.AddLathe(PartShapes.Arc(rs, 0, -90, 90, near ? 6 : 3).ToList(), segments, white, at, f);
                    if (near)
                    {
                        // The smoked window: a ring section standing a little proud of the sphere, round its middle.
                        var (c0, s0) = (MathF.Cos(-0.49f), MathF.Sin(-0.49f)); var (c1, s1) = (MathF.Cos(0.14f), MathF.Sin(0.14f));
                        var ro = rs + 0.004f; var ri = rs * 0.9f;
                        b.AddLathe(PartShapes.Loop(new(ri * c0, ri * s0), new(ro * c0, ro * s0), new(ro * c1, ro * s1), new(ri * c1, ri * s1)), segments, smoked, at, f);
                    }
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown camera variation.");
        }
    }
}

/// <summary>
/// Indoor loudspeaker (室内喇叭). Variation 0: wall box speaker, a white cabinet with a dark grille, a woofer and a tweeter, on a wall
/// bracket. 1: pendant horn on a rod from the ceiling, a white trumpet pointing −Z. 2: slim wall column with three small drivers
/// behind a dark grille. Back on +Z; visual only.
/// </summary>
[Part(PartCatalog.IndoorSpeaker, Owner = "WP-12")]
public sealed class IndoorSpeakerPart() : MepPart<EquipmentParams>(new(800, 200))
{
    protected override Vector3 Minimum => new(0.12f, 0.25f, 0.15f);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var f = MepStyle.Flags;
        var segments = near ? 16 : 8;
        var white = MepStyle.Casing(MepStyle.White);
        var dark = MepStyle.Insert(MepStyle.Dark);
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f; var h = p.Height;
        switch (Variant(p))
        {
            case 0:
                {
                    var front = -hz + 0.02f; var back = hz - 0.03f;
                    b.AddBox(new(-hx, 0, front), new(hx, h, back), near ? MepStyle.Bevel : 0, white, f);
                    b.AddBox(new(-hx * 0.3f, h * 0.2f, back - MepStyle.Relief), new(hx * 0.3f, h * 0.8f, hz), 0, MepStyle.Metal(MepStyle.Aluminium), f);
                    var gx = hx - 0.03f;
                    b.AddBox(new(-gx, 0.03f, front - MepStyle.Relief), new(gx, h - 0.03f, front + 0.003f), 0, dark, f);
                    if (!near) break;
                    // Woofer ring below, tweeter above (discs facing −Z).
                    var ring = MepStyle.Metal(MepStyle.Grey);
                    b.AddDisc(0, h * 0.32f, front - 0.012f, front + 0.002f, Math.Min(gx * 0.7f, 0.075f), segments, ring, f);
                    b.AddDisc(0, h * 0.32f, front - 0.016f, front - 0.008f, Math.Min(gx * 0.7f, 0.075f) * 0.55f, segments, dark, f);
                    b.AddDisc(0, h * 0.74f, front - 0.01f, front + 0.002f, 0.03f, 12, ring, f);
                    break;
                }
            case 1:
                {
                    var cy = h * 0.34f; var zNeck = hz - 0.02f;
                    var r = Math.Min(hz - 0.03f, Math.Min(hx, 0.12f));
                    var length = p.Depth - 0.04f;
                    var turn = Xf.RotX(-90f).Then(Xf.Move(0, cy, zNeck));
                    var pts = new[] { new Vector2(0.03f, 0f), new Vector2(0.036f + (r - 0.036f) * 0.05f, length * 0.3f), new Vector2(0.036f + (r - 0.036f) * 0.35f, length * 0.62f), new Vector2(0.036f + (r - 0.036f) * 0.75f, length * 0.88f), new Vector2(r, length) };
                    var bands = new List<LatheBand> { LatheBand.Flat(0, 0, 0.03f, 0) };
                    bands.AddRange(PartShapes.Smooth(near ? pts : [pts[0], pts[2], pts[4]]));
                    bands.Add(LatheBand.Flat(r, length, 0, length));
                    b.AddLathe(bands, segments, MepStyle.Curved(MepStyle.White), turn, f);
                    b.AddLathe(PartShapes.Creased(new(0, length - 0.004f), new(r * 0.78f, length - 0.004f), new(r * 0.78f, length + 0.0005f), new(0, length + 0.0005f)), segments, dark, turn, f);
                    // Rod from the ceiling to the neck.
                    b.AddPipe(new(0, h, zNeck), new(0, cy + 0.02f, zNeck), 0.011f, 6, MepStyle.Pipe(MepStyle.Aluminium), f);
                    break;
                }
            case 2:
                {
                    var w = Math.Min(p.Width, 0.14f) * 0.5f; var d = Math.Min(p.Depth, 0.14f) * 0.5f;
                    var back = hz;
                    b.AddBox(new(-w, 0, back - 2 * d), new(w, h, back), near ? MepStyle.Bevel : 0, white, f);
                    b.AddBox(new(-w + 0.012f, 0.05f, back - 2 * d - MepStyle.Relief), new(w - 0.012f, h - 0.05f, back - 2 * d + 0.003f), 0, dark, f);
                    if (!near) break;
                    var ring = MepStyle.Metal(MepStyle.Grey);
                    for (var i = 0; i < 3; i++)
                        b.AddDisc(0, 0.1f + i * (h - 0.2f) * 0.5f, back - 2 * d - 0.012f, back - 2 * d + 0.002f, Math.Min(w - 0.02f, 0.035f), 12, ring, f);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown speaker variation.");
        }
    }
}
