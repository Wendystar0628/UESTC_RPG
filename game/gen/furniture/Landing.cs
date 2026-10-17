using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

/// <summary>
/// A slab the furniture stands on, in the furniture's plan frame <see cref="Frame"/> (rotation and plan position, y = 0):
/// ±<see cref="HalfWidth"/> along local X, ±<see cref="HalfDepth"/> along local Z, from <see cref="Bottom"/> (below the lowest
/// ground, its skirt) to <see cref="Top"/> (where the furniture stands). On gentle ground it is a paved bay just proud of the
/// paving; where the footprint is steeper than 5% it is the concrete plinth (台基) of WLD-TERRAIN.
/// </summary>
public sealed record Pad(Matrix4x4 Frame, float HalfWidth, float HalfDepth, float Bottom, float Top, bool Plinth)
{
    public string Family => Plinth ? S.PlinthStone : S.BayStone;

    public ToonSpec Material => StreetStyle.Stone(new(StreetStyle.Lit(Family), StreetStyle.Shade(Family)), false);

    /// <summary>World-space geometry of the slab (one bevelled box).</summary>
    public PartGeometry Geometry()
    {
        var min = new Vector3(-HalfWidth, Bottom, -HalfDepth);
        var max = new Vector3(HalfWidth, Top, HalfDepth);
        var local = new PartBuilder().AddBox(min, max, S.PadBevel, Material, VertexFlag.NoShadow).Finish(new(min, max));
        var corners = Enumerable.Range(0, 8).Select(i => Xf.Point(Frame, new((i & 1) == 0 ? min.X : max.X, (i & 2) == 0 ? min.Y : max.Y, (i & 4) == 0 ? min.Z : max.Z))).ToArray();
        return new PartBuilder().Add(local, Frame).Finish(new(corners.Aggregate(Vector3.Min), corners.Aggregate(Vector3.Max)));
    }

    /// <summary>The slab's collider: standable, a low step up from the lawn.</summary>
    public Collider Collider() =>
        new BoxCollider(Xf.Move(0, (Bottom + Top) * 0.5f, 0).Then(Frame), new(HalfWidth, (Top - Bottom) * 0.5f, HalfDepth), SurfaceTag.Prop)
        { Foot = Plinth ? FootMat.Concrete : S.BayFoot };
}

/// <summary>
/// Landing on the terrain (WLD-TERRAIN). Parts are generated flat with their origin on the ground; the placer reads
/// <see cref="ITerrain"/> and picks the origin height. WP-15 lays its surfaces above the height field (lawn
/// <see cref="FurnitureStyle.LawnLift"/>, paving and kerb tops <see cref="FurnitureStyle.PavingLift"/>), so heights are taken
/// from those levels:
/// <list type="bullet">
/// <item>point things (poles, posts): y = H(centre) + lawn level − burial, burial 0.05–0.3 m growing with the drop under the
/// footprint; none where the grade exceeds 25%;</item>
/// <item>small entities (benches, bins, the kiosk): on a pad whose top is just proud of the paving over the highest ground and
/// whose skirt reaches below the lowest; up to 5% it is a paved bay, steeper it is a concrete plinth;</item>
/// <item>entrance kits stand on the entrance floor (read by the layout).</item>
/// </list>
/// </summary>
public static class Land
{
    /// <summary>Origin height of a pole or post of footprint radius <paramref name="radius"/>, or null where it may not stand.</summary>
    public static float? Point(ITerrain terrain, Vector2 p, float radius)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        var grade = terrain.FactsAt(p.X, p.Y).Grade;
        if (!(grade <= S.PointMaxGrade)) return null;
        var h = terrain.HeightAt(p.X, p.Y);
        var low = h;
        foreach (var (dx, dz) in Around)
            low = MathF.Min(low, terrain.HeightAt(p.X + dx * radius, p.Y + dz * radius));
        if ((h - low) / MathF.Max(radius, 0.01f) > S.PointMaxGrade) return null;
        var burial = Math.Clamp(S.BurialMin + MathF.Max(h - low, radius * grade), S.BurialMin, S.BurialMax);
        return h + S.LawnLift - burial;
    }

    /// <summary>
    /// The pad under a footprint whose plan frame is <paramref name="frame"/> (rotation and plan position) and which spans
    /// ±<paramref name="halfWidth"/> along local X and ±<paramref name="halfDepth"/> along local Z; its top is where the furniture
    /// stands. Null when the drop across the footprint would make the pad read as a wall. <paramref name="baseY"/>, when given,
    /// is a level the pad sits on instead of the height field (a pad or a deck).
    /// </summary>
    public static Pad? Pad(ITerrain terrain, Matrix4x4 frame, float halfWidth, float halfDepth, float? baseY = null)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        float low = float.MaxValue, high = float.MinValue;
        if (baseY is { } level) low = high = level;
        else
            for (var i = -1; i <= 1; i++)
                for (var k = -1; k <= 1; k++)
                {
                    var q = Xf.Point(frame, new(i * halfWidth, 0, k * halfDepth));
                    var h = terrain.HeightAt(q.X, q.Z);
                    low = MathF.Min(low, h);
                    high = MathF.Max(high, h);
                }
        if (high - low > S.PlinthMaxDrop) return null;
        var span = 2f * MathF.Sqrt(halfWidth * halfWidth + halfDepth * halfDepth);
        var grade = baseY is null ? MathF.Max(terrain.FactsAt(frame.M41, frame.M43).Grade, (high - low) / span) : 0f;
        return new Pad(frame, halfWidth, halfDepth, low - S.PadSkirt, high + S.PavingLift + S.PadProud, grade > S.SmallMaxGrade);
    }

    private static readonly (float, float)[] Around = [(1f, 0f), (-1f, 0f), (0f, 1f), (0f, -1f)];
}
