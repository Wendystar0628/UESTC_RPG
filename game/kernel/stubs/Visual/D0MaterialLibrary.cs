using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Style.Stubs;

/// <summary>D0-only shared material; authored colours live in a data file for Opus review.</summary>
public sealed class D0MaterialLibrary
{
    private readonly Dictionary<string, Color> _palette = ReadPalette();
    private ShaderMaterial? _material;
    public ShaderMaterial Material => _material ??= new ShaderMaterial
    {
        Shader = GD.Load<Shader>("res://style/shaders/toon_stub.gdshader")
    };
    public Color ColorOf(PaletteToken token) => _palette.GetValueOrDefault(token.Value, _palette["d0.building"]);
    private static Dictionary<string, Color> ReadPalette()
    {
        using var document = JsonDocument.Parse(Godot.FileAccess.GetFileAsBytes("res://data/d0/palette.json"));
        return document.RootElement.EnumerateObject().ToDictionary(p => p.Name,
            p => new Color(p.Value[0].GetSingle(), p.Value[1].GetSingle(), p.Value[2].GetSingle()), StringComparer.Ordinal);
    }
}
