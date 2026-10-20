<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# UniformSetCacheRD

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Uniform set cache manager for [`RenderingDevice`](RenderingDevice.md)-based renderers. Provides a way to create a uniform set and reuse it in subsequent calls for as long as the uniform set exists. Uniform set will automatically be cleaned up when dependent objects are freed.

## 构造函数

### UniformSetCacheRD

```csharp
public UniformSetCacheRD()
```

## 方法

### GetCache

```csharp
public static Rid GetCache(Rid shader, uint set, Array<RDUniform> uniforms)
```

引擎名：`get_cache`

Creates/returns a cached uniform set based on the provided uniforms for a given shader.
