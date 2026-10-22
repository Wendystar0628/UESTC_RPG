using Campus.Kernel.Boot;
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
    // 本组建的着色器与材质要等包装终结才归还原生引用；整组跑完在这里收齐，单跑本组时才不漏到引擎退出（DroppedNatives）。
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

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
            var source = new ShaderMaterialSource(new RequiredResources());
            var palettes = new PaletteRegistrySource(new RequiredResources());
            using var provider = new ToonLibrary(palettes, source, textures, new RequiredResources());
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
