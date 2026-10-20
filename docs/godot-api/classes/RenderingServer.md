<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderingServer

命名空间 `Godot`；static class

The rendering server is the API backend for everything visible. The whole scene system mounts on it to display. The rendering server is completely opaque: the internals are entirely implementation-specific and cannot be accessed.

The rendering server can be used to bypass the scene/[`Node`](Node.md) system entirely. This can improve performance in cases where the scene system is the bottleneck, but won't improve performance otherwise (for instance, if the GPU is already fully utilized).

Resources are created using the `*_create` functions. These functions return [`Rid`](Rid.md)s which are not references to the objects themselves, but opaque *pointers* towards these objects.

All objects are drawn to a viewport. You can use the [`Viewport`](Viewport.md) attached to the [`SceneTree`](SceneTree.md) or you can create one yourself with `ViewportCreate()`. When using a custom scenario or canvas, the scenario or canvas needs to be attached to the viewport using `ViewportSetScenario()` or `ViewportAttachCanvas()`.

**Scenarios:** In 3D, all visual objects must be associated with a scenario. The scenario is a visual representation of the world. If accessing the rendering server from a running game, the scenario can be accessed from the scene tree from any [`Node3D`](Node3D.md) node with [`Node3D.GetWorld3D`](Node3D.md). Otherwise, a scenario can be created with `ScenarioCreate()`.

Similarly, in 2D, a canvas is needed to draw all canvas items.

**3D:** In 3D, all visible objects are comprised of a resource and an instance. A resource can be a mesh, a particle system, a light, or any other 3D object. In order to be visible resources must be attached to an instance using `InstanceSetBase()`. The instance must also be attached to the scenario using `InstanceSetScenario()` in order to be visible. RenderingServer methods that don't have a prefix are usually 3D-specific (but not always).

**2D:** In 2D, all visible objects are some form of canvas item. In order to be visible, a canvas item needs to be the child of a canvas attached to a viewport, or it needs to be the child of another canvas item that is eventually attached to the canvas. 2D-specific RenderingServer methods generally start with `canvas_*`.

**Headless mode:** Starting the engine with the `--headless` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html) disables all rendering and window management functions. Most functions from `RenderingServer` will return dummy values in this case.

## 属性

### RenderLoopEnabled

```csharp
public static bool RenderLoopEnabled { get; set; }
```

引擎名：`render_loop_enabled`

If `false`, disables rendering completely, but the engine logic is still being processed. You can call `ForceDraw()` to draw a frame even with rendering disabled.

### Singleton

```csharp
public static RenderingServerInstance Singleton { get; }
```

## 方法

### AreaLightCreate

```csharp
public static Rid AreaLightCreate()
```

引擎名：`area_light_create`

Creates a new area light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this area light to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`AreaLight3D`](AreaLight3D.md).

### BakeRenderUV2

```csharp
public static Array<Image> BakeRenderUV2(Rid base, Array<Rid> materialOverrides, Vector2I imageSize)
```

引擎名：`bake_render_uv2`

Bakes the material data of the Mesh passed in the `base` parameter with optional `materialOverrides` to a set of [`Image`](Image.md)s of size `imageSize`. Returns an array of [`Image`](Image.md)s containing material properties as specified in [`RenderingServer.BakeChannels`](RenderingServer.md).

### CallOnRenderThread

```csharp
public static void CallOnRenderThread(Callable callable)
```

引擎名：`call_on_render_thread`

As the RenderingServer actual logic may run on a separate thread, accessing its internals from the main (or any other) thread will result in errors. To make it easier to run code that can safely access the rendering internals (such as [`RenderingDevice`](RenderingDevice.md) and similar RD classes), push a callable via this function so it will be executed on the render thread.

### CameraAttributesCreate

```csharp
public static Rid CameraAttributesCreate()
```

引擎名：`camera_attributes_create`

Creates a camera attributes object and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `camera_attributes_` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`CameraAttributes`](CameraAttributes.md).

### CameraAttributesSetAutoExposure

```csharp
public static void CameraAttributesSetAutoExposure(Rid cameraAttributes, bool enable, float minSensitivity, float maxSensitivity, float speed, float scale)
```

引擎名：`camera_attributes_set_auto_exposure`

Sets the parameters to use with the auto-exposure effect. These parameters take on the same meaning as their counterparts in [`CameraAttributes`](CameraAttributes.md) and [`CameraAttributesPractical`](CameraAttributesPractical.md).

### CameraAttributesSetDofBlur

```csharp
public static void CameraAttributesSetDofBlur(Rid cameraAttributes, bool farEnable, float farDistance, float farTransition, bool nearEnable, float nearDistance, float nearTransition, float amount)
```

引擎名：`camera_attributes_set_dof_blur`

Sets the parameters to use with the DOF blur effect. These parameters take on the same meaning as their counterparts in [`CameraAttributesPractical`](CameraAttributesPractical.md).

### CameraAttributesSetDofBlurBokehShape

```csharp
public static void CameraAttributesSetDofBlurBokehShape(RenderingServer.DofBokehShape shape)
```

引擎名：`camera_attributes_set_dof_blur_bokeh_shape`

Sets the shape of the DOF bokeh pattern to `shape`. Different shapes may be used to achieve artistic effect, or to meet performance targets.

### CameraAttributesSetDofBlurQuality

```csharp
public static void CameraAttributesSetDofBlurQuality(RenderingServer.DofBlurQuality quality, bool useJitter)
```

引擎名：`camera_attributes_set_dof_blur_quality`

Sets the quality level of the DOF blur effect to `quality`. `useJitter` can be used to jitter samples taken during the blur pass to hide artifacts at the cost of looking more fuzzy.

### CameraAttributesSetExposure

```csharp
public static void CameraAttributesSetExposure(Rid cameraAttributes, float multiplier, float normalization)
```

引擎名：`camera_attributes_set_exposure`

Sets the exposure values that will be used by the renderers. The normalization amount is used to bake a given Exposure Value (EV) into rendering calculations to reduce the dynamic range of the scene.

The normalization factor can be calculated from exposure value (EV100) as follows:

```csharp
func get_exposure_normalization(ev100: float):
return 1.0 / (pow(2.0, ev100) * 1.2)

```

The exposure value can be calculated from aperture (in f-stops), shutter speed (in seconds), and sensitivity (in ISO) as follows:

```csharp
func get_exposure(aperture: float, shutter_speed: float, sensitivity: float):
return log((aperture * aperture) / shutter_speed * (100.0 / sensitivity)) / log(2)

```

### CameraCreate

```csharp
public static Rid CameraCreate()
```

引擎名：`camera_create`

Creates a 3D camera and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `camera_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`Camera3D`](Camera3D.md).

### CameraSetCameraAttributes

```csharp
public static void CameraSetCameraAttributes(Rid camera, Rid effects)
```

引擎名：`camera_set_camera_attributes`

Sets the camera_attributes created with `CameraAttributesCreate()` to the given camera.

### CameraSetCompositor

```csharp
public static void CameraSetCompositor(Rid camera, Rid compositor)
```

引擎名：`camera_set_compositor`

Sets the compositor used by this camera. Equivalent to [`Camera3D.Compositor`](Camera3D.md).

### CameraSetCullMask

```csharp
public static void CameraSetCullMask(Rid camera, uint layers)
```

引擎名：`camera_set_cull_mask`

Sets the cull mask associated with this camera. The cull mask describes which 3D layers are rendered by this camera. Equivalent to [`Camera3D.CullMask`](Camera3D.md).

### CameraSetEnvironment

```csharp
public static void CameraSetEnvironment(Rid camera, Rid env)
```

引擎名：`camera_set_environment`

Sets the environment used by this camera. Equivalent to [`Camera3D.Environment`](Camera3D.md).

### CameraSetFrustum

```csharp
public static void CameraSetFrustum(Rid camera, float size, Vector2 offset, float zNear, float zFar)
```

引擎名：`camera_set_frustum`

Sets camera to use frustum projection. This mode allows adjusting the `offset` argument to create "tilted frustum" effects.

### CameraSetOrthogonal

```csharp
public static void CameraSetOrthogonal(Rid camera, float size, float zNear, float zFar)
```

引擎名：`camera_set_orthogonal`

Sets camera to use orthogonal projection, also known as orthographic projection. Objects remain the same size on the screen no matter how far away they are.

### CameraSetPerspective

```csharp
public static void CameraSetPerspective(Rid camera, float fovyDegrees, float zNear, float zFar)
```

引擎名：`camera_set_perspective`

Sets camera to use perspective projection. Objects on the screen becomes smaller when they are far away.

### CameraSetTransform

```csharp
public static void CameraSetTransform(Rid camera, Transform3D transform)
```

引擎名：`camera_set_transform`

Sets [`Transform3D`](Transform3D.md) of camera.

### CameraSetUseVerticalAspect

```csharp
public static void CameraSetUseVerticalAspect(Rid camera, bool enable)
```

引擎名：`camera_set_use_vertical_aspect`

If `true`, preserves the horizontal aspect ratio which is equivalent to [`Camera3D.KeepAspectEnum.Width`](Camera3D.md). If `false`, preserves the vertical aspect ratio which is equivalent to [`Camera3D.KeepAspectEnum.Height`](Camera3D.md).

### CanvasCreate

```csharp
public static Rid CanvasCreate()
```

引擎名：`canvas_create`

Creates a canvas and returns the assigned [`Rid`](Rid.md). It can be accessed with the RID that is returned. This RID will be used in all `canvas_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

Canvas has no [`Resource`](Resource.md) or [`Node`](Node.md) equivalent.

### CanvasItemAddAnimationSlice

```csharp
public static void CanvasItemAddAnimationSlice(Rid item, double animationLength, double sliceBegin, double sliceEnd, double offset = 0)
```

引擎名：`canvas_item_add_animation_slice`

Subsequent drawing commands will be ignored unless they fall within the specified animation slice. This is a faster way to implement animations that loop on background rather than redrawing constantly.

### CanvasItemAddCircle

```csharp
public static void CanvasItemAddCircle(Rid item, Vector2 pos, float radius, Color color)
```

引擎名：`canvas_item_add_circle`

Draws a circle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawCircle`](CanvasItem.md).

### CanvasItemAddCircle

```csharp
public static void CanvasItemAddCircle(Rid item, Vector2 pos, float radius, Color color, bool antialiased = false)
```

引擎名：`canvas_item_add_circle`

Draws a circle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawCircle`](CanvasItem.md).

### CanvasItemAddClipIgnore

```csharp
public static void CanvasItemAddClipIgnore(Rid item, bool ignore)
```

引擎名：`canvas_item_add_clip_ignore`

If `ignore` is `true`, ignore clipping on items drawn with this canvas item until this is called again with `ignore` set to `false`.

### CanvasItemAddEllipse

```csharp
public static void CanvasItemAddEllipse(Rid item, Vector2 pos, float major, float minor, Color color, bool antialiased = false)
```

引擎名：`canvas_item_add_ellipse`

Draws an ellipse with semi-major axis `major` and semi-minor axis `minor` on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawEllipse`](CanvasItem.md).

### CanvasItemAddLcdTextureRectRegion

```csharp
public static void CanvasItemAddLcdTextureRectRegion(Rid item, Rect2 rect, Rid texture, Rect2 srcRect, Color modulate)
```

引擎名：`canvas_item_add_lcd_texture_rect_region`

See also [`CanvasItem.DrawLcdTextureRectRegion`](CanvasItem.md).

### CanvasItemAddLine

```csharp
public static void CanvasItemAddLine(Rid item, Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)
```

引擎名：`canvas_item_add_line`

Draws a line on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawLine`](CanvasItem.md).

### CanvasItemAddMesh

```csharp
public static void CanvasItemAddMesh(Rid item, Rid mesh, Transform2D? transform = null, Color? modulate = null, Rid texture = default)
```

引擎名：`canvas_item_add_mesh`

Draws a mesh created with `MeshCreate()` with given `transform`, `modulate` color, and `texture`. This is used internally by [`MeshInstance2D`](MeshInstance2D.md).

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddMsdfTextureRectRegion

```csharp
public static void CanvasItemAddMsdfTextureRectRegion(Rid item, Rect2 rect, Rid texture, Rect2 srcRect, Color? modulate = null, int outlineSize = 0, float pxRange = 1f, float scale = 1f)
```

引擎名：`canvas_item_add_msdf_texture_rect_region`

See also [`CanvasItem.DrawMsdfTextureRectRegion`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddMultiline

```csharp
public static void CanvasItemAddMultiline(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width)
```

引擎名：`canvas_item_add_multiline`

Draws a 2D multiline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultiline`](CanvasItem.md) and [`CanvasItem.DrawMultilineColors`](CanvasItem.md).

### CanvasItemAddMultiline

```csharp
public static void CanvasItemAddMultiline(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased)
```

引擎名：`canvas_item_add_multiline`

Draws a 2D multiline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultiline`](CanvasItem.md) and [`CanvasItem.DrawMultilineColors`](CanvasItem.md).

### CanvasItemAddMultiline

```csharp
public static void CanvasItemAddMultiline(Rid item, Vector2[] points, Color[] colors, float width = -1f, bool antialiased = false)
```

引擎名：`canvas_item_add_multiline`

Draws a 2D multiline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultiline`](CanvasItem.md) and [`CanvasItem.DrawMultilineColors`](CanvasItem.md).

### CanvasItemAddMultiline

```csharp
public static void CanvasItemAddMultiline(Rid item, Vector2[] points, Color[] colors, float width)
```

引擎名：`canvas_item_add_multiline`

Draws a 2D multiline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultiline`](CanvasItem.md) and [`CanvasItem.DrawMultilineColors`](CanvasItem.md).

### CanvasItemAddMultimesh

```csharp
public static void CanvasItemAddMultimesh(Rid item, Rid mesh, Rid texture = default)
```

引擎名：`canvas_item_add_multimesh`

Draws a 2D [`MultiMesh`](MultiMesh.md) on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultimesh`](CanvasItem.md).

### CanvasItemAddNinePatch

```csharp
public static void CanvasItemAddNinePatch(Rid item, Rect2 rect, Rect2 source, Rid texture, Vector2 topleft, Vector2 bottomright, RenderingServer.NinePatchAxisMode xAxisMode = RenderingServer.NinePatchAxisMode.Stretch, RenderingServer.NinePatchAxisMode yAxisMode = RenderingServer.NinePatchAxisMode.Stretch, bool drawCenter = true, Color? modulate = null)
```

引擎名：`canvas_item_add_nine_patch`

Draws a nine-patch rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddParticles

```csharp
public static void CanvasItemAddParticles(Rid item, Rid particles, Rid texture)
```

引擎名：`canvas_item_add_particles`

Draws particles on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md).

### CanvasItemAddPolygon

```csharp
public static void CanvasItemAddPolygon(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Rid texture)
```

引擎名：`canvas_item_add_polygon`

Draws a 2D polygon on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). If you need more flexibility (such as being able to use bones), use `CanvasItemAddTriangleArray()` instead. See also [`CanvasItem.DrawPolygon`](CanvasItem.md).

**Note:** If you frequently redraw the same polygon with a large number of vertices, consider pre-calculating the triangulation with [`Geometry2D.TriangulatePolygon`](Geometry2D.md) and using [`CanvasItem.DrawMesh`](CanvasItem.md), [`CanvasItem.DrawMultimesh`](CanvasItem.md), or `CanvasItemAddTriangleArray()`.

### CanvasItemAddPolygon

```csharp
public static void CanvasItemAddPolygon(Rid item, Vector2[] points, Color[] colors, Vector2[] uvs = null, Rid texture = default)
```

引擎名：`canvas_item_add_polygon`

Draws a 2D polygon on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). If you need more flexibility (such as being able to use bones), use `CanvasItemAddTriangleArray()` instead. See also [`CanvasItem.DrawPolygon`](CanvasItem.md).

**Note:** If you frequently redraw the same polygon with a large number of vertices, consider pre-calculating the triangulation with [`Geometry2D.TriangulatePolygon`](Geometry2D.md) and using [`CanvasItem.DrawMesh`](CanvasItem.md), [`CanvasItem.DrawMultimesh`](CanvasItem.md), or `CanvasItemAddTriangleArray()`.

参数：
- `uvs`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.

### CanvasItemAddPolyline

```csharp
public static void CanvasItemAddPolyline(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased)
```

引擎名：`canvas_item_add_polyline`

Draws a 2D polyline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawPolyline`](CanvasItem.md) and [`CanvasItem.DrawPolylineColors`](CanvasItem.md).

### CanvasItemAddPolyline

```csharp
public static void CanvasItemAddPolyline(Rid item, Vector2[] points, Color[] colors, float width = -1f, bool antialiased = false)
```

引擎名：`canvas_item_add_polyline`

Draws a 2D polyline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawPolyline`](CanvasItem.md) and [`CanvasItem.DrawPolylineColors`](CanvasItem.md).

### CanvasItemAddPrimitive

```csharp
public static void CanvasItemAddPrimitive(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Rid texture)
```

引擎名：`canvas_item_add_primitive`

Draws a 2D primitive on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawPrimitive`](CanvasItem.md).

### CanvasItemAddPrimitive

```csharp
public static void CanvasItemAddPrimitive(Rid item, Vector2[] points, Color[] colors, Vector2[] uvs, Rid texture)
```

引擎名：`canvas_item_add_primitive`

Draws a 2D primitive on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawPrimitive`](CanvasItem.md).

### CanvasItemAddRect

```csharp
public static void CanvasItemAddRect(Rid item, Rect2 rect, Color color)
```

引擎名：`canvas_item_add_rect`

Draws a rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawRect`](CanvasItem.md).

### CanvasItemAddRect

```csharp
public static void CanvasItemAddRect(Rid item, Rect2 rect, Color color, bool antialiased = false)
```

引擎名：`canvas_item_add_rect`

Draws a rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawRect`](CanvasItem.md).

### CanvasItemAddSetTransform

```csharp
public static void CanvasItemAddSetTransform(Rid item, Transform2D transform)
```

引擎名：`canvas_item_add_set_transform`

Sets a [`Transform2D`](Transform2D.md) that will be used to transform subsequent canvas item commands.

### CanvasItemAddTextureRect

```csharp
public static void CanvasItemAddTextureRect(Rid item, Rect2 rect, Rid texture, bool tile = false, Color? modulate = null, bool transpose = false)
```

引擎名：`canvas_item_add_texture_rect`

Draws a 2D textured rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawTextureRect`](CanvasItem.md) and [`Texture2D.DrawRect`](Texture2D.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddTextureRectRegion

```csharp
public static void CanvasItemAddTextureRectRegion(Rid item, Rect2 rect, Rid texture, Rect2 srcRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
```

引擎名：`canvas_item_add_texture_rect_region`

Draws the specified region of a 2D textured rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawTextureRectRegion`](CanvasItem.md) and [`Texture2D.DrawRectRegion`](Texture2D.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddTriangleArray

```csharp
public static void CanvasItemAddTriangleArray(Rid item, ReadOnlySpan<int> indices, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> bones, ReadOnlySpan<float> weights, Rid texture, int count)
```

引擎名：`canvas_item_add_triangle_array`

Draws a triangle array on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). This is internally used by [`Line2D`](Line2D.md) and [`StyleBoxFlat`](StyleBoxFlat.md) for rendering. `CanvasItemAddTriangleArray()` is highly flexible, but more complex to use than `CanvasItemAddPolygon()`.

**Note:** If `count` is set to a non-negative value, only the first `count * 3` indices (corresponding to `count` triangles) will be drawn. Otherwise, all indices are drawn.

### CanvasItemAddTriangleArray

```csharp
public static void CanvasItemAddTriangleArray(Rid item, int[] indices, Vector2[] points, Color[] colors, Vector2[] uvs = null, int[] bones = null, float[] weights = null, Rid texture = default, int count = -1)
```

引擎名：`canvas_item_add_triangle_array`

Draws a triangle array on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). This is internally used by [`Line2D`](Line2D.md) and [`StyleBoxFlat`](StyleBoxFlat.md) for rendering. `CanvasItemAddTriangleArray()` is highly flexible, but more complex to use than `CanvasItemAddPolygon()`.

**Note:** If `count` is set to a non-negative value, only the first `count * 3` indices (corresponding to `count` triangles) will be drawn. Otherwise, all indices are drawn.

参数：
- `uvs`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.
- `bones`：If the parameter is null, then the default value is `Array.Empty<int>()`.
- `weights`：If the parameter is null, then the default value is `Array.Empty<float>()`.

### CanvasItemAttachSkeleton

```csharp
public static void CanvasItemAttachSkeleton(Rid item, Rid skeleton)
```

引擎名：`canvas_item_attach_skeleton`

Attaches a skeleton to the [`CanvasItem`](CanvasItem.md). Removes the previous skeleton.

### CanvasItemClear

```csharp
public static void CanvasItemClear(Rid item)
```

引擎名：`canvas_item_clear`

Clears the [`CanvasItem`](CanvasItem.md) and removes all commands in it.

### CanvasItemCreate

```csharp
public static Rid CanvasItemCreate()
```

引擎名：`canvas_item_create`

Creates a new CanvasItem instance and returns its [`Rid`](Rid.md). It can be accessed with the RID that is returned. This RID will be used in all `canvas_item_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`CanvasItem`](CanvasItem.md).

### CanvasItemGetInstanceShaderParameter

```csharp
public static Variant CanvasItemGetInstanceShaderParameter(Rid instance, StringName parameter)
```

引擎名：`canvas_item_get_instance_shader_parameter`

Returns the value of the per-instance shader uniform from the specified canvas item instance. Equivalent to [`CanvasItem.GetInstanceShaderParameter`](CanvasItem.md).

### CanvasItemGetInstanceShaderParameterDefaultValue

```csharp
public static Variant CanvasItemGetInstanceShaderParameterDefaultValue(Rid instance, StringName parameter)
```

引擎名：`canvas_item_get_instance_shader_parameter_default_value`

Returns the default value of the per-instance shader uniform from the specified canvas item instance. Equivalent to [`CanvasItem.GetInstanceShaderParameter`](CanvasItem.md).

### CanvasItemGetInstanceShaderParameterList

```csharp
public static Array<Dictionary> CanvasItemGetInstanceShaderParameterList(Rid instance)
```

引擎名：`canvas_item_get_instance_shader_parameter_list`

Returns a dictionary of per-instance shader uniform names of the per-instance shader uniform from the specified canvas item instance.

The returned dictionary is in PropertyInfo format, with the keys `name`, `class_name`, `type`, `hint`, `hint_string`, and `usage`.

### CanvasItemResetPhysicsInterpolation

```csharp
public static void CanvasItemResetPhysicsInterpolation(Rid item)
```

引擎名：`canvas_item_reset_physics_interpolation`

Prevents physics interpolation for the current physics tick.

This is useful when moving a canvas item to a new location, to give an instantaneous change rather than interpolation from the previous location.

### CanvasItemSetCanvasGroupMode

```csharp
public static void CanvasItemSetCanvasGroupMode(Rid item, RenderingServer.CanvasGroupMode mode, float clearMargin = 5f, bool fitEmpty = false, float fitMargin = 0f, bool blurMipmaps = false)
```

引擎名：`canvas_item_set_canvas_group_mode`

Sets the canvas group mode used during 2D rendering for the canvas item specified by the `item` RID. For faster but more limited clipping, use `CanvasItemSetClip()` instead.

**Note:** The equivalent node functionality is found in [`CanvasGroup`](CanvasGroup.md) and [`CanvasItem.ClipChildren`](CanvasItem.md).

### CanvasItemSetClip

```csharp
public static void CanvasItemSetClip(Rid item, bool clip)
```

引擎名：`canvas_item_set_clip`

If `clip` is `true`, makes the canvas item specified by the `item` RID not draw anything outside of its rect's coordinates. This clipping is fast, but works only with axis-aligned rectangles. This means that rotation is ignored by the clipping rectangle. For more advanced clipping shapes, use `CanvasItemSetCanvasGroupMode()` instead.

**Note:** The equivalent node functionality is found in [`Label.ClipText`](Label.md), [`RichTextLabel`](RichTextLabel.md) (always enabled) and more.

### CanvasItemSetCopyToBackbuffer

```csharp
public static void CanvasItemSetCopyToBackbuffer(Rid item, bool enabled, Rect2 rect)
```

引擎名：`canvas_item_set_copy_to_backbuffer`

Sets the [`CanvasItem`](CanvasItem.md) to copy a rect to the backbuffer.

### CanvasItemSetCustomRect

```csharp
public static void CanvasItemSetCustomRect(Rid item, bool useCustomRect, Rect2? rect = null)
```

引擎名：`canvas_item_set_custom_rect`

If `useCustomRect` is `true`, sets the custom visibility rectangle (used for culling) to `rect` for the canvas item specified by `item`. Setting a custom visibility rect can reduce CPU load when drawing lots of 2D instances. If `useCustomRect` is `false`, automatically computes a visibility rectangle based on the canvas item's draw commands.

参数：
- `rect`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### CanvasItemSetDefaultTextureFilter

```csharp
public static void CanvasItemSetDefaultTextureFilter(Rid item, RenderingServer.CanvasItemTextureFilter filter)
```

引擎名：`canvas_item_set_default_texture_filter`

Sets the default texture filter mode for the canvas item specified by the `item` RID. Equivalent to [`CanvasItem.TextureFilter`](CanvasItem.md).

### CanvasItemSetDefaultTextureRepeat

```csharp
public static void CanvasItemSetDefaultTextureRepeat(Rid item, RenderingServer.CanvasItemTextureRepeat repeat)
```

引擎名：`canvas_item_set_default_texture_repeat`

Sets the default texture repeat mode for the canvas item specified by the `item` RID. Equivalent to [`CanvasItem.TextureRepeat`](CanvasItem.md).

### CanvasItemSetDistanceFieldMode

```csharp
public static void CanvasItemSetDistanceFieldMode(Rid item, bool enabled)
```

引擎名：`canvas_item_set_distance_field_mode`

If `enabled` is `true`, enables multichannel signed distance field rendering mode for the canvas item specified by the `item` RID. This is meant to be used for font rendering, or with specially generated images using [msdfgen](https://github.com/Chlumsky/msdfgen).

### CanvasItemSetDrawBehindParent

```csharp
public static void CanvasItemSetDrawBehindParent(Rid item, bool enabled)
```

引擎名：`canvas_item_set_draw_behind_parent`

If `enabled` is `true`, draws the canvas item specified by the `item` RID behind its parent. Equivalent to [`CanvasItem.ShowBehindParent`](CanvasItem.md).

### CanvasItemSetDrawIndex

```csharp
public static void CanvasItemSetDrawIndex(Rid item, int index)
```

引擎名：`canvas_item_set_draw_index`

Sets the index for the [`CanvasItem`](CanvasItem.md).

### CanvasItemSetInstanceShaderParameter

```csharp
public static void CanvasItemSetInstanceShaderParameter(Rid instance, StringName parameter, Variant value)
```

引擎名：`canvas_item_set_instance_shader_parameter`

Sets the per-instance shader uniform on the specified canvas item instance. Equivalent to [`CanvasItem.SetInstanceShaderParameter`](CanvasItem.md).

### CanvasItemSetInterpolated

```csharp
public static void CanvasItemSetInterpolated(Rid item, bool interpolated)
```

引擎名：`canvas_item_set_interpolated`

If `interpolated` is `true`, turns on physics interpolation for the canvas item.

### CanvasItemSetLightMask

```csharp
public static void CanvasItemSetLightMask(Rid item, int mask)
```

引擎名：`canvas_item_set_light_mask`

Sets the light `mask` for the canvas item specified by the `item` RID. Equivalent to [`CanvasItem.LightMask`](CanvasItem.md).

### CanvasItemSetMaterial

```csharp
public static void CanvasItemSetMaterial(Rid item, Rid material)
```

引擎名：`canvas_item_set_material`

Sets a new `material` to the canvas item specified by the `item` RID. Equivalent to [`CanvasItem.Material`](CanvasItem.md).

### CanvasItemSetModulate

```csharp
public static void CanvasItemSetModulate(Rid item, Color color)
```

引擎名：`canvas_item_set_modulate`

Multiplies the color of the canvas item specified by the `item` RID, while affecting its children. See also `CanvasItemSetSelfModulate()`. Equivalent to [`CanvasItem.Modulate`](CanvasItem.md).

### CanvasItemSetParent

```csharp
public static void CanvasItemSetParent(Rid item, Rid parent)
```

引擎名：`canvas_item_set_parent`

Sets a parent [`CanvasItem`](CanvasItem.md) to the [`CanvasItem`](CanvasItem.md). The item will inherit transform, modulation and visibility from its parent, like [`CanvasItem`](CanvasItem.md) nodes in the scene tree.

### CanvasItemSetSelfModulate

```csharp
public static void CanvasItemSetSelfModulate(Rid item, Color color)
```

引擎名：`canvas_item_set_self_modulate`

Multiplies the color of the canvas item specified by the `item` RID, without affecting its children. See also `CanvasItemSetModulate()`. Equivalent to [`CanvasItem.SelfModulate`](CanvasItem.md).

### CanvasItemSetSortChildrenByY

```csharp
public static void CanvasItemSetSortChildrenByY(Rid item, bool enabled)
```

引擎名：`canvas_item_set_sort_children_by_y`

If `enabled` is `true`, child nodes with the lowest Y position are drawn before those with a higher Y position. Y-sorting only affects children that inherit from the canvas item specified by the `item` RID, not the canvas item itself. Equivalent to [`CanvasItem.YSortEnabled`](CanvasItem.md).

### CanvasItemSetTransform

```csharp
public static void CanvasItemSetTransform(Rid item, Transform2D transform)
```

引擎名：`canvas_item_set_transform`

Sets the `transform` of the canvas item specified by the `item` RID. This affects where and how the item will be drawn. Child canvas items' transforms are multiplied by their parent's transform. Equivalent to [`Node2D.Transform`](Node2D.md).

### CanvasItemSetUseParentMaterial

```csharp
public static void CanvasItemSetUseParentMaterial(Rid item, bool enabled)
```

引擎名：`canvas_item_set_use_parent_material`

Sets if the [`CanvasItem`](CanvasItem.md) uses its parent's material.

### CanvasItemSetVisibilityLayer

```csharp
public static void CanvasItemSetVisibilityLayer(Rid item, uint visibilityLayer)
```

引擎名：`canvas_item_set_visibility_layer`

Sets the rendering visibility layer associated with this [`CanvasItem`](CanvasItem.md). Only [`Viewport`](Viewport.md) nodes with a matching rendering mask will render this [`CanvasItem`](CanvasItem.md).

### CanvasItemSetVisibilityNotifier

```csharp
public static void CanvasItemSetVisibilityNotifier(Rid item, bool enable, Rect2 area, Callable enterCallable, Callable exitCallable)
```

引擎名：`canvas_item_set_visibility_notifier`

Sets the given [`CanvasItem`](CanvasItem.md) as visibility notifier. `area` defines the area of detecting visibility. `enterCallable` is called when the [`CanvasItem`](CanvasItem.md) enters the screen, `exitCallable` is called when the [`CanvasItem`](CanvasItem.md) exits the screen. If `enable` is `false`, the item will no longer function as notifier.

This method can be used to manually mimic [`VisibleOnScreenNotifier2D`](VisibleOnScreenNotifier2D.md).

### CanvasItemSetVisible

```csharp
public static void CanvasItemSetVisible(Rid item, bool visible)
```

引擎名：`canvas_item_set_visible`

Sets the visibility of the [`CanvasItem`](CanvasItem.md).

### CanvasItemSetZAsRelativeToParent

```csharp
public static void CanvasItemSetZAsRelativeToParent(Rid item, bool enabled)
```

引擎名：`canvas_item_set_z_as_relative_to_parent`

If this is enabled, the Z index of the parent will be added to the children's Z index.

### CanvasItemSetZIndex

```csharp
public static void CanvasItemSetZIndex(Rid item, int zIndex)
```

引擎名：`canvas_item_set_z_index`

Sets the [`CanvasItem`](CanvasItem.md)'s Z index, i.e. its draw order (lower indexes are drawn first).

### CanvasItemTransformPhysicsInterpolation

```csharp
public static void CanvasItemTransformPhysicsInterpolation(Rid item, Transform2D transform)
```

引擎名：`canvas_item_transform_physics_interpolation`

Transforms both the current and previous stored transform for a canvas item.

This allows transforming a canvas item without creating a "glitch" in the interpolation, which is particularly useful for large worlds utilizing a shifting origin.

### CanvasLightAttachToCanvas

```csharp
public static void CanvasLightAttachToCanvas(Rid light, Rid canvas)
```

引擎名：`canvas_light_attach_to_canvas`

Attaches the canvas light to the canvas. Removes it from its previous canvas.

### CanvasLightCreate

```csharp
public static Rid CanvasLightCreate()
```

引擎名：`canvas_light_create`

Creates a canvas light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `canvas_light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`Light2D`](Light2D.md).

### CanvasLightOccluderAttachToCanvas

```csharp
public static void CanvasLightOccluderAttachToCanvas(Rid occluder, Rid canvas)
```

引擎名：`canvas_light_occluder_attach_to_canvas`

Attaches a light occluder to the canvas. Removes it from its previous canvas.

### CanvasLightOccluderCreate

```csharp
public static Rid CanvasLightOccluderCreate()
```

引擎名：`canvas_light_occluder_create`

Creates a light occluder and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `canvas_light_occluder_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`LightOccluder2D`](LightOccluder2D.md).

### CanvasLightOccluderResetPhysicsInterpolation

```csharp
public static void CanvasLightOccluderResetPhysicsInterpolation(Rid occluder)
```

引擎名：`canvas_light_occluder_reset_physics_interpolation`

Prevents physics interpolation for the current physics tick.

This is useful when moving an occluder to a new location, to give an instantaneous change rather than interpolation from the previous location.

### CanvasLightOccluderSetAsSdfCollision

```csharp
public static void CanvasLightOccluderSetAsSdfCollision(Rid occluder, bool enable)
```

引擎名：`canvas_light_occluder_set_as_sdf_collision`

Enables or disables using the light occluder as a signed distance field for 2D particle collision.

### CanvasLightOccluderSetEnabled

```csharp
public static void CanvasLightOccluderSetEnabled(Rid occluder, bool enabled)
```

引擎名：`canvas_light_occluder_set_enabled`

Enables or disables light occluder.

### CanvasLightOccluderSetInterpolated

```csharp
public static void CanvasLightOccluderSetInterpolated(Rid occluder, bool interpolated)
```

引擎名：`canvas_light_occluder_set_interpolated`

If `interpolated` is `true`, turns on physics interpolation for the light occluder.

### CanvasLightOccluderSetLightMask

```csharp
public static void CanvasLightOccluderSetLightMask(Rid occluder, int mask)
```

引擎名：`canvas_light_occluder_set_light_mask`

The light mask. See [`LightOccluder2D`](LightOccluder2D.md) for more information on light masks.

### CanvasLightOccluderSetPolygon

```csharp
public static void CanvasLightOccluderSetPolygon(Rid occluder, Rid polygon)
```

引擎名：`canvas_light_occluder_set_polygon`

Sets a light occluder's polygon.

### CanvasLightOccluderSetTransform

```csharp
public static void CanvasLightOccluderSetTransform(Rid occluder, Transform2D transform)
```

引擎名：`canvas_light_occluder_set_transform`

Sets a light occluder's [`Transform2D`](Transform2D.md).

### CanvasLightOccluderTransformPhysicsInterpolation

```csharp
public static void CanvasLightOccluderTransformPhysicsInterpolation(Rid occluder, Transform2D transform)
```

引擎名：`canvas_light_occluder_transform_physics_interpolation`

Transforms both the current and previous stored transform for a light occluder.

This allows transforming an occluder without creating a "glitch" in the interpolation, which is particularly useful for large worlds utilizing a shifting origin.

### CanvasLightResetPhysicsInterpolation

```csharp
public static void CanvasLightResetPhysicsInterpolation(Rid light)
```

引擎名：`canvas_light_reset_physics_interpolation`

Prevents physics interpolation for the current physics tick.

This is useful when moving a canvas item to a new location, to give an instantaneous change rather than interpolation from the previous location.

### CanvasLightSetBlendMode

```csharp
public static void CanvasLightSetBlendMode(Rid light, RenderingServer.CanvasLightBlendMode mode)
```

引擎名：`canvas_light_set_blend_mode`

Sets the blend mode for the given canvas light to `mode`. Equivalent to [`Light2D.BlendMode`](Light2D.md).

### CanvasLightSetColor

```csharp
public static void CanvasLightSetColor(Rid light, Color color)
```

引擎名：`canvas_light_set_color`

Sets the color for a light.

### CanvasLightSetEnabled

```csharp
public static void CanvasLightSetEnabled(Rid light, bool enabled)
```

引擎名：`canvas_light_set_enabled`

Enables or disables a canvas light.

### CanvasLightSetEnergy

```csharp
public static void CanvasLightSetEnergy(Rid light, float energy)
```

引擎名：`canvas_light_set_energy`

Sets a canvas light's energy.

### CanvasLightSetHeight

```csharp
public static void CanvasLightSetHeight(Rid light, float height)
```

引擎名：`canvas_light_set_height`

Sets a canvas light's height.

### CanvasLightSetInterpolated

```csharp
public static void CanvasLightSetInterpolated(Rid light, bool interpolated)
```

引擎名：`canvas_light_set_interpolated`

If `interpolated` is `true`, turns on physics interpolation for the canvas light.

### CanvasLightSetItemCullMask

```csharp
public static void CanvasLightSetItemCullMask(Rid light, int mask)
```

引擎名：`canvas_light_set_item_cull_mask`

The light mask. See [`LightOccluder2D`](LightOccluder2D.md) for more information on light masks.

### CanvasLightSetItemShadowCullMask

```csharp
public static void CanvasLightSetItemShadowCullMask(Rid light, int mask)
```

引擎名：`canvas_light_set_item_shadow_cull_mask`

The binary mask used to determine which layers this canvas light's shadows affects. See [`LightOccluder2D`](LightOccluder2D.md) for more information on light masks.

### CanvasLightSetLayerRange

```csharp
public static void CanvasLightSetLayerRange(Rid light, int minLayer, int maxLayer)
```

引擎名：`canvas_light_set_layer_range`

The layer range that gets rendered with this light.

### CanvasLightSetMode

```csharp
public static void CanvasLightSetMode(Rid light, RenderingServer.CanvasLightMode mode)
```

引擎名：`canvas_light_set_mode`

Sets the mode of the canvas light.

### CanvasLightSetShadowColor

```csharp
public static void CanvasLightSetShadowColor(Rid light, Color color)
```

引擎名：`canvas_light_set_shadow_color`

Sets the color of the canvas light's shadow.

### CanvasLightSetShadowEnabled

```csharp
public static void CanvasLightSetShadowEnabled(Rid light, bool enabled)
```

引擎名：`canvas_light_set_shadow_enabled`

Enables or disables the canvas light's shadow.

### CanvasLightSetShadowFilter

```csharp
public static void CanvasLightSetShadowFilter(Rid light, RenderingServer.CanvasLightShadowFilter filter)
```

引擎名：`canvas_light_set_shadow_filter`

Sets the canvas light's shadow's filter.

### CanvasLightSetShadowSmooth

```csharp
public static void CanvasLightSetShadowSmooth(Rid light, float smooth)
```

引擎名：`canvas_light_set_shadow_smooth`

Smoothens the shadow. The lower, the smoother.

### CanvasLightSetTexture

```csharp
public static void CanvasLightSetTexture(Rid light, Rid texture)
```

引擎名：`canvas_light_set_texture`

Sets the texture to be used by a [`PointLight2D`](PointLight2D.md). Equivalent to [`PointLight2D.Texture`](PointLight2D.md).

### CanvasLightSetTextureOffset

```csharp
public static void CanvasLightSetTextureOffset(Rid light, Vector2 offset)
```

引擎名：`canvas_light_set_texture_offset`

Sets the offset of a [`PointLight2D`](PointLight2D.md)'s texture. Equivalent to [`PointLight2D.Offset`](PointLight2D.md).

### CanvasLightSetTextureScale

```csharp
public static void CanvasLightSetTextureScale(Rid light, float scale)
```

引擎名：`canvas_light_set_texture_scale`

Sets the scale factor of a [`PointLight2D`](PointLight2D.md)'s texture. Equivalent to [`PointLight2D.TextureScale`](PointLight2D.md).

### CanvasLightSetTransform

```csharp
public static void CanvasLightSetTransform(Rid light, Transform2D transform)
```

引擎名：`canvas_light_set_transform`

Sets the canvas light's [`Transform2D`](Transform2D.md).

### CanvasLightSetZRange

```csharp
public static void CanvasLightSetZRange(Rid light, int minZ, int maxZ)
```

引擎名：`canvas_light_set_z_range`

Sets the Z range of objects that will be affected by this light. Equivalent to [`Light2D.RangeZMin`](Light2D.md) and [`Light2D.RangeZMax`](Light2D.md).

### CanvasLightTransformPhysicsInterpolation

```csharp
public static void CanvasLightTransformPhysicsInterpolation(Rid light, Transform2D transform)
```

引擎名：`canvas_light_transform_physics_interpolation`

Transforms both the current and previous stored transform for a canvas light.

This allows transforming a light without creating a "glitch" in the interpolation, which is particularly useful for large worlds utilizing a shifting origin.

### CanvasOccluderPolygonCreate

```csharp
public static Rid CanvasOccluderPolygonCreate()
```

引擎名：`canvas_occluder_polygon_create`

Creates a new light occluder polygon and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `canvas_occluder_polygon_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`OccluderPolygon2D`](OccluderPolygon2D.md).

### CanvasOccluderPolygonSetCullMode

```csharp
public static void CanvasOccluderPolygonSetCullMode(Rid occluderPolygon, RenderingServer.CanvasOccluderPolygonCullMode mode)
```

引擎名：`canvas_occluder_polygon_set_cull_mode`

Sets an occluder polygon's cull mode.

### CanvasOccluderPolygonSetShape

```csharp
public static void CanvasOccluderPolygonSetShape(Rid occluderPolygon, ReadOnlySpan<Vector2> shape, bool closed)
```

引擎名：`canvas_occluder_polygon_set_shape`

Sets the shape of the occluder polygon.

### CanvasOccluderPolygonSetShape

```csharp
public static void CanvasOccluderPolygonSetShape(Rid occluderPolygon, Vector2[] shape, bool closed)
```

引擎名：`canvas_occluder_polygon_set_shape`

Sets the shape of the occluder polygon.

### CanvasSetDisableScale

```csharp
public static void CanvasSetDisableScale(bool disable)
```

引擎名：`canvas_set_disable_scale`

If `disable` is `true`, makes 2D rendering ignore the canvas scale defined for each canvas layer. This affects [`CanvasLayer`](CanvasLayer.md)s with the [`CanvasLayer.FollowViewportEnabled`](CanvasLayer.md) property set to `true`.

In the editor, this is set to `true` by default, and set to `false` when **View > Preview Canvas Scale** is enabled at the top of the 2D editor viewport.

**Note:** Setting this to `true` does not impact the behavior of [`CanvasLayer.Scale`](CanvasLayer.md), [`Node2D.Scale`](Node2D.md), or [`Control.Scale`](Control.md).

### CanvasSetItemMirroring

```csharp
public static void CanvasSetItemMirroring(Rid canvas, Rid item, Vector2 mirroring)
```

引擎名：`canvas_set_item_mirroring`

A copy of the canvas item will be drawn with a local offset of the `mirroring`.

**Note:** This is equivalent to calling `CanvasSetItemRepeat()` like `canvas_set_item_repeat(item, mirroring, 1)`, with an additional check ensuring `canvas` is a parent of `item`.

### CanvasSetItemRepeat

```csharp
public static void CanvasSetItemRepeat(Rid item, Vector2 repeatSize, int repeatTimes)
```

引擎名：`canvas_set_item_repeat`

A copy of the canvas item will be drawn with a local offset of the `repeatSize` by the number of times of the `repeatTimes`. As the `repeatTimes` increases, the copies will spread away from the origin texture.

### CanvasSetModulate

```csharp
public static void CanvasSetModulate(Rid canvas, Color color)
```

引擎名：`canvas_set_modulate`

Modulates all colors in the given canvas.

### CanvasSetShadowTextureSize

```csharp
public static void CanvasSetShadowTextureSize(int size)
```

引擎名：`canvas_set_shadow_texture_size`

Sets the `ProjectSettings.rendering/2d/shadow_atlas/size` to use for [`Light2D`](Light2D.md) shadow rendering (in pixels). The value is rounded up to the nearest power of 2.

### CanvasTextureCreate

```csharp
public static Rid CanvasTextureCreate()
```

引擎名：`canvas_texture_create`

Creates a canvas texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `canvas_texture_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method. See also `Texture2DCreate()`.

**Note:** The equivalent resource is [`CanvasTexture`](CanvasTexture.md) and is only meant to be used in 2D rendering, not 3D.

### CanvasTextureSetChannel

```csharp
public static void CanvasTextureSetChannel(Rid canvasTexture, RenderingServer.CanvasTextureChannel channel, Rid texture)
```

引擎名：`canvas_texture_set_channel`

Sets the `channel`'s `texture` for the canvas texture specified by the `canvasTexture` RID. Equivalent to [`CanvasTexture.DiffuseTexture`](CanvasTexture.md), [`CanvasTexture.NormalTexture`](CanvasTexture.md) and [`CanvasTexture.SpecularTexture`](CanvasTexture.md).

### CanvasTextureSetShadingParameters

```csharp
public static void CanvasTextureSetShadingParameters(Rid canvasTexture, Color baseColor, float shininess)
```

引擎名：`canvas_texture_set_shading_parameters`

Sets the `baseColor` and `shininess` to use for the canvas texture specified by the `canvasTexture` RID. Equivalent to [`CanvasTexture.SpecularColor`](CanvasTexture.md) and [`CanvasTexture.SpecularShininess`](CanvasTexture.md).

### CanvasTextureSetTextureFilter

```csharp
public static void CanvasTextureSetTextureFilter(Rid canvasTexture, RenderingServer.CanvasItemTextureFilter filter)
```

引擎名：`canvas_texture_set_texture_filter`

Sets the texture `filter` mode to use for the canvas texture specified by the `canvasTexture` RID.

### CanvasTextureSetTextureRepeat

```csharp
public static void CanvasTextureSetTextureRepeat(Rid canvasTexture, RenderingServer.CanvasItemTextureRepeat repeat)
```

引擎名：`canvas_texture_set_texture_repeat`

Sets the texture `repeat` mode to use for the canvas texture specified by the `canvasTexture` RID.

### CompositorCreate

```csharp
public static Rid CompositorCreate()
```

引擎名：`compositor_create`

Creates a new compositor and adds it to the RenderingServer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

### CompositorEffectCreate

```csharp
public static Rid CompositorEffectCreate()
```

引擎名：`compositor_effect_create`

Creates a new rendering effect and adds it to the RenderingServer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

### CompositorEffectSetCallback

```csharp
public static void CompositorEffectSetCallback(Rid effect, RenderingServer.CompositorEffectCallbackType callbackType, Callable callback)
```

引擎名：`compositor_effect_set_callback`

Sets the callback type (`callbackType`) and callback method(`callback`) for this rendering effect.

### CompositorEffectSetEnabled

```csharp
public static void CompositorEffectSetEnabled(Rid effect, bool enabled)
```

引擎名：`compositor_effect_set_enabled`

Enables/disables this rendering effect.

### CompositorEffectSetFlag

```csharp
public static void CompositorEffectSetFlag(Rid effect, RenderingServer.CompositorEffectFlags flag, bool set)
```

引擎名：`compositor_effect_set_flag`

Sets the flag (`flag`) for this rendering effect to `true` or `false` (`set`).

### CompositorSetCompositorEffects

```csharp
public static void CompositorSetCompositorEffects(Rid compositor, Array<Rid> effects)
```

引擎名：`compositor_set_compositor_effects`

Sets the compositor effects for the specified compositor RID. `effects` should be an array containing RIDs created with `CompositorEffectCreate()`.

### CreateLocalRenderingDevice

```csharp
public static RenderingDevice CreateLocalRenderingDevice()
```

引擎名：`create_local_rendering_device`

Creates a RenderingDevice that can be used to do draw and compute operations on a separate thread. Cannot draw to the screen nor share data with the global RenderingDevice.

**Note:** When using the OpenGL rendering driver or when running in headless mode, this function always returns `null`.

### DebugCanvasItemGetRect

```csharp
public static Rect2 DebugCanvasItemGetRect(Rid item)
```

引擎名：`debug_canvas_item_get_rect`

Returns the bounding rectangle for a canvas item in local space, as calculated by the renderer. This bound is used internally for culling.

**Warning:** This function is intended for debugging in the editor, and will pass through and return a zero [`Rect2`](Rect2.md) in exported projects.

### DecalCreate

```csharp
public static Rid DecalCreate()
```

引擎名：`decal_create`

Creates a decal and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `decal_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this decal to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`Decal`](Decal.md).

### DecalSetAlbedoMix

```csharp
public static void DecalSetAlbedoMix(Rid decal, float albedoMix)
```

引擎名：`decal_set_albedo_mix`

Sets the `albedoMix` in the decal specified by the `decal` RID. Equivalent to [`Decal.AlbedoMix`](Decal.md).

### DecalSetCullMask

```csharp
public static void DecalSetCullMask(Rid decal, uint mask)
```

引擎名：`decal_set_cull_mask`

Sets the cull `mask` in the decal specified by the `decal` RID. Equivalent to [`Decal.CullMask`](Decal.md).

### DecalSetDistanceFade

```csharp
public static void DecalSetDistanceFade(Rid decal, bool enabled, float begin, float length)
```

引擎名：`decal_set_distance_fade`

Sets the distance fade parameters in the decal specified by the `decal` RID. Equivalent to [`Decal.DistanceFadeEnabled`](Decal.md), [`Decal.DistanceFadeBegin`](Decal.md) and [`Decal.DistanceFadeLength`](Decal.md).

### DecalSetEmissionEnergy

```csharp
public static void DecalSetEmissionEnergy(Rid decal, float energy)
```

引擎名：`decal_set_emission_energy`

Sets the emission `energy` in the decal specified by the `decal` RID. Equivalent to [`Decal.EmissionEnergy`](Decal.md).

### DecalSetFade

```csharp
public static void DecalSetFade(Rid decal, float above, float below)
```

引擎名：`decal_set_fade`

Sets the upper fade (`above`) and lower fade (`below`) in the decal specified by the `decal` RID. Equivalent to [`Decal.UpperFade`](Decal.md) and [`Decal.LowerFade`](Decal.md).

### DecalSetModulate

```csharp
public static void DecalSetModulate(Rid decal, Color color)
```

引擎名：`decal_set_modulate`

Sets the color multiplier in the decal specified by the `decal` RID to `color`. Equivalent to [`Decal.Modulate`](Decal.md).

### DecalSetNormalFade

```csharp
public static void DecalSetNormalFade(Rid decal, float fade)
```

引擎名：`decal_set_normal_fade`

Sets the normal `fade` in the decal specified by the `decal` RID. Equivalent to [`Decal.NormalFade`](Decal.md).

### DecalSetSize

```csharp
public static void DecalSetSize(Rid decal, Vector3 size)
```

引擎名：`decal_set_size`

Sets the `size` of the decal specified by the `decal` RID. Equivalent to [`Decal.Size`](Decal.md).

### DecalSetTexture

```csharp
public static void DecalSetTexture(Rid decal, RenderingServer.DecalTexture type, Rid texture)
```

引擎名：`decal_set_texture`

Sets the `texture` in the given texture `type` slot for the specified decal. Equivalent to [`Decal.SetTexture`](Decal.md).

### DecalsSetFilter

```csharp
public static void DecalsSetFilter(RenderingServer.DecalFilter filter)
```

引擎名：`decals_set_filter`

Sets the texture `filter` mode to use when rendering decals. This parameter is global and cannot be set on a per-decal basis.

### DirectionalLightCreate

```csharp
public static Rid DirectionalLightCreate()
```

引擎名：`directional_light_create`

Creates a directional light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this directional light to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`DirectionalLight3D`](DirectionalLight3D.md).

### DirectionalShadowAtlasSetSize

```csharp
public static void DirectionalShadowAtlasSetSize(int size, bool is16Bits)
```

引擎名：`directional_shadow_atlas_set_size`

Sets the `size` of the directional light shadows in 3D. See also `ProjectSettings.rendering/lights_and_shadows/directional_shadow/size`. This parameter is global and cannot be set on a per-viewport basis.

### DirectionalSoftShadowFilterSetQuality

```csharp
public static void DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality quality)
```

引擎名：`directional_soft_shadow_filter_set_quality`

Sets the filter `quality` for directional light shadows in 3D. See also `ProjectSettings.rendering/lights_and_shadows/directional_shadow/soft_shadow_filter_quality`. This parameter is global and cannot be set on a per-viewport basis.

### EnvironmentBakePanorama

```csharp
public static Image EnvironmentBakePanorama(Rid environment, bool bakeIrradiance, Vector2I size)
```

引擎名：`environment_bake_panorama`

Generates and returns an [`Image`](Image.md) containing the radiance map for the specified `environment` RID's sky. This supports built-in sky material and custom sky shaders. If `bakeIrradiance` is `true`, the irradiance map is saved instead of the radiance map. The radiance map is used to render reflected light, while the irradiance map is used to render ambient light. See also `SkyBakePanorama()`.

**Note:** The image is saved using linear encoding without any tonemapping performed, which means it will look too dark if viewed directly in an image editor.

**Note:** `size` should be a 2:1 aspect ratio for the generated panorama to have square pixels. For radiance maps, there is no point in using a height greater than [`Sky.RadianceSize`](Sky.md), as it won't increase detail. Irradiance maps only contain low-frequency data, so there is usually no point in going past a size of 128×64 pixels when saving an irradiance map.

### EnvironmentCreate

```csharp
public static Rid EnvironmentCreate()
```

引擎名：`environment_create`

Creates an environment and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `environment_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Environment`](Environment.md).

### EnvironmentGlowSetUseBicubicUpscale

```csharp
public static void EnvironmentGlowSetUseBicubicUpscale(bool enable)
```

引擎名：`environment_glow_set_use_bicubic_upscale`

If `enable` is `true`, enables bicubic upscaling for glow which improves quality at the cost of performance. Equivalent to `ProjectSettings.rendering/environment/glow/upscale_mode`.

**Note:** This setting is only effective when using the Forward+ or Mobile rendering methods, as Compatibility uses a different glow implementation.

### EnvironmentSetAdjustment

```csharp
public static void EnvironmentSetAdjustment(Rid env, bool enable, float brightness, float contrast, float saturation, bool use1DColorCorrection, Rid colorCorrection)
```

引擎名：`environment_set_adjustment`

Sets the values to be used with the "adjustments" post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetAmbientLight

```csharp
public static void EnvironmentSetAmbientLight(Rid env, Color color, RenderingServer.EnvironmentAmbientSource ambient = RenderingServer.EnvironmentAmbientSource.Bg, float energy = 1f, float skyContribution = 0f, RenderingServer.EnvironmentReflectionSource reflectionSource = RenderingServer.EnvironmentReflectionSource.Bg)
```

引擎名：`environment_set_ambient_light`

Sets the values to be used for ambient light rendering. See [`Environment`](Environment.md) for more details.

### EnvironmentSetBackground

```csharp
public static void EnvironmentSetBackground(Rid env, RenderingServer.EnvironmentBG bg)
```

引擎名：`environment_set_background`

Sets the environment's background mode. Equivalent to [`Environment.BackgroundMode`](Environment.md).

### EnvironmentSetBgColor

```csharp
public static void EnvironmentSetBgColor(Rid env, Color color)
```

引擎名：`environment_set_bg_color`

Color displayed for clear areas of the scene. Only effective if using the [`RenderingServer.EnvironmentBG.Color`](RenderingServer.md) background mode.

### EnvironmentSetBgEnergy

```csharp
public static void EnvironmentSetBgEnergy(Rid env, float multiplier, float exposureValue)
```

引擎名：`environment_set_bg_energy`

Sets the intensity of the background color.

### EnvironmentSetCameraId

```csharp
public static void EnvironmentSetCameraId(Rid env, int id)
```

引擎名：`environment_set_camera_id`

Sets the camera ID to be used as environment background.

### EnvironmentSetCanvasMaxLayer

```csharp
public static void EnvironmentSetCanvasMaxLayer(Rid env, int maxLayer)
```

引擎名：`environment_set_canvas_max_layer`

Sets the maximum layer to use if using Canvas background mode.

### EnvironmentSetFog

```csharp
public static void EnvironmentSetFog(Rid env, bool enable, Color lightColor, float lightEnergy, float sunScatter, float density, float height, float heightDensity, float aerialPerspective, float skyAffect)
```

引擎名：`environment_set_fog`

Configures fog for the specified environment RID. See `fog_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetFog

```csharp
public static void EnvironmentSetFog(Rid env, bool enable, Color lightColor, float lightEnergy, float sunScatter, float density, float height, float heightDensity, float aerialPerspective, float skyAffect, RenderingServer.EnvironmentFogMode fogMode = RenderingServer.EnvironmentFogMode.Exponential)
```

引擎名：`environment_set_fog`

Configures fog for the specified environment RID. See `fog_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetFogDepth

```csharp
public static void EnvironmentSetFogDepth(Rid env, float curve, float begin, float end)
```

引擎名：`environment_set_fog_depth`

Configures fog depth for the specified environment RID. Only has an effect when the fog mode of the environment is [`RenderingServer.EnvironmentFogMode.Depth`](RenderingServer.md). See `fog_depth_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetGlow

```csharp
public static void EnvironmentSetGlow(Rid env, bool enable, ReadOnlySpan<float> levels, float intensity, float strength, float mix, float bloomThreshold, RenderingServer.EnvironmentGlowBlendMode blendMode, float hdrBleedThreshold, float hdrBleedScale, float hdrLuminanceCap, float glowMapStrength, Rid glowMap)
```

引擎名：`environment_set_glow`

Configures glow for the specified environment RID. See `glow_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetGlow

```csharp
public static void EnvironmentSetGlow(Rid env, bool enable, float[] levels, float intensity, float strength, float mix, float bloomThreshold, RenderingServer.EnvironmentGlowBlendMode blendMode, float hdrBleedThreshold, float hdrBleedScale, float hdrLuminanceCap, float glowMapStrength, Rid glowMap)
```

引擎名：`environment_set_glow`

Configures glow for the specified environment RID. See `glow_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetSdfgi

```csharp
public static void EnvironmentSetSdfgi(Rid env, bool enable, int cascades, float minCellSize, RenderingServer.EnvironmentSdfgiyScale yScale, bool useOcclusion, float bounceFeedback, bool readSky, float energy, float normalBias, float probeBias)
```

引擎名：`environment_set_sdfgi`

Configures signed distance field global illumination for the specified environment RID. See `sdfgi_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetSdfgiFramesToConverge

```csharp
public static void EnvironmentSetSdfgiFramesToConverge(RenderingServer.EnvironmentSdfgiFramesToConverge frames)
```

引擎名：`environment_set_sdfgi_frames_to_converge`

Sets the number of frames to use for converging signed distance field global illumination. Equivalent to `ProjectSettings.rendering/global_illumination/sdfgi/frames_to_converge`.

### EnvironmentSetSdfgiFramesToUpdateLight

```csharp
public static void EnvironmentSetSdfgiFramesToUpdateLight(RenderingServer.EnvironmentSdfgiFramesToUpdateLight frames)
```

引擎名：`environment_set_sdfgi_frames_to_update_light`

Sets the update speed for dynamic lights' indirect lighting when computing signed distance field global illumination. Equivalent to `ProjectSettings.rendering/global_illumination/sdfgi/frames_to_update_lights`.

### EnvironmentSetSdfgiRayCount

```csharp
public static void EnvironmentSetSdfgiRayCount(RenderingServer.EnvironmentSdfgiRayCount rayCount)
```

引擎名：`environment_set_sdfgi_ray_count`

Sets the number of rays to throw per frame when computing signed distance field global illumination. Equivalent to `ProjectSettings.rendering/global_illumination/sdfgi/probe_ray_count`.

### EnvironmentSetSky

```csharp
public static void EnvironmentSetSky(Rid env, Rid sky)
```

引擎名：`environment_set_sky`

Sets the [`Sky`](Sky.md) to be used as the environment's background when using *BGMode* sky. Equivalent to [`Environment.Sky`](Environment.md).

### EnvironmentSetSkyCustomFov

```csharp
public static void EnvironmentSetSkyCustomFov(Rid env, float scale)
```

引擎名：`environment_set_sky_custom_fov`

Sets a custom field of view for the background [`Sky`](Sky.md). Equivalent to [`Environment.SkyCustomFov`](Environment.md).

### EnvironmentSetSkyOrientation

```csharp
public static void EnvironmentSetSkyOrientation(Rid env, Basis orientation)
```

引擎名：`environment_set_sky_orientation`

Sets the rotation of the background [`Sky`](Sky.md) expressed as a [`Basis`](Basis.md). Equivalent to [`Environment.SkyRotation`](Environment.md), where the rotation vector is used to construct the [`Basis`](Basis.md).

### EnvironmentSetSsao

```csharp
public static void EnvironmentSetSsao(Rid env, bool enable, float radius, float intensity, float power, float detail, float horizon, float sharpness, float lightAffect, float aOChannelAffect)
```

引擎名：`environment_set_ssao`

Sets the variables to be used with the screen-space ambient occlusion (SSAO) post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetSsaoQuality

```csharp
public static void EnvironmentSetSsaoQuality(RenderingServer.EnvironmentSsaoQuality quality, bool halfSize, float adaptiveTarget, int blurPasses, float fadeOutFrom, float fadeOutTo)
```

引擎名：`environment_set_ssao_quality`

Sets the quality level of the screen-space ambient occlusion (SSAO) post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetSsilQuality

```csharp
public static void EnvironmentSetSsilQuality(RenderingServer.EnvironmentSsilQuality quality, bool halfSize, float adaptiveTarget, int blurPasses, float fadeOutFrom, float fadeOutTo)
```

引擎名：`environment_set_ssil_quality`

Sets the quality level of the screen-space indirect lighting (SSIL) post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetSsr

```csharp
public static void EnvironmentSetSsr(Rid env, bool enable, int maxSteps, float fadeIn, float fadeOut, float depthTolerance)
```

引擎名：`environment_set_ssr`

Sets the variables to be used with the screen-space reflections (SSR) post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetSsrHalfSize

```csharp
public static void EnvironmentSetSsrHalfSize(bool halfSize)
```

引擎名：`environment_set_ssr_half_size`

Sets whether screen-space reflections will be rendered at full or half size. Half size is faster, but may look pixelated or cause flickering.

### EnvironmentSetSsrRoughnessQuality

```csharp
public static void EnvironmentSetSsrRoughnessQuality(RenderingServer.EnvironmentSsrRoughnessQuality quality)
```

引擎名：`environment_set_ssr_roughness_quality`

### EnvironmentSetTonemap

```csharp
public static void EnvironmentSetTonemap(Rid env, RenderingServer.EnvironmentToneMapper toneMapper, float exposure, float white)
```

引擎名：`environment_set_tonemap`

Sets the variables to be used with the "tonemap" post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetTonemapAgxContrast

```csharp
public static void EnvironmentSetTonemapAgxContrast(Rid env, float agxContrast)
```

引擎名：`environment_set_tonemap_agx_contrast`

See [`Environment.TonemapAgxContrast`](Environment.md) for more details.

### EnvironmentSetVolumetricFog

```csharp
public static void EnvironmentSetVolumetricFog(Rid env, bool enable, float density, Color albedo, Color emission, float emissionEnergy, float anisotropy, float length, float detailSpread, float gIInject, bool temporalReprojection, float temporalReprojectionAmount, float ambientInject, float skyAffect)
```

引擎名：`environment_set_volumetric_fog`

Sets the variables to be used with the volumetric fog post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetVolumetricFogFilterActive

```csharp
public static void EnvironmentSetVolumetricFogFilterActive(bool active)
```

引擎名：`environment_set_volumetric_fog_filter_active`

Enables filtering of the volumetric fog scattering buffer. This results in much smoother volumes with very few under-sampling artifacts.

### EnvironmentSetVolumetricFogVolumeSize

```csharp
public static void EnvironmentSetVolumetricFogVolumeSize(int size, int depth)
```

引擎名：`environment_set_volumetric_fog_volume_size`

Sets the resolution of the volumetric fog's froxel buffer. `size` is modified by the screen's aspect ratio and then used to set the width and height of the buffer. While `depth` is directly used to set the depth of the buffer.

### FogVolumeCreate

```csharp
public static Rid FogVolumeCreate()
```

引擎名：`fog_volume_create`

Creates a new fog volume and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `fog_volume_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`FogVolume`](FogVolume.md).

### FogVolumeSetMaterial

```csharp
public static void FogVolumeSetMaterial(Rid fogVolume, Rid material)
```

引擎名：`fog_volume_set_material`

Sets the [`Material`](Material.md) of the fog volume. Can be either a [`FogMaterial`](FogMaterial.md) or a custom [`ShaderMaterial`](ShaderMaterial.md).

### FogVolumeSetShape

```csharp
public static void FogVolumeSetShape(Rid fogVolume, RenderingServer.FogVolumeShape shape)
```

引擎名：`fog_volume_set_shape`

Sets the shape of the fog volume to either [`RenderingServer.FogVolumeShape.Ellipsoid`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cone`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cylinder`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Box`](RenderingServer.md) or [`RenderingServer.FogVolumeShape.World`](RenderingServer.md).

### FogVolumeSetSize

```csharp
public static void FogVolumeSetSize(Rid fogVolume, Vector3 size)
```

引擎名：`fog_volume_set_size`

Sets the size of the fog volume when shape is [`RenderingServer.FogVolumeShape.Ellipsoid`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cone`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cylinder`](RenderingServer.md) or [`RenderingServer.FogVolumeShape.Box`](RenderingServer.md).

### ForceDraw

```csharp
public static void ForceDraw(bool swapBuffers = true, double frameStep = 0)
```

引擎名：`force_draw`

Forces redrawing of all viewports at once. Must be called from the main thread.

### ForceSync

```csharp
public static void ForceSync()
```

引擎名：`force_sync`

Forces a synchronization between the CPU and GPU, which may be required in certain cases. Only call this when needed, as CPU-GPU synchronization has a performance cost.

### FreeRid

```csharp
public static void FreeRid(Rid rid)
```

引擎名：`free_rid`

Tries to free an object in the RenderingServer. To avoid memory leaks, this should be called after using an object as memory management does not occur automatically when using RenderingServer directly.

### GISetUseHalfResolution

```csharp
public static void GISetUseHalfResolution(bool halfResolution)
```

引擎名：`gi_set_use_half_resolution`

If `halfResolution` is `true`, renders [`VoxelGI`](VoxelGI.md) and SDFGI ([`Environment.SdfgiEnabled`](Environment.md)) buffers at halved resolution on each axis (e.g. 960×540 when the viewport size is 1920×1080). This improves performance significantly when VoxelGI or SDFGI is enabled, at the cost of artifacts that may be visible on polygon edges. The loss in quality becomes less noticeable as the viewport resolution increases. [`LightmapGI`](LightmapGI.md) rendering is not affected by this setting. Equivalent to `ProjectSettings.rendering/global_illumination/gi/use_half_resolution`.

### GetCurrentRenderingDriverName

```csharp
public static string GetCurrentRenderingDriverName()
```

引擎名：`get_current_rendering_driver_name`

Returns the name of the current rendering driver. This can be `vulkan`, `d3d12`, `metal`, `opengl3`, `opengl3_es`, or `opengl3_angle`. See also `GetCurrentRenderingMethod()`.

When `ProjectSettings.rendering/renderer/rendering_method` is `forward_plus` or `mobile`, the rendering driver is determined by `ProjectSettings.rendering/rendering_device/driver`.

When `ProjectSettings.rendering/renderer/rendering_method` is `gl_compatibility`, the rendering driver is determined by `ProjectSettings.rendering/gl_compatibility/driver`.

The rendering driver is also determined by the `--rendering-driver` command line argument that overrides this project setting, or an automatic fallback that is applied depending on the hardware.

### GetCurrentRenderingMethod

```csharp
public static string GetCurrentRenderingMethod()
```

引擎名：`get_current_rendering_method`

Returns the name of the current rendering method. This can be `forward_plus`, `mobile`, or `gl_compatibility`. See also `GetCurrentRenderingDriverName()`.

The rendering method is determined by `ProjectSettings.rendering/renderer/rendering_method`, the `--rendering-method` command line argument that overrides this project setting, or an automatic fallback that is applied depending on the hardware.

### GetDefaultClearColor

```csharp
public static Color GetDefaultClearColor()
```

引擎名：`get_default_clear_color`

Returns the default clear color which is used when a specific clear color has not been selected. See also `SetDefaultClearColor()`.

### GetFrameSetupTimeCpu

```csharp
public static double GetFrameSetupTimeCpu()
```

引擎名：`get_frame_setup_time_cpu`

Returns the time taken to setup rendering on the CPU in milliseconds. This value is shared across all viewports and does *not* require `ViewportSetMeasureRenderTime()` to be enabled on a viewport to be queried. See also `ViewportGetMeasuredRenderTimeCpu()`.

### GetRenderingDevice

```csharp
public static RenderingDevice GetRenderingDevice()
```

引擎名：`get_rendering_device`

Returns the global RenderingDevice.

**Note:** When using the OpenGL rendering driver or when running in headless mode, this function always returns `null`.

### GetRenderingInfo

```csharp
public static ulong GetRenderingInfo(RenderingServer.RenderingInfo info)
```

引擎名：`get_rendering_info`

Returns a statistic about the rendering engine which can be used for performance profiling. See also `ViewportGetRenderInfo()`, which returns information specific to a viewport.

**Note:** Only 3D rendering is currently taken into account by some of these values, such as the number of draw calls.

**Note:** Rendering information is not available until at least 2 frames have been rendered by the engine. If rendering information is not available, `GetRenderingInfo()` returns `0`. To print rendering information in `_ready()` successfully, use the following:

```csharp
func _ready():
for _i in 2:
await get_tree().process_frame

print(RenderingServer.get_rendering_info(RENDERING_INFO_TOTAL_DRAW_CALLS_IN_FRAME))

```

### GetShaderParameterList

```csharp
public static Array<Dictionary> GetShaderParameterList(Rid shader)
```

引擎名：`get_shader_parameter_list`

Returns the parameters of a shader.

### GetTestCube

```csharp
public static Rid GetTestCube()
```

引擎名：`get_test_cube`

Returns the RID of the test cube. This mesh will be created and returned on the first call to `GetTestCube()`, then it will be cached for subsequent calls. See also `MakeSphereMesh()`.

### GetTestTexture

```csharp
public static Rid GetTestTexture()
```

引擎名：`get_test_texture`

Returns the RID of a 256×256 texture with a testing pattern on it (in [`Image.Format.Rgb8`](Image.md) format). This texture will be created and returned on the first call to `GetTestTexture()`, then it will be cached for subsequent calls. See also `GetWhiteTexture()`.

**Example:** Get the test texture and apply it to a [`Sprite2D`](Sprite2D.md) node:

```csharp
var texture_rid = RenderingServer.get_test_texture()
var texture = ImageTexture.create_from_image(RenderingServer.texture_2d_get(texture_rid))
$Sprite2D.texture = texture

```

### GetVideoAdapterApiVersion

```csharp
public static string GetVideoAdapterApiVersion()
```

引擎名：`get_video_adapter_api_version`

Returns the version of the graphics video adapter *currently in use* (e.g. "1.2.189" for Vulkan, "3.3.0 NVIDIA 510.60.02" for OpenGL). This version may be different from the actual latest version supported by the hardware, as Godot may not always request the latest version. See also [`OS.GetVideoAdapterDriverInfo`](OS.md).

**Note:** When running a headless or server binary, this function returns an empty string.

### GetVideoAdapterName

```csharp
public static string GetVideoAdapterName()
```

引擎名：`get_video_adapter_name`

Returns the name of the video adapter (e.g. "GeForce GTX 1080/PCIe/SSE2").

**Note:** When running a headless or server binary, this function returns an empty string.

**Note:** On the web platform, some browsers such as Firefox may report a different, fixed GPU name such as "GeForce GTX 980" (regardless of the user's actual GPU model). This is done to make fingerprinting more difficult.

### GetVideoAdapterType

```csharp
public static RenderingDevice.DeviceType GetVideoAdapterType()
```

引擎名：`get_video_adapter_type`

Returns the type of the video adapter. Since dedicated graphics cards from a given generation will *usually* be significantly faster than integrated graphics made in the same generation, the device type can be used as a basis for automatic graphics settings adjustment. However, this is not always true, so make sure to provide users with a way to manually override graphics settings.

**Note:** When using the OpenGL rendering driver or when running in headless mode, this function always returns [`RenderingDevice.DeviceType.Other`](RenderingDevice.md).

### GetVideoAdapterVendor

```csharp
public static string GetVideoAdapterVendor()
```

引擎名：`get_video_adapter_vendor`

Returns the vendor of the video adapter (e.g. "NVIDIA Corporation").

**Note:** When running a headless or server binary, this function returns an empty string.

### GetWhiteTexture

```csharp
public static Rid GetWhiteTexture()
```

引擎名：`get_white_texture`

Returns the ID of a 4×4 white texture (in [`Image.Format.Rgb8`](Image.md) format). This texture will be created and returned on the first call to `GetWhiteTexture()`, then it will be cached for subsequent calls. See also `GetTestTexture()`.

**Example:** Get the white texture and apply it to a [`Sprite2D`](Sprite2D.md) node:

```csharp
var texture_rid = RenderingServer.get_white_texture()
var texture = ImageTexture.create_from_image(RenderingServer.texture_2d_get(texture_rid))
$Sprite2D.texture = texture

```

### GlobalShaderParameterAdd

```csharp
public static void GlobalShaderParameterAdd(StringName name, RenderingServer.GlobalShaderParameterType type, Variant defaultValue)
```

引擎名：`global_shader_parameter_add`

Creates a new global shader uniform.

**Note:** Global shader parameter names are case-sensitive.

### GlobalShaderParameterGet

```csharp
public static Variant GlobalShaderParameterGet(StringName name)
```

引擎名：`global_shader_parameter_get`

Returns the value of the global shader uniform specified by `name`.

**Note:** `GlobalShaderParameterGet()` has a large performance penalty as the rendering thread needs to synchronize with the calling thread, which is slow. Do not use this method during gameplay to avoid stuttering. If you need to read values in a script after setting them, consider creating an autoload where you store the values you need to query at the same time you're setting them as global parameters.

### GlobalShaderParameterGetList

```csharp
public static Array<StringName> GlobalShaderParameterGetList()
```

引擎名：`global_shader_parameter_get_list`

Returns the list of global shader uniform names.

**Note:** `GlobalShaderParameterGet()` has a large performance penalty as the rendering thread needs to synchronize with the calling thread, which is slow. Do not use this method during gameplay to avoid stuttering. If you need to read values in a script after setting them, consider creating an autoload where you store the values you need to query at the same time you're setting them as global parameters.

### GlobalShaderParameterGetType

```csharp
public static RenderingServer.GlobalShaderParameterType GlobalShaderParameterGetType(StringName name)
```

引擎名：`global_shader_parameter_get_type`

Returns the type associated to the global shader uniform specified by `name`.

**Note:** `GlobalShaderParameterGet()` has a large performance penalty as the rendering thread needs to synchronize with the calling thread, which is slow. Do not use this method during gameplay to avoid stuttering. If you need to read values in a script after setting them, consider creating an autoload where you store the values you need to query at the same time you're setting them as global parameters.

### GlobalShaderParameterRemove

```csharp
public static void GlobalShaderParameterRemove(StringName name)
```

引擎名：`global_shader_parameter_remove`

Removes the global shader uniform specified by `name`.

### GlobalShaderParameterSet

```csharp
public static void GlobalShaderParameterSet(StringName name, Variant value)
```

引擎名：`global_shader_parameter_set`

Sets the global shader uniform `name` to `value`.

### GlobalShaderParameterSetOverride

```csharp
public static void GlobalShaderParameterSetOverride(StringName name, Variant value)
```

引擎名：`global_shader_parameter_set_override`

Overrides the global shader uniform `name` with `value`. Equivalent to the [`ShaderGlobalsOverride`](ShaderGlobalsOverride.md) node.

### HasChanged

```csharp
public static bool HasChanged()
```

引擎名：`has_changed`

Returns `true` if changes have been made to the RenderingServer's data. `ForceDraw()` is usually called if this happens.

### HasFeature

```csharp
public static bool HasFeature(RenderingServer.Features feature)
```

引擎名：`has_feature`

This method does nothing and always returns `false`.

### HasOsFeature

```csharp
public static bool HasOsFeature(string feature)
```

引擎名：`has_os_feature`

Returns `true` if the OS supports a certain `feature`. Features might be `s3tc`, `etc`, and `etc2`.

### InstanceAttachObjectInstanceId

```csharp
public static void InstanceAttachObjectInstanceId(Rid instance, ulong id)
```

引擎名：`instance_attach_object_instance_id`

Attaches a unique Object ID to instance. Object ID must be attached to instance for proper culling with `InstancesCullAabb()`, `InstancesCullConvex()`, and `InstancesCullRay()`.

### InstanceAttachSkeleton

```csharp
public static void InstanceAttachSkeleton(Rid instance, Rid skeleton)
```

引擎名：`instance_attach_skeleton`

Attaches a skeleton to an instance. Removes the previous skeleton from the instance.

### InstanceCreate

```csharp
public static Rid InstanceCreate()
```

引擎名：`instance_create`

Creates a visual instance and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `instance_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

An instance is a way of placing a 3D object in the scenario. Objects like particles, meshes, reflection probes and decals need to be associated with an instance to be visible in the scenario using `InstanceSetBase()`.

**Note:** The equivalent node is [`VisualInstance3D`](VisualInstance3D.md).

### InstanceCreate2

```csharp
public static Rid InstanceCreate2(Rid base, Rid scenario)
```

引擎名：`instance_create2`

Creates a visual instance, adds it to the RenderingServer, and sets both base and scenario. It can be accessed with the RID that is returned. This RID will be used in all `instance_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method. This is a shorthand for using `InstanceCreate()` and setting the base and scenario manually.

### InstanceGeometryGetShaderParameter

```csharp
public static Variant InstanceGeometryGetShaderParameter(Rid instance, StringName parameter)
```

引擎名：`instance_geometry_get_shader_parameter`

Returns the value of the per-instance shader uniform from the specified 3D geometry instance. Equivalent to [`GeometryInstance3D.GetInstanceShaderParameter`](GeometryInstance3D.md).

**Note:** Per-instance shader parameter names are case-sensitive.

### InstanceGeometryGetShaderParameterDefaultValue

```csharp
public static Variant InstanceGeometryGetShaderParameterDefaultValue(Rid instance, StringName parameter)
```

引擎名：`instance_geometry_get_shader_parameter_default_value`

Returns the default value of the per-instance shader uniform from the specified 3D geometry instance. Equivalent to [`GeometryInstance3D.GetInstanceShaderParameter`](GeometryInstance3D.md).

### InstanceGeometryGetShaderParameterList

```csharp
public static Array<Dictionary> InstanceGeometryGetShaderParameterList(Rid instance)
```

引擎名：`instance_geometry_get_shader_parameter_list`

Returns a dictionary of per-instance shader uniform names of the per-instance shader uniform from the specified 3D geometry instance. The returned dictionary is in PropertyInfo format, with the keys `name`, `class_name`, `type`, `hint`, `hint_string` and `usage`. Equivalent to [`GeometryInstance3D.GetInstanceShaderParameter`](GeometryInstance3D.md).

### InstanceGeometrySetCastShadowsSetting

```csharp
public static void InstanceGeometrySetCastShadowsSetting(Rid instance, RenderingServer.ShadowCastingSetting shadowCastingSetting)
```

引擎名：`instance_geometry_set_cast_shadows_setting`

Sets the shadow casting setting. Equivalent to [`GeometryInstance3D.CastShadow`](GeometryInstance3D.md).

### InstanceGeometrySetFlag

```csharp
public static void InstanceGeometrySetFlag(Rid instance, RenderingServer.InstanceFlags flag, bool enabled)
```

引擎名：`instance_geometry_set_flag`

Sets the `flag` for a given `instance` to `enabled`.

### InstanceGeometrySetLightmap

```csharp
public static void InstanceGeometrySetLightmap(Rid instance, Rid lightmap, Rect2 lightmapUVScale, int lightmapSlice)
```

引擎名：`instance_geometry_set_lightmap`

Sets the lightmap GI instance to use for the specified 3D geometry instance. The lightmap UV scale for the specified instance (equivalent to [`GeometryInstance3D.GILightmapScale`](GeometryInstance3D.md)) and lightmap atlas slice must also be specified.

### InstanceGeometrySetLodBias

```csharp
public static void InstanceGeometrySetLodBias(Rid instance, float lodBias)
```

引擎名：`instance_geometry_set_lod_bias`

Sets the level of detail bias to use when rendering the specified 3D geometry instance. Higher values result in higher detail from further away. Equivalent to [`GeometryInstance3D.LodBias`](GeometryInstance3D.md).

### InstanceGeometrySetMaterialOverlay

```csharp
public static void InstanceGeometrySetMaterialOverlay(Rid instance, Rid material)
```

引擎名：`instance_geometry_set_material_overlay`

Sets a material that will be rendered for all surfaces on top of active materials for the mesh associated with this instance. Equivalent to [`GeometryInstance3D.MaterialOverlay`](GeometryInstance3D.md).

### InstanceGeometrySetMaterialOverride

```csharp
public static void InstanceGeometrySetMaterialOverride(Rid instance, Rid material)
```

引擎名：`instance_geometry_set_material_override`

Sets a material that will override the material for all surfaces on the mesh associated with this instance. Equivalent to [`GeometryInstance3D.MaterialOverride`](GeometryInstance3D.md).

### InstanceGeometrySetShaderParameter

```csharp
public static void InstanceGeometrySetShaderParameter(Rid instance, StringName parameter, Variant value)
```

引擎名：`instance_geometry_set_shader_parameter`

Sets the per-instance shader uniform on the specified 3D geometry instance. Equivalent to [`GeometryInstance3D.SetInstanceShaderParameter`](GeometryInstance3D.md).

### InstanceGeometrySetTransparency

```csharp
public static void InstanceGeometrySetTransparency(Rid instance, float transparency)
```

引擎名：`instance_geometry_set_transparency`

Sets the transparency for the given geometry instance. Equivalent to [`GeometryInstance3D.Transparency`](GeometryInstance3D.md).

A transparency of `0.0` is fully opaque, while `1.0` is fully transparent. Values greater than `0.0` (exclusive) will force the geometry's materials to go through the transparent pipeline, which is slower to render and can exhibit rendering issues due to incorrect transparency sorting. However, unlike using a transparent material, setting `transparency` to a value greater than `0.0` (exclusive) will *not* disable shadow rendering.

In spatial shaders, `1.0 - transparency` is set as the default value of the `ALPHA` built-in.

**Note:** `transparency` is clamped between `0.0` and `1.0`, so this property cannot be used to make transparent materials more opaque than they originally are.

### InstanceGeometrySetVisibilityRange

```csharp
public static void InstanceGeometrySetVisibilityRange(Rid instance, float min, float max, float minMargin, float maxMargin, RenderingServer.VisibilityRangeFadeMode fadeMode)
```

引擎名：`instance_geometry_set_visibility_range`

Sets the visibility range values for the given geometry instance. Equivalent to [`GeometryInstance3D.VisibilityRangeBegin`](GeometryInstance3D.md) and related properties.

### InstanceResetPhysicsInterpolation

```csharp
public static void InstanceResetPhysicsInterpolation(Rid instance)
```

### InstanceSetBase

```csharp
public static void InstanceSetBase(Rid instance, Rid base)
```

引擎名：`instance_set_base`

Sets the base of the instance. A base can be any of the 3D objects that are created in the RenderingServer that can be displayed. For example, any of the light types, mesh, multimesh, particle system, reflection probe, decal, lightmap, voxel GI and visibility notifiers are all types that can be set as the base of an instance in order to be displayed in the scenario.

### InstanceSetBlendShapeWeight

```csharp
public static void InstanceSetBlendShapeWeight(Rid instance, int shape, float weight)
```

引擎名：`instance_set_blend_shape_weight`

Sets the weight for a given blend shape associated with this instance.

### InstanceSetCustomAabb

```csharp
public static void InstanceSetCustomAabb(Rid instance, Aabb aabb)
```

引擎名：`instance_set_custom_aabb`

Sets a custom AABB to use when culling objects from the view frustum. Equivalent to setting [`GeometryInstance3D.CustomAabb`](GeometryInstance3D.md).

### InstanceSetExtraVisibilityMargin

```csharp
public static void InstanceSetExtraVisibilityMargin(Rid instance, float margin)
```

引擎名：`instance_set_extra_visibility_margin`

Sets a margin to increase the size of the AABB when culling objects from the view frustum. This allows you to avoid culling objects that fall outside the view frustum. Equivalent to [`GeometryInstance3D.ExtraCullMargin`](GeometryInstance3D.md).

### InstanceSetIgnoreCulling

```csharp
public static void InstanceSetIgnoreCulling(Rid instance, bool enabled)
```

引擎名：`instance_set_ignore_culling`

If `true`, ignores all culling on the specified 3D geometry instance, including frustum culling, occlusion culling, and layer culling. This is not the same as [`GeometryInstance3D.IgnoreOcclusionCulling`](GeometryInstance3D.md), which only ignores occlusion culling but leaves frustum and layer culling intact.

### InstanceSetInterpolated

```csharp
public static void InstanceSetInterpolated(Rid instance, bool interpolated)
```

### InstanceSetLayerMask

```csharp
public static void InstanceSetLayerMask(Rid instance, uint mask)
```

引擎名：`instance_set_layer_mask`

Sets the render layers that this instance will be drawn to. Equivalent to [`VisualInstance3D.Layers`](VisualInstance3D.md).

### InstanceSetPivotData

```csharp
public static void InstanceSetPivotData(Rid instance, float sortingOffset, bool useAabbCenter)
```

引擎名：`instance_set_pivot_data`

Sets the sorting offset and switches between using the bounding box or instance origin for depth sorting.

### InstanceSetScenario

```csharp
public static void InstanceSetScenario(Rid instance, Rid scenario)
```

引擎名：`instance_set_scenario`

Sets the scenario that the instance is in. The scenario is the 3D world that the objects will be displayed in.

### InstanceSetSurfaceOverrideMaterial

```csharp
public static void InstanceSetSurfaceOverrideMaterial(Rid instance, int surface, Rid material)
```

引擎名：`instance_set_surface_override_material`

Sets the override material of a specific surface. Equivalent to [`MeshInstance3D.SetSurfaceOverrideMaterial`](MeshInstance3D.md).

### InstanceSetTransform

```csharp
public static void InstanceSetTransform(Rid instance, Transform3D transform)
```

引擎名：`instance_set_transform`

Sets the world space transform of the instance. Equivalent to [`Node3D.GlobalTransform`](Node3D.md).

### InstanceSetVisibilityParent

```csharp
public static void InstanceSetVisibilityParent(Rid instance, Rid parent)
```

引擎名：`instance_set_visibility_parent`

Sets the visibility parent for the given instance. Equivalent to [`Node3D.VisibilityParent`](Node3D.md).

### InstanceSetVisible

```csharp
public static void InstanceSetVisible(Rid instance, bool visible)
```

引擎名：`instance_set_visible`

Sets whether an instance is drawn or not. Equivalent to [`Node3D.Visible`](Node3D.md).

### InstanceTeleport

```csharp
public static void InstanceTeleport(Rid instance)
```

引擎名：`instance_teleport`

Resets motion vectors and other interpolated values. Use this *after* teleporting a mesh from one position to another to avoid ghosting artifacts.

### InstancesCullAabb

```csharp
public static long[] InstancesCullAabb(Aabb aabb, Rid scenario = default)
```

引擎名：`instances_cull_aabb`

Returns an array of object IDs intersecting with the provided AABB. Only 3D nodes that inherit from [`VisualInstance3D`](VisualInstance3D.md) are considered, such as [`MeshInstance3D`](MeshInstance3D.md) or [`DirectionalLight3D`](DirectionalLight3D.md). Use `GodotObject.InstanceFromId` to obtain the actual nodes. A scenario RID must be provided, which is available in the [`World3D`](World3D.md) you want to query. This forces an update for all resources queued to update.

**Warning:** This function is primarily intended for editor usage. For in-game use cases, prefer physics collision.

### InstancesCullConvex

```csharp
public static long[] InstancesCullConvex(Array<Plane> convex, Rid scenario = default)
```

引擎名：`instances_cull_convex`

Returns an array of object IDs intersecting with the provided convex shape. Only 3D nodes that inherit from [`VisualInstance3D`](VisualInstance3D.md) are considered, such as [`MeshInstance3D`](MeshInstance3D.md) or [`DirectionalLight3D`](DirectionalLight3D.md). Use `GodotObject.InstanceFromId` to obtain the actual nodes. A scenario RID must be provided, which is available in the [`World3D`](World3D.md) you want to query. This forces an update for all resources queued to update.

**Warning:** This function is primarily intended for editor usage. For in-game use cases, prefer physics collision.

### InstancesCullRay

```csharp
public static long[] InstancesCullRay(Vector3 from, Vector3 to, Rid scenario = default)
```

引擎名：`instances_cull_ray`

Returns an array of object IDs intersecting with the provided 3D ray. Only 3D nodes that inherit from [`VisualInstance3D`](VisualInstance3D.md) are considered, such as [`MeshInstance3D`](MeshInstance3D.md) or [`DirectionalLight3D`](DirectionalLight3D.md). Use `GodotObject.InstanceFromId` to obtain the actual nodes. A scenario RID must be provided, which is available in the [`World3D`](World3D.md) you want to query. This forces an update for all resources queued to update.

**Warning:** This function is primarily intended for editor usage. For in-game use cases, prefer physics collision.

### IsOnRenderThread

```csharp
public static bool IsOnRenderThread()
```

引擎名：`is_on_render_thread`

Returns `true` if our code is currently executing on the rendering thread.

### IsRenderLoopEnabled

```csharp
public static bool IsRenderLoopEnabled()
```

引擎名：`is_render_loop_enabled`

### LightAreaSetNormalizeEnergy

```csharp
public static void LightAreaSetNormalizeEnergy(Rid light, bool enable)
```

引擎名：`light_area_set_normalize_energy`

Defines whether the energy of an [`AreaLight3D`](AreaLight3D.md) is normalized (divided) by its area. If set to `true`, changing the size does not affect the total energy output. Equivalent to [`AreaLight3D.AreaNormalizeEnergy`](AreaLight3D.md).

### LightAreaSetSize

```csharp
public static void LightAreaSetSize(Rid light, Vector2 size)
```

引擎名：`light_area_set_size`

Sets the extents (width and height) in meters for this area light. Equivalent to [`AreaLight3D.AreaSize`](AreaLight3D.md).

### LightDirectionalSetBlendSplits

```csharp
public static void LightDirectionalSetBlendSplits(Rid light, bool enable)
```

引擎名：`light_directional_set_blend_splits`

If `true`, this directional light will blend between shadow map splits resulting in a smoother transition between them. Equivalent to [`DirectionalLight3D.DirectionalShadowBlendSplits`](DirectionalLight3D.md).

### LightDirectionalSetShadowMode

```csharp
public static void LightDirectionalSetShadowMode(Rid light, RenderingServer.LightDirectionalShadowMode mode)
```

引擎名：`light_directional_set_shadow_mode`

Sets the shadow mode for this directional light. Equivalent to [`DirectionalLight3D.DirectionalShadowMode`](DirectionalLight3D.md).

### LightDirectionalSetSkyMode

```csharp
public static void LightDirectionalSetSkyMode(Rid light, RenderingServer.LightDirectionalSkyMode mode)
```

引擎名：`light_directional_set_sky_mode`

If `true`, this light will not be used for anything except sky shaders. Use this for lights that impact your sky shader that you may want to hide from affecting the rest of the scene. For example, you may want to enable this when the sun in your sky shader falls below the horizon.

### LightOmniSetShadowMode

```csharp
public static void LightOmniSetShadowMode(Rid light, RenderingServer.LightOmniShadowMode mode)
```

引擎名：`light_omni_set_shadow_mode`

Sets whether to use a dual paraboloid or a cubemap for the shadow map. Dual paraboloid is faster but may suffer from artifacts. Equivalent to [`OmniLight3D.OmniShadowMode`](OmniLight3D.md).

### LightProjectorsSetFilter

```csharp
public static void LightProjectorsSetFilter(RenderingServer.LightProjectorFilter filter)
```

引擎名：`light_projectors_set_filter`

Sets the texture filter mode to use when rendering light projectors. This parameter is global and cannot be set on a per-light basis.

### LightSetBakeMode

```csharp
public static void LightSetBakeMode(Rid light, RenderingServer.LightBakeMode bakeMode)
```

引擎名：`light_set_bake_mode`

Sets the bake mode to use for the specified 3D light. Equivalent to [`Light3D.LightBakeMode`](Light3D.md).

### LightSetColor

```csharp
public static void LightSetColor(Rid light, Color color)
```

引擎名：`light_set_color`

Sets the color of the light. Equivalent to [`Light3D.LightColor`](Light3D.md).

### LightSetCullMask

```csharp
public static void LightSetCullMask(Rid light, uint mask)
```

引擎名：`light_set_cull_mask`

Sets the cull mask for this 3D light. Lights only affect objects in the selected layers. Equivalent to [`Light3D.LightCullMask`](Light3D.md).

### LightSetDistanceFade

```csharp
public static void LightSetDistanceFade(Rid decal, bool enabled, float begin, float shadow, float length)
```

引擎名：`light_set_distance_fade`

Sets the distance fade for this 3D light. This acts as a form of level of detail (LOD) and can be used to improve performance. Equivalent to [`Light3D.DistanceFadeEnabled`](Light3D.md), [`Light3D.DistanceFadeBegin`](Light3D.md), [`Light3D.DistanceFadeShadow`](Light3D.md), and [`Light3D.DistanceFadeLength`](Light3D.md).

### LightSetMaxSdfgiCascade

```csharp
public static void LightSetMaxSdfgiCascade(Rid light, uint cascade)
```

引擎名：`light_set_max_sdfgi_cascade`

Sets the maximum SDFGI cascade in which the 3D light's indirect lighting is rendered. Higher values allow the light to be rendered in SDFGI further away from the camera.

### LightSetNegative

```csharp
public static void LightSetNegative(Rid light, bool enable)
```

引擎名：`light_set_negative`

If `true`, the 3D light will subtract light instead of adding light. Equivalent to [`Light3D.LightNegative`](Light3D.md).

### LightSetParam

```csharp
public static void LightSetParam(Rid light, RenderingServer.LightParam param, float value)
```

引擎名：`light_set_param`

Sets the specified 3D light parameter. Equivalent to [`Light3D.SetParam`](Light3D.md).

### LightSetProjector

```csharp
public static void LightSetProjector(Rid light, Rid texture)
```

引擎名：`light_set_projector`

Sets the projector texture to use for the specified 3D light. Equivalent to [`Light3D.LightProjector`](Light3D.md).

### LightSetReverseCullFaceMode

```csharp
public static void LightSetReverseCullFaceMode(Rid light, bool enabled)
```

引擎名：`light_set_reverse_cull_face_mode`

If `true`, reverses the backface culling of the mesh. This can be useful when you have a flat mesh that has a light behind it. If you need to cast a shadow on both sides of the mesh, set the mesh to use double-sided shadows with `InstanceGeometrySetCastShadowsSetting()`. Equivalent to [`Light3D.ShadowReverseCullFace`](Light3D.md).

### LightSetShadow

```csharp
public static void LightSetShadow(Rid light, bool enabled)
```

引擎名：`light_set_shadow`

If `true`, light will cast shadows. Equivalent to [`Light3D.ShadowEnabled`](Light3D.md).

### LightSetShadowCasterMask

```csharp
public static void LightSetShadowCasterMask(Rid light, uint mask)
```

引擎名：`light_set_shadow_caster_mask`

Sets the shadow caster mask for this 3D light. Shadows will only be cast using objects in the selected layers. Equivalent to [`Light3D.ShadowCasterMask`](Light3D.md).

### LightmapCreate

```csharp
public static Rid LightmapCreate()
```

引擎名：`lightmap_create`

Creates a new lightmap global illumination instance and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `lightmap_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`LightmapGI`](LightmapGI.md).

### LightmapGetProbeCaptureBspTree

```csharp
public static int[] LightmapGetProbeCaptureBspTree(Rid lightmap)
```

引擎名：`lightmap_get_probe_capture_bsp_tree`

Returns the BSP tree data used for accelerating probe lookups. The BSP data is structured as a series of six signed 32-bit values per BSP node in this order: `float plane_x`, `float plane_y`, `float plane_z`, `float plane_distance`, `int32_t over`, `int32_t under`. An empty leaf is denoted by the value `-2147483648` (the minimum 32-bit signed integer). See also `LightmapSetProbeCaptureData()`.

### LightmapGetProbeCapturePoints

```csharp
public static Vector3[] LightmapGetProbeCapturePoints(Rid lightmap)
```

引擎名：`lightmap_get_probe_capture_points`

Returns the *local space* positions of each lightmap probe capture point. Keep in mind the lightmap instance may have a non-zero transform, which will affect the position of the probe capture points. See also `LightmapSetProbeCaptureData()`.

### LightmapGetProbeCaptureSh

```csharp
public static Color[] LightmapGetProbeCaptureSh(Rid lightmap)
```

引擎名：`lightmap_get_probe_capture_sh`

Returns the L0, L1, and L2 [spherical harmonics](https://en.wikipedia.org/wiki/Spherical_harmonics) data for each lightmap probe capture point. This is specified as 9 [`Color`](Color.md) values per probe, which means the size of the returned data is always 9 times the number of probe points. See also `LightmapSetProbeCaptureData()`.

### LightmapGetProbeCaptureTetrahedra

```csharp
public static int[] LightmapGetProbeCaptureTetrahedra(Rid lightmap)
```

引擎名：`lightmap_get_probe_capture_tetrahedra`

Returns the tetrahedralization data used for interpolating between lightmap probe capture points. Each tetrahedron is specified as a series of 4 numbers, each being an index into the probe capture points array returned by `LightmapGetProbeCapturePoints()`. See also `LightmapSetProbeCaptureData()`.

### LightmapSetBakedExposureNormalization

```csharp
public static void LightmapSetBakedExposureNormalization(Rid lightmap, float bakedExposure)
```

引擎名：`lightmap_set_baked_exposure_normalization`

Used to inform the renderer what exposure normalization value was used while baking the lightmap. This value will be used and modulated at run time to ensure that the lightmap maintains a consistent level of exposure even if the scene-wide exposure normalization is changed at run time. For more information see `CameraAttributesSetExposure()`.

### LightmapSetProbeBounds

```csharp
public static void LightmapSetProbeBounds(Rid lightmap, Aabb bounds)
```

引擎名：`lightmap_set_probe_bounds`

Sets the bounds that this lightmap instance should visually affect, both in terms of static lightmap baking and probe-based global illumination.

### LightmapSetProbeCaptureData

```csharp
public static void LightmapSetProbeCaptureData(Rid lightmap, ReadOnlySpan<Vector3> points, ReadOnlySpan<Color> pointSh, ReadOnlySpan<int> tetrahedra, ReadOnlySpan<int> bspTree)
```

引擎名：`lightmap_set_probe_capture_data`

Sets the probe capture data for the given lightmap instance. See `LightmapGetProbeCapturePoints()`, `LightmapGetProbeCaptureSh()`, `LightmapGetProbeCaptureTetrahedra()`, and `LightmapGetProbeCaptureBspTree()` for the expected data formats.

### LightmapSetProbeCaptureData

```csharp
public static void LightmapSetProbeCaptureData(Rid lightmap, Vector3[] points, Color[] pointSh, int[] tetrahedra, int[] bspTree)
```

引擎名：`lightmap_set_probe_capture_data`

Sets the probe capture data for the given lightmap instance. See `LightmapGetProbeCapturePoints()`, `LightmapGetProbeCaptureSh()`, `LightmapGetProbeCaptureTetrahedra()`, and `LightmapGetProbeCaptureBspTree()` for the expected data formats.

### LightmapSetProbeCaptureUpdateSpeed

```csharp
public static void LightmapSetProbeCaptureUpdateSpeed(float speed)
```

引擎名：`lightmap_set_probe_capture_update_speed`

The framerate-independent update speed when representing dynamic object lighting from [`LightmapProbe`](LightmapProbe.md)s. Higher values make dynamic object lighting update faster. Higher values can prevent fast-moving objects from having "outdated" indirect lighting displayed on them, at the cost of possible flickering when an object moves from a bright area to a shaded area. See also `ProjectSettings.rendering/lightmapping/probe_capture/update_speed`.

### LightmapSetProbeInterior

```csharp
public static void LightmapSetProbeInterior(Rid lightmap, bool interior)
```

引擎名：`lightmap_set_probe_interior`

Sets whether the lightmap instance should be considered as interior (when `interior` is `true`). If the lightmap is marked as interior, environment lighting is ignored when baking lightmaps.

### LightmapSetTextures

```csharp
public static void LightmapSetTextures(Rid lightmap, Rid light, bool usesSh)
```

引擎名：`lightmap_set_textures`

Set the textures on the given `lightmap` GI instance to the texture array pointed to by the `light` RID. If the lightmap texture was baked with [`LightmapGI.Directional`](LightmapGI.md) set to `true`, then `usesSh` must also be `true`.

### LightmapsSetBicubicFilter

```csharp
public static void LightmapsSetBicubicFilter(bool enable)
```

引擎名：`lightmaps_set_bicubic_filter`

Toggles whether a bicubic filter should be used when lightmaps are sampled. This smoothens their appearance at a performance cost.

### MakeSphereMesh

```csharp
public static Rid MakeSphereMesh(int latitudes, int longitudes, float radius)
```

引擎名：`make_sphere_mesh`

Returns a mesh of a sphere with the given number of horizontal subdivisions, vertical subdivisions and radius. See also `GetTestCube()`.

### MaterialCreate

```csharp
public static Rid MaterialCreate()
```

引擎名：`material_create`

Creates an empty material and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `material_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Material`](Material.md).

### MaterialGetParam

```csharp
public static Variant MaterialGetParam(Rid material, StringName parameter)
```

引擎名：`material_get_param`

Returns the value of a certain material's parameter.

### MaterialSetNextPass

```csharp
public static void MaterialSetNextPass(Rid material, Rid nextMaterial)
```

引擎名：`material_set_next_pass`

Sets an object's next material.

### MaterialSetParam

```csharp
public static void MaterialSetParam(Rid material, StringName parameter, Variant value)
```

引擎名：`material_set_param`

Sets a material's parameter.

### MaterialSetRenderPriority

```csharp
public static void MaterialSetRenderPriority(Rid material, int priority)
```

引擎名：`material_set_render_priority`

Sets a material's render priority.

### MaterialSetShader

```csharp
public static void MaterialSetShader(Rid shaderMaterial, Rid shader)
```

引擎名：`material_set_shader`

Sets a shader material's shader.

### MaterialSetUseDebanding

```csharp
public static void MaterialSetUseDebanding(bool enable)
```

引擎名：`material_set_use_debanding`

When using the Mobile renderer, `MaterialSetUseDebanding()` can be used to enable or disable the debanding feature of 3D materials ([`BaseMaterial3D`](BaseMaterial3D.md) and [`ShaderMaterial`](ShaderMaterial.md)).

`MaterialSetUseDebanding()` has no effect when using the Compatibility or Forward+ renderer. In Forward+, [`Viewport`](Viewport.md) debanding can be used instead.

See also `ProjectSettings.rendering/anti_aliasing/quality/use_debanding` and `ViewportSetUseDebanding()`.

### MeshAddSurface

```csharp
public static void MeshAddSurface(Rid mesh, Dictionary surface)
```

引擎名：`mesh_add_surface`

Creates a new surface on the given `mesh`. Equivalent to `MeshAddSurfaceFromArrays()`, but takes a single [`Collections.Dictionary`](Collections.Dictionary.md) argument instead of separate arguments. The dictionary must follow this structure:

```csharp
{
# Required:
"primitive": RenderingServer.PrimitiveType,
"format": RenderingServer.ArrayFormat,
"vertex_data": PackedByteArray,
"vertex_count": int,
"aabb": AABB,

# Optional:
"attribute_data": PackedByteArray,
"skin_data": PackedByteArray,
"index_data": PackedByteArray,
"index_count": int, # Required if `index_data` is specified.
"uv_scale": Vector4,
"lods": [
# Both values are required for each LOD level.
{
"edge_length": float,
"index_data": PackedByteArray,
},
],
"bone_aabbs": Array[AABB],
"blend_shape_data": PackedByteArray,
"material": Material,
}

```

See also `MeshGetSurface()`, which returns data in the same structure defined above.

### MeshAddSurfaceFromArrays

```csharp
public static void MeshAddSurfaceFromArrays(Rid mesh, RenderingServer.PrimitiveType primitive, Array arrays, Array blendShapes = null, Dictionary lods = null, RenderingServer.ArrayFormat compressFormat = RenderingServer.ArrayFormat.FlagFormatVersion1)
```

引擎名：`mesh_add_surface_from_arrays`

Creates a new surface on the given `mesh`. `MeshGetSurfaceCount()` will become the surface index for this new surface.

Surfaces are created to be rendered using a `primitive`, which may be any of the values defined in [`Mesh.PrimitiveType`](Mesh.md).

The `arrays` argument is an array of arrays. Each of the [`Mesh.ArrayType.Max`](Mesh.md) elements contains an array with some of the mesh data for this surface as described by the corresponding member of [`Mesh.ArrayType`](Mesh.md) or `null` if it is not used by the surface. For example, `arrays[0]` is the array of vertices. That first vertex sub-array is always required; the others are optional. Adding an index array puts this surface into "index mode" where the vertex and other arrays become the sources of data and the index array defines the vertex order. All sub-arrays must have the same length as the vertex array (or be an exact multiple of the vertex array's length, when multiple elements of a sub-array correspond to a single vertex) or be empty, except for [`Mesh.ArrayType.Index`](Mesh.md) if it is used.

The `blendShapes` argument is an array of vertex data for each blend shape. Each element is an array of the same structure as `arrays`, but [`Mesh.ArrayType.Vertex`](Mesh.md), [`Mesh.ArrayType.Normal`](Mesh.md), and [`Mesh.ArrayType.Tangent`](Mesh.md) are set if and only if they are set in `arrays` and all other entries are `null`.

The `lods` argument is a dictionary with `System.Single` keys and `System.Int32`[] values. Each entry in the dictionary represents an LOD level of the surface, where the value is the [`Mesh.ArrayType.Index`](Mesh.md) array to use for the LOD level and the key is roughly proportional to the distance at which the LOD stats being used. I.e., increasing the key of an LOD also increases the distance that the objects has to be from the camera before the LOD is used.

The `compressFormat` argument is the bitwise OR of, as required: One value of [`RenderingServer.ArrayFormat`](RenderingServer.md) left shifted by `ARRAY_FORMAT_CUSTOMn_SHIFT` for each custom channel in use, [`RenderingServer.ArrayFormat.FlagUseDynamicUpdate`](RenderingServer.md), [`RenderingServer.ArrayFormat.FlagUse8BoneWeights`](RenderingServer.md), or [`RenderingServer.ArrayFormat.FlagUsesEmptyVertexArray`](RenderingServer.md).

See [`ArrayMesh.AddSurfaceFromArrays`](ArrayMesh.md) and [`ImporterMesh.AddSurface`](ImporterMesh.md) for higher-level equivalents of this method.

**Note:** When using indices, it is recommended to only use points, lines, or triangles.

### MeshClear

```csharp
public static void MeshClear(Rid mesh)
```

引擎名：`mesh_clear`

Removes all surfaces from a mesh.

### MeshCreate

```csharp
public static Rid MeshCreate()
```

引擎名：`mesh_create`

Creates a new mesh and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `mesh_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this mesh to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent resource is [`Mesh`](Mesh.md).

### MeshCreateFromSurfaces

```csharp
public static Rid MeshCreateFromSurfaces(Array<Dictionary> surfaces, int blendShapeCount = 0)
```

引擎名：`mesh_create_from_surfaces`

Creates a new mesh with predefined surfaces for it and adds the mesh to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `mesh_*` RenderingServer functions. This method is more efficient for creating meshes with multiple surfaces compared to creating an empty mesh with `MeshCreate()` and adding surfaces one by one with `MeshAddSurface()`.

Each element in the `surfaces` array must follow the same structure as described in `MeshAddSurface()`. The `blendShapeCount` parameter must match the blend shape data defined in all surfaces.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this mesh to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent resource is [`Mesh`](Mesh.md).

### MeshGetBlendShapeCount

```csharp
public static int MeshGetBlendShapeCount(Rid mesh)
```

引擎名：`mesh_get_blend_shape_count`

Returns a mesh's blend shape count.

### MeshGetBlendShapeMode

```csharp
public static RenderingServer.BlendShapeMode MeshGetBlendShapeMode(Rid mesh)
```

引擎名：`mesh_get_blend_shape_mode`

Returns a mesh's blend shape mode.

### MeshGetCustomAabb

```csharp
public static Aabb MeshGetCustomAabb(Rid mesh)
```

引擎名：`mesh_get_custom_aabb`

Returns a mesh's custom aabb.

### MeshGetSurface

```csharp
public static Dictionary MeshGetSurface(Rid mesh, int surface)
```

引擎名：`mesh_get_surface`

Returns a mesh's surface as a dictionary following the same structure as described in `MeshAddSurface()`.

### MeshGetSurfaceCount

```csharp
public static int MeshGetSurfaceCount(Rid mesh)
```

引擎名：`mesh_get_surface_count`

Returns a mesh's number of surfaces.

### MeshSetBlendShapeMode

```csharp
public static void MeshSetBlendShapeMode(Rid mesh, RenderingServer.BlendShapeMode mode)
```

引擎名：`mesh_set_blend_shape_mode`

Sets a mesh's blend shape mode.

### MeshSetCustomAabb

```csharp
public static void MeshSetCustomAabb(Rid mesh, Aabb aabb)
```

引擎名：`mesh_set_custom_aabb`

Sets a mesh's custom aabb.

### MeshSetShadowMesh

```csharp
public static void MeshSetShadowMesh(Rid mesh, Rid shadowMesh)
```

引擎名：`mesh_set_shadow_mesh`

Sets an optional second mesh which can be used for rendering shadows and the depth prepass. Can be used to increase performance by supplying a mesh with fused vertices and only vertex position data (without normals, UVs, colors, etc.).

**Note:** This mesh must have exactly the same vertex positions as the source mesh (including the source mesh's LODs, if present). If vertex positions differ, then the mesh will not draw correctly.

### MeshSurfaceGetArrays

```csharp
public static Array MeshSurfaceGetArrays(Rid mesh, int surface)
```

引擎名：`mesh_surface_get_arrays`

Returns a mesh's surface's buffer arrays.

### MeshSurfaceGetBlendShapeArrays

```csharp
public static Array<Array> MeshSurfaceGetBlendShapeArrays(Rid mesh, int surface)
```

引擎名：`mesh_surface_get_blend_shape_arrays`

Returns a mesh's surface's arrays for blend shapes.

### MeshSurfaceGetFormatAttributeStride

```csharp
public static uint MeshSurfaceGetFormatAttributeStride(RenderingServer.ArrayFormat format, int vertexCount)
```

引擎名：`mesh_surface_get_format_attribute_stride`

Returns the stride of the attribute buffer for a mesh with given `format`.

### MeshSurfaceGetFormatIndexStride

```csharp
public static uint MeshSurfaceGetFormatIndexStride(RenderingServer.ArrayFormat format, int vertexCount)
```

引擎名：`mesh_surface_get_format_index_stride`

Returns the stride of the index buffer for a mesh with the given `format`.

### MeshSurfaceGetFormatNormalTangentStride

```csharp
public static uint MeshSurfaceGetFormatNormalTangentStride(RenderingServer.ArrayFormat format, int vertexCount)
```

引擎名：`mesh_surface_get_format_normal_tangent_stride`

Returns the stride of the combined normals and tangents for a mesh with given `format`. Note importantly that, while normals and tangents are in the vertex buffer with vertices, they are only interleaved with each other and so have a different stride than vertex positions.

### MeshSurfaceGetFormatOffset

```csharp
public static uint MeshSurfaceGetFormatOffset(RenderingServer.ArrayFormat format, int vertexCount, int arrayIndex)
```

引擎名：`mesh_surface_get_format_offset`

Returns the offset of a given attribute by `arrayIndex` in the start of its respective buffer.

### MeshSurfaceGetFormatSkinStride

```csharp
public static uint MeshSurfaceGetFormatSkinStride(RenderingServer.ArrayFormat format, int vertexCount)
```

引擎名：`mesh_surface_get_format_skin_stride`

Returns the stride of the skin buffer for a mesh with given `format`.

### MeshSurfaceGetFormatVertexStride

```csharp
public static uint MeshSurfaceGetFormatVertexStride(RenderingServer.ArrayFormat format, int vertexCount)
```

引擎名：`mesh_surface_get_format_vertex_stride`

Returns the stride of the vertex positions for a mesh with given `format`. Note importantly that vertex positions are stored consecutively and are not interleaved with the other attributes in the vertex buffer (normals and tangents).

### MeshSurfaceGetMaterial

```csharp
public static Rid MeshSurfaceGetMaterial(Rid mesh, int surface)
```

引擎名：`mesh_surface_get_material`

Returns a mesh's surface's material.

### MeshSurfaceRemove

```csharp
public static void MeshSurfaceRemove(Rid mesh, int surface)
```

引擎名：`mesh_surface_remove`

Removes the surface at the given index from the Mesh, shifting surfaces with higher index down by one.

### MeshSurfaceSetMaterial

```csharp
public static void MeshSurfaceSetMaterial(Rid mesh, int surface, Rid material)
```

引擎名：`mesh_surface_set_material`

Sets a mesh's surface's material.

### MeshSurfaceUpdateAttributeRegion

```csharp
public static void MeshSurfaceUpdateAttributeRegion(Rid mesh, int surface, int offset, ReadOnlySpan<byte> data)
```

引擎名：`mesh_surface_update_attribute_region`

Updates the attribute buffer of the mesh surface with the given `data`. The expected data per attribute is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatAttributeStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each attribute.

A [`Vector3`](Vector3.md)[] of attribute locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateAttributeRegion

```csharp
public static void MeshSurfaceUpdateAttributeRegion(Rid mesh, int surface, int offset, byte[] data)
```

引擎名：`mesh_surface_update_attribute_region`

Updates the attribute buffer of the mesh surface with the given `data`. The expected data per attribute is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatAttributeStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each attribute.

A [`Vector3`](Vector3.md)[] of attribute locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateIndexRegion

```csharp
public static void MeshSurfaceUpdateIndexRegion(Rid mesh, int surface, int offset, ReadOnlySpan<byte> data)
```

引擎名：`mesh_surface_update_index_region`

Updates the index buffer of the mesh surface with the given `data`. The expected data are 16 or 32-bit unsigned integers, which can be determined with `MeshSurfaceGetFormatIndexStride()`.

### MeshSurfaceUpdateIndexRegion

```csharp
public static void MeshSurfaceUpdateIndexRegion(Rid mesh, int surface, int offset, byte[] data)
```

引擎名：`mesh_surface_update_index_region`

Updates the index buffer of the mesh surface with the given `data`. The expected data are 16 or 32-bit unsigned integers, which can be determined with `MeshSurfaceGetFormatIndexStride()`.

### MeshSurfaceUpdateSkinRegion

```csharp
public static void MeshSurfaceUpdateSkinRegion(Rid mesh, int surface, int offset, ReadOnlySpan<byte> data)
```

引擎名：`mesh_surface_update_skin_region`

Updates the skin buffer of the mesh surface with the given `data`. The expected data per skin is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatSkinStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each skin.

A [`Vector3`](Vector3.md)[] of skin locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateSkinRegion

```csharp
public static void MeshSurfaceUpdateSkinRegion(Rid mesh, int surface, int offset, byte[] data)
```

引擎名：`mesh_surface_update_skin_region`

Updates the skin buffer of the mesh surface with the given `data`. The expected data per skin is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatSkinStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each skin.

A [`Vector3`](Vector3.md)[] of skin locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateVertexRegion

```csharp
public static void MeshSurfaceUpdateVertexRegion(Rid mesh, int surface, int offset, ReadOnlySpan<byte> data)
```

引擎名：`mesh_surface_update_vertex_region`

Updates the vertex buffer of the mesh surface with the given `data`. The expected data per vertex is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatVertexStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each vertex.

A [`Vector3`](Vector3.md)[] of vertex locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateVertexRegion

```csharp
public static void MeshSurfaceUpdateVertexRegion(Rid mesh, int surface, int offset, byte[] data)
```

引擎名：`mesh_surface_update_vertex_region`

Updates the vertex buffer of the mesh surface with the given `data`. The expected data per vertex is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatVertexStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each vertex.

A [`Vector3`](Vector3.md)[] of vertex locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MultimeshAllocateData

```csharp
public static void MultimeshAllocateData(Rid multimesh, int instances, RenderingServer.MultimeshTransformFormat transformFormat, bool colorFormat = false, bool customDataFormat = false, bool useIndirect = false)
```

引擎名：`multimesh_allocate_data`

Sets up the multimesh using the specified data. The number of instances is set by `instances`. The format of the instance transforms is set by `transformFormat`, which should be set according to whether the multimesh is meant to be rendered in 2D or 3D. If `colorFormat` is `true`, each instance will have a color associated with it. If `customDataFormat` is `true`, each instance will have a custom data vector associated with it. If `useIndirect` is `true`, an indirect command buffer will be created for this multimesh, allowing the instance count to be modified directly on the GPU. See also `MultimeshGetCommandBufferRdRid()`.

### MultimeshAllocateData

```csharp
public static void MultimeshAllocateData(Rid multimesh, int instances, RenderingServer.MultimeshTransformFormat transformFormat, bool colorFormat, bool customDataFormat)
```

引擎名：`multimesh_allocate_data`

Sets up the multimesh using the specified data. The number of instances is set by `instances`. The format of the instance transforms is set by `transformFormat`, which should be set according to whether the multimesh is meant to be rendered in 2D or 3D. If `colorFormat` is `true`, each instance will have a color associated with it. If `customDataFormat` is `true`, each instance will have a custom data vector associated with it. If `useIndirect` is `true`, an indirect command buffer will be created for this multimesh, allowing the instance count to be modified directly on the GPU. See also `MultimeshGetCommandBufferRdRid()`.

### MultimeshCreate

```csharp
public static Rid MultimeshCreate()
```

引擎名：`multimesh_create`

Creates a new multimesh on the RenderingServer and returns an [`Rid`](Rid.md) handle. This RID will be used in all `multimesh_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this multimesh to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent resource is [`MultiMesh`](MultiMesh.md).

### MultimeshGetAabb

```csharp
public static Aabb MultimeshGetAabb(Rid multimesh)
```

引擎名：`multimesh_get_aabb`

Calculates and returns the axis-aligned bounding box that encloses all instances within the multimesh.

### MultimeshGetBuffer

```csharp
public static float[] MultimeshGetBuffer(Rid multimesh)
```

引擎名：`multimesh_get_buffer`

Returns the MultiMesh data (such as instance transforms, colors, etc.). See `MultimeshSetBuffer()` for details on the returned data.

**Note:** If the buffer is in the engine's internal cache, it will have to be fetched from GPU memory and possibly decompressed. This means `MultimeshGetBuffer()` is potentially a slow operation and should be avoided whenever possible.

### MultimeshGetBufferRdRid

```csharp
public static Rid MultimeshGetBufferRdRid(Rid multimesh)
```

引擎名：`multimesh_get_buffer_rd_rid`

Returns the [`RenderingDevice`](RenderingDevice.md) [`Rid`](Rid.md) handle of the [`MultiMesh`](MultiMesh.md), which can be used as any other buffer on the Rendering Device.

### MultimeshGetCommandBufferRdRid

```csharp
public static Rid MultimeshGetCommandBufferRdRid(Rid multimesh)
```

引擎名：`multimesh_get_command_buffer_rd_rid`

Returns the [`RenderingDevice`](RenderingDevice.md) [`Rid`](Rid.md) handle of the [`MultiMesh`](MultiMesh.md) command buffer. This [`Rid`](Rid.md) is only valid if `use_indirect` is set to `true` when allocating data through `MultimeshAllocateData()`. It can be used to directly modify the instance count via buffer.

The data structure is dependent on both how many surfaces the mesh contains and whether it is indexed or not, the buffer has 5 integers in it, with the last unused if the mesh is not indexed.

Each of the values in the buffer correspond to these options:

```csharp
Indexed:
0 - indexCount;
1 - instanceCount;
2 - firstIndex;
3 - vertexOffset;
4 - firstInstance;
Non-indexed:
0 - vertexCount;
1 - instanceCount;
2 - firstVertex;
3 - firstInstance;
4 - unused;

```

### MultimeshGetCustomAabb

```csharp
public static Aabb MultimeshGetCustomAabb(Rid multimesh)
```

引擎名：`multimesh_get_custom_aabb`

Returns the custom AABB defined for this MultiMesh resource.

### MultimeshGetInstanceCount

```csharp
public static int MultimeshGetInstanceCount(Rid multimesh)
```

引擎名：`multimesh_get_instance_count`

Returns the number of instances allocated for this multimesh.

### MultimeshGetMesh

```csharp
public static Rid MultimeshGetMesh(Rid multimesh)
```

引擎名：`multimesh_get_mesh`

Returns the RID of the mesh that will be used in drawing this multimesh.

### MultimeshGetVisibleInstances

```csharp
public static int MultimeshGetVisibleInstances(Rid multimesh)
```

引擎名：`multimesh_get_visible_instances`

Returns the number of visible instances for this multimesh.

### MultimeshInstanceGetColor

```csharp
public static Color MultimeshInstanceGetColor(Rid multimesh, int index)
```

引擎名：`multimesh_instance_get_color`

Returns the color by which the specified instance will be modulated.

### MultimeshInstanceGetCustomData

```csharp
public static Color MultimeshInstanceGetCustomData(Rid multimesh, int index)
```

引擎名：`multimesh_instance_get_custom_data`

Returns the custom data associated with the specified instance.

### MultimeshInstanceGetTransform

```csharp
public static Transform3D MultimeshInstanceGetTransform(Rid multimesh, int index)
```

引擎名：`multimesh_instance_get_transform`

Returns the [`Transform3D`](Transform3D.md) of the specified instance.

### MultimeshInstanceGetTransform2D

```csharp
public static Transform2D MultimeshInstanceGetTransform2D(Rid multimesh, int index)
```

Returns the [`Transform2D`](Transform2D.md) of the specified instance. For use when the multimesh is set to use 2D transforms.

### MultimeshInstanceResetPhysicsInterpolation

```csharp
public static void MultimeshInstanceResetPhysicsInterpolation(Rid multimesh, int index)
```

引擎名：`multimesh_instance_reset_physics_interpolation`

Prevents physics interpolation for the specified instance during the current physics tick.

This is useful when moving an instance to a new location, to give an instantaneous change rather than interpolation from the previous location.

### MultimeshInstanceSetColor

```csharp
public static void MultimeshInstanceSetColor(Rid multimesh, int index, Color color)
```

引擎名：`multimesh_instance_set_color`

Sets the color by which this instance will be modulated. Equivalent to [`MultiMesh.SetInstanceColor`](MultiMesh.md).

### MultimeshInstanceSetCustomData

```csharp
public static void MultimeshInstanceSetCustomData(Rid multimesh, int index, Color customData)
```

引擎名：`multimesh_instance_set_custom_data`

Sets the custom data for this instance. Custom data is passed as a [`Color`](Color.md), but is interpreted as a `vec4` in the shader. Equivalent to [`MultiMesh.SetInstanceCustomData`](MultiMesh.md).

### MultimeshInstanceSetTransform

```csharp
public static void MultimeshInstanceSetTransform(Rid multimesh, int index, Transform3D transform)
```

引擎名：`multimesh_instance_set_transform`

Sets the [`Transform3D`](Transform3D.md) for this instance. Equivalent to [`MultiMesh.SetInstanceTransform`](MultiMesh.md).

### MultimeshInstanceSetTransform2D

```csharp
public static void MultimeshInstanceSetTransform2D(Rid multimesh, int index, Transform2D transform)
```

Sets the [`Transform2D`](Transform2D.md) for this instance. For use when multimesh is used in 2D. Equivalent to [`MultiMesh.SetInstanceTransform2D`](MultiMesh.md).

### MultimeshInstancesResetPhysicsInterpolation

```csharp
public static void MultimeshInstancesResetPhysicsInterpolation(Rid multimesh)
```

引擎名：`multimesh_instances_reset_physics_interpolation`

Prevents physics interpolation for all instances during the current physics tick.

This is useful when moving all instances to new locations, to give instantaneous changes rather than interpolation from the previous locations.

### MultimeshSetBuffer

```csharp
public static void MultimeshSetBuffer(Rid multimesh, ReadOnlySpan<float> buffer)
```

引擎名：`multimesh_set_buffer`

Set the entire data to use for drawing the `multimesh` at once to `buffer` (such as instance transforms and colors). `buffer`'s size must match the number of instances multiplied by the per-instance data size (which depends on the enabled MultiMesh fields). Otherwise, an error message is printed and nothing is rendered. See also `MultimeshGetBuffer()`.

The per-instance data size and expected data order is:

```csharp
2D:
- Position: 8 floats (8 floats for Transform2D)
- Position + Vertex color: 12 floats (8 floats for Transform2D, 4 floats for Color)
- Position + Custom data: 12 floats (8 floats for Transform2D, 4 floats of custom data)
- Position + Vertex color + Custom data: 16 floats (8 floats for Transform2D, 4 floats for Color, 4 floats of custom data)
3D:
- Position: 12 floats (12 floats for Transform3D)
- Position + Vertex color: 16 floats (12 floats for Transform3D, 4 floats for Color)
- Position + Custom data: 16 floats (12 floats for Transform3D, 4 floats of custom data)
- Position + Vertex color + Custom data: 20 floats (12 floats for Transform3D, 4 floats for Color, 4 floats of custom data)

```

Instance transforms are in row-major order. Specifically:

- For [`Transform2D`](Transform2D.md) the float-order is: `(x.x, y.x, padding_float, origin.x, x.y, y.y, padding_float, origin.y)`.

- For [`Transform3D`](Transform3D.md) the float-order is: `(basis.x.x, basis.y.x, basis.z.x, origin.x, basis.x.y, basis.y.y, basis.z.y, origin.y, basis.x.z, basis.y.z, basis.z.z, origin.z)`.

### MultimeshSetBuffer

```csharp
public static void MultimeshSetBuffer(Rid multimesh, float[] buffer)
```

引擎名：`multimesh_set_buffer`

Set the entire data to use for drawing the `multimesh` at once to `buffer` (such as instance transforms and colors). `buffer`'s size must match the number of instances multiplied by the per-instance data size (which depends on the enabled MultiMesh fields). Otherwise, an error message is printed and nothing is rendered. See also `MultimeshGetBuffer()`.

The per-instance data size and expected data order is:

```csharp
2D:
- Position: 8 floats (8 floats for Transform2D)
- Position + Vertex color: 12 floats (8 floats for Transform2D, 4 floats for Color)
- Position + Custom data: 12 floats (8 floats for Transform2D, 4 floats of custom data)
- Position + Vertex color + Custom data: 16 floats (8 floats for Transform2D, 4 floats for Color, 4 floats of custom data)
3D:
- Position: 12 floats (12 floats for Transform3D)
- Position + Vertex color: 16 floats (12 floats for Transform3D, 4 floats for Color)
- Position + Custom data: 16 floats (12 floats for Transform3D, 4 floats of custom data)
- Position + Vertex color + Custom data: 20 floats (12 floats for Transform3D, 4 floats for Color, 4 floats of custom data)

```

Instance transforms are in row-major order. Specifically:

- For [`Transform2D`](Transform2D.md) the float-order is: `(x.x, y.x, padding_float, origin.x, x.y, y.y, padding_float, origin.y)`.

- For [`Transform3D`](Transform3D.md) the float-order is: `(basis.x.x, basis.y.x, basis.z.x, origin.x, basis.x.y, basis.y.y, basis.z.y, origin.y, basis.x.z, basis.y.z, basis.z.z, origin.z)`.

### MultimeshSetBufferInterpolated

```csharp
public static void MultimeshSetBufferInterpolated(Rid multimesh, ReadOnlySpan<float> buffer, ReadOnlySpan<float> bufferPrevious)
```

引擎名：`multimesh_set_buffer_interpolated`

Alternative version of `MultimeshSetBuffer()` for use with physics interpolation.

Takes both an array of current data and an array of data for the previous physics tick.

### MultimeshSetBufferInterpolated

```csharp
public static void MultimeshSetBufferInterpolated(Rid multimesh, float[] buffer, float[] bufferPrevious)
```

引擎名：`multimesh_set_buffer_interpolated`

Alternative version of `MultimeshSetBuffer()` for use with physics interpolation.

Takes both an array of current data and an array of data for the previous physics tick.

### MultimeshSetCustomAabb

```csharp
public static void MultimeshSetCustomAabb(Rid multimesh, Aabb aabb)
```

引擎名：`multimesh_set_custom_aabb`

Sets the custom AABB for this MultiMesh resource.

### MultimeshSetMesh

```csharp
public static void MultimeshSetMesh(Rid multimesh, Rid mesh)
```

引擎名：`multimesh_set_mesh`

Sets the mesh to be drawn by the multimesh. Equivalent to [`MultiMesh.Mesh`](MultiMesh.md).

### MultimeshSetPhysicsInterpolated

```csharp
public static void MultimeshSetPhysicsInterpolated(Rid multimesh, bool interpolated)
```

引擎名：`multimesh_set_physics_interpolated`

Turns on and off physics interpolation for this MultiMesh resource.

### MultimeshSetPhysicsInterpolationQuality

```csharp
public static void MultimeshSetPhysicsInterpolationQuality(Rid multimesh, RenderingServer.MultimeshPhysicsInterpolationQuality quality)
```

引擎名：`multimesh_set_physics_interpolation_quality`

Sets the physics interpolation quality for the [`MultiMesh`](MultiMesh.md).

A value of [`RenderingServer.MultimeshPhysicsInterpolationQuality.Fast`](RenderingServer.md) gives fast but low quality interpolation, a value of [`RenderingServer.MultimeshPhysicsInterpolationQuality.High`](RenderingServer.md) gives slower but higher quality interpolation.

### MultimeshSetVisibleInstances

```csharp
public static void MultimeshSetVisibleInstances(Rid multimesh, int visible)
```

引擎名：`multimesh_set_visible_instances`

Sets the number of instances visible at a given time. If -1, all instances that have been allocated are drawn. Equivalent to [`MultiMesh.VisibleInstanceCount`](MultiMesh.md).

### OccluderCreate

```csharp
public static Rid OccluderCreate()
```

引擎名：`occluder_create`

Creates an occluder instance and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `occluder_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Occluder3D`](Occluder3D.md) (not to be confused with the [`OccluderInstance3D`](OccluderInstance3D.md) node).

### OccluderSetMesh

```csharp
public static void OccluderSetMesh(Rid occluder, ReadOnlySpan<Vector3> vertices, ReadOnlySpan<int> indices)
```

引擎名：`occluder_set_mesh`

Sets the mesh data for the given occluder RID, which controls the shape of the occlusion culling that will be performed.

### OccluderSetMesh

```csharp
public static void OccluderSetMesh(Rid occluder, Vector3[] vertices, int[] indices)
```

引擎名：`occluder_set_mesh`

Sets the mesh data for the given occluder RID, which controls the shape of the occlusion culling that will be performed.

### OmniLightCreate

```csharp
public static Rid OmniLightCreate()
```

引擎名：`omni_light_create`

Creates a new omni light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this omni light to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`OmniLight3D`](OmniLight3D.md).

### ParticlesCollisionCreate

```csharp
public static Rid ParticlesCollisionCreate()
```

引擎名：`particles_collision_create`

Creates a new 3D GPU particle collision or attractor and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `particles_collision_*` RenderingServer functions.

**Note:** The equivalent nodes are [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) and [`GpuParticlesAttractor3D`](GpuParticlesAttractor3D.md).

### ParticlesCollisionHeightFieldUpdate

```csharp
public static void ParticlesCollisionHeightFieldUpdate(Rid particlesCollision)
```

引擎名：`particles_collision_height_field_update`

Requests an update for the 3D GPU particle collision heightfield. This may be automatically called by the 3D GPU particle collision heightfield depending on its [`GpuParticlesCollisionHeightField3D.UpdateMode`](GpuParticlesCollisionHeightField3D.md).

### ParticlesCollisionSetAttractorAttenuation

```csharp
public static void ParticlesCollisionSetAttractorAttenuation(Rid particlesCollision, float curve)
```

引擎名：`particles_collision_set_attractor_attenuation`

Sets the attenuation `curve` for the 3D GPU particles attractor specified by the `particlesCollision` RID. Only used for attractors, not colliders. Equivalent to [`GpuParticlesAttractor3D.Attenuation`](GpuParticlesAttractor3D.md).

### ParticlesCollisionSetAttractorDirectionality

```csharp
public static void ParticlesCollisionSetAttractorDirectionality(Rid particlesCollision, float amount)
```

引擎名：`particles_collision_set_attractor_directionality`

Sets the directionality `amount` for the 3D GPU particles attractor specified by the `particlesCollision` RID. Only used for attractors, not colliders. Equivalent to [`GpuParticlesAttractor3D.Directionality`](GpuParticlesAttractor3D.md).

### ParticlesCollisionSetAttractorStrength

```csharp
public static void ParticlesCollisionSetAttractorStrength(Rid particlesCollision, float strength)
```

引擎名：`particles_collision_set_attractor_strength`

Sets the `strength` for the 3D GPU particles attractor specified by the `particlesCollision` RID. Only used for attractors, not colliders. Equivalent to [`GpuParticlesAttractor3D.Strength`](GpuParticlesAttractor3D.md).

### ParticlesCollisionSetBoxExtents

```csharp
public static void ParticlesCollisionSetBoxExtents(Rid particlesCollision, Vector3 extents)
```

引擎名：`particles_collision_set_box_extents`

Sets the `extents` for the 3D GPU particles collision by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionBox3D.Size`](GpuParticlesCollisionBox3D.md), [`GpuParticlesCollisionSdf3D.Size`](GpuParticlesCollisionSdf3D.md), [`GpuParticlesCollisionHeightField3D.Size`](GpuParticlesCollisionHeightField3D.md), [`GpuParticlesAttractorBox3D.Size`](GpuParticlesAttractorBox3D.md) or [`GpuParticlesAttractorVectorField3D.Size`](GpuParticlesAttractorVectorField3D.md) depending on the `particlesCollision` type.

### ParticlesCollisionSetCollisionType

```csharp
public static void ParticlesCollisionSetCollisionType(Rid particlesCollision, RenderingServer.ParticlesCollisionType type)
```

引擎名：`particles_collision_set_collision_type`

Sets the collision or attractor shape `type` for the 3D GPU particles collision or attractor specified by the `particlesCollision` RID.

### ParticlesCollisionSetCullMask

```csharp
public static void ParticlesCollisionSetCullMask(Rid particlesCollision, uint mask)
```

引擎名：`particles_collision_set_cull_mask`

Sets the cull `mask` for the 3D GPU particles collision or attractor specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollision3D.CullMask`](GpuParticlesCollision3D.md) or [`GpuParticlesAttractor3D.CullMask`](GpuParticlesAttractor3D.md) depending on the `particlesCollision` type.

### ParticlesCollisionSetFieldTexture

```csharp
public static void ParticlesCollisionSetFieldTexture(Rid particlesCollision, Rid texture)
```

引擎名：`particles_collision_set_field_texture`

Sets the signed distance field `texture` for the 3D GPU particles collision specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionSdf3D.Texture`](GpuParticlesCollisionSdf3D.md) or [`GpuParticlesAttractorVectorField3D.Texture`](GpuParticlesAttractorVectorField3D.md) depending on the `particlesCollision` type.

### ParticlesCollisionSetHeightFieldMask

```csharp
public static void ParticlesCollisionSetHeightFieldMask(Rid particlesCollision, uint mask)
```

引擎名：`particles_collision_set_height_field_mask`

Sets the heightfield `mask` for the 3D GPU particles heightfield collision specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionHeightField3D.HeightfieldMask`](GpuParticlesCollisionHeightField3D.md).

### ParticlesCollisionSetHeightFieldResolution

```csharp
public static void ParticlesCollisionSetHeightFieldResolution(Rid particlesCollision, RenderingServer.ParticlesCollisionHeightfieldResolution resolution)
```

引擎名：`particles_collision_set_height_field_resolution`

Sets the heightmap `resolution` for the 3D GPU particles heightfield collision specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionHeightField3D.Resolution`](GpuParticlesCollisionHeightField3D.md).

### ParticlesCollisionSetSphereRadius

```csharp
public static void ParticlesCollisionSetSphereRadius(Rid particlesCollision, float radius)
```

引擎名：`particles_collision_set_sphere_radius`

Sets the `radius` for the 3D GPU particles sphere collision or attractor specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionSphere3D.Radius`](GpuParticlesCollisionSphere3D.md) or [`GpuParticlesAttractorSphere3D.Radius`](GpuParticlesAttractorSphere3D.md) depending on the `particlesCollision` type.

### ParticlesCreate

```csharp
public static Rid ParticlesCreate()
```

引擎名：`particles_create`

Creates a GPU-based particle system and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `particles_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach these particles to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent nodes are [`GpuParticles2D`](GpuParticles2D.md) and [`GpuParticles3D`](GpuParticles3D.md).

**Note:** All `particles_*` methods only apply to GPU-based particles, not CPU-based particles. [`CpuParticles2D`](CpuParticles2D.md) and [`CpuParticles3D`](CpuParticles3D.md) do not have equivalent RenderingServer functions available, as these use [`MultiMeshInstance2D`](MultiMeshInstance2D.md) and [`MultiMeshInstance3D`](MultiMeshInstance3D.md) under the hood (see `multimesh_*` methods).

### ParticlesEmit

```csharp
public static void ParticlesEmit(Rid particles, Transform3D transform, Vector3 velocity, Color color, Color custom, uint emitFlags)
```

引擎名：`particles_emit`

Manually emits particles from the `particles` instance.

### ParticlesGetCurrentAabb

```csharp
public static Aabb ParticlesGetCurrentAabb(Rid particles)
```

引擎名：`particles_get_current_aabb`

Calculates and returns the axis-aligned bounding box that contains all the particles. Equivalent to [`GpuParticles3D.CaptureAabb`](GpuParticles3D.md).

### ParticlesGetEmitting

```csharp
public static bool ParticlesGetEmitting(Rid particles)
```

引擎名：`particles_get_emitting`

Returns `true` if particles are currently set to emitting.

### ParticlesIsInactive

```csharp
public static bool ParticlesIsInactive(Rid particles)
```

引擎名：`particles_is_inactive`

Returns `true` if particles are not emitting and particles are set to inactive.

### ParticlesRequestProcess

```csharp
public static void ParticlesRequestProcess(Rid particles)
```

引擎名：`particles_request_process`

Add particle system to list of particle systems that need to be updated. Update will take place on the next frame, or on the next call to `InstancesCullAabb()`, `InstancesCullConvex()`, or `InstancesCullRay()`.

### ParticlesRequestProcessTime

```csharp
public static void ParticlesRequestProcessTime(Rid particles, float processTime)
```

引擎名：`particles_request_process_time`

Requests the particles to process for extra process time during a single frame.

`processTime` defines the time that the particles will process while emitting is on. `processTimeResidual` defines the time that particles will process with emitting turned off for the simulation. When combined with the particles' speed scale set to `0.0`, this is useful to be able to seek a particle system timeline.

### ParticlesRequestProcessTime

```csharp
public static void ParticlesRequestProcessTime(Rid particles, float processTime, float processTimeResidual = 0f)
```

引擎名：`particles_request_process_time`

Requests the particles to process for extra process time during a single frame.

`processTime` defines the time that the particles will process while emitting is on. `processTimeResidual` defines the time that particles will process with emitting turned off for the simulation. When combined with the particles' speed scale set to `0.0`, this is useful to be able to seek a particle system timeline.

### ParticlesRestart

```csharp
public static void ParticlesRestart(Rid particles)
```

引擎名：`particles_restart`

Reset the particles on the next update. Equivalent to [`GpuParticles3D.Restart`](GpuParticles3D.md).

### ParticlesSetAmount

```csharp
public static void ParticlesSetAmount(Rid particles, int amount)
```

引擎名：`particles_set_amount`

Sets the number of particles to be drawn and allocates the memory for them. Equivalent to [`GpuParticles3D.Amount`](GpuParticles3D.md).

### ParticlesSetAmountRatio

```csharp
public static void ParticlesSetAmountRatio(Rid particles, float ratio)
```

引擎名：`particles_set_amount_ratio`

Sets the amount ratio for particles to be emitted. Equivalent to [`GpuParticles3D.AmountRatio`](GpuParticles3D.md).

### ParticlesSetCollisionBaseSize

```csharp
public static void ParticlesSetCollisionBaseSize(Rid particles, float size)
```

引擎名：`particles_set_collision_base_size`

Sets the base size for particle collision. Equivalent to [`GpuParticles3D.CollisionBaseSize`](GpuParticles3D.md).

### ParticlesSetCustomAabb

```csharp
public static void ParticlesSetCustomAabb(Rid particles, Aabb aabb)
```

引擎名：`particles_set_custom_aabb`

Sets a custom axis-aligned bounding box for the particle system. Equivalent to [`GpuParticles3D.VisibilityAabb`](GpuParticles3D.md).

### ParticlesSetDrawOrder

```csharp
public static void ParticlesSetDrawOrder(Rid particles, RenderingServer.ParticlesDrawOrder order)
```

引擎名：`particles_set_draw_order`

Sets the draw order of the particles. Equivalent to [`GpuParticles3D.DrawOrder`](GpuParticles3D.md).

### ParticlesSetDrawPassMesh

```csharp
public static void ParticlesSetDrawPassMesh(Rid particles, int pass, Rid mesh)
```

引擎名：`particles_set_draw_pass_mesh`

Sets the mesh to be used for the specified draw pass. Equivalent to [`GpuParticles3D.DrawPass1`](GpuParticles3D.md), [`GpuParticles3D.DrawPass2`](GpuParticles3D.md), [`GpuParticles3D.DrawPass3`](GpuParticles3D.md), and [`GpuParticles3D.DrawPass4`](GpuParticles3D.md).

### ParticlesSetDrawPasses

```csharp
public static void ParticlesSetDrawPasses(Rid particles, int count)
```

引擎名：`particles_set_draw_passes`

Sets the number of draw passes to use. Equivalent to [`GpuParticles3D.DrawPasses`](GpuParticles3D.md).

### ParticlesSetEmissionTransform

```csharp
public static void ParticlesSetEmissionTransform(Rid particles, Transform3D transform)
```

引擎名：`particles_set_emission_transform`

Sets the [`Transform3D`](Transform3D.md) that will be used by the particles when they first emit.

### ParticlesSetEmitterVelocity

```csharp
public static void ParticlesSetEmitterVelocity(Rid particles, Vector3 velocity)
```

引擎名：`particles_set_emitter_velocity`

Sets the velocity of a particle node, that will be used by [`ParticleProcessMaterial.InheritVelocityRatio`](ParticleProcessMaterial.md).

### ParticlesSetEmitting

```csharp
public static void ParticlesSetEmitting(Rid particles, bool emitting)
```

引擎名：`particles_set_emitting`

If `true`, particles will emit over time. Setting to `false` does not reset the particles, but only stops their emission. Equivalent to [`GpuParticles3D.Emitting`](GpuParticles3D.md).

### ParticlesSetExplosivenessRatio

```csharp
public static void ParticlesSetExplosivenessRatio(Rid particles, float ratio)
```

引擎名：`particles_set_explosiveness_ratio`

Sets the explosiveness ratio. Equivalent to [`GpuParticles3D.Explosiveness`](GpuParticles3D.md).

### ParticlesSetFixedFps

```csharp
public static void ParticlesSetFixedFps(Rid particles, int fps)
```

引擎名：`particles_set_fixed_fps`

Sets the frame rate that the particle system rendering will be fixed to. Equivalent to [`GpuParticles3D.FixedFps`](GpuParticles3D.md).

### ParticlesSetFractionalDelta

```csharp
public static void ParticlesSetFractionalDelta(Rid particles, bool enable)
```

引擎名：`particles_set_fractional_delta`

If `true`, uses fractional delta which smooths the movement of the particles. Equivalent to [`GpuParticles3D.FractDelta`](GpuParticles3D.md).

### ParticlesSetInterpToEnd

```csharp
public static void ParticlesSetInterpToEnd(Rid particles, float factor)
```

引擎名：`particles_set_interp_to_end`

Sets the value that informs a [`ParticleProcessMaterial`](ParticleProcessMaterial.md) to rush all particles towards the end of their lifetime.

### ParticlesSetInterpolate

```csharp
public static void ParticlesSetInterpolate(Rid particles, bool enable)
```

引擎名：`particles_set_interpolate`

Sets whether particles should use interpolation between fixed steps. Equivalent to [`GpuParticles3D.Interpolate`](GpuParticles3D.md).

### ParticlesSetLifetime

```csharp
public static void ParticlesSetLifetime(Rid particles, double lifetime)
```

引擎名：`particles_set_lifetime`

Sets the lifetime of each particle in the system. Equivalent to [`GpuParticles3D.Lifetime`](GpuParticles3D.md).

### ParticlesSetMode

```csharp
public static void ParticlesSetMode(Rid particles, RenderingServer.ParticlesMode mode)
```

引擎名：`particles_set_mode`

Sets whether the GPU particles specified by the `particles` RID should be rendered in 2D or 3D according to `mode`.

### ParticlesSetOneShot

```csharp
public static void ParticlesSetOneShot(Rid particles, bool oneShot)
```

引擎名：`particles_set_one_shot`

If `true`, particles will emit once and then stop. Equivalent to [`GpuParticles3D.OneShot`](GpuParticles3D.md).

### ParticlesSetPreProcessTime

```csharp
public static void ParticlesSetPreProcessTime(Rid particles, double time)
```

引擎名：`particles_set_pre_process_time`

Sets the preprocess time for the particles' animation. This lets you delay starting an animation until after the particles have begun emitting. Equivalent to [`GpuParticles3D.Preprocess`](GpuParticles3D.md).

### ParticlesSetProcessMaterial

```csharp
public static void ParticlesSetProcessMaterial(Rid particles, Rid material)
```

引擎名：`particles_set_process_material`

Sets the material for processing the particles.

**Note:** This is not the material used to draw the materials. Equivalent to [`GpuParticles3D.ProcessMaterial`](GpuParticles3D.md).

### ParticlesSetRandomnessRatio

```csharp
public static void ParticlesSetRandomnessRatio(Rid particles, float ratio)
```

引擎名：`particles_set_randomness_ratio`

Sets the emission randomness ratio. This randomizes the emission of particles within their phase. Equivalent to [`GpuParticles3D.Randomness`](GpuParticles3D.md).

### ParticlesSetSpeedScale

```csharp
public static void ParticlesSetSpeedScale(Rid particles, double scale)
```

引擎名：`particles_set_speed_scale`

Sets the speed scale of the particle system. Equivalent to [`GpuParticles3D.SpeedScale`](GpuParticles3D.md).

### ParticlesSetSubemitter

```csharp
public static void ParticlesSetSubemitter(Rid particles, Rid subemitterParticles)
```

引擎名：`particles_set_subemitter`

Sets the subemitter particles for the particle system. Equivalent to [`GpuParticles3D.SubEmitter`](GpuParticles3D.md).

### ParticlesSetTrailBindPoses

```csharp
public static void ParticlesSetTrailBindPoses(Rid particles, Array<Transform3D> bindPoses)
```

引擎名：`particles_set_trail_bind_poses`

Sets the trail bind poses for the particle system. This specified as an array of [`Transform3D`](Transform3D.md)s representing the bind pose for each draw pass. See [`GpuParticles3D.DrawSkin`](GpuParticles3D.md), [`Skin.GetBindCount`](Skin.md), and [`Skin.GetBindPose`](Skin.md). Set the value for each draw pass to `Transform3D.Identity` to use the default behavior, which is what built-in trails use ([`RibbonTrailMesh`](RibbonTrailMesh.md) and [`TubeTrailMesh`](TubeTrailMesh.md)).

### ParticlesSetTrails

```csharp
public static void ParticlesSetTrails(Rid particles, bool enable, float lengthSec)
```

引擎名：`particles_set_trails`

If `enable` is `true`, enables trails for the `particles` with the specified `lengthSec` in seconds. Equivalent to [`GpuParticles3D.TrailEnabled`](GpuParticles3D.md) and [`GpuParticles3D.TrailLifetime`](GpuParticles3D.md).

### ParticlesSetTransformAlign

```csharp
public static void ParticlesSetTransformAlign(Rid particles, RenderingServer.ParticlesTransformAlign align)
```

引擎名：`particles_set_transform_align`

Sets the transform alignment for the particle system. Equivalent to [`GpuParticles3D.TransformAlign`](GpuParticles3D.md).

### ParticlesSetTransformAlignAxis

```csharp
public static void ParticlesSetTransformAlignAxis(Rid particles, RenderingServer.ParticlesTransformAlignAxis rotationAxis)
```

引擎名：`particles_set_transform_align_axis`

Sets which axis to use for transform alignment.

### ParticlesSetTransformAlignChannelFilter

```csharp
public static void ParticlesSetTransformAlignChannelFilter(Rid particles, RenderingServer.ParticlesTransformAlignCustomSrc channelFilter)
```

引擎名：`particles_set_transform_align_channel_filter`

When using Z-Billboarding, which CUSTOM channel to read from.

### ParticlesSetUseLocalCoordinates

```csharp
public static void ParticlesSetUseLocalCoordinates(Rid particles, bool enable)
```

引擎名：`particles_set_use_local_coordinates`

If `true`, particles use local coordinates. If `false` they use global coordinates. Equivalent to [`GpuParticles3D.LocalCoords`](GpuParticles3D.md).

### PositionalSoftShadowFilterSetQuality

```csharp
public static void PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality quality)
```

引擎名：`positional_soft_shadow_filter_set_quality`

Sets the filter quality for omni and spot light shadows in 3D. See also `ProjectSettings.rendering/lights_and_shadows/positional_shadow/soft_shadow_filter_quality`. This parameter is global and cannot be set on a per-viewport basis.

### ReflectionProbeCreate

```csharp
public static Rid ReflectionProbeCreate()
```

引擎名：`reflection_probe_create`

Creates a reflection probe and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `reflection_probe_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this reflection probe to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`ReflectionProbe`](ReflectionProbe.md).

### ReflectionProbeSetAmbientColor

```csharp
public static void ReflectionProbeSetAmbientColor(Rid probe, Color color)
```

引擎名：`reflection_probe_set_ambient_color`

Sets the reflection probe's custom ambient light color. Equivalent to [`ReflectionProbe.AmbientColor`](ReflectionProbe.md).

### ReflectionProbeSetAmbientEnergy

```csharp
public static void ReflectionProbeSetAmbientEnergy(Rid probe, float energy)
```

引擎名：`reflection_probe_set_ambient_energy`

Sets the reflection probe's custom ambient light energy. Equivalent to [`ReflectionProbe.AmbientColorEnergy`](ReflectionProbe.md).

### ReflectionProbeSetAmbientMode

```csharp
public static void ReflectionProbeSetAmbientMode(Rid probe, RenderingServer.ReflectionProbeAmbientMode mode)
```

引擎名：`reflection_probe_set_ambient_mode`

Sets the reflection probe's ambient light mode. Equivalent to [`ReflectionProbe.AmbientMode`](ReflectionProbe.md).

### ReflectionProbeSetAsInterior

```csharp
public static void ReflectionProbeSetAsInterior(Rid probe, bool enable)
```

引擎名：`reflection_probe_set_as_interior`

If `true`, reflections will ignore sky contribution. Equivalent to [`ReflectionProbe.Interior`](ReflectionProbe.md).

### ReflectionProbeSetBlendDistance

```csharp
public static void ReflectionProbeSetBlendDistance(Rid probe, float blendDistance)
```

引擎名：`reflection_probe_set_blend_distance`

Sets the distance in meters over which a probe blends into the scene.

### ReflectionProbeSetCullMask

```csharp
public static void ReflectionProbeSetCullMask(Rid probe, uint layers)
```

引擎名：`reflection_probe_set_cull_mask`

Sets the render cull mask for this reflection probe. Only instances with a matching layer will be reflected by this probe. Equivalent to [`ReflectionProbe.CullMask`](ReflectionProbe.md).

### ReflectionProbeSetEnableBoxProjection

```csharp
public static void ReflectionProbeSetEnableBoxProjection(Rid probe, bool enable)
```

引擎名：`reflection_probe_set_enable_box_projection`

If `true`, uses box projection. This can make reflections look more correct in certain situations. Equivalent to [`ReflectionProbe.BoxProjection`](ReflectionProbe.md).

### ReflectionProbeSetEnableShadows

```csharp
public static void ReflectionProbeSetEnableShadows(Rid probe, bool enable)
```

引擎名：`reflection_probe_set_enable_shadows`

If `true`, computes shadows in the reflection probe. This makes the reflection much slower to compute. Equivalent to [`ReflectionProbe.EnableShadows`](ReflectionProbe.md).

### ReflectionProbeSetIntensity

```csharp
public static void ReflectionProbeSetIntensity(Rid probe, float intensity)
```

引擎名：`reflection_probe_set_intensity`

Sets the intensity of the reflection probe. Intensity modulates the strength of the reflection. Equivalent to [`ReflectionProbe.Intensity`](ReflectionProbe.md).

### ReflectionProbeSetMaxDistance

```csharp
public static void ReflectionProbeSetMaxDistance(Rid probe, float distance)
```

引擎名：`reflection_probe_set_max_distance`

Sets the max distance away from the probe an object can be before it is culled. Equivalent to [`ReflectionProbe.MaxDistance`](ReflectionProbe.md).

### ReflectionProbeSetMeshLodThreshold

```csharp
public static void ReflectionProbeSetMeshLodThreshold(Rid probe, float pixels)
```

引擎名：`reflection_probe_set_mesh_lod_threshold`

Sets the mesh level of detail to use in the reflection probe rendering. Higher values will use less detailed versions of meshes that have LOD variations generated, which can improve performance. Equivalent to [`ReflectionProbe.MeshLodThreshold`](ReflectionProbe.md).

### ReflectionProbeSetOriginOffset

```csharp
public static void ReflectionProbeSetOriginOffset(Rid probe, Vector3 offset)
```

引擎名：`reflection_probe_set_origin_offset`

Sets the origin offset to be used when this reflection probe is in box project mode. Equivalent to [`ReflectionProbe.OriginOffset`](ReflectionProbe.md).

### ReflectionProbeSetReflectionMask

```csharp
public static void ReflectionProbeSetReflectionMask(Rid probe, uint layers)
```

引擎名：`reflection_probe_set_reflection_mask`

Sets the render reflection mask for this reflection probe. Only instances with a matching layer will have reflections applied from this probe. Equivalent to [`ReflectionProbe.ReflectionMask`](ReflectionProbe.md).

### ReflectionProbeSetResolution

```csharp
public static void ReflectionProbeSetResolution(Rid probe, int resolution)
```

引擎名：`reflection_probe_set_resolution`

Deprecated. This method does nothing.

### ReflectionProbeSetSize

```csharp
public static void ReflectionProbeSetSize(Rid probe, Vector3 size)
```

引擎名：`reflection_probe_set_size`

Sets the size of the area that the reflection probe will capture. Equivalent to [`ReflectionProbe.Size`](ReflectionProbe.md).

### ReflectionProbeSetUpdateMode

```csharp
public static void ReflectionProbeSetUpdateMode(Rid probe, RenderingServer.ReflectionProbeUpdateMode mode)
```

引擎名：`reflection_probe_set_update_mode`

Sets how often the reflection probe updates. Can either be once or every frame.

### RequestFrameDrawnCallback

```csharp
public static void RequestFrameDrawnCallback(Callable callable)
```

引擎名：`request_frame_drawn_callback`

Schedules a callback to the given callable after a frame has been drawn.

### ScenarioCreate

```csharp
public static Rid ScenarioCreate()
```

引擎名：`scenario_create`

Creates a scenario and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `scenario_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

The scenario is the 3D world that all the visual instances exist in.

### ScenarioSetCameraAttributes

```csharp
public static void ScenarioSetCameraAttributes(Rid scenario, Rid effects)
```

引擎名：`scenario_set_camera_attributes`

Sets the camera attributes (`effects`) that will be used with this scenario. See also [`CameraAttributes`](CameraAttributes.md).

### ScenarioSetCompositor

```csharp
public static void ScenarioSetCompositor(Rid scenario, Rid compositor)
```

引擎名：`scenario_set_compositor`

Sets the compositor (`compositor`) that will be used with this scenario. See also [`Compositor`](Compositor.md).

### ScenarioSetEnvironment

```csharp
public static void ScenarioSetEnvironment(Rid scenario, Rid environment)
```

引擎名：`scenario_set_environment`

Sets the environment that will be used with this scenario. See also [`Environment`](Environment.md).

### ScenarioSetFallbackEnvironment

```csharp
public static void ScenarioSetFallbackEnvironment(Rid scenario, Rid environment)
```

引擎名：`scenario_set_fallback_environment`

Sets the fallback environment to be used by this scenario. The fallback environment is used if no environment is set. Internally, this is used by the editor to provide a default environment.

### ScreenSpaceRoughnessLimiterSetActive

```csharp
public static void ScreenSpaceRoughnessLimiterSetActive(bool enable, float amount, float limit)
```

引擎名：`screen_space_roughness_limiter_set_active`

Sets the screen-space roughness limiter parameters, such as whether it should be enabled and its thresholds. Equivalent to `ProjectSettings.rendering/anti_aliasing/screen_space_roughness_limiter/enabled`, `ProjectSettings.rendering/anti_aliasing/screen_space_roughness_limiter/amount` and `ProjectSettings.rendering/anti_aliasing/screen_space_roughness_limiter/limit`.

### SetBootImage

```csharp
public static void SetBootImage(Image image, Color color, bool scale, bool useFilter = true)
```

引擎名：`set_boot_image`

Sets a boot image. The `color` defines the background color. The value of `scale` indicates if the image will be scaled to fit the screen size. If `useFilter` is `true`, the image will be scaled with linear interpolation. If `useFilter` is `false`, the image will be scaled with nearest-neighbor interpolation.

### SetBootImageWithStretch

```csharp
public static void SetBootImageWithStretch(Image image, Color color, RenderingServer.SplashStretchMode stretchMode, bool useFilter = true)
```

引擎名：`set_boot_image_with_stretch`

Sets a boot image. The `color` defines the background color. The value of `stretchMode` indicates how the image will be stretched (see [`RenderingServer.SplashStretchMode`](RenderingServer.md) for possible values). If `useFilter` is `true`, the image will be scaled with linear interpolation. If `useFilter` is `false`, the image will be scaled with nearest-neighbor interpolation.

### SetDebugGenerateWireframes

```csharp
public static void SetDebugGenerateWireframes(bool generate)
```

引擎名：`set_debug_generate_wireframes`

If `generate` is `true`, generates debug wireframes for all meshes that are loaded when using the Compatibility renderer. By default, the engine does not generate debug wireframes at runtime, since they slow down loading of assets and take up VRAM.

**Note:** You must call this method before loading any meshes when using the Compatibility renderer. Otherwise, wireframes will not be used.

### SetDefaultClearColor

```csharp
public static void SetDefaultClearColor(Color color)
```

引擎名：`set_default_clear_color`

Sets the default clear color which is used when a specific clear color has not been selected. See also `GetDefaultClearColor()`.

### SetRenderLoopEnabled

```csharp
public static void SetRenderLoopEnabled(bool enabled)
```

引擎名：`set_render_loop_enabled`

### ShaderCreate

```csharp
public static Rid ShaderCreate()
```

引擎名：`shader_create`

Creates an empty shader and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `shader_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Shader`](Shader.md).

### ShaderGetCode

```csharp
public static string ShaderGetCode(Rid shader)
```

引擎名：`shader_get_code`

Returns a shader's source code as a string.

### ShaderGetDefaultTextureParameter

```csharp
public static Rid ShaderGetDefaultTextureParameter(Rid shader, StringName name, int index = 0)
```

引擎名：`shader_get_default_texture_parameter`

Returns a default texture from a shader searched by name.

**Note:** If the sampler array is used use `index` to access the specified texture.

### ShaderGetParameterDefault

```csharp
public static Variant ShaderGetParameterDefault(Rid shader, StringName name)
```

引擎名：`shader_get_parameter_default`

Returns the default value for the specified shader uniform. This is usually the value written in the shader source code.

### ShaderSetCode

```csharp
public static void ShaderSetCode(Rid shader, string code)
```

引擎名：`shader_set_code`

Sets the shader's source code (which triggers recompilation after being changed).

### ShaderSetDefaultTextureParameter

```csharp
public static void ShaderSetDefaultTextureParameter(Rid shader, StringName name, Rid texture, int index = 0)
```

引擎名：`shader_set_default_texture_parameter`

Sets a shader's default texture. Overwrites the texture given by name.

**Note:** If the sampler array is used use `index` to access the specified texture.

### ShaderSetPathHint

```csharp
public static void ShaderSetPathHint(Rid shader, string path)
```

引擎名：`shader_set_path_hint`

Sets the path hint for the specified shader. This should generally match the [`Shader`](Shader.md) resource's [`Resource.ResourcePath`](Resource.md).

### SkeletonAllocateData

```csharp
public static void SkeletonAllocateData(Rid skeleton, int bones, bool is2DSkeleton = false)
```

引擎名：`skeleton_allocate_data`

Allocates data for this skeleton using the number of bones specified in `bones`. If `is2DSkeleton` is `true`, the skeleton will be treated as a 2D skeleton instead of a 3D skeleton. See also `SkeletonGetBoneCount()`.

### SkeletonBoneGetTransform

```csharp
public static Transform3D SkeletonBoneGetTransform(Rid skeleton, int bone)
```

引擎名：`skeleton_bone_get_transform`

Returns the [`Transform3D`](Transform3D.md) set for a specific bone of this skeleton.

### SkeletonBoneGetTransform2D

```csharp
public static Transform2D SkeletonBoneGetTransform2D(Rid skeleton, int bone)
```

Returns the [`Transform2D`](Transform2D.md) set for a specific bone of this skeleton.

### SkeletonBoneSetTransform

```csharp
public static void SkeletonBoneSetTransform(Rid skeleton, int bone, Transform3D transform)
```

引擎名：`skeleton_bone_set_transform`

Sets the [`Transform3D`](Transform3D.md) for a specific bone of this skeleton.

### SkeletonBoneSetTransform2D

```csharp
public static void SkeletonBoneSetTransform2D(Rid skeleton, int bone, Transform2D transform)
```

Sets the [`Transform2D`](Transform2D.md) for a specific bone of this skeleton.

### SkeletonCreate

```csharp
public static Rid SkeletonCreate()
```

引擎名：`skeleton_create`

Creates a skeleton and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `skeleton_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

### SkeletonGetBoneCount

```csharp
public static int SkeletonGetBoneCount(Rid skeleton)
```

引擎名：`skeleton_get_bone_count`

Returns the number of bones allocated for this skeleton. See also `SkeletonAllocateData()`.

### SkeletonSetBaseTransform2D

```csharp
public static void SkeletonSetBaseTransform2D(Rid skeleton, Transform2D baseTransform)
```

Sets the base [`Transform2D`](Transform2D.md) to use for the specified skeleton.

### SkyBakePanorama

```csharp
public static Image SkyBakePanorama(Rid sky, float energy, bool bakeIrradiance, Vector2I size)
```

引擎名：`sky_bake_panorama`

Generates and returns an [`Image`](Image.md) containing the radiance map for the specified `sky` RID. This supports built-in sky material and custom sky shaders. If `bakeIrradiance` is `true`, the irradiance map is saved instead of the radiance map. The radiance map is used to render reflected light, while the irradiance map is used to render ambient light. See also `EnvironmentBakePanorama()`.

**Note:** The image is saved using linear encoding without any tonemapping performed, which means it will look too dark if viewed directly in an image editor. `energy` values above `1.0` can be used to brighten the resulting image.

**Note:** `size` should be a 2:1 aspect ratio for the generated panorama to have square pixels. For radiance maps, there is no point in using a height greater than [`Sky.RadianceSize`](Sky.md), as it won't increase detail. Irradiance maps only contain low-frequency data, so there is usually no point in going past a size of 128×64 pixels when saving an irradiance map.

### SkyCreate

```csharp
public static Rid SkyCreate()
```

引擎名：`sky_create`

Creates an empty sky and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `sky_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

### SkySetMaterial

```csharp
public static void SkySetMaterial(Rid sky, Rid material)
```

引擎名：`sky_set_material`

Sets the material that the sky uses to render the background, ambient and reflection maps.

### SkySetMode

```csharp
public static void SkySetMode(Rid sky, RenderingServer.SkyMode mode)
```

引擎名：`sky_set_mode`

Sets the process `mode` of the sky specified by the `sky` RID. Equivalent to [`Sky.ProcessMode`](Sky.md).

### SkySetRadianceSize

```csharp
public static void SkySetRadianceSize(Rid sky, int radianceSize)
```

引擎名：`sky_set_radiance_size`

Sets the `radianceSize` of the sky specified by the `sky` RID (in pixels). Equivalent to [`Sky.RadianceSize`](Sky.md).

### SpotLightCreate

```csharp
public static Rid SpotLightCreate()
```

引擎名：`spot_light_create`

Creates a spot light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this spot light to an instance using `InstanceSetBase()` using the returned RID.

### SubSurfaceScatteringSetQuality

```csharp
public static void SubSurfaceScatteringSetQuality(RenderingServer.SubSurfaceScatteringQuality quality)
```

引擎名：`sub_surface_scattering_set_quality`

Sets `ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_quality` to use when rendering materials that have subsurface scattering enabled.

### SubSurfaceScatteringSetScale

```csharp
public static void SubSurfaceScatteringSetScale(float scale, float depthScale)
```

引擎名：`sub_surface_scattering_set_scale`

Sets the `ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_scale` and `ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_depth_scale` to use when rendering materials that have subsurface scattering enabled.

### Texture2DCreate

```csharp
public static Rid Texture2DCreate(Image image)
```

Creates a 2-dimensional texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_2d_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Texture2D`](Texture2D.md).

**Note:** Not to be confused with [`RenderingDevice.TextureCreate`](RenderingDevice.md), which creates the graphics API's own texture type as opposed to the Godot-specific [`Texture2D`](Texture2D.md) resource.

### Texture2DGet

```csharp
public static Image Texture2DGet(Rid texture)
```

Returns an [`Image`](Image.md) instance from the given `texture` [`Rid`](Rid.md).

**Example:** Get the test texture from `GetTestTexture()` and apply it to a [`Sprite2D`](Sprite2D.md) node:

```csharp
var texture_rid = RenderingServer.get_test_texture()
var texture = ImageTexture.create_from_image(RenderingServer.texture_2d_get(texture_rid))
$Sprite2D.texture = texture

```

### Texture2DLayerGet

```csharp
public static Image Texture2DLayerGet(Rid texture, int layer)
```

Returns an [`Image`](Image.md) instance from the given `texture` [`Rid`](Rid.md) and `layer`.

### Texture2DLayeredCreate

```csharp
public static Rid Texture2DLayeredCreate(Array<Image> layers, RenderingServer.TextureLayeredType layeredType)
```

Creates a 2-dimensional layered texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_2d_layered_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`TextureLayered`](TextureLayered.md).

### Texture2DLayeredPlaceholderCreate

```csharp
public static Rid Texture2DLayeredPlaceholderCreate(RenderingServer.TextureLayeredType layeredType)
```

Creates a placeholder for a 2-dimensional layered texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_2d_layered_*` RenderingServer functions, although it does nothing when used. See also `Texture2DPlaceholderCreate()`.

**Note:** The equivalent resource is [`PlaceholderTextureLayered`](PlaceholderTextureLayered.md).

### Texture2DPlaceholderCreate

```csharp
public static Rid Texture2DPlaceholderCreate()
```

Creates a placeholder for a 2-dimensional layered texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_2d_layered_*` RenderingServer functions, although it does nothing when used. See also `Texture2DLayeredPlaceholderCreate()`.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`PlaceholderTexture2D`](PlaceholderTexture2D.md).

### Texture2DUpdate

```csharp
public static void Texture2DUpdate(Rid texture, Image image, int layer)
```

Updates the texture specified by the `texture` [`Rid`](Rid.md) with the data in `image`. A `layer` must also be specified, which should be `0` when updating a single-layer texture ([`Texture2D`](Texture2D.md)).

**Note:** The `image` must have the same width, height and format as the current `texture` data. Otherwise, an error will be printed and the original texture won't be modified. If you need to use different width, height or format, use `TextureReplace()` instead.

### Texture3DCreate

```csharp
public static Rid Texture3DCreate(Image.Format format, int width, int height, int depth, bool mipmaps, Array<Image> data)
```

**Note:** The equivalent resource is [`Texture3D`](Texture3D.md).

### Texture3DGet

```csharp
public static Array<Image> Texture3DGet(Rid texture)
```

Returns 3D texture data as an array of [`Image`](Image.md)s for the specified texture [`Rid`](Rid.md).

### Texture3DPlaceholderCreate

```csharp
public static Rid Texture3DPlaceholderCreate()
```

Creates a placeholder for a 3-dimensional texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_3d_*` RenderingServer functions, although it does nothing when used.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`PlaceholderTexture3D`](PlaceholderTexture3D.md).

### Texture3DUpdate

```csharp
public static void Texture3DUpdate(Rid texture, Array<Image> data)
```

Updates the texture specified by the `texture` [`Rid`](Rid.md)'s data with the data in `data`. All the texture's layers must be replaced at once.

**Note:** The `texture` must have the same width, height, depth and format as the current texture data. Otherwise, an error will be printed and the original texture won't be modified. If you need to use different width, height, depth or format, use `TextureReplace()` instead.

### TextureCreateFromNativeHandle

```csharp
public static Rid TextureCreateFromNativeHandle(RenderingServer.TextureType type, Image.Format format, ulong nativeHandle, int width, int height, int depth, int layers = 1, RenderingServer.TextureLayeredType layeredType = RenderingServer.TextureLayeredType.Layered2DArray)
```

引擎名：`texture_create_from_native_handle`

Creates a texture based on a native handle that was created outside of Godot's renderer.

**Note:** If using only the rendering device renderer, it's recommend to use [`RenderingDevice.TextureCreateFromExtension`](RenderingDevice.md) together with `TextureRdCreate()`, rather than this method. This way, the texture's format and usage can be controlled more effectively.

### TextureDrawableBlitRect

```csharp
public static void TextureDrawableBlitRect(Array<Rid> textures, Rect2I rect, Rid material, Color modulate, Array<Rid> sourceTextures, int toMipmap = 0)
```

引擎名：`texture_drawable_blit_rect`

Draws to `rect` on up to 4 given Drawable `textures`, using a TextureBlit Shader from `material`. `modulate` and up to 4 `sourceTextures` are uniforms for the Shader to process with. `toMipmap` can specify to perform this draw to a lower mipmap level.

**Note:** All `textures` must be the same size and format.

### TextureDrawableCreate

```csharp
public static Rid TextureDrawableCreate(int width, int height, RenderingServer.TextureDrawableFormat format, Color? color = null, bool withMipmaps = false)
```

引擎名：`texture_drawable_create`

Creates a 2-dimensional texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_drawable*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`DrawableTexture2D`](DrawableTexture2D.md).

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### TextureDrawableGenerateMipmaps

```csharp
public static void TextureDrawableGenerateMipmaps(Rid texture)
```

引擎名：`texture_drawable_generate_mipmaps`

Calculates new MipMaps for the given Drawable `texture`.

### TextureDrawableGetDefaultMaterial

```csharp
public static Rid TextureDrawableGetDefaultMaterial()
```

引擎名：`texture_drawable_get_default_material`

Returns a ShaderMaterial with the default texture_blit Shader.

### TextureGetFormat

```csharp
public static Image.Format TextureGetFormat(Rid texture)
```

引擎名：`texture_get_format`

Returns the format for the texture.

### TextureGetNativeHandle

```csharp
public static ulong TextureGetNativeHandle(Rid texture, bool srgb = false)
```

引擎名：`texture_get_native_handle`

Returns the internal graphics handle for this texture object. For use when communicating with third-party APIs mostly with GDExtension.

`srgb` should be `true` when the texture uses nonlinear sRGB encoding and `false` when the texture uses linear encoding.

**Note:** This function returns a `uint64_t` which internally maps to a `GLuint` (OpenGL) or `VkImage` (Vulkan).

### TextureGetPath

```csharp
public static string TextureGetPath(Rid texture)
```

引擎名：`texture_get_path`

Returns the resource path (starting with `res://` or `uid://`) for the specified texture RID. Returns an empty `System.String` if the resource is built-in. See also `TextureSetPath()`.

### TextureGetRdTexture

```csharp
public static Rid TextureGetRdTexture(Rid texture, bool srgb = false)
```

引擎名：`texture_get_rd_texture`

Returns a texture [`Rid`](Rid.md) that can be used with [`RenderingDevice`](RenderingDevice.md).

`srgb` should be `true` when the texture uses nonlinear sRGB encoding and `false` when the texture uses linear encoding.

### TextureProxyCreate

```csharp
public static Rid TextureProxyCreate(Rid base)
```

引擎名：`texture_proxy_create`

This method does nothing and always returns an invalid [`Rid`](Rid.md).

### TextureProxyUpdate

```csharp
public static void TextureProxyUpdate(Rid texture, Rid proxyTo)
```

引擎名：`texture_proxy_update`

This method does nothing.

### TextureRdCreate

```csharp
public static Rid TextureRdCreate(Rid rdTexture, RenderingServer.TextureLayeredType layerType = RenderingServer.TextureLayeredType.Layered2DArray)
```

引擎名：`texture_rd_create`

Creates a new texture object based on a texture created directly on the [`RenderingDevice`](RenderingDevice.md). If the texture contains layers, `layerType` is used to define the layer type.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The RenderingServer's `FreeRid()` won't free the underlying `rdTexture`, you will want to free the `rdTexture` using [`RenderingDevice.FreeRid`](RenderingDevice.md).

### TextureReplace

```csharp
public static void TextureReplace(Rid texture, Rid byTexture)
```

引擎名：`texture_replace`

Replaces `texture`'s texture data by the texture specified by the `byTexture` RID, without changing `texture`'s RID.

### TextureSetForceRedrawIfVisible

```csharp
public static void TextureSetForceRedrawIfVisible(Rid texture, bool enable)
```

引擎名：`texture_set_force_redraw_if_visible`

Sets whether the texture RID should force redrawing when it's visible on screen when [`OS.LowProcessorUsageMode`](OS.md) is `true`. This is used by [`AnimatedTexture`](AnimatedTexture.md) to force redrawing.

### TextureSetPath

```csharp
public static void TextureSetPath(Rid texture, string path)
```

引擎名：`texture_set_path`

Sets the resource path for this texture RID. See also `TextureGetPath()`.

**Note:** This is purely a hint and does not cause the texture to be automatically saved when set to a `res://` path.

### TextureSetSizeOverride

```csharp
public static void TextureSetSizeOverride(Rid texture, int width, int height)
```

引擎名：`texture_set_size_override`

Sets the size at which the texture should be *displayed* in 2D, ignoring its original size. This does not rescale the texture data itself, only how it is drawn in 2D. Set `width` and `height` to 0 to disable the size override.

### ViewportAttachCamera

```csharp
public static void ViewportAttachCamera(Rid viewport, Rid camera)
```

引擎名：`viewport_attach_camera`

Sets a viewport's camera.

### ViewportAttachCanvas

```csharp
public static void ViewportAttachCanvas(Rid viewport, Rid canvas)
```

引擎名：`viewport_attach_canvas`

Sets a viewport's canvas.

### ViewportAttachToScreen

```csharp
public static void ViewportAttachToScreen(Rid viewport, Rect2? rect = null, int screen = 0)
```

引擎名：`viewport_attach_to_screen`

Copies the viewport to a region of the screen specified by `rect`. If `ViewportSetRenderDirectToScreen()` is `true`, then the viewport does not use a framebuffer and the contents of the viewport are rendered directly to screen. However, note that the root viewport is drawn last, therefore it will draw over the screen. Accordingly, you must set the root viewport to an area that does not cover the area that you have attached this viewport to.

For example, you can set the root viewport to not render at all with the following code:

Using this can result in significant optimization, especially on lower-end devices. However, it comes at the cost of having to manage your viewports manually. For further optimization, see `ViewportSetRenderDirectToScreen()`.

参数：
- `rect`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### ViewportCreate

```csharp
public static Rid ViewportCreate()
```

引擎名：`viewport_create`

Creates an empty viewport and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `viewport_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`Viewport`](Viewport.md).

### ViewportGetMeasuredRenderTimeCpu

```csharp
public static double ViewportGetMeasuredRenderTimeCpu(Rid viewport)
```

引擎名：`viewport_get_measured_render_time_cpu`

Returns the CPU time taken to render the last frame in milliseconds. This *only* includes time spent in rendering-related operations; scripts' `_process` functions and other engine subsystems are not included in this readout. To get a complete readout of CPU time spent to render the scene, sum the render times of all viewports that are drawn every frame plus `GetFrameSetupTimeCpu()`. Unlike [`Engine.GetFramesPerSecond`](Engine.md), this method will accurately reflect CPU utilization even if framerate is capped via V-Sync or [`Engine.MaxFps`](Engine.md). See also `ViewportGetMeasuredRenderTimeGpu()`.

**Note:** Requires measurements to be enabled on the specified `viewport` using `ViewportSetMeasureRenderTime()`. Otherwise, this method returns `0.0`.

### ViewportGetMeasuredRenderTimeGpu

```csharp
public static double ViewportGetMeasuredRenderTimeGpu(Rid viewport)
```

引擎名：`viewport_get_measured_render_time_gpu`

Returns the GPU time taken to render the last frame in milliseconds. To get a complete readout of GPU time spent to render the scene, sum the render times of all viewports that are drawn every frame. Unlike [`Engine.GetFramesPerSecond`](Engine.md), this method accurately reflects GPU utilization even if framerate is capped via V-Sync or [`Engine.MaxFps`](Engine.md). See also `ViewportGetMeasuredRenderTimeCpu()`.

**Note:** Requires measurements to be enabled on the specified `viewport` using `ViewportSetMeasureRenderTime()`. Otherwise, this method returns `0.0`.

**Note:** When GPU utilization is low enough during a certain period of time, GPUs will decrease their power state (which in turn decreases core and memory clock speeds). This can cause the reported GPU time to increase if GPU utilization is kept low enough by a framerate cap (compared to what it would be at the GPU's highest power state). Keep this in mind when benchmarking using `ViewportGetMeasuredRenderTimeGpu()`. This behavior can be overridden in the graphics driver settings at the cost of higher power usage.

### ViewportGetRenderInfo

```csharp
public static int ViewportGetRenderInfo(Rid viewport, RenderingServer.ViewportRenderInfoType type, RenderingServer.ViewportRenderInfo info)
```

引擎名：`viewport_get_render_info`

Returns a statistic about the rendering engine which can be used for performance profiling. This is separated into render pass `type`s, each of them having the same `info`s you can query (different passes will return different values).

See also `GetRenderingInfo()`, which returns global information across all viewports.

**Note:** Viewport rendering information is not available until at least 2 frames have been rendered by the engine. If rendering information is not available, `ViewportGetRenderInfo()` returns `0`. To print rendering information in `_ready()` successfully, use the following:

```csharp
func _ready():
for _i in 2:
await get_tree().process_frame

print(
RenderingServer.viewport_get_render_info(get_viewport().get_viewport_rid(),
RenderingServer.VIEWPORT_RENDER_INFO_TYPE_VISIBLE,
RenderingServer.VIEWPORT_RENDER_INFO_DRAW_CALLS_IN_FRAME)
)

```

### ViewportGetRenderTarget

```csharp
public static Rid ViewportGetRenderTarget(Rid viewport)
```

引擎名：`viewport_get_render_target`

Returns the render target for the viewport.

### ViewportGetTexture

```csharp
public static Rid ViewportGetTexture(Rid viewport)
```

引擎名：`viewport_get_texture`

Returns the viewport's last rendered frame.

### ViewportGetUpdateMode

```csharp
public static RenderingServer.ViewportUpdateMode ViewportGetUpdateMode(Rid viewport)
```

引擎名：`viewport_get_update_mode`

Returns the viewport's update mode.

**Warning:** Calling this from any thread other than the rendering thread will be detrimental to performance.

### ViewportRemoveCanvas

```csharp
public static void ViewportRemoveCanvas(Rid viewport, Rid canvas)
```

引擎名：`viewport_remove_canvas`

Detaches a viewport from a canvas.

### ViewportSetActive

```csharp
public static void ViewportSetActive(Rid viewport, bool active)
```

引擎名：`viewport_set_active`

If `true`, sets the viewport active, else sets it inactive.

### ViewportSetAnisotropicFilteringLevel

```csharp
public static void ViewportSetAnisotropicFilteringLevel(Rid viewport, RenderingServer.ViewportAnisotropicFiltering anisotropicFilteringLevel)
```

引擎名：`viewport_set_anisotropic_filtering_level`

Sets the maximum number of samples to take when using anisotropic filtering on textures (as a power of two). A higher sample count will result in sharper textures at oblique angles, but is more expensive to compute. A value of `0` forcibly disables anisotropic filtering, even on materials where it is enabled.

The anisotropic filtering level also affects decals and light projectors if they are configured to use anisotropic filtering. See `ProjectSettings.rendering/textures/decals/filter` and `ProjectSettings.rendering/textures/light_projectors/filter`.

**Note:** In 3D, for this setting to have an effect, set [`BaseMaterial3D.TextureFilter`](BaseMaterial3D.md) to [`BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic`](BaseMaterial3D.md) or [`BaseMaterial3D.TextureFilterEnum.NearestWithMipmapsAnisotropic`](BaseMaterial3D.md) on materials.

**Note:** In 2D, for this setting to have an effect, set [`CanvasItem.TextureFilter`](CanvasItem.md) to [`CanvasItem.TextureFilterEnum.LinearWithMipmapsAnisotropic`](CanvasItem.md) or [`CanvasItem.TextureFilterEnum.NearestWithMipmapsAnisotropic`](CanvasItem.md) on the [`CanvasItem`](CanvasItem.md) node displaying the texture (or in [`CanvasTexture`](CanvasTexture.md)). However, anisotropic filtering is rarely useful in 2D, so only enable it for textures in 2D if it makes a meaningful visual difference.

### ViewportSetCanvasCullMask

```csharp
public static void ViewportSetCanvasCullMask(Rid viewport, uint canvasCullMask)
```

引擎名：`viewport_set_canvas_cull_mask`

Sets the rendering mask associated with this [`Viewport`](Viewport.md). Only [`CanvasItem`](CanvasItem.md) nodes with a matching rendering visibility layer will be rendered by this [`Viewport`](Viewport.md).

### ViewportSetCanvasStacking

```csharp
public static void ViewportSetCanvasStacking(Rid viewport, Rid canvas, int layer, int sublayer)
```

引擎名：`viewport_set_canvas_stacking`

Sets the stacking order for a viewport's canvas.

`layer` is the actual canvas layer, while `sublayer` specifies the stacking order of the canvas among those in the same layer.

**Note:** `layer` should be between `CanvasLayerMin` and `CanvasLayerMax` (inclusive). Any other value will wrap around.

### ViewportSetCanvasTransform

```csharp
public static void ViewportSetCanvasTransform(Rid viewport, Rid canvas, Transform2D offset)
```

引擎名：`viewport_set_canvas_transform`

Sets the transformation of a viewport's canvas.

### ViewportSetClearMode

```csharp
public static void ViewportSetClearMode(Rid viewport, RenderingServer.ViewportClearMode clearMode)
```

引擎名：`viewport_set_clear_mode`

Sets the clear mode of a viewport.

### ViewportSetDebugDraw

```csharp
public static void ViewportSetDebugDraw(Rid viewport, RenderingServer.ViewportDebugDraw draw)
```

引擎名：`viewport_set_debug_draw`

Sets the debug draw mode of a viewport.

### ViewportSetDefaultCanvasItemTextureFilter

```csharp
public static void ViewportSetDefaultCanvasItemTextureFilter(Rid viewport, RenderingServer.CanvasItemTextureFilter filter)
```

引擎名：`viewport_set_default_canvas_item_texture_filter`

Sets the default texture filtering mode for the specified `viewport` RID.

### ViewportSetDefaultCanvasItemTextureRepeat

```csharp
public static void ViewportSetDefaultCanvasItemTextureRepeat(Rid viewport, RenderingServer.CanvasItemTextureRepeat repeat)
```

引擎名：`viewport_set_default_canvas_item_texture_repeat`

Sets the default texture repeat mode for the specified `viewport` RID.

### ViewportSetDisable2D

```csharp
public static void ViewportSetDisable2D(Rid viewport, bool disable)
```

If `true`, the viewport's canvas (i.e. 2D and GUI elements) is not rendered.

### ViewportSetDisable3D

```csharp
public static void ViewportSetDisable3D(Rid viewport, bool disable)
```

If `true`, the viewport's 3D elements are not rendered.

### ViewportSetEnvironmentMode

```csharp
public static void ViewportSetEnvironmentMode(Rid viewport, RenderingServer.ViewportEnvironmentMode mode)
```

引擎名：`viewport_set_environment_mode`

Sets the viewport's environment mode which allows enabling or disabling rendering of 3D environment over 2D canvas. When disabled, 2D will not be affected by the environment. When enabled, 2D will be affected by the environment if the environment background mode is [`RenderingServer.EnvironmentBG.Canvas`](RenderingServer.md). The default behavior is to inherit the setting from the viewport's parent. If the topmost parent is also set to [`RenderingServer.ViewportEnvironmentMode.Inherit`](RenderingServer.md), then the behavior will be the same as if it was set to [`RenderingServer.ViewportEnvironmentMode.Enabled`](RenderingServer.md).

### ViewportSetFsrSharpness

```csharp
public static void ViewportSetFsrSharpness(Rid viewport, float sharpness)
```

引擎名：`viewport_set_fsr_sharpness`

Determines how sharp the upscaled image will be when using the FSR upscaling mode. Sharpness halves with every whole number. Values go from 0.0 (sharpest) to 2.0. Values above 2.0 won't make a visible difference.

### ViewportSetGlobalCanvasTransform

```csharp
public static void ViewportSetGlobalCanvasTransform(Rid viewport, Transform2D transform)
```

引擎名：`viewport_set_global_canvas_transform`

Sets the viewport's global transformation matrix.

### ViewportSetMeasureRenderTime

```csharp
public static void ViewportSetMeasureRenderTime(Rid viewport, bool enable)
```

引擎名：`viewport_set_measure_render_time`

Sets the measurement for the given `viewport` RID (obtained using [`Viewport.GetViewportRid`](Viewport.md)). Once enabled, `ViewportGetMeasuredRenderTimeCpu()` and `ViewportGetMeasuredRenderTimeGpu()` will return values greater than `0.0` when queried with the given `viewport`.

### ViewportSetMsaa2D

```csharp
public static void ViewportSetMsaa2D(Rid viewport, RenderingServer.ViewportMsaa msaa)
```

Sets the multisample antialiasing mode for 2D/Canvas on the specified `viewport` RID. Equivalent to `ProjectSettings.rendering/anti_aliasing/quality/msaa_2d` or [`Viewport.Msaa2D`](Viewport.md).

### ViewportSetMsaa3D

```csharp
public static void ViewportSetMsaa3D(Rid viewport, RenderingServer.ViewportMsaa msaa)
```

Sets the multisample antialiasing mode for 3D on the specified `viewport` RID. Equivalent to `ProjectSettings.rendering/anti_aliasing/quality/msaa_3d` or [`Viewport.Msaa3D`](Viewport.md).

### ViewportSetOcclusionCullingBuildQuality

```csharp
public static void ViewportSetOcclusionCullingBuildQuality(RenderingServer.ViewportOcclusionCullingBuildQuality quality)
```

引擎名：`viewport_set_occlusion_culling_build_quality`

Sets the `ProjectSettings.rendering/occlusion_culling/bvh_build_quality` to use for occlusion culling. This parameter is global and cannot be set on a per-viewport basis.

### ViewportSetOcclusionRaysPerThread

```csharp
public static void ViewportSetOcclusionRaysPerThread(int raysPerThread)
```

引擎名：`viewport_set_occlusion_rays_per_thread`

Sets the `ProjectSettings.rendering/occlusion_culling/occlusion_rays_per_thread` to use for occlusion culling. This parameter is global and cannot be set on a per-viewport basis.

### ViewportSetParentViewport

```csharp
public static void ViewportSetParentViewport(Rid viewport, Rid parentViewport)
```

引擎名：`viewport_set_parent_viewport`

Sets the viewport's parent to the viewport specified by the `parentViewport` RID.

### ViewportSetPositionalShadowAtlasQuadrantSubdivision

```csharp
public static void ViewportSetPositionalShadowAtlasQuadrantSubdivision(Rid viewport, int quadrant, int subdivision)
```

引擎名：`viewport_set_positional_shadow_atlas_quadrant_subdivision`

Sets the number of subdivisions to use in the specified shadow atlas `quadrant` for omni and spot shadows. See also [`Viewport.SetPositionalShadowAtlasQuadrantSubdiv`](Viewport.md).

### ViewportSetPositionalShadowAtlasSize

```csharp
public static void ViewportSetPositionalShadowAtlasSize(Rid viewport, int size, bool use16Bits = false)
```

引擎名：`viewport_set_positional_shadow_atlas_size`

Sets the `size` of the shadow atlas's images (used for omni and spot lights) on the viewport specified by the `viewport` RID. The value is rounded up to the nearest power of 2. If `use16Bits` is `true`, use 16 bits for the omni/spot shadow depth map. Enabling this results in shadows having less precision and may result in shadow acne, but can lead to performance improvements on some devices.

**Note:** If this is set to `0`, no positional shadows will be visible at all. This can improve performance significantly on low-end systems by reducing both the CPU and GPU load (as fewer draw calls are needed to draw the scene without shadows).

### ViewportSetRenderDirectToScreen

```csharp
public static void ViewportSetRenderDirectToScreen(Rid viewport, bool enabled)
```

引擎名：`viewport_set_render_direct_to_screen`

If `true`, render the contents of the viewport directly to screen. This allows a low-level optimization where you can skip drawing a viewport to the root viewport. While this optimization can result in a significant increase in speed (especially on older devices), it comes at a cost of usability. When this is enabled, you cannot read from the viewport or from the screen_texture. You also lose the benefit of certain window settings, such as the various stretch modes. Another consequence to be aware of is that in 2D the rendering happens in window coordinates, so if you have a viewport that is double the size of the window, and you set this, then only the portion that fits within the window will be drawn, no automatic scaling is possible, even if your game scene is significantly larger than the window size.

### ViewportSetScaling3DMode

```csharp
public static void ViewportSetScaling3DMode(Rid viewport, RenderingServer.ViewportScaling3DMode scaling3DMode)
```

Sets the 3D resolution scaling mode. Bilinear scaling renders at different resolution to either undersample or supersample the viewport. FidelityFX Super Resolution 1.0, abbreviated to FSR, is an upscaling technology that produces high quality images at fast framerates by using a spatially aware upscaling algorithm. FSR is slightly more expensive than bilinear, but it produces significantly higher image quality. FSR should be used where possible.

### ViewportSetScaling3DScale

```csharp
public static void ViewportSetScaling3DScale(Rid viewport, float scale)
```

Scales the 3D render buffer based on the viewport size uses an image filter specified in [`RenderingServer.ViewportScaling3DMode`](RenderingServer.md) to scale the output image to the full viewport size. Values lower than `1.0` can be used to speed up 3D rendering at the cost of quality (undersampling). Values greater than `1.0` are only valid for bilinear mode and can be used to improve 3D rendering quality at a high performance cost (supersampling). See also [`RenderingServer.ViewportMsaa`](RenderingServer.md) for multi-sample antialiasing, which is significantly cheaper but only smoothens the edges of polygons.

When using FSR upscaling, AMD recommends exposing the following values as preset options to users "Ultra Quality: 0.77", "Quality: 0.67", "Balanced: 0.59", "Performance: 0.5" instead of exposing the entire scale.

### ViewportSetScenario

```csharp
public static void ViewportSetScenario(Rid viewport, Rid scenario)
```

引擎名：`viewport_set_scenario`

Sets a viewport's scenario. The scenario contains information about environment information, reflection atlas, etc.

### ViewportSetScreenSpaceAA

```csharp
public static void ViewportSetScreenSpaceAA(Rid viewport, RenderingServer.ViewportScreenSpaceAA mode)
```

引擎名：`viewport_set_screen_space_aa`

Sets the viewport's screen-space antialiasing mode. Equivalent to `ProjectSettings.rendering/anti_aliasing/quality/screen_space_aa` or [`Viewport.ScreenSpaceAA`](Viewport.md).

### ViewportSetSdfOversizeAndScale

```csharp
public static void ViewportSetSdfOversizeAndScale(Rid viewport, RenderingServer.ViewportSdfOversize oversize, RenderingServer.ViewportSdfScale scale)
```

引擎名：`viewport_set_sdf_oversize_and_scale`

Sets the viewport's 2D signed distance field `ProjectSettings.rendering/2d/sdf/oversize` and `ProjectSettings.rendering/2d/sdf/scale`. This is used when sampling the signed distance field in [`CanvasItem`](CanvasItem.md) shaders as well as [`GpuParticles2D`](GpuParticles2D.md) collision. This is *not* used by SDFGI in 3D rendering.

### ViewportSetSize

```csharp
public static void ViewportSetSize(Rid viewport, int width, int height)
```

引擎名：`viewport_set_size`

Sets the viewport's `width` and `height` in pixels. Optionally the `viewCount` can be set to increase the number of view layers for stereo rendering.

### ViewportSetSize

```csharp
public static void ViewportSetSize(Rid viewport, int width, int height, int viewCount = 1)
```

引擎名：`viewport_set_size`

Sets the viewport's `width` and `height` in pixels. Optionally the `viewCount` can be set to increase the number of view layers for stereo rendering.

### ViewportSetSnap2DTransformsToPixel

```csharp
public static void ViewportSetSnap2DTransformsToPixel(Rid viewport, bool enabled)
```

If `true`, canvas item transforms (i.e. origin position) are snapped to the nearest pixel when rendering. This can lead to a crisper appearance at the cost of less smooth movement, especially when [`Camera2D`](Camera2D.md) smoothing is enabled. Equivalent to `ProjectSettings.rendering/2d/snap/snap_2d_transforms_to_pixel`.

### ViewportSetSnap2DVerticesToPixel

```csharp
public static void ViewportSetSnap2DVerticesToPixel(Rid viewport, bool enabled)
```

If `true`, canvas item vertices (i.e. polygon points) are snapped to the nearest pixel when rendering. This can lead to a crisper appearance at the cost of less smooth movement, especially when [`Camera2D`](Camera2D.md) smoothing is enabled. Equivalent to `ProjectSettings.rendering/2d/snap/snap_2d_vertices_to_pixel`.

### ViewportSetTextureMipmapBias

```csharp
public static void ViewportSetTextureMipmapBias(Rid viewport, float mipmapBias)
```

引擎名：`viewport_set_texture_mipmap_bias`

Affects the final texture sharpness by reading from a lower or higher mipmap (also called "texture LOD bias"). Negative values make mipmapped textures sharper but grainier when viewed at a distance, while positive values make mipmapped textures blurrier (even when up close). To get sharper textures at a distance without introducing too much graininess, set this between `-0.75` and `0.0`. Enabling temporal antialiasing (`ProjectSettings.rendering/anti_aliasing/quality/use_taa`) can help reduce the graininess visible when using negative mipmap bias.

**Note:** When the 3D scaling mode is set to FSR 1.0, this value is used to adjust the automatic mipmap bias which is calculated internally based on the scale factor. The formula for this is `-log2(1.0 / scale) + mipmap_bias`.

**Note:** This method is only supported in the Forward+ and Mobile renderers, not Compatibility. In Compatibility, this method is always treated as if `mipmapBias` was set to `0.0`.

### ViewportSetTransparentBackground

```csharp
public static void ViewportSetTransparentBackground(Rid viewport, bool enabled)
```

引擎名：`viewport_set_transparent_background`

If `true`, the viewport renders its background as transparent.

### ViewportSetUpdateMode

```csharp
public static void ViewportSetUpdateMode(Rid viewport, RenderingServer.ViewportUpdateMode updateMode)
```

引擎名：`viewport_set_update_mode`

Sets when the viewport should be updated.

### ViewportSetUseDebanding

```csharp
public static void ViewportSetUseDebanding(Rid viewport, bool enable)
```

引擎名：`viewport_set_use_debanding`

Equivalent to [`Viewport.UseDebanding`](Viewport.md). See also `ProjectSettings.rendering/anti_aliasing/quality/use_debanding`.

### ViewportSetUseHdr2D

```csharp
public static void ViewportSetUseHdr2D(Rid viewport, bool enabled)
```

If `true`, 2D rendering will use a high dynamic range (HDR) `RGBA16` format framebuffer. Additionally, 2D rendering will be performed on linear values and will be converted using the appropriate transfer function immediately before blitting to the screen (if the Viewport is attached to the screen).

Practically speaking, this means that the end result of the Viewport will not be clamped to the `0-1` range and can be used in 3D rendering without color encoding adjustments. This allows 2D rendering to take advantage of effects requiring high dynamic range (e.g. 2D glow) as well as substantially improves the appearance of effects requiring highly detailed gradients. This setting has the same effect as [`Viewport.UseHdr2D`](Viewport.md).

### ViewportSetUseOcclusionCulling

```csharp
public static void ViewportSetUseOcclusionCulling(Rid viewport, bool enable)
```

引擎名：`viewport_set_use_occlusion_culling`

If `true`, enables occlusion culling on the specified viewport. Equivalent to `ProjectSettings.rendering/occlusion_culling/use_occlusion_culling`.

### ViewportSetUseTaa

```csharp
public static void ViewportSetUseTaa(Rid viewport, bool enable)
```

引擎名：`viewport_set_use_taa`

If `true`, use temporal antialiasing. Equivalent to `ProjectSettings.rendering/anti_aliasing/quality/use_taa` or [`Viewport.UseTaa`](Viewport.md).

### ViewportSetUseXR

```csharp
public static void ViewportSetUseXR(Rid viewport, bool useXR)
```

引擎名：`viewport_set_use_xr`

If `true`, the viewport uses augmented or virtual reality technologies. See [`XRInterface`](XRInterface.md).

### ViewportSetVrsMode

```csharp
public static void ViewportSetVrsMode(Rid viewport, RenderingServer.ViewportVrsMode mode)
```

引擎名：`viewport_set_vrs_mode`

Sets the Variable Rate Shading (VRS) mode for the viewport. If the GPU does not support VRS, this property is ignored. Equivalent to `ProjectSettings.rendering/vrs/mode`.

### ViewportSetVrsTexture

```csharp
public static void ViewportSetVrsTexture(Rid viewport, Rid texture)
```

引擎名：`viewport_set_vrs_texture`

The texture to use when the VRS mode is set to [`RenderingServer.ViewportVrsMode.Texture`](RenderingServer.md). Equivalent to `ProjectSettings.rendering/vrs/texture`.

### ViewportSetVrsUpdateMode

```csharp
public static void ViewportSetVrsUpdateMode(Rid viewport, RenderingServer.ViewportVrsUpdateMode mode)
```

引擎名：`viewport_set_vrs_update_mode`

Sets the update mode for Variable Rate Shading (VRS) for the viewport. VRS requires the input texture to be converted to the format usable by the VRS method supported by the hardware. The update mode defines how often this happens. If the GPU does not support VRS, or VRS is not enabled, this property is ignored.

If set to [`RenderingServer.ViewportVrsUpdateMode.Once`](RenderingServer.md), the input texture is copied once and the mode is changed to [`RenderingServer.ViewportVrsUpdateMode.Disabled`](RenderingServer.md).

### VisibilityNotifierCreate

```csharp
public static Rid VisibilityNotifierCreate()
```

引擎名：`visibility_notifier_create`

Creates a new 3D visibility notifier object and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `visibility_notifier_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this notifier to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`VisibleOnScreenNotifier3D`](VisibleOnScreenNotifier3D.md).

### VisibilityNotifierSetAabb

```csharp
public static void VisibilityNotifierSetAabb(Rid notifier, Aabb aabb)
```

引擎名：`visibility_notifier_set_aabb`

Sets the AABB of the specified visibility notifier.

### VisibilityNotifierSetCallbacks

```csharp
public static void VisibilityNotifierSetCallbacks(Rid notifier, Callable enterCallable, Callable exitCallable)
```

引擎名：`visibility_notifier_set_callbacks`

Sets the methods to be called when the notifier enters or exits the view.

### VoxelGIAllocateData

```csharp
public static void VoxelGIAllocateData(Rid voxelGI, Transform3D toCellXform, Aabb aabb, Vector3I octreeSize, ReadOnlySpan<byte> octreeCells, ReadOnlySpan<byte> dataCells, ReadOnlySpan<byte> distanceField, ReadOnlySpan<int> levelCounts)
```

引擎名：`voxel_gi_allocate_data`

Allocates and initializes the voxel GI data for the specified `voxelGI` RID. `octreeCells` must be a multiple of 32. `octreeCells` must be double the size of `dataCells`. The allocated data can be retrieved later using the various `voxel_gi_get_*` methods.

### VoxelGIAllocateData

```csharp
public static void VoxelGIAllocateData(Rid voxelGI, Transform3D toCellXform, Aabb aabb, Vector3I octreeSize, byte[] octreeCells, byte[] dataCells, byte[] distanceField, int[] levelCounts)
```

引擎名：`voxel_gi_allocate_data`

Allocates and initializes the voxel GI data for the specified `voxelGI` RID. `octreeCells` must be a multiple of 32. `octreeCells` must be double the size of `dataCells`. The allocated data can be retrieved later using the various `voxel_gi_get_*` methods.

### VoxelGICreate

```csharp
public static Rid VoxelGICreate()
```

引擎名：`voxel_gi_create`

Creates a new voxel-based global illumination object and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `voxel_gi_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`VoxelGI`](VoxelGI.md).

### VoxelGIGetDataCells

```csharp
public static byte[] VoxelGIGetDataCells(Rid voxelGI)
```

引擎名：`voxel_gi_get_data_cells`

Returns the data cells for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGIGetDistanceField

```csharp
public static byte[] VoxelGIGetDistanceField(Rid voxelGI)
```

引擎名：`voxel_gi_get_distance_field`

Returns the distance field data for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGIGetLevelCounts

```csharp
public static int[] VoxelGIGetLevelCounts(Rid voxelGI)
```

引擎名：`voxel_gi_get_level_counts`

Returns the level counts for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGIGetOctreeCells

```csharp
public static byte[] VoxelGIGetOctreeCells(Rid voxelGI)
```

引擎名：`voxel_gi_get_octree_cells`

Returns the octree cell data for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGIGetOctreeSize

```csharp
public static Vector3I VoxelGIGetOctreeSize(Rid voxelGI)
```

引擎名：`voxel_gi_get_octree_size`

Returns the octree size for the specified voxel GI data instance, which corresponds to the number of subdivisions per axis. This can be viewed in the editor by hovering the **Bake VoxelGI** button at the top of the 3D editor viewport when a [`VoxelGI`](VoxelGI.md) node is selected and looking at the **Subdivisions** field in the tooltip.

### VoxelGIGetToCellXform

```csharp
public static Transform3D VoxelGIGetToCellXform(Rid voxelGI)
```

引擎名：`voxel_gi_get_to_cell_xform`

Returns the transform to cell space for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGISetBakedExposureNormalization

```csharp
public static void VoxelGISetBakedExposureNormalization(Rid voxelGI, float bakedExposure)
```

引擎名：`voxel_gi_set_baked_exposure_normalization`

Used to inform the renderer what exposure normalization value was used while baking the voxel gi. This value will be used and modulated at run time to ensure that the voxel gi maintains a consistent level of exposure even if the scene-wide exposure normalization is changed at run time. For more information see `CameraAttributesSetExposure()`.

### VoxelGISetBias

```csharp
public static void VoxelGISetBias(Rid voxelGI, float bias)
```

引擎名：`voxel_gi_set_bias`

Sets the [`VoxelGIData.Bias`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetDynamicRange

```csharp
public static void VoxelGISetDynamicRange(Rid voxelGI, float range)
```

引擎名：`voxel_gi_set_dynamic_range`

Sets the [`VoxelGIData.DynamicRange`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetEnergy

```csharp
public static void VoxelGISetEnergy(Rid voxelGI, float energy)
```

引擎名：`voxel_gi_set_energy`

Sets the [`VoxelGIData.Energy`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetInterior

```csharp
public static void VoxelGISetInterior(Rid voxelGI, bool enable)
```

引擎名：`voxel_gi_set_interior`

Sets the [`VoxelGIData.Interior`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetNormalBias

```csharp
public static void VoxelGISetNormalBias(Rid voxelGI, float bias)
```

引擎名：`voxel_gi_set_normal_bias`

Sets the [`VoxelGIData.NormalBias`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetPropagation

```csharp
public static void VoxelGISetPropagation(Rid voxelGI, float amount)
```

引擎名：`voxel_gi_set_propagation`

Sets the [`VoxelGIData.Propagation`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetQuality

```csharp
public static void VoxelGISetQuality(RenderingServer.VoxelGIQuality quality)
```

引擎名：`voxel_gi_set_quality`

Sets the `ProjectSettings.rendering/global_illumination/voxel_gi/quality` value to use when rendering. This parameter is global and cannot be set on a per-VoxelGI basis.

### VoxelGISetUseTwoBounces

```csharp
public static void VoxelGISetUseTwoBounces(Rid voxelGI, bool enable)
```

引擎名：`voxel_gi_set_use_two_bounces`

Sets the [`VoxelGIData.UseTwoBounces`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

## 事件（信号）

### FramePostDraw

```csharp
public event Action FramePostDraw
```

引擎名：`frame_post_draw`

Emitted at the end of the frame, after the RenderingServer has finished updating all the Viewports.

### FramePreDraw

```csharp
public event Action FramePreDraw
```

引擎名：`frame_pre_draw`

Emitted at the beginning of the frame, before the RenderingServer updates all the Viewports.

## 常量

### ArrayCustomCount

```csharp
public const long ArrayCustomCount = 4
```

The number of custom data arrays available ([`RenderingServer.ArrayType.Custom0`](RenderingServer.md), [`RenderingServer.ArrayType.Custom1`](RenderingServer.md), [`RenderingServer.ArrayType.Custom2`](RenderingServer.md), [`RenderingServer.ArrayType.Custom3`](RenderingServer.md)).

### ArrayWeightsSize

```csharp
public const long ArrayWeightsSize = 4
```

Number of weights/bones per vertex.

### CanvasItemZMax

```csharp
public const long CanvasItemZMax = 4096
```

The maximum Z-layer for canvas items.

### CanvasItemZMin

```csharp
public const long CanvasItemZMin = -4096
```

The minimum Z-layer for canvas items.

### CanvasLayerMax

```csharp
public const long CanvasLayerMax = 2147483647
```

The maximum canvas layer.

### CanvasLayerMin

```csharp
public const long CanvasLayerMin = -2147483648
```

The minimum canvas layer.

### MaterialRenderPriorityMax

```csharp
public const long MaterialRenderPriorityMax = 127
```

The maximum renderpriority of all materials.

### MaterialRenderPriorityMin

```csharp
public const long MaterialRenderPriorityMin = -128
```

The minimum renderpriority of all materials.

### Max2DDirectionalLights

```csharp
public const long Max2DDirectionalLights = 8
```

The maximum number of directional lights that can be rendered at a given time in 2D.

### MaxCursors

```csharp
public const long MaxCursors = 8
```

### MaxGlowLevels

```csharp
public const long MaxGlowLevels = 7
```

The maximum number of glow levels that can be used with the glow post-processing effect.

### MaxMeshSurfaces

```csharp
public const long MaxMeshSurfaces = 256
```

The maximum number of surfaces a mesh can have.

### NoIndexArray

```csharp
public const long NoIndexArray = -1
```

Marks an error that shows that the index array is empty.

### ParticlesEmitFlagColor

```csharp
public const long ParticlesEmitFlagColor = 8
```

Particle starts with specified color.

### ParticlesEmitFlagCustom

```csharp
public const long ParticlesEmitFlagCustom = 16
```

Particle starts with specified `CUSTOM` data.

### ParticlesEmitFlagPosition

```csharp
public const long ParticlesEmitFlagPosition = 1
```

Particle starts at the specified position.

### ParticlesEmitFlagRotationScale

```csharp
public const long ParticlesEmitFlagRotationScale = 2
```

Particle starts with specified rotation and scale.

### ParticlesEmitFlagVelocity

```csharp
public const long ParticlesEmitFlagVelocity = 4
```

Particle starts with the specified velocity vector, which defines the emission direction and speed.

## ArrayCustomFormat

命名空间 `Godot`；enum

### 枚举值

- `Rgba8Unorm = 0`：Custom data array contains 8-bit-per-channel red/green/blue/alpha color data.
- `Rgba8Snorm = 1`：Custom data array contains 8-bit-per-channel red/green/blue/alpha color data.
- `RgHalf = 2`：Custom data array contains 16-bit-per-channel red/green color data.
- `RgbaHalf = 3`：Custom data array contains 16-bit-per-channel red/green/blue/alpha color data.
- `RFloat = 4`：Custom data array contains 32-bit-per-channel red color data.
- `RgFloat = 5`：Custom data array contains 32-bit-per-channel red/green color data.
- `RgbFloat = 6`：Custom data array contains 32-bit-per-channel red/green/blue color data.
- `RgbaFloat = 7`：Custom data array contains 32-bit-per-channel red/green/blue/alpha color data.
- `Max = 8`：Represents the size of the `RenderingServer.ArrayCustomFormat` enum.

## ArrayFormat

命名空间 `Godot`；enum

### 枚举值

- `FormatVertex = 1`：Flag used to mark a vertex position array.
- `FormatNormal = 2`：Flag used to mark a normal array.
- `FormatTangent = 4`：Flag used to mark a tangent array.
- `FormatColor = 8`：Flag used to mark a vertex color array.
- `FormatTexUV = 16`：Flag used to mark a UV coordinates array.
- `FormatTexUV2 = 32`：Flag used to mark a UV coordinates array for the second UV coordinates.
- `FormatCustom0 = 64`：Flag used to mark an array of custom per-vertex data for the first set of custom data.
- `FormatCustom1 = 128`：Flag used to mark an array of custom per-vertex data for the second set of custom data.
- `FormatCustom2 = 256`：Flag used to mark an array of custom per-vertex data for the third set of custom data.
- `FormatCustom3 = 512`：Flag used to mark an array of custom per-vertex data for the fourth set of custom data.
- `FormatBones = 1024`：Flag used to mark a bone information array.
- `FormatWeights = 2048`：Flag used to mark a weights array.
- `FormatIndex = 4096`：Flag used to mark an index array.
- `FormatBlendShapeMask = 7`：Mask of mesh channels permitted in blend shapes.
- `FormatCustomBase = 13`：Shift of first custom channel.
- `FormatCustomBits = 3`：Number of format bits per custom channel.
- `FormatCustom0Shift = 13`：Amount to shift [`RenderingServer.ArrayCustomFormat`](RenderingServer.md) for custom channel index 0.
- `FormatCustom1Shift = 16`：Amount to shift [`RenderingServer.ArrayCustomFormat`](RenderingServer.md) for custom channel index 1.
- `FormatCustom2Shift = 19`：Amount to shift [`RenderingServer.ArrayCustomFormat`](RenderingServer.md) for custom channel index 2.
- `FormatCustom3Shift = 22`：Amount to shift [`RenderingServer.ArrayCustomFormat`](RenderingServer.md) for custom channel index 3.
- `FormatCustomMask = 7`：Mask of custom format bits per custom channel.
- `CompressFlagsBase = 25`：Shift of first compress flag.
- `FlagUse2DVertices = 33554432`：Flag used to mark that the array contains 2D vertices.
- `FlagUseDynamicUpdate = 67108864`：Flag used to mark that the mesh data will use `GL_DYNAMIC_DRAW` on GLES.
- `FlagUse8BoneWeights = 134217728`：Flag used to mark that the array uses 8 bone weights instead of 4.
- `FlagUsesEmptyVertexArray = 268435456`：Flag used to mark that the mesh does not have a vertex array and instead will infer vertex positions in the shader using indices and other information.
- `FlagCompressAttributes = 536870912`：Flag used to mark that a mesh is using compressed attributes (vertices, normals, tangents, UVs).
- `FlagFormatVersionBase = 35`：Flag used to mark the start of the bits used to store the mesh version.
- `FlagFormatVersionShift = 35`：Flag used to shift a mesh format int to bring the version into the lowest digits.
- `FlagFormatVersion1 = 0`：Flag used to record the format used by prior mesh versions before the introduction of a version.
- `FlagFormatVersion2 = 34359738368`：Flag used to record the second iteration of the mesh version flag.
- `FlagFormatCurrentVersion = 34359738368`：Flag used to record the current version that the engine expects.
- `FlagFormatVersionMask = 255`：Flag used to isolate the bits used for mesh version after using `FlagFormatVersionShift` to shift them into place.

## ArrayType

命名空间 `Godot`；enum

### 枚举值

- `Vertex = 0`：Array is a vertex position array.
- `Normal = 1`：Array is a normal array.
- `Tangent = 2`：Array is a tangent array.
- `Color = 3`：Array is a vertex color array.
- `TexUV = 4`：Array is a UV coordinates array.
- `TexUV2 = 5`：Array is a UV coordinates array for the second set of UV coordinates.
- `Custom0 = 6`：Array is a custom data array for the first set of custom data.
- `Custom1 = 7`：Array is a custom data array for the second set of custom data.
- `Custom2 = 8`：Array is a custom data array for the third set of custom data.
- `Custom3 = 9`：Array is a custom data array for the fourth set of custom data.
- `Bones = 10`：Array contains bone information.
- `Weights = 11`：Array is weight information.
- `Index = 12`：Array is an index array.
- `Max = 13`：Represents the size of the `RenderingServer.ArrayType` enum.

## BakeChannels

命名空间 `Godot`；enum

### 枚举值

- `AlbedoAlpha = 0`：Index of [`Image`](Image.md) in array of [`Image`](Image.md)s returned by [`RenderingServer.BakeRenderUV2`](RenderingServer.md).
- `Normal = 1`：Index of [`Image`](Image.md) in array of [`Image`](Image.md)s returned by [`RenderingServer.BakeRenderUV2`](RenderingServer.md).
- `Orm = 2`：Index of [`Image`](Image.md) in array of [`Image`](Image.md)s returned by [`RenderingServer.BakeRenderUV2`](RenderingServer.md).
- `Emission = 3`：Index of [`Image`](Image.md) in array of [`Image`](Image.md)s returned by [`RenderingServer.BakeRenderUV2`](RenderingServer.md).

## BlendShapeMode

命名空间 `Godot`；enum

### 枚举值

- `Normalized = 0`：Blend shapes are normalized.
- `Relative = 1`：Blend shapes are relative to base weight.

## CanvasGroupMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Child draws over parent and is not clipped.
- `ClipOnly = 1`：Parent is used for the purposes of clipping only.
- `ClipAndDraw = 2`：Parent is used for clipping child, but parent is also drawn underneath child as normal before clipping child to its visible area.
- `Transparent = 3`

## CanvasItemTextureFilter

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Uses the default filter mode for this [`Viewport`](Viewport.md).
- `Nearest = 1`：The texture filter reads from the nearest pixel only.
- `Linear = 2`：The texture filter blends between the nearest 4 pixels.
- `NearestWithMipmaps = 3`：The texture filter reads from the nearest pixel and blends between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `LinearWithMipmaps = 4`：The texture filter blends between the nearest 4 pixels and between the nearest 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`).
- `NearestWithMipmapsAnisotropic = 5`：The texture filter reads from the nearest pixel and blends between 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`) based on the angle bet…
- `LinearWithMipmapsAnisotropic = 6`：The texture filter blends between the nearest 4 pixels and blends between 2 mipmaps (or uses the nearest mipmap if `ProjectSettings.rendering/textures/default_filters/use_nearest_mipmap_filter` is `true`) based on the an…
- `Max = 7`：Max value for `RenderingServer.CanvasItemTextureFilter` enum.

## CanvasItemTextureRepeat

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Uses the default repeat mode for this [`Viewport`](Viewport.md).
- `Disabled = 1`：Disables textures repeating.
- `Enabled = 2`：Enables the texture to repeat when UV coordinates are outside the 0-1 range.
- `Mirror = 3`：Flip the texture when repeating so that the edge lines up instead of abruptly changing.
- `Max = 4`：Max value for `RenderingServer.CanvasItemTextureRepeat` enum.

## CanvasLightBlendMode

命名空间 `Godot`；enum

### 枚举值

- `Add = 0`：Adds light color additive to the canvas.
- `Sub = 1`：Adds light color subtractive to the canvas.
- `Mix = 2`：The light adds color depending on transparency.

## CanvasLightMode

命名空间 `Godot`；enum

### 枚举值

- `Point = 0`：2D point light (see [`PointLight2D`](PointLight2D.md)).
- `Directional = 1`：2D directional (sun/moon) light (see [`DirectionalLight2D`](DirectionalLight2D.md)).

## CanvasLightShadowFilter

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Do not apply a filter to canvas light shadows.
- `Pcf5 = 1`：Use PCF5 filtering to filter canvas light shadows.
- `Pcf13 = 2`：Use PCF13 filtering to filter canvas light shadows.
- `Max = 3`：Max value of the `RenderingServer.CanvasLightShadowFilter` enum.

## CanvasOccluderPolygonCullMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Culling of the canvas occluder is disabled.
- `Clockwise = 1`：Culling of the canvas occluder is clockwise.
- `CounterClockwise = 2`：Culling of the canvas occluder is counterclockwise.

## CanvasTextureChannel

命名空间 `Godot`；enum

### 枚举值

- `Diffuse = 0`：Diffuse canvas texture ([`CanvasTexture.DiffuseTexture`](CanvasTexture.md)).
- `Normal = 1`：Normal map canvas texture ([`CanvasTexture.NormalTexture`](CanvasTexture.md)).
- `Specular = 2`：Specular map canvas texture ([`CanvasTexture.SpecularTexture`](CanvasTexture.md)).

## CompositorEffectCallbackType

命名空间 `Godot`；enum

### 枚举值

- `PreOpaque = 0`：The callback is called before our opaque rendering pass, but after depth prepass (if applicable).
- `PostOpaque = 1`：The callback is called after our opaque rendering pass, but before our sky is rendered.
- `PostSky = 2`：The callback is called after our sky is rendered, but before our back buffers are created (and if enabled, before subsurface scattering and/or screen space reflections).
- `PreTransparent = 3`：The callback is called before our transparent rendering pass, but after our sky is rendered and we've created our back buffers.
- `PostTransparent = 4`：The callback is called after our transparent rendering pass, but before any built-in post-processing effects and output to our render target.
- `Any = -1`

## CompositorEffectFlags

命名空间 `Godot`；enum

### 枚举值

- `AccessResolvedColor = 1`：The rendering effect requires the color buffer to be resolved if MSAA is enabled.
- `AccessResolvedDepth = 2`：The rendering effect requires the depth buffer to be resolved if MSAA is enabled.
- `NeedsMotionVectors = 4`：The rendering effect requires motion vectors to be produced.
- `NeedsRoughness = 8`：The rendering effect requires normals and roughness g-buffer to be produced (Forward+ only).
- `NeedsSeparateSpecular = 16`：The rendering effect requires specular data to be separated out (Forward+ only).

## CubeMapLayer

命名空间 `Godot`；enum

### 枚举值

- `Left = 0`：Left face of a [`Cubemap`](Cubemap.md).
- `Right = 1`：Right face of a [`Cubemap`](Cubemap.md).
- `Bottom = 2`：Bottom face of a [`Cubemap`](Cubemap.md).
- `Top = 3`：Top face of a [`Cubemap`](Cubemap.md).
- `Front = 4`：Front face of a [`Cubemap`](Cubemap.md).
- `Back = 5`：Back face of a [`Cubemap`](Cubemap.md).

## DecalFilter

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：Nearest-neighbor filter for decals (use for pixel art decals).
- `Linear = 1`：Linear filter for decals (use for non-pixel art decals).
- `NearestMipmaps = 2`：Nearest-neighbor filter for decals (use for pixel art decals).
- `LinearMipmaps = 3`：Linear filter for decals (use for non-pixel art decals).
- `NearestMipmapsAnisotropic = 4`：Nearest-neighbor filter for decals (use for pixel art decals).
- `LinearMipmapsAnisotropic = 5`：Linear filter for decals (use for non-pixel art decals).

## DecalTexture

命名空间 `Godot`；enum

### 枚举值

- `Albedo = 0`：Albedo texture slot in a decal ([`Decal.TextureAlbedo`](Decal.md)).
- `Normal = 1`：Normal map texture slot in a decal ([`Decal.TextureNormal`](Decal.md)).
- `Orm = 2`：Occlusion/Roughness/Metallic texture slot in a decal ([`Decal.TextureOrm`](Decal.md)).
- `Emission = 3`：Emission texture slot in a decal ([`Decal.TextureEmission`](Decal.md)).
- `Max = 4`：Represents the size of the `RenderingServer.DecalTexture` enum.

## DofBlurQuality

命名空间 `Godot`；enum

### 枚举值

- `VeryLow = 0`：Lowest quality DOF blur.
- `Low = 1`：Low quality DOF blur.
- `Medium = 2`：Medium quality DOF blur.
- `High = 3`：Highest quality DOF blur.

## DofBokehShape

命名空间 `Godot`；enum

### 枚举值

- `Box = 0`：Calculate the DOF blur using a box filter.
- `Hexagon = 1`：Calculates DOF blur using a hexagon shaped filter.
- `Circle = 2`：Calculates DOF blur using a circle shaped filter.

## EnvironmentAmbientSource

命名空间 `Godot`；enum

### 枚举值

- `Bg = 0`：Gather ambient light from whichever source is specified as the background.
- `Disabled = 1`：Disable ambient light.
- `Color = 2`：Specify a specific [`Color`](Color.md) for ambient light.
- `Sky = 3`：Gather ambient light from the [`Sky`](Sky.md) regardless of what the background is.

## EnvironmentBG

命名空间 `Godot`；enum

### 枚举值

- `ClearColor = 0`：Use the clear color as background.
- `Color = 1`：Use a specified color as the background.
- `Sky = 2`：Use a sky resource for the background.
- `Canvas = 3`：Use a specified canvas layer as the background.
- `Keep = 4`：Do not clear the background, use whatever was rendered last frame as the background.
- `CameraFeed = 5`：Displays a camera feed in the background.
- `Max = 6`：Represents the size of the `RenderingServer.EnvironmentBG` enum.

## EnvironmentFogMode

命名空间 `Godot`；enum

### 枚举值

- `Exponential = 0`：Use a physically-based fog model defined primarily by fog density.
- `Depth = 1`：Use a simple fog model defined by start and end positions and a custom curve.

## EnvironmentGlowBlendMode

命名空间 `Godot`；enum

### 枚举值

- `Additive = 0`：Adds the glow effect to the scene.
- `Screen = 1`：Adds the glow effect to the scene after modifying the glow influence based on the scene value; dark values will be highly influenced by glow and bright values will not be influenced by glow.
- `Softlight = 2`：Adds the glow effect to the tonemapped image after modifying the glow influence based on the image value; dark values and bright values will not be influenced by glow and mid-range values will be highly influenced by glo…
- `Replace = 3`：Replaces all pixels' color by the glow effect.
- `Mix = 4`：Mixes the glow image with the scene image.

## EnvironmentReflectionSource

命名空间 `Godot`；enum

### 枚举值

- `Bg = 0`：Use the background for reflections.
- `Disabled = 1`：Disable reflections.
- `Sky = 2`：Use the [`Sky`](Sky.md) for reflections regardless of what the background is.

## EnvironmentSdfgiFramesToConverge

命名空间 `Godot`；enum

### 枚举值

- `In5Frames = 0`：Converge SDFGI over 5 frames.
- `In10Frames = 1`：Configure SDFGI to fully converge over 10 frames.
- `In15Frames = 2`：Configure SDFGI to fully converge over 15 frames.
- `In20Frames = 3`：Configure SDFGI to fully converge over 20 frames.
- `In25Frames = 4`：Configure SDFGI to fully converge over 25 frames.
- `In30Frames = 5`：Configure SDFGI to fully converge over 30 frames.
- `Max = 6`：Represents the size of the `RenderingServer.EnvironmentSdfgiFramesToConverge` enum.

## EnvironmentSdfgiFramesToUpdateLight

命名空间 `Godot`；enum

### 枚举值

- `In1Frame = 0`：Update indirect light from dynamic lights in SDFGI over 1 frame.
- `In2Frames = 1`：Update indirect light from dynamic lights in SDFGI over 2 frames.
- `In4Frames = 2`：Update indirect light from dynamic lights in SDFGI over 4 frames.
- `In8Frames = 3`：Update indirect light from dynamic lights in SDFGI over 8 frames.
- `In16Frames = 4`：Update indirect light from dynamic lights in SDFGI over 16 frames.
- `Max = 5`：Represents the size of the `RenderingServer.EnvironmentSdfgiFramesToUpdateLight` enum.

## EnvironmentSdfgiRayCount

命名空间 `Godot`；enum

### 枚举值

- `Count4 = 0`：Throw 4 rays per frame when converging SDFGI.
- `Count8 = 1`：Throw 8 rays per frame when converging SDFGI.
- `Count16 = 2`：Throw 16 rays per frame when converging SDFGI.
- `Count32 = 3`：Throw 32 rays per frame when converging SDFGI.
- `Count64 = 4`：Throw 64 rays per frame when converging SDFGI.
- `Count96 = 5`：Throw 96 rays per frame when converging SDFGI.
- `Count128 = 6`：Throw 128 rays per frame when converging SDFGI.
- `Max = 7`：Represents the size of the `RenderingServer.EnvironmentSdfgiRayCount` enum.

## EnvironmentSdfgiyScale

命名空间 `Godot`；enum

### 枚举值

- `Scale50Percent = 0`：Use 50% scale for SDFGI on the Y (vertical) axis.
- `Scale75Percent = 1`：Use 75% scale for SDFGI on the Y (vertical) axis.
- `Scale100Percent = 2`：Use 100% scale for SDFGI on the Y (vertical) axis.

## EnvironmentSsaoQuality

命名空间 `Godot`；enum

### 枚举值

- `VeryLow = 0`：Lowest quality of screen-space ambient occlusion.
- `Low = 1`：Low quality screen-space ambient occlusion.
- `Medium = 2`：Medium quality screen-space ambient occlusion.
- `High = 3`：High quality screen-space ambient occlusion.
- `Ultra = 4`：Highest quality screen-space ambient occlusion.

## EnvironmentSsilQuality

命名空间 `Godot`；enum

### 枚举值

- `VeryLow = 0`：Lowest quality of screen-space indirect lighting.
- `Low = 1`：Low quality screen-space indirect lighting.
- `Medium = 2`：High quality screen-space indirect lighting.
- `High = 3`：High quality screen-space indirect lighting.
- `Ultra = 4`：Highest quality screen-space indirect lighting.

## EnvironmentSsrRoughnessQuality

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Lowest quality of roughness filter for screen-space reflections.
- `Low = 1`：Low quality of roughness filter for screen-space reflections.
- `Medium = 2`：Medium quality of roughness filter for screen-space reflections.
- `High = 3`：High quality of roughness filter for screen-space reflections.

## EnvironmentToneMapper

命名空间 `Godot`；enum

### 枚举值

- `Linear = 0`：Does not modify color data, resulting in a linear tonemapping curve which unnaturally clips bright values, causing bright lighting to look blown out.
- `Reinhard = 1`：A simple tonemapping curve that rolls off bright values to prevent clipping.
- `Filmic = 2`：Uses a film-like tonemapping curve to prevent clipping of bright values and provide better contrast than `Reinhard`.
- `Aces = 3`：Uses a high-contrast film-like tonemapping curve and desaturates bright values for a more realistic appearance.
- `Agx = 4`：Uses an adjustable film-like tonemapping curve and desaturates bright values for a more realistic appearance.

## Features

命名空间 `Godot`；enum

### 枚举值

- `Shaders = 0`
- `Multithreaded = 1`

## FogVolumeShape

命名空间 `Godot`；enum

### 枚举值

- `Ellipsoid = 0`：[`FogVolume`](FogVolume.md) will be shaped like an ellipsoid (stretched sphere).
- `Cone = 1`：[`FogVolume`](FogVolume.md) will be shaped like a cone pointing upwards (in local coordinates).
- `Cylinder = 2`：[`FogVolume`](FogVolume.md) will be shaped like an upright cylinder (in local coordinates).
- `Box = 3`：[`FogVolume`](FogVolume.md) will be shaped like a box.
- `World = 4`：[`FogVolume`](FogVolume.md) will have no shape, will cover the whole world and will not be culled.
- `Max = 5`：Represents the size of the `RenderingServer.FogVolumeShape` enum.

## GlobalShaderParameterType

命名空间 `Godot`；enum

### 枚举值

- `Bool = 0`：Boolean global shader parameter (`global uniform bool ...`).
- `Bvec2 = 1`：2-dimensional boolean vector global shader parameter (`global uniform bvec2 ...`).
- `Bvec3 = 2`：3-dimensional boolean vector global shader parameter (`global uniform bvec3 ...`).
- `Bvec4 = 3`：4-dimensional boolean vector global shader parameter (`global uniform bvec4 ...`).
- `Int = 4`：Integer global shader parameter (`global uniform int ...`).
- `Ivec2 = 5`：2-dimensional integer vector global shader parameter (`global uniform ivec2 ...`).
- `Ivec3 = 6`：3-dimensional integer vector global shader parameter (`global uniform ivec3 ...`).
- `Ivec4 = 7`：4-dimensional integer vector global shader parameter (`global uniform ivec4 ...`).
- `Rect2I = 8`：2-dimensional integer rectangle global shader parameter (`global uniform ivec4 ...`).
- `Uint = 9`：Unsigned integer global shader parameter (`global uniform uint ...`).
- `Uvec2 = 10`：2-dimensional unsigned integer vector global shader parameter (`global uniform uvec2 ...`).
- `Uvec3 = 11`：3-dimensional unsigned integer vector global shader parameter (`global uniform uvec3 ...`).
- `Uvec4 = 12`：4-dimensional unsigned integer vector global shader parameter (`global uniform uvec4 ...`).
- `Float = 13`：Single-precision floating-point global shader parameter (`global uniform float ...`).
- `Vec2 = 14`：2-dimensional floating-point vector global shader parameter (`global uniform vec2 ...`).
- `Vec3 = 15`：3-dimensional floating-point vector global shader parameter (`global uniform vec3 ...`).
- `Vec4 = 16`：4-dimensional floating-point vector global shader parameter (`global uniform vec4 ...`).
- `Color = 17`：Color global shader parameter (`global uniform vec4 ...`).
- `Rect2 = 18`：2-dimensional floating-point rectangle global shader parameter (`global uniform vec4 ...`).
- `Mat2 = 19`：2×2 matrix global shader parameter (`global uniform mat2 ...`).
- `Mat3 = 20`：3×3 matrix global shader parameter (`global uniform mat3 ...`).
- `Mat4 = 21`：4×4 matrix global shader parameter (`global uniform mat4 ...`).
- `Transform2D = 22`：2-dimensional transform global shader parameter (`global uniform mat2x3 ...`).
- `Transform = 23`：3-dimensional transform global shader parameter (`global uniform mat3x4 ...`).
- `Sampler2D = 24`：2D sampler global shader parameter (`global uniform sampler2D ...`).
- `Sampler2Darray = 25`：2D sampler array global shader parameter (`global uniform sampler2DArray ...`).
- `Sampler3D = 26`：3D sampler global shader parameter (`global uniform sampler3D ...`).
- `Samplercube = 27`：Cubemap sampler global shader parameter (`global uniform samplerCube ...`).
- `Samplerext = 28`：External sampler global shader parameter (`global uniform samplerExternalOES ...`).
- `Max = 29`：Represents the size of the `RenderingServer.GlobalShaderParameterType` enum.

## InstanceFlags

命名空间 `Godot`；enum

### 枚举值

- `UseBakedLight = 0`：Allows the instance to be used in baked lighting.
- `UseDynamicGI = 1`：Allows the instance to be used with dynamic global illumination.
- `DrawNextFrameIfVisible = 2`：When set, manually requests to draw geometry on next frame.
- `IgnoreOcclusionCulling = 3`：Always draw, even if the instance would be culled by occlusion culling.
- `Max = 4`：Represents the size of the `RenderingServer.InstanceFlags` enum.

## InstanceType

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The instance does not have a type.
- `Mesh = 1`：The instance is a mesh.
- `Multimesh = 2`：The instance is a multimesh.
- `Particles = 3`：The instance is a particle emitter.
- `ParticlesCollision = 4`：The instance is a GPUParticles collision shape.
- `Light = 5`：The instance is a light.
- `ReflectionProbe = 6`：The instance is a reflection probe.
- `Decal = 7`：The instance is a decal.
- `VoxelGI = 8`：The instance is a VoxelGI.
- `Lightmap = 9`：The instance is a lightmap.
- `Occluder = 10`：The instance is an occlusion culling occluder.
- `VisiblityNotifier = 11`：The instance is a visible on-screen notifier.
- `FogVolume = 12`：The instance is a fog volume.
- `Max = 13`：Represents the size of the `RenderingServer.InstanceType` enum.
- `GeometryMask = 14`：A combination of the flags of geometry instances (mesh, multimesh, immediate and particles).

## LightBakeMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Light is ignored when baking.
- `Static = 1`：Light is taken into account in static baking ([`VoxelGI`](VoxelGI.md), [`LightmapGI`](LightmapGI.md), SDFGI ([`Environment.SdfgiEnabled`](Environment.md))).
- `Dynamic = 2`：Light is taken into account in dynamic baking ([`VoxelGI`](VoxelGI.md) and SDFGI ([`Environment.SdfgiEnabled`](Environment.md)) only).

## LightDirectionalShadowMode

命名空间 `Godot`；enum

### 枚举值

- `Orthogonal = 0`：Use orthogonal shadow projection for directional light.
- `Parallel2Splits = 1`：Use 2 splits for shadow projection when using directional light.
- `Parallel4Splits = 2`：Use 4 splits for shadow projection when using directional light.

## LightDirectionalSkyMode

命名空间 `Godot`；enum

### 枚举值

- `LightAndSky = 0`：Use DirectionalLight3D in both sky rendering and scene lighting.
- `LightOnly = 1`：Only use DirectionalLight3D in scene lighting.
- `SkyOnly = 2`：Only use DirectionalLight3D in sky rendering.

## LightOmniShadowMode

命名空间 `Godot`；enum

### 枚举值

- `DualParaboloid = 0`：Use a dual paraboloid shadow map for omni lights.
- `Cube = 1`：Use a cubemap shadow map for omni lights.

## LightParam

命名空间 `Godot`；enum

### 枚举值

- `Energy = 0`：The light's energy multiplier.
- `IndirectEnergy = 1`：The light's indirect energy multiplier (final indirect energy is `Energy` * `IndirectEnergy`).
- `VolumetricFogEnergy = 2`：The light's volumetric fog energy multiplier (final volumetric fog energy is `Energy` * `VolumetricFogEnergy`).
- `Specular = 3`：The light's influence on specularity.
- `Range = 4`：The light's range.
- `Size = 5`：The size of the light when using spot light or omni light.
- `Attenuation = 6`：The light's attenuation.
- `SpotAngle = 7`：The spotlight's angle.
- `SpotAttenuation = 8`：The spotlight's attenuation.
- `ShadowMaxDistance = 9`：The maximum distance for shadow splits.
- `ShadowSplit1Offset = 10`：Proportion of shadow atlas occupied by the first split.
- `ShadowSplit2Offset = 11`：Proportion of shadow atlas occupied by the second split.
- `ShadowSplit3Offset = 12`：Proportion of shadow atlas occupied by the third split.
- `ShadowFadeStart = 13`：Proportion of shadow max distance where the shadow will start to fade out.
- `ShadowNormalBias = 14`：Normal bias used to offset shadow lookup by object normal.
- `ShadowBias = 15`：Bias for the shadow lookup to fix self-shadowing artifacts.
- `ShadowPancakeSize = 16`：Sets the size of the directional shadow pancake.
- `ShadowOpacity = 17`：The light's shadow opacity.
- `ShadowBlur = 18`：Blurs the edges of the shadow.
- `TransmittanceBias = 19`
- `Intensity = 20`：Constant representing the intensity of the light, measured in Lumens when dealing with a [`SpotLight3D`](SpotLight3D.md) or [`OmniLight3D`](OmniLight3D.md), or measured in Lux with a [`DirectionalLight3D`](DirectionalLig…
- `Max = 21`：Represents the size of the `RenderingServer.LightParam` enum.

## LightProjectorFilter

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：Nearest-neighbor filter for light projectors (use for pixel art light projectors).
- `Linear = 1`：Linear filter for light projectors (use for non-pixel art light projectors).
- `NearestMipmaps = 2`：Nearest-neighbor filter for light projectors (use for pixel art light projectors).
- `LinearMipmaps = 3`：Linear filter for light projectors (use for non-pixel art light projectors).
- `NearestMipmapsAnisotropic = 4`：Nearest-neighbor filter for light projectors (use for pixel art light projectors).
- `LinearMipmapsAnisotropic = 5`：Linear filter for light projectors (use for non-pixel art light projectors).

## LightType

命名空间 `Godot`；enum

### 枚举值

- `Directional = 0`：Directional (sun/moon) light (see [`DirectionalLight3D`](DirectionalLight3D.md)).
- `Omni = 1`：Omni light (see [`OmniLight3D`](OmniLight3D.md)).
- `Spot = 2`：Spot light (see [`SpotLight3D`](SpotLight3D.md)).
- `Area = 3`：Area light (see [`AreaLight3D`](AreaLight3D.md)).

## MultimeshPhysicsInterpolationQuality

命名空间 `Godot`；enum

### 枚举值

- `Fast = 0`：MultiMesh physics interpolation favors speed over quality.
- `High = 1`：MultiMesh physics interpolation favors quality over speed.

## MultimeshTransformFormat

命名空间 `Godot`；enum

### 枚举值

- `Transform2D = 0`：Use [`Transform2D`](Transform2D.md) to store MultiMesh transform.
- `Transform3D = 1`：Use [`Transform3D`](Transform3D.md) to store MultiMesh transform.

## NinePatchAxisMode

命名空间 `Godot`；enum

### 枚举值

- `Stretch = 0`：The nine patch gets stretched where needed.
- `Tile = 1`：The nine patch gets filled with tiles where needed.
- `TileFit = 2`：The nine patch gets filled with tiles where needed and stretches them a bit if needed.

## ParticlesCollisionHeightfieldResolution

命名空间 `Godot`；enum

### 枚举值

- `Resolution256 = 0`：256×256 heightfield resolution for [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md).
- `Resolution512 = 1`：512×512 heightfield resolution for [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md).
- `Resolution1024 = 2`：1024×1024 heightfield resolution for [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md).
- `Resolution2048 = 3`：2048×2048 heightfield resolution for [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md).
- `Resolution4096 = 4`：4096×4096 heightfield resolution for [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md).
- `Resolution8192 = 5`：8192×8192 heightfield resolution for [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md).
- `Max = 6`：Represents the size of the `RenderingServer.ParticlesCollisionHeightfieldResolution` enum.

## ParticlesCollisionType

命名空间 `Godot`；enum

### 枚举值

- `SphereAttract = 0`：Sphere attractor type for [`GpuParticles3D`](GpuParticles3D.md) (see [`GpuParticlesAttractorSphere3D`](GpuParticlesAttractorSphere3D.md)).
- `BoxAttract = 1`：Box attractor type for [`GpuParticles3D`](GpuParticles3D.md) (see [`GpuParticlesAttractorBox3D`](GpuParticlesAttractorBox3D.md)).
- `VectorFieldAttract = 2`：Vector field attractor type for [`GpuParticles3D`](GpuParticles3D.md) (see [`GpuParticlesAttractorVectorField3D`](GpuParticlesAttractorVectorField3D.md)).
- `SphereCollide = 3`：Sphere collision type for [`GpuParticles3D`](GpuParticles3D.md) (see [`GpuParticlesCollisionSphere3D`](GpuParticlesCollisionSphere3D.md)).
- `BoxCollide = 4`：Box collision type for [`GpuParticles3D`](GpuParticles3D.md) (see [`GpuParticlesCollisionBox3D`](GpuParticlesCollisionBox3D.md)).
- `SdfCollide = 5`：Signed distance field collision type for [`GpuParticles3D`](GpuParticles3D.md) (see [`GpuParticlesCollisionSdf3D`](GpuParticlesCollisionSdf3D.md)).
- `HeightfieldCollide = 6`：Heightfield collision type for [`GpuParticles3D`](GpuParticles3D.md) (see [`GpuParticlesCollisionHeightField3D`](GpuParticlesCollisionHeightField3D.md)).

## ParticlesDrawOrder

命名空间 `Godot`；enum

### 枚举值

- `Index = 0`：Draw particles in the order that they appear in the particles array.
- `Lifetime = 1`：Sort particles based on their lifetime.
- `ReverseLifetime = 2`：Sort particles based on the inverse of their lifetime.
- `ViewDepth = 3`：Sort particles based on their distance to the camera.

## ParticlesMode

命名空间 `Godot`；enum

### 枚举值

- `Mode2D = 0`：2D particles.
- `Mode3D = 1`：3D particles.

## ParticlesTransformAlign

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not align particle transforms relative to the camera or velocity.
- `ZBillboard = 1`：Align each particle's Z axis to face the camera.
- `YToVelocity = 2`：Align each particle's Y axis to the velocity vector.
- `ZBillboardYToVelocity = 3`：Align each particle's Z axis to face the camera and Y axis to the velocity vector.
- `LocalBillboard = 4`：Billboard each particles around a local axis.

## ParticlesTransformAlignAxis

命名空间 `Godot`；enum

### 枚举值

- `X = 0`：Use the X axis for local billboarding.
- `Y = 1`：Use the Y axis for local billboarding.

## ParticlesTransformAlignCustomSrc

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not read from CUSTOM when performing billboarding.
- `X = 1`：Read from `CUSTOM.x` when performing billboarding and use it as an angle, in radians.
- `Y = 2`：Read from `CUSTOM.y` when performing billboarding and use it as an angle, in radians.
- `Z = 3`：Read from `CUSTOM.z` when performing billboarding and use it as an angle, in radians.
- `W = 4`：Read from `CUSTOM.w` when performing billboarding and use it as an angle, in radians.

## PipelineSource

命名空间 `Godot`；enum

### 枚举值

- `Canvas = 0`：Pipeline compilation that was triggered by the 2D canvas renderer.
- `Mesh = 1`：Pipeline compilation that was triggered by loading a mesh.
- `Surface = 2`：Pipeline compilation that was triggered by building the surface cache before rendering the scene.
- `Draw = 3`：Pipeline compilation that was triggered while drawing the scene.
- `Specialization = 4`：Pipeline compilation that was triggered to optimize the current scene.
- `Max = 5`：Represents the size of the `RenderingServer.PipelineSource` enum.

## PrimitiveType

命名空间 `Godot`；enum

### 枚举值

- `Points = 0`：Primitive to draw consists of points.
- `Lines = 1`：Primitive to draw consists of lines.
- `LineStrip = 2`：Primitive to draw consists of a line strip from start to end.
- `Triangles = 3`：Primitive to draw consists of triangles.
- `TriangleStrip = 4`：Primitive to draw consists of a triangle strip (the last 3 vertices are always combined to make a triangle).
- `Max = 5`：Represents the size of the `RenderingServer.PrimitiveType` enum.

## ReflectionProbeAmbientMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not apply any ambient lighting inside the reflection probe's box defined by its size.
- `Environment = 1`：Apply automatically-sourced environment lighting inside the reflection probe's box defined by its size.
- `Color = 2`：Apply custom ambient lighting inside the reflection probe's box defined by its size.

## ReflectionProbeUpdateMode

命名空间 `Godot`；enum

### 枚举值

- `Once = 0`：Reflection probe will update reflections once and then stop.
- `Always = 1`：Reflection probe will update each frame.

## RenderingInfo

命名空间 `Godot`；enum

### 枚举值

- `TotalObjectsInFrame = 0`：Number of objects rendered in the current 3D scene.
- `TotalPrimitivesInFrame = 1`：Number of points, lines, or triangles rendered in the current 3D scene.
- `TotalDrawCallsInFrame = 2`：Number of draw calls performed to render in the current 3D scene.
- `TextureMemUsed = 3`：Texture memory used (in bytes).
- `BufferMemUsed = 4`：Buffer memory used (in bytes).
- `VideoMemUsed = 5`：Video memory used (in bytes).
- `PipelineCompilationsCanvas = 6`：Number of pipeline compilations that were triggered by the 2D canvas renderer.
- `PipelineCompilationsMesh = 7`：Number of pipeline compilations that were triggered by loading meshes.
- `PipelineCompilationsSurface = 8`：Number of pipeline compilations that were triggered by building the surface cache before rendering the scene.
- `PipelineCompilationsDraw = 9`：Number of pipeline compilations that were triggered while drawing the scene.
- `PipelineCompilationsSpecialization = 10`：Number of pipeline compilations that were triggered to optimize the current scene.

## ShaderMode

命名空间 `Godot`；enum

### 枚举值

- `Spatial = 0`：Shader is a 3D shader.
- `CanvasItem = 1`：Shader is a 2D shader.
- `Particles = 2`：Shader is a particle shader (can be used in both 2D and 3D).
- `Sky = 3`：Shader is a 3D sky shader.
- `Fog = 4`：Shader is a 3D fog shader.
- `TextureBlit = 5`：Shader is a texture_blit shader.
- `Max = 6`：Represents the size of the `RenderingServer.ShaderMode` enum.

## ShadowCastingSetting

命名空间 `Godot`；enum

### 枚举值

- `Off = 0`：Disable shadows from this instance.
- `On = 1`：Cast shadows from this instance.
- `DoubleSided = 2`：Disable backface culling when rendering the shadow of the object.
- `ShadowsOnly = 3`：Only render the shadows from the object.

## ShadowQuality

命名空间 `Godot`；enum

### 枚举值

- `Hard = 0`：Lowest shadow filtering quality (fastest).
- `SoftVeryLow = 1`：Very low shadow filtering quality (faster).
- `SoftLow = 2`：Low shadow filtering quality (fast).
- `SoftMedium = 3`：Medium low shadow filtering quality (average).
- `SoftHigh = 4`：High low shadow filtering quality (slow).
- `SoftUltra = 5`：Highest low shadow filtering quality (slowest).
- `Max = 6`：Represents the size of the `RenderingServer.ShadowQuality` enum.

## SkyMode

命名空间 `Godot`；enum

### 枚举值

- `Automatic = 0`：Automatically selects the appropriate process mode based on your sky shader.
- `Quality = 1`：Uses high quality importance sampling to process the radiance map.
- `Incremental = 2`：Uses the same high quality importance sampling to process the radiance map as `Quality`, but updates over several frames.
- `Realtime = 3`：Uses the fast filtering algorithm to process the radiance map.

## SplashStretchMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：No stretching is applied.
- `Keep = 1`：Stretches image to fullscreen while preserving aspect ratio.
- `KeepWidth = 2`：Stretches the height of the image based on the width of the screen.
- `KeepHeight = 3`：Stretches the width of the image based on the height of the screen.
- `Cover = 4`：Stretches the image to cover the entire screen while preserving aspect ratio.
- `Ignore = 5`：Stretches the image to cover the entire screen but doesn't preserve aspect ratio.

## SubSurfaceScatteringQuality

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables subsurface scattering entirely, even on materials that have [`BaseMaterial3D.SubsurfScatterEnabled`](BaseMaterial3D.md) set to `true`.
- `Low = 1`：Low subsurface scattering quality.
- `Medium = 2`：Medium subsurface scattering quality.
- `High = 3`：High subsurface scattering quality.

## TextureDrawableFormat

命名空间 `Godot`；enum

### 枚举值

- `Rgba8 = 0`：OpenGL texture format RGBA with four components, each with a bitdepth of 8.
- `Rgba8Srgb = 1`：OpenGL texture format RGBA with four components, each with a bitdepth of 8.
- `Rgbah = 2`：OpenGL texture format GL_RGBA16F where there are four components, each a 16-bit "half-precision" floating-point value.
- `Rgbaf = 3`：OpenGL texture format GL_RGBA32F where there are four components, each a 32-bit floating-point value.

## TextureLayeredType

命名空间 `Godot`；enum

### 枚举值

- `Layered2DArray = 0`：Array of 2-dimensional textures (see [`Texture2DArray`](Texture2DArray.md)).
- `Cubemap = 1`：Cubemap texture (see [`Cubemap`](Cubemap.md)).
- `CubemapArray = 2`：Array of cubemap textures (see [`CubemapArray`](CubemapArray.md)).

## TextureType

命名空间 `Godot`；enum

### 枚举值

- `Type2D = 0`：2D texture.
- `Layered = 1`：Layered texture.
- `Type3D = 2`：3D texture.

## ViewportAnisotropicFiltering

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Anisotropic filtering is disabled.
- `Anisotropy2X = 1`：Use 2× anisotropic filtering.
- `Anisotropy4X = 2`：Use 4× anisotropic filtering.
- `Anisotropy8X = 3`：Use 8× anisotropic filtering.
- `Anisotropy16X = 4`：Use 16× anisotropic filtering.
- `Max = 5`：Represents the size of the `RenderingServer.ViewportAnisotropicFiltering` enum.

## ViewportClearMode

命名空间 `Godot`；enum

### 枚举值

- `Always = 0`：Always clear the viewport's render target before drawing.
- `Never = 1`：Never clear the viewport's render target.
- `OnlyNextFrame = 2`：Clear the viewport's render target on the next frame, then switch to `Never`.

## ViewportDebugDraw

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Debug draw is disabled.
- `Unshaded = 1`：Objects are displayed without light information.
- `Lighting = 2`：Objects are displayed with only light information.
- `Overdraw = 3`：Objects are displayed semi-transparent with additive blending so you can see where they are drawing over top of one another.
- `Wireframe = 4`：Debug draw draws objects in wireframe.
- `NormalBuffer = 5`：Normal buffer is drawn instead of regular scene so you can see the per-pixel normals that will be used by post-processing effects.
- `VoxelGIAlbedo = 6`：Objects are displayed with only the albedo value from [`VoxelGI`](VoxelGI.md)s.
- `VoxelGILighting = 7`：Objects are displayed with only the lighting value from [`VoxelGI`](VoxelGI.md)s.
- `VoxelGIEmission = 8`：Objects are displayed with only the emission color from [`VoxelGI`](VoxelGI.md)s.
- `ShadowAtlas = 9`：Draws the shadow atlas that stores shadows from [`OmniLight3D`](OmniLight3D.md)s and [`SpotLight3D`](SpotLight3D.md)s in the upper left quadrant of the [`Viewport`](Viewport.md).
- `DirectionalShadowAtlas = 10`：Draws the shadow atlas that stores shadows from [`DirectionalLight3D`](DirectionalLight3D.md)s in the upper left quadrant of the [`Viewport`](Viewport.md).
- `SceneLuminance = 11`：Draws the estimated scene luminance.
- `Ssao = 12`：Draws the screen space ambient occlusion texture instead of the scene so that you can clearly see how it is affecting objects.
- `Ssil = 13`：Draws the screen space indirect lighting texture instead of the scene so that you can clearly see how it is affecting objects.
- `PssmSplits = 14`：Colors each PSSM split for the [`DirectionalLight3D`](DirectionalLight3D.md)s in the scene a different color so you can see where the splits are.
- `DecalAtlas = 15`：Draws the decal atlas that stores decal textures from [`Decal`](Decal.md)s.
- `Sdfgi = 16`：Draws SDFGI cascade data.
- `SdfgiProbes = 17`：Draws SDFGI probe data.
- `GIBuffer = 18`：Draws the global illumination buffer from [`VoxelGI`](VoxelGI.md) or SDFGI.
- `DisableLod = 19`：Disable mesh LOD.
- `ClusterOmniLights = 20`：Draws the [`OmniLight3D`](OmniLight3D.md) cluster.
- `ClusterSpotLights = 21`：Draws the [`SpotLight3D`](SpotLight3D.md) cluster.
- `ClusterDecals = 22`：Draws the [`Decal`](Decal.md) cluster.
- `ClusterReflectionProbes = 23`：Draws the [`ReflectionProbe`](ReflectionProbe.md) cluster.
- `Occluders = 24`：Draws the occlusion culling buffer.
- `MotionVectors = 25`：Draws the motion vectors buffer.
- `InternalBuffer = 26`：Internal buffer is drawn instead of regular scene so you can see the per-pixel output that will be used by post-processing effects.

## ViewportEnvironmentMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disable rendering of 3D environment over 2D canvas.
- `Enabled = 1`：Enable rendering of 3D environment over 2D canvas.
- `Inherit = 2`：Inherit enable/disable value from parent.
- `Max = 3`：Represents the size of the `RenderingServer.ViewportEnvironmentMode` enum.

## ViewportMsaa

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Multisample antialiasing for 3D is disabled.
- `Msaa2X = 1`：Multisample antialiasing uses 2 samples per pixel for 3D.
- `Msaa4X = 2`：Multisample antialiasing uses 4 samples per pixel for 3D.
- `Msaa8X = 3`：Multisample antialiasing uses 8 samples per pixel for 3D.
- `Max = 4`：Represents the size of the `RenderingServer.ViewportMsaa` enum.

## ViewportOcclusionCullingBuildQuality

命名空间 `Godot`；enum

### 枚举值

- `Low = 0`：Low occlusion culling BVH build quality (as defined by Embree).
- `Medium = 1`：Medium occlusion culling BVH build quality (as defined by Embree).
- `High = 2`：High occlusion culling BVH build quality (as defined by Embree).

## ViewportRenderInfo

命名空间 `Godot`；enum

### 枚举值

- `ObjectsInFrame = 0`：Number of objects drawn in a single frame.
- `PrimitivesInFrame = 1`：Number of points, lines, or triangles drawn in a single frame.
- `DrawCallsInFrame = 2`：Number of draw calls during this frame.
- `Max = 3`：Represents the size of the `RenderingServer.ViewportRenderInfo` enum.

## ViewportRenderInfoType

命名空间 `Godot`；enum

### 枚举值

- `Visible = 0`：Visible render pass (excluding shadows).
- `Shadow = 1`：Shadow render pass.
- `Canvas = 2`：Canvas item rendering.
- `Max = 3`：Represents the size of the `RenderingServer.ViewportRenderInfoType` enum.

## ViewportScaling3DMode

命名空间 `Godot`；enum

### 枚举值

- `Bilinear = 0`：Use bilinear scaling for the viewport's 3D buffer.
- `Fsr = 1`：Use AMD FidelityFX Super Resolution 1.0 upscaling for the viewport's 3D buffer.
- `Fsr2 = 2`：Use AMD FidelityFX Super Resolution 2.2 upscaling for the viewport's 3D buffer.
- `MetalfxSpatial = 3`：Use MetalFX spatial upscaling for the viewport's 3D buffer.
- `MetalfxTemporal = 4`：Use MetalFX temporal upscaling for the viewport's 3D buffer.
- `Nearest = 5`：Use nearest-neighbor filtering for the viewport's 3D buffer.
- `Max = 6`：Represents the size of the `RenderingServer.ViewportScaling3DMode` enum.

## ViewportScreenSpaceAA

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not perform any antialiasing in the full screen post-process.
- `Fxaa = 1`：Use fast approximate antialiasing.
- `Smaa = 2`：Use subpixel morphological antialiasing.
- `Max = 3`：Represents the size of the `RenderingServer.ViewportScreenSpaceAA` enum.

## ViewportSdfOversize

命名空间 `Godot`；enum

### 枚举值

- `Oversize100Percent = 0`：Do not oversize the 2D signed distance field.
- `Oversize120Percent = 1`：2D signed distance field covers 20% of the viewport's size outside the viewport on each side (top, right, bottom, left).
- `Oversize150Percent = 2`：2D signed distance field covers 50% of the viewport's size outside the viewport on each side (top, right, bottom, left).
- `Oversize200Percent = 3`：2D signed distance field covers 100% of the viewport's size outside the viewport on each side (top, right, bottom, left).
- `Max = 4`：Represents the size of the `RenderingServer.ViewportSdfOversize` enum.

## ViewportSdfScale

命名空间 `Godot`；enum

### 枚举值

- `Scale100Percent = 0`：Full resolution 2D signed distance field scale.
- `Scale50Percent = 1`：Half resolution 2D signed distance field scale on each axis (25% of the viewport pixel count).
- `Scale25Percent = 2`：Quarter resolution 2D signed distance field scale on each axis (6.25% of the viewport pixel count).
- `Max = 3`：Represents the size of the `RenderingServer.ViewportSdfScale` enum.

## ViewportUpdateMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not update the viewport's render target.
- `Once = 1`：Update the viewport's render target once, then switch to `Disabled`.
- `WhenVisible = 2`：Update the viewport's render target only when it is visible.
- `WhenParentVisible = 3`：Update the viewport's render target only when its parent is visible.
- `Always = 4`：Always update the viewport's render target.

## ViewportVrsMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Variable rate shading is disabled.
- `Texture = 1`：Variable rate shading uses a texture.
- `XR = 2`：Variable rate shading texture is supplied by the primary [`XRInterface`](XRInterface.md).
- `Max = 3`：Represents the size of the `RenderingServer.ViewportVrsMode` enum.

## ViewportVrsUpdateMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：The input texture for variable rate shading will not be processed.
- `Once = 1`：The input texture for variable rate shading will be processed once.
- `Always = 2`：The input texture for variable rate shading will be processed each frame.
- `Max = 3`：Represents the size of the `RenderingServer.ViewportVrsUpdateMode` enum.

## VisibilityRangeFadeMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disable visibility range fading for the given instance.
- `Self = 1`：Fade-out the given instance when it approaches its visibility range limits.
- `Dependencies = 2`：Fade-in the given instance's dependencies when reaching its visibility range limits.

## VoxelGIQuality

命名空间 `Godot`；enum

### 枚举值

- `Low = 0`：Low [`VoxelGI`](VoxelGI.md) rendering quality using 4 cones.
- `High = 1`：High [`VoxelGI`](VoxelGI.md) rendering quality using 6 cones.
