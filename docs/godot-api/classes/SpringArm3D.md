<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpringArm3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`SpringArm3D` casts a ray or a shape along its Z axis and moves all its direct children to the collision point, with an optional margin. This is useful for 3rd person cameras that move closer to the player when inside a tight space (you may need to exclude the player's collider from the `SpringArm3D`'s collision check).

## 构造函数

### SpringArm3D

```csharp
public SpringArm3D()
```

## 属性

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The layers against which the collision check will be done. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### Margin

```csharp
public float Margin { get; set; }
```

引擎名：`margin`

When the collision check is made, a candidate length for the SpringArm3D is given.

The margin is then subtracted to this length and the translation is applied to the child objects of the SpringArm3D.

This margin is useful for when the SpringArm3D has a [`Camera3D`](Camera3D.md) as a child node: without the margin, the [`Camera3D`](Camera3D.md) would be placed on the exact point of collision, while with the margin the [`Camera3D`](Camera3D.md) would be placed close to the point of collision.

### Shape

```csharp
public Shape3D Shape { get; set; }
```

引擎名：`shape`

The [`Shape3D`](Shape3D.md) to use for the SpringArm3D.

When the shape is set, the SpringArm3D will cast the [`Shape3D`](Shape3D.md) on its z axis instead of performing a ray cast.

### SpringLength

```csharp
public float SpringLength { get; set; }
```

引擎名：`spring_length`

The maximum extent of the SpringArm3D. This is used as a length for both the ray and the shape cast used internally to calculate the desired position of the SpringArm3D's child nodes.

To know more about how to perform a shape cast or a ray cast, please consult the [`PhysicsDirectSpaceState3D`](PhysicsDirectSpaceState3D.md) documentation.

## 方法

### AddExcludedObject

```csharp
public void AddExcludedObject(Rid rID)
```

引擎名：`add_excluded_object`

Adds the [`PhysicsBody3D`](PhysicsBody3D.md) object with the given [`Rid`](Rid.md) to the list of [`PhysicsBody3D`](PhysicsBody3D.md) objects excluded from the collision check.

### ClearExcludedObjects

```csharp
public void ClearExcludedObjects()
```

引擎名：`clear_excluded_objects`

Clears the list of [`PhysicsBody3D`](PhysicsBody3D.md) objects excluded from the collision check.

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

引擎名：`get_collision_mask`

### GetHitLength

```csharp
public float GetHitLength()
```

引擎名：`get_hit_length`

Returns the spring arm's current length.

### GetLength

```csharp
public float GetLength()
```

引擎名：`get_length`

### GetMargin

```csharp
public float GetMargin()
```

引擎名：`get_margin`

### GetShape

```csharp
public Shape3D GetShape()
```

引擎名：`get_shape`

### RemoveExcludedObject

```csharp
public bool RemoveExcludedObject(Rid rID)
```

引擎名：`remove_excluded_object`

Removes the given [`Rid`](Rid.md) from the list of [`PhysicsBody3D`](PhysicsBody3D.md) objects excluded from the collision check.

### SetCollisionMask

```csharp
public void SetCollisionMask(uint mask)
```

引擎名：`set_collision_mask`

### SetLength

```csharp
public void SetLength(float length)
```

引擎名：`set_length`

### SetMargin

```csharp
public void SetMargin(float margin)
```

引擎名：`set_margin`

### SetShape

```csharp
public void SetShape(Shape3D shape)
```

引擎名：`set_shape`
