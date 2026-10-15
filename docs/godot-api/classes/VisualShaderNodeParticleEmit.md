<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeParticleEmit

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This node internally calls `emit_subparticle` shader method. It will emit a particle from the configured sub-emitter and also allows to customize how its emitted. Requires a sub-emitter assigned to the particles node with this shader.

## 构造函数

### VisualShaderNodeParticleEmit

```csharp
public VisualShaderNodeParticleEmit()
```

## 属性

### Flags

```csharp
public VisualShaderNodeParticleEmit.EmitFlags Flags { get; set; }
```

引擎名：`flags`

Flags used to override the properties defined in the sub-emitter's process material.

## 方法

### GetFlags

```csharp
public VisualShaderNodeParticleEmit.EmitFlags GetFlags()
```

引擎名：`get_flags`

### SetFlags

```csharp
public void SetFlags(VisualShaderNodeParticleEmit.EmitFlags flags)
```

引擎名：`set_flags`

## EmitFlags

命名空间 `Godot`；enum

### 枚举值

- `Position = 1`：If enabled, the particle starts with the position defined by this node.
- `RotScale = 2`：If enabled, the particle starts with the rotation and scale defined by this node.
- `Velocity = 4`：If enabled,the particle starts with the velocity defined by this node.
- `Color = 8`：If enabled, the particle starts with the color defined by this node.
- `Custom = 16`：If enabled, the particle starts with the `CUSTOM` data defined by this node.
