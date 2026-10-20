using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using W = Campus.Gen.Maps.Qingshuihe.West.WestTune.Windows;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>
/// 六号科研楼 (MAP-QSH-WEST), a landmark built by this zone instead of WP-14: blocks A (9 floors, the tallest), B and C
/// (7 floors) each stand on a two-storey dark glass base set back under the stone, with stone columns in the stone line;
/// above it deep square windows in a warm grey stone grid and one continuous dark louvre strip per floor. C carries the
/// crown sign over its east face (red emblem, silver characters) and the open east door (C 区东门, interiors.json six.c.east):
/// a glass vestibule out to the stone line, doors standing open, a granite threshold apron. The A–B link is two storeys of glass;
/// the B–C link is the 2F outdoor bridge over the drive-through, reaching B and C on narrow decks; the north tower is the
/// stone stair core. Every other entrance is shut: glass doors, a lobby card behind them, DoorClosed. Lit and shaded by the
/// live sun only; nothing here keys on the clock (§1.5). Each stone block is one named window grid (contract-29); the base
/// glass stays unnumbered.
/// </summary>
public sealed partial class SixSite : ZoneSite
{
    /// <summary>One stone block: its entity, its source part, its window grid key and, for C only, its own floors above the base.</summary>
    internal sealed record Block(string Entity, string Part, string Key, int? Fixed = null)
    {
        /// <summary>Floors above the glass base: the entity's storeys in the world model less the base (D-239), or C's own count.</summary>
        public int Upper(IWorldModel world)
        {
            if (Fixed is { } floors) return floors;
            return world.Features.TryGetValue(new(Entity), out var feature) && feature.Storeys is { } storeys
                ? storeys - S.BaseStoreys
                : throw new InvalidOperationException($"六号科研楼 entity {Entity} has no storeys in the world model.");
        }
    }

    internal static readonly IReadOnlyList<Block> Blocks =
        [new(S.EntityA, S.PartA, "a"), new(S.EntityB, S.PartB, "b"), new(S.EntityC, S.PartC, "c", S.UpperC)];

    /// <summary>The blocks' plan frame, X along <see cref="S.Axis"/>.</summary>
    internal static readonly PlanFrame Frame = PlanFrame.Along(Vector2.Zero, S.Axis);

    public override string Id => "19a.six";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [.. new[] { S.EntityA, S.EntityB, S.EntityC }.Select(id => SiteRanges.Bounds(world.Feature(id), S.ApronDepth)),
         .. Bridges(world).Select(bridge => SiteRanges.Bounds([bridge.From, bridge.To], S.Recess))];

    public override IReadOnlyList<Claim> Claims =>
        [.. new[] { S.EntityA, S.EntityB, S.EntityC }.Select(e => (Claim)new FeatureClaim(new(e), Aspects.Massing | Aspects.Facade | Aspects.Roof))];

    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel lod)
    {
        foreach (var block in Blocks)
        {
            var contours = new List<PolygonRings> { Plan(context.World, block.Entity, block.Part) };
            if (block.Entity == S.EntityC)
                contours.AddRange(new[] { S.LinkAB, S.LinkBC, S.Tower }.Select(part => Plan(context.World, S.EntityC, part)));
            yield return SiteSourceTrace.Build(context, lod, $"{Id}/{block.Key}", new(block.Entity),
                b => Build(b, block.Entity), c => Collide(c, block.Entity), contours);
        }
    }

    // ---- Inspection probes (MAP-QSH-WEST Q4, Q8a, Q8c) ----

}
