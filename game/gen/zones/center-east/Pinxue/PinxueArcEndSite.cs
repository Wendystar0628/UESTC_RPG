using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.CenterEast.Framework;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using P = Campus.Gen.Zones.CenterEast.CenterEastTune.Pinxue;

namespace Campus.Gen.Zones.CenterEast.Pinxue;

/// <summary>
/// The curved end of 品学楼's right wing (MAP-QSH-CENTER-EAST). Seen from the forecourt the right wing is B; its far
/// end is the diagonal south-west lobby, which WP-14 builds as a flat-faced block. This site wraps that face in a curved
/// curtain-wall bay: a dark plinth, glass behind white slab bands at each storey, a deep attic band and coping, and a roof
/// cap over the bulge. B stays unclaimed; the bay is registered as a design addition in world/center-east.json.
/// </summary>
public sealed class PinxueArcEndSite : ZoneSite
{
    public override string Id => "21a.pinxue-arc-end";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(CenterEastIds.Layout.PinxueArcChord), P.ArcSagitta + 0.6f)];

    private static Vector2 Side => Vector2.Normalize(P.ArcBulge);

    /// <summary>The bay's chord (the registered outline's ends) and the height and storeys of the lobby it wraps, read by ID.</summary>
    private static (Vector2 Start, Vector2 End, float Height, int Storeys) Lobby(IWorldModel world)
    {
        var (start, end) = world.Feature(CenterEastIds.Layout.PinxueArcChord).PointPair();
        var (height, storeys) = world.Feature(CenterEastIds.Pinxue.WingB).Part(CenterEastIds.Pinxue.ArcLobby).Levels();
        return (start, end, height, storeys);
    }

    /// <summary>Plan points of the bay's outer face, from one end of the chord to the other.</summary>
    internal static Vector2[] Face(IWorldModel world, int segments)
    {
        var lobby = Lobby(world);
        return Shapes.Arc(lobby.Start, lobby.End, P.ArcSagitta, Side, segments);
    }

    /// <summary>A section in (lateral, up): lateral positive is inward, the right of the path from the chord's west end to its east end.</summary>
    private static SweepProfile Section(float in0, float in1, float height) =>
        new([new(in0, 0), new(in1, 0), new(in1, height), new(in0, height)]);

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var y = world.Terrain.PadY(new(CenterEastIds.Pinxue.WingB));
        var segments = b.Detail switch { SiteDetail.Full => 16, SiteDetail.Plain => 10, _ => 6 };
        var lobby = Lobby(world);
        var face = Face(world, segments);
        var height = lobby.Height;
        var storeys = lobby.Storeys;
        var storey = height / storeys;
        Sweep(b, face, Section(-0.1f, 0.6f, 0.9f), L.DarkGranite, y - 0.3f);
        Sweep(b, face, Section(0.12f, 0.18f, height - 0.9f - 0.6f), ZoneLooks.Glass, y + 0.6f);
        for (var k = 1; k < storeys; k++)
            Sweep(b, face, Section(-0.12f, 0.45f, 0.55f), L.Wall, y + k * storey - 0.3f);
        Sweep(b, face, Section(-0.12f, 0.45f, 0.9f), L.Wall, y + height - 0.9f);
        Sweep(b, face, Section(-0.2f, 0.55f, 0.25f), L.Trim, y + height);
        // Roof cap over the bulge, down to the attic band.
        b.Add(PartGeometryKernel.Extrude(new PolygonRings(face), 0.5f, L.RoofSlab, y + height - 0.4f), Matrix4x4.Identity);
        if (b.Detail != SiteDetail.Full) return;
        // Mullions at the interior vertices, just outside the glass.
        var centre = Centre(lobby.Start, lobby.End);
        for (var i = 1; i < face.Length - 1; i++)
        {
            var inward = Vector2.Normalize(centre - face[i]);
            var tangent = new Vector2(-inward.Y, inward.X);
            var p = face[i] + inward * 0.08f;
            var frame = PlanFrame.Along(p, tangent);
            for (var k = 0; k < storeys; k++)
            {
                float y0 = k == 0 ? y + 0.6f : y + k * storey + 0.25f, y1 = k == storeys - 1 ? y + height - 0.9f : y + (k + 1) * storey - 0.3f;
                b.Add(PartGeometryKernel.BeveledBox(0.08f, y1 - y0, 0.12f, 0.01f, L.Metal), frame.At(0, y0, 0));
            }
        }
    }

    private static void Sweep(SiteBuilder b, Vector2[] face, SweepProfile section, ToonSpec material, float baseY) =>
        b.Add(PartGeometryKernel.Sweep(section, face, material), Matrix4x4.CreateTranslation(0, baseY, 0));

    /// <summary>Centre of the face's circle (the same construction as <see cref="Shapes.Arc"/>).</summary>
    private static Vector2 Centre(Vector2 start, Vector2 end)
    {
        var half = Vector2.Distance(start, end) * 0.5f;
        var radius = (half * half + P.ArcSagitta * P.ArcSagitta) / (2f * P.ArcSagitta);
        return (start + end) * 0.5f - Side * (radius - P.ArcSagitta);
    }

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        var y = world.Terrain.PadY(new(CenterEastIds.Pinxue.WingB));
        var face = Face(world, 8);
        var height = Lobby(world).Height;
        for (var i = 0; i + 1 < face.Length; i++)
            c.Wall(face[i], face[i + 1], 0.2f, y, y + height, SurfaceTag.Wall, FootMat.Concrete);
    }
}
