<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Shape2D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Abstract base class for all 2D shapes, intended for use in physics.

**Performance:** Primitive shapes, especially [`CircleShape2D`](CircleShape2D.md), are fast to check collisions against. [`ConvexPolygonShape2D`](ConvexPolygonShape2D.md) is slower, and [`ConcavePolygonShape2D`](ConcavePolygonShape2D.md) is the slowest.

## 属性

### CustomSolverBias

```csharp
public float CustomSolverBias { get; set; }
```

引擎名：`custom_solver_bias`

The shape's custom solver bias. Defines how much bodies react to enforce contact separation when this shape is involved.

When set to `0`, the default value from `ProjectSettings.physics/2d/solver/default_contact_bias` is used.

## 方法

### Collide

```csharp
public bool Collide(Transform2D localXform, Shape2D withShape, Transform2D shapeXform)
```

引擎名：`collide`

Returns `true` if this shape is colliding with another.

This method needs the transformation matrix for this shape (`localXform`), the shape to check collisions with (`withShape`), and the transformation matrix of that shape (`shapeXform`).

### CollideAndGetContacts

```csharp
public Vector2[] CollideAndGetContacts(Transform2D localXform, Shape2D withShape, Transform2D shapeXform)
```

引擎名：`collide_and_get_contacts`

Returns a list of contact point pairs where this shape touches another.

If there are no collisions, the returned list is empty. Otherwise, the returned list contains contact points arranged in pairs, with entries alternating between points on the boundary of this shape and points on the boundary of `withShape`.

A collision pair A, B can be used to calculate the collision normal with `(B - A).normalized()`, and the collision depth with `(B - A).length()`. This information is typically used to separate shapes, particularly in collision solvers.

This method needs the transformation matrix for this shape (`localXform`), the shape to check collisions with (`withShape`), and the transformation matrix of that shape (`shapeXform`).

### CollideWithMotion

```csharp
public bool CollideWithMotion(Transform2D localXform, Vector2 localMotion, Shape2D withShape, Transform2D shapeXform, Vector2 shapeMotion)
```

引擎名：`collide_with_motion`

Returns whether this shape would collide with another, if a given movement was applied.

This method needs the transformation matrix for this shape (`localXform`), the movement to test on this shape (`localMotion`), the shape to check collisions with (`withShape`), the transformation matrix of that shape (`shapeXform`), and the movement to test onto the other object (`shapeMotion`).

### CollideWithMotionAndGetContacts

```csharp
public Vector2[] CollideWithMotionAndGetContacts(Transform2D localXform, Vector2 localMotion, Shape2D withShape, Transform2D shapeXform, Vector2 shapeMotion)
```

引擎名：`collide_with_motion_and_get_contacts`

Returns a list of contact point pairs where this shape would touch another, if a given movement was applied.

If there would be no collisions, the returned list is empty. Otherwise, the returned list contains contact points arranged in pairs, with entries alternating between points on the boundary of this shape and points on the boundary of `withShape`.

A collision pair A, B can be used to calculate the collision normal with `(B - A).normalized()`, and the collision depth with `(B - A).length()`. This information is typically used to separate shapes, particularly in collision solvers.

This method needs the transformation matrix for this shape (`localXform`), the movement to test on this shape (`localMotion`), the shape to check collisions with (`withShape`), the transformation matrix of that shape (`shapeXform`), and the movement to test onto the other object (`shapeMotion`).

### Draw

```csharp
public void Draw(Rid canvasItem, Color color)
```

引擎名：`draw`

Draws a solid shape onto a [`CanvasItem`](CanvasItem.md) with the [`RenderingServer`](RenderingServer.md) API filled with the specified `color`. The exact drawing method is specific for each shape and cannot be configured.

### GetCustomSolverBias

```csharp
public float GetCustomSolverBias()
```

引擎名：`get_custom_solver_bias`

### GetRect

```csharp
public Rect2 GetRect()
```

引擎名：`get_rect`

Returns a [`Rect2`](Rect2.md) representing the shapes boundary.

### SetCustomSolverBias

```csharp
public void SetCustomSolverBias(float bias)
```

引擎名：`set_custom_solver_bias`
