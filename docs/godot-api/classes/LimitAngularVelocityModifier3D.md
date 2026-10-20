<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LimitAngularVelocityModifier3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This modifier limits bone rotation angular velocity by comparing poses between previous and current frame.

You can add bone chains by specifying their root and end bones, then add the bones between them to a list. Modifier processes either that list or the bones excluding those in the list depending on the option `Exclude`.

**Note:** Most methods in this class take an `index` parameter. This parameter specifies which setting list entry to return if the IK has multiple entries (e.g. `settings/<index>/root_bone_name`).

## 构造函数

### LimitAngularVelocityModifier3D

```csharp
public LimitAngularVelocityModifier3D()
```

## 属性

### ChainCount

```csharp
public int ChainCount { get; set; }
```

引擎名：`chain_count`

The number of chains.

### Exclude

```csharp
public bool Exclude { get; set; }
```

引擎名：`exclude`

If `true`, the modifier processes bones not included in the bone list.

If `false`, the bones processed by the modifier are equal to the bone list.

### JointCount

```csharp
public int JointCount { get; }
```

引擎名：`joint_count`

The number of joints in the list which created by chains dynamically.

### MaxAngularVelocity

```csharp
public double MaxAngularVelocity { get; set; }
```

引擎名：`max_angular_velocity`

The maximum angular velocity per second.

## 方法

### ClearChains

```csharp
public void ClearChains()
```

引擎名：`clear_chains`

Clear all chains.

### GetChainCount

```csharp
public int GetChainCount()
```

引擎名：`get_chain_count`

### GetEndBone

```csharp
public int GetEndBone(int index)
```

引擎名：`get_end_bone`

Returns the end bone index of the bone chain.

### GetEndBoneName

```csharp
public string GetEndBoneName(int index)
```

引擎名：`get_end_bone_name`

Returns the end bone name of the bone chain.

### GetMaxAngularVelocity

```csharp
public double GetMaxAngularVelocity()
```

引擎名：`get_max_angular_velocity`

### GetRootBone

```csharp
public int GetRootBone(int index)
```

引擎名：`get_root_bone`

Returns the root bone index of the bone chain.

### GetRootBoneName

```csharp
public string GetRootBoneName(int index)
```

引擎名：`get_root_bone_name`

Returns the root bone name of the bone chain.

### IsExclude

```csharp
public bool IsExclude()
```

引擎名：`is_exclude`

### Reset

```csharp
public void Reset()
```

引擎名：`reset`

Sets the reference pose for angle comparison to the current pose with the influence of constraints removed. This function is automatically triggered when joints change or upon activation.

### SetChainCount

```csharp
public void SetChainCount(int count)
```

引擎名：`set_chain_count`

### SetEndBone

```csharp
public void SetEndBone(int index, int bone)
```

引擎名：`set_end_bone`

Sets the end bone index of the bone chain.

### SetEndBoneName

```csharp
public void SetEndBoneName(int index, string boneName)
```

引擎名：`set_end_bone_name`

Sets the end bone name of the bone chain.

**Note:** End bone must be the root bone or a child of the root bone.

### SetExclude

```csharp
public void SetExclude(bool exclude)
```

引擎名：`set_exclude`

### SetMaxAngularVelocity

```csharp
public void SetMaxAngularVelocity(double angularVelocity)
```

引擎名：`set_max_angular_velocity`

### SetRootBone

```csharp
public void SetRootBone(int index, int bone)
```

引擎名：`set_root_bone`

Sets the root bone index of the bone chain.

### SetRootBoneName

```csharp
public void SetRootBoneName(int index, string boneName)
```

引擎名：`set_root_bone_name`

Sets the root bone name of the bone chain.
