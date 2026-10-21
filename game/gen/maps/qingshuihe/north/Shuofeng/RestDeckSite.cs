using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using D = Campus.Gen.Maps.Qingshuihe.North.NorthTune.Deck;
using L = Campus.Gen.Maps.Qingshuihe.North.NorthTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.North.Shuofeng;

/// <summary>
/// 硕丰苑十组团's courtyard stay point (20b1; DECISION-185, MAP-QSH-ADDITIONS R6, §2.1 休憩平台; registered as
/// design:north-sf10-deck): a 0.45 m deck whose edge is the seat, deep grey steel frame under a wood top laid as four boards
/// round a square pit, a clipped shrub in the pit. The cluster is closed, so it is seen through the fence. It stands on level
/// ground: the base is the lowest ground under its corners and the frame is sunk below it (WLD-TERRAIN).
/// </summary>
public sealed class RestDeckSite : ZoneSite
{
    public override string Id => "20b1.sf10-deck";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        Locate(world) is { } plan ? [SiteRanges.Bounds(plan.Frame.Rect(-plan.Length * .5f - D.Overhang,
            -plan.Width * .5f - D.Overhang, plan.Length * .5f + D.Overhang, plan.Width * .5f + D.Overhang))] : [];

    /// <summary>The deck's plan from the overlay polygon (DECISION-239): centre, long axis, length and width, and the height above the ground.</summary>
    internal sealed record Layout(PlanFrame Frame, float Length, float Width, float Height)
    {
        /// <summary>The deck's plan outline (the overlay polygon), counter-clockwise in the frame.</summary>
        public Vector2[] Outline() => Frame.Rect(-Length * 0.5f, -Width * 0.5f, Length * 0.5f, Width * 0.5f);
    }

    /// <summary>The deck, or null when absent; its rectangle direction is explicitly recorded.</summary>
    internal static Layout? Locate(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(NorthFeatureIds.RestDeck), out var feature)) return null;
        var rectangle = feature.Rectangle(feature.Pair("direction"));
        return new(rectangle.Frame, rectangle.Width, rectangle.Depth, feature.Height ?? 0f);
    }

    private static float Base(Layout plan, Func<Vector2, float> ground) => plan.Outline().Min(ground);

    public override void Build(SiteBuilder b)
    {
        if (Locate(b.Context.World) is not { } plan) return;
        var f = plan.Frame;
        float length = plan.Length, width = plan.Width, height = plan.Height;
        var g = Base(plan, b.Ground);
        var deck = g + height;
        float hl = length * 0.5f, hw = width * 0.5f, hp = D.Pit * 0.5f;
        // Steel frame from below the ground up to the underside of the boards.
        b.Add(PartSolids.BeveledBox(length, height - D.Top + D.Sink, width, 0.02f, L.Steel), f.At(0, g - D.Sink, 0));
        if (b.Detail == SiteDetail.Mass)
        {
            b.Add(PartSolids.BeveledBox(length + 2 * D.Overhang, D.Top, width + 2 * D.Overhang, 0, L.Wood), f.At(0, deck - D.Top, 0));
            return;
        }
        // Four boards round the pit: two full-width ends, two side boards between them; seams leave thin dark lines.
        float lo = hl + D.Overhang, wo = hw + D.Overhang, s = D.Seam * 0.5f;
        float endLength = lo - hp - s;
        foreach (var side in new[] { -1f, 1f })
        {
            b.Add(PartSolids.BeveledBox(endLength, D.Top, 2 * wo, D.TopBevel, L.Wood), f.At(side * (hp + s + endLength * 0.5f), deck - D.Top, 0));
            var sideDepth = wo - hp - s;
            b.Add(PartSolids.BeveledBox(D.Pit - 2 * s, D.Top, sideDepth, D.TopBevel, L.Wood), f.At(0, deck - D.Top, side * (hp + s + sideDepth * 0.5f)));
        }
        // Pit: a steel kerb ring flush with the boards, the shrub rising out of it.
        foreach (var side in new[] { -1f, 1f })
        {
            b.Add(PartSolids.BeveledBox(D.Pit, D.Top + 0.01f, D.Kerb, 0.01f, L.Steel), f.At(0, deck - D.Top, side * (hp - D.Kerb * 0.5f)));
            b.Add(PartSolids.BeveledBox(D.Kerb, D.Top + 0.01f, D.Pit - 2 * D.Kerb, 0.01f, L.Steel), f.At(side * (hp - D.Kerb * 0.5f), deck - D.Top, 0));
        }
        var shrub = D.Pit - 2 * D.Kerb - 0.1f;
        b.Add(PartSolids.BeveledBox(shrub, D.Shrub + D.Top, shrub, D.ShrubBevel, L.Hedge), f.At(0, deck - D.Top, 0));
    }

    /// <summary>One box for the deck, a prop the player could step onto (the cluster is closed, so nobody reaches it on foot).</summary>
    public override void Collide(SiteColliders c)
    {
        if (Locate(c.Context.World) is not { } plan) return;
        var g = Base(plan, c.Ground);
        var half = (plan.Height + D.Sink) * 0.5f;
        c.Box(plan.Frame.At(0, g - D.Sink + half, 0), new(plan.Length * 0.5f + D.Overhang, half, plan.Width * 0.5f + D.Overhang), SurfaceTag.Prop, FootMat.WoodDeck);
    }
}
