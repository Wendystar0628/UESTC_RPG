<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SphereShape3D

命名空间 `Godot`；class；继承 [`Shape3D`](Shape3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 3D sphere shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape3D`](CollisionShape3D.md).

**Performance:** `SphereShape3D` is fast to check collisions against. It is faster than [`BoxShape3D`](BoxShape3D.md), [`CapsuleShape3D`](CapsuleShape3D.md), and [`CylinderShape3D`](CylinderShape3D.md).

## 构造函数

### SphereShape3D

```csharp
public SphereShape3D()
```

## 属性

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The sphere's radius. The shape's diameter is double the radius.

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
