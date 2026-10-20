<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Viewport

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A `Viewport` creates a different view into the screen, or a sub-view inside another viewport. Child 2D nodes will display on it, and child Camera3D 3D nodes will render on it too.

Optionally, a viewport can have its own 2D or 3D world, so it doesn't share what it draws with other viewports.

Viewports can also choose to be audio listeners, so they generate positional audio depending on a 2D or 3D camera child of it.

Also, viewports can be assigned to different screens in case the devices have multiple screens.

Finally, viewports can also behave as render targets, in which case they will not be visible unless the associated texture is used to draw.

## 属性

### AnisotropicFilteringLevel

```csharp
public Viewport.AnisotropicFiltering AnisotropicFilteringLevel { get; set; }
```

引擎名：`anisotropic_filtering_level`

Sets the maximum number of samples to take when using anisotropic filtering on textures (as a power of two). A higher sample count will result in sharper textures at oblique angles, but is more expensive to compute. A value of `0` forcibly disables anisotropic filtering, even on materials where it is enabled.

The anisotropic filtering level also affects decals and light projectors if they are configured to use anisotropic filtering. See `ProjectSettings.rendering/textures/decals/filter` and `ProjectSettings.rendering/textures/light_projectors/filter`.

**Note:** In 3D, for this setting to have an effect, set [`BaseMaterial3D.TextureFilter`](BaseMaterial3D.md) to [`BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic`](BaseMaterial3D.md) or [`BaseMaterial3D.TextureFilterEnum.NearestWithMipmapsAnisotropic`](BaseMaterial3D.md) on materials.

**Note:** In 2D, for this setting to have an effect, set [`CanvasItem.TextureFilter`](CanvasItem.md) to [`CanvasItem.TextureFilterEnum.LinearWithMipmapsAnisotropic`](CanvasItem.md) or [`CanvasItem.TextureFilterEnum.NearestWithMipmapsAnisotropic`](CanvasItem.md) on the [`CanvasItem`](CanvasItem.md) node displaying the texture (or in [`CanvasTexture`](CanvasTexture.md)). However, anisotropic filtering is rarely useful in 2D, so only enable it for textures in 2D if it makes a meaningful visual difference.

### AudioListenerEnable2D

```csharp
public bool AudioListenerEnable2D { get; set; }
```

If `true`, the viewport will process 2D audio streams.

### AudioListenerEnable3D

```csharp
public bool AudioListenerEnable3D { get; set; }
```

If `true`, the viewport will process 3D audio streams.

### CanvasCullMask

```csharp
public uint CanvasCullMask { get; set; }
```

引擎名：`canvas_cull_mask`

The rendering layers in which this `Viewport` renders [`CanvasItem`](CanvasItem.md) nodes.

**Note:** A [`CanvasItem`](CanvasItem.md) does not inherit its parents' visibility layers. See [`CanvasItem.VisibilityLayer`](CanvasItem.md)'s description for details.

### CanvasItemDefaultTextureFilter

```csharp
public Viewport.DefaultCanvasItemTextureFilter CanvasItemDefaultTextureFilter { get; set; }
```

引擎名：`canvas_item_default_texture_filter`

The default filter mode used by [`CanvasItem`](CanvasItem.md) nodes in this viewport.

### CanvasItemDefaultTextureRepeat

```csharp
public Viewport.DefaultCanvasItemTextureRepeat CanvasItemDefaultTextureRepeat { get; set; }
```

引擎名：`canvas_item_default_texture_repeat`

The default repeat mode used by [`CanvasItem`](CanvasItem.md) nodes in this viewport.

### CanvasTransform

```csharp
public Transform2D CanvasTransform { get; set; }
```

引擎名：`canvas_transform`

The canvas transform of the viewport, useful for changing the on-screen positions of all child [`CanvasItem`](CanvasItem.md)s. This is relative to the global canvas transform of the viewport.

### DebugDraw

```csharp
public Viewport.DebugDrawEnum DebugDraw { get; set; }
```

引擎名：`debug_draw`

The overlay mode for test rendered geometry in debug purposes.

### Disable3D

```csharp
public bool Disable3D { get; set; }
```

Disable 3D rendering (but keep 2D rendering).

### FsrSharpness

```csharp
public float FsrSharpness { get; set; }
```

引擎名：`fsr_sharpness`

Determines how sharp the upscaled image will be when using the FSR upscaling mode. Sharpness halves with every whole number. Values go from 0.0 (sharpest) to 2.0. Values above 2.0 won't make a visible difference.

To control this property on the root viewport, set the `ProjectSettings.rendering/scaling_3d/fsr_sharpness` project setting.

### GlobalCanvasTransform

```csharp
public Transform2D GlobalCanvasTransform { get; set; }
```

引擎名：`global_canvas_transform`

The global canvas transform of the viewport. The canvas transform is relative to this.

### GuiDisableInput

```csharp
public bool GuiDisableInput { get; set; }
```

引擎名：`gui_disable_input`

If `true`, the viewport will not receive input events.

### GuiDragThreshold

```csharp
public int GuiDragThreshold { get; set; }
```

引擎名：`gui_drag_threshold`

The minimum distance the mouse cursor must move while pressed before a drag operation begins.

### GuiEmbedSubwindows

```csharp
public bool GuiEmbedSubwindows { get; set; }
```

引擎名：`gui_embed_subwindows`

If `true`, sub-windows (popups and dialogs) will be embedded inside application window as control-like nodes. If `false`, they will appear as separate windows handled by the operating system.

### GuiSnapControlsToPixels

```csharp
public bool GuiSnapControlsToPixels { get; set; }
```

引擎名：`gui_snap_controls_to_pixels`

If `true`, the GUI controls on the viewport will lay pixel perfectly.

### HandleInputLocally

```csharp
public bool HandleInputLocally { get; set; }
```

引擎名：`handle_input_locally`

If `true`, this viewport will mark incoming input events as handled by itself. If `false`, this is instead done by the first parent viewport that is set to handle input locally.

A [`SubViewportContainer`](SubViewportContainer.md) will automatically set this property to `false` for the `Viewport` contained inside of it.

See also `SetInputAsHandled()` and `IsInputHandled()`.

### MeshLodThreshold

```csharp
public float MeshLodThreshold { get; set; }
```

引擎名：`mesh_lod_threshold`

The automatic LOD bias to use for meshes rendered within the `Viewport` (this is analogous to [`ReflectionProbe.MeshLodThreshold`](ReflectionProbe.md)). Higher values will use less detailed versions of meshes that have LOD variations generated. If set to `0.0`, automatic LOD is disabled. Increase `MeshLodThreshold` to improve performance at the cost of geometry detail.

To control this property on the root viewport, set the `ProjectSettings.rendering/mesh_lod/lod_change/threshold_pixels` project setting.

**Note:** Depending on the mesh's attributes (vertex colors, blend shapes, ...), a mesh may have fewer levels of LOD generated to avoid visible distortion of the mesh once it is affected by vertex colors or blend shapes. Meshes with a very low vertex count will also not have any LODs generated, which means this setting will not affect them at all. In general, this setting makes the largest impact on static meshes with a high vertex count.

**Note:** `MeshLodThreshold` does not affect [`GeometryInstance3D`](GeometryInstance3D.md) visibility ranges (also known as "manual" LOD or hierarchical LOD).

### Msaa2D

```csharp
public Viewport.Msaa Msaa2D { get; set; }
```

The multisample antialiasing mode for 2D/Canvas rendering. A higher number results in smoother edges at the cost of significantly worse performance. A value of [`Viewport.Msaa.Msaa2X`](Viewport.md) or [`Viewport.Msaa.Msaa4X`](Viewport.md) is best unless targeting very high-end systems. This has no effect on shader-induced aliasing or texture aliasing.

See also `ProjectSettings.rendering/anti_aliasing/quality/msaa_2d` and [`RenderingServer.ViewportSetMsaa2D`](RenderingServer.md).

### Msaa3D

```csharp
public Viewport.Msaa Msaa3D { get; set; }
```

The multisample antialiasing mode for 3D rendering. A higher number results in smoother edges at the cost of significantly worse performance. A value of [`Viewport.Msaa.Msaa2X`](Viewport.md) or [`Viewport.Msaa.Msaa4X`](Viewport.md) is best unless targeting very high-end systems. See also bilinear scaling 3D `Scaling3DMode` for supersampling, which provides higher quality but is much more expensive. This has no effect on shader-induced aliasing or texture aliasing.

See also `ProjectSettings.rendering/anti_aliasing/quality/msaa_3d` and [`RenderingServer.ViewportSetMsaa3D`](RenderingServer.md).

### Oversampling

```csharp
public bool Oversampling { get; set; }
```

引擎名：`oversampling`

If `true` and one of the following conditions are true: [`SubViewport.Size2DOverrideStretch`](SubViewport.md) and [`SubViewport.Size2DOverride`](SubViewport.md) are set, [`Window.ContentScaleFactor`](Window.md) is set and scaling is enabled, `OversamplingOverride` is set, font and [`DpiTexture`](DpiTexture.md) oversampling are enabled.

### OversamplingOverride

```csharp
public float OversamplingOverride { get; set; }
```

引擎名：`oversampling_override`

If greater than zero, this value is used as the font oversampling factor, otherwise oversampling is equal to viewport scale.

### OwnWorld3D

```csharp
public bool OwnWorld3D { get; set; }
```

If `true`, the viewport will use a unique copy of the [`World3D`](World3D.md) defined in `World3D`.

### PhysicsObjectPicking

```csharp
public bool PhysicsObjectPicking { get; set; }
```

引擎名：`physics_object_picking`

If `true`, the objects rendered by viewport become subjects of mouse picking process.

**Note:** The number of simultaneously pickable objects is limited to 64 and they are selected in a non-deterministic order, which can be different in each picking process.

### PhysicsObjectPickingFirstOnly

```csharp
public bool PhysicsObjectPickingFirstOnly { get; set; }
```

引擎名：`physics_object_picking_first_only`

If `true`, the input_event signal will only be sent to one physics object in the mouse picking process. If you want to get the top object only, you must also enable `PhysicsObjectPickingSort`.

If `false`, an input_event signal will be sent to all physics objects in the mouse picking process.

This applies to 2D CanvasItem object picking only.

### PhysicsObjectPickingSort

```csharp
public bool PhysicsObjectPickingSort { get; set; }
```

引擎名：`physics_object_picking_sort`

If `true`, objects receive mouse picking events sorted primarily by their [`CanvasItem.ZIndex`](CanvasItem.md) and secondarily by their position in the scene tree. If `false`, the order is undetermined.

**Note:** This setting is disabled by default because of its potential expensive computational cost.

**Note:** Sorting happens after selecting the pickable objects. Because of the limitation of 64 simultaneously pickable objects, it is not guaranteed that the object with the highest [`CanvasItem.ZIndex`](CanvasItem.md) receives the picking event.

### PositionalShadowAtlas16Bits

```csharp
public bool PositionalShadowAtlas16Bits { get; set; }
```

Use 16 bits for the omni/spot shadow depth map. Enabling this results in shadows having less precision and may result in shadow acne, but can lead to performance improvements on some devices.

### PositionalShadowAtlasQuad0

```csharp
public Viewport.PositionalShadowAtlasQuadrantSubdiv PositionalShadowAtlasQuad0 { get; set; }
```

The subdivision amount of the first quadrant on the shadow atlas.

### PositionalShadowAtlasQuad1

```csharp
public Viewport.PositionalShadowAtlasQuadrantSubdiv PositionalShadowAtlasQuad1 { get; set; }
```

The subdivision amount of the second quadrant on the shadow atlas.

### PositionalShadowAtlasQuad2

```csharp
public Viewport.PositionalShadowAtlasQuadrantSubdiv PositionalShadowAtlasQuad2 { get; set; }
```

The subdivision amount of the third quadrant on the shadow atlas.

### PositionalShadowAtlasQuad3

```csharp
public Viewport.PositionalShadowAtlasQuadrantSubdiv PositionalShadowAtlasQuad3 { get; set; }
```

The subdivision amount of the fourth quadrant on the shadow atlas.

### PositionalShadowAtlasSize

```csharp
public int PositionalShadowAtlasSize { get; set; }
```

引擎名：`positional_shadow_atlas_size`

The shadow atlas' resolution (used for omni and spot lights). The value is rounded up to the nearest power of 2.

**Note:** If this is set to `0`, no positional shadows will be visible at all. This can improve performance significantly on low-end systems by reducing both the CPU and GPU load (as fewer draw calls are needed to draw the scene without shadows).

### Scaling3DMode

```csharp
public Viewport.Scaling3DModeEnum Scaling3DMode { get; set; }
```

Sets scaling 3D mode. Bilinear scaling renders at different resolution to either undersample or supersample the viewport. FidelityFX Super Resolution 1.0, abbreviated to FSR, is an upscaling technology that produces high quality images at fast framerates by using a spatially aware upscaling algorithm. FSR is slightly more expensive than bilinear, but it produces significantly higher image quality. FSR should be used where possible.

To control this property on the root viewport, set the `ProjectSettings.rendering/scaling_3d/mode` project setting.

### Scaling3DScale

```csharp
public float Scaling3DScale { get; set; }
```

Scales the 3D render buffer based on the viewport size uses an image filter specified in `ProjectSettings.rendering/scaling_3d/mode` to scale the output image to the full viewport size. Values lower than `1.0` can be used to speed up 3D rendering at the cost of quality (undersampling). Values greater than `1.0` are only valid for bilinear mode and can be used to improve 3D rendering quality at a high performance cost (supersampling). See also `ProjectSettings.rendering/anti_aliasing/quality/msaa_3d` for multi-sample antialiasing, which is significantly cheaper but only smooths the edges of polygons.

When using FSR upscaling, AMD recommends exposing the following values as preset options to users "Ultra Quality: 0.77", "Quality: 0.67", "Balanced: 0.59", "Performance: 0.5" instead of exposing the entire scale.

To control this property on the root viewport, set the `ProjectSettings.rendering/scaling_3d/scale` project setting.

### ScreenSpaceAA

```csharp
public Viewport.ScreenSpaceAAEnum ScreenSpaceAA { get; set; }
```

引擎名：`screen_space_aa`

Sets the screen-space antialiasing method used. Screen-space antialiasing works by selectively blurring edges in a post-process shader. It differs from MSAA which takes multiple coverage samples while rendering objects. Screen-space AA methods are typically faster than MSAA and will smooth out specular aliasing, but tend to make scenes appear blurry.

See also `ProjectSettings.rendering/anti_aliasing/quality/screen_space_aa` and [`RenderingServer.ViewportSetScreenSpaceAA`](RenderingServer.md).

### SdfOversize

```csharp
public Viewport.SdfOversizeEnum SdfOversize { get; set; }
```

引擎名：`sdf_oversize`

Controls how much of the original viewport's size should be covered by the 2D signed distance field. This SDF can be sampled in [`CanvasItem`](CanvasItem.md) shaders and is also used for [`GpuParticles2D`](GpuParticles2D.md) collision. Higher values allow portions of occluders located outside the viewport to still be taken into account in the generated signed distance field, at the cost of performance. If you notice particles falling through [`LightOccluder2D`](LightOccluder2D.md)s as the occluders leave the viewport, increase this setting.

The percentage is added on each axis and on both sides. For example, with the default [`Viewport.SdfOversizeEnum.Oversize120Percent`](Viewport.md), the signed distance field will cover 20% of the viewport's size outside the viewport on each side (top, right, bottom, left).

### SdfScale

```csharp
public Viewport.SdfScaleEnum SdfScale { get; set; }
```

引擎名：`sdf_scale`

The resolution scale to use for the 2D signed distance field. Higher values lead to a more precise and more stable signed distance field as the camera moves, at the cost of performance.

### Snap2DTransformsToPixel

```csharp
public bool Snap2DTransformsToPixel { get; set; }
```

If `true`, [`CanvasItem`](CanvasItem.md) nodes will internally snap to full pixels. Their position can still be sub-pixel, but the decimals will not have effect. This can lead to a crisper appearance at the cost of less smooth movement, especially when [`Camera2D`](Camera2D.md) smoothing is enabled.

### Snap2DVerticesToPixel

```csharp
public bool Snap2DVerticesToPixel { get; set; }
```

If `true`, vertices of [`CanvasItem`](CanvasItem.md) nodes will snap to full pixels. Only affects the final vertex positions, not the transforms. This can lead to a crisper appearance at the cost of less smooth movement, especially when [`Camera2D`](Camera2D.md) smoothing is enabled.

### TextureMipmapBias

```csharp
public float TextureMipmapBias { get; set; }
```

引擎名：`texture_mipmap_bias`

Affects the final texture sharpness by reading from a lower or higher mipmap (also called "texture LOD bias"). Negative values make mipmapped textures sharper but grainier when viewed at a distance, while positive values make mipmapped textures blurrier (even when up close).

Enabling temporal antialiasing (`UseTaa`) will automatically apply a `-0.5` offset to this value, while enabling FXAA (`ScreenSpaceAA`) will automatically apply a `-0.25` offset to this value. If both TAA and FXAA are enabled at the same time, an offset of `-0.75` is applied to this value.

To control this property on the root viewport, set the `ProjectSettings.rendering/textures/default_filters/texture_mipmap_bias` project setting.

**Note:** If `Scaling3DScale` is lower than `1.0` (exclusive), `TextureMipmapBias` is used to adjust the automatic mipmap bias which is calculated internally based on the scale factor. The formula for this is `log2(scaling_3d_scale) + mipmap_bias`.

**Note:** This property is only supported in the Forward+ and Mobile renderers, not Compatibility. In Compatibility, this property is always treated as if it was set to `0.0`.

### TransparentBg

```csharp
public bool TransparentBg { get; set; }
```

引擎名：`transparent_bg`

If `true`, the viewport should render its background as transparent.

**Note:** Due to technical limitations, certain rendering features are disabled when a viewport has a transparent background. This currently applies to screen-space reflections, subsurface scattering, and depth of field.

### UseDebanding

```csharp
public bool UseDebanding { get; set; }
```

引擎名：`use_debanding`

When using the Mobile or Forward+ renderers, set `UseDebanding` to enable or disable the debanding feature of this `Viewport`. If `UseHdr2D` is `false`, 2D rendering is *not* affected by debanding unless the [`Environment.BackgroundMode`](Environment.md) is [`Environment.BGMode.Canvas`](Environment.md). If `UseHdr2D` is `true`, debanding will only be applied if this is the root `Viewport` and will affect all 2D and 3D rendering, including canvas items.

`UseDebanding` has no effect when using the Compatibility rendering method. The Mobile renderer can also use material debanding, which can be set with [`RenderingServer.MaterialSetUseDebanding`](RenderingServer.md) or configured with `ProjectSettings.rendering/anti_aliasing/quality/use_debanding`.

See also `ProjectSettings.rendering/anti_aliasing/quality/use_debanding`, [`RenderingServer.MaterialSetUseDebanding`](RenderingServer.md), and [`RenderingServer.ViewportSetUseDebanding`](RenderingServer.md).

### UseHdr2D

```csharp
public bool UseHdr2D { get; set; }
```

If `true`, 2D rendering will use a high dynamic range (HDR) `RGBA16` format framebuffer. Additionally, 2D rendering will be performed on linear values and will be converted using the appropriate transfer function immediately before blitting to the screen (if the Viewport is attached to the screen).

Practically speaking, this means that the end result of the Viewport will not be clamped to the `0-1` range and can be used in 3D rendering without color encoding adjustments. This allows 2D rendering to take advantage of effects requiring high dynamic range (e.g. 2D glow) as well as substantially improves the appearance of effects requiring highly detailed gradients.

### UseOcclusionCulling

```csharp
public bool UseOcclusionCulling { get; set; }
```

引擎名：`use_occlusion_culling`

If `true`, [`OccluderInstance3D`](OccluderInstance3D.md) nodes will be usable for occlusion culling in 3D for this viewport. For the root viewport, `ProjectSettings.rendering/occlusion_culling/use_occlusion_culling` must be set to `true` instead.

**Note:** Enabling occlusion culling has a cost on the CPU. Only enable occlusion culling if you actually plan to use it, and think whether your scene can actually benefit from occlusion culling. Large, open scenes with few or no objects blocking the view will generally not benefit much from occlusion culling. Large open scenes generally benefit more from mesh LOD and visibility ranges ([`GeometryInstance3D.VisibilityRangeBegin`](GeometryInstance3D.md) and [`GeometryInstance3D.VisibilityRangeEnd`](GeometryInstance3D.md)) compared to occlusion culling.

**Note:** Due to memory constraints, occlusion culling is not supported by default in Web export templates. It can be enabled by compiling custom Web export templates with `module_raycast_enabled=yes`.

### UseTaa

```csharp
public bool UseTaa { get; set; }
```

引擎名：`use_taa`

Enables temporal antialiasing for this viewport. TAA works by jittering the camera and accumulating the images of the last rendered frames, motion vector rendering is used to account for camera and object motion.

**Note:** The implementation is not complete yet, some visual instances such as particles and skinned meshes may show artifacts.

See also `ProjectSettings.rendering/anti_aliasing/quality/use_taa` and [`RenderingServer.ViewportSetUseTaa`](RenderingServer.md).

### UseXR

```csharp
public bool UseXR { get; set; }
```

引擎名：`use_xr`

If `true`, the viewport will use the primary XR interface to render XR output. When applicable this can result in a stereoscopic image and the resulting render being output to a headset.

### VrsMode

```csharp
public Viewport.VrsModeEnum VrsMode { get; set; }
```

引擎名：`vrs_mode`

The Variable Rate Shading (VRS) mode that is used for this viewport. Note, if hardware does not support VRS this property is ignored.

### VrsTexture

```csharp
public Texture2D VrsTexture { get; set; }
```

引擎名：`vrs_texture`

Texture to use when `VrsMode` is set to [`Viewport.VrsModeEnum.Texture`](Viewport.md).

The texture *must* use a lossless compression format so that colors can be matched precisely. The following VRS densities are mapped to various colors, with brighter colors representing a lower level of shading precision:

```csharp
- 1×1 = rgb(0, 0, 0)     - #000000
- 1×2 = rgb(0, 85, 0)    - #005500
- 2×1 = rgb(85, 0, 0)    - #550000
- 2×2 = rgb(85, 85, 0)   - #555500
- 2×4 = rgb(85, 170, 0)  - #55aa00
- 4×2 = rgb(170, 85, 0)  - #aa5500
- 4×4 = rgb(170, 170, 0) - #aaaa00
- 4×8 = rgb(170, 255, 0) - #aaff00 - Not supported on most hardware
- 8×4 = rgb(255, 170, 0) - #ffaa00 - Not supported on most hardware
- 8×8 = rgb(255, 255, 0) - #ffff00 - Not supported on most hardware

```

### VrsUpdateMode

```csharp
public Viewport.VrsUpdateModeEnum VrsUpdateMode { get; set; }
```

引擎名：`vrs_update_mode`

Sets the update mode for Variable Rate Shading (VRS) for the viewport. VRS requires the input texture to be converted to the format usable by the VRS method supported by the hardware. The update mode defines how often this happens. If the GPU does not support VRS, or VRS is not enabled, this property is ignored.

### World2D

```csharp
public World2D World2D { get; set; }
```

The custom [`World2D`](World2D.md) which can be used as 2D environment source.

### World3D

```csharp
public World3D World3D { get; set; }
```

The custom [`World3D`](World3D.md) which can be used as 3D environment source.

## 方法

### FindWorld2D

```csharp
public World2D FindWorld2D()
```

Returns the first valid [`World2D`](World2D.md) for this viewport, searching the `World2D` property of itself and any Viewport ancestor.

### FindWorld3D

```csharp
public World3D FindWorld3D()
```

Returns the first valid [`World3D`](World3D.md) for this viewport, searching the `World3D` property of itself and any Viewport ancestor.

### GetAnisotropicFilteringLevel

```csharp
public Viewport.AnisotropicFiltering GetAnisotropicFilteringLevel()
```

引擎名：`get_anisotropic_filtering_level`

### GetAudioListener2D

```csharp
public AudioListener2D GetAudioListener2D()
```

Returns the currently active 2D audio listener. Returns `null` if there are no active 2D audio listeners, in which case the active 2D camera will be treated as listener.

### GetAudioListener3D

```csharp
public AudioListener3D GetAudioListener3D()
```

Returns the currently active 3D audio listener. Returns `null` if there are no active 3D audio listeners, in which case the active 3D camera will be treated as listener.

### GetCamera2D

```csharp
public Camera2D GetCamera2D()
```

Returns the currently active 2D camera. Returns `null` if there are no active cameras.

**Note:** If called while the *Camera Override* system is active in editor, this will return the internally managed override camera. It is therefore advised to avoid caching the return value, or to check that the cached value is still a valid instance and is the current camera before use. See `GodotObject.IsInstanceValid` and [`Camera2D.IsCurrent`](Camera2D.md).

### GetCamera3D

```csharp
public Camera3D GetCamera3D()
```

Returns the currently active 3D camera. Returns `null` if there are no active cameras.

**Note:** If called while the *Camera Override* system is active in editor, this will return the internally managed override camera. It is therefore advised to avoid caching the return value, or to check that the cached value is a valid instance and is the current camera before use. See `GodotObject.IsInstanceValid` and [`Camera3D.Current`](Camera3D.md).

### GetCanvasCullMask

```csharp
public uint GetCanvasCullMask()
```

引擎名：`get_canvas_cull_mask`

### GetCanvasCullMaskBit

```csharp
public bool GetCanvasCullMaskBit(uint layer)
```

引擎名：`get_canvas_cull_mask_bit`

Returns an individual bit on the rendering layer mask.

### GetCanvasTransform

```csharp
public Transform2D GetCanvasTransform()
```

引擎名：`get_canvas_transform`

### GetDebugDraw

```csharp
public Viewport.DebugDrawEnum GetDebugDraw()
```

引擎名：`get_debug_draw`

### GetDefaultCanvasItemTextureFilter

```csharp
public Viewport.DefaultCanvasItemTextureFilter GetDefaultCanvasItemTextureFilter()
```

引擎名：`get_default_canvas_item_texture_filter`

### GetDefaultCanvasItemTextureRepeat

```csharp
public Viewport.DefaultCanvasItemTextureRepeat GetDefaultCanvasItemTextureRepeat()
```

引擎名：`get_default_canvas_item_texture_repeat`

### GetDragThreshold

```csharp
public int GetDragThreshold()
```

引擎名：`get_drag_threshold`

### GetEmbeddedSubwindows

```csharp
public Array<Window> GetEmbeddedSubwindows()
```

引擎名：`get_embedded_subwindows`

Returns a list of the visible embedded [`Window`](Window.md)s inside the viewport.

**Note:** [`Window`](Window.md)s inside other viewports will not be listed.

### GetFinalTransform

```csharp
public Transform2D GetFinalTransform()
```

引擎名：`get_final_transform`

Returns the transform from the viewport's coordinate system to the embedder's coordinate system.

### GetFsrSharpness

```csharp
public float GetFsrSharpness()
```

引擎名：`get_fsr_sharpness`

### GetGlobalCanvasTransform

```csharp
public Transform2D GetGlobalCanvasTransform()
```

引擎名：`get_global_canvas_transform`

### GetMeshLodThreshold

```csharp
public float GetMeshLodThreshold()
```

引擎名：`get_mesh_lod_threshold`

### GetMousePosition

```csharp
public Vector2 GetMousePosition()
```

引擎名：`get_mouse_position`

Returns the mouse's position in this `Viewport` using the coordinate system of this `Viewport`.

### GetMsaa2D

```csharp
public Viewport.Msaa GetMsaa2D()
```

### GetMsaa3D

```csharp
public Viewport.Msaa GetMsaa3D()
```

### GetOversampling

```csharp
public float GetOversampling()
```

引擎名：`get_oversampling`

Returns viewport oversampling factor.

### GetOversamplingOverride

```csharp
public float GetOversamplingOverride()
```

引擎名：`get_oversampling_override`

### GetPhysicsObjectPicking

```csharp
public bool GetPhysicsObjectPicking()
```

引擎名：`get_physics_object_picking`

### GetPhysicsObjectPickingFirstOnly

```csharp
public bool GetPhysicsObjectPickingFirstOnly()
```

引擎名：`get_physics_object_picking_first_only`

### GetPhysicsObjectPickingSort

```csharp
public bool GetPhysicsObjectPickingSort()
```

引擎名：`get_physics_object_picking_sort`

### GetPositionalShadowAtlas16Bits

```csharp
public bool GetPositionalShadowAtlas16Bits()
```

### GetPositionalShadowAtlasQuadrantSubdiv

```csharp
public Viewport.PositionalShadowAtlasQuadrantSubdiv GetPositionalShadowAtlasQuadrantSubdiv(int quadrant)
```

引擎名：`get_positional_shadow_atlas_quadrant_subdiv`

Returns the positional shadow atlas quadrant subdivision of the specified quadrant.

### GetPositionalShadowAtlasSize

```csharp
public int GetPositionalShadowAtlasSize()
```

引擎名：`get_positional_shadow_atlas_size`

### GetRenderInfo

```csharp
public int GetRenderInfo(Viewport.RenderInfoType type, Viewport.RenderInfo info)
```

引擎名：`get_render_info`

Returns rendering statistics of the given type.

### GetScaling3DMode

```csharp
public Viewport.Scaling3DModeEnum GetScaling3DMode()
```

### GetScaling3DScale

```csharp
public float GetScaling3DScale()
```

### GetScreenSpaceAA

```csharp
public Viewport.ScreenSpaceAAEnum GetScreenSpaceAA()
```

引擎名：`get_screen_space_aa`

### GetScreenTransform

```csharp
public Transform2D GetScreenTransform()
```

引擎名：`get_screen_transform`

Returns the transform from the Viewport's coordinates to the screen coordinates of the containing window manager window.

### GetSdfOversize

```csharp
public Viewport.SdfOversizeEnum GetSdfOversize()
```

引擎名：`get_sdf_oversize`

### GetSdfScale

```csharp
public Viewport.SdfScaleEnum GetSdfScale()
```

引擎名：`get_sdf_scale`

### GetStretchTransform

```csharp
public Transform2D GetStretchTransform()
```

引擎名：`get_stretch_transform`

Returns the automatically computed 2D stretch transform, taking the `Viewport`'s stretch settings into account. The final value is multiplied by [`Window.ContentScaleFactor`](Window.md), but only for the root viewport. If this method is called on a [`SubViewport`](SubViewport.md) (e.g., in a scene tree with [`SubViewportContainer`](SubViewportContainer.md) and [`SubViewport`](SubViewport.md)), the scale factor of the root window will not be applied. Using `Transform2D.get_scale` on the returned value, this can be used to compensate for scaling when zooming a [`Camera2D`](Camera2D.md) node, or to scale down a [`TextureRect`](TextureRect.md) to be pixel-perfect regardless of the automatically computed scale factor.

**Note:** Due to how pixel scaling works, the returned transform's X and Y scale may differ slightly, even when [`Window.ContentScaleAspect`](Window.md) is set to a mode that preserves the pixels' aspect ratio. If [`Window.ContentScaleAspect`](Window.md) is [`Window.ContentScaleAspectEnum.Ignore`](Window.md), the X and Y scale may differ *significantly*.

### GetTexture

```csharp
public ViewportTexture GetTexture()
```

引擎名：`get_texture`

Returns the viewport's texture.

**Note:** When trying to store the current texture (e.g. in a file), it might be completely black or outdated if used too early, especially when used in e.g. [`Node.Ready`](Node.md). To make sure the texture you get is correct, you can await [`RenderingServer.FramePostDraw`](RenderingServer.md) signal.

```csharp
public async override void _Ready()
{
await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
var viewport = GetNode<Viewport>("Viewport");
viewport.GetTexture().GetImage().SavePng("user://Screenshot.png");
}

```

**Note:** When `UseHdr2D` is `true` the returned texture will be an HDR image using linear encoding.

### GetTextureMipmapBias

```csharp
public float GetTextureMipmapBias()
```

引擎名：`get_texture_mipmap_bias`

### GetViewportRid

```csharp
public Rid GetViewportRid()
```

引擎名：`get_viewport_rid`

Returns the viewport's RID from the [`RenderingServer`](RenderingServer.md).

### GetVisibleRect

```csharp
public Rect2 GetVisibleRect()
```

引擎名：`get_visible_rect`

Returns the visible rectangle in global screen coordinates.

### GetVrsMode

```csharp
public Viewport.VrsModeEnum GetVrsMode()
```

引擎名：`get_vrs_mode`

### GetVrsTexture

```csharp
public Texture2D GetVrsTexture()
```

引擎名：`get_vrs_texture`

### GetVrsUpdateMode

```csharp
public Viewport.VrsUpdateModeEnum GetVrsUpdateMode()
```

引擎名：`get_vrs_update_mode`

### GetWorld2D

```csharp
public World2D GetWorld2D()
```

### GetWorld3D

```csharp
public World3D GetWorld3D()
```

### GuiCancelDrag

```csharp
public void GuiCancelDrag()
```

引擎名：`gui_cancel_drag`

Cancels the drag operation that was previously started through [`Control._GetDragData()`](Control.md) or forced with [`Control.ForceDrag`](Control.md).

### GuiGetDragData

```csharp
public Variant GuiGetDragData()
```

引擎名：`gui_get_drag_data`

Returns the drag data from the GUI, that was previously returned by [`Control._GetDragData()`](Control.md).

### GuiGetDragDescription

```csharp
public string GuiGetDragDescription()
```

引擎名：`gui_get_drag_description`

Returns the human-readable description of the drag data, used for assistive apps.

### GuiGetFocusOwner

```csharp
public Control GuiGetFocusOwner()
```

引擎名：`gui_get_focus_owner`

Returns the currently focused [`Control`](Control.md) within this viewport. If no [`Control`](Control.md) is focused, returns `null`.

### GuiGetHoveredControl

```csharp
public Control GuiGetHoveredControl()
```

引擎名：`gui_get_hovered_control`

Returns the [`Control`](Control.md) that the mouse is currently hovering over in this viewport. If no [`Control`](Control.md) has the cursor, returns `null`.

Typically the leaf [`Control`](Control.md) node or deepest level of the subtree which claims hover. This is very useful when used together with [`Node.IsAncestorOf`](Node.md) to find if the mouse is within a control tree.

### GuiIsDragSuccessful

```csharp
public bool GuiIsDragSuccessful()
```

引擎名：`gui_is_drag_successful`

Returns `true` if the drag operation is successful.

### GuiIsDragging

```csharp
public bool GuiIsDragging()
```

引擎名：`gui_is_dragging`

Returns `true` if a drag operation is currently ongoing and where the drop action could happen in this viewport.

Alternative to [`Node.NotificationDragBegin`](Node.md) and [`Node.NotificationDragEnd`](Node.md) when you prefer polling the value.

### GuiReleaseFocus

```csharp
public void GuiReleaseFocus()
```

引擎名：`gui_release_focus`

Removes the focus from the currently focused [`Control`](Control.md) within this viewport. If no [`Control`](Control.md) has the focus, does nothing.

### GuiSetDragDescription

```csharp
public void GuiSetDragDescription(string description)
```

引擎名：`gui_set_drag_description`

Sets the human-readable description of the drag data to `description`, used for assistive apps.

### HasTransparentBackground

```csharp
public bool HasTransparentBackground()
```

引擎名：`has_transparent_background`

### Is3DDisabled

```csharp
public bool Is3DDisabled()
```

### IsAudioListener2D

```csharp
public bool IsAudioListener2D()
```

### IsAudioListener3D

```csharp
public bool IsAudioListener3D()
```

### IsEmbeddingSubwindows

```csharp
public bool IsEmbeddingSubwindows()
```

引擎名：`is_embedding_subwindows`

### IsHandlingInputLocally

```csharp
public bool IsHandlingInputLocally()
```

引擎名：`is_handling_input_locally`

### IsInputDisabled

```csharp
public bool IsInputDisabled()
```

引擎名：`is_input_disabled`

### IsInputHandled

```csharp
public bool IsInputHandled()
```

引擎名：`is_input_handled`

Returns whether the current [`InputEvent`](InputEvent.md) has been handled. Input events are not handled until `SetInputAsHandled()` has been called during the lifetime of an [`InputEvent`](InputEvent.md).

This is usually done as part of input handling methods like [`Node._Input()`](Node.md), [`Control.GuiInput`](Control.md) or others, as well as in corresponding signal handlers.

If `HandleInputLocally` is set to `false`, this method will try finding the first parent viewport that is set to handle input locally, and return its value for `IsInputHandled()` instead.

### IsSnap2DTransformsToPixelEnabled

```csharp
public bool IsSnap2DTransformsToPixelEnabled()
```

### IsSnap2DVerticesToPixelEnabled

```csharp
public bool IsSnap2DVerticesToPixelEnabled()
```

### IsSnapControlsToPixelsEnabled

```csharp
public bool IsSnapControlsToPixelsEnabled()
```

引擎名：`is_snap_controls_to_pixels_enabled`

### IsUsingDebanding

```csharp
public bool IsUsingDebanding()
```

引擎名：`is_using_debanding`

### IsUsingHdr2D

```csharp
public bool IsUsingHdr2D()
```

### IsUsingOcclusionCulling

```csharp
public bool IsUsingOcclusionCulling()
```

引擎名：`is_using_occlusion_culling`

### IsUsingOversampling

```csharp
public bool IsUsingOversampling()
```

引擎名：`is_using_oversampling`

### IsUsingOwnWorld3D

```csharp
public bool IsUsingOwnWorld3D()
```

### IsUsingTaa

```csharp
public bool IsUsingTaa()
```

引擎名：`is_using_taa`

### IsUsingXR

```csharp
public bool IsUsingXR()
```

引擎名：`is_using_xr`

### NotifyMouseEntered

```csharp
public void NotifyMouseEntered()
```

引擎名：`notify_mouse_entered`

Inform the Viewport that the mouse has entered its area. Use this function before sending an [`InputEventMouseButton`](InputEventMouseButton.md) or [`InputEventMouseMotion`](InputEventMouseMotion.md) to the `Viewport` with `PushInput()`. See also `NotifyMouseExited()`.

**Note:** In most cases, it is not necessary to call this function because [`SubViewport`](SubViewport.md) nodes that are children of [`SubViewportContainer`](SubViewportContainer.md) are notified automatically. This is only necessary when interacting with viewports in non-default ways, for example as textures in [`TextureRect`](TextureRect.md) or with an [`Area3D`](Area3D.md) that forwards input events.

### NotifyMouseExited

```csharp
public void NotifyMouseExited()
```

引擎名：`notify_mouse_exited`

Inform the Viewport that the mouse has left its area. Use this function when the node that displays the viewport notices the mouse has left the area of the displayed viewport. See also `NotifyMouseEntered()`.

**Note:** In most cases, it is not necessary to call this function because [`SubViewport`](SubViewport.md) nodes that are children of [`SubViewportContainer`](SubViewportContainer.md) are notified automatically. This is only necessary when interacting with viewports in non-default ways, for example as textures in [`TextureRect`](TextureRect.md) or with an [`Area3D`](Area3D.md) that forwards input events.

### PushInput

```csharp
public void PushInput(InputEvent event, bool inLocalCoords = false)
```

引擎名：`push_input`

Triggers the given `event` in this `Viewport`. This can be used to pass an [`InputEvent`](InputEvent.md) between viewports, or to locally apply inputs that were sent over the network or saved to a file.

If `inLocalCoords` is `false`, the event's position is in the embedder's coordinates and will be converted to viewport coordinates. If `inLocalCoords` is `true`, the event's position is in viewport coordinates.

While this method serves a similar purpose as [`Input.ParseInputEvent`](Input.md), it does not remap the specified `event` based on project settings like `ProjectSettings.input_devices/pointing/emulate_touch_from_mouse`.

Calling this method will propagate calls to child nodes for following methods in the given order:

- [`Node._Input()`](Node.md)

- [`Control.GuiInput`](Control.md) for [`Control`](Control.md) nodes

- [`Node._ShortcutInput()`](Node.md)

- [`Node._UnhandledKeyInput()`](Node.md)

- [`Node._UnhandledInput()`](Node.md)

If an earlier method marks the input as handled via `SetInputAsHandled()`, any later method in this list will not be called.

If none of the methods handle the event and `PhysicsObjectPicking` is `true`, the event is used for physics object picking.

### PushTextInput

```csharp
public void PushTextInput(string text)
```

引擎名：`push_text_input`

Helper method which calls the `set_text()` method on the currently focused [`Control`](Control.md), provided that it is defined (e.g. if the focused Control is [`Button`](Button.md) or [`LineEdit`](LineEdit.md)).

### PushUnhandledInput

```csharp
public void PushUnhandledInput(InputEvent event, bool inLocalCoords = false)
```

引擎名：`push_unhandled_input`

Triggers the given `event` in this `Viewport`. This can be used to pass an [`InputEvent`](InputEvent.md) between viewports, or to locally apply inputs that were sent over the network or saved to a file.

If `inLocalCoords` is `false`, the event's position is in the embedder's coordinates and will be converted to viewport coordinates. If `inLocalCoords` is `true`, the event's position is in viewport coordinates.

Calling this method will propagate calls to child nodes for following methods in the given order:

- [`Node._ShortcutInput()`](Node.md)

- [`Node._UnhandledKeyInput()`](Node.md)

- [`Node._UnhandledInput()`](Node.md)

If an earlier method marks the input as handled via `SetInputAsHandled()`, any later method in this list will not be called.

If none of the methods handle the event and `PhysicsObjectPicking` is `true`, the event is used for physics object picking.

**Note:** This method doesn't propagate input events to embedded [`Window`](Window.md)s or [`SubViewport`](SubViewport.md)s.

### SetAnisotropicFilteringLevel

```csharp
public void SetAnisotropicFilteringLevel(Viewport.AnisotropicFiltering anisotropicFilteringLevel)
```

引擎名：`set_anisotropic_filtering_level`

### SetAsAudioListener2D

```csharp
public void SetAsAudioListener2D(bool enable)
```

### SetAsAudioListener3D

```csharp
public void SetAsAudioListener3D(bool enable)
```

### SetCanvasCullMask

```csharp
public void SetCanvasCullMask(uint mask)
```

引擎名：`set_canvas_cull_mask`

### SetCanvasCullMaskBit

```csharp
public void SetCanvasCullMaskBit(uint layer, bool enable)
```

引擎名：`set_canvas_cull_mask_bit`

Set/clear individual bits on the rendering layer mask. This simplifies editing this `Viewport`'s layers.

### SetCanvasTransform

```csharp
public void SetCanvasTransform(Transform2D xform)
```

引擎名：`set_canvas_transform`

### SetDebugDraw

```csharp
public void SetDebugDraw(Viewport.DebugDrawEnum debugDraw)
```

引擎名：`set_debug_draw`

### SetDefaultCanvasItemTextureFilter

```csharp
public void SetDefaultCanvasItemTextureFilter(Viewport.DefaultCanvasItemTextureFilter mode)
```

引擎名：`set_default_canvas_item_texture_filter`

### SetDefaultCanvasItemTextureRepeat

```csharp
public void SetDefaultCanvasItemTextureRepeat(Viewport.DefaultCanvasItemTextureRepeat mode)
```

引擎名：`set_default_canvas_item_texture_repeat`

### SetDisable3D

```csharp
public void SetDisable3D(bool disable)
```

### SetDisableInput

```csharp
public void SetDisableInput(bool disable)
```

引擎名：`set_disable_input`

### SetDragThreshold

```csharp
public void SetDragThreshold(int threshold)
```

引擎名：`set_drag_threshold`

### SetEmbeddingSubwindows

```csharp
public void SetEmbeddingSubwindows(bool enable)
```

引擎名：`set_embedding_subwindows`

### SetFsrSharpness

```csharp
public void SetFsrSharpness(float fsrSharpness)
```

引擎名：`set_fsr_sharpness`

### SetGlobalCanvasTransform

```csharp
public void SetGlobalCanvasTransform(Transform2D xform)
```

引擎名：`set_global_canvas_transform`

### SetHandleInputLocally

```csharp
public void SetHandleInputLocally(bool enable)
```

引擎名：`set_handle_input_locally`

### SetInputAsHandled

```csharp
public void SetInputAsHandled()
```

引擎名：`set_input_as_handled`

Stops the input from propagating further up the [`SceneTree`](SceneTree.md).

**Note:** This does not affect the methods in [`Input`](Input.md), only the way events are propagated.

### SetMeshLodThreshold

```csharp
public void SetMeshLodThreshold(float pixels)
```

引擎名：`set_mesh_lod_threshold`

### SetMsaa2D

```csharp
public void SetMsaa2D(Viewport.Msaa msaa)
```

### SetMsaa3D

```csharp
public void SetMsaa3D(Viewport.Msaa msaa)
```

### SetOversamplingOverride

```csharp
public void SetOversamplingOverride(float oversampling)
```

引擎名：`set_oversampling_override`

### SetPhysicsObjectPicking

```csharp
public void SetPhysicsObjectPicking(bool enable)
```

引擎名：`set_physics_object_picking`

### SetPhysicsObjectPickingFirstOnly

```csharp
public void SetPhysicsObjectPickingFirstOnly(bool enable)
```

引擎名：`set_physics_object_picking_first_only`

### SetPhysicsObjectPickingSort

```csharp
public void SetPhysicsObjectPickingSort(bool enable)
```

引擎名：`set_physics_object_picking_sort`

### SetPositionalShadowAtlas16Bits

```csharp
public void SetPositionalShadowAtlas16Bits(bool enable)
```

### SetPositionalShadowAtlasQuadrantSubdiv

```csharp
public void SetPositionalShadowAtlasQuadrantSubdiv(int quadrant, Viewport.PositionalShadowAtlasQuadrantSubdiv subdiv)
```

引擎名：`set_positional_shadow_atlas_quadrant_subdiv`

Sets the number of subdivisions to use in the specified quadrant. A higher number of subdivisions allows you to have more shadows in the scene at once, but reduces the quality of the shadows. A good practice is to have quadrants with a varying number of subdivisions and to have as few subdivisions as possible.

### SetPositionalShadowAtlasSize

```csharp
public void SetPositionalShadowAtlasSize(int size)
```

引擎名：`set_positional_shadow_atlas_size`

### SetScaling3DMode

```csharp
public void SetScaling3DMode(Viewport.Scaling3DModeEnum scaling3DMode)
```

### SetScaling3DScale

```csharp
public void SetScaling3DScale(float scale)
```

### SetScreenSpaceAA

```csharp
public void SetScreenSpaceAA(Viewport.ScreenSpaceAAEnum screenSpaceAA)
```

引擎名：`set_screen_space_aa`

### SetSdfOversize

```csharp
public void SetSdfOversize(Viewport.SdfOversizeEnum oversize)
```

引擎名：`set_sdf_oversize`

### SetSdfScale

```csharp
public void SetSdfScale(Viewport.SdfScaleEnum scale)
```

引擎名：`set_sdf_scale`

### SetSnap2DTransformsToPixel

```csharp
public void SetSnap2DTransformsToPixel(bool enabled)
```

### SetSnap2DVerticesToPixel

```csharp
public void SetSnap2DVerticesToPixel(bool enabled)
```

### SetSnapControlsToPixels

```csharp
public void SetSnapControlsToPixels(bool enabled)
```

引擎名：`set_snap_controls_to_pixels`

### SetTextureMipmapBias

```csharp
public void SetTextureMipmapBias(float textureMipmapBias)
```

引擎名：`set_texture_mipmap_bias`

### SetTransparentBackground

```csharp
public void SetTransparentBackground(bool enable)
```

引擎名：`set_transparent_background`

### SetUseDebanding

```csharp
public void SetUseDebanding(bool enable)
```

引擎名：`set_use_debanding`

### SetUseHdr2D

```csharp
public void SetUseHdr2D(bool enable)
```

### SetUseOcclusionCulling

```csharp
public void SetUseOcclusionCulling(bool enable)
```

引擎名：`set_use_occlusion_culling`

### SetUseOversampling

```csharp
public void SetUseOversampling(bool enable)
```

引擎名：`set_use_oversampling`

### SetUseOwnWorld3D

```csharp
public void SetUseOwnWorld3D(bool enable)
```

### SetUseTaa

```csharp
public void SetUseTaa(bool enable)
```

引擎名：`set_use_taa`

### SetUseXR

```csharp
public void SetUseXR(bool use)
```

引擎名：`set_use_xr`

### SetVrsMode

```csharp
public void SetVrsMode(Viewport.VrsModeEnum mode)
```

引擎名：`set_vrs_mode`

### SetVrsTexture

```csharp
public void SetVrsTexture(Texture2D texture)
```

引擎名：`set_vrs_texture`

### SetVrsUpdateMode

```csharp
public void SetVrsUpdateMode(Viewport.VrsUpdateModeEnum mode)
```

引擎名：`set_vrs_update_mode`

### SetWorld2D

```csharp
public void SetWorld2D(World2D world2D)
```

### SetWorld3D

```csharp
public void SetWorld3D(World3D world3D)
```

### UpdateMouseCursorState

```csharp
public void UpdateMouseCursorState()
```

引擎名：`update_mouse_cursor_state`

Force instantly updating the display based on the current mouse cursor position. This includes updating the mouse cursor shape and sending necessary [`Control.MouseEntered`](Control.md), [`CollisionObject2D.MouseEntered`](CollisionObject2D.md), [`CollisionObject3D.MouseEntered`](CollisionObject3D.md) and [`Window.MouseEntered`](Window.md) signals and their respective `mouse_exited` counterparts.

### WarpMouse

```csharp
public void WarpMouse(Vector2 position)
```

引擎名：`warp_mouse`

Moves the mouse pointer to the specified position in this `Viewport` using the coordinate system of this `Viewport`.

**Note:** `WarpMouse()` is only supported on Windows, macOS and Linux. It has no effect on Android, iOS and Web.

## 事件（信号）

### GuiFocusChanged

```csharp
public event Viewport.GuiFocusChangedEventHandler GuiFocusChanged
```

引擎名：`gui_focus_changed`

Emitted when a Control node grabs keyboard focus.

**Note:** A Control node losing focus doesn't cause this signal to be emitted.

### SizeChanged

```csharp
public event Action SizeChanged
```

引擎名：`size_changed`

Emitted when the size of the viewport is changed, whether by resizing of window, or some other means.

## AnisotropicFiltering

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Anisotropic filtering is disabled.
- `Anisotropy2X = 1`：Use 2× anisotropic filtering.
- `Anisotropy4X = 2`：Use 4× anisotropic filtering.
- `Anisotropy8X = 3`：Use 8× anisotropic filtering.
- `Anisotropy16X = 4`：Use 16× anisotropic filtering.
- `Max = 5`：Represents the size of the `Viewport.AnisotropicFiltering` enum.

## DebugDrawEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Objects are displayed normally.
- `Unshaded = 1`：Objects are displayed without light information.
- `Lighting = 2`：Objects are displayed without textures and only with lighting information.
- `Overdraw = 3`：Objects are displayed semi-transparent with additive blending so you can see where they are drawing over top of one another.
- `Wireframe = 4`：Objects are displayed as wireframe models.
- `NormalBuffer = 5`：Objects are displayed without lighting information and their textures replaced by normal mapping.
- `VoxelGIAlbedo = 6`：Objects are displayed with only the albedo value from [`VoxelGI`](VoxelGI.md)s.
- `VoxelGILighting = 7`：Objects are displayed with only the lighting value from [`VoxelGI`](VoxelGI.md)s.
- `VoxelGIEmission = 8`：Objects are displayed with only the emission color from [`VoxelGI`](VoxelGI.md)s.
- `ShadowAtlas = 9`：Draws the shadow atlas that stores shadows from [`OmniLight3D`](OmniLight3D.md)s and [`SpotLight3D`](SpotLight3D.md)s in the upper left quadrant of the [`Viewport`](Viewport.md).
- `DirectionalShadowAtlas = 10`：Draws the shadow atlas that stores shadows from [`DirectionalLight3D`](DirectionalLight3D.md)s in the upper left quadrant of the [`Viewport`](Viewport.md).
- `SceneLuminance = 11`：Draws the scene luminance buffer (if available) in the upper left quadrant of the [`Viewport`](Viewport.md).
- `Ssao = 12`：Draws the screen-space ambient occlusion texture instead of the scene so that you can clearly see how it is affecting objects.
- `Ssil = 13`：Draws the screen-space indirect lighting texture instead of the scene so that you can clearly see how it is affecting objects.
- `PssmSplits = 14`：Colors each PSSM split for the [`DirectionalLight3D`](DirectionalLight3D.md)s in the scene a different color so you can see where the splits are.
- `DecalAtlas = 15`：Draws the decal atlas used by [`Decal`](Decal.md)s and light projector textures in the upper left quadrant of the [`Viewport`](Viewport.md).
- `Sdfgi = 16`：Draws the cascades used to render signed distance field global illumination (SDFGI).
- `SdfgiProbes = 17`：Draws the probes used for signed distance field global illumination (SDFGI).
- `GIBuffer = 18`：Draws the buffer used for global illumination from [`VoxelGI`](VoxelGI.md) or SDFGI.
- `DisableLod = 19`：Draws all of the objects at their highest polycount regardless of their distance from the camera.
- `ClusterOmniLights = 20`：Draws the cluster used by [`OmniLight3D`](OmniLight3D.md) nodes to optimize light rendering.
- `ClusterSpotLights = 21`：Draws the cluster used by [`SpotLight3D`](SpotLight3D.md) nodes to optimize light rendering.
- `ClusterDecals = 22`：Draws the cluster used by [`Decal`](Decal.md) nodes to optimize decal rendering.
- `ClusterReflectionProbes = 23`：Draws the cluster used by [`ReflectionProbe`](ReflectionProbe.md) nodes to optimize reflection probes.
- `Occluders = 24`：Draws the buffer used for occlusion culling.
- `MotionVectors = 25`：Draws vector lines over the viewport to indicate the movement of pixels between frames.
- `InternalBuffer = 26`：Draws the internal resolution buffer of the scene in linear colorspace before tonemapping or post-processing is applied.
- `ClusterAreaLights = 27`：Draws the cluster used by [`AreaLight3D`](AreaLight3D.md) nodes to optimize light rendering.
- `AreaLightAtlas = 28`：Draws the atlas used by [`AreaLight3D`](AreaLight3D.md) nodes in the upper left quadrant of the [`Viewport`](Viewport.md).

## DefaultCanvasItemTextureFilter

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：The texture filter reads from the nearest pixel only.
- `Linear = 1`：The texture filter blends between the nearest 4 pixels.
- `LinearWithMipmaps = 2`：The texture filter blends between the nearest 4 pixels and between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `NearestWithMipmaps = 3`：The texture filter reads from the nearest pixel and blends between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `ParentNode = 4`：The [`Viewport`](Viewport.md) will inherit the filter from its parent [`CanvasItem`](CanvasItem.md) or [`Viewport`](Viewport.md).
- `Max = 5`：Represents the size of the `Viewport.DefaultCanvasItemTextureFilter` enum.

## DefaultCanvasItemTextureRepeat

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables textures repeating.
- `Enabled = 1`：Enables the texture to repeat when UV coordinates are outside the 0-1 range.
- `Mirror = 2`：Flip the texture when repeating so that the edge lines up instead of abruptly changing.
- `ParentNode = 3`：The [`Viewport`](Viewport.md) will inherit the repeat mode from its parent [`CanvasItem`](CanvasItem.md) or [`Viewport`](Viewport.md).
- `Max = 4`：Represents the size of the `Viewport.DefaultCanvasItemTextureRepeat` enum.

## GuiFocusChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Viewport.GuiFocusChanged`](Viewport.md) event of a [`Viewport`](Viewport.md) class.

### 签名

#### Invoke

```csharp
delegate void GuiFocusChangedEventHandler(Control node)
```

## Msaa

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Multisample antialiasing mode disabled.
- `Msaa2X = 1`：Use 2× Multisample Antialiasing.
- `Msaa4X = 2`：Use 4× Multisample Antialiasing.
- `Msaa8X = 3`：Use 8× Multisample Antialiasing.
- `Max = 4`：Represents the size of the `Viewport.Msaa` enum.

## PositionalShadowAtlasQuadrantSubdiv

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：This quadrant will not be used.
- `Subdiv1 = 1`：This quadrant will only be used by one shadow map.
- `Subdiv4 = 2`：This quadrant will be split in 4 and used by up to 4 shadow maps.
- `Subdiv16 = 3`：This quadrant will be split 16 ways and used by up to 16 shadow maps.
- `Subdiv64 = 4`：This quadrant will be split 64 ways and used by up to 64 shadow maps.
- `Subdiv256 = 5`：This quadrant will be split 256 ways and used by up to 256 shadow maps.
- `Subdiv1024 = 6`：This quadrant will be split 1024 ways and used by up to 1024 shadow maps.
- `Max = 7`：Represents the size of the `Viewport.PositionalShadowAtlasQuadrantSubdiv` enum.

## RenderInfo

命名空间 `Godot`；enum

### 枚举值

- `ObjectsInFrame = 0`：Amount of objects in frame.
- `PrimitivesInFrame = 1`：Amount of vertices in frame.
- `DrawCallsInFrame = 2`：Amount of draw calls in frame.
- `Max = 3`：Represents the size of the `Viewport.RenderInfo` enum.

## RenderInfoType

命名空间 `Godot`；enum

### 枚举值

- `Visible = 0`：Visible render pass (excluding shadows).
- `Shadow = 1`：Shadow render pass.
- `Canvas = 2`：Canvas item rendering.
- `Max = 3`：Represents the size of the `Viewport.RenderInfoType` enum.

## Scaling3DModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Bilinear = 0`：Use bilinear scaling for the viewport's 3D buffer.
- `Fsr = 1`：Use AMD FidelityFX Super Resolution 1.0 upscaling for the viewport's 3D buffer.
- `Fsr2 = 2`：Use AMD FidelityFX Super Resolution 2.2 upscaling for the viewport's 3D buffer.
- `MetalfxSpatial = 3`：Use the [MetalFX spatial upscaler](https://developer.apple.com/documentation/metalfx/mtlfxspatialscaler#overview) for the viewport's 3D buffer.
- `MetalfxTemporal = 4`：Use the [MetalFX temporal upscaler](https://developer.apple.com/documentation/metalfx/mtlfxtemporalscaler#overview) for the viewport's 3D buffer.
- `Nearest = 5`：Use nearest-neighbor filtering for the viewport's 3D buffer.
- `Max = 6`：Represents the size of the `Viewport.Scaling3DModeEnum` enum.

## ScreenSpaceAAEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not perform any antialiasing in the full screen post-process.
- `Fxaa = 1`：Use fast approximate antialiasing.
- `Smaa = 2`：Use subpixel morphological antialiasing.
- `Max = 3`：Represents the size of the `Viewport.ScreenSpaceAAEnum` enum.

## SdfOversizeEnum

命名空间 `Godot`；enum

### 枚举值

- `Oversize100Percent = 0`：The signed distance field only covers the viewport's own rectangle.
- `Oversize120Percent = 1`：The signed distance field is expanded to cover 20% of the viewport's size around the borders.
- `Oversize150Percent = 2`：The signed distance field is expanded to cover 50% of the viewport's size around the borders.
- `Oversize200Percent = 3`：The signed distance field is expanded to cover 100% (double) of the viewport's size around the borders.
- `Max = 4`：Represents the size of the `Viewport.SdfOversizeEnum` enum.

## SdfScaleEnum

命名空间 `Godot`；enum

### 枚举值

- `Scale100Percent = 0`：The signed distance field is rendered at full resolution.
- `Scale50Percent = 1`：The signed distance field is rendered at half the resolution of this viewport.
- `Scale25Percent = 2`：The signed distance field is rendered at a quarter the resolution of this viewport.
- `Max = 3`：Represents the size of the `Viewport.SdfScaleEnum` enum.

## VrsModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Variable Rate Shading is disabled.
- `Texture = 1`：Variable Rate Shading uses a texture.
- `XR = 2`：Variable Rate Shading's texture is supplied by the primary [`XRInterface`](XRInterface.md).
- `Max = 3`：Represents the size of the `Viewport.VrsModeEnum` enum.

## VrsUpdateModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：The input texture for variable rate shading will not be processed.
- `Once = 1`：The input texture for variable rate shading will be processed once.
- `Always = 2`：The input texture for variable rate shading will be processed each frame.
- `Max = 3`：Represents the size of the `Viewport.VrsUpdateModeEnum` enum.
