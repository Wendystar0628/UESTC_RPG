using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Openings;

/// <summary>
/// 幕墙: one stick-system module, Columns × Rows lights. Pressure caps stand proud of the glass plane, bodies sit
/// behind it; module edges carry half members so neighbouring modules close into full ones. Depth is the
/// mullion depth. Glass comes from the building (window numbers per light, <see cref="OpeningGlass"/>).
/// L0: caps and bodies; L1: caps only; L2 and further: nothing.
/// </summary>
[Part(PartCatalog.CurtainWall, Owner = "WP-10")]
public sealed class CurtainWallPart() : OpeningPart<WindowParams>(new(900, 200))
{
    protected override Box3 Extent(WindowParams p) =>
        new(new(-p.Width * 0.5f, 0, -OpeningStyle.WallCapDepth - 0.002f), new(p.Width * 0.5f, p.Height, Math.Max(p.Depth * 0.5f, 0.02f)));

    protected override void Validate(WindowParams p)
    {
        Range(p.Columns, 1, 12, nameof(p.Columns));
        Range(p.Rows, 1, 8, nameof(p.Rows));
        if (p.Width / p.Columns < OpeningStyle.WallCapFace * 2 || p.Height / p.Rows < OpeningStyle.WallCapFace * 2)
            throw new ArgumentOutOfRangeException(nameof(p), "Too many lights for the module size.");
    }

    private protected override void Build(PartMesh mesh, WindowParams p, LodLevel lod)
    {
        if (lod >= LodLevel.L2) return;
        const byte flags = VertexFlag.NoShadow;
        var spec = Pick(p, OpeningStyle.Metal);
        var ins = OpeningStyle.Insert;
        var clear = OpeningStyle.GlassClear;
        float x0 = -p.Width * 0.5f, x1 = p.Width * 0.5f;
        var back = Math.Max(p.Depth * 0.5f, 0.02f);
        var capHalf = OpeningStyle.WallCapFace * 0.5f;
        var bodyHalf = OpeningStyle.WallMullionBody * 0.5f;
        var tHalf = OpeningStyle.WallTransomCapFace * 0.5f;
        var cols = p.Columns;
        var rows = p.Rows;

        // Mullions: full members inside, half members on the module edges.
        for (var i = 0; i <= cols; i++)
        {
            var x = x0 + p.Width * i / cols;
            float c0 = Math.Max(x - capHalf, x0), c1 = Math.Min(x + capHalf, x1);
            Solid.Box(mesh, spec, flags, c0, 0, -OpeningStyle.WallCapDepth, c1, p.Height, -clear);
            if (lod == LodLevel.L0)
                Solid.Box(mesh, spec, flags, Math.Max(x - bodyHalf, x0), 0, clear, Math.Min(x + bodyHalf, x1), p.Height, back);
        }

        // Transoms run between the mullion caps, shallower than the caps so the verticals lead.
        for (var j = 0; j <= rows; j++)
        {
            var y = p.Height * j / rows;
            float y0 = Math.Max(y - tHalf, 0), y1 = Math.Min(y + tHalf, p.Height);
            for (var c = 0; c < cols; c++)
            {
                var left = x0 + p.Width * c / cols;
                var right = c + 1 == cols ? x1 : x0 + p.Width * (c + 1) / cols;
                Solid.Box(mesh, spec, flags, left + capHalf - ins, y0, -OpeningStyle.WallTransomCapDepth, right - capHalf + ins, y1, -clear);
                if (lod == LodLevel.L0)
                    Solid.Box(mesh, spec, flags, left + bodyHalf - ins, y0, clear, right - bodyHalf + ins, y1, back * 0.8f);
            }
        }
    }
}
