<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Node2D

命名空间 `Godot`；class；继承 [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A 2D game object, with a transform (position, rotation, and scale). All 2D nodes, including physics objects and sprites, inherit from Node2D. Use Node2D as a parent node to move, scale and rotate children in a 2D project. Also gives control of the node's render order.

**Note:** Since both `Node2D` and [`Control`](Control.md) inherit from [`CanvasItem`](CanvasItem.md), they share several concepts from the class such as the [`CanvasItem.ZIndex`](CanvasItem.md) and [`CanvasItem.Visible`](CanvasItem.md) properties.

## 构造函数

### Node2D

```csharp
public Node2D()
```

## 属性

### GlobalPosition

```csharp
public Vector2 GlobalPosition { get; set; }
```

引擎名：`global_position`

Global position. See also `Position`.

### GlobalRotation

```csharp
public float GlobalRotation { get; set; }
```

引擎名：`global_rotation`

Global rotation in radians. See also `Rotation`.

### GlobalRotationDegrees

```csharp
public float GlobalRotationDegrees { get; set; }
```

引擎名：`global_rotation_degrees`

Helper property to access `GlobalRotation` in degrees instead of radians. See also `RotationDegrees`.

### GlobalScale

```csharp
public Vector2 GlobalScale { get; set; }
```

引擎名：`global_scale`

Global scale. See also `Scale`.

### GlobalSkew

```csharp
public float GlobalSkew { get; set; }
```

引擎名：`global_skew`

Global skew in radians. See also `Skew`.

### GlobalTransform

```csharp
public Transform2D GlobalTransform { get; set; }
```

引擎名：`global_transform`

Global [`Transform2D`](Transform2D.md). See also `Transform`.

### Position

```csharp
public Vector2 Position { get; set; }
```

引擎名：`position`

Position, relative to the node's parent. See also `GlobalPosition`.

### Rotation

```csharp
public float Rotation { get; set; }
```

引擎名：`rotation`

Rotation in radians, relative to the node's parent. See also `GlobalRotation`.

**Note:** This property is edited in the inspector in degrees. If you want to use degrees in a script, use `RotationDegrees`.

### RotationDegrees

```csharp
public float RotationDegrees { get; set; }
```

引擎名：`rotation_degrees`

Helper property to access `Rotation` in degrees instead of radians. See also `GlobalRotationDegrees`.

### Scale

```csharp
public Vector2 Scale { get; set; }
```

引擎名：`scale`

The node's scale, relative to the node's parent. Unscaled value: `(1, 1)`. See also `GlobalScale`.

**Note:** Negative X scales in 2D are not decomposable from the transformation matrix. Due to the way scale is represented with transformation matrices in Godot, negative scales on the X axis will be changed to negative scales on the Y axis and a rotation of 180 degrees when decomposed.

### Skew

```csharp
public float Skew { get; set; }
```

引擎名：`skew`

If set to a non-zero value, slants the node in one direction or another. This can be used for pseudo-3D effects. See also `GlobalSkew`.

**Note:** Skew is performed on the X axis only, and *between* rotation and scaling.

**Note:** This property is edited in the inspector in degrees. If you want to use degrees in a script, use `skew = deg_to_rad(value_in_degrees)`.

### Transform

```csharp
public Transform2D Transform { get; set; }
```

引擎名：`transform`

The node's [`Transform2D`](Transform2D.md), relative to the node's parent. See also `GlobalTransform`.

## 方法

### ApplyScale

```csharp
public void ApplyScale(Vector2 ratio)
```

引擎名：`apply_scale`

Multiplies the current scale by the `ratio` vector.

### GetAngleTo

```csharp
public float GetAngleTo(Vector2 point)
```

引擎名：`get_angle_to`

Returns the angle between the node and the `point` in radians. See also `LookAt()`.

[Illustration of the returned angle.](https://raw.githubusercontent.com/godotengine/godot-docs/master/img/node2d_get_angle_to.png)

### GetGlobalPosition

```csharp
public Vector2 GetGlobalPosition()
```

引擎名：`get_global_position`

### GetGlobalRotation

```csharp
public float GetGlobalRotation()
```

引擎名：`get_global_rotation`

### GetGlobalRotationDegrees

```csharp
public float GetGlobalRotationDegrees()
```

引擎名：`get_global_rotation_degrees`

### GetGlobalScale

```csharp
public Vector2 GetGlobalScale()
```

引擎名：`get_global_scale`

### GetGlobalSkew

```csharp
public float GetGlobalSkew()
```

引擎名：`get_global_skew`

### GetPosition

```csharp
public Vector2 GetPosition()
```

引擎名：`get_position`

### GetRelativeTransformToParent

```csharp
public Transform2D GetRelativeTransformToParent(Node parent)
```

引擎名：`get_relative_transform_to_parent`

Returns the [`Transform2D`](Transform2D.md) relative to this node's parent.

### GetRotation

```csharp
public float GetRotation()
```

引擎名：`get_rotation`

### GetRotationDegrees

```csharp
public float GetRotationDegrees()
```

引擎名：`get_rotation_degrees`

### GetScale

```csharp
public Vector2 GetScale()
```

引擎名：`get_scale`

### GetSkew

```csharp
public float GetSkew()
```

引擎名：`get_skew`

### GlobalTranslate

```csharp
public void GlobalTranslate(Vector2 offset)
```

引擎名：`global_translate`

Adds the `offset` vector to the node's global position.

### LookAt

```csharp
public void LookAt(Vector2 point)
```

引擎名：`look_at`

Rotates the node so that its local +X axis points towards the `point`, which is expected to use global coordinates. This method is a combination of both `Rotate()` and `GetAngleTo()`.

`point` should not be the same as the node's position, otherwise the node always looks to the right.

### MoveLocalX

```csharp
public void MoveLocalX(float delta, bool scaled = false)
```

引擎名：`move_local_x`

Applies a local translation on the node's X axis with the amount specified in `delta`. If `scaled` is `false`, normalizes the movement to occur independently of the node's `Scale`.

### MoveLocalY

```csharp
public void MoveLocalY(float delta, bool scaled = false)
```

引擎名：`move_local_y`

Applies a local translation on the node's Y axis with the amount specified in `delta`. If `scaled` is `false`, normalizes the movement to occur independently of the node's `Scale`.

### Rotate

```csharp
public void Rotate(float radians)
```

引擎名：`rotate`

Applies a rotation to the node, in radians, starting from its current rotation. This is equivalent to `rotation += radians`.

### SetGlobalPosition

```csharp
public void SetGlobalPosition(Vector2 position)
```

引擎名：`set_global_position`

### SetGlobalRotation

```csharp
public void SetGlobalRotation(float radians)
```

引擎名：`set_global_rotation`

### SetGlobalRotationDegrees

```csharp
public void SetGlobalRotationDegrees(float degrees)
```

引擎名：`set_global_rotation_degrees`

### SetGlobalScale

```csharp
public void SetGlobalScale(Vector2 scale)
```

引擎名：`set_global_scale`

### SetGlobalSkew

```csharp
public void SetGlobalSkew(float radians)
```

引擎名：`set_global_skew`

### SetGlobalTransform

```csharp
public void SetGlobalTransform(Transform2D xform)
```

引擎名：`set_global_transform`

### SetPosition

```csharp
public void SetPosition(Vector2 position)
```

引擎名：`set_position`

### SetRotation

```csharp
public void SetRotation(float radians)
```

引擎名：`set_rotation`

### SetRotationDegrees

```csharp
public void SetRotationDegrees(float degrees)
```

引擎名：`set_rotation_degrees`

### SetScale

```csharp
public void SetScale(Vector2 scale)
```

引擎名：`set_scale`

### SetSkew

```csharp
public void SetSkew(float radians)
```

引擎名：`set_skew`

### SetTransform

```csharp
public void SetTransform(Transform2D xform)
```

引擎名：`set_transform`

### ToGlobal

```csharp
public Vector2 ToGlobal(Vector2 localPoint)
```

引擎名：`to_global`

Transforms the provided local position into a position in global coordinate space. The input is expected to be local relative to the `Node2D` it is called on. e.g. Applying this method to the positions of child nodes will correctly transform their positions into the global coordinate space, but applying it to a node's own position will give an incorrect result, as it will incorporate the node's own transformation into its global position.

### ToLocal

```csharp
public Vector2 ToLocal(Vector2 globalPoint)
```

引擎名：`to_local`

Transforms the provided global position into a position in local coordinate space. The output will be local relative to the `Node2D` it is called on. e.g. It is appropriate for determining the positions of child nodes, but it is not appropriate for determining its own position relative to its parent.

### Translate

```csharp
public void Translate(Vector2 offset)
```

引擎名：`translate`

Translates the node by the given `offset` in local coordinates. This is equivalent to `position += offset`.
