using Campus.Contracts;
using Campus.Style.Materials;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class MatTableTests
{
    private static readonly ToonSpec Plain = new(new("wall.lit"), new("wall.shade"));

    private static int Texel(byte[] bytes, int id, int row, int channel) => bytes[(row * MatTable.Width + id) * 4 + channel];

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EscalatorSurvivesMaterialPackingWithoutChangingNormalOrMetalResponse()
    {
        var plain = new byte[MatTable.ByteCount];
        var moving = new byte[MatTable.ByteCount];
        var metal = Plain with { Normal = ToonNormal.AnalyticCylinder, Fx = ToonFx.AnisoBand | ToonFx.Rim | ToonFx.BevelLift };
        MatTable.WriteRows(plain, 9, metal);
        MatTable.WriteRows(moving, 9, metal with { Fx = metal.Fx | ToonFx.Escalator });
        for (var row = 0; row < MatTable.Height; row++)
            for (var channel = 0; channel < 4; channel++)
            {
                var expected = Texel(plain, 9, row, channel);
                if (row == 3 && channel == 0) expected |= (int)ToonFx.Escalator;
                AssertThat(Texel(moving, 9, row, channel) == expected).IsTrue();
            }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EmissiveChannelIsWrittenAsSlotPlusOneInRowTwoAlpha()
    {
        var bytes = new byte[MatTable.ByteCount];
        MatTable.WriteRows(bytes, 5, Plain with { Emissive = LightChannelCatalog.Id(MatTable.LastEmissiveSlot) });
        MatTable.WriteRows(bytes, 6, Plain with { Emissive = LightChannelCatalog.Id(0) });
        MatTable.WriteRows(bytes, 7, Plain);
        AssertThat(Texel(bytes, 5, 2, 3) == MatTable.LastEmissiveSlot + 1).OverrideFailureMessage("The emissive channel must be stored as its slot plus one.").IsTrue();
        AssertThat(Texel(bytes, 6, 2, 3) == 1).OverrideFailureMessage("Slot 0 must stay distinguishable from 'no emissive'.").IsTrue();
        AssertThat(Texel(bytes, 7, 2, 3) == 0).OverrideFailureMessage("A material without Emissive stores 0.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EmissiveLeavesTheOtherRowTwoBytesAlone()
    {
        var plain = new byte[MatTable.ByteCount];
        var lit = new byte[MatTable.ByteCount];
        var spec = Plain with { Fx = ToonFx.Rim, Translucency = 0.3f };
        MatTable.WriteRows(plain, 9, spec);
        MatTable.WriteRows(lit, 9, spec with { Emissive = LightChannelCatalog.Id(MatTable.LastEmissiveSlot) });
        for (var row = 0; row < MatTable.Height; row++)
            for (var channel = 0; channel < 4; channel++)
                if (row != 2 || channel != 3)
                    AssertThat(Texel(plain, 9, row, channel) == Texel(lit, 9, row, channel)).OverrideFailureMessage($"Row {row} channel {channel} changed with Emissive.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void UnknownAndSystemChannelsAreRejected()
    {
        foreach (var bad in new[] { new ChannelId("missing-fixture"), LightChannelCatalog.Id(MatTable.LastEmissiveSlot + 1), LightChannelCatalog.Id(LightChannelCatalog.Count - 1) })
        {
            var threw = false;
            try { MatTable.WriteRows(new byte[MatTable.ByteCount], 3, Plain with { Emissive = bad }); }
            catch (ArgumentException) { threw = true; }
            AssertThat(threw).OverrideFailureMessage($"Emissive channel '{bad.Value}' must be rejected.").IsTrue();
        }
        AssertThat(MatTable.EmissiveByte(LightChannelCatalog.Id(MatTable.LastEmissiveSlot)) == MatTable.LastEmissiveSlot + 1).OverrideFailureMessage("The last light-level slot is accepted.").IsTrue();
    }
}
