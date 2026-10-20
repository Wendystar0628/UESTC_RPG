using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Renderer;

/// <summary>Writes the six outward-facing camera planes without allocating a Godot array.</summary>
public static class CameraFrustum
{
    public const int PlaneCount = 6;

    public static void Fill(Camera3D camera, Span<N.Plane> destination) =>
        Fill(camera.GetCameraTransform(), camera.GetCameraProjection(), destination);

    /// <summary>Also serves a synthetic demand pose using the camera's projection and offsets.</summary>
    public static void Fill(Transform3D transform, Projection projection, Span<N.Plane> destination)
    {
        if (destination.Length != PlaneCount) throw new ArgumentException("A camera frustum needs six planes.", nameof(destination));
        for (var i = 0; i < destination.Length; i++)
        {
            var plane = transform * projection.GetProjectionPlane((Projection.Planes)i);
            destination[i] = new(new(plane.Normal.X, plane.Normal.Y, plane.Normal.Z), -plane.D);
        }
    }
}
