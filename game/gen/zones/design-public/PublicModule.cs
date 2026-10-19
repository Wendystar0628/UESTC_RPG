using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.DesignPublic;

/// <summary>WP-29 pocket greens from world/public.json; paving and trees remain with their respective producers.</summary>
[ContentModule("zone.design-public", Owner = "WP-29")]
public sealed class PublicModule : ZoneModuleBase
{
    private static readonly IReadOnlyList<ZoneSite> Sites = [new PocketGreenSite()];

    public PublicModule() : base(Sites, new(TrianglesL0: 12_000, TrianglesL1: 6_000, DrawCalls: 4),
        GeometryMergePolicy.MaterialGroups) { }

    public override ContentOutput Build(BuildContext context, TileKey tile, LodLevel lod)
    {
        var output = base.Build(context, tile, lod);
        if (lod != LodLevel.L0 || output.Meshes.Count == 0) return output;
        float Ground(Vector2 p) => context.World.Terrain.HeightAt(p.X, p.Y);
        var seats = PocketGreens.Benches(context.World)
            .Where(bench => TileGrid.At(bench.Centre) == tile)
            .SelectMany(bench => PocketGreens.Seats(bench, Ground)).ToArray();
        return output with { Seats = seats };
    }
}

internal sealed class PocketGreenSite : ZoneSite
{
    public override string Id => "29a.pocket-greens";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [.. PocketGreens.Benches(world).Select(bench => SiteRanges.Point(bench.Centre,
                bench.Radius + PublicTune.Bench.PostOffset + PublicTune.Bench.PlinthLength)),
            .. PocketGreens.Pits(world).Select(pit => SiteRanges.Point(pit.Centre, pit.Radius))];

    public override void Build(SiteBuilder builder)
    {
        if (builder.Lod is LodLevel.L3 or LodLevel.L4) return;
        var meshes = new List<GeometryBundle>();
        foreach (var bench in PocketGreens.Benches(builder.Context.World))
        {
            if (!builder.Includes(bench.Centre)) continue;
            PocketGreens.Build(bench, builder.Ground, builder.Lod, meshes);
            foreach (var mesh in meshes) builder.Add(mesh, Matrix4x4.Identity, bench.Centre);
            meshes.Clear();
        }
        foreach (var pit in PocketGreens.Pits(builder.Context.World))
        {
            if (!builder.Includes(pit.Centre)) continue;
            PocketGreens.Build(pit, builder.Ground, builder.Lod, meshes);
            foreach (var mesh in meshes) builder.Add(mesh, Matrix4x4.Identity, pit.Centre);
            meshes.Clear();
        }
    }

    public override void Collide(SiteColliders colliders)
    {
        foreach (var bench in PocketGreens.Benches(colliders.Context.World))
        {
            if (!colliders.Includes(bench.Centre)) continue;
            foreach (var box in PocketGreens.SeatBoxes(bench, colliders.Ground)) colliders.Add(box, bench.Centre);
        }
        foreach (var pit in PocketGreens.Pits(colliders.Context.World))
        {
            if (!colliders.Includes(pit.Centre)) continue;
            colliders.Add(PocketGreens.PitPost(pit, colliders.Ground), pit.Centre);
        }
    }
}
