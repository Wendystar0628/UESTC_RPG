<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BaseMaterial3D

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class serves as a default material with a wide variety of rendering features and properties without the need to write shader code. See the tutorial below for details.

## 属性

### AOEnabled

```csharp
public bool AOEnabled { get; set; }
```

引擎名：`ao_enabled`

If `true`, ambient occlusion is enabled. Ambient occlusion darkens areas based on the `AOTexture`.

### AOLightAffect

```csharp
public float AOLightAffect { get; set; }
```

引擎名：`ao_light_affect`

Amount that ambient occlusion affects lighting from lights. If `0`, ambient occlusion only affects ambient light. If `1`, ambient occlusion affects lights just as much as it affects ambient light. This can be used to impact the strength of the ambient occlusion effect, but typically looks unrealistic.

### AOOnUV2

```csharp
public bool AOOnUV2 { get; set; }
```

引擎名：`ao_on_uv2`

If `true`, use `UV2` coordinates to look up from the `AOTexture`.

### AOTexture

```csharp
public Texture2D AOTexture { get; set; }
```

引擎名：`ao_texture`

Texture that defines the amount of ambient occlusion for a given point on the object.

### AOTextureChannel

```csharp
public BaseMaterial3D.TextureChannel AOTextureChannel { get; set; }
```

引擎名：`ao_texture_channel`

Specifies the channel of the `AOTexture` in which the ambient occlusion information is stored. This is useful when you store the information for multiple effects in a single texture. For example if you stored metallic in the red channel, roughness in the blue, and ambient occlusion in the green you could reduce the number of textures you use.

### AlbedoColor

```csharp
public Color AlbedoColor { get; set; }
```

引擎名：`albedo_color`

The material's base color.

**Note:** If `DetailEnabled` is `true` and a `DetailAlbedo` texture is specified, `AlbedoColor` will *not* modulate the detail texture. This can be used to color partial areas of a material by not specifying an albedo texture and using a transparent `DetailAlbedo` texture instead.

### AlbedoTexture

```csharp
public Texture2D AlbedoTexture { get; set; }
```

引擎名：`albedo_texture`

Texture to multiply by `AlbedoColor`. Used for basic texturing of objects.

If the texture appears unexpectedly too dark or too bright, check `AlbedoTextureForceSrgb`.

### AlbedoTextureForceSrgb

```csharp
public bool AlbedoTextureForceSrgb { get; set; }
```

引擎名：`albedo_texture_force_srgb`

If `true`, forces a conversion of the `AlbedoTexture` from nonlinear sRGB encoding to linear encoding. See also `VertexColorIsSrgb`.

This should only be enabled when needed (typically when using a [`ViewportTexture`](ViewportTexture.md) as `AlbedoTexture`). If `AlbedoTextureForceSrgb` is `true` when it shouldn't be, the texture will appear to be too dark. If `AlbedoTextureForceSrgb` is `false` when it shouldn't be, the texture will appear to be too bright.

### AlbedoTextureMsdf

```csharp
public bool AlbedoTextureMsdf { get; set; }
```

引擎名：`albedo_texture_msdf`

Enables multichannel signed distance field rendering shader. Use `MsdfPixelRange` and `MsdfOutlineSize` to configure MSDF parameters.

### AlphaAntialiasingEdge

```csharp
public float AlphaAntialiasingEdge { get; set; }
```

引擎名：`alpha_antialiasing_edge`

Threshold at which antialiasing will be applied on the alpha channel.

### AlphaAntialiasingMode

```csharp
public BaseMaterial3D.AlphaAntiAliasing AlphaAntialiasingMode { get; set; }
```

引擎名：`alpha_antialiasing_mode`

The type of alpha antialiasing to apply.

### AlphaHashScale

```csharp
public float AlphaHashScale { get; set; }
```

引擎名：`alpha_hash_scale`

The hashing scale for Alpha Hash. Recommended values between `0` and `2`.

### AlphaScissorThreshold

```csharp
public float AlphaScissorThreshold { get; set; }
```

引擎名：`alpha_scissor_threshold`

Threshold at which the alpha scissor will discard values. Higher values will result in more pixels being discarded. If the material becomes too opaque at a distance, try increasing `AlphaScissorThreshold`. If the material disappears at a distance, try decreasing `AlphaScissorThreshold`.

### Anisotropy

```csharp
public float Anisotropy { get; set; }
```

引擎名：`anisotropy`

The strength of the anisotropy effect. This is multiplied by `AnisotropyFlowmap`'s alpha channel if a texture is defined there and the texture contains an alpha channel.

### AnisotropyEnabled

```csharp
public bool AnisotropyEnabled { get; set; }
```

引擎名：`anisotropy_enabled`

If `true`, anisotropy is enabled. Anisotropy changes the shape of the specular blob and aligns it to tangent space. This is useful for brushed aluminum and hair reflections.

**Note:** Mesh tangents are needed for anisotropy to work. If the mesh does not contain tangents, the anisotropy effect will appear broken.

**Note:** Material anisotropy should not to be confused with anisotropic texture filtering, which can be enabled by setting `TextureFilter` to [`BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic`](BaseMaterial3D.md).

### AnisotropyFlowmap

```csharp
public Texture2D AnisotropyFlowmap { get; set; }
```

引擎名：`anisotropy_flowmap`

Texture that offsets the tangent map for anisotropy calculations and optionally controls the anisotropy effect (if an alpha channel is present). The flowmap texture is expected to be a derivative map, with the red channel representing distortion on the X axis and green channel representing distortion on the Y axis. Values below 0.5 will result in negative distortion, whereas values above 0.5 will result in positive distortion.

If present, the texture's alpha channel will be used to multiply the strength of the `Anisotropy` effect. Fully opaque pixels will keep the anisotropy effect's original strength while fully transparent pixels will disable the anisotropy effect entirely. The flowmap texture's blue channel is ignored.

### Backlight

```csharp
public Color Backlight { get; set; }
```

引擎名：`backlight`

The color used by the backlight effect. Represents the light passing through an object.

### BacklightEnabled

```csharp
public bool BacklightEnabled { get; set; }
```

引擎名：`backlight_enabled`

If `true`, the backlight effect is enabled. See also `SubsurfScatterTransmittanceEnabled`.

### BacklightTexture

```csharp
public Texture2D BacklightTexture { get; set; }
```

引擎名：`backlight_texture`

Texture used to control the backlight effect per-pixel. Added to `Backlight`.

### BentNormalEnabled

```csharp
public bool BentNormalEnabled { get; set; }
```

引擎名：`bent_normal_enabled`

If `true`, the bent normal map is enabled. This allows for more accurate indirect lighting and specular occlusion.

### BentNormalTexture

```csharp
public Texture2D BentNormalTexture { get; set; }
```

引擎名：`bent_normal_texture`

Texture that specifies the average direction of incoming ambient light at a given pixel. The `BentNormalTexture` only uses the red and green channels; the blue and alpha channels are ignored. The normal read from `BentNormalTexture` is oriented around the surface normal provided by the [`Mesh`](Mesh.md).

**Note:** A bent normal map is different from a regular normal map. When baking a bent normal map make sure to use **a cosine distribution** for the bent normal map to work correctly.

**Note:** The mesh must have both normals and tangents defined in its vertex data. Otherwise, the shading produced by the bent normal map will not look correct. If creating geometry with [`SurfaceTool`](SurfaceTool.md), you can use [`SurfaceTool.GenerateNormals`](SurfaceTool.md) and [`SurfaceTool.GenerateTangents`](SurfaceTool.md) to automatically generate normals and tangents respectively.

**Note:** Godot expects the bent normal map to use X+, Y+, and Z+ coordinates. See [this page](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Common_Swizzle_Coordinates) for a comparison of normal map coordinates expected by popular engines.

### BillboardKeepScale

```csharp
public bool BillboardKeepScale { get; set; }
```

引擎名：`billboard_keep_scale`

If `true`, the shader will keep the scale set for the mesh. Otherwise, the scale is lost when billboarding. Only applies when `BillboardMode` is not [`BaseMaterial3D.BillboardModeEnum.Disabled`](BaseMaterial3D.md).

### BillboardMode

```csharp
public BaseMaterial3D.BillboardModeEnum BillboardMode { get; set; }
```

引擎名：`billboard_mode`

Controls how the object faces the camera.

**Note:** Billboard mode is not suitable for VR because the left-right vector of the camera is not horizontal when the screen is attached to your head instead of on the table. See [GitHub issue #41567](https://github.com/godotengine/godot/issues/41567) for details.

### BlendMode

```csharp
public BaseMaterial3D.BlendModeEnum BlendMode { get; set; }
```

引擎名：`blend_mode`

The material's blend mode.

**Note:** Values other than `Mix` force the object into the transparent pipeline.

### Clearcoat

```csharp
public float Clearcoat { get; set; }
```

引擎名：`clearcoat`

Sets the strength of the clearcoat effect. Setting to `0` looks the same as disabling the clearcoat effect.

### ClearcoatEnabled

```csharp
public bool ClearcoatEnabled { get; set; }
```

引擎名：`clearcoat_enabled`

If `true`, clearcoat rendering is enabled. Adds a secondary transparent pass to the lighting calculation resulting in an added specular blob. This makes materials appear as if they have a clear layer on them that can be either glossy or rough.

**Note:** Clearcoat rendering is not visible if the material's `ShadingMode` is [`BaseMaterial3D.ShadingModeEnum.Unshaded`](BaseMaterial3D.md).

### ClearcoatRoughness

```csharp
public float ClearcoatRoughness { get; set; }
```

引擎名：`clearcoat_roughness`

Sets the roughness of the clearcoat pass. A higher value results in a rougher clearcoat while a lower value results in a smoother clearcoat.

### ClearcoatTexture

```csharp
public Texture2D ClearcoatTexture { get; set; }
```

引擎名：`clearcoat_texture`

Texture that defines the strength of the clearcoat effect and the glossiness of the clearcoat. Strength is specified in the red channel while glossiness is specified in the green channel.

### CullMode

```csharp
public BaseMaterial3D.CullModeEnum CullMode { get; set; }
```

引擎名：`cull_mode`

Determines which side of the triangle to cull depending on whether the triangle faces towards or away from the camera.

### DepthDrawMode

```csharp
public BaseMaterial3D.DepthDrawModeEnum DepthDrawMode { get; set; }
```

引擎名：`depth_draw_mode`

Determines when depth rendering takes place. See also `Transparency`.

### DepthTest

```csharp
public BaseMaterial3D.DepthTestEnum DepthTest { get; set; }
```

引擎名：`depth_test`

Determines which comparison operator is used when testing depth.

**Note:** Changing `DepthTest` to a non-default value only has a visible effect when used on a transparent material, or a material that has `DepthDrawMode` set to [`BaseMaterial3D.DepthDrawModeEnum.Disabled`](BaseMaterial3D.md).

### DetailAlbedo

```csharp
public Texture2D DetailAlbedo { get; set; }
```

引擎名：`detail_albedo`

Texture that specifies the color of the detail overlay. `DetailAlbedo`'s alpha channel is used as a mask, even when the material is opaque. To use a dedicated texture as a mask, see `DetailMask`.

**Note:** `DetailAlbedo` is *not* modulated by `AlbedoColor`.

### DetailBlendMode

```csharp
public BaseMaterial3D.BlendModeEnum DetailBlendMode { get; set; }
```

引擎名：`detail_blend_mode`

Specifies how the `DetailAlbedo` should blend with the current `ALBEDO`.

### DetailEnabled

```csharp
public bool DetailEnabled { get; set; }
```

引擎名：`detail_enabled`

If `true`, enables the detail overlay. Detail is a second texture that gets mixed over the surface of the object based on `DetailMask` and `DetailAlbedo`'s alpha channel. This can be used to add variation to objects, or to blend between two different albedo/normal textures.

### DetailMask

```csharp
public Texture2D DetailMask { get; set; }
```

引擎名：`detail_mask`

Texture used to specify how the detail textures get blended with the base textures. `DetailMask` can be used together with `DetailAlbedo`'s alpha channel (if any).

### DetailNormal

```csharp
public Texture2D DetailNormal { get; set; }
```

引擎名：`detail_normal`

Texture that specifies the per-pixel normal of the detail overlay. The `DetailNormal` texture only uses the red and green channels; the blue and alpha channels are ignored. The normal read from `DetailNormal` is oriented around the surface normal provided by the [`Mesh`](Mesh.md).

**Note:** Godot expects the normal map to use X+, Y+, and Z+ coordinates. See [this page](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Common_Swizzle_Coordinates) for a comparison of normal map coordinates expected by popular engines.

### DetailUVLayer

```csharp
public BaseMaterial3D.DetailUV DetailUVLayer { get; set; }
```

引擎名：`detail_uv_layer`

Specifies whether to use `UV` or `UV2` for the detail layer.

### DiffuseMode

```csharp
public BaseMaterial3D.DiffuseModeEnum DiffuseMode { get; set; }
```

引擎名：`diffuse_mode`

The algorithm used for diffuse light scattering.

### DisableAmbientLight

```csharp
public bool DisableAmbientLight { get; set; }
```

引擎名：`disable_ambient_light`

If `true`, the object receives no ambient light.

### DisableFog

```csharp
public bool DisableFog { get; set; }
```

引擎名：`disable_fog`

If `true`, the object will not be affected by fog (neither volumetric nor depth fog). This is useful for unshaded or transparent materials (e.g. particles), which without this setting will be affected even if fully transparent.

### DisableReceiveShadows

```csharp
public bool DisableReceiveShadows { get; set; }
```

引擎名：`disable_receive_shadows`

If `true`, the object receives no shadow that would otherwise be cast onto it.

### DisableSpecularOcclusion

```csharp
public bool DisableSpecularOcclusion { get; set; }
```

引擎名：`disable_specular_occlusion`

If `true`, disables specular occlusion even if `ProjectSettings.rendering/reflections/specular_occlusion/enabled` is `false`.

### DistanceFadeMaxDistance

```csharp
public float DistanceFadeMaxDistance { get; set; }
```

引擎名：`distance_fade_max_distance`

Distance at which the object appears fully opaque.

**Note:** If `DistanceFadeMaxDistance` is less than `DistanceFadeMinDistance`, the behavior will be reversed. The object will start to fade away at `DistanceFadeMaxDistance` and will fully disappear once it reaches `DistanceFadeMinDistance`.

### DistanceFadeMinDistance

```csharp
public float DistanceFadeMinDistance { get; set; }
```

引擎名：`distance_fade_min_distance`

Distance at which the object starts to become visible. If the object is less than this distance away, it will be invisible.

**Note:** If `DistanceFadeMinDistance` is greater than `DistanceFadeMaxDistance`, the behavior will be reversed. The object will start to fade away at `DistanceFadeMaxDistance` and will fully disappear once it reaches `DistanceFadeMinDistance`.

### DistanceFadeMode

```csharp
public BaseMaterial3D.DistanceFadeModeEnum DistanceFadeMode { get; set; }
```

引擎名：`distance_fade_mode`

Specifies which type of fade to use. Can be any of the [`BaseMaterial3D.DistanceFadeModeEnum`](BaseMaterial3D.md)s.

### Emission

```csharp
public Color Emission { get; set; }
```

引擎名：`emission`

The emitted light's color. See `EmissionEnabled`.

### EmissionEnabled

```csharp
public bool EmissionEnabled { get; set; }
```

引擎名：`emission_enabled`

If `true`, the body emits light. Emitting light makes the object appear brighter. The object can also cast light on other objects if a [`VoxelGI`](VoxelGI.md), SDFGI, or [`LightmapGI`](LightmapGI.md) is used and this object is used in baked lighting.

### EmissionEnergyMultiplier

```csharp
public float EmissionEnergyMultiplier { get; set; }
```

引擎名：`emission_energy_multiplier`

Multiplier for emitted light. See `EmissionEnabled`.

### EmissionIntensity

```csharp
public float EmissionIntensity { get; set; }
```

引擎名：`emission_intensity`

Luminance of emitted light, measured in nits (candela per square meter). Only available when `ProjectSettings.rendering/lights_and_shadows/use_physical_light_units` is enabled. The default is roughly equivalent to an indoor lightbulb.

### EmissionOnUV2

```csharp
public bool EmissionOnUV2 { get; set; }
```

引擎名：`emission_on_uv2`

Use `UV2` to read from the `EmissionTexture`.

### EmissionOperator

```csharp
public BaseMaterial3D.EmissionOperatorEnum EmissionOperator { get; set; }
```

引擎名：`emission_operator`

Sets how `Emission` interacts with `EmissionTexture`. Can either add or multiply.

### EmissionTexture

```csharp
public Texture2D EmissionTexture { get; set; }
```

引擎名：`emission_texture`

Texture that specifies how much surface emits light at a given point.

### FixedSize

```csharp
public bool FixedSize { get; set; }
```

引擎名：`fixed_size`

If `true`, the object is rendered at the same size regardless of distance. The object's size on screen is the same as if the camera was `1.0` units away from the object's origin, regardless of the actual distance from the camera. The [`Camera3D`](Camera3D.md)'s field of view (or [`Camera3D.Size`](Camera3D.md) when in orthogonal/frustum mode) still affects the size the object is drawn at.

### FovOverride

```csharp
public float FovOverride { get; set; }
```

引擎名：`fov_override`

Overrides the [`Camera3D`](Camera3D.md)'s field of view angle (in degrees).

**Note:** This behaves as if the field of view is set on a [`Camera3D`](Camera3D.md) with [`Camera3D.KeepAspect`](Camera3D.md) set to [`Camera3D.KeepAspectEnum.Height`](Camera3D.md). Additionally, it may not look correct on a non-perspective camera where the field of view setting is ignored.

### Grow

```csharp
public bool Grow { get; set; }
```

引擎名：`grow`

If `true`, enables the vertex grow setting. This can be used to create mesh-based outlines using a second material pass and its `CullMode` set to [`BaseMaterial3D.CullModeEnum.Front`](BaseMaterial3D.md). See also `GrowAmount`.

**Note:** Vertex growth cannot create new vertices, which means that visible gaps may occur in sharp corners. This can be alleviated by designing the mesh to use smooth normals exclusively using [face weighted normals](http://wiki.polycount.com/wiki/Face_weighted_normals) in the 3D authoring software. In this case, grow will be able to join every outline together, just like in the original mesh.

### GrowAmount

```csharp
public float GrowAmount { get; set; }
```

引擎名：`grow_amount`

Grows object vertices in the direction of their normals. Only effective if `Grow` is `true`.

### HeightmapDeepParallax

```csharp
public bool HeightmapDeepParallax { get; set; }
```

引擎名：`heightmap_deep_parallax`

If `true`, uses parallax occlusion mapping to represent depth in the material instead of simple offset mapping (see `HeightmapEnabled`). This results in a more convincing depth effect, but is much more expensive on the GPU. Only enable this on materials where it makes a significant visual difference.

### HeightmapEnabled

```csharp
public bool HeightmapEnabled { get; set; }
```

引擎名：`heightmap_enabled`

If `true`, height mapping is enabled (also called "parallax mapping" or "depth mapping"). See also `NormalEnabled`. Height mapping is a demanding feature on the GPU, so it should only be used on materials where it makes a significant visual difference.

**Note:** Height mapping is not supported if triplanar mapping is used on the same material. The value of `HeightmapEnabled` will be ignored if `Uv1Triplanar` is enabled.

### HeightmapFlipBinormal

```csharp
public bool HeightmapFlipBinormal { get; set; }
```

引擎名：`heightmap_flip_binormal`

If `true`, flips the mesh's binormal vectors when interpreting the height map. If the heightmap effect looks strange when the camera moves (even with a reasonable `HeightmapScale`), try setting this to `true`.

### HeightmapFlipTangent

```csharp
public bool HeightmapFlipTangent { get; set; }
```

引擎名：`heightmap_flip_tangent`

If `true`, flips the mesh's tangent vectors when interpreting the height map. If the heightmap effect looks strange when the camera moves (even with a reasonable `HeightmapScale`), try setting this to `true`.

### HeightmapFlipTexture

```csharp
public bool HeightmapFlipTexture { get; set; }
```

引擎名：`heightmap_flip_texture`

If `true`, interprets the height map texture as a depth map, with brighter values appearing to be "lower" in altitude compared to darker values.

This can be enabled for compatibility with some materials authored for Godot 3.x. This is not necessary if the Invert import option was used to invert the depth map in Godot 3.x, in which case `HeightmapFlipTexture` should remain `false`.

### HeightmapMaxLayers

```csharp
public int HeightmapMaxLayers { get; set; }
```

引擎名：`heightmap_max_layers`

The number of layers to use for parallax occlusion mapping when the camera is up close to the material. Higher values result in a more convincing depth effect, especially in materials that have steep height changes. Higher values have a significant cost on the GPU, so it should only be increased on materials where it makes a significant visual difference.

**Note:** Only effective if `HeightmapDeepParallax` is `true`.

### HeightmapMinLayers

```csharp
public int HeightmapMinLayers { get; set; }
```

引擎名：`heightmap_min_layers`

The number of layers to use for parallax occlusion mapping when the camera is far away from the material. Higher values result in a more convincing depth effect, especially in materials that have steep height changes. Higher values have a significant cost on the GPU, so it should only be increased on materials where it makes a significant visual difference.

**Note:** Only effective if `HeightmapDeepParallax` is `true`.

### HeightmapScale

```csharp
public float HeightmapScale { get; set; }
```

引擎名：`heightmap_scale`

The heightmap scale to use for the parallax effect (see `HeightmapEnabled`). The default value is tuned so that the highest point (value = 255) appears to be 5 cm higher than the lowest point (value = 0). Higher values result in a deeper appearance, but may result in artifacts appearing when looking at the material from oblique angles, especially when the camera moves. Negative values can be used to invert the parallax effect, but this is different from inverting the texture using `HeightmapFlipTexture` as the material will also appear to be "closer" to the camera. In most cases, `HeightmapScale` should be kept to a positive value.

**Note:** If the height map effect looks strange regardless of this value, try adjusting `HeightmapFlipBinormal` and `HeightmapFlipTangent`. See also `HeightmapTexture` for recommendations on authoring heightmap textures, as the way the heightmap texture is authored affects how `HeightmapScale` behaves.

### HeightmapTexture

```csharp
public Texture2D HeightmapTexture { get; set; }
```

引擎名：`heightmap_texture`

The texture to use as a height map. See also `HeightmapEnabled`.

For best results, the texture should be normalized (with `HeightmapScale` reduced to compensate). In [GIMP](https://gimp.org), this can be done using **Colors > Auto > Equalize**. If the texture only uses a small part of its available range, the parallax effect may look strange, especially when the camera moves.

**Note:** To reduce memory usage and improve loading times, you may be able to use a lower-resolution heightmap texture as most heightmaps are only comprised of low-frequency data.

### Metallic

```csharp
public float Metallic { get; set; }
```

引擎名：`metallic`

A high value makes the material appear more like a metal. Non-metals use their albedo as the diffuse color and add diffuse to the specular reflection. With non-metals, the reflection appears on top of the albedo color. Metals use their albedo as a multiplier to the specular reflection and set the diffuse color to black resulting in a tinted reflection. Materials work better when fully metal or fully non-metal, values between `0` and `1` should only be used for blending between metal and non-metal sections. To alter the amount of reflection use `Roughness`.

### MetallicSpecular

```csharp
public float MetallicSpecular { get; set; }
```

引擎名：`metallic_specular`

Adjusts the strength of specular reflections. Specular reflections are composed of scene reflections and the specular lobe which is the bright spot that is reflected from light sources. When set to `0.0`, no specular reflections will be visible. This differs from the [`BaseMaterial3D.SpecularModeEnum.Disabled`](BaseMaterial3D.md) [`BaseMaterial3D.SpecularModeEnum`](BaseMaterial3D.md) as [`BaseMaterial3D.SpecularModeEnum.Disabled`](BaseMaterial3D.md) only applies to the specular lobe from the light source.

**Note:** Unlike `Metallic`, this is not energy-conserving, so it should be left at `0.5` in most cases. See also `Roughness`.

### MetallicTexture

```csharp
public Texture2D MetallicTexture { get; set; }
```

引擎名：`metallic_texture`

Texture used to specify metallic for an object. This is multiplied by `Metallic`.

### MetallicTextureChannel

```csharp
public BaseMaterial3D.TextureChannel MetallicTextureChannel { get; set; }
```

引擎名：`metallic_texture_channel`

Specifies the channel of the `MetallicTexture` in which the metallic information is stored. This is useful when you store the information for multiple effects in a single texture. For example if you stored metallic in the red channel, roughness in the blue, and ambient occlusion in the green you could reduce the number of textures you use.

### MsdfOutlineSize

```csharp
public float MsdfOutlineSize { get; set; }
```

引擎名：`msdf_outline_size`

The width of the shape outline.

### MsdfPixelRange

```csharp
public float MsdfPixelRange { get; set; }
```

引擎名：`msdf_pixel_range`

The width of the range around the shape between the minimum and maximum representable signed distance.

### NoDepthTest

```csharp
public bool NoDepthTest { get; set; }
```

引擎名：`no_depth_test`

If `true`, depth testing is disabled and the object will be drawn in render order.

### NormalEnabled

```csharp
public bool NormalEnabled { get; set; }
```

引擎名：`normal_enabled`

If `true`, normal mapping is enabled. This has a slight performance cost, especially on mobile GPUs.

### NormalScale

```csharp
public float NormalScale { get; set; }
```

引擎名：`normal_scale`

The strength of the normal map's effect.

### NormalTexture

```csharp
public Texture2D NormalTexture { get; set; }
```

引擎名：`normal_texture`

Texture used to specify the normal at a given pixel. The `NormalTexture` only uses the red and green channels; the blue and alpha channels are ignored. The normal read from `NormalTexture` is oriented around the surface normal provided by the [`Mesh`](Mesh.md).

**Note:** The mesh must have both normals and tangents defined in its vertex data. Otherwise, the normal map won't render correctly and will only appear to darken the whole surface. If creating geometry with [`SurfaceTool`](SurfaceTool.md), you can use [`SurfaceTool.GenerateNormals`](SurfaceTool.md) and [`SurfaceTool.GenerateTangents`](SurfaceTool.md) to automatically generate normals and tangents respectively.

**Note:** Godot expects the normal map to use X+, Y+, and Z+ coordinates. See [this page](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Common_Swizzle_Coordinates) for a comparison of normal map coordinates expected by popular engines.

**Note:** If `DetailEnabled` is `true`, the `DetailAlbedo` texture is drawn *below* the `NormalTexture`. To display a normal map *above* the `DetailAlbedo` texture, use `DetailNormal` instead.

### OrmTexture

```csharp
public Texture2D OrmTexture { get; set; }
```

引擎名：`orm_texture`

The Occlusion/Roughness/Metallic texture to use. This is a more efficient replacement of `AOTexture`, `RoughnessTexture` and `MetallicTexture` in [`OrmMaterial3D`](OrmMaterial3D.md). Ambient occlusion is stored in the red channel. Roughness map is stored in the green channel. Metallic map is stored in the blue channel. The alpha channel is ignored.

### ParticlesAnimHFrames

```csharp
public int ParticlesAnimHFrames { get; set; }
```

引擎名：`particles_anim_h_frames`

The number of horizontal frames in the particle sprite sheet. Only enabled when using [`BaseMaterial3D.BillboardModeEnum.Particles`](BaseMaterial3D.md). See `BillboardMode`.

### ParticlesAnimLoop

```csharp
public bool ParticlesAnimLoop { get; set; }
```

引擎名：`particles_anim_loop`

If `true`, particle animations are looped. Only enabled when using [`BaseMaterial3D.BillboardModeEnum.Particles`](BaseMaterial3D.md). See `BillboardMode`.

### ParticlesAnimVFrames

```csharp
public int ParticlesAnimVFrames { get; set; }
```

引擎名：`particles_anim_v_frames`

The number of vertical frames in the particle sprite sheet. Only enabled when using [`BaseMaterial3D.BillboardModeEnum.Particles`](BaseMaterial3D.md). See `BillboardMode`.

### PointSize

```csharp
public float PointSize { get; set; }
```

引擎名：`point_size`

The point size in pixels. See `UsePointSize`.

### ProximityFadeDistance

```csharp
public float ProximityFadeDistance { get; set; }
```

引擎名：`proximity_fade_distance`

Distance over which the fade effect takes place. The larger the distance the longer it takes for an object to fade.

### ProximityFadeEnabled

```csharp
public bool ProximityFadeEnabled { get; set; }
```

引擎名：`proximity_fade_enabled`

If `true`, the proximity fade effect is enabled. The proximity fade effect fades out each pixel based on its distance to another object.

### RefractionEnabled

```csharp
public bool RefractionEnabled { get; set; }
```

引擎名：`refraction_enabled`

If `true`, the refraction effect is enabled. Distorts transparency based on light from behind the object.

**Note:** Refraction is implemented using the screen texture. Only opaque materials will appear in the refraction, since transparent materials do not appear in the screen texture.

### RefractionScale

```csharp
public float RefractionScale { get; set; }
```

引擎名：`refraction_scale`

The strength of the refraction effect.

### RefractionTexture

```csharp
public Texture2D RefractionTexture { get; set; }
```

引擎名：`refraction_texture`

Texture that controls the strength of the refraction per-pixel. Multiplied by `RefractionScale`.

### RefractionTextureChannel

```csharp
public BaseMaterial3D.TextureChannel RefractionTextureChannel { get; set; }
```

引擎名：`refraction_texture_channel`

Specifies the channel of the `RefractionTexture` in which the refraction information is stored. This is useful when you store the information for multiple effects in a single texture. For example if you stored refraction in the red channel, roughness in the blue, and ambient occlusion in the green you could reduce the number of textures you use.

### Rim

```csharp
public float Rim { get; set; }
```

引擎名：`rim`

Sets the strength of the rim lighting effect.

### RimEnabled

```csharp
public bool RimEnabled { get; set; }
```

引擎名：`rim_enabled`

If `true`, rim effect is enabled. Rim lighting increases the brightness at glancing angles on an object.

**Note:** Rim lighting is not visible if the material's `ShadingMode` is [`BaseMaterial3D.ShadingModeEnum.Unshaded`](BaseMaterial3D.md).

### RimTexture

```csharp
public Texture2D RimTexture { get; set; }
```

引擎名：`rim_texture`

Texture used to set the strength of the rim lighting effect per-pixel. Multiplied by `Rim`.

### RimTint

```csharp
public float RimTint { get; set; }
```

引擎名：`rim_tint`

The amount of to blend light and albedo color when rendering rim effect. If `0` the light color is used, while `1` means albedo color is used. An intermediate value generally works best.

### Roughness

```csharp
public float Roughness { get; set; }
```

引擎名：`roughness`

Surface reflection. A value of `0` represents a perfect mirror while a value of `1` completely blurs the reflection. See also `Metallic`.

### RoughnessTexture

```csharp
public Texture2D RoughnessTexture { get; set; }
```

引擎名：`roughness_texture`

Texture used to control the roughness per-pixel. Multiplied by `Roughness`.

### RoughnessTextureChannel

```csharp
public BaseMaterial3D.TextureChannel RoughnessTextureChannel { get; set; }
```

引擎名：`roughness_texture_channel`

Specifies the channel of the `RoughnessTexture` in which the roughness information is stored. This is useful when you store the information for multiple effects in a single texture. For example if you stored metallic in the red channel, roughness in the blue, and ambient occlusion in the green you could reduce the number of textures you use.

### ShadingMode

```csharp
public BaseMaterial3D.ShadingModeEnum ShadingMode { get; set; }
```

引擎名：`shading_mode`

Sets whether the shading takes place, per-pixel, per-vertex or unshaded. Per-vertex lighting is faster, making it the best choice for mobile applications, however it looks considerably worse than per-pixel. Unshaded rendering is the fastest, but disables all interactions with lights.

### ShadowToOpacity

```csharp
public bool ShadowToOpacity { get; set; }
```

引擎名：`shadow_to_opacity`

If `true`, enables the "shadow to opacity" render mode where lighting modifies the alpha so shadowed areas are opaque and non-shadowed areas are transparent. Useful for overlaying shadows onto a camera feed in AR.

### SpecularMode

```csharp
public BaseMaterial3D.SpecularModeEnum SpecularMode { get; set; }
```

引擎名：`specular_mode`

The method for rendering the specular blob.

**Note:** `SpecularMode` only applies to the specular blob. It does not affect specular reflections from the sky, screen-space reflections, [`VoxelGI`](VoxelGI.md), SDFGI or [`ReflectionProbe`](ReflectionProbe.md)s. To disable reflections from these sources as well, set `MetallicSpecular` to `0.0` instead.

### StencilColor

```csharp
public Color StencilColor { get; set; }
```

引擎名：`stencil_color`

The primary color of the stencil effect.

### StencilCompare

```csharp
public BaseMaterial3D.StencilCompareEnum StencilCompare { get; set; }
```

引擎名：`stencil_compare`

The comparison operator to use for stencil masking operations.

### StencilFlags

```csharp
public int StencilFlags { get; set; }
```

引擎名：`stencil_flags`

The flags dictating how the stencil operation behaves.

### StencilMode

```csharp
public BaseMaterial3D.StencilModeEnum StencilMode { get; set; }
```

引擎名：`stencil_mode`

The stencil effect mode.

### StencilOutlineThickness

```csharp
public float StencilOutlineThickness { get; set; }
```

引擎名：`stencil_outline_thickness`

The outline thickness for [`BaseMaterial3D.StencilModeEnum.Outline`](BaseMaterial3D.md).

### StencilReference

```csharp
public int StencilReference { get; set; }
```

引擎名：`stencil_reference`

The stencil reference value (0-255). Typically a power of 2.

### SubsurfScatterEnabled

```csharp
public bool SubsurfScatterEnabled { get; set; }
```

引擎名：`subsurf_scatter_enabled`

If `true`, subsurface scattering is enabled. Emulates light that penetrates an object's surface, is scattered, and then emerges. Subsurface scattering quality is controlled by `ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_quality`.

**Note:** Subsurface scattering is not supported on viewports that have a transparent background (where [`Viewport.TransparentBg`](Viewport.md) is `true`).

### SubsurfScatterSkinMode

```csharp
public bool SubsurfScatterSkinMode { get; set; }
```

引擎名：`subsurf_scatter_skin_mode`

If `true`, subsurface scattering will use a special mode optimized for the color and density of human skin, such as boosting the intensity of the red channel in subsurface scattering.

### SubsurfScatterStrength

```csharp
public float SubsurfScatterStrength { get; set; }
```

引擎名：`subsurf_scatter_strength`

The strength of the subsurface scattering effect. The depth of the effect is also controlled by `ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_scale`, which is set globally.

### SubsurfScatterTexture

```csharp
public Texture2D SubsurfScatterTexture { get; set; }
```

引擎名：`subsurf_scatter_texture`

Texture used to control the subsurface scattering strength. Stored in the red texture channel. Multiplied by `SubsurfScatterStrength`.

### SubsurfScatterTransmittanceBoost

```csharp
public float SubsurfScatterTransmittanceBoost { get; set; }
```

引擎名：`subsurf_scatter_transmittance_boost`

The intensity of the subsurface scattering transmittance effect.

### SubsurfScatterTransmittanceColor

```csharp
public Color SubsurfScatterTransmittanceColor { get; set; }
```

引擎名：`subsurf_scatter_transmittance_color`

The color to multiply the subsurface scattering transmittance effect with. Ignored if `SubsurfScatterSkinMode` is `true`.

### SubsurfScatterTransmittanceDepth

```csharp
public float SubsurfScatterTransmittanceDepth { get; set; }
```

引擎名：`subsurf_scatter_transmittance_depth`

The depth of the subsurface scattering transmittance effect.

### SubsurfScatterTransmittanceEnabled

```csharp
public bool SubsurfScatterTransmittanceEnabled { get; set; }
```

引擎名：`subsurf_scatter_transmittance_enabled`

If `true`, enables subsurface scattering transmittance. Only effective if `SubsurfScatterEnabled` is `true`. See also `BacklightEnabled`.

### SubsurfScatterTransmittanceTexture

```csharp
public Texture2D SubsurfScatterTransmittanceTexture { get; set; }
```

引擎名：`subsurf_scatter_transmittance_texture`

The texture to use for multiplying the intensity of the subsurface scattering transmittance intensity. See also `SubsurfScatterTexture`. Ignored if `SubsurfScatterSkinMode` is `true`.

### TextureFilter

```csharp
public BaseMaterial3D.TextureFilterEnum TextureFilter { get; set; }
```

引擎名：`texture_filter`

Filter flags for the texture.

**Note:** `HeightmapTexture` is always sampled with linear filtering, even if nearest-neighbor filtering is selected here. This is to ensure the heightmap effect looks as intended. If you need sharper height transitions between pixels, resize the heightmap texture in an image editor with nearest-neighbor filtering.

### TextureRepeat

```csharp
public bool TextureRepeat { get; set; }
```

引擎名：`texture_repeat`

If `true`, the texture repeats when exceeding the texture's size. See [`BaseMaterial3D.Flags.UseTextureRepeat`](BaseMaterial3D.md).

### Transparency

```csharp
public BaseMaterial3D.TransparencyEnum Transparency { get; set; }
```

引擎名：`transparency`

The material's transparency mode. Some transparency modes will disable shadow casting. Any transparency mode other than [`BaseMaterial3D.TransparencyEnum.Disabled`](BaseMaterial3D.md) has a greater performance impact compared to opaque rendering. See also `BlendMode`.

### UV2Offset

```csharp
public Vector3 UV2Offset { get; set; }
```

引擎名：`uv2_offset`

How much to offset the `UV2` coordinates. This amount will be added to `UV2` in the vertex function. This can be used to offset a texture. The Z component is used when `UV2Triplanar` is enabled, but it is not used anywhere else.

### UV2Scale

```csharp
public Vector3 UV2Scale { get; set; }
```

引擎名：`uv2_scale`

How much to scale the `UV2` coordinates. This is multiplied by `UV2` in the vertex function. The Z component is used when `UV2Triplanar` is enabled, but it is not used anywhere else.

### UV2Triplanar

```csharp
public bool UV2Triplanar { get; set; }
```

引擎名：`uv2_triplanar`

If `true`, instead of using `UV2` textures will use a triplanar texture lookup to determine how to apply textures. Triplanar uses the orientation of the object's surface to blend between texture coordinates. It reads from the source texture 3 times, once for each axis and then blends between the results based on how closely the pixel aligns with each axis. This is often used for natural features to get a realistic blend of materials. Because triplanar texturing requires many more texture reads per-pixel it is much slower than normal UV texturing. Additionally, because it is blending the texture between the three axes, it is unsuitable when you are trying to achieve crisp texturing.

### UV2TriplanarSharpness

```csharp
public float UV2TriplanarSharpness { get; set; }
```

引擎名：`uv2_triplanar_sharpness`

A lower number blends the texture more softly while a higher number blends the texture more sharply.

**Note:** `UV2TriplanarSharpness` is clamped between `0.0` and `150.0` (inclusive) as values outside that range can look broken depending on the mesh.

### UV2WorldTriplanar

```csharp
public bool UV2WorldTriplanar { get; set; }
```

引擎名：`uv2_world_triplanar`

If `true`, triplanar mapping for `UV2` is calculated in world space rather than object local space. See also `UV2Triplanar`.

### UseFovOverride

```csharp
public bool UseFovOverride { get; set; }
```

引擎名：`use_fov_override`

If `true` use `FovOverride` to override the [`Camera3D`](Camera3D.md)'s field of view angle.

### UseParticleTrails

```csharp
public bool UseParticleTrails { get; set; }
```

引擎名：`use_particle_trails`

If `true`, enables parts of the shader required for [`GpuParticles3D`](GpuParticles3D.md) trails to function. This also requires using a mesh with appropriate skinning, such as [`RibbonTrailMesh`](RibbonTrailMesh.md) or [`TubeTrailMesh`](TubeTrailMesh.md). Enabling this feature outside of materials used in [`GpuParticles3D`](GpuParticles3D.md) meshes will break material rendering.

### UsePointSize

```csharp
public bool UsePointSize { get; set; }
```

引擎名：`use_point_size`

If `true`, render point size can be changed.

**Note:** This is only effective for objects whose geometry is point-based rather than triangle-based. See also `PointSize`.

### UseZClipScale

```csharp
public bool UseZClipScale { get; set; }
```

引擎名：`use_z_clip_scale`

If `true` use `ZClipScale` to scale the object being rendered towards the camera to avoid clipping into things like walls.

### Uv1Offset

```csharp
public Vector3 Uv1Offset { get; set; }
```

引擎名：`uv1_offset`

How much to offset the `UV` coordinates. This amount will be added to `UV` in the vertex function. This can be used to offset a texture. The Z component is used when `Uv1Triplanar` is enabled, but it is not used anywhere else.

### Uv1Scale

```csharp
public Vector3 Uv1Scale { get; set; }
```

引擎名：`uv1_scale`

How much to scale the `UV` coordinates. This is multiplied by `UV` in the vertex function. The Z component is used when `Uv1Triplanar` is enabled, but it is not used anywhere else.

### Uv1Triplanar

```csharp
public bool Uv1Triplanar { get; set; }
```

引擎名：`uv1_triplanar`

If `true`, instead of using `UV` textures will use a triplanar texture lookup to determine how to apply textures. Triplanar uses the orientation of the object's surface to blend between texture coordinates. It reads from the source texture 3 times, once for each axis and then blends between the results based on how closely the pixel aligns with each axis. This is often used for natural features to get a realistic blend of materials. Because triplanar texturing requires many more texture reads per-pixel it is much slower than normal UV texturing. Additionally, because it is blending the texture between the three axes, it is unsuitable when you are trying to achieve crisp texturing.

### Uv1TriplanarSharpness

```csharp
public float Uv1TriplanarSharpness { get; set; }
```

引擎名：`uv1_triplanar_sharpness`

A lower number blends the texture more softly while a higher number blends the texture more sharply.

**Note:** `Uv1TriplanarSharpness` is clamped between `0.0` and `150.0` (inclusive) as values outside that range can look broken depending on the mesh.

### Uv1WorldTriplanar

```csharp
public bool Uv1WorldTriplanar { get; set; }
```

引擎名：`uv1_world_triplanar`

If `true`, triplanar mapping for `UV` is calculated in world space rather than object local space. See also `Uv1Triplanar`.

### VertexColorIsSrgb

```csharp
public bool VertexColorIsSrgb { get; set; }
```

引擎名：`vertex_color_is_srgb`

If `true`, vertex colors are considered to be stored in nonlinear sRGB encoding and are converted to linear encoding during rendering. If `false`, vertex colors are considered to be stored in linear encoding and are rendered as-is. See also `AlbedoTextureForceSrgb`.

**Note:** Only effective when using the Forward+ and Mobile rendering methods, not Compatibility.

### VertexColorUseAsAlbedo

```csharp
public bool VertexColorUseAsAlbedo { get; set; }
```

引擎名：`vertex_color_use_as_albedo`

If `true`, the vertex color is used as albedo color.

### ZClipScale

```csharp
public float ZClipScale { get; set; }
```

引擎名：`z_clip_scale`

Scales the object being rendered towards the camera to avoid clipping into things like walls. This is intended to be used for objects that are fixed with respect to the camera like player arms, tools, etc. Lighting and shadows will continue to work correctly when this setting is adjusted, but screen-space effects like SSAO and SSR may break with lower scales. Therefore, try to keep this setting as close to `1.0` as possible.

## 方法

### GetAOLightAffect

```csharp
public float GetAOLightAffect()
```

引擎名：`get_ao_light_affect`

### GetAOTextureChannel

```csharp
public BaseMaterial3D.TextureChannel GetAOTextureChannel()
```

引擎名：`get_ao_texture_channel`

### GetAlbedo

```csharp
public Color GetAlbedo()
```

引擎名：`get_albedo`

### GetAlphaAntialiasing

```csharp
public BaseMaterial3D.AlphaAntiAliasing GetAlphaAntialiasing()
```

引擎名：`get_alpha_antialiasing`

### GetAlphaAntialiasingEdge

```csharp
public float GetAlphaAntialiasingEdge()
```

引擎名：`get_alpha_antialiasing_edge`

### GetAlphaHashScale

```csharp
public float GetAlphaHashScale()
```

引擎名：`get_alpha_hash_scale`

### GetAlphaScissorThreshold

```csharp
public float GetAlphaScissorThreshold()
```

引擎名：`get_alpha_scissor_threshold`

### GetAnisotropy

```csharp
public float GetAnisotropy()
```

引擎名：`get_anisotropy`

### GetBacklight

```csharp
public Color GetBacklight()
```

引擎名：`get_backlight`

### GetBillboardMode

```csharp
public BaseMaterial3D.BillboardModeEnum GetBillboardMode()
```

引擎名：`get_billboard_mode`

### GetBlendMode

```csharp
public BaseMaterial3D.BlendModeEnum GetBlendMode()
```

引擎名：`get_blend_mode`

### GetClearcoat

```csharp
public float GetClearcoat()
```

引擎名：`get_clearcoat`

### GetClearcoatRoughness

```csharp
public float GetClearcoatRoughness()
```

引擎名：`get_clearcoat_roughness`

### GetCullMode

```csharp
public BaseMaterial3D.CullModeEnum GetCullMode()
```

引擎名：`get_cull_mode`

### GetDepthDrawMode

```csharp
public BaseMaterial3D.DepthDrawModeEnum GetDepthDrawMode()
```

引擎名：`get_depth_draw_mode`

### GetDepthTest

```csharp
public BaseMaterial3D.DepthTestEnum GetDepthTest()
```

引擎名：`get_depth_test`

### GetDetailBlendMode

```csharp
public BaseMaterial3D.BlendModeEnum GetDetailBlendMode()
```

引擎名：`get_detail_blend_mode`

### GetDetailUV

```csharp
public BaseMaterial3D.DetailUV GetDetailUV()
```

引擎名：`get_detail_uv`

### GetDiffuseMode

```csharp
public BaseMaterial3D.DiffuseModeEnum GetDiffuseMode()
```

引擎名：`get_diffuse_mode`

### GetDistanceFade

```csharp
public BaseMaterial3D.DistanceFadeModeEnum GetDistanceFade()
```

引擎名：`get_distance_fade`

### GetDistanceFadeMaxDistance

```csharp
public float GetDistanceFadeMaxDistance()
```

引擎名：`get_distance_fade_max_distance`

### GetDistanceFadeMinDistance

```csharp
public float GetDistanceFadeMinDistance()
```

引擎名：`get_distance_fade_min_distance`

### GetEmission

```csharp
public Color GetEmission()
```

引擎名：`get_emission`

### GetEmissionEnergyMultiplier

```csharp
public float GetEmissionEnergyMultiplier()
```

引擎名：`get_emission_energy_multiplier`

### GetEmissionIntensity

```csharp
public float GetEmissionIntensity()
```

引擎名：`get_emission_intensity`

### GetEmissionOperator

```csharp
public BaseMaterial3D.EmissionOperatorEnum GetEmissionOperator()
```

引擎名：`get_emission_operator`

### GetFeature

```csharp
public bool GetFeature(BaseMaterial3D.Feature feature)
```

引擎名：`get_feature`

Returns `true` if the specified `feature` is enabled.

### GetFlag

```csharp
public bool GetFlag(BaseMaterial3D.Flags flag)
```

引擎名：`get_flag`

Returns `true` if the specified `flag` is enabled.

### GetFovOverride

```csharp
public float GetFovOverride()
```

引擎名：`get_fov_override`

### GetGrow

```csharp
public float GetGrow()
```

引擎名：`get_grow`

### GetHeightmapDeepParallaxFlipBinormal

```csharp
public bool GetHeightmapDeepParallaxFlipBinormal()
```

引擎名：`get_heightmap_deep_parallax_flip_binormal`

### GetHeightmapDeepParallaxFlipTangent

```csharp
public bool GetHeightmapDeepParallaxFlipTangent()
```

引擎名：`get_heightmap_deep_parallax_flip_tangent`

### GetHeightmapDeepParallaxMaxLayers

```csharp
public int GetHeightmapDeepParallaxMaxLayers()
```

引擎名：`get_heightmap_deep_parallax_max_layers`

### GetHeightmapDeepParallaxMinLayers

```csharp
public int GetHeightmapDeepParallaxMinLayers()
```

引擎名：`get_heightmap_deep_parallax_min_layers`

### GetHeightmapScale

```csharp
public float GetHeightmapScale()
```

引擎名：`get_heightmap_scale`

### GetMetallic

```csharp
public float GetMetallic()
```

引擎名：`get_metallic`

### GetMetallicTextureChannel

```csharp
public BaseMaterial3D.TextureChannel GetMetallicTextureChannel()
```

引擎名：`get_metallic_texture_channel`

### GetMsdfOutlineSize

```csharp
public float GetMsdfOutlineSize()
```

引擎名：`get_msdf_outline_size`

### GetMsdfPixelRange

```csharp
public float GetMsdfPixelRange()
```

引擎名：`get_msdf_pixel_range`

### GetNormalScale

```csharp
public float GetNormalScale()
```

引擎名：`get_normal_scale`

### GetParticlesAnimHFrames

```csharp
public int GetParticlesAnimHFrames()
```

引擎名：`get_particles_anim_h_frames`

### GetParticlesAnimLoop

```csharp
public bool GetParticlesAnimLoop()
```

引擎名：`get_particles_anim_loop`

### GetParticlesAnimVFrames

```csharp
public int GetParticlesAnimVFrames()
```

引擎名：`get_particles_anim_v_frames`

### GetPointSize

```csharp
public float GetPointSize()
```

引擎名：`get_point_size`

### GetProximityFadeDistance

```csharp
public float GetProximityFadeDistance()
```

引擎名：`get_proximity_fade_distance`

### GetRefraction

```csharp
public float GetRefraction()
```

引擎名：`get_refraction`

### GetRefractionTextureChannel

```csharp
public BaseMaterial3D.TextureChannel GetRefractionTextureChannel()
```

引擎名：`get_refraction_texture_channel`

### GetRim

```csharp
public float GetRim()
```

引擎名：`get_rim`

### GetRimTint

```csharp
public float GetRimTint()
```

引擎名：`get_rim_tint`

### GetRoughness

```csharp
public float GetRoughness()
```

引擎名：`get_roughness`

### GetRoughnessTextureChannel

```csharp
public BaseMaterial3D.TextureChannel GetRoughnessTextureChannel()
```

引擎名：`get_roughness_texture_channel`

### GetShadingMode

```csharp
public BaseMaterial3D.ShadingModeEnum GetShadingMode()
```

引擎名：`get_shading_mode`

### GetSpecular

```csharp
public float GetSpecular()
```

引擎名：`get_specular`

### GetSpecularMode

```csharp
public BaseMaterial3D.SpecularModeEnum GetSpecularMode()
```

引擎名：`get_specular_mode`

### GetStencilCompare

```csharp
public BaseMaterial3D.StencilCompareEnum GetStencilCompare()
```

引擎名：`get_stencil_compare`

### GetStencilEffectColor

```csharp
public Color GetStencilEffectColor()
```

引擎名：`get_stencil_effect_color`

### GetStencilEffectOutlineThickness

```csharp
public float GetStencilEffectOutlineThickness()
```

引擎名：`get_stencil_effect_outline_thickness`

### GetStencilFlags

```csharp
public int GetStencilFlags()
```

引擎名：`get_stencil_flags`

### GetStencilMode

```csharp
public BaseMaterial3D.StencilModeEnum GetStencilMode()
```

引擎名：`get_stencil_mode`

### GetStencilReference

```csharp
public int GetStencilReference()
```

引擎名：`get_stencil_reference`

### GetSubsurfaceScatteringStrength

```csharp
public float GetSubsurfaceScatteringStrength()
```

引擎名：`get_subsurface_scattering_strength`

### GetTexture

```csharp
public Texture2D GetTexture(BaseMaterial3D.TextureParam param)
```

引擎名：`get_texture`

Returns the [`Texture2D`](Texture2D.md) associated with the specified texture `param`.

### GetTextureFilter

```csharp
public BaseMaterial3D.TextureFilterEnum GetTextureFilter()
```

引擎名：`get_texture_filter`

### GetTransmittanceBoost

```csharp
public float GetTransmittanceBoost()
```

引擎名：`get_transmittance_boost`

### GetTransmittanceColor

```csharp
public Color GetTransmittanceColor()
```

引擎名：`get_transmittance_color`

### GetTransmittanceDepth

```csharp
public float GetTransmittanceDepth()
```

引擎名：`get_transmittance_depth`

### GetTransparency

```csharp
public BaseMaterial3D.TransparencyEnum GetTransparency()
```

引擎名：`get_transparency`

### GetUV2Offset

```csharp
public Vector3 GetUV2Offset()
```

引擎名：`get_uv2_offset`

### GetUV2Scale

```csharp
public Vector3 GetUV2Scale()
```

引擎名：`get_uv2_scale`

### GetUV2TriplanarBlendSharpness

```csharp
public float GetUV2TriplanarBlendSharpness()
```

引擎名：`get_uv2_triplanar_blend_sharpness`

### GetUv1Offset

```csharp
public Vector3 GetUv1Offset()
```

引擎名：`get_uv1_offset`

### GetUv1Scale

```csharp
public Vector3 GetUv1Scale()
```

引擎名：`get_uv1_scale`

### GetUv1TriplanarBlendSharpness

```csharp
public float GetUv1TriplanarBlendSharpness()
```

引擎名：`get_uv1_triplanar_blend_sharpness`

### GetZClipScale

```csharp
public float GetZClipScale()
```

引擎名：`get_z_clip_scale`

### IsGrowEnabled

```csharp
public bool IsGrowEnabled()
```

引擎名：`is_grow_enabled`

### IsHeightmapDeepParallaxEnabled

```csharp
public bool IsHeightmapDeepParallaxEnabled()
```

引擎名：`is_heightmap_deep_parallax_enabled`

### IsProximityFadeEnabled

```csharp
public bool IsProximityFadeEnabled()
```

引擎名：`is_proximity_fade_enabled`

### SetAOLightAffect

```csharp
public void SetAOLightAffect(float amount)
```

引擎名：`set_ao_light_affect`

### SetAOTextureChannel

```csharp
public void SetAOTextureChannel(BaseMaterial3D.TextureChannel channel)
```

引擎名：`set_ao_texture_channel`

### SetAlbedo

```csharp
public void SetAlbedo(Color albedo)
```

引擎名：`set_albedo`

### SetAlphaAntialiasing

```csharp
public void SetAlphaAntialiasing(BaseMaterial3D.AlphaAntiAliasing alphaAA)
```

引擎名：`set_alpha_antialiasing`

### SetAlphaAntialiasingEdge

```csharp
public void SetAlphaAntialiasingEdge(float edge)
```

引擎名：`set_alpha_antialiasing_edge`

### SetAlphaHashScale

```csharp
public void SetAlphaHashScale(float threshold)
```

引擎名：`set_alpha_hash_scale`

### SetAlphaScissorThreshold

```csharp
public void SetAlphaScissorThreshold(float threshold)
```

引擎名：`set_alpha_scissor_threshold`

### SetAnisotropy

```csharp
public void SetAnisotropy(float anisotropy)
```

引擎名：`set_anisotropy`

### SetBacklight

```csharp
public void SetBacklight(Color backlight)
```

引擎名：`set_backlight`

### SetBillboardMode

```csharp
public void SetBillboardMode(BaseMaterial3D.BillboardModeEnum mode)
```

引擎名：`set_billboard_mode`

### SetBlendMode

```csharp
public void SetBlendMode(BaseMaterial3D.BlendModeEnum blendMode)
```

引擎名：`set_blend_mode`

### SetClearcoat

```csharp
public void SetClearcoat(float clearcoat)
```

引擎名：`set_clearcoat`

### SetClearcoatRoughness

```csharp
public void SetClearcoatRoughness(float clearcoatRoughness)
```

引擎名：`set_clearcoat_roughness`

### SetCullMode

```csharp
public void SetCullMode(BaseMaterial3D.CullModeEnum cullMode)
```

引擎名：`set_cull_mode`

### SetDepthDrawMode

```csharp
public void SetDepthDrawMode(BaseMaterial3D.DepthDrawModeEnum depthDrawMode)
```

引擎名：`set_depth_draw_mode`

### SetDepthTest

```csharp
public void SetDepthTest(BaseMaterial3D.DepthTestEnum depthTest)
```

引擎名：`set_depth_test`

### SetDetailBlendMode

```csharp
public void SetDetailBlendMode(BaseMaterial3D.BlendModeEnum detailBlendMode)
```

引擎名：`set_detail_blend_mode`

### SetDetailUV

```csharp
public void SetDetailUV(BaseMaterial3D.DetailUV detailUV)
```

引擎名：`set_detail_uv`

### SetDiffuseMode

```csharp
public void SetDiffuseMode(BaseMaterial3D.DiffuseModeEnum diffuseMode)
```

引擎名：`set_diffuse_mode`

### SetDistanceFade

```csharp
public void SetDistanceFade(BaseMaterial3D.DistanceFadeModeEnum mode)
```

引擎名：`set_distance_fade`

### SetDistanceFadeMaxDistance

```csharp
public void SetDistanceFadeMaxDistance(float distance)
```

引擎名：`set_distance_fade_max_distance`

### SetDistanceFadeMinDistance

```csharp
public void SetDistanceFadeMinDistance(float distance)
```

引擎名：`set_distance_fade_min_distance`

### SetEmission

```csharp
public void SetEmission(Color emission)
```

引擎名：`set_emission`

### SetEmissionEnergyMultiplier

```csharp
public void SetEmissionEnergyMultiplier(float emissionEnergyMultiplier)
```

引擎名：`set_emission_energy_multiplier`

### SetEmissionIntensity

```csharp
public void SetEmissionIntensity(float emissionEnergyMultiplier)
```

引擎名：`set_emission_intensity`

### SetEmissionOperator

```csharp
public void SetEmissionOperator(BaseMaterial3D.EmissionOperatorEnum operator)
```

引擎名：`set_emission_operator`

### SetFeature

```csharp
public void SetFeature(BaseMaterial3D.Feature feature, bool enable)
```

引擎名：`set_feature`

If `enable` is `true`, enables the specified `feature`. Many features that are available in `BaseMaterial3D` need to be enabled before use. This way, the cost for using the feature is only incurred when specified. Features can also be enabled by setting their corresponding property to `true`.

### SetFlag

```csharp
public void SetFlag(BaseMaterial3D.Flags flag, bool enable)
```

引擎名：`set_flag`

If `enable` is `true`, enables the specified `flag`. Flags are optional behavior that can be turned on and off. Only one flag can be enabled at a time with this function, the flag enumerators cannot be bit-masked together to enable or disable multiple flags at once. Flags can also be enabled by setting their corresponding property to `true`.

### SetFovOverride

```csharp
public void SetFovOverride(float scale)
```

引擎名：`set_fov_override`

### SetGrow

```csharp
public void SetGrow(float amount)
```

引擎名：`set_grow`

### SetGrowEnabled

```csharp
public void SetGrowEnabled(bool enable)
```

引擎名：`set_grow_enabled`

### SetHeightmapDeepParallax

```csharp
public void SetHeightmapDeepParallax(bool enable)
```

引擎名：`set_heightmap_deep_parallax`

### SetHeightmapDeepParallaxFlipBinormal

```csharp
public void SetHeightmapDeepParallaxFlipBinormal(bool flip)
```

引擎名：`set_heightmap_deep_parallax_flip_binormal`

### SetHeightmapDeepParallaxFlipTangent

```csharp
public void SetHeightmapDeepParallaxFlipTangent(bool flip)
```

引擎名：`set_heightmap_deep_parallax_flip_tangent`

### SetHeightmapDeepParallaxMaxLayers

```csharp
public void SetHeightmapDeepParallaxMaxLayers(int layer)
```

引擎名：`set_heightmap_deep_parallax_max_layers`

### SetHeightmapDeepParallaxMinLayers

```csharp
public void SetHeightmapDeepParallaxMinLayers(int layer)
```

引擎名：`set_heightmap_deep_parallax_min_layers`

### SetHeightmapScale

```csharp
public void SetHeightmapScale(float heightmapScale)
```

引擎名：`set_heightmap_scale`

### SetMetallic

```csharp
public void SetMetallic(float metallic)
```

引擎名：`set_metallic`

### SetMetallicTextureChannel

```csharp
public void SetMetallicTextureChannel(BaseMaterial3D.TextureChannel channel)
```

引擎名：`set_metallic_texture_channel`

### SetMsdfOutlineSize

```csharp
public void SetMsdfOutlineSize(float size)
```

引擎名：`set_msdf_outline_size`

### SetMsdfPixelRange

```csharp
public void SetMsdfPixelRange(float range)
```

引擎名：`set_msdf_pixel_range`

### SetNormalScale

```csharp
public void SetNormalScale(float normalScale)
```

引擎名：`set_normal_scale`

### SetParticlesAnimHFrames

```csharp
public void SetParticlesAnimHFrames(int frames)
```

引擎名：`set_particles_anim_h_frames`

### SetParticlesAnimLoop

```csharp
public void SetParticlesAnimLoop(bool loop)
```

引擎名：`set_particles_anim_loop`

### SetParticlesAnimVFrames

```csharp
public void SetParticlesAnimVFrames(int frames)
```

引擎名：`set_particles_anim_v_frames`

### SetPointSize

```csharp
public void SetPointSize(float pointSize)
```

引擎名：`set_point_size`

### SetProximityFadeDistance

```csharp
public void SetProximityFadeDistance(float distance)
```

引擎名：`set_proximity_fade_distance`

### SetProximityFadeEnabled

```csharp
public void SetProximityFadeEnabled(bool enabled)
```

引擎名：`set_proximity_fade_enabled`

### SetRefraction

```csharp
public void SetRefraction(float refraction)
```

引擎名：`set_refraction`

### SetRefractionTextureChannel

```csharp
public void SetRefractionTextureChannel(BaseMaterial3D.TextureChannel channel)
```

引擎名：`set_refraction_texture_channel`

### SetRim

```csharp
public void SetRim(float rim)
```

引擎名：`set_rim`

### SetRimTint

```csharp
public void SetRimTint(float rimTint)
```

引擎名：`set_rim_tint`

### SetRoughness

```csharp
public void SetRoughness(float roughness)
```

引擎名：`set_roughness`

### SetRoughnessTextureChannel

```csharp
public void SetRoughnessTextureChannel(BaseMaterial3D.TextureChannel channel)
```

引擎名：`set_roughness_texture_channel`

### SetShadingMode

```csharp
public void SetShadingMode(BaseMaterial3D.ShadingModeEnum shadingMode)
```

引擎名：`set_shading_mode`

### SetSpecular

```csharp
public void SetSpecular(float specular)
```

引擎名：`set_specular`

### SetSpecularMode

```csharp
public void SetSpecularMode(BaseMaterial3D.SpecularModeEnum specularMode)
```

引擎名：`set_specular_mode`

### SetStencilCompare

```csharp
public void SetStencilCompare(BaseMaterial3D.StencilCompareEnum stencilCompare)
```

引擎名：`set_stencil_compare`

### SetStencilEffectColor

```csharp
public void SetStencilEffectColor(Color stencilColor)
```

引擎名：`set_stencil_effect_color`

### SetStencilEffectOutlineThickness

```csharp
public void SetStencilEffectOutlineThickness(float stencilOutlineThickness)
```

引擎名：`set_stencil_effect_outline_thickness`

### SetStencilFlags

```csharp
public void SetStencilFlags(int stencilFlags)
```

引擎名：`set_stencil_flags`

### SetStencilMode

```csharp
public void SetStencilMode(BaseMaterial3D.StencilModeEnum stencilMode)
```

引擎名：`set_stencil_mode`

### SetStencilReference

```csharp
public void SetStencilReference(int stencilReference)
```

引擎名：`set_stencil_reference`

### SetSubsurfaceScatteringStrength

```csharp
public void SetSubsurfaceScatteringStrength(float strength)
```

引擎名：`set_subsurface_scattering_strength`

### SetTexture

```csharp
public void SetTexture(BaseMaterial3D.TextureParam param, Texture2D texture)
```

引擎名：`set_texture`

Sets the texture for the slot specified by `param`.

### SetTextureFilter

```csharp
public void SetTextureFilter(BaseMaterial3D.TextureFilterEnum mode)
```

引擎名：`set_texture_filter`

### SetTransmittanceBoost

```csharp
public void SetTransmittanceBoost(float boost)
```

引擎名：`set_transmittance_boost`

### SetTransmittanceColor

```csharp
public void SetTransmittanceColor(Color color)
```

引擎名：`set_transmittance_color`

### SetTransmittanceDepth

```csharp
public void SetTransmittanceDepth(float depth)
```

引擎名：`set_transmittance_depth`

### SetTransparency

```csharp
public void SetTransparency(BaseMaterial3D.TransparencyEnum transparency)
```

引擎名：`set_transparency`

### SetUV2Offset

```csharp
public void SetUV2Offset(Vector3 offset)
```

引擎名：`set_uv2_offset`

### SetUV2Scale

```csharp
public void SetUV2Scale(Vector3 scale)
```

引擎名：`set_uv2_scale`

### SetUV2TriplanarBlendSharpness

```csharp
public void SetUV2TriplanarBlendSharpness(float sharpness)
```

引擎名：`set_uv2_triplanar_blend_sharpness`

### SetUv1Offset

```csharp
public void SetUv1Offset(Vector3 offset)
```

引擎名：`set_uv1_offset`

### SetUv1Scale

```csharp
public void SetUv1Scale(Vector3 scale)
```

引擎名：`set_uv1_scale`

### SetUv1TriplanarBlendSharpness

```csharp
public void SetUv1TriplanarBlendSharpness(float sharpness)
```

引擎名：`set_uv1_triplanar_blend_sharpness`

### SetZClipScale

```csharp
public void SetZClipScale(float scale)
```

引擎名：`set_z_clip_scale`

## AlphaAntiAliasing

命名空间 `Godot`；enum

### 枚举值

- `Off = 0`：Disables Alpha AntiAliasing for the material.
- `AlphaToCoverage = 1`：Enables AlphaToCoverage.
- `AlphaToCoverageAndToOne = 2`：Enables AlphaToCoverage and forces all non-zero alpha values to `1`.

## BillboardModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Billboard mode is disabled.
- `Enabled = 1`：The object's Z axis will always face the camera.
- `FixedY = 2`：The object's X axis will always face the camera.
- `Particles = 3`：Used for particle systems when assigned to [`GpuParticles3D`](GpuParticles3D.md) and [`CpuParticles3D`](CpuParticles3D.md) nodes (flipbook animation).

## BlendModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Mix = 0`：Default blend mode.
- `Add = 1`：The color of the object is added to the background.
- `Sub = 2`：The color of the object is subtracted from the background.
- `Mul = 3`：The color of the object is multiplied by the background.
- `PremultAlpha = 4`：The color of the object is added to the background and the alpha channel is used to mask out the background.

## CullModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Back = 0`：Default cull mode.
- `Front = 1`：Front face triangles will be culled when facing the camera.
- `Disabled = 2`：No face culling is performed; both the front face and back face will be visible.

## DepthDrawModeEnum

命名空间 `Godot`；enum

### 枚举值

- `OpaqueOnly = 0`：Default depth draw mode.
- `Always = 1`：Objects will write to depth during the opaque and the transparent passes.
- `Disabled = 2`：Objects will not write their depth to the depth buffer, even during the depth prepass (if enabled).

## DepthTestEnum

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Depth test will discard the pixel if it is behind other pixels.
- `Inverted = 1`：Depth test will discard the pixel if it is in front of other pixels.

## DetailUV

命名空间 `Godot`；enum

### 枚举值

- `UV1 = 0`：Use `UV` with the detail texture.
- `UV2 = 1`：Use `UV2` with the detail texture.

## DiffuseModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Burley = 0`：Default diffuse scattering algorithm.
- `Lambert = 1`：Diffuse scattering ignores roughness.
- `LambertWrap = 2`：Extends Lambert to cover more than 90 degrees when roughness increases.
- `Toon = 3`：Uses a hard cut for lighting, with smoothing affected by roughness.

## DistanceFadeModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not use distance fade.
- `PixelAlpha = 1`：Smoothly fades the object out based on each pixel's distance from the camera using the alpha channel.
- `PixelDither = 2`：Smoothly fades the object out based on each pixel's distance from the camera using a dithering approach.
- `ObjectDither = 3`：Smoothly fades the object out based on the object's distance from the camera using a dithering approach.

## EmissionOperatorEnum

命名空间 `Godot`；enum

### 枚举值

- `Add = 0`：Adds the emission color to the color from the emission texture.
- `Multiply = 1`：Multiplies the emission color by the color from the emission texture.

## Feature

命名空间 `Godot`；enum

### 枚举值

- `Emission = 0`：Constant for setting [`BaseMaterial3D.EmissionEnabled`](BaseMaterial3D.md).
- `NormalMapping = 1`：Constant for setting [`BaseMaterial3D.NormalEnabled`](BaseMaterial3D.md).
- `Rim = 2`：Constant for setting [`BaseMaterial3D.RimEnabled`](BaseMaterial3D.md).
- `Clearcoat = 3`：Constant for setting [`BaseMaterial3D.ClearcoatEnabled`](BaseMaterial3D.md).
- `Anisotropy = 4`：Constant for setting [`BaseMaterial3D.AnisotropyEnabled`](BaseMaterial3D.md).
- `AmbientOcclusion = 5`：Constant for setting [`BaseMaterial3D.AOEnabled`](BaseMaterial3D.md).
- `HeightMapping = 6`：Constant for setting [`BaseMaterial3D.HeightmapEnabled`](BaseMaterial3D.md).
- `SubsurfaceScattering = 7`：Constant for setting [`BaseMaterial3D.SubsurfScatterEnabled`](BaseMaterial3D.md).
- `SubsurfaceTransmittance = 8`：Constant for setting [`BaseMaterial3D.SubsurfScatterTransmittanceEnabled`](BaseMaterial3D.md).
- `Backlight = 9`：Constant for setting [`BaseMaterial3D.BacklightEnabled`](BaseMaterial3D.md).
- `Refraction = 10`：Constant for setting [`BaseMaterial3D.RefractionEnabled`](BaseMaterial3D.md).
- `Detail = 11`：Constant for setting [`BaseMaterial3D.DetailEnabled`](BaseMaterial3D.md).
- `BentNormalMapping = 12`：Constant for setting [`BaseMaterial3D.BentNormalEnabled`](BaseMaterial3D.md).
- `Max = 13`：Represents the size of the `BaseMaterial3D.Feature` enum.

## Flags

命名空间 `Godot`；enum

### 枚举值

- `DisableDepthTest = 0`：Disables the depth test, so this object is drawn on top of all others drawn before it.
- `AlbedoFromVertexColor = 1`：Set `ALBEDO` to the per-vertex color specified in the mesh.
- `SrgbVertexColor = 2`：Vertex colors are considered to be stored in nonlinear sRGB encoding and are converted to linear encoding during rendering.
- `UsePointSize = 3`：Uses point size to alter the size of primitive points.
- `FixedSize = 4`：Object is scaled by depth so that it always appears the same size on screen.
- `BillboardKeepScale = 5`：Shader will keep the scale set for the mesh.
- `Uv1UseTriplanar = 6`：Use triplanar texture lookup for all texture lookups that would normally use `UV`.
- `UV2UseTriplanar = 7`：Use triplanar texture lookup for all texture lookups that would normally use `UV2`.
- `Uv1UseWorldTriplanar = 8`：Use triplanar texture lookup for all texture lookups that would normally use `UV`.
- `UV2UseWorldTriplanar = 9`：Use triplanar texture lookup for all texture lookups that would normally use `UV2`.
- `AOOnUV2 = 10`：Use `UV2` coordinates to look up from the [`BaseMaterial3D.AOTexture`](BaseMaterial3D.md).
- `EmissionOnUV2 = 11`：Use `UV2` coordinates to look up from the [`BaseMaterial3D.EmissionTexture`](BaseMaterial3D.md).
- `AlbedoTextureForceSrgb = 12`：Forces the shader to convert albedo from nonlinear sRGB encoding to linear encoding.
- `DontReceiveShadows = 13`：Disables receiving shadows from other objects.
- `DisableAmbientLight = 14`：Disables receiving ambient light.
- `UseShadowToOpacity = 15`：Enables the shadow to opacity feature.
- `UseTextureRepeat = 16`：Enables the texture to repeat when UV coordinates are outside the 0-1 range.
- `InvertHeightmap = 17`：Invert values read from a depth texture to convert them to height values (heightmap).
- `SubsurfaceModeSkin = 18`：Enables the skin mode for subsurface scattering which is used to improve the look of subsurface scattering when used for human skin.
- `ParticleTrailsMode = 19`：Enables parts of the shader required for [`GpuParticles3D`](GpuParticles3D.md) trails to function.
- `AlbedoTextureMsdf = 20`：Enables multichannel signed distance field rendering shader.
- `DisableFog = 21`：Disables receiving depth-based or volumetric fog.
- `DisableSpecularOcclusion = 22`：Disables specular occlusion.
- `UseZClipScale = 23`：Enables using [`BaseMaterial3D.ZClipScale`](BaseMaterial3D.md).
- `UseFovOverride = 24`：Enables using [`BaseMaterial3D.FovOverride`](BaseMaterial3D.md).
- `Max = 25`：Represents the size of the `BaseMaterial3D.Flags` enum.

## ShadingModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Unshaded = 0`：The object will not receive shadows.
- `PerPixel = 1`：The object will be shaded per pixel.
- `PerVertex = 2`：The object will be shaded per vertex.
- `Max = 3`：Represents the size of the `BaseMaterial3D.ShadingModeEnum` enum.

## SpecularModeEnum

命名空间 `Godot`；enum

### 枚举值

- `SchlickGgx = 0`：Default specular blob.
- `Toon = 1`：Toon blob which changes size based on roughness.
- `Disabled = 2`：No specular blob.

## StencilCompareEnum

命名空间 `Godot`；enum

### 枚举值

- `Always = 0`：Always passes the stencil test.
- `Less = 1`：Passes the stencil test when the reference value is less than the existing stencil value.
- `Equal = 2`：Passes the stencil test when the reference value is equal to the existing stencil value.
- `LessOrEqual = 3`：Passes the stencil test when the reference value is less than or equal to the existing stencil value.
- `Greater = 4`：Passes the stencil test when the reference value is greater than the existing stencil value.
- `NotEqual = 5`：Passes the stencil test when the reference value is not equal to the existing stencil value.
- `GreaterOrEqual = 6`：Passes the stencil test when the reference value is greater than or equal to the existing stencil value.

## StencilFlagsEnum

命名空间 `Godot`；enum

### 枚举值

- `Read = 1`：The material will only be rendered where it passes a stencil comparison with existing stencil buffer values.
- `Write = 2`：The material will write the reference value to the stencil buffer where it passes the depth test.
- `WriteDepthFail = 4`：The material will write the reference value to the stencil buffer where it fails the depth test.

## StencilModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables stencil operations.
- `Outline = 1`：Stencil preset which applies an outline to the object.
- `Xray = 2`：Stencil preset which shows a silhouette of the object behind walls.
- `Custom = 3`：Enables stencil operations without a preset.

## TextureChannel

命名空间 `Godot`；enum

### 枚举值

- `Red = 0`：Used to read from the red channel of a texture.
- `Green = 1`：Used to read from the green channel of a texture.
- `Blue = 2`：Used to read from the blue channel of a texture.
- `Alpha = 3`：Used to read from the alpha channel of a texture.
- `Grayscale = 4`：Used to read from the linear (non-perceptual) average of the red, green and blue channels of a texture.

## TextureFilterEnum

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：The texture filter reads from the nearest pixel only.
- `Linear = 1`：The texture filter blends between the nearest 4 pixels.
- `NearestWithMipmaps = 2`：The texture filter reads from the nearest pixel and blends between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `LinearWithMipmaps = 3`：The texture filter blends between the nearest 4 pixels and between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `NearestWithMipmapsAnisotropic = 4`：The texture filter reads from the nearest pixel and blends between 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`) based on the angle bet…
- `LinearWithMipmapsAnisotropic = 5`：The texture filter blends between the nearest 4 pixels and blends between 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`) based on the an…
- `Max = 6`：Represents the size of the `BaseMaterial3D.TextureFilterEnum` enum.

## TextureParam

命名空间 `Godot`；enum

### 枚举值

- `Albedo = 0`：Texture specifying per-pixel color.
- `Metallic = 1`：Texture specifying per-pixel metallic value.
- `Roughness = 2`：Texture specifying per-pixel roughness value.
- `Emission = 3`：Texture specifying per-pixel emission color.
- `Normal = 4`：Texture specifying per-pixel normal vector.
- `BentNormal = 18`：Texture specifying per-pixel bent normal vector.
- `Rim = 5`：Texture specifying per-pixel rim value.
- `Clearcoat = 6`：Texture specifying per-pixel clearcoat value.
- `Flowmap = 7`：Texture specifying per-pixel flowmap direction for use with [`BaseMaterial3D.Anisotropy`](BaseMaterial3D.md).
- `AmbientOcclusion = 8`：Texture specifying per-pixel ambient occlusion value.
- `Heightmap = 9`：Texture specifying per-pixel height.
- `SubsurfaceScattering = 10`：Texture specifying per-pixel subsurface scattering.
- `SubsurfaceTransmittance = 11`：Texture specifying per-pixel transmittance for subsurface scattering.
- `Backlight = 12`：Texture specifying per-pixel backlight color.
- `Refraction = 13`：Texture specifying per-pixel refraction strength.
- `DetailMask = 14`：Texture specifying per-pixel detail mask blending value.
- `DetailAlbedo = 15`：Texture specifying per-pixel detail color.
- `DetailNormal = 16`：Texture specifying per-pixel detail normal.
- `Orm = 17`：Texture holding ambient occlusion, roughness, and metallic.
- `Max = 19`：Represents the size of the `BaseMaterial3D.TextureParam` enum.

## TransparencyEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：The material will not use transparency.
- `Alpha = 1`：The material will use the texture's alpha values for transparency.
- `AlphaScissor = 2`：The material will cut off all values below a threshold, the rest will remain opaque.
- `AlphaHash = 3`：The material will cut off all values below a spatially-deterministic threshold, the rest will remain opaque.
- `AlphaDepthPrePass = 4`：The material will use the texture's alpha value for transparency, but will discard fragments with an alpha of less than 0.99 during the depth prepass and fragments with an alpha less than 0.1 during the shadow pass.
- `Max = 5`：Represents the size of the `BaseMaterial3D.TransparencyEnum` enum.
