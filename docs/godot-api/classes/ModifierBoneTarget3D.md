<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ModifierBoneTarget3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node selects a bone in a [`Skeleton3D`](Skeleton3D.md) and attaches to it. This means that the `ModifierBoneTarget3D` node will dynamically copy the 3D transform of the selected bone.

The functionality is similar to [`BoneAttachment3D`](BoneAttachment3D.md), but this node adopts the [`SkeletonModifier3D`](SkeletonModifier3D.md) cycle and is intended to be used as another [`SkeletonModifier3D`](SkeletonModifier3D.md)'s target.

## 构造函数

### ModifierBoneTarget3D

```csharp
public ModifierBoneTarget3D()
```

## 属性

### Bone

```csharp
public int Bone { get; set; }
```

引擎名：`bone`

The index of the attached bone.

### BoneName

```csharp
public string BoneName { get; set; }
```

引擎名：`bone_name`

The name of the attached bone.

## 方法

### GetBone

```csharp
public int GetBone()
```

引擎名：`get_bone`

### GetBoneName

```csharp
public string GetBoneName()
```

引擎名：`get_bone_name`

### SetBone

```csharp
public void SetBone(int bone)
```

引擎名：`set_bone`

### SetBoneName

```csharp
public void SetBoneName(string boneName)
```

引擎名：`set_bone_name`
