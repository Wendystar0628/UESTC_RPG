<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ConeTwistJoint3D

命名空间 `Godot`；class；继承 [`Joint3D`](Joint3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A physics joint that connects two 3D physics bodies in a way that simulates a ball-and-socket joint. The twist axis is initiated as the X axis of the `ConeTwistJoint3D`. Once the physics bodies swing, the twist axis is calculated as the middle of the X axes of the joint in the local space of the two physics bodies. Useful for limbs like shoulders and hips, lamps hanging off a ceiling, etc.

## 构造函数

### ConeTwistJoint3D

```csharp
public ConeTwistJoint3D()
```

## 属性

### Bias

```csharp
public float Bias { get; set; }
```

引擎名：`bias`

The speed with which the swing or twist will take place.

The higher, the faster.

### Relaxation

```csharp
public float Relaxation { get; set; }
```

引擎名：`relaxation`

Defines, how fast the swing- and twist-speed-difference on both sides gets synced.

### Softness

```csharp
public float Softness { get; set; }
```

引擎名：`softness`

The ease with which the joint starts to twist. If it's too low, it takes more force to start twisting the joint.

### SwingSpan

```csharp
public float SwingSpan { get; set; }
```

引擎名：`swing_span`

Swing is rotation from side to side, around the axis perpendicular to the twist axis.

The swing span defines, how much rotation will not get corrected along the swing axis.

Could be defined as looseness in the `ConeTwistJoint3D`.

If below 0.05, this behavior is locked.

### TwistSpan

```csharp
public float TwistSpan { get; set; }
```

引擎名：`twist_span`

Twist is the rotation around the twist axis, this value defined how far the joint can twist.

Twist is locked if below 0.05.

## 方法

### GetParam

```csharp
public float GetParam(ConeTwistJoint3D.Param param)
```

引擎名：`get_param`

Returns the value of the specified parameter.

### SetParam

```csharp
public void SetParam(ConeTwistJoint3D.Param param, float value)
```

引擎名：`set_param`

Sets the value of the specified parameter.

## Param

命名空间 `Godot`；enum

### 枚举值

- `SwingSpan = 0`：Swing is rotation from side to side, around the axis perpendicular to the twist axis.
- `TwistSpan = 1`：Twist is the rotation around the twist axis, this value defined how far the joint can twist.
- `Bias = 2`：The speed with which the swing or twist will take place.
- `Softness = 3`：The ease with which the joint starts to twist.
- `Relaxation = 4`：Defines, how fast the swing- and twist-speed-difference on both sides gets synced.
- `Max = 5`：Represents the size of the `ConeTwistJoint3D.Param` enum.
