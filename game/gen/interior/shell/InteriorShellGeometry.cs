using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Circulation;

namespace Campus.Gen.Interior.Shell;

/// <summary>Interior shell details composed from the shared geometry stream and circulation parts.</summary>
public static class InteriorShellGeometry
{
    public static void GlassGuard(this InteriorGeometry sink, Vector2 a, Vector2 b, float y, float height)
    {
        var frame = PlanFrame.Toward(a, b); var length = Vector2.Distance(a, b);
        var p = new RailingParams();
        if (sink.Draw)
        {
            var panel = CirculationStyle.PanelThickness;
            sink.Parts.Add(PartSolids.BeveledBox(length, height - CirculationStyle.TopRailHeight - CirculationStyle.BottomRailClearance,
                panel, 0, PartMaterials.Glazing()), frame.At(length * 0.5f, y + CirculationStyle.BottomRailClearance, 0),
                VertexFlag.Interior | VertexFlag.NoShadow);
            sink.Parts.Add(PartSolids.BeveledBox(length, CirculationStyle.TopRailHeight, p.Depth, CirculationStyle.RailBevel,
                CirculationStyle.Metal), frame.At(length * 0.5f, y + height - CirculationStyle.TopRailHeight, 0));
            var count = Math.Max(1, (int)MathF.Ceiling(length / p.PostSpacing));
            for (var i = 0; i <= count; i++)
                sink.Parts.Add(PartSolids.BeveledBox(CirculationStyle.PostSize, height - CirculationStyle.TopRailHeight,
                    CirculationStyle.PostSize, CirculationStyle.PostBevel, CirculationStyle.Metal), frame.At(length * i / count, y, 0));
        }
        sink.Collision.Add(new WallCollider(a, b, p.Depth * 0.5f, y, y + WalkRules.BarrierHeight, SurfaceTag.Barrier) { Foot = FootMat.Glass });
    }
}
