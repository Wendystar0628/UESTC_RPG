<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CharacterBody2D

命名空间 `Godot`；class；继承 [`PhysicsBody2D`](PhysicsBody2D.md) → [`CollisionObject2D`](CollisionObject2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`CharacterBody2D` is a specialized class for physics bodies that are meant to be user-controlled. They are not affected by physics at all, but they affect other physics bodies in their path. They are mainly used to provide high-level API to move objects with wall and slope detection (`MoveAndSlide()` method) in addition to the general collision detection provided by [`PhysicsBody2D.MoveAndCollide`](PhysicsBody2D.md). This makes it useful for highly configurable physics bodies that must move in specific ways and collide with the world, as is often the case with user-controlled characters.

For game objects that don't require complex movement or collision detection, such as moving platforms, [`AnimatableBody2D`](AnimatableBody2D.md) is simpler to configure.

## 构造函数

### CharacterBody2D

```csharp
public CharacterBody2D()
```

## 属性

### FloorBlockOnWall

```csharp
public bool FloorBlockOnWall { get; set; }
```

引擎名：`floor_block_on_wall`

If `true`, the body will be able to move on the floor only. This option avoids to be able to walk on walls, it will however allow to slide down along them.

### FloorConstantSpeed

```csharp
public bool FloorConstantSpeed { get; set; }
```

引擎名：`floor_constant_speed`

If `false` (by default), the body will move faster on downward slopes and slower on upward slopes.

If `true`, the body will always move at the same speed on the ground no matter the slope. Note that you need to use `FloorSnapLength` to stick along a downward slope at constant speed.

### FloorMaxAngle

```csharp
public float FloorMaxAngle { get; set; }
```

引擎名：`floor_max_angle`

Maximum angle (in radians) where a slope is still considered a floor (or a ceiling), rather than a wall, when calling `MoveAndSlide()`. The default value equals 45 degrees.

### FloorSnapLength

```csharp
public float FloorSnapLength { get; set; }
```

引擎名：`floor_snap_length`

Sets a snapping distance. When set to a value different from `0.0`, the body is kept attached to slopes when calling `MoveAndSlide()`. The snapping vector is determined by the given distance along the opposite direction of the `UpDirection`.

As long as the snapping vector is in contact with the ground and the body moves against `UpDirection`, the body will remain attached to the surface. Snapping is not applied if the body moves along `UpDirection`, meaning it contains vertical rising velocity, so it will be able to detach from the ground when jumping or when the body is pushed up by something. If you want to apply a snap without taking into account the velocity, use `ApplyFloorSnap()`.

### FloorStopOnSlope

```csharp
public bool FloorStopOnSlope { get; set; }
```

引擎名：`floor_stop_on_slope`

If `true`, the body will not slide on slopes when calling `MoveAndSlide()` when the body is standing still.

If `false`, the body will slide on floor's slopes when `Velocity` applies a downward force.

### MaxSlides

```csharp
public int MaxSlides { get; set; }
```

引擎名：`max_slides`

Maximum number of times the body can change direction before it stops when calling `MoveAndSlide()`. Must be greater than zero.

### MotionMode

```csharp
public CharacterBody2D.MotionModeEnum MotionMode { get; set; }
```

引擎名：`motion_mode`

Sets the motion mode which defines the behavior of `MoveAndSlide()`.

### PlatformFloorLayers

```csharp
public uint PlatformFloorLayers { get; set; }
```

引擎名：`platform_floor_layers`

Collision layers that will be included for detecting floor bodies that will act as moving platforms to be followed by the `CharacterBody2D`. By default, all floor bodies are detected and propagate their velocity.

### PlatformOnLeave

```csharp
public CharacterBody2D.PlatformOnLeaveEnum PlatformOnLeave { get; set; }
```

引擎名：`platform_on_leave`

Sets the behavior to apply when you leave a moving platform. By default, to be physically accurate, when you leave the last platform velocity is applied.

### PlatformWallLayers

```csharp
public uint PlatformWallLayers { get; set; }
```

引擎名：`platform_wall_layers`

Collision layers that will be included for detecting wall bodies that will act as moving platforms to be followed by the `CharacterBody2D`. By default, all wall bodies are ignored.

### SafeMargin

```csharp
public float SafeMargin { get; set; }
```

引擎名：`safe_margin`

Extra margin used for collision recovery when calling `MoveAndSlide()`.

If the body is at least this close to another body, it will consider them to be colliding and will be pushed away before performing the actual motion.

A higher value means it's more flexible for detecting collision, which helps with consistently detecting walls and floors.

A lower value forces the collision algorithm to use more exact detection, so it can be used in cases that specifically require precision, e.g at very low scale to avoid visible jittering, or for stability with a stack of character bodies.

### SlideOnCeiling

```csharp
public bool SlideOnCeiling { get; set; }
```

引擎名：`slide_on_ceiling`

If `true`, during a jump against the ceiling, the body will slide, if `false` it will be stopped and will fall vertically.

### UpDirection

```csharp
public Vector2 UpDirection { get; set; }
```

引擎名：`up_direction`

Vector pointing upwards, used to determine what is a wall and what is a floor (or a ceiling) when calling `MoveAndSlide()`. Defaults to `Vector2.Up`. As the vector will be normalized it can't be equal to `Vector2.Zero`, if you want all collisions to be reported as walls, consider using [`CharacterBody2D.MotionModeEnum.Floating`](CharacterBody2D.md) as `MotionMode`.

### Velocity

```csharp
public Vector2 Velocity { get; set; }
```

引擎名：`velocity`

Current velocity vector in pixels per second, used and modified during calls to `MoveAndSlide()`.

**Note:** A common mistake is setting this property to the desired velocity multiplied by `delta`, which produces a motion vector in pixels.

### WallMinSlideAngle

```csharp
public float WallMinSlideAngle { get; set; }
```

引擎名：`wall_min_slide_angle`

Minimum angle (in radians) where the body is allowed to slide when it encounters a wall. The default value equals 15 degrees. This property only affects movement when `MotionMode` is [`CharacterBody2D.MotionModeEnum.Floating`](CharacterBody2D.md).

## 方法

### ApplyFloorSnap

```csharp
public void ApplyFloorSnap()
```

引擎名：`apply_floor_snap`

Allows to manually apply a snap to the floor regardless of the body's velocity. This function does nothing when `IsOnFloor()` returns `true`.

### GetFloorAngle

```csharp
public float GetFloorAngle(Vector2? upDirection = null)
```

引擎名：`get_floor_angle`

Returns the floor's collision angle at the last collision point according to `upDirection`, which is `Vector2.Up` by default. This value is always positive and only valid after calling `MoveAndSlide()` and when `IsOnFloor()` returns `true`.

参数：
- `upDirection`：If the parameter is null, then the default value is `new Vector2(0.0f, -1.0f)`.

### GetFloorMaxAngle

```csharp
public float GetFloorMaxAngle()
```

引擎名：`get_floor_max_angle`

### GetFloorNormal

```csharp
public Vector2 GetFloorNormal()
```

引擎名：`get_floor_normal`

Returns the collision normal of the floor at the last collision point. Only valid after calling `MoveAndSlide()` and when `IsOnFloor()` returns `true`.

**Warning:** The collision normal is not always the same as the surface normal.

### GetFloorSnapLength

```csharp
public float GetFloorSnapLength()
```

引擎名：`get_floor_snap_length`

### GetLastMotion

```csharp
public Vector2 GetLastMotion()
```

引擎名：`get_last_motion`

Returns the last motion applied to the `CharacterBody2D` during the last call to `MoveAndSlide()`. The movement can be split into multiple motions when sliding occurs, and this method return the last one, which is useful to retrieve the current direction of the movement.

### GetLastSlideCollision

```csharp
public KinematicCollision2D GetLastSlideCollision()
```

引擎名：`get_last_slide_collision`

Returns a [`KinematicCollision2D`](KinematicCollision2D.md) if a collision occurred. The returned value contains information about the latest collision that occurred during the last call to `MoveAndSlide()`. Returns `null` if no collision occurred. See also `GetSlideCollision()`.

### GetMaxSlides

```csharp
public int GetMaxSlides()
```

引擎名：`get_max_slides`

### GetMotionMode

```csharp
public CharacterBody2D.MotionModeEnum GetMotionMode()
```

引擎名：`get_motion_mode`

### GetPlatformFloorLayers

```csharp
public uint GetPlatformFloorLayers()
```

引擎名：`get_platform_floor_layers`

### GetPlatformOnLeave

```csharp
public CharacterBody2D.PlatformOnLeaveEnum GetPlatformOnLeave()
```

引擎名：`get_platform_on_leave`

### GetPlatformVelocity

```csharp
public Vector2 GetPlatformVelocity()
```

引擎名：`get_platform_velocity`

Returns the linear velocity of the platform at the last collision point. Only valid after calling `MoveAndSlide()`.

### GetPlatformWallLayers

```csharp
public uint GetPlatformWallLayers()
```

引擎名：`get_platform_wall_layers`

### GetPositionDelta

```csharp
public Vector2 GetPositionDelta()
```

引擎名：`get_position_delta`

Returns the travel (position delta) that occurred during the last call to `MoveAndSlide()`.

### GetRealVelocity

```csharp
public Vector2 GetRealVelocity()
```

引擎名：`get_real_velocity`

Returns the current real velocity since the last call to `MoveAndSlide()`. For example, when you climb a slope, you will move diagonally even though the velocity is horizontal. This method returns the diagonal movement, as opposed to `Velocity` which returns the requested velocity.

### GetSafeMargin

```csharp
public float GetSafeMargin()
```

引擎名：`get_safe_margin`

### GetSlideCollision

```csharp
public KinematicCollision2D GetSlideCollision(int slideIdx)
```

引擎名：`get_slide_collision`

Returns a [`KinematicCollision2D`](KinematicCollision2D.md), which contains information about a collision that occurred during the last call to `MoveAndSlide()`. Since the body can collide several times in a single call to `MoveAndSlide()`, you must specify the index of the collision in the range 0 to (`GetSlideCollisionCount()` - 1). See also `GetLastSlideCollision()`.

**Example:** Iterate through the collisions with a `for` loop:

```csharp
for (int i = 0; i < GetSlideCollisionCount(); i++)
{
KinematicCollision2D collision = GetSlideCollision(i);
GD.Print("Collided with: ", (collision.GetCollider() as Node).Name);
}

```

### GetSlideCollisionCount

```csharp
public int GetSlideCollisionCount()
```

引擎名：`get_slide_collision_count`

Returns the number of times the body collided and changed direction during the last call to `MoveAndSlide()`.

### GetUpDirection

```csharp
public Vector2 GetUpDirection()
```

引擎名：`get_up_direction`

### GetVelocity

```csharp
public Vector2 GetVelocity()
```

引擎名：`get_velocity`

### GetWallMinSlideAngle

```csharp
public float GetWallMinSlideAngle()
```

引擎名：`get_wall_min_slide_angle`

### GetWallNormal

```csharp
public Vector2 GetWallNormal()
```

引擎名：`get_wall_normal`

Returns the collision normal of the wall at the last collision point. Only valid after calling `MoveAndSlide()` and when `IsOnWall()` returns `true`.

**Warning:** The collision normal is not always the same as the surface normal.

### IsFloorBlockOnWallEnabled

```csharp
public bool IsFloorBlockOnWallEnabled()
```

引擎名：`is_floor_block_on_wall_enabled`

### IsFloorConstantSpeedEnabled

```csharp
public bool IsFloorConstantSpeedEnabled()
```

引擎名：`is_floor_constant_speed_enabled`

### IsFloorStopOnSlopeEnabled

```csharp
public bool IsFloorStopOnSlopeEnabled()
```

引擎名：`is_floor_stop_on_slope_enabled`

### IsOnCeiling

```csharp
public bool IsOnCeiling()
```

引擎名：`is_on_ceiling`

Returns `true` if the body collided with the ceiling on the last call of `MoveAndSlide()`. Otherwise, returns `false`. The `UpDirection` and `FloorMaxAngle` are used to determine whether a surface is "ceiling" or not.

### IsOnCeilingOnly

```csharp
public bool IsOnCeilingOnly()
```

引擎名：`is_on_ceiling_only`

Returns `true` if the body collided only with the ceiling on the last call of `MoveAndSlide()`. Otherwise, returns `false`. The `UpDirection` and `FloorMaxAngle` are used to determine whether a surface is "ceiling" or not.

### IsOnFloor

```csharp
public bool IsOnFloor()
```

引擎名：`is_on_floor`

Returns `true` if the body collided with the floor on the last call of `MoveAndSlide()`. Otherwise, returns `false`. The `UpDirection` and `FloorMaxAngle` are used to determine whether a surface is "floor" or not.

### IsOnFloorOnly

```csharp
public bool IsOnFloorOnly()
```

引擎名：`is_on_floor_only`

Returns `true` if the body collided only with the floor on the last call of `MoveAndSlide()`. Otherwise, returns `false`. The `UpDirection` and `FloorMaxAngle` are used to determine whether a surface is "floor" or not.

### IsOnWall

```csharp
public bool IsOnWall()
```

引擎名：`is_on_wall`

Returns `true` if the body collided with a wall on the last call of `MoveAndSlide()`. Otherwise, returns `false`. The `UpDirection` and `FloorMaxAngle` are used to determine whether a surface is "wall" or not.

### IsOnWallOnly

```csharp
public bool IsOnWallOnly()
```

引擎名：`is_on_wall_only`

Returns `true` if the body collided only with a wall on the last call of `MoveAndSlide()`. Otherwise, returns `false`. The `UpDirection` and `FloorMaxAngle` are used to determine whether a surface is "wall" or not.

### IsSlideOnCeilingEnabled

```csharp
public bool IsSlideOnCeilingEnabled()
```

引擎名：`is_slide_on_ceiling_enabled`

### MoveAndSlide

```csharp
public bool MoveAndSlide()
```

引擎名：`move_and_slide`

Moves the body based on `Velocity`. If the body collides with another, it will slide along the other body (by default only on floor) rather than stop immediately. If the other body is a `CharacterBody2D` or [`RigidBody2D`](RigidBody2D.md), it will also be affected by the motion of the other body. You can use this to make moving and rotating platforms, or to make nodes push other nodes.

This method should be used in [`Node._PhysicsProcess()`](Node.md) (or in a method called by [`Node._PhysicsProcess()`](Node.md)), as it uses the physics step's `delta` value automatically in calculations. Otherwise, the simulation will run at an incorrect speed.

Modifies `Velocity` if a slide collision occurred. To get the latest collision call `GetLastSlideCollision()`, for detailed information about collisions that occurred, use `GetSlideCollision()`.

When the body touches a moving platform, the platform's velocity is automatically added to the body motion. If a collision occurs due to the platform's motion, it will always be first in the slide collisions.

The general behavior and available properties change according to the `MotionMode`.

Returns `true` if the body collided, otherwise, returns `false`.

### SetFloorBlockOnWallEnabled

```csharp
public void SetFloorBlockOnWallEnabled(bool enabled)
```

引擎名：`set_floor_block_on_wall_enabled`

### SetFloorConstantSpeedEnabled

```csharp
public void SetFloorConstantSpeedEnabled(bool enabled)
```

引擎名：`set_floor_constant_speed_enabled`

### SetFloorMaxAngle

```csharp
public void SetFloorMaxAngle(float radians)
```

引擎名：`set_floor_max_angle`

### SetFloorSnapLength

```csharp
public void SetFloorSnapLength(float floorSnapLength)
```

引擎名：`set_floor_snap_length`

### SetFloorStopOnSlopeEnabled

```csharp
public void SetFloorStopOnSlopeEnabled(bool enabled)
```

引擎名：`set_floor_stop_on_slope_enabled`

### SetMaxSlides

```csharp
public void SetMaxSlides(int maxSlides)
```

引擎名：`set_max_slides`

### SetMotionMode

```csharp
public void SetMotionMode(CharacterBody2D.MotionModeEnum mode)
```

引擎名：`set_motion_mode`

### SetPlatformFloorLayers

```csharp
public void SetPlatformFloorLayers(uint excludeLayer)
```

引擎名：`set_platform_floor_layers`

### SetPlatformOnLeave

```csharp
public void SetPlatformOnLeave(CharacterBody2D.PlatformOnLeaveEnum onLeaveApplyVelocity)
```

引擎名：`set_platform_on_leave`

### SetPlatformWallLayers

```csharp
public void SetPlatformWallLayers(uint excludeLayer)
```

引擎名：`set_platform_wall_layers`

### SetSafeMargin

```csharp
public void SetSafeMargin(float margin)
```

引擎名：`set_safe_margin`

### SetSlideOnCeilingEnabled

```csharp
public void SetSlideOnCeilingEnabled(bool enabled)
```

引擎名：`set_slide_on_ceiling_enabled`

### SetUpDirection

```csharp
public void SetUpDirection(Vector2 upDirection)
```

引擎名：`set_up_direction`

### SetVelocity

```csharp
public void SetVelocity(Vector2 velocity)
```

引擎名：`set_velocity`

### SetWallMinSlideAngle

```csharp
public void SetWallMinSlideAngle(float radians)
```

引擎名：`set_wall_min_slide_angle`

## MotionModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Grounded = 0`：Apply when notions of walls, ceiling and floor are relevant.
- `Floating = 1`：Apply when there is no notion of floor or ceiling.

## PlatformOnLeaveEnum

命名空间 `Godot`；enum

### 枚举值

- `AddVelocity = 0`：Add the last platform velocity to the [`CharacterBody2D.Velocity`](CharacterBody2D.md) when you leave a moving platform.
- `AddUpwardVelocity = 1`：Add the last platform velocity to the [`CharacterBody2D.Velocity`](CharacterBody2D.md) when you leave a moving platform, but any downward motion is ignored.
- `DoNothing = 2`：Do nothing when leaving a platform.
