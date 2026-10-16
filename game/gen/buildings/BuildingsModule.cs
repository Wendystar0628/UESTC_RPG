using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings;

/// <summary>
/// WP-14 generic building generator: every building that no zone module has claimed gets massing, storeys,
/// bays and numbered windows from the facade rules (MAP-QSH-ADDITIONS §3, ART-MODELING §7.8).
/// Pure .NET: reads only the world model, outputs contract geometry for the WP-02 baker.
/// </summary>
[ContentModule("buildings", Owner = "WP-14")]
public sealed class BuildingsModule : IContentModule
{
    private readonly IColorIds? _colorIds;

    /// <param name="palette">
    /// Colour-name registry for per-vertex palette IDs. Until BuildContext carries it (contract request),
    /// the kernel constructs the module without one and bundles are split per colour family instead.
    /// </param>
    public BuildingsModule(PaletteRegistry? palette = null)
    {
        _colorIds = palette is null ? null : new RegistryColorIds(palette);
    }

    /// <summary>Generic module: claims no specific feature (SYS-MAPS §7).</summary>
    public IReadOnlyList<Claim> Claims { get; } = [];

    /// <summary>
    /// Upper bounds per level cell, measured over the whole campus with the WP-10 openings (heaviest cell: L0 ≈ 624k,
    /// L1 ≈ 141k triangles; campus L0 ≈ 24.7M, L1 ≈ 4.1M, L2 ≈ 0.18M). Draw calls: ten material classes per merged
    /// block (the six of the shell plus sash, curtain fabric, curtain rail and door astragal).
    /// </summary>
    public ModuleBudget Budget { get; } = new(TrianglesL0: 640_000, TrianglesL1: 150_000, DrawCalls: 40);

    public IReadOnlyList<ChannelDecl> Channels { get; } = LightSlots.Declared
        .Select(c => new ChannelDecl(new ChannelId(c.Name), ChannelKind.Scalar, 0f, 1f, ChannelRate.Continuous, c.Name))
        .ToArray();

    /// <summary>
    /// Which buildings this module generates. Mainline: all of them, since no zone module is merged yet.
    /// Once claims are visible to generic modules (contract request), buildings claimed for Massing or Facade are skipped.
    /// </summary>
    public static bool Unclaimed(BuildContext ctx, BuildingPlan plan) => true;

    public IEnumerable<TileKey> Tiles(BuildContext ctx)
    {
        var set = BuildingSet.For(ctx.World, ctx.WorldSeed);
        return set.Plans.Where(p => Unclaimed(ctx, p)).Select(p => p.Tile(LodLevel.L0)).Distinct()
            .OrderBy(t => t.X).ThenBy(t => t.Z).ToArray();
    }

    public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var set = BuildingSet.For(ctx.World, ctx.WorldSeed);
        var meshes = new List<GeometryBundle>();
        var windows = new List<WindowDecl>();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var plan = set.Plans[i];
            if (!Unclaimed(ctx, plan) || plan.Tile(lod) != tile) continue;
            meshes.AddRange(BuildOne(plan, set.Blocks[i], lod));
            // The light inventory needs each window once: declared with the L0 cell only.
            if (lod == LodLevel.L0) windows.AddRange(FacadeOpenings.Declarations(plan, set.Blocks[i]));
        }
        return meshes.Count == 0 ? ContentOutput.Empty : new ContentOutput(meshes, []) { LooseWindows = windows };
    }

    /// <summary>One building at one level, as its own bundles (the baker merges per material and block).</summary>
    public IReadOnlyList<GeometryBundle> BuildOne(BuildingPlan plan, WindowBlock block, LodLevel lod)
    {
        var sink = new MeshSink(_colorIds);
        new BuildingMesher(plan, block, lod, sink).Build();
        return sink.Finish();
    }

    /// <summary>
    /// Wall colliders along the skin (SYS-WALK §13); the entrance span is DoorClosed: closed buildings stay closed (WLD-INTERIORS §5.2).
    /// Collision cells are fixed 64 m cells.
    /// </summary>
    public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile)
    {
        var set = BuildingSet.For(ctx.World, ctx.WorldSeed);
        var list = new List<Collider>();
        foreach (var plan in set.Plans)
        {
            if (!Unclaimed(ctx, plan) || plan.Tile(LodLevel.L0) != tile) continue;
            var y0 = plan.SunkY;
            var y1 = plan.EdgeTopY;
            var fi = 0;
            for (var r = 0; r < plan.RingCount; r++)
            {
                var skin = plan.SkinAt(r);
                for (var e = 0; e < skin.Count; e++, fi++)
                {
                    var a = skin.Points[e];
                    var b = skin.Points[(e + 1) % skin.Count];
                    var inward = -skin.Normal[e] * 0.15f;
                    if (fi == plan.EntranceFacade)
                    {
                        var door = plan.Openings[fi].First(o => o.Kind == BayKind.Door);
                        var d = skin.Dir[e];
                        var p0 = a + d * door.U0;
                        var p1 = a + d * door.U1;
                        Add(a, p0, SurfaceTag.Wall);
                        Add(p0, p1, SurfaceTag.DoorClosed);
                        Add(p1, b, SurfaceTag.Wall);
                    }
                    else
                    {
                        Add(a, b, SurfaceTag.Wall);
                    }

                    void Add(Vector2 p, Vector2 q, SurfaceTag tag)
                    {
                        if (Vector2.Distance(p, q) < 0.05f) return;
                        list.Add(new WallCollider(p + inward, q + inward, 0.15f, y0, y1, tag) { Foot = Contracts.Audio.FootMat.Concrete });
                    }
                }
            }
        }
        return list;
    }
}
