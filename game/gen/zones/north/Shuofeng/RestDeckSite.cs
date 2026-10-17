using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.North.Framework;
using D = Campus.Gen.Zones.North.NorthTune.Deck;
using L = Campus.Gen.Zones.North.NorthTune.Look;

namespace Campus.Gen.Zones.North.Shuofeng;

/// <summary>
/// 硕丰苑十组团's courtyard stay point (20b1; D-185, MAP-QSH-ADDITIONS §0 R6, §2.1 休憩平台; registered as
/// design:north-sf10-deck): a 0.45 m deck whose edge is the seat, deep grey steel frame under a wood top laid as four boards
/// round a square pit, a clipped shrub in the pit. The cluster is closed, so it is seen through the fence. It stands on level
/// ground: the base is the lowest ground under its corners and the frame is sunk below it (WLD-TERRAIN §4.1).
/// </summary>
public sealed class RestDeckSite : NorthSite
{
    public override string Id => "20b1.sf10-deck";

    internal static PlanFrame Frame => PlanFrame.Along(D.Centre, D.Along);

    /// <summary>The deck's plan outline (the overlay polygon), counter-clockwise in the frame.</summary>
    internal static Vector2[] Outline() => Frame.Rect(-D.Length * 0.5f, -D.Width * 0.5f, D.Length * 0.5f, D.Width * 0.5f);

    private static float Base(Func<Vector2, float> ground) => Outline().Min(ground);

    public override void Build(SiteBuilder b)
    {
        var f = Frame;
        var g = Base(b.Ground);
        var deck = g + D.Height;
        float hl = D.Length * 0.5f, hw = D.Width * 0.5f, hp = D.Pit * 0.5f;
        // Steel frame from below the ground up to the underside of the boards.
        b.Add(PartGeometryKernel.BeveledBox(D.Length, D.Height - D.Top + D.Sink, D.Width, 0.02f, L.Steel), f.At(0, g - D.Sink, 0));
        if (b.Detail == SiteDetail.Mass)
        {
            b.Add(PartGeometryKernel.BeveledBox(D.Length + 2 * D.Overhang, D.Top, D.Width + 2 * D.Overhang, 0, L.Wood), f.At(0, deck - D.Top, 0));
            return;
        }
        // Four boards round the pit: two full-width ends, two side boards between them; seams leave thin dark lines.
        float lo = hl + D.Overhang, wo = hw + D.Overhang, s = D.Seam * 0.5f;
        float endLength = lo - hp - s;
        foreach (var side in new[] { -1f, 1f })
        {
            b.Add(PartGeometryKernel.BeveledBox(endLength, D.Top, 2 * wo, D.TopBevel, L.Wood), f.At(side * (hp + s + endLength * 0.5f), deck - D.Top, 0));
            var sideDepth = wo - hp - s;
            b.Add(PartGeometryKernel.BeveledBox(D.Pit - 2 * s, D.Top, sideDepth, D.TopBevel, L.Wood), f.At(0, deck - D.Top, side * (hp + s + sideDepth * 0.5f)));
        }
        // Pit: a steel kerb ring flush with the boards, the shrub rising out of it.
        foreach (var side in new[] { -1f, 1f })
        {
            b.Add(PartGeometryKernel.BeveledBox(D.Pit, D.Top + 0.01f, D.Kerb, 0.01f, L.Steel), f.At(0, deck - D.Top, side * (hp - D.Kerb * 0.5f)));
            b.Add(PartGeometryKernel.BeveledBox(D.Kerb, D.Top + 0.01f, D.Pit - 2 * D.Kerb, 0.01f, L.Steel), f.At(side * (hp - D.Kerb * 0.5f), deck - D.Top, 0));
        }
        var shrub = D.Pit - 2 * D.Kerb - 0.1f;
        b.Add(PartGeometryKernel.BeveledBox(shrub, D.Shrub + D.Top, shrub, D.ShrubBevel, L.Hedge), f.At(0, deck - D.Top, 0));
    }

    /// <summary>One box for the deck, a prop the player could step onto (the cluster is closed, so nobody reaches it on foot).</summary>
    public override void Collide(SiteColliders c)
    {
        var g = Base(c.Ground);
        var half = (D.Height + D.Sink) * 0.5f;
        c.Box(Frame.At(0, g - D.Sink + half, 0), new(D.Length * 0.5f + D.Overhang, half, D.Width * 0.5f + D.Overhang), SurfaceTag.Prop, FootMat.WoodDeck);
    }
}
