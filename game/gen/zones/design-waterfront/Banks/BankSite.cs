using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using T = Campus.Gen.Zones.DesignWaterfront.WaterfrontTune;

namespace Campus.Gen.Zones.DesignWaterfront.Banks;

/// <summary>Concrete shore wall, continuous waterline band, bevelled coping and open water-access steps.</summary>
internal sealed class BankSite : ZoneSite
{
    private static readonly StepsPart Steps = new();
    public override string Id => "28b.registered-banks";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) => BankPlan.Ranges(world);

    public override void Build(SiteBuilder builder)
    {
        if (builder.Lod == LodLevel.L4) return;
        foreach (var bank in BankPlan.All(builder.Context.World, builder.Includes))
        {
            foreach (var piece in bank.Pieces(builder.Includes))
            {
                var frame = PlanFrame.Toward(piece.A, piece.B);
                var length = Vector2.Distance(piece.A, piece.B);
                var bottom = bank.WaterY - T.BankSubmersion - T.BankRiser;
                var wetTop = bank.WaterY + T.BankWetHeight;
                var wall = new PartBuilder();
                AddPanel(wall, frame, length, bottom, bottom, bank.WaterY, bank.WaterY,
                    T.BankCopingWidth - T.BankCopingOverhang, 0, T.BankWallSpec);
                AddPanel(wall, frame, length, bank.WaterY, bank.WaterY, Math.Min(wetTop, piece.TopA), Math.Min(wetTop, piece.TopB),
                    T.BankCopingWidth - T.BankCopingOverhang, 0, T.BankWetSpec);
                AddPanel(wall, frame, length, Math.Min(wetTop, piece.TopA), Math.Min(wetTop, piece.TopB), piece.TopA, piece.TopB,
                    T.BankCopingWidth - T.BankCopingOverhang, 0, T.BankWallSpec);
                AddPanel(wall, frame, length, piece.TopA, piece.TopB, piece.TopA + T.BankCopingThickness, piece.TopB + T.BankCopingThickness,
                    T.BankCopingWidth, builder.Lod == LodLevel.L0 ? Math.Min(T.BankCopingBevel, length / 4) : 0, T.KerbSpec, T.BankCopingOverhang);
                builder.Add(wall.Finish(Bounds(wall)), Matrix4x4.Identity, (piece.A + piece.B) / 2);
            }
            foreach (var exit in bank.Exits)
                builder.Add(Steps.Lods[builder.Lod](exit.Steps), exit.At, exit.Frame.Origin);
        }
    }

    public override void Collide(SiteColliders colliders)
    {
        foreach (var bank in BankPlan.All(colliders.Context.World, colliders.Includes))
        {
            foreach (var piece in bank.Pieces(colliders.Includes))
            {
                var frame = PlanFrame.Toward(piece.A, piece.B);
                // The terrain owns the bank slope. This narrow walkable top matches the visible coping.
                var land = -(T.BankCopingWidth - T.BankCopingOverhang);
                var width = Vector2.Distance(piece.A, piece.B);
                Vector3 P(float x, float z, float y) { var p = frame.P(x, z); return new(p.X, y, p.Y); }
                colliders.Add(new RampCollider(P(0, land, piece.TopA + T.BankCopingThickness),
                    P(0, T.BankCopingOverhang, piece.TopA + T.BankCopingThickness),
                    P(width, T.BankCopingOverhang, piece.TopB + T.BankCopingThickness),
                    P(width, land, piece.TopB + T.BankCopingThickness), T.BankCopingThickness, SurfaceTag.Ground)
                    { Foot = T.StoneFoot }, (piece.A + piece.B) / 2);
            }
            foreach (var exit in bank.Exits)
            {
                foreach (var collider in Steps.Collision(exit.Steps))
                {
                    var placed = GeometryPlacement.Transform(collider, exit.At);
                    if (placed is RampCollider { Stair: { } spec } ramp)
                    {
                        var nosing = GeometryPlacement.Point(new(-exit.Steps.Width / 2, spec.RiserM, -exit.Steps.Depth / 2), exit.At);
                        var water = exit.Frame.W;
                        placed = ramp with { Treads = new StairFrame(nosing, new(-water.X, 0, -water.Y), Vector3.UnitY, exit.Steps.Width) };
                    }
                    colliders.Add(placed);
                }
            }
        }
    }

    private static Box3 Bounds(PartBuilder builder)
    {
        var (min, max) = builder.Measure();
        return new(min, max);
    }

    /// <summary>Extrude a vertical profile back onto land. A coping alone overhangs toward water.</summary>
    private static void AddPanel(PartBuilder builder, PlanFrame frame, float length, float bottomA, float bottomB,
        float topA, float topB, float depth, float bevel, ToonSpec material, float overhang = 0)
    {
        if (topA <= bottomA && topB <= bottomB) return;
        var profile = new Vector2[] { new(0, bottomA), new(length, bottomB), new(length, topB), new(0, topA) };
        var stand = new Matrix4x4(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, overhang, 1);
        builder.Add(PartGeometryKernel.BeveledExtrude(profile, depth, bevel, material), GeometryPlacement.Multiply(stand, frame.At(0, 0, 0)));
    }
}
