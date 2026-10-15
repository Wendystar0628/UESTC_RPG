<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRCompositionLayer

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Composition layers allow 2D viewports to be displayed inside of the headset by the XR compositor through special projections that retain their quality. This allows for rendering clear text while keeping the layer at a native resolution.

**Note:** If the OpenXR runtime doesn't support the given composition layer type, a fallback mesh can be generated with a [`ViewportTexture`](ViewportTexture.md), in order to emulate the composition layer.

## 属性

### AlphaBlend

```csharp
public bool AlphaBlend { get; set; }
```

引擎名：`alpha_blend`

Enables the blending the layer using its alpha channel.

Can be combined with [`Viewport.TransparentBg`](Viewport.md) to give the layer a transparent background.

### AndroidSurfaceSize

```csharp
public Vector2I AndroidSurfaceSize { get; set; }
```

引擎名：`android_surface_size`

The size of the Android surface to create if `UseAndroidSurface` is enabled.

### EnableHolePunch

```csharp
public bool EnableHolePunch { get; set; }
```

引擎名：`enable_hole_punch`

Enables a technique called "hole punching", which allows putting the composition layer behind the main projection layer (i.e. setting `SortOrder` to a negative value) while "punching a hole" through everything rendered by Godot so that the layer is still visible.

This can be used to create the illusion that the composition layer exists in the same 3D space as everything rendered by Godot, allowing objects to appear to pass both behind or in front of the composition layer.

### EyeVisibility

```csharp
public OpenXRCompositionLayer.EyeVisibilityEnum EyeVisibility { get; set; }
```

引擎名：`eye_visibility`

The eye(s) the composition layer is visible to.

**Note:** Not all composition layer types or runtimes support restricting visibility to a single eye.

### LayerViewport

```csharp
public SubViewport LayerViewport { get; set; }
```

引擎名：`layer_viewport`

The [`SubViewport`](SubViewport.md) to render on the composition layer.

### ProtectedContent

```csharp
public bool ProtectedContent { get; set; }
```

引擎名：`protected_content`

If enabled, the OpenXR swapchain will be created with the `XR_SWAPCHAIN_CREATE_PROTECTED_CONTENT_BIT` flag, which will protect its contents from CPU access.

When used with an Android Surface, this may allow DRM content to be presented, and will only take effect when the Surface is first created; later changes to this property will have no effect.

### SortOrder

```csharp
public int SortOrder { get; set; }
```

引擎名：`sort_order`

The sort order for this composition layer. Higher numbers will be shown in front of lower numbers.

**Note:** This will have no effect if a fallback mesh is being used.

### SwapchainStateAlphaSwizzle

```csharp
public OpenXRCompositionLayer.Swizzle SwapchainStateAlphaSwizzle { get; set; }
```

引擎名：`swapchain_state_alpha_swizzle`

The swizzle value for the alpha channel of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateBlueSwizzle

```csharp
public OpenXRCompositionLayer.Swizzle SwapchainStateBlueSwizzle { get; set; }
```

引擎名：`swapchain_state_blue_swizzle`

The swizzle value for the blue channel of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateBorderColor

```csharp
public Color SwapchainStateBorderColor { get; set; }
```

引擎名：`swapchain_state_border_color`

The border color of the swapchain state that is used when the wrap mode clamps to the border.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateGreenSwizzle

```csharp
public OpenXRCompositionLayer.Swizzle SwapchainStateGreenSwizzle { get; set; }
```

引擎名：`swapchain_state_green_swizzle`

The swizzle value for the green channel of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateHorizontalWrap

```csharp
public OpenXRCompositionLayer.Wrap SwapchainStateHorizontalWrap { get; set; }
```

引擎名：`swapchain_state_horizontal_wrap`

The horizontal wrap mode of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateMagFilter

```csharp
public OpenXRCompositionLayer.Filter SwapchainStateMagFilter { get; set; }
```

引擎名：`swapchain_state_mag_filter`

The magnification filter of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateMaxAnisotropy

```csharp
public float SwapchainStateMaxAnisotropy { get; set; }
```

引擎名：`swapchain_state_max_anisotropy`

The max anisotropy of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateMinFilter

```csharp
public OpenXRCompositionLayer.Filter SwapchainStateMinFilter { get; set; }
```

引擎名：`swapchain_state_min_filter`

The minification filter of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateMipmapMode

```csharp
public OpenXRCompositionLayer.MipmapMode SwapchainStateMipmapMode { get; set; }
```

引擎名：`swapchain_state_mipmap_mode`

The mipmap mode of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateRedSwizzle

```csharp
public OpenXRCompositionLayer.Swizzle SwapchainStateRedSwizzle { get; set; }
```

引擎名：`swapchain_state_red_swizzle`

The swizzle value for the red channel of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### SwapchainStateVerticalWrap

```csharp
public OpenXRCompositionLayer.Wrap SwapchainStateVerticalWrap { get; set; }
```

引擎名：`swapchain_state_vertical_wrap`

The vertical wrap mode of the swapchain state.

**Note:** This property only has an effect on devices that support the OpenXR XR_FB_swapchain_update_state OpenGLES/Vulkan extensions.

### UseAndroidSurface

```csharp
public bool UseAndroidSurface { get; set; }
```

引擎名：`use_android_surface`

If enabled, an Android surface will be created (with the dimensions from `AndroidSurfaceSize`) which will provide the 2D content for the composition layer, rather than using `LayerViewport`.

See `GetAndroidSurface()` for information about how to get the surface so that your application can draw to it.

**Note:** This will only work in Android builds.

## 方法

### GetAlphaBlend

```csharp
public bool GetAlphaBlend()
```

引擎名：`get_alpha_blend`

### GetAlphaSwizzle

```csharp
public OpenXRCompositionLayer.Swizzle GetAlphaSwizzle()
```

引擎名：`get_alpha_swizzle`

### GetAndroidSurface

```csharp
public JavaObject GetAndroidSurface()
```

引擎名：`get_android_surface`

Returns a [`JavaObject`](JavaObject.md) representing an `android.view.Surface` if `UseAndroidSurface` is enabled and OpenXR has created the surface. Otherwise, this will return `null`.

**Note:** The surface can only be created during an active OpenXR session. So, if `UseAndroidSurface` is enabled outside of an OpenXR session, it won't be created until a new session fully starts.

### GetAndroidSurfaceSize

```csharp
public Vector2I GetAndroidSurfaceSize()
```

引擎名：`get_android_surface_size`

### GetBlueSwizzle

```csharp
public OpenXRCompositionLayer.Swizzle GetBlueSwizzle()
```

引擎名：`get_blue_swizzle`

### GetBorderColor

```csharp
public Color GetBorderColor()
```

引擎名：`get_border_color`

### GetEnableHolePunch

```csharp
public bool GetEnableHolePunch()
```

引擎名：`get_enable_hole_punch`

### GetEyeVisibility

```csharp
public OpenXRCompositionLayer.EyeVisibilityEnum GetEyeVisibility()
```

引擎名：`get_eye_visibility`

### GetGreenSwizzle

```csharp
public OpenXRCompositionLayer.Swizzle GetGreenSwizzle()
```

引擎名：`get_green_swizzle`

### GetHorizontalWrap

```csharp
public OpenXRCompositionLayer.Wrap GetHorizontalWrap()
```

引擎名：`get_horizontal_wrap`

### GetLayerViewport

```csharp
public SubViewport GetLayerViewport()
```

引擎名：`get_layer_viewport`

### GetMagFilter

```csharp
public OpenXRCompositionLayer.Filter GetMagFilter()
```

引擎名：`get_mag_filter`

### GetMaxAnisotropy

```csharp
public float GetMaxAnisotropy()
```

引擎名：`get_max_anisotropy`

### GetMinFilter

```csharp
public OpenXRCompositionLayer.Filter GetMinFilter()
```

引擎名：`get_min_filter`

### GetMipmapMode

```csharp
public OpenXRCompositionLayer.MipmapMode GetMipmapMode()
```

引擎名：`get_mipmap_mode`

### GetRedSwizzle

```csharp
public OpenXRCompositionLayer.Swizzle GetRedSwizzle()
```

引擎名：`get_red_swizzle`

### GetSortOrder

```csharp
public int GetSortOrder()
```

引擎名：`get_sort_order`

### GetUseAndroidSurface

```csharp
public bool GetUseAndroidSurface()
```

引擎名：`get_use_android_surface`

### GetVerticalWrap

```csharp
public OpenXRCompositionLayer.Wrap GetVerticalWrap()
```

引擎名：`get_vertical_wrap`

### IntersectsRay

```csharp
public Vector2 IntersectsRay(Vector3 origin, Vector3 direction)
```

引擎名：`intersects_ray`

Returns UV coordinates where the given ray intersects with the composition layer. `origin` and `direction` must be in global space.

Returns `Vector2(-1.0, -1.0)` if the ray doesn't intersect.

### IsNativelySupported

```csharp
public bool IsNativelySupported()
```

引擎名：`is_natively_supported`

Returns `true` if the OpenXR runtime natively supports this composition layer type.

**Note:** This will only return an accurate result after the OpenXR session has started.

### IsProtectedContent

```csharp
public bool IsProtectedContent()
```

引擎名：`is_protected_content`

### SetAlphaBlend

```csharp
public void SetAlphaBlend(bool enabled)
```

引擎名：`set_alpha_blend`

### SetAlphaSwizzle

```csharp
public void SetAlphaSwizzle(OpenXRCompositionLayer.Swizzle mode)
```

引擎名：`set_alpha_swizzle`

### SetAndroidSurfaceSize

```csharp
public void SetAndroidSurfaceSize(Vector2I size)
```

引擎名：`set_android_surface_size`

### SetBlueSwizzle

```csharp
public void SetBlueSwizzle(OpenXRCompositionLayer.Swizzle mode)
```

引擎名：`set_blue_swizzle`

### SetBorderColor

```csharp
public void SetBorderColor(Color color)
```

引擎名：`set_border_color`

### SetEnableHolePunch

```csharp
public void SetEnableHolePunch(bool enable)
```

引擎名：`set_enable_hole_punch`

### SetEyeVisibility

```csharp
public void SetEyeVisibility(OpenXRCompositionLayer.EyeVisibilityEnum eyeVisibility)
```

引擎名：`set_eye_visibility`

### SetGreenSwizzle

```csharp
public void SetGreenSwizzle(OpenXRCompositionLayer.Swizzle mode)
```

引擎名：`set_green_swizzle`

### SetHorizontalWrap

```csharp
public void SetHorizontalWrap(OpenXRCompositionLayer.Wrap mode)
```

引擎名：`set_horizontal_wrap`

### SetLayerViewport

```csharp
public void SetLayerViewport(SubViewport viewport)
```

引擎名：`set_layer_viewport`

### SetMagFilter

```csharp
public void SetMagFilter(OpenXRCompositionLayer.Filter mode)
```

引擎名：`set_mag_filter`

### SetMaxAnisotropy

```csharp
public void SetMaxAnisotropy(float value)
```

引擎名：`set_max_anisotropy`

### SetMinFilter

```csharp
public void SetMinFilter(OpenXRCompositionLayer.Filter mode)
```

引擎名：`set_min_filter`

### SetMipmapMode

```csharp
public void SetMipmapMode(OpenXRCompositionLayer.MipmapMode mode)
```

引擎名：`set_mipmap_mode`

### SetProtectedContent

```csharp
public void SetProtectedContent(bool protectedContent)
```

引擎名：`set_protected_content`

### SetRedSwizzle

```csharp
public void SetRedSwizzle(OpenXRCompositionLayer.Swizzle mode)
```

引擎名：`set_red_swizzle`

### SetSortOrder

```csharp
public void SetSortOrder(int order)
```

引擎名：`set_sort_order`

### SetUseAndroidSurface

```csharp
public void SetUseAndroidSurface(bool enable)
```

引擎名：`set_use_android_surface`

### SetVerticalWrap

```csharp
public void SetVerticalWrap(OpenXRCompositionLayer.Wrap mode)
```

引擎名：`set_vertical_wrap`

## EyeVisibilityEnum

命名空间 `Godot`；enum

### 枚举值

- `Both = 0`：The layer is visible to both the left and right eyes.
- `Left = 1`：The layer is visible only to the left eye.
- `Right = 2`：The layer is visible only to the right eye.

## Filter

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：Perform nearest-neighbor filtering when sampling the texture.
- `Linear = 1`：Perform linear filtering when sampling the texture.
- `Cubic = 2`：Perform cubic filtering when sampling the texture.

## MipmapMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disable mipmapping.
- `Nearest = 1`：Use the mipmap of the nearest resolution.
- `Linear = 2`：Use linear interpolation of the two mipmaps of the nearest resolution.

## Swizzle

命名空间 `Godot`；enum

### 枚举值

- `Red = 0`：Maps a color channel to the value of the red channel.
- `Green = 1`：Maps a color channel to the value of the green channel.
- `Blue = 2`：Maps a color channel to the value of the blue channel.
- `Alpha = 3`：Maps a color channel to the value of the alpha channel.
- `Zero = 4`：Maps a color channel to the value of zero.
- `One = 5`：Maps a color channel to the value of one.

## Wrap

命名空间 `Godot`；enum

### 枚举值

- `ClampToBorder = 0`：Clamp the texture to its specified border color.
- `ClampToEdge = 1`：Clamp the texture to its edge color.
- `Repeat = 2`：Repeat the texture infinitely.
- `MirroredRepeat = 3`：Repeat the texture infinitely, mirroring it on each repeat.
- `MirrorClampToEdge = 4`：Mirror the texture once and then clamp the texture to its edge color.
