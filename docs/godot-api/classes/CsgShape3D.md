<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CsgShape3D

命名空间 `Godot`；class；继承 [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This is the CSG base class that provides CSG operation support to the various CSG nodes in Godot.

**Performance:** CSG nodes are only intended for prototyping as they have a significant CPU performance cost. Consider baking final CSG operation results into static geometry that replaces the CSG nodes.

Individual CSG root node results can be baked to nodes with static resources with the editor menu that appears when a CSG root node is selected.

Individual CSG root nodes can also be baked to static resources with scripts by calling `BakeStaticMesh()` for the visual mesh or `BakeCollisionShape()` for the physics collision.

Entire scenes of CSG nodes can be baked to static geometry and exported with the editor glTF scene exporter: **Scene > Export As... > glTF 2.0 Scene...**

## 属性

### Autosmooth

```csharp
public bool Autosmooth { get; set; }
```

Enables automatic smoothing. This overrides any smoothing on the CSG node and instead uses `SmoothingAngle` to calculate normals based on the angle between faces.

Children of a [`CsgCombiner3D`](CsgCombiner3D.md) node will be treated as a single mesh.

### CalculateTangents

```csharp
public bool CalculateTangents { get; set; }
```

Calculate tangents for the CSG shape which allows the use of normal and height maps. This is only applied on the root shape, this setting is ignored on any child. Setting this to `false` can speed up shape generation slightly.

### CollisionLayer

```csharp
public uint CollisionLayer { get; set; }
```

The physics layers this area is in.

Collidable objects can exist in any of 32 different layers. These layers work like a tagging system, and are not visual. A collidable can use these layers to select with which objects it can collide, using the collision_mask property.

A contact is detected if object A is in any of the layers that object B scans, or object B is in any layer scanned by object A. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

The physics layers this CSG shape scans for collisions. Only effective if `UseCollision` is `true`. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionPriority

```csharp
public float CollisionPriority { get; set; }
```

The priority used to solve colliding when occurring penetration. Only effective if `UseCollision` is `true`. The higher the priority is, the lower the penetration into the object will be. This can for example be used to prevent the player from breaking through the boundaries of a level.

### Operation

```csharp
public CsgShape3D.OperationEnum Operation { get; set; }
```

The operation that is performed on this shape. This is ignored for the first CSG child node as the operation is between this node and the previous child of this nodes parent.

### SmoothingAngle

```csharp
public float SmoothingAngle { get; set; }
```

When autosmooth is enabled, faces with an angle between them greater than this will be smoothed, while faces with a smaller angle will remain sharp.

Note: An angle lower than 0.1 will cause all smoothing to be disabled, this can be used to increase performance.

### Snap

```csharp
public float Snap { get; set; }
```

This property does nothing.

### UseCollision

```csharp
public bool UseCollision { get; set; }
```

Adds a collision shape to the physics engine for our CSG shape. This will always act like a static body. Note that the collision shape is still active even if the CSG shape itself is hidden. See also `CollisionMask` and `CollisionPriority`.

## 方法

### BakeCollisionShape

```csharp
public ConcavePolygonShape3D BakeCollisionShape()
```

Returns a baked physics [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md) of this node's CSG operation result. Returns an empty shape if the node is not a CSG root node or has no valid geometry.

**Performance:** If the CSG operation results in a very detailed geometry with many faces physics performance will be very slow. Concave shapes should in general only be used for static level geometry and not with dynamic objects that are moving.

**Note:** CSG mesh data updates are deferred, which means they are updated with a delay of one rendered frame. To avoid getting an empty shape or outdated mesh data, make sure to call `await get_tree().process_frame` before using `BakeCollisionShape()` in [`Node.Ready`](Node.md) or after changing properties on the `CsgShape3D`.

### BakeStaticMesh

```csharp
public ArrayMesh BakeStaticMesh()
```

Returns a baked static [`ArrayMesh`](ArrayMesh.md) of this node's CSG operation result. Materials from involved CSG nodes are added as extra mesh surfaces. Returns an empty mesh if the node is not a CSG root node or has no valid geometry.

**Note:** CSG mesh data updates are deferred, which means they are updated with a delay of one rendered frame. To avoid getting an empty mesh or outdated mesh data, make sure to call `await get_tree().process_frame` before using `BakeStaticMesh()` in [`Node.Ready`](Node.md) or after changing properties on the `CsgShape3D`.

### GetCollisionLayer

```csharp
public uint GetCollisionLayer()
```

### GetCollisionLayerValue

```csharp
public bool GetCollisionLayerValue(int layerNumber)
```

Returns whether or not the specified layer of the `CollisionLayer` is enabled, given a `layerNumber` between 1 and 32.

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

### GetCollisionMaskValue

```csharp
public bool GetCollisionMaskValue(int layerNumber)
```

Returns whether or not the specified layer of the `CollisionMask` is enabled, given a `layerNumber` between 1 and 32.

### GetCollisionPriority

```csharp
public float GetCollisionPriority()
```

### GetMeshes

```csharp
public Array GetMeshes()
```

Returns an [`Collections.Array`](Collections.Array.md) with two elements, the first is the [`Transform3D`](Transform3D.md) of this node and the second is the root [`Mesh`](Mesh.md) of this node. Only works when this node is the root shape.

**Note:** CSG mesh data updates are deferred, which means they are updated with a delay of one rendered frame. To avoid getting an empty shape or outdated mesh data, make sure to call `await get_tree().process_frame` before using `GetMeshes()` in [`Node.Ready`](Node.md) or after changing properties on the `CsgShape3D`.

### GetOperation

```csharp
public CsgShape3D.OperationEnum GetOperation()
```

### GetSmoothingAngle

```csharp
public float GetSmoothingAngle()
```

### GetSnap

```csharp
public float GetSnap()
```

### IsAutosmooth

```csharp
public bool IsAutosmooth()
```

### IsCalculatingTangents

```csharp
public bool IsCalculatingTangents()
```

### IsRootShape

```csharp
public bool IsRootShape()
```

Returns `true` if this is a root shape and is thus the object that is rendered.

### IsUsingCollision

```csharp
public bool IsUsingCollision()
```

### SetAutosmooth

```csharp
public void SetAutosmooth(bool autosmooth)
```

### SetCalculateTangents

```csharp
public void SetCalculateTangents(bool enabled)
```

### SetCollisionLayer

```csharp
public void SetCollisionLayer(uint layer)
```

### SetCollisionLayerValue

```csharp
public void SetCollisionLayerValue(int layerNumber, bool value)
```

Based on `value`, enables or disables the specified layer in the `CollisionLayer`, given a `layerNumber` between 1 and 32.

### SetCollisionMask

```csharp
public void SetCollisionMask(uint mask)
```

### SetCollisionMaskValue

```csharp
public void SetCollisionMaskValue(int layerNumber, bool value)
```

Based on `value`, enables or disables the specified layer in the `CollisionMask`, given a `layerNumber` between 1 and 32.

### SetCollisionPriority

```csharp
public void SetCollisionPriority(float priority)
```

### SetOperation

```csharp
public void SetOperation(CsgShape3D.OperationEnum operation)
```

### SetSmoothingAngle

```csharp
public void SetSmoothingAngle(float smoothingAngle)
```

### SetSnap

```csharp
public void SetSnap(float snap)
```

### SetUseCollision

```csharp
public void SetUseCollision(bool operation)
```

## OperationEnum

命名空间 `Godot`；enum

### 枚举值

- `Union = 0`：Geometry of both primitives is merged, intersecting geometry is removed.
- `Intersection = 1`：Only intersecting geometry remains, the rest is removed.
- `Subtraction = 2`：The second shape is subtracted from the first, leaving a dent with its shape.
