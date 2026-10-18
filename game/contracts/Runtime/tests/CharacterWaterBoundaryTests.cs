using System.Numerics;
using System.Text.Json;
using Campus.Contracts.Audio;
using GdUnit4;
using static GdUnit4.Assertions;

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
        AssertThat(current.CanInterpolateFrom(before) && current.Perceptible).OverrideFailureMessage("Adjacent live physical samples must remain usable.").IsTrue();
        AssertThat(!(current with { Presence = WalkPresence.Pending }).Perceptible).OverrideFailureMessage("An unvalidated landing stimulated animals.").IsTrue();
        AssertThat(!(current with { Mode = ViewMode.Fly }).Perceptible).OverrideFailureMessage("A flying camera was treated as a body.").IsTrue();
        AssertThat(!(current with { Hold = WalkHold.WaterNotReady }).CanInterpolateFrom(before)).OverrideFailureMessage("Missing water data allowed interpolation.").IsTrue();
        AssertThat(!(current with { Stamp = current.Stamp with { Continuity = 2 } }).CanInterpolateFrom(before)).OverrideFailureMessage("Warp swept from the old position.").IsTrue();
        AssertThat(!(current with { Stamp = current.Stamp with { WorldInstance = 2 } }).CanInterpolateFrom(before)).OverrideFailureMessage("Restore blended an old world body.").IsTrue();
        AssertThat(!(current with { Stamp = current.Stamp with { Tick = 12 } }).CanInterpolateFrom(before)).OverrideFailureMessage("A missing physics tick was invented.").IsTrue();
    }

    [TestCase]
    public void MissingWaterNeverBecomesReadyAndSwimmingDepthIsNotFootImmersion()
    {
        WaterQueryResult missing = default;
        AssertThat(missing.Status == WaterQueryStatus.NotReady && missing.Sample is null).OverrideFailureMessage("Default query must fail closed.").IsTrue();
        var sample = new WaterSample(new(0, -.75f, 0), new("test:water"), "volume", "swim-area", null,
            0, -1.2f, WaterBottomKind.Terrain, -2, 1, WaterAccess.SurfaceSwim);
        var hit = new WaterQueryResult(WaterQueryStatus.Ready, sample, .5f);
        AssertThat(MathF.Abs(hit.Sample!.Value.ColumnDepth - 1.2f) < .00001f && hit.Sample.Value.DepthAtPoint == .75f).OverrideFailureMessage("Column depth and the swimming reference-point immersion were conflated.").IsTrue();
        AssertThrown(() => new WaterQueryResult(WaterQueryStatus.Ready)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new WaterQueryResult(WaterQueryStatus.NotReady, sample)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new WaterQueryResult(WaterQueryStatus.Ready, sample with { SwimAreaId = null })).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new WaterQueryResult(WaterQueryStatus.Ready, sample with { SurfaceY = float.NaN })).IsInstanceOf<ArgumentException>();
        AssertThrown(() => new WaterQueryResult(WaterQueryStatus.Ready, sample, 1.01f)).IsInstanceOf<ArgumentOutOfRangeException>();
    }

    [TestCase]
    public void VisibleTreadsRoundTripWithoutInventingMetadata()
    {
        var spec = new StairSpec(.15f, .3f, 10);
        var ramp = new RampCollider(new(0, -.2f, 0), new(1.4f, -.2f, 0),
            new(1.4f, 1.5f, 3), new(0, 1.5f, 3), .15f, SurfaceTag.Stair)
        { Foot = FootMat.Concrete, Stair = spec };
        AssertThat(ramp.Treads is null).OverrideFailureMessage("A ramp acquired invented stair geometry.").IsTrue();
        string oldJson = JsonSerializer.Serialize<Collider>(ramp, ContractJson.Options);
        AssertThat(!oldJson.Contains("treads", StringComparison.Ordinal)).OverrideFailureMessage("Null tread metadata changed old collider JSON.").IsTrue();
        var frame = new StairFrame(new(0, .15f, .1f), Vector3.UnitZ, Vector3.UnitY, 1.4f);
        frame.Validate(spec);
        var enriched = ramp with { Treads = frame };
        var restored = (RampCollider)JsonSerializer.Deserialize<Collider>(
            JsonSerializer.Serialize<Collider>(enriched, ContractJson.Options), ContractJson.Options)!;
        AssertThat(restored.Treads == frame && restored.P0 == ramp.P0).OverrideFailureMessage("Visible tread origin was lost or replaced the buried collision ramp.").IsTrue();
        AssertThrown(() => (frame with { Run = Vector3.UnitY }).Validate(spec)).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void LegacyRulesetConstructorAndJsonKeepBodyPermissionOffUnlessExplicit()
    {
        var legacy = new RulesetDescriptor(new("legacy"), [ViewMode.Fly, ViewMode.Walk],
            WorldControlMask.None, "all-unlocked", [], []);
        AssertThat(!legacy.PlayerBody && !legacy.Characters).OverrideFailureMessage("A legacy ruleset unexpectedly enabled a character.").IsTrue();
        string json = JsonSerializer.Serialize(legacy, ContractJson.Options);
        using var parsed = JsonDocument.Parse(json);
        var withoutBody = parsed.RootElement.EnumerateObject().Where(p => p.Name != "playerBody")
            .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal);
        var restored = JsonSerializer.Deserialize<RulesetDescriptor>(JsonSerializer.Serialize(withoutBody), ContractJson.Options)!;
        AssertThat(!restored.PlayerBody).OverrideFailureMessage("Omitting the additive permission no longer preserves old rulesets.").IsTrue();
        var withBody = legacy with { PlayerBody = true };
        AssertThat(withBody.PlayerBody && !withBody.Characters).OverrideFailureMessage("Player-body permission enabled NPCs or other people.").IsTrue();
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
}
