<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BoneMap

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class contains a dictionary that uses a list of bone names in [`SkeletonProfile`](SkeletonProfile.md) as key names.

By assigning the actual [`Skeleton3D`](Skeleton3D.md) bone name as the key value, it maps the [`Skeleton3D`](Skeleton3D.md) to the [`SkeletonProfile`](SkeletonProfile.md).

## 构造函数

### BoneMap

```csharp
public BoneMap()
```

## 属性

### Profile

```csharp
public SkeletonProfile Profile { get; set; }
```

引擎名：`profile`

A [`SkeletonProfile`](SkeletonProfile.md) of the mapping target. Key names in the `BoneMap` are synchronized with it.

## 方法

### FindProfileBoneName

```csharp
public StringName FindProfileBoneName(StringName skeletonBoneName)
```

引擎名：`find_profile_bone_name`

Returns a profile bone name having `skeletonBoneName`. If not found, an empty [`StringName`](StringName.md) will be returned.

In the retargeting process, the returned bone name is the bone name of the target skeleton.

### GetProfile

```csharp
public SkeletonProfile GetProfile()
```

引擎名：`get_profile`

### GetSkeletonBoneName

```csharp
public StringName GetSkeletonBoneName(StringName profileBoneName)
```

引擎名：`get_skeleton_bone_name`

Returns a skeleton bone name is mapped to `profileBoneName`.

In the retargeting process, the returned bone name is the bone name of the source skeleton.

### SetProfile

```csharp
public void SetProfile(SkeletonProfile profile)
```

引擎名：`set_profile`

### SetSkeletonBoneName

```csharp
public void SetSkeletonBoneName(StringName profileBoneName, StringName skeletonBoneName)
```

引擎名：`set_skeleton_bone_name`

Maps a skeleton bone name to `profileBoneName`.

In the retargeting process, the setting bone name is the bone name of the source skeleton.

## 事件（信号）

### BoneMapUpdated

```csharp
public event Action BoneMapUpdated
```

引擎名：`bone_map_updated`

This signal is emitted when change the key value in the `BoneMap`. This is used to validate mapping and to update `BoneMap` editor.

### ProfileUpdated

```csharp
public event Action ProfileUpdated
```

引擎名：`profile_updated`

This signal is emitted when change the value in profile or change the reference of profile. This is used to update key names in the `BoneMap` and to redraw the `BoneMap` editor.
