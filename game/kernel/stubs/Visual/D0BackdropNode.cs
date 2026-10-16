using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Stubs.Visual;

/// <summary>Explicitly enabled by D0Stage; a formal streamer owns its own resident set.</summary>
[KernelNode("d0.backdrop", Owner = "WP-00")]
public sealed partial class D0BackdropNode : Node, IMapAware
{
    private D0BackdropResident? _resident;
    private Camera3D? _camera;
    private IMapContext? _active;
    public Task MountTask => _resident?.Pending ?? Task.CompletedTask;
    public string Evidence => $"map={_active?.Descriptor.Id.Value} far={_camera?.Far} residentReady={_resident?.Ready} surfaces={_resident?.SurfaceCount} triangles={_resident?.Triangles}";

    public void Configure(IRenderHost renderer, Camera3D camera)
    {
        if (_resident is not null) throw new InvalidOperationException("D0 backdrop is already configured.");
        _camera = camera;
        _resident = new(renderer, error => GD.PushError($"WP-00 discarded backdrop worker failed: {error}"));
    }

    public void OnMapLoaded(IMapContext context)
    {
        if (_resident is null || ReferenceEquals(_active, context)) return;
        _resident.Unload();
        _active = null;
        var camera = _camera ?? throw new InvalidOperationException("D0 backdrop camera is missing.");
        var far = BackdropInputs.CameraFar(context.Descriptor, context.SourceInputs, camera.Far, camera.Near);
        var registration = Services.Services.ForMap(context.Descriptor.Id.Value)
            .Single(entry => entry.Kind == "content" && entry.Id == "diorama");
        // The approved diorama has a parameterless CPU-only constructor. A fresh instance belongs to this map epoch,
        // rather than the registry's cached module (which can retain geometry for a previous input snapshot).
        if (registration.Type.GetConstructor(Type.EmptyTypes) is null ||
            typeof(GodotObject).IsAssignableFrom(registration.Type))
            throw new InvalidOperationException("D0 diorama requires a parameterless CPU content module.");
        var module = (IContentModule)(Activator.CreateInstance(registration.Type)
            ?? throw new InvalidOperationException("Cannot construct the D0 diorama module."));
        var build = new BuildContext
        {
            Map = context.Descriptor,
            World = context.World,
            WorldSeed = context.Descriptor.Seed,
            SourceInputs = context.SourceInputs.Entries,
            GlobalInputs = Services.GlobalBuildInputs
        };
        camera.Far = far;
        _active = context;
        _resident.Load(build, module, new(registration.Owner));
    }

    public void OnMapUnloading(MapId from)
    {
        if (_active?.Descriptor.Id != from) return;
        _active = null;
        _resident?.Unload();
    }

    public override void _ExitTree()
    {
        _active = null;
        _resident?.Dispose();
    }
}
