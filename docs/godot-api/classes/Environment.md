<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Environment

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Resource for environment nodes (like [`WorldEnvironment`](WorldEnvironment.md)) that define multiple environment operations (such as background [`Sky`](Sky.md) or [`Color`](Color.md), ambient light, fog, depth-of-field...). These parameters affect the final render of the scene. The order of these operations is:

- Depth of Field Blur

- Auto Exposure

- Glow

- Tonemap

- Adjustments

## 构造函数

### Environment

```csharp
public Environment()
```

## 属性

### AdjustmentBrightness

```csharp
public float AdjustmentBrightness { get; set; }
```

引擎名：`adjustment_brightness`

Applies a simple brightness adjustment to the rendered image after tonemaping. To adjust scene brightness use `TonemapExposure` instead, which is applied before tonemapping and thus less prone to issues with bright colors. Effective only if `AdjustmentEnabled` is `true`.

### AdjustmentColorCorrection

```csharp
public Texture AdjustmentColorCorrection { get; set; }
```

引擎名：`adjustment_color_correction`

The [`Texture2D`](Texture2D.md) or [`Texture3D`](Texture3D.md) lookup table (LUT) to use for the built-in post-process color grading. Can use a [`GradientTexture1D`](GradientTexture1D.md) for a 1-dimensional LUT, or a [`Texture3D`](Texture3D.md) for a more complex LUT. Effective only if `AdjustmentEnabled` is `true`.

**Note:** Color correction does not currently support HDR output due to only supporting values in the SDR (0.0 to 1.0) range.

### AdjustmentContrast

```csharp
public float AdjustmentContrast { get; set; }
```

引擎名：`adjustment_contrast`

Increasing `AdjustmentContrast` will make dark values darker and bright values brighter. This simple adjustment is applied to the rendered image after tonemaping. When set to a value greater than `1.0`, `AdjustmentContrast` is prone to clipping colors that become too bright or too dark. Effective only if `AdjustmentEnabled` is `true`.

### AdjustmentEnabled

```csharp
public bool AdjustmentEnabled { get; set; }
```

引擎名：`adjustment_enabled`

If `true`, enables the `adjustment_*` properties provided by this resource. If `false`, modifications to the `adjustment_*` properties will have no effect on the rendered scene.

### AdjustmentSaturation

```csharp
public float AdjustmentSaturation { get; set; }
```

引擎名：`adjustment_saturation`

Applies a simple saturation adjustment to the rendered image after tonemaping. When `AdjustmentSaturation` is set to `0.0`, the rendered image will be fully converted to a grayscale image. Effective only if `AdjustmentEnabled` is `true`.

### AmbientLightColor

```csharp
public Color AmbientLightColor { get; set; }
```

引擎名：`ambient_light_color`

The ambient light's [`Color`](Color.md). Only effective if `AmbientLightSkyContribution` is lower than `1.0` (exclusive).

### AmbientLightEnergy

```csharp
public float AmbientLightEnergy { get; set; }
```

引擎名：`ambient_light_energy`

The ambient light's energy. The higher the value, the stronger the light. Only effective if `AmbientLightSkyContribution` is lower than `1.0` (exclusive).

### AmbientLightSkyContribution

```csharp
public float AmbientLightSkyContribution { get; set; }
```

引擎名：`ambient_light_sky_contribution`

Defines the amount of light that the sky brings on the scene. A value of `0.0` means that the sky's light emission has no effect on the scene illumination, thus all ambient illumination is provided by the ambient light. On the contrary, a value of `1.0` means that *all* the light that affects the scene is provided by the sky, thus the ambient light parameter has no effect on the scene.

**Note:** `AmbientLightSkyContribution` is internally clamped between `0.0` and `1.0` (inclusive).

### AmbientLightSource

```csharp
public Environment.AmbientSource AmbientLightSource { get; set; }
```

引擎名：`ambient_light_source`

The ambient light source to use for rendering materials and global illumination.

### BackgroundCameraFeedId

```csharp
public int BackgroundCameraFeedId { get; set; }
```

引擎名：`background_camera_feed_id`

The ID of the camera feed to show in the background.

### BackgroundCanvasMaxLayer

```csharp
public int BackgroundCanvasMaxLayer { get; set; }
```

引擎名：`background_canvas_max_layer`

The maximum layer ID to display. Only effective when using the [`Environment.BGMode.Canvas`](Environment.md) background mode.

### BackgroundColor

```csharp
public Color BackgroundColor { get; set; }
```

引擎名：`background_color`

The [`Color`](Color.md) displayed for clear areas of the scene. Only effective when using the [`Environment.BGMode.Color`](Environment.md) background mode.

### BackgroundEnergyMultiplier

```csharp
public float BackgroundEnergyMultiplier { get; set; }
```

引擎名：`background_energy_multiplier`

Multiplier for background energy. Increase to make background brighter, decrease to make background dimmer.

### BackgroundIntensity

```csharp
public float BackgroundIntensity { get; set; }
```

引擎名：`background_intensity`

Luminance of background measured in nits (candela per square meter). Only used when `ProjectSettings.rendering/lights_and_shadows/use_physical_light_units` is enabled. The default value is roughly equivalent to the sky at midday.

### BackgroundMode

```csharp
public Environment.BGMode BackgroundMode { get; set; }
```

引擎名：`background_mode`

The background mode.

### FogAerialPerspective

```csharp
public float FogAerialPerspective { get; set; }
```

引擎名：`fog_aerial_perspective`

If set above `0.0` (exclusive), blends between the fog's color and the color of the background [`Sky`](Sky.md), as read from the radiance cubemap. This has a small performance cost when set above `0.0`. Must have `BackgroundMode` set to [`Environment.BGMode.Sky`](Environment.md).

This is useful to simulate [aerial perspective](https://en.wikipedia.org/wiki/Aerial_perspective) in large scenes with low density fog. However, it is not very useful for high-density fog, as the sky will shine through. When set to `1.0`, the fog color comes completely from the [`Sky`](Sky.md). If set to `0.0`, aerial perspective is disabled.

Notice that this does not sample the [`Sky`](Sky.md) directly, but rather the radiance cubemap. The cubemap is sampled at a mipmap level depending on the depth of the rendered pixel; the farther away, the higher the resolution of the sampled mipmap. This results in the actual color being a blurred version of the sky, with more blur closer to the camera. The highest mipmap resolution is used at a depth of [`Camera3D.Far`](Camera3D.md).

### FogDensity

```csharp
public float FogDensity { get; set; }
```

引擎名：`fog_density`

The fog density to be used. This is demonstrated in different ways depending on the `FogMode` mode chosen:

**Exponential Fog Mode:** Higher values result in denser fog. The fog rendering is exponential like in real life.

**Depth Fog mode:** The maximum intensity of the deep fog, effect will appear in the distance (relative to the camera). At `1.0` the fog will fully obscure the scene, at `0.0` the fog will not be visible.

### FogDepthBegin

```csharp
public float FogDepthBegin { get; set; }
```

引擎名：`fog_depth_begin`

The fog's depth starting distance from the camera. Only available when `FogMode` is set to [`Environment.FogModeEnum.Depth`](Environment.md).

### FogDepthCurve

```csharp
public float FogDepthCurve { get; set; }
```

引擎名：`fog_depth_curve`

The fog depth's intensity curve. A number of presets are available in the Inspector by right-clicking the curve. Only available when `FogMode` is set to [`Environment.FogModeEnum.Depth`](Environment.md).

### FogDepthEnd

```csharp
public float FogDepthEnd { get; set; }
```

引擎名：`fog_depth_end`

The fog's depth end distance from the camera. If this value is set to `0`, it will be equal to the current camera's [`Camera3D.Far`](Camera3D.md) value. Only available when `FogMode` is set to [`Environment.FogModeEnum.Depth`](Environment.md).

### FogEnabled

```csharp
public bool FogEnabled { get; set; }
```

引擎名：`fog_enabled`

If `true`, fog effects are enabled.

### FogHeight

```csharp
public float FogHeight { get; set; }
```

引擎名：`fog_height`

The height at which the height fog effect begins.

### FogHeightDensity

```csharp
public float FogHeightDensity { get; set; }
```

引擎名：`fog_height_density`

The density used to increase fog as height decreases. To make fog increase as height increases, use a negative value.

### FogLightColor

```csharp
public Color FogLightColor { get; set; }
```

引擎名：`fog_light_color`

The fog's color.

### FogLightEnergy

```csharp
public float FogLightEnergy { get; set; }
```

引擎名：`fog_light_energy`

The fog's brightness. Higher values result in brighter fog.

### FogMode

```csharp
public Environment.FogModeEnum FogMode { get; set; }
```

引擎名：`fog_mode`

The fog mode.

### FogSkyAffect

```csharp
public float FogSkyAffect { get; set; }
```

引擎名：`fog_sky_affect`

The factor to use when affecting the sky with non-volumetric fog. `1.0` means that fog can fully obscure the sky. Lower values reduce the impact of fog on sky rendering, with `0.0` not affecting sky rendering at all.

**Note:** `FogSkyAffect` has no visual effect if `FogAerialPerspective` is `1.0`.

### FogSunScatter

```csharp
public float FogSunScatter { get; set; }
```

引擎名：`fog_sun_scatter`

If set above `0.0`, renders the scene's directional light(s) in the fog color depending on the view angle. This can be used to give the impression that the sun is "piercing" through the fog.

### GlowBlendMode

```csharp
public Environment.GlowBlendModeEnum GlowBlendMode { get; set; }
```

引擎名：`glow_blend_mode`

The glow blending mode.

**Note:** The Compatibility renderer always uses [`Environment.GlowBlendModeEnum.Screen`](Environment.md) and `GlowBlendMode` will have no effect.

### GlowBloom

```csharp
public float GlowBloom { get; set; }
```

引擎名：`glow_bloom`

The bloom's intensity. If set to a value higher than `0`, this will make glow visible in areas darker than the `GlowHdrThreshold`.

### GlowEnabled

```csharp
public bool GlowEnabled { get; set; }
```

引擎名：`glow_enabled`

If `true`, the glow effect is enabled. This simulates real world atmosphere and eye/camera behavior by causing bright pixels to bleed onto surrounding pixels.

**Note:** When using the Mobile rendering method, glow looks different due to the lower dynamic range available in the Mobile rendering method.

**Note:** When using the Compatibility rendering method, glow uses a different implementation with some properties being unavailable and hidden from the inspector: `glow_levels/*`, `GlowNormalized`, `GlowStrength`, `GlowBlendMode`, `GlowMix`, `GlowMap`, and `GlowMapStrength`. This implementation is optimized to run on low-end devices and is less flexible as a result.

### GlowHdrLuminanceCap

```csharp
public float GlowHdrLuminanceCap { get; set; }
```

引擎名：`glow_hdr_luminance_cap`

The higher threshold of the HDR glow. Areas brighter than this threshold will be clamped for the purposes of the glow effect.

### GlowHdrScale

```csharp
public float GlowHdrScale { get; set; }
```

引擎名：`glow_hdr_scale`

Smooths the transition between values that are below and above `GlowHdrThreshold` by reducing the amount of glow generated by values that are close to `GlowHdrThreshold`. Values above `glow_hdr_threshold + glow_hdr_scale` will not have glow reduced in this way.

### GlowHdrThreshold

```csharp
public float GlowHdrThreshold { get; set; }
```

引擎名：`glow_hdr_threshold`

The lower threshold of the HDR glow. When using the Mobile rendering method (which only supports a lower dynamic range up to `2.0`), this may need to be below `1.0` for glow to be visible. A value of `0.9` works well in this case. This value also needs to be decreased below `1.0` when using glow in 2D, as 2D rendering is performed in SDR.

### GlowIntensity

```csharp
public float GlowIntensity { get; set; }
```

引擎名：`glow_intensity`

The overall brightness multiplier that is applied to the glow effect just before it is blended with the scene. When using the Mobile rendering method (which only supports a lower dynamic range up to `2.0`), this should be increased to `1.5` to compensate.

### GlowMap

```csharp
public Texture GlowMap { get; set; }
```

引擎名：`glow_map`

The texture that should be used as a glow map to *multiply* the resulting glow color according to `GlowMapStrength`. This can be used to create a "lens dirt" effect. The texture's RGB color channels are used for modulation, but the alpha channel is ignored.

**Note:** The texture will be stretched to fit the screen. Therefore, it's recommended to use a texture with an aspect ratio that matches your project's base aspect ratio (typically 16:9).

**Note:** `GlowMap` has no effect when using the Compatibility rendering method, due to this rendering method using a simpler glow implementation optimized for low-end devices.

### GlowMapStrength

```csharp
public float GlowMapStrength { get; set; }
```

引擎名：`glow_map_strength`

How strong of an influence the `GlowMap` should have on the overall glow effect. A strength of `0.0` means the glow map has no influence, while a strength of `1.0` means the glow map has full influence.

**Note:** If the glow map has black areas, a value of `1.0` can also turn off the glow effect entirely in specific areas of the screen.

**Note:** `GlowMapStrength` has no effect when using the Compatibility rendering method, due to this rendering method using a simpler glow implementation optimized for low-end devices.

### GlowMix

```csharp
public float GlowMix { get; set; }
```

引擎名：`glow_mix`

When using the [`Environment.GlowBlendModeEnum.Mix`](Environment.md) `GlowBlendMode`, this controls how much the source image is blended with the glow layer. A value of `0.0` makes the glow rendering invisible, while a value of `1.0` is equivalent to [`Environment.GlowBlendModeEnum.Replace`](Environment.md).

**Note:** `GlowMix` has no effect when using the Compatibility rendering method, due to this rendering method using a simpler glow implementation optimized for low-end devices.

### GlowNormalized

```csharp
public bool GlowNormalized { get; set; }
```

引擎名：`glow_normalized`

If `true`, glow levels will be normalized so that summed together their intensities equal `1.0`.

**Note:** `GlowNormalized` has no effect when using the Compatibility rendering method, due to this rendering method using a simpler glow implementation optimized for low-end devices.

### GlowStrength

```csharp
public float GlowStrength { get; set; }
```

引擎名：`glow_strength`

The strength that is used when blurring across the screen to generate the glow effect. This affects the distance and intensity of the blur. When using the Mobile rendering method, this should be increased to compensate for the lower dynamic range.

**Note:** `GlowStrength` has no effect when using the Compatibility rendering method, due to this rendering method using a simpler glow implementation optimized for low-end devices.

### ReflectedLightSource

```csharp
public Environment.ReflectionSource ReflectedLightSource { get; set; }
```

引擎名：`reflected_light_source`

The reflected (specular) light source.

### SdfgiBounceFeedback

```csharp
public float SdfgiBounceFeedback { get; set; }
```

引擎名：`sdfgi_bounce_feedback`

The energy multiplier applied to light every time it bounces from a surface when using SDFGI. Values greater than `0.0` will simulate multiple bounces, resulting in a more realistic appearance. Increasing `SdfgiBounceFeedback` generally has no performance impact. See also `SdfgiEnergy`.

**Note:** Values greater than `0.5` can cause infinite feedback loops and should be avoided in scenes with bright materials.

**Note:** If `SdfgiBounceFeedback` is `0.0`, indirect lighting will not be represented in reflections as light will only bounce one time.

### SdfgiCascade0Distance

```csharp
public float SdfgiCascade0Distance { get; set; }
```

引擎名：`sdfgi_cascade0_distance`

**Note:** This property is linked to `SdfgiMinCellSize` and `SdfgiMaxDistance`. Changing its value will automatically change those properties as well.

### SdfgiCascades

```csharp
public int SdfgiCascades { get; set; }
```

引擎名：`sdfgi_cascades`

The number of cascades to use for SDFGI (between 1 and 8). A higher number of cascades allows displaying SDFGI further away while preserving detail up close, at the cost of performance. When using SDFGI on small-scale levels, `SdfgiCascades` can often be decreased between `1` and `4` to improve performance.

### SdfgiEnabled

```csharp
public bool SdfgiEnabled { get; set; }
```

引擎名：`sdfgi_enabled`

If `true`, enables signed distance field global illumination for meshes that have their [`GeometryInstance3D.GIMode`](GeometryInstance3D.md) set to [`GeometryInstance3D.GIModeEnum.Static`](GeometryInstance3D.md). SDFGI is a real-time global illumination technique that works well with procedurally generated and user-built levels, including in situations where geometry is created during gameplay. The signed distance field is automatically generated around the camera as it moves. Dynamic lights are supported, but dynamic occluders and emissive surfaces are not.

**Note:** SDFGI is only supported in the Forward+ rendering method, not Mobile or Compatibility.

**Performance:** SDFGI is relatively demanding on the GPU and is not suited to low-end hardware such as integrated graphics (consider [`LightmapGI`](LightmapGI.md) instead). To improve SDFGI performance, enable `ProjectSettings.rendering/global_illumination/gi/use_half_resolution` in the Project Settings.

**Note:** Meshes should have sufficiently thick walls to avoid light leaks (avoid one-sided walls). For interior levels, enclose your level geometry in a sufficiently large box and bridge the loops to close the mesh.

### SdfgiEnergy

```csharp
public float SdfgiEnergy { get; set; }
```

引擎名：`sdfgi_energy`

The energy multiplier to use for SDFGI. Higher values will result in brighter indirect lighting and reflections. See also `SdfgiBounceFeedback`.

### SdfgiMaxDistance

```csharp
public float SdfgiMaxDistance { get; set; }
```

引擎名：`sdfgi_max_distance`

The maximum distance at which SDFGI is visible. Beyond this distance, environment lighting or other sources of GI such as [`ReflectionProbe`](ReflectionProbe.md) will be used as a fallback.

**Note:** This property is linked to `SdfgiMinCellSize` and `SdfgiCascade0Distance`. Changing its value will automatically change those properties as well.

### SdfgiMinCellSize

```csharp
public float SdfgiMinCellSize { get; set; }
```

引擎名：`sdfgi_min_cell_size`

The cell size to use for the closest SDFGI cascade (in 3D units). Lower values allow SDFGI to be more precise up close, at the cost of making SDFGI updates more demanding. This can cause stuttering when the camera moves fast. Higher values allow SDFGI to cover more ground, while also reducing the performance impact of SDFGI updates.

**Note:** This property is linked to `SdfgiMaxDistance` and `SdfgiCascade0Distance`. Changing its value will automatically change those properties as well.

### SdfgiNormalBias

```csharp
public float SdfgiNormalBias { get; set; }
```

引擎名：`sdfgi_normal_bias`

The normal bias to use for SDFGI probes. Increasing this value can reduce visible streaking artifacts on sloped surfaces, at the cost of increased light leaking.

### SdfgiProbeBias

```csharp
public float SdfgiProbeBias { get; set; }
```

引擎名：`sdfgi_probe_bias`

The constant bias to use for SDFGI probes. Increasing this value can reduce visible streaking artifacts on sloped surfaces, at the cost of increased light leaking.

### SdfgiReadSkyLight

```csharp
public bool SdfgiReadSkyLight { get; set; }
```

引擎名：`sdfgi_read_sky_light`

If `true`, SDFGI takes the environment lighting into account. This should be set to `false` for interior scenes.

### SdfgiUseOcclusion

```csharp
public bool SdfgiUseOcclusion { get; set; }
```

引擎名：`sdfgi_use_occlusion`

If `true`, SDFGI uses an occlusion detection approach to reduce light leaking. Occlusion may however introduce dark blotches in certain spots, which may be undesired in mostly outdoor scenes. `SdfgiUseOcclusion` has a performance impact and should only be enabled when needed.

### SdfgiYScale

```csharp
public Environment.SdfgiyScale SdfgiYScale { get; set; }
```

引擎名：`sdfgi_y_scale`

The Y scale to use for SDFGI cells. Lower values will result in SDFGI cells being packed together more closely on the Y axis. This is used to balance between quality and covering a lot of vertical ground. `SdfgiYScale` should be set depending on how vertical your scene is (and how fast your camera may move on the Y axis).

### Sky

```csharp
public Sky Sky { get; set; }
```

引擎名：`sky`

The [`Sky`](Sky.md) resource used for this `Environment`.

### SkyCustomFov

```csharp
public float SkyCustomFov { get; set; }
```

引擎名：`sky_custom_fov`

If set to a value greater than `0.0`, overrides the field of view to use for sky rendering. If set to `0.0`, the same FOV as the current [`Camera3D`](Camera3D.md) is used for sky rendering.

### SkyRotation

```csharp
public Vector3 SkyRotation { get; set; }
```

引擎名：`sky_rotation`

The rotation to use for sky rendering.

### SsaoAOChannelAffect

```csharp
public float SsaoAOChannelAffect { get; set; }
```

引擎名：`ssao_ao_channel_affect`

The screen-space ambient occlusion intensity on materials that have an AO texture defined. Values higher than `0` will make the SSAO effect visible in areas darkened by AO textures.

### SsaoDetail

```csharp
public float SsaoDetail { get; set; }
```

引擎名：`ssao_detail`

Sets the strength of the additional level of detail for the screen-space ambient occlusion effect. A high value makes the detail pass more prominent, but it may contribute to aliasing in your final image.

### SsaoEnabled

```csharp
public bool SsaoEnabled { get; set; }
```

引擎名：`ssao_enabled`

If `true`, the screen-space ambient occlusion effect is enabled. This darkens objects' corners and cavities to simulate ambient light not reaching the entire object as in real life. This works well for small, dynamic objects, but baked lighting or ambient occlusion textures will do a better job at displaying ambient occlusion on large static objects. Godot uses a form of SSAO called Adaptive Screen Space Ambient Occlusion which is itself a form of Horizon Based Ambient Occlusion.

**Note:** SSAO is only supported in the Forward+ and Compatibility rendering methods, not Mobile.

### SsaoHorizon

```csharp
public float SsaoHorizon { get; set; }
```

引擎名：`ssao_horizon`

The threshold for considering whether a given point on a surface is occluded or not represented as an angle from the horizon mapped into the `0.0-1.0` range. A value of `1.0` results in no occlusion.

### SsaoIntensity

```csharp
public float SsaoIntensity { get; set; }
```

引擎名：`ssao_intensity`

The primary screen-space ambient occlusion intensity. Acts as a multiplier for the screen-space ambient occlusion effect. A higher value results in darker occlusion.

### SsaoLightAffect

```csharp
public float SsaoLightAffect { get; set; }
```

引擎名：`ssao_light_affect`

The screen-space ambient occlusion intensity in direct light. In real life, ambient occlusion only applies to indirect light, which means its effects can't be seen in direct light. Values higher than `0` will make the SSAO effect visible in direct light.

### SsaoPower

```csharp
public float SsaoPower { get; set; }
```

引擎名：`ssao_power`

The distribution of occlusion. A higher value results in darker occlusion, similar to `SsaoIntensity`, but with a sharper falloff.

### SsaoRadius

```csharp
public float SsaoRadius { get; set; }
```

引擎名：`ssao_radius`

The distance at which objects can occlude each other when calculating screen-space ambient occlusion. Higher values will result in occlusion over a greater distance at the cost of performance and quality.

### SsaoSharpness

```csharp
public float SsaoSharpness { get; set; }
```

引擎名：`ssao_sharpness`

The amount that the screen-space ambient occlusion effect is allowed to blur over the edges of objects. Setting too high will result in aliasing around the edges of objects. Setting too low will make object edges appear blurry.

### SsilEnabled

```csharp
public bool SsilEnabled { get; set; }
```

引擎名：`ssil_enabled`

If `true`, the screen-space indirect lighting effect is enabled. Screen space indirect lighting is a form of indirect lighting that allows diffuse light to bounce between nearby objects. Screen-space indirect lighting works very similarly to screen-space ambient occlusion, in that it only affects a limited range. It is intended to be used along with a form of proper global illumination like SDFGI or [`VoxelGI`](VoxelGI.md). Screen-space indirect lighting is not affected by individual light's [`Light3D.LightIndirectEnergy`](Light3D.md).

**Note:** SSIL is only supported in the Forward+ rendering method, not Mobile or Compatibility.

### SsilIntensity

```csharp
public float SsilIntensity { get; set; }
```

引擎名：`ssil_intensity`

The brightness multiplier for the screen-space indirect lighting effect. A higher value will result in brighter light.

### SsilNormalRejection

```csharp
public float SsilNormalRejection { get; set; }
```

引擎名：`ssil_normal_rejection`

Amount of normal rejection used when calculating screen-space indirect lighting. Normal rejection uses the normal of a given sample point to reject samples that are facing away from the current pixel. Normal rejection is necessary to avoid light leaking when only one side of an object is illuminated. However, normal rejection can be disabled if light leaking is desirable, such as when the scene mostly contains emissive objects that emit light from faces that cannot be seen from the camera.

### SsilRadius

```csharp
public float SsilRadius { get; set; }
```

引擎名：`ssil_radius`

The distance that bounced lighting can travel when using the screen space indirect lighting effect. A larger value will result in light bouncing further in a scene, but may result in under-sampling artifacts which look like long spikes surrounding light sources.

### SsilSharpness

```csharp
public float SsilSharpness { get; set; }
```

引擎名：`ssil_sharpness`

The amount that the screen-space indirect lighting effect is allowed to blur over the edges of objects. Setting too high will result in aliasing around the edges of objects. Setting too low will make object edges appear blurry.

### SsrDepthTolerance

```csharp
public float SsrDepthTolerance { get; set; }
```

引擎名：`ssr_depth_tolerance`

The depth tolerance for screen-space reflections.

### SsrEnabled

```csharp
public bool SsrEnabled { get; set; }
```

引擎名：`ssr_enabled`

If `true`, screen-space reflections are enabled. Screen-space reflections are more accurate than reflections from [`VoxelGI`](VoxelGI.md)s or [`ReflectionProbe`](ReflectionProbe.md)s, but are slower and can't reflect surfaces occluded by others.

**Note:** SSR is only supported in the Forward+ rendering method, not Mobile or Compatibility.

**Note:** SSR is not supported on viewports that have a transparent background (where [`Viewport.TransparentBg`](Viewport.md) is `true`).

### SsrFadeIn

```csharp
public float SsrFadeIn { get; set; }
```

引擎名：`ssr_fade_in`

The fade-in distance for screen-space reflections. Affects the area from the reflected material to the screen-space reflection. Only positive values are valid (negative values will be clamped to `0.0`).

### SsrFadeOut

```csharp
public float SsrFadeOut { get; set; }
```

引擎名：`ssr_fade_out`

The fade-out distance for screen-space reflections. Affects the area from the screen-space reflection to the "global" reflection. Only positive values are valid (negative values will be clamped to `0.0`).

### SsrMaxSteps

```csharp
public int SsrMaxSteps { get; set; }
```

引擎名：`ssr_max_steps`

The maximum number of steps for screen-space reflections. Higher values are slower.

### TonemapAgxContrast

```csharp
public float TonemapAgxContrast { get; set; }
```

引擎名：`tonemap_agx_contrast`

Increasing `TonemapAgxContrast` will make dark values darker and bright values brighter. Produces a higher quality result than `AdjustmentContrast` without any additional performance cost, but is only available when using the [`Environment.ToneMapper.Agx`](Environment.md) tonemapper.

### TonemapAgxWhite

```csharp
public float TonemapAgxWhite { get; set; }
```

引擎名：`tonemap_agx_white`

The white reference value for tonemapping, which indicates where bright white is located in the scale of values provided to the tonemapper. For photorealistic lighting, it is recommended to set `TonemapAgxWhite` to at least `6.0`. Higher values result in less blown out highlights, but may make the scene appear lower contrast. `TonemapAgxWhite` is the same as `TonemapWhite`, but is only effective with the [`Environment.ToneMapper.Agx`](Environment.md) tonemapper. See also `TonemapExposure`.

**Note:** When using the Mobile renderer with [`Viewport.UseHdr2D`](Viewport.md) disabled, `TonemapAgxWhite` is ignored and a white value of `2.0` will always be used instead. Otherwise, `TonemapAgxWhite` will be dynamically adjusted at runtime by multiplying it by the parent window's [`Window.GetOutputMaxLinearValue`](Window.md) when using [`Viewport.UseHdr2D`](Viewport.md) to ensure good behavior with both SDR and HDR output.

### TonemapExposure

```csharp
public float TonemapExposure { get; set; }
```

引擎名：`tonemap_exposure`

Adjusts the brightness of values before they are provided to the tonemapper. Higher `TonemapExposure` values result in a brighter image. See also `TonemapWhite`.

**Note:** Values provided to the tonemapper will also be multiplied by `2.0` and `1.8` for [`Environment.ToneMapper.Filmic`](Environment.md) and [`Environment.ToneMapper.Aces`](Environment.md) respectively to produce a similar apparent brightness as [`Environment.ToneMapper.Linear`](Environment.md).

### TonemapMode

```csharp
public Environment.ToneMapper TonemapMode { get; set; }
```

引擎名：`tonemap_mode`

The tonemapping mode to use. Tonemapping is the process that "converts" HDR values to be suitable for rendering on an LDR display. (Godot doesn't support rendering on HDR displays yet.)

### TonemapWhite

```csharp
public float TonemapWhite { get; set; }
```

引擎名：`tonemap_white`

The white reference value for tonemapping, which indicates where bright white is located in the scale of values provided to the tonemapper. For photorealistic lighting, it is recommended to set `TonemapWhite` to at least `6.0`. Higher values result in less blown out highlights, but may make the scene appear lower contrast. `TonemapAgxWhite` will be used instead when using the [`Environment.ToneMapper.Agx`](Environment.md) tonemapper. See also `TonemapExposure`.

**Note:** `TonemapWhite` must be set to `2.0` or lower on the Mobile renderer to produce bright images.

**Note:** `TonemapWhite` is ignored when using [`Environment.ToneMapper.Linear`](Environment.md) and will be dynamically adjusted at runtime to never be less than the parent window's [`Window.GetOutputMaxLinearValue`](Window.md) when using [`Environment.ToneMapper.Reinhardt`](Environment.md) with [`Viewport.UseHdr2D`](Viewport.md).

### VolumetricFogAlbedo

```csharp
public Color VolumetricFogAlbedo { get; set; }
```

引擎名：`volumetric_fog_albedo`

The [`Color`](Color.md) of the volumetric fog when interacting with lights. Mist and fog have an albedo close to `Color(1, 1, 1, 1)` while smoke has a darker albedo.

### VolumetricFogAmbientInject

```csharp
public float VolumetricFogAmbientInject { get; set; }
```

引擎名：`volumetric_fog_ambient_inject`

Scales the strength of ambient light used in the volumetric fog. A value of `0.0` means that ambient light will not impact the volumetric fog. `VolumetricFogAmbientInject` has a small performance cost when set above `0.0`.

**Note:** This has no visible effect if `VolumetricFogDensity` is `0.0` or if `VolumetricFogAlbedo` is a fully black color.

### VolumetricFogAnisotropy

```csharp
public float VolumetricFogAnisotropy { get; set; }
```

引擎名：`volumetric_fog_anisotropy`

The direction of scattered light as it goes through the volumetric fog. A value close to `1.0` means almost all light is scattered forward. A value close to `0.0` means light is scattered equally in all directions. A value close to `-1.0` means light is scattered mostly backward. Fog and mist scatter light slightly forward, while smoke scatters light equally in all directions.

### VolumetricFogDensity

```csharp
public float VolumetricFogDensity { get; set; }
```

引擎名：`volumetric_fog_density`

The base *exponential* density of the volumetric fog. Set this to the lowest density you want to have globally. [`FogVolume`](FogVolume.md)s can be used to add to or subtract from this density in specific areas. Fog rendering is exponential as in real life.

A value of `0.0` disables global volumetric fog while allowing [`FogVolume`](FogVolume.md)s to display volumetric fog in specific areas.

To make volumetric fog work as a volumetric *lighting* solution, set `VolumetricFogDensity` to the lowest non-zero value (`0.0001`) then increase lights' [`Light3D.LightVolumetricFogEnergy`](Light3D.md) to values between `10000` and `100000` to compensate for the very low density.

### VolumetricFogDetailSpread

```csharp
public float VolumetricFogDetailSpread { get; set; }
```

引擎名：`volumetric_fog_detail_spread`

The distribution of size down the length of the froxel buffer. A higher value compresses the froxels closer to the camera and places more detail closer to the camera.

### VolumetricFogEmission

```csharp
public Color VolumetricFogEmission { get; set; }
```

引擎名：`volumetric_fog_emission`

The emitted light from the volumetric fog. Even with emission, volumetric fog will not cast light onto other surfaces. Emission is useful to establish an ambient color. As the volumetric fog effect uses single-scattering only, fog tends to need a little bit of emission to soften the harsh shadows.

### VolumetricFogEmissionEnergy

```csharp
public float VolumetricFogEmissionEnergy { get; set; }
```

引擎名：`volumetric_fog_emission_energy`

The brightness of the emitted light from the volumetric fog.

### VolumetricFogEnabled

```csharp
public bool VolumetricFogEnabled { get; set; }
```

引擎名：`volumetric_fog_enabled`

Enables the volumetric fog effect. Volumetric fog uses a screen-aligned froxel buffer to calculate accurate volumetric scattering in the short to medium range. Volumetric fog interacts with [`FogVolume`](FogVolume.md)s and lights to calculate localized and global fog. Volumetric fog uses a PBR single-scattering model based on extinction, scattering, and emission which it exposes to users as density, albedo, and emission.

**Note:** Volumetric fog is only supported in the Forward+ rendering method, not Mobile or Compatibility.

### VolumetricFogGIInject

```csharp
public float VolumetricFogGIInject { get; set; }
```

引擎名：`volumetric_fog_gi_inject`

Scales the strength of Global Illumination used in the volumetric fog's albedo color. A value of `0.0` means that Global Illumination will not impact the volumetric fog. `VolumetricFogGIInject` has a small performance cost when set above `0.0`.

**Note:** This has no visible effect if `VolumetricFogDensity` is `0.0` or if `VolumetricFogAlbedo` is a fully black color.

**Note:** Only [`VoxelGI`](VoxelGI.md) and SDFGI (`SdfgiEnabled`) are taken into account when using `VolumetricFogGIInject`. Global illumination from [`LightmapGI`](LightmapGI.md), [`ReflectionProbe`](ReflectionProbe.md) and SSIL (see `SsilEnabled`) will be ignored by volumetric fog.

### VolumetricFogLength

```csharp
public float VolumetricFogLength { get; set; }
```

引擎名：`volumetric_fog_length`

The distance over which the volumetric fog is computed. Increase to compute fog over a greater range, decrease to add more detail when a long range is not needed. For best quality fog, keep this as low as possible. See also `ProjectSettings.rendering/environment/volumetric_fog/volume_depth`.

### VolumetricFogSkyAffect

```csharp
public float VolumetricFogSkyAffect { get; set; }
```

引擎名：`volumetric_fog_sky_affect`

The factor to use when affecting the sky with volumetric fog. `1.0` means that volumetric fog can fully obscure the sky. Lower values reduce the impact of volumetric fog on sky rendering, with `0.0` not affecting sky rendering at all.

**Note:** `VolumetricFogSkyAffect` also affects [`FogVolume`](FogVolume.md)s, even if `VolumetricFogDensity` is `0.0`. If you notice [`FogVolume`](FogVolume.md)s are disappearing when looking towards the sky, set `VolumetricFogSkyAffect` to `1.0`.

### VolumetricFogTemporalReprojectionAmount

```csharp
public float VolumetricFogTemporalReprojectionAmount { get; set; }
```

引擎名：`volumetric_fog_temporal_reprojection_amount`

The amount by which to blend the last frame with the current frame. A higher number results in smoother volumetric fog, but makes "ghosting" much worse. A lower value reduces ghosting but can result in the per-frame temporal jitter becoming visible.

### VolumetricFogTemporalReprojectionEnabled

```csharp
public bool VolumetricFogTemporalReprojectionEnabled { get; set; }
```

引擎名：`volumetric_fog_temporal_reprojection_enabled`

Enables temporal reprojection in the volumetric fog. Temporal reprojection blends the current frame's volumetric fog with the last frame's volumetric fog to smooth out jagged edges. The performance cost is minimal; however, it leads to moving [`FogVolume`](FogVolume.md)s and [`Light3D`](Light3D.md)s "ghosting" and leaving a trail behind them. When temporal reprojection is enabled, try to avoid moving [`FogVolume`](FogVolume.md)s or [`Light3D`](Light3D.md)s too fast. Short-lived dynamic lighting effects should have [`Light3D.LightVolumetricFogEnergy`](Light3D.md) set to `0.0` to avoid ghosting.

## 方法

### GetAdjustmentBrightness

```csharp
public float GetAdjustmentBrightness()
```

引擎名：`get_adjustment_brightness`

### GetAdjustmentColorCorrection

```csharp
public Texture GetAdjustmentColorCorrection()
```

引擎名：`get_adjustment_color_correction`

### GetAdjustmentContrast

```csharp
public float GetAdjustmentContrast()
```

引擎名：`get_adjustment_contrast`

### GetAdjustmentSaturation

```csharp
public float GetAdjustmentSaturation()
```

引擎名：`get_adjustment_saturation`

### GetAmbientLightColor

```csharp
public Color GetAmbientLightColor()
```

引擎名：`get_ambient_light_color`

### GetAmbientLightEnergy

```csharp
public float GetAmbientLightEnergy()
```

引擎名：`get_ambient_light_energy`

### GetAmbientLightSkyContribution

```csharp
public float GetAmbientLightSkyContribution()
```

引擎名：`get_ambient_light_sky_contribution`

### GetAmbientSource

```csharp
public Environment.AmbientSource GetAmbientSource()
```

引擎名：`get_ambient_source`

### GetBackground

```csharp
public Environment.BGMode GetBackground()
```

引擎名：`get_background`

### GetBgColor

```csharp
public Color GetBgColor()
```

引擎名：`get_bg_color`

### GetBgEnergyMultiplier

```csharp
public float GetBgEnergyMultiplier()
```

引擎名：`get_bg_energy_multiplier`

### GetBgIntensity

```csharp
public float GetBgIntensity()
```

引擎名：`get_bg_intensity`

### GetCameraFeedId

```csharp
public int GetCameraFeedId()
```

引擎名：`get_camera_feed_id`

### GetCanvasMaxLayer

```csharp
public int GetCanvasMaxLayer()
```

引擎名：`get_canvas_max_layer`

### GetFogAerialPerspective

```csharp
public float GetFogAerialPerspective()
```

引擎名：`get_fog_aerial_perspective`

### GetFogDensity

```csharp
public float GetFogDensity()
```

引擎名：`get_fog_density`

### GetFogDepthBegin

```csharp
public float GetFogDepthBegin()
```

引擎名：`get_fog_depth_begin`

### GetFogDepthCurve

```csharp
public float GetFogDepthCurve()
```

引擎名：`get_fog_depth_curve`

### GetFogDepthEnd

```csharp
public float GetFogDepthEnd()
```

引擎名：`get_fog_depth_end`

### GetFogHeight

```csharp
public float GetFogHeight()
```

引擎名：`get_fog_height`

### GetFogHeightDensity

```csharp
public float GetFogHeightDensity()
```

引擎名：`get_fog_height_density`

### GetFogLightColor

```csharp
public Color GetFogLightColor()
```

引擎名：`get_fog_light_color`

### GetFogLightEnergy

```csharp
public float GetFogLightEnergy()
```

引擎名：`get_fog_light_energy`

### GetFogMode

```csharp
public Environment.FogModeEnum GetFogMode()
```

引擎名：`get_fog_mode`

### GetFogSkyAffect

```csharp
public float GetFogSkyAffect()
```

引擎名：`get_fog_sky_affect`

### GetFogSunScatter

```csharp
public float GetFogSunScatter()
```

引擎名：`get_fog_sun_scatter`

### GetGlowBlendMode

```csharp
public Environment.GlowBlendModeEnum GetGlowBlendMode()
```

引擎名：`get_glow_blend_mode`

### GetGlowBloom

```csharp
public float GetGlowBloom()
```

引擎名：`get_glow_bloom`

### GetGlowHdrBleedScale

```csharp
public float GetGlowHdrBleedScale()
```

引擎名：`get_glow_hdr_bleed_scale`

### GetGlowHdrBleedThreshold

```csharp
public float GetGlowHdrBleedThreshold()
```

引擎名：`get_glow_hdr_bleed_threshold`

### GetGlowHdrLuminanceCap

```csharp
public float GetGlowHdrLuminanceCap()
```

引擎名：`get_glow_hdr_luminance_cap`

### GetGlowIntensity

```csharp
public float GetGlowIntensity()
```

引擎名：`get_glow_intensity`

### GetGlowLevel

```csharp
public float GetGlowLevel(int idx)
```

引擎名：`get_glow_level`

Returns the intensity of the glow level `idx`.

### GetGlowMap

```csharp
public Texture GetGlowMap()
```

引擎名：`get_glow_map`

### GetGlowMapStrength

```csharp
public float GetGlowMapStrength()
```

引擎名：`get_glow_map_strength`

### GetGlowMix

```csharp
public float GetGlowMix()
```

引擎名：`get_glow_mix`

### GetGlowStrength

```csharp
public float GetGlowStrength()
```

引擎名：`get_glow_strength`

### GetReflectionSource

```csharp
public Environment.ReflectionSource GetReflectionSource()
```

引擎名：`get_reflection_source`

### GetSdfgiBounceFeedback

```csharp
public float GetSdfgiBounceFeedback()
```

引擎名：`get_sdfgi_bounce_feedback`

### GetSdfgiCascade0Distance

```csharp
public float GetSdfgiCascade0Distance()
```

引擎名：`get_sdfgi_cascade0_distance`

### GetSdfgiCascades

```csharp
public int GetSdfgiCascades()
```

引擎名：`get_sdfgi_cascades`

### GetSdfgiEnergy

```csharp
public float GetSdfgiEnergy()
```

引擎名：`get_sdfgi_energy`

### GetSdfgiMaxDistance

```csharp
public float GetSdfgiMaxDistance()
```

引擎名：`get_sdfgi_max_distance`

### GetSdfgiMinCellSize

```csharp
public float GetSdfgiMinCellSize()
```

引擎名：`get_sdfgi_min_cell_size`

### GetSdfgiNormalBias

```csharp
public float GetSdfgiNormalBias()
```

引擎名：`get_sdfgi_normal_bias`

### GetSdfgiProbeBias

```csharp
public float GetSdfgiProbeBias()
```

引擎名：`get_sdfgi_probe_bias`

### GetSdfgiYScale

```csharp
public Environment.SdfgiyScale GetSdfgiYScale()
```

引擎名：`get_sdfgi_y_scale`

### GetSky

```csharp
public Sky GetSky()
```

引擎名：`get_sky`

### GetSkyCustomFov

```csharp
public float GetSkyCustomFov()
```

引擎名：`get_sky_custom_fov`

### GetSkyRotation

```csharp
public Vector3 GetSkyRotation()
```

引擎名：`get_sky_rotation`

### GetSsaoAOChannelAffect

```csharp
public float GetSsaoAOChannelAffect()
```

引擎名：`get_ssao_ao_channel_affect`

### GetSsaoDetail

```csharp
public float GetSsaoDetail()
```

引擎名：`get_ssao_detail`

### GetSsaoDirectLightAffect

```csharp
public float GetSsaoDirectLightAffect()
```

引擎名：`get_ssao_direct_light_affect`

### GetSsaoHorizon

```csharp
public float GetSsaoHorizon()
```

引擎名：`get_ssao_horizon`

### GetSsaoIntensity

```csharp
public float GetSsaoIntensity()
```

引擎名：`get_ssao_intensity`

### GetSsaoPower

```csharp
public float GetSsaoPower()
```

引擎名：`get_ssao_power`

### GetSsaoRadius

```csharp
public float GetSsaoRadius()
```

引擎名：`get_ssao_radius`

### GetSsaoSharpness

```csharp
public float GetSsaoSharpness()
```

引擎名：`get_ssao_sharpness`

### GetSsilIntensity

```csharp
public float GetSsilIntensity()
```

引擎名：`get_ssil_intensity`

### GetSsilNormalRejection

```csharp
public float GetSsilNormalRejection()
```

引擎名：`get_ssil_normal_rejection`

### GetSsilRadius

```csharp
public float GetSsilRadius()
```

引擎名：`get_ssil_radius`

### GetSsilSharpness

```csharp
public float GetSsilSharpness()
```

引擎名：`get_ssil_sharpness`

### GetSsrDepthTolerance

```csharp
public float GetSsrDepthTolerance()
```

引擎名：`get_ssr_depth_tolerance`

### GetSsrFadeIn

```csharp
public float GetSsrFadeIn()
```

引擎名：`get_ssr_fade_in`

### GetSsrFadeOut

```csharp
public float GetSsrFadeOut()
```

引擎名：`get_ssr_fade_out`

### GetSsrMaxSteps

```csharp
public int GetSsrMaxSteps()
```

引擎名：`get_ssr_max_steps`

### GetTonemapAgxContrast

```csharp
public float GetTonemapAgxContrast()
```

引擎名：`get_tonemap_agx_contrast`

### GetTonemapAgxWhite

```csharp
public float GetTonemapAgxWhite()
```

引擎名：`get_tonemap_agx_white`

### GetTonemapExposure

```csharp
public float GetTonemapExposure()
```

引擎名：`get_tonemap_exposure`

### GetTonemapWhite

```csharp
public float GetTonemapWhite()
```

引擎名：`get_tonemap_white`

### GetTonemapper

```csharp
public Environment.ToneMapper GetTonemapper()
```

引擎名：`get_tonemapper`

### GetVolumetricFogAlbedo

```csharp
public Color GetVolumetricFogAlbedo()
```

引擎名：`get_volumetric_fog_albedo`

### GetVolumetricFogAmbientInject

```csharp
public float GetVolumetricFogAmbientInject()
```

引擎名：`get_volumetric_fog_ambient_inject`

### GetVolumetricFogAnisotropy

```csharp
public float GetVolumetricFogAnisotropy()
```

引擎名：`get_volumetric_fog_anisotropy`

### GetVolumetricFogDensity

```csharp
public float GetVolumetricFogDensity()
```

引擎名：`get_volumetric_fog_density`

### GetVolumetricFogDetailSpread

```csharp
public float GetVolumetricFogDetailSpread()
```

引擎名：`get_volumetric_fog_detail_spread`

### GetVolumetricFogEmission

```csharp
public Color GetVolumetricFogEmission()
```

引擎名：`get_volumetric_fog_emission`

### GetVolumetricFogEmissionEnergy

```csharp
public float GetVolumetricFogEmissionEnergy()
```

引擎名：`get_volumetric_fog_emission_energy`

### GetVolumetricFogGIInject

```csharp
public float GetVolumetricFogGIInject()
```

引擎名：`get_volumetric_fog_gi_inject`

### GetVolumetricFogLength

```csharp
public float GetVolumetricFogLength()
```

引擎名：`get_volumetric_fog_length`

### GetVolumetricFogSkyAffect

```csharp
public float GetVolumetricFogSkyAffect()
```

引擎名：`get_volumetric_fog_sky_affect`

### GetVolumetricFogTemporalReprojectionAmount

```csharp
public float GetVolumetricFogTemporalReprojectionAmount()
```

引擎名：`get_volumetric_fog_temporal_reprojection_amount`

### IsAdjustmentEnabled

```csharp
public bool IsAdjustmentEnabled()
```

引擎名：`is_adjustment_enabled`

### IsFogEnabled

```csharp
public bool IsFogEnabled()
```

引擎名：`is_fog_enabled`

### IsGlowEnabled

```csharp
public bool IsGlowEnabled()
```

引擎名：`is_glow_enabled`

### IsGlowNormalized

```csharp
public bool IsGlowNormalized()
```

引擎名：`is_glow_normalized`

### IsSdfgiEnabled

```csharp
public bool IsSdfgiEnabled()
```

引擎名：`is_sdfgi_enabled`

### IsSdfgiReadingSkyLight

```csharp
public bool IsSdfgiReadingSkyLight()
```

引擎名：`is_sdfgi_reading_sky_light`

### IsSdfgiUsingOcclusion

```csharp
public bool IsSdfgiUsingOcclusion()
```

引擎名：`is_sdfgi_using_occlusion`

### IsSsaoEnabled

```csharp
public bool IsSsaoEnabled()
```

引擎名：`is_ssao_enabled`

### IsSsilEnabled

```csharp
public bool IsSsilEnabled()
```

引擎名：`is_ssil_enabled`

### IsSsrEnabled

```csharp
public bool IsSsrEnabled()
```

引擎名：`is_ssr_enabled`

### IsVolumetricFogEnabled

```csharp
public bool IsVolumetricFogEnabled()
```

引擎名：`is_volumetric_fog_enabled`

### IsVolumetricFogTemporalReprojectionEnabled

```csharp
public bool IsVolumetricFogTemporalReprojectionEnabled()
```

引擎名：`is_volumetric_fog_temporal_reprojection_enabled`

### SetAdjustmentBrightness

```csharp
public void SetAdjustmentBrightness(float brightness)
```

引擎名：`set_adjustment_brightness`

### SetAdjustmentColorCorrection

```csharp
public void SetAdjustmentColorCorrection(Texture colorCorrection)
```

引擎名：`set_adjustment_color_correction`

### SetAdjustmentContrast

```csharp
public void SetAdjustmentContrast(float contrast)
```

引擎名：`set_adjustment_contrast`

### SetAdjustmentEnabled

```csharp
public void SetAdjustmentEnabled(bool enabled)
```

引擎名：`set_adjustment_enabled`

### SetAdjustmentSaturation

```csharp
public void SetAdjustmentSaturation(float saturation)
```

引擎名：`set_adjustment_saturation`

### SetAmbientLightColor

```csharp
public void SetAmbientLightColor(Color color)
```

引擎名：`set_ambient_light_color`

### SetAmbientLightEnergy

```csharp
public void SetAmbientLightEnergy(float energy)
```

引擎名：`set_ambient_light_energy`

### SetAmbientLightSkyContribution

```csharp
public void SetAmbientLightSkyContribution(float ratio)
```

引擎名：`set_ambient_light_sky_contribution`

### SetAmbientSource

```csharp
public void SetAmbientSource(Environment.AmbientSource source)
```

引擎名：`set_ambient_source`

### SetBackground

```csharp
public void SetBackground(Environment.BGMode mode)
```

引擎名：`set_background`

### SetBgColor

```csharp
public void SetBgColor(Color color)
```

引擎名：`set_bg_color`

### SetBgEnergyMultiplier

```csharp
public void SetBgEnergyMultiplier(float energy)
```

引擎名：`set_bg_energy_multiplier`

### SetBgIntensity

```csharp
public void SetBgIntensity(float energy)
```

引擎名：`set_bg_intensity`

### SetCameraFeedId

```csharp
public void SetCameraFeedId(int id)
```

引擎名：`set_camera_feed_id`

### SetCanvasMaxLayer

```csharp
public void SetCanvasMaxLayer(int layer)
```

引擎名：`set_canvas_max_layer`

### SetFogAerialPerspective

```csharp
public void SetFogAerialPerspective(float aerialPerspective)
```

引擎名：`set_fog_aerial_perspective`

### SetFogDensity

```csharp
public void SetFogDensity(float density)
```

引擎名：`set_fog_density`

### SetFogDepthBegin

```csharp
public void SetFogDepthBegin(float begin)
```

引擎名：`set_fog_depth_begin`

### SetFogDepthCurve

```csharp
public void SetFogDepthCurve(float curve)
```

引擎名：`set_fog_depth_curve`

### SetFogDepthEnd

```csharp
public void SetFogDepthEnd(float end)
```

引擎名：`set_fog_depth_end`

### SetFogEnabled

```csharp
public void SetFogEnabled(bool enabled)
```

引擎名：`set_fog_enabled`

### SetFogHeight

```csharp
public void SetFogHeight(float height)
```

引擎名：`set_fog_height`

### SetFogHeightDensity

```csharp
public void SetFogHeightDensity(float heightDensity)
```

引擎名：`set_fog_height_density`

### SetFogLightColor

```csharp
public void SetFogLightColor(Color lightColor)
```

引擎名：`set_fog_light_color`

### SetFogLightEnergy

```csharp
public void SetFogLightEnergy(float lightEnergy)
```

引擎名：`set_fog_light_energy`

### SetFogMode

```csharp
public void SetFogMode(Environment.FogModeEnum mode)
```

引擎名：`set_fog_mode`

### SetFogSkyAffect

```csharp
public void SetFogSkyAffect(float skyAffect)
```

引擎名：`set_fog_sky_affect`

### SetFogSunScatter

```csharp
public void SetFogSunScatter(float sunScatter)
```

引擎名：`set_fog_sun_scatter`

### SetGlowBlendMode

```csharp
public void SetGlowBlendMode(Environment.GlowBlendModeEnum mode)
```

引擎名：`set_glow_blend_mode`

### SetGlowBloom

```csharp
public void SetGlowBloom(float amount)
```

引擎名：`set_glow_bloom`

### SetGlowEnabled

```csharp
public void SetGlowEnabled(bool enabled)
```

引擎名：`set_glow_enabled`

### SetGlowHdrBleedScale

```csharp
public void SetGlowHdrBleedScale(float scale)
```

引擎名：`set_glow_hdr_bleed_scale`

### SetGlowHdrBleedThreshold

```csharp
public void SetGlowHdrBleedThreshold(float threshold)
```

引擎名：`set_glow_hdr_bleed_threshold`

### SetGlowHdrLuminanceCap

```csharp
public void SetGlowHdrLuminanceCap(float amount)
```

引擎名：`set_glow_hdr_luminance_cap`

### SetGlowIntensity

```csharp
public void SetGlowIntensity(float intensity)
```

引擎名：`set_glow_intensity`

### SetGlowLevel

```csharp
public void SetGlowLevel(int idx, float intensity)
```

引擎名：`set_glow_level`

Sets the intensity of the glow level `idx`. A value above `0.0` enables the level. Each level relies on the previous level. This means that enabling higher glow levels will slow down the glow effect rendering, even if previous levels aren't enabled.

### SetGlowMap

```csharp
public void SetGlowMap(Texture mode)
```

引擎名：`set_glow_map`

### SetGlowMapStrength

```csharp
public void SetGlowMapStrength(float strength)
```

引擎名：`set_glow_map_strength`

### SetGlowMix

```csharp
public void SetGlowMix(float mix)
```

引擎名：`set_glow_mix`

### SetGlowNormalized

```csharp
public void SetGlowNormalized(bool normalize)
```

引擎名：`set_glow_normalized`

### SetGlowStrength

```csharp
public void SetGlowStrength(float strength)
```

引擎名：`set_glow_strength`

### SetReflectionSource

```csharp
public void SetReflectionSource(Environment.ReflectionSource source)
```

引擎名：`set_reflection_source`

### SetSdfgiBounceFeedback

```csharp
public void SetSdfgiBounceFeedback(float amount)
```

引擎名：`set_sdfgi_bounce_feedback`

### SetSdfgiCascade0Distance

```csharp
public void SetSdfgiCascade0Distance(float distance)
```

引擎名：`set_sdfgi_cascade0_distance`

### SetSdfgiCascades

```csharp
public void SetSdfgiCascades(int amount)
```

引擎名：`set_sdfgi_cascades`

### SetSdfgiEnabled

```csharp
public void SetSdfgiEnabled(bool enabled)
```

引擎名：`set_sdfgi_enabled`

### SetSdfgiEnergy

```csharp
public void SetSdfgiEnergy(float amount)
```

引擎名：`set_sdfgi_energy`

### SetSdfgiMaxDistance

```csharp
public void SetSdfgiMaxDistance(float distance)
```

引擎名：`set_sdfgi_max_distance`

### SetSdfgiMinCellSize

```csharp
public void SetSdfgiMinCellSize(float size)
```

引擎名：`set_sdfgi_min_cell_size`

### SetSdfgiNormalBias

```csharp
public void SetSdfgiNormalBias(float bias)
```

引擎名：`set_sdfgi_normal_bias`

### SetSdfgiProbeBias

```csharp
public void SetSdfgiProbeBias(float bias)
```

引擎名：`set_sdfgi_probe_bias`

### SetSdfgiReadSkyLight

```csharp
public void SetSdfgiReadSkyLight(bool enable)
```

引擎名：`set_sdfgi_read_sky_light`

### SetSdfgiUseOcclusion

```csharp
public void SetSdfgiUseOcclusion(bool enable)
```

引擎名：`set_sdfgi_use_occlusion`

### SetSdfgiYScale

```csharp
public void SetSdfgiYScale(Environment.SdfgiyScale scale)
```

引擎名：`set_sdfgi_y_scale`

### SetSky

```csharp
public void SetSky(Sky sky)
```

引擎名：`set_sky`

### SetSkyCustomFov

```csharp
public void SetSkyCustomFov(float scale)
```

引擎名：`set_sky_custom_fov`

### SetSkyRotation

```csharp
public void SetSkyRotation(Vector3 eulerRadians)
```

引擎名：`set_sky_rotation`

### SetSsaoAOChannelAffect

```csharp
public void SetSsaoAOChannelAffect(float amount)
```

引擎名：`set_ssao_ao_channel_affect`

### SetSsaoDetail

```csharp
public void SetSsaoDetail(float detail)
```

引擎名：`set_ssao_detail`

### SetSsaoDirectLightAffect

```csharp
public void SetSsaoDirectLightAffect(float amount)
```

引擎名：`set_ssao_direct_light_affect`

### SetSsaoEnabled

```csharp
public void SetSsaoEnabled(bool enabled)
```

引擎名：`set_ssao_enabled`

### SetSsaoHorizon

```csharp
public void SetSsaoHorizon(float horizon)
```

引擎名：`set_ssao_horizon`

### SetSsaoIntensity

```csharp
public void SetSsaoIntensity(float intensity)
```

引擎名：`set_ssao_intensity`

### SetSsaoPower

```csharp
public void SetSsaoPower(float power)
```

引擎名：`set_ssao_power`

### SetSsaoRadius

```csharp
public void SetSsaoRadius(float radius)
```

引擎名：`set_ssao_radius`

### SetSsaoSharpness

```csharp
public void SetSsaoSharpness(float sharpness)
```

引擎名：`set_ssao_sharpness`

### SetSsilEnabled

```csharp
public void SetSsilEnabled(bool enabled)
```

引擎名：`set_ssil_enabled`

### SetSsilIntensity

```csharp
public void SetSsilIntensity(float intensity)
```

引擎名：`set_ssil_intensity`

### SetSsilNormalRejection

```csharp
public void SetSsilNormalRejection(float normalRejection)
```

引擎名：`set_ssil_normal_rejection`

### SetSsilRadius

```csharp
public void SetSsilRadius(float radius)
```

引擎名：`set_ssil_radius`

### SetSsilSharpness

```csharp
public void SetSsilSharpness(float sharpness)
```

引擎名：`set_ssil_sharpness`

### SetSsrDepthTolerance

```csharp
public void SetSsrDepthTolerance(float depthTolerance)
```

引擎名：`set_ssr_depth_tolerance`

### SetSsrEnabled

```csharp
public void SetSsrEnabled(bool enabled)
```

引擎名：`set_ssr_enabled`

### SetSsrFadeIn

```csharp
public void SetSsrFadeIn(float fadeIn)
```

引擎名：`set_ssr_fade_in`

### SetSsrFadeOut

```csharp
public void SetSsrFadeOut(float fadeOut)
```

引擎名：`set_ssr_fade_out`

### SetSsrMaxSteps

```csharp
public void SetSsrMaxSteps(int maxSteps)
```

引擎名：`set_ssr_max_steps`

### SetTonemapAgxContrast

```csharp
public void SetTonemapAgxContrast(float contrast)
```

引擎名：`set_tonemap_agx_contrast`

### SetTonemapAgxWhite

```csharp
public void SetTonemapAgxWhite(float white)
```

引擎名：`set_tonemap_agx_white`

### SetTonemapExposure

```csharp
public void SetTonemapExposure(float exposure)
```

引擎名：`set_tonemap_exposure`

### SetTonemapWhite

```csharp
public void SetTonemapWhite(float white)
```

引擎名：`set_tonemap_white`

### SetTonemapper

```csharp
public void SetTonemapper(Environment.ToneMapper mode)
```

引擎名：`set_tonemapper`

### SetVolumetricFogAlbedo

```csharp
public void SetVolumetricFogAlbedo(Color color)
```

引擎名：`set_volumetric_fog_albedo`

### SetVolumetricFogAmbientInject

```csharp
public void SetVolumetricFogAmbientInject(float enabled)
```

引擎名：`set_volumetric_fog_ambient_inject`

### SetVolumetricFogAnisotropy

```csharp
public void SetVolumetricFogAnisotropy(float anisotropy)
```

引擎名：`set_volumetric_fog_anisotropy`

### SetVolumetricFogDensity

```csharp
public void SetVolumetricFogDensity(float density)
```

引擎名：`set_volumetric_fog_density`

### SetVolumetricFogDetailSpread

```csharp
public void SetVolumetricFogDetailSpread(float detailSpread)
```

引擎名：`set_volumetric_fog_detail_spread`

### SetVolumetricFogEmission

```csharp
public void SetVolumetricFogEmission(Color color)
```

引擎名：`set_volumetric_fog_emission`

### SetVolumetricFogEmissionEnergy

```csharp
public void SetVolumetricFogEmissionEnergy(float begin)
```

引擎名：`set_volumetric_fog_emission_energy`

### SetVolumetricFogEnabled

```csharp
public void SetVolumetricFogEnabled(bool enabled)
```

引擎名：`set_volumetric_fog_enabled`

### SetVolumetricFogGIInject

```csharp
public void SetVolumetricFogGIInject(float gIInject)
```

引擎名：`set_volumetric_fog_gi_inject`

### SetVolumetricFogLength

```csharp
public void SetVolumetricFogLength(float length)
```

引擎名：`set_volumetric_fog_length`

### SetVolumetricFogSkyAffect

```csharp
public void SetVolumetricFogSkyAffect(float skyAffect)
```

引擎名：`set_volumetric_fog_sky_affect`

### SetVolumetricFogTemporalReprojectionAmount

```csharp
public void SetVolumetricFogTemporalReprojectionAmount(float temporalReprojectionAmount)
```

引擎名：`set_volumetric_fog_temporal_reprojection_amount`

### SetVolumetricFogTemporalReprojectionEnabled

```csharp
public void SetVolumetricFogTemporalReprojectionEnabled(bool enabled)
```

引擎名：`set_volumetric_fog_temporal_reprojection_enabled`

## AmbientSource

命名空间 `Godot`；enum

### 枚举值

- `Bg = 0`：Gather ambient light from whichever source is specified as the background.
- `Disabled = 1`：Disable ambient light.
- `Color = 2`：Specify a specific [`Color`](Color.md) for ambient light.
- `Sky = 3`：Gather ambient light from the [`Sky`](Sky.md) regardless of what the background is.

## BGMode

命名空间 `Godot`；enum

### 枚举值

- `ClearColor = 0`：Clears the background using the clear color defined in `ProjectSettings.rendering/environment/defaults/default_clear_color`.
- `Color = 1`：Clears the background using a custom clear color.
- `Sky = 2`：Displays a user-defined sky in the background.
- `Canvas = 3`：Displays a [`CanvasLayer`](CanvasLayer.md) in the background.
- `Keep = 4`：Keeps on screen every pixel drawn in the background.
- `CameraFeed = 5`：Displays a camera feed in the background.
- `Max = 6`：Represents the size of the `Environment.BGMode` enum.

## FogModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Exponential = 0`：Use a physically-based fog model defined primarily by fog density.
- `Depth = 1`：Use a simple fog model defined by start and end positions and a custom curve.

## GlowBlendModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Additive = 0`：Adds the glow effect to the scene.
- `Screen = 1`：Adds the glow effect to the scene after modifying the glow influence based on the scene value; dark values will be highly influenced by glow and bright values will not be influenced by glow.
- `Softlight = 2`：Adds the glow effect to the tonemapped image after modifying the glow influence based on the image value; dark values and bright values will not be influenced by glow and mid-range values will be highly influenced by glo…
- `Replace = 3`：Replaces all pixels' color by the glow effect.
- `Mix = 4`：Mixes the glow image with the scene image.

## ReflectionSource

命名空间 `Godot`；enum

### 枚举值

- `Bg = 0`：Use the background for reflections.
- `Disabled = 1`：Disable reflections.
- `Sky = 2`：Use the [`Sky`](Sky.md) for reflections regardless of what the background is.

## SdfgiyScale

命名空间 `Godot`；enum

### 枚举值

- `Scale50Percent = 0`：Use 50% scale for SDFGI on the Y (vertical) axis.
- `Scale75Percent = 1`：Use 75% scale for SDFGI on the Y (vertical) axis.
- `Scale100Percent = 2`：Use 100% scale for SDFGI on the Y (vertical) axis.

## ToneMapper

命名空间 `Godot`；enum

### 枚举值

- `Linear = 0`：Does not modify color data, resulting in a linear tonemapping curve which unnaturally clips bright values, causing bright lighting to look blown out.
- `Reinhardt = 1`：A simple tonemapping curve that rolls off bright values to prevent clipping.
- `Filmic = 2`：Uses a film-like tonemapping curve to prevent clipping of bright values and provide better contrast than `Reinhardt`.
- `Aces = 3`：Uses a high-contrast film-like tonemapping curve and desaturates bright values for a more realistic appearance.
- `Agx = 4`：Uses an adjustable film-like tonemapping curve and desaturates bright values for a more realistic appearance.
