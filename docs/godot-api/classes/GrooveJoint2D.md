<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GrooveJoint2D

命名空间 `Godot`；class；继承 [`Joint2D`](Joint2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A physics joint that restricts the movement of two 2D physics bodies to a fixed axis. For example, a [`StaticBody2D`](StaticBody2D.md) representing a piston base can be attached to a [`RigidBody2D`](RigidBody2D.md) representing the piston head, moving up and down.

## 构造函数

### GrooveJoint2D

```csharp
public GrooveJoint2D()
```

## 属性

### InitialOffset

```csharp
public float InitialOffset { get; set; }
```

引擎名：`initial_offset`

The body B's initial anchor position defined by the joint's origin and a local offset `InitialOffset` along the joint's Y axis (along the groove).

### Length

```csharp
public float Length { get; set; }
```

引擎名：`length`

The groove's length. The groove is from the joint's origin towards `Length` along the joint's local Y axis.

## 方法

### GetInitialOffset

```csharp
public float GetInitialOffset()
```

引擎名：`get_initial_offset`

### GetLength

```csharp
public float GetLength()
```

引擎名：`get_length`

### SetInitialOffset

```csharp
public void SetInitialOffset(float offset)
```

引擎名：`set_initial_offset`

### SetLength

```csharp
public void SetLength(float length)
```

引擎名：`set_length`
