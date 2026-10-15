using System;
using System.IO;
using Campus.Contracts;
using Campus.Kernel.Materials;
using Godot;

namespace Campus.Style.Stubs;

/// <summary>D0 material source; loads an authored whitelist shader only when a caller requests it.</summary>
[KernelService(typeof(IShaderMaterialSource), Owner = "WP-00", IsStub = true)]
public sealed class ShaderMaterialSourceStub : IShaderMaterialSource
{
    public ShaderMaterial Create(ShaderId shader)
    {
        var path = shader switch
        {
            ShaderId.Sky => "res://style/shaders/sky.gdshader",
            ShaderId.Water => "res://style/shaders/water.gdshader",
            ShaderId.WaterClear => "res://style/shaders/water_clear.gdshader",
            ShaderId.ReflectFlat => "res://style/shaders/reflect_flat.gdshader",
            ShaderId.FxGlow => "res://style/shaders/fx_glow.gdshader",
            ShaderId.FxFall => "res://style/shaders/fx_fall.gdshader",
            ShaderId.Precip => "res://style/shaders/precip.gdshader",
            ShaderId.VegTrunk => "res://style/shaders/veg_trunk.gdshader",
            ShaderId.VegCard => "res://style/shaders/veg_card.gdshader",
            ShaderId.VegImpostor => "res://style/shaders/veg_impostor.gdshader",
            ShaderId.VegGrass => "res://style/shaders/veg_grass.gdshader",
            ShaderId.VegLotus => "res://style/shaders/veg_lotus.gdshader",
            ShaderId.DioramaSection => "res://style/shaders/diorama_section.gdshader",
            ShaderId.DioramaFar => "res://style/shaders/diorama_far.gdshader",
            ShaderId.MediumOverlay => "res://style/shaders/medium_overlay.gdshader",
            ShaderId.CineCloud => "res://style/shaders/cine_cloud.gdshader",
            ShaderId.UiPanel => "res://style/shaders/ui_panel.gdshader",
            ShaderId.UiScreen => "res://style/shaders/ui_screen.gdshader",
            ShaderId.MapBase => "res://style/shaders/map_base.gdshader",
            _ => throw new ArgumentOutOfRangeException(nameof(shader), shader, "Shader ID is not registered in the material whitelist.")
        };

        if (!ResourceLoader.Exists(path, "Shader"))
        {
            throw new FileNotFoundException($"Whitelisted shader '{shader}' is not available at '{path}'. Its owning work package must supply the shader.", path);
        }

        var resource = ResourceLoader.Load<Shader>(path)
            ?? throw new InvalidOperationException($"Failed to load whitelisted shader '{shader}' from '{path}'.");
        return new ShaderMaterial { Shader = resource };
    }
}
