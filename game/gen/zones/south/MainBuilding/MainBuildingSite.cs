using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Roof;
using Campus.Gen.Zones.South.Framework;
using M = Campus.Gen.Zones.South.SouthTune.Main;
using L = Campus.Gen.Zones.South.SouthTune.Look;

namespace Campus.Gen.Zones.South.MainBuilding;

/// <summary>
/// Main building identity parts (MAP-QSH-SOUTH §2). The wings' massing, storeys and window bays stay with WP-14's facade
/// rules (heights set by HeightOps in south.overlay.json); this site adds what makes the building read as the main
/// building: the front platform at the 2.6 m first floor with its grand steps, the flat-roofed square-column portico with
/// round-headed windows and door in its back wall, the colonnaded roof gallery (空廊) with railing parapets on B2, and the
/// rear door porch. The front steps are a "building" in the OSM data, so this site claims them.
/// </summary>
public sealed class MainBuildingSite : SouthSite
{
    public const string B2 = "qsh:way:687370085";
    public const string FrontSteps = "qsh:way:618752446";

    /// <summary>Front frame: X along B2's south-west facade, +Z outward toward the gate.</summary>
    private static readonly PlanFrame F = PlanFrame.Toward(M.FrontA, M.FrontB);
    /// <summary>Rear frame at b2.rear: X reversed, so +Z is outward from the north-east facade.</summary>
    private static readonly PlanFrame R = new(F.P(M.RearX, M.RearZ), -F.U);

    private static float Xc => (M.StepsX0 + M.StepsX1) * 0.5f;
    private static float Width => M.StepsX1 - M.StepsX0;
    private static float Riser => M.FloorY / (M.RisersLower + M.RisersUpper);
    private static float LowerRise => Riser * M.RisersLower;
    /// <summary>Front edge of the platform (top of the upper flight).</summary>
    private static float PlatformZ => M.StepsZ - (M.RisersLower + M.RisersUpper) * M.Tread - M.MidLanding;

    public override string Id => "18a.main-building";

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(FrontSteps), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            var portico = F.P(Xc + 2.5f, M.ColumnZ - 2.2f);
            var plinth = F.P(M.StepsX0 - 3f, 0.6f);
            return
            [
                Probe("Q1", B2, new(portico.X, M.FloorY + 1.62f, portico.Y), -F.W, 4f, 6f),
                Probe("Q4", B2, new(plinth.X, 1.62f, plinth.Y), -F.W, -25f, 0.5f),
            ];
        }
    }

    public override void Build(SiteBuilder b)
    {
        var y = b.Ground(F.P(Xc, M.StepsZ));
        Platform(b, y);
        FrontSteps_(b, y);
        Portico(b, y);
        RoofGallery(b, b.Ground(F.P(36f, -20f)));
        RearPorch(b, b.Ground(R.Origin));
    }

    // ---- Front platform and grand steps (§2.4: about 17 steps up to the 2.6 m first floor) ----

    private static void Platform(SiteBuilder b, float y)
    {
        float z0 = -0.3f, z1 = PlatformZ, d = z1 - z0, top = y + M.FloorY;
        b.Add(PartGeometryKernel.BeveledBox(Width, M.FloorY - 0.02f, d, 0.03f, L.Stone), F.At(Xc, y - 0.1f, z0 + d * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(Width + 0.06f, 0.14f, d + 0.03f, 0.02f, L.StoneTrim), F.At(Xc, top - 0.14f, z0 + d * 0.5f - 0.015f));
        // A plinth band reads the podium as built stone rather than a bare block.
        b.Add(PartGeometryKernel.BeveledBox(Width + 0.08f, 0.55f, d + 0.04f, 0.02f, L.Granite), F.At(Xc, y - 0.1f, z0 + d * 0.5f - 0.02f));
        // 1.1 m stone balustrade walls on the two open side edges (drop over 1.0 m, WLD-TERRAIN §2.3).
        foreach (var x in new[] { M.StepsX0 + 0.18f, M.StepsX1 - 0.18f })
        {
            b.Add(PartGeometryKernel.BeveledBox(0.36f, 1.0f, d - 0.3f, 0.02f, L.Stone), F.At(x, top, z0 + d * 0.5f - 0.15f));
            b.Add(PartGeometryKernel.BeveledBox(0.46f, 0.1f, d - 0.25f, 0.015f, L.StoneTrim), F.At(x, top + 1.0f, z0 + d * 0.5f - 0.125f));
        }
    }

    private static StairParams Flight(int risers, float width, int variation) =>
        new(width, Riser * risers, M.Tread * risers, risers) { Variation = variation, Material = L.Granite, Foot = FootMat.StonePaving };

    private static readonly StepsPart Steps = new();

    private static void FrontSteps_(SiteBuilder b, float y)
    {
        var upper = Flight(M.RisersUpper, Width, 1);
        var lower = Flight(M.RisersLower, Width, 1);
        float upperZ = PlatformZ + upper.Depth * 0.5f, landing0 = PlatformZ + upper.Depth, landing1 = landing0 + M.MidLanding;
        // Parts climb toward their local +Z; reversed, they climb toward the facade.
        b.Add(Steps.Lods[b.Lod](upper), F.AtReversed(Xc, y + LowerRise, upperZ));
        b.Add(PartGeometryKernel.BeveledBox(Width, LowerRise + 0.1f, M.MidLanding, 0.02f, L.Granite), F.At(Xc, y - 0.1f, (landing0 + landing1) * 0.5f));
        foreach (var x in new[] { M.StepsX0 + 0.15f, M.StepsX1 - 0.15f })
            b.Add(PartGeometryKernel.BeveledBox(0.3f, LowerRise + 0.25f, M.MidLanding, 0.015f, L.StoneTrim), F.At(x, y - 0.1f, (landing0 + landing1) * 0.5f));
        b.Add(Steps.Lods[b.Lod](lower), F.AtReversed(Xc, y, landing1 + lower.Depth * 0.5f));
    }

    // ---- Portico (§2.4): flat roof on four square columns with a pair at each end; round-headed back wall ----

    private static void Portico(SiteBuilder b, float y)
    {
        var floor = y + M.FloorY;
        var columns = M.SingleColumns.Concat(M.PairColumns);
        foreach (var dx in columns) Column(b, Xc + dx, floor, b.Detail);
        var hw = M.PorticoHalfWidth;
        var top = floor + M.PorticoHeight;
        float z0 = -0.3f, z1 = M.PorticoFront, d = z1 - z0;
        b.Add(PartGeometryKernel.BeveledBox(2 * hw, M.EntablatureDepth, d, 0.04f, L.Stone), F.At(Xc, top, z0 + d * 0.5f));
        // Cornice band and coping: a horizontal shadow line under the roof edge.
        b.Add(PartGeometryKernel.BeveledBox(2 * hw + 0.5f, 0.3f, d + 0.25f, 0.03f, L.StoneTrim), F.At(Xc, top + M.EntablatureDepth - 0.3f, z0 + d * 0.5f + 0.125f));
        b.Add(PartGeometryKernel.BeveledBox(2 * hw + 0.3f, 0.45f, d + 0.1f, 0.03f, L.Stone), F.At(Xc, top + M.EntablatureDepth, z0 + d * 0.5f + 0.05f));
        if (b.Detail != SiteDetail.Mass)
            b.Add(PartGeometryKernel.BeveledBox(2 * hw - 0.4f, 0.05f, d - 0.4f, 0.01f, L.RoofSlab), F.At(Xc, top + M.EntablatureDepth + 0.45f, z0 + d * 0.5f));
        BackWall(b, floor);
    }

    private static void Column(SiteBuilder b, float x, float floor, SiteDetail detail)
    {
        var s = M.ColumnSize;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.35f, 0.45f, s + 0.35f, 0.03f, L.StoneTrim), F.At(x, floor, M.ColumnZ));
        b.Add(PartGeometryKernel.BeveledBox(s, M.PorticoHeight - 0.45f - 0.6f, s, 0.05f, L.Stone), F.At(x, floor + 0.45f, M.ColumnZ));
        b.Add(PartGeometryKernel.BeveledBox(s + 0.3f, 0.6f, s + 0.3f, 0.03f, L.StoneTrim), F.At(x, floor + M.PorticoHeight - 0.6f, M.ColumnZ));
        if (detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.1f, 0.12f, s + 0.1f, 0.02f, L.StoneTrim), F.At(x, floor + M.PorticoHeight - 0.85f, M.ColumnZ));
    }

    /// <summary>
    /// The portico's back wall stands in front of B2's facade (z −0.3…2.0) and carries four round-headed windows and the
    /// round-headed door. The openings are 2.3 m deep reveals with glass at the back, which also buries the generic
    /// facade's ground-floor openings behind the 2.6 m platform.
    /// </summary>
    private static void BackWall(SiteBuilder b, float floor)
    {
        var hw = M.PorticoHalfWidth;
        var h = M.PorticoHeight;
        var depth = M.BackWall + 0.3f;
        var door = Shapes.RoundArch(0, 0, M.DoorWidth, M.DoorSpring, 16);
        // Outer ring with the door as a notch in its bottom edge (WallPanel convention).
        var outer = new List<Vector2> { new(-hw, 0), door[0] };
        for (var i = door.Length - 1; i >= 2; i--) outer.Add(door[i]);
        outer.Add(door[1]);
        outer.AddRange([new(hw, 0), new(hw, h), new(-hw, h)]);
        var holes = M.ArchWindows.Select(x => (IReadOnlyList<Vector2>)Shapes.RoundArch(x, M.ArchSill, M.ArchWidth, M.ArchSpring, 12)).ToArray();
        var face = new PolygonRings(outer, holes);
        var centreZ = M.BackWall - depth * 0.5f;
        b.Add(PartGeometryKernel.WallPanel(face, depth, L.Stone), F.At(Xc, floor, centreZ));
        if (b.Detail == SiteDetail.Mass) return;
        var glassZ = 0.45f;
        foreach (var x in M.ArchWindows)
        {
            b.Add(PartGeometryKernel.WallPanel(new PolygonRings(Shapes.RoundArch(x, M.ArchSill, M.ArchWidth, M.ArchSpring, 12)), 0.04f, L.Glass), F.At(Xc, floor, glassZ));
            b.Add(PartGeometryKernel.BeveledBox(M.ArchWidth + 0.4f, 0.12f, 0.35f, 0.02f, L.StoneTrim), F.At(Xc + x, floor + M.ArchSill - 0.12f, M.BackWall + 0.1f));
            if (b.Detail != SiteDetail.Full) continue;
            // Mullion, transom at the springing line and two glazing bars: the window reads as a window, not a hole.
            b.Add(PartGeometryKernel.BeveledBox(0.1f, M.ArchSpring + M.ArchWidth * 0.5f - 0.05f, 0.12f, 0.015f, L.Metal), F.At(Xc + x, floor + M.ArchSill, glassZ + 0.06f));
            foreach (var k in new[] { 0.33f, 0.66f, 1f })
                b.Add(PartGeometryKernel.BeveledBox(M.ArchWidth, 0.08f, 0.1f, 0.012f, L.Metal), F.At(Xc + x, floor + M.ArchSill + M.ArchSpring * k - 0.04f, glassZ + 0.06f));
        }
        // Door: glass vestibule screen at the back, leaves fixed open against the reveals (SYS-WALK §8), stone threshold.
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(door), 0.05f, L.Glass), F.At(Xc, floor, glassZ));
        b.Add(PartGeometryKernel.BeveledBox(M.DoorWidth + 0.2f, 0.12f, 0.15f, 0.015f, L.Metal), F.At(Xc, floor + M.DoorSpring, glassZ + 0.08f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(0.06f, M.DoorSpring - 0.1f, 1.5f, 0.012f, L.Wood), F.At(Xc + side * (M.DoorWidth * 0.5f - 0.05f), floor, glassZ + 0.85f));
        // Entrance presentation (WLD-INTERIORS §4): mat, open sign, door lamp; WP-13 parts, boxes until they land.
        b.Place(PartCatalog.EntranceMat, new PanelParams(2.4f, 0.03f, 1.4f), F.At(Xc, floor, M.BackWall + 1.1f));
        b.Place(PartCatalog.OpenSign, new SignParams(0.42f, 0.24f, 0.02f), F.At(Xc + M.DoorWidth * 0.5f + 0.7f, floor + 1.45f, M.BackWall + 0.02f));
        b.Place(PartCatalog.EntranceLamp, new LampParams(0.3f, 0.3f, 0.2f), F.At(Xc, floor + M.DoorSpring + M.DoorWidth * 0.5f + 0.5f, M.BackWall + 0.1f));
    }

    // ---- Roof gallery (空廊, §2.3): a covered walkway ring of square columns, railing between the outer columns ----

    private static void RoofGallery(SiteBuilder b, float y)
    {
        float x0 = M.GalleryX0, x1 = M.GalleryX1, z0 = M.GalleryZ0, z1 = M.GalleryZ1, walk = M.GalleryWalk, c = M.GalleryColumn;
        var deck = y + M.RoofY;
        var colTop = y + M.TopY - M.GalleryFascia;
        var colHeight = colTop - deck;
        // Walkway deck ring, carried down into the roof so no seam shows against the generic cornice.
        Ring(b, x0, z0, x1, z1, walk, deck - 0.6f, 0.6f, L.Concrete);
        // Fascia ring on top: the crown's horizontal line at 46.1 m.
        Ring(b, x0 - 0.3f, z0 - 0.3f, x1 + 0.3f, z1 + 0.3f, walk + 0.6f, colTop, M.GalleryFascia, L.Stone);
        Ring(b, x0 - 0.4f, z0 - 0.4f, x1 + 0.4f, z1 + 0.4f, walk + 0.8f, colTop + M.GalleryFascia - 0.25f, 0.25f, L.StoneTrim);
        foreach (var p in Perimeter(x0 + c * 0.5f, z0 + c * 0.5f, x1 - c * 0.5f, z1 - c * 0.5f, M.GalleryPitch))
            b.Add(PartGeometryKernel.BeveledBox(c, colHeight, c, 0.03f, L.Stone), F.At(p.X, deck, p.Y));
        foreach (var p in Perimeter(x0 + walk - c * 0.5f, z0 + walk - c * 0.5f, x1 - walk + c * 0.5f, z1 - walk + c * 0.5f, M.GalleryPitch * 2))
            b.Add(PartGeometryKernel.BeveledBox(c, colHeight, c, 0.03f, L.Stone), F.At(p.X, deck, p.Y));
        if (b.Detail == SiteDetail.Mass) return;
        // Railing parapet between outer columns (1.1 m, collision 1.5 m).
        var railing = new RailingPart();
        foreach (var (a, e) in Bays(x0 + c * 0.5f, z0 + c * 0.5f, x1 - c * 0.5f, z1 - c * 0.5f, M.GalleryPitch))
        {
            var mid = (a + e) * 0.5f;
            var length = Vector2.Distance(a, e) - c - 0.02f;
            var along = MathF.Abs(e.X - a.X) > MathF.Abs(e.Y - a.Y);
            var p = new RailingParams(length, 1.1f, 0.1f, 1.2f);
            var at = along ? F.At(mid.X, deck, mid.Y) : F.AtTurned(mid.X, deck, mid.Y);
            b.Add(railing.Lods[b.Lod](p), at);
            b.Add(PartGeometryKernel.BeveledBox(length, 0.15f, 0.3f, 0.02f, L.StoneTrim), at);
        }
        // Plant enclosure inside the ring: 1.8 m perforated aluminium, locked (MAP-QSH-ADDITIONS §2.9).
        if (b.Lod != LodLevel.L4)
        {
            var inset = walk + 0.6f;
            var pen = new[] { F.P(x0 + inset, z1 - inset), F.P(x1 - inset, z1 - inset), F.P(x1 - inset, z0 + inset), F.P(x0 + inset, z0 + inset) };
            b.Add(EquipmentFencePart.Along(pen, true, new RailingParams(1f, 1.8f, 0.06f, 1.3f), b.Lod), Matrix4x4.CreateTranslation(0, deck, 0));
        }
    }

    /// <summary>Four slabs forming a rectangular ring of the given width, as seen in the front frame.</summary>
    private static void Ring(SiteBuilder b, float x0, float z0, float x1, float z1, float width, float y, float height, ToonSpec material)
    {
        float lx = x1 - x0, lz = z1 - z0 - 2 * width;
        b.Add(PartGeometryKernel.BeveledBox(lx, height, width, 0.03f, material), F.At((x0 + x1) * 0.5f, y, z1 - width * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(lx, height, width, 0.03f, material), F.At((x0 + x1) * 0.5f, y, z0 + width * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(width, height, lz, 0.03f, material), F.At(x0 + width * 0.5f, y, (z0 + z1) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(width, height, lz, 0.03f, material), F.At(x1 - width * 0.5f, y, (z0 + z1) * 0.5f));
    }

    /// <summary>Column points around a rectangle (front-frame x, z), corners included, sides divided near the pitch.</summary>
    internal static IEnumerable<Vector2> Perimeter(float x0, float z0, float x1, float z1, float pitch)
    {
        foreach (var (a, _) in Bays(x0, z0, x1, z1, pitch)) yield return a;
    }

    internal static IEnumerable<(Vector2 A, Vector2 B)> Bays(float x0, float z0, float x1, float z1, float pitch)
    {
        Vector2[] corners = [new(x0, z1), new(x1, z1), new(x1, z0), new(x0, z0)];
        for (var s = 0; s < 4; s++)
        {
            var a = corners[s];
            var e = corners[(s + 1) % 4];
            var n = Math.Max(1, (int)MathF.Round(Vector2.Distance(a, e) / pitch));
            for (var k = 0; k < n; k++) yield return (a + (e - a) * (k / (float)n), a + (e - a) * ((k + 1) / (float)n));
        }
    }

    // ---- Rear porch at b2.rear: landing at the 2.6 m floor, an entrance block with a round-headed timber door ----

    private static void RearPorch(SiteBuilder b, float y)
    {
        var floor = y + M.FloorY;
        float lw = M.RearLandingWidth, ld = M.RearLandingDepth;
        // The entrance block spans 7.2 m and stands 2.9 m deep against the facade (z −0.3…2.6).
        const float blockHalf = 3.6f, blockFront = 2.6f, blockTop = 4.8f;
        var landingFront = blockFront + ld;
        b.Add(PartGeometryKernel.BeveledBox(lw, M.FloorY + 0.08f, landingFront + 0.3f, 0.03f, L.Stone), R.At(0, y - 0.1f, (landingFront - 0.3f) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(lw + 0.06f, 0.14f, landingFront + 0.33f, 0.02f, L.StoneTrim), R.At(0, floor - 0.14f, (landingFront - 0.3f) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(lw + 0.08f, 0.55f, landingFront + 0.34f, 0.02f, L.Granite), R.At(0, y - 0.1f, (landingFront - 0.3f) * 0.5f));
        // Side parapets and the two short front parapets beside the steps.
        var stepsHalf = M.RearStepsWidth * 0.5f;
        foreach (var side in new[] { -1f, 1f })
        {
            b.Add(PartGeometryKernel.BeveledBox(0.36f, 1.0f, landingFront + 0.3f, 0.02f, L.Stone), R.At(side * (lw * 0.5f - 0.18f), floor, (landingFront - 0.3f) * 0.5f));
            var frontRun = lw * 0.5f - stepsHalf;
            b.Add(PartGeometryKernel.BeveledBox(frontRun, 1.0f, 0.36f, 0.02f, L.Stone), R.At(side * (stepsHalf + frontRun * 0.5f), floor, landingFront - 0.18f));
        }
        // Entrance block with the door notch; glass screen near its front buries the generic ground-floor canopy.
        var door = Shapes.RoundArch(0, 0, 2.4f, 2.7f, 14);
        var outer = new List<Vector2> { new(-blockHalf, 0), door[0] };
        for (var i = door.Length - 1; i >= 2; i--) outer.Add(door[i]);
        outer.Add(door[1]);
        outer.AddRange([new(blockHalf, 0), new(blockHalf, blockTop), new(-blockHalf, blockTop)]);
        var blockDepth = blockFront + 0.3f;
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(outer), blockDepth, L.Stone), R.At(0, floor, blockFront - blockDepth * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(2 * blockHalf + 0.3f, 0.35f, blockDepth + 0.15f, 0.03f, L.StoneTrim), R.At(0, floor + blockTop, blockFront - blockDepth * 0.5f + 0.075f));
        // Flat canopy on four white round columns over the landing (rear colonnade, §2.4).
        foreach (var x in new[] { -3.3f, -1.15f, 1.15f, 3.3f })
        {
            b.Add(PartGeometryKernel.BeveledBox(0.7f, 0.3f, 0.7f, 0.02f, L.StoneTrim), R.At(x, floor, landingFront - 0.6f));
            b.Add(Shapes.Shaft(M.RearColumnRadius, M.RearColumnRadius * 0.88f, M.RearCanopyY - M.FloorY - 0.6f, b.Detail == SiteDetail.Full ? 20 : 10, L.Stone), R.At(x, floor + 0.3f, landingFront - 0.6f));
        }
        var canopyDepth = landingFront - blockFront + 0.2f;
        b.Add(PartGeometryKernel.BeveledBox(lw - 0.6f, 0.5f, canopyDepth, 0.03f, L.StoneTrim), R.At(0, y + M.RearCanopyY - 0.3f, blockFront + canopyDepth * 0.5f - 0.1f));
        // Steps down to the rear plaza.
        var upper = Flight(M.RisersUpper, M.RearStepsWidth, 0) with { Depth = M.RearTread * M.RisersUpper };
        var lower = Flight(M.RisersLower, M.RearStepsWidth, 0) with { Depth = M.RearTread * M.RisersLower };
        float l0 = landingFront + upper.Depth, l1 = l0 + 1.5f;
        b.Add(Steps.Lods[b.Lod](upper), R.AtReversed(0, y + LowerRise, landingFront + upper.Depth * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(M.RearStepsWidth, LowerRise + 0.1f, 1.5f, 0.02f, L.Granite), R.At(0, y - 0.1f, (l0 + l1) * 0.5f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(0.3f, LowerRise + 0.25f, 1.5f, 0.015f, L.StoneTrim), R.At(side * (stepsHalf - 0.15f), y - 0.1f, (l0 + l1) * 0.5f));
        b.Add(Steps.Lods[b.Lod](lower), R.AtReversed(0, y, l1 + lower.Depth * 0.5f));
        if (b.Detail == SiteDetail.Mass) return;
        // Timber double door, leaves swung back flat against the block face; dark glass screen inside the arch.
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(door), 0.05f, L.Glass), R.At(0, floor, blockFront - 0.15f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(1.15f, 2.65f, 0.07f, 0.015f, L.Wood), R.At(side * 1.85f, floor, blockFront + 0.04f));
        b.Place(PartCatalog.EntranceMat, new PanelParams(1.8f, 0.03f, 1.2f), R.At(0, floor, blockFront + 0.9f));
        b.Place(PartCatalog.OpenSign, new SignParams(0.42f, 0.24f, 0.02f), R.At(1.85f + 0.95f, floor + 1.45f, blockFront + 0.02f));
        b.Place(PartCatalog.EntranceLamp, new LampParams(0.3f, 0.3f, 0.2f), R.At(0, floor + 4.2f, blockFront + 0.1f));
    }

    public override void Collide(SiteColliders c)
    {
        var y = c.Ground(F.P(Xc, M.StepsZ).X, F.P(Xc, M.StepsZ).Y);
        var floor = y + M.FloorY;
        // Front platform top, steps, landing, side balustrades and the portico columns.
        float pz0 = -0.3f, pz1 = PlatformZ;
        c.Box(F.At(Xc, floor - 0.15f, (pz0 + pz1) * 0.5f), new(Width * 0.5f, 0.15f, (pz1 - pz0) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        var upper = Flight(M.RisersUpper, Width, 1);
        var lower = Flight(M.RisersLower, Width, 1);
        float landing0 = PlatformZ + upper.Depth, landing1 = landing0 + M.MidLanding;
        c.Add(Steps.Collision(upper), F.AtReversed(Xc, y + LowerRise, PlatformZ + upper.Depth * 0.5f));
        c.Box(F.At(Xc, y + LowerRise - 0.15f, (landing0 + landing1) * 0.5f), new(Width * 0.5f, 0.15f, M.MidLanding * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        c.Add(Steps.Collision(lower), F.AtReversed(Xc, y, landing1 + lower.Depth * 0.5f));
        foreach (var x in new[] { M.StepsX0 + 0.18f, M.StepsX1 - 0.18f })
            c.Wall(F.P(x, pz0), F.P(x, pz1), 0.18f, floor, floor + 1.5f, SurfaceTag.Barrier, FootMat.StonePaving);
        foreach (var dx in M.SingleColumns.Concat(M.PairColumns))
            c.Box(F.At(Xc + dx, floor + M.PorticoHeight * 0.5f, M.ColumnZ), new(M.ColumnSize * 0.5f + 0.05f, M.PorticoHeight * 0.5f, M.ColumnSize * 0.5f + 0.05f), SurfaceTag.Wall, FootMat.StonePaving);
        // Back wall either side of the open door (the doorway itself belongs to WP-31a's interior entrance).
        var hw = M.PorticoHalfWidth;
        c.Wall(F.P(Xc - hw, M.BackWall), F.P(Xc - M.DoorWidth * 0.5f, M.BackWall), 0.15f, floor, floor + M.PorticoHeight, SurfaceTag.Wall, FootMat.StonePaving);
        c.Wall(F.P(Xc + M.DoorWidth * 0.5f, M.BackWall), F.P(Xc + hw, M.BackWall), 0.15f, floor, floor + M.PorticoHeight, SurfaceTag.Wall, FootMat.StonePaving);
        // Roof gallery: the railing parapet and the plant enclosure; the walkway floor comes with WP-31a's roof interior.
        var deck = c.Ground(F.P(36f, -20f).X, F.P(36f, -20f).Y) + M.RoofY;
        var g = M.GalleryColumn * 0.5f;
        Vector2[] rail = [F.P(M.GalleryX0 + g, M.GalleryZ1 - g), F.P(M.GalleryX1 - g, M.GalleryZ1 - g), F.P(M.GalleryX1 - g, M.GalleryZ0 + g), F.P(M.GalleryX0 + g, M.GalleryZ0 + g)];
        for (var i = 0; i < 4; i++) c.Wall(rail[i], rail[(i + 1) % 4], 0.08f, deck - 1f, deck + 1.5f, SurfaceTag.Barrier, FootMat.Metal);
        var inset = M.GalleryWalk + 0.6f;
        Vector2[] pen = [F.P(M.GalleryX0 + inset, M.GalleryZ1 - inset), F.P(M.GalleryX1 - inset, M.GalleryZ1 - inset), F.P(M.GalleryX1 - inset, M.GalleryZ0 + inset), F.P(M.GalleryX0 + inset, M.GalleryZ0 + inset)];
        for (var i = 0; i < 4; i++) c.Wall(pen[i], pen[(i + 1) % 4], 0.05f, deck, deck + 1.8f, SurfaceTag.Barrier, FootMat.Metal);
        // Rear porch landing, parapets and steps.
        var ry = c.Ground(R.Origin.X, R.Origin.Y);
        var rfloor = ry + M.FloorY;
        var landingFront = 2.6f + M.RearLandingDepth;
        c.Box(R.At(0, rfloor - 0.15f, (landingFront - 0.3f) * 0.5f), new(M.RearLandingWidth * 0.5f, 0.15f, (landingFront + 0.3f) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        // Entrance block: front face either side of the doorway, and its two flanks.
        c.Wall(R.P(-3.6f, 2.6f), R.P(-1.2f, 2.6f), 0.15f, rfloor, rfloor + 4.8f, SurfaceTag.Wall, FootMat.StonePaving);
        c.Wall(R.P(1.2f, 2.6f), R.P(3.6f, 2.6f), 0.15f, rfloor, rfloor + 4.8f, SurfaceTag.Wall, FootMat.StonePaving);
        foreach (var side in new[] { -3.6f, 3.6f })
            c.Wall(R.P(side, -0.3f), R.P(side, 2.6f), 0.15f, rfloor, rfloor + 4.8f, SurfaceTag.Wall, FootMat.StonePaving);
        var stepsHalf = M.RearStepsWidth * 0.5f;
        foreach (var side in new[] { -1f, 1f })
        {
            c.Wall(R.P(side * (M.RearLandingWidth * 0.5f - 0.18f), -0.3f), R.P(side * (M.RearLandingWidth * 0.5f - 0.18f), landingFront), 0.18f, rfloor, rfloor + 1.5f, SurfaceTag.Barrier, FootMat.StonePaving);
            c.Wall(R.P(side * stepsHalf, landingFront - 0.18f), R.P(side * M.RearLandingWidth * 0.5f, landingFront - 0.18f), 0.18f, rfloor, rfloor + 1.5f, SurfaceTag.Barrier, FootMat.StonePaving);
        }
        var rupper = Flight(M.RisersUpper, M.RearStepsWidth, 0) with { Depth = M.RearTread * M.RisersUpper };
        var rlower = Flight(M.RisersLower, M.RearStepsWidth, 0) with { Depth = M.RearTread * M.RisersLower };
        float l0 = landingFront + rupper.Depth, l1 = l0 + 1.5f;
        c.Add(Steps.Collision(rupper), R.AtReversed(0, ry + LowerRise, landingFront + rupper.Depth * 0.5f));
        c.Box(R.At(0, ry + LowerRise - 0.15f, (l0 + l1) * 0.5f), new(stepsHalf, 0.15f, 0.75f), SurfaceTag.Ground, FootMat.StonePaving);
        c.Add(Steps.Collision(rlower), R.AtReversed(0, ry, l1 + rlower.Depth * 0.5f));
    }
}
