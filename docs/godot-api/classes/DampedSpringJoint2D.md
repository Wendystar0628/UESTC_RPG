<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DampedSpringJoint2D

命名空间 `Godot`；class；继承 [`Joint2D`](Joint2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A physics joint that connects two 2D physics bodies with a spring-like force. This behaves like a spring that always wants to stretch to a given length.

## 构造函数

### DampedSpringJoint2D

```csharp
public DampedSpringJoint2D()
```

## 属性

### Damping

```csharp
public float Damping { get; set; }
```

引擎名：`damping`

The spring joint's damping ratio. A value between `0` and `1`. When the two bodies move into different directions the system tries to align them to the spring axis again. A high `Damping` value forces the attached bodies to align faster.

### Length

```csharp
public float Length { get; set; }
```

引擎名：`length`

The spring joint's maximum length. The two attached bodies cannot stretch it past this value.

### RestLength

```csharp
public float RestLength { get; set; }
```

引擎名：`rest_length`

When the bodies attached to the spring joint move they stretch or squash it. The joint always tries to resize towards this length.

### Stiffness

```csharp
public float Stiffness { get; set; }
```

引擎名：`stiffness`

The higher the value, the less the bodies attached to the joint will deform it. The joint applies an opposing force to the bodies, the product of the stiffness multiplied by the size difference from its resting length.

## 方法

### GetDamping

```csharp
public float GetDamping()
```

引擎名：`get_damping`

### GetLength

```csharp
public float GetLength()
```

引擎名：`get_length`

### GetRestLength

```csharp
public float GetRestLength()
```

引擎名：`get_rest_length`

### GetStiffness

```csharp
public float GetStiffness()
```

引擎名：`get_stiffness`

### SetDamping

```csharp
public void SetDamping(float damping)
```

引擎名：`set_damping`

### SetLength

```csharp
public void SetLength(float length)
```

引擎名：`set_length`

### SetRestLength

```csharp
public void SetRestLength(float restLength)
```

引擎名：`set_rest_length`

### SetStiffness

```csharp
public void SetStiffness(float stiffness)
```

引擎名：`set_stiffness`
