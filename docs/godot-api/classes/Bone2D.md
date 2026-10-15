<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Bone2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A hierarchy of `Bone2D`s can be bound to a [`Skeleton2D`](Skeleton2D.md) to control and animate other [`Node2D`](Node2D.md) nodes.

You can use `Bone2D` and [`Skeleton2D`](Skeleton2D.md) nodes to animate 2D meshes created with the [`Polygon2D`](Polygon2D.md) UV editor.

Each bone has a `Rest` transform that you can reset to with `ApplyRest()`. These rest poses are relative to the bone's parent.

If in the editor, you can set the rest pose of an entire skeleton using a menu option, from the code, you need to iterate over the bones to set their individual rest poses.

## 构造函数

### Bone2D

```csharp
public Bone2D()
```

## 属性

### Rest

```csharp
public Transform2D Rest { get; set; }
```

引擎名：`rest`

Rest transform of the bone. You can reset the node's transforms to this value using `ApplyRest()`.

## 方法

### ApplyRest

```csharp
public void ApplyRest()
```

引擎名：`apply_rest`

Resets the bone to the rest pose. This is equivalent to setting [`Node2D.Transform`](Node2D.md) to `Rest`.

### GetAutocalculateLengthAndAngle

```csharp
public bool GetAutocalculateLengthAndAngle()
```

引擎名：`get_autocalculate_length_and_angle`

Returns whether this `Bone2D` is going to autocalculate its length and bone angle using its first `Bone2D` child node, if one exists. If there are no `Bone2D` children, then it cannot autocalculate these values and will print a warning.

### GetBoneAngle

```csharp
public float GetBoneAngle()
```

引擎名：`get_bone_angle`

Returns the angle of the bone in the `Bone2D`.

**Note:** This is different from the `Bone2D`'s rotation. The bone's angle is the rotation of the bone shown by the gizmo, which is unaffected by the `Bone2D`'s [`Node2D.Transform`](Node2D.md).

### GetIndexInSkeleton

```csharp
public int GetIndexInSkeleton()
```

引擎名：`get_index_in_skeleton`

Returns the node's index as part of the entire skeleton. See [`Skeleton2D`](Skeleton2D.md).

### GetLength

```csharp
public float GetLength()
```

引擎名：`get_length`

Returns the length of the bone in the `Bone2D` node.

### GetRest

```csharp
public Transform2D GetRest()
```

引擎名：`get_rest`

### GetSkeletonRest

```csharp
public Transform2D GetSkeletonRest()
```

引擎名：`get_skeleton_rest`

Returns the node's `Rest` [`Transform2D`](Transform2D.md) if it doesn't have a parent, or its rest pose relative to its parent.

### SetAutocalculateLengthAndAngle

```csharp
public void SetAutocalculateLengthAndAngle(bool autoCalculate)
```

引擎名：`set_autocalculate_length_and_angle`

When set to `true`, the `Bone2D` node will attempt to automatically calculate the bone angle and length using the first child `Bone2D` node, if one exists. If none exist, the `Bone2D` cannot automatically calculate these values and will print a warning.

### SetBoneAngle

```csharp
public void SetBoneAngle(float angle)
```

引擎名：`set_bone_angle`

Sets the bone angle for the `Bone2D`. This is typically set to the rotation from the `Bone2D` to a child `Bone2D` node.

**Note:** This is different from the `Bone2D`'s rotation. The bone's angle is the rotation of the bone shown by the gizmo, which is unaffected by the `Bone2D`'s [`Node2D.Transform`](Node2D.md).

### SetLength

```csharp
public void SetLength(float length)
```

引擎名：`set_length`

Sets the length of the bone in the `Bone2D`.

### SetRest

```csharp
public void SetRest(Transform2D rest)
```

引擎名：`set_rest`
