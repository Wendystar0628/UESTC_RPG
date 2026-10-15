<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRCompositionLayerEquirect

命名空间 `Godot`；class；继承 [`OpenXRCompositionLayer`](OpenXRCompositionLayer.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An OpenXR composition layer that allows rendering a [`SubViewport`](SubViewport.md) on an internal slice of a sphere.

## 构造函数

### OpenXRCompositionLayerEquirect

```csharp
public OpenXRCompositionLayerEquirect()
```

## 属性

### CentralHorizontalAngle

```csharp
public float CentralHorizontalAngle { get; set; }
```

引擎名：`central_horizontal_angle`

The central horizontal angle of the sphere. Used to set the width.

### FallbackSegments

```csharp
public uint FallbackSegments { get; set; }
```

引擎名：`fallback_segments`

The number of segments to use in the fallback mesh.

### LowerVerticalAngle

```csharp
public float LowerVerticalAngle { get; set; }
```

引擎名：`lower_vertical_angle`

The lower vertical angle of the sphere. Used (together with `UpperVerticalAngle`) to set the height.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The radius of the sphere.

### UpperVerticalAngle

```csharp
public float UpperVerticalAngle { get; set; }
```

引擎名：`upper_vertical_angle`

The upper vertical angle of the sphere. Used (together with `LowerVerticalAngle`) to set the height.

## 方法

### GetCentralHorizontalAngle

```csharp
public float GetCentralHorizontalAngle()
```

引擎名：`get_central_horizontal_angle`

### GetFallbackSegments

```csharp
public uint GetFallbackSegments()
```

引擎名：`get_fallback_segments`

### GetLowerVerticalAngle

```csharp
public float GetLowerVerticalAngle()
```

引擎名：`get_lower_vertical_angle`

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### GetUpperVerticalAngle

```csharp
public float GetUpperVerticalAngle()
```

引擎名：`get_upper_vertical_angle`

### SetCentralHorizontalAngle

```csharp
public void SetCentralHorizontalAngle(float angle)
```

引擎名：`set_central_horizontal_angle`

### SetFallbackSegments

```csharp
public void SetFallbackSegments(uint segments)
```

引擎名：`set_fallback_segments`

### SetLowerVerticalAngle

```csharp
public void SetLowerVerticalAngle(float angle)
```

引擎名：`set_lower_vertical_angle`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`

### SetUpperVerticalAngle

```csharp
public void SetUpperVerticalAngle(float angle)
```

引擎名：`set_upper_vertical_angle`
