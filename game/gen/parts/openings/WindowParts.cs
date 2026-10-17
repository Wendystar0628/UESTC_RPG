using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

internal enum WindowType { Casement, Ribbon, Slit, High, Observation, Service }

internal enum LightKind { Fixed, SideHung, TopHung, Sliding }

internal readonly record struct Light(float X0, float Y0, float X1, float Y1, LightKind Kind, bool HandleRight);

/// <summary>
/// Frame, mullions, sashes, beads, gaskets and handles of a glazed opening (ART-FIDELITY; ART-MODELING).
/// Glass, reveal, sill and head stay in the wall block with the window number; this is the per-building
/// MultiMesh part. L0: two-level section, gaskets and handles. L1: one simplified frame ring with mullions.
/// L2 and further: nothing (the building draws flat panes).
/// </summary>
internal static class WindowBuilder
{
    private const float Lap = 0.008f;   // sash and bead edges run under the frame

    public static float Face(WindowType type) => type switch
    {
        WindowType.Slit => OpeningStyle.SlitFrameFace,
        WindowType.Observation => OpeningStyle.FrameFace * 0.7f,
        WindowType.Casement or WindowType.Ribbon or WindowType.High or WindowType.Service => OpeningStyle.FrameFace,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown window type."),
    };

    private static float Mullion(WindowType type) => type == WindowType.Observation ? OpeningStyle.MullionFace * 0.7f : OpeningStyle.MullionFace;

    /// <summary>Frame depth: the part depth, except the service hatch whose depth is its counter.</summary>
    public static float FrameDepth(WindowParams p, WindowType type) => type == WindowType.Service ? Math.Min(p.Depth, 0.08f) : p.Depth;

    public static float HandleReach => OpeningStyle.RoseDepth + OpeningStyle.LeverStandOff + OpeningStyle.LeverDepth - OpeningStyle.SashStep;

    public static Box3 Extent(WindowParams p, WindowType type)
    {
        var e = OpeningStyle.PressIn;
        var d = FrameDepth(p, type) * 0.5f;
        var handles = Layout(p, type).Any(l => l.Kind is LightKind.SideHung or LightKind.TopHung);
        var reach = handles ? HandleReach + 0.002f : 0f;
        var front = d + Math.Max(type == WindowType.Ribbon ? OpeningStyle.FinProjection : 0f, OpeningStyle.HandleOnExterior ? reach : 0f) + 0.006f;
        var back = d + (OpeningStyle.HandleOnExterior ? 0f : reach);
        var x = p.Width * 0.5f + e;
        var y0 = -e;
        if (type == WindowType.Service)
        {
            x += 0.06f;
            y0 = -0.04f;
            front = Math.Max(front, p.Depth * 0.5f);
            back = Math.Max(back, p.Depth * 0.5f);
        }
        return new(new(-x, y0, -front), new(x, p.Height + e, back));
    }

    public static void Validate(WindowParams p, WindowType type)
    {
        if (p.Columns is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(p), "Columns must be within 1–12.");
        if (p.Rows is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(p), "Rows must be within 1–8.");
        foreach (var light in Layout(p, type))
            if (light.X1 - light.X0 < 0.1f || light.Y1 - light.Y0 < 0.1f)
                throw new ArgumentOutOfRangeException(nameof(p), "Too many lights for the window size.");
    }

    /// <summary>Clear lights inside the frame, bottom row first; mullions are continuous, transoms run between them.</summary>
    public static List<Light> Layout(WindowParams p, WindowType type)
    {
        var face = Face(type);
        var mull = Mullion(type);
        int cols = p.Columns, rows = p.Rows;
        var innerW = p.Width - 2 * face - (cols - 1) * mull;
        var innerH = p.Height - 2 * face - (rows - 1) * mull;
        var transomRow = rows >= 2 && type is WindowType.Casement or WindowType.Ribbon;
        var top = transomRow ? Math.Clamp(p.Height * OpeningStyle.TransomShare, OpeningStyle.TransomMin, OpeningStyle.TransomMax) : 0f;
        top = Math.Min(top, innerH * 0.5f);
        var rowH = transomRow ? (innerH - top) / (rows - 1) : innerH / rows;
        var colW = innerW / cols;
        var lights = new List<Light>(cols * rows);
        var y = face;
        for (var r = 0; r < rows; r++)
        {
            var h = transomRow && r == rows - 1 ? top : rowH;
            for (var c = 0; c < cols; c++)
            {
                var x = -p.Width * 0.5f + face + c * (colW + mull);
                var kind = Kind(type, c, r, rows);
                lights.Add(new(x, y, x + colW, y + h, kind, c + 0.5f < cols * 0.5f));
            }
            y += h + mull;
        }
        return lights;
    }

    private static LightKind Kind(WindowType type, int col, int row, int rows) => type switch
    {
        WindowType.Casement => rows >= 2 && row == rows - 1 ? LightKind.Fixed : col % 2 == 0 ? LightKind.SideHung : LightKind.Fixed,
        WindowType.Ribbon => rows >= 2 && row == rows - 1 ? LightKind.Fixed : LightKind.Sliding,
        WindowType.Slit => rows >= 2 && row == rows / 2 ? LightKind.TopHung : LightKind.Fixed,
        WindowType.High => LightKind.TopHung,
        WindowType.Observation => LightKind.Fixed,
        WindowType.Service => LightKind.Sliding,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown window type."),
    };

    public static void Build(PartMesh mesh, WindowParams p, WindowType type, ToonSpec frameSpec, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        const byte flags = OpeningPart<WindowParams>.NoShadow;
        var e = OpeningStyle.PressIn;
        var ins = OpeningStyle.Insert;
        var d = FrameDepth(p, type) * 0.5f;
        var lights = Layout(p, type);

        // Frame, mullions and transoms: one flush slab with a hole per light, its outer edge pressed into the
        // reveal. Watertight and without the 2–4 mm steps that stipple the outline at distance.
        // L1 keeps a single simplified ring (ART-MODELING).
        if (lod == LodLevel.L1)
            Solid.Ring(mesh, frameSpec, flags, -p.Width * 0.5f - e, -e, p.Width * 0.5f + e, p.Height + e, Face(type) + e, -d, d);
        else
        {
            var xs = Solid.Breaks(lights.SelectMany(l => new[] { l.X0, l.X1 }).Append(-p.Width * 0.5f - e).Append(p.Width * 0.5f + e));
            var ys = Solid.Breaks(lights.SelectMany(l => new[] { l.Y0, l.Y1 }).Append(-e).Append(p.Height + e));
            Solid.Grid(mesh, frameSpec, flags, Place.Identity, xs, ys, (i, j) =>
            {
                float cx = (xs[i] + xs[i + 1]) * 0.5f, cy = (ys[j] + ys[j + 1]) * 0.5f;
                return !lights.Any(l => cx > l.X0 && cx < l.X1 && cy > l.Y0 && cy < l.Y1);
            }, -d, d);
        }
        if (type == WindowType.Ribbon)
        {
            var columns = lights.Where(l => l.Y0 == lights[0].Y0).OrderBy(l => l.X0).ToArray();
            for (var c = 1; c < columns.Length; c++)
            {
                var mid = (columns[c - 1].X1 + columns[c].X0) * 0.5f;
                Solid.Box(mesh, frameSpec, flags, mid - OpeningStyle.FinFace * 0.5f, 0f, -d - OpeningStyle.FinProjection,
                    mid + OpeningStyle.FinFace * 0.5f, p.Height, -d + ins);
            }
        }
        if (lod != LodLevel.L0) return;

        // Sashes and beads differ from the frame by the anisotropic band only: a separate material ID, so the
        // frame–sash boundary is a stable material line rather than a thin step face.
        var sash = frameSpec with { Fx = frameSpec.Fx ^ ToonFx.AnisoBand };
        foreach (var light in lights)
            switch (light.Kind)
            {
                case LightKind.Fixed: Bead(mesh, sash, light, d); break;
                case LightKind.SideHung:
                case LightKind.TopHung: Sash(mesh, sash, light, d); break;
                case LightKind.Sliding: Sliding(mesh, sash, light, d); break;
                default: throw new InvalidOperationException("Unknown light kind.");
            }
    }

    /// <summary>Fixed light: a glazing bead one level behind the frame face, then the dark gasket line.</summary>
    private static void Bead(PartMesh mesh, ToonSpec spec, Light l, float d)
    {
        const byte flags = OpeningPart<WindowParams>.NoShadow;
        var front = Math.Max(-OpeningStyle.BeadDepth, -d + OpeningStyle.SashStep) - OpeningStyle.GlassClear;
        Solid.Ring(mesh, spec, flags, l.X0 - Lap, l.Y0 - Lap, l.X1 + Lap, l.Y1 + Lap, OpeningStyle.BeadFace + Lap,
            front, -OpeningStyle.GlassClear);
        Gasket(mesh, l.X0 + OpeningStyle.BeadFace, l.Y0 + OpeningStyle.BeadFace, l.X1 - OpeningStyle.BeadFace, l.Y1 - OpeningStyle.BeadFace);
    }

    /// <summary>Opening sash: its face one step behind the frame, straddling the glass plane, with a lever handle.</summary>
    private static void Sash(PartMesh mesh, ToonSpec spec, Light l, float d)
    {
        const byte flags = OpeningPart<WindowParams>.NoShadow;
        var front = -d + OpeningStyle.SashStep;
        var back = Math.Min(front + OpeningStyle.SashDepth, d - 0.002f);
        if (back - front < 0.01f) { Bead(mesh, spec, l, d); return; }
        var sf = OpeningStyle.SashFace;
        Solid.Ring(mesh, spec, flags, l.X0 - Lap, l.Y0 - Lap, l.X1 + Lap, l.Y1 + Lap, sf + Lap, front, back);
        Gasket(mesh, l.X0 + sf, l.Y0 + sf, l.X1 - sf, l.Y1 - sf);
        float hx, hy;
        if (l.Kind == LightKind.TopHung) { hx = (l.X0 + l.X1) * 0.5f; hy = l.Y1 - sf * 0.5f; }
        else { hx = l.HandleRight ? l.X1 - sf * 0.5f : l.X0 + sf * 0.5f; hy = (l.Y0 + l.Y1) * 0.5f; }
        Handle(mesh, spec, hx, hy, front, back, Math.Min(OpeningStyle.LeverLength, (l.Y1 - l.Y0) * 0.4f));
    }

    /// <summary>Sliding pair: front-track sash on the left, back-track sash on the right, overlapping at the meeting stiles.</summary>
    private static void Sliding(PartMesh mesh, ToonSpec spec, Light l, float d)
    {
        const byte flags = OpeningPart<WindowParams>.NoShadow;
        var sf = OpeningStyle.SashFace;
        var mid = (l.X0 + l.X1) * 0.5f;
        var lap = OpeningStyle.SlideOverlap * 0.5f;
        var aFront = -d + 0.006f;
        var aBack = aFront + OpeningStyle.SlideSashDepth;
        var bFront = aBack + 0.002f;
        var bBack = Math.Min(bFront + OpeningStyle.SlideSashDepth, d - 0.002f);
        if (bBack - bFront < 0.008f || aBack > -OpeningStyle.GlassClear) { Bead(mesh, spec, l, d); return; }
        Solid.Ring(mesh, spec, flags, l.X0 - Lap, l.Y0 - Lap, mid + lap, l.Y1 + Lap, sf + Lap, aFront, aBack);
        Solid.Ring(mesh, spec, flags, mid - lap, l.Y0 - Lap, l.X1 + Lap, l.Y1 + Lap, sf + Lap, bFront, bBack);
        Gasket(mesh, l.X0 + sf, l.Y0 + sf, mid + lap - sf - Lap, l.Y1 - sf);
        Gasket(mesh, mid - lap + sf + Lap, l.Y0 + sf, l.X1 - sf, l.Y1 - sf);
        // Crescent latch on the front meeting stile.
        var lx = mid + lap - (sf + Lap) * 0.5f;
        var ly = (l.Y0 + l.Y1) * 0.5f;
        Solid.Box(mesh, spec, flags, lx - OpeningStyle.LatchWidth * 0.5f, ly - OpeningStyle.LatchHeight * 0.5f, aFront - OpeningStyle.LatchDepth,
            lx + OpeningStyle.LatchWidth * 0.5f, ly + OpeningStyle.LatchHeight * 0.5f, aFront + OpeningStyle.Insert);
    }

    private static void Gasket(PartMesh mesh, float x0, float y0, float x1, float y1)
    {
        const float under = 0.002f;
        if (x1 - x0 < 0.05f || y1 - y0 < 0.05f) return;
        Solid.Ring(mesh, OpeningStyle.Gasket, OpeningPart<WindowParams>.NoShadow, x0 - under, y0 - under, x1 + under, y1 + under,
            OpeningStyle.GasketWidth + under, -OpeningStyle.GasketDepth - OpeningStyle.GlassClear, -OpeningStyle.GlassClear);
    }

    /// <summary>Lever handle: rose on the sash face, short spindle, lever hanging down (closed position).</summary>
    internal static void Handle(PartMesh mesh, ToonSpec spec, float hx, float hy, float faceFront, float faceBack, float lever)
    {
        const byte flags = OpeningPart<WindowParams>.NoShadow;
        var ins = OpeningStyle.Insert;
        float rw = OpeningStyle.RoseWidth * 0.5f, rh = OpeningStyle.RoseHeight * 0.5f;
        var top = hy + rh - 0.005f;
        // Depth bands measured outward from the face; mirrored onto the back face for interior handles.
        var outside = OpeningStyle.HandleOnExterior;
        float Z(float outward) => outside ? faceFront - outward : faceBack + outward;
        void Slab(float x0, float y0, float x1, float y1, float out0, float out1)
        {
            float a = Z(out0), b = Z(out1);
            Solid.Box(mesh, spec, flags, x0, y0, Math.Min(a, b), x1, y1, Math.Max(a, b));
        }
        var rose = OpeningStyle.RoseDepth;
        var stand = OpeningStyle.LeverStandOff;
        Slab(hx - rw, hy - rh, hx + rw, hy + rh, -ins, rose);
        Slab(hx - 0.006f, top - 0.016f, hx + 0.006f, top - 0.004f, rose - ins, rose + stand + ins);
        Slab(hx - OpeningStyle.LeverWidth * 0.5f, top - lever, hx + OpeningStyle.LeverWidth * 0.5f, top, rose + stand, rose + stand + OpeningStyle.LeverDepth);
    }
}

/// <summary>Shared plumbing of the six window parts.</summary>
public abstract class WindowPart(PartBudget budget) : OpeningPart<WindowParams>(budget)
{
    private protected abstract WindowType Type { get; }
    protected override Box3 Extent(WindowParams p) => WindowBuilder.Extent(p, Type);
    protected override void Validate(WindowParams p) => WindowBuilder.Validate(p, Type);
    private protected override void Build(PartMesh mesh, WindowParams p, LodLevel lod)
    {
        WindowBuilder.Build(mesh, p, Type, Pick(p, OpeningStyle.Metal), lod);
        if (Type == WindowType.Service && lod <= LodLevel.L3)
        {
            // Counter shelf through the hatch: a closure of the wall, so it casts shadow and stays to L3.
            var bevel = Bevelled(p, lod) ? OpeningStyle.LargeBevel : 0f;
            Solid.Box(mesh, Hard(OpeningStyle.Counter), 0, -p.Width * 0.5f - 0.06f, -0.04f, -p.Depth * 0.5f,
                p.Width * 0.5f + 0.06f, OpeningStyle.Insert, p.Depth * 0.5f, bevel);
        }
    }
}

/// <summary>平开窗：side-hung sash in the even columns, fixed lights between, a fixed transom light when Rows ≥ 2.</summary>
[Part(PartCatalog.WindowCasement, Owner = "WP-10")]
public sealed class CasementWindowPart() : WindowPart(new(480, 40))
{
    private protected override WindowType Type => WindowType.Casement;
}

/// <summary>带形窗：sliding pairs between fin mullions; a fixed top row when Rows ≥ 2.</summary>
[Part(PartCatalog.WindowRibbon, Owner = "WP-10")]
public sealed class RibbonWindowPart() : WindowPart(new(640, 60))
{
    private protected override WindowType Type => WindowType.Ribbon;
}

/// <summary>竖缝窗：deep, wide-faced frame; a top-hung sash in the middle light when Rows ≥ 2.</summary>
[Part(PartCatalog.WindowSlit, Owner = "WP-10")]
public sealed class SlitWindowPart() : WindowPart(new(400, 40))
{
    private protected override WindowType Type => WindowType.Slit;
}

/// <summary>走廊侧高窗：top-hung lights.</summary>
[Part(PartCatalog.WindowHigh, Owner = "WP-10")]
public sealed class HighWindowPart() : WindowPart(new(440, 40))
{
    private protected override WindowType Type => WindowType.High;
}

/// <summary>观察窗：fixed lights in a slim frame.</summary>
[Part(PartCatalog.WindowObservation, Owner = "WP-10")]
public sealed class ObservationWindowPart() : WindowPart(new(240, 40))
{
    private protected override WindowType Type => WindowType.Observation;
}

/// <summary>服务窗口：sliding pair over a counter shelf that runs through the wall.</summary>
[Part(PartCatalog.WindowService, Owner = "WP-10")]
public sealed class ServiceWindowPart() : WindowPart(new(460, 60))
{
    private protected override WindowType Type => WindowType.Service;
}
