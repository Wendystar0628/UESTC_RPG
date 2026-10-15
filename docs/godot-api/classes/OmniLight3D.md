<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OmniLight3D

命名空间 `Godot`；class；继承 [`Light3D`](Light3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An Omnidirectional light is a type of [`Light3D`](Light3D.md) that emits light in all directions. The light is attenuated by distance and this attenuation can be configured by changing its energy, radius, and attenuation parameters.

**Note:** When using the Mobile rendering method, only 8 omni lights can be displayed on each mesh resource. Attempting to display more than 8 omni lights on a single mesh resource will result in omni lights flickering in and out as the camera moves. When using the Compatibility rendering method, only 8 omni lights can be displayed on each mesh resource by default, but this can be increased by adjusting `ProjectSettings.rendering/limits/opengl/max_lights_per_object`.

**Note:** When using the Mobile or Compatibility rendering methods, omni lights will only correctly affect meshes whose visibility AABB intersects with the light's AABB. If using a shader to deform the mesh in a way that makes it go outside its AABB, [`GeometryInstance3D.ExtraCullMargin`](GeometryInstance3D.md) must be increased on the mesh. Otherwise, the light may not be visible on the mesh.

## 构造函数

### OmniLight3D

```csharp
public OmniLight3D()
```

## 属性

### OmniAttenuation

```csharp
public float OmniAttenuation { get; set; }
```

引擎名：`omni_attenuation`

Controls the distance attenuation function for omnilights.

A value of `0.0` will maintain a constant brightness through most of the range, but smoothly attenuate the light at the edge of the range. Use a value of `2.0` for physically accurate lights as it results in the proper inverse square attenutation.

**Note:** Setting attenuation to `2.0` or higher may result in distant objects receiving minimal light, even within range. For example, with a range of `4096`, an object at `100` units is attenuated by a factor of `0.0001`. With a default brightness of `1`, the light would not be visible at that distance.

**Note:** Using negative or values higher than `10.0` may lead to unexpected results.

### OmniRange

```csharp
public float OmniRange { get; set; }
```

引擎名：`omni_range`

The light's radius. Note that the effectively lit area may appear to be smaller depending on the `OmniAttenuation` in use. No matter the `OmniAttenuation` in use, the light will never reach anything outside this radius.

**Note:** `OmniRange` is not affected by [`Node3D.Scale`](Node3D.md) (the light's scale or its parent's scale).

### OmniShadowMode

```csharp
public OmniLight3D.ShadowMode OmniShadowMode { get; set; }
```

引擎名：`omni_shadow_mode`

## 方法

### GetShadowMode

```csharp
public OmniLight3D.ShadowMode GetShadowMode()
```

引擎名：`get_shadow_mode`

### SetShadowMode

```csharp
public void SetShadowMode(OmniLight3D.ShadowMode mode)
```

引擎名：`set_shadow_mode`

## ShadowMode

命名空间 `Godot`；enum

### 枚举值

- `DualParaboloid = 0`：Shadows are rendered to a dual-paraboloid texture.
- `Cube = 1`：Shadows are rendered to a cubemap.
