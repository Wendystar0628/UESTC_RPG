using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Straight guard railing segment. Width is the run along X, Height the top of the top rail (real 1.05–1.1 m, at most
/// 1.2 m: a human-scale anchor), Depth the top-rail width (0.08–0.15 m), PostSpacing the maximum post pitch.
/// Variation: 0 balusters, 1 solid panel, 2 horizontal bars. Material overrides the aluminium default.
/// Collision: one barrier wall, top max(Height, 1.5 m), reaching 1 m below the base for sloping ground (SYS-WALK).
/// </summary>
[Part(PartCatalog.Railing, Owner = "WP-11")]
public sealed class RailingPart() : CirculationPart<RailingParams>(new(440, 240))
{
    private protected override Box3 BoundsOf(RailingParams p) => Centered(p.Width, 0, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(RailingParams p) =>
    [
        new WallCollider(new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0), Math.Max(p.Depth * 0.5f, S.BarrierMinHalfThickness),
            -S.BarrierBelow, Math.Max(p.Height, S.BarrierTop), SurfaceTag.Barrier) { Foot = Foot(p.Foot, S.MetalFoot) },
    ];

    private protected override void Compose(MeshBuilder mesh, RailingParams p, Detail detail)
    {
        Range(p.Height, 0.5f, 2.5f, nameof(p.Height));
        Range(p.Depth, 0.04f, 0.3f, nameof(p.Depth));
        Range(p.PostSpacing, 0.5f, 3f, nameof(p.PostSpacing));
        RailKit.Guard(mesh, Pick(p.Material, S.Metal), -p.Width * 0.5f, p.Width * 0.5f, 0, 0, p.Height, p.Depth, p.PostSpacing,
            RailKit.InfillOf(p.Variation), detail, Frame.Identity);
    }
}

/// <summary>
/// The rail bar alone, for wall-mounted handrails: Width is its length along X, Height and Depth its section, base at
/// y = 0. The mounting height is the caller's placement. No collision: wall-side handrails do not block (WLD-INTERIORS).
/// </summary>
[Part(PartCatalog.Handrail, Owner = "WP-11")]
public sealed class HandrailPart() : CirculationPart<RailingParams>(new(44, 12))
{
    private protected override Box3 BoundsOf(RailingParams p) => Centered(p.Width, 0, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(RailingParams p) => [];

    private protected override void Compose(MeshBuilder mesh, RailingParams p, Detail detail)
    {
        var bevel = detail == Detail.Full ? Math.Min(S.RailBevel, Math.Min(p.Height, p.Depth) * 0.3f) : 0;
        mesh.Box(Pick(p.Material, S.Metal), new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f), bevel, Frame.Identity);
    }
}

/// <summary>
/// Clothes-drying rail: Width is the span along X, Depth the rack depth (one bar per 0.3 m, at least one), Height the
/// bracket height. Bars are thin tubes held by end brackets. Small overhead item: no collision, no shadow (WLD-INTERIORS).
/// </summary>
[Part(PartCatalog.DryingRail, Owner = "WP-11")]
public sealed class DryingRailPart() : CirculationPart<RailingParams>(new(150, 60))
{
    private protected override Box3 BoundsOf(RailingParams p) => Centered(p.Width, 0, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(RailingParams p) => [];

    private protected override void Compose(MeshBuilder mesh, RailingParams p, Detail detail)
    {
        Range(p.Width, 0.3f, 12f, nameof(p.Width));
        var bar = Math.Min(S.DryingBarDiameter, Math.Min(p.Height, p.Depth) * 0.8f);
        var count = p.Depth < S.DryingBarPitch ? 1 : 1 + (int)Math.Floor((p.Depth - bar) / S.DryingBarPitch + 1e-4);
        var bracket = Math.Min(S.DryingBracketThickness, p.Width * 0.1f);
        var metal = Pick(p.Material, S.Metal);
        var x0 = -p.Width * 0.5f + bracket; var x1 = p.Width * 0.5f - bracket;
        var y = p.Height - bar * 0.5f - Math.Min(0.01f, (p.Height - bar) * 0.5f);
        var section = detail <= Detail.Plain ? Profiles.Circle(bar * 0.5f, detail == Detail.Full ? 8 : 6) : Profiles.Rect(-bar * 0.5f, bar * 0.5f, -bar * 0.5f, bar * 0.5f, 0);
        for (var i = 0; i < count; i++)
        {
            var z = count == 1 ? 0 : -p.Depth * 0.5f + bar * 0.5f + (p.Depth - bar) * i / (count - 1);
            // A bar runs along kernel X with its section centred at height y.
            mesh.Add(Profiles.Bar(section, [new(x0 - S.Joint, z), new(x1 + S.Joint, z)], S.MetalFine with { Lit = metal.Lit, Shade = metal.Shade }, y),
                Frame.Identity, MeshBuilder.NoShadow);
        }
        if (detail == Detail.Mass) return;
        foreach (var side in new[] { -1f, 1f })
        {
            var xa = side < 0 ? -p.Width * 0.5f : x1; var xb = side < 0 ? x0 : p.Width * 0.5f;
            mesh.Box(metal, new(xa, 0, -p.Depth * 0.5f), new(xb, p.Height, p.Depth * 0.5f), detail == Detail.Full ? Math.Min(0.004f, bracket * 0.3f) : 0, Frame.Identity);
        }
    }
}

/// <summary>
/// Steel bar gate used as a seal in open buildings (WLD-INTERIORS): Width × Height leaf area, Depth the frame depth,
/// Leaves 1 or 2. Collision: a DoorClosed wall, top at least 1.5 m (SYS-WALK).
/// </summary>
[Part(PartCatalog.IronGate, Owner = "WP-11")]
public sealed class IronGatePart() : CirculationPart<DoorParams>(new(540, 300))
{
    private protected override Box3 BoundsOf(DoorParams p) => Centered(p.Width, 0, p.Height, p.Depth);

    private protected override IReadOnlyList<Collider> Colliders(DoorParams p) =>
    [
        new WallCollider(new(-p.Width * 0.5f, 0), new(p.Width * 0.5f, 0), Math.Max(p.Depth * 0.5f, S.BarrierMinHalfThickness),
            0, Math.Max(p.Height, S.BarrierTop), SurfaceTag.DoorClosed) { Foot = Foot(p.Foot, S.MetalFoot) },
    ];

    private protected override void Compose(MeshBuilder mesh, DoorParams p, Detail detail)
    {
        Range(p.Width, 0.6f, 8f, nameof(p.Width)); Range(p.Height, 1f, 4f, nameof(p.Height)); Range(p.Depth, 0.04f, 0.4f, nameof(p.Depth));
        var metal = Pick(p.Material, S.Metal);
        var f = Math.Min(S.GateFrame, p.Depth); var hd = p.Depth * 0.5f; var w = p.Width * 0.5f;
        var bevel = detail == Detail.Full ? S.PostBevel : 0;
        var leaves = Math.Clamp(p.Leaves, 1, 2);
        // Stiles: the two jambs, plus a meeting stile for a pair of leaves.
        var stiles = leaves == 2 ? new[] { -w + f * 0.5f, 0, w - f * 0.5f } : [-w + f * 0.5f, w - f * 0.5f];
        foreach (var x in stiles)
            mesh.Box(metal, new(x - f * 0.5f, 0, -hd), new(x + f * 0.5f, p.Height, hd), bevel, Frame.Identity);
        if (detail == Detail.Mass) return;
        var rail = f * 0.75f;
        foreach (var y in new[] { 0.06f, p.Height * 0.45f, p.Height - rail })
            mesh.Box(metal, new(-w + f - S.Joint, y, -hd * 0.8f), new(w - f + S.Joint, y + rail, hd * 0.8f), bevel, Frame.Identity);
        if (detail == Detail.Coarse) return;
        var bar = S.GateBar;
        var count = (int)Math.Floor((p.Width - 2 * f) / S.GateBarPitch);
        var start = -(count - 1) * S.GateBarPitch * 0.5f;
        for (var i = 0; i < count; i++)
        {
            var x = start + i * S.GateBarPitch;
            if (stiles.Any(s => Math.Abs(s - x) < (f + bar) * 0.5f + 0.01f)) continue;
            mesh.Box(metal with { Line = LineClass.Fine }, new(x - bar * 0.5f, 0.06f + rail - S.Joint, -bar * 0.5f), new(x + bar * 0.5f, p.Height - rail + S.Joint, bar * 0.5f),
                0, Frame.Identity, MeshBuilder.NoShadow);
        }
        // Lock box on the latch side at hand height.
        var lx = leaves == 2 ? f * 0.5f + 0.06f : w - f - 0.06f;
        mesh.Box(metal, new(lx - 0.06f, 1.0f, -hd), new(lx + 0.06f, 1.18f, hd), bevel * 0.5f, Frame.Identity);
    }
}
