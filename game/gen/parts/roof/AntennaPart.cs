using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof;

/// <summary>
/// Roof antenna (天线). Variation 0: a white parabolic dish on a post, tilted up by <see cref="RoofStyle.DishTiltDeg"/>
/// towards −Z, Width is the dish diameter and the rim top reaches Height (the science buildings' identity dishes,
/// MAP-QSH-ADDITIONS). Variation ≥ 1: a communication mast with three white panel antennas at 120° (tall-building corners).
/// Bounds are measured over all levels, because the tilted and turned pieces do not reduce to a closed-form box.
/// </summary>
[Part(PartCatalog.Antenna, Owner = "WP-12")]
public sealed class AntennaPart : IPartDef<EquipmentParams>
{
    private const float Shell = 0.03f;
    private const float Arm = 0.25f;

    public AntennaPart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(440, 130);

    public Box3 Bounds(EquipmentParams p)
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var level in Lods.Keys)
        {
            var (a, b) = Raw(p, level).Measure();
            min = Vector3.Min(min, a); max = Vector3.Max(max, b);
        }
        return Levels.Padded(min, max);
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod) => Raw(p, lod).Finish(Bounds(p));

    private static PartBuilder Raw(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        return p.Variation >= 1 ? Mast(p, lod) : Dish(p, lod);
    }

    private static PartBuilder Dish(EquipmentParams p, LodLevel lod)
    {
        var near = lod == LodLevel.L0;
        var radius = Math.Min(p.Width * 0.5f * 0.95f, (p.Height - 0.35f) / 1.9f);
        var bowl = 2 * radius * RoofStyle.DishDepthRatio;
        var c = bowl / (radius * radius);
        float Front(float r) => c * r * r;
        Vector2 FrontNormal(float r) => Norm(-2 * c * r, 1);
        Vector2 BackNormal(float r) => Norm(2 * c * r, -1);
        var steps = near ? 3 : 2;
        var bands = new List<LatheBand>();
        for (var i = 0; i < steps; i++)
        {
            float r0 = radius * i / steps, r1 = radius * (i + 1) / steps;
            bands.Add(new(r0, Front(r0), r1, Front(r1), FrontNormal(r0), FrontNormal(r1)));
        }
        bands.Add(new(radius, Front(radius), radius, Front(radius) - Shell, Vector2.UnitX, Vector2.UnitX));
        for (var i = steps; i > 0; i--)
        {
            float r0 = radius * i / steps, r1 = radius * (i - 1) / steps;
            bands.Add(new(r0, Front(r0) - Shell, r1, Front(r1) - Shell, BackNormal(r0), BackNormal(r1)));
        }
        // The rim is the silhouette: 24 sides near keep it round at roof-walking distance; the bowl needs few rings.
        var dish = Shapes.Lathe(bands, near ? 24 : 8, RoofStyle.Curved(RoofStyle.CasingWhite));
        // Aim: the bowl axis (+Y) turns to (0, sin tilt, −cos tilt); the rim top lands on Height.
        var aim = Xf.RotX(-(90f - RoofStyle.DishTiltDeg));
        var top = float.MinValue;
        var mesh = dish.Meshes[0];
        for (var i = 0; i < mesh.Position.Length; i += 3)
            top = Math.Max(top, Xf.Point(aim, new(mesh.Position[i], mesh.Position[i + 1], mesh.Position[i + 2])).Y);
        var at = aim.Then(Xf.Move(0, p.Height - top, radius * 0.25f));
        var builder = new PartBuilder().Add(dish, at);
        var steel = RoofStyle.Pipe(RoofStyle.Metal(RoofStyle.Steel));
        var back = Xf.Point(at, new(0, -Shell, 0));
        var armEnd = Xf.Point(at, new(0, -Shell - Arm, 0));
        builder.AddPipe(back, armEnd, 0.04f, near ? 8 : 6, steel);
        var baseTop = 0.1f;
        builder.AddColumn(armEnd.X, baseTop - 0.003f, armEnd.Y + 0.04f, armEnd.Z, 0.06f, near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar, steel);
        if (lod <= LodLevel.L1)
        {
            var size = Math.Min(0.7f, p.Width * 0.7f);
            builder.AddBox(new(armEnd.X - size * 0.5f, 0, armEnd.Z - size * 0.5f), new(armEnd.X + size * 0.5f, baseTop, armEnd.Z + size * 0.5f), 0, RoofStyle.Metal(RoofStyle.Aluminium));
        }
        if (near)
        {
            // Offset feed: one strut from the lower rim to the focus, and the feed horn.
            var focus = radius * radius / (4 * bowl);
            var rim = Xf.Point(at, new(0, Front(radius * 0.9f), -radius * 0.9f));
            var feed = Xf.Point(at, new(0, focus, 0));
            builder.AddPipe(rim, feed, 0.015f, 6, steel, VertexFlag.NoShadow);
            builder.AddCentredBox(new(0.08f, 0.14f, 0.08f), Xf.Move(0, focus, 0).Then(at), RoofStyle.Casing(RoofStyle.CasingWhite), 0, VertexFlag.NoShadow);
        }
        return builder;
    }

    private static PartBuilder Mast(EquipmentParams p, LodLevel lod)
    {
        var near = lod == LodLevel.L0;
        var builder = new PartBuilder();
        var baseTop = 0.1f;
        builder.AddBox(new(-0.25f, 0, -0.25f), new(0.25f, baseTop, 0.25f), 0, RoofStyle.Metal(RoofStyle.Aluminium));
        var mastR = RoofStyle.MastDiameter * 0.5f;
        builder.AddColumn(0, baseTop - 0.003f, p.Height, 0, mastR, near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar, RoofStyle.Pipe(RoofStyle.Metal(RoofStyle.Steel)));
        var panelH = Math.Min(1.2f, p.Height * 0.45f);
        var depth = RoofStyle.PanelAntennaDepth;
        var centreY = p.Height - 0.05f - panelH * 0.5f;
        var reach = mastR + 0.12f + depth * 0.5f;
        var panel = RoofStyle.Casing(RoofStyle.CasingWhite);
        for (var k = 0; k < 3; k++)
        {
            var turn = Xf.RotY(120f * k);
            builder.AddCentredBox(new(RoofStyle.PanelAntennaWidth, panelH, depth), Xf.Move(0, centreY, -reach).Then(turn), panel, near ? 0.02f : 0);
            if (!near) continue;
            var armLength = reach - depth * 0.5f - mastR + 0.01f;
            foreach (var y in new[] { centreY - panelH * 0.35f, centreY + panelH * 0.35f })
                builder.AddCentredBox(new(0.04f, 0.04f, armLength), Xf.Move(0, y, -(mastR - 0.005f + armLength * 0.5f)).Then(turn), RoofStyle.Metal(RoofStyle.Steel), 0, VertexFlag.NoShadow);
        }
        return builder;
    }

    private static Vector2 Norm(float x, float y)
    {
        var length = Math.Sqrt((double)x * x + (double)y * y);
        return new((float)(x / length), (float)(y / length));
    }

    private static void Validate(EquipmentParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Width < 0.4f || p.Height < 1f) throw new ArgumentOutOfRangeException(nameof(p), "An antenna is at least 0.4 m wide and 1 m high.");
    }
}
