using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Scissor folding gate (折叠铁闸), drawn closed across an opening: two stiles, a top rail and a bottom track, and between them a
/// pantograph of crossing flat bars, one pair to a cell. Width × Height is the opening, Depth the footprint (the track and stiles take
/// at most <see cref="CirculationStyle.FoldTrack"/> of it). Variation 0: grey aluminium. 1: grey-green paint. 2: dark steel with a
/// meeting stile in the middle (two leaves). Collision: a DoorClosed wall, top at least 1.5 m (SYS-WALK), like the iron gate.
/// </summary>
[Part(PartCatalog.FoldingGate, Owner = "WP-11")]
public sealed class FoldingGatePart() : CirculationPart<DoorParams>(new(1300, 450))
{
    private static float Track(DoorParams p) => Math.Min(p.Depth, S.FoldTrack);

    private protected override Box3 BoundsOf(DoorParams p) => Centered(p.Width + 0.1f, 0, p.Height, p.Depth);   // 门边锁件伸出门宽约 3 cm

    private protected override IReadOnlyList<Collider> Colliders(DoorParams p) =>
    [
        new WallCollider(new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0), Math.Max(Track(p) * 0.5f, S.BarrierMinHalfThickness),
            0, Math.Max(p.Height, WalkRules.BarrierHeight), SurfaceTag.DoorClosed) { Foot = Foot(p.Foot, S.MetalFoot) },
    ];

    private protected override void Compose(MeshBuilder mesh, DoorParams p, Detail detail)
    {
        Range(p.Width, 1.2f, 8f, nameof(p.Width)); Range(p.Height, 1.2f, 3f, nameof(p.Height)); Range(p.Depth, 0.04f, 0.6f, nameof(p.Depth));
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Folding-gate variation must be 0–2.");
        var metal = Pick(p.Material, p.Variation switch { 1 => S.GateGreen, 2 => S.GateDark, _ => S.Metal });
        var w = p.Width * 0.5f; var h = p.Height; var hz = Track(p) * 0.5f; var j = S.Joint;
        var full = detail == Detail.Full;
        var bevel = full ? S.PostBevel : 0;
        var xi = w - S.FoldStile; var y0 = S.FoldBottomRail; var y1 = h - S.FoldTopRail;
        if (detail == Detail.Mass)
        {
            mesh.Box(metal, new(-w, 0, -hz * 0.5f), new(w, h, hz * 0.5f), 0, Frame.Identity);
            return;
        }
        // Rails along the whole width; stiles (and the meeting stile) between them, a joint into both.
        mesh.Box(metal, new(-w, h - S.FoldTopRail, -hz), new(w, h, hz), bevel, Frame.Identity);
        mesh.Box(metal, new(-w, 0, -hz), new(w, y0, hz), bevel, Frame.Identity);
        var stiles = p.Variation == 2 ? new[] { -w + S.FoldStile * 0.5f, 0f, w - S.FoldStile * 0.5f } : [-w + S.FoldStile * 0.5f, w - S.FoldStile * 0.5f];
        foreach (var x in stiles)
            mesh.Box(metal, new(x - S.FoldStile * 0.5f, y0 - j, -hz), new(x + S.FoldStile * 0.5f, y1 + j, hz), bevel, Frame.Identity);
        if (detail == Detail.Coarse)
        {
            // One panel stands for the lattice.
            mesh.Box(metal with { Line = LineClass.Fine }, new(-xi, y0 - j, -S.FoldBarThick * 0.5f), new(xi, y1 + j, S.FoldBarThick * 0.5f), 0, Frame.Identity, MeshBuilder.NoShadow);
            return;
        }
        // The pantograph: every leaf is a field of cells, two crossing bars each; the plain level takes cells twice as big.
        var thin = metal with { Line = LineClass.Fine };
        var step = detail == Detail.Plain ? 2f : 1f;
        var leaves = p.Variation == 2 ? 2 : 1;
        for (var leaf = 0; leaf < leaves; leaf++)
        {
            var x0 = leaves == 1 ? -xi : (leaf == 0 ? -xi : S.FoldStile * 0.5f - j);
            var x1 = leaves == 1 ? xi : (leaf == 0 ? -S.FoldStile * 0.5f + j : xi);
            var cols = Math.Max(2, (int)MathF.Round((x1 - x0) / (S.FoldCellWidth * step)));
            var rows = Math.Max(1, (int)MathF.Round((y1 - y0) / (S.FoldCellHeight * step)));
            var cw = (x1 - x0) / cols; var ch = (y1 - y0) / rows;
            for (var c = 0; c < cols; c++)
                for (var r = 0; r < rows; r++)
                {
                    float ax = x0 + c * cw, bx = ax + cw, ay = y0 + r * ch, by = ay + ch;
                    Bar(mesh, thin, new(ax, ay), new(bx, by));
                    Bar(mesh, thin, new(ax, by), new(bx, ay));
                }
        }
        if (!full) return;
        // The lock box on the latch stile at hand height, with a handle bar across its front.
        var lx = w - S.FoldStile * 0.5f;
        mesh.Box(metal, new(lx - 0.07f, 0.9f, -hz), new(lx + 0.07f, 1.1f, hz), 0.006f, Frame.Identity);
    }

    /// <summary>One flat bar between two cell corners in the gate plane, reaching a hair past each end so the joints close.</summary>
    private static void Bar(MeshBuilder mesh, ToonSpec material, Vector2 a, Vector2 b)
    {
        var d = b - a; var length = d.Length();
        var dir = d / length;
        var frame = new Frame(new((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f, 0), new(dir.X, dir.Y, 0), new(-dir.Y, dir.X, 0), Vector3.UnitZ);
        var half = (length + 0.02f) * 0.5f;
        mesh.Box(material, new(-half, -S.FoldBar * 0.5f, -S.FoldBarThick * 0.5f), new(half, S.FoldBar * 0.5f, S.FoldBarThick * 0.5f), 0, frame, MeshBuilder.NoShadow);
    }
}

/// <summary>
/// Lift landing door (电梯门), closed: a jamb frame round two sliding leaves, a head box over them with a dark floor-indicator window and a
/// threshold sill, and a call-button plate with two buttons on the right jamb (seen from −Z). Width × Height is the frame's outer
/// opening, Depth its footprint; the leaves stand 2 cm behind the frame's front. Variation 0: brushed stainless. 1: white paint. 2: dark
/// steel. Closed lift doors only seal (WLD-INTERIORS-ACCESS): a DoorClosed wall, top at least 1.5 m.
/// </summary>
[Part(PartCatalog.ElevatorDoor, Owner = "WP-11")]
public sealed class ElevatorDoorPart() : CirculationPart<DoorParams>(new(520, 200))
{
    private protected override Box3 BoundsOf(DoorParams p) => Centered(p.Width, 0, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(DoorParams p) =>
    [
        new WallCollider(new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0), Math.Max(p.Depth * 0.5f, S.BarrierMinHalfThickness),
            0, Math.Max(p.Height, WalkRules.BarrierHeight), SurfaceTag.DoorClosed) { Foot = Foot(p.Foot, S.MetalFoot) },
    ];

    private protected override void Compose(MeshBuilder mesh, DoorParams p, Detail detail)
    {
        Range(p.Width, 0.9f, 3f, nameof(p.Width)); Range(p.Height, 2f, 3.5f, nameof(p.Height)); Range(p.Depth, 0.06f, 0.4f, nameof(p.Depth));
        if (p.Variation is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Lift-door variation must be 0–2.");
        var steel = Pick(p.Material, p.Variation switch { 1 => S.LiftWhite, 2 => S.GateDark, _ => S.LiftSteel });
        var w = p.Width * 0.5f; var h = p.Height; var hz = p.Depth * 0.5f; var j = S.Joint;
        var full = detail == Detail.Full;
        var bevel = full ? S.PostBevel : 0;
        if (detail == Detail.Mass)
        {
            mesh.Box(steel, new(-w, 0, -hz * 0.5f), new(w, h, hz * 0.5f), 0, Frame.Identity);
            return;
        }
        var jamb = S.LiftJamb; var head = S.LiftHead; var sill = S.LiftSill;
        var front = -hz + S.LiftFrameRecess;
        // Frame: head full width, jambs between sill and head, threshold sill in dark steel.
        mesh.Box(steel, new(-w, h - head, front), new(w, h, hz), bevel, Frame.Identity);
        foreach (var side in new[] { -1f, 1f })
        {
            var x0 = side < 0 ? -w : w - jamb; var x1 = side < 0 ? -w + jamb : w;
            mesh.Box(steel, new(x0, sill - j, front), new(x1, h - head + j, hz), bevel, Frame.Identity);
        }
        mesh.Box(S.GateDark, new(-w + jamb, 0, front), new(w - jamb, sill, hz), 0, Frame.Identity);
        // Two sliding leaves meet at the middle, set back from the frame's front.
        var leafFront = -hz + S.LiftRecess; var leafBack = hz - S.LiftRecess;
        var lw = (p.Width - 2 * jamb - S.LiftGap) * 0.5f;
        mesh.Box(steel, new(-S.LiftGap * 0.5f - lw, sill - j, leafFront), new(-S.LiftGap * 0.5f, h - head + j, leafBack), bevel, Frame.Identity);
        mesh.Box(steel, new(S.LiftGap * 0.5f, sill - j, leafFront), new(S.LiftGap * 0.5f + lw, h - head + j, leafBack), bevel, Frame.Identity);
        if (detail == Detail.Coarse) return;
        // Floor indicator window in the head; the call plate on the jamb on the viewer's right (-X seen from the front), two buttons on it.
        var dark = S.Insert;
        mesh.Box(dark, new(-0.14f, h - head + 0.04f, -hz), new(0.14f, h - head + 0.14f, front + j), 0, Frame.Identity, MeshBuilder.NoShadow);
        if (!full) return;
        var px = -w + jamb * 0.5f;
        mesh.Box(S.GateDark, new(px - 0.04f, 1.0f, -hz + 0.004f), new(px + 0.04f, 1.24f, front + j), 0.003f, Frame.Identity, MeshBuilder.NoShadow);
        foreach (var y in new[] { 1.19f, 1.08f })
            mesh.Box(steel, new(px - 0.015f, y - 0.015f, -hz), new(px + 0.015f, y + 0.015f, front + 0.008f), 0.003f, Frame.Identity, MeshBuilder.NoShadow);
    }
}
