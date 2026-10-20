using Campus.Tests;
using System;
using System.Linq;
using Campus.Style.Shaders.Cards;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Style.Shaders.Tests;

[TestSuite]
public sealed class WindowCardTableTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EveryVfbCardTypeAndTheLobbyHaveARow()
    {
        var data = WindowCardTable.Pack();
        AssertThat(data.Length == WindowCardTable.Width * WindowCardTable.Rows * 4).OverrideFailureMessage("Table size changed.").IsTrue();
        for (var kind = 1; kind <= (int)WindowCardKind.Lobby; kind++)
            AssertThat(Texel(data, kind, 5)[3] == WindowCardTable.Tag).OverrideFailureMessage($"Card {(WindowCardKind)kind} lacks its tag.").IsTrue();
        AssertThat(Texel(data, 0, 5)[3] == 0f).OverrideFailureMessage("Row 0 (unspecified glass) must stay untagged.").IsTrue();
        for (var row = (int)WindowCardKind.Lobby + 1; row < WindowCardTable.Rows; row++)
            AssertThat(Texel(data, row, 5)[3] == 0f).OverrideFailureMessage($"Unused row {row} carries a tag.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CardKindsMatchTheContractWindowTypeByte()
    {
        // VF-B (GeometryBundle.Reserved on glass) stores the WindowCardKind byte.
        WindowCardKind[] kinds = [WindowCardKind.Classroom, WindowCardKind.Lecture, WindowCardKind.Corridor, WindowCardKind.Stair,
            WindowCardKind.Office, WindowCardKind.Lab, WindowCardKind.Clean, WindowCardKind.Dorm, WindowCardKind.Toilet,
            WindowCardKind.Canteen, WindowCardKind.Shop, WindowCardKind.Medical, WindowCardKind.Gym, WindowCardKind.Pool,
            WindowCardKind.Mechanical];
        AssertThat(WindowCardCatalog.All.Count == kinds.Length + 1).OverrideFailureMessage("Card list length differs from the contract.").IsTrue();
        for (var i = 0; i < kinds.Length; i++)
            AssertThat((int)kinds[i] == i + 1 && WindowCardCatalog.Get(kinds[i]).Kind == kinds[i]).OverrideFailureMessage($"{kinds[i]} is not byte {i + 1}.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DepthWeightsAreCumulativeAndScreensStayInRange()
    {
        var data = WindowCardTable.Pack();
        foreach (var card in WindowCardCatalog.All)
        {
            var x0 = Texel(data, (int)card.Kind, 0);
            AssertThat(x0[0] >= 0f && x0[0] <= x0[1] && x0[1] <= 1f + TestTolerance.FloatRelative).OverrideFailureMessage($"{card.Kind}: depth thresholds are not cumulative.").IsTrue();
            var x2 = Texel(data, (int)card.Kind, 2);
            AssertThat(x2[2] <= x2[3] && x2[3] <= 1f).OverrideFailureMessage($"{card.Kind}: coverage range broken.").IsTrue();
            AssertThat(card.Screen != CardScreen.None || x2[1] == 0f).OverrideFailureMessage($"{card.Kind}: chance without a screen.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LayersFitTheEightyFourLayerArray()
    {
        var used = WindowCardCatalog.All.Sum(c => c.LayerCount);
        var top = WindowCardCatalog.All.Max(c => c.LayerBase + c.LayerCount);
        AssertThat(used == top).OverrideFailureMessage($"Card layers overlap: used {used}, top {top}.").IsTrue();
        var ordered = WindowCardCatalog.All.OrderBy(c => c.LayerBase).ToArray();
        for (var i = 1; i < ordered.Length; i++)
            AssertThat(ordered[i].LayerBase == ordered[i - 1].LayerBase + ordered[i - 1].LayerCount).OverrideFailureMessage("Card layers are not contiguous.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void PackingIsDeterministicAndRejectsBadCards()
    {
        AssertThat(WindowCardTable.Pack().SequenceEqual(WindowCardTable.Pack())).OverrideFailureMessage("Packing is not deterministic.").IsTrue();
        var good = WindowCardCatalog.Get(WindowCardKind.Office);
        AssertThrown(() => WindowCardTable.Pack([good, good])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => WindowCardTable.Pack([good with { CoverMin = 0.8f, CoverMax = 0.2f }])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => WindowCardTable.Pack([good with { Depth3 = 0f, Depth45 = 0f, Depth6 = 0f }])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => WindowCardTable.Pack([new WindowCardSpec(WindowCardKind.None) { Depth3 = 1f }])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => WindowCardTable.Pack([good with { Screen = CardScreen.None }])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => WindowCardCatalog.Get(WindowCardKind.None)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => WindowCardTable.Pack([good])).IsInstanceOf<ArgumentException>();
        AssertThrown(() => WindowCardTable.Pack(WindowCardCatalog.All.Where(card => card.Kind != good.Kind).ToArray())).IsInstanceOf<ArgumentException>();
        AssertThrown(() => WindowCardTable.Pack([good with { Kind = (WindowCardKind)((int)WindowCardKind.Lobby + 1) }])).IsInstanceOf<ArgumentException>();
    }

    private static float[] Texel(float[] data, int row, int x)
    {
        var i = (row * WindowCardTable.Width + x) * 4;
        return [data[i], data[i + 1], data[i + 2], data[i + 3]];
    }
}
