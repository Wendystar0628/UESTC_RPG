<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeParticleEmitter

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Particle emitter nodes can be used in "start" step of particle shaders and they define the starting position of the particles. Connect them to the Position output port.

## 属性

### Mode2D

```csharp
public bool Mode2D { get; set; }
```

If `true`, the result of this emitter is projected to 2D space. By default it is `false` and meant for use in 3D space.

## 方法

### IsMode2D

```csharp
public bool IsMode2D()
```

### SetMode2D

```csharp
public void SetMode2D(bool enabled)
```
