<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeParticleAccelerator

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Particle accelerator can be used in "process" step of particle shader. It will accelerate the particles. Connect it to the Velocity output port.

## 构造函数

### VisualShaderNodeParticleAccelerator

```csharp
public VisualShaderNodeParticleAccelerator()
```

## 属性

### Mode

```csharp
public VisualShaderNodeParticleAccelerator.ModeEnum Mode { get; set; }
```

引擎名：`mode`

Defines in what manner the particles will be accelerated.

## 方法

### GetMode

```csharp
public VisualShaderNodeParticleAccelerator.ModeEnum GetMode()
```

引擎名：`get_mode`

### SetMode

```csharp
public void SetMode(VisualShaderNodeParticleAccelerator.ModeEnum mode)
```

引擎名：`set_mode`

## ModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Linear = 0`：The particles will be accelerated based on their velocity.
- `Radial = 1`：The particles will be accelerated towards or away from the center.
- `Tangential = 2`：The particles will be accelerated tangentially to the radius vector from center to their position.
- `Max = 3`：Represents the size of the `VisualShaderNodeParticleAccelerator.ModeEnum` enum.
