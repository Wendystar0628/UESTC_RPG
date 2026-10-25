using Campus.Kernel.Boot;
using Campus.Contracts;
using Campus.Contracts.Startup;
using System.Text.Json;
using System.Text.Json.Nodes;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class BootArgumentsTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CollectionReadsTheFrozenRequestAndRejectsMissingOrInvalidValues()
    {
        var root = Path.Combine(Path.GetTempPath(), "campus-collection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "collection.json");
        var request = new PerfCollectionOptions
        {
            SceneId = "fixture-scene", SampleSegmentId = "sample-fixture", FunctionalSmoke = true, SaveAfterSample = true, StableFrames = 2,
            RealtimeSeconds = 1, SampleSeconds = 2, OtherGpuFraction = .2, MaximumFactAgeSeconds = 3,
            RoutePointIds = ["road:fixture/0"], TravelPrefetch = true
        };
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(request, ContractJson.Options));
            var options = BootArguments.Parse(["--perf-out=" + root, "--perf-collection=" + path]);
            AssertThat(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(options.PerfCollection, ContractJson.Options),
                JsonSerializer.SerializeToNode(request, ContractJson.Options))).IsTrue();
            AssertThat(options.Mode == BootRunMode.Interactive && options.Title).IsTrue();
            File.WriteAllText(path, JsonSerializer.Serialize(request with { FunctionalSmoke = false }, ContractJson.Options));
            AssertThat(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(
                BootArguments.Parse(["--perf-out=" + root, "--perf-collection=" + path]).PerfCollection, ContractJson.Options),
                JsonSerializer.SerializeToNode(request with { FunctionalSmoke = false }, ContractJson.Options))).IsTrue();
            var missingFlag = JsonSerializer.SerializeToNode(request, ContractJson.Options)!.AsObject();
            missingFlag.Remove("functionalSmoke");
            File.WriteAllText(path, missingFlag.ToJsonString());
            JsonException? missingPurpose = null;
            try { BootArguments.Parse(["--perf-out=" + root, "--perf-collection=" + path]); }
            catch (JsonException error) { missingPurpose = error; }
            AssertThat(missingPurpose is not null).IsTrue();
            File.WriteAllText(path, JsonSerializer.Serialize(request, ContractJson.Options));
            foreach (var invalid in new[]
            {
                new[] { "--perf-collection=" + path },
                new[] { "--perf-out=" + root, "--perf-collection=relative.json" },
                new[] { "--perf-out=" + root, "--perf-collection=" + path, "--bake=all" },
                new[] { "--perf-out=" + root, "--perf-collection=" + path, "--frames=1" }
            })
            {
                ArgumentException? failure = null;
                try { BootArguments.Parse(invalid); }
                catch (ArgumentException error) { failure = error; }
                AssertThat(failure is not null).IsTrue();
            }
            File.WriteAllText(path, JsonSerializer.Serialize(request with { SampleSeconds = -1 }, ContractJson.Options));
            ArgumentException? rejected = null;
            try { BootArguments.Parse(["--perf-out=" + root, "--perf-collection=" + path]); }
            catch (ArgumentException error) { rejected = error; }
            AssertThat(rejected is not null).IsTrue();
            File.WriteAllText(path, "{}");
            JsonException? missing = null;
            try { BootArguments.Parse(["--perf-out=" + root, "--perf-collection=" + path]); }
            catch (JsonException error) { missing = error; }
            AssertThat(missing is not null).IsTrue();
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PerformanceOutputIsAnIndependentExplicitRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "campus-perf");
        var options = BootArguments.Parse(["--perf-out=" + root]);
        AssertThat(options.PerfOutputDirectory == root && options.StartupRoute is null &&
            options.Mode == BootRunMode.Interactive && !options.Automated && options.Title).IsTrue();
        var routed = BootArguments.Parse(["--perf-out=" + root, "--startup-scenario=route", "--route-start=route_point",
            "--route-mode=walk", "--route-seconds=5"]);
        AssertThat(routed.PerfOutputDirectory == root && routed.StartupRoute is not null).IsTrue();
        foreach (var invalid in new[] { "--perf-out=", "--perf-out=relative/perf" })
        {
            ArgumentException? failure = null;
            try { BootArguments.Parse([invalid]); }
            catch (ArgumentException error) { failure = error; }
            AssertThat(failure is not null).IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RoutesRetainTypedChoicesWithoutCaptureDefaults()
    {
        foreach (var (text, mode) in new[] { ("walk", ViewMode.Walk), ("fly", ViewMode.Fly) })
        {
            var options = BootArguments.Parse(["--startup-scenario=route", "--route-start=route_point",
                "--route-mode=" + text, "--route-seconds=5.5"]);
            AssertThat(options.StartupScenario == StartupScenario.Route &&
                options.StartupRoute == new StartupRouteOptions("route_point", mode, 5.5)).IsTrue();
            AssertThat(options.Mode == BootRunMode.Interactive && options.Title && !options.Automated &&
                options.Flow.Spawn is null && options.World.Pose is null && options.World.ViewMode is null).IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RouteParametersRequireTheirScenarioAndValidCompleteChoices()
    {
        string[] valid = ["--startup-scenario=route", "--route-start=route_point", "--route-mode=walk", "--route-seconds=5"];
        var invalid = new List<string[]>
        {
            new[] { "--route-start=route_point" },
            new[] { "--startup-scenario=playing", "--route-mode=walk" }
        };
        for (var index = 1; index < valid.Length; index++) invalid.Add(valid.Where((_, i) => i != index).ToArray());
        foreach (var seconds in new[] { "0", "-1", "NaN", "Infinity" })
            invalid.Add([.. valid[..3], "--route-seconds=" + seconds]);
        invalid.Add([valid[0], "--route-start=route point", valid[2], valid[3]]);
        invalid.Add([valid[0], valid[1], "--route-mode=invalid", valid[3]]);
        foreach (var conflict in new[] { "--mode=walk", "--spawn=route_point", "--pose=route_point", "--title=0", "--frames=5" })
            invalid.Add([.. valid, conflict]);
        foreach (var args in invalid)
        {
            ArgumentException? rejected = null;
            try { BootArguments.Parse(args); }
            catch (ArgumentException error) { rejected = error; }
            AssertThat(rejected is not null).IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CaptureDefaultsRemainSeparateFromExplicitArguments()
    {
        var options = BootArguments.Parse(["--frames=30"]);
        if (options.World.Clock != 17.5 ||
            options.World.ViewMode != ViewMode.Fly || options.Ui.Visible is not null || options.Ui.Motion is not null ||
            options.Audio.Enabled || !options.Bake.Background)
            throw new InvalidOperationException("Implicit defaults became explicit inputs.");
        var titleUi = BootArguments.Parse(["--title=1", "--shots=ui", "--out=C:\\shots"]);
        if (!titleUi.CaptureMode || !titleUi.Title || titleUi.World.Pose is not null ||
            !titleUi.Capture.Shots.SequenceEqual(["ui"]))
            throw new InvalidOperationException("A static title UI capture acquired a world destination.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CaptureDefaultsNeverOverrideLoadedWorld()
    {
        var options = BootArguments.Parse(["--slot=1", "--frames=120"]);
        if (options.World.Clock is not null || options.World.Pose is not null || options.Save.Enabled || options.Title)
            throw new InvalidOperationException("Capture defaults replaced a saved world.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BadOrConflictingArgumentsFailBeforeStartingServices()
    {
        string[][] invalid = [["--unknown=1"], ["--weather-queue=rain:3"], ["--flow=2"], ["--clock=NaN"], ["--clock=24"], ["--anim=480"],
            ["--slot=1", "--weather-seed=1"], ["--load=C:\\test.sav", "--map=map"], ["--load=C:\\test.sav", "--clock=6"],
            ["--slot=1", "--clock=6"],
            ["--title=2"], ["--quality-set=not_a_knob=1"], ["--frames=0"], ["--shots=../escape", "--out=C:\\shots"],
            ["--clock=1", "--clock=2"], ["--quality"]];
        foreach (var args in invalid)
        {
            var rejected = false;
            try { BootArguments.Parse(args); }
            catch (ArgumentException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Invalid arguments were accepted: " + string.Join(' ', args));
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MovieEventsKeepTheirFrameOrderAndAnimationRunning()
    {
        var options = BootArguments.Parse(["--shots=P00", "--out=C:\\shots", "--movie-frames=180",
            "--movie-events=60:season=winter;90:season=spring,clock=8,weather=clear,intensity=0.5"]);
        if (!options.CaptureMode || options.FreezeAnimation || options.Capture.MovieFrames != 180 ||
            options.Capture.MovieEvents.Count != 2)
            throw new InvalidOperationException("Movie capture did not retain its frame count and live animation.");
        var first = options.Capture.MovieEvents[0];
        var second = options.Capture.MovieEvents[1];
        if (first.Frame != 60 || first.Season != Season.Winter || first.Clock is not null || first.Weather is not null ||
            second.Frame != 90 || second.Season != Season.Spring || second.Clock != 8 ||
            second.Weather != WeatherKind.Clear || second.Intensity != .5f || second.AutomaticWeather)
            throw new InvalidOperationException("Movie events lost their frame order or typed world changes.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MovieEventsRejectOutOfRangeOrRepeatedFrames()
    {
        foreach (var events in new[] { "180:season=winter", "60:season=winter;60:season=spring" })
        {
            var rejected = false;
            try
            {
                BootArguments.Parse(["--shots=P00", "--out=C:\\shots", "--movie-frames=180",
                    "--movie-events=" + events]);
            }
            catch (ArgumentException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Invalid movie event frames were accepted: " + events);
        }
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public void NormalBootKeepsTitle()
    {
        var normal = BootArguments.Parse([]);
        if (!normal.Title || !normal.Audio.Enabled)
            throw new InvalidOperationException("Normal startup defaults are incorrect.");
        var inspect = BootArguments.Parse(["--inspect=palette"]);
        if (!inspect.Automated || inspect.CaptureMode)
            throw new InvalidOperationException("Inspect must automate camera input without enabling capture defaults.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CpuBakeHasOneExecutionOwner()
    {
        var bake = BootArguments.Parse(["--bake=all", "--map=*"]);
        var registry = BootArguments.Parse(["--gate=registry", "--map=*"]);
        if (!bake.CpuBakeMode || !bake.AllMaps || bake.Flow.Map is not null ||
            !registry.AllMaps || registry.Flow.Map is not null)
            throw new InvalidOperationException("All-map commands leaked a selector into a map identity.");
        var single = BootArguments.Parse(["--bake=map", "--map=fixture"]);
        if (single.AllMaps || single.Flow.Map != new MapId("fixture"))
            throw new InvalidOperationException("A concrete map command lost its identity.");
        foreach (var args in new[] { new[] { "--map=*" }, new[] { "--gate=save-close", "--map=*" } })
        {
            var invalidSelection = false;
            try { BootArguments.Parse(args); }
            catch (ArgumentException) { invalidSelection = true; }
            if (!invalidSelection) throw new InvalidOperationException("World entry accepted an all-map command selector.");
        }
        var rejected = false;
        try { BootArguments.Parse(["--bake=all", "--frames=30"]); }
        catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("A bake command silently combined with a render command.");
    }
}
