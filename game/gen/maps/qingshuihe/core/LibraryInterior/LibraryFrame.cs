using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using NetTopologySuite.Geometries;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using S = Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior.LibraryInteriorStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;

/// <summary>
/// The library interior's plan frame, read from the shell so the two agree: the octagon centre and face angle, the storey
/// table, the blocks and exterior wall pieces (<see cref="LibraryPlan"/>) and the window holes (<see cref="LibraryShell"/>).
/// Directions are numbered from the front face (the SE tower with lib.main): j = 0 front, 1 S (sector D), 2 SW tower,
/// 3 W (sector C), 4 NW, 5 N (sector B), 6 NE (cafe side), 7 E (sector A); faces j carry their outward normal Dir(j).
/// </summary>
internal sealed class LibraryFrame
{
    public const float Step = MathF.PI / 4f;
    private static readonly float HalfTan = MathF.Tan(Step * 0.5f);

    private LibraryFrame(LibraryShell shell)
    {
        Shell = shell;
        Plan = shell.Plan;
        var front = Plan.EntranceFace ?? throw new InvalidOperationException("The library shell has no front entrance face.");
        int best = 0;
        var score = float.MinValue;
        for (var k = 0; k < 8; k++)
        {
            var d = Vector2.Dot(Angle(Plan.FaceAngle + k * Step), front.Out);
            if (d > score) { score = d; best = k; }
        }
        Front = Plan.FaceAngle + best * Step;
        GalleryApothem = Plan.SkylightRadius * MathF.Cos(Step * 0.5f);
        VoidApothem = GalleryApothem - S.GalleryWidth;
        Core = Plan.Blocks.Single(b => b.Kind == BlockKind.Core);
        Holes = shell.Openings.ToLookup(o => o.Face, o => o.Hole);
    }

    /// <summary>The frame of the world's library shell; the library interior cannot exist without it.</summary>
    public static LibraryFrame Of(IWorldModel world) =>
        DerivedMemo.ForWorld<LibraryFrame>(world, typeof(LibraryFrame), () => CreateOf(world));

    private static LibraryFrame CreateOf(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return new LibraryFrame(LibraryShell.From(world)
            ?? throw new InvalidOperationException("The library interior needs the library shell (MAP-QSH-CORE) in this world."));
    }

    public static LibraryFrame Of(BuildContext context) => Of(context.World);

    public LibraryShell Shell { get; }
    public LibraryPlan Plan { get; }
    public Block Core { get; }
    /// <summary>The shell's window and door holes by wall face.</summary>
    public ILookup<Face, Rect> Holes { get; }
    public Vector2 Centre => Plan.Centre;
    /// <summary>Plan angle (x = cos, z = sin) of the front face's outward normal.</summary>
    public float Front { get; }
    /// <summary>Apothem of the outer edge of every gallery ring: the skylight edge.</summary>
    public float GalleryApothem { get; }
    public float VoidApothem { get; }
    public int Storeys => LibraryStyle.LibraryStoreys.Length;

    public static Vector2 Angle(float a) => new(MathF.Cos(a), MathF.Sin(a));
    public Vector2 Dir(float j) => Angle(Front + j * Step);

    /// <summary>Plan point at distance r along Dir(j) and t across it (positive toward Dir(j + 2)).</summary>
    public Vector2 At(float j, float r, float t) => Centre + Dir(j) * r + Dir(j + 2) * t;

    /// <summary>Half the length of an octagon face at an apothem.</summary>
    public static float HalfFace(float apothem) => apothem * HalfTan;

    public float FloorY(int storey) => Plan.StoreyY(storey - 1);
    /// <summary>Top of the storey's space: the next floor, or the roof for the top storey.</summary>
    public float TopY(int storey) => Plan.StoreyY(storey);
    public float SoffitY(int storey) => storey < Storeys ? FloorY(storey + 1) - S.SlabThick : Plan.RoofY - S.CeilingGap;
    public static Vector3 V(Vector2 p, float y) => new(p.X, y, p.Y);

    /// <summary>Blocks the storey's floor stands in: they start at or below it and rise at least a metre above it.</summary>
    public IEnumerable<Block> BlocksAt(int storey)
    {
        var y = FloorY(storey);
        return Plan.Blocks.Where(b => b.Y0 <= y + 0.05f && b.Y1 >= y + 1f);
    }

    /// <summary>
    /// The storey's space footprint: the outline of the union of <see cref="BlocksAt"/>, the same blocks the shell interior lays
    /// floors in. A storey's space is one ring (StoreyInfo), so its blocks must join into one polygon; the union's holes are only
    /// the centimetre seams where neighbouring source rings nearly meet, not spaces, and the outline closes them.
    /// </summary>
    public IReadOnlyList<Vector2> Footprint(int storey)
    {
        var union = Factory.CreateMultiPolygon(BlocksAt(storey).Select(b => Factory.CreatePolygon(
            b.Ring.Append(b.Ring[0]).Select(p => new Coordinate(p.X, p.Y)).ToArray())).ToArray()).Union();
        if (union is not Polygon polygon)
            throw new InvalidDataException($"Library storey {storey}'s blocks do not join into one footprint ({union.GeometryType}).");
        return PlanRings.OpenRing(polygon.ExteriorRing.Coordinates.Select(c => new Vector2((float)c.X, (float)c.Y)));
    }

    private static readonly GeometryFactory Factory = new();

    /// <summary>Whether a block also stands in the next storey (otherwise its roof is this storey's ceiling).</summary>
    public bool Continues(Block block, int storey) => storey < Storeys && block.Y1 >= FloorY(storey + 1) + 1f;

    /// <summary>Counterclockwise regular octagon with its faces on Dir(j); a chamfer cuts each corner this far along both edges.</summary>
    public IReadOnlyList<Vector2> Octagon(float apothem, float chamfer = 0f)
    {
        var r = apothem / MathF.Cos(Step * 0.5f);
        var points = new List<Vector2>(16);
        for (var k = 0; k < 8; k++)
        {
            var v = Centre + Angle(Front + (k + 0.5f) * Step) * r;
            if (chamfer <= 0f) { points.Add(v); continue; }
            var previous = Centre + Angle(Front + (k - 0.5f) * Step) * r;
            var next = Centre + Angle(Front + (k + 1.5f) * Step) * r;
            points.Add(v + Vector2.Normalize(previous - v) * chamfer);
            points.Add(v + Vector2.Normalize(next - v) * chamfer);
        }
        return points;
    }

    /// <summary>
    /// The runs of a chamfered octagon at an apothem: every face minus its open spans (t0, t1 across the face), and the
    /// corner cuts between faces. Each run goes A → B counterclockwise.
    /// </summary>
    public IEnumerable<(Vector2 A, Vector2 B)> OctagonRuns(float apothem, Func<int, IReadOnlyList<(float T0, float T1)>> open)
    {
        var h = HalfFace(apothem) - S.OctagonChamfer;
        for (var j = 0; j < 8; j++)
        {
            var t = -h;
            foreach (var (t0, t1) in open(j).OrderBy(g => g.T0))
            {
                if (t0 > t + 0.05f) yield return (At(j, apothem, t), At(j, apothem, MathF.Min(t0, h)));
                t = MathF.Max(t, t1);
            }
            if (h > t + 0.05f) yield return (At(j, apothem, t), At(j, apothem, h));
            yield return (At(j, apothem, h), At(j + 1, apothem, -h));
        }
    }
}
