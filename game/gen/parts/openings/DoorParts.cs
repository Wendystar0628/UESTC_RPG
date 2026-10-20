using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

internal enum DoorStyle { Entrance, Wood, Fire, Glass, Screen, Stall }

internal readonly record struct LeafSlot(float X0, float X1, bool HingeLeft, bool Swings);

/// <summary>
/// Door frames and leaves (WLD-INTERIORS B). Local frame: origin at the bottom centre of the opening on the
/// leaf mid-plane, exterior towards −Z. Leaves open outwards about their hinge by OpenAngleDeg; with three or more
/// leaves the outer two stay shut as side lights. Door glass carries the corridor/lobby card like WP-14's door bays.
/// L0: frame, leaves and hardware; L1: frame and leaf slabs; L2 and further: nothing.
/// </summary>
internal static class DoorBuilder
{
    /// <summary>Hardware reach beyond a leaf face (pull handles, panic bar).</summary>
    public const float Reach = 0.14f;
    private static Vector2 DoorUv2 => new(0, 32);

    public static float FrameFace(DoorStyle s) => s switch
    {
        DoorStyle.Entrance => OpeningStyle.DoorFrameFace,
        DoorStyle.Wood => OpeningStyle.CasingFace,
        DoorStyle.Fire => 0.08f,
        DoorStyle.Glass => 0.05f,
        DoorStyle.Screen => OpeningStyle.ScreenFrameFace,
        DoorStyle.Stall => 0f,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown door style."),
    };

    public static float Thickness(DoorStyle s) => s switch
    {
        DoorStyle.Entrance => OpeningStyle.DoorLeafThickness,
        DoorStyle.Wood => 0.045f,
        DoorStyle.Fire => 0.05f,
        DoorStyle.Glass => 0.012f,
        DoorStyle.Screen => 0.07f,
        DoorStyle.Stall => 0.025f,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown door style."),
    };

    public static ToonSpec FrameSpec(DoorStyle s) => s switch
    {
        DoorStyle.Entrance or DoorStyle.Glass or DoorStyle.Screen => OpeningStyle.Metal,
        DoorStyle.Wood => OpeningStyle.Casing,
        DoorStyle.Fire => OpeningStyle.PaintedSteel,
        DoorStyle.Stall => OpeningStyle.Laminate,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown door style."),
    };

    public static ToonSpec LeafSpec(DoorStyle s) => s switch
    {
        DoorStyle.Entrance or DoorStyle.Glass or DoorStyle.Screen => OpeningStyle.Metal,
        DoorStyle.Wood => OpeningStyle.Wood,
        DoorStyle.Fire => OpeningStyle.PaintedSteel,
        DoorStyle.Stall => OpeningStyle.Laminate,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown door style."),
    };

    public static FootMat Foot(DoorStyle s) => s switch
    {
        DoorStyle.Entrance or DoorStyle.Glass => FootMat.Glass,
        DoorStyle.Wood or DoorStyle.Stall => FootMat.WoodDoor,
        DoorStyle.Fire or DoorStyle.Screen => FootMat.Metal,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown door style."),
    };

    private static bool Threshold(DoorStyle s) => s is DoorStyle.Entrance or DoorStyle.Fire or DoorStyle.Screen;
    private static float FrameProud(DoorStyle s) => s switch
    {
        DoorStyle.Wood => OpeningStyle.CasingProud,
        DoorStyle.Screen => 0.02f,
        _ => 0f,
    };

    /// <summary>Leaf bottom and top, and the transom bar's bottom when the opening is tall enough for a light above.</summary>
    public static (float Bottom, float Top, float? Transom) Heights(DoorParams p, DoorStyle s)
    {
        if (s == DoorStyle.Stall) return (OpeningStyle.StallLift, p.Height, null);
        var clear = p.Height - FrameFace(s) - OpeningStyle.DoorHeadGap;
        var max = OpeningStyle.DoorLeafMaxHeight;
        if (clear > max + OpeningStyle.MullionFace + OpeningStyle.DoorTransomMinLight + OpeningStyle.DoorHeadGap)
            return (OpeningStyle.DoorFloorGap, max, max + OpeningStyle.DoorHeadGap);
        return (OpeningStyle.DoorFloorGap, clear, null);
    }

    public static List<LeafSlot> Leaves(DoorParams p, DoorStyle s)
    {
        // Leaves run 4 mm under the frame and meet edge to edge: sub-pixel gaps would stipple the outline.
        var side = s == DoorStyle.Stall ? 0f : -0.004f;
        var face = FrameFace(s);
        var x0 = -p.Width * 0.5f + face + side;
        var inner = p.Width - 2 * face - 2 * side;
        var n = p.Leaves;
        var w = inner / n;
        var slots = new List<LeafSlot>(n);
        const float overlap = 0.0005f;   // touching leaves interpenetrate slightly rather than share faces
        for (var i = 0; i < n; i++)
        {
            var a = x0 + i * w;
            slots.Add(new(i == 0 ? a : a - overlap, i == n - 1 ? a + w : a + w + overlap, i + 0.5f <= n * 0.5f, n <= 2 || (i != 0 && i != n - 1)));
        }
        return slots;
    }

    public static Box3 Extent(DoorParams p, DoorStyle s)
    {
        var e = OpeningStyle.PressIn;
        var t = Thickness(s);
        var z = Math.Max(p.Depth * 0.5f + FrameProud(s), t * 0.5f + Reach);
        var x = p.Width * 0.5f + e;
        var front = z;
        if (p.OpenAngleDeg > 0)
        {
            var widest = Leaves(p, s).Max(l => l.X1 - l.X0);
            front = Math.Max(front, widest + t + Reach + 0.02f);
            x += t + Reach;
        }
        return new(new(-x, 0, -front), new(x, p.Height + e, z));
    }

    public static void Validate(DoorParams p, DoorStyle s)
    {
        if (p.Leaves is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(p), "Leaves must be within 1–4.");
        if (!float.IsFinite(p.OpenAngleDeg) || p.OpenAngleDeg is < 0 or > 120) throw new ArgumentOutOfRangeException(nameof(p), "OpenAngleDeg must be within 0–120.");
        var (bottom, top, _) = Heights(p, s);
        if (top - bottom < 0.5f) throw new ArgumentOutOfRangeException(nameof(p), "The door is too low for its frame.");
        foreach (var leaf in Leaves(p, s))
            if (leaf.X1 - leaf.X0 < 0.25f) throw new ArgumentOutOfRangeException(nameof(p), "Too many leaves for the door width.");
    }

    public static void Frame(PartMesh mesh, DoorParams p, DoorStyle s, ToonSpec spec, LodLevel lod)
    {
        if (s == DoorStyle.Stall) return;
        var e = OpeningStyle.PressIn;
        var ins = OpeningStyle.Insert;
        var face = FrameFace(s);
        var d = p.Depth * 0.5f;
        var proud = FrameProud(s);
        var shadow = (byte)0;
        // Jambs, head and transom bar as one flush slab: the leaf opening is a notch in the outline (it meets the floor),
        // the transom light a hole.
        float x0 = -p.Width * 0.5f - e, x1 = p.Width * 0.5f + e, i0 = -p.Width * 0.5f + face, i1 = p.Width * 0.5f - face;
        var (_, _, transom) = Heights(p, s);
        var notchTop = transom ?? p.Height - face;
        var lightBottom = transom is { } t0 ? t0 + OpeningStyle.MullionFace : notchTop;
        float[] xs = [x0, i0, i1, x1];
        var ys = Solid.Breaks([0, notchTop, lightBottom, p.Height - face, p.Height + e]);
        Solid.Grid(mesh, spec, shadow, Place.Identity, xs, ys, (i, j) =>
        {
            float cx = (xs[i] + xs[i + 1]) * 0.5f, cy = (ys[j] + ys[j + 1]) * 0.5f;
            var inside = cx > i0 && cx < i1;
            return !(inside && cy < notchTop) && !(inside && cy > lightBottom && cy < p.Height - face);
        }, -d - proud, d + proud);
        if (Threshold(s))
            Solid.Box(mesh, OpeningStyle.Metal, shadow, i0 - ins, 0, -d + 0.005f, i1 + ins, OpeningStyle.ThresholdHeight, d - 0.005f);
        if (transom is not { } tb) return;
        var top = tb + OpeningStyle.MullionFace;
        var glassTop = p.Height - face;
        Solid.Glass(mesh, shadow, Place.Identity, -p.Width * 0.5f + face - 0.01f, top - 0.01f, -0.006f, p.Width * 0.5f - face + 0.01f,
            glassTop + 0.01f, 0.006f, OpeningStyle.DoorCard, DoorUv2);
        if (lod == LodLevel.L0)
            Solid.Ring(mesh, OpeningStyle.Metal, VertexFlag.NoShadow, -p.Width * 0.5f + face - 0.008f, top - 0.008f,
                p.Width * 0.5f - face + 0.008f, glassTop + 0.008f, OpeningStyle.BeadFace + 0.008f, -0.006f - OpeningStyle.BeadDepth, -0.006f + 0.002f);
    }

    /// <summary>Turn for a leaf: outward (−Z) about its hinge edge on the front face.</summary>
    public static Place Turn(LeafSlot slot, float openDeg, float thickness)
    {
        if (openDeg <= 0 || !slot.Swings) return Place.Identity;
        var radians = openDeg * Angles.DegreesToRadians;
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        var pivot = new Vector3(slot.HingeLeft ? slot.X0 : slot.X1, 0, -thickness * 0.5f);
        return Place.TurnY(cos, slot.HingeLeft ? sin : -sin, pivot);
    }

    public static void Leaf(PartMesh mesh, DoorStyle s, ToonSpec spec, Place turn, float a, float b, float y0, float y1, bool hingeLeft,
        LodLevel lod)
    {
        var t = Thickness(s) * 0.5f;
        var ins = OpeningStyle.Insert;
        const byte cast = 0;
        const byte small = VertexFlag.NoShadow;
        var full = lod == LodLevel.L0;
        var lockX = hingeLeft ? b : a;
        var toHinge = hingeLeft ? -1f : 1f;
        switch (s)
        {
            case DoorStyle.Entrance:
                {
                    // Stiles, rails and kick rail are one flush slab around the glass.
                    var st = OpeningStyle.DoorStile;
                    var kick = Math.Min(OpeningStyle.DoorKickRail, (y1 - y0) * 0.2f);
                    Solid.Grid(mesh, spec, cast, turn, [a, a + st, b - st, b], [y0, y0 + kick, y1 - st, y1], (i, j) => i != 1 || j != 1, -t, t);
                    Solid.Glass(mesh, cast, turn, a + st - 0.01f, y0 + kick - 0.01f, -0.006f, b - st + 0.01f, y1 - st + 0.01f, 0.006f,
                        OpeningStyle.DoorCard, DoorUv2);
                    if (full) PullPair(mesh, turn, lockX + toHinge * st * 0.5f, y0 + kick + 0.05f, y1 - st - 0.05f, t, OpeningStyle.PullLength);
                    break;
                }
            case DoorStyle.Glass:
                {
                    var rail = OpeningStyle.GlassDoorRail;
                    Solid.Glass(mesh, cast, turn, a, y0 + rail - 0.01f, -t, b, y1 - rail + 0.01f, t, OpeningStyle.DoorCard, DoorUv2);
                    Solid.Box(mesh, OpeningStyle.Metal, cast, turn, a, y0, -t - 0.007f, b, y0 + rail, t + 0.007f);
                    Solid.Box(mesh, OpeningStyle.Metal, cast, turn, a, y1 - rail, -t - 0.007f, b, y1, t + 0.007f);
                    if (full) PullPair(mesh, turn, lockX + toHinge * 0.1f, y0 + rail + 0.1f, y1 - rail - 0.1f, t, 1.2f);
                    break;
                }
            case DoorStyle.Wood:
            case DoorStyle.Fire:
                {
                    var vw = s == DoorStyle.Wood ? OpeningStyle.VisionWidth : OpeningStyle.VisionWidth * 0.75f;
                    var vh = s == DoorStyle.Wood ? OpeningStyle.VisionHeight : OpeningStyle.VisionHeight * 0.8f;
                    var vx1 = lockX + toHinge * 0.12f;
                    var vx0 = vx1 + toHinge * vw;
                    var vy1 = y1 - OpeningStyle.VisionTopGap;
                    var vy0 = vy1 - vh;
                    float hx0 = Math.Min(vx0, vx1), hx1 = Math.Max(vx0, vx1);
                    var vision = full && vy0 > y0 + 0.9f;
                    if (vision)
                    {
                        var c = (a + b) * 0.5f;
                        IReadOnlyList<Vector2> hole = [new(hx0 - c, vy0 - y0), new(hx1 - c, vy0 - y0), new(hx1 - c, vy1 - y0), new(hx0 - c, vy1 - y0)];
                        Solid.Panel(mesh, spec, cast, turn, a, y0, b, y1, 0, 2 * t, [hole]);
                        // A flush bead filling the hole edge: a material line around the glass, no thin step to stipple.
                        const float bead = 0.018f;
                        Solid.Ring(mesh, OpeningStyle.Metal, small, hx0, vy0, hx1, vy1, bead, -t, t, 0f, turn);
                        Solid.Glass(mesh, cast, turn, hx0 + bead - 0.004f, vy0 + bead - 0.004f, -0.004f, hx1 - bead + 0.004f, vy1 - bead + 0.004f, 0.004f,
                            OpeningStyle.DoorCard, DoorUv2);
                    }
                    else
                    {
                        Solid.Box(mesh, spec, cast, turn, a, y0, -t, b, y1, t);
                    }
                    if (!full) break;
                    var lx = lockX + toHinge * 0.065f;
                    if (s == DoorStyle.Fire)
                    {
                        PanicBar(mesh, turn, a, b, t);
                        Lever(mesh, turn, lx, OpeningStyle.LeverCentreY, -t, -1, toHinge);
                    }
                    else
                    {
                        Lever(mesh, turn, lx, OpeningStyle.LeverCentreY, -t, -1, toHinge);
                        Lever(mesh, turn, lx, OpeningStyle.LeverCentreY, t, 1, toHinge);
                    }
                    break;
                }
            case DoorStyle.Screen:
                {
                    Solid.Box(mesh, spec, cast, turn, a, y0, -t, b, y1, t);
                    if (!full) break;
                    var inset = OpeningStyle.ScreenPanelInset;
                    var proud = OpeningStyle.ScreenPanelProud;
                    Solid.Ring(mesh, spec, small, a + inset, y0 + inset, b - inset, y1 - inset, 0.035f, -t - proud, -t + ins, 0f, turn);
                    Solid.Ring(mesh, spec, small, a + inset, y0 + inset, b - inset, y1 - inset, 0.035f, t - ins, t + proud, 0f, turn);
                    var hingeX = hingeLeft ? a : b;
                    foreach (var hy in new[] { y0 + 0.28f, (y0 + y1) * 0.5f, y1 - 0.28f })
                        Solid.Box(mesh, OpeningStyle.DarkMetal, small, turn, hingeX - 0.017f, hy - 0.07f, -t - 0.012f, hingeX + 0.017f, hy + 0.07f, -t + 0.02f);
                    var lx = lockX + toHinge * 0.08f;
                    Lever(mesh, turn, lx, OpeningStyle.LeverCentreY, -t - proud, -1, toHinge);
                    Lever(mesh, turn, lx, OpeningStyle.LeverCentreY, t + proud, 1, toHinge);
                    break;
                }
            case DoorStyle.Stall:
                {
                    Solid.Box(mesh, spec, cast, turn, a, y0, -t, b, y1, t);
                    if (!full) break;
                    var lx = lockX + toHinge * 0.05f;
                    Solid.Box(mesh, OpeningStyle.Metal, small, turn, lx - 0.025f, 0.985f, -t - 0.012f, lx + 0.025f, 1.015f, -t + ins);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown door style.");
        }
    }

    /// <summary>Dark astragal over a meeting line, on both faces, carried by the leaf on the +X side.</summary>
    public static void Seam(PartMesh mesh, DoorStyle s, Place turn, float x, float y0, float y1)
    {
        var t = Thickness(s) * 0.5f;
        var w = OpeningStyle.SeamWidth * 0.5f;
        var ins = OpeningStyle.Insert;
        Solid.Box(mesh, OpeningStyle.Gasket, VertexFlag.NoShadow, turn, x - w, y0 + 0.01f, -t - OpeningStyle.SeamProud, x + w, y1 - 0.01f, -t + ins);
        Solid.Box(mesh, OpeningStyle.Gasket, VertexFlag.NoShadow, turn, x - w, y0 + 0.01f, t - ins, x + w, y1 - 0.01f, t + OpeningStyle.SeamProud);
    }

    /// <summary>Vertical pull bars on both faces, on two stand-offs each.</summary>
    private static void PullPair(PartMesh mesh, Place turn, float x, float yMin, float yMax, float t, float length)
    {
        const byte flags = VertexFlag.NoShadow;
        var ins = OpeningStyle.Insert;
        var cy = Math.Clamp(OpeningStyle.PullCentreY, yMin + length * 0.5f, Math.Max(yMin + length * 0.5f, yMax - length * 0.5f));
        var half = Math.Min(length, yMax - yMin) * 0.5f;
        var bar = OpeningStyle.PullBar * 0.5f;
        var off = OpeningStyle.PullStandOff;
        foreach (var side in new[] { -1f, 1f })
        {
            float Z(float outward) => side * (t + outward);
            void Span(float x0, float y0, float x1, float y1, float o0, float o1)
            {
                float za = Z(o0), zb = Z(o1);
                Solid.Box(mesh, OpeningStyle.Metal, flags, turn, x0, y0, Math.Min(za, zb), x1, y1, Math.Max(za, zb));
            }
            Span(x - bar, cy - half, x + bar, cy + half, off, off + 2 * bar);
            foreach (var sy in new[] { cy - half + 0.06f, cy + half - 0.08f })
                Span(x - 0.009f, sy, x + 0.009f, sy + 0.02f, -ins, off + ins);
        }
    }

    /// <summary>Horizontal lever on a back plate; <paramref name="side"/> −1 is the front face, +1 the back face.</summary>
    private static void Lever(PartMesh mesh, Place turn, float x, float y, float face, float side, float toHinge)
    {
        const byte flags = VertexFlag.NoShadow;
        var ins = OpeningStyle.Insert;
        float Z(float outward) => face + side * outward;
        void Span(float x0, float y0, float x1, float y1, float o0, float o1)
        {
            float za = Z(o0), zb = Z(o1);
            Solid.Box(mesh, OpeningStyle.Metal, flags, turn, Math.Min(x0, x1), y0, Math.Min(za, zb), Math.Max(x0, x1), y1, Math.Max(za, zb));
        }
        Span(x - 0.022f, y - 0.11f, x + 0.022f, y + 0.05f, -ins, 0.008f);
        Span(x - 0.007f, y + 0.02f, x + 0.007f, y + 0.034f, 0.008f - ins, 0.05f + ins);
        Span(x, y + 0.018f, x + toHinge * 0.125f, y + 0.036f, 0.05f, 0.068f);
    }

    /// <summary>Push bar across the leaf on its back (+Z) face, with end brackets.</summary>
    private static void PanicBar(PartMesh mesh, Place turn, float a, float b, float t)
    {
        const byte flags = VertexFlag.NoShadow;
        var y = OpeningStyle.PanicBarY;
        var s = OpeningStyle.PanicBarSize * 0.5f;
        var off = OpeningStyle.PanicStandOff;
        Solid.Box(mesh, OpeningStyle.Metal, flags, turn, a + 0.09f, y - s, t + off, b - 0.09f, y + s, t + off + 2 * s);
        foreach (var x in new[] { a + 0.07f, b - 0.11f })
            Solid.Box(mesh, OpeningStyle.DarkMetal, flags, turn, x, y - s * 1.6f, t - OpeningStyle.Insert, x + 0.04f, y + s * 1.6f, t + off + 2 * s);
    }
}

/// <summary>Shared plumbing of the door parts: frame plus leaves, closed collision as DoorClosed.</summary>
public abstract class DoorPart(PartBudget budget) : OpeningPart<DoorParams>(budget)
{
    private protected abstract DoorStyle Style { get; }
    protected override Box3 Extent(DoorParams p) => DoorBuilder.Extent(p, Style);
    protected override void Validate(DoorParams p) => DoorBuilder.Validate(p, Style);

    private protected override void Build(PartMesh mesh, DoorParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        DoorBuilder.Frame(mesh, p, Style, DoorBuilder.FrameSpec(Style), lod);
        var leafSpec = Pick(p, DoorBuilder.LeafSpec(Style));
        var (bottom, top, _) = DoorBuilder.Heights(p, Style);
        var slots = DoorBuilder.Leaves(p, Style);
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var turn = DoorBuilder.Turn(slot, p.OpenAngleDeg, DoorBuilder.Thickness(Style));
            DoorBuilder.Leaf(mesh, Style, leafSpec, turn, slot.X0, slot.X1, bottom, top, slot.HingeLeft, lod);
            if (i > 0 && lod == LodLevel.L0) DoorBuilder.Seam(mesh, Style, turn, slot.X0, bottom, top);
        }
    }

    /// <summary>Shut doors are static closures (WLD-INTERIORS: DoorClosed); open leaves carry no collision.</summary>
    public override IReadOnlyList<Collider> Collision(DoorParams p)
    {
        _ = Bounds(p);
        if (p.OpenAngleDeg >= 2) return [];
        var at = Matrix4x4.Identity;
        at.M42 = p.Height * 0.5f;
        var foot = p.Foot == FootMat.Auto ? DoorBuilder.Foot(Style) : p.Foot;
        return [new BoxCollider(at, new(p.Width * 0.5f, p.Height * 0.5f, Math.Max(p.Depth * 0.5f, 0.05f)), SurfaceTag.DoorClosed) { Foot = foot }];
    }
}

/// <summary>门（出入口）：aluminium glass leaves with kick rails and pull bars; transom light above tall openings.</summary>
[Part(PartCatalog.Door)]
public sealed class EntranceDoorPart() : DoorPart(new(700, 280))
{
    private protected override DoorStyle Style => DoorStyle.Entrance;
}

/// <summary>木门：flush leaf with a lock-side vision panel, lever handles, painted casing proud of both wall faces.</summary>
[Part(PartCatalog.DoorWood)]
public sealed class WoodDoorPart() : DoorPart(new(500, 120))
{
    private protected override DoorStyle Style => DoorStyle.Wood;
}

/// <summary>防火门：painted steel leaf, narrow vision panel, push bar on the back face.</summary>
[Part(PartCatalog.DoorFire)]
public sealed class FireDoorPart() : DoorPart(new(560, 120))
{
    private protected override DoorStyle Style => DoorStyle.Fire;
}

/// <summary>玻璃地弹门：frameless glass leaves between metal rails, long pull bars.</summary>
[Part(PartCatalog.DoorGlass)]
public sealed class GlassDoorPart() : DoorPart(new(700, 280))
{
    private protected override DoorStyle Style => DoorStyle.Glass;
}

/// <summary>屏蔽门：heavy steel leaf with raised panels and visible hinges in a deep frame.</summary>
[Part(PartCatalog.DoorScreen)]
public sealed class ScreenDoorPart() : DoorPart(new(700, 120))
{
    private protected override DoorStyle Style => DoorStyle.Screen;
}

/// <summary>隔间门：laminate partition leaf lifted off the floor, occupancy latch.</summary>
[Part(PartCatalog.DoorStall)]
public sealed class StallDoorPart() : DoorPart(new(120, 40))
{
    private protected override DoorStyle Style => DoorStyle.Stall;
}
