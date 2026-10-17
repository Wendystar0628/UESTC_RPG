using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Zones.Core.Framework;
using S = Campus.Gen.Zones.Core.CoreStyle;

namespace Campus.Gen.Zones.Core.Axis;

/// <summary>
/// 日晷 (MAP-QSH-CORE): in the wood on the avenue side of the time plaza, the foreground of P01. A bronze
/// equatorial dial set in a polished stainless ring on a three-tier round stone base; the gnomon is parallel to the
/// earth's axis, pointing true north at the latitude's elevation, and is the 3D scene's north arrow (SYS-VIEW).
/// The dial is perpendicular to the gnomon, so its face normal has y = sin(latitude) ≈ 0.51 and never holds snow.
/// Its position is the overlay point <c>design:core-sundial</c> (the real sundial is not in OSM).
/// </summary>
public sealed class Sundial : ICorePiece
{
    private readonly IWorldModel _world;

    private Sundial(IWorldModel world, Vector2 position)
    {
        _world = world;
        Position = position;
        Base = world.Terrain.HeightAt(position.X, position.Y) + S.PavingLift;
        var phi = S.Latitude * (Shapes.Tau / 360f);
        Gnomon = new(0, DetMath.Sin(phi), -DetMath.Cos(phi));
    }

    public static Sundial? From(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Features.TryGetValue(CoreFeatures.Sundial, out var f) || f.Geometry is not PointGeometry point) return null;
        return new Sundial(world, new(point.Position.X, point.Position.Z));
    }

    public string Id => "sundial";
    public Vector2 Anchor => Position;
    public IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(CoreFeatures.Sundial, Aspects.Massing | Aspects.Props)];

    public Vector2 Position { get; }
    /// <summary>Top of the granite pad the tiers stand on.</summary>
    public float Base { get; }
    /// <summary>Unit gnomon direction: true north (−z), raised by the latitude.</summary>
    public Vector3 Gnomon { get; }

    private float TiersTop => Base + S.SundialTiers.Sum(t => t.Height);
    public Vector3 DialCentre => new(Position.X, Base + S.SundialDialCentre, Position.Y);

    public void Build(CoreBuild build, LodLevel lod)
    {
        if (lod >= LodLevel.L3) return;
        var mesh = build.Mesh;
        var stone = S.Stone(S.Granite);
        var round = S.CircleSides(lod) / 2;
        var small = S.SmallSides(lod) * 2;
        var at = Matrix4x4.CreateTranslation(Position.X, 0, Position.Y);
        var ground = _world.Terrain.HeightAt(Position.X, Position.Y);
        // Granite pad flush with paving, then the three tiers.
        Shapes.Lathe(mesh, stone, at, Shapes.Tier(S.SundialPadRadius, ground - S.Sink, Base, lod == LodLevel.L0 ? 0.02f : 0f), round);
        var y = Base;
        foreach (var (radius, height) in S.SundialTiers)
        {
            Shapes.Lathe(mesh, stone, at, Shapes.Tier(radius, y - 0.01f, y + height, lod == LodLevel.L0 ? S.SundialTierBevel : 0f), round);
            y += height;
        }
        // Pedestal under the dial's low edge.
        var dial = DialCentre;
        var low = dial.Y - S.SundialDial * MathF.Sqrt(1f - Gnomon.Y * Gnomon.Y);
        Shapes.Lathe(mesh, stone, at, Shapes.Tier(S.SundialPedestal, y - 0.01f, MathF.Max(y + 0.05f, low + 0.08f), lod == LodLevel.L0 ? 0.02f : 0f), small);
        // Dial and ring in the plane normal to the gnomon; local +Y is the gnomon.
        var frame = Shapes.Frame(dial, Gnomon, Vector3.UnitX);
        var t = S.SundialDialThick * 0.5f;
        Shapes.Lathe(mesh, S.Metal(S.Bronze), frame, [new(0, -t), new(S.SundialDial, -t), new(S.SundialDial, t), new(0, t)], small * 2);
        var h = S.SundialRingThick * 0.5f;
        Shapes.Lathe(mesh, S.Metal(S.Steel), frame,
            [new(S.SundialRingInner, -h), new(S.SundialRingOuter, -h), new(S.SundialRingOuter, h), new(S.SundialRingInner, h), new(S.SundialRingInner, -h)], small * 2);
        // Gnomon rod through the centre, most of it on the north (sky) side.
        var rod = Matrix4x4.CreateTranslation(0, -0.35f, 0) * frame;
        Shapes.Lathe(mesh, S.Metal(S.Steel), rod, [new(0, 0), new(S.SundialGnomon, 0), new(S.SundialGnomon, S.SundialGnomonLength), new(0, S.SundialGnomonLength)], 8);
    }

    public IReadOnlyList<Collider> Collide()
    {
        var list = new List<Collider>();
        var y = Base;
        foreach (var (radius, height) in S.SundialTiers)
        {
            // Two squares turned 45° make the octagon that stands in for each round tier.
            var half = new Vector3(radius * 0.924f, (height + S.Sink) * 0.5f, radius * 0.924f);
            var centre = new Vector3(Position.X, y + height - half.Y, Position.Y);
            list.Add(new BoxCollider(Matrix4x4.CreateTranslation(centre), half, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
            list.Add(new BoxCollider(DetMath.RotationY(Shapes.Tau / 8f) * Matrix4x4.CreateTranslation(centre), half, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
            y += height;
        }
        list.Add(new PostCollider(Position, S.SundialRingOuter, TiersTop, DialCentre.Y + S.SundialRingOuter, SurfaceTag.Prop) { Foot = FootMat.Metal });
        return list;
    }
}
