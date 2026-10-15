using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Stubs.Visual;

public sealed class D0RenderHost(Node3D stage) : IRenderHost
{
    private readonly Dictionary<ulong, Node3D> _roots = [];
    private readonly Campus.Style.Stubs.D0MaterialLibrary _materials = new();
    private ulong _next;
    public RenderHandle Mount(ContentOutput content, WpId owner)
    {
        var root = new Node3D { Name = $"D0_{owner.Value}_{++_next}" };
        foreach (var geometry in content.Meshes)
        {
            if (geometry.Index.Length == 0) continue;
            var vertices = new Vector3[geometry.Position.Length / 3];
            var normals = new Vector3[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new(geometry.Position[3 * i], geometry.Position[3 * i + 1], geometry.Position[3 * i + 2]);
                normals[i] = new(geometry.Normal[3 * i], geometry.Normal[3 * i + 1], geometry.Normal[3 * i + 2]);
            }
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = vertices;
            arrays[(int)Mesh.ArrayType.Normal] = normals;
            arrays[(int)Mesh.ArrayType.Index] = geometry.Index;
            arrays[(int)Mesh.ArrayType.Color] = Enumerable.Repeat(_materials.ColorOf(geometry.Material.Spec.Lit), vertices.Length).ToArray();
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            root.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = _materials.Material });
        }
        stage.AddChild(root);
        _roots.Add(_next, root);
        return new(_next);
    }
    public void Unmount(RenderHandle handle)
    {
        if (_roots.Remove(handle.Value, out var root)) root.QueueFree();
    }
}
