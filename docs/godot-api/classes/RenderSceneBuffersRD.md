<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderSceneBuffersRD

命名空间 `Godot`；class；继承 [`RenderSceneBuffers`](RenderSceneBuffers.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object manages all 3D rendering buffers for the rendering device based renderers. An instance of this object is created for every viewport that has 3D rendering enabled. See also [`RenderSceneBuffers`](RenderSceneBuffers.md).

All buffers are organized in **contexts**. The default context is called **render_buffers** and can contain amongst others the color buffer, depth buffer, velocity buffers, VRS density map and MSAA variants of these buffers.

Buffers are only guaranteed to exist during rendering of the viewport.

**Note:** This is an internal rendering server object. Do not instantiate this class from a script.

## 构造函数

### RenderSceneBuffersRD

```csharp
public RenderSceneBuffersRD()
```

## 方法

### ClearContext

```csharp
public void ClearContext(StringName context)
```

引擎名：`clear_context`

Frees all buffers related to this context.

### CreateTexture

```csharp
public Rid CreateTexture(StringName context, StringName name, RenderingDevice.DataFormat dataFormat, uint usageBits, RenderingDevice.TextureSamples textureSamples, Vector2I size, uint layers, uint mipmaps, bool unique)
```

引擎名：`create_texture`

Create a new texture with the given definition and cache this under the given name. Will return the existing texture if it already exists.

### CreateTexture

```csharp
public Rid CreateTexture(StringName context, StringName name, RenderingDevice.DataFormat dataFormat, uint usageBits, RenderingDevice.TextureSamples textureSamples, Vector2I size, uint layers, uint mipmaps, bool unique, bool discardable)
```

引擎名：`create_texture`

Create a new texture with the given definition and cache this under the given name. Will return the existing texture if it already exists.

### CreateTextureFromFormat

```csharp
public Rid CreateTextureFromFormat(StringName context, StringName name, RDTextureFormat format, RDTextureView view, bool unique)
```

引擎名：`create_texture_from_format`

Create a new texture using the given format and view and cache this under the given name. Will return the existing texture if it already exists.

### CreateTextureView

```csharp
public Rid CreateTextureView(StringName context, StringName name, StringName viewName, RDTextureView view)
```

引擎名：`create_texture_view`

Create a new texture view for an existing texture and cache this under the given `viewName`. Will return the existing texture view if it already exists. Will error if the source texture doesn't exist.

### GetColorLayer

```csharp
public Rid GetColorLayer(uint layer)
```

引擎名：`get_color_layer`

Returns the specified layer from the color texture we are rendering 3D content to.

If `msaa` is `true` and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetColorLayer

```csharp
public Rid GetColorLayer(uint layer, bool msaa = false)
```

引擎名：`get_color_layer`

Returns the specified layer from the color texture we are rendering 3D content to.

If `msaa` is `true` and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetColorTexture

```csharp
public Rid GetColorTexture()
```

引擎名：`get_color_texture`

Returns the color texture we are rendering 3D content to. If multiview is used this will be a texture array with all views.

If `msaa` is `true` and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetColorTexture

```csharp
public Rid GetColorTexture(bool msaa = false)
```

引擎名：`get_color_texture`

Returns the color texture we are rendering 3D content to. If multiview is used this will be a texture array with all views.

If `msaa` is `true` and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetDepthLayer

```csharp
public Rid GetDepthLayer(uint layer)
```

引擎名：`get_depth_layer`

Returns the specified layer from the depth texture we are rendering 3D content to.

If `msaa` is `true` and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetDepthLayer

```csharp
public Rid GetDepthLayer(uint layer, bool msaa = false)
```

引擎名：`get_depth_layer`

Returns the specified layer from the depth texture we are rendering 3D content to.

If `msaa` is `true` and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetDepthTexture

```csharp
public Rid GetDepthTexture()
```

引擎名：`get_depth_texture`

Returns the depth texture we are rendering 3D content to. If multiview is used this will be a texture array with all views.

If `msaa` is `true` and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetDepthTexture

```csharp
public Rid GetDepthTexture(bool msaa = false)
```

引擎名：`get_depth_texture`

Returns the depth texture we are rendering 3D content to. If multiview is used this will be a texture array with all views.

If `msaa` is `true` and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetFsrSharpness

```csharp
public float GetFsrSharpness()
```

引擎名：`get_fsr_sharpness`

Returns the FSR sharpness value used while rendering the 3D content (if `GetScaling3DMode()` is an FSR mode).

### GetInternalSize

```csharp
public Vector2I GetInternalSize()
```

引擎名：`get_internal_size`

Returns the internal size of the render buffer (size before upscaling) with which textures are created by default.

### GetMsaa3D

```csharp
public RenderingServer.ViewportMsaa GetMsaa3D()
```

Returns the applied 3D MSAA mode for this viewport.

### GetRenderTarget

```csharp
public Rid GetRenderTarget()
```

引擎名：`get_render_target`

Returns the render target associated with this buffers object.

### GetScaling3DMode

```csharp
public RenderingServer.ViewportScaling3DMode GetScaling3DMode()
```

Returns the scaling mode used for upscaling.

### GetScreenSpaceAA

```csharp
public RenderingServer.ViewportScreenSpaceAA GetScreenSpaceAA()
```

引擎名：`get_screen_space_aa`

Returns the screen-space antialiasing method applied.

### GetTargetSize

```csharp
public Vector2I GetTargetSize()
```

引擎名：`get_target_size`

Returns the target size of the render buffer (size after upscaling).

### GetTexture

```csharp
public Rid GetTexture(StringName context, StringName name)
```

引擎名：`get_texture`

Returns a cached texture with this name.

### GetTextureFormat

```csharp
public RDTextureFormat GetTextureFormat(StringName context, StringName name)
```

引擎名：`get_texture_format`

Returns the texture format information with which a cached texture was created.

### GetTextureSamples

```csharp
public RenderingDevice.TextureSamples GetTextureSamples()
```

引擎名：`get_texture_samples`

Returns the number of MSAA samples used.

### GetTextureSlice

```csharp
public Rid GetTextureSlice(StringName context, StringName name, uint layer, uint mipmap, uint layers, uint mipmaps)
```

引擎名：`get_texture_slice`

Returns a specific slice (layer or mipmap) for a cached texture.

### GetTextureSliceSize

```csharp
public Vector2I GetTextureSliceSize(StringName context, StringName name, uint mipmap)
```

引擎名：`get_texture_slice_size`

Returns the texture size of a given slice of a cached texture.

### GetTextureSliceView

```csharp
public Rid GetTextureSliceView(StringName context, StringName name, uint layer, uint mipmap, uint layers, uint mipmaps, RDTextureView view)
```

引擎名：`get_texture_slice_view`

Returns a specific view of a slice (layer or mipmap) for a cached texture.

### GetUseDebanding

```csharp
public bool GetUseDebanding()
```

引擎名：`get_use_debanding`

Returns `true` if debanding is enabled.

### GetUseTaa

```csharp
public bool GetUseTaa()
```

引擎名：`get_use_taa`

Returns `true` if TAA is enabled.

### GetVelocityLayer

```csharp
public Rid GetVelocityLayer(uint layer)
```

引擎名：`get_velocity_layer`

Returns the specified layer from the velocity texture we are rendering 3D content to.

### GetVelocityLayer

```csharp
public Rid GetVelocityLayer(uint layer, bool msaa = false)
```

引擎名：`get_velocity_layer`

Returns the specified layer from the velocity texture we are rendering 3D content to.

### GetVelocityTexture

```csharp
public Rid GetVelocityTexture()
```

引擎名：`get_velocity_texture`

Returns the velocity texture we are rendering 3D content to. If multiview is used this will be a texture array with all views.

If `msaa` is **true** and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetVelocityTexture

```csharp
public Rid GetVelocityTexture(bool msaa = false)
```

引擎名：`get_velocity_texture`

Returns the velocity texture we are rendering 3D content to. If multiview is used this will be a texture array with all views.

If `msaa` is **true** and MSAA is enabled, this returns the MSAA variant of the buffer.

### GetViewCount

```csharp
public uint GetViewCount()
```

引擎名：`get_view_count`

Returns the view count for the associated viewport.

### HasTexture

```csharp
public bool HasTexture(StringName context, StringName name)
```

引擎名：`has_texture`

Returns `true` if a cached texture exists for this name.
