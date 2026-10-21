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
public sealed class AnchorFacingTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void AKioskOffThePavingTurnsOntoItWholeWithItsBackToTheEdge()
    {
        // Opus review 14:20: an apron that does not touch paving stands whole on it, 0.5 m inside the edge, its back parallel to the
        // edge and its face towards the paving, and the layout diagnosticss the yaw (and centre) the anchor data should take over. Judged on
        // a synthetic anchor: the first kiosk of the data moved to a grass notch inside the plaza near (120, -65) (A04's old spot).
        var world = World.Value;
        var site = Site.Value;
        var spot = new Vector2(120f, -65f);
        AssertThat(site.Paving.PavingEdgeFacing(spot) is { Gap: > 1f }).OverrideFailureMessage("the notch is not off the paving any more: pick another spot").IsTrue();
        var original = world.Features.Values.Where(f => f.Kind.Value == "anchor" && WorldProps.TryText(f.Props, "form") == "kiosk" && f.Props!.Value.TryGetProperty("groundY", out var y) && y.GetSingle() < 0.5f)
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal).First();
        var ring = new List<Vector2> { spot + new Vector2(-0.4f, -1f), spot + new Vector2(-0.4f, 1f), spot + new Vector2(0.4f, 1f), spot + new Vector2(0.4f, -1f) };
        var moved = original with { Geometry = new PolygonGeometry(new PolygonRings(ring, [])), Props = Edit(original.Props!.Value, "yawDeg", 90f) };
        var layout = Build(MadeWorld(world, [], [moved]));
        var turned = Turned(layout, moved.Id.Value) ?? throw new InvalidOperationException("the kiosk off the paving is not logged as turned: " + string.Join(" | ", layout.Diagnostics.Select(d => d.Message)));
        var item = layout.Items.Single(i => i.Key == "anchor:" + moved.Id.Value);
        var facing = Front(item.Part.At);
        AssertThat(Vector2.Dot(facing, Angles.Heading(turned.Yaw)) > 0.999f && Vector2.Distance(item.Anchor, turned.Centre) < TestTolerance.Position).OverrideFailureMessage("the diagnostic does not give the placement").IsTrue();
        AssertThat(Vector2.Dot(facing, Angles.Heading(90f)) < 0.99f).OverrideFailureMessage("the yaw of the data stands, though the apron was off the paving").IsTrue();
        var apron = item.Pad ?? throw new InvalidOperationException("no apron");
        var right = new Vector2(-facing.Y, facing.X);
        // The whole outline of the apron is on paving.
        for (var u = -apron.HalfWidth; u <= apron.HalfWidth + 1e-3f; u += 0.25f)
            foreach (var v in new[] { -apron.HalfDepth, apron.HalfDepth })
                AssertThat(site.Paving.PavingEdgeFacing(item.Anchor + right * u + facing * v) is { Gap: <= 0f }).OverrideFailureMessage($"the apron's outline leaves the paving at ({u:F2}, {v:F2})").IsTrue();
        for (var v = -apron.HalfDepth; v <= apron.HalfDepth + 1e-3f; v += 0.25f)
            foreach (var u in new[] { -apron.HalfWidth, apron.HalfWidth })
                AssertThat(site.Paving.PavingEdgeFacing(item.Anchor + right * u + facing * v) is { Gap: <= 0f }).OverrideFailureMessage($"the apron's side leaves the paving at ({u:F2}, {v:F2})").IsTrue();
        // The back edge runs along the paving edge, 0.5 m inside it: stepping 0.7 m behind the back edge's middle reaches 0.2 m past the edge.
        var back = item.Anchor - facing * apron.HalfDepth;
        var behind = site.Paving.PavingEdgeFacing(back - facing * (FurnitureStyle.ApronPavingClear + 0.2f)) ?? throw new InvalidOperationException("no paving edge behind the kiosk");
        AssertThat(behind.Gap is > 0.05f and < 0.35f).OverrideFailureMessage($"the paving edge lies {behind.Gap + FurnitureStyle.ApronPavingClear:F2} m behind the back edge, not 0.5 m").IsTrue();
        AssertThat(Vector2.Dot(behind.Inward, facing) > 0.98f).OverrideFailureMessage("the back edge is not parallel to the paving edge, or the face looks at it").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void AnchorsAndEntrancesFaceTheirData()
    {
        var world = World.Value;
        var layout = RealLayout.Value;
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "anchor"))
        {
            var item = layout.Items.Single(i => i.Key == "anchor:" + feature.Id.Value);
            var ring = ((PolygonGeometry)feature.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            // On its footprint, or moved to the paving edge nearby (AnchorsStandOnOrBesidePaving).
            AssertThat(Vector2.Distance(centre, item.Anchor) < FurnitureStyle.AnchorPavingReach + TestTolerance.Layout * 2).OverrideFailureMessage($"{item.Key} strayed {Vector2.Distance(centre, item.Anchor):F2} m from its footprint").IsTrue();
            var yaw = feature.Props!.Value.GetProperty("yawDeg").GetSingle();
            AssertThat(Vector2.Dot(Front(item.Part.At), ExpectedFacing(layout, feature)) > 0.999f).OverrideFailureMessage($"{item.Key} does not face {yaw}° (or the yaw its diagnostic message gives)").IsTrue();
        }
        foreach (var door in world.Interiors.EntrancesNear(Vector3.Zero, 1e5f).Where(e => e.Open))
        {
            var outward = Angles.Heading(door.YawDeg);
            var mat = layout.Items.Single(i => i.Key == $"door:{door.Id}:mat");
            var lamp = layout.Items.Single(i => i.Key == $"door:{door.Id}:lamp");
            AssertThat(Vector2.Dot(mat.Anchor - new Vector2(door.Center.X, door.Center.Z), outward) > 0.4f).OverrideFailureMessage($"{door.Id}: mat not in front of the door").IsTrue();
            // On the entrance floor, never below the paving in front of the door.
            var floor = MathF.Max(door.Center.Y, world.Terrain.HeightAt(mat.Anchor.X, mat.Anchor.Y) + GroundLevels.Paving);
            AssertThat(MathF.Abs(mat.Part.At.M42 - floor) < TestTolerance.Position && lamp.Part.At.M42 > floor + 2f).OverrideFailureMessage($"{door.Id}: kit off the entrance floor").IsTrue();
            AssertThat(Vector2.Dot(Front(lamp.Part.At), outward) > 0.999f && lamp.Emitters.Single().Channel == StreetStyle.FirstGroup).OverrideFailureMessage($"{door.Id}: lamp").IsTrue();
        }
        // A building whose props another package claims keeps its own kit.
        var claimed = world.Interiors.EntrancesNear(Vector3.Zero, 1e5f).First(e => e.Open);
        var owner = world.Features.Keys.First(f => world.Interiors.Get(f)?.Entrances.Any(e => e.Id == claimed.Id) == true);
        var building = world.Interiors.Get(owner)!.Feature;
        var skipped = Build(world, (f, a) => f == building && a == Aspects.Props ? "zone.south" : null);
        AssertThat(!skipped.Items.Any(i => i.Key.StartsWith($"door:{claimed.Id}:", StringComparison.Ordinal))).OverrideFailureMessage("kit placed on a claimed building").IsTrue();
        AssertThat(skipped.Count(FurnitureKind.EntranceMat) == layout.Count(FurnitureKind.EntranceMat) - world.Interiors.Get(owner)!.Entrances.Count(e => e.Open)).OverrideFailureMessage("claim skipped other kits").IsTrue();
    }
}
