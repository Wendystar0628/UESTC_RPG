using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Gen.World;
using Campus.Gen.Sites;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using T = Campus.Gen.Maps.Qingshuihe.Waterfront.WaterfrontTune;

namespace Campus.Gen.Maps.Qingshuihe.Waterfront.Bridges;

/// <summary>
/// Bridge-head lying stone (桥头卧石, MAP-QSH-ADDITIONS): rough-hewn white stone, about 1.8 m long, lying on the lawn by
/// the head of a main bridge with its long face turned to the approaching walker (props <c>faceDeg</c>, a compass bearing;
/// R1 wants 240° ± 30° so the face takes the low sun). The face stays blank: the 墨绿行书 bridge name waits for the scene
/// lettering atlas (contract-26). Collision: one box, jumpable onto (高 0.62 m, SYS-WALK 第 5 条).
/// </summary>
internal sealed record BridgeStone(FeatureId Id, Vector2 Position, float FaceDeg, float GroundY)
{
    /// <summary>
    /// Side profile in (along, up), a fraction of length and height: flat bottom, a sloped and broken top that rises to one
    /// shoulder, so the stone lies rather than stands. Extruded through its depth; the two flat sides are the faces.
    /// </summary>
    private static readonly Vector2[] Profile =
    [
        new(-0.50f, 0.00f), new(0.50f, 0.00f), new(0.51f, 0.52f), new(0.38f, 0.86f),
        new(-0.12f, 1.00f), new(-0.44f, 0.80f), new(-0.51f, 0.40f),
    ];
    public static IEnumerable<WorldFeature> Sources(IWorldModel world) =>
        world.Features.Values.Where(f => f.Kind.Value == T.StoneKind && f.Geometry is PointGeometry).OrderBy(f => f.Id.Value, StringComparer.Ordinal);

    public static IReadOnlyList<BridgeStone> All(IWorldModel world, Func<Vector2, bool>? includes = null) =>
        [.. Sources(world).Where(f => includes is null || includes(PositionOf(f)))
            .Select(f =>
            {
                var p = ((PointGeometry)f.Geometry).Position;
                var face = f.Number("faceDeg");
                return new BridgeStone(f.Id, new(p.X, p.Z), face, world.Terrain.HeightAt(p.X, p.Z));
            })];

    public static Vector2 PositionOf(WorldFeature feature)
    {
        var p = ((PointGeometry)feature.Geometry).Position;
        return new(p.X, p.Z);
    }

    /// <summary>Unit vector of the face's outward normal in (X, Z): bearing 0° is north (−Z), 90° east (+X).</summary>
    public Vector2 Facing => Angles.Heading(FaceDeg);

    /// <summary>Local X along the stone (across the face normal), Z the face normal, origin on the ground at the centre.</summary>
    public Matrix4x4 Frame => new PlanFrame(Position, new(Facing.Y, -Facing.X)).At(0, GroundY, 0);

    public PartGeometry? Build(LodLevel lod)
    {
        if (lod is LodLevel.L3 or LodLevel.L4) return null;
        var ring = PlanRings.OpenRing(Profile.Select(p => new Vector2(p.X * T.StoneLength, p.Y * (T.StoneHeight + 0.05f) - 0.05f))).ToArray();
        var b = new PartBuilder();
        // The profile is extruded along local Y, then turned so that it stands in X/Y and runs through the depth along Z.
        var stand = new Matrix4x4(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, T.StoneDepth * 0.5f, 1);
        b.Add(PartSolids.BeveledExtrude(ring, T.StoneDepth, lod == LodLevel.L0 ? T.StoneBevel : 0f, T.StoneSpec), stand.Then(Frame));
        return b.Finish(new(new(Position.X - 2f, GroundY - 0.1f, Position.Y - 2f), new(Position.X + 2f, GroundY + 1f, Position.Y + 2f)));
    }

    public Collider Collider
    {
        get
        {
            var half = new Vector3(T.StoneLength * 0.5f, T.StoneHeight * 0.5f + 0.025f, T.StoneDepth * 0.5f);
            return new BoxCollider(Xf.Move(0, T.StoneHeight * 0.5f - 0.025f, 0).Then(Frame), half, SurfaceTag.Prop) { Foot = T.StoneFoot };
        }
    }
}
