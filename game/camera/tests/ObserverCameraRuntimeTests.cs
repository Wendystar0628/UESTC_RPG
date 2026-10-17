using Campus.Camera;
using Campus.Contracts;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class ObserverCameraRuntimeTests
{
    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void MapMountKeepsPresentationPoseAndDoesNotRequireAnInlineSpawn()
    {
        if (Engine.GetMainLoop() is not SceneTree)
            throw new InvalidOperationException("The camera map-mount regression requires a real Godot scene tree.");
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("The camera map-mount regression must run on Godot's main thread.");

        // Empty inline Spawns is valid when DefaultSpawn names a separate waypoint.
        var context = FlightBoundsTests.CreateContext();
        var camera = new ObserverCamera();
        try
        {
            // Invoke the production callback against native Node/RenderingServer resources.
            // This isolated regression does not enter _Ready or claim full startup coverage.
            var presentation = new Pose(new(400, 500, 3100), 13, -2.5f, 38);
            camera.SetPose(presentation);
            camera.OnMapLoaded(context);
            if (camera.Pose != presentation)
                throw new InvalidOperationException("MapAware replaced the presentation pose before MapService selected the destination.");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(camera)) camera.Free();
        }
    }
}
