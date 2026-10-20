<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CameraAttributesPractical

命名空间 `Godot`；class；继承 [`CameraAttributes`](CameraAttributes.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Controls camera-specific attributes such as auto-exposure, depth of field, and exposure override.

When used in a [`WorldEnvironment`](WorldEnvironment.md) it provides default settings for exposure, auto-exposure, and depth of field that will be used by all cameras without their own [`CameraAttributes`](CameraAttributes.md), including the editor camera. When used in a [`Camera3D`](Camera3D.md) it will override any [`CameraAttributes`](CameraAttributes.md) set in the [`WorldEnvironment`](WorldEnvironment.md). When used in [`VoxelGI`](VoxelGI.md) or [`LightmapGI`](LightmapGI.md), only the exposure settings will be used.

**Note:** Depth of field blur is only supported in the Forward+ and Mobile rendering methods, not Compatibility.

**Note:** Auto-exposure is only supported in the Forward+ rendering method, not Mobile or Compatibility.

## 构造函数

### CameraAttributesPractical

```csharp
public CameraAttributesPractical()
```

## 属性

### AutoExposureMaxSensitivity

```csharp
public float AutoExposureMaxSensitivity { get; set; }
```

引擎名：`auto_exposure_max_sensitivity`

The maximum sensitivity (in ISO) used when calculating auto exposure. When calculating scene average luminance, color values will be clamped to at least this value. This limits the auto-exposure from exposing below a certain brightness, resulting in a cut off point where the scene will remain bright.

### AutoExposureMinSensitivity

```csharp
public float AutoExposureMinSensitivity { get; set; }
```

引擎名：`auto_exposure_min_sensitivity`

The minimum sensitivity (in ISO) used when calculating auto exposure. When calculating scene average luminance, color values will be clamped to at least this value. This limits the auto-exposure from exposing above a certain brightness, resulting in a cut off point where the scene will remain dark.

### DofBlurAmount

```csharp
public float DofBlurAmount { get; set; }
```

引擎名：`dof_blur_amount`

Sets the maximum amount of blur. When using physically-based blur amounts, will instead act as a multiplier. High values lead to an increased amount of blurriness, but can be much more expensive to calculate. It is best to keep this as low as possible for a given art style.

### DofBlurFarDistance

```csharp
public float DofBlurFarDistance { get; set; }
```

引擎名：`dof_blur_far_distance`

Objects further from the [`Camera3D`](Camera3D.md) by this amount will be blurred by the depth of field effect. Measured in meters.

### DofBlurFarEnabled

```csharp
public bool DofBlurFarEnabled { get; set; }
```

引擎名：`dof_blur_far_enabled`

Enables depth of field blur for objects further than `DofBlurFarDistance`. Strength of blur is controlled by `DofBlurAmount` and modulated by `DofBlurFarTransition`.

**Note:** Depth of field blur is only supported in the Forward+ and Mobile rendering methods, not Compatibility.

**Note:** Depth of field blur is not supported on viewports that have a transparent background (where [`Viewport.TransparentBg`](Viewport.md) is `true`).

### DofBlurFarTransition

```csharp
public float DofBlurFarTransition { get; set; }
```

引擎名：`dof_blur_far_transition`

When positive, distance over which (starting from `DofBlurFarDistance`) blur effect will scale from 0 to `DofBlurAmount`. When negative, uses physically-based scaling so depth of field effect will scale from 0 at `DofBlurFarDistance` and will increase in a physically accurate way as objects get further from the [`Camera3D`](Camera3D.md).

### DofBlurNearDistance

```csharp
public float DofBlurNearDistance { get; set; }
```

引擎名：`dof_blur_near_distance`

Objects closer from the [`Camera3D`](Camera3D.md) by this amount will be blurred by the depth of field effect. Measured in meters.

### DofBlurNearEnabled

```csharp
public bool DofBlurNearEnabled { get; set; }
```

引擎名：`dof_blur_near_enabled`

Enables depth of field blur for objects closer than `DofBlurNearDistance`. Strength of blur is controlled by `DofBlurAmount` and modulated by `DofBlurNearTransition`.

**Note:** Depth of field blur is only supported in the Forward+ and Mobile rendering methods, not Compatibility.

**Note:** Depth of field blur is not supported on viewports that have a transparent background (where [`Viewport.TransparentBg`](Viewport.md) is `true`).

### DofBlurNearTransition

```csharp
public float DofBlurNearTransition { get; set; }
```

引擎名：`dof_blur_near_transition`

When positive, distance over which blur effect will scale from 0 to `DofBlurAmount`, ending at `DofBlurNearDistance`. When negative, uses physically-based scaling so depth of field effect will scale from 0 at `DofBlurNearDistance` and will increase in a physically accurate way as objects get closer to the [`Camera3D`](Camera3D.md).

## 方法

### GetAutoExposureMaxSensitivity

```csharp
public float GetAutoExposureMaxSensitivity()
```

引擎名：`get_auto_exposure_max_sensitivity`

### GetAutoExposureMinSensitivity

```csharp
public float GetAutoExposureMinSensitivity()
```

引擎名：`get_auto_exposure_min_sensitivity`

### GetDofBlurAmount

```csharp
public float GetDofBlurAmount()
```

引擎名：`get_dof_blur_amount`

### GetDofBlurFarDistance

```csharp
public float GetDofBlurFarDistance()
```

引擎名：`get_dof_blur_far_distance`

### GetDofBlurFarTransition

```csharp
public float GetDofBlurFarTransition()
```

引擎名：`get_dof_blur_far_transition`

### GetDofBlurNearDistance

```csharp
public float GetDofBlurNearDistance()
```

引擎名：`get_dof_blur_near_distance`

### GetDofBlurNearTransition

```csharp
public float GetDofBlurNearTransition()
```

引擎名：`get_dof_blur_near_transition`

### IsDofBlurFarEnabled

```csharp
public bool IsDofBlurFarEnabled()
```

引擎名：`is_dof_blur_far_enabled`

### IsDofBlurNearEnabled

```csharp
public bool IsDofBlurNearEnabled()
```

引擎名：`is_dof_blur_near_enabled`

### SetAutoExposureMaxSensitivity

```csharp
public void SetAutoExposureMaxSensitivity(float maxSensitivity)
```

引擎名：`set_auto_exposure_max_sensitivity`

### SetAutoExposureMinSensitivity

```csharp
public void SetAutoExposureMinSensitivity(float minSensitivity)
```

引擎名：`set_auto_exposure_min_sensitivity`

### SetDofBlurAmount

```csharp
public void SetDofBlurAmount(float amount)
```

引擎名：`set_dof_blur_amount`

### SetDofBlurFarDistance

```csharp
public void SetDofBlurFarDistance(float distance)
```

引擎名：`set_dof_blur_far_distance`

### SetDofBlurFarEnabled

```csharp
public void SetDofBlurFarEnabled(bool enabled)
```

引擎名：`set_dof_blur_far_enabled`

### SetDofBlurFarTransition

```csharp
public void SetDofBlurFarTransition(float distance)
```

引擎名：`set_dof_blur_far_transition`

### SetDofBlurNearDistance

```csharp
public void SetDofBlurNearDistance(float distance)
```

引擎名：`set_dof_blur_near_distance`

### SetDofBlurNearEnabled

```csharp
public void SetDofBlurNearEnabled(bool enabled)
```

引擎名：`set_dof_blur_near_enabled`

### SetDofBlurNearTransition

```csharp
public void SetDofBlurNearTransition(float distance)
```

引擎名：`set_dof_blur_near_transition`
