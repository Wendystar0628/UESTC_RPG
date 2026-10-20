<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRCompositionLayerCylinder

命名空间 `Godot`；class；继承 [`OpenXRCompositionLayer`](OpenXRCompositionLayer.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An OpenXR composition layer that allows rendering a [`SubViewport`](SubViewport.md) on an internal slice of a cylinder.

## 构造函数

### OpenXRCompositionLayerCylinder

```csharp
public OpenXRCompositionLayerCylinder()
```

## 属性

### AspectRatio

```csharp
public float AspectRatio { get; set; }
```

引擎名：`aspect_ratio`

The aspect ratio of the slice. Used to set the height relative to the width.

### CentralAngle

```csharp
public float CentralAngle { get; set; }
```

引擎名：`central_angle`

The central angle of the cylinder. Used to set the width.

### FallbackSegments

```csharp
public uint FallbackSegments { get; set; }
```

引擎名：`fallback_segments`

The number of segments to use in the fallback mesh.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The radius of the cylinder.

## 方法

### GetAspectRatio

```csharp
public float GetAspectRatio()
```

引擎名：`get_aspect_ratio`

### GetCentralAngle

```csharp
public float GetCentralAngle()
```

引擎名：`get_central_angle`

### GetFallbackSegments

```csharp
public uint GetFallbackSegments()
```

引擎名：`get_fallback_segments`

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### SetAspectRatio

```csharp
public void SetAspectRatio(float aspectRatio)
```

引擎名：`set_aspect_ratio`

### SetCentralAngle

```csharp
public void SetCentralAngle(float angle)
```

引擎名：`set_central_angle`

### SetFallbackSegments

```csharp
public void SetFallbackSegments(uint segments)
```

引擎名：`set_fallback_segments`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`
