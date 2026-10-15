<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# JointLimitationCone3D

命名空间 `Godot`；class；继承 [`JointLimitation3D`](JointLimitation3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A cone shape limitation that interacts with [`ChainIK3D`](ChainIK3D.md).

## 构造函数

### JointLimitationCone3D

```csharp
public JointLimitationCone3D()
```

## 属性

### Angle

```csharp
public float Angle { get; set; }
```

引擎名：`angle`

The radius range of the hole made by the cone.

`0` degrees makes a sphere without hole, `180` degrees makes a hemisphere, and `360` degrees become empty (no limitation).

## 方法

### GetAngle

```csharp
public float GetAngle()
```

引擎名：`get_angle`

### SetAngle

```csharp
public void SetAngle(float angle)
```

引擎名：`set_angle`
