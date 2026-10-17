using Campus.Kernel.Boot;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class BootArgumentsTests
{
    [TestCase]
    public void CaptureDefaultsRemainSeparateFromExplicitArguments()
    {
        var options = BootArguments.Parse(["--frames=30"], true);
        if (options.Value("clock") != "17.5" || options.Has("clock") || options.ExplicitArguments.Count != 1)
            throw new InvalidOperationException("Implicit defaults became explicit inputs.");
    }

    [TestCase]
    public void CaptureDefaultsNeverOverrideLoadedWorld()
    {
        var options = BootArguments.Parse(["--slot=1", "--frames=120"], true);
        if (options.Values.ContainsKey("clock") || options.Values.ContainsKey("pose") || options.Value("save") != "off" || options.Title)
            throw new InvalidOperationException("Capture defaults replaced a saved world.");
    }

    [TestCase]
    public void BadOrConflictingArgumentsFailBeforeStartingServices()
    {
        string[][] invalid = [["--unknown=1"], ["--flow=2"], ["--clock=NaN"], ["--clock=24"], ["--anim=480"],
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
    public void WithoutExplicitlyRejectsUnsupportedModuleFiltering()
    {
        foreach (var development in new[] { true, false })
        {
            ArgumentException? failure = null;
            try { BootArguments.Parse(["--without=WP-18,WP-19"], development); }
            catch (ArgumentException error) { failure = error; }
            if (failure is null || !failure.Message.Contains("NotSupported: --without", StringComparison.Ordinal) ||
                !failure.Message.Contains("no modules have been excluded", StringComparison.Ordinal))
                throw new InvalidOperationException("--without must clearly reject unsupported filtering before boot.");
        }
    }

    [TestCase]
    public void NormalAndMainlineBootKeepTitle()
    {
        var normal = BootArguments.Parse([], true);
        var mainline = BootArguments.Parse(["--gate=mainline"], true);
        if (!normal.Title || normal.Value("audio") != "on" || !mainline.Title || mainline.Value("audio") != "off")
            throw new InvalidOperationException("Normal or mainline startup defaults are incorrect.");
    }

    [TestCase]
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
