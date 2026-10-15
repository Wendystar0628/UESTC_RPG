<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CameraAttributes

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Controls camera-specific attributes such as depth of field and exposure override.

When used in a [`WorldEnvironment`](WorldEnvironment.md) it provides default settings for exposure, auto-exposure, and depth of field that will be used by all cameras without their own `CameraAttributes`, including the editor camera. When used in a [`Camera3D`](Camera3D.md) it will override any `CameraAttributes` set in the [`WorldEnvironment`](WorldEnvironment.md). When used in [`VoxelGI`](VoxelGI.md) or [`LightmapGI`](LightmapGI.md), only the exposure settings will be used.

See also [`Environment`](Environment.md) for general 3D environment settings.

This is a pure virtual class that is inherited by [`CameraAttributesPhysical`](CameraAttributesPhysical.md) and [`CameraAttributesPractical`](CameraAttributesPractical.md).

## 构造函数

### CameraAttributes

```csharp
public CameraAttributes()
```

## 属性

### AutoExposureEnabled

```csharp
public bool AutoExposureEnabled { get; set; }
```

引擎名：`auto_exposure_enabled`

If `true`, enables the tonemapping auto exposure mode of the scene renderer. If `true`, the renderer will automatically determine the exposure setting to adapt to the scene's illumination and the observed light.

**Note:** Auto-exposure is only supported in the Forward+ rendering method, not Mobile or Compatibility.

### AutoExposureScale

```csharp
public float AutoExposureScale { get; set; }
```

引擎名：`auto_exposure_scale`

The scale of the auto exposure effect. Affects the intensity of auto exposure.

### AutoExposureSpeed

```csharp
public float AutoExposureSpeed { get; set; }
```

引擎名：`auto_exposure_speed`

The speed of the auto exposure effect. Affects the time needed for the camera to perform auto exposure.

### ExposureMultiplier

```csharp
public float ExposureMultiplier { get; set; }
```

引擎名：`exposure_multiplier`

Multiplier for the exposure amount. A higher value results in a brighter image.

### ExposureSensitivity

```csharp
public float ExposureSensitivity { get; set; }
```

引擎名：`exposure_sensitivity`

Sensitivity of camera sensors, measured in ISO. A higher sensitivity results in a brighter image.

If `AutoExposureEnabled` is `true`, this can be used as a method of exposure compensation, doubling the value will increase the exposure value (measured in EV100) by 1 stop.

**Note:** Only available when `ProjectSettings.rendering/lights_and_shadows/use_physical_light_units` is enabled.

## 方法

### GetAutoExposureScale

```csharp
public float GetAutoExposureScale()
```

引擎名：`get_auto_exposure_scale`

### GetAutoExposureSpeed

```csharp
public float GetAutoExposureSpeed()
```

引擎名：`get_auto_exposure_speed`

### GetExposureMultiplier

```csharp
public float GetExposureMultiplier()
```

引擎名：`get_exposure_multiplier`

### GetExposureSensitivity

```csharp
public float GetExposureSensitivity()
```

引擎名：`get_exposure_sensitivity`

### IsAutoExposureEnabled

```csharp
public bool IsAutoExposureEnabled()
```

引擎名：`is_auto_exposure_enabled`

### SetAutoExposureEnabled

```csharp
public void SetAutoExposureEnabled(bool enabled)
```

引擎名：`set_auto_exposure_enabled`

### SetAutoExposureScale

```csharp
public void SetAutoExposureScale(float exposureGrey)
```

引擎名：`set_auto_exposure_scale`

### SetAutoExposureSpeed

```csharp
public void SetAutoExposureSpeed(float exposureSpeed)
```

引擎名：`set_auto_exposure_speed`

### SetExposureMultiplier

```csharp
public void SetExposureMultiplier(float multiplier)
```

引擎名：`set_exposure_multiplier`

### SetExposureSensitivity

```csharp
public void SetExposureSensitivity(float sensitivity)
```

引擎名：`set_exposure_sensitivity`
