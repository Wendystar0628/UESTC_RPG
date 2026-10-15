<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MobileVRInterface

命名空间 `Godot`；class；继承 [`XRInterface`](XRInterface.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This is a generic mobile VR implementation where you need to provide details about the phone and HMD used. It does not rely on any existing framework. This is the most basic interface we have. For the best effect, you need a mobile phone with a gyroscope and accelerometer.

Note that even though there is no positional tracking, the camera will assume the headset is at a height of 1.85 meters. You can change this by setting `EyeHeight`.

You can initialize this interface as follows:

```csharp
var interface = XRServer.find_interface("Native mobile")
if interface and interface.initialize():
get_viewport().use_xr = true

```

**Note:** For Android, `ProjectSettings.input_devices/sensors/enable_accelerometer`, `ProjectSettings.input_devices/sensors/enable_gravity`, `ProjectSettings.input_devices/sensors/enable_gyroscope` and `ProjectSettings.input_devices/sensors/enable_magnetometer` must be enabled.

## 构造函数

### MobileVRInterface

```csharp
public MobileVRInterface()
```

## 属性

### DisplayToLens

```csharp
public double DisplayToLens { get; set; }
```

引擎名：`display_to_lens`

The distance between the display and the lenses inside of the device in centimeters.

### DisplayWidth

```csharp
public double DisplayWidth { get; set; }
```

引擎名：`display_width`

The width of the display in centimeters.

### EyeHeight

```csharp
public double EyeHeight { get; set; }
```

引擎名：`eye_height`

The height at which the camera is placed in relation to the ground (i.e. [`XROrigin3D`](XROrigin3D.md) node).

### Iod

```csharp
public double Iod { get; set; }
```

引擎名：`iod`

The interocular distance, also known as the interpupillary distance. The distance between the pupils of the left and right eye.

### K1

```csharp
public double K1 { get; set; }
```

引擎名：`k1`

The k1 lens factor is one of the two constants that define the strength of the lens used and directly influences the lens distortion effect.

### K2

```csharp
public double K2 { get; set; }
```

引擎名：`k2`

The k2 lens factor, see k1.

### OffsetRect

```csharp
public Rect2 OffsetRect { get; set; }
```

引擎名：`offset_rect`

Set the offset rect relative to the area being rendered. A length of 1 represents the whole rendering area on that axis.

### Oversample

```csharp
public double Oversample { get; set; }
```

引擎名：`oversample`

The oversample setting. Because of the lens distortion we have to render our buffers at a higher resolution then the screen can natively handle. A value between 1.5 and 2.0 often provides good results but at the cost of performance.

### VrsMinRadius

```csharp
public float VrsMinRadius { get; set; }
```

引擎名：`vrs_min_radius`

The minimum radius around the focal point where full quality is guaranteed if VRS is used as a percentage of screen size.

**Note:** Mobile and Forward+ renderers only. Requires [`Viewport.VrsMode`](Viewport.md) to be set to [`Viewport.VrsModeEnum.XR`](Viewport.md).

### VrsStrength

```csharp
public float VrsStrength { get; set; }
```

引擎名：`vrs_strength`

The strength used to calculate the VRS density map. The greater this value, the more noticeable VRS is. This improves performance at the cost of quality.

**Note:** Mobile and Forward+ renderers only. Requires [`Viewport.VrsMode`](Viewport.md) to be set to [`Viewport.VrsModeEnum.XR`](Viewport.md).

## 方法

### GetDisplayToLens

```csharp
public double GetDisplayToLens()
```

引擎名：`get_display_to_lens`

### GetDisplayWidth

```csharp
public double GetDisplayWidth()
```

引擎名：`get_display_width`

### GetEyeHeight

```csharp
public double GetEyeHeight()
```

引擎名：`get_eye_height`

### GetIod

```csharp
public double GetIod()
```

引擎名：`get_iod`

### GetK1

```csharp
public double GetK1()
```

引擎名：`get_k1`

### GetK2

```csharp
public double GetK2()
```

引擎名：`get_k2`

### GetOffsetRect

```csharp
public Rect2 GetOffsetRect()
```

引擎名：`get_offset_rect`

### GetOversample

```csharp
public double GetOversample()
```

引擎名：`get_oversample`

### GetVrsMinRadius

```csharp
public float GetVrsMinRadius()
```

引擎名：`get_vrs_min_radius`

### GetVrsStrength

```csharp
public float GetVrsStrength()
```

引擎名：`get_vrs_strength`

### SetDisplayToLens

```csharp
public void SetDisplayToLens(double displayToLens)
```

引擎名：`set_display_to_lens`

### SetDisplayWidth

```csharp
public void SetDisplayWidth(double displayWidth)
```

引擎名：`set_display_width`

### SetEyeHeight

```csharp
public void SetEyeHeight(double eyeHeight)
```

引擎名：`set_eye_height`

### SetIod

```csharp
public void SetIod(double iod)
```

引擎名：`set_iod`

### SetK1

```csharp
public void SetK1(double k)
```

引擎名：`set_k1`

### SetK2

```csharp
public void SetK2(double k)
```

引擎名：`set_k2`

### SetOffsetRect

```csharp
public void SetOffsetRect(Rect2 offsetRect)
```

引擎名：`set_offset_rect`

### SetOversample

```csharp
public void SetOversample(double oversample)
```

引擎名：`set_oversample`

### SetVrsMinRadius

```csharp
public void SetVrsMinRadius(float radius)
```

引擎名：`set_vrs_min_radius`

### SetVrsStrength

```csharp
public void SetVrsStrength(float strength)
```

引擎名：`set_vrs_strength`
