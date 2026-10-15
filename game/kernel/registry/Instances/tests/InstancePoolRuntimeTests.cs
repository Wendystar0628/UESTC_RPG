using Campus.Style.Stubs;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class InstancePoolRuntimeTests
{
    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void DynamicPoolOwnsAndReleasesEngineResources()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            throw new InvalidOperationException("The instance-pool test requires a Godot scene tree.");
        using var material = new D0MaterialLibrary().Material;
        var parent = new Node3D();
        try
        {
            tree.Root.AddChild(parent);
            InstancePoolRuntimeFixture.Run(parent, material);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(parent)) parent.Free();
        }
    }
}
