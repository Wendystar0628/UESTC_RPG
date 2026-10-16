using Campus.Contracts;
using Campus.Style.Materials;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class MatTableTests
{
    private static readonly ToonSpec Plain = new(new("wall.lit"), new("wall.shade"));

    private static int Texel(byte[] bytes, int id, int row, int channel) => bytes[(row * MatTable.Width + id) * 4 + channel];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [TestCase]
    public void EmissiveChannelIsWrittenAsSlotPlusOneInRowTwoAlpha()
    {
        var bytes = new byte[MatTable.ByteCount];
        MatTable.WriteRows(bytes, 5, Plain with { Emissive = new ChannelId("sign.signal") });
        MatTable.WriteRows(bytes, 6, Plain with { Emissive = LightChannelCatalog.Id(0) });
        MatTable.WriteRows(bytes, 7, Plain);
        Require(Texel(bytes, 5, 2, 3) == 23, "sign.signal is slot 22, stored as 23.");
        Require(Texel(bytes, 6, 2, 3) == 1, "Slot 0 must stay distinguishable from 'no emissive'.");
        Require(Texel(bytes, 7, 2, 3) == 0, "A material without Emissive stores 0.");
    }

    [TestCase]
    public void EmissiveLeavesTheOtherRowTwoBytesAlone()
    {
        var plain = new byte[MatTable.ByteCount];
        var lit = new byte[MatTable.ByteCount];
        var spec = Plain with { Fx = ToonFx.Rim, Translucency = 0.3f };
        MatTable.WriteRows(plain, 9, spec);
        MatTable.WriteRows(lit, 9, spec with { Emissive = new ChannelId("sign.shop") });
        for (var row = 0; row < MatTable.Height; row++)
            for (var channel = 0; channel < 4; channel++)
                if (row != 2 || channel != 3)
                    Require(Texel(plain, 9, row, channel) == Texel(lit, 9, row, channel), $"Row {row} channel {channel} changed with Emissive.");
    }

    [TestCase]
    public void UnknownAndSystemChannelsAreRejected()
    {
        foreach (var bad in new[] { new ChannelId("sign.nope"), new ChannelId("sys.cue"), new ChannelId("sys.poolGain") })
        {
            var threw = false;
            try { MatTable.WriteRows(new byte[MatTable.ByteCount], 3, Plain with { Emissive = bad }); }
            catch (ArgumentException) { threw = true; }
            Require(threw, $"Emissive channel '{bad.Value}' must be rejected.");
        }
        Require(MatTable.EmissiveByte(LightChannelCatalog.Id(MatTable.LastEmissiveSlot)) == MatTable.LastEmissiveSlot + 1,
            "The last light-level slot is accepted.");
    }
}
