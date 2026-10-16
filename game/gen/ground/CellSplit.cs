namespace Campus.Gen.Ground;

/// <summary>
/// The one triangulation of a lattice cell that every ground height uses: the anti-diagonal, which joins corner (1, 0)
/// to corner (0, 1) (WLD-TERRAIN 9.3). The L0 mesh, the terrain draping, the lawn tone field and the collision height
/// field's sampler all interpolate through <see cref="Lerp"/>, and the mesher cuts its blocks and cells along the same
/// line, so mesh, collision and draping describe one surface.
/// Godot 4.7.2's Jolt height-field shape (65 x 65 map data, x fastest, rows along +Z) splits its cells this way: measured
/// with native rays on a non-coplanar fixture, the anti-diagonal fits to 0.0000112 m where the main diagonal is 0.36 m off.
/// </summary>
internal static class CellSplit
{
    /// <summary>
    /// Height at fractions (<paramref name="u"/> along +x, <paramref name="w"/> along +z) of a cell with corner values
    /// <c>hXZ</c>. Lower-left triangle 00-10-01 where u + w &lt;= 1, upper-right triangle 10-11-01 beyond it.
    /// </summary>
    public static float Lerp(float h00, float h10, float h01, float h11, float u, float w) =>
        u + w <= 1f
            ? h00 + u * (h10 - h00) + w * (h01 - h00)
            : h11 + (1f - w) * (h10 - h11) + (1f - u) * (h01 - h11);
}
