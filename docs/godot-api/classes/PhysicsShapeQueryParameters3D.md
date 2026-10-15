<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsShapeQueryParameters3D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By changing various properties of this object, such as the shape, you can configure the parameters for [`PhysicsDirectSpaceState3D`](PhysicsDirectSpaceState3D.md)'s methods.

## 构造函数

### PhysicsShapeQueryParameters3D

```csharp
public PhysicsShapeQueryParameters3D()
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

### Margin

```csharp
public float Margin { get; set; }
```

引擎名：`margin`

The collision margin for the shape.

### Motion

```csharp
public Vector3 Motion { get; set; }
```

引擎名：`motion`

The motion of the shape being queried for.

### Shape

```csharp
public Resource Shape { get; set; }
```

引擎名：`shape`

The [`Shape3D`](Shape3D.md) that will be used for collision/intersection queries. This stores the actual reference which avoids the shape to be released while being used for queries, so always prefer using this over `ShapeRid`.

### ShapeRid

```csharp
public Rid ShapeRid { get; set; }
```

引擎名：`shape_rid`

The queried shape's [`Rid`](Rid.md) that will be used for collision/intersection queries. Use this over `Shape` if you want to optimize for performance using the Servers API:

```csharp
RID shapeRid = PhysicsServer3D.SphereShapeCreate();
float radius = 2.0f;
PhysicsServer3D.ShapeSetData(shapeRid, radius);

var params = new PhysicsShapeQueryParameters3D();
params.ShapeRid = shapeRid;

// Execute physics queries here...

// Release the shape when done with physics queries.
PhysicsServer3D.FreeRid(shapeRid);

```

### Transform

```csharp
public Transform3D Transform { get; set; }
```

引擎名：`transform`

The queried shape's transform matrix.

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

### GetMargin

```csharp
public float GetMargin()
```

引擎名：`get_margin`

### GetMotion

```csharp
public Vector3 GetMotion()
```

引擎名：`get_motion`

### GetShape

```csharp
public Resource GetShape()
```

引擎名：`get_shape`

### GetShapeRid

```csharp
public Rid GetShapeRid()
```

引擎名：`get_shape_rid`

### GetTransform

```csharp
public Transform3D GetTransform()
```

引擎名：`get_transform`

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

### SetMargin

```csharp
public void SetMargin(float margin)
```

引擎名：`set_margin`

### SetMotion

```csharp
public void SetMotion(Vector3 motion)
```

引擎名：`set_motion`

### SetShape

```csharp
public void SetShape(Resource shape)
```

引擎名：`set_shape`

### SetShapeRid

```csharp
public void SetShapeRid(Rid shape)
```

引擎名：`set_shape_rid`

### SetTransform

```csharp
public void SetTransform(Transform3D transform)
```

引擎名：`set_transform`
