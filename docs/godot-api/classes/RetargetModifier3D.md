<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RetargetModifier3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Retrieves the pose (or global pose) relative to the parent Skeleton's rest in model space and transfers it to the child Skeleton.

This modifier rewrites the pose of the child skeleton directly in the parent skeleton's update process. This means that it overwrites the mapped bone pose set in the normal process on the target skeleton. If you want to set the target skeleton bone pose after retargeting, you will need to add a [`SkeletonModifier3D`](SkeletonModifier3D.md) child to the target skeleton and thereby modify the pose.

**Note:** When the `UseGlobalPose` is enabled, even if it is an unmapped bone, it can cause visual problems because the global pose is applied ignoring the parent bone's pose **if it has mapped bone children**. See also `UseGlobalPose`.

## 构造函数

### RetargetModifier3D

```csharp
public RetargetModifier3D()
```

## 属性

### Enable

```csharp
public RetargetModifier3D.TransformFlag Enable { get; set; }
```

引擎名：`enable`

Flags to control the process of the transform elements individually when `UseGlobalPose` is disabled.

### Profile

```csharp
public SkeletonProfile Profile { get; set; }
```

引擎名：`profile`

[`SkeletonProfile`](SkeletonProfile.md) for retargeting bones with names matching the bone list.

### UseGlobalPose

```csharp
public bool UseGlobalPose { get; set; }
```

引擎名：`use_global_pose`

If `false`, in case the target skeleton has fewer bones than the source skeleton, the source bone parent's transform will be ignored.

Instead, it is possible to retarget between models with different body shapes, and position, rotation, and scale can be retargeted separately.

If `true`, retargeting is performed taking into account global pose.

In case the target skeleton has fewer bones than the source skeleton, the source bone parent's transform is taken into account. However, bone length between skeletons must match exactly, if not, the bones will be forced to expand or shrink.

This is useful for using dummy bone with length `0` to match postures when retargeting between models with different number of bones.

## 方法

### GetEnableFlags

```csharp
public RetargetModifier3D.TransformFlag GetEnableFlags()
```

引擎名：`get_enable_flags`

### GetProfile

```csharp
public SkeletonProfile GetProfile()
```

引擎名：`get_profile`

### IsPositionEnabled

```csharp
public bool IsPositionEnabled()
```

引擎名：`is_position_enabled`

Returns `true` if `Enable` has [`RetargetModifier3D.TransformFlag.Position`](RetargetModifier3D.md).

### IsRotationEnabled

```csharp
public bool IsRotationEnabled()
```

引擎名：`is_rotation_enabled`

Returns `true` if `Enable` has [`RetargetModifier3D.TransformFlag.Rotation`](RetargetModifier3D.md).

### IsScaleEnabled

```csharp
public bool IsScaleEnabled()
```

引擎名：`is_scale_enabled`

Returns `true` if `Enable` has [`RetargetModifier3D.TransformFlag.Scale`](RetargetModifier3D.md).

### IsUsingGlobalPose

```csharp
public bool IsUsingGlobalPose()
```

引擎名：`is_using_global_pose`

### SetEnableFlags

```csharp
public void SetEnableFlags(RetargetModifier3D.TransformFlag enableFlags)
```

引擎名：`set_enable_flags`

### SetPositionEnabled

```csharp
public void SetPositionEnabled(bool enabled)
```

引擎名：`set_position_enabled`

Sets [`RetargetModifier3D.TransformFlag.Position`](RetargetModifier3D.md) into `Enable`.

### SetProfile

```csharp
public void SetProfile(SkeletonProfile profile)
```

引擎名：`set_profile`

### SetRotationEnabled

```csharp
public void SetRotationEnabled(bool enabled)
```

引擎名：`set_rotation_enabled`

Sets [`RetargetModifier3D.TransformFlag.Rotation`](RetargetModifier3D.md) into `Enable`.

### SetScaleEnabled

```csharp
public void SetScaleEnabled(bool enabled)
```

引擎名：`set_scale_enabled`

Sets [`RetargetModifier3D.TransformFlag.Scale`](RetargetModifier3D.md) into `Enable`.

### SetUseGlobalPose

```csharp
public void SetUseGlobalPose(bool useGlobalPose)
```

引擎名：`set_use_global_pose`

## TransformFlag

命名空间 `Godot`；enum

### 枚举值

- `Position = 1`：If set, allows to retarget the position.
- `Rotation = 2`：If set, allows to retarget the rotation.
- `Scale = 4`：If set, allows to retarget the scale.
- `All = 7`：If set, allows to retarget the position/rotation/scale.
