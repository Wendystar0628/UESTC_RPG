namespace Campus.Contracts;

/// <summary>
/// Integer lattice noise shared by scenery, sky and wet surfaces. The existing hash salts, bit selection and
/// cubic/quintic fades are intentionally distinct: changing any of them changes authored procedural output.
/// Explicit arithmetic is needed for shader parity and for retaining the current bake output.
/// </summary>
public static class IntegerNoise
{
    /// <summary>The shared scenery/lawn/cloud avalanche; callers supply the already combined integer seed.</summary>
    public static uint Mix(uint value)
    {
        unchecked
        {
            value ^= value >> 15;
            value *= 0x2C1B3C6Du;
            value ^= value >> 12;
            value *= 0x297A2D39u;
            return value ^ (value >> 15);
        }
    }

    public static uint Hash1D(uint x, uint seed) => Mix(unchecked(x * 0x9E3779B1u ^ seed * 0x85EBCA77u));

    /// <summary>Raw seed XOR layout used by lawn.</summary>
    public static uint Hash2D(uint x, uint z, uint seed) => Mix(unchecked(x * 0x8DA6B343u ^ z * 0xD8163841u ^ seed));

    /// <summary>Three multiplied coordinates, as used by cloud cells and their salted hash channels.</summary>
    public static uint Hash3D(uint x, uint y, uint z) => Hash2D(x, y, unchecked(z * 0xCB1AB31Fu));

    /// <summary>Scenery, lawn and StableHash samples use the high 24 bits; clouds use the low 24 bits.</summary>
    public static float High24(uint hash) => (hash >> 8) * (1f / 16777216f);
    public static float Low24(uint hash) => (hash & 0xFFFFFFu) / 16777216f;

    /// <summary>Cubic 1D noise in [-1,1), optionally periodic in lattice cells, as used by the diorama.</summary>
    public static float Value1D(float t, uint seed, int wrap = 0)
    {
        int i = (int)MathF.Floor(t);
        float a = High24(Hash1D(unchecked((uint)CellIndex(i, wrap)), seed));
        float b = High24(Hash1D(unchecked((uint)CellIndex(i + 1, wrap)), seed));
        return ScalarMath.Lerp(a, b, ScalarMath.Smoothstep(t - i)) * 2f - 1f;
    }

    /// <summary>Unbounded quintic 2D noise in [0,1), with the lawn's raw-seed/high-bit layout.</summary>
    public static float Value2D(float x, float z, uint seed) => Sample2D(x, z, seed, Lattice.Lawn, 0);

    /// <summary>Tileable cloud noise; normalized u/v are multiplied by frequency before sampling.</summary>
    public static float TileableValue2D(float u, float v, int frequency, uint salt) =>
        Sample2D(u * frequency, v * frequency, salt, Lattice.Cloud, frequency);

    /// <summary>Cubic wet-surface noise, matching precip_wet.gdshaderinc's nested integer hash.</summary>
    public static float PuddleValue2D(float x, float z) => Sample2D(x, z, 0, Lattice.Puddle, 0);

    private enum Lattice { Lawn, Cloud, Puddle }

    private static float Sample2D(float x, float z, uint seed, Lattice lattice, int period)
    {
        float floorX = MathF.Floor(x), floorZ = MathF.Floor(z);
        int ix = (int)floorX, iz = (int)floorZ;
        float fx = x - floorX, fz = z - floorZ;
        fx = lattice == Lattice.Puddle ? ScalarMath.Smoothstep(fx) : ScalarMath.Smootherstep(fx);
        fz = lattice == Lattice.Puddle ? ScalarMath.Smoothstep(fz) : ScalarMath.Smootherstep(fz);
        float a = Cell(ix, iz, seed, lattice, period), b = Cell(ix + 1, iz, seed, lattice, period);
        float c = Cell(ix, iz + 1, seed, lattice, period), d = Cell(ix + 1, iz + 1, seed, lattice, period);
        return ScalarMath.Lerp(ScalarMath.Lerp(a, b, fx), ScalarMath.Lerp(c, d, fx), fz);
    }

    private static float Cell(int x, int z, uint seed, Lattice lattice, int period)
    {
        unchecked
        {
            return lattice switch
            {
                Lattice.Lawn => High24(Hash2D((uint)x, (uint)z, seed)),
                Lattice.Cloud => Low24(Hash3D((uint)CellIndex(x, period), (uint)CellIndex(z, period), seed)),
                Lattice.Puddle => High24(PuddleHash((uint)x * 73856093u ^ PuddleHash((uint)z * 19349663u + 1u))),
                _ => throw new ArgumentOutOfRangeException(nameof(lattice))
            };
        }
    }

    private static uint PuddleHash(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 2146121005u;
            value ^= value >> 15;
            value *= 2221713035u;
            return value ^ (value >> 16);
        }
    }

    private static int CellIndex(int index, int period) => period > 0 ? ((index % period) + period) % period : index;
}
