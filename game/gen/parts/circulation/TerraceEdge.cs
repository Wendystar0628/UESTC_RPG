using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Circulation.Geometry;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Terrace edge band, the v1 retaining edge of WLD-TERRAIN §5.4, generated along the height field's transition line.
/// The band is 2 m wide and centred on the line. For a line running (dx, dz) the upper ground lies toward (dz, −dx), the
/// WP-09 sweep's positive lateral; a line running +x therefore has the high side toward −z. Its front is a
/// vertical stone wall reaching 0.4 m below the low ground, its top is level with the upper ground, and a trim coping
/// with its own material ID overhangs the wall by 0.06 m. Drops are 0.35–1.0 m (higher drops terrace, §5.5).
/// It has no PartCatalog ID yet, so WP-15 calls it directly; coordinates are world or tile-local metres.
/// </summary>
public static class TerraceEdge
{
    public const float Width = 2.0f;
    public const float Below = 0.4f;
    public const float CopingOverhang = 0.06f, CopingWidth = 0.3f, CopingThickness = 0.08f;

    /// <summary>Depth of the soil bed under the coping line in a planted band (退台花池), and the soil's drop below the top.</summary>
    public const float PlantedBed = 0.08f, PlantedSoilDrop = 0.05f;

    /// <summary>
    /// Geometry of one band. <paramref name="line"/> is an X/Z polyline without repeated points. A planted band
    /// (退台花池, MAP-QSH-TERRAIN §7, §10) keeps the coping at the top and fills the rest of its top with soil 5 cm down for
    /// WP-23 to plant; a paved band is stone to the top.
    /// </summary>
    public static PartGeometry Build(IReadOnlyList<Vector2> line, float lowY, float topY, LodLevel lod, bool planted = false)
    {
        Check(line, lowY, topY);
        var mesh = new MeshBuilder();
        var bevel = lod == LodLevel.L0 ? S.RailBevel : 0;
        // Kernel sweeps take (lateral, up) sections; positive lateral is the upper side.
        var bodyTop = planted ? topY - PlantedBed : topY;
        var body = Profiles.Rect(-Width * 0.5f, Width * 0.5f, 0, bodyTop - lowY + Below, lod == LodLevel.L0 ? S.SlabBevel : 0);
        mesh.Add(PartGeometryKernel.Sweep(body, line, S.Stone, baseY: lowY - Below), Frame.Identity);
        if (lod <= LodLevel.L2)
        {
            var coping = Profiles.Rect(-Width * 0.5f - CopingOverhang, -Width * 0.5f + CopingWidth, -CopingThickness - (planted ? S.Joint : 0), S.Joint, bevel);
            mesh.Add(PartGeometryKernel.Sweep(coping, line, S.Trim, baseY: topY), Frame.Identity);
            if (planted)
            {
                var soil = Profiles.Rect(-Width * 0.5f + CopingWidth - S.Joint, Width * 0.5f, -PlantedBed - S.Joint, -PlantedSoilDrop, 0);
                mesh.Add(PartGeometryKernel.Sweep(soil, line, S.Soil, baseY: topY), Frame.Identity);
            }
        }
        return mesh.Finish(Bounds(line, lowY, topY));
    }

    /// <summary>
    /// Front wall segments (WallCollider, Barrier) from 0.4 m under the low ground to the top, and a walkable box per
    /// segment for the band's top (WLD-TERRAIN §5.4 "碰撞"). A 0.35–1.0 m edge has no railing: it can be sat on and jumped.
    /// </summary>
    public static IReadOnlyList<Collider> Collide(IReadOnlyList<Vector2> line, float lowY, float topY, FootMat foot = FootMat.StonePaving)
    {
        Check(line, lowY, topY);
        var colliders = new List<Collider>();
        for (var i = 0; i + 1 < line.Count; i++)
        {
            var a = line[i]; var b = line[i + 1];
            var d = b - a; var length = V3.Length(d);
            // Toward the upper side, as the sweep's positive lateral (dz, −dx); the wall face is on the other side.
            var upper = new Vector2(d.Y / length, -d.X / length);
            var half = S.BarrierMinHalfThickness;
            var front = upper * (-Width * 0.5f + half);
            colliders.Add(new WallCollider(a + front, b + front, half, lowY - Below, topY, SurfaceTag.Wall) { Foot = foot });
            var centre = (a + b) * 0.5f;
            var frame = Frame.Along(new(centre.X, (topY + lowY - Below) * 0.5f, centre.Y), new(d.X, 0, d.Y));
            colliders.Add(new BoxCollider(frame.ToMatrix(), new(length * 0.5f, (topY - lowY + Below) * 0.5f, Width * 0.5f), SurfaceTag.Ground) { Foot = foot });
        }
        return colliders;
    }

    private static void Check(IReadOnlyList<Vector2> line, float lowY, float topY)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line.Count < 2) throw new ArgumentException("A terrace edge needs a polyline.", nameof(line));
        var drop = topY - lowY;
        if (!float.IsFinite(drop) || drop < 0.3f || drop > 1.05f)
            throw new ArgumentOutOfRangeException(nameof(topY), "A terrace edge drops 0.35–1.0 m (WLD-TERRAIN §5.4); higher drops terrace.");
    }

    private static Box3 Bounds(IReadOnlyList<Vector2> line, float lowY, float topY)
    {
        // Mitred joins reach at most miterLimit (4) × the half width beyond the line.
        var reach = Width * 0.5f * 4 + CopingOverhang;
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var p in line) { minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); minZ = Math.Min(minZ, p.Y); maxZ = Math.Max(maxZ, p.Y); }
        return new(new(minX - reach, lowY - Below, minZ - reach), new(maxX + reach, topY + S.Joint, maxZ + reach));
    }
}
