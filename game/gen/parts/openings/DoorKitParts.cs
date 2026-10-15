using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

/// <summary>Door style of the automatic-door frame and leaf: Variation 0 wood room door, 1 fire door, 2 glass swing door (DoorLeafKind order).</summary>
internal static class AutoDoor
{
    public static DoorStyle Style(int variation) => variation switch
    {
        0 => DoorStyle.Wood,
        1 => DoorStyle.Fire,
        2 => DoorStyle.Entrance,
        _ => throw new ArgumentOutOfRangeException(nameof(variation), variation, "Variation must be 0, 1 or 2."),
    };
}

/// <summary>
/// 自动门门框 (WLD-INTERIORS §4.1): the static half of a runtime door, frame, threshold and transom only.
/// The runtime body and its collision belong to WP-07 and WP-43.
/// </summary>
[Part(PartCatalog.AutomaticDoorFrame, Owner = "WP-10")]
public sealed class AutomaticDoorFramePart() : OpeningPart<DoorParams>(new(260, 60))
{
    protected override Box3 Extent(DoorParams p) => DoorBuilder.Extent(p with { OpenAngleDeg = 0 }, AutoDoor.Style(p.Variation));
    protected override void Validate(DoorParams p) => DoorBuilder.Validate(p with { OpenAngleDeg = 0 }, AutoDoor.Style(p.Variation));

    private protected override void Build(PartMesh mesh, DoorParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        var style = AutoDoor.Style(p.Variation);
        DoorBuilder.Frame(mesh, p, style, Pick(p, DoorBuilder.FrameSpec(style)), lod);
    }
}

/// <summary>
/// 自动门门扇: one leaf with its hinge axis on the local Y axis (origin at the hinge foot, leaf along +X, mid-plane z = 0),
/// so the runtime turns the MultiMesh instance about its origin (DoorLeafDecl.Hinge). Width × Height is the leaf.
/// </summary>
[Part(PartCatalog.AutomaticDoorLeaf, Owner = "WP-10")]
public sealed class AutomaticDoorLeafPart() : OpeningPart<DoorParams>(new(420, 40))
{
    protected override Box3 Extent(DoorParams p)
    {
        var t = DoorBuilder.Thickness(AutoDoor.Style(p.Variation)) * 0.5f + DoorBuilder.Reach;
        return new(new(-0.01f, 0, -t), new(p.Width + 0.01f, p.Height, t));
    }

    protected override void Validate(DoorParams p)
    {
        if (p.Width < 0.3f || p.Height < 0.6f) throw new ArgumentOutOfRangeException(nameof(p), "A door leaf must be at least 0.3 × 0.6 m.");
    }

    private protected override void Build(PartMesh mesh, DoorParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        var style = AutoDoor.Style(p.Variation);
        DoorBuilder.Leaf(mesh, style, Pick(p, DoorBuilder.LeafSpec(style)), Place.Identity, 0, p.Width, OpeningStyle.DoorFloorGap,
            p.Height, true, lod);
    }
}

/// <summary>闭门器 (WLD-INTERIORS §7.5 B): dark grey body with a folded arm in front of it. The back face (+Z) mounts on the leaf.</summary>
[Part(PartCatalog.DoorCloser, Owner = "WP-10")]
public sealed class DoorCloserPart() : OpeningPart<EquipmentParams>(new(140, 12))
{
    private const float Arm = 0.16f;

    protected override Box3 Extent(EquipmentParams p) =>
        new(new(-p.Width * 0.5f - 0.04f, -0.036f, -p.Depth * 0.5f - Arm - 0.02f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));

    private protected override void Build(PartMesh mesh, EquipmentParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        const byte flags = NoShadow;
        var spec = Pick(p, OpeningStyle.DarkMetal);
        float w = p.Width * 0.5f, d = p.Depth * 0.5f;
        Solid.Box(mesh, spec, flags, -w, 0, -d, w, p.Height, d);
        if (lod != LodLevel.L0) return;
        var ins = OpeningStyle.Insert;
        var sx = w - 0.04f;
        Solid.Box(mesh, spec, flags, sx - 0.012f, -0.014f, -0.012f, sx + 0.012f, ins, 0.012f);
        Solid.Box(mesh, spec, flags, sx - 0.01f, -0.024f, -d - Arm, sx + 0.01f, -0.012f, 0.008f);
        Solid.Box(mesh, spec, flags, -w - 0.04f, -0.036f, -d - Arm - 0.02f, sx + 0.01f + ins, -0.024f + ins, -d - Arm + 0.002f);
    }
}

/// <summary>门吸: floor-mounted stop, base plate, post and a dark rubber cap.</summary>
[Part(PartCatalog.DoorStop, Owner = "WP-10")]
public sealed class DoorStopPart() : OpeningPart<EquipmentParams>(new(100, 24))
{
    protected override Box3 Extent(EquipmentParams p) => new(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));

    private protected override void Build(PartMesh mesh, EquipmentParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        const byte flags = NoShadow;
        var spec = Pick(p, OpeningStyle.Metal);
        float w = p.Width * 0.5f, d = p.Depth * 0.5f, ins = OpeningStyle.Insert;
        var cap = Math.Min(0.025f, p.Height * 0.4f);
        float post = Math.Min(w, d) * 0.4f, head = Math.Min(w, d) * 0.55f;
        Solid.Box(mesh, spec, flags, -w, 0, -d, w, 0.012f, d);
        Solid.Box(mesh, spec, flags, -post, 0.012f - ins, -post, post, p.Height - cap + ins, post);
        Solid.Box(mesh, OpeningStyle.Gasket, flags, -head, p.Height - cap, -head, head, p.Height, head);
    }
}

/// <summary>门上视窗: glass insert with a bead frame proud of both leaf faces; Depth is the leaf thickness.</summary>
[Part(PartCatalog.DoorVisionPanel, Owner = "WP-10")]
public sealed class DoorVisionPanelPart() : OpeningPart<WindowParams>(new(80, 40))
{
    private const float Bead = 0.022f;
    private const float Proud = 0.003f;

    protected override Box3 Extent(WindowParams p) =>
        new(new(-p.Width * 0.5f - Bead, -Bead, -p.Depth * 0.5f - Proud), new(p.Width * 0.5f + Bead, p.Height + Bead, p.Depth * 0.5f + Proud));

    private protected override void Build(PartMesh mesh, WindowParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        float w = p.Width * 0.5f, d = p.Depth * 0.5f;
        Solid.Ring(mesh, Pick(p, OpeningStyle.Metal), NoShadow, -w - Bead, -Bead, w + Bead, p.Height + Bead, Bead + 0.008f, -d - Proud, d + Proud);
        var g = Math.Min(0.004f, d * 0.5f);
        Solid.Glass(mesh, 0, Place.Identity, -w - 0.004f, -0.004f, -g, w + 0.004f, p.Height + 0.004f, g, OpeningStyle.DoorCard, new Vector2(0, 32));
    }
}
