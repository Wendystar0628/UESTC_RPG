using Campus.Style.Materials;
using Campus.Style.Palette;
using Campus.Contracts;
using Campus.Kernel.Instances;
using Campus.Kernel.Renderer;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class InstancePoolRuntimeTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task DynamicPoolOwnsAndReleasesEngineResources()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            throw new InvalidOperationException("The instance-pool test requires a Godot scene tree.");
        using var textures = new GlobalDataTextureRegistry();
        try
        {
            var source = new ShaderMaterialSource();
            var palettes = new PaletteRegistrySource();
            using var provider = new ToonLibrary(palettes, source, textures);
            using var material = source.Create(ShaderId.Precip);
            var parent = new Node3D();
            try
            {
                tree.Root.AddChild(parent);
                InstancePoolRuntimeFixture.Run(parent, material, new InstancePoolHost(provider, source, palettes));
            }
            finally
            {
                if (GodotObject.IsInstanceValid(parent)) parent.Free();
            }
        }
        finally
        {
            await textures.ShutdownAsync();
        }
    }
}
