<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DirectionalLight3D

命名空间 `Godot`；class；继承 [`Light3D`](Light3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A directional light is a type of [`Light3D`](Light3D.md) node that models an infinite number of parallel rays covering the entire scene. It is used for lights with strong intensity that are located far away from the scene to model sunlight or moonlight.

Light is emitted in the -Z direction of the node's global basis. For an unrotated light, this means that the light is emitted forwards, illuminating the front side of a 3D model (see `Vector3.Forward` and `Vector3.ModelFront`). The position of the node is ignored; only the basis is used to determine light direction.

## 构造函数

### DirectionalLight3D

```csharp
public DirectionalLight3D()
```

## 属性

### DirectionalShadowBlendSplits

```csharp
public bool DirectionalShadowBlendSplits { get; set; }
```

引擎名：`directional_shadow_blend_splits`

If `true`, shadow detail is sacrificed in exchange for smoother transitions between splits. Enabling shadow blend splitting also has a moderate performance cost. This is ignored when `DirectionalShadowMode` is [`DirectionalLight3D.ShadowMode.Orthogonal`](DirectionalLight3D.md).

### DirectionalShadowFadeStart

```csharp
public float DirectionalShadowFadeStart { get; set; }
```

引擎名：`directional_shadow_fade_start`

Proportion of `DirectionalShadowMaxDistance` at which point the shadow starts to fade. At `DirectionalShadowMaxDistance`, the shadow will disappear. The default value is a balance between smooth fading and distant shadow visibility. If the camera moves fast and the `DirectionalShadowMaxDistance` is low, consider lowering `DirectionalShadowFadeStart` below `0.8` to make shadow transitions less noticeable. On the other hand, if you tuned `DirectionalShadowMaxDistance` to cover the entire scene, you can set `DirectionalShadowFadeStart` to `1.0` to prevent the shadow from fading in the distance (it will suddenly cut off instead).

### DirectionalShadowMaxDistance

```csharp
public float DirectionalShadowMaxDistance { get; set; }
```

引擎名：`directional_shadow_max_distance`

The maximum distance for shadow splits. Increasing this value will make directional shadows visible from further away, at the cost of lower overall shadow detail and performance (since more objects need to be included in the directional shadow rendering).

### DirectionalShadowMode

```csharp
public DirectionalLight3D.ShadowMode DirectionalShadowMode { get; set; }
```

引擎名：`directional_shadow_mode`

The light's shadow rendering algorithm.

### DirectionalShadowPancakeSize

```csharp
public float DirectionalShadowPancakeSize { get; set; }
```

引擎名：`directional_shadow_pancake_size`

Sets the size of the directional shadow pancake. The pancake offsets the start of the shadow's camera frustum to provide a higher effective depth resolution for the shadow. However, a high pancake size can cause artifacts in the shadows of large objects that are close to the edge of the frustum. Reducing the pancake size can help. Setting the size to `0` turns off the pancaking effect.

### DirectionalShadowSplit1

```csharp
public float DirectionalShadowSplit1 { get; set; }
```

The distance from camera to shadow split 1. Relative to `DirectionalShadowMaxDistance`. Only used when `DirectionalShadowMode` is [`DirectionalLight3D.ShadowMode.Parallel2Splits`](DirectionalLight3D.md) or [`DirectionalLight3D.ShadowMode.Parallel4Splits`](DirectionalLight3D.md).

### DirectionalShadowSplit2

```csharp
public float DirectionalShadowSplit2 { get; set; }
```

The distance from shadow split 1 to split 2. Relative to `DirectionalShadowMaxDistance`. Only used when `DirectionalShadowMode` is [`DirectionalLight3D.ShadowMode.Parallel4Splits`](DirectionalLight3D.md).

### DirectionalShadowSplit3

```csharp
public float DirectionalShadowSplit3 { get; set; }
```

The distance from shadow split 2 to split 3. Relative to `DirectionalShadowMaxDistance`. Only used when `DirectionalShadowMode` is [`DirectionalLight3D.ShadowMode.Parallel4Splits`](DirectionalLight3D.md).

### SkyMode

```csharp
public DirectionalLight3D.SkyModeEnum SkyMode { get; set; }
```

引擎名：`sky_mode`

Whether this `DirectionalLight3D` is visible in the sky, in the scene, or both in the sky and in the scene.

## 方法

### GetShadowMode

```csharp
public DirectionalLight3D.ShadowMode GetShadowMode()
```

引擎名：`get_shadow_mode`

### GetSkyMode

```csharp
public DirectionalLight3D.SkyModeEnum GetSkyMode()
```

引擎名：`get_sky_mode`

### IsBlendSplitsEnabled

```csharp
public bool IsBlendSplitsEnabled()
```

引擎名：`is_blend_splits_enabled`

### SetBlendSplits

```csharp
public void SetBlendSplits(bool enabled)
```

引擎名：`set_blend_splits`

### SetShadowMode

```csharp
public void SetShadowMode(DirectionalLight3D.ShadowMode mode)
```

引擎名：`set_shadow_mode`

### SetSkyMode

```csharp
public void SetSkyMode(DirectionalLight3D.SkyModeEnum mode)
```

引擎名：`set_sky_mode`

## ShadowMode

命名空间 `Godot`；enum

### 枚举值

- `Orthogonal = 0`：Renders the entire scene's shadow map from an orthogonal point of view.
- `Parallel2Splits = 1`：Splits the view frustum in 2 areas, each with its own shadow map.
- `Parallel4Splits = 2`：Splits the view frustum in 4 areas, each with its own shadow map.

## SkyModeEnum

命名空间 `Godot`；enum

### 枚举值

- `LightAndSky = 0`：Makes the light visible in both scene lighting and sky rendering.
- `LightOnly = 1`：Makes the light visible in scene lighting only (including direct lighting and global illumination).
- `SkyOnly = 2`：Makes the light visible to sky shaders only.
