using Campus.Contracts;
using Godot;

namespace Campus.Contracts;

/// <summary>Creates materials backed by the approved shader whitelist.</summary>
public interface IShaderMaterialSource
{
    /// <summary>
    /// Returns a new material. Callers may set uniforms but must not replace its shader,
    /// change render modes or change shader definitions.
    /// </summary>
    ShaderMaterial Create(ShaderId shader);
}

/// <summary>Engine-side material bridge; pipeline precompilation belongs to the engine.</summary>
public interface IToonMaterialProvider
{
    Material Get(ToonSpec spec);
    /// <summary>Creates an owned representative material for the build-time combination catalog.</summary>
    Material CreatePipelineMaterial(ToonKind kind);
}
