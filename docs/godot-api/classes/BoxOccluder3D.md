<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BoxOccluder3D

命名空间 `Godot`；class；继承 [`Occluder3D`](Occluder3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`BoxOccluder3D` stores a cuboid shape that can be used by the engine's occlusion culling system.

See [`OccluderInstance3D`](OccluderInstance3D.md)'s documentation for instructions on setting up occlusion culling.

## 构造函数

### BoxOccluder3D

```csharp
public BoxOccluder3D()
```

## 属性

### Size

```csharp
public Vector3 Size { get; set; }
```

引擎名：`size`

The box's size in 3D units.

## 方法

### GetSize

```csharp
public Vector3 GetSize()
```

引擎名：`get_size`

### SetSize

```csharp
public void SetSize(Vector3 size)
```

引擎名：`set_size`
