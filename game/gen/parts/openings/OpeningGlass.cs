using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

/// <summary>
/// Glass for the wall block behind a WP-10 frame part, in the part frame (glass plane z = 0, exterior −Z), encoded as the
/// VF-B glass contract (GeometryBundle) and WP-14 write it, so toon_glass keeps its window cards (WLD-INTERIORS):
/// floor(UV) = building-grid (column, row), UV2 = (BaseId, channel slot + 32 × TotalColumns), spare byte = window type
/// 1–15. Window ID = BaseId + row × TotalColumns + column. Columns run along the part's +X, which is WP-14's facade
/// Along. One-sided and shadow-casting: it closes the building shell.
/// </summary>
public static class OpeningGlass
{
    /// <summary>One window ID for the whole opening, as WP-14 does for punched windows.</summary>
    public static GeometryBundle Opening(float width, float height, int windowBase, int column, int row, int columns, ChannelId channel, byte card) =>
        Grid(width, height, 1, 1, windowBase, column, row, columns, channel, card);

    /// <summary>
    /// The light-inventory entry (ContentOutput.LooseWindows) for a window drawn with <see cref="Opening"/> under the
    /// row-vector placement <paramref name="frame"/> (<see cref="OpeningPlacement.Frame"/>). Right × Up points outdoors.
    /// </summary>
    public static WindowDecl Declare(Matrix4x4 frame, float width, float height, int windowBase, int column, int row, int columns, ChannelId channel)
    {
        _ = Slot(channel);
        var half = height * 0.5f;
        var centre = new Vector3(frame.M41 + frame.M21 * half, frame.M42 + frame.M22 * half, frame.M43 + frame.M23 * half);
        return new(windowBase + row * columns + column, windowBase, columns, channel, centre,
            new(-frame.M11, -frame.M12, -frame.M13), new(frame.M21, frame.M22, frame.M23), width, height);
    }

    /// <summary>
    /// A grid of lights, each its own window cell: light (c, r) is cell (column + c, row + r). Used for curtain-wall
    /// modules, whose lights take consecutive columns and storey rows of the facade numbering.
    /// </summary>
    public static GeometryBundle Grid(float width, float height, int lightColumns, int lightRows, int windowBase, int column, int row,
        int columns, ChannelId channel, byte card)
    {
        if (!(width > 0) || !(height > 0) || !float.IsFinite(width) || !float.IsFinite(height)) throw new ArgumentOutOfRangeException(nameof(width));
        if (lightColumns < 1 || lightRows < 1 || column < 0 || row < 0 || columns < column + lightColumns)
            throw new ArgumentOutOfRangeException(nameof(columns), "The lights must fit the facade's column grid.");
        if (windowBase is < 0 or > 1 << 24) throw new ArgumentOutOfRangeException(nameof(windowBase), "BaseId is an integer up to 2^24.");
        if (card > 15) throw new ArgumentOutOfRangeException(nameof(card), "Window types are 0–15.");
        var mesh = new PartMesh();
        var layer = mesh.Get(OpeningStyle.Glass);
        var uv2 = new Vector2(windowBase, Slot(channel) + 32f * columns);
        const float m = 0.002f;
        for (var r = 0; r < lightRows; r++)
            for (var c = 0; c < lightColumns; c++)
            {
                float x0 = -width * 0.5f + width * c / lightColumns, x1 = -width * 0.5f + width * (c + 1) / lightColumns;
                float y0 = height * r / lightRows, y1 = height * (r + 1) / lightRows;
                float u0 = column + c + m, u1 = column + c + 1 - m, v0 = row + r + m, v1 = row + r + 1 - m;
                var n = -Vector3.UnitZ;
                var a = layer.Vertex(new(x0, y0, 0), n, new(u0, v0), 1f, uv2, card);
                var b = layer.Vertex(new(x1, y0, 0), n, new(u1, v0), 1f, uv2, card);
                var d = layer.Vertex(new(x1, y1, 0), n, new(u1, v1), 1f, uv2, card);
                var e = layer.Vertex(new(x0, y1, 0), n, new(u0, v1), 1f, uv2, card);
                layer.Quad(a, b, d, e, n);
            }
        return mesh.Finish(new(new(-width * 0.5f, 0, 0), new(width * 0.5f, height, 0))).Meshes.Single();
    }

    private static int Slot(ChannelId channel)
    {
        var slot = LightChannelCatalog.Slot(channel);
        return slot >= 0 ? slot : throw new ArgumentException($"Unregistered light channel '{channel.Value}'.", nameof(channel));
    }
}

/// <summary>Helpers for WP-14 and the zone packs when they place WP-10 parts (PLAN-WP).</summary>
public static class OpeningPlacement
{
    /// <summary>
    /// Row-vector placement of an opening part: <paramref name="origin"/> is the bottom centre of the clear opening on
    /// the glass plane and <paramref name="outward"/> (horizontal) points out of the building, the part's −Z. The
    /// part's +X follows as up × (−outward): seen from outside it runs to the viewer's left.
    /// </summary>
    public static Matrix4x4 Frame(Vector3 origin, Vector3 outward)
    {
        var z = VectorMath.Unit(-outward, 1e-9);
        if (Math.Abs(z.Y) > 1e-4) throw new ArgumentException("A facade normal must be horizontal.", nameof(outward));
        // x = up × z, y = z × x (= up for a horizontal z).
        var x = VectorMath.Unit(new Vector3(z.Z, 0, -z.X), 1e-9);
        var y = VectorMath.Cross(z, x);
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, origin.X, origin.Y, origin.Z, 1);
    }

    /// <summary>
    /// Curtain for one window, stable per window number: about 60 % of windows get one, open : half : drawn = 3 : 4 : 3,
    /// colours differ (ART-FIDELITY). Returns null for a bare window. toon_glass is opaque, so the curtain hangs
    /// between the glass and the sash face (depth ≤ 16 mm), sized to the opening so rail and edges hide behind the frame.
    /// </summary>
    public static PanelParams? Curtain(uint seed, int windowNumber, float glassWidth, float glassHeight, float depth = 0.016f)
    {
        var text = windowNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var hash = StableHash.Hash32(seed, "opening.curtain", text);
        if ((hash & 0xffff) >= OpeningStyle.CurtainShare * 65536f) return null;
        var total = OpeningStyle.RatioOpen + OpeningStyle.RatioHalf + OpeningStyle.RatioDrawn;
        var pick = (int)(((hash >> 16) & 0xff) * total / 256u);
        var state = pick < OpeningStyle.RatioOpen ? 0 : pick < OpeningStyle.RatioOpen + OpeningStyle.RatioHalf ? 1 : 2;
        var colours = OpeningStyle.CurtainColours;
        var weights = colours.Sum(c => c.Weight);
        var ticket = (int)(((hash >> 24) & 0xff) * (uint)weights / 256u);
        var family = colours[^1].Family;
        foreach (var (name, weight) in colours)
        {
            if (ticket < weight) { family = name; break; }
            ticket -= weight;
        }
        var fabric = OpeningStyle.Fabric with { Lit = new(family + ".lit"), Shade = new(family + ".shade") };
        return new PanelParams(glassWidth, glassHeight, depth) { Variation = state, Material = fabric };
    }
}
