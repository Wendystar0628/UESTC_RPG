using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurniturePlanner;
using static Campus.Gen.Furniture.FurnitureParts;

namespace Campus.Gen.Furniture;

/// <summary>
/// The network lines of one tier and one name that get lamps and rest stops by one <see cref="TierRule"/>, with the features of their own the
/// map's overlay gives that road (<see cref="FurnitureStyle.RestNodeKind"/>, <see cref="FurnitureStyle.KerbRampsKind"/>), if any.
/// <paramref name="Key"/> names them in item keys. <paramref name="Nodes"/> are the places where its rest stops stand instead of along its line.
/// </summary>
internal sealed record RoadGroup(string Key, RoadTier Tier, string? Name, IReadOnlyList<NetworkLine> Lines, IReadOnlyList<RestNode> Nodes, bool KerbRamps);

/// <summary>A rest node of a road: a place on the map (a plaza edge, a junction) where the road's rest stops stand. <paramref name="At"/> is its plan position in map metres.</summary>
internal sealed record RestNode(FeatureId Id, Vector2 At);

/// <summary>Groups furnished roads and places their tier-ordered lamp rhythm.</summary>
internal static class FurnitureRoads
{
    /// <summary>
    /// The roads that get lamps, grouped: the network lines of one tier (<see cref="FurnitureStyle.TierOf"/>, by each line's own road
    /// class) and one name (the unnamed ones of a tier make one group), in tier then name order, so a group's key is stable for one map.
    /// A line under cover stays out. A road the planting rules give <see cref="FurnitureStyle.AvenueRows"/> offsets is tree-lined, the avenue.
    /// A road's own features come from the world's overlay features that name it in their <c>road</c> prop, nodes in ID order.
    /// </summary>
    internal static IReadOnlyList<RoadGroup> Groups(IWorldModel world, IReadOnlyList<RoadRows> rows)
    {
        var nodes = world.Features.Values.Where(f => f.Kind.Value == S.RestNodeKind && f.Geometry is PointGeometry)
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Select(f => (Road: WorldProps.TryText(f.Props, "road"), Node: new RestNode(f.Id, new(((PointGeometry)f.Geometry).Position.X, ((PointGeometry)f.Geometry).Position.Z))))
            .Where(n => n.Road is not null).ToLookup(n => n.Road!, n => n.Node, StringComparer.Ordinal);
        var ramped = world.Features.Values.Where(f => f.Kind.Value == S.KerbRampsKind)
            .Select(f => WorldProps.TryText(f.Props, "road")).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var treeLined = rows.Where(r => r.Offsets.Count >= S.AvenueRows).Select(r => r.Name).ToHashSet(StringComparer.Ordinal);
        var graded = world.Network.Roads
            .Where(l => l.Line.Points.Count >= 2 && !StreetNetwork.Covered(l))
            .Select(l => (Line: l, Name: StreetNetwork.RoadName(l)))
            .Select(g => (g.Line, g.Name, Tier: S.TierOf(StreetNetwork.RoadClass(g.Line), g.Name is { } name && treeLined.Contains(name))))
            .Where(g => g.Tier is not null)
            .GroupBy(g => (Tier: g.Tier!.Value, Name: g.Name ?? ""))
            .OrderBy(g => g.Key.Tier).ThenBy(g => g.Key.Name, StringComparer.Ordinal);
        var groups = new List<RoadGroup>();
        var inTier = new Dictionary<RoadTier, int>();
        foreach (var g in graded)
        {
            var index = inTier.GetValueOrDefault(g.Key.Tier);
            inTier[g.Key.Tier] = index + 1;
            var name = g.Key.Name.Length > 0 ? g.Key.Name : null;
            groups.Add(new($"{g.Key.Tier}{Num(index)}", g.Key.Tier, name, [.. g.Select(t => t.Line).OrderBy(l => l.Id.Value, StringComparer.Ordinal)],
                name is null ? [] : [.. nodes[name]], name is not null && ramped.Contains(name)));
        }
        return groups.AsReadOnly();
    }

    internal static void Plan(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics, RoadGroup group)
    {
        var rule = S.Rule(group.Tier);
        var half = group.Lines.Max(StreetNetwork.HalfWidth);
        var spacing = rule.LampSpacing;
        var lamp = rule.DiscLamp ? S.MainLamp : S.SecondaryLamp;
        var nodes = group.Nodes;
        var chains = StreetNetwork.Chains(group.Lines);
        for (var c = 0; c < chains.Count; c++)
        {
            var run = new StreetNetwork.Run(chains[c]);
            var n = (int)MathF.Floor(run.Length / spacing);
            if (n < 1) continue;
            var s0 = (run.Length - (n - 1) * spacing) * 0.5f;
            // Both sides lit: opposite each other; else the side with more free slots.
            float[] lampSides = rule.BothSides ? [1f, -1f] : [BestSide(site, run, s0, spacing, n, half)];
            foreach (var side in lampSides)
                for (var k = 0; k < n; k++)
                    PlaceLamp(site, items, run, s0 + k * spacing, side, half, lamp, $"lamp:{group.Key}:{Num(c)}:{Side(side)}:{Num(k)}");
            // Rest stops: every RestEvery lamp gaps on a side; two lit sides alternate, a one-sided road's sit opposite its lamps.
            // A road with rest nodes (Opus review 16:23) has none along its line: its stops are at the nodes.
            float[] restSides = nodes.Count > 0 || rule.RestEvery == 0 ? [] : rule.BothSides ? [1f, -1f] : [-lampSides[0]];
            foreach (var side in restSides)
            {
                var phase = rule.BothSides && side < 0f ? rule.RestEvery / 2 : 0;
                int stops = 0, bins = 0;
                for (var k = 0; k + 1 < n; k++)
                {
                    if ((k + phase) % rule.RestEvery != rule.RestEvery / 2) continue;
                    var key = $"rest:{group.Key}:{Num(c)}:{Side(side)}:{Num(k)}";
                    // A bin at every RestBinEvery-th stop; one that found no room passes to the next stop.
                    var (bench, bin) = FurnitureRestStops.Place(site, items, run, s0 + (k + 0.5f) * spacing, side, half, withBin: bins * S.RestBinEvery <= stops, key);
                    if (bench) stops++;
                    if (bin) bins++;
                }
            }
        }
        if (nodes.Count > 0) FurnitureNodeSeating.Plan(site, items, diagnostics, group.Key, group.Name!, nodes);
    }

    private static float BestSide(StreetSite site, StreetNetwork.Run run, float s0, float spacing, int n, float half)
    {
        int Count(float side)
        {
            var free = 0;
            for (var k = 0; k < n; k++)
                if (run.Normal(s0 + k * spacing) is { } normal && site.Free(run.At(s0 + k * spacing) + normal * (side * (half + S.LampKerbGap)), PoleRadius)) free++;
            return free;
        }
        return Count(-1f) > Count(1f) ? -1f : 1f;
    }

    private static void PlaceLamp(StreetSite site, List<FurnitureItem> items, StreetNetwork.Run run, float s, float side, float half, LampParams p, string key)
    {
        var terrain = site.World.Terrain;
        foreach (var shift in Shifts(S.LampMaxShift))
        {
            var at = s + shift;
            if (at < 0f || at > run.Length || run.Normal(at) is not { } normal) continue;
            var pos = run.At(at) + normal * (side * (half + S.LampKerbGap));
            if (!site.Free(pos, PoleRadius) || Land.Point(terrain, pos, PoleRadius) is not { } y) continue;
            // The front (−Z) faces the road, so a single-arm head reaches over the carriageway.
            var frame = Frame(pos, -normal * side, y);
            items.Add(Item(key, FurnitureKind.StreetLamp, PartCatalog.StreetLamp, p, frame, Lamp.Collision(p), [StreetLampPart.Emitter(p, frame)]));
            site.Occupy(pos, PoleRadius);
            return;
        }
    }
}
