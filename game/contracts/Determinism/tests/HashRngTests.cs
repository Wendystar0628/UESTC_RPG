using Campus.Tests;
using System;
using System.Text;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class HashRngTests
{
    [TestCase]
    [TestCategory(TestReason.Determinism)]
    public void HashProtocolHasFixedGoldenValues()
    {
        // Independently calculated from the documented little-endian byte protocol.
        AssertThat(StableHash.Hash32(0u)).IsEqual(0x4129D170u);
        AssertThat(StableHash.Hash32(0u, "")).IsEqual(0xFAF2D791u);
        AssertThat(StableHash.Hash32(1u, "ab", "c")).IsEqual(0xB650219Cu);
        AssertThat(StableHash.Hash32(1u, "a", "bc")).IsEqual(0x524DDD28u);
        AssertThat(StableHash.Hash32(1u, "a", "", "bc")).IsEqual(0x6ECAD833u);
        AssertThat(StableHash.Hash32(uint.MaxValue, "电光年华", "\U0001F320")).IsEqual(0xBE839C36u);
        AssertThat(StableHash.Hash32(42u, "a\0b")).IsEqual(0xCB611E69u);
    }

    [TestCase]
    [TestCategory(TestReason.Determinism)]
    public void HashUsesExactUnicodeWithoutImplicitNormalization()
    {
        AssertThat(StableHash.Hash32(42u, "e\u0301")).IsEqual(0xBDE2FFE0u);
        AssertThat(StableHash.Hash32(42u, "\u00e9")).IsEqual(0x8BBF2565u);
        AssertThrown(() => StableHash.Hash32(0u, (string[])null!)).IsInstanceOf<ArgumentNullException>();
        AssertThrown(() => StableHash.Hash32(0u, [null!])).IsInstanceOf<ArgumentNullException>();
        AssertThrown(() => StableHash.Hash32(0u, "\ud800")).IsInstanceOf<EncoderFallbackException>();
        AssertThrown(() => StableHash.Hash32(0u, "\udc00")).IsInstanceOf<EncoderFallbackException>();
    }

    [TestCase]
    [TestCategory(TestReason.Determinism)]
    public void FloatStreamsHaveFixedGoldenBits()
    {
        AssertFloatStream(0u, [0x3EC8C4A0, 0x3F59C079, 0x3F2F362E, 0x3EFF5112, 0x3F44671B, 0x3F71D2EE, 0x3F067A40, 0x3F237724]);
        AssertFloatStream(1u, [0x3EBC5A2E, 0x3DA724C0, 0x3F520EA1, 0x3F3382F3, 0x3E1982E0, 0x3F0812DD, 0x3F091C68, 0x3F4DD881]);
        AssertFloatStream(uint.MaxValue, [0x3F6B721C, 0x3F7FE8BD, 0x3E9B4550, 0x3F24A5CC, 0x3F485A6B, 0x3EB588D0, 0x3F210FDE, 0x3E929332]);
    }

    [TestCase]
    [TestCategory(TestReason.Determinism)]
    public void IntegerStreamsIncludeRejectionSamplingGoldenValues()
    {
        AssertIntegerStream(0u, 10, [8, 6, 6, 0, 3, 2, 1, 5]);
        // This bound rejects two of the first ten raw values, detecting modulo-only implementations.
        AssertIntegerStream(1u, 1073741825, [506271601, 302948858, 864219959, 135454062, 152856750, 232292774, 575857852, 589788286]);
        AssertIntegerStream(uint.MaxValue, int.MaxValue, [1802640523, 2145959221, 1302505678, 614845574, 1213885416, 1522821371, 554688045, 1229560074]);
    }

    [TestCase]
    [TestCategory(TestReason.Determinism)]
    public void RangesAndStateCopyAreStable()
    {
        Rng zero = default;
        Rng seededZero = new(0u);
        for (int i = 0; i < 100; i++)
        {
            AssertThat(zero.NextFloat()).IsEqual(seededZero.NextFloat());
        }

        Rng copied = zero;
        for (int i = 0; i < 100; i++)
        {
            AssertThat(zero.NextInt(1009)).IsEqual(copied.NextInt(1009));
        }

        Rng rng = new(0xE13579BDu);
        foreach (int bound in new[] { 1, 2, 3, 10, 127, 1073741825, int.MaxValue })
        {
            for (int i = 0; i < TestSamples.PerProperty; i++)
            {
                int integer = rng.NextInt(bound);
                float fraction = rng.NextFloat();
                AssertThat(integer >= 0 && integer < bound).OverrideFailureMessage("NextInt escaped its half-open range.").IsTrue();
                AssertThat(fraction >= 0f && fraction < 1f).OverrideFailureMessage("NextFloat escaped its half-open range.").IsTrue();
            }
        }
    }

    [TestCase]
    [TestCategory(TestReason.Determinism)]
    public void InvalidBoundsDoNotAdvanceTheStream()
    {
        Rng rng = new(123u);
        Rng expected = rng;
        AssertThrown(() => rng.NextInt(0)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => rng.NextInt(-1)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThat(rng.NextFloat()).IsEqual(expected.NextFloat());
    }

    private static void AssertFloatStream(uint seed, int[] expected)
    {
        Rng rng = new(seed);
        foreach (int bits in expected)
        {
            AssertThat(BitConverter.SingleToInt32Bits(rng.NextFloat())).IsEqual(bits);
        }
    }

    private static void AssertIntegerStream(uint seed, int max, int[] expected)
    {
        Rng rng = new(seed);
        foreach (int value in expected)
        {
            AssertThat(rng.NextInt(max)).IsEqual(value);
        }
    }
}
