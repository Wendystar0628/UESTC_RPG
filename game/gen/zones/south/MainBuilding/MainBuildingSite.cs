using Campus.Gen.Geometry;
using System.Numerics;
using System.Runtime.CompilerServices;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Roof;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.South.Framework;
using M = Campus.Gen.Zones.South.SouthTune.Main;
using L = Campus.Gen.Zones.South.SouthTune.Look;
using W = Campus.Gen.Zones.South.SouthTune.Windows;

namespace Campus.Gen.Zones.South.MainBuilding;

/// <summary>
/// Main building identity parts (MAP-QSH-SOUTH). The wings' massing, storeys and window bays stay with WP-14's facade
/// rules (heights set by HeightOps in overlay/south.d/); this site adds what makes the building read as the main
/// building: the front platform at the 2.6 m first floor with its grand steps, the flat-roofed square-column portico with
/// round-headed windows and door in its back wall, the colonnaded roof gallery (空廊) with railing parapets on B2, and the
/// rear door porch. The front steps are a "building" in the OSM data, so this site claims them.
/// </summary>
public sealed class MainBuildingSite : ZoneSite
{
    public static readonly string B2 = SouthIds.MainB2.Entity;
    public static readonly string FrontSteps = SouthIds.MainSteps.Entity;
    /// <summary>Window grids of the claimed front-steps feature (contract-29); B2's own windows stay WP-14's.</summary>
    public const string FrontPorchKey = "front-porch", RearPorchKey = "rear-porch";

    /// <summary>The rear entrance block: front face out from the facade, its round-headed door.</summary>
    private const float RearBlockFront = 2.6f, RearDoorWidth = 2.4f, RearDoorSpring = 2.7f;
    /// <summary>Glass planes behind the openings: the portico back wall's and the rear block's, in their frames.</summary>
    private const float FrontGlassZ = 0.45f, RearGlassZ = RearBlockFront - 0.15f;

    private static float Riser => M.FloorY / (M.RisersLower + M.RisersUpper);
    private static float LowerRise => Riser * M.RisersLower;

    /// <summary>
    /// The building's plan, read from the map data (D-239). Front frame F: X along B2's south-west facade (the longest edge of its
    /// facade outline), +Z outward toward the gate. Rear frame R at the b2.rear door: X reversed, so +Z is outward from the
    /// north-east facade. The steps zone is the front steps' footprint in F; RoofY is B2's height from the overlay.
    /// </summary>
    internal sealed record Layout(PlanFrame F, PlanFrame R, float StepsX0, float StepsX1, float StepsZ, float RoofY)
    {
        public float Xc => (StepsX0 + StepsX1) * 0.5f;
        public float Width => StepsX1 - StepsX0;
        /// <summary>Front edge of the platform (top of the upper flight).</summary>
        public float PlatformZ => StepsZ - (M.RisersLower + M.RisersUpper) * M.Tread - M.MidLanding;
    }

    private static readonly ConditionalWeakTable<IWorldModel, Layout> Layouts = new();

    internal static Layout LayoutOf(IWorldModel world) => Layouts.GetValue(world, Read);

    private static Layout Read(IWorldModel world)
    {
        var b2 = world.Feature(B2);
        var front = world.ContourFrame(SouthIds.MainFront, originAtEnd: true, forward: false);
        Vector2 Side(string id)
        {
            var edge = world.ContourSegment(id);
            return (front.Local(edge[0]) + front.Local(edge[^1])) * 0.5f;
        }
        var left = Side(SouthIds.MainStepsLeft);
        var right = Side(SouthIds.MainStepsRight);
        var bottom = Side(SouthIds.MainStepsFront);
        var rear = world.Interiors.Get(new(B2))?.Entrances.SingleOrDefault(e => e.Id == "b2.rear")
            ?? throw new InvalidOperationException($"{B2} has no b2.rear entrance in the interior index.");
        var turned = new PlanFrame(Vector2.Zero, -front.U);
        var rearFrame = new PlanFrame(new Vector2(rear.Center.X, rear.Center.Z) - turned.W * RearBlockFront, turned.U);
        return new(front, rearFrame, left.X, right.X, bottom.Y, b2.Height ?? throw new InvalidOperationException($"{B2} has no height."));
    }

    public override string Id => "18a.main-building";

    public override IReadOnlyList<Claim> Claims { get; } = [new FeatureClaim(new(FrontSteps), Aspects.Massing | Aspects.Facade | Aspects.Roof)];

    public override IReadOnlyList<InspectionProbe> Probes =>
    [
        // Q1: on the portico platform beside the door axis, looking at the building; Q4: 3 m west of the platform's plinth (stands, not map data).
        Probe("Q1", B2, new(-346.73f, M.FloorY + WalkRules.StandingEye, 504.50f), new(0.5904f, -0.8071f), 4f, 6f),
        Probe("Q4", B2, new(-367.72f, WalkRules.StandingEye, 482.70f), new(0.5904f, -0.8071f), -25f, 0.5f),
    ];

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        var g = LayoutOf(world);
        return [(new(FrontSteps), new(FrontPorchKey, FrontOpenings(g, 0).Length, 1)), (new(FrontSteps), new(RearPorchKey, RearOpenings(g, 0).Length, 1))];
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var g = LayoutOf(ctx.World);
        var terrain = ctx.World.Terrain;
        var front = FrontOpenings(g, terrain.HeightAt(g.F.P(g.Xc, g.StepsZ).X, g.F.P(g.Xc, g.StepsZ).Y) + M.FloorY);
        var rear = RearOpenings(g, terrain.HeightAt(g.R.Origin.X, g.R.Origin.Y) + M.FloorY);
        WindowNumbers.Declare(windows, front, Allocation(ctx, FrontPorchKey, front.Length, true)!.Value, W.Hall);
        WindowNumbers.Declare(windows, rear, Allocation(ctx, RearPorchKey, rear.Length, true)!.Value, W.Hall);
    }

    /// <summary>
    /// The portico back wall's glazed openings in grid-column order along the front (column 0 at the west end): the four arched
    /// windows and the door's vestibule glass. Each pane is the opening's bounding rectangle; the wall hides its corners.
    /// </summary>
    private static GlazedOpening[] FrontOpenings(Layout g, float floor)
    {
        var outward = new Vector3(g.F.W.X, 0, g.F.W.Y);
        GlazedOpening At(float x, float sill, float width, float height)
        {
            var p = g.F.P(g.Xc + x, FrontGlassZ);
            return new(0, 0, new(p.X, floor + sill, p.Y), outward, width, height);
        }
        var openings = M.ArchWindows.Select(x => At(x, M.ArchSill, M.ArchWidth, M.ArchSpring + M.ArchWidth * 0.5f))
            .Append(At(0, 0, M.DoorWidth, M.DoorSpring + M.DoorWidth * 0.5f))
            .OrderBy(o => g.F.Local(new(o.Origin.X, o.Origin.Z)).X).ToArray();
        return [.. openings.Select((o, i) => o with { Column = i })];
    }

    /// <summary>The rear entrance block's door glass, one cell.</summary>
    private static GlazedOpening[] RearOpenings(Layout g, float floor)
    {
        var p = g.R.P(0, RearGlassZ);
        return [new(0, 0, new(p.X, floor, p.Y), new(g.R.W.X, 0, g.R.W.Y), RearDoorWidth, RearDoorSpring + RearDoorWidth * 0.5f)];
    }

    /// <summary>A porch grid's allocation (null without a snapshot), checked against its opening count.</summary>
    private static WindowAllocation? Allocation(BuildContext ctx, string key, int columns, bool required = false)
    {
        var feature = new FeatureId(FrontSteps);
        var allocation = required ? WindowNumbers.Required(ctx, feature, key) : WindowNumbers.Allocation(ctx, feature, key);
        if (allocation is { } a) WindowNumbers.Fit(columns, 1, feature, key, a);
        return allocation;
    }

    public override void Build(SiteBuilder b)
    {
        var g = LayoutOf(b.Context.World);
        Front(b, g);
        RoofGallery(b, g, b.Ground(g.F.P(36f, -20f)));
        RearPorch(b, g, b.Ground(g.R.Origin));
    }

    private static void Front(SiteBuilder b, Layout g)
    {
        var y = b.Ground(g.F.P(g.Xc, g.StepsZ));
        Platform(b, g, y);
        FrontSteps_(b, g, y);
        Portico(b, g, y);
    }

    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext ctx, LodLevel lod)
    {
        var g = LayoutOf(ctx.World);
        yield return SiteSourceTrace.Build(ctx, lod, Id + "/front", new(FrontSteps), b => Front(b, g), c => CollideFront(c, g),
            [new(g.F.Rect(g.StepsX0, -0.3f, g.StepsX1, g.StepsZ))]);
        yield return SiteSourceTrace.Build(ctx, lod, Id + "/roof-gallery", new(B2),
            b => RoofGallery(b, g, b.Ground(g.F.P(36f, -20f))), c => CollideGallery(c, g),
            [new(g.F.Rect(M.GalleryX0, M.GalleryZ0, M.GalleryX1, M.GalleryZ1),
                [g.F.Rect(M.GalleryX0 + M.GalleryWalk, M.GalleryZ0 + M.GalleryWalk,
                    M.GalleryX1 - M.GalleryWalk, M.GalleryZ1 - M.GalleryWalk)])]);
        yield return SiteSourceTrace.Build(ctx, lod, Id + "/rear-porch", new(B2),
            b => RearPorch(b, g, b.Ground(g.R.Origin)), c => CollideRear(c, g),
            [new(g.R.Rect(-M.RearLandingWidth * 0.5f, -0.3f, M.RearLandingWidth * 0.5f, 2.6f + M.RearLandingDepth))]);
    }

    // ---- Front platform and grand steps (§2.4: about 17 steps up to the 2.6 m first floor) ----

    private static void Platform(SiteBuilder b, Layout g, float y)
    {
        float z0 = -0.3f, z1 = g.PlatformZ, d = z1 - z0, top = y + M.FloorY;
        b.Add(PartGeometryKernel.BeveledBox(g.Width, M.FloorY - 0.02f, d, 0.03f, L.Stone), g.F.At(g.Xc, y - 0.1f, z0 + d * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(g.Width + 0.06f, 0.14f, d + 0.03f, 0.02f, L.StoneTrim), g.F.At(g.Xc, top - 0.14f, z0 + d * 0.5f - 0.015f));
        // A plinth band reads the podium as built stone rather than a bare block.
        b.Add(PartGeometryKernel.BeveledBox(g.Width + 0.08f, 0.55f, d + 0.04f, 0.02f, L.Granite), g.F.At(g.Xc, y - 0.1f, z0 + d * 0.5f - 0.02f));
        // 1.1 m stone balustrade walls on the two open side edges (drop over 1.0 m, WLD-TERRAIN).
        foreach (var x in new[] { g.StepsX0 + 0.18f, g.StepsX1 - 0.18f })
        {
            b.Add(PartGeometryKernel.BeveledBox(0.36f, 1.0f, d - 0.3f, 0.02f, L.Stone), g.F.At(x, top, z0 + d * 0.5f - 0.15f));
            b.Add(PartGeometryKernel.BeveledBox(0.46f, 0.1f, d - 0.25f, 0.015f, L.StoneTrim), g.F.At(x, top + 1.0f, z0 + d * 0.5f - 0.125f));
        }
    }

    private static StairParams Flight(int risers, float width, int variation) =>
        new(width, Riser * risers, M.Tread * risers, risers) { Variation = variation, Material = L.Granite, Foot = FootMat.StonePaving };

    private static readonly StepsPart Steps = new();

    private static void FrontSteps_(SiteBuilder b, Layout g, float y)
    {
        var upper = Flight(M.RisersUpper, g.Width, 1);
        var lower = Flight(M.RisersLower, g.Width, 1);
        float upperZ = g.PlatformZ + upper.Depth * 0.5f, landing0 = g.PlatformZ + upper.Depth, landing1 = landing0 + M.MidLanding;
        // Parts climb toward their local +Z; reversed, they climb toward the facade.
        b.Add(Steps.Lods[b.Lod](upper), g.F.AtReversed(g.Xc, y + LowerRise, upperZ));
        b.Add(PartGeometryKernel.BeveledBox(g.Width, LowerRise + 0.1f, M.MidLanding, 0.02f, L.Granite), g.F.At(g.Xc, y - 0.1f, (landing0 + landing1) * 0.5f));
        foreach (var x in new[] { g.StepsX0 + 0.15f, g.StepsX1 - 0.15f })
            b.Add(PartGeometryKernel.BeveledBox(0.3f, LowerRise + 0.25f, M.MidLanding, 0.015f, L.StoneTrim), g.F.At(x, y - 0.1f, (landing0 + landing1) * 0.5f));
        b.Add(Steps.Lods[b.Lod](lower), g.F.AtReversed(g.Xc, y, landing1 + lower.Depth * 0.5f));
    }

    // ---- Portico (§2.4): flat roof on four square columns with a pair at each end; round-headed back wall ----

    private static void Portico(SiteBuilder b, Layout g, float y)
    {
        var floor = y + M.FloorY;
        var columns = M.SingleColumns.Concat(M.PairColumns);
        foreach (var dx in columns) Column(b, g, g.Xc + dx, floor, b.Detail);
        var hw = M.PorticoHalfWidth;
        var top = floor + M.PorticoHeight;
        float z0 = -0.3f, z1 = M.PorticoFront, d = z1 - z0;
        b.Add(PartGeometryKernel.BeveledBox(2 * hw, M.EntablatureDepth, d, 0.04f, L.Stone), g.F.At(g.Xc, top, z0 + d * 0.5f));
        // Cornice band and coping: a horizontal shadow line under the roof edge.
        b.Add(PartGeometryKernel.BeveledBox(2 * hw + 0.5f, 0.3f, d + 0.25f, 0.03f, L.StoneTrim), g.F.At(g.Xc, top + M.EntablatureDepth - 0.3f, z0 + d * 0.5f + 0.125f));
        b.Add(PartGeometryKernel.BeveledBox(2 * hw + 0.3f, 0.45f, d + 0.1f, 0.03f, L.Stone), g.F.At(g.Xc, top + M.EntablatureDepth, z0 + d * 0.5f + 0.05f));
        if (b.Detail != SiteDetail.Mass)
            b.Add(PartGeometryKernel.BeveledBox(2 * hw - 0.4f, 0.05f, d - 0.4f, 0.01f, L.RoofSlab), g.F.At(g.Xc, top + M.EntablatureDepth + 0.45f, z0 + d * 0.5f));
        BackWall(b, g, floor);
    }

    private static void Column(SiteBuilder b, Layout g, float x, float floor, SiteDetail detail)
    {
        var s = M.ColumnSize;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.35f, 0.45f, s + 0.35f, 0.03f, L.StoneTrim), g.F.At(x, floor, M.ColumnZ));
        b.Add(PartGeometryKernel.BeveledBox(s, M.PorticoHeight - 0.45f - 0.6f, s, 0.05f, L.Stone), g.F.At(x, floor + 0.45f, M.ColumnZ));
        b.Add(PartGeometryKernel.BeveledBox(s + 0.3f, 0.6f, s + 0.3f, 0.03f, L.StoneTrim), g.F.At(x, floor + M.PorticoHeight - 0.6f, M.ColumnZ));
        if (detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.1f, 0.12f, s + 0.1f, 0.02f, L.StoneTrim), g.F.At(x, floor + M.PorticoHeight - 0.85f, M.ColumnZ));
    }

    /// <summary>
    /// The portico's back wall stands in front of B2's facade (z −0.3…2.0) and carries four round-headed windows and the
    /// round-headed door. The openings are 2.3 m deep reveals with glass at the back, which also buries the generic
    /// facade's ground-floor openings behind the 2.6 m platform.
    /// </summary>
    private static void BackWall(SiteBuilder b, Layout g, float floor)
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
        b.Add(PartGeometryKernel.WallPanel(face, depth, L.Stone), g.F.At(g.Xc, floor, centreZ));
        if (b.Detail == SiteDetail.Mass) return;
        var glassZ = FrontGlassZ;
        // Glass: numbered panes once the map's window numbering is in the context, the plain arched glass without one.
        var openings = FrontOpenings(g, floor);
        var numbered = Allocation(b.Context, FrontPorchKey, openings.Length);
        if (numbered is { } allocation) WindowNumbers.Panes(b, openings, allocation, W.Hall, W.Corridor);
        foreach (var x in M.ArchWindows)
        {
            if (numbered is null)
                b.Add(PartGeometryKernel.WallPanel(new PolygonRings(Shapes.RoundArch(x, M.ArchSill, M.ArchWidth, M.ArchSpring, 12)), 0.04f, ZoneLooks.Glass), g.F.At(g.Xc, floor, glassZ));
            b.Add(PartGeometryKernel.BeveledBox(M.ArchWidth + 0.4f, 0.12f, 0.35f, 0.02f, L.StoneTrim), g.F.At(g.Xc + x, floor + M.ArchSill - 0.12f, M.BackWall + 0.1f));
            if (b.Detail != SiteDetail.Full) continue;
            // Mullion, transom at the springing line and two glazing bars: the window reads as a window, not a hole.
            b.Add(PartGeometryKernel.BeveledBox(0.1f, M.ArchSpring + M.ArchWidth * 0.5f - 0.05f, 0.12f, 0.015f, L.Metal), g.F.At(g.Xc + x, floor + M.ArchSill, glassZ + 0.06f));
            foreach (var k in new[] { 0.33f, 0.66f, 1f })
                b.Add(PartGeometryKernel.BeveledBox(M.ArchWidth, 0.08f, 0.1f, 0.012f, L.Metal), g.F.At(g.Xc + x, floor + M.ArchSill + M.ArchSpring * k - 0.04f, glassZ + 0.06f));
        }
        // Door: glass vestibule screen at the back, leaves fixed open against the reveals (SYS-WALK), stone threshold.
        if (numbered is null) b.Add(PartGeometryKernel.WallPanel(new PolygonRings(door), 0.05f, ZoneLooks.Glass), g.F.At(g.Xc, floor, glassZ));
        b.Add(PartGeometryKernel.BeveledBox(M.DoorWidth + 0.2f, 0.12f, 0.15f, 0.015f, L.Metal), g.F.At(g.Xc, floor + M.DoorSpring, glassZ + 0.08f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(0.06f, M.DoorSpring - 0.1f, 1.5f, 0.012f, L.Wood), g.F.At(g.Xc + side * (M.DoorWidth * 0.5f - 0.05f), floor, glassZ + 0.85f));
        // The mat, open sign and door lamp are WP-13's, placed on the b2.front entrance anchor (WLD-INTERIORS).
    }

    // ---- Roof gallery (空廊, §2.3): a covered walkway ring of square columns, railing between the outer columns ----

    private static void RoofGallery(SiteBuilder b, Layout g, float y)
    {
        float x0 = M.GalleryX0, x1 = M.GalleryX1, z0 = M.GalleryZ0, z1 = M.GalleryZ1, walk = M.GalleryWalk, c = M.GalleryColumn;
        var deck = y + g.RoofY;
        var colTop = y + M.TopY - M.GalleryFascia;
        var colHeight = colTop - deck;
        // Walkway deck ring, carried down into the roof so no seam shows against the generic cornice.
        Ring(b, g, x0, z0, x1, z1, walk, deck - 0.6f, 0.6f, L.Concrete);
        // Fascia ring on top: the crown's horizontal line at 46.1 m.
        Ring(b, g, x0 - 0.3f, z0 - 0.3f, x1 + 0.3f, z1 + 0.3f, walk + 0.6f, colTop, M.GalleryFascia, L.Stone);
        Ring(b, g, x0 - 0.4f, z0 - 0.4f, x1 + 0.4f, z1 + 0.4f, walk + 0.8f, colTop + M.GalleryFascia - 0.25f, 0.25f, L.StoneTrim);
        foreach (var p in Perimeter(x0 + c * 0.5f, z0 + c * 0.5f, x1 - c * 0.5f, z1 - c * 0.5f, M.GalleryPitch))
            b.Add(PartGeometryKernel.BeveledBox(c, colHeight, c, 0.03f, L.Stone), g.F.At(p.X, deck, p.Y));
        foreach (var p in Perimeter(x0 + walk - c * 0.5f, z0 + walk - c * 0.5f, x1 - walk + c * 0.5f, z1 - walk + c * 0.5f, M.GalleryPitch * 2))
            b.Add(PartGeometryKernel.BeveledBox(c, colHeight, c, 0.03f, L.Stone), g.F.At(p.X, deck, p.Y));
        if (b.Detail == SiteDetail.Mass) return;
        // Railing parapet between outer columns (1.1 m, collision 1.5 m).
        var railing = new RailingPart();
        foreach (var (a, e) in Bays(x0 + c * 0.5f, z0 + c * 0.5f, x1 - c * 0.5f, z1 - c * 0.5f, M.GalleryPitch))
        {
            var mid = (a + e) * 0.5f;
            var length = Vector2.Distance(a, e) - c - 0.02f;
            var along = MathF.Abs(e.X - a.X) > MathF.Abs(e.Y - a.Y);
            var p = new RailingParams(length, 1.1f, 0.1f, 1.2f);
            var at = along ? g.F.At(mid.X, deck, mid.Y) : g.F.AtTurned(mid.X, deck, mid.Y);
            b.Add(railing.Lods[b.Lod](p), at);
            b.Add(PartGeometryKernel.BeveledBox(length, 0.15f, 0.3f, 0.02f, L.StoneTrim), at);
        }
        // Plant enclosure inside the ring: 1.8 m perforated aluminium, locked (MAP-QSH-ADDITIONS).
        if (b.Lod != LodLevel.L4)
        {
            var inset = walk + 0.6f;
            var pen = new[] { g.F.P(x0 + inset, z1 - inset), g.F.P(x1 - inset, z1 - inset), g.F.P(x1 - inset, z0 + inset), g.F.P(x0 + inset, z0 + inset) };
            b.Add(EquipmentFencePart.Along(pen, true, new RailingParams(1f, 1.8f, 0.06f, 1.3f), b.Lod), Matrix4x4.CreateTranslation(0, deck, 0));
        }
    }

    /// <summary>Four slabs forming a rectangular ring of the given width, as seen in the front frame.</summary>
    private static void Ring(SiteBuilder b, Layout g, float x0, float z0, float x1, float z1, float width, float y, float height, ToonSpec material)
    {
        float lx = x1 - x0, lz = z1 - z0 - 2 * width;
        b.Add(PartGeometryKernel.BeveledBox(lx, height, width, 0.03f, material), g.F.At((x0 + x1) * 0.5f, y, z1 - width * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(lx, height, width, 0.03f, material), g.F.At((x0 + x1) * 0.5f, y, z0 + width * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(width, height, lz, 0.03f, material), g.F.At(x0 + width * 0.5f, y, (z0 + z1) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(width, height, lz, 0.03f, material), g.F.At(x1 - width * 0.5f, y, (z0 + z1) * 0.5f));
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

    private static void RearPorch(SiteBuilder b, Layout g, float y)
    {
        var floor = y + M.FloorY;
        float lw = M.RearLandingWidth, ld = M.RearLandingDepth;
        // The entrance block spans 7.2 m and stands 2.9 m deep against the facade (z −0.3…2.6).
        const float blockHalf = 3.6f, blockFront = RearBlockFront, blockTop = 4.8f;
        var landingFront = blockFront + ld;
        b.Add(PartGeometryKernel.BeveledBox(lw, M.FloorY + 0.08f, landingFront + 0.3f, 0.03f, L.Stone), g.R.At(0, y - 0.1f, (landingFront - 0.3f) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(lw + 0.06f, 0.14f, landingFront + 0.33f, 0.02f, L.StoneTrim), g.R.At(0, floor - 0.14f, (landingFront - 0.3f) * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(lw + 0.08f, 0.55f, landingFront + 0.34f, 0.02f, L.Granite), g.R.At(0, y - 0.1f, (landingFront - 0.3f) * 0.5f));
        // Side parapets and the two short front parapets beside the steps.
        var stepsHalf = M.RearStepsWidth * 0.5f;
        foreach (var side in new[] { -1f, 1f })
        {
            b.Add(PartGeometryKernel.BeveledBox(0.36f, 1.0f, landingFront + 0.3f, 0.02f, L.Stone), g.R.At(side * (lw * 0.5f - 0.18f), floor, (landingFront - 0.3f) * 0.5f));
            var frontRun = lw * 0.5f - stepsHalf;
            b.Add(PartGeometryKernel.BeveledBox(frontRun, 1.0f, 0.36f, 0.02f, L.Stone), g.R.At(side * (stepsHalf + frontRun * 0.5f), floor, landingFront - 0.18f));
        }
        // Entrance block with the door notch; glass screen near its front buries the generic ground-floor canopy.
        var door = Shapes.RoundArch(0, 0, RearDoorWidth, RearDoorSpring, 14);
        var outer = new List<Vector2> { new(-blockHalf, 0), door[0] };
        for (var i = door.Length - 1; i >= 2; i--) outer.Add(door[i]);
        outer.Add(door[1]);
        outer.AddRange([new(blockHalf, 0), new(blockHalf, blockTop), new(-blockHalf, blockTop)]);
        var blockDepth = blockFront + 0.3f;
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(outer), blockDepth, L.Stone), g.R.At(0, floor, blockFront - blockDepth * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(2 * blockHalf + 0.3f, 0.35f, blockDepth + 0.15f, 0.03f, L.StoneTrim), g.R.At(0, floor + blockTop, blockFront - blockDepth * 0.5f + 0.075f));
        // Flat canopy on four white round columns over the landing (rear colonnade, §2.4).
        foreach (var x in new[] { -3.3f, -1.15f, 1.15f, 3.3f })
        {
            b.Add(PartGeometryKernel.BeveledBox(0.7f, 0.3f, 0.7f, 0.02f, L.StoneTrim), g.R.At(x, floor, landingFront - 0.6f));
            b.Add(Shapes.Shaft(M.RearColumnRadius, M.RearColumnRadius * 0.88f, M.RearCanopyY - M.FloorY - 0.6f, b.Detail == SiteDetail.Full ? 20 : 10, L.Stone), g.R.At(x, floor + 0.3f, landingFront - 0.6f));
        }
        var canopyDepth = landingFront - blockFront + 0.2f;
        b.Add(PartGeometryKernel.BeveledBox(lw - 0.6f, 0.5f, canopyDepth, 0.03f, L.StoneTrim), g.R.At(0, y + M.RearCanopyY - 0.3f, blockFront + canopyDepth * 0.5f - 0.1f));
        // Steps down to the rear plaza.
        var upper = Flight(M.RisersUpper, M.RearStepsWidth, 0) with { Depth = M.RearTread * M.RisersUpper };
        var lower = Flight(M.RisersLower, M.RearStepsWidth, 0) with { Depth = M.RearTread * M.RisersLower };
        float l0 = landingFront + upper.Depth, l1 = l0 + 1.5f;
        b.Add(Steps.Lods[b.Lod](upper), g.R.AtReversed(0, y + LowerRise, landingFront + upper.Depth * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(M.RearStepsWidth, LowerRise + 0.1f, 1.5f, 0.02f, L.Granite), g.R.At(0, y - 0.1f, (l0 + l1) * 0.5f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(0.3f, LowerRise + 0.25f, 1.5f, 0.015f, L.StoneTrim), g.R.At(side * (stepsHalf - 0.15f), y - 0.1f, (l0 + l1) * 0.5f));
        b.Add(Steps.Lods[b.Lod](lower), g.R.AtReversed(0, y, l1 + lower.Depth * 0.5f));
        if (b.Detail == SiteDetail.Mass) return;
        // Timber double door, leaves swung back flat against the block face; dark glass screen inside the arch.
        var openings = RearOpenings(g, floor);
        if (Allocation(b.Context, RearPorchKey, openings.Length) is { } allocation) WindowNumbers.Panes(b, openings, allocation, W.Hall, W.Corridor);
        else b.Add(PartGeometryKernel.WallPanel(new PolygonRings(door), 0.05f, ZoneLooks.Glass), g.R.At(0, floor, RearGlassZ));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(1.15f, 2.65f, 0.07f, 0.015f, L.Wood), g.R.At(side * 1.85f, floor, blockFront + 0.04f));
        // The mat, open sign and door lamp are WP-13's, placed on the b2.rear entrance anchor (WLD-INTERIORS).
    }

    public override void Collide(SiteColliders c)
    {
        var g = LayoutOf(c.Context.World);
        CollideFront(c, g);
        CollideGallery(c, g);
        CollideRear(c, g);
    }

    private static void CollideFront(SiteColliders c, Layout g)
    {
        var y = c.Ground(g.F.P(g.Xc, g.StepsZ).X, g.F.P(g.Xc, g.StepsZ).Y);
        var floor = y + M.FloorY;
        // Front platform top, steps, landing, side balustrades and the portico columns.
        float pz0 = -0.3f, pz1 = g.PlatformZ;
        c.Box(g.F.At(g.Xc, floor - 0.15f, (pz0 + pz1) * 0.5f), new(g.Width * 0.5f, 0.15f, (pz1 - pz0) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        var upper = Flight(M.RisersUpper, g.Width, 1);
        var lower = Flight(M.RisersLower, g.Width, 1);
        float landing0 = g.PlatformZ + upper.Depth, landing1 = landing0 + M.MidLanding;
        c.Add(Steps.Collision(upper), g.F.AtReversed(g.Xc, y + LowerRise, g.PlatformZ + upper.Depth * 0.5f));
        c.Box(g.F.At(g.Xc, y + LowerRise - 0.15f, (landing0 + landing1) * 0.5f), new(g.Width * 0.5f, 0.15f, M.MidLanding * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        c.Add(Steps.Collision(lower), g.F.AtReversed(g.Xc, y, landing1 + lower.Depth * 0.5f));
        foreach (var x in new[] { g.StepsX0 + 0.18f, g.StepsX1 - 0.18f })
            c.Wall(g.F.P(x, pz0), g.F.P(x, pz1), 0.18f, floor, floor + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.StonePaving);
        foreach (var dx in M.SingleColumns.Concat(M.PairColumns))
            c.Box(g.F.At(g.Xc + dx, floor + M.PorticoHeight * 0.5f, M.ColumnZ), new(M.ColumnSize * 0.5f + 0.05f, M.PorticoHeight * 0.5f, M.ColumnSize * 0.5f + 0.05f), SurfaceTag.Wall, FootMat.StonePaving);
        // Back wall either side of the open door (the doorway itself belongs to WP-31a's interior entrance).
        var hw = M.PorticoHalfWidth;
        c.Wall(g.F.P(g.Xc - hw, M.BackWall), g.F.P(g.Xc - M.DoorWidth * 0.5f, M.BackWall), 0.15f, floor, floor + M.PorticoHeight, SurfaceTag.Wall, FootMat.StonePaving);
        c.Wall(g.F.P(g.Xc + M.DoorWidth * 0.5f, M.BackWall), g.F.P(g.Xc + hw, M.BackWall), 0.15f, floor, floor + M.PorticoHeight, SurfaceTag.Wall, FootMat.StonePaving);
    }

    private static void CollideGallery(SiteColliders c, Layout g)
    {
        // Roof gallery: the railing parapet and the plant enclosure; the walkway floor comes with WP-31a's roof interior.
        var deck = c.Ground(g.F.P(36f, -20f).X, g.F.P(36f, -20f).Y) + g.RoofY;
        var h = M.GalleryColumn * 0.5f;
        Vector2[] rail = [g.F.P(M.GalleryX0 + h, M.GalleryZ1 - h), g.F.P(M.GalleryX1 - h, M.GalleryZ1 - h), g.F.P(M.GalleryX1 - h, M.GalleryZ0 + h), g.F.P(M.GalleryX0 + h, M.GalleryZ0 + h)];
        for (var i = 0; i < 4; i++) c.Wall(rail[i], rail[(i + 1) % 4], 0.08f, deck - 1f, deck + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.Metal);
        var inset = M.GalleryWalk + 0.6f;
        Vector2[] pen = [g.F.P(M.GalleryX0 + inset, M.GalleryZ1 - inset), g.F.P(M.GalleryX1 - inset, M.GalleryZ1 - inset), g.F.P(M.GalleryX1 - inset, M.GalleryZ0 + inset), g.F.P(M.GalleryX0 + inset, M.GalleryZ0 + inset)];
        for (var i = 0; i < 4; i++) c.Wall(pen[i], pen[(i + 1) % 4], 0.05f, deck, deck + 1.8f, SurfaceTag.Barrier, FootMat.Metal);
    }

    private static void CollideRear(SiteColliders c, Layout g)
    {
        // Rear porch landing, parapets and steps.
        var ry = c.Ground(g.R.Origin.X, g.R.Origin.Y);
        var rfloor = ry + M.FloorY;
        var landingFront = 2.6f + M.RearLandingDepth;
        c.Box(g.R.At(0, rfloor - 0.15f, (landingFront - 0.3f) * 0.5f), new(M.RearLandingWidth * 0.5f, 0.15f, (landingFront + 0.3f) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        // Entrance block: front face either side of the doorway, and its two flanks.
        c.Wall(g.R.P(-3.6f, 2.6f), g.R.P(-1.2f, 2.6f), 0.15f, rfloor, rfloor + 4.8f, SurfaceTag.Wall, FootMat.StonePaving);
        c.Wall(g.R.P(1.2f, 2.6f), g.R.P(3.6f, 2.6f), 0.15f, rfloor, rfloor + 4.8f, SurfaceTag.Wall, FootMat.StonePaving);
        foreach (var side in new[] { -3.6f, 3.6f })
            c.Wall(g.R.P(side, -0.3f), g.R.P(side, 2.6f), 0.15f, rfloor, rfloor + 4.8f, SurfaceTag.Wall, FootMat.StonePaving);
        var stepsHalf = M.RearStepsWidth * 0.5f;
        foreach (var side in new[] { -1f, 1f })
        {
            c.Wall(g.R.P(side * (M.RearLandingWidth * 0.5f - 0.18f), -0.3f), g.R.P(side * (M.RearLandingWidth * 0.5f - 0.18f), landingFront), 0.18f, rfloor, rfloor + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.StonePaving);
            c.Wall(g.R.P(side * stepsHalf, landingFront - 0.18f), g.R.P(side * M.RearLandingWidth * 0.5f, landingFront - 0.18f), 0.18f, rfloor, rfloor + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.StonePaving);
        }
        var rupper = Flight(M.RisersUpper, M.RearStepsWidth, 0) with { Depth = M.RearTread * M.RisersUpper };
        var rlower = Flight(M.RisersLower, M.RearStepsWidth, 0) with { Depth = M.RearTread * M.RisersLower };
        float l0 = landingFront + rupper.Depth, l1 = l0 + 1.5f;
        c.Add(Steps.Collision(rupper), g.R.AtReversed(0, ry + LowerRise, landingFront + rupper.Depth * 0.5f));
        c.Box(g.R.At(0, ry + LowerRise - 0.15f, (l0 + l1) * 0.5f), new(stepsHalf, 0.15f, 0.75f), SurfaceTag.Ground, FootMat.StonePaving);
        c.Add(Steps.Collision(rlower), g.R.AtReversed(0, ry, l1 + rlower.Depth * 0.5f));
    }
}
