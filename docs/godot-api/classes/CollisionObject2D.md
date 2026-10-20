<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CollisionObject2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Abstract base class for 2D physics objects. `CollisionObject2D` can hold any number of [`Shape2D`](Shape2D.md)s for collision. Each shape must be assigned to a *shape owner*. Shape owners are not nodes and do not appear in the editor, but are accessible through code using the `shape_owner_*` methods.

**Note:** Only collisions between objects within the same canvas ([`Viewport`](Viewport.md) canvas or [`CanvasLayer`](CanvasLayer.md)) are supported. The behavior of collisions between objects in different canvases is undefined.

## 属性

### CollisionLayer

```csharp
public uint CollisionLayer { get; set; }
```

引擎名：`collision_layer`

The physics layers this CollisionObject2D is in. Collision objects can exist in one or more of 32 different layers. See also `CollisionMask`.

**Note:** Object A can detect a contact with object B only if object B is in any of the layers that object A scans. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The physics layers this CollisionObject2D scans. Collision objects can scan one or more of 32 different layers. See also `CollisionLayer`.

**Note:** Object A can detect a contact with object B only if object B is in any of the layers that object A scans. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionPriority

```csharp
public float CollisionPriority { get; set; }
```

引擎名：`collision_priority`

The priority used to solve colliding when occurring penetration. The higher the priority is, the lower the penetration into the object will be. This can for example be used to prevent the player from breaking through the boundaries of a level.

### DisableMode

```csharp
public CollisionObject2D.DisableModeEnum DisableMode { get; set; }
```

引擎名：`disable_mode`

Defines the behavior in physics when [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md).

### InputPickable

```csharp
public bool InputPickable { get; set; }
```

引擎名：`input_pickable`

If `true`, this object is pickable. A pickable object can detect the mouse pointer entering/leaving, and if the mouse is inside it, report input events. Requires at least one `CollisionLayer` bit to be set.

## 方法

### CreateShapeOwner

```csharp
public uint CreateShapeOwner(GodotObject owner)
```

引擎名：`create_shape_owner`

Creates a new shape owner for the given object. Returns `owner_id` of the new owner for future reference.

### GetCollisionLayer

```csharp
public uint GetCollisionLayer()
```

引擎名：`get_collision_layer`

### GetCollisionLayerValue

```csharp
public bool GetCollisionLayerValue(int layerNumber)
```

引擎名：`get_collision_layer_value`

Returns whether or not the specified layer of the `CollisionLayer` is enabled, given a `layerNumber` between 1 and 32.

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

引擎名：`get_collision_mask`

### GetCollisionMaskValue

```csharp
public bool GetCollisionMaskValue(int layerNumber)
```

引擎名：`get_collision_mask_value`

Returns whether or not the specified layer of the `CollisionMask` is enabled, given a `layerNumber` between 1 and 32.

### GetCollisionPriority

```csharp
public float GetCollisionPriority()
```

引擎名：`get_collision_priority`

### GetDisableMode

```csharp
public CollisionObject2D.DisableModeEnum GetDisableMode()
```

引擎名：`get_disable_mode`

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the object's [`Rid`](Rid.md).

### GetShapeOwnerOneWayCollisionDirection

```csharp
public Vector2 GetShapeOwnerOneWayCollisionDirection(uint ownerId)
```

引擎名：`get_shape_owner_one_way_collision_direction`

Returns the `one_way_collision_direction` of the shape owner identified by the given `ownerId`.

### GetShapeOwnerOneWayCollisionMargin

```csharp
public float GetShapeOwnerOneWayCollisionMargin(uint ownerId)
```

引擎名：`get_shape_owner_one_way_collision_margin`

Returns the `one_way_collision_margin` of the shape owner identified by given `ownerId`.

### GetShapeOwners

```csharp
public int[] GetShapeOwners()
```

引擎名：`get_shape_owners`

Returns an [`Collections.Array`](Collections.Array.md) of `owner_id` identifiers. You can use these ids in other methods that take `owner_id` as an argument.

### IsPickable

```csharp
public bool IsPickable()
```

引擎名：`is_pickable`

### IsShapeOwnerDisabled

```csharp
public bool IsShapeOwnerDisabled(uint ownerId)
```

引擎名：`is_shape_owner_disabled`

If `true`, the shape owner and its shapes are disabled.

### IsShapeOwnerOneWayCollisionEnabled

```csharp
public bool IsShapeOwnerOneWayCollisionEnabled(uint ownerId)
```

引擎名：`is_shape_owner_one_way_collision_enabled`

Returns `true` if collisions for the shape owner originating from this `CollisionObject2D` will not be reported to collided with `CollisionObject2D`s.

### RemoveShapeOwner

```csharp
public void RemoveShapeOwner(uint ownerId)
```

引擎名：`remove_shape_owner`

Removes the given shape owner.

### SetCollisionLayer

```csharp
public void SetCollisionLayer(uint layer)
```

引擎名：`set_collision_layer`

### SetCollisionLayerValue

```csharp
public void SetCollisionLayerValue(int layerNumber, bool value)
```

引擎名：`set_collision_layer_value`

Based on `value`, enables or disables the specified layer in the `CollisionLayer`, given a `layerNumber` between 1 and 32.

### SetCollisionMask

```csharp
public void SetCollisionMask(uint mask)
```

引擎名：`set_collision_mask`

### SetCollisionMaskValue

```csharp
public void SetCollisionMaskValue(int layerNumber, bool value)
```

引擎名：`set_collision_mask_value`

Based on `value`, enables or disables the specified layer in the `CollisionMask`, given a `layerNumber` between 1 and 32.

### SetCollisionPriority

```csharp
public void SetCollisionPriority(float priority)
```

引擎名：`set_collision_priority`

### SetDisableMode

```csharp
public void SetDisableMode(CollisionObject2D.DisableModeEnum mode)
```

引擎名：`set_disable_mode`

### SetPickable

```csharp
public void SetPickable(bool enabled)
```

引擎名：`set_pickable`

### ShapeFindOwner

```csharp
public uint ShapeFindOwner(int shapeIndex)
```

引擎名：`shape_find_owner`

Returns the `owner_id` of the given shape.

### ShapeOwnerAddShape

```csharp
public void ShapeOwnerAddShape(uint ownerId, Shape2D shape)
```

引擎名：`shape_owner_add_shape`

Adds a [`Shape2D`](Shape2D.md) to the shape owner.

### ShapeOwnerClearShapes

```csharp
public void ShapeOwnerClearShapes(uint ownerId)
```

引擎名：`shape_owner_clear_shapes`

Removes all shapes from the shape owner.

### ShapeOwnerGetOwner

```csharp
public GodotObject ShapeOwnerGetOwner(uint ownerId)
```

引擎名：`shape_owner_get_owner`

Returns the parent object of the given shape owner.

### ShapeOwnerGetShape

```csharp
public Shape2D ShapeOwnerGetShape(uint ownerId, int shapeId)
```

引擎名：`shape_owner_get_shape`

Returns the [`Shape2D`](Shape2D.md) with the given ID from the given shape owner.

### ShapeOwnerGetShapeCount

```csharp
public int ShapeOwnerGetShapeCount(uint ownerId)
```

引擎名：`shape_owner_get_shape_count`

Returns the number of shapes the given shape owner contains.

### ShapeOwnerGetShapeIndex

```csharp
public int ShapeOwnerGetShapeIndex(uint ownerId, int shapeId)
```

引擎名：`shape_owner_get_shape_index`

Returns the child index of the [`Shape2D`](Shape2D.md) with the given ID from the given shape owner.

### ShapeOwnerGetTransform

```csharp
public Transform2D ShapeOwnerGetTransform(uint ownerId)
```

引擎名：`shape_owner_get_transform`

Returns the shape owner's [`Transform2D`](Transform2D.md).

### ShapeOwnerRemoveShape

```csharp
public void ShapeOwnerRemoveShape(uint ownerId, int shapeId)
```

引擎名：`shape_owner_remove_shape`

Removes a shape from the given shape owner.

### ShapeOwnerSetDisabled

```csharp
public void ShapeOwnerSetDisabled(uint ownerId, bool disabled)
```

引擎名：`shape_owner_set_disabled`

If `true`, disables the given shape owner.

### ShapeOwnerSetOneWayCollision

```csharp
public void ShapeOwnerSetOneWayCollision(uint ownerId, bool enable)
```

引擎名：`shape_owner_set_one_way_collision`

If `enable` is `true`, collisions for the shape owner originating from this `CollisionObject2D` will not be reported to collided with `CollisionObject2D`s.

### ShapeOwnerSetOneWayCollisionDirection

```csharp
public void ShapeOwnerSetOneWayCollisionDirection(uint ownerId, Vector2 direction)
```

引擎名：`shape_owner_set_one_way_collision_direction`

Sets the `one_way_collision_direction` of the shape owner identified by the given `ownerId` to `direction`.

### ShapeOwnerSetOneWayCollisionMargin

```csharp
public void ShapeOwnerSetOneWayCollisionMargin(uint ownerId, float margin)
```

引擎名：`shape_owner_set_one_way_collision_margin`

Sets the `one_way_collision_margin` of the shape owner identified by given `ownerId` to `margin` pixels.

### ShapeOwnerSetTransform

```csharp
public void ShapeOwnerSetTransform(uint ownerId, Transform2D transform)
```

引擎名：`shape_owner_set_transform`

Sets the [`Transform2D`](Transform2D.md) of the given shape owner.

### _InputEvent

```csharp
public virtual void _InputEvent(Viewport viewport, InputEvent event, int shapeIdx)
```

引擎名：`_input_event`

Accepts unhandled [`InputEvent`](InputEvent.md)s. `shapeIdx` is the child index of the clicked [`Shape2D`](Shape2D.md). Connect to `InputEvent` to easily pick up these events.

**Note:** `_InputEvent()` requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be set.

### _MouseEnter

```csharp
public virtual void _MouseEnter()
```

引擎名：`_mouse_enter`

Called when the mouse pointer enters any of this object's shapes. Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be set. Note that moving between different shapes within a single `CollisionObject2D` won't cause this function to be called.

### _MouseExit

```csharp
public virtual void _MouseExit()
```

引擎名：`_mouse_exit`

Called when the mouse pointer exits all this object's shapes. Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be set. Note that moving between different shapes within a single `CollisionObject2D` won't cause this function to be called.

### _MouseShapeEnter

```csharp
public virtual void _MouseShapeEnter(int shapeIdx)
```

引擎名：`_mouse_shape_enter`

Called when the mouse pointer enters any of this object's shapes or moves from one shape to another. `shapeIdx` is the child index of the newly entered [`Shape2D`](Shape2D.md). Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be called.

### _MouseShapeExit

```csharp
public virtual void _MouseShapeExit(int shapeIdx)
```

引擎名：`_mouse_shape_exit`

Called when the mouse pointer exits any of this object's shapes. `shapeIdx` is the child index of the exited [`Shape2D`](Shape2D.md). Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be called.

## 事件（信号）

### InputEvent

```csharp
public event CollisionObject2D.InputEventEventHandler InputEvent
```

引擎名：`input_event`

Emitted when an input event occurs. Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be set. See `_InputEvent()` for details.

### MouseEntered

```csharp
public event Action MouseEntered
```

引擎名：`mouse_entered`

Emitted when the mouse pointer enters any of this object's shapes. Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be set. Note that moving between different shapes within a single `CollisionObject2D` won't cause this signal to be emitted.

**Note:** Due to the lack of continuous collision detection, this signal may not be emitted in the expected order if the mouse moves fast enough and the `CollisionObject2D`'s area is small. This signal may also not be emitted if another `CollisionObject2D` is overlapping the `CollisionObject2D` in question.

### MouseExited

```csharp
public event Action MouseExited
```

引擎名：`mouse_exited`

Emitted when the mouse pointer exits all this object's shapes. Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be set. Note that moving between different shapes within a single `CollisionObject2D` won't cause this signal to be emitted.

**Note:** Due to the lack of continuous collision detection, this signal may not be emitted in the expected order if the mouse moves fast enough and the `CollisionObject2D`'s area is small. This signal may also not be emitted if another `CollisionObject2D` is overlapping the `CollisionObject2D` in question.

### MouseShapeEntered

```csharp
public event CollisionObject2D.MouseShapeEnteredEventHandler MouseShapeEntered
```

引擎名：`mouse_shape_entered`

Emitted when the mouse pointer enters any of this object's shapes or moves from one shape to another. `shapeIdx` is the child index of the newly entered [`Shape2D`](Shape2D.md). Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be set.

### MouseShapeExited

```csharp
public event CollisionObject2D.MouseShapeExitedEventHandler MouseShapeExited
```

引擎名：`mouse_shape_exited`

Emitted when the mouse pointer exits any of this object's shapes. `shapeIdx` is the child index of the exited [`Shape2D`](Shape2D.md). Requires `InputPickable` to be `true` and at least one `CollisionLayer` bit to be set.

## DisableModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Remove = 0`：When [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md), remove from the physics simulation to stop all physics interactions with this [`CollisionObject2D`](CollisionObject2D.md).
- `MakeStatic = 1`：When [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md), make the body static.
- `KeepActive = 2`：When [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md), do not affect the physics simulation.

## InputEventEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CollisionObject2D.InputEvent`](CollisionObject2D.md) event of a [`CollisionObject2D`](CollisionObject2D.md) class.

### 签名

#### Invoke

```csharp
delegate void InputEventEventHandler(Node viewport, InputEvent event, long shapeIdx)
```

## MouseShapeEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CollisionObject2D.MouseShapeEntered`](CollisionObject2D.md) event of a [`CollisionObject2D`](CollisionObject2D.md) class.

### 签名

#### Invoke

```csharp
delegate void MouseShapeEnteredEventHandler(long shapeIdx)
```

## MouseShapeExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CollisionObject2D.MouseShapeExited`](CollisionObject2D.md) event of a [`CollisionObject2D`](CollisionObject2D.md) class.

### 签名

#### Invoke

```csharp
delegate void MouseShapeExitedEventHandler(long shapeIdx)
```
