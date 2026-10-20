using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Core;

/// <summary>A pure geometry example; the kernel owns camera, selection, background scheduling and mounting.</summary>
public sealed record GalleryExample(string Id, string Label, PartGeometry Geometry);

public static class PartGallery
{
    /// <summary>Build on a bake worker. Examples are individual local-coordinate objects, not a world module.</summary>
    public static IReadOnlyList<GalleryExample> CreateExamples()
    {
        var material = new ToonSpec(new("wall.lit"), new("wall.shade")) { Fx = ToonFx.BevelLift };
        var trimMaterial = material with { Line = PartStyleDefaults.TrimLine };
        Vector2[] concave = [new(-2, -1), new(2, -1), new(2, 0), new(0, 0), new(0, 2), new(-2, 2)];
        Vector2[] window = [new(-0.8f, 0.8f), new(0.8f, 0.8f), new(0.8f, 2.2f), new(-0.8f, 2.2f)];
        return Array.AsReadOnly<GalleryExample>(
        [
            new("beveled-box", "单段倒角与平面法线", new BoxPart().Lods[LodLevel.L0](new(2, 2, 2) { Bevel = 0.12f, Material = material })),
            new("concave-extrusion", "凹多边形挤出", PartSolids.Extrude(new(concave), 1.2f, material)),
            new("window-opening", "有厚度的开洞墙板", PartSolids.WallPanel(3.6f, 3, 0.35f, [window], material)),
            new("mitered-trim", "转角斜接与倒角扫掠", PartSweeps.Sweep(PartStyleDefaults.CorniceProfile(), [new(-2, 0), new(1, 0), new(1, 2)], trimMaterial)),
            new("beveled-extrusion", "多边形上下沿倒角", PartSolids.BeveledExtrude([new(-1, -1), new(1, -1), new(1.4f, 0), new(1, 1), new(-1, 1)], 1, 0.12f, material)),
        ]);
    }

    /// <summary>Stable sample placements for inspection of the array helper; IDs already exist in PartCatalog.</summary>
    public static IReadOnlyList<PlacedPart> CreateArrayExample() => PartPlacement.Linear(
        new(new(PartCatalog.Column), new ColumnParams(), Matrix4x4.Identity), 4, new(1.2f, 0, 0));
}
