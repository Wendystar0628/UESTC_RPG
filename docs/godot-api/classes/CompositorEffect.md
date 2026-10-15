<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CompositorEffect

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource defines a custom rendering effect that can be applied to [`Viewport`](Viewport.md)s through the viewports' [`Environment`](Environment.md). You can implement a callback that is called during rendering at a given stage of the rendering pipeline and allows you to insert additional passes. Note that this callback happens on the rendering thread. CompositorEffect is an abstract base class and must be extended to implement specific rendering logic.

## 构造函数

### CompositorEffect

```csharp
public CompositorEffect()
```

## 属性

### AccessResolvedColor

```csharp
public bool AccessResolvedColor { get; set; }
```

引擎名：`access_resolved_color`

If `true` and MSAA is enabled, this will trigger a color buffer resolve before the effect is run.

**Note:** In `_RenderCallback()`, to access the resolved buffer use:

```csharp
var render_scene_buffers = render_data.get_render_scene_buffers()
var color_buffer = render_scene_buffers.get_texture("render_buffers", "color")

```

### AccessResolvedDepth

```csharp
public bool AccessResolvedDepth { get; set; }
```

引擎名：`access_resolved_depth`

If `true` and MSAA is enabled, this will trigger a depth buffer resolve before the effect is run.

**Note:** In `_RenderCallback()`, to access the resolved buffer use:

```csharp
var render_scene_buffers = render_data.get_render_scene_buffers()
var depth_buffer = render_scene_buffers.get_texture("render_buffers", "depth")

```

### EffectCallbackType

```csharp
public CompositorEffect.EffectCallbackTypeEnum EffectCallbackType { get; set; }
```

引擎名：`effect_callback_type`

The type of effect that is implemented, determines at what stage of rendering the callback is called.

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

If `true` this rendering effect is applied to any viewport it is added to.

### NeedsMotionVectors

```csharp
public bool NeedsMotionVectors { get; set; }
```

引擎名：`needs_motion_vectors`

If `true` this triggers motion vectors being calculated during the opaque render state.

**Note:** In `_RenderCallback()`, to access the motion vector buffer use:

```csharp
var render_scene_buffers = render_data.get_render_scene_buffers()
var motion_buffer = render_scene_buffers.get_velocity_texture()

```

### NeedsNormalRoughness

```csharp
public bool NeedsNormalRoughness { get; set; }
```

引擎名：`needs_normal_roughness`

If `true` this triggers normal and roughness data to be output during our depth pre-pass, only applicable for the Forward+ renderer.

**Note:** In `_RenderCallback()`, to access the roughness buffer use:

```csharp
var render_scene_buffers = render_data.get_render_scene_buffers()
var roughness_buffer = render_scene_buffers.get_texture("forward_clustered", "normal_roughness")

```

The raw normal and roughness buffer is stored in an optimized format, different than the one available in Spatial shaders. When sampling the buffer, a conversion function must be applied. Use this function, copied from [here](https://github.com/godotengine/godot/blob/da5f39889f155658cef7f7ec3cc1abb94e17d815/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered_inc.glsl#L334-L341):

```csharp
vec4 normal_roughness_compatibility(vec4 p_normal_roughness) {
float roughness = p_normal_roughness.w;
if (roughness > 0.5) {
roughness = 1.0 - roughness;
}
roughness /= (127.0 / 255.0);
return vec4(normalize(p_normal_roughness.xyz * 2.0 - 1.0) * 0.5 + 0.5, roughness);
}

```

### NeedsSeparateSpecular

```csharp
public bool NeedsSeparateSpecular { get; set; }
```

引擎名：`needs_separate_specular`

If `true` this triggers specular data being rendered to a separate buffer and combined after effects have been applied, only applicable for the Forward+ renderer.

## 方法

### GetAccessResolvedColor

```csharp
public bool GetAccessResolvedColor()
```

引擎名：`get_access_resolved_color`

### GetAccessResolvedDepth

```csharp
public bool GetAccessResolvedDepth()
```

引擎名：`get_access_resolved_depth`

### GetEffectCallbackType

```csharp
public CompositorEffect.EffectCallbackTypeEnum GetEffectCallbackType()
```

引擎名：`get_effect_callback_type`

### GetEnabled

```csharp
public bool GetEnabled()
```

引擎名：`get_enabled`

### GetNeedsMotionVectors

```csharp
public bool GetNeedsMotionVectors()
```

引擎名：`get_needs_motion_vectors`

### GetNeedsNormalRoughness

```csharp
public bool GetNeedsNormalRoughness()
```

引擎名：`get_needs_normal_roughness`

### GetNeedsSeparateSpecular

```csharp
public bool GetNeedsSeparateSpecular()
```

引擎名：`get_needs_separate_specular`

### SetAccessResolvedColor

```csharp
public void SetAccessResolvedColor(bool enable)
```

引擎名：`set_access_resolved_color`

### SetAccessResolvedDepth

```csharp
public void SetAccessResolvedDepth(bool enable)
```

引擎名：`set_access_resolved_depth`

### SetEffectCallbackType

```csharp
public void SetEffectCallbackType(CompositorEffect.EffectCallbackTypeEnum effectCallbackType)
```

引擎名：`set_effect_callback_type`

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetNeedsMotionVectors

```csharp
public void SetNeedsMotionVectors(bool enable)
```

引擎名：`set_needs_motion_vectors`

### SetNeedsNormalRoughness

```csharp
public void SetNeedsNormalRoughness(bool enable)
```

引擎名：`set_needs_normal_roughness`

### SetNeedsSeparateSpecular

```csharp
public void SetNeedsSeparateSpecular(bool enable)
```

引擎名：`set_needs_separate_specular`

### _RenderCallback

```csharp
public virtual void _RenderCallback(int effectCallbackType, RenderData renderData)
```

引擎名：`_render_callback`

Implement this function with your custom rendering code. `effectCallbackType` should always match the effect callback type you've specified in `EffectCallbackType`. `renderData` provides access to the rendering state, it is only valid during rendering and should not be stored.

## EffectCallbackTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `PreOpaque = 0`：The callback is called before our opaque rendering pass, but after depth prepass (if applicable).
- `PostOpaque = 1`：The callback is called after our opaque rendering pass, but before our sky is rendered.
- `PostSky = 2`：The callback is called after our sky is rendered, but before our back buffers are created (and if enabled, before subsurface scattering and/or screen space reflections).
- `PreTransparent = 3`：The callback is called before our transparent rendering pass, but after our sky is rendered and we've created our back buffers.
- `PostTransparent = 4`：The callback is called after our transparent rendering pass, but before any built-in post-processing effects and output to our render target.
- `Max = 5`：Represents the size of the `CompositorEffect.EffectCallbackTypeEnum` enum.
