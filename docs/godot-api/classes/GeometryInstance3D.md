<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GeometryInstance3D

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Base node for geometry-based visual instances. Shares some common functionality like visibility and custom materials.

## 构造函数

### GeometryInstance3D

```csharp
public GeometryInstance3D()
```

## 属性

### CastShadow

```csharp
public GeometryInstance3D.ShadowCastingSetting CastShadow { get; set; }
```

引擎名：`cast_shadow`

The mode used to cast shadows from this instance.

### CustomAabb

```csharp
public Aabb CustomAabb { get; set; }
```

引擎名：`custom_aabb`

Overrides the bounding box of this node with a custom one. This can be used to avoid the expensive [`Aabb`](Aabb.md) recalculation that happens when a skeleton is used with a [`MeshInstance3D`](MeshInstance3D.md) or to have precise control over the [`MeshInstance3D`](MeshInstance3D.md)'s bounding box. To use the default AABB, set value to an [`Aabb`](Aabb.md) with all fields set to `0.0`. To avoid frustum culling, set `CustomAabb` to a very large AABB that covers your entire game world such as `AABB(-10000, -10000, -10000, 20000, 20000, 20000)`. To disable all forms of culling (including occlusion and layer culling), call [`RenderingServer.InstanceSetIgnoreCulling`](RenderingServer.md) on the `GeometryInstance3D`'s [`Rid`](Rid.md).

### ExtraCullMargin

```csharp
public float ExtraCullMargin { get; set; }
```

引擎名：`extra_cull_margin`

The extra distance added to the GeometryInstance3D's bounding box ([`Aabb`](Aabb.md)) to increase its cull box.

### GILightmapScale

```csharp
public GeometryInstance3D.LightmapScale GILightmapScale { get; set; }
```

引擎名：`gi_lightmap_scale`

The texel density to use for lightmapping in [`LightmapGI`](LightmapGI.md).

### GILightmapTexelScale

```csharp
public float GILightmapTexelScale { get; set; }
```

引擎名：`gi_lightmap_texel_scale`

The texel density to use for lightmapping in [`LightmapGI`](LightmapGI.md). Greater scale values provide higher resolution in the lightmap, which can result in sharper shadows for lights that have both direct and indirect light baked. However, greater scale values will also increase the space taken by the mesh in the lightmap texture, which increases the memory, storage, and bake time requirements. When using a single mesh at different scales, consider adjusting this value to keep the lightmap texel density consistent across meshes.

For example, doubling `GILightmapTexelScale` doubles the lightmap texture resolution for this object *on each axis*, so it will *quadruple* the texel count.

### GIMode

```csharp
public GeometryInstance3D.GIModeEnum GIMode { get; set; }
```

引擎名：`gi_mode`

The global illumination mode to use for the whole geometry. To avoid inconsistent results, use a mode that matches the purpose of the mesh during gameplay (static/dynamic).

**Note:** Lights' bake mode will also affect the global illumination rendering. See [`Light3D.LightBakeMode`](Light3D.md).

### IgnoreOcclusionCulling

```csharp
public bool IgnoreOcclusionCulling { get; set; }
```

引擎名：`ignore_occlusion_culling`

If `true`, disables occlusion culling for this instance. Useful for gizmos that must be rendered even when occlusion culling is in use.

**Note:** `IgnoreOcclusionCulling` does not affect frustum culling (which is what happens when an object is not visible given the camera's angle). To avoid frustum culling, set `CustomAabb` to a very large AABB that covers your entire game world such as `AABB(-10000, -10000, -10000, 20000, 20000, 20000)`.

### LodBias

```csharp
public float LodBias { get; set; }
```

引擎名：`lod_bias`

Changes how quickly the mesh transitions to a lower level of detail. A value of 0 will force the mesh to its lowest level of detail, a value of 1 will use the default settings, and larger values will keep the mesh in a higher level of detail at farther distances.

Useful for testing level of detail transitions in the editor.

### MaterialOverlay

```csharp
public Material MaterialOverlay { get; set; }
```

引擎名：`material_overlay`

The material overlay for the whole geometry.

If a material is assigned to this property, it will be rendered on top of any other active material for all the surfaces.

### MaterialOverride

```csharp
public Material MaterialOverride { get; set; }
```

引擎名：`material_override`

The material override for the whole geometry.

If a material is assigned to this property, it will be used instead of any material set in any material slot of the mesh.

### Transparency

```csharp
public float Transparency { get; set; }
```

引擎名：`transparency`

The transparency applied to the whole geometry (as a multiplier of the materials' existing transparency). `0.0` is fully opaque, while `1.0` is fully transparent. Values greater than `0.0` (exclusive) will force the geometry's materials to go through the transparent pipeline, which is slower to render and can exhibit rendering issues due to incorrect transparency sorting. However, unlike using a transparent material, setting `Transparency` to a value greater than `0.0` (exclusive) will *not* disable shadow rendering.

In spatial shaders, `1.0 - transparency` is set as the default value of the `ALPHA` built-in.

**Note:** `Transparency` is clamped between `0.0` and `1.0`, so this property cannot be used to make transparent materials more opaque than they originally are.

**Note:** Only supported when using the Forward+ rendering method. When using the Mobile or Compatibility rendering method, `Transparency` is ignored and is considered as always being `0.0`.

### VisibilityRangeBegin

```csharp
public float VisibilityRangeBegin { get; set; }
```

引擎名：`visibility_range_begin`

Starting distance from which the GeometryInstance3D will be visible, taking `VisibilityRangeBeginMargin` into account as well. The default value of 0 is used to disable the range check.

### VisibilityRangeBeginMargin

```csharp
public float VisibilityRangeBeginMargin { get; set; }
```

引擎名：`visibility_range_begin_margin`

Margin for the `VisibilityRangeBegin` threshold. The GeometryInstance3D will only change its visibility state when it goes over or under the `VisibilityRangeBegin` threshold by this amount.

If `VisibilityRangeFadeMode` is [`GeometryInstance3D.VisibilityRangeFadeModeEnum.Disabled`](GeometryInstance3D.md), this acts as a hysteresis distance. If `VisibilityRangeFadeMode` is [`GeometryInstance3D.VisibilityRangeFadeModeEnum.Self`](GeometryInstance3D.md) or [`GeometryInstance3D.VisibilityRangeFadeModeEnum.Dependencies`](GeometryInstance3D.md), this acts as a fade transition distance and must be set to a value greater than `0.0` for the effect to be noticeable.

### VisibilityRangeEnd

```csharp
public float VisibilityRangeEnd { get; set; }
```

引擎名：`visibility_range_end`

Distance from which the GeometryInstance3D will be hidden, taking `VisibilityRangeEndMargin` into account as well. The default value of 0 is used to disable the range check.

### VisibilityRangeEndMargin

```csharp
public float VisibilityRangeEndMargin { get; set; }
```

引擎名：`visibility_range_end_margin`

Margin for the `VisibilityRangeEnd` threshold. The GeometryInstance3D will only change its visibility state when it goes over or under the `VisibilityRangeEnd` threshold by this amount.

If `VisibilityRangeFadeMode` is [`GeometryInstance3D.VisibilityRangeFadeModeEnum.Disabled`](GeometryInstance3D.md), this acts as a hysteresis distance. If `VisibilityRangeFadeMode` is [`GeometryInstance3D.VisibilityRangeFadeModeEnum.Self`](GeometryInstance3D.md) or [`GeometryInstance3D.VisibilityRangeFadeModeEnum.Dependencies`](GeometryInstance3D.md), this acts as a fade transition distance and must be set to a value greater than `0.0` for the effect to be noticeable.

### VisibilityRangeFadeMode

```csharp
public GeometryInstance3D.VisibilityRangeFadeModeEnum VisibilityRangeFadeMode { get; set; }
```

引擎名：`visibility_range_fade_mode`

Controls which instances will be faded when approaching the limits of the visibility range.

## 方法

### GetCastShadowsSetting

```csharp
public GeometryInstance3D.ShadowCastingSetting GetCastShadowsSetting()
```

引擎名：`get_cast_shadows_setting`

### GetCustomAabb

```csharp
public Aabb GetCustomAabb()
```

引擎名：`get_custom_aabb`

### GetExtraCullMargin

```csharp
public float GetExtraCullMargin()
```

引擎名：`get_extra_cull_margin`

### GetGIMode

```csharp
public GeometryInstance3D.GIModeEnum GetGIMode()
```

引擎名：`get_gi_mode`

### GetInstanceShaderParameter

```csharp
public Variant GetInstanceShaderParameter(StringName name)
```

引擎名：`get_instance_shader_parameter`

Get the value of a shader parameter as set on this instance.

### GetLightmapScale

```csharp
public GeometryInstance3D.LightmapScale GetLightmapScale()
```

引擎名：`get_lightmap_scale`

### GetLightmapTexelScale

```csharp
public float GetLightmapTexelScale()
```

引擎名：`get_lightmap_texel_scale`

### GetLodBias

```csharp
public float GetLodBias()
```

引擎名：`get_lod_bias`

### GetMaterialOverlay

```csharp
public Material GetMaterialOverlay()
```

引擎名：`get_material_overlay`

### GetMaterialOverride

```csharp
public Material GetMaterialOverride()
```

引擎名：`get_material_override`

### GetTransparency

```csharp
public float GetTransparency()
```

引擎名：`get_transparency`

### GetVisibilityRangeBegin

```csharp
public float GetVisibilityRangeBegin()
```

引擎名：`get_visibility_range_begin`

### GetVisibilityRangeBeginMargin

```csharp
public float GetVisibilityRangeBeginMargin()
```

引擎名：`get_visibility_range_begin_margin`

### GetVisibilityRangeEnd

```csharp
public float GetVisibilityRangeEnd()
```

引擎名：`get_visibility_range_end`

### GetVisibilityRangeEndMargin

```csharp
public float GetVisibilityRangeEndMargin()
```

引擎名：`get_visibility_range_end_margin`

### GetVisibilityRangeFadeMode

```csharp
public GeometryInstance3D.VisibilityRangeFadeModeEnum GetVisibilityRangeFadeMode()
```

引擎名：`get_visibility_range_fade_mode`

### IsIgnoringOcclusionCulling

```csharp
public bool IsIgnoringOcclusionCulling()
```

引擎名：`is_ignoring_occlusion_culling`

### SetCastShadowsSetting

```csharp
public void SetCastShadowsSetting(GeometryInstance3D.ShadowCastingSetting shadowCastingSetting)
```

引擎名：`set_cast_shadows_setting`

### SetCustomAabb

```csharp
public void SetCustomAabb(Aabb aabb)
```

引擎名：`set_custom_aabb`

### SetExtraCullMargin

```csharp
public void SetExtraCullMargin(float margin)
```

引擎名：`set_extra_cull_margin`

### SetGIMode

```csharp
public void SetGIMode(GeometryInstance3D.GIModeEnum mode)
```

引擎名：`set_gi_mode`

### SetIgnoreOcclusionCulling

```csharp
public void SetIgnoreOcclusionCulling(bool ignoreCulling)
```

引擎名：`set_ignore_occlusion_culling`

### SetInstanceShaderParameter

```csharp
public void SetInstanceShaderParameter(StringName name, Variant value)
```

引擎名：`set_instance_shader_parameter`

Set the value of a shader uniform for this instance only ([per-instance uniform]($DOCS_URL/tutorials/shaders/shader_reference/shading_language.html#per-instance-uniforms)). See also [`ShaderMaterial.SetShaderParameter`](ShaderMaterial.md) to assign a uniform on all instances using the same [`ShaderMaterial`](ShaderMaterial.md).

**Note:** For a shader uniform to be assignable on a per-instance basis, it *must* be defined with `instance uniform ...` rather than `uniform ...` in the shader code.

**Note:** `name` is case-sensitive and must match the name of the uniform in the code exactly (not the capitalized name in the inspector).

**Note:** Per-instance shader uniforms are only available in Spatial and CanvasItem shaders, but not for Fog, Sky, or Particles shaders.

### SetLightmapScale

```csharp
public void SetLightmapScale(GeometryInstance3D.LightmapScale scale)
```

引擎名：`set_lightmap_scale`

### SetLightmapTexelScale

```csharp
public void SetLightmapTexelScale(float scale)
```

引擎名：`set_lightmap_texel_scale`

### SetLodBias

```csharp
public void SetLodBias(float bias)
```

引擎名：`set_lod_bias`

### SetMaterialOverlay

```csharp
public void SetMaterialOverlay(Material material)
```

引擎名：`set_material_overlay`

### SetMaterialOverride

```csharp
public void SetMaterialOverride(Material material)
```

引擎名：`set_material_override`

### SetTransparency

```csharp
public void SetTransparency(float transparency)
```

引擎名：`set_transparency`

### SetVisibilityRangeBegin

```csharp
public void SetVisibilityRangeBegin(float distance)
```

引擎名：`set_visibility_range_begin`

### SetVisibilityRangeBeginMargin

```csharp
public void SetVisibilityRangeBeginMargin(float distance)
```

引擎名：`set_visibility_range_begin_margin`

### SetVisibilityRangeEnd

```csharp
public void SetVisibilityRangeEnd(float distance)
```

引擎名：`set_visibility_range_end`

### SetVisibilityRangeEndMargin

```csharp
public void SetVisibilityRangeEndMargin(float distance)
```

引擎名：`set_visibility_range_end_margin`

### SetVisibilityRangeFadeMode

```csharp
public void SetVisibilityRangeFadeMode(GeometryInstance3D.VisibilityRangeFadeModeEnum mode)
```

引擎名：`set_visibility_range_fade_mode`

## GIModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disabled global illumination mode.
- `Static = 1`：Baked global illumination mode.
- `Dynamic = 2`：Dynamic global illumination mode.

## LightmapScale

命名空间 `Godot`；enum

### 枚举值

- `Scale1X = 0`：The standard texel density for lightmapping with [`LightmapGI`](LightmapGI.md).
- `Scale2X = 1`：Multiplies texel density by 2× for lightmapping with [`LightmapGI`](LightmapGI.md).
- `Scale4X = 2`：Multiplies texel density by 4× for lightmapping with [`LightmapGI`](LightmapGI.md).
- `Scale8X = 3`：Multiplies texel density by 8× for lightmapping with [`LightmapGI`](LightmapGI.md).
- `Max = 4`：Represents the size of the `GeometryInstance3D.LightmapScale` enum.

## ShadowCastingSetting

命名空间 `Godot`；enum

### 枚举值

- `Off = 0`：Will not cast any shadows.
- `On = 1`：Will cast shadows from all visible faces in the GeometryInstance3D.
- `DoubleSided = 2`：Will cast shadows from all visible faces in the GeometryInstance3D.
- `ShadowsOnly = 3`：Will only show the shadows casted from this object.

## VisibilityRangeFadeModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Will not fade itself nor its visibility dependencies, hysteresis will be used instead.
- `Self = 1`：Will fade-out itself when reaching the limits of its own visibility range.
- `Dependencies = 2`：Will fade-in its visibility dependencies (see [`Node3D.VisibilityParent`](Node3D.md)) when reaching the limits of its own visibility range.
