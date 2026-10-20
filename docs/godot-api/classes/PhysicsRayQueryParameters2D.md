<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsRayQueryParameters2D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By changing various properties of this object, such as the ray position, you can configure the parameters for [`PhysicsDirectSpaceState2D.IntersectRay`](PhysicsDirectSpaceState2D.md).

## 构造函数

### PhysicsRayQueryParameters2D

```csharp
public PhysicsRayQueryParameters2D()
```

## 属性

### CollideWithAreas

```csharp
public bool CollideWithAreas { get; set; }
```

引擎名：`collide_with_areas`

If `true`, the query will take [`Area2D`](Area2D.md)s into account.

### CollideWithBodies

```csharp
public bool CollideWithBodies { get; set; }
```

引擎名：`collide_with_bodies`

If `true`, the query will take [`PhysicsBody2D`](PhysicsBody2D.md)s into account.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The physics layers the query will detect (as a bitmask). By default, all collision layers are detected. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### Exclude

```csharp
public Array<Rid> Exclude { get; set; }
```

引擎名：`exclude`

The list of object [`Rid`](Rid.md)s that will be excluded from collisions. Use [`CollisionObject2D.GetRid`](CollisionObject2D.md) to get the [`Rid`](Rid.md) associated with a [`CollisionObject2D`](CollisionObject2D.md)-derived node.

**Note:** The returned array is copied and any changes to it will not update the original property value. To update the value you need to modify the returned array, and then assign it to the property again.

### From

```csharp
public Vector2 From { get; set; }
```

引擎名：`from`

The starting point of the ray being queried for, in global coordinates.

### HitFromInside

```csharp
public bool HitFromInside { get; set; }
```

引擎名：`hit_from_inside`

If `true`, the query will detect a hit when starting inside shapes. In this case the collision normal will be `Vector2(0, 0)`. Does not affect concave polygon shapes.

### To

```csharp
public Vector2 To { get; set; }
```

引擎名：`to`

The ending point of the ray being queried for, in global coordinates.

## 方法

### Create

```csharp
public static PhysicsRayQueryParameters2D Create(Vector2 from, Vector2 to, uint collisionMask = 4294967295, Array<Rid> exclude = null)
```

引擎名：`create`

Returns a new, pre-configured `PhysicsRayQueryParameters2D` object. Use it to quickly create query parameters using the most common options.

```csharp
var query = PhysicsRayQueryParameters2D.create(global_position, global_position + Vector2(0, 100))
var collision = get_world_2d().direct_space_state.intersect_ray(query)

```

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

引擎名：`get_collision_mask`

### GetExclude

```csharp
public Array<Rid> GetExclude()
```

引擎名：`get_exclude`

### GetFrom

```csharp
public Vector2 GetFrom()
```

引擎名：`get_from`

### GetTo

```csharp
public Vector2 GetTo()
```

引擎名：`get_to`

### IsCollideWithAreasEnabled

```csharp
public bool IsCollideWithAreasEnabled()
```

引擎名：`is_collide_with_areas_enabled`

### IsCollideWithBodiesEnabled

```csharp
public bool IsCollideWithBodiesEnabled()
```

引擎名：`is_collide_with_bodies_enabled`

### IsHitFromInsideEnabled

```csharp
public bool IsHitFromInsideEnabled()
```

引擎名：`is_hit_from_inside_enabled`

### SetCollideWithAreas

```csharp
public void SetCollideWithAreas(bool enable)
```

引擎名：`set_collide_with_areas`

### SetCollideWithBodies

```csharp
public void SetCollideWithBodies(bool enable)
```

引擎名：`set_collide_with_bodies`

### SetCollisionMask

```csharp
public void SetCollisionMask(uint collisionMask)
```

引擎名：`set_collision_mask`

### SetExclude

```csharp
public void SetExclude(Array<Rid> exclude)
```

引擎名：`set_exclude`

### SetFrom

```csharp
public void SetFrom(Vector2 from)
```

引擎名：`set_from`

### SetHitFromInside

```csharp
public void SetHitFromInside(bool enable)
```

引擎名：`set_hit_from_inside`

### SetTo

```csharp
public void SetTo(Vector2 to)
```

引擎名：`set_to`
