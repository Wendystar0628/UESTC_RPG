using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>Reads ArrayMesh's cached 64-bit surface metadata without downloading GPU surface buffers.</summary>
public static class MeshSurfaceFormat
{
    private static readonly StringName SurfaceGetFormatMethod = new("surface_get_format");

    public static ulong Read(ArrayMesh mesh, int surface = 0)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (!GodotObject.IsInstanceValid(mesh)) throw new ObjectDisposedException(nameof(mesh));
        if (surface < 0 || surface >= mesh.GetSurfaceCount()) throw new ArgumentOutOfRangeException(nameof(surface));
        // The typed SurfaceGetFormat binding drops format-version bits in the locked 4.7.2 runtime.
        using var format = mesh.Call(SurfaceGetFormatMethod, surface);
        if (format.VariantType != Variant.Type.Int)
            throw new InvalidOperationException($"Surface {surface} did not expose an integer format.");
        return format.AsUInt64();
    }
}
