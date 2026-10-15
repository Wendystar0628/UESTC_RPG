namespace Campus.Contracts;

/// <summary>Scalar deterministic mathematics for generation and baking.</summary>
/// <remarks>
/// Fixed binary64 operation order, range reduction and iteration counts; float
/// overloads compute in binary64 then round once. No platform transcendental
/// functions, SIMD reductions or fused multiply-add are used. Sqrt and Round
/// are the permitted IEEE operations. Cross-architecture byte identity still
/// requires the bake verification gate; a local accuracy test cannot prove it.
///
/// Sin/Cos/Tan accept finite radians with absolute value at most 1048576 (2^20).
/// Larger angles and infinities return NaN: full-range Payne-Hanek reduction is
/// deliberately outside this API's current numerical contract. Other scalar
/// functions cover their real domains, with NaN for domain errors, infinities
/// for poles/overflow, and signed zero where meaningful. Tan is ill-conditioned
/// near its poles; Pow error grows with the exponent. No correct-rounding or
/// global ULP guarantee is claimed. Tests record sampled, function-specific
/// error limits; changing coefficients/order requires rechecking the bake ABI.
/// </remarks>
public static partial class DetMath
{
    private const double Pi = 3.14159265358979323846264338327950288;
    private const double HalfPi = 1.57079632679489661923132169163975144;
    private const double QuarterPi = 0.78539816339744830961566084581987572;
    private const double MaxAngle = 1048576d;
    private const double InvHalfPi = 0.63661977236758134307553505349005745;
    private const double HalfPiHigh = 1.570796326734125614166;
    private const double HalfPiLow = 6.07710050650619224932e-11;
    private const double Ln2High = 0.69314718036912381649;
    private const double Ln2Low = 1.90821492927058770002e-10;
    private const double InvLn2 = 1.44269504088896340735992468100189214;
    private const double InvLn10 = 0.43429448190325182765112891891660508;

    public static float Sin(float x) => (float)Sin((double)x);
    public static float Cos(float x) => (float)Cos((double)x);
    public static float Tan(float x) => (float)Tan((double)x);
    public static float Asin(float x) => (float)Asin((double)x);
    public static float Acos(float x) => (float)Acos((double)x);
    public static float Atan(float x) => (float)Atan((double)x);
    public static float Atan2(float y, float x) => (float)Atan2((double)y, x);
    public static float Exp(float x) => (float)Exp((double)x);
    public static float Log(float x) => (float)Log((double)x);
    public static float Log10(float x) => (float)Log10((double)x);
    public static float Pow(float x, float y) => (float)Pow((double)x, y);
    public static float Cbrt(float x) => (float)Cbrt((double)x);

    public static double Sin(double x)
    {
        if (x == 0d)
        {
            return x;
        }

        (double sin, _) = SinCosCore(x);
        return sin;
    }

    public static double Cos(double x)
    {
        (_, double cos) = SinCosCore(x);
        return cos;
    }

    public static double Tan(double x)
    {
        if (x == 0d)
        {
            return x;
        }

        (double sin, double cos) = SinCosCore(x);
        return sin / cos;
    }

    public static double Asin(double x)
    {
        if (double.IsNaN(x) || x < -1d || x > 1d)
        {
            return double.NaN;
        }

        return Atan2(x, Math.Sqrt((1d - x) * (1d + x)));
    }

    public static double Acos(double x)
    {
        if (double.IsNaN(x) || x < -1d || x > 1d)
        {
            return double.NaN;
        }

        return Atan2(Math.Sqrt((1d - x) * (1d + x)), x);
    }

    public static double Atan(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        if (x == 0d)
        {
            return x;
        }

        double magnitude = Math.Abs(x);
        bool reciprocal = magnitude > 1d;
        double reduced = reciprocal ? 1d / magnitude : magnitude;
        bool aroundOne = reduced > 0.41421356237309504880168872420969808;
        if (aroundOne)
        {
            reduced = (reduced - 1d) / (reduced + 1d);
        }

        // |reduced| <= tan(pi/8). The omitted term is below 1e-21.
        double square = reduced * reduced;
        double term = reduced;
        double sum = term;
        for (int i = 1; i < 26; i++)
        {
            term *= -square;
            sum += term / (2 * i + 1);
        }

        double result = aroundOne ? QuarterPi + sum : sum;
        if (reciprocal)
        {
            result = HalfPi - result;
        }

        return CopySign(result, x);
    }

    /// <summary>Quadrant-aware angle, retaining the sign of zero.</summary>
    /// <remarks>
    /// Atan2(+/-0, negative x or -0) is +/-pi. Two infinite coordinates use
    /// their signed diagonal direction (+/-pi/4 or +/-3pi/4), independent of
    /// host-library conventions. Any NaN argument produces NaN.
    /// </remarks>
    public static double Atan2(double y, double x)
    {
        if (double.IsNaN(x) || double.IsNaN(y))
        {
            return double.NaN;
        }

        if (y == 0d)
        {
            return IsNegative(x) ? CopySign(Pi, y) : y;
        }

        if (x == 0d)
        {
            return CopySign(HalfPi, y);
        }

        if (double.IsInfinity(y))
        {
            double result = double.IsInfinity(x) ? (x > 0d ? QuarterPi : 3d * QuarterPi) : HalfPi;
            return CopySign(result, y);
        }

        if (double.IsInfinity(x))
        {
            return CopySign(x > 0d ? 0d : Pi, y);
        }

        double ax = Math.Abs(x);
        double ay = Math.Abs(y);
        // Divide the smaller magnitude by the larger to avoid ratio overflow.
        double angle = ax >= ay ? Atan(ay / ax) : HalfPi - Atan(ax / ay);
        if (x < 0d)
        {
            angle = Pi - angle;
        }

        return CopySign(angle, y);
    }

    public static double Exp(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        if (x > 709.782712893383973096d)
        {
            return double.PositiveInfinity;
        }

        if (x < -745.13321910194110842d)
        {
            return 0d;
        }

        int exponent = (int)Math.Round(x * InvLn2, MidpointRounding.ToEven);
        double reduced = (x - exponent * Ln2High) - exponent * Ln2Low;
        double term = 1d;
        double sum = 1d;
        // |reduced| <= ln(2)/2. The omitted term is below 2e-26.
        for (int i = 1; i <= 18; i++)
        {
            term *= reduced / i;
            sum += term;
        }

        return ScaleByPowerOfTwo(sum, exponent);
    }

    public static double Log(double x)
    {
        if (double.IsNaN(x) || x < 0d)
        {
            return double.NaN;
        }

        if (x == 0d)
        {
            return double.NegativeInfinity;
        }

        if (double.IsPositiveInfinity(x))
        {
            return double.PositiveInfinity;
        }

        (double mantissa, int exponent) = Decompose(x);
        if (mantissa > 1.41421356237309504880168872420969808)
        {
            mantissa *= 0.5d;
            exponent++;
        }

        double reduced = (mantissa - 1d) / (mantissa + 1d);
        double square = reduced * reduced;
        double term = reduced;
        double sum = term;
        // log(m) = 2 atanh((m-1)/(m+1)); |reduced| <= 0.172.
        for (int i = 1; i < 20; i++)
        {
            term *= square;
            sum += term / (2 * i + 1);
        }

        return exponent * Ln2High + (2d * sum + exponent * Ln2Low);
    }

    public static double Log10(double x) => Log(x) * InvLn10;

    public static double Pow(double x, double y)
    {
        if (y == 0d || x == 1d)
        {
            return 1d;
        }

        if (double.IsNaN(x) || double.IsNaN(y))
        {
            return double.NaN;
        }

        double magnitude = Math.Abs(x);
        if (double.IsInfinity(y))
        {
            if (magnitude == 1d)
            {
                return 1d;
            }

            return (magnitude > 1d) == (y > 0d) ? double.PositiveInfinity : 0d;
        }

        bool integer = y == Math.Round(y, MidpointRounding.ToEven);
        bool odd = integer && Math.Abs(y) < 9007199254740992d && (((long)y & 1L) != 0L);
        if (x == 0d || double.IsInfinity(x))
        {
            double result = (x == 0d) == (y > 0d) ? 0d : double.PositiveInfinity;
            return odd ? CopySign(result, x) : result;
        }

        if (x < 0d && !integer)
        {
            return double.NaN;
        }

        double value;
        if (integer && Math.Abs(y) <= 64d)
        {
            // Preserve exact small integral powers, especially binary scalings.
            int exponent = (int)Math.Abs(y);
            double factor = y < 0d ? 1d / magnitude : magnitude;
            value = 1d;
            while (exponent > 0)
            {
                if ((exponent & 1) != 0)
                {
                    value *= factor;
                }

                exponent >>= 1;
                if (exponent != 0)
                {
                    factor *= factor;
                }
            }
        }
        else
        {
            value = Exp(y * Log(magnitude));
        }

        return x < 0d && odd ? -value : value;
    }

    public static double Cbrt(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        if (x == 0d || double.IsInfinity(x))
        {
            return x;
        }

        (double mantissa, int exponent) = Decompose(Math.Abs(x));
        int third = exponent / 3;
        double reduced = ScaleByPowerOfTwo(mantissa, exponent - 3 * third);
        double root = 1d;
        // reduced lies in [0.25, 8); ten Newton steps cover this whole interval.
        for (int i = 0; i < 10; i++)
        {
            root = (2d * root + reduced / (root * root)) / 3d;
        }

        return CopySign(ScaleByPowerOfTwo(root, third), x);
    }

    private static (double Sin, double Cos) SinCosCore(double x)
    {
        if (!double.IsFinite(x) || Math.Abs(x) > MaxAngle)
        {
            return (double.NaN, double.NaN);
        }

        int quadrant = (int)Math.Round(x * InvHalfPi, MidpointRounding.ToEven);
        double reduced = (x - quadrant * HalfPiHigh) - quadrant * HalfPiLow;
        double square = reduced * reduced;
        double sinTerm = reduced;
        double cosTerm = 1d;
        double sin = sinTerm;
        double cos = cosTerm;
        // |reduced| <= pi/4; terms through degree 19 and 18, respectively.
        for (int i = 1; i <= 9; i++)
        {
            sinTerm *= -square / ((2 * i) * (2 * i + 1));
            cosTerm *= -square / ((2 * i - 1) * (2 * i));
            sin += sinTerm;
            cos += cosTerm;
        }

        return (quadrant & 3) switch
        {
            0 => (sin, cos),
            1 => (cos, -sin),
            2 => (-sin, -cos),
            3 => (-cos, sin),
            _ => throw new InvalidOperationException("Invalid reduced quadrant.")
        };
    }

    // The input is finite and strictly positive. Subnormals are normalized exactly.
    private static (double Mantissa, int Exponent) Decompose(double x)
    {
        int adjustment = 0;
        long bits = BitConverter.DoubleToInt64Bits(x);
        if ((bits & 0x7ff0000000000000L) == 0L)
        {
            x *= 4503599627370496d; // 2^52, including the smallest subnormal.
            adjustment = -52;
            bits = BitConverter.DoubleToInt64Bits(x);
        }

        int exponent = (int)((bits >> 52) & 0x7ffL) - 1023 + adjustment;
        double mantissa = BitConverter.Int64BitsToDouble((bits & 0x000fffffffffffffL) | 0x3ff0000000000000L);
        return (mantissa, exponent);
    }

    // Callers supply a near-unity value and an exponent in [-1075, 1024].
    private static double ScaleByPowerOfTwo(double value, int exponent)
    {
        if (exponent > 1023)
        {
            return (value * PowerOfTwo(1023)) * 2d;
        }

        if (exponent < -1022)
        {
            return (value * PowerOfTwo(exponent + 54)) * (1d / 18014398509481984d);
        }

        return value * PowerOfTwo(exponent);
    }

    private static double PowerOfTwo(int exponent) => BitConverter.Int64BitsToDouble((long)(exponent + 1023) << 52);

    private static bool IsNegative(double x) => BitConverter.DoubleToInt64Bits(x) < 0L;

    private static double CopySign(double magnitude, double sign) => BitConverter.Int64BitsToDouble(
        (BitConverter.DoubleToInt64Bits(magnitude) & 0x7fffffffffffffffL) |
        (BitConverter.DoubleToInt64Bits(sign) & long.MinValue));
}
