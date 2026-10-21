using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class FurnitureStreetLayoutTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TheLitRoadsAreFurnishedDeterministically()
    {
        var a = RealLayout.Value;
        var b = Build(World.Value);
        var lamps = a.Items.Where(i => i.Kind == FurnitureKind.StreetLamp).ToList();
        AssertThat(a.Items.Select(i => (i.Key, i.Part.At, i.Part.Params)).SequenceEqual(b.Items.Select(i => (i.Key, i.Part.At, i.Part.Params)))).OverrideFailureMessage("two builds differ").IsTrue();
        AssertThat(a.Items.Select(i => i.Key).Distinct().Count() == a.Items.Count).OverrideFailureMessage("duplicate keys").IsTrue();
        AssertThat(lamps.Any(i => ((LampParams)i.Part.Params).Variation == 0) && lamps.Any(i => ((LampParams)i.Part.Params).Variation == 1)).OverrideFailureMessage("both lamp forms are placed").IsTrue();
        // Rest stops only where a walk takes a bench (Opus review 12:15).
        AssertThat(a.Count(FurnitureKind.Bench) > 0).OverrideFailureMessage($"{a.Count(FurnitureKind.Bench)} benches").IsTrue();
        // A bin at about every other rest stop along a road (a stop with no room for its bin hands it to the next); node benches (on the plazas) come without.
        var rest = a.Items.Count(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith("rest:", StringComparison.Ordinal));
        AssertThat(a.Count(FurnitureKind.Bin) * FurnitureStyle.RestBinEvery >= rest * 0.6f && a.Count(FurnitureKind.Bin) <= rest).OverrideFailureMessage("a bin at about every other rest stop").IsTrue();
        // SYS-ATLAS: a kiosk or a post on every anchor on open ground or an authored elevated deck. The counts follow the anchor data, so
        // moving an anchor (SYS-ATLAS-TRAVEL) never needs a new number here.
        var landings = AnchorLandings();
        var kiosks = landings.Count(l => l.Form == "kiosk" && l.Landing == Landing.Open);
        var posts = landings.Count(l => l.Form == "post" && l.Landing == Landing.Open);
        AssertThat(kiosks + posts > 0).OverrideFailureMessage("no anchor lands on open ground").IsTrue();
        AssertThat(a.Count(FurnitureKind.AtlasKiosk) == kiosks && a.Count(FurnitureKind.AtlasPost) == posts).OverrideFailureMessage($"{a.Count(FurnitureKind.AtlasKiosk)} kiosks and {a.Count(FurnitureKind.AtlasPost)} posts for {kiosks} and {posts} anchors on open ground").IsTrue();
        AssertThat(a.Count(FurnitureKind.BusStop) > 0).OverrideFailureMessage($"{a.Count(FurnitureKind.BusStop)} shuttle stops").IsTrue();
        var open = World.Value.Interiors.EntrancesNear(Vector3.Zero, 1e5f).Where(e => e.Open).Select(e => e.Id).Distinct().Count();
        AssertThat(new[] { FurnitureKind.EntranceMat, FurnitureKind.OpenSign, FurnitureKind.EntranceLamp }.All(k => a.Count(k) == open)).OverrideFailureMessage($"one kit at each of {open} open entrances").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void BenchesLineAWalkAndBinsStandApart()
    {
        // Opus review 12:15: a bench lines a walk and faces it, its bay joined to the walk, a passage of 1.2 m left in front; it
        // never faces a carriageway or sits in the strip between the kerb and the tree rows. Its bin stands 2–4 m away on its own bay.
        var world = World.Value;
        var layout = RealLayout.Value;
        var walks = world.Network.Roads.Where(r => Highway(r) is "footway" or "path" or "pedestrian").ToList();
        var lanes = world.Network.Roads.Where(Carriageway).ToList();
        // Rest stops along walks; a rest node's benches (key "node:") stand at a plaza or walk end and are judged by their own test.
        var benches = layout.Items.Where(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith("rest:", StringComparison.Ordinal)).ToList();
        foreach (var item in benches.Concat(layout.Items.Where(i => i.Kind == FurnitureKind.Bin)))
        {
            var p = (FurnitureParams)item.Part.Params;
            var facing = Front(item.Part.At);
            var front = item.Anchor + facing * (p.Depth * 0.5f);
            var walk = walks.MinBy(w => (float)PlanLines.Project(w.Line.Points, front).Distance);
            var edge = (float)PlanLines.Project(walk!.Line.Points, front).Distance - Half(walk!);
            AssertThat(edge >= -TestTolerance.Position && edge <= FurnitureStyle.WalkGap + TestTolerance.Layout * 2).OverrideFailureMessage($"{item.Key}: front {edge:F2} m off its walk's edge").IsTrue();
            var toWalk = Vector2.Normalize(PlanLines.Project(walk!.Line.Points, front).Point - item.Anchor);
            AssertThat(Vector2.Dot(toWalk, facing) > 0.97f).OverrideFailureMessage($"{item.Key} does not face its walk").IsTrue();
            AssertThat(2f * Half(walk!) - FurnitureStyle.FeetReach >= FurnitureStyle.PassClear).OverrideFailureMessage($"{item.Key}: walk too narrow to pass").IsTrue();
            var pad = item.Pad ?? throw new InvalidOperationException($"{item.Key} has no bay");
            var padFront = new Vector2(pad.Frame.M41, pad.Frame.M43) + facing * pad.HalfDepth;
            AssertThat((float)PlanLines.Project(walk!.Line.Points, padFront).Distance < Half(walk!)).OverrideFailureMessage($"{item.Key}: its bay does not reach the walk").IsTrue();
            AssertThat(MathF.Abs(pad.Top - item.Part.At.M42) < TestTolerance.Position).OverrideFailureMessage($"{item.Key} is off its bay").IsTrue();
            var radius = p.Depth * 0.5f;
            foreach (var lane in lanes)
                AssertThat((float)PlanLines.Project(lane.Line.Points, item.Anchor).Distance - Half(lane) - radius >= FurnitureStyle.RoadSetback - 0.6f).OverrideFailureMessage($"{item.Key} stands in the kerbside strip of {lane.Id.Value}").IsTrue();
            if (item.Kind != FurnitureKind.Bench) continue;
            for (var d = 0f; d <= FurnitureStyle.FaceClear; d += 0.5f)
                AssertThat(lanes.All(l => (float)PlanLines.Project(l.Line.Points, front + facing * d).Distance >= Half(l))).OverrideFailureMessage($"{item.Key} faces a carriageway {d:F1} m ahead").IsTrue();
        }
        foreach (var bin in layout.Items.Where(i => i.Kind == FurnitureKind.Bin))
        {
            var bench = benches.Single(i => i.Key == bin.Key.Replace(":bin", ":bench", StringComparison.Ordinal));
            var gap = Vector2.Distance(bin.Anchor, bench.Anchor) - (FurnitureStyle.Bench.Width + FurnitureStyle.Bin.Width) * 0.5f;
            AssertThat(gap >= FurnitureStyle.BinGapMin - TestTolerance.Layout * 3 && gap <= FurnitureStyle.BinGapMax + TestTolerance.Layout * 3).OverrideFailureMessage($"{bin.Key} stands {gap:F2} m from its bench").IsTrue();
            AssertThat(!ReferenceEquals(bin.Pad, bench.Pad)).OverrideFailureMessage($"{bin.Key} shares the bench's bay").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void AnchorsOnACarriagewayInWaterOrInABuildingAreRejected()
    {
        var world = World.Value;
        var layout = RealLayout.Value;
        var landings = AnchorLandings();
        foreach (var (feature, _, _, landing) in landings)
        {
            AssertThat(landing == Landing.Open).OverrideFailureMessage($"{feature.Id.Value} has an invalid authored landing.").IsTrue();
            AssertThat(layout.Items.Any(item => item.Key == "anchor:" + feature.Id.Value)).IsTrue();
        }
        var building = world.Features.Values.Where(f => f.Kind.Value == "building" && f.Geometry is PolygonGeometry).OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Select(f => (Feature: f, Centre: PlanPolygons.Centroid(((PolygonGeometry)f.Geometry).Rings.Outer)))
            .First(b => PlanRings.Contains(((PolygonGeometry)b.Feature.Geometry).Rings, b.Centre) && PlanRings.DistanceToRing(b.Centre, ((PolygonGeometry)b.Feature.Geometry).Rings.Outer) > 3f);
        var anchor = landings.First(l => l.Landing == Landing.Open);
        var ring = ((PolygonGeometry)anchor.Feature.Geometry).Rings.Outer;
        var shift = building.Centre - anchor.Centre;
        var moved = anchor.Feature with { Geometry = new PolygonGeometry(new PolygonRings([.. ring.Select(p => p + shift)], [])) };
        AssertThrown(() => Build(MadeWorld(world, [], [moved]))).IsInstanceOf<WorldDataException>();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void AnchorsStandOnOrBesidePaving()
    {
        // Opus review 12:15: a kiosk's apron or a post's foot touches pedestrian paving (≤ 0.5 m off it); the facing never changes.
        var world = World.Value;
        var layout = RealLayout.Value;
        var site = Site.Value;
        var lines = world.Network.Roads.Where(r => Highway(r) is "footway" or "path" or "pedestrian" or "steps").ToList();
        var areas = new List<PolygonRings>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "road"))
        {
            var sources = feature.Parts.Count == 0 ? [(feature.Props, feature.Geometry)] : feature.Parts.Select(p => (p.Props ?? feature.Props, p.Geometry)).ToList();
            foreach (var (props, geometry) in sources)
                if (RoadType(props) is "footway" or "path" or "pedestrian" or "steps")
                    areas.AddRange(geometry switch { PolygonGeometry p => [p.Rings], MultiPolygonGeometry m => m.Polygons, _ => [] });
        }
        float Gap(Vector2 q) => MathF.Max(0f, MathF.Min(
            lines.Select(l => (float)PlanLines.Project(l.Line.Points, q).Distance - Half(l)).DefaultIfEmpty(float.MaxValue).Min(),
            areas.Select(a => PlanRings.Contains(a, q) ? 0f : a.Holes.Append(a.Outer).Min(r => PlanRings.DistanceToRing(q, r))).DefaultIfEmpty(float.MaxValue).Min()));
        foreach (var item in layout.Items.Where(i => i.Kind is FurnitureKind.AtlasKiosk or FurnitureKind.AtlasPost))
        {
            var feature = world.Features[new(item.Key["anchor:".Length..])];
            AssertThat(Vector2.Dot(Front(item.Part.At), ExpectedFacing(layout, feature)) > 0.999f).OverrideFailureMessage($"{item.Key} turned").IsTrue();
            if (feature.Props!.Value.TryGetProperty("groundY", out var deck) && deck.GetSingle() > 0.5f) continue;
            var ring = ((PolygonGeometry)feature.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            if (Vector2.Distance(centre, item.Anchor) < TestTolerance.Position && Gap(centre) > FurnitureStyle.AnchorPavingReach)
                continue;
            var hw = item.Pad is { } apron ? apron.HalfWidth : ((SignParams)item.Part.Params).Width * 0.5f;
            var hd = item.Pad is { } pad ? pad.HalfDepth : ((SignParams)item.Part.Params).Depth * 0.5f;
            var gap = float.MaxValue;
            for (var i = -1; i <= 1; i++)
                for (var k = -1; k <= 1; k++)
                    if (i != 0 || k != 0)
                    {
                        var q = Xf.Point(item.Part.At, new(i * hw, 0, k * hd));
                        gap = MathF.Min(gap, Gap(new(q.X, q.Z)));
                    }
            AssertThat(gap <= FurnitureStyle.AnchorPavingGap + TestTolerance.Layout * 2).OverrideFailureMessage($"{item.Key}: footprint {gap:F2} m off pedestrian paving").IsTrue();
            // Opus review 17:28 (A03): a kiosk's apron stands whole on the paving, not half over its edge.
            if (item.Kind != FurnitureKind.AtlasKiosk) continue;
            var outside = 0; var samples = 0;
            for (var u = -hw; u <= hw + 1e-3f; u += 0.25f)
                for (var v = -hd; v <= hd + 1e-3f; v += 0.25f)
                {
                    if (MathF.Abs(u) < hw - 1e-3f && MathF.Abs(v) < hd - 1e-3f) continue;
                    var q = Xf.Point(item.Part.At, new(u, 0, v));
                    samples++;
                    if (site.Paving.PavingEdgeFacing(new(q.X, q.Z)) is not { Gap: <= 0f }) outside++;
                }
            AssertThat(outside == 0).OverrideFailureMessage($"{item.Key}: {outside}/{samples} outline points of its apron off the paving").IsTrue();
        }
    }

    /// <summary>
    /// Every kiosk and post anchor of the real world in ID order, with its footprint centre and what that point lands on: the data the
    /// planner places by, so the tests' expectations follow the anchor data.
    /// </summary>
    private static List<(WorldFeature Feature, string Form, Vector2 Centre, Landing Landing)> AnchorLandings()
    {
        var world = World.Value;
        var site = Site.Value;
        var found = new List<(WorldFeature, string, Vector2, Landing)>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "anchor").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Geometry is not PolygonGeometry { Rings.Outer: { Count: >= 3 } ring }) continue;
            if (feature.Props is not { ValueKind: JsonValueKind.Object } props || !props.TryGetProperty("form", out var value) || value.ValueKind != JsonValueKind.String) continue;
            var form = value.GetString();
            if (form is not ("kiosk" or "post")) continue;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            var deck = form == "kiosk" && feature.TryNumber("groundY") is { } groundY
                && groundY > world.Terrain.HeightAt(centre.X, centre.Y) + FurnitureStyle.DeckAbove;
            found.Add((feature, form, centre, deck ? Landing.Open : site.LandingAt(centre)));
        }
        return found;
    }

    private static bool Carriageway(NetworkLine line) => Highway(line) is "service" or "residential" or "tertiary" or "secondary" or "primary" or "unclassified" or "living_street";
}
