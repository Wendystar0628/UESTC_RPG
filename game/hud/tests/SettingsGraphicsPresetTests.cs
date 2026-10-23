using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Hud.Pages;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;
using static Campus.Tests.QualityApplicationTestFixture;

[TestSuite]
public sealed class SettingsGraphicsPresetTests
{
    // 画面页恢复默认还没应用时再选预设，恢复默认复位的独立项不能被已生效的偏好顶回去（DECISION-322、GAME-212）。
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task PresetChosenAfterPendingRestoreDefaultsKeepsDefaultIndependentKnobs()
    {
        var catalog = Catalog();
        var knob = QualityKnob.RenderScale;
        var defaults = catalog.ResolveRequested(catalog.DefaultProfile, catalog.DefaultPreset);
        var current = catalog[knob].Values is { Count: > 0 } values ? values.First(value => value != defaults[(int)knob]) : catalog[knob].Min;
        var baseline = new QualityProfile(catalog.DefaultPreset, new Dictionary<QualityKnob, float> { [knob] = current });
        var (service, _, store) = await Session("graphics-page-reset-preset", baseline, new QualityBootOptions());
        await using var serviceCleanup = service;
        using var cleanup = store;

        var reset = SettingsCardGraphics.ResetRequest(service);
        var resetPlan = service.Plan(reset);
        AssertThat(reset.EditedKnobs.Count == 0 && service.Effective.Requested(knob) != defaults[(int)knob])
            .OverrideFailureMessage("The regression needs an empty restore-defaults edit list over a non-default render scale.").IsTrue();

        var preset = QualityPreset.Balanced;
        var chosen = service.Plan(SettingsCardGraphics.PresetRequest(service, reset, resetPlan.Next, preset));
        var planned = catalog.ResolveRequested(chosen.Next, preset);
        AssertThat(chosen.Next.Preset == preset && planned[(int)knob] == defaults[(int)knob])
            .OverrideFailureMessage($"Choosing {preset} after a pending restore kept render scale {planned[(int)knob]} instead of the default {defaults[(int)knob]}.").IsTrue();
    }
}
