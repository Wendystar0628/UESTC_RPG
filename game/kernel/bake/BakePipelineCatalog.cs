using System.Collections.Concurrent;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;
using Campus.Contracts.Vegetation;
using Campus.Kernel.Renderer;
using Format = Godot.RenderingServer.ArrayFormat;

namespace Campus.Kernel.Bake;

/// <summary>Collects actual CPU products during the explicit offline batch, without retaining their materials or geometry.</summary>
internal sealed class BakePipelineCatalog
{
    private const string ResourcePath = "res://data/pipeline-combinations.json";
    private readonly ConcurrentDictionary<PipelineCombination, byte> _combinations = [];

    /// <summary>Reads the global shipped asset; player CPU cache state never participates in pipeline preparation.</summary>
    internal static IReadOnlyList<PipelineCombination> Read(IRequiredResources resources)
    {
        var bytes = resources.ReadBytes(ResourcePath);
        try
        {
            var combinations = JsonSerializer.Deserialize<PipelineCombination[]>(bytes, ContractJson.Options)
                ?? throw new InvalidDataException("Missing pipeline combinations in the build asset.");
            if (combinations.Length == 0) throw new InvalidDataException("The build pipeline catalogue is empty.");
            foreach (var combination in combinations) Validate(combination);
            return Array.AsReadOnly(combinations);
        }
        catch (Exception error) when (error is InvalidDataException or JsonException)
        { throw new AppStartupException("Build pipeline catalogue is invalid.", error); }
    }

    /// <summary>Called only after the explicit all-map bake has completed successfully.</summary>
    internal void Write(string projectRoot)
    {
        var combinations = Complete(ReadWhitelist(projectRoot));
        var options = new JsonSerializerOptions(ContractJson.Options) { WriteIndented = true, NewLine = "\n" };
        File.WriteAllBytes(Path.Combine(projectRoot, "data", "pipeline-combinations.json"),
            JsonSerializer.SerializeToUtf8Bytes(combinations, options));
    }

    internal static PipelineCombination[] Combinations(TileBundle bundle)
    {
        var result = new HashSet<PipelineCombination>();
        foreach (var surface in bundle.Surfaces) AddSurface(surface, false);
        foreach (var instances in bundle.MultiMeshes) AddSurface(instances.Surface, true);
        return Order(result);

        void AddSurface(BakedSurface surface, bool instanced)
        {
            var shadows = surface.NoShadow || surface.Shader is ShaderId.Water ||
                bundle.Level == LodLevel.L4 && surface.Shader == ShaderId.DioramaFar
                ? PipelineShadowMode.Off : PipelineShadowMode.On;
            result.Add(new(surface.Shader is null ? surface.Material.Spec.Kind : null, surface.Shader,
                surface.Format, surface.Primitive, instanced, false, shadows));
            if (surface.Reflect)
                result.Add(new(null, ShaderId.ReflectFlat, surface.Format, surface.Primitive,
                    instanced, true, PipelineShadowMode.Off));
        }
    }

    internal void Add(object product, BakeGeometryBuilder geometry)
    {
        switch (product)
        {
            case TileBundle tile:
                foreach (var combination in Combinations(tile)) Add(combination);
                break;
            case VegetationTileContent vegetation:
                foreach (var mother in vegetation.Mothers)
                    for (var slot = 0; slot < mother.Prototypes.Count; slot++)
                        foreach (var surface in mother.Prototypes[slot].Surfaces)
                            Add(new(surface.Shader is null ? surface.Material.Spec.Kind : null, surface.Shader,
                                GeometrySurfaceUpload.FormatOf(surface), BakedPrimitive.Triangles, true, false,
                                slot == 3 ? PipelineShadowMode.ShadowsOnly : PipelineShadowMode.Off));
                break;
            case FaunaTileContent fauna:
                foreach (var site in fauna.Content.Sites)
                    foreach (var output in site.Sets)
                    {
                        if (output.Instances.Count != 0)
                            throw new InvalidDataException("Fauna dynamic content cannot mount instance streams.");
                        foreach (var surface in geometry.ExpandMeshes(output, LodLevel.L0))
                        {
                            if (surface.Position.Length == 0 || surface.Index.Length == 0) continue;
                            var format = GeometrySurfaceUpload.FormatOf(surface);
                            var shadows = MaterialShaders.HasNoShadow(surface.Material.Spec.Kind, surface.Shader, surface.Flags)
                                ? PipelineShadowMode.Off : PipelineShadowMode.On;
                            Add(new(surface.Shader is null ? surface.Material.Spec.Kind : null, surface.Shader,
                                format, BakedPrimitive.Triangles, false, false, shadows));
                            if (surface.Reflect)
                                Add(new(null, ShaderId.ReflectFlat, format, BakedPrimitive.Triangles,
                                    false, true, PipelineShadowMode.Off));
                        }
                    }
                break;
        }
    }

    internal PipelineCombination[] Complete(IReadOnlySet<string> whitelist)
    {
        // These two producers construct fixed meshes outside TileBundle/auxiliary content.
        // InstancePoolHost raw pools upload only positions, UVs and indices, without compression.
        var raw = (ulong)(Format.FlagFormatVersion2 | Format.FormatVertex | Format.FormatTexUV | Format.FormatIndex);
        Add(new(null, ShaderId.FxGlow, raw, BakedPrimitive.Triangles, true, false, PipelineShadowMode.Off));
        Add(new(null, ShaderId.Precip, raw, BakedPrimitive.Triangles, true, false, PipelineShadowMode.Off));
        // CanvasItem shaders have no spatial vertex format. Their production controls provide the draw type.
        AddCanvas(ShaderId.UiPanel, PipelineCanvasDraw.Rect);
        AddCanvas(ShaderId.UiPanel, PipelineCanvasDraw.TexturedRect);
        AddCanvas(ShaderId.UiPanel, PipelineCanvasDraw.Polygon);
        AddCanvas(ShaderId.UiPanel, PipelineCanvasDraw.MsdfText);
        AddCanvas(ShaderId.UiScreen, PipelineCanvasDraw.Rect);
        AddCanvas(ShaderId.MapBase, PipelineCanvasDraw.Rect);
        var result = Order(_combinations.Keys);
        foreach (var item in result)
        {
            Validate(item);
            var path = ShaderPath(item);
            if (!whitelist.Contains(path))
                throw new InvalidDataException($"Pipeline shader is absent from the build whitelist: {path}.");
        }
        return result;
    }

    private void Add(PipelineCombination combination) => _combinations.TryAdd(combination, 0);
    private void AddCanvas(ShaderId shader, PipelineCanvasDraw draw) =>
        Add(new(null, shader, 0, BakedPrimitive.Triangles, false, false, PipelineShadowMode.Off, draw));

    internal static PipelineCombination[] Order(IEnumerable<PipelineCombination> values) => values
        .Distinct().OrderBy(value => value.Kind).ThenBy(value => value.Shader).ThenBy(value => value.Format)
        .ThenBy(value => value.Primitive).ThenBy(value => value.Instanced).ThenBy(value => value.Reflect)
        .ThenBy(value => value.Shadows).ThenBy(value => value.Canvas).ToArray();

    internal static void Validate(PipelineCombination value)
    {
        if ((value.Kind is null) == (value.Shader is null) ||
            value.Kind is { } kind && !Enum.IsDefined(kind) || value.Shader is { } shader && !Enum.IsDefined(shader) ||
            !Enum.IsDefined(value.Primitive) || !Enum.IsDefined(value.Shadows) || !Enum.IsDefined(value.Canvas))
            throw new InvalidDataException("Invalid baked pipeline combination.");
        if (value.Reflect && (value.Shader != ShaderId.ReflectFlat || value.Shadows != PipelineShadowMode.Off))
            throw new InvalidDataException("Reflection pipelines must use the non-shadowing reflection shader.");
        var canvas = value.Shader is ShaderId.UiPanel or ShaderId.UiScreen or ShaderId.MapBase;
        if (canvas != (value.Format == 0) || canvas != (value.Canvas != PipelineCanvasDraw.None) ||
            canvas && (value.Instanced || value.Reflect || value.Shadows != PipelineShadowMode.Off))
            throw new InvalidDataException("Pipeline vertex format does not match its shader draw type.");
    }

    private static string ShaderPath(PipelineCombination value) =>
        "game/" + MaterialShaders.FileOf(value.Shader ?? MaterialShaders.ShaderFor(value.Kind
            ?? throw new InvalidDataException("Pipeline has no shader family.")));

    internal static IReadOnlySet<string> ReadWhitelist(string projectRoot)
    {
        using var source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(projectRoot, "..", "scripts", "gates", "shader-whitelist.json")));
        return source.RootElement.EnumerateArray()
            .Select(value => value.GetString() ?? throw new InvalidDataException("Empty shader whitelist path."))
            .ToHashSet(StringComparer.Ordinal);
    }
}
