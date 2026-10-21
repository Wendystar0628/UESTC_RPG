using Campus.Kernel.Boot;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class BootArgumentsTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CaptureDefaultsRemainSeparateFromExplicitArguments()
    {
        var options = BootArguments.Parse(["--frames=30"], true);
        if (options.World.Clock != 17.5 || options.World.ExplicitFlow || options.World.ExplicitViewMode ||
            options.World.ViewMode != ViewMode.Fly || options.Ui.Visible is not null || options.Ui.Motion is not null ||
            options.Audio.Enabled || !options.Bake.Background)
            throw new InvalidOperationException("Implicit defaults became explicit inputs.");
        var titleUi = BootArguments.Parse(["--title=1", "--shots=ui", "--out=C:\\shots"], true);
        if (!titleUi.CaptureMode || !titleUi.Title || titleUi.World.Pose is not null ||
            !titleUi.Capture.Shots.SequenceEqual(["ui"]))
            throw new InvalidOperationException("A static title UI capture acquired a world destination.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CaptureDefaultsNeverOverrideLoadedWorld()
    {
        var options = BootArguments.Parse(["--slot=1", "--frames=120"], true);
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
            try { BootArguments.Parse(args, true); }
            catch (ArgumentException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Invalid arguments were accepted: " + string.Join(' ', args));
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MovieEventsKeepTheirFrameOrderAndAnimationRunning()
    {
        var options = BootArguments.Parse(["--shots=P00", "--out=C:\\shots", "--movie-frames=180",
            "--movie-events=60:season=winter;90:season=spring,clock=8,weather=clear,intensity=0.5"], true);
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
                    "--movie-events=" + events], true);
            }
            catch (ArgumentException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Invalid movie event frames were accepted: " + events);
        }
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public void NormalBootKeepsTitle()
    {
        var normal = BootArguments.Parse([], true);
        if (!normal.Title || !normal.Audio.Enabled)
            throw new InvalidOperationException("Normal startup defaults are incorrect.");
        var inspect = BootArguments.Parse(["--inspect=palette"], true);
        if (!inspect.Automated || inspect.CaptureMode)
            throw new InvalidOperationException("Inspect must automate camera input without enabling capture defaults.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CpuBakeHasOneExecutionOwner()
    {
        var bake = BootArguments.Parse(["--bake=all", "--map=*"], true);
        var registry = BootArguments.Parse(["--gate=registry", "--map=*"], true);
        if (!bake.CpuBakeMode || !bake.AllMaps || bake.Flow.Map is not null ||
            !registry.AllMaps || registry.Flow.Map is not null)
            throw new InvalidOperationException("All-map commands leaked a selector into a map identity.");
        var single = BootArguments.Parse(["--bake=map", "--map=fixture"], true);
        if (single.AllMaps || single.Flow.Map != new MapId("fixture"))
            throw new InvalidOperationException("A concrete map command lost its identity.");
        foreach (var args in new[] { new[] { "--map=*" }, new[] { "--gate=save-close", "--map=*" } })
        {
            var invalidSelection = false;
            try { BootArguments.Parse(args, true); }
            catch (ArgumentException) { invalidSelection = true; }
            if (!invalidSelection) throw new InvalidOperationException("World entry accepted an all-map command selector.");
        }
        var rejected = false;
        try { BootArguments.Parse(["--bake=all", "--frames=30"], true); }
        catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("A bake command silently combined with a render command.");
    }
}
