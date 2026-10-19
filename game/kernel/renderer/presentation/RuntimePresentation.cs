using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>Engine-side material bridge; pipeline precompilation belongs to the engine.</summary>
public interface IToonMaterialProvider
{
    Material Get(ToonSpec spec);
    /// <summary>Creates an owned representative material for the build-time combination catalog.</summary>
    Material CreatePipelineMaterial(ToonKind kind);
}
