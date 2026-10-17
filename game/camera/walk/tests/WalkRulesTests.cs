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
    public void SaveRoundTripKeepsLegacyFeetLookCrouchSpeedAndSafePoints()
    {
        var dto = new WalkSave(new(new(12.25f, 3, -7), 83, -12), true, 4,
            [new(new(11, 3, -7), 45, 9), new(new(10, 3, -7), 0, 0)]);
        var context = new WalkSaveJsonContext(new JsonSerializerOptions(ContractJson.Options));
        var json = JsonSerializer.Serialize(dto, context.WalkSave);
        var restored = JsonSerializer.Deserialize(json, context.WalkSave)!;
        Require(restored.Feet == dto.Feet && restored.Crouching && restored.SpeedStep == 4,
            "Walk save lost its physical state.");
        Require(restored.SafePoints.SequenceEqual(dto.SafePoints), "Walk save lost the existing safePoints field.");
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
        Require(spatial.GroundY(0, 0) == -1f && spatial.MediumAt(feet).Depth == .5f,
            "Regression fixture must distinguish bed height from water height.");
        Require(WalkSafetyQueries.NeedsRecovery(spatial, feet),
            "Standing on the riverbed beside a walkable bank prevents the water return.");
        Require(WalkSafetyQueries.InWater(spatial, feet) && !WalkSafetyQueries.AllowsFeet(spatial, feet),
            "A supported underwater position is treated as a settled safe snapshot.");
        var walkCell = new SafetySpatial(-1f, -.5f, WalkClass.Walk);
        Require(WalkSafetyQueries.NeedsRecovery(walkCell, feet),
            "A coarse walk cell hides actual submersion at the feet.");
    }

    [TestCase]
    public void SupportedBridgeAboveWaterDoesNotTriggerWaterRecovery()
    {
        var feet = new Vector3(0, 2, 0);
        var bridge = new SafetySpatial(2f, -.5f, WalkClass.Walk, ground: -1f);
        Require(!WalkSafetyQueries.InWater(bridge, feet) && !WalkSafetyQueries.NeedsRecovery(bridge, feet),
            "A dry supported bridge is mistaken for the riverbed beneath it.");
        Require(WalkSafetyQueries.AllowsFeet(bridge, feet), "A dry bridge region is rejected before native support checks.");
        var coarseWaterCell = new SafetySpatial(2f, -.5f, WalkClass.Water, ground: -1f);
        Require(!WalkSafetyQueries.NeedsRecovery(coarseWaterCell, feet),
            "A supported dry bridge over a coarse water cell triggers a water return.");
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
        public WalkClass WalkClassAt(float x, float z) => nearShore && (MathF.Abs(x) > 1 || MathF.Abs(z) > 1)
            ? WalkClass.Wade : classification;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
