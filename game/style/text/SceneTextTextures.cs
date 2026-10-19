using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Style.Materials;

/// <summary>The material library owns the shared MSDF array, loaded in the metadata's page order.</summary>
internal sealed class SceneTextTextures : IDisposable
{
    private readonly Texture2DArray _texture = new();
    private readonly float _pixelRange;

    public SceneTextTextures()
    {
        using var document = JsonDocument.Parse(Godot.FileAccess.GetFileAsBytes("res://data/" + SceneTextInputs.AtlasKey));
        var root = document.RootElement;
        _pixelRange = root.GetProperty("pixelRange").GetSingle();
        var images = new Godot.Collections.Array<Image>();
        try
        {
            foreach (var page in root.GetProperty("pages").EnumerateArray())
            {
                var path = "res://assets/scene-text/" + page.GetProperty("file").GetString();
                var source = ResourceLoader.Load<Texture2D>(path) ?? throw new FileNotFoundException(path);
                var image = source.GetImage();
                image.Convert(Image.Format.Rgba8);
                images.Add(image);
            }
            var result = _texture.CreateFromImages(images);
            if (result != Error.Ok) throw new InvalidDataException($"Scene MSDF texture array creation failed: {result}");
        }
        catch
        {
            _texture.Dispose();
            throw;
        }
        finally
        {
            foreach (var image in images) image.Dispose();
        }
    }

    public void Bind(ShaderMaterial material)
    {
        material.SetShaderParameter("scene_text_pages", _texture);
        material.SetShaderParameter("scene_text_range", _pixelRange);
    }

    public void Dispose() => _texture.Dispose();
}
