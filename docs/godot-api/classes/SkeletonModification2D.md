<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModification2D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource provides an interface that can be expanded so code that operates on [`Bone2D`](Bone2D.md) nodes in a [`Skeleton2D`](Skeleton2D.md) can be mixed and matched together to create complex interactions.

This is used to provide Godot with a flexible and powerful Inverse Kinematics solution that can be adapted for many different uses.

## 构造函数

### SkeletonModification2D

```csharp
public SkeletonModification2D()
```

## 属性

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

If `true`, the modification's `_Execute()` function will be called by the [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md).

### ExecutionMode

```csharp
public int ExecutionMode { get; set; }
```

引擎名：`execution_mode`

The execution mode for the modification. This tells the modification stack when to execute the modification. Some modifications have settings that are only available in certain execution modes.

## 方法

### ClampAngle

```csharp
public float ClampAngle(float angle, float min, float max, bool invert)
```

引擎名：`clamp_angle`

Takes an angle and clamps it so it is within the passed-in `min` and `max` range. `invert` will inversely clamp the angle, clamping it to the range outside of the given bounds.

### GetEditorDrawGizmo

```csharp
public bool GetEditorDrawGizmo()
```

引擎名：`get_editor_draw_gizmo`

Returns whether this modification will call `_DrawEditorGizmo()` in the Godot editor to draw modification-specific gizmos.

### GetEnabled

```csharp
public bool GetEnabled()
```

引擎名：`get_enabled`

### GetExecutionMode

```csharp
public int GetExecutionMode()
```

引擎名：`get_execution_mode`

### GetIsSetup

```csharp
public bool GetIsSetup()
```

引擎名：`get_is_setup`

Returns whether this modification has been successfully setup or not.

### GetModificationStack

```csharp
public SkeletonModificationStack2D GetModificationStack()
```

引擎名：`get_modification_stack`

Returns the [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md) that this modification is bound to. Through the modification stack, you can access the Skeleton2D the modification is operating on.

### SetEditorDrawGizmo

```csharp
public void SetEditorDrawGizmo(bool drawGizmo)
```

引擎名：`set_editor_draw_gizmo`

Sets whether this modification will call `_DrawEditorGizmo()` in the Godot editor to draw modification-specific gizmos.

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetExecutionMode

```csharp
public void SetExecutionMode(int executionMode)
```

引擎名：`set_execution_mode`

### SetIsSetup

```csharp
public void SetIsSetup(bool isSetup)
```

引擎名：`set_is_setup`

Manually allows you to set the setup state of the modification. This function should only rarely be used, as the [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md) the modification is bound to should handle setting the modification up.

### _DrawEditorGizmo

```csharp
public virtual void _DrawEditorGizmo()
```

引擎名：`_draw_editor_gizmo`

Used for drawing **editor-only** modification gizmos. This function will only be called in the Godot editor and can be overridden to draw custom gizmos.

**Note:** You will need to use the Skeleton2D from [`SkeletonModificationStack2D.GetSkeleton`](SkeletonModificationStack2D.md) and it's draw functions, as the `SkeletonModification2D` resource cannot draw on its own.

### _Execute

```csharp
public virtual void _Execute(double delta)
```

引擎名：`_execute`

Executes the given modification. This is where the modification performs whatever function it is designed to do.

### _SetupModification

```csharp
public virtual void _SetupModification(SkeletonModificationStack2D modificationStack)
```

引擎名：`_setup_modification`

Called when the modification is setup. This is where the modification performs initialization.
