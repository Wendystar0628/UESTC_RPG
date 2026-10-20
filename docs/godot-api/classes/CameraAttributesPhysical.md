<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CameraAttributesPhysical

命名空间 `Godot`；class；继承 [`CameraAttributes`](CameraAttributes.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`CameraAttributesPhysical` is used to set rendering settings based on a physically-based camera's settings. It is responsible for exposure, auto-exposure, and depth of field.

When used in a [`WorldEnvironment`](WorldEnvironment.md) it provides default settings for exposure, auto-exposure, and depth of field that will be used by all cameras without their own [`CameraAttributes`](CameraAttributes.md), including the editor camera. When used in a [`Camera3D`](Camera3D.md) it will override any [`CameraAttributes`](CameraAttributes.md) set in the [`WorldEnvironment`](WorldEnvironment.md) and will override the [`Camera3D`](Camera3D.md)s [`Camera3D.Far`](Camera3D.md), [`Camera3D.Near`](Camera3D.md), [`Camera3D.Fov`](Camera3D.md), and [`Camera3D.KeepAspect`](Camera3D.md) properties. When used in [`VoxelGI`](VoxelGI.md) or [`LightmapGI`](LightmapGI.md), only the exposure settings will be used.

The default settings are intended for use in an outdoor environment, tips for settings for use in an indoor environment can be found in each setting's documentation.

**Note:** Depth of field blur is only supported in the Forward+ and Mobile rendering methods, not Compatibility.

**Note:** Auto-exposure is only supported in the Forward+ rendering method, not Mobile or Compatibility.

## 构造函数

### CameraAttributesPhysical

```csharp
public CameraAttributesPhysical()
```

## 属性

### AutoExposureMaxExposureValue

```csharp
public float AutoExposureMaxExposureValue { get; set; }
```

引擎名：`auto_exposure_max_exposure_value`

The maximum luminance (in EV100) used when calculating auto exposure. When calculating scene average luminance, color values will be clamped to at least this value. This limits the auto-exposure from exposing below a certain brightness, resulting in a cut off point where the scene will remain bright.

### AutoExposureMinExposureValue

```csharp
public float AutoExposureMinExposureValue { get; set; }
```

引擎名：`auto_exposure_min_exposure_value`

The minimum luminance (in EV100) used when calculating auto exposure. When calculating scene average luminance, color values will be clamped to at least this value. This limits the auto-exposure from exposing above a certain brightness, resulting in a cut off point where the scene will remain dark.

### ExposureAperture

```csharp
public float ExposureAperture { get; set; }
```

引擎名：`exposure_aperture`

Size of the aperture of the camera, measured in f-stops. An f-stop is a unitless ratio between the focal length of the camera and the diameter of the aperture. A high aperture setting will result in a smaller aperture which leads to a dimmer image and sharper focus. A low aperture results in a wide aperture which lets in more light resulting in a brighter, less-focused image. Default is appropriate for outdoors at daytime (i.e. for use with a default [`DirectionalLight3D`](DirectionalLight3D.md)), for indoor lighting, a value between 2 and 4 is more appropriate.

Only available when `ProjectSettings.rendering/lights_and_shadows/use_physical_light_units` is enabled.

### ExposureShutterSpeed

```csharp
public float ExposureShutterSpeed { get; set; }
```

引擎名：`exposure_shutter_speed`

Time for shutter to open and close, evaluated as `1 / shutter_speed` seconds. A higher value will allow less light (leading to a darker image), while a lower value will allow more light (leading to a brighter image).

Only available when `ProjectSettings.rendering/lights_and_shadows/use_physical_light_units` is enabled.

### FrustumFar

```csharp
public float FrustumFar { get; set; }
```

引擎名：`frustum_far`

Override value for [`Camera3D.Far`](Camera3D.md). Used internally when calculating depth of field. When attached to a [`Camera3D`](Camera3D.md) as its [`Camera3D.Attributes`](Camera3D.md), it will override the [`Camera3D.Far`](Camera3D.md) property.

### FrustumFocalLength

```csharp
public float FrustumFocalLength { get; set; }
```

引擎名：`frustum_focal_length`

Distance between camera lens and camera aperture, measured in millimeters. Controls field of view and depth of field. A larger focal length will result in a smaller field of view and a narrower depth of field meaning fewer objects will be in focus. A smaller focal length will result in a wider field of view and a larger depth of field meaning more objects will be in focus. When attached to a [`Camera3D`](Camera3D.md) as its [`Camera3D.Attributes`](Camera3D.md), it will override the [`Camera3D.Fov`](Camera3D.md) property and the [`Camera3D.KeepAspect`](Camera3D.md) property.

### FrustumFocusDistance

```csharp
public float FrustumFocusDistance { get; set; }
```

引擎名：`frustum_focus_distance`

Distance from camera of object that will be in focus, measured in meters. Internally this will be clamped to be at least 1 millimeter larger than `FrustumFocalLength`.

### FrustumNear

```csharp
public float FrustumNear { get; set; }
```

引擎名：`frustum_near`

Override value for [`Camera3D.Near`](Camera3D.md). Used internally when calculating depth of field. When attached to a [`Camera3D`](Camera3D.md) as its [`Camera3D.Attributes`](Camera3D.md), it will override the [`Camera3D.Near`](Camera3D.md) property.

## 方法

### GetAperture

```csharp
public float GetAperture()
```

引擎名：`get_aperture`

### GetAutoExposureMaxExposureValue

```csharp
public float GetAutoExposureMaxExposureValue()
```

引擎名：`get_auto_exposure_max_exposure_value`

### GetAutoExposureMinExposureValue

```csharp
public float GetAutoExposureMinExposureValue()
```

引擎名：`get_auto_exposure_min_exposure_value`

### GetFar

```csharp
public float GetFar()
```

引擎名：`get_far`

### GetFocalLength

```csharp
public float GetFocalLength()
```

引擎名：`get_focal_length`

### GetFocusDistance

```csharp
public float GetFocusDistance()
```

引擎名：`get_focus_distance`

### GetFov

```csharp
public float GetFov()
```

引擎名：`get_fov`

Returns the vertical field of view that corresponds to the `FrustumFocalLength`. This value is calculated internally whenever `FrustumFocalLength` is changed.

### GetNear

```csharp
public float GetNear()
```

引擎名：`get_near`

### GetShutterSpeed

```csharp
public float GetShutterSpeed()
```

引擎名：`get_shutter_speed`

### SetAperture

```csharp
public void SetAperture(float aperture)
```

引擎名：`set_aperture`

### SetAutoExposureMaxExposureValue

```csharp
public void SetAutoExposureMaxExposureValue(float exposureValueMax)
```

引擎名：`set_auto_exposure_max_exposure_value`

### SetAutoExposureMinExposureValue

```csharp
public void SetAutoExposureMinExposureValue(float exposureValueMin)
```

引擎名：`set_auto_exposure_min_exposure_value`

### SetFar

```csharp
public void SetFar(float far)
```

引擎名：`set_far`

### SetFocalLength

```csharp
public void SetFocalLength(float focalLength)
```

引擎名：`set_focal_length`

### SetFocusDistance

```csharp
public void SetFocusDistance(float focusDistance)
```

引擎名：`set_focus_distance`

### SetNear

```csharp
public void SetNear(float near)
```

引擎名：`set_near`

### SetShutterSpeed

```csharp
public void SetShutterSpeed(float shutterSpeed)
```

引擎名：`set_shutter_speed`
