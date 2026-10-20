<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PolygonOccluder3D

命名空间 `Godot`；class；继承 [`Occluder3D`](Occluder3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`PolygonOccluder3D` stores a polygon shape that can be used by the engine's occlusion culling system. When an [`OccluderInstance3D`](OccluderInstance3D.md) with a `PolygonOccluder3D` is selected in the editor, an editor will appear at the top of the 3D viewport so you can add/remove points. All points must be placed on the same 2D plane, which means it is not possible to create arbitrary 3D shapes with a single `PolygonOccluder3D`. To use arbitrary 3D shapes as occluders, use [`ArrayOccluder3D`](ArrayOccluder3D.md) or [`OccluderInstance3D`](OccluderInstance3D.md)'s baking feature instead.

See [`OccluderInstance3D`](OccluderInstance3D.md)'s documentation for instructions on setting up occlusion culling.

## 构造函数

### PolygonOccluder3D

```csharp
public PolygonOccluder3D()
```

## 属性

### Polygon

```csharp
public Vector2[] Polygon { get; set; }
```

引擎名：`polygon`

The polygon to use for occlusion culling. The polygon can be convex or concave, but it should have as few points as possible to maximize performance.

The polygon must *not* have intersecting lines. Otherwise, triangulation will fail (with an error message printed).

## 方法

### GetPolygon

```csharp
public Vector2[] GetPolygon()
```

引擎名：`get_polygon`

### SetPolygon

```csharp
public void SetPolygon(ReadOnlySpan<Vector2> polygon)
```

引擎名：`set_polygon`

### SetPolygon

```csharp
public void SetPolygon(Vector2[] polygon)
```

引擎名：`set_polygon`
