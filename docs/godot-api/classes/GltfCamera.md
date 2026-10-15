<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfCamera

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Represents a camera as defined by the base glTF spec.

## 构造函数

### GltfCamera

```csharp
public GltfCamera()
```

## 属性

### DepthFar

```csharp
public float DepthFar { get; set; }
```

The distance to the far culling boundary for this camera relative to its local Z axis, in meters. This maps to glTF's `zfar` property.

### DepthNear

```csharp
public float DepthNear { get; set; }
```

The distance to the near culling boundary for this camera relative to its local Z axis, in meters. This maps to glTF's `znear` property.

### Fov

```csharp
public float Fov { get; set; }
```

The FOV of the camera. This class and glTF define the camera FOV in radians, while Godot uses degrees. This maps to glTF's `yfov` property. This value is only used for perspective cameras, when `Perspective` is `true`.

### Perspective

```csharp
public bool Perspective { get; set; }
```

If `true`, the camera is in perspective mode. Otherwise, the camera is in orthographic/orthogonal mode. This maps to glTF's camera `type` property. See [`Camera3D.Projection`](Camera3D.md) and the glTF spec for more information.

### SizeMag

```csharp
public float SizeMag { get; set; }
```

The size of the camera. This class and glTF define the camera size magnitude as a radius in meters, while Godot defines it as a diameter in meters. This maps to glTF's `ymag` property. This value is only used for orthographic/orthogonal cameras, when `Perspective` is `false`.

## 方法

### FromDictionary

```csharp
public static GltfCamera FromDictionary(Dictionary dictionary)
```

Creates a new GLTFCamera instance by parsing the given [`Collections.Dictionary`](Collections.Dictionary.md).

### FromNode

```csharp
public static GltfCamera FromNode(Camera3D cameraNode)
```

Create a new GLTFCamera instance from the given Godot [`Camera3D`](Camera3D.md) node.

### GetDepthFar

```csharp
public float GetDepthFar()
```

### GetDepthNear

```csharp
public float GetDepthNear()
```

### GetFov

```csharp
public float GetFov()
```

### GetPerspective

```csharp
public bool GetPerspective()
```

### GetSizeMag

```csharp
public float GetSizeMag()
```

### SetDepthFar

```csharp
public void SetDepthFar(float zdepthFar)
```

### SetDepthNear

```csharp
public void SetDepthNear(float zdepthNear)
```

### SetFov

```csharp
public void SetFov(float fov)
```

### SetPerspective

```csharp
public void SetPerspective(bool perspective)
```

### SetSizeMag

```csharp
public void SetSizeMag(float sizeMag)
```

### ToDictionary

```csharp
public Dictionary ToDictionary()
```

Serializes this GLTFCamera instance into a [`Collections.Dictionary`](Collections.Dictionary.md).

### ToNode

```csharp
public Camera3D ToNode()
```

Converts this GLTFCamera instance into a Godot [`Camera3D`](Camera3D.md) node.
