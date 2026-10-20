<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WorldBoundaryShape3D

命名空间 `Godot`；class；继承 [`Shape3D`](Shape3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 3D world boundary shape, intended for use in physics. `WorldBoundaryShape3D` works like an infinite plane that forces all physics bodies to stay above it. The `Plane`'s normal determines which direction is considered as "above" and in the editor, the line over the plane represents this direction. It can for example be used for endless flat floors.

**Note:** When the physics engine is set to **Jolt Physics** in the project settings (`ProjectSettings.physics/3d/physics_engine`), `WorldBoundaryShape3D` has a finite size (centered at the shape's origin). It can be adjusted by changing `ProjectSettings.physics/jolt_physics_3d/limits/world_boundary_shape_size`.

## 构造函数

### WorldBoundaryShape3D

```csharp
public WorldBoundaryShape3D()
```

## 属性

### Plane

```csharp
public Plane Plane { get; set; }
```

引擎名：`plane`

The [`Plane`](Plane.md) used by the `WorldBoundaryShape3D` for collision.

## 方法

### GetPlane

```csharp
public Plane GetPlane()
```

引擎名：`get_plane`

### SetPlane

```csharp
public void SetPlane(Plane plane)
```

引擎名：`set_plane`
