using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Indoor hydrant box (消火栓箱), the rectangular red-framed glass door of WLD-INTERIORS: a box set against the wall, a
/// chamfered front frame with a recessed pane, "消火栓" across the upper pane and a pull on the right stile.
/// Variation 0: red frame, glass door, white lettering. 1: red steel door with a small vision pane. 2: stainless frame, glass
/// door, red lettering. Back on +Z; visual only (it sits inside the wall collider's clearance).
/// </summary>
[Part(PartCatalog.Hydrant)]
public sealed class HydrantPart() : MepPart<EquipmentParams>(new(700, 120))
{
    private const float Pull = 0.012f;

    protected override Vector3 Minimum => new(0.4f, 0.5f, 0.12f);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var f = MepStyle.Flags;
        var hx = p.Width * 0.5f; var hz = p.Depth * 0.5f;
        var frame = PartMaterials.Casing(v == 2 ? MepStyle.Stainless : MepStyle.FireRed);
        var red = PartMaterials.Casing(MepStyle.FireRed);
        var fw = MepStyle.HydrantFrame; var fd = MepStyle.HydrantFrameDepth;
        var face = -hz + Pull;   // frame front; the pull stands proud of it
        b.AddBox(new(-hx + 0.006f, 0.006f, face + fd - 0.003f), new(hx - 0.006f, p.Height - 0.006f, hz), 0, frame, f);
        Vector2[] opening = [new(-hx + fw, fw), new(hx - fw, fw), new(hx - fw, p.Height - fw), new(-hx + fw, p.Height - fw)];
        b.Add(PartSolids.WallPanel(p.Width, p.Height, fd, [opening], frame), Xf.Move(0, 0, face + fd * 0.5f), f);
        // Door leaf in the opening, set back from the frame front.
        var pane = face + MepStyle.HydrantGlassRecess;
        Vector3 d0 = new(-hx + fw - 0.004f, fw - 0.004f, pane), d1 = new(hx - fw + 0.004f, p.Height - fw + 0.004f, pane + 0.012f);
        var glass = PartMaterials.Grille(MepStyle.Glass);
        if (v == 1)
        {
            b.AddBox(d0, d1, 0, red, f);
            if (near) b.AddBox(new(-hx * 0.4f, p.Height * 0.2f, pane - 0.002f), new(hx * 0.4f, p.Height * 0.4f, pane + 0.004f), 0, glass, f);
        }
        else b.AddBox(d0, d1, 0, glass, f);
        if (!near) return;
        b.AddBox(new(hx - fw * 0.5f - 0.009f, p.Height * 0.4f, -hz), new(hx - fw * 0.5f + 0.009f, p.Height * 0.6f, face + 0.002f), 0.004f, PartMaterials.Metal(MepStyle.Aluminium), f);
        var lettering = PartMaterials.Grille(v == 2 ? MepStyle.FireRed : MepStyle.White);
        var h = Math.Min(p.Height * MepStyle.HydrantLabelRatio, (p.Width - 2 * fw - 0.06f) / StrokeText.Width(MepStyle.HydrantLabel, 1));
        var zText = v == 1 ? pane : pane - 0.0005f;
        StrokeFont.Write(b, MepStyle.HydrantLabel, 0, p.Height - fw - 0.04f - h * 0.6f, zText, h, lettering);
    }
}

/// <summary>
/// Dry-powder extinguisher (灭火器): red body with a domed shoulder, white label band, dark valve head with lever and gauge,
/// and a dark hose down the side. Variation 0: one on the floor. 1: a pair side by side. 2: one in a wall bracket with a strap.
/// Floor-standing prop.
/// </summary>
[Part(PartCatalog.Extinguisher)]
public sealed class ExtinguisherPart() : MepPart<EquipmentParams>(new(1300, 200))
{
    protected override Vector3 Minimum => new(0.12f, 0.3f, 0.12f);

    public override IReadOnlyList<Collider> Collision(EquipmentParams p) => PropBox(p);

    private protected override void Compose(PartBuilder b, EquipmentParams p, bool near)
    {
        var v = Variant(p);
        var count = v == 1 ? 2 : 1;
        var cell = p.Width / count;
        var r = Math.Max(count == 1 ? 0.03f : 0.02f, Math.Min(cell, p.Depth) * 0.5f - 0.022f);
        var body = p.Height * (1 - MepStyle.ExtinguisherValveRatio);
        for (var i = 0; i < count; i++)
        {
            var x = -p.Width * 0.5f + cell * (i + 0.5f);
            // A single unit hangs its hose down the side; a pair hangs both down the front, so narrow pairs stay in their cells.
            AddUnit(b, x, r, body, p.Height, count == 1 ? Vector2.UnitX : -Vector2.UnitY, near);
        }
        if (v == 2 && near)
        {
            // Wall bracket: plate behind, strap round the body.
            var metal = PartMaterials.Metal(MepStyle.Aluminium);
            b.AddBox(new(-0.03f, body * 0.25f, p.Depth * 0.5f - 0.006f), new(0.03f, body * 0.78f, p.Depth * 0.5f), 0, metal, MepStyle.Flags);
            b.AddBox(new(-0.015f, body * 0.6f, r - 0.004f), new(0.015f, body * 0.66f, p.Depth * 0.5f - 0.004f), 0, metal, MepStyle.Flags);
            b.AddColumn(0, body * 0.6f, body * 0.66f, 0, r + 0.004f, RoofStyle.RoundSegmentsFine, metal, MepStyle.Flags);
        }
    }

    /// <param name="hose">Direction in X/Z (as X, Y) the hose hangs off the body.</param>
    private static void AddUnit(PartBuilder b, float x, float r, float body, float height, Vector2 hose, bool near)
    {
        var f = MepStyle.Flags;
        var segments = near ? RoofStyle.RoundSegmentsFine : RoofStyle.RoundSegmentsFar;
        var dome = r * 0.62f;
        LatheBand[] bands =
        [
            new(0, 0, r - 0.008f, 0, new(0, -1), new(0, -1)),
            new(r - 0.008f, 0, r, 0.012f, new(0, -1), new(1, 0)),
            new(r, 0.012f, r, body - dome, new(1, 0), new(1, 0)),
            new(r, body - dome, r * 0.72f, body - dome * 0.25f, new(1, 0), new(0.6f, 0.8f)),
            new(r * 0.72f, body - dome * 0.25f, r * 0.3f, body, new(0.6f, 0.8f), new(0.15f, 0.99f)),
            new(r * 0.3f, body, 0, body, new(0, 1), new(0, 1)),
        ];
        b.Add(PartShapes.Lathe(bands, segments, PartMaterials.Curved(MepStyle.FireRed)), Xf.Move(x, 0, 0), f);
        var dark = PartMaterials.Grille(MepStyle.Dark);
        var valve = height - body;
        b.AddBox(new(x - 0.017f, body - 0.004f, -0.022f), new(x + 0.017f, body + valve * 0.55f, 0.022f), near ? 0.004f : 0, dark, f);
        if (!near) return;
        b.AddColumn(x, body * 0.36f, body * 0.62f, 0, r + 0.002f, segments, PartMaterials.Curved(MepStyle.White), f);
        // Lever and carry handle, front to back over the valve; gauge facing front.
        b.AddCentredBox(new(0.024f, 0.01f, Math.Min(0.12f, r * 1.7f)), Xf.RotX(-8).Then(Xf.Move(x, body + valve * 0.82f, -0.004f)), dark, 0.002f, f);
        b.AddCentredBox(new(0.024f, 0.01f, Math.Min(0.1f, r * 1.4f)), Xf.Move(x, body + valve * 0.6f, 0.006f), dark, 0.002f, f);
        b.AddDisc(x, body + valve * 0.32f, -0.03f, -0.021f, 0.013f, 10, PartMaterials.Casing(MepStyle.White), f);
        // Hose from the valve down the body to a nozzle clipped at mid height.
        var hx = x + hose.X * (r + 0.011f); var hz = hose.Y * (r + 0.011f);
        var pipe = PartMaterials.Pipe(MepStyle.Dark);
        b.AddPipe(new(x + hose.X * 0.016f, body + valve * 0.25f, hose.Y * 0.016f), new(hx, body - dome * 0.4f, hz), 0.0065f, 8, pipe, f);
        b.AddPipe(new(hx, body - dome * 0.4f + 0.003f, hz), new(hx, body * 0.42f, hz), 0.0065f, 8, pipe, f);
        b.AddColumn(hx, body * 0.34f, body * 0.43f, hz, 0.009f, 8, pipe, f);
    }
}
