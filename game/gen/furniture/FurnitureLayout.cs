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
    public int Count(FurnitureKind kind) => Items.Count(i => i.Kind == kind);
}

/// <summary>
/// WP-13 placement (13b): pure and deterministic from the world model and the map's claims.
/// <list type="bullet">
/// <item>Atlas anchors (SYS-ATLAS §6): a kiosk or a post on every <c>anchor</c> feature of WP-48c's <c>atlas.overlay.json</c>,
/// at its footprint, facing its <c>yawDeg</c>; the kiosk on its pad (or the deck the overlay names), the post landed as a point.</item>
/// <item>Open entrances (WLD-INTERIORS §4): mat, standing open sign and door lamp at every open entrance of the interior index,
/// on the entrance floor, facing out; skipped where another package claims the building's <see cref="Aspects.Props"/>.</item>
/// <item>Shuttle stops (WLD-LIFE §4.3): a stop sign at the kerb of the carriageway nearest each <c>bus</c> feature.</item>
/// <item>Roads (WLD-LIFE §4, MAP-QSH-ADDITIONS §8.1): lamps along the <see cref="FurnitureStyle.Roads"/> at the tier's spacing,
/// and rest stops (bench, with a four-stream bin at every other one) midway between lamps.</item>
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
        var items = new List<FurnitureItem>();
        Anchors(site, items);
        Entrances(site, items, claimedBy);
        Stops(site, items);
        for (var r = 0; r < S.Roads.Count; r++) Road(site, items, r, S.Roads[r]);
        return new(items.AsReadOnly());
    }

    // ---------------------------------------------------------------------------------------------------------
    // Atlas anchors: kiosks and posts on WP-48c's overlay features.
    // ---------------------------------------------------------------------------------------------------------

    private static void Anchors(StreetSite site, List<FurnitureItem> items)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.Where(f => f.Kind.Value == "anchor").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PolygonGeometry { Rings.Outer: { Count: >= 3 } ring }) continue;
            var form = Text(feature.Props, "form");
            var yaw = Number(feature.Props, "yawDeg") ?? 0f;
            var facing = Facing(yaw);
            var right = new Vector2(-facing.Y, facing.X);
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            var width = Extent(ring, right);
            var depth = Extent(ring, facing);
            var key = "anchor:" + feature.Id.Value;
            switch (form)
            {
                case "kiosk":
                    {
                        var p = S.Kiosk with { Width = width >= 1.4f ? width : S.Kiosk.Width, Depth = depth >= 0.4f ? depth : S.Kiosk.Depth, Height = feature.Height is float h && h >= 2.2f ? h : S.Kiosk.Height };
                        // Large entity (WLD-TERRAIN §4.4): on its pad, with a paved apron; a deck the height field does not carry (A06)
                        // comes from the overlay's groundY.
                        var ground = terrain.HeightAt(centre.X, centre.Y);
                        var level = Number(feature.Props, "groundY") is { } deck && deck > ground + S.DeckAbove ? deck : terrain.PadY(feature.Id);
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
                        if (Land.Point(terrain, centre, radius) is not { } y) continue;
                        var at = Frame(centre, facing, y);
                        items.Add(Item(key, FurnitureKind.AtlasPost, PartCatalog.AtlasPost, p, at, PostDef.Collision(p), [AtlasPostPart.Emitter(p, at)]));
                        site.Occupy(centre, radius);
                        break;
                    }
            }
        }
    }

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
                var stop = 0;
                for (var k = 0; k + 1 < n; k++)
                {
                    if ((k + phase) % S.RestEvery != S.RestEvery / 2) continue;
                    var key = $"rest:{r}:{c}:{Side(side)}:{Num(k)}";
                    if (PlaceRest(site, items, run, s0 + (k + 0.5f) * spacing, side, half, withBin: stop % 2 == 0, key)) stop++;
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
    /// A bench facing the road, its front <see cref="FurnitureStyle.BenchKerbGap"/> off the kerb, and at every other stop a bin beside
    /// it on the same front line; both stand on one paved bay from the back of the kerb to behind them (a plinth on a slope).
    /// </summary>
    private static bool PlaceRest(StreetSite site, List<FurnitureItem> items, StreetSite.Run run, float s, float side, float half, bool withBin, string key)
    {
        foreach (var shift in Shifts(S.ShiftStep * 2f))
        {
            var at = s + shift;
            if (at < 0f || at > run.Length || run.Normal(at) is not { } normal) continue;
            var along = new Vector2(normal.Y, -normal.X);
            var outward = normal * side;
            var road = run.At(at);
            var bench = Slot(site, road, outward, along, half, S.Bench);
            if (bench is not { } b) continue;
            var bin = withBin ? Slot(site, road + along * S.BinAlong, outward, along, half, S.Bin, b.Ends) : null;
            if (withBin && bin is null) continue;
            // The bay: along from the bench's far end to the group's other end, across from the kerb's back to behind the furniture.
            var from = -S.Bench.Width * 0.5f - S.BayEnds;
            var to = (bin is null ? S.Bench.Width * 0.5f : S.BinAlong + S.Bin.Width * 0.5f) + S.BayEnds;
            var near = half + S.KerbWidth;
            var far = half + S.BenchKerbGap + MathF.Max(S.Bench.Depth, withBin ? S.Bin.Depth : 0f) + S.BayBehind;
            var centre = road + along * ((from + to) * 0.5f) + outward * ((near + far) * 0.5f);
            if (Land.Pad(site.World.Terrain, Frame(centre, -outward, 0f), (to - from) * 0.5f, (far - near) * 0.5f) is not { } bay) continue;
            var facing = -outward;
            var benchItem = Item(key + ":bench", FurnitureKind.Bench, PartCatalog.Bench, S.Bench, Frame(b.Centre, facing, bay.Top), BenchDef.Collision(S.Bench), []);
            items.Add(benchItem with { Pad = bay, Colliders = [.. benchItem.Colliders, bay.Collider()] });
            site.Occupy(b.Ends.A, b.Ends.B, b.Ends.R);
            if (bin is { } n)
            {
                items.Add(Item(key + ":bin", FurnitureKind.Bin, PartCatalog.BinFour, S.Bin, Frame(n.Centre, facing, bay.Top), BinDef.Collision(S.Bin), []));
                site.Occupy(n.Ends.A, n.Ends.B, n.Ends.R);
            }
            return true;
        }
        return false;
    }

    private readonly record struct SlotFit(Vector2 Centre, (Vector2 A, Vector2 B, float R) Ends);

    /// <summary>A small entity whose front edge lies <see cref="FurnitureStyle.BenchKerbGap"/> off the kerb at <paramref name="road"/>, if it fits.</summary>
    private static SlotFit? Slot(StreetSite site, Vector2 road, Vector2 outward, Vector2 along, float half, FurnitureParams p, (Vector2 A, Vector2 B, float R)? beside = null)
    {
        var centre = road + outward * (half + S.BenchKerbGap + p.Depth * 0.5f);
        var reach = MathF.Max(0f, p.Width * 0.5f - p.Depth * 0.5f);
        var ends = (centre - along * reach, centre + along * reach, p.Depth * 0.5f + 0.05f);
        if (!site.Free(ends.Item1, ends.Item2, ends.Item3)) return null;
        // A neighbour placed in the same step is not in the site yet: keep the item gap to it here.
        if (beside is { } o && Vector2.Distance(ends.Item1, o.B) < ends.Item3 + o.R + S.ItemGap) return null;
        return new(centre, ends);
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
}
