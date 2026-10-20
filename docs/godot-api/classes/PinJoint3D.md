<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PinJoint3D

命名空间 `Godot`；class；继承 [`Joint3D`](Joint3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A physics joint that attaches two 3D physics bodies at a single point, allowing them to freely rotate. For example, a [`RigidBody3D`](RigidBody3D.md) can be attached to a [`StaticBody3D`](StaticBody3D.md) to create a pendulum or a seesaw.

## 构造函数

### PinJoint3D

```csharp
public PinJoint3D()
```

## 方法

### GetParam

```csharp
public float GetParam(PinJoint3D.Param param)
```

引擎名：`get_param`

Returns the value of the specified parameter.

### SetParam

```csharp
public void SetParam(PinJoint3D.Param param, float value)
```

引擎名：`set_param`

Sets the value of the specified parameter.

## Param

命名空间 `Godot`；enum

### 枚举值

- `Bias = 0`：The force with which the pinned objects stay in positional relation to each other.
- `Damping = 1`：The force with which the pinned objects stay in velocity relation to each other.
- `ImpulseClamp = 2`：If above 0, this value is the maximum value for an impulse that this Joint3D produces.
