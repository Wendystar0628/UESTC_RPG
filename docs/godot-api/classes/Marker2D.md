<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Marker2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Generic 2D position hint for editing. It's just like a plain [`Node2D`](Node2D.md), but it displays as a cross in the 2D editor at all times. You can set the cross' visual size by using the gizmo in the 2D editor while the node is selected.

## 构造函数

### Marker2D

```csharp
public Marker2D()
```

## 属性

### GizmoExtents

```csharp
public float GizmoExtents { get; set; }
```

引擎名：`gizmo_extents`

Size of the gizmo cross that appears in the editor.

## 方法

### GetGizmoExtents

```csharp
public float GetGizmoExtents()
```

引擎名：`get_gizmo_extents`

### SetGizmoExtents

```csharp
public void SetGizmoExtents(float extents)
```

引擎名：`set_gizmo_extents`
