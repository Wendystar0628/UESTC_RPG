using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.CenterEast.Framework;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using M = Campus.Gen.Zones.CenterEast.CenterEastTune.Monuments;

namespace Campus.Gen.Zones.CenterEast.Research;

/// <summary>
/// 科研楼碑 (MAP-QSH-CENTER-EAST), the campus stone-carving family: a rough-hewn white stone, wider than it is tall,
/// with a chunky chamfer and a top that falls away to one end, set straight into the lawn at the 银杏大道 roadside between
/// 科 A and 科 B. It stands obliquely, its face turned to the walker coming north from the bridge, leaning back a few
/// degrees. The face stays blank until the scene text atlas writes 科研楼 in ink-green running script (contract-26).
/// A small beat of R5 (§8), dark at night (§7).
/// </summary>
public sealed class ResearchSteleSite : CenterEastSite
{
    internal static PlanFrame Frame => PlanMath.Facing(M.SteleAt, M.SteleFacing);

    public override string Id => "21b2.research-stele";

    /// <summary>
    /// Placement of the stone: the outline's x runs across the face, its y up, the thickness (local Y) through the stele's
    /// point toward its back; the whole stone turned back by the lean about its foot's centre line. Rows are rigid
    /// (det +1), so the extrusion keeps its winding.
    /// </summary>
    internal static Matrix4x4 SlabAt(float footY)
    {
        var f = Frame;
        var lean = M.SteleLean * (MathF.PI / 180f);
        float c = MathF.Cos(lean), s = MathF.Sin(lean);
        Vector3 across = new(f.U.X, 0, f.U.Y), back = new(-f.W.X, 0, -f.W.Y), up = Vector3.UnitY;
        var y = back * c - up * s;
        var z = up * c + back * s;
        return new(across.X, across.Y, across.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, M.SteleAt.X, footY, M.SteleAt.Y, 1);
    }

    public override void Build(SiteBuilder b)
    {
        var bevel = b.Detail == SiteDetail.Mass ? 0f : M.SteleBevel;
        var stone = PartGeometryKernel.BeveledExtrude(M.SteleOutline, M.SteleThickness, bevel, L.WhiteStone, -M.SteleThickness * 0.5f);
        b.Add(stone, SlabAt(b.Ground(M.SteleAt) - M.SteleSunk));
    }

    /// <summary>One box in the stone's leaning frame, from the foot to the highest point of the outline.</summary>
    public override void Collide(SiteColliders c)
    {
        var at = SlabAt(c.Ground(M.SteleAt.X, M.SteleAt.Y) - M.SteleSunk);
        float x0 = M.SteleOutline.Min(p => p.X), x1 = M.SteleOutline.Max(p => p.X), half = M.SteleOutline.Max(p => p.Y) * 0.5f, mid = (x0 + x1) * 0.5f;
        c.Box(at with
        {
            M41 = at.M41 + at.M11 * mid + at.M31 * half,
            M42 = at.M42 + at.M12 * mid + at.M32 * half,
            M43 = at.M43 + at.M13 * mid + at.M33 * half,
        }, new((x1 - x0) * 0.5f, M.SteleThickness * 0.5f, half), SurfaceTag.Prop, FootMat.StonePaving);
    }
}
