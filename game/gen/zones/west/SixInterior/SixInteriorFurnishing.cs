using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Parts.Roof.Mep;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using S = Campus.Gen.Zones.West.WestTune.Six;

namespace Campus.Gen.Zones.West.SixInterior;

/// <summary>The documented white equations and flask, with existing campus equipment and closed service doors.</summary>
internal static class SixInteriorFurnishing
{
    private static readonly string[] Equations = ["E = mc²", "F = ma", "U = IR", "P = UI", "a² + b² = c²"];
    private static readonly ToonSpec Ink = PartMaterials.Grille("glass");

    public static void Hall(BuildContext ctx, SixInteriorGeometry sink, SixInteriorPlan p)
    {
        var rise = S.BaseStorey / 30;
        var tread = p.FormulaFlightRun / 15;
        for (var flight = 0; flight < 2; flight++)
            for (var step = 0; step < 15; step++)
            {
                var z = -p.FormulaRun * 0.5f + flight * (p.FormulaFlightRun + p.FormulaLanding) + step * tread;
                // The catalogue stair's first strip is lower floor; its first visible riser is one tread in.
                var y = p.GroundY + flight * S.BaseStorey * 0.5f + step * rise + rise * 0.5f;
                Text(ctx, sink, Equations[(flight * 15 + step) % Equations.Length], rise * 0.5f,
                    SixInteriorProvider.WallMaterial, SixInteriorProvider.WoodMaterial,
                    p.FormulaFrame.At(0, y, z + tread));
            }
        // The existing library flask recipe: stainless body, rolled rim and separate cap; no collision for the trace.
        var flask = new PartBuilder();
        var steel = PartMaterials.Metal("roof.silverMetal");
        flask.AddLathe(PartShapes.Creased(new(0, 0), new(0.036f, 0), new(0.036f, 0.21f), new(0, 0.21f)),
            14, steel, Matrix4x4.Identity, VertexFlag.Interior | VertexFlag.NoShadow);
        flask.AddLathe(PartShapes.Creased(new(0, 0.21f), new(0.039f, 0.21f), new(0.039f, 0.255f), new(0, 0.255f)),
            14, steel, Matrix4x4.Identity, VertexFlag.Interior | VertexFlag.NoShadow);
        var (min, max) = flask.Measure();
        sink.Add(flask.Finish(new(min, max)), p.FormulaFrame.At(-p.FormulaWidth * 0.25f,
            p.GroundY + S.BaseStorey * 0.5f, 0), VertexFlag.Interior | VertexFlag.NoShadow);

        var data = ctx.World.Feature(S.EntityC).RequiredProps().GetProperty("interior");
        var service = data.GetProperty("serviceFace").EnumerateArray().Select(v => new Vector2(v[0].GetSingle(), v[1].GetSingle())).ToArray();
        var wall = PlanFrame.Toward(service[0], service[1]);
        var length = Vector2.Distance(service[0], service[1]);
        var door = new DoorParams(0.95f, WalkRules.DefaultDoorHeight, 0.2f);
        sink.Part(new FireDoorPart(), door, wall.At(length * 0.25f, p.GroundY, 0));
        sink.Part(new ElevatorDoorPart(), new DoorParams(1.2f, 2.2f, 0.1f), wall.At(length * 0.75f, p.GroundY, 0));
        Text(ctx, sink, "报告厅", 0.15f, Ink, SixInteriorProvider.WallMaterial,
            wall.At(length * 0.25f, p.GroundY + door.Height + 0.15f, -0.11f));
        Text(ctx, sink, "电梯厅", 0.15f, Ink, SixInteriorProvider.WallMaterial,
            wall.At(length * 0.75f, p.GroundY + 2.2f + 0.15f, -0.11f));
        sink.Part(new BenchPart(), new FurnitureParams(), wall.At(length * 0.5f, p.GroundY, -0.4f));
        sink.Part(new DirectorySignPart(), new SignParams(0.6f, 1f, 0.04f), wall.At(length * 0.5f, p.GroundY + 1.2f, -0.13f));
        sink.Part(new HydrantPart(), new EquipmentParams(0.7f, 1f, 0.2f), wall.At(0.4f, p.GroundY + 0.8f, -0.13f));
        Text(ctx, sink, "基础与前沿研究院", 0.3f, Ink, SixInteriorProvider.WallMaterial,
            wall.At(length * 0.5f, p.GroundY + S.BaseStorey - WalkRules.DefaultDoorHeight * 0.5f, -0.11f));
    }

    public static void PublicBay(BuildContext ctx, SixInteriorGeometry sink, SixInteriorPlan p)
    {
        var data = ctx.World.Feature(S.EntityC).RequiredProps().GetProperty("interior").GetProperty("publicB");
        var a = new Vector2(data[2][0].GetSingle(), data[2][1].GetSingle());
        var b = new Vector2(data[3][0].GetSingle(), data[3][1].GetSingle());
        var frame = PlanFrame.Toward(a, b);
        var length = Vector2.Distance(a, b);
        sink.Part(new FireDoorPart(), new DoorParams(0.95f, WalkRules.DefaultDoorHeight, 0.2f), frame.At(length * 0.5f, p.UpperY, 0));
        sink.Part(new ExitSignPart(), new SignParams(0.4f, 0.15f, 0.04f), frame.At(length * 0.5f, p.UpperY + WalkRules.DefaultDoorHeight, -0.13f));
    }

    private static void Text(BuildContext ctx, SixInteriorGeometry sink, string text, float height,
        ToonSpec ink, ToonSpec background, Matrix4x4 at)
    {
        var atlas = new SceneTextAtlas(ctx.GlobalInputs.Require(SceneTextInputs.AtlasKey));
        var builder = new PartBuilder();
        Lettering.Write(builder, atlas.Layout(text), ctx.Catalog.Resolve(background.Lit), 0, 0, 0, false, height, ink);
        var (min, max) = builder.Measure();
        sink.Add(builder.Finish(new(min, max)), at, VertexFlag.Interior | VertexFlag.NoShadow);
    }
}
