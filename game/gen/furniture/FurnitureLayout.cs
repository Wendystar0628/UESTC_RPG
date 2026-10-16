using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

public enum FurnitureKind : byte { StreetLamp, Bench, Bin, BusStop, AtlasKiosk, AtlasPost, EntranceMat, OpenSign, EntranceLamp }

/// <summary>
/// One placed piece of street furniture: the catalogue part with its world matrix, its lamp-inventory markers, its world-space
/// colliders (the part's own, plus its pad's) and the pad it brings, if any (a rest stop's bay, a kiosk's apron, a plinth).
/// </summary>
public sealed record FurnitureItem(string Key, FurnitureKind Kind, PlacedPart Part)
{
    public IReadOnlyList<LightEmitter> Emitters { get; init; } = [];
    public IReadOnlyList<Collider> Colliders { get; init; } = [];
    public Pad? Pad { get; init; }
    public Vector2 Anchor => new(Part.At.M41, Part.At.M43);
}

/// <summary>All street furniture of one map, in a stable order (anchors, entrances, stops, then roads in rule order).</summary>
public sealed record FurnitureLayout(IReadOnlyList<FurnitureItem> Items)
{
    /// <summary>
    /// One line per atlas anchor the layout left out (its point lands on a carriageway, in water or in a building) or left where
    /// the data put it (no pedestrian paving within reach), in anchor order: for the anchor data's review (WP-48c).
    /// </summary>
    public IReadOnlyList<string> Log { get; init; } = [];

    public int Count(FurnitureKind kind) => Items.Count(i => i.Kind == kind);
}

/// <summary>
/// WP-13 placement (13b): pure and deterministic from the world model and the map's claims.
/// <list type="bullet">
/// <item>Atlas anchors (SYS-ATLAS §6): a kiosk or a post on every <c>anchor</c> feature of WP-48c's <c>atlas.overlay.json</c>,
/// at its footprint, facing its <c>yawDeg</c>; the kiosk on its pad (or the deck the overlay names), the post landed as a point.
/// A footprint more than <see cref="FurnitureStyle.AnchorPavingGap"/> off pedestrian paving moves the shortest way to its edge.</item>
/// <item>Open entrances (WLD-INTERIORS §4): mat, standing open sign and door lamp at every open entrance of the interior index,
/// on the entrance floor, facing out; skipped where another package claims the building's <see cref="Aspects.Props"/>.</item>
/// <item>Shuttle stops (WLD-LIFE §4.3): a stop sign at the kerb of the carriageway nearest each <c>bus</c> feature.</item>
/// <item>Roads (WLD-LIFE §4, MAP-QSH-ADDITIONS §8.1): lamps along the <see cref="FurnitureStyle.Roads"/> at the tier's spacing,
/// and rest stops midway between lamps: a bench lining the nearest walk on that side, facing the walk on a bay joined to it,
/// never facing a carriageway or in the strip between the kerb and the tree rows; at every other stop a four-stream bin on its
/// own bay 2–4 m along the same walk, towards a junction where there is one (Opus review 12:15).</item>
/// </list>
/// Everything lands on <see cref="IWorldModel.Terrain"/> (<see cref="Land"/>) and keeps clear of buildings, water, courts,
/// parking, other roads, bridges, trees and each other (<see cref="StreetSite"/>).
/// </summary>
public static class FurniturePlanner
{
    private static readonly StreetLampPart Lamp = new();
    private static readonly BenchPart BenchDef = new();
    private static readonly BinFourPart BinDef = new();
    private static readonly BusStopPart StopDef = new();
    private static readonly AtlasKioskPart KioskDef = new();
    private static readonly AtlasPostPart PostDef = new();
    private static readonly OpenSignPart SignDef = new();
    private static readonly WpId Self = new("WP-13");

    /// <summary>Pole footprint radius used for clearances and landing (the lamp's base flange).</summary>
    private const float PoleRadius = 0.25f;

    /// <summary>Half the stop board's thickness plus its clamp, for its clearance capsule.</summary>
    private const float StopHalfDepth = 0.15f;

    public static FurnitureLayout Build(IWorldModel world, Func<FeatureId, Aspects, WpId?>? claimedBy = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        claimedBy ??= (_, _) => null;
        var site = new StreetSite(world);
        foreach (var rule in S.Roads) site.AddRows(world.Network.Roads.Where(l => StreetSite.RoadName(l) == rule.Name), rule.Rows);
        var items = new List<FurnitureItem>();
        var log = new List<string>();
        Anchors(site, items, log);
        Entrances(site, items, claimedBy);
        Stops(site, items);
        for (var r = 0; r < S.Roads.Count; r++) Road(site, items, r, S.Roads[r]);
        return new(items.AsReadOnly()) { Log = log.AsReadOnly() };
    }

    // ---------------------------------------------------------------------------------------------------------
    // Atlas anchors: kiosks and posts on WP-48c's overlay features.
    // ---------------------------------------------------------------------------------------------------------

    private static void Anchors(StreetSite site, List<FurnitureItem> items, List<string> log)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.Where(f => f.Kind.Value == "anchor").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PolygonGeometry { Rings.Outer: { Count: >= 3 } ring }) continue;
            var form = Text(feature.Props, "form");
            if (form is not ("kiosk" or "post")) continue;
            var yaw = Number(feature.Props, "yawDeg") ?? 0f;
            var facing = Facing(yaw);
            var right = new Vector2(-facing.Y, facing.X);
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            var width = Extent(ring, right);
            var depth = Extent(ring, facing);
            var key = "anchor:" + feature.Id.Value;
            // Fallback while anchor data is under review: nothing stands on a carriageway, in water or in a building (A01 and A05
            // wait for the gate plazas of 18c3 and WP-30).
            var landing = site.LandingAt(centre);
            if (landing != Landing.Open)
            {
                log.Add($"anchor {feature.Id.Value} ({form}) not placed: its point lands {Where(landing)}");
                continue;
            }
            switch (form)
            {
                case "kiosk":
                    {
                        var p = S.Kiosk with { Width = width >= 1.4f ? width : S.Kiosk.Width, Depth = depth >= 0.4f ? depth : S.Kiosk.Depth, Height = feature.Height is float h && h >= 2.2f ? h : S.Kiosk.Height };
                        // Large entity (WLD-TERRAIN §4.4): on its pad, with a paved apron; a deck the height field does not carry (A06)
                        // comes from the overlay's groundY. An apron off the paving moves to its edge (a deck is paving already).
                        var ground = terrain.HeightAt(centre.X, centre.Y);
                        var deck = Number(feature.Props, "groundY") is { } y && y > ground + S.DeckAbove ? y : (float?)null;
                        if (deck is null) centre = ToPaving(site, centre, facing, p.Width * 0.5f + S.ApronMargin, p.Depth * 0.5f + S.ApronMargin, log, feature.Id, form);
                        var level = deck ?? terrain.PadY(feature.Id);
                        var plan = Frame(centre, facing, 0f);
                        var apron = Land.Pad(terrain, plan, p.Width * 0.5f + S.ApronMargin, p.Depth * 0.5f + S.ApronMargin, level)!;
                        var at = Frame(centre, facing, apron.Top);
                        var kiosk = Item(key, FurnitureKind.AtlasKiosk, PartCatalog.AtlasKiosk, p, at, KioskDef.Collision(p), [AtlasKioskPart.Emitter(p, at)]);
                        items.Add(kiosk with { Pad = apron, Colliders = [.. kiosk.Colliders, apron.Collider()] });
                        site.Occupy(centre, MathF.Max(p.Width, p.Depth) * 0.5f + S.ApronMargin);
                        break;
                    }
                case "post":
                    {
                        var p = S.Post with { Width = width >= 0.16f ? width : S.Post.Width, Depth = depth >= 0.16f ? depth : S.Post.Depth, Height = feature.Height is float h && h >= 0.6f ? h : S.Post.Height };
                        var radius = MathF.Max(p.Width, p.Depth) * 0.5f;
                        centre = ToPaving(site, centre, facing, radius, radius, log, feature.Id, form);
                        if (Land.Point(terrain, centre, radius) is not { } y) continue;
                        var at = Frame(centre, facing, y);
                        items.Add(Item(key, FurnitureKind.AtlasPost, PartCatalog.AtlasPost, p, at, PostDef.Collision(p), [AtlasPostPart.Emitter(p, at)]));
                        site.Occupy(centre, radius);
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// The anchor's centre, moved the shortest way until its footprint (±<paramref name="halfWidth"/> across, ±<paramref name="halfDepth"/>
    /// along <paramref name="facing"/>) meets the nearest pedestrian paving, when it does not touch it yet; the facing stays. The footprint ends <see cref="FurnitureStyle.PadJoin"/> over the paving edge, so the two join. Paving
    /// further than <see cref="FurnitureStyle.AnchorPavingReach"/> is a data question (WP-48c), not a move: the anchor stays, logged.
    /// </summary>
    private static Vector2 ToPaving(StreetSite site, Vector2 centre, Vector2 facing, float halfWidth, float halfDepth, List<string> log, FeatureId id, string form)
    {
        var right = new Vector2(-facing.Y, facing.X);
        var start = centre;
        for (var pass = 0; pass < 3; pass++)
        {
            // The footprint's rim point nearest to paving, and the paving edge it looks at.
            (float Gap, Vector2 From, Vector2 Edge)? near = null;
            for (var i = -1; i <= 1; i++)
                for (var k = -1; k <= 1; k++)
                {
                    var q = centre + right * (i * halfWidth) + facing * (k * halfDepth);
                    if (site.PavingEdge(q) is { } e && (near is null || e.Gap < near.Value.Gap)) near = (e.Gap, q, e.Edge);
                }
            if (near is { Gap: <= 0f }) break;
            if (near is { } n) centre += Vector2.Normalize(n.Edge - n.From) * (n.Gap + S.PadJoin);
            if (near is null || Vector2.Distance(start, centre) > S.AnchorPavingReach)
            {
                log.Add($"anchor {id.Value} ({form}) left in place: no pedestrian paving within {Num(S.AnchorPavingReach)} m");
                return start;
            }
        }
        return centre;
    }

    private static string Where(Landing landing) => landing switch
    {
        Landing.Carriageway => "on a carriageway",
        Landing.Water => "in water",
        Landing.Building => "in a building",
        _ => throw new ArgumentOutOfRangeException(nameof(landing), landing, "Not a fallback landing."),
    };

    // ---------------------------------------------------------------------------------------------------------
    // Open entrances: mat, standing open sign and door lamp on the entrance anchor.
    // ---------------------------------------------------------------------------------------------------------

    private static void Entrances(StreetSite site, List<FurnitureItem> items, Func<FeatureId, Aspects, WpId?> claimedBy)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in site.World.Features.Keys.OrderBy(f => f.Value, StringComparer.Ordinal))
        {
            if (site.World.Interiors.Get(feature) is not { } entry) continue;
            // A zone that builds the doorway and dresses it claims the building's props; its own kit stands there instead.
            if (claimedBy(entry.Feature, Aspects.Props) is { } owner && owner != Self) continue;
            foreach (var door in entry.Entrances.OrderBy(e => e.Id, StringComparer.Ordinal))
            {
                if (!door.Open || !seen.Add(door.Id)) continue;
                var facing = Facing(door.YawDeg);
                var plan = new Vector2(door.Center.X, door.Center.Z);
                // The entrance floor, never below the paving in front of the door (WLD-TERRAIN §4.3: mat and sign on the landing).
                var front = plan + facing * (S.MatDepth * 0.5f + S.MatGap);
                var floor = Frame(plan, facing, MathF.Max(door.Center.Y, site.World.Terrain.HeightAt(front.X, front.Y) + S.PavingLift));
                var key = "door:" + door.Id;
                var matWidth = Math.Clamp(door.ClearWidth + S.MatExtra, S.MatMinWidth, S.MatMaxWidth);
                var mat = new PanelParams(matWidth, S.MatThickness, S.MatDepth);
                items.Add(Item(key + ":mat", FurnitureKind.EntranceMat, PartCatalog.EntranceMat, mat,
                    Xf.Move(0, 0, -(S.MatDepth * 0.5f + S.MatGap)).Then(floor), [], []));
                var sign = S.OpenSign;
                var signAt = Xf.Move(matWidth * 0.5f + S.SignBeside + sign.Width * 0.5f, 0, -S.SignOut).Then(floor);
                items.Add(Item(key + ":sign", FurnitureKind.OpenSign, PartCatalog.OpenSign, sign, signAt, SignDef.Collision(sign), []));
                // The door lamp hangs above head height on the wall, whose collider stands in for it (visual only, like the part).
                var lamp = S.DoorLamp;
                var lampAt = Xf.Move(0, S.DoorLampY, -lamp.Depth * 0.5f).Then(floor);
                items.Add(Item(key + ":lamp", FurnitureKind.EntranceLamp, PartCatalog.EntranceLamp, lamp, lampAt, [],
                    [EntranceLampPart.Emitter(lamp, lampAt)]));
                site.Occupy(front, matWidth * 0.5f + 0.5f);
                site.Occupy(new(signAt.M41, signAt.M43), sign.Width * 0.5f);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Shuttle stops: a sign at the kerb of the carriageway nearest each bus feature (WLD-LIFE §3.2, §4.3).
    // ---------------------------------------------------------------------------------------------------------

    private static void Stops(StreetSite site, List<FurnitureItem> items)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (Text(feature.Props, "subcategory") != "bus") continue;
            var anchor = Anchor(feature);
            if (site.NearestCarriageway(anchor, 45f) is not { } near) continue;
            var normal = new Vector2(-near.Along.Y, near.Along.X);
            var side = Vector2.Dot(anchor - near.Point, normal) >= 0f ? 1f : -1f;
            var offset = StreetSite.HalfWidth(near.Line) + S.StopKerbGap;
            var p = S.Stop;
            foreach (var shift in Shifts(S.LampMaxShift))
            {
                var pos = near.Point + near.Along * shift + normal * (side * offset);
                // The board runs along the kerb: test it as a thin capsule, not a disc of its width.
                var reach = near.Along * (p.Width * 0.5f - StopHalfDepth);
                if (!site.Free(pos - reach, pos + reach, StopHalfDepth) || Land.Point(terrain, pos, PoleRadius) is not { } y) continue;
                var at = Frame(pos, -normal * side, y);
                items.Add(Item("stop:" + feature.Id.Value, FurnitureKind.BusStop, PartCatalog.BusStop, p, at, StopDef.Collision(p), []));
                site.Occupy(pos - reach, pos + reach, StopHalfDepth);
                break;
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Roads: lamps at the tier's spacing; rest stops midway between lamps.
    // ---------------------------------------------------------------------------------------------------------

    private static void Road(StreetSite site, List<FurnitureItem> items, int r, RoadRule rule)
    {
        var lines = site.World.Network.Roads.Where(l => StreetSite.RoadName(l) == rule.Name).ToList();
        if (lines.Count == 0) return;
        var half = lines.Max(StreetSite.HalfWidth);
        var main = rule.Tier == RoadTier.Main;
        var spacing = main ? S.MainLampSpacing : S.SecondaryLampSpacing;
        var lamp = main ? S.MainLamp : S.SecondaryLamp;
        var chains = StreetSite.Chains(lines);
        for (var c = 0; c < chains.Count; c++)
        {
            var run = new StreetSite.Run(chains[c]);
            var n = (int)MathF.Floor(run.Length / spacing);
            if (n < 1) continue;
            var s0 = (run.Length - (n - 1) * spacing) * 0.5f;
            // Grade one lights both sides, opposite each other; grade two one side, the side with more free slots.
            float[] lampSides = main ? [1f, -1f] : [BestSide(site, run, s0, spacing, n, half)];
            foreach (var side in lampSides)
                for (var k = 0; k < n; k++)
                    PlaceLamp(site, items, run, s0 + k * spacing, side, half, lamp, $"lamp:{r}:{c}:{Side(side)}:{Num(k)}");
            // Rest stops: every RestEvery lamp gaps on a side; the avenue's two sides alternate, a secondary road's sit opposite its lamps.
            float[] restSides = main ? [1f, -1f] : [-lampSides[0]];
            foreach (var side in restSides)
            {
                var phase = main && side < 0f ? S.RestEvery / 2 : 0;
                int stops = 0, bins = 0;
                for (var k = 0; k + 1 < n; k++)
                {
                    if ((k + phase) % S.RestEvery != S.RestEvery / 2) continue;
                    var key = $"rest:{r}:{c}:{Side(side)}:{Num(k)}";
                    // A bin at every other stop; one that found no room passes to the next stop.
                    var (bench, bin) = PlaceRest(site, items, run, s0 + (k + 0.5f) * spacing, side, half, withBin: bins * 2 <= stops, key);
                    if (bench) stops++;
                    if (bin) bins++;
                }
            }
        }
    }

    private static float BestSide(StreetSite site, StreetSite.Run run, float s0, float spacing, int n, float half)
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

    private static void PlaceLamp(StreetSite site, List<FurnitureItem> items, StreetSite.Run run, float s, float side, float half, LampParams p, string key)
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

    /// <summary>
    /// The rest stop at arc length <paramref name="s"/> of the road, on <paramref name="side"/>: a bench lining the nearest walk on that
    /// side (within <see cref="FurnitureStyle.RestReach"/> of the centre line, sliding along it by up to <see cref="FurnitureStyle.RestSlide"/>),
    /// its front <see cref="FurnitureStyle.WalkGap"/> off the walk's edge and facing it, on a bay joined to the walk. The walk's road
    /// side is tried first (back to the trees and the road, looking away from it); the far side only takes a bench where no
    /// carriageway lies <see cref="FurnitureStyle.FaceClear"/> ahead. With <paramref name="withBin"/>, a bin on its own bay along the
    /// same edge, <see cref="FurnitureStyle.BinGaps"/> clear of the bench, on the side nearer another walk (a junction).
    /// </summary>
    private static (bool Bench, bool Bin) PlaceRest(StreetSite site, List<FurnitureItem> items, StreetSite.Run run, float s, float side, float half, bool withBin, string key)
    {
        if (run.Normal(s) is not { } normal) return (false, false);
        var road = run.At(s);
        var outward = normal * side;
        foreach (var walk in site.Walks(road, S.RestReach))
        {
            // The walk must lie on this side, beyond the carriageway, and stay wide enough to pass a sitter's feet.
            var whalf = StreetSite.HalfWidth(walk.Line);
            if (Vector2.Dot(walk.Point - road, outward) <= half || 2f * whalf - S.FeetReach < S.PassClear) continue;
            var path = new StreetSite.Run([.. walk.Line.Line.Points]);
            var ends = S.Bench.Width * 0.5f + S.ItemGap;
            foreach (var shift in Shifts(S.RestSlide))
            {
                var at = walk.Along + shift;
                if (at < ends || at > path.Length - ends || path.Normal(at) is not { } n) continue;
                var roadSide = Vector2.Dot(n, outward) < 0f ? 1f : -1f;
                foreach (var bside in new[] { roadSide, -roadSide })
                {
                    if (WalkSlot(site, path, at, bside, walk.Line, S.Bench, facesWalk: true) is not { } bench) continue;
                    items.Add(Placed(key + ":bench", FurnitureKind.Bench, PartCatalog.Bench, S.Bench, bench, BenchDef.Collision(S.Bench)));
                    site.Occupy(bench.Ends.A, bench.Ends.B, bench.Ends.R);
                    return (true, withBin && PlaceBin(site, items, path, at, bside, walk.Line, key + ":bin"));
                }
            }
        }
        return (false, false);
    }

    /// <summary>A bin along the same walk edge as the bench at <paramref name="benchAt"/>, 2–4 m clear of it, nearer side to a junction first.</summary>
    private static bool PlaceBin(StreetSite site, List<FurnitureItem> items, StreetSite.Run path, float benchAt, float bside, NetworkLine walk, string key)
    {
        var centre = S.Bench.Width * 0.5f + S.Bin.Width * 0.5f;
        float Junction(float sign) => site.OtherWalk(path.At(benchAt + sign * (centre + S.BinGaps[0])), walk);
        var first = Junction(-1f) < Junction(1f) ? -1f : 1f;
        foreach (var sign in new[] { first, -first })
            foreach (var gap in S.BinGaps)
            {
                var at = benchAt + sign * (centre + gap);
                if (at < S.Bin.Width * 0.5f || at > path.Length - S.Bin.Width * 0.5f) continue;
                if (WalkSlot(site, path, at, bside, walk, S.Bin, facesWalk: false) is not { } bin) continue;
                items.Add(Placed(key, FurnitureKind.Bin, PartCatalog.BinFour, S.Bin, bin, BinDef.Collision(S.Bin)));
                site.Occupy(bin.Ends.A, bin.Ends.B, bin.Ends.R);
                return true;
            }
        return false;
    }

    /// <summary>A small entity standing at a walk's edge, facing the walk, with the bay that joins it to the walk.</summary>
    private readonly record struct WalkFit(Vector2 Centre, Vector2 Facing, (Vector2 A, Vector2 B, float R) Ends, Pad Bay);

    /// <summary>
    /// A small entity at arc length <paramref name="at"/> of a walk, on side <paramref name="bside"/> of it (along the walk's left
    /// normal), its front <see cref="FurnitureStyle.WalkGap"/> off the walk's edge and facing the walk, if it fits: free of
    /// outlines, roads (only off this walk's surface), trees, the planted rows and placed things; <see cref="FurnitureStyle.RoadSetback"/>
    /// off carriageways; a bench (<paramref name="facesWalk"/>) with no carriageway <see cref="FurnitureStyle.FaceClear"/> ahead.
    /// Its bay reaches <see cref="FurnitureStyle.PadJoin"/> over the walk's edge and <see cref="FurnitureStyle.BayBehind"/> behind it.
    /// </summary>
    private static WalkFit? WalkSlot(StreetSite site, StreetSite.Run path, float at, float bside, NetworkLine walk, FurnitureParams p, bool facesWalk)
    {
        if (path.Normal(at) is not { } n) return null;
        var whalf = StreetSite.HalfWidth(walk);
        var away = n * bside;
        var facing = -away;
        var centre = path.At(at) + away * (whalf + S.WalkGap + p.Depth * 0.5f);
        var tangent = new Vector2(-away.Y, away.X);
        var reach = MathF.Max(0f, p.Width * 0.5f - p.Depth * 0.5f);
        var ends = (centre - tangent * reach, centre + tangent * reach, p.Depth * 0.5f + 0.05f);
        if (!site.Free(ends.Item1, ends.Item2, ends.Item3, walk)) return null;
        foreach (var q in new[] { ends.Item1, centre, ends.Item2 })
            if (!site.OffRows(q, ends.Item3) || !site.OffCarriageways(q, ends.Item3, S.RoadSetback)) return null;
        if (facesWalk && site.CarriagewayAhead(centre + facing * (p.Depth * 0.5f), facing, S.FaceClear)) return null;
        // The bay, in the entity's frame (−Z towards the walk): from just over the walk's edge to behind the entity.
        var front = p.Depth * 0.5f + S.WalkGap + S.PadJoin;
        var back = p.Depth * 0.5f + S.BayBehind;
        var middle = centre + facing * ((front - back) * 0.5f);
        if (Land.Pad(site.World.Terrain, Frame(middle, facing, 0f), p.Width * 0.5f + S.BayEnds, (front + back) * 0.5f) is not { } bay) return null;
        return new(centre, facing, ends, bay);
    }

    private static FurnitureItem Placed(string key, FurnitureKind kind, string part, FurnitureParams p, WalkFit fit, IReadOnlyList<Collider> local)
    {
        var item = Item(key, kind, part, p, Frame(fit.Centre, fit.Facing, fit.Bay.Top), local, []);
        return item with { Pad = fit.Bay, Colliders = [.. item.Colliders, fit.Bay.Collider()] };
    }

    // ---------------------------------------------------------------------------------------------------------
    // Common.
    // ---------------------------------------------------------------------------------------------------------

    private static FurnitureItem Item(string key, FurnitureKind kind, string part, BoxPartParams p, Matrix4x4 at, IReadOnlyList<Collider> local, IReadOnlyList<LightEmitter> emitters) =>
        new(key, kind, new PlacedPart(new(part), p, at)) { Emitters = emitters, Colliders = local.Select(c => Placed(c, at)).ToArray() };

    /// <summary>
    /// Rigid frame whose local −Z (every part's front) points along <paramref name="facing"/> and whose origin is (p.x, y, p.z).
    /// Rows are the images of local X, Y, Z (System.Numerics row vectors).
    /// </summary>
    public static Matrix4x4 Frame(Vector2 p, Vector2 facing, float y)
    {
        var f = Vector2.Normalize(facing);
        return new(-f.Y, 0, f.X, 0, 0, 1, 0, 0, -f.X, 0, -f.Y, 0, p.X, y, p.Y, 1);
    }

    /// <summary>Plan direction of a yaw in degrees: 0 = north (−Z), 90 = east (+X), the convention of anchors and entrances.</summary>
    public static Vector2 Facing(float yawDeg)
    {
        var a = yawDeg * (float.Pi / 180f);
        return new(DetMath.Sin(a), -DetMath.Cos(a));
    }

    /// <summary>Moves a part-local collider by a rigid placement matrix.</summary>
    public static Collider Placed(Collider collider, Matrix4x4 at) => collider switch
    {
        BoxCollider box => box with { At = box.At.Then(at) },
        PostCollider post => post with
        {
            C = new(post.C.X * at.M11 + post.C.Y * at.M31 + at.M41, post.C.X * at.M13 + post.C.Y * at.M33 + at.M43),
            Y0 = post.Y0 + at.M42,
            Y1 = post.Y1 + at.M42,
        },
        _ => throw new ArgumentException("Unsupported collider kind for a placed street part.", nameof(collider)),
    };

    /// <summary>0, +step, −step, +2·step, … up to ±<paramref name="max"/>.</summary>
    private static IEnumerable<float> Shifts(float max)
    {
        yield return 0f;
        for (var d = S.ShiftStep; d <= max + 1e-3f; d += S.ShiftStep)
        {
            yield return d;
            yield return -d;
        }
    }

    private static float Extent(IReadOnlyList<Vector2> ring, Vector2 axis)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var p in ring)
        {
            var d = Vector2.Dot(p, axis);
            min = MathF.Min(min, d);
            max = MathF.Max(max, d);
        }
        return max - min;
    }

    private static Vector2 Anchor(WorldFeature feature)
    {
        if (feature.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("anchor", out var a) &&
            a.ValueKind == JsonValueKind.Array && a.GetArrayLength() >= 3)
            return new(a[0].GetSingle(), a[2].GetSingle());
        return new((feature.Bounds.Min.X + feature.Bounds.Max.X) * 0.5f, (feature.Bounds.Min.Z + feature.Bounds.Max.Z) * 0.5f);
    }

    private static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static float? Number(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : null;

    private static string Side(float side) => side < 0f ? "r" : "l";

    private static string Num(int k) => k.ToString(CultureInfo.InvariantCulture);

    private static string Num(float v) => v.ToString(CultureInfo.InvariantCulture);
}
