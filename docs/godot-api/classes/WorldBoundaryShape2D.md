<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WorldBoundaryShape2D

命名空间 `Godot`；class；继承 [`Shape2D`](Shape2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D world boundary shape, intended for use in physics. `WorldBoundaryShape2D` works like an infinite straight line that forces all physics bodies to stay above it. The line's normal determines which direction is considered as "above" and in the editor, the smaller line over it represents this direction. It can for example be used for endless flat floors.

## 构造函数

### WorldBoundaryShape2D

```csharp
public WorldBoundaryShape2D()
```

## 属性

### Distance

```csharp
public float Distance { get; set; }
```

引擎名：`distance`

The distance from the origin to the line, expressed in terms of `Normal` (according to its direction and magnitude). Actual absolute distance from the origin to the line can be calculated as `abs(distance) / normal.length()`.

In the scalar equation of the line `ax + by = d`, this is `d`, while the `(a, b)` coordinates are represented by the `Normal` property.

### Normal

```csharp
public Vector2 Normal { get; set; }
```

引擎名：`normal`

The line's normal, typically a unit vector. Its direction indicates the non-colliding half-plane. Can be of any length but zero. Defaults to `Vector2.Up`.

## 方法

### GetDistance

```csharp
public float GetDistance()
```

引擎名：`get_distance`

### GetNormal

```csharp
public Vector2 GetNormal()
```

引擎名：`get_normal`

### SetDistance

```csharp
public void SetDistance(float distance)
```

引擎名：`set_distance`

### SetNormal

```csharp
public void SetNormal(Vector2 normal)
```

引擎名：`set_normal`
