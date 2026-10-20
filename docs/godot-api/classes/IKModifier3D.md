<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# IKModifier3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Base class of [`SkeletonModifier3D`](SkeletonModifier3D.md)s that has some joint lists and applies inverse kinematics. This class has some structs, enums, and helper methods which are useful to solve inverse kinematics.

## 属性

### MutableBoneAxes

```csharp
public bool MutableBoneAxes { get; set; }
```

引擎名：`mutable_bone_axes`

If `true`, the solver retrieves the bone axis from the bone pose every frame.

If `false`, the solver retrieves the bone axis from the bone rest and caches it, which increases performance slightly, but position changes in the bone pose made before processing this `IKModifier3D` are ignored.

## 方法

### AreBoneAxesMutable

```csharp
public bool AreBoneAxesMutable()
```

引擎名：`are_bone_axes_mutable`

### ClearSettings

```csharp
public void ClearSettings()
```

引擎名：`clear_settings`

Clears all settings.

### GetSettingCount

```csharp
public int GetSettingCount()
```

引擎名：`get_setting_count`

Returns the number of settings.

### Reset

```csharp
public void Reset()
```

引擎名：`reset`

Resets a state with respect to the current bone pose.

### SetMutableBoneAxes

```csharp
public void SetMutableBoneAxes(bool enabled)
```

引擎名：`set_mutable_bone_axes`

### SetSettingCount

```csharp
public void SetSettingCount(int count)
```

引擎名：`set_setting_count`

Sets the number of settings.
