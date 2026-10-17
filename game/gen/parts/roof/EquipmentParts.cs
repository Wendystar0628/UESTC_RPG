using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof;

/// <summary>
/// Split air-conditioner outdoor unit (空调外机) at real size: off-white casing on two rails, a dark fan disc
/// on the front (−Z) face and a valve cover on the right. The whole unit stays inside its footprint.
/// </summary>
[Part(PartCatalog.AirConditioner, Owner = "WP-12")]
public sealed class AirConditionerPart : IPartDef<EquipmentParams>
{
    private const float Rail = 0.04f;

    public AirConditionerPart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(170, 40);

    public Box3 Bounds(EquipmentParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var casing = RoofStyle.Casing(RoofStyle.CasingWhite);
        var fan = RoofStyle.Grille(RoofStyle.FenceSteel);
        var x0 = -p.Width * 0.5f; var valve = Math.Min(RoofStyle.AcValveBoxWidth, p.Width * 0.12f);
        var x1 = p.Width * 0.5f - valve;
        var front = -p.Depth * 0.5f + RoofStyle.AcFanProud; var back = p.Depth * 0.5f;
        var y0 = near ? Rail : 0;
        var builder = new PartBuilder();
        builder.AddBox(new(x0, y0, front), new(x1 + 0.003f, p.Height, back), near ? RoofStyle.CasingBevel : 0, casing);
        // Fan disc on the left of the front face, proud by AcFanProud.
        var height = p.Height - y0;
        var radius = Math.Min(height * RoofStyle.AcFanRadiusRatio, (x1 - x0) * 0.3f);
        var fx = x0 + (x1 - x0) * RoofStyle.AcFanCentreRatio; var fy = y0 + height * 0.5f;
        // The fan disc is the unit's identity and is seen from a metre away on balconies: 16 sides near.
        var segments = near ? RoofStyle.RoundSegmentsFine : RoofStyle.RoundSegmentsFar;
        builder.AddDisc(fx, fy, -p.Depth * 0.5f + (near ? 0.006f : 0), front + 0.003f, radius, segments, fan, VertexFlag.NoShadow);
        if (near)
        {
            // Motor hub, proud of the fan disc.
            builder.AddDisc(fx, fy, -p.Depth * 0.5f, -p.Depth * 0.5f + 0.009f, radius * 0.22f, 8, casing, VertexFlag.NoShadow);
            // Valve cover on the right, and two rails under the casing.
            builder.AddBox(new(x1, y0 + 0.04f, front + 0.03f), new(p.Width * 0.5f, y0 + Math.Min(0.18f, height * 0.4f), front + 0.03f + Math.Min(0.12f, p.Depth * 0.4f)), 0, casing, VertexFlag.NoShadow);
            var metal = RoofStyle.Metal(RoofStyle.Aluminium);
            foreach (var side in new[] { -1f, 1f })
            {
                var cx = (x0 + x1) * 0.5f + side * (x1 - x0) * 0.32f;
                builder.AddBox(new(cx - 0.025f, 0, front + 0.02f), new(cx + 0.025f, Rail + 0.003f, back - 0.02f), 0, metal, VertexFlag.NoShadow);
            }
        }
        return builder.Finish(Bounds(p));
    }

    private static void Validate(EquipmentParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Height < 0.2f || p.Depth < 0.12f || p.Width < 0.3f) throw new ArgumentOutOfRangeException(nameof(p), "An outdoor unit is at least 0.3 × 0.2 × 0.12 m.");
    }
}

/// <summary>
/// Unit grille (机位格栅): a light steel frame with tilted aluminium louvres. Depth below <see cref="RoofStyle.GrilleCageDepth"/>
/// gives a flat front panel; deeper grilles become a three-sided cage with a lid, open at the back (wall, +Z) and bottom (shelf).
/// The front is −Z. One value step lighter than the building stand-in, so the facade strip reads clean, not dark.
/// </summary>
[Part(PartCatalog.EquipmentGrille, Owner = "WP-12")]
public sealed class EquipmentGrillePart : IPartDef<PanelParams>
{
    private const float PanelDepth = 0.07f;

    public EquipmentGrillePart() => Lods = Levels.Of<PanelParams>(Build, LodLevel.L0, LodLevel.L1);
    public IReadOnlyDictionary<LodLevel, Func<PanelParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(340, 80);

    public Box3 Bounds(PanelParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(PanelParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var builder = new PartBuilder();
        if (p.Depth < RoofStyle.GrilleCageDepth)
        {
            AddPanel(builder, p.Width, p.Height, p.Depth, Matrix4x4.Identity, near);
            return builder.Finish(Bounds(p));
        }
        var lid = RoofStyle.GrilleTopThickness;
        var height = p.Height - lid + 0.003f;
        var front = -p.Depth * 0.5f;
        AddPanel(builder, p.Width, height, PanelDepth, Xf.Move(0, 0, front + PanelDepth * 0.5f), near);
        var steelLid = RoofStyle.Casing(RoofStyle.Steel);
        if (!near)
        {
            // Far: the louvred sides read as one aluminium block behind the front frame.
            builder.AddBox(new(-p.Width * 0.5f + 0.01f, 0, front + 0.01f), new(p.Width * 0.5f - 0.01f, height, p.Depth * 0.5f), 0, RoofStyle.Grille(RoofStyle.Aluminium));
            builder.AddBox(new(-p.Width * 0.5f, p.Height - lid, front), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f), 0, steelLid);
            return builder.Finish(Bounds(p));
        }
        var sideWidth = p.Depth - PanelDepth + 0.004f;
        var sideZ = (PanelDepth - 0.004f) * 0.5f;
        // Left side faces −X, right side faces +X; both run from the front panel back to the wall.
        Matrix4x4 left = Xf.YQuarter, right = new(0, 0, 1, 0, 0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 1);
        AddPanel(builder, sideWidth, height, PanelDepth, left.Then(Xf.Move(-p.Width * 0.5f + PanelDepth * 0.5f, 0, sideZ)), near);
        AddPanel(builder, sideWidth, height, PanelDepth, right.Then(Xf.Move(p.Width * 0.5f - PanelDepth * 0.5f, 0, sideZ)), near);
        builder.AddBox(new(-p.Width * 0.5f, p.Height - lid, front), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f), 0.01f, steelLid);
        return builder.Finish(Bounds(p));
    }

    /// <summary>One louvred panel in local X/Y, bottom at y = 0, centred in X and Z, front face at −depth/2.</summary>
    private static void AddPanel(PartBuilder builder, float width, float height, float depth, Matrix4x4 at, bool near)
    {
        var f = Math.Min(RoofStyle.GrilleFrame, Math.Min(width, height) * 0.2f);
        Vector2[] opening = [new(-width * 0.5f + f, f), new(width * 0.5f - f, f), new(width * 0.5f - f, height - f), new(-width * 0.5f + f, height - f)];
        builder.Add(PartGeometryKernel.WallPanel(width, height, depth, [opening], RoofStyle.Metal(RoofStyle.Steel)), at);
        var blade = RoofStyle.Grille(RoofStyle.Aluminium);
        var clear = height - 2 * f;
        if (!near)
        {
            // Beyond the near level the louvre period is under 4 px: one recessed colour block (ART-MODELING).
            builder.AddCentredBox(new(width - 2 * f + 0.008f, clear + 0.008f, Math.Min(0.02f, depth * 0.4f)), Xf.Move(0, height * 0.5f, 0).Then(at), blade);
            return;
        }
        var count = Math.Max(1, (int)Math.Floor(clear / RoofStyle.GrilleBladePitch));
        var pitch = clear / count;
        var bladeDepth = Math.Min(RoofStyle.GrilleBladeDepth, depth * 0.8f);
        var size = new Vector3(width - 2 * f + 0.008f, Math.Min(RoofStyle.GrilleBladeThickness, pitch * 0.3f), bladeDepth);
        for (var k = 0; k < count; k++)
            builder.AddCentredBox(size, Xf.RotX(-RoofStyle.GrilleBladeTiltDeg).Then(Xf.Move(0, f + pitch * (k + 0.5f), 0)).Then(at), blade, 0, VertexFlag.NoShadow);
    }

    private static void Validate(PanelParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Width < 0.3f || p.Height < 0.3f || p.Depth < 0.04f) throw new ArgumentOutOfRangeException(nameof(p), "A unit grille is at least 0.3 × 0.3 × 0.04 m.");
    }
}

/// <summary>
/// Panel water tank (水箱): stainless shell with flanges at every panel seam (the horizontal light bands at dusk,
/// ART-FIDELITY), a hatch, a vent and a side ladder. Variation 0 sits on two rails; Variation ≥ 1 stands on a
/// dark steel frame (the old teaching blocks, MAP-QSH-ADDITIONS). The ladder hangs off the +X side, outside the footprint.
/// </summary>
[Part(PartCatalog.WaterTank, Owner = "WP-12")]
public sealed class WaterTankPart : IPartDef<EquipmentParams>
{
    private const float LadderReach = 0.2f;
    private const float HatchHeight = 0.08f;
    private const float VentHeight = 0.12f;

    public WaterTankPart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(640, 140);

    public Box3 Bounds(EquipmentParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f + LadderReach, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var shell = RoofStyle.Casing(RoofStyle.Steel);
        var f = RoofStyle.TankFlange;
        var hx = p.Width * 0.5f - f; var hz = p.Depth * 0.5f - f;
        var stand = p.Variation >= 1;
        var y0 = stand ? p.Height * RoofStyle.TankStandRatio : RoofStyle.TankSkid;
        var top = p.Height - HatchHeight;
        var builder = new PartBuilder();
        builder.AddBox(new(-hx, y0, -hz), new(hx, top, hz), near ? 0.04f : 0, shell);
        // Base: two rails, or the steel stand (legs and a ring beam).
        if (lod <= LodLevel.L2)
        {
            if (stand) AddStand(builder, hx, hz, y0, lod);
            else
            {
                var metal = RoofStyle.Metal(RoofStyle.Aluminium);
                foreach (var side in new[] { -1f, 1f })
                    builder.AddBox(new(-hx + 0.05f, 0, side * hz * 0.6f - 0.08f), new(hx - 0.05f, y0 + 0.003f, side * hz * 0.6f + 0.08f), 0, metal);
            }
        }
        if (lod <= LodLevel.L1)
            builder.AddBox(new(-0.3f, top - 0.003f, -hz * 0.4f - 0.3f), new(0.3f, p.Height, -hz * 0.4f + 0.3f), near ? 0.015f : 0, shell);
        if (near)
        {
            // Panel seams: flanges round the shell every metre, ribs up the faces. They take the metal class, so the
            // material boundary draws them even where the step is too shallow for a depth line, and the rim catches the low sun.
            var seam = RoofStyle.Metal(RoofStyle.Steel);
            Vector2[] ring = [new(-hx, -hz), new(hx, -hz), new(hx, hz), new(-hx, hz)];
            var inner = -(0.04f * 0.6f + 0.004f);
            for (var y = y0 + RoofStyle.TankPanel; y < top - 0.25f; y += RoofStyle.TankPanel)
                builder.Add(PartGeometryKernel.Sweep(Shapes.Rect(inner, f, -RoofStyle.TankFlangeHeight * 0.5f, RoofStyle.TankFlangeHeight * 0.5f), ring, seam, true, y), VertexFlag.NoShadow);
            // Ribs stand 6 mm less proud than the flanges, so the horizontal bands lead.
            var rib = f - 0.006f;
            for (var x = -hx + RoofStyle.TankPanel; x < hx - 0.3f; x += RoofStyle.TankPanel)
                foreach (var side in new[] { -1f, 1f })
                    builder.AddBox(new(x - 0.025f, y0 + 0.05f, side < 0 ? -hz - rib : hz - 0.004f), new(x + 0.025f, top - 0.05f, side < 0 ? -hz + 0.004f : hz + rib), 0, seam, VertexFlag.NoShadow);
            for (var z = -hz + RoofStyle.TankPanel; z < hz - 0.3f; z += RoofStyle.TankPanel)
                builder.AddBox(new(-hx - rib, y0 + 0.05f, z - 0.025f), new(-hx + 0.004f, top - 0.05f, z + 0.025f), 0, seam, VertexFlag.NoShadow);
            builder.AddColumn(hx * 0.5f, top - 0.003f, Math.Min(p.Height, top + VentHeight), hz * 0.4f, 0.05f, 8, RoofStyle.Pipe(RoofStyle.Casing(RoofStyle.Steel)), VertexFlag.NoShadow);
            AddLadder(builder, hx, hz, top, p.Height);
        }
        return builder.Finish(Bounds(p));
    }

    private static void AddStand(PartBuilder builder, float hx, float hz, float y0, LodLevel lod)
    {
        var steel = RoofStyle.Metal(RoofStyle.EquipmentGrey);
        var leg = RoofStyle.TankLeg;
        foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
            {
                var cx = sx * (hx - leg * 0.5f); var cz = sz * (hz - leg * 0.5f);
                builder.AddBox(new(cx - leg * 0.5f, 0, cz - leg * 0.5f), new(cx + leg * 0.5f, y0 + 0.003f, cz + leg * 0.5f), 0, steel);
            }
        if (lod > LodLevel.L1) return;
        // Ring beam under the shell; the mid-height ties only near.
        var rings = lod == LodLevel.L0 ? new[] { (y0 - 0.12f, 0.12f + 0.003f), (y0 * 0.45f, 0.06f) } : [(y0 - 0.12f, 0.12f + 0.003f)];
        foreach (var (y, h) in rings)
        {
            var t = h < 0.1f ? 0.05f : leg * 0.8f;
            foreach (var s in new[] { -1f, 1f })
            {
                builder.AddBox(new(-hx + 0.005f, y, s * (hz - leg * 0.5f) - t * 0.5f), new(hx - 0.005f, y + h, s * (hz - leg * 0.5f) + t * 0.5f), 0, steel);
                builder.AddBox(new(s * (hx - leg * 0.5f) - t * 0.5f, y + 0.002f, -hz + leg - 0.003f), new(s * (hx - leg * 0.5f) + t * 0.5f, y + h - 0.002f, hz - leg + 0.003f), 0, steel);
            }
        }
    }

    private static void AddLadder(PartBuilder builder, float hx, float hz, float top, float height)
    {
        var metal = RoofStyle.Metal(RoofStyle.Aluminium);
        var x = hx + RoofStyle.TankFlange + LadderReach - 0.04f;
        var halfSpan = Math.Min(0.2f, hz * 0.5f);
        var railTop = height;
        foreach (var side in new[] { -1f, 1f })
        {
            builder.AddBox(new(x - 0.02f, 0, side * halfSpan - 0.02f), new(x + 0.02f, railTop, side * halfSpan + 0.02f), 0, metal, VertexFlag.NoShadow);
            foreach (var y in new[] { 0.6f, top - 0.3f })
                builder.AddBox(new(hx - 0.004f, y, side * halfSpan - 0.015f), new(x - 0.019f, y + 0.04f, side * halfSpan + 0.015f), 0, metal, VertexFlag.NoShadow);
        }
        for (var y = 0.3f; y + 0.03f < railTop - 0.05f; y += 0.3f)
            builder.AddBox(new(x - 0.015f, y, -halfSpan + 0.019f), new(x + 0.015f, y + 0.03f, halfSpan - 0.019f), 0, metal, VertexFlag.NoShadow);
    }

    private static void Validate(EquipmentParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Width < 1f || p.Depth < 1f || p.Height < 1.2f) throw new ArgumentOutOfRangeException(nameof(p), "A panel tank is at least 1 × 1.2 × 1 m.");
    }
}

/// <summary>
/// Air-source heat pump (热泵): warm light-grey casing on rails, dark coil panels behind horizontal louvres on the long faces
/// (±Z), and one or more top-discharge fan shrouds with dark fan discs. Usually stands inside an equipment fence.
/// </summary>
[Part(PartCatalog.HeatPump, Owner = "WP-12")]
public sealed class HeatPumpPart : IPartDef<EquipmentParams>
{
    private const float Proud = 0.035f;

    public HeatPumpPart() => Lods = Levels.Of<EquipmentParams>(Build, LodLevel.L0, LodLevel.L1, LodLevel.L2);
    public IReadOnlyDictionary<LodLevel, Func<EquipmentParams, PartGeometry>> Lods { get; }
    public PartBudget Budget { get; } = new(520, 140);

    public Box3 Bounds(EquipmentParams p)
    {
        Validate(p);
        return Levels.Padded(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));
    }

    private PartGeometry Build(EquipmentParams p, LodLevel lod)
    {
        Validate(p);
        var near = lod == LodLevel.L0;
        var casing = RoofStyle.Casing(RoofStyle.EquipmentGrey);
        var dark = RoofStyle.Grille(RoofStyle.FenceSteel);
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f - Proud;
        var y0 = RoofStyle.HeatPumpSkid; var top = p.Height - RoofStyle.HeatPumpShroud;
        var builder = new PartBuilder();
        builder.AddBox(new(-hx, y0, -hz), new(hx, top, hz), near ? RoofStyle.CasingBevel : 0, casing);
        if (lod <= LodLevel.L1)
        {
            var fans = Math.Clamp((int)Math.Round(p.Width / p.Depth), 1, 3);
            var pitch = p.Width / fans;
            var radius = Math.Min(pitch, p.Depth) * RoofStyle.HeatPumpFanRatio;
            var segments = near ? RoofStyle.RoundSegments : RoofStyle.RoundSegmentsFar;
            for (var i = 0; i < fans; i++)
            {
                var x = -hx + pitch * (i + 0.5f);
                builder.AddColumn(x, top - 0.003f, p.Height - 0.012f, 0, radius, segments, RoofStyle.Curved(RoofStyle.EquipmentGrey));
                builder.AddColumn(x, p.Height - 0.015f, p.Height, 0, radius - 0.035f, segments, dark, VertexFlag.NoShadow);
            }
            // Coil panels on both long faces, louvres over them near.
            var margin = Math.Min(0.08f, hx * 0.15f);
            var panel0 = y0 + 0.1f; var panel1 = top - 0.1f;
            foreach (var side in new[] { -1f, 1f })
            {
                var face = side * hz;
                builder.AddBox(new(-hx + margin, panel0, side < 0 ? face - 0.008f : face - 0.003f), new(hx - margin, panel1, side < 0 ? face + 0.003f : face + 0.008f), 0, dark);
                if (!near) continue;
                var blade = RoofStyle.Grille(RoofStyle.Aluminium);
                for (var y = panel0 + RoofStyle.HeatPumpLouvrePitch * 0.5f; y < panel1 - 0.02f; y += RoofStyle.HeatPumpLouvrePitch)
                    builder.AddBox(new(-hx + margin - 0.01f, y - 0.011f, side < 0 ? -hz - Proud : face - 0.003f), new(hx - margin + 0.01f, y + 0.011f, side < 0 ? face + 0.003f : hz + Proud), 0, blade, VertexFlag.NoShadow);
            }
        }
        if (near)
        {
            var metal = RoofStyle.Metal(RoofStyle.Aluminium);
            foreach (var side in new[] { -1f, 1f })
                builder.AddBox(new(-hx + 0.02f, 0, side * hz * 0.64f - 0.05f), new(hx - 0.02f, y0 + 0.003f, side * hz * 0.64f + 0.05f), 0, metal);
        }
        else
            builder.AddBox(new(-hx + 0.02f, 0, -hz + 0.02f), new(hx - 0.02f, y0 + 0.003f, hz - 0.02f), 0, RoofStyle.Metal(RoofStyle.Aluminium));
        return builder.Finish(Bounds(p));
    }

    private static void Validate(EquipmentParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Levels.Positive(p.Width, nameof(p.Width)); Levels.Positive(p.Height, nameof(p.Height)); Levels.Positive(p.Depth, nameof(p.Depth));
        if (p.Width < 0.6f || p.Depth < 0.5f || p.Height < 0.6f) throw new ArgumentOutOfRangeException(nameof(p), "A heat pump is at least 0.6 × 0.6 × 0.5 m.");
    }
}
