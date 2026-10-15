<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SplineIK3D

命名空间 `Godot`；class；继承 [`ChainIK3D`](ChainIK3D.md) → [`IKModifier3D`](IKModifier3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A [`SkeletonModifier3D`](SkeletonModifier3D.md) for aligning bones along a [`Path3D`](Path3D.md). The smoothness of the fitting depends on the [`Curve3D.BakeInterval`](Curve3D.md).

If you want the [`Path3D`](Path3D.md) to attach to a specific bone, it is recommended to place a [`ModifierBoneTarget3D`](ModifierBoneTarget3D.md) before the `SplineIK3D` in the [`SkeletonModifier3D`](SkeletonModifier3D.md) list (children of the [`Skeleton3D`](Skeleton3D.md)), and then place a [`Path3D`](Path3D.md) as the [`ModifierBoneTarget3D`](ModifierBoneTarget3D.md)'s child.

Bone twist is determined based on the [`Curve3D.GetPointTilt`](Curve3D.md).

If the root bone joint and the start point of the [`Curve3D`](Curve3D.md) are separated, it assumes that there is a linear line segment between them. This means that the vector pointing toward the start point of the [`Curve3D`](Curve3D.md) takes precedence over the shortest intersection point along the [`Curve3D`](Curve3D.md).

If the end bone joint exceeds the path length, it is bent as close as possible to the end point of the [`Curve3D`](Curve3D.md).

**Note:** All the methods in this class take an `index` parameter. This parameter specifies which setting list entry to return if the IK has multiple entries (e.g. `settings/<index>/root_bone_name`).

## 构造函数

### SplineIK3D

```csharp
public SplineIK3D()
```

## 属性

### SettingCount

```csharp
public int SettingCount { get; set; }
```

引擎名：`setting_count`

The number of settings.

## 方法

### GetPath3D

```csharp
public NodePath GetPath3D(int index)
```

Returns the node path of the [`Path3D`](Path3D.md) which is describing the path.

### GetTiltFadeIn

```csharp
public int GetTiltFadeIn(int index)
```

引擎名：`get_tilt_fade_in`

Returns the tilt interpolation method used between the root bone and the start point of the [`Curve3D`](Curve3D.md) when they are apart. See also `SetTiltFadeIn()`.

### GetTiltFadeOut

```csharp
public int GetTiltFadeOut(int index)
```

引擎名：`get_tilt_fade_out`

Returns the tilt interpolation method used between the end bone and the end point of the [`Curve3D`](Curve3D.md) when they are apart. See also `SetTiltFadeOut()`.

### IsTiltEnabled

```csharp
public bool IsTiltEnabled(int index)
```

引擎名：`is_tilt_enabled`

Returns if the tilt property of the [`Curve3D`](Curve3D.md) affects the bone twist.

### SetPath3D

```csharp
public void SetPath3D(int index, NodePath path3D)
```

Sets the node path of the [`Path3D`](Path3D.md) which is describing the path.

### SetTiltEnabled

```csharp
public void SetTiltEnabled(int index, bool enabled)
```

引擎名：`set_tilt_enabled`

Sets if the tilt property of the [`Curve3D`](Curve3D.md) should affect the bone twist.

### SetTiltFadeIn

```csharp
public void SetTiltFadeIn(int index, int size)
```

引擎名：`set_tilt_fade_in`

If `size` is greater than `0`, the tilt is interpolated between `size` start bones from the start point of the [`Curve3D`](Curve3D.md) when they are apart.

If `size` is equal `0`, the tilts between the root bone head and the start point of the [`Curve3D`](Curve3D.md) are unified with a tilt of the start point of the [`Curve3D`](Curve3D.md).

If `size` is less than `0`, the tilts between the root bone and the start point of the [`Curve3D`](Curve3D.md) are `0.0`.

### SetTiltFadeOut

```csharp
public void SetTiltFadeOut(int index, int size)
```

引擎名：`set_tilt_fade_out`

If `size` is greater than `0`, the tilt is interpolated between `size` end bones from the end point of the [`Curve3D`](Curve3D.md) when they are apart.

If `size` is equal `0`, the tilts between the end bone tail and the end point of the [`Curve3D`](Curve3D.md) are unified with a tilt of the end point of the [`Curve3D`](Curve3D.md).

If `size` is less than `0`, the tilts between the end bone and the end point of the [`Curve3D`](Curve3D.md) are `0.0`.
