using Campus.Contracts;
using Campus.Hud;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class HudSurfaceStateTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SettingsAndSavePagePublishOnlyRealChanges()
    {
        var state = new HudSurfaceState();
        IUiSurfaceSink sink = state;
        var changes = 0;
        state.Changed += () => changes++;

        sink.ReportSettings("graphics");
        sink.ReportSettings("graphics");
        AssertThat(state.Open == UiSurface.SettingsCard && state.SettingsPage == "graphics" && changes == 1 && state.Rev == 1)
            .OverrideFailureMessage("A ready settings page was not published exactly once.").IsTrue();

        sink.ReportSettings(null);
        AssertThat(state.Open == UiSurface.None && state.SettingsPage is null && changes == 2)
            .OverrideFailureMessage("Withdrawing the settings page left the card published.").IsTrue();

        sink.Report(UiSurface.SavePage, true);
        sink.Report(UiSurface.SavePage, false);
        AssertThat(state.Open == UiSurface.None && changes == 4 && state.Rev == 4)
            .OverrideFailureMessage("The save page was not published on open and close.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SettingsCardIsReportedOnlyThroughReportSettings()
    {
        IUiSurfaceSink sink = new HudSurfaceState();
        AssertThrown(() => sink.Report(UiSurface.SettingsCard, true)).IsInstanceOf<ArgumentOutOfRangeException>();
    }
}
