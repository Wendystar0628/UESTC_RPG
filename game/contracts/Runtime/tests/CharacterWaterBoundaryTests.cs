using System.Numerics;
using System.Text.Json;
using Campus.Contracts.Audio;
using GdUnit4;

namespace Campus.Contracts.Tests;

// Four focused boundary tests for after the candidate + shared.patch are applied.
// No Godot scene, native physics, water permissions fixture or asset readiness is claimed here.
[TestSuite]
public sealed class CharacterWaterBoundaryTests
{
    [TestCase]
    public void DiscontinuityFreezeAndUnavailableBodyDoNotInterpolateOrStimulate()
    {
        var before = Live(10);
        var current = Live(11);
        Check(current.CanInterpolateFrom(before) && current.Perceptible, "Adjacent live physical samples must remain usable.");
        Check(!(current with { Presence = WalkPresence.Pending }).Perceptible, "An unvalidated landing stimulated animals.");
        Check(!(current with { Mode = ViewMode.Fly }).Perceptible, "A flying camera was treated as a body.");
        Check(!(current with { Hold = WalkHold.WaterNotReady }).CanInterpolateFrom(before), "Missing water data allowed interpolation.");
        Check(!(current with { Stamp = current.Stamp with { Continuity = 2 } }).CanInterpolateFrom(before), "Warp swept from the old position.");
        Check(!(current with { Stamp = current.Stamp with { WorldInstance = 2 } }).CanInterpolateFrom(before), "Restore blended an old world body.");
        Check(!(current with { Stamp = current.Stamp with { Tick = 12 } }).CanInterpolateFrom(before), "A missing physics tick was invented.");
    }

    [TestCase]
    public void MissingWaterNeverBecomesReadyAndSwimmingDepthIsNotFootImmersion()
    {
        WaterQueryResult missing = default;
        Check(missing.Status == WaterQueryStatus.NotReady && missing.Sample is null, "Default query must fail closed.");
        var sample = new WaterSample(new(0, -.75f, 0), new("test:water"), "volume", "swim-area", null,
            0, -1.2f, WaterBottomKind.Terrain, -2, 1, WaterAccess.SurfaceSwim);
        var hit = new WaterQueryResult(WaterQueryStatus.Ready, sample, .5f);
        Check(MathF.Abs(hit.Sample!.Value.ColumnDepth - 1.2f) < .00001f && hit.Sample.Value.DepthAtPoint == .75f,
            "Column depth and the swimming reference-point immersion were conflated.");
        Throws<ArgumentException>(() => new WaterQueryResult(WaterQueryStatus.Ready));
        Throws<ArgumentException>(() => new WaterQueryResult(WaterQueryStatus.NotReady, sample));
        Throws<ArgumentException>(() => new WaterQueryResult(WaterQueryStatus.Ready, sample with { SwimAreaId = null }));
        Throws<ArgumentException>(() => new WaterQueryResult(WaterQueryStatus.Ready, sample with { SurfaceY = float.NaN }));
        Throws<ArgumentOutOfRangeException>(() => new WaterQueryResult(WaterQueryStatus.Ready, sample, 1.01f));
    }

    [TestCase]
    public void VisibleTreadsRoundTripWithoutInventingMetadata()
    {
        var spec = new StairSpec(.15f, .3f, 10);
        var ramp = new RampCollider(new(0, -.2f, 0), new(1.4f, -.2f, 0),
            new(1.4f, 1.5f, 3), new(0, 1.5f, 3), .15f, SurfaceTag.Stair)
        { Foot = FootMat.Concrete, Stair = spec };
        Check(ramp.Treads is null, "A ramp acquired invented stair geometry.");
        string oldJson = JsonSerializer.Serialize<Collider>(ramp, ContractJson.Options);
        Check(!oldJson.Contains("treads", StringComparison.Ordinal), "Null tread metadata changed old collider JSON.");
        var frame = new StairFrame(new(0, .15f, .1f), Vector3.UnitZ, Vector3.UnitY, 1.4f);
        frame.Validate(spec);
        var enriched = ramp with { Treads = frame };
        var restored = (RampCollider)JsonSerializer.Deserialize<Collider>(
            JsonSerializer.Serialize<Collider>(enriched, ContractJson.Options), ContractJson.Options)!;
        Check(restored.Treads == frame && restored.P0 == ramp.P0, "Visible tread origin was lost or replaced the buried collision ramp.");
        Throws<ArgumentException>(() => (frame with { Run = Vector3.UnitY }).Validate(spec));
    }

    [TestCase]
    public void LegacyRulesetConstructorAndJsonKeepBodyPermissionOffUnlessExplicit()
    {
        var legacy = new RulesetDescriptor(new("legacy"), [ViewMode.Fly, ViewMode.Walk],
            WorldControlMask.None, "all-unlocked", [], []);
        Check(!legacy.PlayerBody && !legacy.Characters, "A legacy ruleset unexpectedly enabled a character.");
        string json = JsonSerializer.Serialize(legacy, ContractJson.Options);
        using var parsed = JsonDocument.Parse(json);
        var withoutBody = parsed.RootElement.EnumerateObject().Where(p => p.Name != "playerBody")
            .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal);
        var restored = JsonSerializer.Deserialize<RulesetDescriptor>(JsonSerializer.Serialize(withoutBody), ContractJson.Options)!;
        Check(!restored.PlayerBody, "Omitting the additive permission no longer preserves old rulesets.");
        var withBody = legacy with { PlayerBody = true };
        Check(withBody.PlayerBody && !withBody.Characters, "Player-body permission enabled NPCs or other people.");
    }

    private static WalkSnapshot Live(ulong tick) => new()
    {
        Stamp = new(new("test-map"), 1, 1, 1, tick),
        Mode = ViewMode.Walk,
        Presence = WalkPresence.Live,
        Shape = new(.3f, 1.8f),
        PhysicalEye = new(0, 1.62f, 0),
        Posture = WalkPosture.Standing,
        Support = new(Vector3.Zero, Vector3.UnitY, SurfaceTag.Ground, null),
        Grounded = true
    };

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
