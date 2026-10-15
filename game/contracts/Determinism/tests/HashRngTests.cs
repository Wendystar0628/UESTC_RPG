using System;
using System.Text;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class HashRngTests
{
    [TestCase]
    public void HashProtocolHasFixedGoldenValues()
    {
        // Independently calculated from the documented little-endian byte protocol.
        DeterminismAssert.Equal(0x4129D170u, StableHash.Hash32(0u));
        DeterminismAssert.Equal(0xFAF2D791u, StableHash.Hash32(0u, ""));
        DeterminismAssert.Equal(0xB650219Cu, StableHash.Hash32(1u, "ab", "c"));
        DeterminismAssert.Equal(0x524DDD28u, StableHash.Hash32(1u, "a", "bc"));
        DeterminismAssert.Equal(0x6ECAD833u, StableHash.Hash32(1u, "a", "", "bc"));
        DeterminismAssert.Equal(0xBE839C36u, StableHash.Hash32(uint.MaxValue, "电光年华", "\U0001F320"));
        DeterminismAssert.Equal(0xCB611E69u, StableHash.Hash32(42u, "a\0b"));
    }

    [TestCase]
    public void HashUsesExactUnicodeWithoutImplicitNormalization()
    {
        DeterminismAssert.Equal(0xBDE2FFE0u, StableHash.Hash32(42u, "e\u0301"));
        DeterminismAssert.Equal(0x8BBF2565u, StableHash.Hash32(42u, "\u00e9"));
        DeterminismAssert.Throws<ArgumentNullException>(() => StableHash.Hash32(0u, (string[])null!));
        DeterminismAssert.Throws<ArgumentNullException>(() => StableHash.Hash32(0u, [null!]));
        DeterminismAssert.Throws<EncoderFallbackException>(() => StableHash.Hash32(0u, "\ud800"));
        DeterminismAssert.Throws<EncoderFallbackException>(() => StableHash.Hash32(0u, "\udc00"));
    }

    [TestCase]
    public void FloatStreamsHaveFixedGoldenBits()
    {
        AssertFloatStream(0u, [0x3EC8C4A0, 0x3F59C079, 0x3F2F362E, 0x3EFF5112, 0x3F44671B, 0x3F71D2EE, 0x3F067A40, 0x3F237724]);
        AssertFloatStream(1u, [0x3EBC5A2E, 0x3DA724C0, 0x3F520EA1, 0x3F3382F3, 0x3E1982E0, 0x3F0812DD, 0x3F091C68, 0x3F4DD881]);
        AssertFloatStream(uint.MaxValue, [0x3F6B721C, 0x3F7FE8BD, 0x3E9B4550, 0x3F24A5CC, 0x3F485A6B, 0x3EB588D0, 0x3F210FDE, 0x3E929332]);
    }

    [TestCase]
    public void IntegerStreamsIncludeRejectionSamplingGoldenValues()
    {
        AssertIntegerStream(0u, 10, [8, 6, 6, 0, 3, 2, 1, 5]);
        // This bound rejects two of the first ten raw values, detecting modulo-only implementations.
        AssertIntegerStream(1u, 1073741825, [506271601, 302948858, 864219959, 135454062, 152856750, 232292774, 575857852, 589788286]);
        AssertIntegerStream(uint.MaxValue, int.MaxValue, [1802640523, 2145959221, 1302505678, 614845574, 1213885416, 1522821371, 554688045, 1229560074]);
    }

    [TestCase]
    public void RangesAndStateCopyAreStable()
    {
        Rng zero = default;
        Rng seededZero = new(0u);
        for (int i = 0; i < 100; i++)
        {
            DeterminismAssert.Equal(seededZero.NextFloat(), zero.NextFloat());
        }

        Rng copied = zero;
        for (int i = 0; i < 100; i++)
        {
            DeterminismAssert.Equal(copied.NextInt(1009), zero.NextInt(1009));
        }

        Rng rng = new(0xE13579BDu);
        foreach (int bound in new[] { 1, 2, 3, 10, 127, 1073741825, int.MaxValue })
        {
            for (int i = 0; i < 4096; i++)
            {
                int integer = rng.NextInt(bound);
                float fraction = rng.NextFloat();
                DeterminismAssert.True(integer >= 0 && integer < bound, "NextInt escaped its half-open range.");
                DeterminismAssert.True(fraction >= 0f && fraction < 1f, "NextFloat escaped its half-open range.");
            }
        }
    }

    [TestCase]
    public void InvalidBoundsDoNotAdvanceTheStream()
    {
        Rng rng = new(123u);
        Rng expected = rng;
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(-1));
        DeterminismAssert.Equal(expected.NextFloat(), rng.NextFloat());
    }

    private static void AssertFloatStream(uint seed, int[] expected)
    {
        Rng rng = new(seed);
        foreach (int bits in expected)
        {
            DeterminismAssert.Equal(bits, BitConverter.SingleToInt32Bits(rng.NextFloat()));
        }
    }

    private static void AssertIntegerStream(uint seed, int max, int[] expected)
    {
        Rng rng = new(seed);
        foreach (int value in expected)
        {
            DeterminismAssert.Equal(value, rng.NextInt(max));
        }
    }
}

internal static class DeterminismAssert
{
    internal static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void Equal<T>(T expected, T actual) where T : IEquatable<T>
    {
        True(expected.Equals(actual), $"Expected {expected}; got {actual}.");
    }

    internal static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    internal static void Close(double expected, double actual, double absolute, double relative, string label)
    {
        if (double.IsNaN(expected))
        {
            True(double.IsNaN(actual), $"{label}: expected NaN; got {actual:R}.");
            return;
        }

        if (double.IsInfinity(expected))
        {
            Equal(expected, actual);
            return;
        }

        True(double.IsFinite(actual) && Math.Abs(actual - expected) <= absolute + relative * Math.Abs(expected),
            $"{label}: expected {expected:R}; got {actual:R}; tolerance abs={absolute:R}, rel={relative:R}.");
    }
}
