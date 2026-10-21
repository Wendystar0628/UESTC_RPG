using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>Infill of a guard railing between its bottom and top rails (BoxPartParams.Variation 0–2).</summary>
internal enum Infill { Balusters, Panel, Bars }

/// <summary>Railing and handrail builders shared by every game/gen/parts/circulation/ part (ART-MODELING "围墙栏杆").</summary>
internal static class RailKit
{
    /// <summary>
    /// Straight guard railing along local X from x0 to x1, centred on z, standing on baseY with its top at top.
    /// Posts carry an L0 bevel; the top rail is a chamfered box whose ends close the run; balusters and bars cast no shadow.
    /// </summary>
    internal static void Guard(MeshBuilder mesh, ToonSpec main, float x0, float x1, float z, float baseY, float top, float railWidth,
        float postSpacing, Infill infill, Detail detail, Frame frame, bool startPost = true)
    {
        var fine = main with { Line = LineClass.Fine };
        var post = Math.Min(S.PostSize, railWidth);
        var railBottom = top - S.TopRailHeight;
        if (!(railBottom - baseY > S.BottomRailClearance + S.BottomRailHeight + 0.1f)) throw new ArgumentException("A guard railing is too low for its rails.");
        var bevel = detail == Detail.Full;
        if (detail == Detail.Mass)
        {
            mesh.Box(main, new(x0, railBottom, z - railWidth * 0.5f), new(x1, top, z + railWidth * 0.5f), 0, frame);
            return;
        }
        mesh.Box(main, new(x0, railBottom, z - railWidth * 0.5f), new(x1, top, z + railWidth * 0.5f), bevel ? S.RailBevel : 0, frame);
        var posts = Math.Max(2, (int)Math.Ceiling((x1 - x0 - post) / postSpacing - 1e-4) + 1);
        var centres = new float[posts];
        for (var i = 0; i < posts; i++)
        {
            centres[i] = x0 + post * 0.5f + (x1 - x0 - post) * i / (posts - 1);
            // A run that starts against another run's corner post shares that post.
            if (i == 0 && !startPost) continue;
            mesh.Box(main, new(centres[i] - post * 0.5f, baseY, z - post * 0.5f), new(centres[i] + post * 0.5f, railBottom + S.Joint, z + post * 0.5f),
                bevel ? S.PostBevel : 0, frame);
        }
        if (detail == Detail.Coarse) return;
        // Without its own start post the bottom rail runs on into the neighbouring corner post.
        var inner0 = startPost ? x0 + post * 0.5f : x0 - post * 0.5f; var inner1 = x1 - post * 0.5f;
        var lowY = baseY + S.BottomRailClearance; var lowTop = lowY + S.BottomRailHeight;
        var lowWidth = Math.Min(S.BottomRailHeight, railWidth);
        mesh.Box(main, new(inner0, lowY, z - lowWidth * 0.5f), new(inner1, lowTop, z + lowWidth * 0.5f), 0, frame);
        switch (infill)
        {
            case Infill.Balusters:
                var size = S.BalusterSize;
                var count = (int)Math.Floor((x1 - x0) / S.BalusterPitch);
                var start = x0 + ((x1 - x0) - (count - 1) * S.BalusterPitch) * 0.5f;
                for (var i = 0; i < count; i++)
                {
                    var x = start + i * S.BalusterPitch;
                    if (centres.Any(c => Math.Abs(c - x) < (post + size) * 0.5f + 0.02f)) continue;
                    mesh.Box(fine, new(x - size * 0.5f, lowTop - S.Joint, z - size * 0.5f), new(x + size * 0.5f, railBottom + S.Joint, z + size * 0.5f), 0, frame, VertexFlag.NoShadow);
                }
                break;
            case Infill.Panel:
                var t = Math.Min(S.PanelThickness, railWidth * 0.5f);
                mesh.Box(main, new(inner0, lowTop - S.Joint, z - t * 0.5f), new(inner1, railBottom + S.Joint, z + t * 0.5f), 0, frame);
                break;
            case Infill.Bars:
                var bar = Math.Min(0.03f, railWidth * 0.5f);
                for (var i = 1; i <= S.HorizontalBars; i++)
                {
                    var y = lowTop + (railBottom - lowTop) * i / (S.HorizontalBars + 1);
                    mesh.Box(fine, new(inner0, y - bar * 0.5f, z - bar * 0.5f), new(inner1, y + bar * 0.5f, z + bar * 0.5f), 0, frame, VertexFlag.NoShadow);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(infill), infill, "Unknown railing infill.");
        }
    }

    internal static Infill InfillOf(int variation) => variation switch
    {
        0 => Infill.Balusters,
        1 => Infill.Panel,
        2 => Infill.Bars,
        _ => throw new ArgumentOutOfRangeException(nameof(variation), variation, "Railing variation must be 0–2."),
    };

    /// <summary>
    /// Round handrail along a (z, y) centreline in the vertical plane through x, carried by posts whose feet follow
    /// <paramref name="footAt"/>. The path must be monotonic in z. Coarse levels use a square bar and no posts.
    /// </summary>
    internal static void Handrail(MeshBuilder mesh, IReadOnlyList<Vector2> path, float x, Func<float, float> footAt, Detail detail, Frame frame,
        bool posts = true, bool capFirst = true, bool capLast = true)
    {
        if (detail == Detail.Mass) return;
        var profile = detail switch
        {
            Detail.Full => Profiles.Circle(S.HandrailRadius, S.HandrailSegments),
            Detail.Plain => Profiles.Circle(S.HandrailRadius, 8),
            _ => Profiles.Rect(-S.HandrailRadius, S.HandrailRadius, -S.HandrailRadius, S.HandrailRadius, 0),
        };
        // Handrails are thin linear items: they stay out of the shadow pass, where they would only speckle (ART-MODELING).
        mesh.VerticalSweep(CirculationStyle.MetalTube, profile, path, x, frame, VertexFlag.NoShadow);
        if (detail == Detail.Coarse) return;
        // Round end knobs instead of bare tube cuts (ART-MODELING "端头圆帽").
        var knob = new Vector3(S.HandrailKnob, S.HandrailKnob, S.HandrailKnob);
        var (rings, segments) = detail == Detail.Full ? (3, 8) : (3, 6);
        if (capFirst) mesh.Ellipsoid(CirculationStyle.MetalTube, new(x, path[0].Y, path[0].X), knob, rings, segments, frame, VertexFlag.NoShadow);
        if (capLast) mesh.Ellipsoid(CirculationStyle.MetalTube, new(x, path[^1].Y, path[^1].X), knob, rings, segments, frame, VertexFlag.NoShadow);
        if (!posts) return;
        var z0 = path[0].X; var z1 = path[^1].X;
        var length = Math.Abs(z1 - z0);
        var inset = Math.Min(0.12f, length * 0.25f);
        var count = Math.Max(2, (int)Math.Ceiling((length - 2 * inset) / S.HandrailPostPitch - 1e-4) + 1);
        var size = S.HandrailPostSize; var bevel = detail == Detail.Full ? 0.008f : 0;
        for (var i = 0; i < count; i++)
        {
            var z = z0 + Math.Sign(z1 - z0) * (inset + (length - 2 * inset) * i / (count - 1));
            var foot = footAt(z) - S.Joint;
            var head = HeightAt(path, z);
            if (!(head - foot > 0.2f)) continue;
            mesh.Box(CirculationStyle.Metal, new(x - size * 0.5f, foot, z - size * 0.5f), new(x + size * 0.5f, head, z + size * 0.5f), bevel, frame, VertexFlag.NoShadow);
        }
    }

    /// <summary>Vertical balusters between a mounting line and a handrail path, one per given z (indoor balustrades).</summary>
    internal static void Balusters(MeshBuilder mesh, IReadOnlyList<Vector2> rail, float x, IEnumerable<(float Z, float Foot)> stations, Detail detail, Frame frame)
    {
        if (detail is Detail.Coarse or Detail.Mass) return;
        var size = S.BalusterSize * 0.75f;
        foreach (var (z, foot) in stations)
        {
            var head = HeightAt(rail, z);
            if (!(head - foot > 0.2f)) continue;
            mesh.Box(CirculationStyle.MetalFine, new(x - size * 0.5f, foot - S.Joint, z - size * 0.5f), new(x + size * 0.5f, head, z + size * 0.5f), 0, frame, VertexFlag.NoShadow);
        }
    }

    /// <summary>Linear interpolation of a z-monotonic (z, y) polyline.</summary>
    internal static float HeightAt(IReadOnlyList<Vector2> path, float z)
    {
        var ascending = path[^1].X > path[0].X;
        for (var i = 0; i + 1 < path.Count; i++)
        {
            var a = path[i]; var b = path[i + 1];
            var inside = ascending ? z >= a.X && z <= b.X : z <= a.X && z >= b.X;
            if (!inside) continue;
            var t = (z - a.X) / (b.X - a.X);
            return a.Y + (b.Y - a.Y) * t;
        }
        return ascending == z < path[0].X ? path[0].Y : path[^1].Y;
    }
}
