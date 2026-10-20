<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsDirectSpaceState2D

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Provides direct access to a physics space in the [`PhysicsServer2D`](PhysicsServer2D.md). It's used mainly to do queries against objects and areas residing in a given space.

**Note:** This class is not meant to be instantiated directly. Use [`World2D.DirectSpaceState`](World2D.md) to get the world's physics 2D space state.

## 方法

### CastMotion

```csharp
public float[] CastMotion(PhysicsShapeQueryParameters2D parameters)
```

引擎名：`cast_motion`

Checks how far a [`Shape2D`](Shape2D.md) can move without colliding. All the parameters for the query, including the shape and the motion, are supplied through a [`PhysicsShapeQueryParameters2D`](PhysicsShapeQueryParameters2D.md) object.

Returns an array with the safe and unsafe proportions (between 0 and 1) of the motion. The safe proportion is the maximum fraction of the motion that can be made without a collision. The unsafe proportion is the minimum fraction of the distance that must be moved for a collision. If no collision is detected a result of `[1.0, 1.0]` will be returned.

**Note:** Any [`Shape2D`](Shape2D.md)s that the shape is already colliding with e.g. inside of, will be ignored. Use `CollideShape()` to determine the [`Shape2D`](Shape2D.md)s that the shape is already colliding with.

### CollideShape

```csharp
public Array<Vector2> CollideShape(PhysicsShapeQueryParameters2D parameters, int maxResults = 32)
```

引擎名：`collide_shape`

Checks the intersections of a shape, given through a [`PhysicsShapeQueryParameters2D`](PhysicsShapeQueryParameters2D.md) object, against the space. The resulting array contains a list of points where the shape intersects another. Like with `IntersectShape()`, the number of returned results can be limited to save processing time.

Returned points are a list of pairs of contact points. For each pair the first one is in the shape passed in [`PhysicsShapeQueryParameters2D`](PhysicsShapeQueryParameters2D.md) object, second one is in the collided shape from the physics space.

### GetRestInfo

```csharp
public Dictionary GetRestInfo(PhysicsShapeQueryParameters2D parameters)
```

引擎名：`get_rest_info`

Checks the intersections of a shape, given through a [`PhysicsShapeQueryParameters2D`](PhysicsShapeQueryParameters2D.md) object, against the space. If it collides with more than one shape, the nearest one is selected. The returned object is a dictionary containing the following fields:

`collider_id`: The colliding object's ID.

`linear_velocity`: The colliding object's velocity [`Vector2`](Vector2.md). If the object is an [`Area2D`](Area2D.md), the result is `(0, 0)`.

`normal`: The collision normal of the query shape at the intersection point, pointing away from the intersecting object.

`point`: The intersection point.

`rid`: The intersecting object's [`Rid`](Rid.md).

`shape`: The shape index of the colliding shape.

If the shape did not intersect anything, then an empty dictionary is returned instead.

### IntersectPoint

```csharp
public Array<Dictionary> IntersectPoint(PhysicsPointQueryParameters2D parameters, int maxResults = 32)
```

引擎名：`intersect_point`

Checks whether a point is inside any solid shape. Position and other parameters are defined through [`PhysicsPointQueryParameters2D`](PhysicsPointQueryParameters2D.md). The shapes the point is inside of are returned in an array containing dictionaries with the following fields:

`collider`: The colliding object.

`collider_id`: The colliding object's ID.

`rid`: The intersecting object's [`Rid`](Rid.md).

`shape`: The shape index of the colliding shape.

The number of intersections can be limited with the `maxResults` parameter, to reduce the processing time.

**Note:** [`ConcavePolygonShape2D`](ConcavePolygonShape2D.md)s and [`CollisionPolygon2D`](CollisionPolygon2D.md)s in `Segments` build mode are not solid shapes. Therefore, they will not be detected.

### IntersectRay

```csharp
public Dictionary IntersectRay(PhysicsRayQueryParameters2D parameters)
```

引擎名：`intersect_ray`

Intersects a ray in a given space. Ray position and other parameters are defined through [`PhysicsRayQueryParameters2D`](PhysicsRayQueryParameters2D.md). The returned object is a dictionary with the following fields:

`collider`: The colliding object.

`collider_id`: The colliding object's ID.

`normal`: The object's surface normal at the intersection point, or `Vector2(0, 0)` if the ray starts inside the shape and [`PhysicsRayQueryParameters2D.HitFromInside`](PhysicsRayQueryParameters2D.md) is `true`.

`position`: The intersection point.

`rid`: The intersecting object's [`Rid`](Rid.md).

`shape`: The shape index of the colliding shape.

If the ray did not intersect anything, then an empty dictionary is returned instead.

### IntersectShape

```csharp
public Array<Dictionary> IntersectShape(PhysicsShapeQueryParameters2D parameters, int maxResults = 32)
```

引擎名：`intersect_shape`

Checks the intersections of a shape, given through a [`PhysicsShapeQueryParameters2D`](PhysicsShapeQueryParameters2D.md) object, against the space. The intersected shapes are returned in an array containing dictionaries with the following fields:

`collider`: The colliding object.

`collider_id`: The colliding object's ID.

`rid`: The intersecting object's [`Rid`](Rid.md).

`shape`: The shape index of the colliding shape.

The number of intersections can be limited with the `maxResults` parameter, to reduce the processing time.
