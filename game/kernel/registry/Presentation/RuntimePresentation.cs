using Campus.Contracts;
using Campus.Contracts.Bake;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>Requested deformation path; declaration alone does not establish renderer support or warm readiness.</summary>
public enum WarmDeformation : byte { None = 0, Skin4 = 1 }

/// <summary>The actual uploaded vertex format, instance mode and primitive topology requested for a material.</summary>
public readonly record struct WarmDrawLayout(ulong Format, bool Instanced, BakedPrimitive Primitive = BakedPrimitive.Triangles)
{
    /// <summary>Skin4 requires four bone influences and an actual skin/skeleton binding; unsupported consumers must reject it.</summary>
    public WarmDeformation Deformation { get; init; } = WarmDeformation.None;
}

/// <summary>A borrowed material for visible prewarming, identified by exactly one ToonSpec or ShaderId.</summary>
public sealed record ToonWarmMaterial
{
    public ToonWarmMaterial(ToonSpec? Spec, Material Material, ShaderId? Shader = null,
        IReadOnlyList<WarmDrawLayout>? Layouts = null)
    {
        if ((Spec is null) == (Shader is null))
            throw new ArgumentException("A warm material must identify exactly one ToonSpec or ShaderId.");
        ArgumentNullException.ThrowIfNull(Material);
        this.Spec = Spec;
        this.Material = Material;
        this.Shader = Shader;
        this.Layouts = Array.AsReadOnly(Layouts?.ToArray() ?? Array.Empty<WarmDrawLayout>());
    }
    public ToonSpec? Spec { get; }
    public Material Material { get; }
    public ShaderId? Shader { get; }
    public IReadOnlyList<WarmDrawLayout> Layouts { get; }
}

/// <summary>A provider-issued snapshot; callers must return this same batch after collecting real draw evidence.</summary>
public sealed record ToonWarmBatch(long Revision, IReadOnlyList<ToonWarmMaterial> Materials);

/// <summary>Engine-side material bridge. WP-04 owns shader creation, palette IDs and pipeline readiness.</summary>
public interface IToonMaterialProvider
{
    Material Get(ToonSpec spec);
    byte PaletteId(PaletteToken token);
    /// <summary>True only when every layout actually requested for the resolved material has valid draw evidence.</summary>
    bool IsReady(ToonSpec spec);
    /// <summary>Track this exact non-toon material instance. Its creator retains ownership and disposal responsibility.</summary>
    void Track(ShaderId shader, Material material);
    /// <summary>Register an actual layout for this borrowed material; a new layout advances the provider revision.</summary>
    void TrackLayout(Material material, WarmDrawLayout layout);
    /// <summary>Readiness covers every requested layout of this actual material, never all materials sharing a ShaderId.</summary>
    bool IsReady(ShaderId shader, Material material);
    ToonWarmBatch CapturePendingWarm();
    /// <summary>Accept only an issued, current batch with its exact material set; stale or forged evidence returns false.</summary>
    bool CompleteWarm(ToonWarmBatch batch);
}

/// <summary>WP-01 owns timing/content; WP-05 draws depth fog followed by the layer-5 opaque cover.</summary>
public readonly record struct VeilFrame(float Opacity, VeilContent Content, Texture2D? Frontispiece);
public interface IVeilPresentation
{
    /// <summary>True only while the currently presented full-viewport layer is fully opaque.</summary>
    bool IsCovered { get; }
    void Present(in VeilFrame frame);
}
