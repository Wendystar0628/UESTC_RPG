using System.Numerics;
using System.Text.Json;
using Campus.Camera.Walk;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Gen.World;
using Campus.Kernel.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class WalkRulesTests
{
    [TestCase]
    public void CylinderDimensionsKeepStandingCrouchingAndFlatBottomShapesDistinctFromCapsules()
    {
        Require(WalkMath.ValidCylinderDimensions(.3f, 1.8f) && WalkMath.ValidCylinderDimensions(.3f, 1.5f),
            "Standing or crouching cylinder dimensions were rejected.");
        Require(WalkMath.ValidCylinderDimensions(.3f, .2f), "A capsule height-to-radius rule was imposed on a cylinder.");
        Require(!WalkMath.ValidCylinderDimensions(float.NaN, 1.8f)
            && !WalkMath.ValidCylinderDimensions(.3f, float.PositiveInfinity)
            && !WalkMath.ValidCylinderDimensions(0, 1.8f) && !WalkMath.ValidCylinderDimensions(.3f, -1),
            "Invalid dimensions can be presented as a usable native shape snapshot.");
    }

    [TestCase]
    public void NonPlayingStatesFreezeVelocityAndInterpolationWithoutMovingFeet()
    {
        var feet = new Vector3(12, 3, -8);
        foreach (var state in new[] { AppState.Boot, AppState.Title, AppState.Entering, AppState.Leaving, AppState.Quitting })
        {
            var previous = new Vector3(-20, 40, 50);
            var velocity = new Vector3(3, -8, 2);
            Require(WalkMath.FreezeForAppState(state, feet, ref previous, ref velocity), "Non-playing state did not freeze physics: " + state);
            Require(previous == feet && velocity == Vector3.Zero && feet == new Vector3(12, 3, -8),
                "Freeze moved the restored body or retained velocity/interpolation drift.");
        }
    }

    [TestCase]
    public void EnteringToPlayingReleasesOnlyTheStateFreezeAndKeepsNormalMotionInputs()
    {
        var feet = new Vector3(12, 3, -8);
        var previous = feet + Vector3.UnitX;
        var velocity = new Vector3(4, -9, 2);
        Require(WalkMath.FreezeForAppState(AppState.Entering, feet, ref previous, ref velocity), "Entering did not freeze.");
        Require(!WalkMath.FreezeForAppState(AppState.Playing, feet, ref previous, ref velocity)
            && previous == feet && velocity == Vector3.Zero, "Playing teleported or injected a resume impulse.");
        previous = feet - Vector3.UnitX;
        velocity = new Vector3(2, -3, 1);
        Require(!WalkMath.FreezeForAppState(AppState.Playing, feet, ref previous, ref velocity)
            && previous == feet - Vector3.UnitX && velocity == new Vector3(2, -3, 1),
            "Playing motion was frozen; Menu/Atlas input ownership must not become a physics pause.");
    }

    [TestCase]
    public void DiagonalMovementHasNoSpeedBonusAndYawMatchesCamera()
    {
        var diagonal = WalkMath.Direction(new(1, 1), 0);
        Require(MathF.Abs(diagonal.Length() - 1) < .00001f, "Diagonal movement has a speed bonus.");
        Require(Vector3.Distance(WalkMath.Direction(new(0, 1), 90), Vector3.UnitX) < .00001f,
            "Walk forward disagrees with the camera's positive yaw.");
        Require(Vector3.Distance(WalkMath.Direction(new(1, 0), 0), Vector3.UnitX) < .00001f,
            "Strafing right uses the wrong axis.");
    }

    [TestCase]
    public void AccelerationDoesNotDependOnPhysicsSubdivision()
    {
        var target = new Vector3(3.6f, 0, 0);
        var coarse = Vector3.Zero;
        var fine = Vector3.Zero;
        for (int i = 0; i < 30; i++) coarse = WalkMath.HorizontalVelocity(coarse, target, true, 1f / 60);
        for (int i = 0; i < 60; i++) fine = WalkMath.HorizontalVelocity(fine, target, true, 1f / 120);
        Require(Vector3.Distance(coarse, fine) < .00001f, "Exponential acceleration changed with fixed-step subdivision.");
        Require(fine.X > 3.58f && fine.X < 3.6f, "Half-second acceleration does not approach the target speed.");
    }

    [TestCase]
    public void SpeedCapPreservesVerticalJumpAndCrouchRemainsSlower()
    {
        var velocity = WalkMath.LimitHorizontal(new(20, WalkTuning.JumpSpeed, 20));
        Require(MathF.Abs(new Vector2(velocity.X, velocity.Z).Length() - 12f) < .00001f,
            "Horizontal speed cap is missing.");
        Require(velocity.Y == WalkTuning.JumpSpeed, "Speed multiplier changes vertical jump impulse.");
        for (int step = 0; step < WalkTuning.SpeedStepCount; step++)
            Require(WalkTuning.CrouchSpeed * WalkTuning.SpeedMultiplier(step) < WalkTuning.WalkSpeed * WalkTuning.SpeedMultiplier(step),
                "Crouch is not slower at a supported speed step.");
    }

    [TestCase]
    public void SafeRingWrapsRestoresAndSkipsRecentPointsOnRepeatedRecovery()
    {
        var ring = new WalkSafetyRing();
        for (int i = 0; i < 20; i++) ring.Add(new(new(i, 0, 0), 0, 0));
        Require(ring.Count == 16, "Safety ring grew beyond its save capacity.");
        Require(ring.Snapshot()[0].Position.X == 19 && ring.Snapshot()[15].Position.X == 4, "Ring wrap order is wrong.");
        var copy = new WalkSafetyRing();
        copy.Restore(ring.Snapshot());
        Require(copy.Snapshot().SequenceEqual(ring.Snapshot()), "Restore reverses the safety ring.");
        Require(copy.Nearest(new(20, 0, 0), 3, 0, _ => true)?.Position.X == 17, "Manual return ignores the 3 m minimum.");
        Require(copy.Nearest(new(20, 0, 0), 0, 2, _ => true)?.Position.X == 17, "Repeated return reuses a rejected recent point.");
        copy.Clear();
        Require(copy.Count == 0 && copy.Nearest(Vector3.Zero, 0, 0, _ => true) is null, "Map unload retains old safe points.");
    }

    [TestCase]
    public void SaveRoundTripKeepsFeetLookCrouchSpeedAndSafeRing()
    {
        var dto = new WalkSave(new(new(12.25f, 3, -7), 83, -12), true, 4,
            [new(new(11, 3, -7), 45, 9), new(new(10, 3, -7), 0, 0)]);
        var context = new WalkSaveJsonContext(new JsonSerializerOptions(ContractJson.Options));
        var json = JsonSerializer.Serialize(dto, context.WalkSave);
        var restored = JsonSerializer.Deserialize(json, context.WalkSave)!;
        Require(restored.Feet == dto.Feet && restored.Crouching && restored.SpeedStep == 4,
            "Walk save lost its physical state.");
        Require(restored.SafePoints.SequenceEqual(dto.SafePoints), "Walk save lost the recovery ring.");
        Require(!json.Contains("velocity", StringComparison.OrdinalIgnoreCase), "Transient velocity leaked into the save.");
    }

    [TestCase]
    public void ForbiddenSurfacesCannotBecomeLandingPoints()
    {
        foreach (var value in new[] { WalkClass.Building, WalkClass.Water, WalkClass.Forbidden, WalkClass.Outside, WalkClass.Steep })
            Require(!WalkMath.CanWalk(value), "Forbidden landing class accepted: " + value);
        foreach (var value in new[] { WalkClass.Walk, WalkClass.Interior, WalkClass.Wade })
            Require(WalkMath.CanWalk(value), "Walkable landing class rejected: " + value);
    }

    [TestCase]
    public void RiverbedNearShoreUsesWaterSurfaceDepthAndCannotBecomeSavedFeet()
    {
        var spatial = new SafetySpatial(-1f, -.5f, WalkClass.Water, nearShore: true);
        var feet = new Vector3(0, -1, 0);
        Require(spatial.SupportY(0, 0, feet.Y) == -1f && spatial.MediumAt(feet).Depth == .5f,
            "Regression fixture must distinguish bed height from water height.");
        Require(WalkSafetyQueries.NeedsRecovery(spatial, feet),
            "Standing on the riverbed beside a walkable bank prevents the water return.");
        Require(WalkSafetyQueries.InWater(spatial, feet) && !WalkSafetyQueries.SupportedDryFeet(spatial, feet),
            "A supported underwater position is treated as a settled safe snapshot.");
        var walkCell = new SafetySpatial(-1f, -.5f, WalkClass.Walk);
        Require(WalkSafetyQueries.NeedsRecovery(walkCell, feet) && !WalkSafetyQueries.SupportedDryFeet(walkCell, feet),
            "A coarse walk cell hides actual submersion at the feet.");
    }

    [TestCase]
    public void SupportedBridgeAboveWaterDoesNotTriggerWaterRecovery()
    {
        var feet = new Vector3(0, 2, 0);
        var bridge = new SafetySpatial(2f, -.5f, WalkClass.Walk, ground: -1f);
        Require(!WalkSafetyQueries.InWater(bridge, feet) && !WalkSafetyQueries.NeedsRecovery(bridge, feet),
            "A dry supported bridge is mistaken for the riverbed beneath it.");
        Require(WalkSafetyQueries.SupportedDryFeet(bridge, feet), "A dry supported bridge cannot be saved.");
        var coarseWaterCell = new SafetySpatial(2f, -.5f, WalkClass.Water, ground: -1f);
        Require(!WalkSafetyQueries.NeedsRecovery(coarseWaterCell, feet),
            "A supported dry bridge over a coarse water cell triggers a water return.");
    }

    [TestCase]
    public void RestoreRejectsHighSafetyPointBeforeRecoveryOrForcedCapture()
    {
        var spatial = new SafetySpatial(0f);
        var context = new RestoreContext { Map = new("test"), FromVersion = 1, Missing = false };
        Pose high = new(new(10, 100, 0), 0, 0);
        Pose supported = new(new(4, 0, 0), 0, 0);
        var restored = WalkSafetyQueries.RestoreSafePoints(spatial, [high, supported], context);
        Require(restored.Count == 1 && restored[0] == supported && context.Repairs.Count == 1,
            "Unsupported safety-ring height survived restore without a repair record.");
        Require(WalkSafetyQueries.SupportedDryFeet(spatial, Vector3.Zero)
            && !WalkSafetyQueries.SupportedDryFeet(spatial, high.Position),
            "Current feet and history do not share the same support-height validation.");
        var ring = new WalkSafetyRing();
        ring.Restore(restored);
        Require(ring.Nearest(Vector3.Zero, 3, 0, p => WalkSafetyQueries.SupportedDryFeet(spatial, p.Position)) == supported,
            "Manual recovery selected the unsupported high point.");
        Require(ring.Snapshot().All(p => p.Position.Y == 0), "A forced snapshot retains an unsupported safety point.");
    }

    [TestCase]
    public void RealP00FlyOnlyDefaultFindsSupportedFeetInLoadedCampus()
    {
        var (entry, context) = LoadCampusSpawnFixture();
        var maps = new SpawnMaps(entry) { Context = context };
        var spawn = maps.ResolveSpawn(entry.Descriptor.Id);
        Require(spawn.Id == "P00" && spawn.Stand is null && spawn.Fly.Position.Y == 928f
            && entry.Descriptor.Spawns.Count == 0, "Regression fixture must be the authored high-altitude, waypoint-only P00.");
        var dto = WalkSafetyQueries.CreateDefault(maps, new(entry.Descriptor.Seed, entry.Descriptor.Id, entry.Descriptor.World.Default));
        Require(maps.LastRequestedMap == entry.Descriptor.Id, "Default resolution used a different map.");
        Require(WalkSafetyQueries.SupportedDryFeet(context.World.Spatial, dto.Feet.Position),
            "P00 new-game walk state does not have a real supported, dry safety destination.");
        Require(dto.Feet.Position != Vector3.Zero && dto.Feet.Position != spawn.Fly.Position && dto.SafePoints.Count == 0,
            "P00 fly position or the origin was passed off as a standing position.");
    }

    [TestCase]
    public void DefaultFeetUsesNewGameTargetCatalogueBeforeThatWorldMounts()
    {
        var (entry, oldContext) = LoadCampusSpawnFixture();
        var target = new MapId("target-fixture");
        var waypoint = entry.Waypoints.Single(w => w.Id == "P01");
        var targetEntry = entry with
        {
            Descriptor = entry.Descriptor with { Id = target, DefaultSpawn = waypoint.Id, Spawns = [] },
            Waypoints = [waypoint]
        };
        var maps = new SpawnMaps(targetEntry) { Current = entry.Descriptor.Id, Context = oldContext };
        var dto = WalkSafetyQueries.CreateDefault(maps, new(1, target, targetEntry.Descriptor.World.Default));
        Require(maps.LastRequestedMap == target && dto.Feet.Position == waypoint.Stand!.Feet
            && dto.Feet.YawDeg == waypoint.Stand.YawDeg,
            "CreateDefault ignored NewGameContext.Map or reused the previously mounted map.");
    }

    [TestCase]
    public void FlyOnlyDefaultRefusesMissingTargetWorldOrMissingSafetyPoint()
    {
        var (entry, context) = LoadCampusSpawnFixture();
        var maps = new SpawnMaps(entry);
        RequireThrows<InvalidOperationException>(() => WalkSafetyQueries.DefaultFeet(maps, entry.Descriptor.Id));
        maps.Current = new("previous-fixture");
        maps.Context = context;
        RequireThrows<InvalidOperationException>(() => WalkSafetyQueries.DefaultFeet(maps, entry.Descriptor.Id));
        maps.Current = entry.Descriptor.Id;
        maps.Context = new SpawnContext(context.Descriptor, new SpatialWorld(context.World, new SafetySpatial(0f)),
            context.DataRoot, context.BakeRoot);
        RequireThrows<InvalidOperationException>(() => WalkSafetyQueries.DefaultFeet(maps, entry.Descriptor.Id));
    }

    [TestCase]
    public void UnknownDefaultSpawnIsNotReplacedByTheOriginOrAnotherPoint()
    {
        var (entry, context) = LoadCampusSpawnFixture();
        var missing = entry with { Descriptor = entry.Descriptor with { DefaultSpawn = "missing-fixture" } };
        var maps = new SpawnMaps(missing) { Context = context };
        RequireThrows<ArgumentException>(() => WalkSafetyQueries.DefaultFeet(maps, entry.Descriptor.Id));
        RequireThrows<ArgumentException>(() => WalkSafetyQueries.DefaultFeet(maps, new("unknown-fixture")));
    }

    private static (MapCatalogEntry Entry, IMapContext Context) LoadCampusSpawnFixture()
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY")
            ?? throw new InvalidOperationException("The gate must provide UESTC_REPOSITORY for the real campus fixture.");
        var index = MapCatalog.ReadIndex(File.ReadAllBytes(Path.Combine(root, "game", "data", "maps", "index.json")));
        var indexed = index.Maps.Single(m => m.Id == index.DefaultMap);
        var data = Path.Combine(root, "game", indexed.Dir["res://".Length..]);
        var entry = MapCatalog.ReadEntry(indexed, File.ReadAllBytes(Path.Combine(data, "map.json")),
            File.ReadAllBytes(Path.Combine(data, "campus.json")), File.ReadAllBytes(Path.Combine(data, "waypoints.json")));
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in new[] { "campus.json", "network.json", "interiors.json", "map.json" })
            files.Add(name, File.ReadAllText(Path.Combine(data, name)));
        var loaded = WorldModelLoader.Load(indexed.Id, files);
        return (entry, entry.CreateContext(loaded, BuildInputSnapshot.Freeze(files.ToDictionary(p => p.Key, p => (ReadOnlyMemory<byte>)System.Text.Encoding.UTF8.GetBytes(p.Value), StringComparer.Ordinal))));
    }

    private sealed class SpawnMaps(MapCatalogEntry entry) : IMapService
    {
        public long Rev => 1;
        public MapId Default => entry.Descriptor.Id;
        public IReadOnlyList<MapDescriptor> Maps => [entry.Descriptor];
        public IReadOnlyList<MapVisit> Visits => [];
        public MapId Current { get; set; } = entry.Descriptor.Id;
        public IMapContext? Context { get; set; }
        public IMapTransition? Transition => null;
        public MapId LastRequestedMap { get; private set; }
        public event Action<MapId, MapTravelCause>? Arrived { add { } remove { } }
        public MapSpawn ResolveSpawn(MapId map, string? spawn = null)
        {
            LastRequestedMap = map;
            if (map != entry.Descriptor.Id) throw new ArgumentException("Unknown fixture map.", nameof(map));
            return entry.ResolveSpawn(spawn ?? entry.Descriptor.DefaultSpawn);
        }
        public IMapTransition Travel(MapTravelRequest request) => throw new NotSupportedException();
    }

    private sealed record SpawnContext(MapDescriptor Descriptor, IWorldModel World, string DataRoot, string BakeRoot) : IMapContext
    {
        public BuildInputSnapshot SourceInputs => BuildInputSnapshot.Empty;
    }

    private sealed class SpatialWorld(IWorldModel source, ISpatialIndex spatial) : IWorldModel
    {
        public PolygonRings Boundary => source.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => source.Features;
        public WorldNetwork Network => source.Network;
        public ISpatialIndex Spatial => spatial;
        public IInteriorIndex Interiors => source.Interiors;
        public IOutskirtsModel? Outskirts => source.Outskirts;
        public IFidelityModel? Fidelity => source.Fidelity;
    }

    private static void RequireThrows<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}; a default feet position was invented instead.");
    }

    private sealed class SafetySpatial(float support, float? water = null, WalkClass classification = WalkClass.Walk,
        bool nearShore = false, float? ground = null) : ISpatialIndex
    {
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => ground ?? support;
        public CameraMedium MediumAt(Vector3 p) => water is { } y && p.Y < y
            ? new(MediumKind.Underwater, Depth: y - p.Y) : new(MediumKind.Air);
        public float? SupportY(float x, float z, float yHint) => support;
        public WalkClass WalkClassAt(float x, float z) => nearShore && (MathF.Abs(x) > 1 || MathF.Abs(z) > 1)
            ? WalkClass.Wade : classification;
        public Vector3? NearestSafe(Vector3 p, float maxDist) => null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
