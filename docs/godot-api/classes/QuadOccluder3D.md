<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# QuadOccluder3D

命名空间 `Godot`；class；继承 [`Occluder3D`](Occluder3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`QuadOccluder3D` stores a flat plane shape that can be used by the engine's occlusion culling system. See also [`PolygonOccluder3D`](PolygonOccluder3D.md) if you need to customize the quad's shape.

See [`OccluderInstance3D`](OccluderInstance3D.md)'s documentation for instructions on setting up occlusion culling.

## 构造函数

### QuadOccluder3D

```csharp
public QuadOccluder3D()
```

## 属性

### Size

```csharp
public Vector2 Size { get; set; }
```

引擎名：`size`

The quad's size in 3D units.

## 方法

### GetSize

```csharp
public Vector2 GetSize()
```

引擎名：`get_size`

### SetSize

```csharp
public void SetSize(Vector2 size)
```

引擎名：`set_size`
