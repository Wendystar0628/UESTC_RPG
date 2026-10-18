using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.West.WestTune.Look;
using S = Campus.Gen.Zones.West.WestTune.Six;
using W = Campus.Gen.Zones.West.WestTune.Windows;

namespace Campus.Gen.Zones.West.Six;

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

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            // Probe pins are fixed coordinates (no world access here). Q4 (ground-floor close-up, K1): 0.5 m from C's set-back
            // glass beside the east door (six.c.east), on the walk, looking along it.
            var door = new Vector2(-588.3f, 230f);
            var outward = Angles.Heading(77f);
            var along = new Vector2(-outward.Y, outward.X);
            var eye4 = door + outward * (-S.Recess + 0.5f) + along * (S.VestibuleWidth * 0.5f + 2.0f);
            var q4 = Probe("Q4", S.EntityC, new(eye4.X, WalkRules.StandingEye, eye4.Y), along, 6f, 0.5f);
            // Q8a (baseline, fly only): 2 m over A's parapet at the corner of its two wings, looking along the south wing.
            var corner = new Vector2(-840.26f, 133.59f) + S.Axis * 3f + new Vector2(-S.Axis.Y, S.Axis.X) * 3f;
            var q8a = Probe("Q8a", S.EntityA, new(corner.X, 41.5f + 2f, corner.Y), S.Axis, -20f, 2f);
            // Q8c (baseline, fly only): 2 m below ground at C's corner by the motto plaza, looking up at the corner.
            var c8 = new Vector2(-583.28f, 251.04f) + outward * 1.5f;
            var q8c = Probe("Q8c", S.EntityC, new(c8.X, -2f, c8.Y), -outward, 30f, 1.5f);
            return [q4, q8a, q8c];
        }
    }
}
