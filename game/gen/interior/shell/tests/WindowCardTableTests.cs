using System;
using System.Linq;
using Campus.Gen.Interior.Shell.Cards;
using GdUnit4;

namespace Campus.Gen.Interior.Shell.Tests;

[TestSuite]
public sealed class WindowCardTableTests
{
    [TestCase]
    public void EveryVfbCardTypeAndTheLobbyHaveARow()
    {
        var data = WindowCardTable.Pack();
        True(data.Length == WindowCardTable.Width * WindowCardTable.Rows * 4, "Table size changed.");
        for (var kind = 1; kind <= (int)WindowCardKind.Lobby; kind++)
            True(Texel(data, kind, 5)[3] == WindowCardTable.Tag, $"Card {(WindowCardKind)kind} lacks its tag.");
        True(Texel(data, 0, 5)[3] == 0f, "Row 0 (unspecified glass) must stay untagged.");
        for (var row = (int)WindowCardKind.Lobby + 1; row < WindowCardTable.Rows; row++)
            True(Texel(data, row, 5)[3] == 0f, $"Unused row {row} carries a tag.");
    }

    [TestCase]
    public void CardKindsMatchTheContractWindowTypeByte()
    {
        // VF-B (GeometryBundle.Reserved on glass): 1–15 are W-CLS … W-MECH in this order.
        string[] contract = ["W-CLS", "W-LEC", "W-COR", "W-STR", "W-OFF", "W-LAB", "W-CLEAN", "W-DORM", "W-WC", "W-CAN",
            "W-SHOP", "W-MED", "W-GYM", "W-POOL", "W-MECH"];
        WindowCardKind[] kinds = [WindowCardKind.Classroom, WindowCardKind.Lecture, WindowCardKind.Corridor, WindowCardKind.Stair,
            WindowCardKind.Office, WindowCardKind.Lab, WindowCardKind.Clean, WindowCardKind.Dorm, WindowCardKind.Toilet,
            WindowCardKind.Canteen, WindowCardKind.Shop, WindowCardKind.Medical, WindowCardKind.Gym, WindowCardKind.Pool,
            WindowCardKind.Mechanical];
        True(contract.Length == kinds.Length, "Card list length differs from the contract.");
        for (var i = 0; i < kinds.Length; i++)
            True((int)kinds[i] == i + 1 && WindowCardCatalog.Get(kinds[i]).Kind == kinds[i], $"{contract[i]} is not byte {i + 1}.");
    }

    [TestCase]
    public void DepthWeightsAreCumulativeAndScreensStayInRange()
    {
        var data = WindowCardTable.Pack();
        foreach (var card in WindowCardCatalog.All)
        {
            var x0 = Texel(data, (int)card.Kind, 0);
            True(x0[0] >= 0f && x0[0] <= x0[1] && x0[1] <= 1f + 1e-6f, $"{card.Kind}: depth thresholds are not cumulative.");
            var x2 = Texel(data, (int)card.Kind, 2);
            True(x2[2] <= x2[3] && x2[3] <= 1f, $"{card.Kind}: coverage range broken.");
            True(card.Screen != CardScreen.None || x2[1] == 0f, $"{card.Kind}: chance without a screen.");
        }
    }

    [TestCase]
    public void LayersFitTheEightyFourLayerArray()
    {
        var used = WindowCardCatalog.All.Sum(c => c.LayerCount);
        var top = WindowCardCatalog.All.Max(c => c.LayerBase + c.LayerCount);
        True(used == top && top <= 84, $"Card layers overlap or exceed 84: used {used}, top {top}.");
        var ordered = WindowCardCatalog.All.OrderBy(c => c.LayerBase).ToArray();
        for (var i = 1; i < ordered.Length; i++)
            True(ordered[i].LayerBase == ordered[i - 1].LayerBase + ordered[i - 1].LayerCount, "Card layers are not contiguous.");
    }

    [TestCase]
    public void PackingIsDeterministicAndRejectsBadCards()
    {
        True(WindowCardTable.Pack().SequenceEqual(WindowCardTable.Pack()), "Packing is not deterministic.");
        var good = WindowCardCatalog.Get(WindowCardKind.Office);
        Throws(() => WindowCardTable.Pack([good, good]));
        Throws(() => WindowCardTable.Pack([good with { CoverMin = 0.8f, CoverMax = 0.2f }]));
        Throws(() => WindowCardTable.Pack([good with { Depth3 = 0f, Depth45 = 0f, Depth6 = 0f }]));
        Throws(() => WindowCardTable.Pack([new WindowCardSpec(WindowCardKind.None) { Depth3 = 1f }]));
        Throws(() => WindowCardTable.Pack([good with { Screen = CardScreen.None }]));
        Throws(() => WindowCardCatalog.Get(WindowCardKind.None));
    }

    private static float[] Texel(float[] data, int row, int x)
    {
        var i = (row * WindowCardTable.Width + x) * 4;
        return [data[i], data[i + 1], data[i + 2], data[i + 3]];
    }

    private static void True(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static void Throws(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("Expected an ArgumentException.");
    }
}
