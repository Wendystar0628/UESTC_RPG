namespace Campus.Contracts.Bake;

public enum PipelineShadowMode : byte { Off, On, ShadowsOnly, DoubleSided }
public enum PipelineCanvasDraw : byte { None, Rect, TexturedRect, Polygon, MsdfText }

/// <summary>Build-time shader, vertex layout and render state used by the map or its runtime effects.</summary>
/// <remarks>Exactly one of Kind and Shader identifies the material. Reflect denotes a reflection proxy.</remarks>
public readonly record struct PipelineCombination(ToonKind? Kind, ShaderId? Shader, ulong Format,
    BakedPrimitive Primitive, bool Instanced, bool Reflect, PipelineShadowMode Shadows,
    PipelineCanvasDraw Canvas = PipelineCanvasDraw.None);
