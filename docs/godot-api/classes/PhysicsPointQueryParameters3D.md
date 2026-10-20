<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsPointQueryParameters3D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By changing various properties of this object, such as the point position, you can configure the parameters for [`PhysicsDirectSpaceState3D.IntersectPoint`](PhysicsDirectSpaceState3D.md).

## 构造函数

### PhysicsPointQueryParameters3D

```csharp
public PhysicsPointQueryParameters3D()
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

### Position

```csharp
public Vector3 Position { get; set; }
```

引擎名：`position`

The position being queried for, in global coordinates.

## 方法

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

### GetPosition

```csharp
public Vector3 GetPosition()
```

引擎名：`get_position`

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

### SetPosition

```csharp
public void SetPosition(Vector3 position)
```

引擎名：`set_position`
