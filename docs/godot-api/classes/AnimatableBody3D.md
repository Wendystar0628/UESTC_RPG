<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimatableBody3D

命名空间 `Godot`；class；继承 [`StaticBody3D`](StaticBody3D.md) → [`PhysicsBody3D`](PhysicsBody3D.md) → [`CollisionObject3D`](CollisionObject3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An animatable 3D physics body. It can't be moved by external forces or contacts, but can be moved manually by other means such as code, [`AnimationMixer`](AnimationMixer.md)s (with [`AnimationMixer.CallbackModeProcess`](AnimationMixer.md) set to [`AnimationMixer.AnimationCallbackModeProcess.Physics`](AnimationMixer.md)), and [`RemoteTransform3D`](RemoteTransform3D.md).

When `AnimatableBody3D` is moved, its linear and angular velocity are estimated and used to affect other physics bodies in its path. This makes it useful for moving platforms, doors, and other moving objects.

## 构造函数

### AnimatableBody3D

```csharp
public AnimatableBody3D()
```

## 属性

### SyncToPhysics

```csharp
public bool SyncToPhysics { get; set; }
```

引擎名：`sync_to_physics`

If `true`, the body's movement will be synchronized to the physics frame. This is useful when animating movement via [`AnimationPlayer`](AnimationPlayer.md), for example on moving platforms. Do **not** use together with [`PhysicsBody3D.MoveAndCollide`](PhysicsBody3D.md).

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
