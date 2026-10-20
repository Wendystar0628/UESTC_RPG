<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModifier3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`SkeletonModifier3D` retrieves a target [`Skeleton3D`](Skeleton3D.md) by having a [`Skeleton3D`](Skeleton3D.md) parent.

If there is an [`AnimationMixer`](AnimationMixer.md), a modification always performs after playback process of the [`AnimationMixer`](AnimationMixer.md).

This node should be used to implement custom IK solvers, constraints, or skeleton physics.

## 构造函数

### SkeletonModifier3D

```csharp
public SkeletonModifier3D()
```

## 属性

### Active

```csharp
public bool Active { get; set; }
```

引擎名：`active`

If `true`, the `SkeletonModifier3D` will be processing.

### Influence

```csharp
public float Influence { get; set; }
```

引擎名：`influence`

Sets the influence of the modification.

**Note:** This value is used by [`Skeleton3D`](Skeleton3D.md) to blend, so the `SkeletonModifier3D` should always apply only 100% of the result without interpolation.

## 方法

### GetInfluence

```csharp
public float GetInfluence()
```

引擎名：`get_influence`

### GetSkeleton

```csharp
public Skeleton3D GetSkeleton()
```

引擎名：`get_skeleton`

Returns the parent [`Skeleton3D`](Skeleton3D.md) node if it exists. Otherwise, returns `null`.

### IsActive

```csharp
public bool IsActive()
```

引擎名：`is_active`

### SetActive

```csharp
public void SetActive(bool active)
```

引擎名：`set_active`

### SetInfluence

```csharp
public void SetInfluence(float influence)
```

引擎名：`set_influence`

### _ProcessModification

```csharp
public virtual void _ProcessModification()
```

引擎名：`_process_modification`

Override this virtual method to implement a custom skeleton modifier. You should do things like get the [`Skeleton3D`](Skeleton3D.md)'s current pose and apply the pose here.

`_ProcessModification()` must not apply `Influence` to bone poses because the [`Skeleton3D`](Skeleton3D.md) automatically applies influence to all bone poses set by the modifier.

### _ProcessModificationWithDelta

```csharp
public virtual void _ProcessModificationWithDelta(double delta)
```

引擎名：`_process_modification_with_delta`

Override this virtual method to implement a custom skeleton modifier. You should do things like get the [`Skeleton3D`](Skeleton3D.md)'s current pose and apply the pose here.

`_ProcessModificationWithDelta()` must not apply `Influence` to bone poses because the [`Skeleton3D`](Skeleton3D.md) automatically applies influence to all bone poses set by the modifier.

`delta` is passed from parent [`Skeleton3D`](Skeleton3D.md). See also [`Skeleton3D.Advance`](Skeleton3D.md).

**Note:** This method may be called outside [`Node._Process()`](Node.md) and [`Node._PhysicsProcess()`](Node.md) with `delta` is `0.0`, since the modification should be processed immediately after initialization of the [`Skeleton3D`](Skeleton3D.md).

### _SkeletonChanged

```csharp
public virtual void _SkeletonChanged(Skeleton3D oldSkeleton, Skeleton3D newSkeleton)
```

引擎名：`_skeleton_changed`

Called when the skeleton is changed.

### _ValidateBoneNames

```csharp
public virtual void _ValidateBoneNames()
```

引擎名：`_validate_bone_names`

Called when bone names and indices need to be validated, such as when entering the scene tree or changing skeleton.

## 事件（信号）

### ModificationProcessed

```csharp
public event Action ModificationProcessed
```

引擎名：`modification_processed`

Notifies when the modification have been finished.

**Note:** If you want to get the modified bone pose by the modifier, you must use [`Skeleton3D.GetBonePose`](Skeleton3D.md) or [`Skeleton3D.GetBoneGlobalPose`](Skeleton3D.md) at the moment this signal is fired.

## BoneAxis

命名空间 `Godot`；enum

### 枚举值

- `PlusX = 0`：Enumerated value for the +X axis.
- `MinusX = 1`：Enumerated value for the -X axis.
- `PlusY = 2`：Enumerated value for the +Y axis.
- `MinusY = 3`：Enumerated value for the -Y axis.
- `PlusZ = 4`：Enumerated value for the +Z axis.
- `MinusZ = 5`：Enumerated value for the -Z axis.

## BoneDirection

命名空间 `Godot`；enum

### 枚举值

- `PlusX = 0`：Enumerated value for the +X axis.
- `MinusX = 1`：Enumerated value for the -X axis.
- `PlusY = 2`：Enumerated value for the +Y axis.
- `MinusY = 3`：Enumerated value for the -Y axis.
- `PlusZ = 4`：Enumerated value for the +Z axis.
- `MinusZ = 5`：Enumerated value for the -Z axis.
- `FromParent = 6`：Enumerated value for the axis from a parent bone to the child bone.

## RotationAxis

命名空间 `Godot`；enum

### 枚举值

- `X = 0`：Enumerated value for the rotation of the X axis.
- `Y = 1`：Enumerated value for the rotation of the Y axis.
- `Z = 2`：Enumerated value for the rotation of the Z axis.
- `All = 3`：Enumerated value for the unconstrained rotation.
- `Custom = 4`：Enumerated value for an optional rotation axis.

## SecondaryDirection

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Enumerated value for the case when the axis is undefined.
- `PlusX = 1`：Enumerated value for the +X axis.
- `MinusX = 2`：Enumerated value for the -X axis.
- `PlusY = 3`：Enumerated value for the +Y axis.
- `MinusY = 4`：Enumerated value for the -Y axis.
- `PlusZ = 5`：Enumerated value for the +Z axis.
- `MinusZ = 6`：Enumerated value for the -Z axis.
- `Custom = 7`：Enumerated value for an optional axis.
