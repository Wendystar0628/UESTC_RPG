<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CollisionObject3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Abstract base class for 3D physics objects. `CollisionObject3D` can hold any number of [`Shape3D`](Shape3D.md)s for collision. Each shape must be assigned to a *shape owner*. Shape owners are not nodes and do not appear in the editor, but are accessible through code using the `shape_owner_*` methods.

**Warning:** With a non-uniform scale, this node will likely not behave as expected. It is advised to keep its scale the same on all axes and adjust its collision shape(s) instead.

## 属性

### CollisionLayer

```csharp
public uint CollisionLayer { get; set; }
```

引擎名：`collision_layer`

The physics layers this CollisionObject3D **is in**. Collision objects can exist in one or more of 32 different layers. See also `CollisionMask`.

**Note:** Object A can detect a contact with object B only if object B is in any of the layers that object A scans. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The physics layers this CollisionObject3D **scans**. Collision objects can scan one or more of 32 different layers. See also `CollisionLayer`.

**Note:** Object A can detect a contact with object B only if object B is in any of the layers that object A scans. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionPriority

```csharp
public float CollisionPriority { get; set; }
```

引擎名：`collision_priority`

The priority used to solve colliding when occurring penetration. The higher the priority is, the lower the penetration into the object will be. This can for example be used to prevent the player from breaking through the boundaries of a level.

### DisableMode

```csharp
public CollisionObject3D.DisableModeEnum DisableMode { get; set; }
```

引擎名：`disable_mode`

Defines the behavior in physics when [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md).

### InputCaptureOnDrag

```csharp
public bool InputCaptureOnDrag { get; set; }
```

引擎名：`input_capture_on_drag`

If `true`, the `CollisionObject3D` will continue to receive input events as the mouse is dragged across its shapes.

### InputRayPickable

```csharp
public bool InputRayPickable { get; set; }
```

引擎名：`input_ray_pickable`

If `true`, this object is pickable. A pickable object can detect the mouse pointer entering/leaving, and if the mouse is inside it, report input events. Requires at least one `CollisionLayer` bit to be set.

## 方法

### CreateShapeOwner

```csharp
public uint CreateShapeOwner(GodotObject owner)
```

引擎名：`create_shape_owner`

Creates a new shape owner for the given object. Returns `owner_id` of the new owner for future reference.

### GetCaptureInputOnDrag

```csharp
public bool GetCaptureInputOnDrag()
```

引擎名：`get_capture_input_on_drag`

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
public CollisionObject3D.DisableModeEnum GetDisableMode()
```

引擎名：`get_disable_mode`

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the object's [`Rid`](Rid.md).

### GetShapeOwners

```csharp
public int[] GetShapeOwners()
```

引擎名：`get_shape_owners`

Returns an [`Collections.Array`](Collections.Array.md) of `owner_id` identifiers. You can use these ids in other methods that take `owner_id` as an argument.

### IsRayPickable

```csharp
public bool IsRayPickable()
```

引擎名：`is_ray_pickable`

### IsShapeOwnerDisabled

```csharp
public bool IsShapeOwnerDisabled(uint ownerId)
```

引擎名：`is_shape_owner_disabled`

If `true`, the shape owner and its shapes are disabled.

### RemoveShapeOwner

```csharp
public void RemoveShapeOwner(uint ownerId)
```

引擎名：`remove_shape_owner`

Removes the given shape owner.

### SetCaptureInputOnDrag

```csharp
public void SetCaptureInputOnDrag(bool enable)
```

引擎名：`set_capture_input_on_drag`

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
public void SetDisableMode(CollisionObject3D.DisableModeEnum mode)
```

引擎名：`set_disable_mode`

### SetRayPickable

```csharp
public void SetRayPickable(bool rayPickable)
```

引擎名：`set_ray_pickable`

### ShapeFindOwner

```csharp
public uint ShapeFindOwner(int shapeIndex)
```

引擎名：`shape_find_owner`

Returns the `owner_id` of the given shape.

### ShapeOwnerAddShape

```csharp
public void ShapeOwnerAddShape(uint ownerId, Shape3D shape)
```

引擎名：`shape_owner_add_shape`

Adds a [`Shape3D`](Shape3D.md) to the shape owner.

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
public Shape3D ShapeOwnerGetShape(uint ownerId, int shapeId)
```

引擎名：`shape_owner_get_shape`

Returns the [`Shape3D`](Shape3D.md) with the given ID from the given shape owner.

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

Returns the child index of the [`Shape3D`](Shape3D.md) with the given ID from the given shape owner.

### ShapeOwnerGetTransform

```csharp
public Transform3D ShapeOwnerGetTransform(uint ownerId)
```

引擎名：`shape_owner_get_transform`

Returns the shape owner's [`Transform3D`](Transform3D.md).

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

### ShapeOwnerSetTransform

```csharp
public void ShapeOwnerSetTransform(uint ownerId, Transform3D transform)
```

引擎名：`shape_owner_set_transform`

Sets the [`Transform3D`](Transform3D.md) of the given shape owner.

### _InputEvent

```csharp
public virtual void _InputEvent(Camera3D camera, InputEvent event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
```

引擎名：`_input_event`

Receives unhandled [`InputEvent`](InputEvent.md)s. `eventPosition` is the location in world space of the mouse pointer on the surface of the shape with index `shapeIdx` and `normal` is the normal vector of the surface at that point. Connect to the `InputEvent` signal to easily pick up these events.

**Note:** `_InputEvent()` requires `InputRayPickable` to be `true` and at least one `CollisionLayer` bit to be set.

### _MouseEnter

```csharp
public virtual void _MouseEnter()
```

引擎名：`_mouse_enter`

Called when the mouse pointer enters any of this object's shapes. Requires `InputRayPickable` to be `true` and at least one `CollisionLayer` bit to be set. Note that moving between different shapes within a single `CollisionObject3D` won't cause this function to be called.

### _MouseExit

```csharp
public virtual void _MouseExit()
```

引擎名：`_mouse_exit`

Called when the mouse pointer exits all this object's shapes. Requires `InputRayPickable` to be `true` and at least one `CollisionLayer` bit to be set. Note that moving between different shapes within a single `CollisionObject3D` won't cause this function to be called.

## 事件（信号）

### InputEvent

```csharp
public event CollisionObject3D.InputEventEventHandler InputEvent
```

引擎名：`input_event`

Emitted when the object receives an unhandled [`InputEvent`](InputEvent.md). `eventPosition` is the location in world space of the mouse pointer on the surface of the shape with index `shapeIdx` and `normal` is the normal vector of the surface at that point.

### MouseEntered

```csharp
public event Action MouseEntered
```

引擎名：`mouse_entered`

Emitted when the mouse pointer enters any of this object's shapes. Requires `InputRayPickable` to be `true` and at least one `CollisionLayer` bit to be set.

**Note:** Due to the lack of continuous collision detection, this signal may not be emitted in the expected order if the mouse moves fast enough and the `CollisionObject3D`'s area is small. This signal may also not be emitted if another `CollisionObject3D` is overlapping the `CollisionObject3D` in question.

### MouseExited

```csharp
public event Action MouseExited
```

引擎名：`mouse_exited`

Emitted when the mouse pointer exits all this object's shapes. Requires `InputRayPickable` to be `true` and at least one `CollisionLayer` bit to be set.

**Note:** Due to the lack of continuous collision detection, this signal may not be emitted in the expected order if the mouse moves fast enough and the `CollisionObject3D`'s area is small. This signal may also not be emitted if another `CollisionObject3D` is overlapping the `CollisionObject3D` in question.

## DisableModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Remove = 0`：When [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md), remove from the physics simulation to stop all physics interactions with this [`CollisionObject3D`](CollisionObject3D.md).
- `MakeStatic = 1`：When [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md), make the body static.
- `KeepActive = 2`：When [`Node.ProcessMode`](Node.md) is set to [`Node.ProcessModeEnum.Disabled`](Node.md), do not affect the physics simulation.

## InputEventEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CollisionObject3D.InputEvent`](CollisionObject3D.md) event of a [`CollisionObject3D`](CollisionObject3D.md) class.

### 签名

#### Invoke

```csharp
delegate void InputEventEventHandler(Node camera, InputEvent event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
```
