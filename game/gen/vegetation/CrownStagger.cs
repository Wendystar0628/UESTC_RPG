using System.Numerics;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Steps neighbouring crown masses front and back along the crown radius (max-110 110c-4). Masses that interpenetrate at the
/// same depth meet in a seam with no depth step, so the vegetation depth line (post_outline, 0.05 relative depth) never draws
/// between them, least of all from under the crown. Overlapping neighbours are two-coloured in plan order: a mass goes to the
/// side its already placed neighbours are not on, then front masses move out and back masses move in along the radius from
/// the crown centre. The crown body along the axis stays put, and the fillers placed afterwards (TreePlan.FillCrown) plug any
/// hole the step opens. No random draws: the mother model's seeded stream is unchanged. Only card and mixed crowns step:
/// cloud crowns are made of small balls that already part at every seam, and cone crowns (metasequoia) are whorled tiers whose
/// gaps the step would open.
/// </summary>
internal static class CrownStagger
{
    /// <summary>A front mass moves out along the crown radius by this fraction of its own radius.</summary>
    public const float Out = 0.5f;

    /// <summary>A back mass moves in along the crown radius by this fraction of its own radius.</summary>
    public const float In = 0.25f;

    /// <summary>Masses within this fraction of the crown radius from the axis, and within the middle of the crown height, are the crown body and stay.</summary>
    public const float AxisClear = 0.3f;

    /// <summary>Crowns tapering more than this towards a cone keep their tiers as they are.</summary>
    public const float MaxTaper = 0.8f;

    public static void Apply(TreePlan plan)
    {
        if (plan.Species.Crown == CrownType.Cloud || plan.Form.Taper > MaxTaper) return;
        var blobs = plan.Blobs;
        var c = plan.CrownCenter;
        var side = new int[blobs.Count];
        for (var i = 0; i < blobs.Count; i++)
        {
            var b = blobs[i];
            var off = new Vector2(b.Center.X - c.X, b.Center.Z - c.Z).Length();
            if (off < AxisClear * plan.CrownRx && MathF.Abs(b.Center.Y - c.Y) < 0.6f * plan.CrownRy) continue;
            var vote = 0;
            for (var j = 0; j < i; j++)
                if (side[j] != 0 && Vector3.Distance(b.Center, blobs[j].Center) < b.Radius + blobs[j].Radius) vote += side[j];
            side[i] = vote > 0 ? -1 : 1;
        }
        for (var i = 0; i < blobs.Count; i++)
        {
            if (side[i] == 0) continue;
            var b = blobs[i];
            var radial = b.Center - c;
            var u = radial.LengthSquared() > 1e-8f ? Vector3.Normalize(radial) : Vector3.UnitY;
            blobs[i] = b with { Center = b.Center + u * (b.Radius * (side[i] > 0 ? Out : -In)) };
        }
    }
}
