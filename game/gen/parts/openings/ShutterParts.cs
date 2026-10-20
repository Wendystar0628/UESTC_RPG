using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

/// <summary>
/// 卷帘门: guide rails, a housing with a drip lip, a slatted curtain and a bottom bar. How far it is raised comes from
/// OpenAngleDeg as a share of 90° (0 shut, 45 half, 90 fully up); the position never follows the clock (WLD-INTERIORS).
/// It closes the wall, so it casts shadow. L0: flat slats parted by V-grooves steep enough for a crease line, chamfered
/// housing; L1: flat curtain; L2–L3: slabs; L4: nothing.
/// </summary>
[Part(PartCatalog.DoorRoller)]
public sealed class RollerShutterPart() : OpeningPart<DoorParams>(new(1100, 80))
{
    protected override Box3 Extent(DoorParams p)
    {
        var e = OpeningStyle.PressIn;
        var front = p.Depth * 0.5f + OpeningStyle.RollerHousingProud + 0.014f;
        return new(new(-p.Width * 0.5f - e, 0, -front), new(p.Width * 0.5f + e, p.Height + e, p.Depth * 0.5f));
    }

    protected override void Validate(DoorParams p)
    {
        if (!float.IsFinite(p.OpenAngleDeg) || p.OpenAngleDeg is < 0 or > 120) throw new ArgumentOutOfRangeException(nameof(p), "OpenAngleDeg must be within 0–120.");
        if (p.Width < 0.6f || p.Height < OpeningStyle.RollerHousing + 0.5f) throw new ArgumentOutOfRangeException(nameof(p), "The shutter opening is too small.");
    }

    /// <summary>Bottom of the curtain (top of the floor gap when shut).</summary>
    public static float Bottom(DoorParams p)
    {
        var open = Math.Clamp(p.OpenAngleDeg / 90f, 0f, 1f);
        return open * (p.Height - OpeningStyle.RollerHousing - OpeningStyle.RollerBottomBar);
    }

    private protected override void Build(PartMesh mesh, DoorParams p, LodLevel lod)
    {
        if (lod == LodLevel.L4) return;
        const byte cast = 0;
        var spec = Pick(p, OpeningStyle.Silver);
        var e = OpeningStyle.PressIn;
        var ins = OpeningStyle.Insert;
        float w = p.Width * 0.5f, d = p.Depth * 0.5f;
        var hb = p.Height - OpeningStyle.RollerHousing;
        var yb = Bottom(p);
        var bar = OpeningStyle.RollerBottomBar;
        var rail = OpeningStyle.RollerRail;
        var proud = OpeningStyle.RollerHousingProud;
        var skin = OpeningStyle.RollerSkin * 0.5f;
        if (lod >= LodLevel.L2)
        {
            Solid.Box(mesh, spec, cast, -w - e, hb, -d - proud, w + e, p.Height + e, d);
            if (yb + bar < hb - 0.05f) Solid.Box(mesh, spec, cast, -w + rail - ins, yb, -skin, w - rail + ins, hb + ins, skin);
            return;
        }
        Solid.Box(mesh, spec, cast, -w - e, 0, -d, -w + rail, hb + ins, d);
        Solid.Box(mesh, spec, cast, w - rail, 0, -d, w + e, hb + ins, d);
        Solid.Box(mesh, Hard(spec), cast, -w - e, hb, -d - proud, w + e, p.Height + e, d, Bevelled(p, lod) ? OpeningStyle.LargeBevel : 0f);
        if (lod == LodLevel.L0)
            Solid.Box(mesh, spec, cast, -w - e, hb - 0.02f, -d - proud - 0.012f, w + e, hb + 0.03f, -d - proud + ins);
        if (yb + bar < hb - 0.05f)
        {
            var c0 = -w + rail * 0.4f;
            var c1 = w - rail * 0.4f;
            if (lod == LodLevel.L0) Slats(mesh, spec, c0, c1, yb + bar - ins, hb + 0.02f, skin);
            else Solid.Box(mesh, spec, cast, c0, yb + bar - ins, -skin, c1, hb + 0.02f, skin);
        }
        var y0 = Math.Min(yb, hb - bar);
        var dark = OpeningStyle.DarkMetal;
        Solid.Box(mesh, dark, cast, -w + rail - ins, y0, -0.03f, w - rail + ins, y0 + bar, 0.03f);
        if (lod == LodLevel.L0)
            foreach (var x in new[] { -w * 0.5f, w * 0.5f })
                Solid.Box(mesh, dark, VertexFlag.NoShadow, x - 0.05f, y0 + 0.018f, -0.042f, x + 0.05f, y0 + 0.05f, -0.03f + ins);
    }

    /// <summary>
    /// The curtain as one closed prism along X: a flat front cut by a V-groove every slat pitch, flat back. The groove
    /// faces stand about 60° off the slat face, so each groove reads as a steady crease line at any sun height.
    /// </summary>
    private static void Slats(PartMesh mesh, ToonSpec spec, float x0, float x1, float y0, float y1, float half)
    {
        var pitch = OpeningStyle.RollerSlatPitch;
        var g = OpeningStyle.RollerGroove * 0.5f;
        var depth = Math.Min(OpeningStyle.RollerGrooveDepth, half * 1.4f);
        var count = Math.Max(1, (int)Math.Floor((y1 - y0) / pitch));
        // (z, y) section, front at z = −half: bottom-front corner, a groove above each slat, then the flat back.
        var section = new List<Vector2> { new(-half, y0) };
        for (var k = 1; k < count; k++)
        {
            var y = y0 + (y1 - y0) * k / count;
            section.Add(new(-half, y - g));
            section.Add(new(-half + depth, y));
            section.Add(new(-half, y + g));
        }
        section.Add(new(-half, y1));
        section.Add(new(half, y1));
        section.Add(new(half, y0));
        Solid.Prism(mesh, spec, 0, section, x0, x1);
    }

    /// <summary>Lowered below head height it blocks the opening as a DoorClosed closure; raised it leaves the way free.</summary>
    public override IReadOnlyList<Collider> Collision(DoorParams p)
    {
        _ = Bounds(p);
        if (Bottom(p) >= OpeningStyle.RollerPassHeight) return [];
        var at = Matrix4x4.Identity;
        at.M42 = p.Height * 0.5f;
        var foot = p.Foot == FootMat.Auto ? FootMat.Shutter : p.Foot;
        return [new BoxCollider(at, new(p.Width * 0.5f, p.Height * 0.5f, Math.Max(p.Depth * 0.5f, 0.05f)), SurfaceTag.DoorClosed) { Foot = foot }];
    }
}

/// <summary>
/// 百叶. Variation 0: exterior louvre (frame, ≈ 35° blades, dark plenum behind; casts shadow). Variation 1: indoor
/// venetian blind lowered fully; 2: lowered over the upper 55 % (the "窗帘或百叶" covering of ART-FIDELITY, no shadow).
/// </summary>
[Part(PartCatalog.Louvre)]
public sealed class LouvrePart() : OpeningPart<PanelParams>(new(420, 160))
{
    protected override Box3 Extent(PanelParams p)
    {
        var e = OpeningStyle.PressIn;
        var z = Math.Max(p.Depth * 0.5f, OpeningStyle.BlindChord * 0.5f + 0.012f);
        return new(new(-p.Width * 0.5f - e, -e, -z), new(p.Width * 0.5f + e, p.Height + e, z));
    }

    protected override void Validate(PanelParams p)
    {
        if (p.Width < 0.2f || p.Height < 0.2f) throw new ArgumentOutOfRangeException(nameof(p), "A louvre must be at least 0.2 × 0.2 m.");
    }

    private protected override void Build(PartMesh mesh, PanelParams p, LodLevel lod)
    {
        if (p.Variation == 0) Exterior(mesh, p, lod);
        else Blind(mesh, p, lod);
    }

    private static void Exterior(PartMesh mesh, PanelParams p, LodLevel lod)
    {
        if (lod == LodLevel.L4) return;
        const byte cast = 0;
        var spec = Pick(p, OpeningStyle.Metal);
        var e = OpeningStyle.PressIn;
        var ins = OpeningStyle.Insert;
        var face = OpeningStyle.LouvreFrame;
        float w = p.Width * 0.5f, d = p.Depth * 0.5f;
        if (lod >= LodLevel.L2)
        {
            Solid.Box(mesh, spec, cast, -w - e, -e, -d, w + e, p.Height + e, d);
            return;
        }
        Solid.Ring(mesh, spec, cast, -w - e, -e, w + e, p.Height + e, face + e, -d, d);
        Solid.Box(mesh, OpeningStyle.Gasket, cast, -w + face - ins, face - ins, d - 0.012f, w - face + ins, p.Height - face + ins, d - 0.004f);
        var inner = p.Height - 2 * face;
        var count = Math.Max(1, (int)Math.Floor(inner / OpeningStyle.LouvrePitch));
        var spacing = inner / count;
        var run = Math.Min(OpeningStyle.LouvreRun, 2 * d - 0.02f);
        var rise = OpeningStyle.LouvreRise * run / OpeningStyle.LouvreRun;
        var length = (float)Math.Sqrt((double)run * run + (double)rise * rise);
        var half = OpeningStyle.LouvreBlade * 0.5f;
        // Normal to the chord, pointing up and out: (z, y) = (−rise, run) / length.
        var nz = -rise / length * half;
        var ny = run / length * half;
        for (var k = 0; k < count; k++)
        {
            var yc = face + spacing * (k + 0.5f);
            var a = new Vector2(run * 0.5f, yc + rise * 0.5f);
            var b = new Vector2(-run * 0.5f, yc - rise * 0.5f);
            Solid.Prism(mesh, spec, cast, [new(a.X + nz, a.Y + ny), new(b.X + nz, b.Y + ny), new(b.X - nz, b.Y - ny), new(a.X - nz, a.Y - ny)],
                -w + face - ins, w - face + ins);
        }
    }

    private static void Blind(PartMesh mesh, PanelParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        const byte flags = VertexFlag.NoShadow;
        var spec = Pick(p, OpeningStyle.Blind);
        var ins = OpeningStyle.Insert;
        var w = p.Width * 0.5f;
        var rail = OpeningStyle.BlindRail;
        var top = p.Height - rail;
        var bottom = p.Variation == 2 ? p.Height * 0.45f : 0f;
        Solid.Box(mesh, spec, flags, -w, top, -rail * 0.5f, w, p.Height, rail * 0.5f);
        Solid.Box(mesh, spec, flags, -w + 0.005f, bottom, -rail * 0.5f, w - 0.005f, bottom + 0.022f, rail * 0.5f);
        var chord = OpeningStyle.BlindChord * 0.5f;
        if (lod == LodLevel.L1)
        {
            Solid.Box(mesh, spec, flags, -w + 0.01f, bottom + 0.022f - ins, -0.006f, w - 0.01f, top + ins, 0.006f);
            return;
        }
        var tilt = OpeningStyle.BlindTilt * 0.5f;
        var half = OpeningStyle.BlindSlat * 0.5f;
        for (var y = top - OpeningStyle.BlindPitch * 0.5f; y > bottom + 0.04f; y -= OpeningStyle.BlindPitch)
            Solid.Prism(mesh, spec, flags, [new(chord, y + tilt + half), new(-chord, y - tilt + half), new(-chord, y - tilt - half), new(chord, y + tilt - half)],
                -w + 0.01f, w - 0.01f);
    }
}
