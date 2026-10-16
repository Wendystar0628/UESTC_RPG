using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Zones.West.Framework;
using I = Campus.Gen.Zones.West.WestTune.Innovation;
using L = Campus.Gen.Zones.West.WestTune.Look;

namespace Campus.Gen.Zones.West.Innovation;

/// <summary>
/// 创新中心 (MAP-QSH-WEST §3; MAP-QSH-RENOVATION W-01). The bars and cross wings are WP-14's research family, renewed by its
/// rules (new windows, rooftop plant in its fenced enclosure, stair penthouses under the main coping: no sheds on the roof).
/// This site adds the two identity pieces:
/// <list type="bullet">
/// <item>the dark glass stair shafts at the four links between the wings and the bars (§3.3), a little wider than each neck
/// and a storey prouder than the wings, with aluminium fins, landing bands and a white coping;</item>
/// <item>the main entrance on the east wing's face to the parking square: a stone portal with a shallow glazed vestibule, shut
/// (glass doors, lobby card, DoorClosed), whose head carries the slot for the red-brown calligraphy sign (§3.2).</item>
/// </list>
/// </summary>
public sealed class InnovationSite : WestSite
{
    public override string Id => "19b.innovation";

    internal static PlanFrame ShaftFrame(Vector2 centre) => PlanFrame.Along(centre, I.WingAxis);

    internal static PlanFrame PortalFrame => PlanMath.Facing(I.MainDoor, I.MainFacing);

    public override void Build(SiteBuilder b)
    {
        foreach (var (centre, length) in I.Shafts) Shaft(b, ShaftFrame(centre), length, b.Ground(centre));
        Portal(b, PortalFrame, b.Ground(I.MainDoor));
    }

    private static void Shaft(SiteBuilder b, PlanFrame g, float length, float y)
    {
        float hl = length * 0.5f, hw = I.ShaftWidth * 0.5f, top = y + I.ShaftHeight, bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        const float Plinth = 0.45f, Coping = 0.55f;
        GridFacade.Box(b, g, -hl, hl, y - 0.1f, y + Plinth, -hw - 0.05f, hw + 0.05f, L.DarkGrey, bevel);
        GridFacade.Box(b, g, -hl, hl, y + Plinth, top - Coping, -hw, hw, b.Detail == SiteDetail.Mass ? L.DarkGrey : L.DarkGlass, 0);
        GridFacade.Box(b, g, -hl, hl, top - Coping, top, -hw - 0.15f, hw + 0.15f, L.WhiteFrame, bevel);
        if (b.Detail == SiteDetail.Mass) return;
        // Fins on both open faces, and a band at every half landing so the stair reads through the dark glass.
        var bays = Math.Max(2, (int)MathF.Round(length / I.ShaftFin));
        foreach (var s in new[] { -1f, 1f })
        {
            float z0 = s < 0 ? -hw - 0.16f : hw, z1 = s < 0 ? -hw : hw + 0.16f;
            for (var i = 1; i < bays; i++)
            {
                var x = -hl + i * length / bays;
                GridFacade.Box(b, g, x - 0.05f, x + 0.05f, y + Plinth, top - Coping, z0, z1, L.Metal, 0);
            }
            for (var k = 0; ; k++)
            {
                var band = y + I.ShaftLanding * (k + 0.5f);
                if (band + 0.3f > top - Coping) break;
                GridFacade.Box(b, g, -hl, hl, band - 0.15f, band + 0.15f, z0 - s * 0.02f, z1 + s * 0.04f, L.Metal, 0.02f);
            }
        }
    }

    private static void Portal(SiteBuilder b, PlanFrame f, float y)
    {
        float hw = I.PortalWidth * 0.5f, m = I.PortalMember, d = I.PortalDepth, top = y + I.PortalHeight, bevel = b.Detail == SiteDetail.Full ? 0.05f : 0f;
        var headBottom = top - I.SignHeight - 0.5f;
        // Stone jambs and a deep head, the head's front a recessed light panel: the sign slot.
        foreach (var s in new[] { -1f, 1f })
            GridFacade.Box(b, f, s < 0 ? -hw : hw - m, s < 0 ? -hw + m : hw, y - 0.1f, top, -0.3f, d, L.Stone, bevel);
        GridFacade.Box(b, f, -hw, hw, headBottom, top, -0.3f, d + 0.3f, L.Stone, bevel);
        GridFacade.Box(b, f, -hw - 0.1f, hw + 0.1f, top, top + 0.16f, -0.3f, d + 0.42f, L.Trim, bevel * 0.5f);
        GridFacade.Box(b, f, -hw + m + 0.1f, hw - m - 0.1f, headBottom - 0.16f, headBottom, -0.2f, d + 0.36f, L.Trim, bevel * 0.5f);
        if (b.Detail == SiteDetail.Mass) return;
        GridFacade.Box(b, f, -hw + 0.6f, hw - 0.6f, top - I.SignHeight - 0.2f, top - 0.2f, d + 0.3f, d + 0.34f, L.Trim, 0.01f);
        // The glazed screen inside the portal, around the shut doors, with the lobby card between it and the wing's wall.
        float iw = hw - m, dw = I.DoorWidth * 0.5f, dh = y + I.DoorHeight, zs = d - I.ScreenBack;
        foreach (var s in new[] { -1f, 1f })
            GridFacade.Box(b, f, s < 0 ? -iw : dw + 0.1f, s < 0 ? -dw - 0.1f : iw, y + 0.2f, headBottom - 0.16f, zs - 0.08f, zs, L.DarkGlass, 0);
        GridFacade.Box(b, f, -dw - 0.1f, dw + 0.1f, dh + 0.18f, headBottom - 0.16f, zs - 0.08f, zs, L.DarkGlass, 0);
        GridFacade.Box(b, f, -dw - 0.12f, dw + 0.12f, dh, dh + 0.18f, zs - 0.06f, zs + 0.1f, L.Metal, 0.02f);
        foreach (var s in new[] { -1f, 1f })
            GridFacade.Box(b, f, s * (dw + 0.1f) - 0.05f, s * (dw + 0.1f) + 0.05f, y + 0.2f, headBottom - 0.16f, zs - 0.04f, zs + 0.1f, L.Metal, 0);
        GridFacade.Box(b, f, -iw, iw, y - 0.05f, y + 0.2f, zs - 0.12f, zs + 0.04f, L.DarkGrey, 0.01f);
        b.Place(PartCatalog.DoorGlass, new DoorParams(I.DoorWidth, I.DoorHeight, 0.08f, 0f, 2) { Foot = FootMat.Glass }, f.AtReversed(0, y, zs - 0.04f));
        b.Place(PartCatalog.InteriorCard, new PanelParams(I.PortalWidth - 2f * m - 0.6f, I.DoorHeight + 0.8f, 0.06f) { Variation = 1 }, f.AtReversed(0, y, 0.25f));
        GridFacade.Box(b, f, -hw, hw, y - 0.12f, y + 0.18f, -0.2f, d + 2.4f, L.Granite, 0.02f);
        // Text hook: "创新中心" in red-brown calligraphy (MAP-QSH-WEST §3.2) goes on the sign panel (z = d + 0.34, between
        // top − SignHeight − 0.2 and top − 0.2) once the scene text atlas (contract-26) arrives; the panel stays blank until then.
    }

    public override void Collide(SiteColliders c)
    {
        foreach (var (centre, length) in I.Shafts)
        {
            var g = ShaftFrame(centre);
            var y = c.Ground(centre.X, centre.Y);
            c.Box(g.At(0, y + I.ShaftHeight * 0.5f, 0), new(length * 0.5f, I.ShaftHeight * 0.5f, I.ShaftWidth * 0.5f), SurfaceTag.Wall, FootMat.Glass);
        }
        var f = PortalFrame;
        var yp = c.Ground(I.MainDoor.X, I.MainDoor.Y);
        float hw = I.PortalWidth * 0.5f, m = I.PortalMember, d = I.PortalDepth;
        foreach (var s in new[] { -1f, 1f })
            c.Box(f.At(s * (hw - m * 0.5f), yp + I.PortalHeight * 0.5f, (d - 0.3f) * 0.5f), new(m * 0.5f, I.PortalHeight * 0.5f, (d + 0.3f) * 0.5f), SurfaceTag.Wall, FootMat.StonePaving);
        c.Wall(f.P(-hw + m, d - I.ScreenBack), f.P(hw - m, d - I.ScreenBack), 0.1f, yp, yp + I.DoorHeight + 0.6f, SurfaceTag.DoorClosed, FootMat.Glass);
    }
}
