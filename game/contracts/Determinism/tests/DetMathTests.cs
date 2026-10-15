using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class DetMathTests
{
    private const double NegativeZero = -0d;

    [TestCase]
    public void FixedOperationOrderHasGoldenDoubleBits()
    {
        // These are protocol regression values, not comparisons to the host libm.
        AssertBits(0x3FDEAEE8744B05F0L, DetMath.Sin(0.5d));
        AssertBits(0x3FEC1528065B7D50L, DetMath.Cos(0.5d));
        AssertBits(0x3FDDAC670561BB4FL, DetMath.Atan(0.5d));
        AssertBits(0x3FFA61298E1E069BL, DetMath.Exp(0.5d));
        AssertBits(0x3FE62E42FEFA39EFL, DetMath.Log(2d));
        AssertBits(0x3FF428A2F98D728BL, DetMath.Cbrt(2d));
        DeterminismAssert.Equal(-8d, DetMath.Pow(-2d, 3d));
        DeterminismAssert.Equal(2d, DetMath.Cbrt(8d));
        DeterminismAssert.Equal(-2d, DetMath.Cbrt(-8d));
    }

    [TestCase]
    public void SignedZerosAndSpecialValuesHaveDefinedSemantics()
    {
        foreach (Func<double, double> fn in new Func<double, double>[] { DetMath.Sin, DetMath.Tan, DetMath.Asin, DetMath.Atan, DetMath.Cbrt })
        {
            AssertBits(long.MinValue, fn(NegativeZero));
            AssertBits(0L, fn(0d));
        }

        AssertBits(long.MinValue, DetMath.Atan2(NegativeZero, 1d));
        AssertBits(0L, DetMath.Atan2(0d, 1d));
        DeterminismAssert.Equal(Math.PI, DetMath.Atan2(0d, NegativeZero));
        DeterminismAssert.Equal(-Math.PI, DetMath.Atan2(NegativeZero, NegativeZero));
        DeterminismAssert.Equal(Math.PI / 4d, DetMath.Atan2(double.PositiveInfinity, double.PositiveInfinity));
        DeterminismAssert.Equal(-Math.PI / 4d, DetMath.Atan2(double.NegativeInfinity, double.PositiveInfinity));
        DeterminismAssert.Equal(3d * Math.PI / 4d, DetMath.Atan2(double.PositiveInfinity, double.NegativeInfinity));
        DeterminismAssert.Equal(-3d * Math.PI / 4d, DetMath.Atan2(double.NegativeInfinity, double.NegativeInfinity));
        AssertBits(long.MinValue, DetMath.Atan2(-1d, double.PositiveInfinity));
        DeterminismAssert.Equal(-Math.PI, DetMath.Atan2(-1d, double.NegativeInfinity));
        AssertBits(long.MinValue, DetMath.Pow(NegativeZero, 3d));
        DeterminismAssert.Equal(double.NegativeInfinity, DetMath.Pow(NegativeZero, -3d));
        DeterminismAssert.Equal(double.PositiveInfinity, DetMath.Pow(NegativeZero, -2d));
        DeterminismAssert.Equal(double.NegativeInfinity, DetMath.Log(NegativeZero));
        DeterminismAssert.Equal(1d, DetMath.Pow(double.NaN, 0d));
        DeterminismAssert.Equal(1d, DetMath.Pow(1d, double.NaN));
        DeterminismAssert.Equal(1d, DetMath.Pow(-1d, double.PositiveInfinity));
        DeterminismAssert.Equal(0d, DetMath.Pow(0.5d, double.PositiveInfinity));
        DeterminismAssert.Equal(double.PositiveInfinity, DetMath.Pow(0.5d, double.NegativeInfinity));
        DeterminismAssert.Equal(double.PositiveInfinity, DetMath.Pow(2d, double.PositiveInfinity));
        DeterminismAssert.Equal(0d, DetMath.Pow(2d, double.NegativeInfinity));
        DeterminismAssert.Equal(double.NegativeInfinity, DetMath.Pow(double.NegativeInfinity, 3d));
        DeterminismAssert.Equal(double.PositiveInfinity, DetMath.Pow(double.NegativeInfinity, 0.5d));
        AssertBits(long.MinValue, DetMath.Pow(double.NegativeInfinity, -3d));
        AssertBits(0L, DetMath.Pow(double.NegativeInfinity, -0.5d));
        DeterminismAssert.True(double.IsNaN(DetMath.Pow(double.NaN, 1d)), "Pow NaN base.");
        DeterminismAssert.True(double.IsNaN(DetMath.Pow(2d, double.NaN)), "Pow NaN exponent.");
        DeterminismAssert.Equal(double.PositiveInfinity, DetMath.Exp(double.PositiveInfinity));
        DeterminismAssert.Equal(0d, DetMath.Exp(double.NegativeInfinity));
        DeterminismAssert.Equal(double.PositiveInfinity, DetMath.Log(double.PositiveInfinity));
        DeterminismAssert.Equal(double.NegativeInfinity, DetMath.Cbrt(double.NegativeInfinity));

        foreach (Func<double, double> fn in new Func<double, double>[]
        {
            DetMath.Sin, DetMath.Cos, DetMath.Tan, DetMath.Asin, DetMath.Acos, DetMath.Atan,
            DetMath.Exp, DetMath.Log, DetMath.Log10, DetMath.Cbrt
        })
        {
            DeterminismAssert.True(double.IsNaN(fn(double.NaN)), "NaN input must remain NaN.");
        }

        foreach (double invalid in new[] { double.PositiveInfinity, double.NegativeInfinity, 1048576.001d, -1048576.001d })
        {
            DeterminismAssert.True(double.IsNaN(DetMath.Sin(invalid)), "Sin must reject an unsupported angle.");
            DeterminismAssert.True(double.IsNaN(DetMath.Cos(invalid)), "Cos must reject an unsupported angle.");
            DeterminismAssert.True(double.IsNaN(DetMath.Tan(invalid)), "Tan must reject an unsupported angle.");
        }

        DeterminismAssert.True(double.IsNaN(DetMath.Asin(1.0000000000000002d)), "Asin domain.");
        DeterminismAssert.True(double.IsNaN(DetMath.Acos(-1.0000000000000002d)), "Acos domain.");
        DeterminismAssert.True(double.IsNaN(DetMath.Log(-1d)), "Log domain.");
        DeterminismAssert.True(double.IsNaN(DetMath.Pow(-2d, 0.5d)), "Pow real domain.");
        DeterminismAssert.True(double.IsNaN(DetMath.Atan2(1d, double.NaN)), "Atan2 domain.");
    }

    [TestCase]
    public void TrigonometricFunctionsMeetSampledErrorEnvelopes()
    {
        List<double> angles = LinearSamples(-1048576d, 1048576d, 8192);
        angles.AddRange(LinearSamples(-Math.PI, Math.PI, 2048));
        angles.AddRange([double.Epsilon, -double.Epsilon, 1e-200d, -1e-200d]);
        CheckSamples("Sin", DetMath.Sin, Math.Sin, angles, 2e-15d, 0d);
        CheckSamples("Cos", DetMath.Cos, Math.Cos, angles, 2e-15d, 0d);

        List<double> tangentAngles = [];
        foreach (double angle in angles)
        {
            // Away from poles this is a meaningful relative-error envelope.
            if (Math.Abs(Math.Cos(angle)) >= 1e-6d)
            {
                tangentAngles.Add(angle);
            }
        }

        CheckSamples("Tan (|cos| >= 1e-6)", DetMath.Tan, Math.Tan, tangentAngles, 3e-15d, 2e-12d);
        foreach (double poleNeighbor in new[] { Math.BitDecrement(Math.PI / 2d), Math.PI / 2d, Math.BitIncrement(Math.PI / 2d) })
        {
            // Near a pole, small angle-reduction errors become large relative errors.
            DeterminismAssert.Close(Math.Tan(poleNeighbor), DetMath.Tan(poleNeighbor), 0d, 1e-8d, "Tan pole neighborhood");
        }

        List<double> unit = LinearSamples(-1d, 1d, 4096);
        unit.AddRange([Math.BitIncrement(-1d), Math.BitDecrement(1d), -1e-200d, 1e-200d]);
        CheckSamples("Asin", DetMath.Asin, Math.Asin, unit, 2e-15d, 0d);
        CheckSamples("Acos", DetMath.Acos, Math.Acos, unit, 2e-15d, 0d);
        List<double> atan = LinearSamples(-100d, 100d, 4096);
        atan.AddRange([double.MinValue, double.MaxValue, double.Epsilon, -double.Epsilon, double.PositiveInfinity, double.NegativeInfinity]);
        CheckSamples("Atan", DetMath.Atan, Math.Atan, atan, 2e-15d, 0d);

        double[] coordinates = [0d, NegativeZero, double.Epsilon, -double.Epsilon, 1e-200d, -1e-200d,
            0.5d, -0.5d, 1d, -1d, 100d, -100d, double.MaxValue, double.MinValue];
        double atan2MaxAbsolute = 0d;
        int atan2Count = 0;
        foreach (double x in coordinates)
        {
            foreach (double y in coordinates)
            {
                if (y == 0d)
                {
                    // Signed-zero and infinity conventions are asserted directly above,
                    // rather than inherited from a particular OS mathematical library.
                    continue;
                }

                double expected = Math.Atan2(y, x);
                double actual = DetMath.Atan2(y, x);
                DeterminismAssert.Close(expected, actual, 2e-15d, 0d, $"Atan2({y:R}, {x:R})");
                atan2MaxAbsolute = Math.Max(atan2MaxAbsolute, Math.Abs(actual - expected));
                atan2Count++;
            }
        }

        Console.WriteLine($"Atan2: n={atan2Count}, max abs={atan2MaxAbsolute:R}; sampled limit abs=2e-15, not global proof.");
    }

    [TestCase]
    public void ExponentialLogarithmAndCubeRootCoverBinary64Range()
    {
        List<double> exp = LinearSamples(-745d, 709d, 8192);
        exp.AddRange([709.782712893384d, Math.BitIncrement(709.782712893384d), -745.1332191019411d,
            Math.BitDecrement(-745.1332191019411d), 0d, 1e-20d, -1e-20d]);
        CheckSamples("Exp", DetMath.Exp, Math.Exp, exp, 2d * double.Epsilon, 2e-14d);

        List<double> positive = [double.Epsilon, double.MaxValue, Math.BitDecrement(1d), 1d, Math.BitIncrement(1d)];
        for (int exponent = -1074; exponent <= 1023; exponent += 1)
        {
            positive.Add(Math.ScaleB(1d, exponent));
            positive.Add(Math.ScaleB(1.25d, exponent));
        }

        CheckSamples("Log", DetMath.Log, Math.Log, positive, 3e-16d, 3e-15d);
        CheckSamples("Log10", DetMath.Log10, Math.Log10, positive, 3e-16d, 3e-15d);
        List<double> signed = new(positive);
        foreach (double value in positive)
        {
            signed.Add(-value);
        }

        CheckSamples("Cbrt", DetMath.Cbrt, Math.Cbrt, signed, 0d, 3e-15d);
    }

    [TestCase]
    public void PowersCoverRealDomainAndSpecialCases()
    {
        double[] specialBases = [double.MinValue, -2d, -1d, NegativeZero, 0d, double.Epsilon, 0.5d, 1d, 2d, double.MaxValue];
        double[] specialExponents = [-65d, -3d, -2d, -0.5d, NegativeZero, 0d, 0.5d, 2d, 3d, 65d];
        foreach (double x in specialBases)
        {
            foreach (double y in specialExponents)
            {
                DeterminismAssert.Close(Math.Pow(x, y), DetMath.Pow(x, y), 2d * double.Epsilon, 3e-12d, $"Pow({x:R}, {y:R})");
            }
        }

        double maxRelative = 0d;
        foreach (double x in LinearSamples(0.01d, 10d, 512))
        {
            foreach (double y in new[] { -12.5d, -3d, -0.5d, 0.5d, 3d, 12.5d, 65d })
            {
                double expected = Math.Pow(x, y);
                double actual = DetMath.Pow(x, y);
                DeterminismAssert.Close(expected, actual, 0d, 3e-12d, $"Pow({x:R}, {y:R})");
                maxRelative = Math.Max(maxRelative, Math.Abs((actual - expected) / expected));
            }
        }

        foreach (double x in new[] { Math.BitIncrement(1d), Math.BitDecrement(1d) })
        {
            foreach (double y in new[] { -1e18d, 1e18d })
            {
                DeterminismAssert.Close(Math.Pow(x, y), DetMath.Pow(x, y), 0d, 3e-12d, "Pow near one, large exponent");
            }
        }

        Console.WriteLine($"Pow sampled maximum relative error: {maxRelative:R} (not a global bound).");
    }

    [TestCase]
    public void FloatOverloadsMeetSinglePrecisionErrorEnvelopes()
    {
        foreach (float x in new[] { -1048576f, -1000f, -3.14f, -0.5f, -1e-20f, 0f, 0.5f, 3.14f, 1000f, 1048576f })
        {
            FloatClose(MathF.Sin(x), DetMath.Sin(x), "Sin(float)");
            FloatClose(MathF.Cos(x), DetMath.Cos(x), "Cos(float)");
            FloatClose(MathF.Tan(x), DetMath.Tan(x), "Tan(float)");
            FloatClose(MathF.Atan(x), DetMath.Atan(x), "Atan(float)");
            FloatClose(MathF.Atan2(x, 0.75f), DetMath.Atan2(x, 0.75f), "Atan2(float)");
            FloatClose(MathF.Cbrt(x), DetMath.Cbrt(x), "Cbrt(float)");
        }

        foreach (float x in new[] { -1f, -0.99f, -0.5f, -1e-20f, 0f, 0.5f, 0.99f, 1f })
        {
            FloatClose(MathF.Asin(x), DetMath.Asin(x), "Asin(float)");
            FloatClose(MathF.Acos(x), DetMath.Acos(x), "Acos(float)");
        }

        foreach (float x in new[] { -103f, -100f, -80f, -1f, 0f, 1f, 80f, 88f, 89f })
        {
            FloatClose(MathF.Exp(x), DetMath.Exp(x), "Exp(float)");
        }

        foreach (float x in new[] { float.Epsilon, 1e-30f, 0.01f, 0.5f, 1f, 2f, 1e30f, float.MaxValue })
        {
            FloatClose(MathF.Log(x), DetMath.Log(x), "Log(float)");
            FloatClose(MathF.Log10(x), DetMath.Log10(x), "Log10(float)");
            FloatClose(MathF.Pow(x, 0.37f), DetMath.Pow(x, 0.37f), "Pow(float)");
            FloatClose(MathF.Cbrt(x), DetMath.Cbrt(x), "Cbrt(float full range)");
        }

        DeterminismAssert.Equal(int.MinValue, BitConverter.SingleToInt32Bits(DetMath.Sin(-0f)));
        DeterminismAssert.True(float.IsNaN(DetMath.Cos(float.PositiveInfinity)), "Float domain.");
    }

    [TestCase]
    public void RotationsMatchNumericsConventions()
    {
        foreach (float angle in new[] { -1048576f, -3.14f, -0.5f, 0f, 0.5f, 3.14f, 1048576f })
        {
            MatrixClose(Matrix4x4.CreateRotationX(angle), DetMath.RotationX(angle));
            MatrixClose(Matrix4x4.CreateRotationY(angle), DetMath.RotationY(angle));
            MatrixClose(Matrix4x4.CreateRotationZ(angle), DetMath.RotationZ(angle));
            foreach (Vector3 axis in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, Vector3.Normalize(new Vector3(1f, 2f, -3f)) })
            {
                MatrixClose(Matrix4x4.CreateFromAxisAngle(axis, angle), DetMath.FromAxisAngle(axis, angle));
                QuaternionClose(Quaternion.CreateFromAxisAngle(axis, angle), DetMath.QuatFromAxisAngle(axis, angle));
            }

            MatrixClose(Matrix4x4.CreateFromYawPitchRoll(angle, angle * 0.5f, angle * 0.25f), DetMath.FromYawPitchRoll(angle, angle * 0.5f, angle * 0.25f));
            QuaternionClose(Quaternion.CreateFromYawPitchRoll(angle, angle * 0.5f, angle * 0.25f), DetMath.QuatFromYawPitchRoll(angle, angle * 0.5f, angle * 0.25f));
        }

        Matrix4x4 quarterTurn = DetMath.RotationY((float)(Math.PI / 2d));
        DeterminismAssert.Close(-1d, quarterTurn.M13, 2e-6d, 0d, "Positive Y rotation maps +X toward -Z.");
        MatrixClose(DetMath.FromAxisAngle(Vector3.UnitX, 0.5f), DetMath.FromAxisAngle(new Vector3(float.Epsilon, 0f, 0f), 0.5f));
        MatrixClose(DetMath.FromAxisAngle(Vector3.UnitX, 0.5f), DetMath.FromAxisAngle(new Vector3(float.MaxValue, 0f, 0f), 0.5f));
    }

    [TestCase]
    public void SlerpIsShortestArcAndPreservesUnitLength()
    {
        Quaternion a = DetMath.QuatFromYawPitchRoll(-0.8f, 0.3f, 0.1f);
        Quaternion[] targets = [a, new Quaternion(-a.X, -a.Y, -a.Z, -a.W),
            DetMath.QuatFromYawPitchRoll(-0.8001f, 0.3001f, 0.1001f),
            DetMath.QuatFromYawPitchRoll(2.1f, -0.9f, 0.7f), Quaternion.Identity];
        foreach (Quaternion b in targets)
        {
            foreach (float t in new[] { 0f, 0.1f, 0.5f, 0.9f, 1f })
            {
                Quaternion result = DetMath.Slerp(a, b, t);
                QuaternionClose(Quaternion.Slerp(a, b, t), result);
                double lengthSquared = (double)result.X * result.X + (double)result.Y * result.Y +
                    (double)result.Z * result.Z + (double)result.W * result.W;
                DeterminismAssert.Close(1d, lengthSquared, 3e-7d, 0d, "Slerp unit length");
            }
        }

        QuaternionClose(Quaternion.Identity, DetMath.Slerp(new Quaternion(0f, 0f, 0f, float.Epsilon), new Quaternion(0f, 0f, 0f, float.MaxValue), 0.5f));
    }

    [TestCase]
    public void InvalidRotationInputsFailEarly()
    {
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => DetMath.RotationX(float.NaN));
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => DetMath.RotationY(1048577f));
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => DetMath.RotationZ(float.PositiveInfinity));
        DeterminismAssert.Throws<ArgumentException>(() => DetMath.FromAxisAngle(Vector3.Zero, 0f));
        DeterminismAssert.Throws<ArgumentException>(() => DetMath.QuatFromAxisAngle(new Vector3(float.NaN, 0f, 0f), 0f));
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => DetMath.FromYawPitchRoll(0f, float.NaN, 0f));
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => DetMath.QuatFromYawPitchRoll(0f, 0f, float.PositiveInfinity));
        DeterminismAssert.Throws<ArgumentException>(() => DetMath.Slerp(default, Quaternion.Identity, 0f));
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => DetMath.Slerp(Quaternion.Identity, Quaternion.Identity, -0.01f));
        DeterminismAssert.Throws<ArgumentOutOfRangeException>(() => DetMath.Slerp(Quaternion.Identity, Quaternion.Identity, float.NaN));
    }

    [TestCase]
    public void ReportsPerCallTimingWithoutMachineDependentPassThresholds()
    {
        // Diagnostics include loop/delegate overhead; only release-build hardware
        // measurements can establish a production budget. No stopwatch is used in gen.
        (string Name, Func<double, double> Call)[] calls =
        [
            ("Sin", DetMath.Sin), ("Cos", DetMath.Cos), ("Tan", DetMath.Tan),
            ("Asin", DetMath.Asin), ("Acos", DetMath.Acos), ("Atan", DetMath.Atan),
            ("Atan2", x => DetMath.Atan2(x, 0.75d)), ("Exp", DetMath.Exp),
            ("Log", DetMath.Log), ("Log10", DetMath.Log10), ("Pow", x => DetMath.Pow(x, 0.37d)), ("Cbrt", DetMath.Cbrt),
            ("Sin(float)", x => DetMath.Sin((float)x)), ("Cos(float)", x => DetMath.Cos((float)x)),
            ("Tan(float)", x => DetMath.Tan((float)x)), ("Asin(float)", x => DetMath.Asin((float)x)),
            ("Acos(float)", x => DetMath.Acos((float)x)), ("Atan(float)", x => DetMath.Atan((float)x)),
            ("Atan2(float)", x => DetMath.Atan2((float)x, 0.75f)), ("Exp(float)", x => DetMath.Exp((float)x)),
            ("Log(float)", x => DetMath.Log((float)x)), ("Log10(float)", x => DetMath.Log10((float)x)),
            ("Pow(float)", x => DetMath.Pow((float)x, 0.37f)), ("Cbrt(float)", x => DetMath.Cbrt((float)x)),
            ("RotationX", x => DetMath.RotationX((float)x).M11), ("RotationY", x => DetMath.RotationY((float)x).M11),
            ("RotationZ", x => DetMath.RotationZ((float)x).M11),
            ("FromAxisAngle", x => DetMath.FromAxisAngle(Vector3.UnitY, (float)x).M11),
            ("FromYawPitchRoll", x => DetMath.FromYawPitchRoll((float)x, 0.2f, 0.3f).M11),
            ("QuatFromAxisAngle", x => DetMath.QuatFromAxisAngle(Vector3.UnitY, (float)x).W),
            ("QuatFromYawPitchRoll", x => DetMath.QuatFromYawPitchRoll((float)x, 0.2f, 0.3f).W),
            ("Slerp", x => DetMath.Slerp(Quaternion.Identity, new Quaternion(0f, 0.6f, 0f, 0.8f), (float)x).W)
        ];
        foreach ((string name, Func<double, double> call) in calls)
        {
            double sink = 0d;
            for (int i = 0; i < 256; i++)
            {
                sink += call(0.1d + (i & 255) / 512d);
            }

            const int count = 8192;
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < count; i++)
            {
                sink += call(0.1d + (i & 255) / 512d);
            }

            double nanoseconds = (Stopwatch.GetTimestamp() - start) * (1e9d / Stopwatch.Frequency) / count;
            DeterminismAssert.True(double.IsFinite(sink), "Timing diagnostic produced a nonfinite result.");
            Console.WriteLine($"DetMath.{name}: {nanoseconds:F1} ns/call including harness overhead; sampled diagnostic only.");
        }
    }

    private static List<double> LinearSamples(double min, double max, int intervals)
    {
        List<double> values = new(intervals + 1);
        for (int i = 0; i <= intervals; i++)
        {
            values.Add(min + (max - min) * i / intervals);
        }

        return values;
    }

    private static void CheckSamples(string name, Func<double, double> actualFunction, Func<double, double> reference,
        IEnumerable<double> values, double absolute, double relative)
    {
        double maxAbsolute = 0d;
        double maxRelative = 0d;
        int count = 0;
        foreach (double value in values)
        {
            double expected = reference(value);
            double actual = actualFunction(value);
            DeterminismAssert.Close(expected, actual, absolute, relative, $"{name}({value:R})");
            if (double.IsFinite(expected))
            {
                double error = Math.Abs(actual - expected);
                maxAbsolute = Math.Max(maxAbsolute, error);
                if (expected != 0d)
                {
                    maxRelative = Math.Max(maxRelative, error / Math.Abs(expected));
                }
            }

            count++;
        }

        Console.WriteLine($"{name}: n={count}, max abs={maxAbsolute:R}, max rel={maxRelative:R}; sampled limits abs={absolute:R}, rel={relative:R}, not global proof.");
    }

    private static void AssertBits(long expected, double actual) => DeterminismAssert.Equal(expected, BitConverter.DoubleToInt64Bits(actual));

    private static void FloatClose(float expected, float actual, string label) =>
        DeterminismAssert.Close(expected, actual, 2d * float.Epsilon, 8e-7d, label);

    private static void QuaternionClose(Quaternion expected, Quaternion actual)
    {
        double sign = (double)expected.X * actual.X + (double)expected.Y * actual.Y + (double)expected.Z * actual.Z + (double)expected.W * actual.W < 0d ? -1d : 1d;
        DeterminismAssert.Close(expected.X, sign * actual.X, 3e-6d, 0d, "Quaternion.X");
        DeterminismAssert.Close(expected.Y, sign * actual.Y, 3e-6d, 0d, "Quaternion.Y");
        DeterminismAssert.Close(expected.Z, sign * actual.Z, 3e-6d, 0d, "Quaternion.Z");
        DeterminismAssert.Close(expected.W, sign * actual.W, 3e-6d, 0d, "Quaternion.W");
    }

    private static void MatrixClose(Matrix4x4 expected, Matrix4x4 actual)
    {
        float[] left = [expected.M11, expected.M12, expected.M13, expected.M14, expected.M21, expected.M22, expected.M23, expected.M24,
            expected.M31, expected.M32, expected.M33, expected.M34, expected.M41, expected.M42, expected.M43, expected.M44];
        float[] right = [actual.M11, actual.M12, actual.M13, actual.M14, actual.M21, actual.M22, actual.M23, actual.M24,
            actual.M31, actual.M32, actual.M33, actual.M34, actual.M41, actual.M42, actual.M43, actual.M44];
        for (int i = 0; i < left.Length; i++)
        {
            DeterminismAssert.Close(left[i], right[i], 3e-6d, 0d, $"Matrix element {i}");
        }
    }
}
