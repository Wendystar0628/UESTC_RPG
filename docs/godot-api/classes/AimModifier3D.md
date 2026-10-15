<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AimModifier3D

命名空间 `Godot`；class；继承 [`BoneConstraint3D`](BoneConstraint3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This is a simple version of [`LookAtModifier3D`](LookAtModifier3D.md) that only allows bone to the reference without advanced options such as angle limitation or time-based interpolation.

The feature is simplified, but instead it is implemented with smooth tracking without euler, see `SetUseEuler()`.

## 构造函数

### AimModifier3D

```csharp
public AimModifier3D()
```

## 属性

### SettingCount

```csharp
public int SettingCount { get; set; }
```

引擎名：`setting_count`

The number of settings in the modifier.

## 方法

### GetForwardAxis

```csharp
public SkeletonModifier3D.BoneAxis GetForwardAxis(int index)
```

引擎名：`get_forward_axis`

Returns the forward axis of the bone.

### GetPrimaryRotationAxis

```csharp
public Vector3.Axis GetPrimaryRotationAxis(int index)
```

引擎名：`get_primary_rotation_axis`

Returns the axis of the first rotation. It is enabled only if `IsUsingEuler()` is `true`.

### IsRelative

```csharp
public bool IsRelative(int index)
```

引擎名：`is_relative`

Returns `true` if the relative option is enabled in the setting at `index`.

### IsUsingEuler

```csharp
public bool IsUsingEuler(int index)
```

引擎名：`is_using_euler`

Returns `true` if it provides rotation with using euler.

### IsUsingSecondaryRotation

```csharp
public bool IsUsingSecondaryRotation(int index)
```

引擎名：`is_using_secondary_rotation`

Returns `true` if it provides rotation by two axes. It is enabled only if `IsUsingEuler()` is `true`.

### SetForwardAxis

```csharp
public void SetForwardAxis(int index, SkeletonModifier3D.BoneAxis axis)
```

引擎名：`set_forward_axis`

Sets the forward axis of the bone.

### SetPrimaryRotationAxis

```csharp
public void SetPrimaryRotationAxis(int index, Vector3.Axis axis)
```

引擎名：`set_primary_rotation_axis`

Sets the axis of the first rotation. It is enabled only if `IsUsingEuler()` is `true`.

### SetRelative

```csharp
public void SetRelative(int index, bool enabled)
```

引擎名：`set_relative`

Sets relative option in the setting at `index` to `enabled`.

If sets `enabled` to `true`, the rotation is applied relative to the pose.

If sets `enabled` to `false`, the rotation is applied relative to the rest. It means to replace the current pose with the `AimModifier3D`'s result.

### SetUseEuler

```csharp
public void SetUseEuler(int index, bool enabled)
```

引擎名：`set_use_euler`

If sets `enabled` to `true`, it provides rotation with using euler.

If sets `enabled` to `false`, it provides rotation with using rotation by arc generated from the forward axis vector and the vector toward the reference.

### SetUseSecondaryRotation

```csharp
public void SetUseSecondaryRotation(int index, bool enabled)
```

引擎名：`set_use_secondary_rotation`

If sets `enabled` to `true`, it provides rotation by two axes. It is enabled only if `IsUsingEuler()` is `true`.
