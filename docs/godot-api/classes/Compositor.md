<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Compositor

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The compositor resource stores attributes used to customize how a [`Viewport`](Viewport.md) is rendered.

## 构造函数

### Compositor

```csharp
public Compositor()
```

## 属性

### CompositorEffects

```csharp
public Array<CompositorEffect> CompositorEffects { get; set; }
```

引擎名：`compositor_effects`

The custom [`CompositorEffect`](CompositorEffect.md)s that are applied during rendering of viewports using this compositor.

## 方法

### GetCompositorEffects

```csharp
public Array<CompositorEffect> GetCompositorEffects()
```

引擎名：`get_compositor_effects`

### SetCompositorEffects

```csharp
public void SetCompositorEffects(Array<CompositorEffect> compositorEffects)
```

引擎名：`set_compositor_effects`
