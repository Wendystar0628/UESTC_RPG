<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CircleShape2D

命名空间 `Godot`；class；继承 [`Shape2D`](Shape2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D circle shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape2D`](CollisionShape2D.md).

**Performance:** `CircleShape2D` is fast to check collisions against. It is faster than [`RectangleShape2D`](RectangleShape2D.md) and [`CapsuleShape2D`](CapsuleShape2D.md).

## 构造函数

### CircleShape2D

```csharp
public CircleShape2D()
```

## 属性

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The circle's radius.

## 方法

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`
