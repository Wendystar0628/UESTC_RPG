<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ConvertTransformModifier3D

命名空间 `Godot`；class；继承 [`BoneConstraint3D`](BoneConstraint3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Apply the copied transform of the bone set by [`BoneConstraint3D.SetReferenceBone`](BoneConstraint3D.md) to the bone set by [`BoneConstraint3D.SetApplyBone`](BoneConstraint3D.md) about the specific axis with remapping it with some options.

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

**Note:** If there is a rotation greater than `180` degrees with constrained axes, flipping may occur.

## 构造函数

### ConvertTransformModifier3D

```csharp
public ConvertTransformModifier3D()
```

## 属性

### SettingCount

```csharp
public int SettingCount { get; set; }
```

引擎名：`setting_count`

The number of settings in the modifier.

## 方法

### GetApplyAxis

```csharp
public Vector3.Axis GetApplyAxis(int index)
```

引擎名：`get_apply_axis`

Returns the axis of the remapping destination transform.

### GetApplyRangeMax

```csharp
public float GetApplyRangeMax(int index)
```

引擎名：`get_apply_range_max`

Returns the maximum value of the remapping destination range.

### GetApplyRangeMin

```csharp
public float GetApplyRangeMin(int index)
```

引擎名：`get_apply_range_min`

Returns the minimum value of the remapping destination range.

### GetApplyTransformMode

```csharp
public ConvertTransformModifier3D.TransformMode GetApplyTransformMode(int index)
```

引擎名：`get_apply_transform_mode`

Returns the operation of the remapping destination transform.

### GetReferenceAxis

```csharp
public Vector3.Axis GetReferenceAxis(int index)
```

引擎名：`get_reference_axis`

Returns the axis of the remapping source transform.

### GetReferenceRangeMax

```csharp
public float GetReferenceRangeMax(int index)
```

引擎名：`get_reference_range_max`

Returns the maximum value of the remapping source range.

### GetReferenceRangeMin

```csharp
public float GetReferenceRangeMin(int index)
```

引擎名：`get_reference_range_min`

Returns the minimum value of the remapping source range.

### GetReferenceTransformMode

```csharp
public ConvertTransformModifier3D.TransformMode GetReferenceTransformMode(int index)
```

引擎名：`get_reference_transform_mode`

Returns the operation of the remapping source transform.

### IsAdditive

```csharp
public bool IsAdditive(int index)
```

引擎名：`is_additive`

Returns `true` if the additive option is enabled in the setting at `index`.

### IsRelative

```csharp
public bool IsRelative(int index)
```

引擎名：`is_relative`

Returns `true` if the relative option is enabled in the setting at `index`.

### SetAdditive

```csharp
public void SetAdditive(int index, bool enabled)
```

引擎名：`set_additive`

Sets additive option in the setting at `index` to `enabled`. This mainly affects the process of applying transform to the [`BoneConstraint3D.SetApplyBone`](BoneConstraint3D.md).

If sets `enabled` to `true`, the processed transform is added to the pose of the current apply bone.

If sets `enabled` to `false`, the pose of the current apply bone is replaced with the processed transform. However, if set `SetRelative()` to `true`, the transform is relative to rest.

### SetApplyAxis

```csharp
public void SetApplyAxis(int index, Vector3.Axis axis)
```

引擎名：`set_apply_axis`

Sets the axis of the remapping destination transform.

### SetApplyRangeMax

```csharp
public void SetApplyRangeMax(int index, float rangeMax)
```

引擎名：`set_apply_range_max`

Sets the maximum value of the remapping destination range.

### SetApplyRangeMin

```csharp
public void SetApplyRangeMin(int index, float rangeMin)
```

引擎名：`set_apply_range_min`

Sets the minimum value of the remapping destination range.

### SetApplyTransformMode

```csharp
public void SetApplyTransformMode(int index, ConvertTransformModifier3D.TransformMode transformMode)
```

引擎名：`set_apply_transform_mode`

Sets the operation of the remapping destination transform.

### SetReferenceAxis

```csharp
public void SetReferenceAxis(int index, Vector3.Axis axis)
```

引擎名：`set_reference_axis`

Sets the axis of the remapping source transform.

### SetReferenceRangeMax

```csharp
public void SetReferenceRangeMax(int index, float rangeMax)
```

引擎名：`set_reference_range_max`

Sets the maximum value of the remapping source range.

### SetReferenceRangeMin

```csharp
public void SetReferenceRangeMin(int index, float rangeMin)
```

引擎名：`set_reference_range_min`

Sets the minimum value of the remapping source range.

### SetReferenceTransformMode

```csharp
public void SetReferenceTransformMode(int index, ConvertTransformModifier3D.TransformMode transformMode)
```

引擎名：`set_reference_transform_mode`

Sets the operation of the remapping source transform.

### SetRelative

```csharp
public void SetRelative(int index, bool enabled)
```

引擎名：`set_relative`

Sets relative option in the setting at `index` to `enabled`.

If sets `enabled` to `true`, the extracted and applying transform is relative to the rest.

If sets `enabled` to `false`, the extracted transform is absolute.

## TransformMode

命名空间 `Godot`；enum

### 枚举值

- `Position = 0`：Convert with position.
- `Rotation = 1`：Convert with rotation.
- `Scale = 2`：Convert with scale.
