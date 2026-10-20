<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CylinderShape3D

命名空间 `Godot`；class；继承 [`Shape3D`](Shape3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 3D cylinder shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape3D`](CollisionShape3D.md).

**Note:** There are several known bugs with cylinder collision shapes. Using [`CapsuleShape3D`](CapsuleShape3D.md) or [`BoxShape3D`](BoxShape3D.md) instead is recommended.

**Performance:** `CylinderShape3D` is fast to check collisions against, but it is slower than [`CapsuleShape3D`](CapsuleShape3D.md), [`BoxShape3D`](BoxShape3D.md), and [`SphereShape3D`](SphereShape3D.md).

## 构造函数

### CylinderShape3D

```csharp
public CylinderShape3D()
```

## 属性

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

The cylinder's height.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The cylinder's radius.

## 方法

### GetHeight

```csharp
public float GetHeight()
```

引擎名：`get_height`

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### SetHeight

```csharp
public void SetHeight(float height)
```

引擎名：`set_height`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`
