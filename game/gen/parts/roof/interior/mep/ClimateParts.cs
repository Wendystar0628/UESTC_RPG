using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Indoor air-conditioner unit (室内空调). Variation 0: split wall unit, a swept side profile with a rounded underside, the dark
/// outlet and louvre under the curve, a display on the right. 1: flat-front wall unit with a proud front panel. 2: floor
/// cabinet (柜机) with louvred outlet at the top and an intake grille at the foot. Wall units are visual only; the cabinet is a
/// floor prop. Front is −Z.
/// </summary>
[Part(PartCatalog.IndoorAirConditioner)]
public sealed class IndoorAirConditionerPart() : MepPart<EquipmentParams>(new(800, 90))
{
    protected override Vector3 Minimum => new(0.4f, 0.2f, 0.12f);

    public override IReadOnlyList<Collider> Collision(EquipmentParams p) => Variant(p) == 2 ? PropBox(p) : [];

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        switch (Variant(p))
        {
            case 0: WallRounded(b, p, near); break;
            case 1: WallFlat(b, p, near); break;
            case 2: Cabinet(b, p, near); break;
            default: throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Unknown air-conditioner variation.");
        }
    }

    private static void WallRounded(PartBuilder b, EquipmentParams p, bool near)
    {
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var d = p.Depth; var h = p.Height;
        var front = d * 0.5f - MepStyle.Relief; var top = h - MepStyle.Relief;
        var dd = front + d * 0.5f; var c = 0.006f;
        // Section in (lateral, up); lateral +ve is −Z when swept along +X. Chamfer bands carry the interpolated normals.
        var section = new SweepProfile(
            [new(-d * 0.5f + c, 0), new(front - 0.4f * dd, 0), new(front - 0.12f * dd, 0.08f * h), new(front, 0.26f * h),
             new(front, 0.86f * h), new(front - 0.12f * dd, top), new(-d * 0.5f + c, top), new(-d * 0.5f, top - c), new(-d * 0.5f, c)],
            [new(0, -1), new(0, -1), new(0.70710677f, -0.70710677f), new(1, 0), new(1, 0), new(0, 1), new(0, 1), new(-1, 0), new(-1, 0)]);
        b.Add(PartGeometryKernel.Sweep(section, [new(-hx, 0), new(hx, 0)], PartMaterials.Casing(MepStyle.White)), f);
        var dark = PartMaterials.Grille(MepStyle.Dark);
        // Outlet: a dark block whose front pokes out under the curve.
        b.AddBox(new(-hx + 0.07f, 0.012f, -(front - 0.08f * dd)), new(hx - 0.07f, 0.11f * h, -(front - 0.36f * dd)), 0, dark, f);
        if (!near) return;
        b.AddBox(new(-hx + 0.08f, 0.001f, -(front - 0.06f * dd)), new(hx - 0.08f, 0.011f, -(front - 0.3f * dd)), 0.003f, PartMaterials.Casing(MepStyle.White), f);
        Display(b, hx, 0.38f * h, front, p.Depth);
        // Intake slots across the top, behind the rounded edge.
        for (var k = 0; k < 3; k++)
        {
            var l = front - 0.2f * dd - k * 0.022f;
            b.AddBox(new(-hx + 0.05f, top - 0.001f, -l - 0.006f), new(hx - 0.05f, h, -l + 0.006f), 0, dark, f);
        }
    }

    private static void WallFlat(PartBuilder b, EquipmentParams p, bool near)
    {
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var front = -p.Depth * 0.5f + MepStyle.Relief; var h = p.Height;
        var white = PartMaterials.Casing(MepStyle.White);
        b.AddBox(new(-hx, 0, front + 0.012f), new(hx, h - MepStyle.Relief, p.Depth * 0.5f), near ? MepStyle.Bevel : 0, white, f);
        b.AddBox(new(-hx + 0.004f, 0.22f * h, front), new(hx - 0.004f, h - MepStyle.Relief - 0.004f, front + 0.016f), near ? 0.006f : 0, white, f);
        b.AddBox(new(-hx + 0.05f, 0.04f * h, front + 0.006f), new(hx - 0.05f, 0.2f * h, front + 0.02f), 0, PartMaterials.Grille(MepStyle.Dark), f);
        if (!near) return;
        b.AddBox(new(-hx + 0.06f, 0.08f * h, front + 0.002f), new(hx - 0.06f, 0.12f * h, front + 0.012f), 0.002f, white, f);
        b.AddBox(new(-hx + 0.03f, 0.22f * h - 0.004f, front - 0.0005f), new(hx - 0.03f, 0.22f * h + 0.002f, front + 0.004f), 0, PartMaterials.Metal(MepStyle.Aluminium), f);
        Display(b, hx, 0.5f * h, -front, p.Depth);
    }

    private static void Cabinet(PartBuilder b, EquipmentParams p, bool near)
    {
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var front = -p.Depth * 0.5f + MepStyle.Relief; var h = p.Height;
        var white = PartMaterials.Casing(MepStyle.White);
        var dark = PartMaterials.Grille(MepStyle.Dark);
        b.AddBox(new(-hx, 0, front), new(hx, h, p.Depth * 0.5f), near ? MepStyle.Bevel : 0, white, f);
        // Outlet at the top, intake at the foot, both slightly proud of the front.
        var o0 = 0.64f * h; var o1 = Math.Min(0.92f * h, h - 0.05f);
        b.AddBox(new(-hx + 0.04f, o0, -p.Depth * 0.5f + 0.001f), new(hx - 0.04f, o1, front + 0.002f), 0, dark, f);
        b.AddBox(new(-hx + 0.04f, 0.06f * h, -p.Depth * 0.5f + 0.001f), new(hx - 0.04f, 0.3f * h, front + 0.002f), 0, PartMaterials.Metal(MepStyle.Aluminium), f);
        if (!near) return;
        for (var y = o0 + 0.03f; y < o1 - 0.02f; y += 0.045f)
            b.AddBox(new(-hx + 0.045f, y, -p.Depth * 0.5f), new(hx - 0.045f, y + 0.012f, front - 0.0005f), 0.002f, white, f);
        for (var y = 0.06f * h + 0.025f; y < 0.3f * h - 0.015f; y += 0.03f)
            b.AddBox(new(-hx + 0.06f, y, -p.Depth * 0.5f - 0.0005f), new(hx - 0.06f, y + 0.01f, -p.Depth * 0.5f + 0.0015f), 0, dark, f);
        Display(b, hx * 0.2f + MepStyle.AcDisplayWidth * 0.5f, 0.54f * h, -front, p.Depth);
    }

    /// <summary>Small display window 0.06 in from the viewer's right at <paramref name="right"/> (that is, at −X), on a face at z = −<paramref name="frontLateral"/>.</summary>
    private static void Display(PartBuilder b, float right, float y, float frontLateral, float depth)
    {
        var x0 = -right + 0.06f; var x1 = x0 + MepStyle.AcDisplayWidth;
        b.AddBox(new(x0, y, -depth * 0.5f), new(x1, y + 0.024f, -frontLateral + 0.002f), 0.002f, PartMaterials.Grille(MepStyle.Screen), MepStyle.Flags);
    }
}

/// <summary>
/// Ceiling fan (吊扇): canopy cup at the ceiling, downrod, rounded motor housing, pitched blades on brackets. Origin is the
/// lowest point of the motor; Height is the drop to the ceiling. Variation 0: three white blades. 1: three tapered grey
/// blades, aluminium motor. 2: four white blades. Overhead, so visual only. Blades are static here (SYS-MOTION animates).
/// </summary>
[Part(PartCatalog.CeilingFan)]
public sealed class CeilingFanPart() : MepPart<EquipmentParams>(new(800, 220))
{
    protected override Vector3 Minimum => new(0.6f, 0.25f, 0.6f);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var segments = near ? RoofStyle.RoundSegmentsFine : RoofStyle.RoundSegmentsFar;
        var rm = MepStyle.FanMotorRadius; var hm = MepStyle.FanMotorHeight;
        var motor = v == 1 ? PartMaterials.Curved(MepStyle.Aluminium) : PartMaterials.Curved(MepStyle.White);
        LatheBand[] housing =
        [
            new(0, 0, rm * 0.55f, 0, new(0, -1), new(0, -1)),
            new(rm * 0.55f, 0, rm, hm * 0.4f, new(0.5f, -0.866f), new(1, 0)),
            new(rm, hm * 0.4f, rm * 0.92f, hm * 0.8f, new(1, 0), new(0.9f, 0.436f)),
            new(rm * 0.92f, hm * 0.8f, rm * 0.3f, hm, new(0.3f, 0.954f), new(0, 1)),
            new(rm * 0.3f, hm, 0, hm, new(0, 1), new(0, 1)),
        ];
        b.Add(PartShapes.Lathe(housing, segments, motor), f);
        var rc = MepStyle.FanCanopyRadius; var hc = MepStyle.FanCanopyHeight; var ceiling = p.Height;
        LatheBand[] canopy =
        [
            new(0, ceiling - hc, rc * 0.4f, ceiling - hc, new(0, -1), new(0, -1)),
            new(rc * 0.4f, ceiling - hc, rc, ceiling - 0.012f, new(0.6f, -0.8f), new(1, 0)),
            new(rc, ceiling - 0.012f, rc, ceiling, new(1, 0), new(1, 0)),
            new(rc, ceiling, 0, ceiling, new(0, 1), new(0, 1)),
        ];
        b.Add(PartShapes.Lathe(canopy, segments, motor), f);
        b.AddColumn(0, hm - 0.01f, ceiling - hc + 0.004f, 0, MepStyle.FanRodRadius, near ? 10 : 6, PartMaterials.Pipe(v == 1 ? MepStyle.Aluminium : MepStyle.White), f);

        var blades = v == 2 ? 4 : 3;
        var blade = PartMaterials.Casing(v == 1 ? MepStyle.Grey : MepStyle.White);
        var w = MepStyle.FanBladeWidth * (v == 2 ? 0.85f : 1f);
        var (w0, w1) = v == 1 ? (w * 0.72f, w * 1.08f) : (w, w);
        var reach = Math.Min(p.Width, p.Depth) * 0.5f;
        var x1 = (float)Math.Sqrt((double)reach * reach - (double)w1 * w1 * 0.25) - 0.012f;
        var x0 = rm + 0.06f;
        var t = MepStyle.FanBladeThickness;
        var yb = hm * 0.42f;
        Vector2[] outline = [new(x0, -w0 * 0.5f), new(x1, -w1 * 0.5f), new(x1, w1 * 0.5f), new(x0, w0 * 0.5f)];
        var piece = PartGeometryKernel.BeveledExtrude(outline, t, near ? 0.004f : 0, blade, -t * 0.5f);
        var metal = PartMaterials.Metal(MepStyle.Aluminium);
        for (var i = 0; i < blades; i++)
        {
            var spin = Xf.RotY(360f * i / blades).Then(Xf.Move(0, yb, 0));
            b.Add(piece, Xf.RotX(MepStyle.FanBladePitchDeg).Then(spin), f);
            // Bracket from the motor rim to under the blade root.
            b.AddCentredBox(new(x0 + 0.08f - rm * 0.7f, 0.008f, 0.028f), Xf.Move((rm * 0.7f + x0 + 0.08f) * 0.5f, -t * 0.5f - 0.004f, 0).Then(spin), metal, 0, f);
        }
    }
}
