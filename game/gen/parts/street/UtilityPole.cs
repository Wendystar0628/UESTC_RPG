using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Concrete utility pole (电线杆, ART-AMBIENCE §5): a tapered round pole with a steel crossarm in front of its top carrying three white
/// porcelain insulators, footholds above head height alternating left and right, and a small number plate at eye level. At most five kinds
/// of piece, as the doc asks; no wires (the span lines are drawn between poles by the placer). Width is the diameter at the foot, Height
/// the pole's top (9–10 m in the back lanes), Depth is unused (the pole is round). The front is −Z (the road side). Variation 0: the plain
/// pole. 1: with a pole-mounted transformer barrel on a strap bracket under the arm; it hums within 6 m (WLD-LIFE-STREET). 2: with a
/// short arm and a cold-white anti-crime lamp at 4.5–5 m (后巷防犯灯, ART-AMBIENCE §8), its diffuser self-lit on the street lamp group.
/// Collision: the pole as a post, Concrete.
/// </summary>
[Part(PartCatalog.UtilityPole, Owner = "WP-13")]
public sealed class UtilityPolePart : IPartDef<ColumnParams>
{
    public UtilityPolePart() => Lods = Levels.Of<ColumnParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<ColumnParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(1500, 420);

    public Box3 Bounds(ColumnParams p)
    {
        Validate(p);
        var hx = S.PoleArmLength * 0.5f + 0.03f;
        return Levels.Padded(new(-hx, 0, -Reach(p)), new(hx, p.Height, p.Width * 0.5f + 0.01f));
    }

    public IReadOnlyList<Collider> Collision(ColumnParams p)
    {
        Validate(p);
        return [new PostCollider(Vector2.Zero, p.Width * 0.5f + 0.03f, -S.PostColliderSink, p.Height, SurfaceTag.Prop) { Foot = StreetShapes.Foot(p.Foot, S.PoleFoot) }];
    }

    /// <summary>The transformer's hum (WLD-LIFE-STREET: only within a few metres); the set waits for the sound-set registry.</summary>
    public IReadOnlyList<SoundSocketDecl> Sounds(ColumnParams p)
    {
        Validate(p);
        if (p.Variation != 1) return [];
        var t = Tank(p);
        return [new SoundSocketDecl("transformer", new(S.TransformerSet), Xf.Move(0, t.Y + S.TankHeight * 0.5f, t.Z), S.TransformerHumDb, S.TransformerHumRadius)];
    }

    private PartGeometry Build(ColumnParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var segments = lod switch { LodLevel.L0 => S.RoundSegmentsSmall, LodLevel.L1 => S.RoundSegmentsFar, _ => 6 };
        var b = new PartBuilder();
        var h = p.Height;
        var concrete = S.Pole(S.StoneGrey);
        var steel = S.Casing(S.GreyMetal);
        // The tapered shaft, its top rounded off.
        var rb = p.Width * 0.5f; var rt = Top(p);
        b.AddLathe(near
            ? PartShapes.Creased(new(0, 0), new(rb, 0), new(rt, h - 0.04f), new(rt - 0.02f, h), new(0, h))
            : PartShapes.Creased(new(0, 0), new(rb, 0), new(rt, h), new(0, h)), segments, concrete, Matrix4x4.Identity);
        // Crossarm in front of the pole's top, three insulators standing on it.
        var ya = h - S.PoleArmDrop;
        var armHalf = S.PoleArmLength * 0.5f; var sec = S.PoleArmSection;
        var armFront = -(R(p, ya) + sec - 0.005f);
        b.AddBox(new(-armHalf, ya - sec * 0.5f, armFront), new(armHalf, ya + sec * 0.5f, -R(p, ya) * 0.5f), near ? 0.008f : 0, steel);
        foreach (var x in new[] { -armHalf + 0.12f, 0f, armHalf - 0.12f })
            b.AddLathe(near ? Insulator : InsulatorFar, near ? 8 : 6, S.Pole(S.PaintWhite), Xf.Move(x, ya + sec * 0.5f - S.Joint, armFront + sec * 0.5f));
        if (lod > LodLevel.L1) return b.Finish(Bounds(p));
        // Number plate at eye level, footholds from 2.6 m up.
        var yp = S.PoleNumberY;
        var plateFront = -(R(p, yp) + 0.014f);
        b.AddBox(new(-0.08f, yp - 0.06f, plateFront), new(0.08f, yp + 0.06f, -R(p, yp) * 0.5f), 0, S.Board(S.PaintWhite), VertexFlag.NoShadow);
        if (near)
        {
            StreetShapes.Ink(b, -0.065f, yp + 0.02f, 0.065f, yp + 0.045f, plateFront, false, S.Ink(S.SignBlue));
            StreetShapes.Ink(b, -0.065f, yp - 0.04f, 0.04f, yp - 0.02f, plateFront, false, S.Ink(S.DarkSteel));
            var side = 1f;
            for (var y = S.PegFirst; y < ya - 0.6f - (p.Variation == 2 ? 0.5f : 0f); y += S.PegPitch)
            {
                b.AddPipe(new(side * R(p, y) * 0.6f, y, 0), new(side * (R(p, y) + S.PegLength), y, 0), S.PegRadius, 5, S.Pole(S.GreyMetal), VertexFlag.NoShadow);
                side = -side;
            }
        }
        switch (p.Variation)
        {
            case 1: Transformer(b, p, near, segments); break;
            case 2: Lamp(b, p, near, segments); break;
        }
        return b.Finish(Bounds(p));
    }

    /// <summary>Pole-mounted transformer: a grey barrel with a domed lid and three bushings, on two strap brackets from the pole.</summary>
    private static void Transformer(PartBuilder b, ColumnParams p, bool near, int segments)
    {
        var t = Tank(p);
        var r = S.TankRadius; var th = S.TankHeight;
        var bands = new List<LatheBand>(near
            ? PartShapes.Creased(new(0, 0), new(r - 0.02f, 0), new(r, 0.02f), new(r, th - 0.08f), new(0, th - 0.08f)).Take(3)
            : PartShapes.Creased(new(0, 0), new(r, 0), new(r, th - 0.08f), new(0, th - 0.08f)).Take(2));
        bands.AddRange(PartShapes.Arc(r, th - 0.08f, 0, 90, near ? 3 : 1, 0.08f / r));
        b.AddLathe(bands, segments, S.Pole(S.LightSteel), Xf.Move(0, t.Y, t.Z));
        foreach (var dy in new[] { 0.12f, th - 0.12f })
        {
            var y = t.Y + dy; var rp = R(p, y);
            b.AddBox(new(-0.09f, y - 0.03f, t.Z), new(0.09f, y + 0.03f, -rp * 0.5f), near ? 0.006f : 0, S.Casing(S.DarkSteel));
        }
        if (!near) return;
        foreach (var x in new[] { -0.09f, 0f, 0.09f })
            b.AddLathe(Bushing, 6, S.Pole(S.PaintWhite), Xf.Move(x, t.Y + th - 0.02f, t.Z));
    }

    /// <summary>Anti-crime lamp: a short arm from a clamp, a shallow white shade over a cold-white diffuser on the street lamp group.</summary>
    private static void Lamp(PartBuilder b, ColumnParams p, bool near, int segments)
    {
        var y = LampY(p);
        var rp = R(p, y);
        var grey = S.Pole(S.GreyMetal);
        b.AddBox(new(-0.07f, y - 0.05f, -(rp + 0.04f)), new(0.07f, y + 0.05f, -rp * 0.5f), near ? 0.006f : 0, S.Casing(S.DarkSteel));
        var tipY = y + S.LampArmRise;
        b.AddPipe(new(0, y, -(rp + 0.02f)), new(0, tipY - 0.015f, -S.LampArmReach), S.LampArmRadius, near ? 6 : 4, grey);
        var head = Xf.Move(0, tipY - S.LampShadeHeight, -S.LampArmReach);
        var sr = S.LampShadeRadius;
        b.AddLathe(PartShapes.Creased(new(0, 0), new(sr, 0), new(sr, 0.02f), new(sr * 0.35f, S.LampShadeHeight), new(0, S.LampShadeHeight)), segments, S.Pole(S.PaintWhite), head);
        var lr = sr * 0.82f;
        b.AddLathe(PartShapes.Creased(new(0, -0.012f), new(lr, -0.012f), new(lr, 0.004f), new(0, 0.004f)), segments, S.Lens(S.ScreenWhite, S.StreetGroup), head, VertexFlag.NoShadow);
    }

    // Insulator profiles (radius, height), standing on y = 0.
    private static readonly IReadOnlyList<LatheBand> Insulator = PartShapes.Creased(
        new(0, 0), new(0.03f, 0), new(0.03f, 0.03f), new(0.048f, 0.036f), new(0.048f, 0.048f), new(0.03f, 0.054f), new(0.03f, 0.1f), new(0, 0.1f));
    private static readonly IReadOnlyList<LatheBand> InsulatorFar = PartShapes.Creased(new(0, 0), new(0.04f, 0), new(0.04f, 0.1f), new(0, 0.1f));
    private static readonly IReadOnlyList<LatheBand> Bushing = PartShapes.Creased(
        new(0, 0), new(0.022f, 0), new(0.022f, 0.03f), new(0.034f, 0.036f), new(0.034f, 0.046f), new(0.022f, 0.052f), new(0.022f, 0.075f), new(0, 0.075f));

    /// <summary>Pole radius at height <paramref name="y"/>: linear taper from the foot to the top.</summary>
    private static float R(ColumnParams p, float y) => p.Width * 0.5f + (Top(p) - p.Width * 0.5f) * Math.Clamp(y / p.Height, 0f, 1f);

    private static float Top(ColumnParams p) => Math.Min(p.Width * 0.5f, Math.Max(p.Width * 0.5f * S.PoleTopRatio, S.PoleTopMin));

    private static float LampY(ColumnParams p) => Math.Min(S.LampHeight, p.Height - 1.2f);

    /// <summary>Transformer barrel: its base height and centre z (in front of the pole, a gap from it).</summary>
    private static (float Y, float Z) Tank(ColumnParams p)
    {
        var y = p.Height - S.TankDrop;
        return (y, -(R(p, y + S.TankHeight * 0.5f) + S.TankGap + S.TankRadius));
    }

    /// <summary>How far the part reaches in front of the pole's axis (towards −Z).</summary>
    private static float Reach(ColumnParams p) => p.Variation switch
    {
        1 => -Tank(p).Z + S.TankRadius,
        2 => S.LampArmReach + S.LampShadeRadius,
        _ => p.Width * 0.5f + S.PoleArmSection + 0.01f,
    };

    private static void Validate(ColumnParams p)
    {
        Levels.Dimensions(p);
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown utility-pole variation.");
        if (p.Width < 0.15f || p.Width > 0.7f || p.Height < 5f || p.Height > 12f) throw new ArgumentOutOfRangeException(nameof(p), "A utility pole is 0.15–0.7 m across at the foot and 5–12 m tall.");
    }
}
