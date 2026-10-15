<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderSceneBuffersConfiguration

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This configuration object is created and populated by the render engine on a viewport change and used to (re)configure a [`RenderSceneBuffers`](RenderSceneBuffers.md) object.

## 构造函数

### RenderSceneBuffersConfiguration

```csharp
public RenderSceneBuffersConfiguration()
```

## 属性

### AnisotropicFilteringLevel

```csharp
public RenderingServer.ViewportAnisotropicFiltering AnisotropicFilteringLevel { get; set; }
```

引擎名：`anisotropic_filtering_level`

Level of the anisotropic filter.

### FsrSharpness

```csharp
public float FsrSharpness { get; set; }
```

引擎名：`fsr_sharpness`

FSR Sharpness applicable if FSR upscaling is used.

### InternalSize

```csharp
public Vector2I InternalSize { get; set; }
```

引擎名：`internal_size`

The size of the 3D render buffer used for rendering.

### Msaa3D

```csharp
public RenderingServer.ViewportMsaa Msaa3D { get; set; }
```

The MSAA mode we're using for 3D rendering.

### RenderTarget

```csharp
public Rid RenderTarget { get; set; }
```

引擎名：`render_target`

The render target associated with these buffer.

### Scaling3DMode

```csharp
public RenderingServer.ViewportScaling3DMode Scaling3DMode { get; set; }
```

The requested scaling mode with which we upscale/downscale if `InternalSize` and `TargetSize` are not equal.

### ScreenSpaceAA

```csharp
public RenderingServer.ViewportScreenSpaceAA ScreenSpaceAA { get; set; }
```

引擎名：`screen_space_aa`

The requested screen space AA applied in post processing.

### TargetSize

```csharp
public Vector2I TargetSize { get; set; }
```

引擎名：`target_size`

The target (upscale) size if scaling is used.

### TextureMipmapBias

```csharp
public float TextureMipmapBias { get; set; }
```

引擎名：`texture_mipmap_bias`

Bias applied to mipmaps.

**Note:** This property is only supported in the Forward+ and Mobile renderers, not Compatibility. In Compatibility, this property is always treated as if it was set to `0.0`.

### ViewCount

```csharp
public uint ViewCount { get; set; }
```

引擎名：`view_count`

The number of views we're rendering.

## 方法

### GetAnisotropicFilteringLevel

```csharp
public RenderingServer.ViewportAnisotropicFiltering GetAnisotropicFilteringLevel()
```

引擎名：`get_anisotropic_filtering_level`

### GetFsrSharpness

```csharp
public float GetFsrSharpness()
```

引擎名：`get_fsr_sharpness`

### GetInternalSize

```csharp
public Vector2I GetInternalSize()
```

引擎名：`get_internal_size`

### GetMsaa3D

```csharp
public RenderingServer.ViewportMsaa GetMsaa3D()
```

### GetRenderTarget

```csharp
public Rid GetRenderTarget()
```

引擎名：`get_render_target`

### GetScaling3DMode

```csharp
public RenderingServer.ViewportScaling3DMode GetScaling3DMode()
```

### GetScreenSpaceAA

```csharp
public RenderingServer.ViewportScreenSpaceAA GetScreenSpaceAA()
```

引擎名：`get_screen_space_aa`

### GetTargetSize

```csharp
public Vector2I GetTargetSize()
```

引擎名：`get_target_size`

### GetTextureMipmapBias

```csharp
public float GetTextureMipmapBias()
```

引擎名：`get_texture_mipmap_bias`

### GetViewCount

```csharp
public uint GetViewCount()
```

引擎名：`get_view_count`

### SetAnisotropicFilteringLevel

```csharp
public void SetAnisotropicFilteringLevel(RenderingServer.ViewportAnisotropicFiltering anisotropicFilteringLevel)
```

引擎名：`set_anisotropic_filtering_level`

### SetFsrSharpness

```csharp
public void SetFsrSharpness(float fsrSharpness)
```

引擎名：`set_fsr_sharpness`

### SetInternalSize

```csharp
public void SetInternalSize(Vector2I internalSize)
```

引擎名：`set_internal_size`

### SetMsaa3D

```csharp
public void SetMsaa3D(RenderingServer.ViewportMsaa msaa3D)
```

### SetRenderTarget

```csharp
public void SetRenderTarget(Rid renderTarget)
```

引擎名：`set_render_target`

### SetScaling3DMode

```csharp
public void SetScaling3DMode(RenderingServer.ViewportScaling3DMode scaling3DMode)
```

### SetScreenSpaceAA

```csharp
public void SetScreenSpaceAA(RenderingServer.ViewportScreenSpaceAA screenSpaceAA)
```

引擎名：`set_screen_space_aa`

### SetTargetSize

```csharp
public void SetTargetSize(Vector2I targetSize)
```

引擎名：`set_target_size`

### SetTextureMipmapBias

```csharp
public void SetTextureMipmapBias(float textureMipmapBias)
```

引擎名：`set_texture_mipmap_bias`

### SetViewCount

```csharp
public void SetViewCount(uint viewCount)
```

引擎名：`set_view_count`
