<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeParticleMultiplyByAxisAngle

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This node helps to multiply a position input vector by rotation using specific axis. Intended to work with emitters.

## 构造函数

### VisualShaderNodeParticleMultiplyByAxisAngle

```csharp
public VisualShaderNodeParticleMultiplyByAxisAngle()
```

## 属性

### DegreesMode

```csharp
public bool DegreesMode { get; set; }
```

引擎名：`degrees_mode`

If `true`, the angle will be interpreted in degrees instead of radians.

## 方法

### IsDegreesMode

```csharp
public bool IsDegreesMode()
```

引擎名：`is_degrees_mode`

### SetDegreesMode

```csharp
public void SetDegreesMode(bool enabled)
```

引擎名：`set_degrees_mode`
