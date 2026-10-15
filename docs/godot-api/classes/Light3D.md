<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Light3D

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Light3D is the *abstract* base class for light nodes. As it can't be instantiated, it shouldn't be used directly. Other types of light nodes inherit from it. Light3D contains the common variables and parameters used for lighting.

## 属性

### DistanceFadeBegin

```csharp
public float DistanceFadeBegin { get; set; }
```

引擎名：`distance_fade_begin`

The distance from the camera at which the light begins to fade away (in 3D units).

**Note:** Only effective for [`OmniLight3D`](OmniLight3D.md) and [`SpotLight3D`](SpotLight3D.md).

### DistanceFadeEnabled

```csharp
public bool DistanceFadeEnabled { get; set; }
```

引擎名：`distance_fade_enabled`

If `true`, the light will smoothly fade away when far from the active [`Camera3D`](Camera3D.md) starting at `DistanceFadeBegin`. This acts as a form of level of detail (LOD). The light will fade out over `DistanceFadeBegin` + `DistanceFadeLength`, after which it will be culled and not sent to the shader at all. Use this to reduce the number of active lights in a scene and thus improve performance.

**Note:** Only effective for [`OmniLight3D`](OmniLight3D.md) and [`SpotLight3D`](SpotLight3D.md).

### DistanceFadeLength

```csharp
public float DistanceFadeLength { get; set; }
```

引擎名：`distance_fade_length`

Distance over which the light and its shadow fades. The light's energy and shadow's opacity is progressively reduced over this distance and is completely invisible at the end.

**Note:** Only effective for [`OmniLight3D`](OmniLight3D.md) and [`SpotLight3D`](SpotLight3D.md).

### DistanceFadeShadow

```csharp
public float DistanceFadeShadow { get; set; }
```

引擎名：`distance_fade_shadow`

The distance from the camera at which the light's shadow cuts off (in 3D units). Set this to a value lower than `DistanceFadeBegin` + `DistanceFadeLength` to further improve performance, as shadow rendering is often more expensive than light rendering itself.

**Note:** Only effective for [`OmniLight3D`](OmniLight3D.md) and [`SpotLight3D`](SpotLight3D.md), and only when `ShadowEnabled` is `true`.

### EditorOnly

```csharp
public bool EditorOnly { get; set; }
```

引擎名：`editor_only`

If `true`, the light only appears in the editor and will not be visible at runtime. If `true`, the light will never be baked in [`LightmapGI`](LightmapGI.md) regardless of its `LightBakeMode`.

### LightAngularDistance

```csharp
public float LightAngularDistance { get; set; }
```

引擎名：`light_angular_distance`

The light's angular size in degrees. Increasing this will make shadows softer at greater distances (also called percentage-closer soft shadows, or PCSS). Only available for [`DirectionalLight3D`](DirectionalLight3D.md)s. For reference, the Sun from the Earth is approximately `0.5`. Increasing this value above `0.0` for lights with shadows enabled will have a noticeable performance cost due to PCSS.

**Note:** `LightAngularDistance` is not affected by [`Node3D.Scale`](Node3D.md) (the light's scale or its parent's scale).

**Note:** PCSS for directional lights is only supported in the Forward+ rendering method, not Mobile or Compatibility.

### LightBakeMode

```csharp
public Light3D.BakeMode LightBakeMode { get; set; }
```

引擎名：`light_bake_mode`

The light's bake mode. This will affect the global illumination techniques that have an effect on the light's rendering.

**Note:** Meshes' global illumination mode will also affect the global illumination rendering. See [`GeometryInstance3D.GIMode`](GeometryInstance3D.md).

### LightColor

```csharp
public Color LightColor { get; set; }
```

引擎名：`light_color`

The light's color in nonlinear sRGB encoding. An *overbright* color can be used to achieve a result equivalent to increasing the light's `LightEnergy`.

### LightCullMask

```csharp
public uint LightCullMask { get; set; }
```

引擎名：`light_cull_mask`

The light will affect objects in the selected layers.

**Note:** The light cull mask is ignored by [`VoxelGI`](VoxelGI.md), SDFGI, [`LightmapGI`](LightmapGI.md), and volumetric fog. These will always render lights in a way that ignores the cull mask. See also [`VisualInstance3D.Layers`](VisualInstance3D.md).

### LightEnergy

```csharp
public float LightEnergy { get; set; }
```

引擎名：`light_energy`

The light's strength multiplier (this is not a physical unit). For [`OmniLight3D`](OmniLight3D.md) and [`SpotLight3D`](SpotLight3D.md), changing this value will only change the light color's intensity, not the light's radius.

### LightIndirectEnergy

```csharp
public float LightIndirectEnergy { get; set; }
```

引擎名：`light_indirect_energy`

Secondary multiplier used with indirect light (light bounces). Used with [`VoxelGI`](VoxelGI.md) and SDFGI (see [`Environment.SdfgiEnabled`](Environment.md)).

**Note:** This property is ignored if `LightEnergy` is equal to `0.0`, as the light won't be present at all in the GI shader.

### LightIntensityLumens

```csharp
public float LightIntensityLumens { get; set; }
```

引擎名：`light_intensity_lumens`

Used by positional lights ([`OmniLight3D`](OmniLight3D.md) and [`SpotLight3D`](SpotLight3D.md)) when `ProjectSettings.rendering/lights_and_shadows/use_physical_light_units` is `true`. Sets the intensity of the light source measured in Lumens. Lumens are a measure of luminous flux, which is the total amount of visible light emitted by a light source per unit of time.

For [`SpotLight3D`](SpotLight3D.md)s, we assume that the area outside the visible cone is surrounded by a perfect light absorbing material. Accordingly, the apparent brightness of the cone area does not change as the cone increases and decreases in size.

A typical household lightbulb can range from around 600 lumens to 1,200 lumens, a candle is about 13 lumens, while a streetlight can be approximately 60,000 lumens.

### LightIntensityLux

```csharp
public float LightIntensityLux { get; set; }
```

引擎名：`light_intensity_lux`

Used by [`DirectionalLight3D`](DirectionalLight3D.md)s when `ProjectSettings.rendering/lights_and_shadows/use_physical_light_units` is `true`. Sets the intensity of the light source measured in Lux. Lux is a measure of luminous flux per unit area, it is equal to one lumen per square meter. Lux is the measure of how much light hits a surface at a given time.

On a clear sunny day a surface in direct sunlight may be approximately 100,000 lux, a typical room in a home may be approximately 50 lux, while the moonlit ground may be approximately 0.1 lux.

### LightNegative

```csharp
public bool LightNegative { get; set; }
```

引擎名：`light_negative`

If `true`, the light's effect is reversed, darkening areas and casting bright shadows.

### LightProjector

```csharp
public Texture2D LightProjector { get; set; }
```

引擎名：`light_projector`

[`Texture2D`](Texture2D.md) projected by light. `ShadowEnabled` must be on for the projector to work. Light projectors make the light appear as if it is shining through a colored but transparent object, almost like light shining through stained-glass.

**Note:** Unlike [`BaseMaterial3D`](BaseMaterial3D.md) whose filter mode can be adjusted on a per-material basis, the filter mode for light projector textures is set globally with `ProjectSettings.rendering/textures/light_projectors/filter`.

**Note:** Light projector textures are only supported in the Forward+ and Mobile rendering methods, not Compatibility.

### LightSize

```csharp
public float LightSize { get; set; }
```

引擎名：`light_size`

The simulated size of the light in Godot units, affecting shading and shadows. For [`OmniLight3D`](OmniLight3D.md)s and [`SpotLight3D`](SpotLight3D.md)s, increasing this value simulates a spherical area light, expanding the size of specular highlights. If shadows are enabled, a penumbra is rendered, making shadows appear blurrier. For [`AreaLight3D`](AreaLight3D.md)s, only the shadows are affected. Penumbras are simulated with percentage-closer soft shadows, or PCSS, which has a noticeable performance cost for values above `0.0`.

**Note:** `LightSize` is not affected by [`Node3D.Scale`](Node3D.md) (the light's scale or its parent's scale).

**Note:** PCSS for positional lights is only supported in the Forward+ and Mobile rendering methods, not Compatibility.

### LightSpecular

```csharp
public float LightSpecular { get; set; }
```

引擎名：`light_specular`

The intensity of the specular blob in objects affected by the light. At `0`, the light becomes a pure diffuse light. When not baking emission, this can be used to avoid unrealistic reflections when placing lights above an emissive surface.

### LightTemperature

```csharp
public float LightTemperature { get; set; }
```

引擎名：`light_temperature`

Sets the color temperature of the light source, measured in Kelvin. This is used to calculate a correlated color temperature which tints the `LightColor`.

The sun on a cloudy day is approximately 6500 Kelvin, on a clear day it is between 5500 to 6000 Kelvin, and on a clear day at sunrise or sunset it ranges to around 1850 Kelvin.

### LightVolumetricFogEnergy

```csharp
public float LightVolumetricFogEnergy { get; set; }
```

引擎名：`light_volumetric_fog_energy`

Secondary multiplier multiplied with `LightEnergy` then used with the [`Environment`](Environment.md)'s volumetric fog (if enabled). If set to `0.0`, computing volumetric fog will be skipped for this light, which can improve performance for large amounts of lights when volumetric fog is enabled.

**Note:** To prevent short-lived dynamic light effects from poorly interacting with volumetric fog, lights used in those effects should have `LightVolumetricFogEnergy` set to `0.0` unless [`Environment.VolumetricFogTemporalReprojectionEnabled`](Environment.md) is disabled (or unless the reprojection amount is significantly lowered).

### ShadowBias

```csharp
public float ShadowBias { get; set; }
```

引擎名：`shadow_bias`

Used to adjust shadow appearance. Too small a value results in self-shadowing ("shadow acne"), while too large a value causes shadows to separate from casters ("peter-panning"). Adjust as needed.

### ShadowBlur

```csharp
public float ShadowBlur { get; set; }
```

引擎名：`shadow_blur`

Blurs the edges of the shadow. Can be used to hide pixel artifacts in low-resolution shadow maps. A high value can impact performance, make shadows appear grainy and can cause other unwanted artifacts. Try to keep as near default as possible.

### ShadowCasterMask

```csharp
public uint ShadowCasterMask { get; set; }
```

引擎名：`shadow_caster_mask`

The light will only cast shadows using objects in the selected layers.

### ShadowEnabled

```csharp
public bool ShadowEnabled { get; set; }
```

引擎名：`shadow_enabled`

If `true`, the light will cast real-time shadows. This has a significant performance cost. Only enable shadow rendering when it makes a noticeable difference in the scene's appearance, and consider using `DistanceFadeEnabled` to hide the light when far away from the [`Camera3D`](Camera3D.md).

### ShadowNormalBias

```csharp
public float ShadowNormalBias { get; set; }
```

引擎名：`shadow_normal_bias`

Offsets the lookup into the shadow map by the object's normal. This can be used to reduce self-shadowing artifacts without using `ShadowBias`. In practice, this value should be tweaked along with `ShadowBias` to reduce artifacts as much as possible.

### ShadowOpacity

```csharp
public float ShadowOpacity { get; set; }
```

引擎名：`shadow_opacity`

The opacity to use when rendering the light's shadow map. Values lower than `1.0` make the light appear through shadows. This can be used to fake global illumination at a low performance cost.

### ShadowReverseCullFace

```csharp
public bool ShadowReverseCullFace { get; set; }
```

引擎名：`shadow_reverse_cull_face`

If `true`, reverses the backface culling of the mesh. This can be useful when you have a flat mesh that has a light behind it. If you need to cast a shadow on both sides of the mesh, set the mesh to use double-sided shadows with [`GeometryInstance3D.ShadowCastingSetting.DoubleSided`](GeometryInstance3D.md).

### ShadowTransmittanceBias

```csharp
public float ShadowTransmittanceBias { get; set; }
```

引擎名：`shadow_transmittance_bias`

## 方法

### GetBakeMode

```csharp
public Light3D.BakeMode GetBakeMode()
```

引擎名：`get_bake_mode`

### GetColor

```csharp
public Color GetColor()
```

引擎名：`get_color`

### GetCorrelatedColor

```csharp
public Color GetCorrelatedColor()
```

引擎名：`get_correlated_color`

Returns the [`Color`](Color.md) of an idealized blackbody at the given `LightTemperature`. This value is calculated internally based on the `LightTemperature`. This [`Color`](Color.md) is multiplied by `LightColor` before being sent to the [`RenderingServer`](RenderingServer.md).

### GetCullMask

```csharp
public uint GetCullMask()
```

引擎名：`get_cull_mask`

### GetDistanceFadeBegin

```csharp
public float GetDistanceFadeBegin()
```

引擎名：`get_distance_fade_begin`

### GetDistanceFadeLength

```csharp
public float GetDistanceFadeLength()
```

引擎名：`get_distance_fade_length`

### GetDistanceFadeShadow

```csharp
public float GetDistanceFadeShadow()
```

引擎名：`get_distance_fade_shadow`

### GetParam

```csharp
public float GetParam(Light3D.Param param)
```

引擎名：`get_param`

Returns the value of the specified [`Light3D.Param`](Light3D.md) parameter.

### GetProjector

```csharp
public Texture2D GetProjector()
```

引擎名：`get_projector`

### GetShadowCasterMask

```csharp
public uint GetShadowCasterMask()
```

引擎名：`get_shadow_caster_mask`

### GetShadowReverseCullFace

```csharp
public bool GetShadowReverseCullFace()
```

引擎名：`get_shadow_reverse_cull_face`

### GetTemperature

```csharp
public float GetTemperature()
```

引擎名：`get_temperature`

### HasShadow

```csharp
public bool HasShadow()
```

引擎名：`has_shadow`

### IsDistanceFadeEnabled

```csharp
public bool IsDistanceFadeEnabled()
```

引擎名：`is_distance_fade_enabled`

### IsEditorOnly

```csharp
public bool IsEditorOnly()
```

引擎名：`is_editor_only`

### IsNegative

```csharp
public bool IsNegative()
```

引擎名：`is_negative`

### SetBakeMode

```csharp
public void SetBakeMode(Light3D.BakeMode bakeMode)
```

引擎名：`set_bake_mode`

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`

### SetCullMask

```csharp
public void SetCullMask(uint cullMask)
```

引擎名：`set_cull_mask`

### SetDistanceFadeBegin

```csharp
public void SetDistanceFadeBegin(float distance)
```

引擎名：`set_distance_fade_begin`

### SetDistanceFadeLength

```csharp
public void SetDistanceFadeLength(float distance)
```

引擎名：`set_distance_fade_length`

### SetDistanceFadeShadow

```csharp
public void SetDistanceFadeShadow(float distance)
```

引擎名：`set_distance_fade_shadow`

### SetEditorOnly

```csharp
public void SetEditorOnly(bool editorOnly)
```

引擎名：`set_editor_only`

### SetEnableDistanceFade

```csharp
public void SetEnableDistanceFade(bool enable)
```

引擎名：`set_enable_distance_fade`

### SetNegative

```csharp
public void SetNegative(bool enabled)
```

引擎名：`set_negative`

### SetParam

```csharp
public void SetParam(Light3D.Param param, float value)
```

引擎名：`set_param`

Sets the value of the specified [`Light3D.Param`](Light3D.md) parameter.

### SetProjector

```csharp
public void SetProjector(Texture2D projector)
```

引擎名：`set_projector`

### SetShadow

```csharp
public void SetShadow(bool enabled)
```

引擎名：`set_shadow`

### SetShadowCasterMask

```csharp
public void SetShadowCasterMask(uint casterMask)
```

引擎名：`set_shadow_caster_mask`

### SetShadowReverseCullFace

```csharp
public void SetShadowReverseCullFace(bool enable)
```

引擎名：`set_shadow_reverse_cull_face`

### SetTemperature

```csharp
public void SetTemperature(float temperature)
```

引擎名：`set_temperature`

## BakeMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Light is ignored when baking.
- `Static = 1`：Light is taken into account in static baking ([`VoxelGI`](VoxelGI.md), [`LightmapGI`](LightmapGI.md), SDFGI ([`Environment.SdfgiEnabled`](Environment.md))).
- `Dynamic = 2`：Light is taken into account in dynamic baking ([`VoxelGI`](VoxelGI.md) and SDFGI ([`Environment.SdfgiEnabled`](Environment.md))).

## Param

命名空间 `Godot`；enum

### 枚举值

- `Energy = 0`：Constant for accessing [`Light3D.LightEnergy`](Light3D.md).
- `IndirectEnergy = 1`：Constant for accessing [`Light3D.LightIndirectEnergy`](Light3D.md).
- `VolumetricFogEnergy = 2`：Constant for accessing [`Light3D.LightVolumetricFogEnergy`](Light3D.md).
- `Specular = 3`：Constant for accessing [`Light3D.LightSpecular`](Light3D.md).
- `Range = 4`：Constant for accessing [`OmniLight3D.OmniRange`](OmniLight3D.md) or [`SpotLight3D.SpotRange`](SpotLight3D.md).
- `Size = 5`：Constant for accessing [`Light3D.LightSize`](Light3D.md).
- `Attenuation = 6`：Constant for accessing [`OmniLight3D.OmniAttenuation`](OmniLight3D.md) or [`SpotLight3D.SpotAttenuation`](SpotLight3D.md).
- `SpotAngle = 7`：Constant for accessing [`SpotLight3D.SpotAngle`](SpotLight3D.md).
- `SpotAttenuation = 8`：Constant for accessing [`SpotLight3D.SpotAngleAttenuation`](SpotLight3D.md).
- `ShadowMaxDistance = 9`：Constant for accessing [`DirectionalLight3D.DirectionalShadowMaxDistance`](DirectionalLight3D.md).
- `ShadowSplit1Offset = 10`：Constant for accessing [`DirectionalLight3D.DirectionalShadowSplit1`](DirectionalLight3D.md).
- `ShadowSplit2Offset = 11`：Constant for accessing [`DirectionalLight3D.DirectionalShadowSplit2`](DirectionalLight3D.md).
- `ShadowSplit3Offset = 12`：Constant for accessing [`DirectionalLight3D.DirectionalShadowSplit3`](DirectionalLight3D.md).
- `ShadowFadeStart = 13`：Constant for accessing [`DirectionalLight3D.DirectionalShadowFadeStart`](DirectionalLight3D.md).
- `ShadowNormalBias = 14`：Constant for accessing [`Light3D.ShadowNormalBias`](Light3D.md).
- `ShadowBias = 15`：Constant for accessing [`Light3D.ShadowBias`](Light3D.md).
- `ShadowPancakeSize = 16`：Constant for accessing [`DirectionalLight3D.DirectionalShadowPancakeSize`](DirectionalLight3D.md).
- `ShadowOpacity = 17`：Constant for accessing [`Light3D.ShadowOpacity`](Light3D.md).
- `ShadowBlur = 18`：Constant for accessing [`Light3D.ShadowBlur`](Light3D.md).
- `TransmittanceBias = 19`：Constant for accessing [`Light3D.ShadowTransmittanceBias`](Light3D.md).
- `Intensity = 20`：Constant for accessing [`Light3D.LightIntensityLumens`](Light3D.md) and [`Light3D.LightIntensityLux`](Light3D.md).
- `Max = 21`：Represents the size of the `Light3D.Param` enum.
