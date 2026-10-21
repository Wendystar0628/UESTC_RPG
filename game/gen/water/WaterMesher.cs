using Campus.Contracts;
using System.Numerics;
using Campus.Gen.Geometry;

namespace Campus.Gen.Water;

/// <summary>Flat surface of one water body with its per-vertex shore distance and flow.</summary>
public sealed record WaterMesh(
    WaterBody Body,
    float[] Position,   // x, y, z per vertex
    float[] Shore,      // distance to the nearest shore (m), clamped to WaterParams.SdfClamp
    float[] Flow,       // flow velocity (vx, vz) in m/s; zero on lakes
    int[] Index)        // triangles, front face up (D0 winding: (b - a) x (c - a) points down)
{
    public int VertexCount => Position.Length / 3;
    public int TriangleCount => Index.Length / 3;
}

/// <summary>
/// Meshes a water polygon on a grid of WaterParams.MeshStep: cells fully inside water are plain quads, cells the shore
/// passes through are cut into trapezoids along the shore. The shore distance is stored per vertex, so the three hard
/// colour bands and the foam line (WLD-WATER) need no mask texture. Pure and deterministic (DECISION-165).
/// </summary>
public static class WaterMesher
{
    public static WaterMesh Build(WaterBody body, IReadOnlyList<WaterBody> all, WaterParams? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(all);
        var p = parameters ?? WaterParams.Default;
        var edges = WaterEdges.Edges(body);
        var topology = new WaterTopology(body.SurfaceY);
        WaterTessellation.Tessellate(body, edges, p.MeshStep, p.SliverFraction, topology);
        var (shore, flow) = WaterShoreField.Build(body, topology.Vertices, all, p);
        return new(body, GeometrySink.Positions(topology.Vertices), shore, flow, topology.Indices.ToArray());
    }
}
