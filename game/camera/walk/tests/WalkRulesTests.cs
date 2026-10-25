using System.Numerics;
using System.Text.Json;
using Campus.Camera.Walk;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class WalkRulesTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CylinderDimensionsKeepStandingCrouchingAndFlatBottomShapesDistinctFromCapsules()
    {
        AssertThat(WalkMath.ValidCylinderDimensions(WalkRules.Radius, WalkRules.StandingHeight)
            && WalkMath.ValidCylinderDimensions(WalkRules.Radius, WalkTuning.CrouchingHeight)).OverrideFailureMessage("Standing or crouching cylinder dimensions were rejected.").IsTrue();
        AssertThat(WalkMath.ValidCylinderDimensions(WalkRules.Radius, .2f)).OverrideFailureMessage("A capsule height-to-radius rule was imposed on a cylinder.").IsTrue();
        AssertThat(!WalkMath.ValidCylinderDimensions(float.NaN, WalkRules.StandingHeight)
            && !WalkMath.ValidCylinderDimensions(WalkRules.Radius, float.PositiveInfinity)
            && !WalkMath.ValidCylinderDimensions(0, WalkRules.StandingHeight) && !WalkMath.ValidCylinderDimensions(WalkRules.Radius, -1)).OverrideFailureMessage("Invalid dimensions can be presented as a usable native shape snapshot.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void NonPlayingStatesFreezeVelocityAndInterpolationWithoutMovingFeet()
    {
        var feet = new Vector3(12, 3, -8);
        var previous = new Vector3(-20, 40, 50);
        var velocity = new Vector3(3, -8, 2);
        AssertThat(WalkMath.FreezeUnlessPlaying(false, feet, ref previous, ref velocity)).OverrideFailureMessage("Non-playing state did not freeze physics.").IsTrue();
        AssertThat(previous == feet && velocity == Vector3.Zero && feet == new Vector3(12, 3, -8)).OverrideFailureMessage("Freeze moved the restored body or retained velocity/interpolation drift.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EnteringToPlayingReleasesOnlyTheStateFreezeAndKeepsNormalMotionInputs()
    {
        var feet = new Vector3(12, 3, -8);
        var previous = feet + Vector3.UnitX;
        var velocity = new Vector3(4, -9, 2);
        AssertThat(WalkMath.FreezeUnlessPlaying(false, feet, ref previous, ref velocity)).OverrideFailureMessage("Entering did not freeze.").IsTrue();
        AssertThat(!WalkMath.FreezeUnlessPlaying(true, feet, ref previous, ref velocity)
            && previous == feet && velocity == Vector3.Zero).OverrideFailureMessage("Playing teleported or injected a resume impulse.").IsTrue();
        previous = feet - Vector3.UnitX;
        velocity = new Vector3(2, -3, 1);
        AssertThat(!WalkMath.FreezeUnlessPlaying(true, feet, ref previous, ref velocity)
            && previous == feet - Vector3.UnitX && velocity == new Vector3(2, -3, 1)).OverrideFailureMessage("Playing motion was frozen; Menu/Atlas input ownership must not become a physics pause.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DiagonalMovementHasNoSpeedBonusAndYawMatchesCamera()
    {
        var diagonal = WalkMath.Direction(new(1, 1), 0);
        AssertThat(MathF.Abs(diagonal.Length() - 1) < TestTolerance.FloatRelative).OverrideFailureMessage("Diagonal movement has a speed bonus.").IsTrue();
        AssertThat(Vector3.Distance(WalkMath.Direction(new(0, 1), 90), Vector3.UnitX) < TestTolerance.FloatRelative).OverrideFailureMessage("Walk forward disagrees with the camera's positive yaw.").IsTrue();
        AssertThat(Vector3.Distance(WalkMath.Direction(new(1, 0), 0), Vector3.UnitX) < TestTolerance.FloatRelative).OverrideFailureMessage("Strafing right uses the wrong axis.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void AccelerationDoesNotDependOnPhysicsSubdivision()
    {
        var target = new Vector3(WalkRules.WalkSpeed, 0, 0);
        var coarse = Vector3.Zero;
        var fine = Vector3.Zero;
        for (int i = 0; i < 30; i++) coarse = WalkMath.HorizontalVelocity(coarse, target, true, 1f / 60);
        for (int i = 0; i < 60; i++) fine = WalkMath.HorizontalVelocity(fine, target, true, 1f / 120);
        AssertThat(Vector3.Distance(coarse, fine) < TestTolerance.Position).OverrideFailureMessage("Exponential acceleration changed with fixed-step subdivision.").IsTrue();
        AssertThat(fine.X > target.X - TestTolerance.Position && fine.X < target.X).OverrideFailureMessage("Half-second acceleration does not approach the target speed.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SpeedCapPreservesVerticalJumpAndCrouchRemainsSlower()
    {
        var velocity = WalkMath.LimitHorizontal(new(20, WalkTuning.JumpSpeed, 20));
        AssertThat(MathF.Abs(new Vector2(velocity.X, velocity.Z).Length() - WalkTuning.MaximumHorizontalSpeed) < TestTolerance.FloatRelative).OverrideFailureMessage("Horizontal speed cap is missing.").IsTrue();
        AssertThat(velocity.Y == WalkTuning.JumpSpeed).OverrideFailureMessage("Speed multiplier changes vertical jump impulse.").IsTrue();
        for (int step = 0; step < WalkRules.SpeedStepCount; step++)
            AssertThat(WalkTuning.CrouchSpeed * WalkRules.SpeedMultiplier(step) < WalkRules.WalkSpeed * WalkRules.SpeedMultiplier(step)).OverrideFailureMessage("Crouch is not slower at a supported speed step.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CampusBoundarySlowsOnlyOutwardMotionAndPreservesSlidingAndJumping()
    {
        var inward = -Vector3.UnitX;
        var incoming = new Vector3(WalkRules.WalkSpeed, WalkTuning.JumpSpeed, WalkRules.WalkSpeed);
        BoundaryHit[] contacts = [new(0, inward)];
        var stopped = WalkMath.SlowAtBoundaries(incoming, contacts);
        AssertThat(stopped.X == 0 && stopped.Y == incoming.Y && stopped.Z == incoming.Z)
            .OverrideFailureMessage("Boundary contact changed tangential or vertical motion instead of stopping only outward motion.").IsTrue();
        contacts[0] = new(WalkRules.BoundarySlowdownDistance * .5f, inward);
        var slowed = WalkMath.SlowAtBoundaries(incoming, contacts);
        AssertThat(slowed.X > stopped.X && slowed.X < incoming.X && slowed.Y == incoming.Y && slowed.Z == incoming.Z)
            .OverrideFailureMessage("Approaching the campus boundary does not preserve sliding and jumping while slowing outward motion.").IsTrue();
        var retreat = incoming with { X = -incoming.X };
        AssertThat(WalkMath.SlowAtBoundaries(retreat, contacts) == retreat)
            .OverrideFailureMessage("The campus boundary slows movement back into campus.").IsTrue();
        contacts[0] = new(WalkRules.BoundarySlowdownDistance, inward);
        AssertThat(WalkMath.SlowAtBoundaries(incoming, contacts) == incoming)
            .OverrideFailureMessage("Boundary slowing extends beyond its declared approach distance.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SaveRoundTripKeepsFeetLookCrouchSpeedAndSafePoints()
    {
        var dto = new WalkSaveData(new(new(12.25f, 3, -7), 83, -12), true, 4,
            [new(new(11, 3, -7), 45, 9), new(new(10, 3, -7), 0, 0)]);
        var context = new WalkSaveJsonContext(new JsonSerializerOptions(ContractJson.Options));
        var json = JsonSerializer.Serialize(dto, context.WalkSaveData);
        var restored = JsonSerializer.Deserialize(json, context.WalkSaveData)!;
        AssertThat(restored.Feet == dto.Feet && restored.Crouching && restored.SpeedStep == 4).OverrideFailureMessage("Walk save lost its physical state.").IsTrue();
        AssertThat(restored.SafePoints.SequenceEqual(dto.SafePoints)).OverrideFailureMessage("Walk save lost the existing safePoints field.").IsTrue();
        AssertThat(!json.Contains("velocity", StringComparison.OrdinalIgnoreCase)).OverrideFailureMessage("Transient velocity leaked into the save.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SafetyHistoryKeepsRecentRingAndChoosesNearestEligiblePoint()
    {
        var points = new WalkSafePoints();
        for (int i = 0; i < WalkTuning.SafetyPointCapacity + 2; i++) points.Record(new(new(i, 0, 0), i, 0));
        var saved = points.Capture();
        AssertThat(saved.Length == WalkTuning.SafetyPointCapacity && saved[0].Position.X == 2
            && saved[^1].Position.X == WalkTuning.SafetyPointCapacity + 1)
            .OverrideFailureMessage("Safety history did not retain the most recent chronological ring.").IsTrue();
        var roundTrip = new WalkSafePoints();
        foreach (var pose in saved) roundTrip.Record(pose);
        AssertThat(roundTrip.Capture().SequenceEqual(saved)
            && roundTrip.Nearest(new(8.5f, 0, 0), WalkTuning.SafetyReturnMinimumDistance)?.Position.X == 5)
            .OverrideFailureMessage("Restored safety history lost order or selected a point inside the recovery exclusion distance.").IsTrue();
        var nearest = roundTrip.Nearest(new(8.5f, 0, 0), WalkTuning.SafetyReturnMinimumDistance)!.Value;
        int skip = saved.Length - Array.LastIndexOf(saved, nearest);
        var earlier = roundTrip.Nearest(nearest.Position, WalkTuning.SafetyReturnMinimumDistance, skip);
        AssertThat(earlier is { } older && Array.IndexOf(saved, older) < Array.IndexOf(saved, nearest))
            .OverrideFailureMessage("Repeated recovery did not select an earlier history point.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ForbiddenSurfacesCannotBecomeLandingPoints()
    {
        foreach (var value in new[] { WalkClass.Building, WalkClass.Water, WalkClass.Forbidden, WalkClass.Outside, WalkClass.Steep })
            AssertThat(!WalkMath.CanWalk(value)).OverrideFailureMessage("Forbidden landing class accepted: " + value).IsTrue();
        foreach (var value in new[] { WalkClass.Walk, WalkClass.Interior, WalkClass.Wade })
            AssertThat(WalkMath.CanWalk(value)).OverrideFailureMessage("Walkable landing class rejected: " + value).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RiverbedNearShoreUsesWaterSurfaceDepthAndCannotBecomeSavedFeet()
    {
        var spatial = new SafetySpatial(-1f, -.5f, WalkClass.Water, nearShore: true);
        var feet = new Vector3(0, -1, 0);
        AssertThat(spatial.GroundY(0, 0) == -1f && spatial.MediumAt(feet).Depth == .5f).OverrideFailureMessage("Regression fixture must distinguish bed height from water height.").IsTrue();
        AssertThat(WalkSafetyQueries.NeedsRecovery(spatial, feet, false)).OverrideFailureMessage("Standing on the riverbed beside a walkable bank prevents the water return.").IsTrue();
        AssertThat(WalkSafetyQueries.InWater(spatial, feet) && !WalkSafetyQueries.AllowsFeet(spatial, feet)).OverrideFailureMessage("A supported underwater position is treated as a settled safe snapshot.").IsTrue();
        var walkCell = new SafetySpatial(-1f, -.5f, WalkClass.Walk);
        AssertThat(WalkSafetyQueries.NeedsRecovery(walkCell, feet, false)).OverrideFailureMessage("A coarse walk cell hides actual submersion at the feet.").IsTrue();
        AssertThat(!WalkSafetyQueries.NeedsRecovery(spatial, feet, true)).OverrideFailureMessage("Permitted swimming or supported wading still triggers deep-water recovery.").IsTrue();
        var entry = new WaterSample(0, -WaterMotionRules.EnterColumnDepth, true);
        var exit = entry with { BottomY = -WaterMotionRules.ExitColumnDepth };
        var between = entry with { BottomY = -(WaterMotionRules.EnterColumnDepth + WaterMotionRules.ExitColumnDepth) * .5f };
        AssertThat(WalkMath.SwimmingAt(entry, feet.Y, false)
            && WalkMath.SwimmingAt(between, feet.Y, true) && !WalkMath.SwimmingAt(between, feet.Y, false)
            && !WalkMath.SwimmingAt(exit, feet.Y, true)).OverrideFailureMessage("Swimming entry and exit lost the shared depth hysteresis.").IsTrue();
        AssertThat(!WalkMath.SwimmingAt(entry with { CanSwim = false }, feet.Y, true)
            && !WalkMath.SwimmingAt(entry, WalkRules.StandingHeight, false)).OverrideFailureMessage("Swimming ignores permission or captures dry bridge feet above the water.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SupportedBridgeAboveWaterDoesNotTriggerWaterRecovery()
    {
        var feet = new Vector3(0, 2, 0);
        var bridge = new SafetySpatial(2f, -.5f, WalkClass.Walk, ground: -1f);
        AssertThat(!WalkSafetyQueries.InWater(bridge, feet) && !WalkSafetyQueries.NeedsRecovery(bridge, feet, false)).OverrideFailureMessage("A dry supported bridge is mistaken for the riverbed beneath it.").IsTrue();
        AssertThat(WalkSafetyQueries.AllowsFeet(bridge, feet)).OverrideFailureMessage("A dry bridge region is rejected before native support checks.").IsTrue();
        var coarseWaterCell = new SafetySpatial(2f, -.5f, WalkClass.Water, ground: -1f);
        AssertThat(!WalkSafetyQueries.NeedsRecovery(coarseWaterCell, feet, false)).OverrideFailureMessage("A supported dry bridge over a coarse water cell triggers a water return.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DefaultFeetUsesNewGameTargetCatalogueBeforeThatWorldMounts()
    {
        var (entry, oldContext) = LoadCampusSpawnFixture();
        var target = new MapId("target-fixture");
        var waypoint = entry.Waypoints.First(w => w.Stand is not null);
        var targetEntry = entry with
        {
            Descriptor = entry.Descriptor with { Id = target, DefaultSpawn = waypoint.Id, Spawns = [] },
            Waypoints = [waypoint]
        };
        var lastRequestedMap = default(MapId);
        var maps = SpawnMaps(targetEntry);
        maps.Current = entry.Descriptor.Id;
        maps.Context = oldContext;
        var resolve = maps.SpawnResolver!;
        maps.SpawnResolver = (map, spawn) => { lastRequestedMap = map; return resolve(map, spawn); };
        var dto = WalkSafetyQueries.CreateDefault(maps, new(1, target, targetEntry.Descriptor.World.Default, ViewMode.Walk));
        AssertThat(lastRequestedMap == target && dto.Feet.Position == waypoint.Stand!.Feet
            && dto.Feet.YawDeg == waypoint.Stand.YawDeg).OverrideFailureMessage("CreateDefault ignored NewGameContext.Map or reused the previously mounted map.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void UnknownDefaultSpawnIsNotReplacedByTheOriginOrAnotherPoint()
    {
        var (entry, context) = LoadCampusSpawnFixture();
        var missing = entry with { Descriptor = entry.Descriptor with { DefaultSpawn = "missing-fixture" } };
        var maps = SpawnMaps(missing);
        maps.Context = context;
        var missingSpawnRejected = false;
        try { WalkSafetyQueries.DefaultFeet(maps, entry.Descriptor.Id); }
        catch (ArgumentException) { missingSpawnRejected = true; }
        AssertThat(missingSpawnRejected).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}; a default feet position was invented instead.").IsTrue();
        var unknownMapRejected = false;
        try { WalkSafetyQueries.DefaultFeet(maps, new("unknown-fixture")); }
        catch (ArgumentException) { unknownMapRejected = true; }
        AssertThat(unknownMapRejected).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}; a default feet position was invented instead.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task RestorePreparationValidatesHistoryAfterTerrainAndFinishesEveryRepairBeforeCommit()
    {
        var terrain = new HistoryTerrain();
        var map = new TestMapContext(FlightTestSupport.CreateContext().Descriptor, new FakeWorld { Spatial = terrain });
        var preparation = new WalkTerrainPreparation(terrain);
        preparation.BindMap(map);
        var safe = new Pose(new(1, 0, 1), 15, 0);
        var unsafePoint = new Pose(new(2, -1, 2), 0, 0);
        var invalid = new Pose(new(float.NaN, 0, 0), 0, 0);
        var dto = new WalkSaveData(invalid with { YawDeg = 43, PitchDeg = 120 }, true, WalkRules.SpeedStepCount,
            [safe, unsafePoint, invalid, new(new(1000, 0, 0), 0, 0)]);
        var context = new RestoreContext { Map = map.Descriptor.Id, FromVersion = 1, Missing = false };
        var preparing = WalkPersistence.PrepareRestore(dto, context, map, preparation, new(new(0, 0, 0), 0, 0), default);
        await terrain.Entered.Task;
        AssertThat(!preparing.IsCompleted && terrain.Queries == 0 && !terrain.Prepared).IsTrue();
        terrain.Release.SetResult();
        var prepared = await preparing;
        AssertThat(prepared.SafePoints.SequenceEqual(new[] { safe }) && prepared.Feet.Position == safe.Position
            && prepared.Feet.YawDeg == 43 && prepared.Feet.PitchDeg == 89
            && prepared.SpeedStep == WalkRules.SpeedStepCount - 1).IsTrue();
        AssertThat(context.Repairs.Contains("walk: unsafe history point removed")
            && context.Repairs.Contains("walk: invalid feet replaced by a pending landing origin")
            && dto.SafePoints.Count == 4 && float.IsNaN(dto.Feet.Position.X)).IsTrue();
        int repairs = context.Repairs.Count;
        await Task.Yield();
        AssertThat(context.Repairs.Count == repairs).OverrideFailureMessage("Preparation left a late repair writer.").IsTrue();
    }

    private sealed class HistoryTerrain : ITerrainPreparation, ISpatialIndex
    {
        public TaskCompletionSource Entered { get; } = new();
        public TaskCompletionSource Release { get; } = new();
        public bool Prepared { get; private set; }
        public int Queries { get; private set; }
        public bool IsPrepared(IWorldModel world, Vector2 point) => Prepared;
        public Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default) => throw new NotSupportedException();
        public async Task PreparePoints(IMapContext context, IReadOnlyList<Vector2> points, CancellationToken cancellation = default)
        {
            AssertThat(points.All(point => float.IsFinite(point.X) && float.IsFinite(point.Y))).IsTrue();
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellation);
            Prepared = true;
        }
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => 0;
        public CameraMedium MediumAt(Vector3 position)
        {
            if (!Prepared) throw new InvalidOperationException("History queried terrain before preparation.");
            Queries++;
            return position.Y < 0 ? new(MediumKind.Underwater) : new(MediumKind.Air);
        }
        public WalkClass WalkClassAt(float x, float z)
        {
            if (!Prepared) throw new InvalidOperationException("History queried terrain before preparation.");
            return WalkClass.Walk;
        }
    }

    private static (MapCatalogEntry Entry, IMapContext Context) LoadCampusSpawnFixture()
    {
        var entry = RealMapFixture.Entry();
        return (entry, new TestMapContext(entry.Descriptor, new FakeWorld(), DataRoot: entry.DataRoot));
    }

    private static TestMapService SpawnMaps(MapCatalogEntry entry) => new()
    {
        Rev = 1,
        Default = entry.Descriptor.Id,
        Current = entry.Descriptor.Id,
        Maps = [entry.Descriptor],
        SpawnResolver = (map, spawn) => map == entry.Descriptor.Id
            ? entry.ResolveSpawn(spawn ?? entry.Descriptor.DefaultSpawn)
            : throw new ArgumentException("Unknown fixture map.", nameof(map)),
    };

    private sealed class SafetySpatial(float support, float? water = null, WalkClass classification = WalkClass.Walk,
        bool nearShore = false, float? ground = null) : ISpatialIndex
    {
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => ground ?? support;
        public CameraMedium MediumAt(Vector3 p) => water is { } y && p.Y < y
            ? new(MediumKind.Underwater, Depth: y - p.Y) : new(MediumKind.Air);
        public WalkClass WalkClassAt(float x, float z) => nearShore && (MathF.Abs(x) > 1 || MathF.Abs(z) > 1)
            ? WalkClass.Wade : classification;
    }

}
