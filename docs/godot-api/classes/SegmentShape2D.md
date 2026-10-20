<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SegmentShape2D

命名空间 `Godot`；class；继承 [`Shape2D`](Shape2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D line segment shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape2D`](CollisionShape2D.md).

## 构造函数

### SegmentShape2D

```csharp
public SegmentShape2D()
```

## 属性

### A

```csharp
public Vector2 A { get; set; }
```

引擎名：`a`

The segment's first point position.

### B

```csharp
public Vector2 B { get; set; }
```

引擎名：`b`

The segment's second point position.

## 方法

### GetA

```csharp
public Vector2 GetA()
```

引擎名：`get_a`

### GetB

```csharp
public Vector2 GetB()
```

引擎名：`get_b`

### SetA

```csharp
public void SetA(Vector2 a)
```

引擎名：`set_a`

### SetB

```csharp
public void SetB(Vector2 b)
```

引擎名：`set_b`
