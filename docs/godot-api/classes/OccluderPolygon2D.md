<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OccluderPolygon2D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Editor facility that helps you draw a 2D polygon used as resource for [`LightOccluder2D`](LightOccluder2D.md).

## 构造函数

### OccluderPolygon2D

```csharp
public OccluderPolygon2D()
```

## 属性

### Closed

```csharp
public bool Closed { get; set; }
```

引擎名：`closed`

If `true`, closes the polygon. A closed OccluderPolygon2D occludes the light coming from any direction. An opened OccluderPolygon2D occludes the light only at its outline's direction.

### CullMode

```csharp
public OccluderPolygon2D.CullModeEnum CullMode { get; set; }
```

引擎名：`cull_mode`

The culling mode to use.

### Polygon

```csharp
public Vector2[] Polygon { get; set; }
```

引擎名：`polygon`

A [`Vector2`](Vector2.md) array with the index for polygon's vertices positions.

## 方法

### GetCullMode

```csharp
public OccluderPolygon2D.CullModeEnum GetCullMode()
```

引擎名：`get_cull_mode`

### GetPolygon

```csharp
public Vector2[] GetPolygon()
```

引擎名：`get_polygon`

### IsClosed

```csharp
public bool IsClosed()
```

引擎名：`is_closed`

### SetClosed

```csharp
public void SetClosed(bool closed)
```

引擎名：`set_closed`

### SetCullMode

```csharp
public void SetCullMode(OccluderPolygon2D.CullModeEnum cullMode)
```

引擎名：`set_cull_mode`

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

## CullModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Culling is disabled.
- `Clockwise = 1`：Culling is performed in the clockwise direction.
- `CounterClockwise = 2`：Culling is performed in the counterclockwise direction.
