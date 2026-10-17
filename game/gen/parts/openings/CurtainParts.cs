using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

/// <summary>
/// 窗帘: a rail and pleated fabric with soft normals. Variation 0 fully open (a bunched stack each side),
/// 1 half open (one drawn panel and one stack), 2 drawn (two panels meeting at the middle). Width × Height is the
/// glass it covers; Depth bounds the pleats. Curtains cast no shadow (ART-MODELING).
/// L0: six segments per pleat; L1: two; L2 and further: nothing (toon_glass draws the state far away).
/// </summary>
[Part(PartCatalog.Curtain, Owner = "WP-10")]
public sealed class CurtainPart() : OpeningPart<PanelParams>(new(900, 300))
{
    protected override Box3 Extent(PanelParams p)
    {
        var z = p.Depth * 0.5f;
        return new(new(-p.Width * 0.5f, 0, -z), new(p.Width * 0.5f, p.Height, z));
    }

    protected override void Validate(PanelParams p)
    {
        if (p.Width < 0.3f || p.Height < 0.3f) throw new ArgumentOutOfRangeException(nameof(p), "A curtain must be at least 0.3 × 0.3 m.");
        if (p.Depth < 0.012f) throw new ArgumentOutOfRangeException(nameof(p), "A curtain needs at least 12 mm of depth.");
    }

    /// <summary>Panels as (left, right, stacked) spans across the width.</summary>
    internal static IReadOnlyList<(float X0, float X1, bool Stacked)> Panels(PanelParams p)
    {
        float w = p.Width * 0.5f, stack = p.Width * OpeningStyle.StackShare;
        return p.Variation switch
        {
            0 => [(-w, -w + stack, true), (w - stack, w, true)],
            1 => [(-w, -w + p.Width * OpeningStyle.HalfDrawnShare, false), (w - stack * 0.9f, w, true)],
            2 => [(-w, -0.006f, false), (0.006f, w, false)],
            _ => throw new ArgumentOutOfRangeException(nameof(p), p.Variation, "Variation must be 0, 1 or 2."),
        };
    }

    private protected override void Build(PartMesh mesh, PanelParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        const byte flags = NoShadow;
        var fabric = Pick(p, OpeningStyle.Fabric);
        var rail = OpeningStyle.CurtainRail;
        var w = p.Width * 0.5f;
        var top = p.Height - rail;
        // The rail stays within the part depth, so behind a window frame it hides under the head member.
        var railHalf = Math.Min(rail, p.Depth) * 0.5f;
        Solid.Box(mesh, OpeningStyle.Casing, flags, -w, top, -railHalf, w, p.Height, railHalf);
        var room = Math.Max(p.Depth * 0.5f - OpeningStyle.CurtainFabric * 0.5f - 0.002f, 0.004f);
        var segments = lod == LodLevel.L0 ? OpeningStyle.PleatSegments : 2;
        foreach (var (x0, x1, stacked) in Panels(p))
        {
            var pitch = stacked ? OpeningStyle.PleatStacked : OpeningStyle.PleatRelaxed;
            var amp = stacked ? room : room * 0.55f;
            var shade = (stacked ? OpeningStyle.PleatSlopeStacked : OpeningStyle.PleatSlopeRelaxed) * pitch / (2 * MathF.PI);
            Solid.Wave(mesh, fabric, flags, true, x0, x1, OpeningStyle.CurtainHem, top + OpeningStyle.Insert, 0, OpeningStyle.CurtainFabric, amp, pitch,
                segments, Math.Max(amp, shade));
        }
    }
}

/// <summary>PVC 门帘: overlapping translucent strips under a rail, alternating slightly in depth. No collision (WLD-INTERIORS).</summary>
[Part(PartCatalog.PvcCurtain, Owner = "WP-10")]
public sealed class PvcCurtainPart() : OpeningPart<PanelParams>(new(160, 100))
{
    private const float Strip = 0.2f;
    private const float Overlap = 0.03f;

    protected override Box3 Extent(PanelParams p)
    {
        var z = Math.Max(p.Depth * 0.5f, 0.03f);
        return new(new(-p.Width * 0.5f, 0, -z), new(p.Width * 0.5f, p.Height, z));
    }

    private protected override void Build(PartMesh mesh, PanelParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        const byte flags = NoShadow;
        var w = p.Width * 0.5f;
        var rail = 0.05f;
        Solid.Box(mesh, OpeningStyle.Metal, flags, -w, p.Height - rail, -0.025f, w, p.Height, 0.025f);
        var spec = Pick(p, OpeningStyle.Spec("roof.silverMetal") with { Translucency = 0.35f, Line = LineClass.Fine, Wet = 0f });
        var count = Math.Max(1, (int)Math.Ceiling((p.Width - Overlap) / (Strip - Overlap)));
        var step = (p.Width - Strip) / Math.Max(1, count - 1);
        for (var i = 0; i < count; i++)
        {
            var x = count == 1 ? -Strip * 0.5f : -w + i * step;
            var z = i % 2 == 0 ? -0.006f : 0.006f;
            Solid.Box(mesh, spec, flags, x, 0.02f, z - 0.002f, Math.Min(x + Strip, w), p.Height - rail + OpeningStyle.Insert, z + 0.002f);
        }
    }
}

/// <summary>
/// 室内视差卡: a stand-alone toon_glass slab for places that need a window card without a wall block (Variation picks
/// classroom, office or dormitory). The parallax itself is drawn by toon_glass.
/// </summary>
[Part(PartCatalog.InteriorCard, Owner = "WP-10")]
public sealed class InteriorCardPart() : OpeningPart<PanelParams>(new(12, 12))
{
    protected override Box3 Extent(PanelParams p) => new(new(-p.Width * 0.5f, 0, -p.Depth * 0.5f), new(p.Width * 0.5f, p.Height, p.Depth * 0.5f));

    private protected override void Build(PartMesh mesh, PanelParams p, LodLevel lod)
    {
        if (lod == LodLevel.L4) return;
        byte card = p.Variation switch { 0 => 1, 1 => 5, 2 => 8, _ => throw new ArgumentOutOfRangeException(nameof(p)) };
        Solid.Glass(mesh, 0, Place.Identity, -p.Width * 0.5f, 0, -p.Depth * 0.5f, p.Width * 0.5f, p.Height, p.Depth * 0.5f, card, new(0, 32));
    }
}
