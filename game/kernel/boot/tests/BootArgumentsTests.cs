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
            ["--slot=1", "--clock=6"], ["--pose=V1", "--focus=entity"],
            ["--title=0", "--pose=T0"], ["--quality-set=not_a_knob=1"], ["--frames=0"], ["--shots=../escape", "--out=C:\\shots"],
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
    public void WithoutExplicitlyRejectsUnsupportedModuleFiltering()
    {
        foreach (var development in new[] { true, false })
        {
            ArgumentException? failure = null;
            try { BootArguments.Parse(["--without=WP-18,WP-19"], development); }
            catch (ArgumentException error) { failure = error; }
            if (failure is null)
                throw new InvalidOperationException("--without accepted unsupported filtering before boot.");
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
        if (!BootArguments.Parse(["--bake=all", "--map=*"], true).CpuBakeMode)
            throw new InvalidOperationException("CPU baking did not select its command path.");
        var rejected = false;
        try { BootArguments.Parse(["--bake=all", "--frames=30"], true); }
        catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("A bake command silently combined with a render command.");
    }
}
