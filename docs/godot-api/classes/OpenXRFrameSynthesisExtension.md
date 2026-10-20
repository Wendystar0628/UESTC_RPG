<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRFrameSynthesisExtension

命名空间 `Godot`；class；继承 [`OpenXRExtensionWrapper`](OpenXRExtensionWrapper.md) → [`GodotObject`](GodotObject.md)

This class implements the [OpenXR Frame synthesis extension](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_EXT_frame_synthesis). When enabled in the project settings and supported by the XR runtime in use, frame synthesis uses advanced reprojection techniques to inject additional frames so that your XR experience hits the full frame rate of the device.

## 构造函数

### OpenXRFrameSynthesisExtension

```csharp
public OpenXRFrameSynthesisExtension()
```

## 属性

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

Enable frame synthesis. When `true` motion vector and depth data is provided to the XR runtime.

### RelaxFrameInterval

```csharp
public bool RelaxFrameInterval { get; set; }
```

引擎名：`relax_frame_interval`

If `true` this informs the XR runtime we will be providing frames at a greatly reduced rate. Enable this when you expect your application to run at low framerates and wish to inject multiple reprojected frames.

## 方法

### GetRelaxFrameInterval

```csharp
public bool GetRelaxFrameInterval()
```

引擎名：`get_relax_frame_interval`

### IsAvailable

```csharp
public bool IsAvailable()
```

引擎名：`is_available`

Returns `true` if frame synthesis is enabled in the project settings and the current XR runtime supports frame synthesis. The value returned will only be valid once OpenXR has been initialized.

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### SetEnabled

```csharp
public void SetEnabled(bool enable)
```

引擎名：`set_enabled`

### SetRelaxFrameInterval

```csharp
public void SetRelaxFrameInterval(bool relaxFrameInterval)
```

引擎名：`set_relax_frame_interval`

### SkipNextFrame

```csharp
public void SkipNextFrame()
```

引擎名：`skip_next_frame`

Queues the next frame to be skipped when supplying motion vector and depth data. Call this after teleporting your player or a similar action has moved the player to prevent incorrect reprojection results due to this movement.
