<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LightmapperRD

命名空间 `Godot`；class；继承 [`Lightmapper`](Lightmapper.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

LightmapperRD ("RD" stands for [`RenderingDevice`](RenderingDevice.md)) is the built-in GPU-based lightmapper for use with [`LightmapGI`](LightmapGI.md). On most dedicated GPUs, it can bake lightmaps much faster than most CPU-based lightmappers. LightmapperRD uses compute shaders to bake lightmaps, so it does not require CUDA or OpenCL libraries to be installed to be usable.

**Note:** This lightmapper requires the GPU to support the [`RenderingDevice`](RenderingDevice.md) backend (Forward+ and Mobile renderers). When using the Compatibility renderer, baking will use a temporary [`RenderingDevice`](RenderingDevice.md). Support for [`RenderingDevice`](RenderingDevice.md) is not required to *render* lightmaps that were already baked beforehand.

## 构造函数

### LightmapperRD

```csharp
public LightmapperRD()
```
