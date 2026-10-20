<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BoxShape3D

命名空间 `Godot`；class；继承 [`Shape3D`](Shape3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 3D box shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape3D`](CollisionShape3D.md).

**Performance:** `BoxShape3D` is fast to check collisions against. It is faster than [`CapsuleShape3D`](CapsuleShape3D.md) and [`CylinderShape3D`](CylinderShape3D.md), but slower than [`SphereShape3D`](SphereShape3D.md).

## 构造函数

### BoxShape3D

```csharp
public BoxShape3D()
```

## 属性

### Size

```csharp
public Vector3 Size { get; set; }
```

引擎名：`size`

The box's width, height and depth.

## 方法

### GetSize

```csharp
public Vector3 GetSize()
```

引擎名：`get_size`

### SetSize

```csharp
public void SetSize(Vector3 size)
```

引擎名：`set_size`
