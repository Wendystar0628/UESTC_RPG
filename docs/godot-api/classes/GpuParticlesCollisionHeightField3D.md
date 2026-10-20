<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GpuParticlesCollisionHeightField3D

命名空间 `Godot`；class；继承 [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A real-time heightmap-shaped 3D particle collision shape affecting [`GpuParticles3D`](GpuParticles3D.md) nodes.

Heightmap shapes allow for efficiently representing collisions for convex and concave objects with a single "floor" (such as terrain). This is less flexible than [`GpuParticlesCollisionSdf3D`](GpuParticlesCollisionSdf3D.md), but it doesn't require a baking step.

`GpuParticlesCollisionHeightField3D` can also be regenerated in real-time when it is moved, when the camera moves, or even continuously. This makes `GpuParticlesCollisionHeightField3D` a good choice for weather effects such as rain and snow and games with highly dynamic geometry. However, this class is limited since heightmaps cannot represent overhangs (e.g. indoors or caves).

**Note:** [`ParticleProcessMaterial.CollisionMode`](ParticleProcessMaterial.md) must be `true` on the [`GpuParticles3D`](GpuParticles3D.md)'s process material for collision to work.

**Note:** Particle collision only affects [`GpuParticles3D`](GpuParticles3D.md), not [`CpuParticles3D`](CpuParticles3D.md).

## 构造函数

### GpuParticlesCollisionHeightField3D

```csharp
public GpuParticlesCollisionHeightField3D()
```

## 属性

### FollowCameraEnabled

```csharp
public bool FollowCameraEnabled { get; set; }
```

If `true`, the `GpuParticlesCollisionHeightField3D` will follow the current camera in global space. The `GpuParticlesCollisionHeightField3D` does not need to be a child of the [`Camera3D`](Camera3D.md) node for this to work.

Following the camera has a performance cost, as it will force the heightmap to update whenever the camera moves. Consider lowering `Resolution` to improve performance if `FollowCameraEnabled` is `true`.

### HeightfieldMask

```csharp
public uint HeightfieldMask { get; set; }
```

The visual layers to account for when updating the heightmap. Only [`MeshInstance3D`](MeshInstance3D.md)s whose [`VisualInstance3D.Layers`](VisualInstance3D.md) match with this `HeightfieldMask` will be included in the heightmap collision update. By default, all 20 user-visible layers are taken into account for updating the heightmap collision.

**Note:** Since the `HeightfieldMask` allows for 32 layers to be stored in total, there are an additional 12 layers that are only used internally by the engine and aren't exposed in the editor. Setting `HeightfieldMask` using a script allows you to toggle those reserved layers, which can be useful for editor plugins.

To adjust `HeightfieldMask` more easily using a script, use `GetHeightfieldMaskValue()` and `SetHeightfieldMaskValue()`.

### Resolution

```csharp
public GpuParticlesCollisionHeightField3D.ResolutionEnum Resolution { get; set; }
```

Higher resolutions can represent small details more accurately in large scenes, at the cost of lower performance. If `UpdateMode` is [`GpuParticlesCollisionHeightField3D.UpdateModeEnum.Always`](GpuParticlesCollisionHeightField3D.md), consider using the lowest resolution possible.

### Size

```csharp
public Vector3 Size { get; set; }
```

The collision heightmap's size in 3D units. To improve heightmap quality, `Size` should be set as small as possible while covering the parts of the scene you need.

### UpdateMode

```csharp
public GpuParticlesCollisionHeightField3D.UpdateModeEnum UpdateMode { get; set; }
```

The update policy to use for the generated heightmap.

## 方法

### GetHeightfieldMask

```csharp
public uint GetHeightfieldMask()
```

### GetHeightfieldMaskValue

```csharp
public bool GetHeightfieldMaskValue(int layerNumber)
```

Returns `true` if the specified layer of the `HeightfieldMask` is enabled, given a `layerNumber` between `1` and `20`, inclusive.

### GetResolution

```csharp
public GpuParticlesCollisionHeightField3D.ResolutionEnum GetResolution()
```

### GetSize

```csharp
public Vector3 GetSize()
```

### GetUpdateMode

```csharp
public GpuParticlesCollisionHeightField3D.UpdateModeEnum GetUpdateMode()
```

### IsFollowCameraEnabled

```csharp
public bool IsFollowCameraEnabled()
```

### SetFollowCameraEnabled

```csharp
public void SetFollowCameraEnabled(bool enabled)
```

### SetHeightfieldMask

```csharp
public void SetHeightfieldMask(uint heightfieldMask)
```

### SetHeightfieldMaskValue

```csharp
public void SetHeightfieldMaskValue(int layerNumber, bool value)
```

Based on `value`, enables or disables the specified layer in the `HeightfieldMask`, given a `layerNumber` between `1` and `20`, inclusive.

### SetResolution

```csharp
public void SetResolution(GpuParticlesCollisionHeightField3D.ResolutionEnum resolution)
```

### SetSize

```csharp
public void SetSize(Vector3 size)
```

### SetUpdateMode

```csharp
public void SetUpdateMode(GpuParticlesCollisionHeightField3D.UpdateModeEnum updateMode)
```

## ResolutionEnum

命名空间 `Godot`；enum

### 枚举值

- `Resolution256 = 0`：Generate a 256×256 heightmap.
- `Resolution512 = 1`：Generate a 512×512 heightmap.
- `Resolution1024 = 2`：Generate a 1024×1024 heightmap.
- `Resolution2048 = 3`：Generate a 2048×2048 heightmap.
- `Resolution4096 = 4`：Generate a 4096×4096 heightmap.
- `Resolution8192 = 5`：Generate a 8192×8192 heightmap.
- `Max = 6`：Represents the size of the `GpuParticlesCollisionHeightField3D.ResolutionEnum` enum.

## UpdateModeEnum

命名空间 `Godot`；enum

### 枚举值

- `WhenMoved = 0`：Only update the heightmap when the [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md) node is moved, or when the camera moves if [`GpuParticlesCollisionHeightField3D.FollowCameraEnabled`](GpuPa…
- `Always = 1`：Update the heightmap every frame.
