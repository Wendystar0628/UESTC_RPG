<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimatableBody2D

命名空间 `Godot`；class；继承 [`StaticBody2D`](StaticBody2D.md) → [`PhysicsBody2D`](PhysicsBody2D.md) → [`CollisionObject2D`](CollisionObject2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An animatable 2D physics body. It can't be moved by external forces or contacts, but can be moved manually by other means such as code, [`AnimationMixer`](AnimationMixer.md)s (with [`AnimationMixer.CallbackModeProcess`](AnimationMixer.md) set to [`AnimationMixer.AnimationCallbackModeProcess.Physics`](AnimationMixer.md)), and [`RemoteTransform2D`](RemoteTransform2D.md).

When `AnimatableBody2D` is moved, its linear and angular velocity are estimated and used to affect other physics bodies in its path. This makes it useful for moving platforms, doors, and other moving objects.

## 构造函数

### AnimatableBody2D

```csharp
public AnimatableBody2D()
```

## 属性

### SyncToPhysics

```csharp
public bool SyncToPhysics { get; set; }
```

引擎名：`sync_to_physics`

If `true`, the body's movement will be synchronized to the physics frame. This is useful when animating movement via [`AnimationPlayer`](AnimationPlayer.md), for example on moving platforms. Do **not** use together with [`PhysicsBody2D.MoveAndCollide`](PhysicsBody2D.md).

## 方法

### IsSyncToPhysicsEnabled

```csharp
public bool IsSyncToPhysicsEnabled()
```

引擎名：`is_sync_to_physics_enabled`

### SetSyncToPhysics

```csharp
public void SetSyncToPhysics(bool enable)
```

引擎名：`set_sync_to_physics`
