<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LightmapGI

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `LightmapGI` node is used to compute and store baked lightmaps. Lightmaps are used to provide high-quality indirect lighting with very little light leaking. `LightmapGI` can also provide rough reflections using spherical harmonics if `Directional` is enabled. Dynamic objects can receive indirect lighting thanks to *light probes*, which can be automatically placed by setting `GenerateProbesSubdiv` to a value other than [`LightmapGI.GenerateProbes.Disabled`](LightmapGI.md). Additional lightmap probes can also be added by creating [`LightmapProbe`](LightmapProbe.md) nodes. The downside is that lightmaps are fully static and cannot be baked in an exported project. Baking a `LightmapGI` node is also slower compared to [`VoxelGI`](VoxelGI.md).

**Procedural generation:** Lightmap baking functionality is only available in the editor. This means `LightmapGI` is not suited to procedurally generated or user-built levels. For procedurally generated or user-built levels, use [`VoxelGI`](VoxelGI.md) or SDFGI instead (see [`Environment.SdfgiEnabled`](Environment.md)).

**Performance:** `LightmapGI` provides the best possible run-time performance for global illumination. It is suitable for low-end hardware including integrated graphics and mobile devices.

**Note:** Due to how lightmaps work, most properties only have a visible effect once lightmaps are baked again.

**Note:** Lightmap baking on [`CsgShape3D`](CsgShape3D.md)s and [`PrimitiveMesh`](PrimitiveMesh.md)es is not supported, as these cannot store UV2 data required for baking.

**Note:** If no custom lightmappers are installed, `LightmapGI` can only be baked from devices that support the Forward+ or Mobile renderers.

**Note:** The `LightmapGI` node only bakes light data for child nodes of its parent. Nodes further up the hierarchy of the scene will not be baked.

## 构造函数

### LightmapGI

```csharp
public LightmapGI()
```

## 属性

### Bias

```csharp
public float Bias { get; set; }
```

引擎名：`bias`

The bias to use when computing shadows. Increasing `Bias` can fix shadow acne on the resulting baked lightmap, but can introduce peter-panning (shadows not connecting to their casters). Real-time [`Light3D`](Light3D.md) shadows are not affected by this `Bias` property.

### BounceIndirectEnergy

```csharp
public float BounceIndirectEnergy { get; set; }
```

引擎名：`bounce_indirect_energy`

The energy multiplier for each bounce. Higher values will make indirect lighting brighter. A value of `1.0` represents physically accurate behavior, but higher values can be used to make indirect lighting propagate more visibly when using a low number of bounces. This can be used to speed up bake times by lowering the number of `Bounces` then increasing `BounceIndirectEnergy`.

**Note:** `BounceIndirectEnergy` only has an effect if `Bounces` is set to a value greater than or equal to `1`.

### Bounces

```csharp
public int Bounces { get; set; }
```

引擎名：`bounces`

Number of light bounces that are taken into account during baking. Higher values result in brighter, more realistic lighting, at the cost of longer bake times. If set to `0`, only environment lighting, direct light and emissive lighting is baked.

### CameraAttributes

```csharp
public CameraAttributes CameraAttributes { get; set; }
```

引擎名：`camera_attributes`

The [`CameraAttributes`](CameraAttributes.md) resource that specifies exposure levels to bake at. Auto-exposure and non exposure properties will be ignored. Exposure settings should be used to reduce the dynamic range present when baking. If exposure is too high, the `LightmapGI` will have banding artifacts or may have over-exposure artifacts.

### DenoiserRange

```csharp
public int DenoiserRange { get; set; }
```

引擎名：`denoiser_range`

The distance in pixels from which the denoiser samples. Lower values preserve more details, but may give blotchy results if the lightmap quality is not high enough. Only effective if `UseDenoiser` is `true` and `ProjectSettings.rendering/lightmapping/denoising/denoiser` is set to JNLM.

### DenoiserStrength

```csharp
public float DenoiserStrength { get; set; }
```

引擎名：`denoiser_strength`

The strength of denoising step applied to the generated lightmaps. Only effective if `UseDenoiser` is `true` and `ProjectSettings.rendering/lightmapping/denoising/denoiser` is set to JNLM.

### Directional

```csharp
public bool Directional { get; set; }
```

引擎名：`directional`

If `true`, bakes lightmaps to contain directional information as spherical harmonics. This results in more realistic lighting appearance, especially with normal mapped materials and for lights that have their direct light baked ([`Light3D.LightBakeMode`](Light3D.md) set to [`Light3D.BakeMode.Static`](Light3D.md) and with [`Light3D.EditorOnly`](Light3D.md) set to `false`). The directional information is also used to provide rough reflections for static and dynamic objects. This has a small run-time performance cost as the shader has to perform more work to interpret the direction information from the lightmap. Directional lightmaps also take longer to bake and result in larger file sizes.

**Note:** The property's name has no relationship with [`DirectionalLight3D`](DirectionalLight3D.md). `Directional` works with all light types.

### EnvironmentCustomColor

```csharp
public Color EnvironmentCustomColor { get; set; }
```

引擎名：`environment_custom_color`

The color to use for environment lighting. Only effective if `EnvironmentMode` is [`LightmapGI.EnvironmentModeEnum.CustomColor`](LightmapGI.md).

### EnvironmentCustomEnergy

```csharp
public float EnvironmentCustomEnergy { get; set; }
```

引擎名：`environment_custom_energy`

The color multiplier to use for environment lighting. Only effective if `EnvironmentMode` is [`LightmapGI.EnvironmentModeEnum.CustomColor`](LightmapGI.md).

### EnvironmentCustomSky

```csharp
public Sky EnvironmentCustomSky { get; set; }
```

引擎名：`environment_custom_sky`

The sky to use as a source of environment lighting. Only effective if `EnvironmentMode` is [`LightmapGI.EnvironmentModeEnum.CustomSky`](LightmapGI.md).

### EnvironmentMode

```csharp
public LightmapGI.EnvironmentModeEnum EnvironmentMode { get; set; }
```

引擎名：`environment_mode`

The environment mode to use when baking lightmaps.

### GenerateProbesSubdiv

```csharp
public LightmapGI.GenerateProbes GenerateProbesSubdiv { get; set; }
```

引擎名：`generate_probes_subdiv`

The level of subdivision to use when automatically generating [`LightmapProbe`](LightmapProbe.md)s for dynamic object lighting. Higher values result in more accurate indirect lighting on dynamic objects, at the cost of longer bake times and larger file sizes.

**Note:** Automatically generated [`LightmapProbe`](LightmapProbe.md)s are not visible as nodes in the Scene tree dock, and cannot be modified this way after they are generated.

**Note:** Regardless of `GenerateProbesSubdiv`, direct lighting on dynamic objects is always applied using [`Light3D`](Light3D.md) nodes in real-time.

### Interior

```csharp
public bool Interior { get; set; }
```

引擎名：`interior`

If `true`, ignore environment lighting when baking lightmaps.

### LightData

```csharp
public LightmapGIData LightData { get; set; }
```

引擎名：`light_data`

The [`LightmapGIData`](LightmapGIData.md) associated to this `LightmapGI` node. This resource is automatically created after baking, and is not meant to be created manually.

### MaxTextureSize

```csharp
public int MaxTextureSize { get; set; }
```

引擎名：`max_texture_size`

The maximum texture size for the generated texture atlas. Higher values will result in fewer slices being generated, but may not work on all hardware as a result of hardware limitations on texture sizes. Leave `MaxTextureSize` at its default value of `16384` if unsure.

### Quality

```csharp
public LightmapGI.BakeQuality Quality { get; set; }
```

引擎名：`quality`

The quality preset to use when baking lightmaps. This affects bake times, but output file sizes remain mostly identical across quality levels.

To further speed up bake times, decrease `Bounces`, disable `UseDenoiser` and/or decrease `TexelScale`.

To further increase quality, enable `Supersampling` and/or increase `TexelScale`.

### ShadowmaskMode

```csharp
public LightmapGIData.ShadowmaskMode ShadowmaskMode { get; set; }
```

引擎名：`shadowmask_mode`

The shadowmasking policy to use for directional shadows on static objects that are baked with this `LightmapGI` instance.

Shadowmasking allows [`DirectionalLight3D`](DirectionalLight3D.md) nodes to cast shadows even outside the range defined by their [`DirectionalLight3D.DirectionalShadowMaxDistance`](DirectionalLight3D.md) property. This is done by baking a texture that contains a shadowmap for the directional light, then using this texture according to the current shadowmask mode.

**Note:** The shadowmask texture is only created if `ShadowmaskMode` is not [`LightmapGIData.ShadowmaskMode.None`](LightmapGIData.md). To see a difference, you need to bake lightmaps again after switching from [`LightmapGIData.ShadowmaskMode.None`](LightmapGIData.md) to any other mode.

### Supersampling

```csharp
public bool Supersampling { get; set; }
```

引擎名：`supersampling`

If `true`, lightmaps are baked with the texel scale multiplied with `SupersamplingFactor` and downsampled before saving the lightmap (so the effective texel density is identical to having supersampling disabled).

Supersampling provides increased lightmap quality with less noise, smoother shadows and better shadowing of small-scale features in objects. However, it may result in significantly increased bake times and memory usage while baking lightmaps. Padding is automatically adjusted to avoid increasing light leaking.

### SupersamplingFactor

```csharp
public float SupersamplingFactor { get; set; }
```

引擎名：`supersampling_factor`

The factor by which the texel density is multiplied for supersampling. For best results, use an integer value. While fractional values are allowed, they can result in increased light leaking and a blurry lightmap.

Higher values may result in better quality, but also increase bake times and memory usage while baking.

See `Supersampling` for more information.

### TexelScale

```csharp
public float TexelScale { get; set; }
```

引擎名：`texel_scale`

Scales the lightmap texel density of all meshes for the current bake. This is a multiplier that builds upon the existing lightmap texel size defined in each imported 3D scene, along with the per-mesh density multiplier (which is designed to be used when the same mesh is used at different scales). Lower values will result in faster bake times.

For example, doubling `TexelScale` doubles the lightmap texture resolution for all objects *on each axis*, so it will *quadruple* the texel count.

### UseDenoiser

```csharp
public bool UseDenoiser { get; set; }
```

引擎名：`use_denoiser`

If `true`, uses a GPU-based denoising algorithm on the generated lightmap. This eliminates most noise within the generated lightmap at the cost of longer bake times. File sizes are generally not impacted significantly by the use of a denoiser, although lossless compression may do a better job at compressing a denoised image.

### UseTextureForBounces

```csharp
public bool UseTextureForBounces { get; set; }
```

引擎名：`use_texture_for_bounces`

If `true`, a texture with the lighting information will be generated to speed up the generation of indirect lighting at the cost of some accuracy. The geometry might exhibit extra light leak artifacts when using low resolution lightmaps or UVs that stretch the lightmap significantly across surfaces. Leave `UseTextureForBounces` at its default value of `true` if unsure.

**Note:** `UseTextureForBounces` only has an effect if `Bounces` is set to a value greater than or equal to `1`.

## 方法

### GetBakeQuality

```csharp
public LightmapGI.BakeQuality GetBakeQuality()
```

引擎名：`get_bake_quality`

### GetBias

```csharp
public float GetBias()
```

引擎名：`get_bias`

### GetBounceIndirectEnergy

```csharp
public float GetBounceIndirectEnergy()
```

引擎名：`get_bounce_indirect_energy`

### GetBounces

```csharp
public int GetBounces()
```

引擎名：`get_bounces`

### GetCameraAttributes

```csharp
public CameraAttributes GetCameraAttributes()
```

引擎名：`get_camera_attributes`

### GetDenoiserRange

```csharp
public int GetDenoiserRange()
```

引擎名：`get_denoiser_range`

### GetDenoiserStrength

```csharp
public float GetDenoiserStrength()
```

引擎名：`get_denoiser_strength`

### GetEnvironmentCustomColor

```csharp
public Color GetEnvironmentCustomColor()
```

引擎名：`get_environment_custom_color`

### GetEnvironmentCustomEnergy

```csharp
public float GetEnvironmentCustomEnergy()
```

引擎名：`get_environment_custom_energy`

### GetEnvironmentCustomSky

```csharp
public Sky GetEnvironmentCustomSky()
```

引擎名：`get_environment_custom_sky`

### GetEnvironmentMode

```csharp
public LightmapGI.EnvironmentModeEnum GetEnvironmentMode()
```

引擎名：`get_environment_mode`

### GetGenerateProbes

```csharp
public LightmapGI.GenerateProbes GetGenerateProbes()
```

引擎名：`get_generate_probes`

### GetLightData

```csharp
public LightmapGIData GetLightData()
```

引擎名：`get_light_data`

### GetMaxTextureSize

```csharp
public int GetMaxTextureSize()
```

引擎名：`get_max_texture_size`

### GetShadowmaskMode

```csharp
public LightmapGIData.ShadowmaskMode GetShadowmaskMode()
```

引擎名：`get_shadowmask_mode`

### GetSupersamplingFactor

```csharp
public float GetSupersamplingFactor()
```

引擎名：`get_supersampling_factor`

### GetTexelScale

```csharp
public float GetTexelScale()
```

引擎名：`get_texel_scale`

### IsDirectional

```csharp
public bool IsDirectional()
```

引擎名：`is_directional`

### IsInterior

```csharp
public bool IsInterior()
```

引擎名：`is_interior`

### IsSupersamplingEnabled

```csharp
public bool IsSupersamplingEnabled()
```

引擎名：`is_supersampling_enabled`

### IsUsingDenoiser

```csharp
public bool IsUsingDenoiser()
```

引擎名：`is_using_denoiser`

### IsUsingTextureForBounces

```csharp
public bool IsUsingTextureForBounces()
```

引擎名：`is_using_texture_for_bounces`

### SetBakeQuality

```csharp
public void SetBakeQuality(LightmapGI.BakeQuality bakeQuality)
```

引擎名：`set_bake_quality`

### SetBias

```csharp
public void SetBias(float bias)
```

引擎名：`set_bias`

### SetBounceIndirectEnergy

```csharp
public void SetBounceIndirectEnergy(float bounceIndirectEnergy)
```

引擎名：`set_bounce_indirect_energy`

### SetBounces

```csharp
public void SetBounces(int bounces)
```

引擎名：`set_bounces`

### SetCameraAttributes

```csharp
public void SetCameraAttributes(CameraAttributes cameraAttributes)
```

引擎名：`set_camera_attributes`

### SetDenoiserRange

```csharp
public void SetDenoiserRange(int denoiserRange)
```

引擎名：`set_denoiser_range`

### SetDenoiserStrength

```csharp
public void SetDenoiserStrength(float denoiserStrength)
```

引擎名：`set_denoiser_strength`

### SetDirectional

```csharp
public void SetDirectional(bool directional)
```

引擎名：`set_directional`

### SetEnvironmentCustomColor

```csharp
public void SetEnvironmentCustomColor(Color color)
```

引擎名：`set_environment_custom_color`

### SetEnvironmentCustomEnergy

```csharp
public void SetEnvironmentCustomEnergy(float energy)
```

引擎名：`set_environment_custom_energy`

### SetEnvironmentCustomSky

```csharp
public void SetEnvironmentCustomSky(Sky sky)
```

引擎名：`set_environment_custom_sky`

### SetEnvironmentMode

```csharp
public void SetEnvironmentMode(LightmapGI.EnvironmentModeEnum mode)
```

引擎名：`set_environment_mode`

### SetGenerateProbes

```csharp
public void SetGenerateProbes(LightmapGI.GenerateProbes subdivision)
```

引擎名：`set_generate_probes`

### SetInterior

```csharp
public void SetInterior(bool enable)
```

引擎名：`set_interior`

### SetLightData

```csharp
public void SetLightData(LightmapGIData data)
```

引擎名：`set_light_data`

### SetMaxTextureSize

```csharp
public void SetMaxTextureSize(int maxTextureSize)
```

引擎名：`set_max_texture_size`

### SetShadowmaskMode

```csharp
public void SetShadowmaskMode(LightmapGIData.ShadowmaskMode mode)
```

引擎名：`set_shadowmask_mode`

### SetSupersamplingEnabled

```csharp
public void SetSupersamplingEnabled(bool enable)
```

引擎名：`set_supersampling_enabled`

### SetSupersamplingFactor

```csharp
public void SetSupersamplingFactor(float factor)
```

引擎名：`set_supersampling_factor`

### SetTexelScale

```csharp
public void SetTexelScale(float texelScale)
```

引擎名：`set_texel_scale`

### SetUseDenoiser

```csharp
public void SetUseDenoiser(bool useDenoiser)
```

引擎名：`set_use_denoiser`

### SetUseTextureForBounces

```csharp
public void SetUseTextureForBounces(bool useTextureForBounces)
```

引擎名：`set_use_texture_for_bounces`

## BakeError

命名空间 `Godot`；enum

### 枚举值

- `Ok = 0`：Lightmap baking was successful.
- `NoSceneRoot = 1`：Lightmap baking failed because the root node for the edited scene could not be accessed.
- `ForeignData = 2`：Lightmap baking failed as the lightmap data resource is embedded in a foreign resource.
- `NoLightmapper = 3`：Lightmap baking failed as there is no lightmapper available in this Godot build.
- `NoSavePath = 4`：Lightmap baking failed as the [`LightmapGIData`](LightmapGIData.md) save path isn't configured in the resource.
- `NoMeshes = 5`：Lightmap baking failed as there are no meshes whose [`GeometryInstance3D.GIMode`](GeometryInstance3D.md) is [`GeometryInstance3D.GIModeEnum.Static`](GeometryInstance3D.md) and with valid UV2 mapping in the current scene.
- `MeshesInvalid = 6`：Lightmap baking failed as the lightmapper failed to analyze some of the meshes marked as static for baking.
- `CantCreateImage = 7`：Lightmap baking failed as the resulting image couldn't be saved or imported by Godot after it was saved.
- `UserAborted = 8`：The user aborted the lightmap baking operation (typically by clicking the **Cancel** button in the progress dialog).
- `TextureSizeTooSmall = 9`：Lightmap baking failed as the maximum texture size is too small to fit some of the meshes marked for baking.
- `LightmapTooSmall = 10`：Lightmap baking failed as the lightmap is too small.
- `AtlasTooSmall = 11`：Lightmap baking failed as the lightmap was unable to fit into an atlas.

## BakeQuality

命名空间 `Godot`；enum

### 枚举值

- `Low = 0`：Low bake quality (fastest bake times).
- `Medium = 1`：Medium bake quality (fast bake times).
- `High = 2`：High bake quality (slow bake times).
- `Ultra = 3`：Highest bake quality (slowest bake times).

## EnvironmentModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Ignore environment lighting when baking lightmaps.
- `Scene = 1`：Use the scene's environment lighting when baking lightmaps.
- `CustomSky = 2`：Use [`LightmapGI.EnvironmentCustomSky`](LightmapGI.md) as a source of environment lighting when baking lightmaps.
- `CustomColor = 3`：Use [`LightmapGI.EnvironmentCustomColor`](LightmapGI.md) multiplied by [`LightmapGI.EnvironmentCustomEnergy`](LightmapGI.md) as a constant source of environment lighting when baking lightmaps.

## GenerateProbes

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Don't generate lightmap probes for lighting dynamic objects.
- `Subdiv4 = 1`：Lowest level of subdivision (fastest bake times, smallest file sizes).
- `Subdiv8 = 2`：Low level of subdivision (fast bake times, small file sizes).
- `Subdiv16 = 3`：High level of subdivision (slow bake times, large file sizes).
- `Subdiv32 = 4`：Highest level of subdivision (slowest bake times, largest file sizes).
