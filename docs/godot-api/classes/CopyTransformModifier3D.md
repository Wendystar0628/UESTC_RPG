<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CopyTransformModifier3D

命名空间 `Godot`；class；继承 [`BoneConstraint3D`](BoneConstraint3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Apply the copied transform of the bone set by [`BoneConstraint3D.SetReferenceBone`](BoneConstraint3D.md) to the bone set by [`BoneConstraint3D.SetApplyBone`](BoneConstraint3D.md) with processing it with some masks and options.

There are 4 ways to apply the transform, depending on the combination of `SetRelative()` and `SetAdditive()`.

**Relative + Additive:**

- Extract reference pose relative to the rest and add it to the apply bone's pose.

**Relative + Not Additive:**

- Extract reference pose relative to the rest and add it to the apply bone's rest.

**Not Relative + Additive:**

- Extract reference pose absolutely and add it to the apply bone's pose.

**Not Relative + Not Additive:**

- Extract reference pose absolutely and the apply bone's pose is replaced with it.

**Note:** Relative option is available only in the case [`BoneConstraint3D.GetReferenceType`](BoneConstraint3D.md) is [`BoneConstraint3D.ReferenceType.Bone`](BoneConstraint3D.md). See also [`BoneConstraint3D.ReferenceType`](BoneConstraint3D.md).

## 构造函数

### CopyTransformModifier3D

```csharp
public CopyTransformModifier3D()
```

## 属性

### SettingCount

```csharp
public int SettingCount { get; set; }
```

引擎名：`setting_count`

The number of settings in the modifier.

## 方法

### GetAxisFlags

```csharp
public CopyTransformModifier3D.AxisFlag GetAxisFlags(int index)
```

引擎名：`get_axis_flags`

Returns the axis flags of the setting at `index`.

### GetCopyFlags

```csharp
public CopyTransformModifier3D.TransformFlag GetCopyFlags(int index)
```

引擎名：`get_copy_flags`

Returns the copy flags of the setting at `index`.

### GetInvertFlags

```csharp
public CopyTransformModifier3D.AxisFlag GetInvertFlags(int index)
```

引擎名：`get_invert_flags`

Returns the invert flags of the setting at `index`.

### IsAdditive

```csharp
public bool IsAdditive(int index)
```

引擎名：`is_additive`

Returns `true` if the additive option is enabled in the setting at `index`.

### IsAxisXEnabled

```csharp
public bool IsAxisXEnabled(int index)
```

引擎名：`is_axis_x_enabled`

Returns `true` if the enable flags has the flag for the X-axis in the setting at `index`. See also `SetAxisFlags()`.

### IsAxisXInverted

```csharp
public bool IsAxisXInverted(int index)
```

引擎名：`is_axis_x_inverted`

Returns `true` if the invert flags has the flag for the X-axis in the setting at `index`. See also `SetInvertFlags()`.

### IsAxisYEnabled

```csharp
public bool IsAxisYEnabled(int index)
```

引擎名：`is_axis_y_enabled`

Returns `true` if the enable flags has the flag for the Y-axis in the setting at `index`. See also `SetAxisFlags()`.

### IsAxisYInverted

```csharp
public bool IsAxisYInverted(int index)
```

引擎名：`is_axis_y_inverted`

Returns `true` if the invert flags has the flag for the Y-axis in the setting at `index`. See also `SetInvertFlags()`.

### IsAxisZEnabled

```csharp
public bool IsAxisZEnabled(int index)
```

引擎名：`is_axis_z_enabled`

Returns `true` if the enable flags has the flag for the Z-axis in the setting at `index`. See also `SetAxisFlags()`.

### IsAxisZInverted

```csharp
public bool IsAxisZInverted(int index)
```

引擎名：`is_axis_z_inverted`

Returns `true` if the invert flags has the flag for the Z-axis in the setting at `index`. See also `SetInvertFlags()`.

### IsPositionCopying

```csharp
public bool IsPositionCopying(int index)
```

引擎名：`is_position_copying`

Returns `true` if the copy flags has the flag for the position in the setting at `index`. See also `SetCopyFlags()`.

### IsRelative

```csharp
public bool IsRelative(int index)
```

引擎名：`is_relative`

Returns `true` if the relative option is enabled in the setting at `index`.

### IsRotationCopying

```csharp
public bool IsRotationCopying(int index)
```

引擎名：`is_rotation_copying`

Returns `true` if the copy flags has the flag for the rotation in the setting at `index`. See also `SetCopyFlags()`.

### IsScaleCopying

```csharp
public bool IsScaleCopying(int index)
```

引擎名：`is_scale_copying`

Returns `true` if the copy flags has the flag for the scale in the setting at `index`. See also `SetCopyFlags()`.

### SetAdditive

```csharp
public void SetAdditive(int index, bool enabled)
```

引擎名：`set_additive`

Sets additive option in the setting at `index` to `enabled`. This mainly affects the process of applying transform to the [`BoneConstraint3D.SetApplyBone`](BoneConstraint3D.md).

If sets `enabled` to `true`, the processed transform is added to the pose of the current apply bone.

If sets `enabled` to `false`, the pose of the current apply bone is replaced with the processed transform. However, if set `SetRelative()` to `true`, the transform is relative to rest.

### SetAxisFlags

```csharp
public void SetAxisFlags(int index, CopyTransformModifier3D.AxisFlag axisFlags)
```

引擎名：`set_axis_flags`

Sets the flags to copy axes. If the flag is valid, the axis is copied.

### SetAxisXEnabled

```csharp
public void SetAxisXEnabled(int index, bool enabled)
```

引擎名：`set_axis_x_enabled`

If sets `enabled` to `true`, the X-axis will be copied.

### SetAxisXInverted

```csharp
public void SetAxisXInverted(int index, bool enabled)
```

引擎名：`set_axis_x_inverted`

If sets `enabled` to `true`, the X-axis will be inverted.

### SetAxisYEnabled

```csharp
public void SetAxisYEnabled(int index, bool enabled)
```

引擎名：`set_axis_y_enabled`

If sets `enabled` to `true`, the Y-axis will be copied.

### SetAxisYInverted

```csharp
public void SetAxisYInverted(int index, bool enabled)
```

引擎名：`set_axis_y_inverted`

If sets `enabled` to `true`, the Y-axis will be inverted.

### SetAxisZEnabled

```csharp
public void SetAxisZEnabled(int index, bool enabled)
```

引擎名：`set_axis_z_enabled`

If sets `enabled` to `true`, the Z-axis will be copied.

### SetAxisZInverted

```csharp
public void SetAxisZInverted(int index, bool enabled)
```

引擎名：`set_axis_z_inverted`

If sets `enabled` to `true`, the Z-axis will be inverted.

### SetCopyFlags

```csharp
public void SetCopyFlags(int index, CopyTransformModifier3D.TransformFlag copyFlags)
```

引擎名：`set_copy_flags`

Sets the flags to process the transform operations. If the flag is valid, the transform operation is processed.

**Note:** If the rotation is valid for only one axis, it respects the roll of the valid axis. If the rotation is valid for two axes, it discards the roll of the invalid axis.

### SetCopyPosition

```csharp
public void SetCopyPosition(int index, bool enabled)
```

引擎名：`set_copy_position`

If sets `enabled` to `true`, the position will be copied.

### SetCopyRotation

```csharp
public void SetCopyRotation(int index, bool enabled)
```

引擎名：`set_copy_rotation`

If sets `enabled` to `true`, the rotation will be copied.

### SetCopyScale

```csharp
public void SetCopyScale(int index, bool enabled)
```

引擎名：`set_copy_scale`

If sets `enabled` to `true`, the scale will be copied.

### SetInvertFlags

```csharp
public void SetInvertFlags(int index, CopyTransformModifier3D.AxisFlag axisFlags)
```

引擎名：`set_invert_flags`

Sets the flags to inverte axes. If the flag is valid, the axis is copied.

**Note:** An inverted scale means an inverse number, not a negative scale. For example, inverting `2.0` means `0.5`.

**Note:** An inverted rotation flips the elements of the quaternion. For example, a two-axis inversion will flip the roll of each axis, and a three-axis inversion will flip the final orientation. However, be aware that flipping only one axis may cause unintended rotation by the unflipped axes, due to the characteristics of the quaternion.

### SetRelative

```csharp
public void SetRelative(int index, bool enabled)
```

引擎名：`set_relative`

Sets relative option in the setting at `index` to `enabled`.

If sets `enabled` to `true`, the extracted and applying transform is relative to the rest.

If sets `enabled` to `false`, the extracted transform is absolute.

## AxisFlag

命名空间 `Godot`；enum

### 枚举值

- `X = 1`：If set, allows to process the X-axis.
- `Y = 2`：If set, allows to process the Y-axis.
- `Z = 4`：If set, allows to process the Z-axis.
- `All = 7`：If set, allows to process the all axes.

## TransformFlag

命名空间 `Godot`；enum

### 枚举值

- `Position = 1`：If set, allows to copy the position.
- `Rotation = 2`：If set, allows to copy the rotation.
- `Scale = 4`：If set, allows to copy the scale.
- `All = 7`：If set, allows to copy the position/rotation/scale.
