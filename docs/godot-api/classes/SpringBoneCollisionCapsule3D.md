<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpringBoneCollisionCapsule3D

命名空间 `Godot`；class；继承 [`SpringBoneCollision3D`](SpringBoneCollision3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A capsule shape collision that interacts with [`SpringBoneSimulator3D`](SpringBoneSimulator3D.md).

## 构造函数

### SpringBoneCollisionCapsule3D

```csharp
public SpringBoneCollisionCapsule3D()
```

## 属性

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

The capsule's full height, including the hemispheres.

**Note:** The `Height` of a capsule must be at least twice its `Radius`. Otherwise, the capsule becomes a sphere. If the `Height` is less than twice the `Radius`, the properties adjust to a valid value.

### Inside

```csharp
public bool Inside { get; set; }
```

引擎名：`inside`

If `true`, the collision acts to trap the joint within the collision.

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

### IsInside

```csharp
public bool IsInside()
```

引擎名：`is_inside`

### SetHeight

```csharp
public void SetHeight(float height)
```

引擎名：`set_height`

### SetInside

```csharp
public void SetInside(bool enabled)
```

引擎名：`set_inside`

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
