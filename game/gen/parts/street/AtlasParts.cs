using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Atlas kiosk (导览亭, SYS-ATLAS, MAP-QSH-ADDITIONS): two dark grey steel side posts, a thin flat canopy, and a map
/// panel in a light aluminium frame. Width is the canopy width, Height the overall height, Depth the canopy depth; the face
/// looks along −Z and the canopy projects <see cref="StreetStyle.KioskOverhang"/> in front of it. The faces carry a plain
/// map-paper stand-in until game/gen/bakeinput/'s baked map lands (front: the campus map, back: the district plan). Not lit, not clickable;
/// the only light is a small warm downlight under the canopy (grade three, first-lamp group, all night), handed out by
/// <see cref="Emitter"/>. Collision: a post per side and a thin box over the panel, Metal.
/// </summary>
[Part(PartCatalog.AtlasKiosk)]
public sealed class AtlasKioskPart : IPartDef<SignParams>
{
    public AtlasKioskPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(340, 100);

    public Box3 Bounds(SignParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    public IReadOnlyList<Collider> Collision(SignParams p)
    {
        var k = Layout(p);
        var foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot;
        var r = S.KioskPost * 0.71f;
        var top = p.Height - S.KioskCanopy;
        return
        [
            new PostCollider(new(-k.PostX, k.FaceZ), r, -S.PostColliderSink, top, SurfaceTag.Prop) { Foot = foot },
            new PostCollider(new(k.PostX, k.FaceZ), r, -S.PostColliderSink, top, SurfaceTag.Prop) { Foot = foot },
            new BoxCollider(Xf.Move(0, k.Sill + k.FaceHeight * 0.5f, k.FaceZ), new(k.PostX, k.FaceHeight * 0.5f + S.KioskFrame, S.KioskFrameDepth * 0.5f + 0.01f), SurfaceTag.Prop) { Foot = foot },
        ];
    }

    /// <summary>Lamp-inventory marker for the canopy downlight: just under the lens, lighting the face (MAP-QSH-ADDITIONS).</summary>
    public static LightEmitter Emitter(SignParams p, Matrix4x4 at)
    {
        var k = Layout(p);
        return new(S.FirstGroup, Xf.Point(at, new(0, p.Height - S.KioskCanopy - 0.012f - S.MarkerDrop, k.LightZ)), S.LampWarm, S.GradeThreeEnergy, S.KioskLightRange);
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        var k = Layout(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var dark = S.Casing(S.DarkSteel);
        var post = S.KioskPost; var canopyY = p.Height - S.KioskCanopy;
        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * k.PostX;
            b.AddBox(new(x - post * 0.5f, 0, k.FaceZ - post * 0.5f), new(x + post * 0.5f, canopyY + S.Joint, k.FaceZ + post * 0.5f), near ? 0.015f : 0, dark);
            if (near) b.AddBox(new(x - post, 0, k.FaceZ - post), new(x + post, 0.025f, k.FaceZ + post), 0.008f, dark, VertexFlag.NoShadow);
        }
        b.AddBox(new(-p.Width * 0.5f, canopyY, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f), near ? 0.02f : 0, dark);
        // Panel frame spans between the posts (sunk into them by the joint); the faces sit proud of it front and back.
        var inner = k.PostX - post * 0.5f + S.Joint;
        var fd = S.KioskFrameDepth; var e = S.KioskFrame;
        b.AddBox(new(-inner, k.Sill - e, k.FaceZ - fd * 0.5f), new(inner, k.Sill + k.FaceHeight + e, k.FaceZ + fd * 0.5f), near ? 0.01f : 0, S.Casing(S.GreyMetal));
        if (lod > LodLevel.L1) return b.Finish(Bounds(p));
        var paper = S.Board(S.MapPaper);
        var hw = k.FaceWidth * 0.5f;
        b.AddBox(new(-hw, k.Sill, k.FaceZ - fd * 0.5f - 0.004f), new(hw, k.Sill + k.FaceHeight, k.FaceZ - fd * 0.5f + 0.002f), 0, paper);
        b.AddBox(new(-hw, k.Sill, k.FaceZ + fd * 0.5f - 0.002f), new(hw, k.Sill + k.FaceHeight, k.FaceZ + fd * 0.5f + 0.004f), 0, paper);
        // Canopy downlight, in front of the face.
        b.AddBox(new(-0.25f, canopyY - 0.012f, k.LightZ - 0.04f), new(0.25f, canopyY + S.Joint, k.LightZ + 0.04f), near ? 0.004f : 0, S.Lens(S.Diffuser, S.FirstGroup), VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    private readonly record struct KioskLayout(float PostX, float FaceZ, float FaceWidth, float FaceHeight, float Sill, float LightZ);

    private static KioskLayout Layout(SignParams p)
    {
        Validate(p);
        var postX = p.Width * 0.5f - S.KioskPostInset - S.KioskPost * 0.5f;
        var faceZ = Math.Min(-p.Depth * 0.5f + S.KioskOverhang, p.Depth * 0.5f - S.KioskPost);
        var faceWidth = Math.Min(S.KioskFaceWidth, 2 * (postX - S.KioskPost * 0.5f - S.KioskFrame));
        var sill = Math.Min(S.KioskFaceSill, p.Height - S.KioskCanopy - S.KioskFaceHeight - 0.3f);
        var lightZ = Math.Max(-p.Depth * 0.5f + 0.1f, faceZ - 0.3f);
        return new(postX, faceZ, faceWidth, S.KioskFaceHeight, sill, lightZ);
    }

    private static void Validate(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Width < 1.4f || p.Height < 2.2f || p.Depth < 0.4f) throw new ArgumentOutOfRangeException(nameof(p), "An atlas kiosk is at least 1.4 m wide, 2.2 m tall and 0.4 m deep.");
    }
}

/// <summary>
/// Atlas post (导览桩, SYS-ATLAS): a 0.2 m dark grey steel square post on a dark stone base, its top cut at 30° facing the
/// viewer (−Z), an aluminium plate on the slant (the etched map and number come with game/gen/bakeinput/'s plate texture), and a small warm
/// lamp with a milky lens on the front at 0.85 m (grade three, first-lamp group, all night, a pool about 1.5 m across). Width
/// and Depth are the base, Height the top of the slant. Collision: one vertical post, Metal.
/// </summary>
[Part(PartCatalog.AtlasPost)]
public sealed class AtlasPostPart : IPartDef<SignParams>
{
    private const float CapThickness = 0.035f;
    private const float CapOverhang = 0.008f;

    public AtlasPostPart() => Lods = Levels.Of<SignParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<SignParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(260, 80);

    public Box3 Bounds(SignParams p)
    {
        var s = Section(p);
        var hx = Math.Max(p.Width * 0.5f, s * 0.5f + CapOverhang);
        var hz = Math.Max(p.Depth * 0.5f, s * 0.5f + 0.016f);
        return Levels.Padded(new(-hx, 0, -hz), new(hx, p.Height, hz));
    }

    public IReadOnlyList<Collider> Collision(SignParams p)
    {
        _ = Section(p);
        return [new PostCollider(Vector2.Zero, Math.Max(p.Width, p.Depth) * 0.5f, -S.PostColliderSink, p.Height, SurfaceTag.Prop)
            { Foot = p.Foot == FootMat.Auto ? S.MetalFoot : p.Foot }];
    }

    /// <summary>Lamp-inventory marker in front of the small lamp: the anchor's warm point at night (SYS-ATLAS).</summary>
    public static LightEmitter Emitter(SignParams p, Matrix4x4 at)
    {
        var s = Section(p);
        return new(S.FirstGroup, Xf.Point(at, new(0, S.PostLampHeight, -s * 0.5f - 0.05f)), S.LampWarm, S.GradeThreeEnergy, S.PostLampRange);
    }

    private PartGeometry Build(SignParams p, LodLevel lod)
    {
        var s = Section(p);
        var near = lod == LodLevel.L0;
        var b = new PartBuilder();
        var dark = S.Casing(S.DarkSteel);
        var plinth = S.PostPlinthHeight;
        b.AddBox(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, plinth, p.Depth * 0.5f), near ? 0.015f : 0, S.Stone(new(S.Lit(S.StoneDark), S.Shade(S.StoneDark)), false), VertexFlag.NoShadow);
        // Shaft up to the low (front) edge of the slant; the slanted cap plate sits on it.
        var tan = MathF.Tan(S.PostSlantDeg * Angles.DegreesToRadians);
        var c = s + 2 * CapOverhang;
        var low = p.Height - c * tan;
        b.AddBox(new(-s * 0.5f, plinth - S.Joint, -s * 0.5f), new(s * 0.5f, low - CapThickness + 0.01f, s * 0.5f), near ? 0.012f : 0, dark);
        // Cap: the side outline swept across X. A sweep along +X puts lateral on −Z, so lateral = −z: the front (z = −c/2,
        // low edge) is lateral +c/2. Counterclockwise in (lateral, up): front-top, back-top, back-bottom, front-bottom.
        Vector2[] outline = [new(c * 0.5f, low), new(-c * 0.5f, p.Height), new(-c * 0.5f, low - CapThickness), new(c * 0.5f, low - CapThickness)];
        var section = near ? Chamfered(outline, 0.008f) : new SweepProfile(outline);
        b.Add(PartSweeps.Sweep(section, [new(-c * 0.5f, 0), new(c * 0.5f, 0)], dark));
        // Plate on the slant: local Y along the slant normal (up and towards the viewer), local Z up the slope.
        var slant = S.PostSlantDeg * Angles.DegreesToRadians;
        var cs = MathF.Cos(slant); var sn = MathF.Sin(slant);
        var mid = new Vector3(0, (low + p.Height) * 0.5f, 0);
        var lift = 0.0035f;
        var frame = new Matrix4x4(1, 0, 0, 0, 0, cs, -sn, 0, 0, sn, cs, 0, mid.X, mid.Y + cs * lift, mid.Z - sn * lift, 1);
        b.AddCentredBox(new(Math.Min(S.PostPlateWidth, c - 0.02f), 0.006f, Math.Min(S.PostPlateHeight, c / cs - 0.03f)), frame, S.Board(S.Stainless), near ? 0.002f : 0, VertexFlag.NoShadow);
        // The small lamp on the front face: a milky lens in an aluminium bezel.
        var face = -s * 0.5f; var ly = S.PostLampHeight; var lr = S.PostLampRadius;
        var segments = near ? S.RoundSegmentsSmall : 6;
        b.AddPipe(new(0, ly, face + 0.004f), new(0, ly, face - 0.012f), lr, segments, S.Lens(S.Diffuser, S.FirstGroup), VertexFlag.NoShadow);
        if (near) b.AddPipe(new(0, ly, face + 0.004f), new(0, ly, face - 0.006f), lr + 0.009f, segments, S.Metal(S.GreyMetal), VertexFlag.NoShadow);
        return b.Finish(Bounds(p));
    }

    /// <summary>
    /// A counterclockwise outline with every corner cut by a chamfer of <paramref name="cut"/> metres; flat faces keep their own
    /// normal and the chamfer bands interpolate between them (the game/gen/parts/core/ bevel convention).
    /// </summary>
    private static SweepProfile Chamfered(IReadOnlyList<Vector2> outline, float cut)
    {
        var n = outline.Count;
        var points = new List<Vector2>(2 * n); var normals = new List<Vector2>(2 * n);
        for (var i = 0; i < n; i++)
        {
            var prev = outline[(i + n - 1) % n]; var at = outline[i]; var next = outline[(i + 1) % n];
            var toPrev = VectorMath.Unit(prev - at); var toNext = VectorMath.Unit(next - at);
            points.Add(at + toPrev * cut); normals.Add(EdgeNormal(prev, at));
            points.Add(at + toNext * cut); normals.Add(EdgeNormal(at, next));
        }
        return new(points.AsReadOnly(), normals.AsReadOnly());
    }

    /// <summary>Outward normal of a counterclockwise edge a→b: the edge direction turned clockwise.</summary>
    private static Vector2 EdgeNormal(Vector2 a, Vector2 b) => VectorMath.Unit(new Vector2(b.Y - a.Y, a.X - b.X));

    private static float Section(SignParams p)
    {
        Levels.Dimensions(p);
        if (p.Height < 0.6f) throw new ArgumentOutOfRangeException(nameof(p), "An atlas post is at least 0.6 m tall.");
        var s = Math.Min(S.PostSection, Math.Min(p.Width, p.Depth) - 0.06f);
        if (s < 0.1f) throw new ArgumentOutOfRangeException(nameof(p), "The base must leave a post of at least 0.1 m.");
        return s;
    }
}
