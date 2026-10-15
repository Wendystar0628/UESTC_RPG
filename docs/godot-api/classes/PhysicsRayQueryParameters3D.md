<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsRayQueryParameters3D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By changing various properties of this object, such as the ray position, you can configure the parameters for [`PhysicsDirectSpaceState3D.IntersectRay`](PhysicsDirectSpaceState3D.md).

## 构造函数

### PhysicsRayQueryParameters3D

```csharp
public PhysicsRayQueryParameters3D()
```

## 属性

### CollideWithAreas

```csharp
public bool CollideWithAreas { get; set; }
```

引擎名：`collide_with_areas`

If `true`, the query will take [`Area3D`](Area3D.md)s into account.

### CollideWithBodies

```csharp
public bool CollideWithBodies { get; set; }
```

引擎名：`collide_with_bodies`

If `true`, the query will take [`PhysicsBody3D`](PhysicsBody3D.md)s into account.

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

The list of object [`Rid`](Rid.md)s that will be excluded from collisions. Use [`CollisionObject3D.GetRid`](CollisionObject3D.md) to get the [`Rid`](Rid.md) associated with a [`CollisionObject3D`](CollisionObject3D.md)-derived node.

**Note:** The returned array is copied and any changes to it will not update the original property value. To update the value you need to modify the returned array, and then assign it to the property again.

### From

```csharp
public Vector3 From { get; set; }
```

引擎名：`from`

The starting point of the ray being queried for, in global coordinates.

### HitBackFaces

```csharp
public bool HitBackFaces { get; set; }
```

引擎名：`hit_back_faces`

If `true`, the query will hit back faces with concave polygon shapes with back face enabled or heightmap shapes.

### HitFromInside

```csharp
public bool HitFromInside { get; set; }
```

引擎名：`hit_from_inside`

If `true`, the query will detect a hit when starting inside shapes. In this case the collision normal will be `Vector3(0, 0, 0)`. Does not affect concave polygon shapes or heightmap shapes.

### To

```csharp
public Vector3 To { get; set; }
```

引擎名：`to`

The ending point of the ray being queried for, in global coordinates.

## 方法

### Create

```csharp
public static PhysicsRayQueryParameters3D Create(Vector3 from, Vector3 to, uint collisionMask = 4294967295, Array<Rid> exclude = null)
```

引擎名：`create`

Returns a new, pre-configured `PhysicsRayQueryParameters3D` object. Use it to quickly create query parameters using the most common options.

```csharp
var query = PhysicsRayQueryParameters3D.create(position, position + Vector3(0, -10, 0))
var collision = get_world_3d().direct_space_state.intersect_ray(query)

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
public Vector3 GetFrom()
```

引擎名：`get_from`

### GetTo

```csharp
public Vector3 GetTo()
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

### IsHitBackFacesEnabled

```csharp
public bool IsHitBackFacesEnabled()
```

引擎名：`is_hit_back_faces_enabled`

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
public void SetFrom(Vector3 from)
```

引擎名：`set_from`

### SetHitBackFaces

```csharp
public void SetHitBackFaces(bool enable)
```

引擎名：`set_hit_back_faces`

### SetHitFromInside

```csharp
public void SetHitFromInside(bool enable)
```

引擎名：`set_hit_from_inside`

### SetTo

```csharp
public void SetTo(Vector3 to)
```

引擎名：`set_to`
