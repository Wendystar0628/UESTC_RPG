<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RectangleShape2D

命名空间 `Godot`；class；继承 [`Shape2D`](Shape2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D rectangle shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape2D`](CollisionShape2D.md).

**Performance:** `RectangleShape2D` is fast to check collisions against. It is faster than [`CapsuleShape2D`](CapsuleShape2D.md), but slower than [`CircleShape2D`](CircleShape2D.md).

## 构造函数

### RectangleShape2D

```csharp
public RectangleShape2D()
```

## 属性

### Size

```csharp
public Vector2 Size { get; set; }
```

引擎名：`size`

The rectangle's width and height.

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
