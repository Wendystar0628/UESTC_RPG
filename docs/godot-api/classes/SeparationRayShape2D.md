<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SeparationRayShape2D

命名空间 `Godot`；class；继承 [`Shape2D`](Shape2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D ray shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape2D`](CollisionShape2D.md). When a `SeparationRayShape2D` collides with an object, it tries to separate itself from it by moving its endpoint to the collision point. For example, a `SeparationRayShape2D` next to a character can allow it to instantly move up when touching stairs.

## 构造函数

### SeparationRayShape2D

```csharp
public SeparationRayShape2D()
```

## 属性

### Length

```csharp
public float Length { get; set; }
```

引擎名：`length`

The ray's length.

### SlideOnSlope

```csharp
public bool SlideOnSlope { get; set; }
```

引擎名：`slide_on_slope`

If `false` (default), the shape always separates and returns a normal along its own direction.

If `true`, the shape can return the correct normal and separate in any direction, allowing sliding motion on slopes.

## 方法

### GetLength

```csharp
public float GetLength()
```

引擎名：`get_length`

### GetSlideOnSlope

```csharp
public bool GetSlideOnSlope()
```

引擎名：`get_slide_on_slope`

### SetLength

```csharp
public void SetLength(float length)
```

引擎名：`set_length`

### SetSlideOnSlope

```csharp
public void SetSlideOnSlope(bool active)
```

引擎名：`set_slide_on_slope`
