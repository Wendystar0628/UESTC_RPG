<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Node3D

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `Node3D` node is the base representation of a node in 3D space. All other 3D nodes inherit from this class.

Affine operations (translation, rotation, scale) are calculated in the coordinate system relative to the parent, unless the `Node3D`'s `TopLevel` is `true`. In this coordinate system, affine operations correspond to direct affine operations on the `Node3D`'s `Transform`. The term *parent space* refers to this coordinate system. The coordinate system that is attached to the `Node3D` itself is referred to as object-local coordinate system, or *local space*.

**Note:** Unless otherwise specified, all methods that need angle parameters must receive angles in *radians*. To convert degrees to radians, use `Mathf.DegToRad`.

**Note:** In Godot 3 and older, `Node3D` was named *Spatial*.

## 构造函数

### Node3D

```csharp
public Node3D()
```

## 属性

### Basis

```csharp
public Basis Basis { get; set; }
```

引擎名：`basis`

Basis of the `Transform` property. Represents the rotation, scale, and shear of this node in parent space (relative to the parent node).

### GlobalBasis

```csharp
public Basis GlobalBasis { get; set; }
```

引擎名：`global_basis`

Basis of the `GlobalTransform` property. Represents the rotation, scale, and shear of this node in global space (relative to the world).

**Note:** If the node is not inside the tree, getting this property fails and returns `Basis.Identity`.

### GlobalPosition

```csharp
public Vector3 GlobalPosition { get; set; }
```

引擎名：`global_position`

Global position (translation) of this node in global space (relative to the world). This is equivalent to the `GlobalTransform`'s `Transform3D.Origin`.

**Note:** If the node is not inside the tree, getting this property fails and returns `Vector3.Zero`.

### GlobalRotation

```csharp
public Vector3 GlobalRotation { get; set; }
```

引擎名：`global_rotation`

Global rotation of this node as [Euler angles](https://en.wikipedia.org/wiki/Euler_angles), in radians and in global space (relative to the world). This value is obtained from `GlobalBasis`'s rotation.

- The `Vector3.X` is the angle around the global X axis (pitch);

- The `Vector3.Y` is the angle around the global Y axis (yaw);

- The `Vector3.Z` is the angle around the global Z axis (roll).

**Note:** Unlike `Rotation`, this property always follows the YXZ convention ([`EulerOrder.Yxz`](EulerOrder.md)).

**Note:** If the node is not inside the tree, getting this property fails and returns `Vector3.Zero`.

### GlobalRotationDegrees

```csharp
public Vector3 GlobalRotationDegrees { get; set; }
```

引擎名：`global_rotation_degrees`

The `GlobalRotation` of this node, in degrees instead of radians.

**Note:** If the node is not inside the tree, getting this property fails and returns `Vector3.Zero`.

### GlobalTransform

```csharp
public Transform3D GlobalTransform { get; set; }
```

引擎名：`global_transform`

The transformation of this node, in global space (relative to the world). Contains and represents this node's `GlobalPosition`, `GlobalRotation`, and global scale.

**Note:** If the node is not inside the tree, getting this property fails and returns `Transform3D.Identity`.

### Position

```csharp
public Vector3 Position { get; set; }
```

引擎名：`position`

Position (translation) of this node in parent space (relative to the parent node). This is equivalent to the `Transform`'s `Transform3D.Origin`.

### Quaternion

```csharp
public Quaternion Quaternion { get; set; }
```

引擎名：`quaternion`

Rotation of this node represented as a [`Quaternion`](Quaternion.md) in parent space (relative to the parent node). This value is obtained from `Basis`'s rotation.

**Note:** Quaternions are much more suitable for 3D math but are less intuitive. Setting this property can be useful for interpolation (see `Quaternion.Slerp`).

### Rotation

```csharp
public Vector3 Rotation { get; set; }
```

引擎名：`rotation`

Rotation of this node as [Euler angles](https://en.wikipedia.org/wiki/Euler_angles), in radians and in parent space (relative to the parent node). This value is obtained from `Basis`'s rotation.

- The `Vector3.X` is the angle around the local X axis (pitch);

- The `Vector3.Y` is the angle around the local Y axis (yaw);

- The `Vector3.Z` is the angle around the local Z axis (roll).

The order of each consecutive rotation can be changed with `RotationOrder` (see [`EulerOrder`](EulerOrder.md) constants). In Godot, Euler angles always use intrinsic order. By default, the intrinsic YXZ convention is used ([`EulerOrder.Yxz`](EulerOrder.md)).

**Note:** This property is edited in degrees in the inspector. If you want to use degrees in a script, use `RotationDegrees`.

### RotationDegrees

```csharp
public Vector3 RotationDegrees { get; set; }
```

引擎名：`rotation_degrees`

The `Rotation` of this node, in degrees instead of radians.

**Note:** This is **not** the property available in the Inspector dock.

### RotationEditMode

```csharp
public Node3D.RotationEditModeEnum RotationEditMode { get; set; }
```

引擎名：`rotation_edit_mode`

How this node's rotation and scale are displayed in the Inspector dock.

### RotationOrder

```csharp
public EulerOrder RotationOrder { get; set; }
```

引擎名：`rotation_order`

The axis rotation order of the `Rotation` property. In Godot, Euler angles always use intrinsic order, meaning that the final orientation is calculated by rotating around the local axes in this order.

### Scale

```csharp
public Vector3 Scale { get; set; }
```

引擎名：`scale`

Scale of this node in local space (relative to this node). This value is obtained from `Basis`'s scale.

**Note:** The behavior of some 3D node types is not affected by this property. These include [`Light3D`](Light3D.md), [`Camera3D`](Camera3D.md), [`AudioStreamPlayer3D`](AudioStreamPlayer3D.md), and more.

**Warning:** The scale's components must either be all positive or all negative, and **not** exactly `0.0`. Otherwise, it won't be possible to obtain the scale from the `Basis`. This may cause the intended scale to be lost when reloaded from disk, and potentially other unstable behavior.

### TopLevel

```csharp
public bool TopLevel { get; set; }
```

引擎名：`top_level`

If `true`, the node does not inherit its transformations from its parent. As such, node transformations will only be in global space, which also means that `GlobalTransform` and `Transform` will be identical.

### Transform

```csharp
public Transform3D Transform { get; set; }
```

引擎名：`transform`

The local transformation of this node, in parent space (relative to the parent node). Contains and represents this node's `Position`, `Rotation`, and `Scale`.

### VisibilityParent

```csharp
public NodePath VisibilityParent { get; set; }
```

引擎名：`visibility_parent`

Path to the visibility range parent for this node and its descendants. The visibility parent must be a [`GeometryInstance3D`](GeometryInstance3D.md).

Any visual instance will only be visible if the visibility parent (and all of its visibility ancestors) is hidden by being closer to the camera than its own [`GeometryInstance3D.VisibilityRangeBegin`](GeometryInstance3D.md). Nodes hidden via the `Visible` property are essentially removed from the visibility dependency tree, so dependent instances will not take the hidden node or its descendants into account.

### Visible

```csharp
public bool Visible { get; set; }
```

引擎名：`visible`

If `true`, this node can be visible. The node is only rendered when all of its ancestors are visible, as well. That means `IsVisibleInTree()` must return `true`.

## 方法

### AddGizmo

```csharp
public void AddGizmo(Node3DGizmo gizmo)
```

引擎名：`add_gizmo`

Attaches the given `gizmo` to this node. Only works in the editor.

**Note:** `gizmo` should be an `EditorNode3DGizmo`. The argument type is [`Node3DGizmo`](Node3DGizmo.md) to avoid depending on editor classes in `Node3D`.

### ClearGizmos

```csharp
public void ClearGizmos()
```

引擎名：`clear_gizmos`

Clears all `EditorNode3DGizmo` objects attached to this node. Only works in the editor.

### ClearSubgizmoSelection

```csharp
public void ClearSubgizmoSelection()
```

引擎名：`clear_subgizmo_selection`

Deselects all subgizmos for this node. Useful to call when the selected subgizmo may no longer exist after a property change. Only works in the editor.

### ForceUpdateTransform

```csharp
public void ForceUpdateTransform()
```

引擎名：`force_update_transform`

Forces the node's `GlobalTransform` to update, by sending `NotificationTransformChanged`. Fails if the node is not inside the tree.

**Note:** For performance reasons, transform changes are usually accumulated and applied *once* at the end of the frame. The update propagates through `Node3D` children, as well. Therefore, use this method only when you need an up-to-date transform (such as during physics operations).

### GetBasis

```csharp
public Basis GetBasis()
```

引擎名：`get_basis`

### GetGizmos

```csharp
public Array<Node3DGizmo> GetGizmos()
```

引擎名：`get_gizmos`

Returns all the `EditorNode3DGizmo` objects attached to this node. Only works in the editor.

### GetGlobalBasis

```csharp
public Basis GetGlobalBasis()
```

引擎名：`get_global_basis`

### GetGlobalPosition

```csharp
public Vector3 GetGlobalPosition()
```

引擎名：`get_global_position`

### GetGlobalRotation

```csharp
public Vector3 GetGlobalRotation()
```

引擎名：`get_global_rotation`

### GetGlobalRotationDegrees

```csharp
public Vector3 GetGlobalRotationDegrees()
```

引擎名：`get_global_rotation_degrees`

### GetGlobalTransform

```csharp
public Transform3D GetGlobalTransform()
```

引擎名：`get_global_transform`

### GetGlobalTransformInterpolated

```csharp
public Transform3D GetGlobalTransformInterpolated()
```

引擎名：`get_global_transform_interpolated`

When using physics interpolation, there will be circumstances in which you want to know the interpolated (displayed) transform of a node rather than the standard transform (which may only be accurate to the most recent physics tick).

This is particularly important for frame-based operations that take place in [`Node._Process()`](Node.md), rather than [`Node._PhysicsProcess()`](Node.md). Examples include [`Camera3D`](Camera3D.md)s focusing on a node, or finding where to fire lasers from on a frame rather than physics tick.

**Note:** This function creates an interpolation pump on the `Node3D` the first time it is called, which can respond to physics interpolation resets. If you get problems with "streaking" when initially following a `Node3D`, be sure to call `GetGlobalTransformInterpolated()` at least once *before* resetting the `Node3D` physics interpolation.

### GetParentNode3D

```csharp
public Node3D GetParentNode3D()
```

Returns the parent `Node3D` that directly affects this node's `GlobalTransform`. Returns `null` if no parent exists, the parent is not a `Node3D`, or `TopLevel` is `true`.

**Note:** This method is not always equivalent to [`Node.GetParent`](Node.md), which does not take `TopLevel` into account.

### GetPosition

```csharp
public Vector3 GetPosition()
```

引擎名：`get_position`

### GetQuaternion

```csharp
public Quaternion GetQuaternion()
```

引擎名：`get_quaternion`

### GetRotation

```csharp
public Vector3 GetRotation()
```

引擎名：`get_rotation`

### GetRotationDegrees

```csharp
public Vector3 GetRotationDegrees()
```

引擎名：`get_rotation_degrees`

### GetRotationEditMode

```csharp
public Node3D.RotationEditModeEnum GetRotationEditMode()
```

引擎名：`get_rotation_edit_mode`

### GetRotationOrder

```csharp
public EulerOrder GetRotationOrder()
```

引擎名：`get_rotation_order`

### GetScale

```csharp
public Vector3 GetScale()
```

引擎名：`get_scale`

### GetTransform

```csharp
public Transform3D GetTransform()
```

引擎名：`get_transform`

### GetVisibilityParent

```csharp
public NodePath GetVisibilityParent()
```

引擎名：`get_visibility_parent`

### GetWorld3D

```csharp
public World3D GetWorld3D()
```

Returns the [`World3D`](World3D.md) this node is registered to.

Usually, this is the same as the world used by this node's viewport (see [`Node.GetViewport`](Node.md) and [`Viewport.FindWorld3D`](Viewport.md)).

### GlobalRotate

```csharp
public void GlobalRotate(Vector3 axis, float angle)
```

引擎名：`global_rotate`

Rotates this node's `GlobalBasis` around the global `axis` by the given `angle`, in radians. This operation is calculated in global space (relative to the world) and preserves the `GlobalPosition`.

### GlobalScale

```csharp
public void GlobalScale(Vector3 scale)
```

引擎名：`global_scale`

Scales this node's `GlobalBasis` by the given `scale` factor. This operation is calculated in global space (relative to the world) and preserves the `GlobalPosition`.

**Note:** This method is not to be confused with the `Scale` property.

### GlobalTranslate

```csharp
public void GlobalTranslate(Vector3 offset)
```

引擎名：`global_translate`

Adds the given translation `offset` to the node's `GlobalPosition` in global space (relative to the world).

### Hide

```csharp
public void Hide()
```

引擎名：`hide`

Prevents this node from being rendered. Equivalent to setting `Visible` to `false`. This is the opposite of `Show()`.

### IsLocalTransformNotificationEnabled

```csharp
public bool IsLocalTransformNotificationEnabled()
```

引擎名：`is_local_transform_notification_enabled`

Returns `true` if the node receives `NotificationLocalTransformChanged` whenever `Transform` changes. This is enabled with `SetNotifyLocalTransform()`.

### IsScaleDisabled

```csharp
public bool IsScaleDisabled()
```

引擎名：`is_scale_disabled`

Returns `true` if this node's `GlobalTransform` is automatically orthonormalized. This results in this node not appearing distorted, as if its global scale were set to `Vector3.One` (or its negative counterpart). See also `SetDisableScale()` and `Orthonormalize()`.

**Note:** `Transform` is not affected by this setting.

### IsSetAsTopLevel

```csharp
public bool IsSetAsTopLevel()
```

引擎名：`is_set_as_top_level`

### IsTransformNotificationEnabled

```csharp
public bool IsTransformNotificationEnabled()
```

引擎名：`is_transform_notification_enabled`

Returns `true` if the node receives `NotificationTransformChanged` whenever `GlobalTransform` changes. This is enabled with `SetNotifyTransform()`.

### IsVisible

```csharp
public bool IsVisible()
```

引擎名：`is_visible`

### IsVisibleInTree

```csharp
public bool IsVisibleInTree()
```

引擎名：`is_visible_in_tree`

Returns `true` if this node is inside the scene tree and the `Visible` property is `true` for this node and all of its `Node3D` ancestors *in sequence*. An ancestor of any other type (such as [`Node`](Node.md) or [`Node2D`](Node2D.md)) breaks the sequence. See also [`Node.GetParent`](Node.md).

**Note:** This method cannot take [`VisualInstance3D.Layers`](VisualInstance3D.md) into account, so even if this method returns `true`, the node may not be rendered.

### LookAt

```csharp
public void LookAt(Vector3 target, Vector3? up = null, bool useModelFront = false)
```

引擎名：`look_at`

Rotates the node so that the local forward axis (-Z, `Vector3.Forward`) points toward the `target` position. This operation is calculated in global space (relative to the world).

The local up axis (+Y) points as close to the `up` vector as possible while staying perpendicular to the local forward axis. The resulting transform is orthogonal, and the scale is preserved. Non-uniform scaling may not work correctly.

The `target` position cannot be the same as the node's position, the `up` vector cannot be `Vector3.Zero`. Furthermore, the direction from the node's position to the `target` position cannot be parallel to the `up` vector, to avoid an unintended rotation around the local Z axis.

If `useModelFront` is `true`, the +Z axis (asset front) is treated as forward (implies +X is left) and points toward the `target` position. By default, the -Z axis (camera forward) is treated as forward (implies +X is right).

**Note:** This method fails if the node is not in the scene tree. If necessary, use `LookAtFromPosition()` instead.

参数：
- `up`：If the parameter is null, then the default value is `new Vector3(0.0f, 1.0f, 0.0f)`.

### LookAt

```csharp
public void LookAt(Vector3 target, Vector3? up)
```

引擎名：`look_at`

Rotates the node so that the local forward axis (-Z, `Vector3.Forward`) points toward the `target` position. This operation is calculated in global space (relative to the world).

The local up axis (+Y) points as close to the `up` vector as possible while staying perpendicular to the local forward axis. The resulting transform is orthogonal, and the scale is preserved. Non-uniform scaling may not work correctly.

The `target` position cannot be the same as the node's position, the `up` vector cannot be `Vector3.Zero`. Furthermore, the direction from the node's position to the `target` position cannot be parallel to the `up` vector, to avoid an unintended rotation around the local Z axis.

If `useModelFront` is `true`, the +Z axis (asset front) is treated as forward (implies +X is left) and points toward the `target` position. By default, the -Z axis (camera forward) is treated as forward (implies +X is right).

**Note:** This method fails if the node is not in the scene tree. If necessary, use `LookAtFromPosition()` instead.

参数：
- `up`：If the parameter is null, then the default value is `new Vector3(0.0f, 1.0f, 0.0f)`.

### LookAtFromPosition

```csharp
public void LookAtFromPosition(Vector3 position, Vector3 target, Vector3? up = null, bool useModelFront = false)
```

引擎名：`look_at_from_position`

Moves the node to the specified `position`, then rotates the node to point toward the `target` position, similar to `LookAt()`. This operation is calculated in global space (relative to the world).

参数：
- `up`：If the parameter is null, then the default value is `new Vector3(0.0f, 1.0f, 0.0f)`.

### LookAtFromPosition

```csharp
public void LookAtFromPosition(Vector3 position, Vector3 target, Vector3? up)
```

引擎名：`look_at_from_position`

Moves the node to the specified `position`, then rotates the node to point toward the `target` position, similar to `LookAt()`. This operation is calculated in global space (relative to the world).

参数：
- `up`：If the parameter is null, then the default value is `new Vector3(0.0f, 1.0f, 0.0f)`.

### Orthonormalize

```csharp
public void Orthonormalize()
```

引擎名：`orthonormalize`

Orthonormalizes this node's `Basis`. This method sets this node's `Scale` to `Vector3.One` (or its negative counterpart), but preserves the `Position` and `Rotation`. See also `Transform3D.Orthonormalized`.

### Rotate

```csharp
public void Rotate(Vector3 axis, float angle)
```

引擎名：`rotate`

Rotates this node's `Basis` around the `axis` by the given `angle`, in radians. This operation is calculated in parent space (relative to the parent) and preserves the `Position`.

### RotateObjectLocal

```csharp
public void RotateObjectLocal(Vector3 axis, float angle)
```

引擎名：`rotate_object_local`

Rotates this node's `Basis` around the `axis` by the given `angle`, in radians. This operation is calculated in local space (relative to this node) and preserves the `Position`.

### RotateX

```csharp
public void RotateX(float angle)
```

引擎名：`rotate_x`

Rotates this node's `Basis` around the X axis by the given `angle`, in radians. This operation is calculated in parent space (relative to the parent) and preserves the `Position`.

### RotateY

```csharp
public void RotateY(float angle)
```

引擎名：`rotate_y`

Rotates this node's `Basis` around the Y axis by the given `angle`, in radians. This operation is calculated in parent space (relative to the parent) and preserves the `Position`.

### RotateZ

```csharp
public void RotateZ(float angle)
```

引擎名：`rotate_z`

Rotates this node's `Basis` around the Z axis by the given `angle`, in radians. This operation is calculated in parent space (relative to the parent) and preserves the `Position`.

### ScaleObjectLocal

```csharp
public void ScaleObjectLocal(Vector3 scale)
```

引擎名：`scale_object_local`

Scales this node's `Basis` by the given `scale` factor. This operation is calculated in local space (relative to this node) and preserves the `Position`.

### SetAsTopLevel

```csharp
public void SetAsTopLevel(bool enable)
```

引擎名：`set_as_top_level`

### SetBasis

```csharp
public void SetBasis(Basis basis)
```

引擎名：`set_basis`

### SetDisableScale

```csharp
public void SetDisableScale(bool disable)
```

引擎名：`set_disable_scale`

If `true`, this node's `GlobalTransform` is automatically orthonormalized. This results in this node not appearing distorted, as if its global scale were set to `Vector3.One` (or its negative counterpart). See also `IsScaleDisabled()` and `Orthonormalize()`.

**Note:** `Transform` is not affected by this setting.

### SetGlobalBasis

```csharp
public void SetGlobalBasis(Basis basis)
```

引擎名：`set_global_basis`

### SetGlobalPosition

```csharp
public void SetGlobalPosition(Vector3 position)
```

引擎名：`set_global_position`

### SetGlobalRotation

```csharp
public void SetGlobalRotation(Vector3 eulerRadians)
```

引擎名：`set_global_rotation`

### SetGlobalRotationDegrees

```csharp
public void SetGlobalRotationDegrees(Vector3 eulerDegrees)
```

引擎名：`set_global_rotation_degrees`

### SetGlobalTransform

```csharp
public void SetGlobalTransform(Transform3D global)
```

引擎名：`set_global_transform`

### SetIdentity

```csharp
public void SetIdentity()
```

引擎名：`set_identity`

Sets this node's `Transform` to `Transform3D.Identity`, which resets all transformations in parent space (`Position`, `Rotation`, and `Scale`).

### SetIgnoreTransformNotification

```csharp
public void SetIgnoreTransformNotification(bool enabled)
```

引擎名：`set_ignore_transform_notification`

If `true`, the node will not receive `NotificationTransformChanged` or `NotificationLocalTransformChanged`.

It may useful to call this method when handling these notifications to prevent infinite recursion.

### SetNotifyLocalTransform

```csharp
public void SetNotifyLocalTransform(bool enable)
```

引擎名：`set_notify_local_transform`

If `true`, the node will receive `NotificationLocalTransformChanged` whenever `Transform` changes.

**Note:** Some 3D nodes such as [`CsgShape3D`](CsgShape3D.md) or [`CollisionShape3D`](CollisionShape3D.md) automatically enable this to function correctly.

### SetNotifyTransform

```csharp
public void SetNotifyTransform(bool enable)
```

引擎名：`set_notify_transform`

If `true`, the node will receive `NotificationTransformChanged` whenever `GlobalTransform` changes.

**Note:** Most 3D nodes such as [`VisualInstance3D`](VisualInstance3D.md) or [`CollisionObject3D`](CollisionObject3D.md) automatically enable this to function correctly.

**Note:** In the editor, nodes will propagate this notification to their children if a gizmo is attached (see `AddGizmo()`).

### SetPosition

```csharp
public void SetPosition(Vector3 position)
```

引擎名：`set_position`

### SetQuaternion

```csharp
public void SetQuaternion(Quaternion quaternion)
```

引擎名：`set_quaternion`

### SetRotation

```csharp
public void SetRotation(Vector3 eulerRadians)
```

引擎名：`set_rotation`

### SetRotationDegrees

```csharp
public void SetRotationDegrees(Vector3 eulerDegrees)
```

引擎名：`set_rotation_degrees`

### SetRotationEditMode

```csharp
public void SetRotationEditMode(Node3D.RotationEditModeEnum editMode)
```

引擎名：`set_rotation_edit_mode`

### SetRotationOrder

```csharp
public void SetRotationOrder(EulerOrder order)
```

引擎名：`set_rotation_order`

### SetScale

```csharp
public void SetScale(Vector3 scale)
```

引擎名：`set_scale`

### SetSubgizmoSelection

```csharp
public void SetSubgizmoSelection(Node3DGizmo gizmo, int id, Transform3D transform)
```

引擎名：`set_subgizmo_selection`

Selects the `gizmo`'s subgizmo with the given `id` and sets its transform. Only works in the editor.

**Note:** The gizmo object would typically be an instance of `EditorNode3DGizmo`, but the argument type is kept generic to avoid creating a dependency on editor classes in `Node3D`.

### SetTransform

```csharp
public void SetTransform(Transform3D local)
```

引擎名：`set_transform`

### SetVisibilityParent

```csharp
public void SetVisibilityParent(NodePath path)
```

引擎名：`set_visibility_parent`

### SetVisible

```csharp
public void SetVisible(bool visible)
```

引擎名：`set_visible`

### Show

```csharp
public void Show()
```

引擎名：`show`

Allows this node to be rendered. Equivalent to setting `Visible` to `true`. This is the opposite of `Hide()`.

### ToGlobal

```csharp
public Vector3 ToGlobal(Vector3 localPoint)
```

引擎名：`to_global`

Returns the `localPoint` converted from this node's local space to global space. This is the opposite of `ToLocal()`.

### ToLocal

```csharp
public Vector3 ToLocal(Vector3 globalPoint)
```

引擎名：`to_local`

Returns the `globalPoint` converted from global space to this node's local space. This is the opposite of `ToGlobal()`.

### Translate

```csharp
public void Translate(Vector3 offset)
```

引擎名：`translate`

Adds the given translation `offset` to the node's position, in local space (relative to this node).

**Note:** Prefer using `TranslateObjectLocal()`, instead, as this method may be changed in a future release.

**Note:** Despite the naming convention, this operation is **not** calculated in parent space for compatibility reasons. To translate in parent space, add `offset` to the `Position` (`node_3d.position += offset`).

### TranslateObjectLocal

```csharp
public void TranslateObjectLocal(Vector3 offset)
```

引擎名：`translate_object_local`

Adds the given translation `offset` to the node's position, in local space (relative to this node).

### UpdateGizmos

```csharp
public void UpdateGizmos()
```

引擎名：`update_gizmos`

Updates all the `EditorNode3DGizmo` objects attached to this node. Only works in the editor.

## 事件（信号）

### VisibilityChanged

```csharp
public event Action VisibilityChanged
```

引擎名：`visibility_changed`

Emitted when this node's visibility changes (see `Visible` and `IsVisibleInTree()`).

This signal is emitted *after* the related `NotificationVisibilityChanged` notification.

## 常量

### NotificationEnterWorld

```csharp
public const long NotificationEnterWorld = 41
```

Notification received when this node is registered to a new [`World3D`](World3D.md) (see `GetWorld3D()`).

### NotificationExitWorld

```csharp
public const long NotificationExitWorld = 42
```

Notification received when this node is unregistered from the current [`World3D`](World3D.md) (see `GetWorld3D()`).

This notification is sent in reversed order.

### NotificationLocalTransformChanged

```csharp
public const long NotificationLocalTransformChanged = 44
```

Notification received when this node's `Transform` changes, if `IsLocalTransformNotificationEnabled()` is `true`. This is not received when a parent `Node3D`'s `Transform` changes. See also `SetNotifyLocalTransform()`.

**Note:** Some 3D nodes such as [`CsgShape3D`](CsgShape3D.md) or [`CollisionShape3D`](CollisionShape3D.md) automatically enable this to function correctly.

### NotificationTransformChanged

```csharp
public const long NotificationTransformChanged = 2000
```

Notification received when this node's `GlobalTransform` changes, if `IsTransformNotificationEnabled()` is `true`. See also `SetNotifyTransform()`.

**Note:** Most 3D nodes such as [`VisualInstance3D`](VisualInstance3D.md) or [`CollisionObject3D`](CollisionObject3D.md) automatically enable this to function correctly.

**Note:** In the editor, nodes will propagate this notification to their children if a gizmo is attached (see `AddGizmo()`).

### NotificationVisibilityChanged

```csharp
public const long NotificationVisibilityChanged = 43
```

Notification received when this node's visibility changes (see `Visible` and `IsVisibleInTree()`).

This notification is received *before* the related `VisibilityChanged` signal.

## RotationEditModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Euler = 0`：The rotation is edited using a [`Vector3`](Vector3.md) in [Euler angles](https://en.wikipedia.org/wiki/Euler_angles).
- `Quaternion = 1`：The rotation is edited using a [`Quaternion`](Quaternion.md).
- `Basis = 2`：The rotation is edited using a [`Basis`](Basis.md).
