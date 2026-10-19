using Campus.Contracts;
using static GdUnit4.Assertions;
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
    private static readonly StringName _shaderInstanceFogExempt = InstanceShaderParameters.FogExempt;
    private static readonly StringName _shaderInstancePrecipD = InstanceShaderParameters.PrecipD;
    private static readonly StringName _shaderInstancePrecipK = InstanceShaderParameters.PrecipK;
    private static readonly StringName _shaderInstancePrecipV = InstanceShaderParameters.PrecipV;

    public static void Run(Node parent, ShaderMaterial material, InstancePoolHost host)
    {
        if (!parent.IsInsideTree()) throw new ArgumentException("Fixture parent must be in the scene tree.", nameof(parent));
        var spec = new InstancePoolSpec("fixture", "WP-00",
            [-1, -1, 0, 1, -1, 0, 1, 1, 0, -1, 1, 0],
            [0, 0, 1, 0, 1, 1, 0, 1], [0, 1, 2, 0, 2, 3],
            material, ShaderId.Precip, 8192, new Aabb(new(-100, -100, -100), new(200, 200, 200)), 1);
        try
        {
            {
                var thrown = false;
                try { host.Mount(spec); }
                catch (InvalidOperationException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
            }
            parent.AddChild(host);
            {
                var thrown = false;
                try { host.Mount(spec with { Capacity = int.MaxValue }); }
                catch (ArgumentOutOfRangeException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentOutOfRangeException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { host.Mount(spec with { Capacity = 0 }); }
                catch (ArgumentOutOfRangeException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentOutOfRangeException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { host.Mount(spec with { Position = [0, 1] }); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { host.Mount(spec with { Uv = [0, 0] }); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { host.Mount(spec with { Index = [0, 1, 4] }); }
                catch (ArgumentOutOfRangeException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentOutOfRangeException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { host.Mount(spec with { Index = [0, 1] }); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
            var badPosition = (float[])spec.Position.Clone();
            badPosition[0] = float.PositiveInfinity;
            {
                var thrown = false;
                try { host.Mount(spec with { Position = badPosition }); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { host.Mount(spec with { Layers = 1u << 20 }); }
                catch (ArgumentOutOfRangeException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentOutOfRangeException).Name}.").IsTrue();
            }
            AssertThat(host.GetChildCount() == 0 && host.GetStatistics().Count == 0).OverrideFailureMessage("Invalid input allocated a pool.").IsTrue();

            var pool = host.Mount(spec);
            {
                var thrown = false;
                try { host.Mount(spec); }
                catch (InvalidOperationException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
            }
            var node = host.GetChild<MultiMeshInstance3D>(0);
            var multiMesh = node.Multimesh;
            var mesh = multiMesh.Mesh;
            AssertThat(pool.Capacity == 8192 && pool.VisibleCount == 0 && !pool.IsReady && !node.Visible).OverrideFailureMessage("Initial pool state is wrong.").IsTrue();
            AssertThat(multiMesh.InstanceCount == 8192 && multiMesh.UseCustomData && !multiMesh.UseColors &&
                multiMesh.TransformFormat == MultiMesh.TransformFormatEnum.Transform3D).OverrideFailureMessage("Buffer format is wrong.").IsTrue();
            AssertThat(node.GetMeta("owner_wp").AsString() == spec.OwnerWp &&
                node.GetMeta("instance_pool_id").AsString() == spec.Id).OverrideFailureMessage("Instance attribution was lost.").IsTrue();
            AssertThat(node.MaterialOverride is null && ReferenceEquals(mesh.SurfaceGetMaterial(0), material)).OverrideFailureMessage("Borrowed surface material was replaced or cloned.").IsTrue();
            {
                var thrown = false;
                try { pool.VisibleCount = 1; }
                catch (InvalidOperationException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { pool.WriteBuffer(new float[16]); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
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
            {
                var thrown = false;
                try { pool.WriteBuffer(buffer); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
            buffer[^1] = 1;
            pool.WriteBuffer(buffer);
            AssertThat(pool.IsReady && !node.Visible).OverrideFailureMessage("Complete buffer upload must be ready while its requested prefix is empty.").IsTrue();
            AssertThat(multiMesh.GetBuffer().SequenceEqual(buffer)).OverrideFailureMessage("The native buffer lost transform or custom data.").IsTrue();
            pool.VisibleCount = 32;
            AssertThat(multiMesh.VisibleInstanceCount == 32).OverrideFailureMessage("Visible prefix did not reach Godot.").IsTrue();
            {
                var thrown = false;
                try { pool.VisibleCount = -1; }
                catch (ArgumentOutOfRangeException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentOutOfRangeException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { pool.VisibleCount = 8193; }
                catch (ArgumentOutOfRangeException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentOutOfRangeException).Name}.").IsTrue();
            }
            var stats = host.GetStatistics().Single();
            AssertThat(stats.OwnerWp == spec.OwnerWp && stats.Id == spec.Id && stats.VisibleTriangles == 64 &&
                stats.InstanceBufferBytes == 524288).OverrideFailureMessage("Ownership statistics are wrong.").IsTrue();

            var bounds = new Aabb(new(-200, -50, -200), new(400, 100, 400));
            pool.SetAabb(bounds);
            AssertThat(multiMesh.CustomAabb == bounds && node.CustomAabb == bounds).OverrideFailureMessage("Bounds did not reach both engine objects.").IsTrue();
            {
                var thrown = false;
                try { pool.SetAabb(new Aabb(new(0, 0, 0), new(-1, 1, 1))); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { pool.SetParameter(_shaderInstanceFogExempt, new Vector4(0, 0, 0, 0)); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { pool.SetParameter(_shaderInstancePrecipD, new Vector4(float.NaN, 0, 0, 1)); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
            var displacement = new Vector4(1, 2, 3, 18);
            pool.SetParameter(_shaderInstancePrecipD, displacement);
            pool.SetParameter(_shaderInstancePrecipV, new Vector4(0, -1, 0, .58f));
            pool.SetParameter(_shaderInstancePrecipK, new Vector4(0, 0, 0, 0));
            AssertThat(node.GetInstanceShaderParameter(_shaderInstancePrecipD).AsVector4() == displacement).OverrideFailureMessage("The registered vec4 did not reach the instance.").IsTrue();
            {
                var thrown = false;
                try { Task.Run(() => pool.VisibleCount = 1).GetAwaiter().GetResult(); }
                catch (InvalidOperationException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
            }

            pool.Free();
            pool.Free();
            {
                var thrown = false;
                try { _ = pool.IsReady; }
                catch (ObjectDisposedException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ObjectDisposedException).Name}.").IsTrue();
            }
            AssertThat(!GodotObject.IsInstanceValid(node) && !GodotObject.IsInstanceValid(multiMesh) &&
                !GodotObject.IsInstanceValid(mesh)).OverrideFailureMessage("Free retained an owned engine object.").IsTrue();
            AssertThat(host.GetStatistics().Count == 0 && host.GetChildCount() == 0 &&
                GodotObject.IsInstanceValid(material)).OverrideFailureMessage("Free changed the material owner or retained accounting.").IsTrue();
            {
                var thrown = false;
                try { pool.WriteBuffer(buffer); }
                catch (ObjectDisposedException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ObjectDisposedException).Name}.").IsTrue();
            }
            var next = host.Mount(spec);
            parent.RemoveChild(host);
            next.Free();
            AssertThat(host.GetStatistics().Count == 0 && GodotObject.IsInstanceValid(material)).OverrideFailureMessage("Tree exit retained a pool.").IsTrue();
            {
                var thrown = false;
                try { next.VisibleCount = 0; }
                catch (ObjectDisposedException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ObjectDisposedException).Name}.").IsTrue();
            }
            {
                var thrown = false;
                try { host.Mount(spec); }
                catch (InvalidOperationException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
            }
        }
        finally
        {
            if (GodotObject.IsInstanceValid(host)) host.Free();
        }
    }

}
