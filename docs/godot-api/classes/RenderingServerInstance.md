<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderingServerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The rendering server is the API backend for everything visible. The whole scene system mounts on it to display. The rendering server is completely opaque: the internals are entirely implementation-specific and cannot be accessed.

The rendering server can be used to bypass the scene/[`Node`](Node.md) system entirely. This can improve performance in cases where the scene system is the bottleneck, but won't improve performance otherwise (for instance, if the GPU is already fully utilized).

Resources are created using the `*_create` functions. These functions return [`Rid`](Rid.md)s which are not references to the objects themselves, but opaque *pointers* towards these objects.

All objects are drawn to a viewport. You can use the [`Viewport`](Viewport.md) attached to the [`SceneTree`](SceneTree.md) or you can create one yourself with `ViewportCreate()`. When using a custom scenario or canvas, the scenario or canvas needs to be attached to the viewport using `ViewportSetScenario()` or `ViewportAttachCanvas()`.

**Scenarios:** In 3D, all visual objects must be associated with a scenario. The scenario is a visual representation of the world. If accessing the rendering server from a running game, the scenario can be accessed from the scene tree from any [`Node3D`](Node3D.md) node with [`Node3D.GetWorld3D`](Node3D.md). Otherwise, a scenario can be created with `ScenarioCreate()`.

Similarly, in 2D, a canvas is needed to draw all canvas items.

**3D:** In 3D, all visible objects are comprised of a resource and an instance. A resource can be a mesh, a particle system, a light, or any other 3D object. In order to be visible resources must be attached to an instance using `InstanceSetBase()`. The instance must also be attached to the scenario using `InstanceSetScenario()` in order to be visible. RenderingServer methods that don't have a prefix are usually 3D-specific (but not always).

**2D:** In 2D, all visible objects are some form of canvas item. In order to be visible, a canvas item needs to be the child of a canvas attached to a viewport, or it needs to be the child of another canvas item that is eventually attached to the canvas. 2D-specific RenderingServer methods generally start with `canvas_*`.

**Headless mode:** Starting the engine with the `--headless` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html) disables all rendering and window management functions. Most functions from [`RenderingServer`](RenderingServer.md) will return dummy values in this case.

## 属性

### RenderLoopEnabled

```csharp
public bool RenderLoopEnabled { get; set; }
```

If `false`, disables rendering completely, but the engine logic is still being processed. You can call `ForceDraw()` to draw a frame even with rendering disabled.

## 方法

### AreaLightCreate

```csharp
public Rid AreaLightCreate()
```

Creates a new area light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this area light to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`AreaLight3D`](AreaLight3D.md).

### BakeRenderUV2

```csharp
public Array<Image> BakeRenderUV2(Rid base, Array<Rid> materialOverrides, Vector2I imageSize)
```

Bakes the material data of the Mesh passed in the `base` parameter with optional `materialOverrides` to a set of [`Image`](Image.md)s of size `imageSize`. Returns an array of [`Image`](Image.md)s containing material properties as specified in [`RenderingServer.BakeChannels`](RenderingServer.md).

### CallOnRenderThread

```csharp
public void CallOnRenderThread(Callable callable)
```

As the RenderingServer actual logic may run on a separate thread, accessing its internals from the main (or any other) thread will result in errors. To make it easier to run code that can safely access the rendering internals (such as [`RenderingDevice`](RenderingDevice.md) and similar RD classes), push a callable via this function so it will be executed on the render thread.

### CameraAttributesCreate

```csharp
public Rid CameraAttributesCreate()
```

Creates a camera attributes object and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `camera_attributes_` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`CameraAttributes`](CameraAttributes.md).

### CameraAttributesSetAutoExposure

```csharp
public void CameraAttributesSetAutoExposure(Rid cameraAttributes, bool enable, float minSensitivity, float maxSensitivity, float speed, float scale)
```

Sets the parameters to use with the auto-exposure effect. These parameters take on the same meaning as their counterparts in [`CameraAttributes`](CameraAttributes.md) and [`CameraAttributesPractical`](CameraAttributesPractical.md).

### CameraAttributesSetDofBlur

```csharp
public void CameraAttributesSetDofBlur(Rid cameraAttributes, bool farEnable, float farDistance, float farTransition, bool nearEnable, float nearDistance, float nearTransition, float amount)
```

Sets the parameters to use with the DOF blur effect. These parameters take on the same meaning as their counterparts in [`CameraAttributesPractical`](CameraAttributesPractical.md).

### CameraAttributesSetDofBlurBokehShape

```csharp
public void CameraAttributesSetDofBlurBokehShape(RenderingServer.DofBokehShape shape)
```

Sets the shape of the DOF bokeh pattern to `shape`. Different shapes may be used to achieve artistic effect, or to meet performance targets.

### CameraAttributesSetDofBlurQuality

```csharp
public void CameraAttributesSetDofBlurQuality(RenderingServer.DofBlurQuality quality, bool useJitter)
```

Sets the quality level of the DOF blur effect to `quality`. `useJitter` can be used to jitter samples taken during the blur pass to hide artifacts at the cost of looking more fuzzy.

### CameraAttributesSetExposure

```csharp
public void CameraAttributesSetExposure(Rid cameraAttributes, float multiplier, float normalization)
```

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
public Rid CameraCreate()
```

Creates a 3D camera and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `camera_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`Camera3D`](Camera3D.md).

### CameraSetCameraAttributes

```csharp
public void CameraSetCameraAttributes(Rid camera, Rid effects)
```

Sets the camera_attributes created with `CameraAttributesCreate()` to the given camera.

### CameraSetCompositor

```csharp
public void CameraSetCompositor(Rid camera, Rid compositor)
```

Sets the compositor used by this camera. Equivalent to [`Camera3D.Compositor`](Camera3D.md).

### CameraSetCullMask

```csharp
public void CameraSetCullMask(Rid camera, uint layers)
```

Sets the cull mask associated with this camera. The cull mask describes which 3D layers are rendered by this camera. Equivalent to [`Camera3D.CullMask`](Camera3D.md).

### CameraSetEnvironment

```csharp
public void CameraSetEnvironment(Rid camera, Rid env)
```

Sets the environment used by this camera. Equivalent to [`Camera3D.Environment`](Camera3D.md).

### CameraSetFrustum

```csharp
public void CameraSetFrustum(Rid camera, float size, Vector2 offset, float zNear, float zFar)
```

Sets camera to use frustum projection. This mode allows adjusting the `offset` argument to create "tilted frustum" effects.

### CameraSetOrthogonal

```csharp
public void CameraSetOrthogonal(Rid camera, float size, float zNear, float zFar)
```

Sets camera to use orthogonal projection, also known as orthographic projection. Objects remain the same size on the screen no matter how far away they are.

### CameraSetPerspective

```csharp
public void CameraSetPerspective(Rid camera, float fovyDegrees, float zNear, float zFar)
```

Sets camera to use perspective projection. Objects on the screen becomes smaller when they are far away.

### CameraSetTransform

```csharp
public void CameraSetTransform(Rid camera, Transform3D transform)
```

Sets [`Transform3D`](Transform3D.md) of camera.

### CameraSetUseVerticalAspect

```csharp
public void CameraSetUseVerticalAspect(Rid camera, bool enable)
```

If `true`, preserves the horizontal aspect ratio which is equivalent to [`Camera3D.KeepAspectEnum.Width`](Camera3D.md). If `false`, preserves the vertical aspect ratio which is equivalent to [`Camera3D.KeepAspectEnum.Height`](Camera3D.md).

### CanvasCreate

```csharp
public Rid CanvasCreate()
```

Creates a canvas and returns the assigned [`Rid`](Rid.md). It can be accessed with the RID that is returned. This RID will be used in all `canvas_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

Canvas has no [`Resource`](Resource.md) or [`Node`](Node.md) equivalent.

### CanvasItemAddAnimationSlice

```csharp
public void CanvasItemAddAnimationSlice(Rid item, double animationLength, double sliceBegin, double sliceEnd, double offset = 0)
```

Subsequent drawing commands will be ignored unless they fall within the specified animation slice. This is a faster way to implement animations that loop on background rather than redrawing constantly.

### CanvasItemAddCircle

```csharp
public void CanvasItemAddCircle(Rid item, Vector2 pos, float radius, Color color)
```

Draws a circle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawCircle`](CanvasItem.md).

### CanvasItemAddCircle

```csharp
public void CanvasItemAddCircle(Rid item, Vector2 pos, float radius, Color color, bool antialiased = false)
```

Draws a circle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawCircle`](CanvasItem.md).

### CanvasItemAddClipIgnore

```csharp
public void CanvasItemAddClipIgnore(Rid item, bool ignore)
```

If `ignore` is `true`, ignore clipping on items drawn with this canvas item until this is called again with `ignore` set to `false`.

### CanvasItemAddEllipse

```csharp
public void CanvasItemAddEllipse(Rid item, Vector2 pos, float major, float minor, Color color, bool antialiased = false)
```

Draws an ellipse with semi-major axis `major` and semi-minor axis `minor` on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawEllipse`](CanvasItem.md).

### CanvasItemAddLcdTextureRectRegion

```csharp
public void CanvasItemAddLcdTextureRectRegion(Rid item, Rect2 rect, Rid texture, Rect2 srcRect, Color modulate)
```

See also [`CanvasItem.DrawLcdTextureRectRegion`](CanvasItem.md).

### CanvasItemAddLine

```csharp
public void CanvasItemAddLine(Rid item, Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)
```

Draws a line on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawLine`](CanvasItem.md).

### CanvasItemAddMesh

```csharp
public void CanvasItemAddMesh(Rid item, Rid mesh, Transform2D? transform = null, Color? modulate = null, Rid texture = default)
```

Draws a mesh created with `MeshCreate()` with given `transform`, `modulate` color, and `texture`. This is used internally by [`MeshInstance2D`](MeshInstance2D.md).

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddMsdfTextureRectRegion

```csharp
public void CanvasItemAddMsdfTextureRectRegion(Rid item, Rect2 rect, Rid texture, Rect2 srcRect, Color? modulate = null, int outlineSize = 0, float pxRange = 1f, float scale = 1f)
```

See also [`CanvasItem.DrawMsdfTextureRectRegion`](CanvasItem.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddMultiline

```csharp
public void CanvasItemAddMultiline(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width)
```

Draws a 2D multiline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultiline`](CanvasItem.md) and [`CanvasItem.DrawMultilineColors`](CanvasItem.md).

### CanvasItemAddMultiline

```csharp
public void CanvasItemAddMultiline(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased)
```

Draws a 2D multiline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultiline`](CanvasItem.md) and [`CanvasItem.DrawMultilineColors`](CanvasItem.md).

### CanvasItemAddMultiline

```csharp
public void CanvasItemAddMultiline(Rid item, Vector2[] points, Color[] colors, float width = -1f, bool antialiased = false)
```

Draws a 2D multiline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultiline`](CanvasItem.md) and [`CanvasItem.DrawMultilineColors`](CanvasItem.md).

### CanvasItemAddMultiline

```csharp
public void CanvasItemAddMultiline(Rid item, Vector2[] points, Color[] colors, float width)
```

Draws a 2D multiline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultiline`](CanvasItem.md) and [`CanvasItem.DrawMultilineColors`](CanvasItem.md).

### CanvasItemAddMultimesh

```csharp
public void CanvasItemAddMultimesh(Rid item, Rid mesh, Rid texture = default)
```

Draws a 2D [`MultiMesh`](MultiMesh.md) on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawMultimesh`](CanvasItem.md).

### CanvasItemAddNinePatch

```csharp
public void CanvasItemAddNinePatch(Rid item, Rect2 rect, Rect2 source, Rid texture, Vector2 topleft, Vector2 bottomright, RenderingServer.NinePatchAxisMode xAxisMode = RenderingServer.NinePatchAxisMode.Stretch, RenderingServer.NinePatchAxisMode yAxisMode = RenderingServer.NinePatchAxisMode.Stretch, bool drawCenter = true, Color? modulate = null)
```

Draws a nine-patch rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddParticles

```csharp
public void CanvasItemAddParticles(Rid item, Rid particles, Rid texture)
```

Draws particles on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md).

### CanvasItemAddPolygon

```csharp
public void CanvasItemAddPolygon(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Rid texture)
```

Draws a 2D polygon on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). If you need more flexibility (such as being able to use bones), use `CanvasItemAddTriangleArray()` instead. See also [`CanvasItem.DrawPolygon`](CanvasItem.md).

**Note:** If you frequently redraw the same polygon with a large number of vertices, consider pre-calculating the triangulation with [`Geometry2D.TriangulatePolygon`](Geometry2D.md) and using [`CanvasItem.DrawMesh`](CanvasItem.md), [`CanvasItem.DrawMultimesh`](CanvasItem.md), or `CanvasItemAddTriangleArray()`.

### CanvasItemAddPolygon

```csharp
public void CanvasItemAddPolygon(Rid item, Vector2[] points, Color[] colors, Vector2[] uvs = null, Rid texture = default)
```

Draws a 2D polygon on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). If you need more flexibility (such as being able to use bones), use `CanvasItemAddTriangleArray()` instead. See also [`CanvasItem.DrawPolygon`](CanvasItem.md).

**Note:** If you frequently redraw the same polygon with a large number of vertices, consider pre-calculating the triangulation with [`Geometry2D.TriangulatePolygon`](Geometry2D.md) and using [`CanvasItem.DrawMesh`](CanvasItem.md), [`CanvasItem.DrawMultimesh`](CanvasItem.md), or `CanvasItemAddTriangleArray()`.

参数：
- `uvs`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.

### CanvasItemAddPolyline

```csharp
public void CanvasItemAddPolyline(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased)
```

Draws a 2D polyline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawPolyline`](CanvasItem.md) and [`CanvasItem.DrawPolylineColors`](CanvasItem.md).

### CanvasItemAddPolyline

```csharp
public void CanvasItemAddPolyline(Rid item, Vector2[] points, Color[] colors, float width = -1f, bool antialiased = false)
```

Draws a 2D polyline on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawPolyline`](CanvasItem.md) and [`CanvasItem.DrawPolylineColors`](CanvasItem.md).

### CanvasItemAddPrimitive

```csharp
public void CanvasItemAddPrimitive(Rid item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, Rid texture)
```

Draws a 2D primitive on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawPrimitive`](CanvasItem.md).

### CanvasItemAddPrimitive

```csharp
public void CanvasItemAddPrimitive(Rid item, Vector2[] points, Color[] colors, Vector2[] uvs, Rid texture)
```

Draws a 2D primitive on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawPrimitive`](CanvasItem.md).

### CanvasItemAddRect

```csharp
public void CanvasItemAddRect(Rid item, Rect2 rect, Color color)
```

Draws a rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawRect`](CanvasItem.md).

### CanvasItemAddRect

```csharp
public void CanvasItemAddRect(Rid item, Rect2 rect, Color color, bool antialiased = false)
```

Draws a rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawRect`](CanvasItem.md).

### CanvasItemAddSetTransform

```csharp
public void CanvasItemAddSetTransform(Rid item, Transform2D transform)
```

Sets a [`Transform2D`](Transform2D.md) that will be used to transform subsequent canvas item commands.

### CanvasItemAddTextureRect

```csharp
public void CanvasItemAddTextureRect(Rid item, Rect2 rect, Rid texture, bool tile = false, Color? modulate = null, bool transpose = false)
```

Draws a 2D textured rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawTextureRect`](CanvasItem.md) and [`Texture2D.DrawRect`](Texture2D.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddTextureRectRegion

```csharp
public void CanvasItemAddTextureRectRegion(Rid item, Rect2 rect, Rid texture, Rect2 srcRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
```

Draws the specified region of a 2D textured rectangle on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). See also [`CanvasItem.DrawTextureRectRegion`](CanvasItem.md) and [`Texture2D.DrawRectRegion`](Texture2D.md).

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### CanvasItemAddTriangleArray

```csharp
public void CanvasItemAddTriangleArray(Rid item, ReadOnlySpan<int> indices, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> bones, ReadOnlySpan<float> weights, Rid texture, int count)
```

Draws a triangle array on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). This is internally used by [`Line2D`](Line2D.md) and [`StyleBoxFlat`](StyleBoxFlat.md) for rendering. `CanvasItemAddTriangleArray()` is highly flexible, but more complex to use than `CanvasItemAddPolygon()`.

**Note:** If `count` is set to a non-negative value, only the first `count * 3` indices (corresponding to `count` triangles) will be drawn. Otherwise, all indices are drawn.

### CanvasItemAddTriangleArray

```csharp
public void CanvasItemAddTriangleArray(Rid item, int[] indices, Vector2[] points, Color[] colors, Vector2[] uvs = null, int[] bones = null, float[] weights = null, Rid texture = default, int count = -1)
```

Draws a triangle array on the [`CanvasItem`](CanvasItem.md) pointed to by the `item` [`Rid`](Rid.md). This is internally used by [`Line2D`](Line2D.md) and [`StyleBoxFlat`](StyleBoxFlat.md) for rendering. `CanvasItemAddTriangleArray()` is highly flexible, but more complex to use than `CanvasItemAddPolygon()`.

**Note:** If `count` is set to a non-negative value, only the first `count * 3` indices (corresponding to `count` triangles) will be drawn. Otherwise, all indices are drawn.

参数：
- `uvs`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.
- `bones`：If the parameter is null, then the default value is `Array.Empty<int>()`.
- `weights`：If the parameter is null, then the default value is `Array.Empty<float>()`.

### CanvasItemAttachSkeleton

```csharp
public void CanvasItemAttachSkeleton(Rid item, Rid skeleton)
```

Attaches a skeleton to the [`CanvasItem`](CanvasItem.md). Removes the previous skeleton.

### CanvasItemClear

```csharp
public void CanvasItemClear(Rid item)
```

Clears the [`CanvasItem`](CanvasItem.md) and removes all commands in it.

### CanvasItemCreate

```csharp
public Rid CanvasItemCreate()
```

Creates a new CanvasItem instance and returns its [`Rid`](Rid.md). It can be accessed with the RID that is returned. This RID will be used in all `canvas_item_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`CanvasItem`](CanvasItem.md).

### CanvasItemGetInstanceShaderParameter

```csharp
public Variant CanvasItemGetInstanceShaderParameter(Rid instance, StringName parameter)
```

Returns the value of the per-instance shader uniform from the specified canvas item instance. Equivalent to [`CanvasItem.GetInstanceShaderParameter`](CanvasItem.md).

### CanvasItemGetInstanceShaderParameterDefaultValue

```csharp
public Variant CanvasItemGetInstanceShaderParameterDefaultValue(Rid instance, StringName parameter)
```

Returns the default value of the per-instance shader uniform from the specified canvas item instance. Equivalent to [`CanvasItem.GetInstanceShaderParameter`](CanvasItem.md).

### CanvasItemGetInstanceShaderParameterList

```csharp
public Array<Dictionary> CanvasItemGetInstanceShaderParameterList(Rid instance)
```

Returns a dictionary of per-instance shader uniform names of the per-instance shader uniform from the specified canvas item instance.

The returned dictionary is in PropertyInfo format, with the keys `name`, `class_name`, `type`, `hint`, `hint_string`, and `usage`.

### CanvasItemResetPhysicsInterpolation

```csharp
public void CanvasItemResetPhysicsInterpolation(Rid item)
```

Prevents physics interpolation for the current physics tick.

This is useful when moving a canvas item to a new location, to give an instantaneous change rather than interpolation from the previous location.

### CanvasItemSetCanvasGroupMode

```csharp
public void CanvasItemSetCanvasGroupMode(Rid item, RenderingServer.CanvasGroupMode mode, float clearMargin = 5f, bool fitEmpty = false, float fitMargin = 0f, bool blurMipmaps = false)
```

Sets the canvas group mode used during 2D rendering for the canvas item specified by the `item` RID. For faster but more limited clipping, use `CanvasItemSetClip()` instead.

**Note:** The equivalent node functionality is found in [`CanvasGroup`](CanvasGroup.md) and [`CanvasItem.ClipChildren`](CanvasItem.md).

### CanvasItemSetClip

```csharp
public void CanvasItemSetClip(Rid item, bool clip)
```

If `clip` is `true`, makes the canvas item specified by the `item` RID not draw anything outside of its rect's coordinates. This clipping is fast, but works only with axis-aligned rectangles. This means that rotation is ignored by the clipping rectangle. For more advanced clipping shapes, use `CanvasItemSetCanvasGroupMode()` instead.

**Note:** The equivalent node functionality is found in [`Label.ClipText`](Label.md), [`RichTextLabel`](RichTextLabel.md) (always enabled) and more.

### CanvasItemSetCopyToBackbuffer

```csharp
public void CanvasItemSetCopyToBackbuffer(Rid item, bool enabled, Rect2 rect)
```

Sets the [`CanvasItem`](CanvasItem.md) to copy a rect to the backbuffer.

### CanvasItemSetCustomRect

```csharp
public void CanvasItemSetCustomRect(Rid item, bool useCustomRect, Rect2? rect = null)
```

If `useCustomRect` is `true`, sets the custom visibility rectangle (used for culling) to `rect` for the canvas item specified by `item`. Setting a custom visibility rect can reduce CPU load when drawing lots of 2D instances. If `useCustomRect` is `false`, automatically computes a visibility rectangle based on the canvas item's draw commands.

参数：
- `rect`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### CanvasItemSetDefaultTextureFilter

```csharp
public void CanvasItemSetDefaultTextureFilter(Rid item, RenderingServer.CanvasItemTextureFilter filter)
```

Sets the default texture filter mode for the canvas item specified by the `item` RID. Equivalent to [`CanvasItem.TextureFilter`](CanvasItem.md).

### CanvasItemSetDefaultTextureRepeat

```csharp
public void CanvasItemSetDefaultTextureRepeat(Rid item, RenderingServer.CanvasItemTextureRepeat repeat)
```

Sets the default texture repeat mode for the canvas item specified by the `item` RID. Equivalent to [`CanvasItem.TextureRepeat`](CanvasItem.md).

### CanvasItemSetDistanceFieldMode

```csharp
public void CanvasItemSetDistanceFieldMode(Rid item, bool enabled)
```

If `enabled` is `true`, enables multichannel signed distance field rendering mode for the canvas item specified by the `item` RID. This is meant to be used for font rendering, or with specially generated images using [msdfgen](https://github.com/Chlumsky/msdfgen).

### CanvasItemSetDrawBehindParent

```csharp
public void CanvasItemSetDrawBehindParent(Rid item, bool enabled)
```

If `enabled` is `true`, draws the canvas item specified by the `item` RID behind its parent. Equivalent to [`CanvasItem.ShowBehindParent`](CanvasItem.md).

### CanvasItemSetDrawIndex

```csharp
public void CanvasItemSetDrawIndex(Rid item, int index)
```

Sets the index for the [`CanvasItem`](CanvasItem.md).

### CanvasItemSetInstanceShaderParameter

```csharp
public void CanvasItemSetInstanceShaderParameter(Rid instance, StringName parameter, Variant value)
```

Sets the per-instance shader uniform on the specified canvas item instance. Equivalent to [`CanvasItem.SetInstanceShaderParameter`](CanvasItem.md).

### CanvasItemSetInterpolated

```csharp
public void CanvasItemSetInterpolated(Rid item, bool interpolated)
```

If `interpolated` is `true`, turns on physics interpolation for the canvas item.

### CanvasItemSetLightMask

```csharp
public void CanvasItemSetLightMask(Rid item, int mask)
```

Sets the light `mask` for the canvas item specified by the `item` RID. Equivalent to [`CanvasItem.LightMask`](CanvasItem.md).

### CanvasItemSetMaterial

```csharp
public void CanvasItemSetMaterial(Rid item, Rid material)
```

Sets a new `material` to the canvas item specified by the `item` RID. Equivalent to [`CanvasItem.Material`](CanvasItem.md).

### CanvasItemSetModulate

```csharp
public void CanvasItemSetModulate(Rid item, Color color)
```

Multiplies the color of the canvas item specified by the `item` RID, while affecting its children. See also `CanvasItemSetSelfModulate()`. Equivalent to [`CanvasItem.Modulate`](CanvasItem.md).

### CanvasItemSetParent

```csharp
public void CanvasItemSetParent(Rid item, Rid parent)
```

Sets a parent [`CanvasItem`](CanvasItem.md) to the [`CanvasItem`](CanvasItem.md). The item will inherit transform, modulation and visibility from its parent, like [`CanvasItem`](CanvasItem.md) nodes in the scene tree.

### CanvasItemSetSelfModulate

```csharp
public void CanvasItemSetSelfModulate(Rid item, Color color)
```

Multiplies the color of the canvas item specified by the `item` RID, without affecting its children. See also `CanvasItemSetModulate()`. Equivalent to [`CanvasItem.SelfModulate`](CanvasItem.md).

### CanvasItemSetSortChildrenByY

```csharp
public void CanvasItemSetSortChildrenByY(Rid item, bool enabled)
```

If `enabled` is `true`, child nodes with the lowest Y position are drawn before those with a higher Y position. Y-sorting only affects children that inherit from the canvas item specified by the `item` RID, not the canvas item itself. Equivalent to [`CanvasItem.YSortEnabled`](CanvasItem.md).

### CanvasItemSetTransform

```csharp
public void CanvasItemSetTransform(Rid item, Transform2D transform)
```

Sets the `transform` of the canvas item specified by the `item` RID. This affects where and how the item will be drawn. Child canvas items' transforms are multiplied by their parent's transform. Equivalent to [`Node2D.Transform`](Node2D.md).

### CanvasItemSetUseParentMaterial

```csharp
public void CanvasItemSetUseParentMaterial(Rid item, bool enabled)
```

Sets if the [`CanvasItem`](CanvasItem.md) uses its parent's material.

### CanvasItemSetVisibilityLayer

```csharp
public void CanvasItemSetVisibilityLayer(Rid item, uint visibilityLayer)
```

Sets the rendering visibility layer associated with this [`CanvasItem`](CanvasItem.md). Only [`Viewport`](Viewport.md) nodes with a matching rendering mask will render this [`CanvasItem`](CanvasItem.md).

### CanvasItemSetVisibilityNotifier

```csharp
public void CanvasItemSetVisibilityNotifier(Rid item, bool enable, Rect2 area, Callable enterCallable, Callable exitCallable)
```

Sets the given [`CanvasItem`](CanvasItem.md) as visibility notifier. `area` defines the area of detecting visibility. `enterCallable` is called when the [`CanvasItem`](CanvasItem.md) enters the screen, `exitCallable` is called when the [`CanvasItem`](CanvasItem.md) exits the screen. If `enable` is `false`, the item will no longer function as notifier.

This method can be used to manually mimic [`VisibleOnScreenNotifier2D`](VisibleOnScreenNotifier2D.md).

### CanvasItemSetVisible

```csharp
public void CanvasItemSetVisible(Rid item, bool visible)
```

Sets the visibility of the [`CanvasItem`](CanvasItem.md).

### CanvasItemSetZAsRelativeToParent

```csharp
public void CanvasItemSetZAsRelativeToParent(Rid item, bool enabled)
```

If this is enabled, the Z index of the parent will be added to the children's Z index.

### CanvasItemSetZIndex

```csharp
public void CanvasItemSetZIndex(Rid item, int zIndex)
```

Sets the [`CanvasItem`](CanvasItem.md)'s Z index, i.e. its draw order (lower indexes are drawn first).

### CanvasItemTransformPhysicsInterpolation

```csharp
public void CanvasItemTransformPhysicsInterpolation(Rid item, Transform2D transform)
```

Transforms both the current and previous stored transform for a canvas item.

This allows transforming a canvas item without creating a "glitch" in the interpolation, which is particularly useful for large worlds utilizing a shifting origin.

### CanvasLightAttachToCanvas

```csharp
public void CanvasLightAttachToCanvas(Rid light, Rid canvas)
```

Attaches the canvas light to the canvas. Removes it from its previous canvas.

### CanvasLightCreate

```csharp
public Rid CanvasLightCreate()
```

Creates a canvas light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `canvas_light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`Light2D`](Light2D.md).

### CanvasLightOccluderAttachToCanvas

```csharp
public void CanvasLightOccluderAttachToCanvas(Rid occluder, Rid canvas)
```

Attaches a light occluder to the canvas. Removes it from its previous canvas.

### CanvasLightOccluderCreate

```csharp
public Rid CanvasLightOccluderCreate()
```

Creates a light occluder and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `canvas_light_occluder_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`LightOccluder2D`](LightOccluder2D.md).

### CanvasLightOccluderResetPhysicsInterpolation

```csharp
public void CanvasLightOccluderResetPhysicsInterpolation(Rid occluder)
```

Prevents physics interpolation for the current physics tick.

This is useful when moving an occluder to a new location, to give an instantaneous change rather than interpolation from the previous location.

### CanvasLightOccluderSetAsSdfCollision

```csharp
public void CanvasLightOccluderSetAsSdfCollision(Rid occluder, bool enable)
```

Enables or disables using the light occluder as a signed distance field for 2D particle collision.

### CanvasLightOccluderSetEnabled

```csharp
public void CanvasLightOccluderSetEnabled(Rid occluder, bool enabled)
```

Enables or disables light occluder.

### CanvasLightOccluderSetInterpolated

```csharp
public void CanvasLightOccluderSetInterpolated(Rid occluder, bool interpolated)
```

If `interpolated` is `true`, turns on physics interpolation for the light occluder.

### CanvasLightOccluderSetLightMask

```csharp
public void CanvasLightOccluderSetLightMask(Rid occluder, int mask)
```

The light mask. See [`LightOccluder2D`](LightOccluder2D.md) for more information on light masks.

### CanvasLightOccluderSetPolygon

```csharp
public void CanvasLightOccluderSetPolygon(Rid occluder, Rid polygon)
```

Sets a light occluder's polygon.

### CanvasLightOccluderSetTransform

```csharp
public void CanvasLightOccluderSetTransform(Rid occluder, Transform2D transform)
```

Sets a light occluder's [`Transform2D`](Transform2D.md).

### CanvasLightOccluderTransformPhysicsInterpolation

```csharp
public void CanvasLightOccluderTransformPhysicsInterpolation(Rid occluder, Transform2D transform)
```

Transforms both the current and previous stored transform for a light occluder.

This allows transforming an occluder without creating a "glitch" in the interpolation, which is particularly useful for large worlds utilizing a shifting origin.

### CanvasLightResetPhysicsInterpolation

```csharp
public void CanvasLightResetPhysicsInterpolation(Rid light)
```

Prevents physics interpolation for the current physics tick.

This is useful when moving a canvas item to a new location, to give an instantaneous change rather than interpolation from the previous location.

### CanvasLightSetBlendMode

```csharp
public void CanvasLightSetBlendMode(Rid light, RenderingServer.CanvasLightBlendMode mode)
```

Sets the blend mode for the given canvas light to `mode`. Equivalent to [`Light2D.BlendMode`](Light2D.md).

### CanvasLightSetColor

```csharp
public void CanvasLightSetColor(Rid light, Color color)
```

Sets the color for a light.

### CanvasLightSetEnabled

```csharp
public void CanvasLightSetEnabled(Rid light, bool enabled)
```

Enables or disables a canvas light.

### CanvasLightSetEnergy

```csharp
public void CanvasLightSetEnergy(Rid light, float energy)
```

Sets a canvas light's energy.

### CanvasLightSetHeight

```csharp
public void CanvasLightSetHeight(Rid light, float height)
```

Sets a canvas light's height.

### CanvasLightSetInterpolated

```csharp
public void CanvasLightSetInterpolated(Rid light, bool interpolated)
```

If `interpolated` is `true`, turns on physics interpolation for the canvas light.

### CanvasLightSetItemCullMask

```csharp
public void CanvasLightSetItemCullMask(Rid light, int mask)
```

The light mask. See [`LightOccluder2D`](LightOccluder2D.md) for more information on light masks.

### CanvasLightSetItemShadowCullMask

```csharp
public void CanvasLightSetItemShadowCullMask(Rid light, int mask)
```

The binary mask used to determine which layers this canvas light's shadows affects. See [`LightOccluder2D`](LightOccluder2D.md) for more information on light masks.

### CanvasLightSetLayerRange

```csharp
public void CanvasLightSetLayerRange(Rid light, int minLayer, int maxLayer)
```

The layer range that gets rendered with this light.

### CanvasLightSetMode

```csharp
public void CanvasLightSetMode(Rid light, RenderingServer.CanvasLightMode mode)
```

Sets the mode of the canvas light.

### CanvasLightSetShadowColor

```csharp
public void CanvasLightSetShadowColor(Rid light, Color color)
```

Sets the color of the canvas light's shadow.

### CanvasLightSetShadowEnabled

```csharp
public void CanvasLightSetShadowEnabled(Rid light, bool enabled)
```

Enables or disables the canvas light's shadow.

### CanvasLightSetShadowFilter

```csharp
public void CanvasLightSetShadowFilter(Rid light, RenderingServer.CanvasLightShadowFilter filter)
```

Sets the canvas light's shadow's filter.

### CanvasLightSetShadowSmooth

```csharp
public void CanvasLightSetShadowSmooth(Rid light, float smooth)
```

Smoothens the shadow. The lower, the smoother.

### CanvasLightSetTexture

```csharp
public void CanvasLightSetTexture(Rid light, Rid texture)
```

Sets the texture to be used by a [`PointLight2D`](PointLight2D.md). Equivalent to [`PointLight2D.Texture`](PointLight2D.md).

### CanvasLightSetTextureOffset

```csharp
public void CanvasLightSetTextureOffset(Rid light, Vector2 offset)
```

Sets the offset of a [`PointLight2D`](PointLight2D.md)'s texture. Equivalent to [`PointLight2D.Offset`](PointLight2D.md).

### CanvasLightSetTextureScale

```csharp
public void CanvasLightSetTextureScale(Rid light, float scale)
```

Sets the scale factor of a [`PointLight2D`](PointLight2D.md)'s texture. Equivalent to [`PointLight2D.TextureScale`](PointLight2D.md).

### CanvasLightSetTransform

```csharp
public void CanvasLightSetTransform(Rid light, Transform2D transform)
```

Sets the canvas light's [`Transform2D`](Transform2D.md).

### CanvasLightSetZRange

```csharp
public void CanvasLightSetZRange(Rid light, int minZ, int maxZ)
```

Sets the Z range of objects that will be affected by this light. Equivalent to [`Light2D.RangeZMin`](Light2D.md) and [`Light2D.RangeZMax`](Light2D.md).

### CanvasLightTransformPhysicsInterpolation

```csharp
public void CanvasLightTransformPhysicsInterpolation(Rid light, Transform2D transform)
```

Transforms both the current and previous stored transform for a canvas light.

This allows transforming a light without creating a "glitch" in the interpolation, which is particularly useful for large worlds utilizing a shifting origin.

### CanvasOccluderPolygonCreate

```csharp
public Rid CanvasOccluderPolygonCreate()
```

Creates a new light occluder polygon and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `canvas_occluder_polygon_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`OccluderPolygon2D`](OccluderPolygon2D.md).

### CanvasOccluderPolygonSetCullMode

```csharp
public void CanvasOccluderPolygonSetCullMode(Rid occluderPolygon, RenderingServer.CanvasOccluderPolygonCullMode mode)
```

Sets an occluder polygon's cull mode.

### CanvasOccluderPolygonSetShape

```csharp
public void CanvasOccluderPolygonSetShape(Rid occluderPolygon, ReadOnlySpan<Vector2> shape, bool closed)
```

Sets the shape of the occluder polygon.

### CanvasOccluderPolygonSetShape

```csharp
public void CanvasOccluderPolygonSetShape(Rid occluderPolygon, Vector2[] shape, bool closed)
```

Sets the shape of the occluder polygon.

### CanvasSetDisableScale

```csharp
public void CanvasSetDisableScale(bool disable)
```

If `disable` is `true`, makes 2D rendering ignore the canvas scale defined for each canvas layer. This affects [`CanvasLayer`](CanvasLayer.md)s with the [`CanvasLayer.FollowViewportEnabled`](CanvasLayer.md) property set to `true`.

In the editor, this is set to `true` by default, and set to `false` when **View > Preview Canvas Scale** is enabled at the top of the 2D editor viewport.

**Note:** Setting this to `true` does not impact the behavior of [`CanvasLayer.Scale`](CanvasLayer.md), [`Node2D.Scale`](Node2D.md), or [`Control.Scale`](Control.md).

### CanvasSetItemMirroring

```csharp
public void CanvasSetItemMirroring(Rid canvas, Rid item, Vector2 mirroring)
```

A copy of the canvas item will be drawn with a local offset of the `mirroring`.

**Note:** This is equivalent to calling `CanvasSetItemRepeat()` like `canvas_set_item_repeat(item, mirroring, 1)`, with an additional check ensuring `canvas` is a parent of `item`.

### CanvasSetItemRepeat

```csharp
public void CanvasSetItemRepeat(Rid item, Vector2 repeatSize, int repeatTimes)
```

A copy of the canvas item will be drawn with a local offset of the `repeatSize` by the number of times of the `repeatTimes`. As the `repeatTimes` increases, the copies will spread away from the origin texture.

### CanvasSetModulate

```csharp
public void CanvasSetModulate(Rid canvas, Color color)
```

Modulates all colors in the given canvas.

### CanvasSetShadowTextureSize

```csharp
public void CanvasSetShadowTextureSize(int size)
```

Sets the `ProjectSettings.rendering/2d/shadow_atlas/size` to use for [`Light2D`](Light2D.md) shadow rendering (in pixels). The value is rounded up to the nearest power of 2.

### CanvasTextureCreate

```csharp
public Rid CanvasTextureCreate()
```

Creates a canvas texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `canvas_texture_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method. See also `Texture2DCreate()`.

**Note:** The equivalent resource is [`CanvasTexture`](CanvasTexture.md) and is only meant to be used in 2D rendering, not 3D.

### CanvasTextureSetChannel

```csharp
public void CanvasTextureSetChannel(Rid canvasTexture, RenderingServer.CanvasTextureChannel channel, Rid texture)
```

Sets the `channel`'s `texture` for the canvas texture specified by the `canvasTexture` RID. Equivalent to [`CanvasTexture.DiffuseTexture`](CanvasTexture.md), [`CanvasTexture.NormalTexture`](CanvasTexture.md) and [`CanvasTexture.SpecularTexture`](CanvasTexture.md).

### CanvasTextureSetShadingParameters

```csharp
public void CanvasTextureSetShadingParameters(Rid canvasTexture, Color baseColor, float shininess)
```

Sets the `baseColor` and `shininess` to use for the canvas texture specified by the `canvasTexture` RID. Equivalent to [`CanvasTexture.SpecularColor`](CanvasTexture.md) and [`CanvasTexture.SpecularShininess`](CanvasTexture.md).

### CanvasTextureSetTextureFilter

```csharp
public void CanvasTextureSetTextureFilter(Rid canvasTexture, RenderingServer.CanvasItemTextureFilter filter)
```

Sets the texture `filter` mode to use for the canvas texture specified by the `canvasTexture` RID.

### CanvasTextureSetTextureRepeat

```csharp
public void CanvasTextureSetTextureRepeat(Rid canvasTexture, RenderingServer.CanvasItemTextureRepeat repeat)
```

Sets the texture `repeat` mode to use for the canvas texture specified by the `canvasTexture` RID.

### CompositorCreate

```csharp
public Rid CompositorCreate()
```

Creates a new compositor and adds it to the RenderingServer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

### CompositorEffectCreate

```csharp
public Rid CompositorEffectCreate()
```

Creates a new rendering effect and adds it to the RenderingServer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

### CompositorEffectSetCallback

```csharp
public void CompositorEffectSetCallback(Rid effect, RenderingServer.CompositorEffectCallbackType callbackType, Callable callback)
```

Sets the callback type (`callbackType`) and callback method(`callback`) for this rendering effect.

### CompositorEffectSetEnabled

```csharp
public void CompositorEffectSetEnabled(Rid effect, bool enabled)
```

Enables/disables this rendering effect.

### CompositorEffectSetFlag

```csharp
public void CompositorEffectSetFlag(Rid effect, RenderingServer.CompositorEffectFlags flag, bool set)
```

Sets the flag (`flag`) for this rendering effect to `true` or `false` (`set`).

### CompositorSetCompositorEffects

```csharp
public void CompositorSetCompositorEffects(Rid compositor, Array<Rid> effects)
```

Sets the compositor effects for the specified compositor RID. `effects` should be an array containing RIDs created with `CompositorEffectCreate()`.

### CreateLocalRenderingDevice

```csharp
public RenderingDevice CreateLocalRenderingDevice()
```

Creates a RenderingDevice that can be used to do draw and compute operations on a separate thread. Cannot draw to the screen nor share data with the global RenderingDevice.

**Note:** When using the OpenGL rendering driver or when running in headless mode, this function always returns `null`.

### DebugCanvasItemGetRect

```csharp
public Rect2 DebugCanvasItemGetRect(Rid item)
```

Returns the bounding rectangle for a canvas item in local space, as calculated by the renderer. This bound is used internally for culling.

**Warning:** This function is intended for debugging in the editor, and will pass through and return a zero [`Rect2`](Rect2.md) in exported projects.

### DecalCreate

```csharp
public Rid DecalCreate()
```

Creates a decal and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `decal_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this decal to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`Decal`](Decal.md).

### DecalSetAlbedoMix

```csharp
public void DecalSetAlbedoMix(Rid decal, float albedoMix)
```

Sets the `albedoMix` in the decal specified by the `decal` RID. Equivalent to [`Decal.AlbedoMix`](Decal.md).

### DecalSetCullMask

```csharp
public void DecalSetCullMask(Rid decal, uint mask)
```

Sets the cull `mask` in the decal specified by the `decal` RID. Equivalent to [`Decal.CullMask`](Decal.md).

### DecalSetDistanceFade

```csharp
public void DecalSetDistanceFade(Rid decal, bool enabled, float begin, float length)
```

Sets the distance fade parameters in the decal specified by the `decal` RID. Equivalent to [`Decal.DistanceFadeEnabled`](Decal.md), [`Decal.DistanceFadeBegin`](Decal.md) and [`Decal.DistanceFadeLength`](Decal.md).

### DecalSetEmissionEnergy

```csharp
public void DecalSetEmissionEnergy(Rid decal, float energy)
```

Sets the emission `energy` in the decal specified by the `decal` RID. Equivalent to [`Decal.EmissionEnergy`](Decal.md).

### DecalSetFade

```csharp
public void DecalSetFade(Rid decal, float above, float below)
```

Sets the upper fade (`above`) and lower fade (`below`) in the decal specified by the `decal` RID. Equivalent to [`Decal.UpperFade`](Decal.md) and [`Decal.LowerFade`](Decal.md).

### DecalSetModulate

```csharp
public void DecalSetModulate(Rid decal, Color color)
```

Sets the color multiplier in the decal specified by the `decal` RID to `color`. Equivalent to [`Decal.Modulate`](Decal.md).

### DecalSetNormalFade

```csharp
public void DecalSetNormalFade(Rid decal, float fade)
```

Sets the normal `fade` in the decal specified by the `decal` RID. Equivalent to [`Decal.NormalFade`](Decal.md).

### DecalSetSize

```csharp
public void DecalSetSize(Rid decal, Vector3 size)
```

Sets the `size` of the decal specified by the `decal` RID. Equivalent to [`Decal.Size`](Decal.md).

### DecalSetTexture

```csharp
public void DecalSetTexture(Rid decal, RenderingServer.DecalTexture type, Rid texture)
```

Sets the `texture` in the given texture `type` slot for the specified decal. Equivalent to [`Decal.SetTexture`](Decal.md).

### DecalsSetFilter

```csharp
public void DecalsSetFilter(RenderingServer.DecalFilter filter)
```

Sets the texture `filter` mode to use when rendering decals. This parameter is global and cannot be set on a per-decal basis.

### DirectionalLightCreate

```csharp
public Rid DirectionalLightCreate()
```

Creates a directional light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this directional light to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`DirectionalLight3D`](DirectionalLight3D.md).

### DirectionalShadowAtlasSetSize

```csharp
public void DirectionalShadowAtlasSetSize(int size, bool is16Bits)
```

Sets the `size` of the directional light shadows in 3D. See also `ProjectSettings.rendering/lights_and_shadows/directional_shadow/size`. This parameter is global and cannot be set on a per-viewport basis.

### DirectionalSoftShadowFilterSetQuality

```csharp
public void DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality quality)
```

Sets the filter `quality` for directional light shadows in 3D. See also `ProjectSettings.rendering/lights_and_shadows/directional_shadow/soft_shadow_filter_quality`. This parameter is global and cannot be set on a per-viewport basis.

### EnvironmentBakePanorama

```csharp
public Image EnvironmentBakePanorama(Rid environment, bool bakeIrradiance, Vector2I size)
```

Generates and returns an [`Image`](Image.md) containing the radiance map for the specified `environment` RID's sky. This supports built-in sky material and custom sky shaders. If `bakeIrradiance` is `true`, the irradiance map is saved instead of the radiance map. The radiance map is used to render reflected light, while the irradiance map is used to render ambient light. See also `SkyBakePanorama()`.

**Note:** The image is saved using linear encoding without any tonemapping performed, which means it will look too dark if viewed directly in an image editor.

**Note:** `size` should be a 2:1 aspect ratio for the generated panorama to have square pixels. For radiance maps, there is no point in using a height greater than [`Sky.RadianceSize`](Sky.md), as it won't increase detail. Irradiance maps only contain low-frequency data, so there is usually no point in going past a size of 128×64 pixels when saving an irradiance map.

### EnvironmentCreate

```csharp
public Rid EnvironmentCreate()
```

Creates an environment and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `environment_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Environment`](Environment.md).

### EnvironmentGlowSetUseBicubicUpscale

```csharp
public void EnvironmentGlowSetUseBicubicUpscale(bool enable)
```

If `enable` is `true`, enables bicubic upscaling for glow which improves quality at the cost of performance. Equivalent to `ProjectSettings.rendering/environment/glow/upscale_mode`.

**Note:** This setting is only effective when using the Forward+ or Mobile rendering methods, as Compatibility uses a different glow implementation.

### EnvironmentSetAdjustment

```csharp
public void EnvironmentSetAdjustment(Rid env, bool enable, float brightness, float contrast, float saturation, bool use1DColorCorrection, Rid colorCorrection)
```

Sets the values to be used with the "adjustments" post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetAmbientLight

```csharp
public void EnvironmentSetAmbientLight(Rid env, Color color, RenderingServer.EnvironmentAmbientSource ambient = RenderingServer.EnvironmentAmbientSource.Bg, float energy = 1f, float skyContribution = 0f, RenderingServer.EnvironmentReflectionSource reflectionSource = RenderingServer.EnvironmentReflectionSource.Bg)
```

Sets the values to be used for ambient light rendering. See [`Environment`](Environment.md) for more details.

### EnvironmentSetBackground

```csharp
public void EnvironmentSetBackground(Rid env, RenderingServer.EnvironmentBG bg)
```

Sets the environment's background mode. Equivalent to [`Environment.BackgroundMode`](Environment.md).

### EnvironmentSetBgColor

```csharp
public void EnvironmentSetBgColor(Rid env, Color color)
```

Color displayed for clear areas of the scene. Only effective if using the [`RenderingServer.EnvironmentBG.Color`](RenderingServer.md) background mode.

### EnvironmentSetBgEnergy

```csharp
public void EnvironmentSetBgEnergy(Rid env, float multiplier, float exposureValue)
```

Sets the intensity of the background color.

### EnvironmentSetCameraId

```csharp
public void EnvironmentSetCameraId(Rid env, int id)
```

Sets the camera ID to be used as environment background.

### EnvironmentSetCanvasMaxLayer

```csharp
public void EnvironmentSetCanvasMaxLayer(Rid env, int maxLayer)
```

Sets the maximum layer to use if using Canvas background mode.

### EnvironmentSetFog

```csharp
public void EnvironmentSetFog(Rid env, bool enable, Color lightColor, float lightEnergy, float sunScatter, float density, float height, float heightDensity, float aerialPerspective, float skyAffect)
```

Configures fog for the specified environment RID. See `fog_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetFog

```csharp
public void EnvironmentSetFog(Rid env, bool enable, Color lightColor, float lightEnergy, float sunScatter, float density, float height, float heightDensity, float aerialPerspective, float skyAffect, RenderingServer.EnvironmentFogMode fogMode = RenderingServer.EnvironmentFogMode.Exponential)
```

Configures fog for the specified environment RID. See `fog_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetFogDepth

```csharp
public void EnvironmentSetFogDepth(Rid env, float curve, float begin, float end)
```

Configures fog depth for the specified environment RID. Only has an effect when the fog mode of the environment is [`RenderingServer.EnvironmentFogMode.Depth`](RenderingServer.md). See `fog_depth_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetGlow

```csharp
public void EnvironmentSetGlow(Rid env, bool enable, ReadOnlySpan<float> levels, float intensity, float strength, float mix, float bloomThreshold, RenderingServer.EnvironmentGlowBlendMode blendMode, float hdrBleedThreshold, float hdrBleedScale, float hdrLuminanceCap, float glowMapStrength, Rid glowMap)
```

Configures glow for the specified environment RID. See `glow_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetGlow

```csharp
public void EnvironmentSetGlow(Rid env, bool enable, float[] levels, float intensity, float strength, float mix, float bloomThreshold, RenderingServer.EnvironmentGlowBlendMode blendMode, float hdrBleedThreshold, float hdrBleedScale, float hdrLuminanceCap, float glowMapStrength, Rid glowMap)
```

Configures glow for the specified environment RID. See `glow_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetSdfgi

```csharp
public void EnvironmentSetSdfgi(Rid env, bool enable, int cascades, float minCellSize, RenderingServer.EnvironmentSdfgiyScale yScale, bool useOcclusion, float bounceFeedback, bool readSky, float energy, float normalBias, float probeBias)
```

Configures signed distance field global illumination for the specified environment RID. See `sdfgi_*` properties in [`Environment`](Environment.md) for more information.

### EnvironmentSetSdfgiFramesToConverge

```csharp
public void EnvironmentSetSdfgiFramesToConverge(RenderingServer.EnvironmentSdfgiFramesToConverge frames)
```

Sets the number of frames to use for converging signed distance field global illumination. Equivalent to `ProjectSettings.rendering/global_illumination/sdfgi/frames_to_converge`.

### EnvironmentSetSdfgiFramesToUpdateLight

```csharp
public void EnvironmentSetSdfgiFramesToUpdateLight(RenderingServer.EnvironmentSdfgiFramesToUpdateLight frames)
```

Sets the update speed for dynamic lights' indirect lighting when computing signed distance field global illumination. Equivalent to `ProjectSettings.rendering/global_illumination/sdfgi/frames_to_update_lights`.

### EnvironmentSetSdfgiRayCount

```csharp
public void EnvironmentSetSdfgiRayCount(RenderingServer.EnvironmentSdfgiRayCount rayCount)
```

Sets the number of rays to throw per frame when computing signed distance field global illumination. Equivalent to `ProjectSettings.rendering/global_illumination/sdfgi/probe_ray_count`.

### EnvironmentSetSky

```csharp
public void EnvironmentSetSky(Rid env, Rid sky)
```

Sets the [`Sky`](Sky.md) to be used as the environment's background when using *BGMode* sky. Equivalent to [`Environment.Sky`](Environment.md).

### EnvironmentSetSkyCustomFov

```csharp
public void EnvironmentSetSkyCustomFov(Rid env, float scale)
```

Sets a custom field of view for the background [`Sky`](Sky.md). Equivalent to [`Environment.SkyCustomFov`](Environment.md).

### EnvironmentSetSkyOrientation

```csharp
public void EnvironmentSetSkyOrientation(Rid env, Basis orientation)
```

Sets the rotation of the background [`Sky`](Sky.md) expressed as a [`Basis`](Basis.md). Equivalent to [`Environment.SkyRotation`](Environment.md), where the rotation vector is used to construct the [`Basis`](Basis.md).

### EnvironmentSetSsao

```csharp
public void EnvironmentSetSsao(Rid env, bool enable, float radius, float intensity, float power, float detail, float horizon, float sharpness, float lightAffect, float aOChannelAffect)
```

Sets the variables to be used with the screen-space ambient occlusion (SSAO) post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetSsaoQuality

```csharp
public void EnvironmentSetSsaoQuality(RenderingServer.EnvironmentSsaoQuality quality, bool halfSize, float adaptiveTarget, int blurPasses, float fadeOutFrom, float fadeOutTo)
```

Sets the quality level of the screen-space ambient occlusion (SSAO) post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetSsilQuality

```csharp
public void EnvironmentSetSsilQuality(RenderingServer.EnvironmentSsilQuality quality, bool halfSize, float adaptiveTarget, int blurPasses, float fadeOutFrom, float fadeOutTo)
```

Sets the quality level of the screen-space indirect lighting (SSIL) post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetSsr

```csharp
public void EnvironmentSetSsr(Rid env, bool enable, int maxSteps, float fadeIn, float fadeOut, float depthTolerance)
```

Sets the variables to be used with the screen-space reflections (SSR) post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetSsrHalfSize

```csharp
public void EnvironmentSetSsrHalfSize(bool halfSize)
```

Sets whether screen-space reflections will be rendered at full or half size. Half size is faster, but may look pixelated or cause flickering.

### EnvironmentSetSsrRoughnessQuality

```csharp
public void EnvironmentSetSsrRoughnessQuality(RenderingServer.EnvironmentSsrRoughnessQuality quality)
```

### EnvironmentSetTonemap

```csharp
public void EnvironmentSetTonemap(Rid env, RenderingServer.EnvironmentToneMapper toneMapper, float exposure, float white)
```

Sets the variables to be used with the "tonemap" post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetTonemapAgxContrast

```csharp
public void EnvironmentSetTonemapAgxContrast(Rid env, float agxContrast)
```

See [`Environment.TonemapAgxContrast`](Environment.md) for more details.

### EnvironmentSetVolumetricFog

```csharp
public void EnvironmentSetVolumetricFog(Rid env, bool enable, float density, Color albedo, Color emission, float emissionEnergy, float anisotropy, float length, float detailSpread, float gIInject, bool temporalReprojection, float temporalReprojectionAmount, float ambientInject, float skyAffect)
```

Sets the variables to be used with the volumetric fog post-process effect. See [`Environment`](Environment.md) for more details.

### EnvironmentSetVolumetricFogFilterActive

```csharp
public void EnvironmentSetVolumetricFogFilterActive(bool active)
```

Enables filtering of the volumetric fog scattering buffer. This results in much smoother volumes with very few under-sampling artifacts.

### EnvironmentSetVolumetricFogVolumeSize

```csharp
public void EnvironmentSetVolumetricFogVolumeSize(int size, int depth)
```

Sets the resolution of the volumetric fog's froxel buffer. `size` is modified by the screen's aspect ratio and then used to set the width and height of the buffer. While `depth` is directly used to set the depth of the buffer.

### FogVolumeCreate

```csharp
public Rid FogVolumeCreate()
```

Creates a new fog volume and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `fog_volume_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`FogVolume`](FogVolume.md).

### FogVolumeSetMaterial

```csharp
public void FogVolumeSetMaterial(Rid fogVolume, Rid material)
```

Sets the [`Material`](Material.md) of the fog volume. Can be either a [`FogMaterial`](FogMaterial.md) or a custom [`ShaderMaterial`](ShaderMaterial.md).

### FogVolumeSetShape

```csharp
public void FogVolumeSetShape(Rid fogVolume, RenderingServer.FogVolumeShape shape)
```

Sets the shape of the fog volume to either [`RenderingServer.FogVolumeShape.Ellipsoid`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cone`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cylinder`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Box`](RenderingServer.md) or [`RenderingServer.FogVolumeShape.World`](RenderingServer.md).

### FogVolumeSetSize

```csharp
public void FogVolumeSetSize(Rid fogVolume, Vector3 size)
```

Sets the size of the fog volume when shape is [`RenderingServer.FogVolumeShape.Ellipsoid`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cone`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cylinder`](RenderingServer.md) or [`RenderingServer.FogVolumeShape.Box`](RenderingServer.md).

### ForceDraw

```csharp
public void ForceDraw(bool swapBuffers = true, double frameStep = 0)
```

Forces redrawing of all viewports at once. Must be called from the main thread.

### ForceSync

```csharp
public void ForceSync()
```

Forces a synchronization between the CPU and GPU, which may be required in certain cases. Only call this when needed, as CPU-GPU synchronization has a performance cost.

### FreeRid

```csharp
public void FreeRid(Rid rid)
```

Tries to free an object in the RenderingServer. To avoid memory leaks, this should be called after using an object as memory management does not occur automatically when using RenderingServer directly.

### GISetUseHalfResolution

```csharp
public void GISetUseHalfResolution(bool halfResolution)
```

If `halfResolution` is `true`, renders [`VoxelGI`](VoxelGI.md) and SDFGI ([`Environment.SdfgiEnabled`](Environment.md)) buffers at halved resolution on each axis (e.g. 960×540 when the viewport size is 1920×1080). This improves performance significantly when VoxelGI or SDFGI is enabled, at the cost of artifacts that may be visible on polygon edges. The loss in quality becomes less noticeable as the viewport resolution increases. [`LightmapGI`](LightmapGI.md) rendering is not affected by this setting. Equivalent to `ProjectSettings.rendering/global_illumination/gi/use_half_resolution`.

### GetCurrentRenderingDriverName

```csharp
public string GetCurrentRenderingDriverName()
```

Returns the name of the current rendering driver. This can be `vulkan`, `d3d12`, `metal`, `opengl3`, `opengl3_es`, or `opengl3_angle`. See also `GetCurrentRenderingMethod()`.

When `ProjectSettings.rendering/renderer/rendering_method` is `forward_plus` or `mobile`, the rendering driver is determined by `ProjectSettings.rendering/rendering_device/driver`.

When `ProjectSettings.rendering/renderer/rendering_method` is `gl_compatibility`, the rendering driver is determined by `ProjectSettings.rendering/gl_compatibility/driver`.

The rendering driver is also determined by the `--rendering-driver` command line argument that overrides this project setting, or an automatic fallback that is applied depending on the hardware.

### GetCurrentRenderingMethod

```csharp
public string GetCurrentRenderingMethod()
```

Returns the name of the current rendering method. This can be `forward_plus`, `mobile`, or `gl_compatibility`. See also `GetCurrentRenderingDriverName()`.

The rendering method is determined by `ProjectSettings.rendering/renderer/rendering_method`, the `--rendering-method` command line argument that overrides this project setting, or an automatic fallback that is applied depending on the hardware.

### GetDefaultClearColor

```csharp
public Color GetDefaultClearColor()
```

Returns the default clear color which is used when a specific clear color has not been selected. See also `SetDefaultClearColor()`.

### GetFrameSetupTimeCpu

```csharp
public double GetFrameSetupTimeCpu()
```

Returns the time taken to setup rendering on the CPU in milliseconds. This value is shared across all viewports and does *not* require `ViewportSetMeasureRenderTime()` to be enabled on a viewport to be queried. See also `ViewportGetMeasuredRenderTimeCpu()`.

### GetRenderingDevice

```csharp
public RenderingDevice GetRenderingDevice()
```

Returns the global RenderingDevice.

**Note:** When using the OpenGL rendering driver or when running in headless mode, this function always returns `null`.

### GetRenderingInfo

```csharp
public ulong GetRenderingInfo(RenderingServer.RenderingInfo info)
```

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
public Array<Dictionary> GetShaderParameterList(Rid shader)
```

Returns the parameters of a shader.

### GetTestCube

```csharp
public Rid GetTestCube()
```

Returns the RID of the test cube. This mesh will be created and returned on the first call to `GetTestCube()`, then it will be cached for subsequent calls. See also `MakeSphereMesh()`.

### GetTestTexture

```csharp
public Rid GetTestTexture()
```

Returns the RID of a 256×256 texture with a testing pattern on it (in [`Image.Format.Rgb8`](Image.md) format). This texture will be created and returned on the first call to `GetTestTexture()`, then it will be cached for subsequent calls. See also `GetWhiteTexture()`.

**Example:** Get the test texture and apply it to a [`Sprite2D`](Sprite2D.md) node:

```csharp
var texture_rid = RenderingServer.get_test_texture()
var texture = ImageTexture.create_from_image(RenderingServer.texture_2d_get(texture_rid))
$Sprite2D.texture = texture

```

### GetVideoAdapterApiVersion

```csharp
public string GetVideoAdapterApiVersion()
```

Returns the version of the graphics video adapter *currently in use* (e.g. "1.2.189" for Vulkan, "3.3.0 NVIDIA 510.60.02" for OpenGL). This version may be different from the actual latest version supported by the hardware, as Godot may not always request the latest version. See also [`OS.GetVideoAdapterDriverInfo`](OS.md).

**Note:** When running a headless or server binary, this function returns an empty string.

### GetVideoAdapterName

```csharp
public string GetVideoAdapterName()
```

Returns the name of the video adapter (e.g. "GeForce GTX 1080/PCIe/SSE2").

**Note:** When running a headless or server binary, this function returns an empty string.

**Note:** On the web platform, some browsers such as Firefox may report a different, fixed GPU name such as "GeForce GTX 980" (regardless of the user's actual GPU model). This is done to make fingerprinting more difficult.

### GetVideoAdapterType

```csharp
public RenderingDevice.DeviceType GetVideoAdapterType()
```

Returns the type of the video adapter. Since dedicated graphics cards from a given generation will *usually* be significantly faster than integrated graphics made in the same generation, the device type can be used as a basis for automatic graphics settings adjustment. However, this is not always true, so make sure to provide users with a way to manually override graphics settings.

**Note:** When using the OpenGL rendering driver or when running in headless mode, this function always returns [`RenderingDevice.DeviceType.Other`](RenderingDevice.md).

### GetVideoAdapterVendor

```csharp
public string GetVideoAdapterVendor()
```

Returns the vendor of the video adapter (e.g. "NVIDIA Corporation").

**Note:** When running a headless or server binary, this function returns an empty string.

### GetWhiteTexture

```csharp
public Rid GetWhiteTexture()
```

Returns the ID of a 4×4 white texture (in [`Image.Format.Rgb8`](Image.md) format). This texture will be created and returned on the first call to `GetWhiteTexture()`, then it will be cached for subsequent calls. See also `GetTestTexture()`.

**Example:** Get the white texture and apply it to a [`Sprite2D`](Sprite2D.md) node:

```csharp
var texture_rid = RenderingServer.get_white_texture()
var texture = ImageTexture.create_from_image(RenderingServer.texture_2d_get(texture_rid))
$Sprite2D.texture = texture

```

### GlobalShaderParameterAdd

```csharp
public void GlobalShaderParameterAdd(StringName name, RenderingServer.GlobalShaderParameterType type, Variant defaultValue)
```

Creates a new global shader uniform.

**Note:** Global shader parameter names are case-sensitive.

### GlobalShaderParameterGet

```csharp
public Variant GlobalShaderParameterGet(StringName name)
```

Returns the value of the global shader uniform specified by `name`.

**Note:** `GlobalShaderParameterGet()` has a large performance penalty as the rendering thread needs to synchronize with the calling thread, which is slow. Do not use this method during gameplay to avoid stuttering. If you need to read values in a script after setting them, consider creating an autoload where you store the values you need to query at the same time you're setting them as global parameters.

### GlobalShaderParameterGetList

```csharp
public Array<StringName> GlobalShaderParameterGetList()
```

Returns the list of global shader uniform names.

**Note:** `GlobalShaderParameterGet()` has a large performance penalty as the rendering thread needs to synchronize with the calling thread, which is slow. Do not use this method during gameplay to avoid stuttering. If you need to read values in a script after setting them, consider creating an autoload where you store the values you need to query at the same time you're setting them as global parameters.

### GlobalShaderParameterGetType

```csharp
public RenderingServer.GlobalShaderParameterType GlobalShaderParameterGetType(StringName name)
```

Returns the type associated to the global shader uniform specified by `name`.

**Note:** `GlobalShaderParameterGet()` has a large performance penalty as the rendering thread needs to synchronize with the calling thread, which is slow. Do not use this method during gameplay to avoid stuttering. If you need to read values in a script after setting them, consider creating an autoload where you store the values you need to query at the same time you're setting them as global parameters.

### GlobalShaderParameterRemove

```csharp
public void GlobalShaderParameterRemove(StringName name)
```

Removes the global shader uniform specified by `name`.

### GlobalShaderParameterSet

```csharp
public void GlobalShaderParameterSet(StringName name, Variant value)
```

Sets the global shader uniform `name` to `value`.

### GlobalShaderParameterSetOverride

```csharp
public void GlobalShaderParameterSetOverride(StringName name, Variant value)
```

Overrides the global shader uniform `name` with `value`. Equivalent to the [`ShaderGlobalsOverride`](ShaderGlobalsOverride.md) node.

### HasChanged

```csharp
public bool HasChanged()
```

Returns `true` if changes have been made to the RenderingServer's data. `ForceDraw()` is usually called if this happens.

### HasFeature

```csharp
public bool HasFeature(RenderingServer.Features feature)
```

This method does nothing and always returns `false`.

### HasOsFeature

```csharp
public bool HasOsFeature(string feature)
```

Returns `true` if the OS supports a certain `feature`. Features might be `s3tc`, `etc`, and `etc2`.

### InstanceAttachObjectInstanceId

```csharp
public void InstanceAttachObjectInstanceId(Rid instance, ulong id)
```

Attaches a unique Object ID to instance. Object ID must be attached to instance for proper culling with `InstancesCullAabb()`, `InstancesCullConvex()`, and `InstancesCullRay()`.

### InstanceAttachSkeleton

```csharp
public void InstanceAttachSkeleton(Rid instance, Rid skeleton)
```

Attaches a skeleton to an instance. Removes the previous skeleton from the instance.

### InstanceCreate

```csharp
public Rid InstanceCreate()
```

Creates a visual instance and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `instance_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

An instance is a way of placing a 3D object in the scenario. Objects like particles, meshes, reflection probes and decals need to be associated with an instance to be visible in the scenario using `InstanceSetBase()`.

**Note:** The equivalent node is [`VisualInstance3D`](VisualInstance3D.md).

### InstanceCreate2

```csharp
public Rid InstanceCreate2(Rid base, Rid scenario)
```

Creates a visual instance, adds it to the RenderingServer, and sets both base and scenario. It can be accessed with the RID that is returned. This RID will be used in all `instance_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method. This is a shorthand for using `InstanceCreate()` and setting the base and scenario manually.

### InstanceGeometryGetShaderParameter

```csharp
public Variant InstanceGeometryGetShaderParameter(Rid instance, StringName parameter)
```

Returns the value of the per-instance shader uniform from the specified 3D geometry instance. Equivalent to [`GeometryInstance3D.GetInstanceShaderParameter`](GeometryInstance3D.md).

**Note:** Per-instance shader parameter names are case-sensitive.

### InstanceGeometryGetShaderParameterDefaultValue

```csharp
public Variant InstanceGeometryGetShaderParameterDefaultValue(Rid instance, StringName parameter)
```

Returns the default value of the per-instance shader uniform from the specified 3D geometry instance. Equivalent to [`GeometryInstance3D.GetInstanceShaderParameter`](GeometryInstance3D.md).

### InstanceGeometryGetShaderParameterList

```csharp
public Array<Dictionary> InstanceGeometryGetShaderParameterList(Rid instance)
```

Returns a dictionary of per-instance shader uniform names of the per-instance shader uniform from the specified 3D geometry instance. The returned dictionary is in PropertyInfo format, with the keys `name`, `class_name`, `type`, `hint`, `hint_string` and `usage`. Equivalent to [`GeometryInstance3D.GetInstanceShaderParameter`](GeometryInstance3D.md).

### InstanceGeometrySetCastShadowsSetting

```csharp
public void InstanceGeometrySetCastShadowsSetting(Rid instance, RenderingServer.ShadowCastingSetting shadowCastingSetting)
```

Sets the shadow casting setting. Equivalent to [`GeometryInstance3D.CastShadow`](GeometryInstance3D.md).

### InstanceGeometrySetFlag

```csharp
public void InstanceGeometrySetFlag(Rid instance, RenderingServer.InstanceFlags flag, bool enabled)
```

Sets the `flag` for a given `instance` to `enabled`.

### InstanceGeometrySetLightmap

```csharp
public void InstanceGeometrySetLightmap(Rid instance, Rid lightmap, Rect2 lightmapUVScale, int lightmapSlice)
```

Sets the lightmap GI instance to use for the specified 3D geometry instance. The lightmap UV scale for the specified instance (equivalent to [`GeometryInstance3D.GILightmapScale`](GeometryInstance3D.md)) and lightmap atlas slice must also be specified.

### InstanceGeometrySetLodBias

```csharp
public void InstanceGeometrySetLodBias(Rid instance, float lodBias)
```

Sets the level of detail bias to use when rendering the specified 3D geometry instance. Higher values result in higher detail from further away. Equivalent to [`GeometryInstance3D.LodBias`](GeometryInstance3D.md).

### InstanceGeometrySetMaterialOverlay

```csharp
public void InstanceGeometrySetMaterialOverlay(Rid instance, Rid material)
```

Sets a material that will be rendered for all surfaces on top of active materials for the mesh associated with this instance. Equivalent to [`GeometryInstance3D.MaterialOverlay`](GeometryInstance3D.md).

### InstanceGeometrySetMaterialOverride

```csharp
public void InstanceGeometrySetMaterialOverride(Rid instance, Rid material)
```

Sets a material that will override the material for all surfaces on the mesh associated with this instance. Equivalent to [`GeometryInstance3D.MaterialOverride`](GeometryInstance3D.md).

### InstanceGeometrySetShaderParameter

```csharp
public void InstanceGeometrySetShaderParameter(Rid instance, StringName parameter, Variant value)
```

Sets the per-instance shader uniform on the specified 3D geometry instance. Equivalent to [`GeometryInstance3D.SetInstanceShaderParameter`](GeometryInstance3D.md).

### InstanceGeometrySetTransparency

```csharp
public void InstanceGeometrySetTransparency(Rid instance, float transparency)
```

Sets the transparency for the given geometry instance. Equivalent to [`GeometryInstance3D.Transparency`](GeometryInstance3D.md).

A transparency of `0.0` is fully opaque, while `1.0` is fully transparent. Values greater than `0.0` (exclusive) will force the geometry's materials to go through the transparent pipeline, which is slower to render and can exhibit rendering issues due to incorrect transparency sorting. However, unlike using a transparent material, setting `transparency` to a value greater than `0.0` (exclusive) will *not* disable shadow rendering.

In spatial shaders, `1.0 - transparency` is set as the default value of the `ALPHA` built-in.

**Note:** `transparency` is clamped between `0.0` and `1.0`, so this property cannot be used to make transparent materials more opaque than they originally are.

### InstanceGeometrySetVisibilityRange

```csharp
public void InstanceGeometrySetVisibilityRange(Rid instance, float min, float max, float minMargin, float maxMargin, RenderingServer.VisibilityRangeFadeMode fadeMode)
```

Sets the visibility range values for the given geometry instance. Equivalent to [`GeometryInstance3D.VisibilityRangeBegin`](GeometryInstance3D.md) and related properties.

### InstanceResetPhysicsInterpolation

```csharp
public void InstanceResetPhysicsInterpolation(Rid instance)
```

### InstanceSetBase

```csharp
public void InstanceSetBase(Rid instance, Rid base)
```

Sets the base of the instance. A base can be any of the 3D objects that are created in the RenderingServer that can be displayed. For example, any of the light types, mesh, multimesh, particle system, reflection probe, decal, lightmap, voxel GI and visibility notifiers are all types that can be set as the base of an instance in order to be displayed in the scenario.

### InstanceSetBlendShapeWeight

```csharp
public void InstanceSetBlendShapeWeight(Rid instance, int shape, float weight)
```

Sets the weight for a given blend shape associated with this instance.

### InstanceSetCustomAabb

```csharp
public void InstanceSetCustomAabb(Rid instance, Aabb aabb)
```

Sets a custom AABB to use when culling objects from the view frustum. Equivalent to setting [`GeometryInstance3D.CustomAabb`](GeometryInstance3D.md).

### InstanceSetExtraVisibilityMargin

```csharp
public void InstanceSetExtraVisibilityMargin(Rid instance, float margin)
```

Sets a margin to increase the size of the AABB when culling objects from the view frustum. This allows you to avoid culling objects that fall outside the view frustum. Equivalent to [`GeometryInstance3D.ExtraCullMargin`](GeometryInstance3D.md).

### InstanceSetIgnoreCulling

```csharp
public void InstanceSetIgnoreCulling(Rid instance, bool enabled)
```

If `true`, ignores all culling on the specified 3D geometry instance, including frustum culling, occlusion culling, and layer culling. This is not the same as [`GeometryInstance3D.IgnoreOcclusionCulling`](GeometryInstance3D.md), which only ignores occlusion culling but leaves frustum and layer culling intact.

### InstanceSetInterpolated

```csharp
public void InstanceSetInterpolated(Rid instance, bool interpolated)
```

### InstanceSetLayerMask

```csharp
public void InstanceSetLayerMask(Rid instance, uint mask)
```

Sets the render layers that this instance will be drawn to. Equivalent to [`VisualInstance3D.Layers`](VisualInstance3D.md).

### InstanceSetPivotData

```csharp
public void InstanceSetPivotData(Rid instance, float sortingOffset, bool useAabbCenter)
```

Sets the sorting offset and switches between using the bounding box or instance origin for depth sorting.

### InstanceSetScenario

```csharp
public void InstanceSetScenario(Rid instance, Rid scenario)
```

Sets the scenario that the instance is in. The scenario is the 3D world that the objects will be displayed in.

### InstanceSetSurfaceOverrideMaterial

```csharp
public void InstanceSetSurfaceOverrideMaterial(Rid instance, int surface, Rid material)
```

Sets the override material of a specific surface. Equivalent to [`MeshInstance3D.SetSurfaceOverrideMaterial`](MeshInstance3D.md).

### InstanceSetTransform

```csharp
public void InstanceSetTransform(Rid instance, Transform3D transform)
```

Sets the world space transform of the instance. Equivalent to [`Node3D.GlobalTransform`](Node3D.md).

### InstanceSetVisibilityParent

```csharp
public void InstanceSetVisibilityParent(Rid instance, Rid parent)
```

Sets the visibility parent for the given instance. Equivalent to [`Node3D.VisibilityParent`](Node3D.md).

### InstanceSetVisible

```csharp
public void InstanceSetVisible(Rid instance, bool visible)
```

Sets whether an instance is drawn or not. Equivalent to [`Node3D.Visible`](Node3D.md).

### InstanceTeleport

```csharp
public void InstanceTeleport(Rid instance)
```

Resets motion vectors and other interpolated values. Use this *after* teleporting a mesh from one position to another to avoid ghosting artifacts.

### InstancesCullAabb

```csharp
public long[] InstancesCullAabb(Aabb aabb, Rid scenario = default)
```

Returns an array of object IDs intersecting with the provided AABB. Only 3D nodes that inherit from [`VisualInstance3D`](VisualInstance3D.md) are considered, such as [`MeshInstance3D`](MeshInstance3D.md) or [`DirectionalLight3D`](DirectionalLight3D.md). Use `GodotObject.InstanceFromId` to obtain the actual nodes. A scenario RID must be provided, which is available in the [`World3D`](World3D.md) you want to query. This forces an update for all resources queued to update.

**Warning:** This function is primarily intended for editor usage. For in-game use cases, prefer physics collision.

### InstancesCullConvex

```csharp
public long[] InstancesCullConvex(Array<Plane> convex, Rid scenario = default)
```

Returns an array of object IDs intersecting with the provided convex shape. Only 3D nodes that inherit from [`VisualInstance3D`](VisualInstance3D.md) are considered, such as [`MeshInstance3D`](MeshInstance3D.md) or [`DirectionalLight3D`](DirectionalLight3D.md). Use `GodotObject.InstanceFromId` to obtain the actual nodes. A scenario RID must be provided, which is available in the [`World3D`](World3D.md) you want to query. This forces an update for all resources queued to update.

**Warning:** This function is primarily intended for editor usage. For in-game use cases, prefer physics collision.

### InstancesCullRay

```csharp
public long[] InstancesCullRay(Vector3 from, Vector3 to, Rid scenario = default)
```

Returns an array of object IDs intersecting with the provided 3D ray. Only 3D nodes that inherit from [`VisualInstance3D`](VisualInstance3D.md) are considered, such as [`MeshInstance3D`](MeshInstance3D.md) or [`DirectionalLight3D`](DirectionalLight3D.md). Use `GodotObject.InstanceFromId` to obtain the actual nodes. A scenario RID must be provided, which is available in the [`World3D`](World3D.md) you want to query. This forces an update for all resources queued to update.

**Warning:** This function is primarily intended for editor usage. For in-game use cases, prefer physics collision.

### IsOnRenderThread

```csharp
public bool IsOnRenderThread()
```

Returns `true` if our code is currently executing on the rendering thread.

### IsRenderLoopEnabled

```csharp
public bool IsRenderLoopEnabled()
```

### LightAreaSetNormalizeEnergy

```csharp
public void LightAreaSetNormalizeEnergy(Rid light, bool enable)
```

Defines whether the energy of an [`AreaLight3D`](AreaLight3D.md) is normalized (divided) by its area. If set to `true`, changing the size does not affect the total energy output. Equivalent to [`AreaLight3D.AreaNormalizeEnergy`](AreaLight3D.md).

### LightAreaSetSize

```csharp
public void LightAreaSetSize(Rid light, Vector2 size)
```

Sets the extents (width and height) in meters for this area light. Equivalent to [`AreaLight3D.AreaSize`](AreaLight3D.md).

### LightDirectionalSetBlendSplits

```csharp
public void LightDirectionalSetBlendSplits(Rid light, bool enable)
```

If `true`, this directional light will blend between shadow map splits resulting in a smoother transition between them. Equivalent to [`DirectionalLight3D.DirectionalShadowBlendSplits`](DirectionalLight3D.md).

### LightDirectionalSetShadowMode

```csharp
public void LightDirectionalSetShadowMode(Rid light, RenderingServer.LightDirectionalShadowMode mode)
```

Sets the shadow mode for this directional light. Equivalent to [`DirectionalLight3D.DirectionalShadowMode`](DirectionalLight3D.md).

### LightDirectionalSetSkyMode

```csharp
public void LightDirectionalSetSkyMode(Rid light, RenderingServer.LightDirectionalSkyMode mode)
```

If `true`, this light will not be used for anything except sky shaders. Use this for lights that impact your sky shader that you may want to hide from affecting the rest of the scene. For example, you may want to enable this when the sun in your sky shader falls below the horizon.

### LightOmniSetShadowMode

```csharp
public void LightOmniSetShadowMode(Rid light, RenderingServer.LightOmniShadowMode mode)
```

Sets whether to use a dual paraboloid or a cubemap for the shadow map. Dual paraboloid is faster but may suffer from artifacts. Equivalent to [`OmniLight3D.OmniShadowMode`](OmniLight3D.md).

### LightProjectorsSetFilter

```csharp
public void LightProjectorsSetFilter(RenderingServer.LightProjectorFilter filter)
```

Sets the texture filter mode to use when rendering light projectors. This parameter is global and cannot be set on a per-light basis.

### LightSetBakeMode

```csharp
public void LightSetBakeMode(Rid light, RenderingServer.LightBakeMode bakeMode)
```

Sets the bake mode to use for the specified 3D light. Equivalent to [`Light3D.LightBakeMode`](Light3D.md).

### LightSetColor

```csharp
public void LightSetColor(Rid light, Color color)
```

Sets the color of the light. Equivalent to [`Light3D.LightColor`](Light3D.md).

### LightSetCullMask

```csharp
public void LightSetCullMask(Rid light, uint mask)
```

Sets the cull mask for this 3D light. Lights only affect objects in the selected layers. Equivalent to [`Light3D.LightCullMask`](Light3D.md).

### LightSetDistanceFade

```csharp
public void LightSetDistanceFade(Rid decal, bool enabled, float begin, float shadow, float length)
```

Sets the distance fade for this 3D light. This acts as a form of level of detail (LOD) and can be used to improve performance. Equivalent to [`Light3D.DistanceFadeEnabled`](Light3D.md), [`Light3D.DistanceFadeBegin`](Light3D.md), [`Light3D.DistanceFadeShadow`](Light3D.md), and [`Light3D.DistanceFadeLength`](Light3D.md).

### LightSetMaxSdfgiCascade

```csharp
public void LightSetMaxSdfgiCascade(Rid light, uint cascade)
```

Sets the maximum SDFGI cascade in which the 3D light's indirect lighting is rendered. Higher values allow the light to be rendered in SDFGI further away from the camera.

### LightSetNegative

```csharp
public void LightSetNegative(Rid light, bool enable)
```

If `true`, the 3D light will subtract light instead of adding light. Equivalent to [`Light3D.LightNegative`](Light3D.md).

### LightSetParam

```csharp
public void LightSetParam(Rid light, RenderingServer.LightParam param, float value)
```

Sets the specified 3D light parameter. Equivalent to [`Light3D.SetParam`](Light3D.md).

### LightSetProjector

```csharp
public void LightSetProjector(Rid light, Rid texture)
```

Sets the projector texture to use for the specified 3D light. Equivalent to [`Light3D.LightProjector`](Light3D.md).

### LightSetReverseCullFaceMode

```csharp
public void LightSetReverseCullFaceMode(Rid light, bool enabled)
```

If `true`, reverses the backface culling of the mesh. This can be useful when you have a flat mesh that has a light behind it. If you need to cast a shadow on both sides of the mesh, set the mesh to use double-sided shadows with `InstanceGeometrySetCastShadowsSetting()`. Equivalent to [`Light3D.ShadowReverseCullFace`](Light3D.md).

### LightSetShadow

```csharp
public void LightSetShadow(Rid light, bool enabled)
```

If `true`, light will cast shadows. Equivalent to [`Light3D.ShadowEnabled`](Light3D.md).

### LightSetShadowCasterMask

```csharp
public void LightSetShadowCasterMask(Rid light, uint mask)
```

Sets the shadow caster mask for this 3D light. Shadows will only be cast using objects in the selected layers. Equivalent to [`Light3D.ShadowCasterMask`](Light3D.md).

### LightmapCreate

```csharp
public Rid LightmapCreate()
```

Creates a new lightmap global illumination instance and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `lightmap_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`LightmapGI`](LightmapGI.md).

### LightmapGetProbeCaptureBspTree

```csharp
public int[] LightmapGetProbeCaptureBspTree(Rid lightmap)
```

Returns the BSP tree data used for accelerating probe lookups. The BSP data is structured as a series of six signed 32-bit values per BSP node in this order: `float plane_x`, `float plane_y`, `float plane_z`, `float plane_distance`, `int32_t over`, `int32_t under`. An empty leaf is denoted by the value `-2147483648` (the minimum 32-bit signed integer). See also `LightmapSetProbeCaptureData()`.

### LightmapGetProbeCapturePoints

```csharp
public Vector3[] LightmapGetProbeCapturePoints(Rid lightmap)
```

Returns the *local space* positions of each lightmap probe capture point. Keep in mind the lightmap instance may have a non-zero transform, which will affect the position of the probe capture points. See also `LightmapSetProbeCaptureData()`.

### LightmapGetProbeCaptureSh

```csharp
public Color[] LightmapGetProbeCaptureSh(Rid lightmap)
```

Returns the L0, L1, and L2 [spherical harmonics](https://en.wikipedia.org/wiki/Spherical_harmonics) data for each lightmap probe capture point. This is specified as 9 [`Color`](Color.md) values per probe, which means the size of the returned data is always 9 times the number of probe points. See also `LightmapSetProbeCaptureData()`.

### LightmapGetProbeCaptureTetrahedra

```csharp
public int[] LightmapGetProbeCaptureTetrahedra(Rid lightmap)
```

Returns the tetrahedralization data used for interpolating between lightmap probe capture points. Each tetrahedron is specified as a series of 4 numbers, each being an index into the probe capture points array returned by `LightmapGetProbeCapturePoints()`. See also `LightmapSetProbeCaptureData()`.

### LightmapSetBakedExposureNormalization

```csharp
public void LightmapSetBakedExposureNormalization(Rid lightmap, float bakedExposure)
```

Used to inform the renderer what exposure normalization value was used while baking the lightmap. This value will be used and modulated at run time to ensure that the lightmap maintains a consistent level of exposure even if the scene-wide exposure normalization is changed at run time. For more information see `CameraAttributesSetExposure()`.

### LightmapSetProbeBounds

```csharp
public void LightmapSetProbeBounds(Rid lightmap, Aabb bounds)
```

Sets the bounds that this lightmap instance should visually affect, both in terms of static lightmap baking and probe-based global illumination.

### LightmapSetProbeCaptureData

```csharp
public void LightmapSetProbeCaptureData(Rid lightmap, ReadOnlySpan<Vector3> points, ReadOnlySpan<Color> pointSh, ReadOnlySpan<int> tetrahedra, ReadOnlySpan<int> bspTree)
```

Sets the probe capture data for the given lightmap instance. See `LightmapGetProbeCapturePoints()`, `LightmapGetProbeCaptureSh()`, `LightmapGetProbeCaptureTetrahedra()`, and `LightmapGetProbeCaptureBspTree()` for the expected data formats.

### LightmapSetProbeCaptureData

```csharp
public void LightmapSetProbeCaptureData(Rid lightmap, Vector3[] points, Color[] pointSh, int[] tetrahedra, int[] bspTree)
```

Sets the probe capture data for the given lightmap instance. See `LightmapGetProbeCapturePoints()`, `LightmapGetProbeCaptureSh()`, `LightmapGetProbeCaptureTetrahedra()`, and `LightmapGetProbeCaptureBspTree()` for the expected data formats.

### LightmapSetProbeCaptureUpdateSpeed

```csharp
public void LightmapSetProbeCaptureUpdateSpeed(float speed)
```

The framerate-independent update speed when representing dynamic object lighting from [`LightmapProbe`](LightmapProbe.md)s. Higher values make dynamic object lighting update faster. Higher values can prevent fast-moving objects from having "outdated" indirect lighting displayed on them, at the cost of possible flickering when an object moves from a bright area to a shaded area. See also `ProjectSettings.rendering/lightmapping/probe_capture/update_speed`.

### LightmapSetProbeInterior

```csharp
public void LightmapSetProbeInterior(Rid lightmap, bool interior)
```

Sets whether the lightmap instance should be considered as interior (when `interior` is `true`). If the lightmap is marked as interior, environment lighting is ignored when baking lightmaps.

### LightmapSetTextures

```csharp
public void LightmapSetTextures(Rid lightmap, Rid light, bool usesSh)
```

Set the textures on the given `lightmap` GI instance to the texture array pointed to by the `light` RID. If the lightmap texture was baked with [`LightmapGI.Directional`](LightmapGI.md) set to `true`, then `usesSh` must also be `true`.

### LightmapsSetBicubicFilter

```csharp
public void LightmapsSetBicubicFilter(bool enable)
```

Toggles whether a bicubic filter should be used when lightmaps are sampled. This smoothens their appearance at a performance cost.

### MakeSphereMesh

```csharp
public Rid MakeSphereMesh(int latitudes, int longitudes, float radius)
```

Returns a mesh of a sphere with the given number of horizontal subdivisions, vertical subdivisions and radius. See also `GetTestCube()`.

### MaterialCreate

```csharp
public Rid MaterialCreate()
```

Creates an empty material and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `material_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Material`](Material.md).

### MaterialGetParam

```csharp
public Variant MaterialGetParam(Rid material, StringName parameter)
```

Returns the value of a certain material's parameter.

### MaterialSetNextPass

```csharp
public void MaterialSetNextPass(Rid material, Rid nextMaterial)
```

Sets an object's next material.

### MaterialSetParam

```csharp
public void MaterialSetParam(Rid material, StringName parameter, Variant value)
```

Sets a material's parameter.

### MaterialSetRenderPriority

```csharp
public void MaterialSetRenderPriority(Rid material, int priority)
```

Sets a material's render priority.

### MaterialSetShader

```csharp
public void MaterialSetShader(Rid shaderMaterial, Rid shader)
```

Sets a shader material's shader.

### MaterialSetUseDebanding

```csharp
public void MaterialSetUseDebanding(bool enable)
```

When using the Mobile renderer, `MaterialSetUseDebanding()` can be used to enable or disable the debanding feature of 3D materials ([`BaseMaterial3D`](BaseMaterial3D.md) and [`ShaderMaterial`](ShaderMaterial.md)).

`MaterialSetUseDebanding()` has no effect when using the Compatibility or Forward+ renderer. In Forward+, [`Viewport`](Viewport.md) debanding can be used instead.

See also `ProjectSettings.rendering/anti_aliasing/quality/use_debanding` and [`RenderingServer.ViewportSetUseDebanding`](RenderingServer.md).

### MeshAddSurface

```csharp
public void MeshAddSurface(Rid mesh, Dictionary surface)
```

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
public void MeshAddSurfaceFromArrays(Rid mesh, RenderingServer.PrimitiveType primitive, Array arrays, Array blendShapes = null, Dictionary lods = null, RenderingServer.ArrayFormat compressFormat = RenderingServer.ArrayFormat.FlagFormatVersion1)
```

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
public void MeshClear(Rid mesh)
```

Removes all surfaces from a mesh.

### MeshCreate

```csharp
public Rid MeshCreate()
```

Creates a new mesh and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `mesh_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this mesh to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent resource is [`Mesh`](Mesh.md).

### MeshCreateFromSurfaces

```csharp
public Rid MeshCreateFromSurfaces(Array<Dictionary> surfaces, int blendShapeCount = 0)
```

Creates a new mesh with predefined surfaces for it and adds the mesh to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `mesh_*` RenderingServer functions. This method is more efficient for creating meshes with multiple surfaces compared to creating an empty mesh with `MeshCreate()` and adding surfaces one by one with `MeshAddSurface()`.

Each element in the `surfaces` array must follow the same structure as described in `MeshAddSurface()`. The `blendShapeCount` parameter must match the blend shape data defined in all surfaces.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this mesh to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent resource is [`Mesh`](Mesh.md).

### MeshGetBlendShapeCount

```csharp
public int MeshGetBlendShapeCount(Rid mesh)
```

Returns a mesh's blend shape count.

### MeshGetBlendShapeMode

```csharp
public RenderingServer.BlendShapeMode MeshGetBlendShapeMode(Rid mesh)
```

Returns a mesh's blend shape mode.

### MeshGetCustomAabb

```csharp
public Aabb MeshGetCustomAabb(Rid mesh)
```

Returns a mesh's custom aabb.

### MeshGetSurface

```csharp
public Dictionary MeshGetSurface(Rid mesh, int surface)
```

Returns a mesh's surface as a dictionary following the same structure as described in `MeshAddSurface()`.

### MeshGetSurfaceCount

```csharp
public int MeshGetSurfaceCount(Rid mesh)
```

Returns a mesh's number of surfaces.

### MeshSetBlendShapeMode

```csharp
public void MeshSetBlendShapeMode(Rid mesh, RenderingServer.BlendShapeMode mode)
```

Sets a mesh's blend shape mode.

### MeshSetCustomAabb

```csharp
public void MeshSetCustomAabb(Rid mesh, Aabb aabb)
```

Sets a mesh's custom aabb.

### MeshSetShadowMesh

```csharp
public void MeshSetShadowMesh(Rid mesh, Rid shadowMesh)
```

Sets an optional second mesh which can be used for rendering shadows and the depth prepass. Can be used to increase performance by supplying a mesh with fused vertices and only vertex position data (without normals, UVs, colors, etc.).

**Note:** This mesh must have exactly the same vertex positions as the source mesh (including the source mesh's LODs, if present). If vertex positions differ, then the mesh will not draw correctly.

### MeshSurfaceGetArrays

```csharp
public Array MeshSurfaceGetArrays(Rid mesh, int surface)
```

Returns a mesh's surface's buffer arrays.

### MeshSurfaceGetBlendShapeArrays

```csharp
public Array<Array> MeshSurfaceGetBlendShapeArrays(Rid mesh, int surface)
```

Returns a mesh's surface's arrays for blend shapes.

### MeshSurfaceGetFormatAttributeStride

```csharp
public uint MeshSurfaceGetFormatAttributeStride(RenderingServer.ArrayFormat format, int vertexCount)
```

Returns the stride of the attribute buffer for a mesh with given `format`.

### MeshSurfaceGetFormatIndexStride

```csharp
public uint MeshSurfaceGetFormatIndexStride(RenderingServer.ArrayFormat format, int vertexCount)
```

Returns the stride of the index buffer for a mesh with the given `format`.

### MeshSurfaceGetFormatNormalTangentStride

```csharp
public uint MeshSurfaceGetFormatNormalTangentStride(RenderingServer.ArrayFormat format, int vertexCount)
```

Returns the stride of the combined normals and tangents for a mesh with given `format`. Note importantly that, while normals and tangents are in the vertex buffer with vertices, they are only interleaved with each other and so have a different stride than vertex positions.

### MeshSurfaceGetFormatOffset

```csharp
public uint MeshSurfaceGetFormatOffset(RenderingServer.ArrayFormat format, int vertexCount, int arrayIndex)
```

Returns the offset of a given attribute by `arrayIndex` in the start of its respective buffer.

### MeshSurfaceGetFormatSkinStride

```csharp
public uint MeshSurfaceGetFormatSkinStride(RenderingServer.ArrayFormat format, int vertexCount)
```

Returns the stride of the skin buffer for a mesh with given `format`.

### MeshSurfaceGetFormatVertexStride

```csharp
public uint MeshSurfaceGetFormatVertexStride(RenderingServer.ArrayFormat format, int vertexCount)
```

Returns the stride of the vertex positions for a mesh with given `format`. Note importantly that vertex positions are stored consecutively and are not interleaved with the other attributes in the vertex buffer (normals and tangents).

### MeshSurfaceGetMaterial

```csharp
public Rid MeshSurfaceGetMaterial(Rid mesh, int surface)
```

Returns a mesh's surface's material.

### MeshSurfaceRemove

```csharp
public void MeshSurfaceRemove(Rid mesh, int surface)
```

Removes the surface at the given index from the Mesh, shifting surfaces with higher index down by one.

### MeshSurfaceSetMaterial

```csharp
public void MeshSurfaceSetMaterial(Rid mesh, int surface, Rid material)
```

Sets a mesh's surface's material.

### MeshSurfaceUpdateAttributeRegion

```csharp
public void MeshSurfaceUpdateAttributeRegion(Rid mesh, int surface, int offset, ReadOnlySpan<byte> data)
```

Updates the attribute buffer of the mesh surface with the given `data`. The expected data per attribute is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatAttributeStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each attribute.

A [`Vector3`](Vector3.md)[] of attribute locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateAttributeRegion

```csharp
public void MeshSurfaceUpdateAttributeRegion(Rid mesh, int surface, int offset, byte[] data)
```

Updates the attribute buffer of the mesh surface with the given `data`. The expected data per attribute is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatAttributeStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each attribute.

A [`Vector3`](Vector3.md)[] of attribute locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateIndexRegion

```csharp
public void MeshSurfaceUpdateIndexRegion(Rid mesh, int surface, int offset, ReadOnlySpan<byte> data)
```

Updates the index buffer of the mesh surface with the given `data`. The expected data are 16 or 32-bit unsigned integers, which can be determined with `MeshSurfaceGetFormatIndexStride()`.

### MeshSurfaceUpdateIndexRegion

```csharp
public void MeshSurfaceUpdateIndexRegion(Rid mesh, int surface, int offset, byte[] data)
```

Updates the index buffer of the mesh surface with the given `data`. The expected data are 16 or 32-bit unsigned integers, which can be determined with `MeshSurfaceGetFormatIndexStride()`.

### MeshSurfaceUpdateSkinRegion

```csharp
public void MeshSurfaceUpdateSkinRegion(Rid mesh, int surface, int offset, ReadOnlySpan<byte> data)
```

Updates the skin buffer of the mesh surface with the given `data`. The expected data per skin is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatSkinStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each skin.

A [`Vector3`](Vector3.md)[] of skin locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateSkinRegion

```csharp
public void MeshSurfaceUpdateSkinRegion(Rid mesh, int surface, int offset, byte[] data)
```

Updates the skin buffer of the mesh surface with the given `data`. The expected data per skin is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatSkinStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each skin.

A [`Vector3`](Vector3.md)[] of skin locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateVertexRegion

```csharp
public void MeshSurfaceUpdateVertexRegion(Rid mesh, int surface, int offset, ReadOnlySpan<byte> data)
```

Updates the vertex buffer of the mesh surface with the given `data`. The expected data per vertex is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatVertexStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each vertex.

A [`Vector3`](Vector3.md)[] of vertex locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MeshSurfaceUpdateVertexRegion

```csharp
public void MeshSurfaceUpdateVertexRegion(Rid mesh, int surface, int offset, byte[] data)
```

Updates the vertex buffer of the mesh surface with the given `data`. The expected data per vertex is 8 or 12 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector2`](Vector2.md) or [`Vector3`](Vector3.md) vertices. This value can be determined with `MeshSurfaceGetFormatVertexStride()` instead.

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each vertex.

A [`Vector3`](Vector3.md)[] of vertex locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### MultimeshAllocateData

```csharp
public void MultimeshAllocateData(Rid multimesh, int instances, RenderingServer.MultimeshTransformFormat transformFormat, bool colorFormat = false, bool customDataFormat = false, bool useIndirect = false)
```

Sets up the multimesh using the specified data. The number of instances is set by `instances`. The format of the instance transforms is set by `transformFormat`, which should be set according to whether the multimesh is meant to be rendered in 2D or 3D. If `colorFormat` is `true`, each instance will have a color associated with it. If `customDataFormat` is `true`, each instance will have a custom data vector associated with it. If `useIndirect` is `true`, an indirect command buffer will be created for this multimesh, allowing the instance count to be modified directly on the GPU. See also `MultimeshGetCommandBufferRdRid()`.

### MultimeshAllocateData

```csharp
public void MultimeshAllocateData(Rid multimesh, int instances, RenderingServer.MultimeshTransformFormat transformFormat, bool colorFormat, bool customDataFormat)
```

Sets up the multimesh using the specified data. The number of instances is set by `instances`. The format of the instance transforms is set by `transformFormat`, which should be set according to whether the multimesh is meant to be rendered in 2D or 3D. If `colorFormat` is `true`, each instance will have a color associated with it. If `customDataFormat` is `true`, each instance will have a custom data vector associated with it. If `useIndirect` is `true`, an indirect command buffer will be created for this multimesh, allowing the instance count to be modified directly on the GPU. See also `MultimeshGetCommandBufferRdRid()`.

### MultimeshCreate

```csharp
public Rid MultimeshCreate()
```

Creates a new multimesh on the RenderingServer and returns an [`Rid`](Rid.md) handle. This RID will be used in all `multimesh_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this multimesh to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent resource is [`MultiMesh`](MultiMesh.md).

### MultimeshGetAabb

```csharp
public Aabb MultimeshGetAabb(Rid multimesh)
```

Calculates and returns the axis-aligned bounding box that encloses all instances within the multimesh.

### MultimeshGetBuffer

```csharp
public float[] MultimeshGetBuffer(Rid multimesh)
```

Returns the MultiMesh data (such as instance transforms, colors, etc.). See `MultimeshSetBuffer()` for details on the returned data.

**Note:** If the buffer is in the engine's internal cache, it will have to be fetched from GPU memory and possibly decompressed. This means `MultimeshGetBuffer()` is potentially a slow operation and should be avoided whenever possible.

### MultimeshGetBufferRdRid

```csharp
public Rid MultimeshGetBufferRdRid(Rid multimesh)
```

Returns the [`RenderingDevice`](RenderingDevice.md) [`Rid`](Rid.md) handle of the [`MultiMesh`](MultiMesh.md), which can be used as any other buffer on the Rendering Device.

### MultimeshGetCommandBufferRdRid

```csharp
public Rid MultimeshGetCommandBufferRdRid(Rid multimesh)
```

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
public Aabb MultimeshGetCustomAabb(Rid multimesh)
```

Returns the custom AABB defined for this MultiMesh resource.

### MultimeshGetInstanceCount

```csharp
public int MultimeshGetInstanceCount(Rid multimesh)
```

Returns the number of instances allocated for this multimesh.

### MultimeshGetMesh

```csharp
public Rid MultimeshGetMesh(Rid multimesh)
```

Returns the RID of the mesh that will be used in drawing this multimesh.

### MultimeshGetVisibleInstances

```csharp
public int MultimeshGetVisibleInstances(Rid multimesh)
```

Returns the number of visible instances for this multimesh.

### MultimeshInstanceGetColor

```csharp
public Color MultimeshInstanceGetColor(Rid multimesh, int index)
```

Returns the color by which the specified instance will be modulated.

### MultimeshInstanceGetCustomData

```csharp
public Color MultimeshInstanceGetCustomData(Rid multimesh, int index)
```

Returns the custom data associated with the specified instance.

### MultimeshInstanceGetTransform

```csharp
public Transform3D MultimeshInstanceGetTransform(Rid multimesh, int index)
```

Returns the [`Transform3D`](Transform3D.md) of the specified instance.

### MultimeshInstanceGetTransform2D

```csharp
public Transform2D MultimeshInstanceGetTransform2D(Rid multimesh, int index)
```

Returns the [`Transform2D`](Transform2D.md) of the specified instance. For use when the multimesh is set to use 2D transforms.

### MultimeshInstanceResetPhysicsInterpolation

```csharp
public void MultimeshInstanceResetPhysicsInterpolation(Rid multimesh, int index)
```

Prevents physics interpolation for the specified instance during the current physics tick.

This is useful when moving an instance to a new location, to give an instantaneous change rather than interpolation from the previous location.

### MultimeshInstanceSetColor

```csharp
public void MultimeshInstanceSetColor(Rid multimesh, int index, Color color)
```

Sets the color by which this instance will be modulated. Equivalent to [`MultiMesh.SetInstanceColor`](MultiMesh.md).

### MultimeshInstanceSetCustomData

```csharp
public void MultimeshInstanceSetCustomData(Rid multimesh, int index, Color customData)
```

Sets the custom data for this instance. Custom data is passed as a [`Color`](Color.md), but is interpreted as a `vec4` in the shader. Equivalent to [`MultiMesh.SetInstanceCustomData`](MultiMesh.md).

### MultimeshInstanceSetTransform

```csharp
public void MultimeshInstanceSetTransform(Rid multimesh, int index, Transform3D transform)
```

Sets the [`Transform3D`](Transform3D.md) for this instance. Equivalent to [`MultiMesh.SetInstanceTransform`](MultiMesh.md).

### MultimeshInstanceSetTransform2D

```csharp
public void MultimeshInstanceSetTransform2D(Rid multimesh, int index, Transform2D transform)
```

Sets the [`Transform2D`](Transform2D.md) for this instance. For use when multimesh is used in 2D. Equivalent to [`MultiMesh.SetInstanceTransform2D`](MultiMesh.md).

### MultimeshInstancesResetPhysicsInterpolation

```csharp
public void MultimeshInstancesResetPhysicsInterpolation(Rid multimesh)
```

Prevents physics interpolation for all instances during the current physics tick.

This is useful when moving all instances to new locations, to give instantaneous changes rather than interpolation from the previous locations.

### MultimeshSetBuffer

```csharp
public void MultimeshSetBuffer(Rid multimesh, ReadOnlySpan<float> buffer)
```

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
public void MultimeshSetBuffer(Rid multimesh, float[] buffer)
```

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
public void MultimeshSetBufferInterpolated(Rid multimesh, ReadOnlySpan<float> buffer, ReadOnlySpan<float> bufferPrevious)
```

Alternative version of `MultimeshSetBuffer()` for use with physics interpolation.

Takes both an array of current data and an array of data for the previous physics tick.

### MultimeshSetBufferInterpolated

```csharp
public void MultimeshSetBufferInterpolated(Rid multimesh, float[] buffer, float[] bufferPrevious)
```

Alternative version of `MultimeshSetBuffer()` for use with physics interpolation.

Takes both an array of current data and an array of data for the previous physics tick.

### MultimeshSetCustomAabb

```csharp
public void MultimeshSetCustomAabb(Rid multimesh, Aabb aabb)
```

Sets the custom AABB for this MultiMesh resource.

### MultimeshSetMesh

```csharp
public void MultimeshSetMesh(Rid multimesh, Rid mesh)
```

Sets the mesh to be drawn by the multimesh. Equivalent to [`MultiMesh.Mesh`](MultiMesh.md).

### MultimeshSetPhysicsInterpolated

```csharp
public void MultimeshSetPhysicsInterpolated(Rid multimesh, bool interpolated)
```

Turns on and off physics interpolation for this MultiMesh resource.

### MultimeshSetPhysicsInterpolationQuality

```csharp
public void MultimeshSetPhysicsInterpolationQuality(Rid multimesh, RenderingServer.MultimeshPhysicsInterpolationQuality quality)
```

Sets the physics interpolation quality for the [`MultiMesh`](MultiMesh.md).

A value of [`RenderingServer.MultimeshPhysicsInterpolationQuality.Fast`](RenderingServer.md) gives fast but low quality interpolation, a value of [`RenderingServer.MultimeshPhysicsInterpolationQuality.High`](RenderingServer.md) gives slower but higher quality interpolation.

### MultimeshSetVisibleInstances

```csharp
public void MultimeshSetVisibleInstances(Rid multimesh, int visible)
```

Sets the number of instances visible at a given time. If -1, all instances that have been allocated are drawn. Equivalent to [`MultiMesh.VisibleInstanceCount`](MultiMesh.md).

### OccluderCreate

```csharp
public Rid OccluderCreate()
```

Creates an occluder instance and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `occluder_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Occluder3D`](Occluder3D.md) (not to be confused with the [`OccluderInstance3D`](OccluderInstance3D.md) node).

### OccluderSetMesh

```csharp
public void OccluderSetMesh(Rid occluder, ReadOnlySpan<Vector3> vertices, ReadOnlySpan<int> indices)
```

Sets the mesh data for the given occluder RID, which controls the shape of the occlusion culling that will be performed.

### OccluderSetMesh

```csharp
public void OccluderSetMesh(Rid occluder, Vector3[] vertices, int[] indices)
```

Sets the mesh data for the given occluder RID, which controls the shape of the occlusion culling that will be performed.

### OmniLightCreate

```csharp
public Rid OmniLightCreate()
```

Creates a new omni light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this omni light to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`OmniLight3D`](OmniLight3D.md).

### ParticlesCollisionCreate

```csharp
public Rid ParticlesCollisionCreate()
```

Creates a new 3D GPU particle collision or attractor and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `particles_collision_*` RenderingServer functions.

**Note:** The equivalent nodes are [`GpuParticlesCollision3D`](GpuParticlesCollision3D.md) and [`GpuParticlesAttractor3D`](GpuParticlesAttractor3D.md).

### ParticlesCollisionHeightFieldUpdate

```csharp
public void ParticlesCollisionHeightFieldUpdate(Rid particlesCollision)
```

Requests an update for the 3D GPU particle collision heightfield. This may be automatically called by the 3D GPU particle collision heightfield depending on its [`GpuParticlesCollisionHeightField3D.UpdateMode`](GpuParticlesCollisionHeightField3D.md).

### ParticlesCollisionSetAttractorAttenuation

```csharp
public void ParticlesCollisionSetAttractorAttenuation(Rid particlesCollision, float curve)
```

Sets the attenuation `curve` for the 3D GPU particles attractor specified by the `particlesCollision` RID. Only used for attractors, not colliders. Equivalent to [`GpuParticlesAttractor3D.Attenuation`](GpuParticlesAttractor3D.md).

### ParticlesCollisionSetAttractorDirectionality

```csharp
public void ParticlesCollisionSetAttractorDirectionality(Rid particlesCollision, float amount)
```

Sets the directionality `amount` for the 3D GPU particles attractor specified by the `particlesCollision` RID. Only used for attractors, not colliders. Equivalent to [`GpuParticlesAttractor3D.Directionality`](GpuParticlesAttractor3D.md).

### ParticlesCollisionSetAttractorStrength

```csharp
public void ParticlesCollisionSetAttractorStrength(Rid particlesCollision, float strength)
```

Sets the `strength` for the 3D GPU particles attractor specified by the `particlesCollision` RID. Only used for attractors, not colliders. Equivalent to [`GpuParticlesAttractor3D.Strength`](GpuParticlesAttractor3D.md).

### ParticlesCollisionSetBoxExtents

```csharp
public void ParticlesCollisionSetBoxExtents(Rid particlesCollision, Vector3 extents)
```

Sets the `extents` for the 3D GPU particles collision by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionBox3D.Size`](GpuParticlesCollisionBox3D.md), [`GpuParticlesCollisionSdf3D.Size`](GpuParticlesCollisionSdf3D.md), [`GpuParticlesCollisionHeightField3D.Size`](GpuParticlesCollisionHeightField3D.md), [`GpuParticlesAttractorBox3D.Size`](GpuParticlesAttractorBox3D.md) or [`GpuParticlesAttractorVectorField3D.Size`](GpuParticlesAttractorVectorField3D.md) depending on the `particlesCollision` type.

### ParticlesCollisionSetCollisionType

```csharp
public void ParticlesCollisionSetCollisionType(Rid particlesCollision, RenderingServer.ParticlesCollisionType type)
```

Sets the collision or attractor shape `type` for the 3D GPU particles collision or attractor specified by the `particlesCollision` RID.

### ParticlesCollisionSetCullMask

```csharp
public void ParticlesCollisionSetCullMask(Rid particlesCollision, uint mask)
```

Sets the cull `mask` for the 3D GPU particles collision or attractor specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollision3D.CullMask`](GpuParticlesCollision3D.md) or [`GpuParticlesAttractor3D.CullMask`](GpuParticlesAttractor3D.md) depending on the `particlesCollision` type.

### ParticlesCollisionSetFieldTexture

```csharp
public void ParticlesCollisionSetFieldTexture(Rid particlesCollision, Rid texture)
```

Sets the signed distance field `texture` for the 3D GPU particles collision specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionSdf3D.Texture`](GpuParticlesCollisionSdf3D.md) or [`GpuParticlesAttractorVectorField3D.Texture`](GpuParticlesAttractorVectorField3D.md) depending on the `particlesCollision` type.

### ParticlesCollisionSetHeightFieldMask

```csharp
public void ParticlesCollisionSetHeightFieldMask(Rid particlesCollision, uint mask)
```

Sets the heightfield `mask` for the 3D GPU particles heightfield collision specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionHeightField3D.HeightfieldMask`](GpuParticlesCollisionHeightField3D.md).

### ParticlesCollisionSetHeightFieldResolution

```csharp
public void ParticlesCollisionSetHeightFieldResolution(Rid particlesCollision, RenderingServer.ParticlesCollisionHeightfieldResolution resolution)
```

Sets the heightmap `resolution` for the 3D GPU particles heightfield collision specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionHeightField3D.Resolution`](GpuParticlesCollisionHeightField3D.md).

### ParticlesCollisionSetSphereRadius

```csharp
public void ParticlesCollisionSetSphereRadius(Rid particlesCollision, float radius)
```

Sets the `radius` for the 3D GPU particles sphere collision or attractor specified by the `particlesCollision` RID. Equivalent to [`GpuParticlesCollisionSphere3D.Radius`](GpuParticlesCollisionSphere3D.md) or [`GpuParticlesAttractorSphere3D.Radius`](GpuParticlesAttractorSphere3D.md) depending on the `particlesCollision` type.

### ParticlesCreate

```csharp
public Rid ParticlesCreate()
```

Creates a GPU-based particle system and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `particles_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach these particles to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent nodes are [`GpuParticles2D`](GpuParticles2D.md) and [`GpuParticles3D`](GpuParticles3D.md).

**Note:** All `particles_*` methods only apply to GPU-based particles, not CPU-based particles. [`CpuParticles2D`](CpuParticles2D.md) and [`CpuParticles3D`](CpuParticles3D.md) do not have equivalent RenderingServer functions available, as these use [`MultiMeshInstance2D`](MultiMeshInstance2D.md) and [`MultiMeshInstance3D`](MultiMeshInstance3D.md) under the hood (see `multimesh_*` methods).

### ParticlesEmit

```csharp
public void ParticlesEmit(Rid particles, Transform3D transform, Vector3 velocity, Color color, Color custom, uint emitFlags)
```

Manually emits particles from the `particles` instance.

### ParticlesGetCurrentAabb

```csharp
public Aabb ParticlesGetCurrentAabb(Rid particles)
```

Calculates and returns the axis-aligned bounding box that contains all the particles. Equivalent to [`GpuParticles3D.CaptureAabb`](GpuParticles3D.md).

### ParticlesGetEmitting

```csharp
public bool ParticlesGetEmitting(Rid particles)
```

Returns `true` if particles are currently set to emitting.

### ParticlesIsInactive

```csharp
public bool ParticlesIsInactive(Rid particles)
```

Returns `true` if particles are not emitting and particles are set to inactive.

### ParticlesRequestProcess

```csharp
public void ParticlesRequestProcess(Rid particles)
```

Add particle system to list of particle systems that need to be updated. Update will take place on the next frame, or on the next call to `InstancesCullAabb()`, `InstancesCullConvex()`, or `InstancesCullRay()`.

### ParticlesRequestProcessTime

```csharp
public void ParticlesRequestProcessTime(Rid particles, float processTime)
```

Requests the particles to process for extra process time during a single frame.

`processTime` defines the time that the particles will process while emitting is on. `processTimeResidual` defines the time that particles will process with emitting turned off for the simulation. When combined with the particles' speed scale set to `0.0`, this is useful to be able to seek a particle system timeline.

### ParticlesRequestProcessTime

```csharp
public void ParticlesRequestProcessTime(Rid particles, float processTime, float processTimeResidual = 0f)
```

Requests the particles to process for extra process time during a single frame.

`processTime` defines the time that the particles will process while emitting is on. `processTimeResidual` defines the time that particles will process with emitting turned off for the simulation. When combined with the particles' speed scale set to `0.0`, this is useful to be able to seek a particle system timeline.

### ParticlesRestart

```csharp
public void ParticlesRestart(Rid particles)
```

Reset the particles on the next update. Equivalent to [`GpuParticles3D.Restart`](GpuParticles3D.md).

### ParticlesSetAmount

```csharp
public void ParticlesSetAmount(Rid particles, int amount)
```

Sets the number of particles to be drawn and allocates the memory for them. Equivalent to [`GpuParticles3D.Amount`](GpuParticles3D.md).

### ParticlesSetAmountRatio

```csharp
public void ParticlesSetAmountRatio(Rid particles, float ratio)
```

Sets the amount ratio for particles to be emitted. Equivalent to [`GpuParticles3D.AmountRatio`](GpuParticles3D.md).

### ParticlesSetCollisionBaseSize

```csharp
public void ParticlesSetCollisionBaseSize(Rid particles, float size)
```

Sets the base size for particle collision. Equivalent to [`GpuParticles3D.CollisionBaseSize`](GpuParticles3D.md).

### ParticlesSetCustomAabb

```csharp
public void ParticlesSetCustomAabb(Rid particles, Aabb aabb)
```

Sets a custom axis-aligned bounding box for the particle system. Equivalent to [`GpuParticles3D.VisibilityAabb`](GpuParticles3D.md).

### ParticlesSetDrawOrder

```csharp
public void ParticlesSetDrawOrder(Rid particles, RenderingServer.ParticlesDrawOrder order)
```

Sets the draw order of the particles. Equivalent to [`GpuParticles3D.DrawOrder`](GpuParticles3D.md).

### ParticlesSetDrawPassMesh

```csharp
public void ParticlesSetDrawPassMesh(Rid particles, int pass, Rid mesh)
```

Sets the mesh to be used for the specified draw pass. Equivalent to [`GpuParticles3D.DrawPass1`](GpuParticles3D.md), [`GpuParticles3D.DrawPass2`](GpuParticles3D.md), [`GpuParticles3D.DrawPass3`](GpuParticles3D.md), and [`GpuParticles3D.DrawPass4`](GpuParticles3D.md).

### ParticlesSetDrawPasses

```csharp
public void ParticlesSetDrawPasses(Rid particles, int count)
```

Sets the number of draw passes to use. Equivalent to [`GpuParticles3D.DrawPasses`](GpuParticles3D.md).

### ParticlesSetEmissionTransform

```csharp
public void ParticlesSetEmissionTransform(Rid particles, Transform3D transform)
```

Sets the [`Transform3D`](Transform3D.md) that will be used by the particles when they first emit.

### ParticlesSetEmitterVelocity

```csharp
public void ParticlesSetEmitterVelocity(Rid particles, Vector3 velocity)
```

Sets the velocity of a particle node, that will be used by [`ParticleProcessMaterial.InheritVelocityRatio`](ParticleProcessMaterial.md).

### ParticlesSetEmitting

```csharp
public void ParticlesSetEmitting(Rid particles, bool emitting)
```

If `true`, particles will emit over time. Setting to `false` does not reset the particles, but only stops their emission. Equivalent to [`GpuParticles3D.Emitting`](GpuParticles3D.md).

### ParticlesSetExplosivenessRatio

```csharp
public void ParticlesSetExplosivenessRatio(Rid particles, float ratio)
```

Sets the explosiveness ratio. Equivalent to [`GpuParticles3D.Explosiveness`](GpuParticles3D.md).

### ParticlesSetFixedFps

```csharp
public void ParticlesSetFixedFps(Rid particles, int fps)
```

Sets the frame rate that the particle system rendering will be fixed to. Equivalent to [`GpuParticles3D.FixedFps`](GpuParticles3D.md).

### ParticlesSetFractionalDelta

```csharp
public void ParticlesSetFractionalDelta(Rid particles, bool enable)
```

If `true`, uses fractional delta which smooths the movement of the particles. Equivalent to [`GpuParticles3D.FractDelta`](GpuParticles3D.md).

### ParticlesSetInterpToEnd

```csharp
public void ParticlesSetInterpToEnd(Rid particles, float factor)
```

Sets the value that informs a [`ParticleProcessMaterial`](ParticleProcessMaterial.md) to rush all particles towards the end of their lifetime.

### ParticlesSetInterpolate

```csharp
public void ParticlesSetInterpolate(Rid particles, bool enable)
```

Sets whether particles should use interpolation between fixed steps. Equivalent to [`GpuParticles3D.Interpolate`](GpuParticles3D.md).

### ParticlesSetLifetime

```csharp
public void ParticlesSetLifetime(Rid particles, double lifetime)
```

Sets the lifetime of each particle in the system. Equivalent to [`GpuParticles3D.Lifetime`](GpuParticles3D.md).

### ParticlesSetMode

```csharp
public void ParticlesSetMode(Rid particles, RenderingServer.ParticlesMode mode)
```

Sets whether the GPU particles specified by the `particles` RID should be rendered in 2D or 3D according to `mode`.

### ParticlesSetOneShot

```csharp
public void ParticlesSetOneShot(Rid particles, bool oneShot)
```

If `true`, particles will emit once and then stop. Equivalent to [`GpuParticles3D.OneShot`](GpuParticles3D.md).

### ParticlesSetPreProcessTime

```csharp
public void ParticlesSetPreProcessTime(Rid particles, double time)
```

Sets the preprocess time for the particles' animation. This lets you delay starting an animation until after the particles have begun emitting. Equivalent to [`GpuParticles3D.Preprocess`](GpuParticles3D.md).

### ParticlesSetProcessMaterial

```csharp
public void ParticlesSetProcessMaterial(Rid particles, Rid material)
```

Sets the material for processing the particles.

**Note:** This is not the material used to draw the materials. Equivalent to [`GpuParticles3D.ProcessMaterial`](GpuParticles3D.md).

### ParticlesSetRandomnessRatio

```csharp
public void ParticlesSetRandomnessRatio(Rid particles, float ratio)
```

Sets the emission randomness ratio. This randomizes the emission of particles within their phase. Equivalent to [`GpuParticles3D.Randomness`](GpuParticles3D.md).

### ParticlesSetSpeedScale

```csharp
public void ParticlesSetSpeedScale(Rid particles, double scale)
```

Sets the speed scale of the particle system. Equivalent to [`GpuParticles3D.SpeedScale`](GpuParticles3D.md).

### ParticlesSetSubemitter

```csharp
public void ParticlesSetSubemitter(Rid particles, Rid subemitterParticles)
```

Sets the subemitter particles for the particle system. Equivalent to [`GpuParticles3D.SubEmitter`](GpuParticles3D.md).

### ParticlesSetTrailBindPoses

```csharp
public void ParticlesSetTrailBindPoses(Rid particles, Array<Transform3D> bindPoses)
```

Sets the trail bind poses for the particle system. This specified as an array of [`Transform3D`](Transform3D.md)s representing the bind pose for each draw pass. See [`GpuParticles3D.DrawSkin`](GpuParticles3D.md), [`Skin.GetBindCount`](Skin.md), and [`Skin.GetBindPose`](Skin.md). Set the value for each draw pass to `Transform3D.Identity` to use the default behavior, which is what built-in trails use ([`RibbonTrailMesh`](RibbonTrailMesh.md) and [`TubeTrailMesh`](TubeTrailMesh.md)).

### ParticlesSetTrails

```csharp
public void ParticlesSetTrails(Rid particles, bool enable, float lengthSec)
```

If `enable` is `true`, enables trails for the `particles` with the specified `lengthSec` in seconds. Equivalent to [`GpuParticles3D.TrailEnabled`](GpuParticles3D.md) and [`GpuParticles3D.TrailLifetime`](GpuParticles3D.md).

### ParticlesSetTransformAlign

```csharp
public void ParticlesSetTransformAlign(Rid particles, RenderingServer.ParticlesTransformAlign align)
```

Sets the transform alignment for the particle system. Equivalent to [`GpuParticles3D.TransformAlign`](GpuParticles3D.md).

### ParticlesSetTransformAlignAxis

```csharp
public void ParticlesSetTransformAlignAxis(Rid particles, RenderingServer.ParticlesTransformAlignAxis rotationAxis)
```

Sets which axis to use for transform alignment.

### ParticlesSetTransformAlignChannelFilter

```csharp
public void ParticlesSetTransformAlignChannelFilter(Rid particles, RenderingServer.ParticlesTransformAlignCustomSrc channelFilter)
```

When using Z-Billboarding, which CUSTOM channel to read from.

### ParticlesSetUseLocalCoordinates

```csharp
public void ParticlesSetUseLocalCoordinates(Rid particles, bool enable)
```

If `true`, particles use local coordinates. If `false` they use global coordinates. Equivalent to [`GpuParticles3D.LocalCoords`](GpuParticles3D.md).

### PositionalSoftShadowFilterSetQuality

```csharp
public void PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality quality)
```

Sets the filter quality for omni and spot light shadows in 3D. See also `ProjectSettings.rendering/lights_and_shadows/positional_shadow/soft_shadow_filter_quality`. This parameter is global and cannot be set on a per-viewport basis.

### ReflectionProbeCreate

```csharp
public Rid ReflectionProbeCreate()
```

Creates a reflection probe and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `reflection_probe_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this reflection probe to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`ReflectionProbe`](ReflectionProbe.md).

### ReflectionProbeSetAmbientColor

```csharp
public void ReflectionProbeSetAmbientColor(Rid probe, Color color)
```

Sets the reflection probe's custom ambient light color. Equivalent to [`ReflectionProbe.AmbientColor`](ReflectionProbe.md).

### ReflectionProbeSetAmbientEnergy

```csharp
public void ReflectionProbeSetAmbientEnergy(Rid probe, float energy)
```

Sets the reflection probe's custom ambient light energy. Equivalent to [`ReflectionProbe.AmbientColorEnergy`](ReflectionProbe.md).

### ReflectionProbeSetAmbientMode

```csharp
public void ReflectionProbeSetAmbientMode(Rid probe, RenderingServer.ReflectionProbeAmbientMode mode)
```

Sets the reflection probe's ambient light mode. Equivalent to [`ReflectionProbe.AmbientMode`](ReflectionProbe.md).

### ReflectionProbeSetAsInterior

```csharp
public void ReflectionProbeSetAsInterior(Rid probe, bool enable)
```

If `true`, reflections will ignore sky contribution. Equivalent to [`ReflectionProbe.Interior`](ReflectionProbe.md).

### ReflectionProbeSetBlendDistance

```csharp
public void ReflectionProbeSetBlendDistance(Rid probe, float blendDistance)
```

Sets the distance in meters over which a probe blends into the scene.

### ReflectionProbeSetCullMask

```csharp
public void ReflectionProbeSetCullMask(Rid probe, uint layers)
```

Sets the render cull mask for this reflection probe. Only instances with a matching layer will be reflected by this probe. Equivalent to [`ReflectionProbe.CullMask`](ReflectionProbe.md).

### ReflectionProbeSetEnableBoxProjection

```csharp
public void ReflectionProbeSetEnableBoxProjection(Rid probe, bool enable)
```

If `true`, uses box projection. This can make reflections look more correct in certain situations. Equivalent to [`ReflectionProbe.BoxProjection`](ReflectionProbe.md).

### ReflectionProbeSetEnableShadows

```csharp
public void ReflectionProbeSetEnableShadows(Rid probe, bool enable)
```

If `true`, computes shadows in the reflection probe. This makes the reflection much slower to compute. Equivalent to [`ReflectionProbe.EnableShadows`](ReflectionProbe.md).

### ReflectionProbeSetIntensity

```csharp
public void ReflectionProbeSetIntensity(Rid probe, float intensity)
```

Sets the intensity of the reflection probe. Intensity modulates the strength of the reflection. Equivalent to [`ReflectionProbe.Intensity`](ReflectionProbe.md).

### ReflectionProbeSetMaxDistance

```csharp
public void ReflectionProbeSetMaxDistance(Rid probe, float distance)
```

Sets the max distance away from the probe an object can be before it is culled. Equivalent to [`ReflectionProbe.MaxDistance`](ReflectionProbe.md).

### ReflectionProbeSetMeshLodThreshold

```csharp
public void ReflectionProbeSetMeshLodThreshold(Rid probe, float pixels)
```

Sets the mesh level of detail to use in the reflection probe rendering. Higher values will use less detailed versions of meshes that have LOD variations generated, which can improve performance. Equivalent to [`ReflectionProbe.MeshLodThreshold`](ReflectionProbe.md).

### ReflectionProbeSetOriginOffset

```csharp
public void ReflectionProbeSetOriginOffset(Rid probe, Vector3 offset)
```

Sets the origin offset to be used when this reflection probe is in box project mode. Equivalent to [`ReflectionProbe.OriginOffset`](ReflectionProbe.md).

### ReflectionProbeSetReflectionMask

```csharp
public void ReflectionProbeSetReflectionMask(Rid probe, uint layers)
```

Sets the render reflection mask for this reflection probe. Only instances with a matching layer will have reflections applied from this probe. Equivalent to [`ReflectionProbe.ReflectionMask`](ReflectionProbe.md).

### ReflectionProbeSetResolution

```csharp
public void ReflectionProbeSetResolution(Rid probe, int resolution)
```

Deprecated. This method does nothing.

### ReflectionProbeSetSize

```csharp
public void ReflectionProbeSetSize(Rid probe, Vector3 size)
```

Sets the size of the area that the reflection probe will capture. Equivalent to [`ReflectionProbe.Size`](ReflectionProbe.md).

### ReflectionProbeSetUpdateMode

```csharp
public void ReflectionProbeSetUpdateMode(Rid probe, RenderingServer.ReflectionProbeUpdateMode mode)
```

Sets how often the reflection probe updates. Can either be once or every frame.

### RequestFrameDrawnCallback

```csharp
public void RequestFrameDrawnCallback(Callable callable)
```

Schedules a callback to the given callable after a frame has been drawn.

### ScenarioCreate

```csharp
public Rid ScenarioCreate()
```

Creates a scenario and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `scenario_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

The scenario is the 3D world that all the visual instances exist in.

### ScenarioSetCameraAttributes

```csharp
public void ScenarioSetCameraAttributes(Rid scenario, Rid effects)
```

Sets the camera attributes (`effects`) that will be used with this scenario. See also [`CameraAttributes`](CameraAttributes.md).

### ScenarioSetCompositor

```csharp
public void ScenarioSetCompositor(Rid scenario, Rid compositor)
```

Sets the compositor (`compositor`) that will be used with this scenario. See also [`Compositor`](Compositor.md).

### ScenarioSetEnvironment

```csharp
public void ScenarioSetEnvironment(Rid scenario, Rid environment)
```

Sets the environment that will be used with this scenario. See also [`Environment`](Environment.md).

### ScenarioSetFallbackEnvironment

```csharp
public void ScenarioSetFallbackEnvironment(Rid scenario, Rid environment)
```

Sets the fallback environment to be used by this scenario. The fallback environment is used if no environment is set. Internally, this is used by the editor to provide a default environment.

### ScreenSpaceRoughnessLimiterSetActive

```csharp
public void ScreenSpaceRoughnessLimiterSetActive(bool enable, float amount, float limit)
```

Sets the screen-space roughness limiter parameters, such as whether it should be enabled and its thresholds. Equivalent to `ProjectSettings.rendering/anti_aliasing/screen_space_roughness_limiter/enabled`, `ProjectSettings.rendering/anti_aliasing/screen_space_roughness_limiter/amount` and `ProjectSettings.rendering/anti_aliasing/screen_space_roughness_limiter/limit`.

### SetBootImage

```csharp
public void SetBootImage(Image image, Color color, bool scale, bool useFilter = true)
```

Sets a boot image. The `color` defines the background color. The value of `scale` indicates if the image will be scaled to fit the screen size. If `useFilter` is `true`, the image will be scaled with linear interpolation. If `useFilter` is `false`, the image will be scaled with nearest-neighbor interpolation.

### SetBootImageWithStretch

```csharp
public void SetBootImageWithStretch(Image image, Color color, RenderingServer.SplashStretchMode stretchMode, bool useFilter = true)
```

Sets a boot image. The `color` defines the background color. The value of `stretchMode` indicates how the image will be stretched (see [`RenderingServer.SplashStretchMode`](RenderingServer.md) for possible values). If `useFilter` is `true`, the image will be scaled with linear interpolation. If `useFilter` is `false`, the image will be scaled with nearest-neighbor interpolation.

### SetDebugGenerateWireframes

```csharp
public void SetDebugGenerateWireframes(bool generate)
```

If `generate` is `true`, generates debug wireframes for all meshes that are loaded when using the Compatibility renderer. By default, the engine does not generate debug wireframes at runtime, since they slow down loading of assets and take up VRAM.

**Note:** You must call this method before loading any meshes when using the Compatibility renderer. Otherwise, wireframes will not be used.

### SetDefaultClearColor

```csharp
public void SetDefaultClearColor(Color color)
```

Sets the default clear color which is used when a specific clear color has not been selected. See also `GetDefaultClearColor()`.

### SetRenderLoopEnabled

```csharp
public void SetRenderLoopEnabled(bool enabled)
```

### ShaderCreate

```csharp
public Rid ShaderCreate()
```

Creates an empty shader and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `shader_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Shader`](Shader.md).

### ShaderGetCode

```csharp
public string ShaderGetCode(Rid shader)
```

Returns a shader's source code as a string.

### ShaderGetDefaultTextureParameter

```csharp
public Rid ShaderGetDefaultTextureParameter(Rid shader, StringName name, int index = 0)
```

Returns a default texture from a shader searched by name.

**Note:** If the sampler array is used use `index` to access the specified texture.

### ShaderGetParameterDefault

```csharp
public Variant ShaderGetParameterDefault(Rid shader, StringName name)
```

Returns the default value for the specified shader uniform. This is usually the value written in the shader source code.

### ShaderSetCode

```csharp
public void ShaderSetCode(Rid shader, string code)
```

Sets the shader's source code (which triggers recompilation after being changed).

### ShaderSetDefaultTextureParameter

```csharp
public void ShaderSetDefaultTextureParameter(Rid shader, StringName name, Rid texture, int index = 0)
```

Sets a shader's default texture. Overwrites the texture given by name.

**Note:** If the sampler array is used use `index` to access the specified texture.

### ShaderSetPathHint

```csharp
public void ShaderSetPathHint(Rid shader, string path)
```

Sets the path hint for the specified shader. This should generally match the [`Shader`](Shader.md) resource's [`Resource.ResourcePath`](Resource.md).

### SkeletonAllocateData

```csharp
public void SkeletonAllocateData(Rid skeleton, int bones, bool is2DSkeleton = false)
```

Allocates data for this skeleton using the number of bones specified in `bones`. If `is2DSkeleton` is `true`, the skeleton will be treated as a 2D skeleton instead of a 3D skeleton. See also `SkeletonGetBoneCount()`.

### SkeletonBoneGetTransform

```csharp
public Transform3D SkeletonBoneGetTransform(Rid skeleton, int bone)
```

Returns the [`Transform3D`](Transform3D.md) set for a specific bone of this skeleton.

### SkeletonBoneGetTransform2D

```csharp
public Transform2D SkeletonBoneGetTransform2D(Rid skeleton, int bone)
```

Returns the [`Transform2D`](Transform2D.md) set for a specific bone of this skeleton.

### SkeletonBoneSetTransform

```csharp
public void SkeletonBoneSetTransform(Rid skeleton, int bone, Transform3D transform)
```

Sets the [`Transform3D`](Transform3D.md) for a specific bone of this skeleton.

### SkeletonBoneSetTransform2D

```csharp
public void SkeletonBoneSetTransform2D(Rid skeleton, int bone, Transform2D transform)
```

Sets the [`Transform2D`](Transform2D.md) for a specific bone of this skeleton.

### SkeletonCreate

```csharp
public Rid SkeletonCreate()
```

Creates a skeleton and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `skeleton_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

### SkeletonGetBoneCount

```csharp
public int SkeletonGetBoneCount(Rid skeleton)
```

Returns the number of bones allocated for this skeleton. See also `SkeletonAllocateData()`.

### SkeletonSetBaseTransform2D

```csharp
public void SkeletonSetBaseTransform2D(Rid skeleton, Transform2D baseTransform)
```

Sets the base [`Transform2D`](Transform2D.md) to use for the specified skeleton.

### SkyBakePanorama

```csharp
public Image SkyBakePanorama(Rid sky, float energy, bool bakeIrradiance, Vector2I size)
```

Generates and returns an [`Image`](Image.md) containing the radiance map for the specified `sky` RID. This supports built-in sky material and custom sky shaders. If `bakeIrradiance` is `true`, the irradiance map is saved instead of the radiance map. The radiance map is used to render reflected light, while the irradiance map is used to render ambient light. See also `EnvironmentBakePanorama()`.

**Note:** The image is saved using linear encoding without any tonemapping performed, which means it will look too dark if viewed directly in an image editor. `energy` values above `1.0` can be used to brighten the resulting image.

**Note:** `size` should be a 2:1 aspect ratio for the generated panorama to have square pixels. For radiance maps, there is no point in using a height greater than [`Sky.RadianceSize`](Sky.md), as it won't increase detail. Irradiance maps only contain low-frequency data, so there is usually no point in going past a size of 128×64 pixels when saving an irradiance map.

### SkyCreate

```csharp
public Rid SkyCreate()
```

Creates an empty sky and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `sky_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

### SkySetMaterial

```csharp
public void SkySetMaterial(Rid sky, Rid material)
```

Sets the material that the sky uses to render the background, ambient and reflection maps.

### SkySetMode

```csharp
public void SkySetMode(Rid sky, RenderingServer.SkyMode mode)
```

Sets the process `mode` of the sky specified by the `sky` RID. Equivalent to [`Sky.ProcessMode`](Sky.md).

### SkySetRadianceSize

```csharp
public void SkySetRadianceSize(Rid sky, int radianceSize)
```

Sets the `radianceSize` of the sky specified by the `sky` RID (in pixels). Equivalent to [`Sky.RadianceSize`](Sky.md).

### SpotLightCreate

```csharp
public Rid SpotLightCreate()
```

Creates a spot light and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID can be used in most `light_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this spot light to an instance using `InstanceSetBase()` using the returned RID.

### SubSurfaceScatteringSetQuality

```csharp
public void SubSurfaceScatteringSetQuality(RenderingServer.SubSurfaceScatteringQuality quality)
```

Sets `ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_quality` to use when rendering materials that have subsurface scattering enabled.

### SubSurfaceScatteringSetScale

```csharp
public void SubSurfaceScatteringSetScale(float scale, float depthScale)
```

Sets the `ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_scale` and `ProjectSettings.rendering/environment/subsurface_scattering/subsurface_scattering_depth_scale` to use when rendering materials that have subsurface scattering enabled.

### Texture2DCreate

```csharp
public Rid Texture2DCreate(Image image)
```

Creates a 2-dimensional texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_2d_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`Texture2D`](Texture2D.md).

**Note:** Not to be confused with [`RenderingDevice.TextureCreate`](RenderingDevice.md), which creates the graphics API's own texture type as opposed to the Godot-specific [`Texture2D`](Texture2D.md) resource.

### Texture2DGet

```csharp
public Image Texture2DGet(Rid texture)
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
public Image Texture2DLayerGet(Rid texture, int layer)
```

Returns an [`Image`](Image.md) instance from the given `texture` [`Rid`](Rid.md) and `layer`.

### Texture2DLayeredCreate

```csharp
public Rid Texture2DLayeredCreate(Array<Image> layers, RenderingServer.TextureLayeredType layeredType)
```

Creates a 2-dimensional layered texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_2d_layered_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`TextureLayered`](TextureLayered.md).

### Texture2DLayeredPlaceholderCreate

```csharp
public Rid Texture2DLayeredPlaceholderCreate(RenderingServer.TextureLayeredType layeredType)
```

Creates a placeholder for a 2-dimensional layered texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_2d_layered_*` RenderingServer functions, although it does nothing when used. See also `Texture2DPlaceholderCreate()`.

**Note:** The equivalent resource is [`PlaceholderTextureLayered`](PlaceholderTextureLayered.md).

### Texture2DPlaceholderCreate

```csharp
public Rid Texture2DPlaceholderCreate()
```

Creates a placeholder for a 2-dimensional layered texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_2d_layered_*` RenderingServer functions, although it does nothing when used. See also `Texture2DLayeredPlaceholderCreate()`.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`PlaceholderTexture2D`](PlaceholderTexture2D.md).

### Texture2DUpdate

```csharp
public void Texture2DUpdate(Rid texture, Image image, int layer)
```

Updates the texture specified by the `texture` [`Rid`](Rid.md) with the data in `image`. A `layer` must also be specified, which should be `0` when updating a single-layer texture ([`Texture2D`](Texture2D.md)).

**Note:** The `image` must have the same width, height and format as the current `texture` data. Otherwise, an error will be printed and the original texture won't be modified. If you need to use different width, height or format, use `TextureReplace()` instead.

### Texture3DCreate

```csharp
public Rid Texture3DCreate(Image.Format format, int width, int height, int depth, bool mipmaps, Array<Image> data)
```

**Note:** The equivalent resource is [`Texture3D`](Texture3D.md).

### Texture3DGet

```csharp
public Array<Image> Texture3DGet(Rid texture)
```

Returns 3D texture data as an array of [`Image`](Image.md)s for the specified texture [`Rid`](Rid.md).

### Texture3DPlaceholderCreate

```csharp
public Rid Texture3DPlaceholderCreate()
```

Creates a placeholder for a 3-dimensional texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_3d_*` RenderingServer functions, although it does nothing when used.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`PlaceholderTexture3D`](PlaceholderTexture3D.md).

### Texture3DUpdate

```csharp
public void Texture3DUpdate(Rid texture, Array<Image> data)
```

Updates the texture specified by the `texture` [`Rid`](Rid.md)'s data with the data in `data`. All the texture's layers must be replaced at once.

**Note:** The `texture` must have the same width, height, depth and format as the current texture data. Otherwise, an error will be printed and the original texture won't be modified. If you need to use different width, height, depth or format, use `TextureReplace()` instead.

### TextureCreateFromNativeHandle

```csharp
public Rid TextureCreateFromNativeHandle(RenderingServer.TextureType type, Image.Format format, ulong nativeHandle, int width, int height, int depth, int layers = 1, RenderingServer.TextureLayeredType layeredType = RenderingServer.TextureLayeredType.Layered2DArray)
```

Creates a texture based on a native handle that was created outside of Godot's renderer.

**Note:** If using only the rendering device renderer, it's recommend to use [`RenderingDevice.TextureCreateFromExtension`](RenderingDevice.md) together with [`RenderingServer.TextureRdCreate`](RenderingServer.md), rather than this method. This way, the texture's format and usage can be controlled more effectively.

### TextureDrawableBlitRect

```csharp
public void TextureDrawableBlitRect(Array<Rid> textures, Rect2I rect, Rid material, Color modulate, Array<Rid> sourceTextures, int toMipmap = 0)
```

Draws to `rect` on up to 4 given Drawable `textures`, using a TextureBlit Shader from `material`. `modulate` and up to 4 `sourceTextures` are uniforms for the Shader to process with. `toMipmap` can specify to perform this draw to a lower mipmap level.

**Note:** All `textures` must be the same size and format.

### TextureDrawableCreate

```csharp
public Rid TextureDrawableCreate(int width, int height, RenderingServer.TextureDrawableFormat format, Color? color = null, bool withMipmaps = false)
```

Creates a 2-dimensional texture and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `texture_drawable*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent resource is [`DrawableTexture2D`](DrawableTexture2D.md).

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### TextureDrawableGenerateMipmaps

```csharp
public void TextureDrawableGenerateMipmaps(Rid texture)
```

Calculates new MipMaps for the given Drawable `texture`.

### TextureDrawableGetDefaultMaterial

```csharp
public Rid TextureDrawableGetDefaultMaterial()
```

Returns a ShaderMaterial with the default texture_blit Shader.

### TextureGetFormat

```csharp
public Image.Format TextureGetFormat(Rid texture)
```

Returns the format for the texture.

### TextureGetNativeHandle

```csharp
public ulong TextureGetNativeHandle(Rid texture, bool srgb = false)
```

Returns the internal graphics handle for this texture object. For use when communicating with third-party APIs mostly with GDExtension.

`srgb` should be `true` when the texture uses nonlinear sRGB encoding and `false` when the texture uses linear encoding.

**Note:** This function returns a `uint64_t` which internally maps to a `GLuint` (OpenGL) or `VkImage` (Vulkan).

### TextureGetPath

```csharp
public string TextureGetPath(Rid texture)
```

Returns the resource path (starting with `res://` or `uid://`) for the specified texture RID. Returns an empty `System.String` if the resource is built-in. See also `TextureSetPath()`.

### TextureGetRdTexture

```csharp
public Rid TextureGetRdTexture(Rid texture, bool srgb = false)
```

Returns a texture [`Rid`](Rid.md) that can be used with [`RenderingDevice`](RenderingDevice.md).

`srgb` should be `true` when the texture uses nonlinear sRGB encoding and `false` when the texture uses linear encoding.

### TextureProxyCreate

```csharp
public Rid TextureProxyCreate(Rid base)
```

This method does nothing and always returns an invalid [`Rid`](Rid.md).

### TextureProxyUpdate

```csharp
public void TextureProxyUpdate(Rid texture, Rid proxyTo)
```

This method does nothing.

### TextureRdCreate

```csharp
public Rid TextureRdCreate(Rid rdTexture, RenderingServer.TextureLayeredType layerType = RenderingServer.TextureLayeredType.Layered2DArray)
```

Creates a new texture object based on a texture created directly on the [`RenderingDevice`](RenderingDevice.md). If the texture contains layers, `layerType` is used to define the layer type.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The RenderingServer's `FreeRid()` won't free the underlying `rdTexture`, you will want to free the `rdTexture` using [`RenderingDevice.FreeRid`](RenderingDevice.md).

### TextureReplace

```csharp
public void TextureReplace(Rid texture, Rid byTexture)
```

Replaces `texture`'s texture data by the texture specified by the `byTexture` RID, without changing `texture`'s RID.

### TextureSetForceRedrawIfVisible

```csharp
public void TextureSetForceRedrawIfVisible(Rid texture, bool enable)
```

Sets whether the texture RID should force redrawing when it's visible on screen when [`OS.LowProcessorUsageMode`](OS.md) is `true`. This is used by [`AnimatedTexture`](AnimatedTexture.md) to force redrawing.

### TextureSetPath

```csharp
public void TextureSetPath(Rid texture, string path)
```

Sets the resource path for this texture RID. See also `TextureGetPath()`.

**Note:** This is purely a hint and does not cause the texture to be automatically saved when set to a `res://` path.

### TextureSetSizeOverride

```csharp
public void TextureSetSizeOverride(Rid texture, int width, int height)
```

Sets the size at which the texture should be *displayed* in 2D, ignoring its original size. This does not rescale the texture data itself, only how it is drawn in 2D. Set `width` and `height` to 0 to disable the size override.

### ViewportAttachCamera

```csharp
public void ViewportAttachCamera(Rid viewport, Rid camera)
```

Sets a viewport's camera.

### ViewportAttachCanvas

```csharp
public void ViewportAttachCanvas(Rid viewport, Rid canvas)
```

Sets a viewport's canvas.

### ViewportAttachToScreen

```csharp
public void ViewportAttachToScreen(Rid viewport, Rect2? rect = null, int screen = 0)
```

Copies the viewport to a region of the screen specified by `rect`. If `ViewportSetRenderDirectToScreen()` is `true`, then the viewport does not use a framebuffer and the contents of the viewport are rendered directly to screen. However, note that the root viewport is drawn last, therefore it will draw over the screen. Accordingly, you must set the root viewport to an area that does not cover the area that you have attached this viewport to.

For example, you can set the root viewport to not render at all with the following code:

Using this can result in significant optimization, especially on lower-end devices. However, it comes at the cost of having to manage your viewports manually. For further optimization, see `ViewportSetRenderDirectToScreen()`.

参数：
- `rect`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### ViewportCreate

```csharp
public Rid ViewportCreate()
```

Creates an empty viewport and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `viewport_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`Viewport`](Viewport.md).

### ViewportGetMeasuredRenderTimeCpu

```csharp
public double ViewportGetMeasuredRenderTimeCpu(Rid viewport)
```

Returns the CPU time taken to render the last frame in milliseconds. This *only* includes time spent in rendering-related operations; scripts' `_process` functions and other engine subsystems are not included in this readout. To get a complete readout of CPU time spent to render the scene, sum the render times of all viewports that are drawn every frame plus `GetFrameSetupTimeCpu()`. Unlike [`Engine.GetFramesPerSecond`](Engine.md), this method will accurately reflect CPU utilization even if framerate is capped via V-Sync or [`Engine.MaxFps`](Engine.md). See also `ViewportGetMeasuredRenderTimeGpu()`.

**Note:** Requires measurements to be enabled on the specified `viewport` using `ViewportSetMeasureRenderTime()`. Otherwise, this method returns `0.0`.

### ViewportGetMeasuredRenderTimeGpu

```csharp
public double ViewportGetMeasuredRenderTimeGpu(Rid viewport)
```

Returns the GPU time taken to render the last frame in milliseconds. To get a complete readout of GPU time spent to render the scene, sum the render times of all viewports that are drawn every frame. Unlike [`Engine.GetFramesPerSecond`](Engine.md), this method accurately reflects GPU utilization even if framerate is capped via V-Sync or [`Engine.MaxFps`](Engine.md). See also `ViewportGetMeasuredRenderTimeCpu()`.

**Note:** Requires measurements to be enabled on the specified `viewport` using `ViewportSetMeasureRenderTime()`. Otherwise, this method returns `0.0`.

**Note:** When GPU utilization is low enough during a certain period of time, GPUs will decrease their power state (which in turn decreases core and memory clock speeds). This can cause the reported GPU time to increase if GPU utilization is kept low enough by a framerate cap (compared to what it would be at the GPU's highest power state). Keep this in mind when benchmarking using `ViewportGetMeasuredRenderTimeGpu()`. This behavior can be overridden in the graphics driver settings at the cost of higher power usage.

### ViewportGetRenderInfo

```csharp
public int ViewportGetRenderInfo(Rid viewport, RenderingServer.ViewportRenderInfoType type, RenderingServer.ViewportRenderInfo info)
```

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
public Rid ViewportGetRenderTarget(Rid viewport)
```

Returns the render target for the viewport.

### ViewportGetTexture

```csharp
public Rid ViewportGetTexture(Rid viewport)
```

Returns the viewport's last rendered frame.

### ViewportGetUpdateMode

```csharp
public RenderingServer.ViewportUpdateMode ViewportGetUpdateMode(Rid viewport)
```

Returns the viewport's update mode.

**Warning:** Calling this from any thread other than the rendering thread will be detrimental to performance.

### ViewportRemoveCanvas

```csharp
public void ViewportRemoveCanvas(Rid viewport, Rid canvas)
```

Detaches a viewport from a canvas.

### ViewportSetActive

```csharp
public void ViewportSetActive(Rid viewport, bool active)
```

If `true`, sets the viewport active, else sets it inactive.

### ViewportSetAnisotropicFilteringLevel

```csharp
public void ViewportSetAnisotropicFilteringLevel(Rid viewport, RenderingServer.ViewportAnisotropicFiltering anisotropicFilteringLevel)
```

Sets the maximum number of samples to take when using anisotropic filtering on textures (as a power of two). A higher sample count will result in sharper textures at oblique angles, but is more expensive to compute. A value of `0` forcibly disables anisotropic filtering, even on materials where it is enabled.

The anisotropic filtering level also affects decals and light projectors if they are configured to use anisotropic filtering. See `ProjectSettings.rendering/textures/decals/filter` and `ProjectSettings.rendering/textures/light_projectors/filter`.

**Note:** In 3D, for this setting to have an effect, set [`BaseMaterial3D.TextureFilter`](BaseMaterial3D.md) to [`BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic`](BaseMaterial3D.md) or [`BaseMaterial3D.TextureFilterEnum.NearestWithMipmapsAnisotropic`](BaseMaterial3D.md) on materials.

**Note:** In 2D, for this setting to have an effect, set [`CanvasItem.TextureFilter`](CanvasItem.md) to [`CanvasItem.TextureFilterEnum.LinearWithMipmapsAnisotropic`](CanvasItem.md) or [`CanvasItem.TextureFilterEnum.NearestWithMipmapsAnisotropic`](CanvasItem.md) on the [`CanvasItem`](CanvasItem.md) node displaying the texture (or in [`CanvasTexture`](CanvasTexture.md)). However, anisotropic filtering is rarely useful in 2D, so only enable it for textures in 2D if it makes a meaningful visual difference.

### ViewportSetCanvasCullMask

```csharp
public void ViewportSetCanvasCullMask(Rid viewport, uint canvasCullMask)
```

Sets the rendering mask associated with this [`Viewport`](Viewport.md). Only [`CanvasItem`](CanvasItem.md) nodes with a matching rendering visibility layer will be rendered by this [`Viewport`](Viewport.md).

### ViewportSetCanvasStacking

```csharp
public void ViewportSetCanvasStacking(Rid viewport, Rid canvas, int layer, int sublayer)
```

Sets the stacking order for a viewport's canvas.

`layer` is the actual canvas layer, while `sublayer` specifies the stacking order of the canvas among those in the same layer.

**Note:** `layer` should be between [`RenderingServer.CanvasLayerMin`](RenderingServer.md) and [`RenderingServer.CanvasLayerMax`](RenderingServer.md) (inclusive). Any other value will wrap around.

### ViewportSetCanvasTransform

```csharp
public void ViewportSetCanvasTransform(Rid viewport, Rid canvas, Transform2D offset)
```

Sets the transformation of a viewport's canvas.

### ViewportSetClearMode

```csharp
public void ViewportSetClearMode(Rid viewport, RenderingServer.ViewportClearMode clearMode)
```

Sets the clear mode of a viewport.

### ViewportSetDebugDraw

```csharp
public void ViewportSetDebugDraw(Rid viewport, RenderingServer.ViewportDebugDraw draw)
```

Sets the debug draw mode of a viewport.

### ViewportSetDefaultCanvasItemTextureFilter

```csharp
public void ViewportSetDefaultCanvasItemTextureFilter(Rid viewport, RenderingServer.CanvasItemTextureFilter filter)
```

Sets the default texture filtering mode for the specified `viewport` RID.

### ViewportSetDefaultCanvasItemTextureRepeat

```csharp
public void ViewportSetDefaultCanvasItemTextureRepeat(Rid viewport, RenderingServer.CanvasItemTextureRepeat repeat)
```

Sets the default texture repeat mode for the specified `viewport` RID.

### ViewportSetDisable2D

```csharp
public void ViewportSetDisable2D(Rid viewport, bool disable)
```

If `true`, the viewport's canvas (i.e. 2D and GUI elements) is not rendered.

### ViewportSetDisable3D

```csharp
public void ViewportSetDisable3D(Rid viewport, bool disable)
```

If `true`, the viewport's 3D elements are not rendered.

### ViewportSetEnvironmentMode

```csharp
public void ViewportSetEnvironmentMode(Rid viewport, RenderingServer.ViewportEnvironmentMode mode)
```

Sets the viewport's environment mode which allows enabling or disabling rendering of 3D environment over 2D canvas. When disabled, 2D will not be affected by the environment. When enabled, 2D will be affected by the environment if the environment background mode is [`RenderingServer.EnvironmentBG.Canvas`](RenderingServer.md). The default behavior is to inherit the setting from the viewport's parent. If the topmost parent is also set to [`RenderingServer.ViewportEnvironmentMode.Inherit`](RenderingServer.md), then the behavior will be the same as if it was set to [`RenderingServer.ViewportEnvironmentMode.Enabled`](RenderingServer.md).

### ViewportSetFsrSharpness

```csharp
public void ViewportSetFsrSharpness(Rid viewport, float sharpness)
```

Determines how sharp the upscaled image will be when using the FSR upscaling mode. Sharpness halves with every whole number. Values go from 0.0 (sharpest) to 2.0. Values above 2.0 won't make a visible difference.

### ViewportSetGlobalCanvasTransform

```csharp
public void ViewportSetGlobalCanvasTransform(Rid viewport, Transform2D transform)
```

Sets the viewport's global transformation matrix.

### ViewportSetMeasureRenderTime

```csharp
public void ViewportSetMeasureRenderTime(Rid viewport, bool enable)
```

Sets the measurement for the given `viewport` RID (obtained using [`Viewport.GetViewportRid`](Viewport.md)). Once enabled, `ViewportGetMeasuredRenderTimeCpu()` and `ViewportGetMeasuredRenderTimeGpu()` will return values greater than `0.0` when queried with the given `viewport`.

### ViewportSetMsaa2D

```csharp
public void ViewportSetMsaa2D(Rid viewport, RenderingServer.ViewportMsaa msaa)
```

Sets the multisample antialiasing mode for 2D/Canvas on the specified `viewport` RID. Equivalent to `ProjectSettings.rendering/anti_aliasing/quality/msaa_2d` or [`Viewport.Msaa2D`](Viewport.md).

### ViewportSetMsaa3D

```csharp
public void ViewportSetMsaa3D(Rid viewport, RenderingServer.ViewportMsaa msaa)
```

Sets the multisample antialiasing mode for 3D on the specified `viewport` RID. Equivalent to `ProjectSettings.rendering/anti_aliasing/quality/msaa_3d` or [`Viewport.Msaa3D`](Viewport.md).

### ViewportSetOcclusionCullingBuildQuality

```csharp
public void ViewportSetOcclusionCullingBuildQuality(RenderingServer.ViewportOcclusionCullingBuildQuality quality)
```

Sets the `ProjectSettings.rendering/occlusion_culling/bvh_build_quality` to use for occlusion culling. This parameter is global and cannot be set on a per-viewport basis.

### ViewportSetOcclusionRaysPerThread

```csharp
public void ViewportSetOcclusionRaysPerThread(int raysPerThread)
```

Sets the `ProjectSettings.rendering/occlusion_culling/occlusion_rays_per_thread` to use for occlusion culling. This parameter is global and cannot be set on a per-viewport basis.

### ViewportSetParentViewport

```csharp
public void ViewportSetParentViewport(Rid viewport, Rid parentViewport)
```

Sets the viewport's parent to the viewport specified by the `parentViewport` RID.

### ViewportSetPositionalShadowAtlasQuadrantSubdivision

```csharp
public void ViewportSetPositionalShadowAtlasQuadrantSubdivision(Rid viewport, int quadrant, int subdivision)
```

Sets the number of subdivisions to use in the specified shadow atlas `quadrant` for omni and spot shadows. See also [`Viewport.SetPositionalShadowAtlasQuadrantSubdiv`](Viewport.md).

### ViewportSetPositionalShadowAtlasSize

```csharp
public void ViewportSetPositionalShadowAtlasSize(Rid viewport, int size, bool use16Bits = false)
```

Sets the `size` of the shadow atlas's images (used for omni and spot lights) on the viewport specified by the `viewport` RID. The value is rounded up to the nearest power of 2. If `use16Bits` is `true`, use 16 bits for the omni/spot shadow depth map. Enabling this results in shadows having less precision and may result in shadow acne, but can lead to performance improvements on some devices.

**Note:** If this is set to `0`, no positional shadows will be visible at all. This can improve performance significantly on low-end systems by reducing both the CPU and GPU load (as fewer draw calls are needed to draw the scene without shadows).

### ViewportSetRenderDirectToScreen

```csharp
public void ViewportSetRenderDirectToScreen(Rid viewport, bool enabled)
```

If `true`, render the contents of the viewport directly to screen. This allows a low-level optimization where you can skip drawing a viewport to the root viewport. While this optimization can result in a significant increase in speed (especially on older devices), it comes at a cost of usability. When this is enabled, you cannot read from the viewport or from the screen_texture. You also lose the benefit of certain window settings, such as the various stretch modes. Another consequence to be aware of is that in 2D the rendering happens in window coordinates, so if you have a viewport that is double the size of the window, and you set this, then only the portion that fits within the window will be drawn, no automatic scaling is possible, even if your game scene is significantly larger than the window size.

### ViewportSetScaling3DMode

```csharp
public void ViewportSetScaling3DMode(Rid viewport, RenderingServer.ViewportScaling3DMode scaling3DMode)
```

Sets the 3D resolution scaling mode. Bilinear scaling renders at different resolution to either undersample or supersample the viewport. FidelityFX Super Resolution 1.0, abbreviated to FSR, is an upscaling technology that produces high quality images at fast framerates by using a spatially aware upscaling algorithm. FSR is slightly more expensive than bilinear, but it produces significantly higher image quality. FSR should be used where possible.

### ViewportSetScaling3DScale

```csharp
public void ViewportSetScaling3DScale(Rid viewport, float scale)
```

Scales the 3D render buffer based on the viewport size uses an image filter specified in [`RenderingServer.ViewportScaling3DMode`](RenderingServer.md) to scale the output image to the full viewport size. Values lower than `1.0` can be used to speed up 3D rendering at the cost of quality (undersampling). Values greater than `1.0` are only valid for bilinear mode and can be used to improve 3D rendering quality at a high performance cost (supersampling). See also [`RenderingServer.ViewportMsaa`](RenderingServer.md) for multi-sample antialiasing, which is significantly cheaper but only smoothens the edges of polygons.

When using FSR upscaling, AMD recommends exposing the following values as preset options to users "Ultra Quality: 0.77", "Quality: 0.67", "Balanced: 0.59", "Performance: 0.5" instead of exposing the entire scale.

### ViewportSetScenario

```csharp
public void ViewportSetScenario(Rid viewport, Rid scenario)
```

Sets a viewport's scenario. The scenario contains information about environment information, reflection atlas, etc.

### ViewportSetScreenSpaceAA

```csharp
public void ViewportSetScreenSpaceAA(Rid viewport, RenderingServer.ViewportScreenSpaceAA mode)
```

Sets the viewport's screen-space antialiasing mode. Equivalent to `ProjectSettings.rendering/anti_aliasing/quality/screen_space_aa` or [`Viewport.ScreenSpaceAA`](Viewport.md).

### ViewportSetSdfOversizeAndScale

```csharp
public void ViewportSetSdfOversizeAndScale(Rid viewport, RenderingServer.ViewportSdfOversize oversize, RenderingServer.ViewportSdfScale scale)
```

Sets the viewport's 2D signed distance field `ProjectSettings.rendering/2d/sdf/oversize` and `ProjectSettings.rendering/2d/sdf/scale`. This is used when sampling the signed distance field in [`CanvasItem`](CanvasItem.md) shaders as well as [`GpuParticles2D`](GpuParticles2D.md) collision. This is *not* used by SDFGI in 3D rendering.

### ViewportSetSize

```csharp
public void ViewportSetSize(Rid viewport, int width, int height)
```

Sets the viewport's `width` and `height` in pixels. Optionally the `viewCount` can be set to increase the number of view layers for stereo rendering.

### ViewportSetSize

```csharp
public void ViewportSetSize(Rid viewport, int width, int height, int viewCount = 1)
```

Sets the viewport's `width` and `height` in pixels. Optionally the `viewCount` can be set to increase the number of view layers for stereo rendering.

### ViewportSetSnap2DTransformsToPixel

```csharp
public void ViewportSetSnap2DTransformsToPixel(Rid viewport, bool enabled)
```

If `true`, canvas item transforms (i.e. origin position) are snapped to the nearest pixel when rendering. This can lead to a crisper appearance at the cost of less smooth movement, especially when [`Camera2D`](Camera2D.md) smoothing is enabled. Equivalent to `ProjectSettings.rendering/2d/snap/snap_2d_transforms_to_pixel`.

### ViewportSetSnap2DVerticesToPixel

```csharp
public void ViewportSetSnap2DVerticesToPixel(Rid viewport, bool enabled)
```

If `true`, canvas item vertices (i.e. polygon points) are snapped to the nearest pixel when rendering. This can lead to a crisper appearance at the cost of less smooth movement, especially when [`Camera2D`](Camera2D.md) smoothing is enabled. Equivalent to `ProjectSettings.rendering/2d/snap/snap_2d_vertices_to_pixel`.

### ViewportSetTextureMipmapBias

```csharp
public void ViewportSetTextureMipmapBias(Rid viewport, float mipmapBias)
```

Affects the final texture sharpness by reading from a lower or higher mipmap (also called "texture LOD bias"). Negative values make mipmapped textures sharper but grainier when viewed at a distance, while positive values make mipmapped textures blurrier (even when up close). To get sharper textures at a distance without introducing too much graininess, set this between `-0.75` and `0.0`. Enabling temporal antialiasing (`ProjectSettings.rendering/anti_aliasing/quality/use_taa`) can help reduce the graininess visible when using negative mipmap bias.

**Note:** When the 3D scaling mode is set to FSR 1.0, this value is used to adjust the automatic mipmap bias which is calculated internally based on the scale factor. The formula for this is `-log2(1.0 / scale) + mipmap_bias`.

**Note:** This method is only supported in the Forward+ and Mobile renderers, not Compatibility. In Compatibility, this method is always treated as if `mipmapBias` was set to `0.0`.

### ViewportSetTransparentBackground

```csharp
public void ViewportSetTransparentBackground(Rid viewport, bool enabled)
```

If `true`, the viewport renders its background as transparent.

### ViewportSetUpdateMode

```csharp
public void ViewportSetUpdateMode(Rid viewport, RenderingServer.ViewportUpdateMode updateMode)
```

Sets when the viewport should be updated.

### ViewportSetUseDebanding

```csharp
public void ViewportSetUseDebanding(Rid viewport, bool enable)
```

Equivalent to [`Viewport.UseDebanding`](Viewport.md). See also `ProjectSettings.rendering/anti_aliasing/quality/use_debanding`.

### ViewportSetUseHdr2D

```csharp
public void ViewportSetUseHdr2D(Rid viewport, bool enabled)
```

If `true`, 2D rendering will use a high dynamic range (HDR) `RGBA16` format framebuffer. Additionally, 2D rendering will be performed on linear values and will be converted using the appropriate transfer function immediately before blitting to the screen (if the Viewport is attached to the screen).

Practically speaking, this means that the end result of the Viewport will not be clamped to the `0-1` range and can be used in 3D rendering without color encoding adjustments. This allows 2D rendering to take advantage of effects requiring high dynamic range (e.g. 2D glow) as well as substantially improves the appearance of effects requiring highly detailed gradients. This setting has the same effect as [`Viewport.UseHdr2D`](Viewport.md).

### ViewportSetUseOcclusionCulling

```csharp
public void ViewportSetUseOcclusionCulling(Rid viewport, bool enable)
```

If `true`, enables occlusion culling on the specified viewport. Equivalent to `ProjectSettings.rendering/occlusion_culling/use_occlusion_culling`.

### ViewportSetUseTaa

```csharp
public void ViewportSetUseTaa(Rid viewport, bool enable)
```

If `true`, use temporal antialiasing. Equivalent to `ProjectSettings.rendering/anti_aliasing/quality/use_taa` or [`Viewport.UseTaa`](Viewport.md).

### ViewportSetUseXR

```csharp
public void ViewportSetUseXR(Rid viewport, bool useXR)
```

If `true`, the viewport uses augmented or virtual reality technologies. See [`XRInterface`](XRInterface.md).

### ViewportSetVrsMode

```csharp
public void ViewportSetVrsMode(Rid viewport, RenderingServer.ViewportVrsMode mode)
```

Sets the Variable Rate Shading (VRS) mode for the viewport. If the GPU does not support VRS, this property is ignored. Equivalent to `ProjectSettings.rendering/vrs/mode`.

### ViewportSetVrsTexture

```csharp
public void ViewportSetVrsTexture(Rid viewport, Rid texture)
```

The texture to use when the VRS mode is set to [`RenderingServer.ViewportVrsMode.Texture`](RenderingServer.md). Equivalent to `ProjectSettings.rendering/vrs/texture`.

### ViewportSetVrsUpdateMode

```csharp
public void ViewportSetVrsUpdateMode(Rid viewport, RenderingServer.ViewportVrsUpdateMode mode)
```

Sets the update mode for Variable Rate Shading (VRS) for the viewport. VRS requires the input texture to be converted to the format usable by the VRS method supported by the hardware. The update mode defines how often this happens. If the GPU does not support VRS, or VRS is not enabled, this property is ignored.

If set to [`RenderingServer.ViewportVrsUpdateMode.Once`](RenderingServer.md), the input texture is copied once and the mode is changed to [`RenderingServer.ViewportVrsUpdateMode.Disabled`](RenderingServer.md).

### VisibilityNotifierCreate

```csharp
public Rid VisibilityNotifierCreate()
```

Creates a new 3D visibility notifier object and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `visibility_notifier_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

To place in a scene, attach this notifier to an instance using `InstanceSetBase()` using the returned RID.

**Note:** The equivalent node is [`VisibleOnScreenNotifier3D`](VisibleOnScreenNotifier3D.md).

### VisibilityNotifierSetAabb

```csharp
public void VisibilityNotifierSetAabb(Rid notifier, Aabb aabb)
```

Sets the AABB of the specified visibility notifier.

### VisibilityNotifierSetCallbacks

```csharp
public void VisibilityNotifierSetCallbacks(Rid notifier, Callable enterCallable, Callable exitCallable)
```

Sets the methods to be called when the notifier enters or exits the view.

### VoxelGIAllocateData

```csharp
public void VoxelGIAllocateData(Rid voxelGI, Transform3D toCellXform, Aabb aabb, Vector3I octreeSize, ReadOnlySpan<byte> octreeCells, ReadOnlySpan<byte> dataCells, ReadOnlySpan<byte> distanceField, ReadOnlySpan<int> levelCounts)
```

Allocates and initializes the voxel GI data for the specified `voxelGI` RID. `octreeCells` must be a multiple of 32. `octreeCells` must be double the size of `dataCells`. The allocated data can be retrieved later using the various `voxel_gi_get_*` methods.

### VoxelGIAllocateData

```csharp
public void VoxelGIAllocateData(Rid voxelGI, Transform3D toCellXform, Aabb aabb, Vector3I octreeSize, byte[] octreeCells, byte[] dataCells, byte[] distanceField, int[] levelCounts)
```

Allocates and initializes the voxel GI data for the specified `voxelGI` RID. `octreeCells` must be a multiple of 32. `octreeCells` must be double the size of `dataCells`. The allocated data can be retrieved later using the various `voxel_gi_get_*` methods.

### VoxelGICreate

```csharp
public Rid VoxelGICreate()
```

Creates a new voxel-based global illumination object and adds it to the RenderingServer. It can be accessed with the RID that is returned. This RID will be used in all `voxel_gi_*` RenderingServer functions.

Once finished with your RID, you will want to free the RID using the RenderingServer's `FreeRid()` method.

**Note:** The equivalent node is [`VoxelGI`](VoxelGI.md).

### VoxelGIGetDataCells

```csharp
public byte[] VoxelGIGetDataCells(Rid voxelGI)
```

Returns the data cells for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGIGetDistanceField

```csharp
public byte[] VoxelGIGetDistanceField(Rid voxelGI)
```

Returns the distance field data for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGIGetLevelCounts

```csharp
public int[] VoxelGIGetLevelCounts(Rid voxelGI)
```

Returns the level counts for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGIGetOctreeCells

```csharp
public byte[] VoxelGIGetOctreeCells(Rid voxelGI)
```

Returns the octree cell data for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGIGetOctreeSize

```csharp
public Vector3I VoxelGIGetOctreeSize(Rid voxelGI)
```

Returns the octree size for the specified voxel GI data instance, which corresponds to the number of subdivisions per axis. This can be viewed in the editor by hovering the **Bake VoxelGI** button at the top of the 3D editor viewport when a [`VoxelGI`](VoxelGI.md) node is selected and looking at the **Subdivisions** field in the tooltip.

### VoxelGIGetToCellXform

```csharp
public Transform3D VoxelGIGetToCellXform(Rid voxelGI)
```

Returns the transform to cell space for the specified voxel GI data instance. See also `VoxelGIAllocateData()`.

### VoxelGISetBakedExposureNormalization

```csharp
public void VoxelGISetBakedExposureNormalization(Rid voxelGI, float bakedExposure)
```

Used to inform the renderer what exposure normalization value was used while baking the voxel gi. This value will be used and modulated at run time to ensure that the voxel gi maintains a consistent level of exposure even if the scene-wide exposure normalization is changed at run time. For more information see `CameraAttributesSetExposure()`.

### VoxelGISetBias

```csharp
public void VoxelGISetBias(Rid voxelGI, float bias)
```

Sets the [`VoxelGIData.Bias`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetDynamicRange

```csharp
public void VoxelGISetDynamicRange(Rid voxelGI, float range)
```

Sets the [`VoxelGIData.DynamicRange`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetEnergy

```csharp
public void VoxelGISetEnergy(Rid voxelGI, float energy)
```

Sets the [`VoxelGIData.Energy`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetInterior

```csharp
public void VoxelGISetInterior(Rid voxelGI, bool enable)
```

Sets the [`VoxelGIData.Interior`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetNormalBias

```csharp
public void VoxelGISetNormalBias(Rid voxelGI, float bias)
```

Sets the [`VoxelGIData.NormalBias`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetPropagation

```csharp
public void VoxelGISetPropagation(Rid voxelGI, float amount)
```

Sets the [`VoxelGIData.Propagation`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

### VoxelGISetQuality

```csharp
public void VoxelGISetQuality(RenderingServer.VoxelGIQuality quality)
```

Sets the `ProjectSettings.rendering/global_illumination/voxel_gi/quality` value to use when rendering. This parameter is global and cannot be set on a per-VoxelGI basis.

### VoxelGISetUseTwoBounces

```csharp
public void VoxelGISetUseTwoBounces(Rid voxelGI, bool enable)
```

Sets the [`VoxelGIData.UseTwoBounces`](VoxelGIData.md) value to use on the specified `voxelGI`'s [`Rid`](Rid.md).

## 事件（信号）

### FramePostDraw

```csharp
public event Action FramePostDraw
```

Emitted at the end of the frame, after the RenderingServer has finished updating all the Viewports.

### FramePreDraw

```csharp
public event Action FramePreDraw
```

Emitted at the beginning of the frame, before the RenderingServer updates all the Viewports.
