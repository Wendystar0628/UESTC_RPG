<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpringBoneCollisionSphere3D

命名空间 `Godot`；class；继承 [`SpringBoneCollision3D`](SpringBoneCollision3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A sphere shape collision that interacts with [`SpringBoneSimulator3D`](SpringBoneSimulator3D.md).

## 构造函数

### SpringBoneCollisionSphere3D

```csharp
public SpringBoneCollisionSphere3D()
```

## 属性

### Inside

```csharp
public bool Inside { get; set; }
```

引擎名：`inside`

If `true`, the collision acts to trap the joint within the collision.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The sphere's radius.

## 方法

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

### SetInside

```csharp
public void SetInside(bool enabled)
```

引擎名：`set_inside`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`
