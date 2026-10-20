<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsTestMotionParameters2D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By changing various properties of this object, such as the motion, you can configure the parameters for [`PhysicsServer2D.BodyTestMotion`](PhysicsServer2D.md).

## 构造函数

### PhysicsTestMotionParameters2D

```csharp
public PhysicsTestMotionParameters2D()
```

## 属性

### CollideSeparationRay

```csharp
public bool CollideSeparationRay { get; set; }
```

引擎名：`collide_separation_ray`

If set to `true`, shapes of type [`PhysicsServer2D.ShapeType.SeparationRay`](PhysicsServer2D.md) are used to detect collisions and can stop the motion. Can be useful when snapping to the ground.

If set to `false`, shapes of type [`PhysicsServer2D.ShapeType.SeparationRay`](PhysicsServer2D.md) are only used for separation when overlapping with other bodies. That's the main use for separation ray shapes.

### ExcludeBodies

```csharp
public Array<Rid> ExcludeBodies { get; set; }
```

引擎名：`exclude_bodies`

Optional array of body [`Rid`](Rid.md) to exclude from collision. Use [`CollisionObject2D.GetRid`](CollisionObject2D.md) to get the [`Rid`](Rid.md) associated with a [`CollisionObject2D`](CollisionObject2D.md)-derived node.

### ExcludeObjects

```csharp
public Array<int> ExcludeObjects { get; set; }
```

引擎名：`exclude_objects`

Optional array of object unique instance ID to exclude from collision. See [`GodotObject.GetInstanceId`](GodotObject.md).

### From

```csharp
public Transform2D From { get; set; }
```

引擎名：`from`

Transform in global space where the motion should start. Usually set to [`Node2D.GlobalTransform`](Node2D.md) for the current body's transform.

### Margin

```csharp
public float Margin { get; set; }
```

引擎名：`margin`

Increases the size of the shapes involved in the collision detection.

### Motion

```csharp
public Vector2 Motion { get; set; }
```

引擎名：`motion`

Motion vector to define the length and direction of the motion to test.

### RecoveryAsCollision

```csharp
public bool RecoveryAsCollision { get; set; }
```

引擎名：`recovery_as_collision`

If set to `true`, any depenetration from the recovery phase is reported as a collision; this is used e.g. by [`CharacterBody2D`](CharacterBody2D.md) for improving floor detection during floor snapping.

If set to `false`, only collisions resulting from the motion are reported, which is generally the desired behavior.

## 方法

### GetExcludeBodies

```csharp
public Array<Rid> GetExcludeBodies()
```

引擎名：`get_exclude_bodies`

### GetExcludeObjects

```csharp
public Array<int> GetExcludeObjects()
```

引擎名：`get_exclude_objects`

### GetFrom

```csharp
public Transform2D GetFrom()
```

引擎名：`get_from`

### GetMargin

```csharp
public float GetMargin()
```

引擎名：`get_margin`

### GetMotion

```csharp
public Vector2 GetMotion()
```

引擎名：`get_motion`

### IsCollideSeparationRayEnabled

```csharp
public bool IsCollideSeparationRayEnabled()
```

引擎名：`is_collide_separation_ray_enabled`

### IsRecoveryAsCollisionEnabled

```csharp
public bool IsRecoveryAsCollisionEnabled()
```

引擎名：`is_recovery_as_collision_enabled`

### SetCollideSeparationRayEnabled

```csharp
public void SetCollideSeparationRayEnabled(bool enabled)
```

引擎名：`set_collide_separation_ray_enabled`

### SetExcludeBodies

```csharp
public void SetExcludeBodies(Array<Rid> excludeList)
```

引擎名：`set_exclude_bodies`

### SetExcludeObjects

```csharp
public void SetExcludeObjects(Array<int> excludeList)
```

引擎名：`set_exclude_objects`

### SetFrom

```csharp
public void SetFrom(Transform2D from)
```

引擎名：`set_from`

### SetMargin

```csharp
public void SetMargin(float margin)
```

引擎名：`set_margin`

### SetMotion

```csharp
public void SetMotion(Vector2 motion)
```

引擎名：`set_motion`

### SetRecoveryAsCollisionEnabled

```csharp
public void SetRecoveryAsCollisionEnabled(bool enabled)
```

引擎名：`set_recovery_as_collision_enabled`
