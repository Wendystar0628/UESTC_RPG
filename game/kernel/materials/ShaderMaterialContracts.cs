using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Materials;

/// <summary>Creates materials backed by the approved non-toon shader whitelist.</summary>
public interface IShaderMaterialSource
{
    /// <summary>
    /// Returns a new material. Callers may set uniforms but must not replace its shader,
    /// change render modes or change shader definitions.
    /// </summary>
    ShaderMaterial Create(ShaderId shader);
}
