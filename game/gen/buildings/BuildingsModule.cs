using Campus.Gen.Geometry;
using Campus.Gen.Parts.Roof;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Parts.Circulation;
using Campus.Contracts.Bake;
using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.World;

namespace Campus.Gen.Buildings;

/// <summary>
/// WP-14 generic building generator: every building that no zone module has claimed gets massing, storeys,
/// bays and numbered windows from the facade rules (MAP-QSH-ADDITIONS, ART-MODELING).
/// Pure .NET: reads only the world model, outputs contract geometry for the WP-02 baker.
/// </summary>


[ContentModule("buildings", Owner = "WP-14")]
public sealed class BuildingsModule : IContentModule, IWindowNumberingProvider, IWindowMetadataSource, IContentSourceTraceProvider
{
    public BakeInputDependencies BakeInputs { get; } = new(BakeInputKinds.World | BakeInputKinds.Windows, [SurfaceMaterialCatalog.SourcePath], []);
    public BakeInputDependencies CollisionInputs => BakeInputDependencies.World;

    /// <summary>Generic module: claims no specific feature (SYS-MAPS).</summary>
    public IReadOnlyList<Claim> Claims { get; } = [];

    /// <summary>
    /// Upper bounds per level cell, measured over the whole campus with the WP-10 openings and the WP-12 roof parts while
    /// the exported fidelity data is still the all-K3 stub (heaviest cell: L0 ≈ 125.5k, L1 ≈ 123.3k triangles; campus
    /// L0 ≈ 6.0M, L1 ≈ 3.6M, L2 ≈ 0.20M). Re-measure once WP-44 exports real zones: K1/K2 facades take the full parts.
    /// Draw calls: about fourteen material classes per merged block (the six of the shell, sash, curtain fabric, curtain
    /// rail and door astragal, and the roof plant's fence board, casing, curved shroud and pipe).
    /// </summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: 130_000, TrianglesL1: 130_000, DrawCalls: 40);

    public IReadOnlyList<ChannelDecl> Channels { get; } = LightSlots.Declared
        .Select(c => new ChannelDecl(new ChannelId(c.Name), ChannelKind.Scalar, 0f, 1f, ChannelRate.Continuous, c.Name))
        .ToArray();

    /// <summary>
    /// Which buildings this module generates: those whose massing and facade no zone module has claimed (the map's
    /// resolved claims ledger, BuildContext.ClaimedBy). A facade-claimed feature has no generic window block either, and
    /// no WP-14 door, entrance parts or colliders at its interior-index anchors: the zone pack builds those doors.
    /// </summary>
    public static bool Unclaimed(BuildContext ctx, BuildingPlan plan) =>
        ctx.ClaimedBy(plan.Id, Aspects.Massing) is null && ctx.ClaimedBy(plan.Id, Aspects.Facade) is null;

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        var set = BuildingSet.For(ctx);
        return set.Plans.Where(p => Unclaimed(ctx, p)).Select(p => p.Tile(LodLevel.L0)).Distinct()
            .OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var set = BuildingSet.For(ctx);
        WindowNumberingSnapshot? numbering = null;
        var meshes = new List<GeometryBundle>();
        var windows = new List<WindowDecl>();
        var notes = new List<BuildDiagnostic>();
        foreach (var plan in set.Plans)
        {
            if (!Unclaimed(ctx, plan) || plan.Tile(lod) != tile) continue;
            // Glass reads the map's one allocation; nothing is numbered here.
            var block = WindowNumbering.BlockOf(numbering ??= WindowNumbering.For(ctx), plan);
            meshes.AddRange(BuildOne(plan, block, lod, ctx.Catalog));
            // The light inventory needs each window once: declared with the L0 cell only; so is the log.
            if (lod != LodLevel.L0) continue;
            windows.AddRange(FacadeOpenings.Declarations(plan, block));
            notes.AddRange(DoorsAlone(ctx, plan));
            notes.AddRange(UnknownPreset(plan));
        }
        return meshes.Count == 0 ? ContentOutput.Empty : new ContentOutput(meshes, []) { LooseWindows = windows, Diagnostics = notes };
    }

    /// <summary>A facade preset key (WorldFeature.Facade) no WP-14 preset has: the family profile stays, logged as a warning.</summary>
    public static IEnumerable<BuildDiagnostic> UnknownPreset(BuildingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Feature.Facade is { Value: { Length: > 0 } key } && plan.Profile.Preset is null)
            yield return new BuildDiagnostic(DiagnosticLevel.Warning, "wp14.preset.unknown",
                $"Facade preset '{key}' of {plan.Id.Value} ({plan.Key}) is not a WP-14 preset: the {plan.Style} family profile stays.", plan.Id);
    }

    /// <summary>
    /// The log list of the anchor doors a building opens without a landing (a porch or deck outside, EntranceRules.DeckAbove,
    /// or an upper floor): one Info diagnostic each, naming the package expected to build what the door opens onto, read
    /// from the claims and the zoning: the claimant of the block standing under the outside landing (its deck), else the zone
    /// pack claiming the ground or props of this building, else the pack of the building's zone; undecided when none says.
    /// </summary>
    public static IEnumerable<BuildDiagnostic> DoorsAlone(BuildContext ctx, BuildingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(plan);
        var world = ctx.World;
        var anchors = world.Interiors.Get(plan.Id)?.Entrances ?? [];
        foreach (var e in plan.Entrances.Where(x => x.Anchor is not null && !x.Landing))
        {
            var decl = anchors.First(a => a.Id == e.Anchor);
            var above = decl.Outside.Y - world.Terrain.HeightAt(decl.Outside.X, decl.Outside.Z);
            var expected = BuildingPlanner.DeckUnder(world, plan.Id, new(decl.Outside.X, decl.Outside.Z), decl.Outside.Y) is { } deck
                ? ctx.ClaimedBy(deck, Aspects.Massing) is { } builder ? $"{builder.Value}, which builds the deck block {deck.Value}"
                    : $"no one yet: the deck block {deck.Value} is a plain WP-14 building"
                : (ctx.ClaimedBy(plan.Id, Aspects.Ground) ?? ctx.ClaimedBy(plan.Id, Aspects.Props)) is { } around ? $"{around.Value}, which claims the ground round it"
                : plan.Feature.Zone is { } zone ? $"the {zone.Value} zone pack (zoning)"
                : "no one yet (undecided)";
            yield return new BuildDiagnostic(DiagnosticLevel.Info, "wp14.door.alone", string.Create(CultureInfo.InvariantCulture,
                $"Door {e.Anchor} of {plan.Id.Value} ({plan.Key}, storey {e.Row + 1}) opens alone, its outside landing {above:0.00} m above the terrain: porch or deck expected from {expected}."),
                plan.Id);
        }
    }

    /// <summary>
    /// The map-wide window allocation (contract-29), called once per map by the kernel before any Build: WP-14's
    /// generic grids with each claimed layout replacing its whole feature. Pure: no geometry, IO or cached state.
    /// </summary>
    public WindowNumberingSnapshot PrepareWindowNumbering(BuildContext prepass, IReadOnlyList<ClaimedWindowLayout> validatedLayouts) =>
        WindowNumbering.Prepare(prepass, validatedLayouts);

    /// <summary>
    /// Every generic window of the map from the published snapshot, without geometry: the same declarations as the
    /// L0 cells' LooseWindows. WP-14 declares no claimed layouts (DescribeWindowLayouts stays empty).
    /// </summary>
    public WindowMetadata DescribeWindows(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var numbering = WindowNumbering.For(context);
        var windows = new List<WindowDecl>();
        foreach (var plan in BuildingSet.For(context).Plans)
            if (Unclaimed(context, plan)) windows.AddRange(FacadeOpenings.Declarations(plan, WindowNumbering.BlockOf(numbering, plan)));
        return new WindowMetadata([], windows);
    }

    /// <summary>One building at one level, as its own bundles (the baker merges per material and block).</summary>
    public IReadOnlyList<GeometryBundle> BuildOne(BuildingPlan plan, WindowBlock block, LodLevel lod, PaletteCatalog catalog)
    {
        var sink = new GeometrySink(catalog);
        new BuildingMesher(plan, block, lod, sink).Build();
        return sink.Finish();
    }

    /// <summary>
    /// Wall colliders along the skin (SYS-WALK); declared open entrances cut their authored doorway range,
    /// while closed entrances stay DoorClosed. The WP-11 landings, slopes and steps bring their own colliders.
    /// Collision cells are fixed 64 m cells.
    /// </summary>
    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        var set = BuildingSet.For(ctx);
        return set.Plans.Where(plan => Unclaimed(ctx, plan) && plan.Tile(LodLevel.L0) == tile)
            .SelectMany(plan => CollideOne(ctx, plan)).ToList();
    }

    /// <summary>The colliders of one building in world coordinates, in the order <see cref="Collide"/> lists them; no render mesh is built.</summary>
    private static List<Collider> CollideOne(BuildContext ctx, BuildingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var list = new List<Collider>();
        var y0 = plan.SunkY;
        var y1 = plan.EdgeTopY;
        var entry = ctx.World.Interiors.Get(plan.Id);
        var fi = 0;
        for (var r = 0; r < plan.RingCount; r++)
        {
            var skin = plan.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++)
            {
                var a = skin.Points[e];
                var b = skin.Points[(e + 1) % skin.Count];
                var inward = -skin.Normal[e] * 0.15f;
                var openings = new List<EntranceWallOpening>();
                foreach (var entrance in plan.Entrances.Where(x => x.Facade == fi))
                {
                    var door = plan.DoorOf(entrance);
                    var declaration = entrance.Anchor is { } anchor ? entry?.Entrances.FirstOrDefault(e => e.Id == anchor) : null;
                    openings.Add(new(door.U0, door.U1, door.Y0, door.Y1, EntranceWallOpenings.IsOpen(entry, declaration)));
                }
                list.AddRange(EntranceWallOpenings.Cut(new WallCollider(a + inward, b + inward, 0.15f, y0, y1, SurfaceTag.Wall)
                    { Foot = Contracts.Audio.FootMat.Concrete }, openings));
            }
        }
        foreach (var entrance in plan.Entrances) list.AddRange(EntranceKit.Colliders(EntranceKit.Pieces(plan, entrance)));
        return list;
    }

    /// <summary>Complete ordinary buildings with the same production meshes, colliders and directed skin contours.</summary>
    public IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel level)
    {
        ArgumentNullException.ThrowIfNull(context);
        var set = BuildingSet.For(context);
        WindowNumberingSnapshot? numbering = null;
        foreach (var plan in set.Plans)
        {
            if (!Unclaimed(context, plan)) continue;
            var block = WindowNumbering.BlockOf(numbering ??= WindowNumbering.For(context), plan);
            var footprint = new PolygonRings(Array.AsReadOnly(plan.SkinOuter.Points.ToArray()),
                Array.AsReadOnly(plan.SkinHoles.Select(h => (IReadOnlyList<Vector2>)Array.AsReadOnly(h.Points.ToArray())).ToArray()));
            yield return new ContentSourceTrace($"building/{plan.Id.Value}/{plan.Key}", plan.Id,
                new ContentOutput(BuildOne(plan, block, level, context.Catalog), []), CollideOne(context, plan), [footprint]);
        }
    }
}
