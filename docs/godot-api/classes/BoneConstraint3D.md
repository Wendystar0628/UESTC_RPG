<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BoneConstraint3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Base class of [`SkeletonModifier3D`](SkeletonModifier3D.md) that modifies the bone set in `SetApplyBone()` based on the transform of the bone retrieved by `GetReferenceBone()`.

**Note:** Most methods in this class take an `index` parameter. This parameter specifies which setting list entry to return if the IK has multiple entries (e.g. `settings/<index>/amount`).

## 构造函数

### BoneConstraint3D

```csharp
public BoneConstraint3D()
```

## 方法

### ClearSetting

```csharp
public void ClearSetting()
```

引擎名：`clear_setting`

Clear all settings.

### GetAmount

```csharp
public float GetAmount(int index)
```

引擎名：`get_amount`

Returns the apply amount of the setting at `index`.

### GetApplyBone

```csharp
public int GetApplyBone(int index)
```

引擎名：`get_apply_bone`

Returns the apply bone of the setting at `index`. This bone will be modified.

### GetApplyBoneName

```csharp
public string GetApplyBoneName(int index)
```

引擎名：`get_apply_bone_name`

Returns the apply bone name of the setting at `index`. This bone will be modified.

### GetReferenceBone

```csharp
public int GetReferenceBone(int index)
```

引擎名：`get_reference_bone`

Returns the reference bone of the setting at `index`.

This bone will be only referenced and not modified by this modifier.

### GetReferenceBoneName

```csharp
public string GetReferenceBoneName(int index)
```

引擎名：`get_reference_bone_name`

Returns the reference bone name of the setting at `index`.

This bone will be only referenced and not modified by this modifier.

### GetReferenceNode

```csharp
public NodePath GetReferenceNode(int index)
```

引擎名：`get_reference_node`

Returns the reference node path of the setting at `index`.

This node will be only referenced and not modified by this modifier.

### GetReferenceType

```csharp
public BoneConstraint3D.ReferenceType GetReferenceType(int index)
```

引擎名：`get_reference_type`

Returns the reference target type of the setting at `index`. See also [`BoneConstraint3D.ReferenceType`](BoneConstraint3D.md).

### GetSettingCount

```csharp
public int GetSettingCount()
```

引擎名：`get_setting_count`

Returns the number of settings in the modifier.

### SetAmount

```csharp
public void SetAmount(int index, float amount)
```

引擎名：`set_amount`

Sets the apply amount of the setting at `index` to `amount`.

### SetApplyBone

```csharp
public void SetApplyBone(int index, int bone)
```

引擎名：`set_apply_bone`

Sets the apply bone of the setting at `index` to `bone`. This bone will be modified.

### SetApplyBoneName

```csharp
public void SetApplyBoneName(int index, string boneName)
```

引擎名：`set_apply_bone_name`

Sets the apply bone of the setting at `index` to `boneName`. This bone will be modified.

### SetReferenceBone

```csharp
public void SetReferenceBone(int index, int bone)
```

引擎名：`set_reference_bone`

Sets the reference bone of the setting at `index` to `bone`.

This bone will be only referenced and not modified by this modifier.

### SetReferenceBoneName

```csharp
public void SetReferenceBoneName(int index, string boneName)
```

引擎名：`set_reference_bone_name`

Sets the reference bone of the setting at `index` to `boneName`.

This bone will be only referenced and not modified by this modifier.

### SetReferenceNode

```csharp
public void SetReferenceNode(int index, NodePath node)
```

引擎名：`set_reference_node`

Sets the reference node path of the setting at `index` to `node`.

This node will be only referenced and not modified by this modifier.

### SetReferenceType

```csharp
public void SetReferenceType(int index, BoneConstraint3D.ReferenceType type)
```

引擎名：`set_reference_type`

Sets the reference target type of the setting at `index` to `type`. See also [`BoneConstraint3D.ReferenceType`](BoneConstraint3D.md).

### SetSettingCount

```csharp
public void SetSettingCount(int count)
```

引擎名：`set_setting_count`

Sets the number of settings in the modifier.

## ReferenceType

命名空间 `Godot`；enum

### 枚举值

- `Bone = 0`：The reference target is a bone.
- `Node = 1`：The reference target is a [`Node3D`](Node3D.md).
