using Campus.Kernel.Instances;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Tests;

/// <summary>
/// Invoke Run from an engine-hosted test on the Godot main thread, with an in-tree parent and
/// a caller-owned spatial material declaring the three registered precipitation instance uniforms.
/// This is intentionally not a plain dotnet test: it creates and inspects real engine resources.
/// The fixture does not author shaders or dispose the supplied material.
/// </summary>
public static class InstancePoolRuntimeFixture
{
    public static void Run(Node parent, ShaderMaterial material)
    {
        if (!parent.IsInsideTree()) throw new ArgumentException("Fixture parent must be in the scene tree.", nameof(parent));
        var spec = new InstancePoolSpec("fixture", "WP-00",
            [-1, -1, 0, 1, -1, 0, 1, 1, 0, -1, 1, 0],
            [0, 0, 1, 0, 1, 1, 0, 1], [0, 1, 2, 0, 2, 3],
            material, 8192, new Aabb(new(-100, -100, -100), new(200, 200, 200)), 1);
        var host = new InstancePoolHost();
        try
        {
            Throws<InvalidOperationException>(() => host.Mount(spec));
            parent.AddChild(host);
            Throws<ArgumentOutOfRangeException>(() => host.Mount(spec with { Capacity = int.MaxValue }));
            Throws<ArgumentOutOfRangeException>(() => host.Mount(spec with { Capacity = 0 }));
            Throws<ArgumentException>(() => host.Mount(spec with { Position = [0, 1] }));
            Throws<ArgumentException>(() => host.Mount(spec with { Uv = [0, 0] }));
            Throws<ArgumentOutOfRangeException>(() => host.Mount(spec with { Index = [0, 1, 4] }));
            Throws<ArgumentException>(() => host.Mount(spec with { Index = [0, 1] }));
            var badPosition = (float[])spec.Position.Clone();
            badPosition[0] = float.PositiveInfinity;
            Throws<ArgumentException>(() => host.Mount(spec with { Position = badPosition }));
            Throws<ArgumentOutOfRangeException>(() => host.Mount(spec with { Layers = 1u << 20 }));
            Check(host.GetChildCount() == 0 && host.GetStatistics().Count == 0, "Invalid input allocated a pool.");

            var pool = host.Mount(spec);
            Throws<InvalidOperationException>(() => host.Mount(spec));
            var node = host.GetChild<MultiMeshInstance3D>(0);
            var multiMesh = node.Multimesh;
            var mesh = multiMesh.Mesh;
            Check(pool.Capacity == 8192 && pool.VisibleCount == 0, "Initial pool state is wrong.");
            Check(multiMesh.InstanceCount == 8192 && multiMesh.UseCustomData && !multiMesh.UseColors &&
                multiMesh.TransformFormat == MultiMesh.TransformFormatEnum.Transform3D, "Buffer format is wrong.");
            Check(node.GetMeta("owner_wp").AsString() == spec.OwnerWp &&
                node.GetMeta("instance_pool_id").AsString() == spec.Id, "Instance attribution was lost.");
            Check(ReferenceEquals(node.MaterialOverride, material), "Material was replaced or cloned.");
            Throws<InvalidOperationException>(() => pool.VisibleCount = 1);
            Throws<ArgumentException>(() => pool.WriteBuffer(new float[16]));
            var buffer = new float[8192 * 16];
            for (var i = 0; i < 8192; i++)
            {
                var offset = i * 16;
                buffer[offset] = buffer[offset + 5] = buffer[offset + 10] = 1;
                buffer[offset + 3] = i * .01f;
                buffer[offset + 12] = .25f;
                buffer[offset + 13] = .5f;
                buffer[offset + 14] = .75f;
                buffer[offset + 15] = 1;
            }
            buffer[^1] = float.NaN;
            Throws<ArgumentException>(() => pool.WriteBuffer(buffer));
            buffer[^1] = 1;
            pool.WriteBuffer(buffer);
            Check(multiMesh.GetBuffer().SequenceEqual(buffer), "The native buffer lost transform or custom data.");
            pool.VisibleCount = 32;
            Check(multiMesh.VisibleInstanceCount == 32, "Visible prefix did not reach Godot.");
            Throws<ArgumentOutOfRangeException>(() => pool.VisibleCount = -1);
            Throws<ArgumentOutOfRangeException>(() => pool.VisibleCount = 8193);
            var stats = host.GetStatistics().Single();
            Check(stats.OwnerWp == spec.OwnerWp && stats.Id == spec.Id && stats.VisibleTriangles == 64 &&
                stats.InstanceBufferBytes == 524288, "Ownership statistics are wrong.");

            var bounds = new Aabb(new(-200, -50, -200), new(400, 100, 400));
            pool.SetAabb(bounds);
            Check(multiMesh.CustomAabb == bounds && node.CustomAabb == bounds, "Bounds did not reach both engine objects.");
            Throws<ArgumentException>(() => pool.SetAabb(new Aabb(new(0, 0, 0), new(-1, 1, 1))));
            Throws<ArgumentException>(() => pool.SetParameter(InstanceShaderNames.FogExempt, new(0, 0, 0, 0)));
            Throws<ArgumentException>(() => pool.SetParameter(InstanceShaderNames.PrecipD, new(float.NaN, 0, 0, 1)));
            var displacement = new Vector4(1, 2, 3, 18);
            pool.SetParameter(InstanceShaderNames.PrecipD, displacement);
            pool.SetParameter(InstanceShaderNames.PrecipV, new(0, -1, 0, .58f));
            pool.SetParameter(InstanceShaderNames.PrecipK, new(0, 0, 0, 0));
            Check(node.GetInstanceShaderParameter(InstanceShaderNames.PrecipD).AsVector4() == displacement,
                "The registered vec4 did not reach the instance.");
            Throws<InvalidOperationException>(() => Task.Run(() => pool.VisibleCount = 1).GetAwaiter().GetResult());

            pool.Free();
            pool.Free();
            Check(!GodotObject.IsInstanceValid(node) && !GodotObject.IsInstanceValid(multiMesh) &&
                !GodotObject.IsInstanceValid(mesh), "Free retained an owned engine object.");
            Check(host.GetStatistics().Count == 0 && host.GetChildCount() == 0 &&
                GodotObject.IsInstanceValid(material), "Free changed the material owner or retained accounting.");
            Throws<ObjectDisposedException>(() => pool.WriteBuffer(buffer));
            var next = host.Mount(spec);
            parent.RemoveChild(host);
            next.Free();
            Check(host.GetStatistics().Count == 0 && GodotObject.IsInstanceValid(material), "Tree exit retained a pool.");
            Throws<ObjectDisposedException>(() => next.VisibleCount = 0);
            Throws<InvalidOperationException>(() => host.Mount(spec));
        }
        finally
        {
            if (GodotObject.IsInstanceValid(host)) host.Free();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action operation) where T : Exception
    {
        try { operation(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
