<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CapsuleShape3D

命名空间 `Godot`；class；继承 [`Shape3D`](Shape3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 3D capsule shape, intended for use in physics. Usually used to provide a shape for a [`CollisionShape3D`](CollisionShape3D.md).

**Performance:** `CapsuleShape3D` is fast to check collisions against. It is faster than [`CylinderShape3D`](CylinderShape3D.md), but slower than [`SphereShape3D`](SphereShape3D.md) and [`BoxShape3D`](BoxShape3D.md).

## 构造函数

### CapsuleShape3D

```csharp
public CapsuleShape3D()
```

## 属性

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

The capsule's full height, including the hemispheres.

**Note:** The `Height` of a capsule must be at least twice its `Radius`. Otherwise, the capsule becomes a sphere. If the `Height` is less than twice the `Radius`, the properties adjust to a valid value.

### MidHeight

```csharp
public float MidHeight { get; set; }
```

引擎名：`mid_height`

The capsule's height, excluding the hemispheres. This is the height of the central cylindrical part in the middle of the capsule, and is the distance between the centers of the two hemispheres. This is a wrapper for `Height`.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The capsule's radius.

**Note:** The `Radius` of a capsule cannot be greater than half of its `Height`. Otherwise, the capsule becomes a sphere. If the `Radius` is greater than half of the `Height`, the properties adjust to a valid value.

## 方法

### GetHeight

```csharp
public float GetHeight()
```

引擎名：`get_height`

### GetMidHeight

```csharp
public float GetMidHeight()
```

引擎名：`get_mid_height`

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

### SetMidHeight

```csharp
public void SetMidHeight(float midHeight)
```

引擎名：`set_mid_height`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`
