using Campus.Contracts;
using Campus.Systems.Nightlight.Windows;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class WindowOverridesTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void OverridesStayWithThePreparedNumbering()
    {
        var grid = new WindowAllocation(new FeatureId("test:window"), "facade", 0, 2, 2);
        var first = new WindowNumberingSnapshot(new MapId("test"), new string('a', 64), [grid]);
        var overrides = new WindowOverrides();
        overrides.Bind(first);
        var id = first.Get(grid.Feature, grid.Key).IdAt(grid.Columns - 1, grid.Rows - 1);
        overrides.Set(id, WindowLight.ForceOn);
        overrides.Bind(first);
        AssertThat(ReferenceEquals(overrides.Numbering, first) && overrides.Count == 1 &&
            overrides.Data[id * 4] == (byte)WindowLight.ForceOn)
            .OverrideFailureMessage("The same prepared snapshot preserves its overrides and IDs.").IsTrue();
        AssertThrown(() => overrides.Set(first.WindowCount, WindowLight.ForceOn)).IsInstanceOf<ArgumentOutOfRangeException>();

        var next = new WindowNumberingSnapshot(first.Map, new string('b', 64), [grid with { Rows = 1 }]);
        overrides.Bind(next);
        AssertThat(ReferenceEquals(overrides.Numbering, next) && overrides.Count == 0 && overrides.Data[id * 4] == 0)
            .OverrideFailureMessage("A new allocation cannot inherit another allocation's override bytes.").IsTrue();
        overrides.Set(next.WindowCount - 1, WindowLight.ForceOff);
        overrides.Bind(null);
        AssertThat(overrides.Numbering is null && overrides.Count == 0)
            .OverrideFailureMessage("Map unloading releases the numbering and overrides.").IsTrue();
        AssertThrown(() => overrides.Set(0, WindowLight.ForceOn)).IsInstanceOf<InvalidOperationException>();
    }
}
