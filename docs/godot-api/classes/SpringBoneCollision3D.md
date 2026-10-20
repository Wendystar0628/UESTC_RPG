<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpringBoneCollision3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A collision can be a child of [`SpringBoneSimulator3D`](SpringBoneSimulator3D.md). If it is not a child of [`SpringBoneSimulator3D`](SpringBoneSimulator3D.md), it has no effect.

The colliding and sliding are done in the [`SpringBoneSimulator3D`](SpringBoneSimulator3D.md)'s modification process in order of its collision list which is set by [`SpringBoneSimulator3D.SetCollisionPath`](SpringBoneSimulator3D.md). If [`SpringBoneSimulator3D.AreAllChildCollisionsEnabled`](SpringBoneSimulator3D.md) is `true`, the order matches [`SceneTree`](SceneTree.md).

If `Bone` is set, it synchronizes with the bone pose of the ancestor [`Skeleton3D`](Skeleton3D.md), which is done in before the [`SpringBoneSimulator3D`](SpringBoneSimulator3D.md)'s modification process as the pre-process.

**Warning:** A scaled `SpringBoneCollision3D` will likely not behave as expected. Make sure that the parent [`Skeleton3D`](Skeleton3D.md) and its bones are not scaled.

## 构造函数

### SpringBoneCollision3D

```csharp
public SpringBoneCollision3D()
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

### PositionOffset

```csharp
public Vector3 PositionOffset { get; set; }
```

引擎名：`position_offset`

The offset of the position from [`Skeleton3D`](Skeleton3D.md)'s `Bone` pose position.

### RotationOffset

```csharp
public Quaternion RotationOffset { get; set; }
```

引擎名：`rotation_offset`

The offset of the rotation from [`Skeleton3D`](Skeleton3D.md)'s `Bone` pose rotation.

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

### GetPositionOffset

```csharp
public Vector3 GetPositionOffset()
```

引擎名：`get_position_offset`

### GetRotationOffset

```csharp
public Quaternion GetRotationOffset()
```

引擎名：`get_rotation_offset`

### GetSkeleton

```csharp
public Skeleton3D GetSkeleton()
```

引擎名：`get_skeleton`

Get parent [`Skeleton3D`](Skeleton3D.md) node of the parent [`SpringBoneSimulator3D`](SpringBoneSimulator3D.md) if found.

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

### SetPositionOffset

```csharp
public void SetPositionOffset(Vector3 offset)
```

引擎名：`set_position_offset`

### SetRotationOffset

```csharp
public void SetRotationOffset(Quaternion offset)
```

引擎名：`set_rotation_offset`
