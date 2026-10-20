<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderingDevice

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

`RenderingDevice` is an abstraction for working with modern low-level graphics APIs such as Vulkan. Compared to [`RenderingServer`](RenderingServer.md) (which works with Godot's own rendering subsystems), `RenderingDevice` is much lower-level and allows working more directly with the underlying graphics APIs. `RenderingDevice` is used in Godot to provide support for several modern low-level graphics APIs while reducing the amount of code duplication required. `RenderingDevice` can also be used in your own projects to perform things that are not exposed by [`RenderingServer`](RenderingServer.md) or high-level nodes, such as using compute shaders.

On startup, Godot creates a global `RenderingDevice` which can be retrieved using [`RenderingServer.GetRenderingDevice`](RenderingServer.md). This global `RenderingDevice` performs drawing to the screen.

**Local RenderingDevices:** Using [`RenderingServer.CreateLocalRenderingDevice`](RenderingServer.md), you can create "secondary" rendering devices to perform drawing and GPU compute operations on separate threads.

**Note:** `RenderingDevice` assumes intermediate knowledge of modern graphics APIs such as Vulkan, Direct3D 12, Metal or WebGPU. These graphics APIs are lower-level than OpenGL or Direct3D 11, requiring you to perform what was previously done by the graphics driver itself. If you have difficulty understanding the concepts used in this class, follow the [Vulkan Tutorial](https://vulkan-tutorial.com/) or [Vulkan Guide](https://vkguide.dev/). It's recommended to have existing modern OpenGL or Direct3D 11 knowledge before attempting to learn a low-level graphics API.

**Note:** `RenderingDevice` is not available when running in headless mode or when using the Compatibility rendering method.

## 方法

### Barrier

```csharp
public void Barrier(RenderingDevice.BarrierMask from = RenderingDevice.BarrierMask.AllBarriers, RenderingDevice.BarrierMask to = RenderingDevice.BarrierMask.AllBarriers)
```

引擎名：`barrier`

This method does nothing.

### BlasBuild

```csharp
public Error BlasBuild(Rid blas)
```

引擎名：`blas_build`

Builds the `blas`.

### BlasCreate

```csharp
public Rid BlasCreate(Array<RDAccelerationStructureGeometry> geometries, RenderingDevice.AccelerationStructureFlagBits flags)
```

引擎名：`blas_create`

Creates a new Bottom-Level Acceleration Structure (BLAS). It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### BufferClear

```csharp
public Error BufferClear(Rid buffer, uint offset, uint sizeBytes)
```

引擎名：`buffer_clear`

Clears the contents of the `buffer`, clearing `sizeBytes` bytes, starting at `offset`.

Prints an error if:

- the size isn't a multiple of four

- the region specified by `offset` + `sizeBytes` exceeds the buffer

- a draw list is currently active (created by `DrawListBegin()`)

- a compute list is currently active (created by `ComputeListBegin()`)

### BufferClear

```csharp
public Error BufferClear(Rid buffer, uint offset, uint sizeBytes, RenderingDevice.BarrierMask postBarrier)
```

引擎名：`buffer_clear`

Clears the contents of the `buffer`, clearing `sizeBytes` bytes, starting at `offset`.

Prints an error if:

- the size isn't a multiple of four

- the region specified by `offset` + `sizeBytes` exceeds the buffer

- a draw list is currently active (created by `DrawListBegin()`)

- a compute list is currently active (created by `ComputeListBegin()`)

### BufferCopy

```csharp
public Error BufferCopy(Rid srcBuffer, Rid dstBuffer, uint srcOffset, uint dstOffset, uint size)
```

引擎名：`buffer_copy`

Copies `size` bytes from the `srcBuffer` at `srcOffset` into `dstBuffer` at `dstOffset`.

Prints an error if:

- `size` exceeds the size of either `srcBuffer` or `dstBuffer` at their corresponding offsets

- a draw list is currently active (created by `DrawListBegin()`)

- a compute list is currently active (created by `ComputeListBegin()`)

### BufferGetData

```csharp
public byte[] BufferGetData(Rid buffer, uint offsetBytes = 0, uint sizeBytes = 0)
```

引擎名：`buffer_get_data`

Returns a copy of the data of the specified `buffer`, optionally `offsetBytes` and `sizeBytes` can be set to copy only a portion of the buffer.

**Note:** This method will block the GPU from working until the data is retrieved. Refer to `BufferGetDataAsync()` for an alternative that returns the data in more performant way.

### BufferGetDataAsync

```csharp
public Error BufferGetDataAsync(Rid buffer, Callable callback, uint offsetBytes = 0, uint sizeBytes = 0)
```

引擎名：`buffer_get_data_async`

Asynchronous version of `BufferGetData()`. RenderingDevice will call `callback` in a certain amount of frames with the data the buffer had at the time of the request.

**Note:** At the moment, the delay corresponds to the amount of frames specified by `ProjectSettings.rendering/rendering_device/vsync/frame_queue_size`.

**Note:** Downloading large buffers can have a prohibitive cost for real-time even when using the asynchronous method due to hardware bandwidth limitations. When dealing with large resources, you can adjust settings such as `ProjectSettings.rendering/rendering_device/staging_buffer/block_size_kb` to improve the transfer speed at the cost of extra memory.

```csharp
func _buffer_get_data_callback(array):
value = array.decode_u32(0)

...

rd.buffer_get_data_async(buffer, _buffer_get_data_callback)

```

### BufferGetDeviceAddress

```csharp
public ulong BufferGetDeviceAddress(Rid buffer)
```

引擎名：`buffer_get_device_address`

Returns the address of the given `buffer` which can be passed to shaders in any way to access underlying data. Buffer must have been created with this feature enabled.

**Note:** You must check that the GPU supports this functionality by calling `HasFeature()` with [`RenderingDevice.Features.BufferDeviceAddress`](RenderingDevice.md) as a parameter.

### BufferUpdate

```csharp
public Error BufferUpdate(Rid buffer, uint offset, uint sizeBytes, ReadOnlySpan<byte> data)
```

引擎名：`buffer_update`

Updates a region of `sizeBytes` bytes, starting at `offset`, in the buffer, with the specified `data`.

Prints an error if:

- the region specified by `offset` + `sizeBytes` exceeds the buffer

- a draw list is currently active (created by `DrawListBegin()`)

- a compute list is currently active (created by `ComputeListBegin()`)

### BufferUpdate

```csharp
public Error BufferUpdate(Rid buffer, uint offset, uint sizeBytes, ReadOnlySpan<byte> data, RenderingDevice.BarrierMask postBarrier)
```

引擎名：`buffer_update`

Updates a region of `sizeBytes` bytes, starting at `offset`, in the buffer, with the specified `data`.

Prints an error if:

- the region specified by `offset` + `sizeBytes` exceeds the buffer

- a draw list is currently active (created by `DrawListBegin()`)

- a compute list is currently active (created by `ComputeListBegin()`)

### BufferUpdate

```csharp
public Error BufferUpdate(Rid buffer, uint offset, uint sizeBytes, byte[] data)
```

引擎名：`buffer_update`

Updates a region of `sizeBytes` bytes, starting at `offset`, in the buffer, with the specified `data`.

Prints an error if:

- the region specified by `offset` + `sizeBytes` exceeds the buffer

- a draw list is currently active (created by `DrawListBegin()`)

- a compute list is currently active (created by `ComputeListBegin()`)

### BufferUpdate

```csharp
public Error BufferUpdate(Rid buffer, uint offset, uint sizeBytes, byte[] data, RenderingDevice.BarrierMask postBarrier)
```

引擎名：`buffer_update`

Updates a region of `sizeBytes` bytes, starting at `offset`, in the buffer, with the specified `data`.

Prints an error if:

- the region specified by `offset` + `sizeBytes` exceeds the buffer

- a draw list is currently active (created by `DrawListBegin()`)

- a compute list is currently active (created by `ComputeListBegin()`)

### CaptureTimestamp

```csharp
public void CaptureTimestamp(string name)
```

引擎名：`capture_timestamp`

Creates a timestamp marker with the specified `name`. This is used for performance reporting with the `GetCapturedTimestampCpuTime()`, `GetCapturedTimestampGpuTime()` and `GetCapturedTimestampName()` methods.

### ComputeListAddBarrier

```csharp
public void ComputeListAddBarrier(long computeList)
```

引擎名：`compute_list_add_barrier`

Raises a Vulkan compute barrier in the specified `computeList`.

### ComputeListBegin

```csharp
public long ComputeListBegin()
```

引擎名：`compute_list_begin`

Starts a list of compute commands created with the `compute_*` methods. The returned value should be passed to other `compute_list_*` functions.

Multiple compute lists cannot be created at the same time; you must finish the previous compute list first using `ComputeListEnd()`.

A simple compute operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var compute_list = rd.compute_list_begin()

rd.compute_list_bind_compute_pipeline(compute_list, compute_shader_dilate_pipeline)
rd.compute_list_bind_uniform_set(compute_list, compute_base_uniform_set, 0)
rd.compute_list_bind_uniform_set(compute_list, dilate_uniform_set, 1)

for i in atlas_slices:
rd.compute_list_set_push_constant(compute_list, push_constant, push_constant.size())
rd.compute_list_dispatch(compute_list, group_size.x, group_size.y, group_size.z)
# No barrier, let them run all together.

rd.compute_list_end()

```

### ComputeListBegin

```csharp
public long ComputeListBegin(bool allowDrawOverlap)
```

引擎名：`compute_list_begin`

Starts a list of compute commands created with the `compute_*` methods. The returned value should be passed to other `compute_list_*` functions.

Multiple compute lists cannot be created at the same time; you must finish the previous compute list first using `ComputeListEnd()`.

A simple compute operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var compute_list = rd.compute_list_begin()

rd.compute_list_bind_compute_pipeline(compute_list, compute_shader_dilate_pipeline)
rd.compute_list_bind_uniform_set(compute_list, compute_base_uniform_set, 0)
rd.compute_list_bind_uniform_set(compute_list, dilate_uniform_set, 1)

for i in atlas_slices:
rd.compute_list_set_push_constant(compute_list, push_constant, push_constant.size())
rd.compute_list_dispatch(compute_list, group_size.x, group_size.y, group_size.z)
# No barrier, let them run all together.

rd.compute_list_end()

```

### ComputeListBindComputePipeline

```csharp
public void ComputeListBindComputePipeline(long computeList, Rid computePipeline)
```

引擎名：`compute_list_bind_compute_pipeline`

Tells the GPU what compute pipeline to use when processing the compute list. If the shader has changed since the last time this function was called, Godot will unbind all descriptor sets and will re-bind them inside `ComputeListDispatch()`.

### ComputeListBindUniformSet

```csharp
public void ComputeListBindUniformSet(long computeList, Rid uniformSet, uint setIndex)
```

引擎名：`compute_list_bind_uniform_set`

Binds the `uniformSet` to this `computeList`. Godot ensures that all textures in the uniform set have the correct Vulkan access masks. If Godot had to change access masks of textures, it will raise a Vulkan image memory barrier.

### ComputeListDispatch

```csharp
public void ComputeListDispatch(long computeList, uint xGroups, uint yGroups, uint zGroups)
```

引擎名：`compute_list_dispatch`

Submits the compute list for processing on the GPU. This is the compute equivalent to `DrawListDraw()`.

### ComputeListDispatchIndirect

```csharp
public void ComputeListDispatchIndirect(long computeList, Rid buffer, uint offset)
```

引擎名：`compute_list_dispatch_indirect`

Submits the compute list for processing on the GPU with the given group counts stored in the `buffer` at `offset`. Buffer must have been created with [`RenderingDevice.StorageBufferUsage.Indirect`](RenderingDevice.md) flag.

### ComputeListEnd

```csharp
public void ComputeListEnd()
```

引擎名：`compute_list_end`

Finishes a list of compute commands created with the `compute_*` methods.

### ComputeListEnd

```csharp
public void ComputeListEnd(RenderingDevice.BarrierMask postBarrier)
```

引擎名：`compute_list_end`

Finishes a list of compute commands created with the `compute_*` methods.

### ComputeListSetPushConstant

```csharp
public void ComputeListSetPushConstant(long computeList, ReadOnlySpan<byte> buffer, uint sizeBytes)
```

引擎名：`compute_list_set_push_constant`

Sets the push constant data to `buffer` for the specified `computeList`. The shader determines how this binary data is used. The buffer's size in bytes must also be specified in `sizeBytes` (this can be obtained by calling the `PackedByteArray.size` method on the passed `buffer`).

### ComputeListSetPushConstant

```csharp
public void ComputeListSetPushConstant(long computeList, byte[] buffer, uint sizeBytes)
```

引擎名：`compute_list_set_push_constant`

Sets the push constant data to `buffer` for the specified `computeList`. The shader determines how this binary data is used. The buffer's size in bytes must also be specified in `sizeBytes` (this can be obtained by calling the `PackedByteArray.size` method on the passed `buffer`).

### ComputePipelineCreate

```csharp
public Rid ComputePipelineCreate(Rid shader, Array<RDPipelineSpecializationConstant> specializationConstants = null)
```

引擎名：`compute_pipeline_create`

Creates a new compute pipeline. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when the `shader` is freed.

### ComputePipelineIsValid

```csharp
public bool ComputePipelineIsValid(Rid computePipeline)
```

引擎名：`compute_pipeline_is_valid`

Returns `true` if the compute pipeline specified by the `computePipeline` RID is valid, `false` otherwise.

### CreateLocalDevice

```csharp
public RenderingDevice CreateLocalDevice()
```

引擎名：`create_local_device`

Create a new local `RenderingDevice`. This is most useful for performing compute operations on the GPU independently from the rest of the engine.

### DrawCommandBeginLabel

```csharp
public void DrawCommandBeginLabel(string name, Color color)
```

引擎名：`draw_command_begin_label`

Create a command buffer debug label region that can be displayed in third-party tools such as [RenderDoc](https://renderdoc.org/). All regions must be ended with a `DrawCommandEndLabel()` call. When viewed from the linear series of submissions to a single queue, calls to `DrawCommandBeginLabel()` and `DrawCommandEndLabel()` must be matched and balanced.

The `VK_EXT_DEBUG_UTILS_EXTENSION_NAME` Vulkan extension must be available and enabled for command buffer debug label region to work. See also `DrawCommandEndLabel()`.

### DrawCommandEndLabel

```csharp
public void DrawCommandEndLabel()
```

引擎名：`draw_command_end_label`

Ends the command buffer debug label region started by a `DrawCommandBeginLabel()` call.

### DrawCommandInsertLabel

```csharp
public void DrawCommandInsertLabel(string name, Color color)
```

引擎名：`draw_command_insert_label`

This method does nothing.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.DrawFlags drawFlags = RenderingDevice.DrawFlags.DefaultAll, Color[] clearColorValues = null, float clearDepthValue = 1f, uint clearStencilValue = 0, Rect2? region = null, uint breadcrumb = 0)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `clearColorValues`：If the parameter is null, then the default value is `Array.Empty<Color>()`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.DrawFlags drawFlags, ReadOnlySpan<Color> clearColorValues, float clearDepthValue, uint clearStencilValue, Rect2? region, uint breadcrumb)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, Color[] clearColorValues, float clearDepth, uint clearStencil, Rect2? region)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `clearColorValues`：If the parameter is null, then the default value is `Array.Empty<Color>()`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, Color[] clearColorValues, float clearDepth, uint clearStencil, Rect2? region, Array storageTextures)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `clearColorValues`：If the parameter is null, then the default value is `Array.Empty<Color>()`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, Color[] clearColorValues, float clearDepth, uint clearStencil, Rect2? region, Array<Rid> storageTextures)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `clearColorValues`：If the parameter is null, then the default value is `Array.Empty<Color>()`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, Color[] clearColorValues, float clearDepth, uint clearStencil, Rect2? region, uint breadcrumb)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `clearColorValues`：If the parameter is null, then the default value is `Array.Empty<Color>()`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, ReadOnlySpan<Color> clearColorValues, float clearDepth, uint clearStencil, Rect2? region)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, ReadOnlySpan<Color> clearColorValues, float clearDepth, uint clearStencil, Rect2? region, Array<Rid> storageTextures)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBegin

```csharp
public long DrawListBegin(Rid framebuffer, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, ReadOnlySpan<Color> clearColorValues, float clearDepth, uint clearStencil, Rect2? region, uint breadcrumb)
```

引擎名：`draw_list_begin`

Starts a list of raster drawing commands created with the `draw_*` methods. The returned value should be passed to other `draw_list_*` functions.

Multiple draw lists cannot be created at the same time; you must finish the previous draw list first using `DrawListEnd()`.

A simple drawing operation might look like this (code is not a complete example):

```csharp
var rd = RenderingDevice.new()
var clear_colors = PackedColorArray([Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(0, 0, 0, 0)])
var draw_list = rd.draw_list_begin(framebuffers[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS)

# Draw opaque.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)
# Draw wire.
rd.draw_list_bind_render_pipeline(draw_list, raster_pipeline_wire)
rd.draw_list_bind_uniform_set(draw_list, raster_base_uniform, 0)
rd.draw_list_set_push_constant(draw_list, raster_push_constant, raster_push_constant.size())
rd.draw_list_draw(draw_list, false, 1, slice_triangle_count[i] * 3)

rd.draw_list_end()

```

The `drawFlags` indicates if the texture attachments of the framebuffer should be cleared or ignored. Only one of the two flags can be used for each individual attachment. Ignoring an attachment means that any contents that existed before the draw list will be completely discarded, reducing the memory bandwidth used by the render pass but producing garbage results if the pixels aren't replaced. The default behavior allows the engine to figure out the right operation to use if the texture is discardable, which can result in increased performance. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

The `breadcrumb` parameter can be an arbitrary 32-bit integer that is useful to diagnose GPU crashes. If Godot is built in dev or debug mode; when the GPU crashes Godot will dump all shaders that were being executed at the time of the crash and the breadcrumb is useful to diagnose what passes did those shaders belong to.

It does not affect rendering behavior and can be set to 0. It is recommended to use [`RenderingDevice.BreadcrumbMarker`](RenderingDevice.md) enumerations for consistency but it's not required. It is also possible to use bitwise operations to add extra data. e.g.

```csharp
rd.draw_list_begin(fb[i], RenderingDevice.CLEAR_COLOR_ALL, clear_colors, true, 1.0f, true, 0, Rect2(), RenderingDevice.OPAQUE_PASS | 5)

```

参数：
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBeginForScreen

```csharp
public long DrawListBeginForScreen(int screen = 0, Color? clearColor = null)
```

引擎名：`draw_list_begin_for_screen`

High-level variant of `DrawListBegin()`, with the parameters automatically being adjusted for drawing onto the window specified by the `screen` ID.

**Note:** Cannot be used with local RenderingDevices, as these don't have a screen. If called on a local RenderingDevice, `DrawListBeginForScreen()` returns `InvalidId`.

参数：
- `clearColor`：If the parameter is null, then the default value is `new Color(0.0f, 0.0f, 0.0f, 1.0f)`.

### DrawListBeginSplit

```csharp
public long[] DrawListBeginSplit(Rid framebuffer, uint splits, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, Color[] clearColorValues = null, float clearDepth = 1f, uint clearStencil = 0, Rect2? region = null, Array<Rid> storageTextures = null)
```

引擎名：`draw_list_begin_split`

This method does nothing and always returns an empty `System.Int64`[].

参数：
- `clearColorValues`：If the parameter is null, then the default value is `Array.Empty<Color>()`.
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBeginSplit

```csharp
public long[] DrawListBeginSplit(Rid framebuffer, uint splits, RenderingDevice.InitialAction initialColorAction, RenderingDevice.FinalAction finalColorAction, RenderingDevice.InitialAction initialDepthAction, RenderingDevice.FinalAction finalDepthAction, ReadOnlySpan<Color> clearColorValues, float clearDepth, uint clearStencil, Rect2? region, Array<Rid> storageTextures)
```

引擎名：`draw_list_begin_split`

This method does nothing and always returns an empty `System.Int64`[].

参数：
- `region`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListBindIndexArray

```csharp
public void DrawListBindIndexArray(long drawList, Rid indexArray)
```

引擎名：`draw_list_bind_index_array`

Binds `indexArray` to the specified `drawList`.

### DrawListBindRenderPipeline

```csharp
public void DrawListBindRenderPipeline(long drawList, Rid renderPipeline)
```

引擎名：`draw_list_bind_render_pipeline`

Binds `renderPipeline` to the specified `drawList`.

### DrawListBindUniformSet

```csharp
public void DrawListBindUniformSet(long drawList, Rid uniformSet, uint setIndex)
```

引擎名：`draw_list_bind_uniform_set`

Binds `uniformSet` to the specified `drawList`. A `setIndex` must also be specified, which is an identifier starting from `0` that must match the one expected by the draw list.

### DrawListBindVertexArray

```csharp
public void DrawListBindVertexArray(long drawList, Rid vertexArray)
```

引擎名：`draw_list_bind_vertex_array`

Binds `vertexArray` to the specified `drawList`.

### DrawListBindVertexBuffersFormat

```csharp
public void DrawListBindVertexBuffersFormat(long drawList, long vertexFormat, uint vertexCount, Array<Rid> vertexBuffers, ReadOnlySpan<long> offsets)
```

引擎名：`draw_list_bind_vertex_buffers_format`

Binds a set of `vertexBuffers` directly to the specified `drawList` using `vertexFormat` without creating a vertex array RID. Provide the number of vertices in `vertexCount`; optional per-buffer byte `offsets` may also be supplied.

### DrawListBindVertexBuffersFormat

```csharp
public void DrawListBindVertexBuffersFormat(long drawList, long vertexFormat, uint vertexCount, Array<Rid> vertexBuffers, long[] offsets = null)
```

引擎名：`draw_list_bind_vertex_buffers_format`

Binds a set of `vertexBuffers` directly to the specified `drawList` using `vertexFormat` without creating a vertex array RID. Provide the number of vertices in `vertexCount`; optional per-buffer byte `offsets` may also be supplied.

参数：
- `offsets`：If the parameter is null, then the default value is `Array.Empty<long>()`.

### DrawListDisableScissor

```csharp
public void DrawListDisableScissor(long drawList)
```

引擎名：`draw_list_disable_scissor`

Removes and disables the scissor rectangle for the specified `drawList`. See also `DrawListEnableScissor()`.

### DrawListDraw

```csharp
public void DrawListDraw(long drawList, bool useIndices, uint instances, uint proceduralVertexCount = 0)
```

引擎名：`draw_list_draw`

Submits `drawList` for rendering on the GPU. This is the raster equivalent to `ComputeListDispatch()`.

### DrawListDrawIndirect

```csharp
public void DrawListDrawIndirect(long drawList, bool useIndices, Rid buffer, uint offset = 0, uint drawCount = 1, uint stride = 0)
```

引擎名：`draw_list_draw_indirect`

Submits `drawList` for rendering on the GPU with the given parameters stored in the `buffer` at `offset`. Parameters being integers: vertex count, instance count, first vertex, first instance. And when using indices: index count, instance count, first index, vertex offset, first instance. Buffer must have been created with [`RenderingDevice.StorageBufferUsage.Indirect`](RenderingDevice.md) flag.

### DrawListEnableScissor

```csharp
public void DrawListEnableScissor(long drawList, Rect2? rect = null)
```

引擎名：`draw_list_enable_scissor`

Creates a scissor rectangle and enables it for the specified `drawList`. Scissor rectangles are used for clipping by discarding fragments that fall outside a specified rectangular portion of the screen. See also `DrawListDisableScissor()`.

**Note:** The specified `rect` is automatically intersected with the screen's dimensions, which means it cannot exceed the screen's dimensions.

参数：
- `rect`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### DrawListEnd

```csharp
public void DrawListEnd()
```

引擎名：`draw_list_end`

Finishes a list of raster drawing commands created with the `draw_*` methods.

### DrawListEnd

```csharp
public void DrawListEnd(RenderingDevice.BarrierMask postBarrier)
```

引擎名：`draw_list_end`

Finishes a list of raster drawing commands created with the `draw_*` methods.

### DrawListSetBlendConstants

```csharp
public void DrawListSetBlendConstants(long drawList, Color color)
```

引擎名：`draw_list_set_blend_constants`

Sets blend constants for the specified `drawList` to `color`. Blend constants are used only if the graphics pipeline is created with [`RenderingDevice.PipelineDynamicStateFlags.BlendConstants`](RenderingDevice.md) flag set.

### DrawListSetPushConstant

```csharp
public void DrawListSetPushConstant(long drawList, ReadOnlySpan<byte> buffer, uint sizeBytes)
```

引擎名：`draw_list_set_push_constant`

Sets the push constant data to `buffer` for the specified `drawList`. The shader determines how this binary data is used. The buffer's size in bytes must also be specified in `sizeBytes` (this can be obtained by calling the `PackedByteArray.size` method on the passed `buffer`).

### DrawListSetPushConstant

```csharp
public void DrawListSetPushConstant(long drawList, byte[] buffer, uint sizeBytes)
```

引擎名：`draw_list_set_push_constant`

Sets the push constant data to `buffer` for the specified `drawList`. The shader determines how this binary data is used. The buffer's size in bytes must also be specified in `sizeBytes` (this can be obtained by calling the `PackedByteArray.size` method on the passed `buffer`).

### DrawListSwitchToNextPass

```csharp
public long DrawListSwitchToNextPass()
```

引擎名：`draw_list_switch_to_next_pass`

Switches to the next draw pass.

### DrawListSwitchToNextPassSplit

```csharp
public long[] DrawListSwitchToNextPassSplit(uint splits)
```

引擎名：`draw_list_switch_to_next_pass_split`

This method does nothing and always returns an empty `System.Int64`[].

### FramebufferCreate

```csharp
public Rid FramebufferCreate(Array<Rid> textures, long validateWithFormat = -1, uint viewCount = 1)
```

引擎名：`framebuffer_create`

Creates a new framebuffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when any of the `textures` is freed.

### FramebufferCreateEmpty

```csharp
public Rid FramebufferCreateEmpty(Vector2I size, RenderingDevice.TextureSamples samples = RenderingDevice.TextureSamples.Samples1, long validateWithFormat = -1)
```

引擎名：`framebuffer_create_empty`

Creates a new empty framebuffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### FramebufferCreateMultipass

```csharp
public Rid FramebufferCreateMultipass(Array<Rid> textures, Array<RDFramebufferPass> passes, long validateWithFormat = -1, uint viewCount = 1)
```

引擎名：`framebuffer_create_multipass`

Creates a new multipass framebuffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when any of the `textures` is freed.

### FramebufferFormatCreate

```csharp
public long FramebufferFormatCreate(Array<RDAttachmentFormat> attachments, uint viewCount = 1)
```

引擎名：`framebuffer_format_create`

Creates a new framebuffer format with the specified `attachments` and `viewCount`. Returns the new framebuffer's unique framebuffer format ID.

If `viewCount` is greater than or equal to `2`, enables multiview which is used for VR rendering. This requires support for the Vulkan multiview extension.

### FramebufferFormatCreateEmpty

```csharp
public long FramebufferFormatCreateEmpty(RenderingDevice.TextureSamples samples = RenderingDevice.TextureSamples.Samples1)
```

引擎名：`framebuffer_format_create_empty`

Creates a new empty framebuffer format with the specified number of `samples` and returns its ID.

### FramebufferFormatCreateMultipass

```csharp
public long FramebufferFormatCreateMultipass(Array<RDAttachmentFormat> attachments, Array<RDFramebufferPass> passes, uint viewCount = 1)
```

引擎名：`framebuffer_format_create_multipass`

Creates a multipass framebuffer format with the specified `attachments`, `passes` and `viewCount` and returns its ID. If `viewCount` is greater than or equal to `2`, enables multiview which is used for VR rendering. This requires support for the Vulkan multiview extension.

### FramebufferFormatGetTextureSamples

```csharp
public RenderingDevice.TextureSamples FramebufferFormatGetTextureSamples(long format, uint renderPass = 0)
```

引擎名：`framebuffer_format_get_texture_samples`

Returns the number of texture samples used for the given framebuffer `format` ID (returned by `FramebufferGetFormat()`).

### FramebufferGetFormat

```csharp
public long FramebufferGetFormat(Rid framebuffer)
```

引擎名：`framebuffer_get_format`

Returns the format ID of the framebuffer specified by the `framebuffer` RID. This ID is guaranteed to be unique for the same formats and does not need to be freed.

### FramebufferIsValid

```csharp
public bool FramebufferIsValid(Rid framebuffer)
```

引擎名：`framebuffer_is_valid`

Returns `true` if the framebuffer specified by the `framebuffer` RID is valid, `false` otherwise.

### FreeRid

```csharp
public void FreeRid(Rid rid)
```

引擎名：`free_rid`

Tries to free an object in the RenderingDevice. To avoid memory leaks, this should be called after using an object as memory management does not occur automatically when using RenderingDevice directly.

### FullBarrier

```csharp
public void FullBarrier()
```

引擎名：`full_barrier`

This method does nothing.

### GetCapturedTimestampCpuTime

```csharp
public ulong GetCapturedTimestampCpuTime(uint index)
```

引擎名：`get_captured_timestamp_cpu_time`

Returns the timestamp in CPU time for the rendering step specified by `index` (in microseconds since the engine started). See also `GetCapturedTimestampGpuTime()` and `CaptureTimestamp()`.

### GetCapturedTimestampGpuTime

```csharp
public ulong GetCapturedTimestampGpuTime(uint index)
```

引擎名：`get_captured_timestamp_gpu_time`

Returns the timestamp in GPU time for the rendering step specified by `index` (in microseconds since the engine started). See also `GetCapturedTimestampCpuTime()` and `CaptureTimestamp()`.

### GetCapturedTimestampName

```csharp
public string GetCapturedTimestampName(uint index)
```

引擎名：`get_captured_timestamp_name`

Returns the timestamp's name for the rendering step specified by `index`. See also `CaptureTimestamp()`.

### GetCapturedTimestampsCount

```csharp
public uint GetCapturedTimestampsCount()
```

引擎名：`get_captured_timestamps_count`

Returns the total number of timestamps (rendering steps) available for profiling.

### GetCapturedTimestampsFrame

```csharp
public ulong GetCapturedTimestampsFrame()
```

引擎名：`get_captured_timestamps_frame`

Returns the index of the last frame rendered that has rendering timestamps available for querying.

### GetDeviceAllocationCount

```csharp
public ulong GetDeviceAllocationCount()
```

引擎名：`get_device_allocation_count`

Returns how many allocations the GPU has performed for internal driver structures.

This is only used by Vulkan in debug builds and can return 0 when this information is not tracked or unknown.

### GetDeviceAllocsByObjectType

```csharp
public ulong GetDeviceAllocsByObjectType(uint type)
```

引擎名：`get_device_allocs_by_object_type`

Same as `GetDeviceAllocationCount()` but filtered for a given object type.

The type argument must be in range `[0; get_tracked_object_type_count - 1]`. If `GetTrackedObjectTypeCount()` is 0, then type argument is ignored and always returns 0.

This is only used by Vulkan in debug builds and can return 0 when this information is not tracked or unknown.

### GetDeviceMemoryByObjectType

```csharp
public ulong GetDeviceMemoryByObjectType(uint type)
```

引擎名：`get_device_memory_by_object_type`

Same as `GetDeviceTotalMemory()` but filtered for a given object type.

The type argument must be in range `[0; get_tracked_object_type_count - 1]`. If `GetTrackedObjectTypeCount()` is 0, then type argument is ignored and always returns 0.

This is only used by Vulkan in debug builds and can return 0 when this information is not tracked or unknown.

### GetDeviceName

```csharp
public string GetDeviceName()
```

引擎名：`get_device_name`

Returns the name of the video adapter (e.g. "GeForce GTX 1080/PCIe/SSE2"). Equivalent to [`RenderingServer.GetVideoAdapterName`](RenderingServer.md). See also `GetDeviceVendorName()`.

### GetDevicePipelineCacheUuid

```csharp
public string GetDevicePipelineCacheUuid()
```

引擎名：`get_device_pipeline_cache_uuid`

Returns the universally unique identifier for the pipeline cache. This is used to cache shader files on disk, which avoids shader recompilations on subsequent engine runs. This UUID varies depending on the graphics card model, but also the driver version. Therefore, updating graphics drivers will invalidate the shader cache.

### GetDeviceTotalMemory

```csharp
public ulong GetDeviceTotalMemory()
```

引擎名：`get_device_total_memory`

Returns how much bytes the GPU is using.

This is only used by Vulkan in debug builds and can return 0 when this information is not tracked or unknown.

### GetDeviceVendorName

```csharp
public string GetDeviceVendorName()
```

引擎名：`get_device_vendor_name`

Returns the vendor of the video adapter (e.g. "NVIDIA Corporation"). Equivalent to [`RenderingServer.GetVideoAdapterVendor`](RenderingServer.md). See also `GetDeviceName()`.

### GetDriverAllocationCount

```csharp
public ulong GetDriverAllocationCount()
```

引擎名：`get_driver_allocation_count`

Returns how many allocations the GPU driver has performed for internal driver structures.

This is only used by Vulkan in debug builds and can return 0 when this information is not tracked or unknown.

### GetDriverAllocsByObjectType

```csharp
public ulong GetDriverAllocsByObjectType(uint type)
```

引擎名：`get_driver_allocs_by_object_type`

Same as `GetDriverAllocationCount()` but filtered for a given object type.

The type argument must be in range `[0; get_tracked_object_type_count - 1]`. If `GetTrackedObjectTypeCount()` is 0, then type argument is ignored and always returns 0.

This is only used by Vulkan in debug builds and can return 0 when this information is not tracked or unknown.

### GetDriverAndDeviceMemoryReport

```csharp
public string GetDriverAndDeviceMemoryReport()
```

引擎名：`get_driver_and_device_memory_report`

Returns string report in CSV format using the following methods:

- `GetTrackedObjectName()`

- `GetTrackedObjectTypeCount()`

- `GetDriverTotalMemory()`

- `GetDriverAllocationCount()`

- `GetDriverMemoryByObjectType()`

- `GetDriverAllocsByObjectType()`

- `GetDeviceTotalMemory()`

- `GetDeviceAllocationCount()`

- `GetDeviceMemoryByObjectType()`

- `GetDeviceAllocsByObjectType()`

This is only used by Vulkan in debug builds. Godot must also be started with the `--extra-gpu-memory-tracking` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html).

### GetDriverMemoryByObjectType

```csharp
public ulong GetDriverMemoryByObjectType(uint type)
```

引擎名：`get_driver_memory_by_object_type`

Same as `GetDriverTotalMemory()` but filtered for a given object type.

The type argument must be in range `[0; get_tracked_object_type_count - 1]`. If `GetTrackedObjectTypeCount()` is 0, then type argument is ignored and always returns 0.

This is only used by Vulkan in debug builds and can return 0 when this information is not tracked or unknown.

### GetDriverResource

```csharp
public ulong GetDriverResource(RenderingDevice.DriverResource resource, Rid rid, ulong index)
```

引擎名：`get_driver_resource`

Returns the unique identifier of the driver `resource` for the specified `rid`. Some driver resource types ignore the specified `rid`. `index` is always ignored but must be specified anyway.

### GetDriverTotalMemory

```csharp
public ulong GetDriverTotalMemory()
```

引擎名：`get_driver_total_memory`

Returns how much bytes the GPU driver is using for internal driver structures.

This is only used by Vulkan in debug builds and can return 0 when this information is not tracked or unknown.

### GetFrameDelay

```csharp
public uint GetFrameDelay()
```

引擎名：`get_frame_delay`

Returns the frame count kept by the graphics API. Higher values result in higher input lag, but with more consistent throughput. For the main `RenderingDevice`, frames are cycled (usually 3 with triple-buffered V-Sync enabled). However, local `RenderingDevice`s only have 1 frame.

### GetMemoryUsage

```csharp
public ulong GetMemoryUsage(RenderingDevice.MemoryType type)
```

引擎名：`get_memory_usage`

Returns the memory usage in bytes corresponding to the given `type`. When using Vulkan, these statistics are calculated by [Vulkan Memory Allocator](https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator).

### GetPerfReport

```csharp
public string GetPerfReport()
```

引擎名：`get_perf_report`

Returns a string with a performance report from the past frame. Updates every frame.

### GetTrackedObjectName

```csharp
public string GetTrackedObjectName(uint typeIndex)
```

引擎名：`get_tracked_object_name`

Returns the name of the type of object for the given `typeIndex`. This value must be in range `[0; get_tracked_object_type_count - 1]`. If `GetTrackedObjectTypeCount()` is 0, then type argument is ignored and always returns the same string.

The return value is important because it gives meaning to the types passed to `GetDriverMemoryByObjectType()`, `GetDriverAllocsByObjectType()`, `GetDeviceMemoryByObjectType()`, and `GetDeviceAllocsByObjectType()`. Examples of strings it can return (not exhaustive):

- DEVICE_MEMORY

- PIPELINE_CACHE

- SWAPCHAIN_KHR

- COMMAND_POOL

Thus if e.g. `get_tracked_object_name(5)` returns "COMMAND_POOL", then `get_device_memory_by_object_type(5)` returns the bytes used by the GPU for command pools.

This is only used by Vulkan in debug builds. Godot must also be started with the `--extra-gpu-memory-tracking` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html).

### GetTrackedObjectTypeCount

```csharp
public ulong GetTrackedObjectTypeCount()
```

引擎名：`get_tracked_object_type_count`

Returns how many types of trackable objects there are.

This is only used by Vulkan in debug builds. Godot must also be started with the `--extra-gpu-memory-tracking` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html).

### HasFeature

```csharp
public bool HasFeature(RenderingDevice.Features feature)
```

引擎名：`has_feature`

Returns `true` if the `feature` is supported by the GPU.

### HitSbtCreate

```csharp
public Rid HitSbtCreate(Rid raytracingPipeline, uint initialHitGroupCapacity)
```

引擎名：`hit_sbt_create`

Creates a new hit shader binding table (SBT). It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when the `raytracingPipeline` is freed.

The hit SBT resizes itself as needed. `initialHitGroupCapacity` is used to allocate the initial backing memory.

### HitSbtRangeAlloc

```csharp
public long HitSbtRangeAlloc(Rid hitSbt, uint hitGroupCount)
```

引擎名：`hit_sbt_range_alloc`

Allocates a contiguous range of SBT entries from `hitSbt`.

The returned value should be assigned to [`RDAccelerationStructureInstance.HitSbtRange`](RDAccelerationStructureInstance.md).

During ray traversal, hit group index is computed as:

(geometry index in [`RDAccelerationStructureInstance.Blas`](RDAccelerationStructureInstance.md))

× (SBT stride used in `traceRayEXT`)

+ (SBT offset used in `traceRayEXT`)

+ (range offset)

`hitGroupCount` must be large enough to cover all SBT entries that may be indexed by this equation. This typically corresponds to:

(geometry count in [`RDAccelerationStructureInstance.Blas`](RDAccelerationStructureInstance.md))

× (SBT stride used in `traceRayEXT`)

The allocated range is uninitialized and must be filled using `HitSbtRangeUpdate()`.

### HitSbtRangeFree

```csharp
public Error HitSbtRangeFree(Rid hitSbt, long range)
```

引擎名：`hit_sbt_range_free`

Frees a hit SBT range previously allocated with `HitSbtRangeAlloc()`.

The range must not be in use by any acceleration structure after being freed.

### HitSbtRangeUpdate

```csharp
public Error HitSbtRangeUpdate(Rid hitSbt, long range, uint offset, ReadOnlySpan<int> hitGroupIndices)
```

引擎名：`hit_sbt_range_update`

Updates the contents of a hit SBT range.

`hitGroupIndices` specifies indices into the hit group array provided in `RaytracingPipelineCreate()`.

The `offset` parameter specifies where within the allocated range the writing begins. This allows partial updates of a range. However, the complete range must be fully initialized before it is used in a raytracing dispatch.

### HitSbtRangeUpdate

```csharp
public Error HitSbtRangeUpdate(Rid hitSbt, long range, uint offset, int[] hitGroupIndices)
```

引擎名：`hit_sbt_range_update`

Updates the contents of a hit SBT range.

`hitGroupIndices` specifies indices into the hit group array provided in `RaytracingPipelineCreate()`.

The `offset` parameter specifies where within the allocated range the writing begins. This allows partial updates of a range. However, the complete range must be fully initialized before it is used in a raytracing dispatch.

### HitSbtSetPipeline

```csharp
public Error HitSbtSetPipeline(Rid hitSbt, Rid raytracingPipeline)
```

引擎名：`hit_sbt_set_pipeline`

Sets a new `raytracingPipeline` for `hitSbt`.

The new pipeline must be a superset of the previous one. Existing hit groups must keep the same order and new hit groups should be appended to the end. This preserves existing SBT entries.

The previous pipeline must remain valid during the call.

### IndexArrayCreate

```csharp
public Rid IndexArrayCreate(Rid indexBuffer, uint indexOffset, uint indexCount)
```

引擎名：`index_array_create`

Creates a new index array. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when the `indexBuffer` is freed.

### IndexBufferCreate

```csharp
public Rid IndexBufferCreate(uint sizeIndices, RenderingDevice.IndexBufferFormat format, ReadOnlySpan<byte> data, bool useRestartIndices)
```

引擎名：`index_buffer_create`

Creates a new index buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### IndexBufferCreate

```csharp
public Rid IndexBufferCreate(uint sizeIndices, RenderingDevice.IndexBufferFormat format, ReadOnlySpan<byte> data, bool useRestartIndices, RenderingDevice.BufferCreationBits creationBits)
```

引擎名：`index_buffer_create`

Creates a new index buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### IndexBufferCreate

```csharp
public Rid IndexBufferCreate(uint sizeIndices, RenderingDevice.IndexBufferFormat format, byte[] data = null, bool useRestartIndices = false, RenderingDevice.BufferCreationBits creationBits = RenderingDevice.BufferCreationBits.)
```

引擎名：`index_buffer_create`

Creates a new index buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### IndexBufferCreate

```csharp
public Rid IndexBufferCreate(uint sizeIndices, RenderingDevice.IndexBufferFormat format, byte[] data, bool useRestartIndices)
```

引擎名：`index_buffer_create`

Creates a new index buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### LimitGet

```csharp
public ulong LimitGet(RenderingDevice.Limit limit)
```

引擎名：`limit_get`

Returns the value of the specified `limit`. This limit varies depending on the current graphics hardware (and sometimes the driver version). If the given limit is exceeded, rendering errors will occur.

Limits for various graphics hardware can be found in the [Vulkan Hardware Database](https://vulkan.gpuinfo.org/).

### RaytracingListBegin

```csharp
public long RaytracingListBegin()
```

引擎名：`raytracing_list_begin`

Starts a list of raytracing commands. The returned value should be passed to other `raytracing_list_*` functions.

Multiple raytracing lists cannot be created at the same time; you must finish the previous raytracing list first using `RaytracingListEnd()`.

A simple raytracing operation might look like this (code is not a complete example):

### RaytracingListBindRaytracingPipeline

```csharp
public void RaytracingListBindRaytracingPipeline(long raytracingList, Rid raytracingPipeline)
```

引擎名：`raytracing_list_bind_raytracing_pipeline`

Binds `raytracingPipeline` to the specified `raytracingList`.

### RaytracingListBindUniformSet

```csharp
public void RaytracingListBindUniformSet(long raytracingList, Rid uniformSet, uint setIndex)
```

引擎名：`raytracing_list_bind_uniform_set`

Binds the `uniformSet` to this `raytracingList`.

### RaytracingListEnd

```csharp
public void RaytracingListEnd()
```

引擎名：`raytracing_list_end`

Finishes a list of raytracing commands created with the `raytracing_*` methods.

### RaytracingListSetPushConstant

```csharp
public void RaytracingListSetPushConstant(long raytracingList, ReadOnlySpan<byte> buffer, uint sizeBytes)
```

引擎名：`raytracing_list_set_push_constant`

Sets the push constant data to `buffer` for the specified `raytracingList`. The shader determines how this binary data is used. The buffer's size in bytes must also be specified in `sizeBytes` (this can be obtained by calling the `PackedByteArray.size` method on the passed `buffer`).

### RaytracingListSetPushConstant

```csharp
public void RaytracingListSetPushConstant(long raytracingList, byte[] buffer, uint sizeBytes)
```

引擎名：`raytracing_list_set_push_constant`

Sets the push constant data to `buffer` for the specified `raytracingList`. The shader determines how this binary data is used. The buffer's size in bytes must also be specified in `sizeBytes` (this can be obtained by calling the `PackedByteArray.size` method on the passed `buffer`).

### RaytracingListTraceRays

```csharp
public void RaytracingListTraceRays(long raytracingList, uint raygenShaderIndex, Rid hitSbt, uint width, uint height, uint depth)
```

引擎名：`raytracing_list_trace_rays`

Initializes a raytracing dispatch for `raytracingList`, launching `width` × `height` × `depth` rays.

`raygenShaderIndex` selects the ray generation shader from the pipeline bound with `RaytracingListBindRaytracingPipeline()`.

`hitSbt` must use the same pipeline bound to `raytracingList`.

### RaytracingPipelineCreate

```csharp
public Rid RaytracingPipelineCreate(Array<RDPipelineShader> raygenShaders, Array<RDPipelineShader> missShaders, Array<RDHitGroup> hitGroups, uint maxTraceRecursionDepth)
```

引擎名：`raytracing_pipeline_create`

Creates a new raytracing pipeline. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

Each shader must provide the required stage. All stages must use compatible pipeline layouts. The pipeline selects the required stage from each shader.

Input order defines stable indices used by the API:

- `raygenShaders` is indexed in `RaytracingListTraceRays()`.

- `missShaders` is indexed in `traceRayEXT`.

- `hitGroups` is indexed in `HitSbtRangeUpdate()`.

### RaytracingPipelineIsValid

```csharp
public bool RaytracingPipelineIsValid(Rid raytracingPipeline)
```

引擎名：`raytracing_pipeline_is_valid`

Returns `true` if the raytracing pipeline specified by the `raytracingPipeline` RID is valid, `false` otherwise.

### RenderPipelineCreate

```csharp
public Rid RenderPipelineCreate(Rid shader, long framebufferFormat, long vertexFormat, RenderingDevice.RenderPrimitive primitive, RDPipelineRasterizationState rasterizationState, RDPipelineMultisampleState multisampleState, RDPipelineDepthStencilState stencilState, RDPipelineColorBlendState colorBlendState, RenderingDevice.PipelineDynamicStateFlags dynamicStateFlags = RenderingDevice.PipelineDynamicStateFlags., uint forRenderPass = 0, Array<RDPipelineSpecializationConstant> specializationConstants = null)
```

引擎名：`render_pipeline_create`

Creates a new render pipeline. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when the `shader` is freed.

### RenderPipelineIsValid

```csharp
public bool RenderPipelineIsValid(Rid renderPipeline)
```

引擎名：`render_pipeline_is_valid`

Returns `true` if the render pipeline specified by the `renderPipeline` RID is valid, `false` otherwise.

### SamplerCreate

```csharp
public Rid SamplerCreate(RDSamplerState state)
```

引擎名：`sampler_create`

Creates a new sampler. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### SamplerIsFormatSupportedForFilter

```csharp
public bool SamplerIsFormatSupportedForFilter(RenderingDevice.DataFormat format, RenderingDevice.SamplerFilter samplerFilter)
```

引擎名：`sampler_is_format_supported_for_filter`

Returns `true` if implementation supports using a texture of `format` with the given `samplerFilter`.

### ScreenGetFramebufferFormat

```csharp
public long ScreenGetFramebufferFormat()
```

引擎名：`screen_get_framebuffer_format`

Returns the framebuffer format of the given screen.

**Note:** Only the main `RenderingDevice` returned by [`RenderingServer.GetRenderingDevice`](RenderingServer.md) has a format. If called on a local `RenderingDevice`, this method prints an error and returns `InvalidId`.

### ScreenGetFramebufferFormat

```csharp
public long ScreenGetFramebufferFormat(int screen = 0)
```

引擎名：`screen_get_framebuffer_format`

Returns the framebuffer format of the given screen.

**Note:** Only the main `RenderingDevice` returned by [`RenderingServer.GetRenderingDevice`](RenderingServer.md) has a format. If called on a local `RenderingDevice`, this method prints an error and returns `InvalidId`.

### ScreenGetHeight

```csharp
public int ScreenGetHeight(int screen = 0)
```

引擎名：`screen_get_height`

Returns the window height matching the graphics API context for the given window ID (in pixels). Despite the parameter being named `screen`, this returns the *window* size. See also `ScreenGetWidth()`.

**Note:** Only the main `RenderingDevice` returned by [`RenderingServer.GetRenderingDevice`](RenderingServer.md) has a height. If called on a local `RenderingDevice`, this method prints an error and returns `InvalidId`.

### ScreenGetWidth

```csharp
public int ScreenGetWidth(int screen = 0)
```

引擎名：`screen_get_width`

Returns the window width matching the graphics API context for the given window ID (in pixels). Despite the parameter being named `screen`, this returns the *window* size. See also `ScreenGetHeight()`.

**Note:** Only the main `RenderingDevice` returned by [`RenderingServer.GetRenderingDevice`](RenderingServer.md) has a width. If called on a local `RenderingDevice`, this method prints an error and returns `InvalidId`.

### SetResourceName

```csharp
public void SetResourceName(Rid id, string name)
```

引擎名：`set_resource_name`

Sets the resource name for `id` to `name`. This is used for debugging with third-party tools such as [RenderDoc](https://renderdoc.org/).

The following types of resources can be named: texture, sampler, vertex buffer, index buffer, uniform buffer, texture buffer, storage buffer, uniform set buffer, shader, render pipeline and compute pipeline. Framebuffers cannot be named. Attempting to name an incompatible resource type will print an error.

**Note:** Resource names are only set when the engine runs in verbose mode ([`OS.IsStdOutVerbose`](OS.md) = `true`), or when using an engine build compiled with the `dev_mode=yes` SCons option. The graphics driver must also support the `VK_EXT_DEBUG_UTILS_EXTENSION_NAME` Vulkan extension for named resources to work.

### ShaderCompileBinaryFromSpirV

```csharp
public byte[] ShaderCompileBinaryFromSpirV(RDShaderSpirV spirVData, string name = "")
```

Compiles a binary shader from `spirVData` and returns the compiled binary data as a `System.Byte`[]. This compiled shader is specific to the GPU model and driver version used; it will not work on different GPU models or even different driver versions. See also `ShaderCompileSpirVFromSource()`.

`name` is an optional human-readable name that can be given to the compiled shader for organizational purposes.

### ShaderCompileSpirVFromSource

```csharp
public RDShaderSpirV ShaderCompileSpirVFromSource(RDShaderSource shaderSource, bool allowCache = true)
```

Compiles a SPIR-V from the shader source code in `shaderSource` and returns the SPIR-V as an [`RDShaderSpirV`](RDShaderSpirV.md). This intermediate language shader is portable across different GPU models and driver versions, but cannot be run directly by GPUs until compiled into a binary shader using `ShaderCompileBinaryFromSpirV()`.

If `allowCache` is `true`, make use of the shader cache generated by Godot. This avoids a potentially lengthy shader compilation step if the shader is already in cache. If `allowCache` is `false`, Godot's shader cache is ignored and the shader will always be recompiled.

### ShaderCreateFromBytecode

```csharp
public Rid ShaderCreateFromBytecode(ReadOnlySpan<byte> binaryData)
```

引擎名：`shader_create_from_bytecode`

Creates a new shader instance from a binary compiled shader. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method. See also `ShaderCompileBinaryFromSpirV()` and `ShaderCreateFromSpirV()`.

### ShaderCreateFromBytecode

```csharp
public Rid ShaderCreateFromBytecode(ReadOnlySpan<byte> binaryData, Rid placeholderRid)
```

引擎名：`shader_create_from_bytecode`

Creates a new shader instance from a binary compiled shader. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method. See also `ShaderCompileBinaryFromSpirV()` and `ShaderCreateFromSpirV()`.

### ShaderCreateFromBytecode

```csharp
public Rid ShaderCreateFromBytecode(byte[] binaryData)
```

引擎名：`shader_create_from_bytecode`

Creates a new shader instance from a binary compiled shader. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method. See also `ShaderCompileBinaryFromSpirV()` and `ShaderCreateFromSpirV()`.

### ShaderCreateFromBytecode

```csharp
public Rid ShaderCreateFromBytecode(byte[] binaryData, Rid placeholderRid = default)
```

引擎名：`shader_create_from_bytecode`

Creates a new shader instance from a binary compiled shader. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method. See also `ShaderCompileBinaryFromSpirV()` and `ShaderCreateFromSpirV()`.

### ShaderCreateFromSpirV

```csharp
public Rid ShaderCreateFromSpirV(RDShaderSpirV spirVData, string name = "")
```

Creates a new shader instance from SPIR-V intermediate code. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method. See also `ShaderCompileSpirVFromSource()` and `ShaderCreateFromBytecode()`.

### ShaderCreatePlaceholder

```csharp
public Rid ShaderCreatePlaceholder()
```

引擎名：`shader_create_placeholder`

Create a placeholder RID by allocating an RID without initializing it for use in `ShaderCreateFromBytecode()`. This allows you to create an RID for a shader and pass it around, but defer compiling the shader to a later time.

### ShaderGetVertexInputAttributeMask

```csharp
public ulong ShaderGetVertexInputAttributeMask(Rid shader)
```

引擎名：`shader_get_vertex_input_attribute_mask`

Returns the internal vertex input mask. Internally, the vertex input mask is an unsigned integer consisting of the locations (specified in GLSL via. `layout(location = ...)`) of the input variables (specified in GLSL by the `in` keyword).

### StorageBufferCreate

```csharp
public Rid StorageBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data, RenderingDevice.StorageBufferUsage usage)
```

引擎名：`storage_buffer_create`

Creates a [storage buffer](https://vkguide.dev/docs/chapter-4/storage_buffers/) with the specified `data` and `usage`. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### StorageBufferCreate

```csharp
public Rid StorageBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data, RenderingDevice.StorageBufferUsage usage, RenderingDevice.BufferCreationBits creationBits)
```

引擎名：`storage_buffer_create`

Creates a [storage buffer](https://vkguide.dev/docs/chapter-4/storage_buffers/) with the specified `data` and `usage`. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### StorageBufferCreate

```csharp
public Rid StorageBufferCreate(uint sizeBytes, byte[] data = null, RenderingDevice.StorageBufferUsage usage = RenderingDevice.StorageBufferUsage., RenderingDevice.BufferCreationBits creationBits = RenderingDevice.BufferCreationBits.)
```

引擎名：`storage_buffer_create`

Creates a [storage buffer](https://vkguide.dev/docs/chapter-4/storage_buffers/) with the specified `data` and `usage`. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### StorageBufferCreate

```csharp
public Rid StorageBufferCreate(uint sizeBytes, byte[] data, RenderingDevice.StorageBufferUsage usage)
```

引擎名：`storage_buffer_create`

Creates a [storage buffer](https://vkguide.dev/docs/chapter-4/storage_buffers/) with the specified `data` and `usage`. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### Submit

```csharp
public void Submit()
```

引擎名：`submit`

Pushes the frame setup and draw command buffers then marks the local device as currently processing (which allows calling `Sync()`).

**Note:** Only available in local RenderingDevices.

### Sync

```csharp
public void Sync()
```

引擎名：`sync`

Forces a synchronization between the CPU and GPU, which may be required in certain cases. Only call this when needed, as CPU-GPU synchronization has a performance cost.

**Note:** Only available in local RenderingDevices.

**Note:** `Sync()` can only be called after a `Submit()`.

### TextureBufferCreate

```csharp
public Rid TextureBufferCreate(uint sizeBytes, RenderingDevice.DataFormat format, ReadOnlySpan<byte> data)
```

引擎名：`texture_buffer_create`

Creates a new texture buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### TextureBufferCreate

```csharp
public Rid TextureBufferCreate(uint sizeBytes, RenderingDevice.DataFormat format, byte[] data = null)
```

引擎名：`texture_buffer_create`

Creates a new texture buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### TextureClear

```csharp
public Error TextureClear(Rid texture, Color color, uint baseMipmap, uint mipmapCount, uint baseLayer, uint layerCount)
```

引擎名：`texture_clear`

Clears the specified `texture` by replacing all of its pixels with the specified `color`. `baseMipmap` and `mipmapCount` determine which mipmaps of the texture are affected by this clear operation, while `baseLayer` and `layerCount` determine which layers of a 3D texture (or texture array) are affected by this clear operation. For 2D textures (which only have one layer by design), `baseLayer` must be `0` and `layerCount` must be `1`.

**Note:** `texture` can't be cleared while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to clear this texture.

### TextureClear

```csharp
public Error TextureClear(Rid texture, Color color, uint baseMipmap, uint mipmapCount, uint baseLayer, uint layerCount, RenderingDevice.BarrierMask postBarrier)
```

引擎名：`texture_clear`

Clears the specified `texture` by replacing all of its pixels with the specified `color`. `baseMipmap` and `mipmapCount` determine which mipmaps of the texture are affected by this clear operation, while `baseLayer` and `layerCount` determine which layers of a 3D texture (or texture array) are affected by this clear operation. For 2D textures (which only have one layer by design), `baseLayer` must be `0` and `layerCount` must be `1`.

**Note:** `texture` can't be cleared while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to clear this texture.

### TextureCopy

```csharp
public Error TextureCopy(Rid fromTexture, Rid toTexture, Vector3 fromPos, Vector3 toPos, Vector3 size, uint srcMipmap, uint dstMipmap, uint srcLayer, uint dstLayer)
```

引擎名：`texture_copy`

Copies the `fromTexture` to `toTexture` with the specified `fromPos`, `toPos` and `size` coordinates. For 2-dimensional textures, `fromPos` and `toPos` must have a Z axis of `0`, and `size` must have a Z axis of `1`. Source and destination mipmaps/layers must also be specified, with these parameters being `0` for textures without mipmaps or single-layer textures. Returns [`Error.Ok`](Error.md) if the texture copy was successful or [`Error.InvalidParameter`](Error.md) otherwise.

**Note:** `fromTexture` texture can't be copied while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to copy this texture.

**Note:** `fromTexture` texture requires the [`RenderingDevice.TextureUsageBits.CanCopyFromBit`](RenderingDevice.md) to be retrieved.

**Note:** `toTexture` can't be copied while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to copy this texture.

**Note:** `toTexture` requires the [`RenderingDevice.TextureUsageBits.CanCopyToBit`](RenderingDevice.md) to be retrieved.

**Note:** `fromTexture` and `toTexture` must be of the same type (color or depth).

### TextureCopy

```csharp
public Error TextureCopy(Rid fromTexture, Rid toTexture, Vector3 fromPos, Vector3 toPos, Vector3 size, uint srcMipmap, uint dstMipmap, uint srcLayer, uint dstLayer, RenderingDevice.BarrierMask postBarrier)
```

引擎名：`texture_copy`

Copies the `fromTexture` to `toTexture` with the specified `fromPos`, `toPos` and `size` coordinates. For 2-dimensional textures, `fromPos` and `toPos` must have a Z axis of `0`, and `size` must have a Z axis of `1`. Source and destination mipmaps/layers must also be specified, with these parameters being `0` for textures without mipmaps or single-layer textures. Returns [`Error.Ok`](Error.md) if the texture copy was successful or [`Error.InvalidParameter`](Error.md) otherwise.

**Note:** `fromTexture` texture can't be copied while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to copy this texture.

**Note:** `fromTexture` texture requires the [`RenderingDevice.TextureUsageBits.CanCopyFromBit`](RenderingDevice.md) to be retrieved.

**Note:** `toTexture` can't be copied while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to copy this texture.

**Note:** `toTexture` requires the [`RenderingDevice.TextureUsageBits.CanCopyToBit`](RenderingDevice.md) to be retrieved.

**Note:** `fromTexture` and `toTexture` must be of the same type (color or depth).

### TextureCreate

```csharp
public Rid TextureCreate(RDTextureFormat format, RDTextureView view, Array<byte[]> data = null)
```

引擎名：`texture_create`

Creates a new texture. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

**Note:** `data` takes an [`Collections.Array`](Collections.Array.md) of `System.Byte`[]s. For [`RenderingDevice.TextureType.Type1D`](RenderingDevice.md), [`RenderingDevice.TextureType.Type2D`](RenderingDevice.md), and [`RenderingDevice.TextureType.Type3D`](RenderingDevice.md) types, this array should only have one element, a `System.Byte`[] containing all the data for the texture. For `_ARRAY` and `_CUBE` types, the length should be the same as the number of [`RDTextureFormat.ArrayLayers`](RDTextureFormat.md) in `format`.

**Note:** Not to be confused with [`RenderingServer.Texture2DCreate`](RenderingServer.md), which creates the Godot-specific [`Texture2D`](Texture2D.md) resource as opposed to the graphics API's own texture type.

### TextureCreateFromExtension

```csharp
public Rid TextureCreateFromExtension(RenderingDevice.TextureType type, RenderingDevice.DataFormat format, RenderingDevice.TextureSamples samples, RenderingDevice.TextureUsageBits usageFlags, ulong image, ulong width, ulong height, ulong depth, ulong layers)
```

引擎名：`texture_create_from_extension`

Returns an RID for an existing `image` (`VkImage`) with the given `type`, `format`, `samples`, `usageFlags`, `width`, `height`, `depth`, `layers`, and `mipmaps`. This can be used to allow Godot to render onto foreign images.

### TextureCreateFromExtension

```csharp
public Rid TextureCreateFromExtension(RenderingDevice.TextureType type, RenderingDevice.DataFormat format, RenderingDevice.TextureSamples samples, RenderingDevice.TextureUsageBits usageFlags, ulong image, ulong width, ulong height, ulong depth, ulong layers, ulong mipmaps = 1)
```

引擎名：`texture_create_from_extension`

Returns an RID for an existing `image` (`VkImage`) with the given `type`, `format`, `samples`, `usageFlags`, `width`, `height`, `depth`, `layers`, and `mipmaps`. This can be used to allow Godot to render onto foreign images.

### TextureCreateShared

```csharp
public Rid TextureCreateShared(RDTextureView view, Rid withTexture)
```

引擎名：`texture_create_shared`

Creates a shared texture using the specified `view` and the texture information from `withTexture`.

This will be freed automatically when the `withTexture` is freed.

### TextureCreateSharedFromSlice

```csharp
public Rid TextureCreateSharedFromSlice(RDTextureView view, Rid withTexture, uint layer, uint mipmap, uint mipmaps = 1, RenderingDevice.TextureSliceType sliceType = RenderingDevice.TextureSliceType.Slice2D)
```

引擎名：`texture_create_shared_from_slice`

Creates a shared texture using the specified `view` and the texture information from `withTexture`'s `layer` and `mipmap`. The number of included mipmaps from the original texture can be controlled using the `mipmaps` parameter. Only relevant for textures with multiple layers, such as 3D textures, texture arrays and cubemaps. For single-layer textures, use `TextureCreateShared()`.

For 2D textures (which only have one layer), `layer` must be `0`.

**Note:** Layer slicing is only supported for 2D texture arrays, not 3D textures or cubemaps.

This will be freed automatically when the `withTexture` is freed.

### TextureGetData

```csharp
public byte[] TextureGetData(Rid texture, uint layer)
```

引擎名：`texture_get_data`

Returns the `texture` data for the specified `layer` as raw binary data. For 2D textures (which only have one layer), `layer` must be `0`.

**Note:** `texture` can't be retrieved while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to retrieve this texture. Otherwise, an error is printed and an empty `System.Byte`[] is returned.

**Note:** `texture` requires the [`RenderingDevice.TextureUsageBits.CanCopyFromBit`](RenderingDevice.md) to be retrieved. Otherwise, an error is printed and an empty `System.Byte`[] is returned.

**Note:** This method will block the GPU from working until the data is retrieved. Refer to `TextureGetDataAsync()` for an alternative that returns the data in more performant way.

### TextureGetDataAsync

```csharp
public Error TextureGetDataAsync(Rid texture, uint layer, Callable callback)
```

引擎名：`texture_get_data_async`

Asynchronous version of `TextureGetData()`. RenderingDevice will call `callback` in a certain amount of frames with the data the texture had at the time of the request.

**Note:** At the moment, the delay corresponds to the amount of frames specified by `ProjectSettings.rendering/rendering_device/vsync/frame_queue_size`.

**Note:** Downloading large textures can have a prohibitive cost for real-time even when using the asynchronous method due to hardware bandwidth limitations. When dealing with large resources, you can adjust settings such as `ProjectSettings.rendering/rendering_device/staging_buffer/texture_download_region_size_px` and `ProjectSettings.rendering/rendering_device/staging_buffer/block_size_kb` to improve the transfer speed at the cost of extra memory.

```csharp
func _texture_get_data_callback(array):
value = array.decode_u32(0)

...

rd.texture_get_data_async(texture, 0, _texture_get_data_callback)

```

### TextureGetFormat

```csharp
public RDTextureFormat TextureGetFormat(Rid texture)
```

引擎名：`texture_get_format`

Returns the data format used to create this texture.

### TextureGetNativeHandle

```csharp
public ulong TextureGetNativeHandle(Rid texture)
```

引擎名：`texture_get_native_handle`

Returns the internal graphics handle for this texture object. For use when communicating with third-party APIs mostly with GDExtension.

**Note:** This function returns a `uint64_t` which internally maps to a `GLuint` (OpenGL) or `VkImage` (Vulkan).

### TextureIsDiscardable

```csharp
public bool TextureIsDiscardable(Rid texture)
```

引擎名：`texture_is_discardable`

Returns `true` if the `texture` is discardable, `false` otherwise. See [`RDTextureFormat`](RDTextureFormat.md) or `TextureSetDiscardable()`.

### TextureIsFormatSupportedForUsage

```csharp
public bool TextureIsFormatSupportedForUsage(RenderingDevice.DataFormat format, RenderingDevice.TextureUsageBits usageFlags)
```

引擎名：`texture_is_format_supported_for_usage`

Returns `true` if the specified `format` is supported for the given `usageFlags`, `false` otherwise.

### TextureIsShared

```csharp
public bool TextureIsShared(Rid texture)
```

引擎名：`texture_is_shared`

Returns `true` if the `texture` is shared, `false` otherwise. See [`RDTextureView`](RDTextureView.md).

### TextureIsValid

```csharp
public bool TextureIsValid(Rid texture)
```

引擎名：`texture_is_valid`

Returns `true` if the `texture` is valid, `false` otherwise.

### TextureResolveMultisample

```csharp
public Error TextureResolveMultisample(Rid fromTexture, Rid toTexture)
```

引擎名：`texture_resolve_multisample`

Resolves the `fromTexture` texture onto `toTexture` with multisample antialiasing enabled. This must be used when rendering a framebuffer for MSAA to work. Returns [`Error.Ok`](Error.md) if successful, [`Error.InvalidParameter`](Error.md) otherwise.

**Note:** `fromTexture` and `toTexture` textures must have the same dimension, format and type (color or depth).

**Note:** `fromTexture` can't be copied while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to resolve this texture.

**Note:** `fromTexture` requires the [`RenderingDevice.TextureUsageBits.CanCopyFromBit`](RenderingDevice.md) to be retrieved.

**Note:** `fromTexture` must be multisampled and must also be 2D (or a slice of a 3D/cubemap texture).

**Note:** `toTexture` can't be copied while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to resolve this texture.

**Note:** `toTexture` texture requires the [`RenderingDevice.TextureUsageBits.CanCopyToBit`](RenderingDevice.md) to be retrieved.

**Note:** `toTexture` texture must **not** be multisampled and must also be 2D (or a slice of a 3D/cubemap texture).

### TextureResolveMultisample

```csharp
public Error TextureResolveMultisample(Rid fromTexture, Rid toTexture, RenderingDevice.BarrierMask postBarrier)
```

引擎名：`texture_resolve_multisample`

Resolves the `fromTexture` texture onto `toTexture` with multisample antialiasing enabled. This must be used when rendering a framebuffer for MSAA to work. Returns [`Error.Ok`](Error.md) if successful, [`Error.InvalidParameter`](Error.md) otherwise.

**Note:** `fromTexture` and `toTexture` textures must have the same dimension, format and type (color or depth).

**Note:** `fromTexture` can't be copied while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to resolve this texture.

**Note:** `fromTexture` requires the [`RenderingDevice.TextureUsageBits.CanCopyFromBit`](RenderingDevice.md) to be retrieved.

**Note:** `fromTexture` must be multisampled and must also be 2D (or a slice of a 3D/cubemap texture).

**Note:** `toTexture` can't be copied while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to resolve this texture.

**Note:** `toTexture` texture requires the [`RenderingDevice.TextureUsageBits.CanCopyToBit`](RenderingDevice.md) to be retrieved.

**Note:** `toTexture` texture must **not** be multisampled and must also be 2D (or a slice of a 3D/cubemap texture).

### TextureSetDiscardable

```csharp
public void TextureSetDiscardable(Rid texture, bool discardable)
```

引擎名：`texture_set_discardable`

Updates the discardable property of `texture`.

If a texture is discardable, its contents do not need to be preserved between frames. This flag is only relevant when the texture is used as target in a draw list.

This information is used by `RenderingDevice` to figure out if a texture's contents can be discarded, eliminating unnecessary writes to memory and boosting performance.

### TextureUpdate

```csharp
public Error TextureUpdate(Rid texture, uint layer, ReadOnlySpan<byte> data)
```

引擎名：`texture_update`

Updates texture data with new data, replacing the previous data in place. The updated texture data must have the same dimensions and format. For 2D textures (which only have one layer), `layer` must be `0`. Returns [`Error.Ok`](Error.md) if the update was successful, [`Error.InvalidParameter`](Error.md) otherwise.

**Note:** Updating textures is forbidden during creation of a draw or compute list.

**Note:** The existing `texture` can't be updated while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to update this texture.

**Note:** The existing `texture` requires the [`RenderingDevice.TextureUsageBits.CanUpdateBit`](RenderingDevice.md) to be updatable.

### TextureUpdate

```csharp
public Error TextureUpdate(Rid texture, uint layer, ReadOnlySpan<byte> data, RenderingDevice.BarrierMask postBarrier)
```

引擎名：`texture_update`

Updates texture data with new data, replacing the previous data in place. The updated texture data must have the same dimensions and format. For 2D textures (which only have one layer), `layer` must be `0`. Returns [`Error.Ok`](Error.md) if the update was successful, [`Error.InvalidParameter`](Error.md) otherwise.

**Note:** Updating textures is forbidden during creation of a draw or compute list.

**Note:** The existing `texture` can't be updated while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to update this texture.

**Note:** The existing `texture` requires the [`RenderingDevice.TextureUsageBits.CanUpdateBit`](RenderingDevice.md) to be updatable.

### TextureUpdate

```csharp
public Error TextureUpdate(Rid texture, uint layer, byte[] data)
```

引擎名：`texture_update`

Updates texture data with new data, replacing the previous data in place. The updated texture data must have the same dimensions and format. For 2D textures (which only have one layer), `layer` must be `0`. Returns [`Error.Ok`](Error.md) if the update was successful, [`Error.InvalidParameter`](Error.md) otherwise.

**Note:** Updating textures is forbidden during creation of a draw or compute list.

**Note:** The existing `texture` can't be updated while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to update this texture.

**Note:** The existing `texture` requires the [`RenderingDevice.TextureUsageBits.CanUpdateBit`](RenderingDevice.md) to be updatable.

### TextureUpdate

```csharp
public Error TextureUpdate(Rid texture, uint layer, byte[] data, RenderingDevice.BarrierMask postBarrier)
```

引擎名：`texture_update`

Updates texture data with new data, replacing the previous data in place. The updated texture data must have the same dimensions and format. For 2D textures (which only have one layer), `layer` must be `0`. Returns [`Error.Ok`](Error.md) if the update was successful, [`Error.InvalidParameter`](Error.md) otherwise.

**Note:** Updating textures is forbidden during creation of a draw or compute list.

**Note:** The existing `texture` can't be updated while a draw list that uses it as part of a framebuffer is being created. Ensure the draw list is finalized (and that the color/depth texture using it is not set to [`RenderingDevice.FinalAction.Continue`](RenderingDevice.md)) to update this texture.

**Note:** The existing `texture` requires the [`RenderingDevice.TextureUsageBits.CanUpdateBit`](RenderingDevice.md) to be updatable.

### TlasBuild

```csharp
public Error TlasBuild(Rid tlas, Array<RDAccelerationStructureInstance> instances)
```

引擎名：`tlas_build`

Builds the `tlas`. The contents of previous builds are discarded.

Any BLAS provided through the [`RDAccelerationStructureInstance.Blas`](RDAccelerationStructureInstance.md) member must already have been built using the `BlasBuild()` method.

The number of instances can be equal to or smaller than the maximum instance count provided in the `TlasCreate()` method.

**Note:** Freeing or rebuilding any of the provided BLASes after this method invalidates the TLAS and requires it to be rebuilt.

### TlasCreate

```csharp
public Rid TlasCreate(uint maxInstanceCount, RenderingDevice.AccelerationStructureFlagBits flags)
```

引擎名：`tlas_create`

Creates a new Top-Level Acceleration Structure (TLAS). It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### UniformBufferCreate

```csharp
public Rid UniformBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data)
```

引擎名：`uniform_buffer_create`

Creates a new uniform buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### UniformBufferCreate

```csharp
public Rid UniformBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data, RenderingDevice.BufferCreationBits creationBits)
```

引擎名：`uniform_buffer_create`

Creates a new uniform buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### UniformBufferCreate

```csharp
public Rid UniformBufferCreate(uint sizeBytes, byte[] data = null, RenderingDevice.BufferCreationBits creationBits = RenderingDevice.BufferCreationBits.)
```

引擎名：`uniform_buffer_create`

Creates a new uniform buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### UniformBufferCreate

```csharp
public Rid UniformBufferCreate(uint sizeBytes, byte[] data)
```

引擎名：`uniform_buffer_create`

Creates a new uniform buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### UniformSetCreate

```csharp
public Rid UniformSetCreate(Array<RDUniform> uniforms, Rid shader, uint shaderSet)
```

引擎名：`uniform_set_create`

Creates a new uniform set. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when the `shader` or any of the RIDs in the `uniforms` is freed.

### UniformSetIsValid

```csharp
public bool UniformSetIsValid(Rid uniformSet)
```

引擎名：`uniform_set_is_valid`

Checks if the `uniformSet` is valid, i.e. is owned.

### VertexArrayCreate

```csharp
public Rid VertexArrayCreate(uint vertexCount, long vertexFormat, Array<Rid> srcBuffers, ReadOnlySpan<long> offsets)
```

引擎名：`vertex_array_create`

Creates a vertex array based on the specified buffers. Optionally, `offsets` (in bytes) may be defined for each buffer.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when any of the `srcBuffers` is freed.

### VertexArrayCreate

```csharp
public Rid VertexArrayCreate(uint vertexCount, long vertexFormat, Array<Rid> srcBuffers, long[] offsets = null)
```

引擎名：`vertex_array_create`

Creates a vertex array based on the specified buffers. Optionally, `offsets` (in bytes) may be defined for each buffer.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

This will be freed automatically when any of the `srcBuffers` is freed.

参数：
- `offsets`：If the parameter is null, then the default value is `Array.Empty<long>()`.

### VertexBufferCreate

```csharp
public Rid VertexBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data, RenderingDevice.BufferCreationBits creationBits)
```

引擎名：`vertex_buffer_create`

Creates a new vertex buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### VertexBufferCreate

```csharp
public Rid VertexBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data, bool useAsStorage)
```

引擎名：`vertex_buffer_create`

Creates a new vertex buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

### VertexBufferCreate

```csharp
public Rid VertexBufferCreate(uint sizeBytes, byte[] data = null, RenderingDevice.BufferCreationBits creationBits = RenderingDevice.BufferCreationBits.)
```

引擎名：`vertex_buffer_create`

Creates a new vertex buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### VertexBufferCreate

```csharp
public Rid VertexBufferCreate(uint sizeBytes, byte[] data, bool useAsStorage)
```

引擎名：`vertex_buffer_create`

Creates a new vertex buffer. It can be accessed with the RID that is returned.

Once finished with your RID, you will want to free the RID using the RenderingDevice's `FreeRid()` method.

参数：
- `data`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### VertexFormatCreate

```csharp
public long VertexFormatCreate(Array<RDVertexAttribute> vertexDescriptions)
```

引擎名：`vertex_format_create`

Creates a new vertex format with the specified `vertexDescriptions`. Returns a unique vertex format ID corresponding to the newly created vertex format.

## 常量

### InvalidFormatId

```csharp
public const long InvalidFormatId = -1
```

Returned by functions that return a format ID if a value is invalid.

### InvalidId

```csharp
public const long InvalidId = -1
```

Returned by functions that return an ID if a value is invalid.

## AccelerationStructureFlagBits

命名空间 `Godot`；enum

### 枚举值

- `AllowUpdateBit = 1`：Allows the acceleration structure to be updated after it has been built.
- `AllowCompactionBit = 2`：Allows the acceleration structure to be compacted to reduce memory usage after it has been built.
- `PreferFastTraceBit = 4`：Prioritizes ray traversal performance over build performance when building the acceleration structure.
- `PreferFastBuildBit = 8`：Prioritizes build performance over ray traversal performance when building the acceleration structure.
- `LowMemoryBit = 16`：Reduces the memory usage of the acceleration structure, potentially at the cost of reduced ray traversal performance.

## AccelerationStructureGeometryFlagBits

命名空间 `Godot`；enum

### 枚举值

- `OpaqueBit = 1`：An opaque geometry does not invoke the any hit shaders.
- `NoDuplicateAnyHitInvocationBit = 2`：This geometry only calls the any hit shader a single time for each primitive.

## AccelerationStructureInstanceFlagBits

命名空间 `Godot`；enum

### 枚举值

- `TriangleFacingCullDisableBit = 1`：Disables triangle face culling for this instance during ray traversal.
- `TriangleFlipFacingBit = 2`：Flips the triangle facing direction for this instance during ray traversal.
- `ForceOpaqueBit = 4`：Forces all geometries in this instance to be treated as opaque, preventing any hit shaders from being invoked.
- `ForceNoOpaqueBit = 8`：Forces all geometries in this instance to be treated as non-opaque, allowing any hit shaders to be invoked.

## BarrierMask

命名空间 `Godot`；enum

### 枚举值

- `Vertex = 1`：Vertex shader barrier mask.
- `Fragment = 8`：Fragment shader barrier mask.
- `Compute = 2`：Compute barrier mask.
- `Transfer = 4`：Transfer barrier mask.
- `Raster = 9`：Raster barrier mask (vertex and fragment).
- `AllBarriers = 32767`：Barrier mask for all types (vertex, fragment, compute, transfer).
- `NoBarrier = 32768`：No barrier for any type.

## BlendFactor

命名空间 `Godot`；enum

### 枚举值

- `Zero = 0`：Constant `0.0` blend factor.
- `One = 1`：Constant `1.0` blend factor.
- `SrcColor = 2`：Color blend factor is `source color`.
- `OneMinusSrcColor = 3`：Color blend factor is `1.0 - source color`.
- `DstColor = 4`：Color blend factor is `destination color`.
- `OneMinusDstColor = 5`：Color blend factor is `1.0 - destination color`.
- `SrcAlpha = 6`：Color and alpha blend factor is `source alpha`.
- `OneMinusSrcAlpha = 7`：Color and alpha blend factor is `1.0 - source alpha`.
- `DstAlpha = 8`：Color and alpha blend factor is `destination alpha`.
- `OneMinusDstAlpha = 9`：Color and alpha blend factor is `1.0 - destination alpha`.
- `ConstantColor = 10`：Color blend factor is `blend constant color`.
- `OneMinusConstantColor = 11`：Color blend factor is `1.0 - blend constant color`.
- `ConstantAlpha = 12`：Color and alpha blend factor is `blend constant alpha` (see [`RenderingDevice.DrawListSetBlendConstants`](RenderingDevice.md)).
- `OneMinusConstantAlpha = 13`：Color and alpha blend factor is `1.0 - blend constant alpha` (see [`RenderingDevice.DrawListSetBlendConstants`](RenderingDevice.md)).
- `SrcAlphaSaturate = 14`：Color blend factor is `min(source alpha, 1.0 - destination alpha)`.
- `Src1Color = 15`：Color blend factor is `second source color`.
- `OneMinusSrc1Color = 16`：Color blend factor is `1.0 - second source color`.
- `Src1Alpha = 17`：Color and alpha blend factor is `second source alpha`.
- `OneMinusSrc1Alpha = 18`：Color and alpha blend factor is `1.0 - second source alpha`.
- `Max = 19`：Represents the size of the `RenderingDevice.BlendFactor` enum.

## BlendOperation

命名空间 `Godot`；enum

### 枚举值

- `Add = 0`：Additive blending operation (`source + destination`).
- `Subtract = 1`：Subtractive blending operation (`source - destination`).
- `ReverseSubtract = 2`：Reverse subtractive blending operation (`destination - source`).
- `Minimum = 3`：Minimum blending operation (keep the lowest value of the two).
- `Maximum = 4`：Maximum blending operation (keep the highest value of the two).
- `Max = 5`：Represents the size of the `RenderingDevice.BlendOperation` enum.

## BreadcrumbMarker

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No breadcrumb marker will be added.
- `ReflectionProbes = 65536`：During a GPU crash in dev or debug mode, Godot's error message will include `"REFLECTION_PROBES"` for added context as to when the crash occurred.
- `SkyPass = 131072`：During a GPU crash in dev or debug mode, Godot's error message will include `"SKY_PASS"` for added context as to when the crash occurred.
- `LightmapperPass = 196608`：During a GPU crash in dev or debug mode, Godot's error message will include `"LIGHTMAPPER_PASS"` for added context as to when the crash occurred.
- `ShadowPassDirectional = 262144`：During a GPU crash in dev or debug mode, Godot's error message will include `"SHADOW_PASS_DIRECTIONAL"` for added context as to when the crash occurred.
- `ShadowPassCube = 327680`：During a GPU crash in dev or debug mode, Godot's error message will include `"SHADOW_PASS_CUBE"` for added context as to when the crash occurred.
- `OpaquePass = 393216`：During a GPU crash in dev or debug mode, Godot's error message will include `"OPAQUE_PASS"` for added context as to when the crash occurred.
- `AlphaPass = 458752`：During a GPU crash in dev or debug mode, Godot's error message will include `"ALPHA_PASS"` for added context as to when the crash occurred.
- `TransparentPass = 524288`：During a GPU crash in dev or debug mode, Godot's error message will include `"TRANSPARENT_PASS"` for added context as to when the crash occurred.
- `PostProcessingPass = 589824`：During a GPU crash in dev or debug mode, Godot's error message will include `"POST_PROCESSING_PASS"` for added context as to when the crash occurred.
- `BlitPass = 655360`：During a GPU crash in dev or debug mode, Godot's error message will include `"BLIT_PASS"` for added context as to when the crash occurred.
- `UiPass = 720896`：During a GPU crash in dev or debug mode, Godot's error message will include `"UI_PASS"` for added context as to when the crash occurred.
- `DebugPass = 786432`：During a GPU crash in dev or debug mode, Godot's error message will include `"DEBUG_PASS"` for added context as to when the crash occurred.

## BufferCreationBits

命名空间 `Godot`；enum

### 枚举值

- `DeviceAddressBit = 1`：Optionally, set this flag if you wish to use [`RenderingDevice.BufferGetDeviceAddress`](RenderingDevice.md) functionality.
- `AsStorageBit = 2`：Set this flag so that it is created as storage.
- `AccelerationStructureBuildInputReadOnlyBit = 8`：Allows usage of this buffer as input data for an acceleration structure build operation.

## CompareOperator

命名空间 `Godot`；enum

### 枚举值

- `Never = 0`："Never" comparison (opposite of `Always`).
- `Less = 1`："Less than" comparison.
- `Equal = 2`："Equal" comparison.
- `LessOrEqual = 3`："Less than or equal" comparison.
- `Greater = 4`："Greater than" comparison.
- `NotEqual = 5`："Not equal" comparison.
- `GreaterOrEqual = 6`："Greater than or equal" comparison.
- `Always = 7`："Always" comparison (opposite of `Never`).
- `Max = 8`：Represents the size of the `RenderingDevice.CompareOperator` enum.

## DataFormat

命名空间 `Godot`；enum

### 枚举值

- `R4G4UnormPack8 = 0`：4-bit-per-channel red/green channel data format, packed into 8 bits.
- `R4G4B4A4UnormPack16 = 1`：4-bit-per-channel red/green/blue/alpha channel data format, packed into 16 bits.
- `B4G4R4A4UnormPack16 = 2`：4-bit-per-channel blue/green/red/alpha channel data format, packed into 16 bits.
- `R5G6B5UnormPack16 = 3`：Red/green/blue channel data format with 5 bits of red, 6 bits of green and 5 bits of blue, packed into 16 bits.
- `B5G6R5UnormPack16 = 4`：Blue/green/red channel data format with 5 bits of blue, 6 bits of green and 5 bits of red, packed into 16 bits.
- `R5G5B5A1UnormPack16 = 5`：Red/green/blue/alpha channel data format with 5 bits of red, 6 bits of green, 5 bits of blue and 1 bit of alpha, packed into 16 bits.
- `B5G5R5A1UnormPack16 = 6`：Blue/green/red/alpha channel data format with 5 bits of blue, 6 bits of green, 5 bits of red and 1 bit of alpha, packed into 16 bits.
- `A1R5G5B5UnormPack16 = 7`：Alpha/red/green/blue channel data format with 1 bit of alpha, 5 bits of red, 6 bits of green and 5 bits of blue, packed into 16 bits.
- `R8Unorm = 8`：8-bit-per-channel unsigned floating-point red channel data format with normalized value.
- `R8Snorm = 9`：8-bit-per-channel signed floating-point red channel data format with normalized value.
- `R8Uscaled = 10`：8-bit-per-channel unsigned floating-point red channel data format with scaled value (value is converted from integer to float).
- `R8Sscaled = 11`：8-bit-per-channel signed floating-point red channel data format with scaled value (value is converted from integer to float).
- `R8Uint = 12`：8-bit-per-channel unsigned integer red channel data format.
- `R8Sint = 13`：8-bit-per-channel signed integer red channel data format.
- `R8Srgb = 14`：8-bit-per-channel unsigned floating-point red channel data format with normalized value and nonlinear sRGB encoding.
- `R8G8Unorm = 15`：8-bit-per-channel unsigned floating-point red/green channel data format with normalized value.
- `R8G8Snorm = 16`：8-bit-per-channel signed floating-point red/green channel data format with normalized value.
- `R8G8Uscaled = 17`：8-bit-per-channel unsigned floating-point red/green channel data format with scaled value (value is converted from integer to float).
- `R8G8Sscaled = 18`：8-bit-per-channel signed floating-point red/green channel data format with scaled value (value is converted from integer to float).
- `R8G8Uint = 19`：8-bit-per-channel unsigned integer red/green channel data format.
- `R8G8Sint = 20`：8-bit-per-channel signed integer red/green channel data format.
- `R8G8Srgb = 21`：8-bit-per-channel unsigned floating-point red/green channel data format with normalized value and nonlinear sRGB encoding.
- `R8G8B8Unorm = 22`：8-bit-per-channel unsigned floating-point red/green/blue channel data format with normalized value.
- `R8G8B8Snorm = 23`：8-bit-per-channel signed floating-point red/green/blue channel data format with normalized value.
- `R8G8B8Uscaled = 24`：8-bit-per-channel unsigned floating-point red/green/blue channel data format with scaled value (value is converted from integer to float).
- `R8G8B8Sscaled = 25`：8-bit-per-channel signed floating-point red/green/blue channel data format with scaled value (value is converted from integer to float).
- `R8G8B8Uint = 26`：8-bit-per-channel unsigned integer red/green/blue channel data format.
- `R8G8B8Sint = 27`：8-bit-per-channel signed integer red/green/blue channel data format.
- `R8G8B8Srgb = 28`：8-bit-per-channel unsigned floating-point red/green/blue channel data format with normalized value and nonlinear sRGB encoding.
- `B8G8R8Unorm = 29`：8-bit-per-channel unsigned floating-point blue/green/red channel data format with normalized value.
- `B8G8R8Snorm = 30`：8-bit-per-channel signed floating-point blue/green/red channel data format with normalized value.
- `B8G8R8Uscaled = 31`：8-bit-per-channel unsigned floating-point blue/green/red channel data format with scaled value (value is converted from integer to float).
- `B8G8R8Sscaled = 32`：8-bit-per-channel signed floating-point blue/green/red channel data format with scaled value (value is converted from integer to float).
- `B8G8R8Uint = 33`：8-bit-per-channel unsigned integer blue/green/red channel data format.
- `B8G8R8Sint = 34`：8-bit-per-channel signed integer blue/green/red channel data format.
- `B8G8R8Srgb = 35`：8-bit-per-channel unsigned floating-point blue/green/red data format with normalized value and nonlinear sRGB encoding.
- `R8G8B8A8Unorm = 36`：8-bit-per-channel unsigned floating-point red/green/blue/alpha channel data format with normalized value.
- `R8G8B8A8Snorm = 37`：8-bit-per-channel signed floating-point red/green/blue/alpha channel data format with normalized value.
- `R8G8B8A8Uscaled = 38`：8-bit-per-channel unsigned floating-point red/green/blue/alpha channel data format with scaled value (value is converted from integer to float).
- `R8G8B8A8Sscaled = 39`：8-bit-per-channel signed floating-point red/green/blue/alpha channel data format with scaled value (value is converted from integer to float).
- `R8G8B8A8Uint = 40`：8-bit-per-channel unsigned integer red/green/blue/alpha channel data format.
- `R8G8B8A8Sint = 41`：8-bit-per-channel signed integer red/green/blue/alpha channel data format.
- `R8G8B8A8Srgb = 42`：8-bit-per-channel unsigned floating-point red/green/blue/alpha channel data format with normalized value and nonlinear sRGB encoding.
- `B8G8R8A8Unorm = 43`：8-bit-per-channel unsigned floating-point blue/green/red/alpha channel data format with normalized value.
- `B8G8R8A8Snorm = 44`：8-bit-per-channel signed floating-point blue/green/red/alpha channel data format with normalized value.
- `B8G8R8A8Uscaled = 45`：8-bit-per-channel unsigned floating-point blue/green/red/alpha channel data format with scaled value (value is converted from integer to float).
- `B8G8R8A8Sscaled = 46`：8-bit-per-channel signed floating-point blue/green/red/alpha channel data format with scaled value (value is converted from integer to float).
- `B8G8R8A8Uint = 47`：8-bit-per-channel unsigned integer blue/green/red/alpha channel data format.
- `B8G8R8A8Sint = 48`：8-bit-per-channel signed integer blue/green/red/alpha channel data format.
- `B8G8R8A8Srgb = 49`：8-bit-per-channel unsigned floating-point blue/green/red/alpha channel data format with normalized value and nonlinear sRGB encoding.
- `A8B8G8R8UnormPack32 = 50`：8-bit-per-channel unsigned floating-point alpha/red/green/blue channel data format with normalized value, packed in 32 bits.
- `A8B8G8R8SnormPack32 = 51`：8-bit-per-channel signed floating-point alpha/red/green/blue channel data format with normalized value, packed in 32 bits.
- `A8B8G8R8UscaledPack32 = 52`：8-bit-per-channel unsigned floating-point alpha/red/green/blue channel data format with scaled value (value is converted from integer to float), packed in 32 bits.
- `A8B8G8R8SscaledPack32 = 53`：8-bit-per-channel signed floating-point alpha/red/green/blue channel data format with scaled value (value is converted from integer to float), packed in 32 bits.
- `A8B8G8R8UintPack32 = 54`：8-bit-per-channel unsigned integer alpha/red/green/blue channel data format, packed in 32 bits.
- `A8B8G8R8SintPack32 = 55`：8-bit-per-channel signed integer alpha/red/green/blue channel data format, packed in 32 bits.
- `A8B8G8R8SrgbPack32 = 56`：8-bit-per-channel unsigned floating-point alpha/red/green/blue channel data format with normalized value and nonlinear sRGB encoding, packed in 32 bits.
- `A2R10G10B10UnormPack32 = 57`：Unsigned floating-point alpha/red/green/blue channel data format with normalized value, packed in 32 bits.
- `A2R10G10B10SnormPack32 = 58`：Signed floating-point alpha/red/green/blue channel data format with normalized value, packed in 32 bits.
- `A2R10G10B10UscaledPack32 = 59`：Unsigned floating-point alpha/red/green/blue channel data format with normalized value, packed in 32 bits.
- `A2R10G10B10SscaledPack32 = 60`：Signed floating-point alpha/red/green/blue channel data format with normalized value, packed in 32 bits.
- `A2R10G10B10UintPack32 = 61`：Unsigned integer alpha/red/green/blue channel data format with normalized value, packed in 32 bits.
- `A2R10G10B10SintPack32 = 62`：Signed integer alpha/red/green/blue channel data format with normalized value, packed in 32 bits.
- `A2B10G10R10UnormPack32 = 63`：Unsigned floating-point alpha/blue/green/red channel data format with normalized value, packed in 32 bits.
- `A2B10G10R10SnormPack32 = 64`：Signed floating-point alpha/blue/green/red channel data format with normalized value, packed in 32 bits.
- `A2B10G10R10UscaledPack32 = 65`：Unsigned floating-point alpha/blue/green/red channel data format with normalized value, packed in 32 bits.
- `A2B10G10R10SscaledPack32 = 66`：Signed floating-point alpha/blue/green/red channel data format with normalized value, packed in 32 bits.
- `A2B10G10R10UintPack32 = 67`：Unsigned integer alpha/blue/green/red channel data format with normalized value, packed in 32 bits.
- `A2B10G10R10SintPack32 = 68`：Signed integer alpha/blue/green/red channel data format with normalized value, packed in 32 bits.
- `R16Unorm = 69`：16-bit-per-channel unsigned floating-point red channel data format with normalized value.
- `R16Snorm = 70`：16-bit-per-channel signed floating-point red channel data format with normalized value.
- `R16Uscaled = 71`：16-bit-per-channel unsigned floating-point red channel data format with scaled value (value is converted from integer to float).
- `R16Sscaled = 72`：16-bit-per-channel signed floating-point red channel data format with scaled value (value is converted from integer to float).
- `R16Uint = 73`：16-bit-per-channel unsigned integer red channel data format.
- `R16Sint = 74`：16-bit-per-channel signed integer red channel data format.
- `R16Sfloat = 75`：16-bit-per-channel signed floating-point red channel data format with the value stored as-is.
- `R16G16Unorm = 76`：16-bit-per-channel unsigned floating-point red/green channel data format with normalized value.
- `R16G16Snorm = 77`：16-bit-per-channel signed floating-point red/green channel data format with normalized value.
- `R16G16Uscaled = 78`：16-bit-per-channel unsigned floating-point red/green channel data format with scaled value (value is converted from integer to float).
- `R16G16Sscaled = 79`：16-bit-per-channel signed floating-point red/green channel data format with scaled value (value is converted from integer to float).
- `R16G16Uint = 80`：16-bit-per-channel unsigned integer red/green channel data format.
- `R16G16Sint = 81`：16-bit-per-channel signed integer red/green channel data format.
- `R16G16Sfloat = 82`：16-bit-per-channel signed floating-point red/green channel data format with the value stored as-is.
- `R16G16B16Unorm = 83`：16-bit-per-channel unsigned floating-point red/green/blue channel data format with normalized value.
- `R16G16B16Snorm = 84`：16-bit-per-channel signed floating-point red/green/blue channel data format with normalized value.
- `R16G16B16Uscaled = 85`：16-bit-per-channel unsigned floating-point red/green/blue channel data format with scaled value (value is converted from integer to float).
- `R16G16B16Sscaled = 86`：16-bit-per-channel signed floating-point red/green/blue channel data format with scaled value (value is converted from integer to float).
- `R16G16B16Uint = 87`：16-bit-per-channel unsigned integer red/green/blue channel data format.
- `R16G16B16Sint = 88`：16-bit-per-channel signed integer red/green/blue channel data format.
- `R16G16B16Sfloat = 89`：16-bit-per-channel signed floating-point red/green/blue channel data format with the value stored as-is.
- `R16G16B16A16Unorm = 90`：16-bit-per-channel unsigned floating-point red/green/blue/alpha channel data format with normalized value.
- `R16G16B16A16Snorm = 91`：16-bit-per-channel signed floating-point red/green/blue/alpha channel data format with normalized value.
- `R16G16B16A16Uscaled = 92`：16-bit-per-channel unsigned floating-point red/green/blue/alpha channel data format with scaled value (value is converted from integer to float).
- `R16G16B16A16Sscaled = 93`：16-bit-per-channel signed floating-point red/green/blue/alpha channel data format with scaled value (value is converted from integer to float).
- `R16G16B16A16Uint = 94`：16-bit-per-channel unsigned integer red/green/blue/alpha channel data format.
- `R16G16B16A16Sint = 95`：16-bit-per-channel signed integer red/green/blue/alpha channel data format.
- `R16G16B16A16Sfloat = 96`：16-bit-per-channel signed floating-point red/green/blue/alpha channel data format with the value stored as-is.
- `R32Uint = 97`：32-bit-per-channel unsigned integer red channel data format.
- `R32Sint = 98`：32-bit-per-channel signed integer red channel data format.
- `R32Sfloat = 99`：32-bit-per-channel signed floating-point red channel data format with the value stored as-is.
- `R32G32Uint = 100`：32-bit-per-channel unsigned integer red/green channel data format.
- `R32G32Sint = 101`：32-bit-per-channel signed integer red/green channel data format.
- `R32G32Sfloat = 102`：32-bit-per-channel signed floating-point red/green channel data format with the value stored as-is.
- `R32G32B32Uint = 103`：32-bit-per-channel unsigned integer red/green/blue channel data format.
- `R32G32B32Sint = 104`：32-bit-per-channel signed integer red/green/blue channel data format.
- `R32G32B32Sfloat = 105`：32-bit-per-channel signed floating-point red/green/blue channel data format with the value stored as-is.
- `R32G32B32A32Uint = 106`：32-bit-per-channel unsigned integer red/green/blue/alpha channel data format.
- `R32G32B32A32Sint = 107`：32-bit-per-channel signed integer red/green/blue/alpha channel data format.
- `R32G32B32A32Sfloat = 108`：32-bit-per-channel signed floating-point red/green/blue/alpha channel data format with the value stored as-is.
- `R64Uint = 109`：64-bit-per-channel unsigned integer red channel data format.
- `R64Sint = 110`：64-bit-per-channel signed integer red channel data format.
- `R64Sfloat = 111`：64-bit-per-channel signed floating-point red channel data format with the value stored as-is.
- `R64G64Uint = 112`：64-bit-per-channel unsigned integer red/green channel data format.
- `R64G64Sint = 113`：64-bit-per-channel signed integer red/green channel data format.
- `R64G64Sfloat = 114`：64-bit-per-channel signed floating-point red/green channel data format with the value stored as-is.
- `R64G64B64Uint = 115`：64-bit-per-channel unsigned integer red/green/blue channel data format.
- `R64G64B64Sint = 116`：64-bit-per-channel signed integer red/green/blue channel data format.
- `R64G64B64Sfloat = 117`：64-bit-per-channel signed floating-point red/green/blue channel data format with the value stored as-is.
- `R64G64B64A64Uint = 118`：64-bit-per-channel unsigned integer red/green/blue/alpha channel data format.
- `R64G64B64A64Sint = 119`：64-bit-per-channel signed integer red/green/blue/alpha channel data format.
- `R64G64B64A64Sfloat = 120`：64-bit-per-channel signed floating-point red/green/blue/alpha channel data format with the value stored as-is.
- `B10G11R11UfloatPack32 = 121`：Unsigned floating-point blue/green/red data format with the value stored as-is, packed in 32 bits.
- `E5B9G9R9UfloatPack32 = 122`：Unsigned floating-point exposure/blue/green/red data format with the value stored as-is, packed in 32 bits.
- `D16Unorm = 123`：16-bit unsigned floating-point depth data format with normalized value.
- `X8D24UnormPack32 = 124`：24-bit unsigned floating-point depth data format with normalized value, plus 8 unused bits, packed in 32 bits.
- `D32Sfloat = 125`：32-bit signed floating-point depth data format with the value stored as-is.
- `S8Uint = 126`：8-bit unsigned integer stencil data format.
- `D16UnormS8Uint = 127`：16-bit unsigned floating-point depth data format with normalized value, plus 8 bits of stencil in unsigned integer format.
- `D24UnormS8Uint = 128`：24-bit unsigned floating-point depth data format with normalized value, plus 8 bits of stencil in unsigned integer format.
- `D32SfloatS8Uint = 129`：32-bit signed floating-point depth data format with the value stored as-is, plus 8 bits of stencil in unsigned integer format.
- `Bc1RgbUnormBlock = 130`：VRAM-compressed unsigned red/green/blue channel data format with normalized value.
- `Bc1RgbSrgbBlock = 131`：VRAM-compressed unsigned red/green/blue channel data format with normalized value and nonlinear sRGB encoding.
- `Bc1RgbaUnormBlock = 132`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value.
- `Bc1RgbaSrgbBlock = 133`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value and nonlinear sRGB encoding.
- `Bc2UnormBlock = 134`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value.
- `Bc2SrgbBlock = 135`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value and nonlinear sRGB encoding.
- `Bc3UnormBlock = 136`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value.
- `Bc3SrgbBlock = 137`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value and nonlinear sRGB encoding.
- `Bc4UnormBlock = 138`：VRAM-compressed unsigned red channel data format with normalized value.
- `Bc4SnormBlock = 139`：VRAM-compressed signed red channel data format with normalized value.
- `Bc5UnormBlock = 140`：VRAM-compressed unsigned red/green channel data format with normalized value.
- `Bc5SnormBlock = 141`：VRAM-compressed signed red/green channel data format with normalized value.
- `Bc6HUfloatBlock = 142`：VRAM-compressed unsigned red/green/blue channel data format with the floating-point value stored as-is.
- `Bc6HSfloatBlock = 143`：VRAM-compressed signed red/green/blue channel data format with the floating-point value stored as-is.
- `Bc7UnormBlock = 144`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value.
- `Bc7SrgbBlock = 145`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value and nonlinear sRGB encoding.
- `Etc2R8G8B8UnormBlock = 146`：VRAM-compressed unsigned red/green/blue channel data format with normalized value.
- `Etc2R8G8B8SrgbBlock = 147`：VRAM-compressed unsigned red/green/blue channel data format with normalized value and nonlinear sRGB encoding.
- `Etc2R8G8B8A1UnormBlock = 148`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value.
- `Etc2R8G8B8A1SrgbBlock = 149`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value and nonlinear sRGB encoding.
- `Etc2R8G8B8A8UnormBlock = 150`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value.
- `Etc2R8G8B8A8SrgbBlock = 151`：VRAM-compressed unsigned red/green/blue/alpha channel data format with normalized value and nonlinear sRGB encoding.
- `EacR11UnormBlock = 152`：11-bit VRAM-compressed unsigned red channel data format with normalized value.
- `EacR11SnormBlock = 153`：11-bit VRAM-compressed signed red channel data format with normalized value.
- `EacR11G11UnormBlock = 154`：11-bit VRAM-compressed unsigned red/green channel data format with normalized value.
- `EacR11G11SnormBlock = 155`：11-bit VRAM-compressed signed red/green channel data format with normalized value.
- `Astc4X4UnormBlock = 156`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 4×4 blocks (highest quality).
- `Astc4X4SrgbBlock = 157`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 4×4 blocks (highest quality).
- `Astc5X4UnormBlock = 158`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 5×4 blocks.
- `Astc5X4SrgbBlock = 159`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 5×4 blocks.
- `Astc5X5UnormBlock = 160`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 5×5 blocks.
- `Astc5X5SrgbBlock = 161`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 5×5 blocks.
- `Astc6X5UnormBlock = 162`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 6×5 blocks.
- `Astc6X5SrgbBlock = 163`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 6×5 blocks.
- `Astc6X6UnormBlock = 164`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 6×6 blocks.
- `Astc6X6SrgbBlock = 165`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 6×6 blocks.
- `Astc8X5UnormBlock = 166`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 8×5 blocks.
- `Astc8X5SrgbBlock = 167`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 8×5 blocks.
- `Astc8X6UnormBlock = 168`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 8×6 blocks.
- `Astc8X6SrgbBlock = 169`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 8×6 blocks.
- `Astc8X8UnormBlock = 170`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 8×8 blocks.
- `Astc8X8SrgbBlock = 171`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 8×8 blocks.
- `Astc10X5UnormBlock = 172`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 10×5 blocks.
- `Astc10X5SrgbBlock = 173`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 10×5 blocks.
- `Astc10X6UnormBlock = 174`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 10×6 blocks.
- `Astc10X6SrgbBlock = 175`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 10×6 blocks.
- `Astc10X8UnormBlock = 176`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 10×8 blocks.
- `Astc10X8SrgbBlock = 177`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 10×8 blocks.
- `Astc10X10UnormBlock = 178`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 10×10 blocks.
- `Astc10X10SrgbBlock = 179`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 10×10 blocks.
- `Astc12X10UnormBlock = 180`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 12×10 blocks.
- `Astc12X10SrgbBlock = 181`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 12×10 blocks.
- `Astc12X12UnormBlock = 182`：VRAM-compressed unsigned floating-point data format with normalized value, packed in 12 blocks (lowest quality).
- `Astc12X12SrgbBlock = 183`：VRAM-compressed unsigned floating-point data format with normalized value and nonlinear sRGB encoding, packed in 12 blocks (lowest quality).
- `G8B8G8R8422Unorm = 184`：8-bit-per-channel unsigned floating-point green/blue/red channel data format with normalized value.
- `B8G8R8G8422Unorm = 185`：8-bit-per-channel unsigned floating-point blue/green/red channel data format with normalized value.
- `G8B8R83Plane420Unorm = 186`：8-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, stored across 3 separate planes (green + blue + red).
- `G8B8R82Plane420Unorm = 187`：8-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, stored across 2 separate planes (green + blue/red).
- `G8B8R83Plane422Unorm = 188`：8-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, stored across 2 separate planes (green + blue + red).
- `G8B8R82Plane422Unorm = 189`：8-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, stored across 2 separate planes (green + blue/red).
- `G8B8R83Plane444Unorm = 190`：8-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, stored across 3 separate planes.
- `R10X6UnormPack16 = 191`：10-bit-per-channel unsigned floating-point red channel data with normalized value, plus 6 unused bits, packed in 16 bits.
- `R10X6G10X6Unorm2Pack16 = 192`：10-bit-per-channel unsigned floating-point red/green channel data with normalized value, plus 6 unused bits after each channel, packed in 2×16 bits.
- `R10X6G10X6B10X6A10X6Unorm4Pack16 = 193`：10-bit-per-channel unsigned floating-point red/green/blue/alpha channel data with normalized value, plus 6 unused bits after each channel, packed in 4×16 bits.
- `G10X6B10X6G10X6R10X6422Unorm4Pack16 = 194`：10-bit-per-channel unsigned floating-point green/blue/green/red channel data with normalized value, plus 6 unused bits after each channel, packed in 4×16 bits.
- `B10X6G10X6R10X6G10X6422Unorm4Pack16 = 195`：10-bit-per-channel unsigned floating-point blue/green/red/green channel data with normalized value, plus 6 unused bits after each channel, packed in 4×16 bits.
- `G10X6B10X6R10X63Plane420Unorm3Pack16 = 196`：10-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G10X6B10X6R10X62Plane420Unorm3Pack16 = 197`：10-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G10X6B10X6R10X63Plane422Unorm3Pack16 = 198`：10-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G10X6B10X6R10X62Plane422Unorm3Pack16 = 199`：10-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G10X6B10X6R10X63Plane444Unorm3Pack16 = 200`：10-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `R12X4UnormPack16 = 201`：12-bit-per-channel unsigned floating-point red channel data with normalized value, plus 6 unused bits, packed in 16 bits.
- `R12X4G12X4Unorm2Pack16 = 202`：12-bit-per-channel unsigned floating-point red/green channel data with normalized value, plus 6 unused bits after each channel, packed in 2×16 bits.
- `R12X4G12X4B12X4A12X4Unorm4Pack16 = 203`：12-bit-per-channel unsigned floating-point red/green/blue/alpha channel data with normalized value, plus 6 unused bits after each channel, packed in 4×16 bits.
- `G12X4B12X4G12X4R12X4422Unorm4Pack16 = 204`：12-bit-per-channel unsigned floating-point green/blue/green/red channel data with normalized value, plus 6 unused bits after each channel, packed in 4×16 bits.
- `B12X4G12X4R12X4G12X4422Unorm4Pack16 = 205`：12-bit-per-channel unsigned floating-point blue/green/red/green channel data with normalized value, plus 6 unused bits after each channel, packed in 4×16 bits.
- `G12X4B12X4R12X43Plane420Unorm3Pack16 = 206`：12-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G12X4B12X4R12X42Plane420Unorm3Pack16 = 207`：12-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G12X4B12X4R12X43Plane422Unorm3Pack16 = 208`：12-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G12X4B12X4R12X42Plane422Unorm3Pack16 = 209`：12-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G12X4B12X4R12X43Plane444Unorm3Pack16 = 210`：12-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G16B16G16R16422Unorm = 211`：16-bit-per-channel unsigned floating-point green/blue/red channel data format with normalized value.
- `B16G16R16G16422Unorm = 212`：16-bit-per-channel unsigned floating-point blue/green/red channel data format with normalized value.
- `G16B16R163Plane420Unorm = 213`：16-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G16B16R162Plane420Unorm = 214`：16-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G16B16R163Plane422Unorm = 215`：16-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G16B16R162Plane422Unorm = 216`：16-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `G16B16R163Plane444Unorm = 217`：16-bit-per-channel unsigned floating-point green/blue/red channel data with normalized value, plus 6 unused bits after each channel.
- `Astc4X4SfloatBlock = 218`
- `Astc5X4SfloatBlock = 219`
- `Astc5X5SfloatBlock = 220`
- `Astc6X5SfloatBlock = 221`
- `Astc6X6SfloatBlock = 222`
- `Astc8X5SfloatBlock = 223`
- `Astc8X6SfloatBlock = 224`
- `Astc8X8SfloatBlock = 225`
- `Astc10X5SfloatBlock = 226`
- `Astc10X6SfloatBlock = 227`
- `Astc10X8SfloatBlock = 228`
- `Astc10X10SfloatBlock = 229`
- `Astc12X10SfloatBlock = 230`
- `Astc12X12SfloatBlock = 231`
- `Max = 232`：Represents the size of the `RenderingDevice.DataFormat` enum.

## DeviceType

命名空间 `Godot`；enum

### 枚举值

- `Other = 0`：Rendering device type does not match any of the other enum values or is unknown.
- `IntegratedGpu = 1`：Rendering device is an integrated GPU, which is typically *(but not always)* slower than dedicated GPUs (`DiscreteGpu`).
- `DiscreteGpu = 2`：Rendering device is a dedicated GPU, which is typically *(but not always)* faster than integrated GPUs (`IntegratedGpu`).
- `VirtualGpu = 3`：Rendering device is an emulated GPU in a virtual environment.
- `Cpu = 4`：Rendering device is provided by software emulation (such as Lavapipe or [SwiftShader](https://github.com/google/swiftshader)).
- `Max = 5`：Represents the size of the `RenderingDevice.DeviceType` enum.

## DrawFlags

命名空间 `Godot`；enum

### 枚举值

- `DefaultAll = 0`：Do not clear or ignore any attachments.
- `ClearColor0 = 1`：Clear the first color attachment.
- `ClearColor1 = 2`：Clear the second color attachment.
- `ClearColor2 = 4`：Clear the third color attachment.
- `ClearColor3 = 8`：Clear the fourth color attachment.
- `ClearColor4 = 16`：Clear the fifth color attachment.
- `ClearColor5 = 32`：Clear the sixth color attachment.
- `ClearColor6 = 64`：Clear the seventh color attachment.
- `ClearColor7 = 128`：Clear the eighth color attachment.
- `ClearColorMask = 255`：Mask for clearing all color attachments.
- `ClearColorAll = 255`：Clear all color attachments.
- `IgnoreColor0 = 256`：Ignore the previous contents of the first color attachment.
- `IgnoreColor1 = 512`：Ignore the previous contents of the second color attachment.
- `IgnoreColor2 = 1024`：Ignore the previous contents of the third color attachment.
- `IgnoreColor3 = 2048`：Ignore the previous contents of the fourth color attachment.
- `IgnoreColor4 = 4096`：Ignore the previous contents of the fifth color attachment.
- `IgnoreColor5 = 8192`：Ignore the previous contents of the sixth color attachment.
- `IgnoreColor6 = 16384`：Ignore the previous contents of the seventh color attachment.
- `IgnoreColor7 = 32768`：Ignore the previous contents of the eighth color attachment.
- `IgnoreColorMask = 65280`：Mask for ignoring all the previous contents of the color attachments.
- `IgnoreColorAll = 65280`：Ignore the previous contents of all color attachments.
- `ClearDepth = 65536`：Clear the depth attachment.
- `IgnoreDepth = 131072`：Ignore the previous contents of the depth attachment.
- `ClearStencil = 262144`：Clear the stencil attachment.
- `IgnoreStencil = 524288`：Ignore the previous contents of the stencil attachment.
- `ClearAll = 327935`：Clear all attachments.
- `IgnoreAll = 720640`：Ignore the previous contents of all attachments.

## DriverResource

命名空间 `Godot`；enum

### 枚举值

- `LogicalDevice = 0`：Specific device object based on a physical device (`rid` parameter is ignored).
- `PhysicalDevice = 1`：Physical device the specific logical device is based on (`rid` parameter is ignored).
- `TopmostObject = 2`：Top-most graphics API entry object (`rid` parameter is ignored).
- `CommandQueue = 3`：The main graphics-compute command queue (`rid` parameter is ignored).
- `QueueFamily = 4`：The specific family the main queue belongs to (`rid` parameter is ignored).
- `Texture = 5`：- Vulkan: `VkImage`.
- `TextureView = 6`：The view of an owned or shared texture.
- `TextureDataFormat = 7`：The native id of the data format of the texture.
- `Sampler = 8`：- Vulkan: `VkSampler`.
- `UniformSet = 9`：- Vulkan: `VkDescriptorSet`.
- `Buffer = 10`：Buffer of any kind of (storage, vertex, etc.).
- `ComputePipeline = 11`：- Vulkan: `VkPipeline`.
- `RenderPipeline = 12`：- Vulkan: `VkPipeline`.
- `VulkanDevice = 0`
- `VulkanPhysicalDevice = 1`
- `VulkanInstance = 2`
- `VulkanQueue = 3`
- `VulkanQueueFamilyIndex = 4`
- `VulkanImage = 5`
- `VulkanImageView = 6`
- `VulkanImageNativeTextureFormat = 7`
- `VulkanSampler = 8`
- `VulkanDescriptorSet = 9`
- `VulkanBuffer = 10`
- `VulkanComputePipeline = 11`
- `VulkanRenderPipeline = 12`

## Features

命名空间 `Godot`；enum

### 枚举值

- `MetalfxSpatial = 3`：Support for MetalFX spatial upscaling.
- `MetalfxTemporal = 4`：Support for MetalFX temporal upscaling.
- `BufferDeviceAddress = 6`：Features support for buffer device address extension.
- `ImageAtomic32Bit = 7`：Support for 32-bit image atomic operations.
- `RayQuery = 11`：Support for ray query extension.
- `RaytracingPipeline = 12`：Support for raytracing pipeline extension.
- `HdrOutput = 13`：Support for high dynamic range (HDR) output.

## FinalAction

命名空间 `Godot`；enum

### 枚举值

- `Store = 0`：Store the result of the draw list in the framebuffer.
- `Discard = 1`：Discard the contents of the framebuffer.
- `Max = 2`：Represents the size of the `RenderingDevice.FinalAction` enum.
- `Read = 0`
- `Continue = 0`

## IndexBufferFormat

命名空间 `Godot`；enum

### 枚举值

- `Uint16 = 0`：Index buffer in 16-bit unsigned integer format.
- `Uint32 = 1`：Index buffer in 32-bit unsigned integer format.

## InitialAction

命名空间 `Godot`；enum

### 枚举值

- `Load = 0`：Load the previous contents of the framebuffer.
- `Clear = 1`：Clear the whole framebuffer or its specified region.
- `Discard = 2`：Ignore the previous contents of the framebuffer.
- `Max = 3`：Represents the size of the `RenderingDevice.InitialAction` enum.
- `ClearRegion = 1`
- `ClearRegionContinue = 1`
- `Keep = 0`
- `Drop = 2`
- `Continue = 0`

## Limit

命名空间 `Godot`；enum

### 枚举值

- `MaxBoundUniformSets = 0`：Maximum number of uniform sets that can be bound at a given time.
- `MaxFramebufferColorAttachments = 1`：Maximum number of color framebuffer attachments that can be used at a given time.
- `MaxTexturesPerUniformSet = 2`：Maximum number of textures that can be used per uniform set.
- `MaxSamplersPerUniformSet = 3`：Maximum number of samplers that can be used per uniform set.
- `MaxStorageBuffersPerUniformSet = 4`：Maximum number of [storage buffers](https://vkguide.dev/docs/chapter-4/storage_buffers/) per uniform set.
- `MaxStorageImagesPerUniformSet = 5`：Maximum number of storage images per uniform set.
- `MaxUniformBuffersPerUniformSet = 6`：Maximum number of uniform buffers per uniform set.
- `MaxDrawIndexedIndex = 7`：Maximum index for an indexed draw command.
- `MaxFramebufferHeight = 8`：Maximum height of a framebuffer (in pixels).
- `MaxFramebufferWidth = 9`：Maximum width of a framebuffer (in pixels).
- `MaxTextureArrayLayers = 10`：Maximum number of texture array layers.
- `MaxTextureSize1D = 11`：Maximum supported 1-dimensional texture size (in pixels on a single axis).
- `MaxTextureSize2D = 12`：Maximum supported 2-dimensional texture size (in pixels on a single axis).
- `MaxTextureSize3D = 13`：Maximum supported 3-dimensional texture size (in pixels on a single axis).
- `MaxTextureSizeCube = 14`：Maximum supported cubemap texture size (in pixels on a single axis of a single face).
- `MaxTexturesPerShaderStage = 15`：Maximum number of textures per shader stage.
- `MaxSamplersPerShaderStage = 16`：Maximum number of samplers per shader stage.
- `MaxStorageBuffersPerShaderStage = 17`：Maximum number of [storage buffers](https://vkguide.dev/docs/chapter-4/storage_buffers/) per shader stage.
- `MaxStorageImagesPerShaderStage = 18`：Maximum number of storage images per shader stage.
- `MaxUniformBuffersPerShaderStage = 19`：Maximum number of uniform buffers per uniform set.
- `MaxPushConstantSize = 20`：Maximum size of a push constant.
- `MaxUniformBufferSize = 21`：Maximum size of a uniform buffer.
- `MaxVertexInputAttributeOffset = 22`：Maximum vertex input attribute offset.
- `MaxVertexInputAttributes = 23`：Maximum number of vertex input attributes.
- `MaxVertexInputBindings = 24`：Maximum number of vertex input bindings.
- `MaxVertexInputBindingStride = 25`：Maximum vertex input binding stride.
- `MinUniformBufferOffsetAlignment = 26`：Minimum uniform buffer offset alignment.
- `MaxComputeSharedMemorySize = 27`：Maximum shared memory size for compute shaders.
- `MaxComputeWorkgroupCountX = 28`：Maximum number of workgroups for compute shaders on the X axis.
- `MaxComputeWorkgroupCountY = 29`：Maximum number of workgroups for compute shaders on the Y axis.
- `MaxComputeWorkgroupCountZ = 30`：Maximum number of workgroups for compute shaders on the Z axis.
- `MaxComputeWorkgroupInvocations = 31`：Maximum number of workgroup invocations for compute shaders.
- `MaxComputeWorkgroupSizeX = 32`：Maximum workgroup size for compute shaders on the X axis.
- `MaxComputeWorkgroupSizeY = 33`：Maximum workgroup size for compute shaders on the Y axis.
- `MaxComputeWorkgroupSizeZ = 34`：Maximum workgroup size for compute shaders on the Z axis.
- `MaxViewportDimensionsX = 35`：Maximum viewport width (in pixels).
- `MaxViewportDimensionsY = 36`：Maximum viewport height (in pixels).
- `MetalfxTemporalScalerMinScale = 46`：Returns the smallest value for `ProjectSettings.rendering/scaling_3d/scale` when using the MetalFX temporal upscaler.
- `MetalfxTemporalScalerMaxScale = 47`：Returns the largest value for `ProjectSettings.rendering/scaling_3d/scale` when using the MetalFX temporal upscaler.

## LogicOperation

命名空间 `Godot`；enum

### 枚举值

- `Clear = 0`：Clear logic operation (result is always `0`).
- `And = 1`：AND logic operation.
- `AndReverse = 2`：AND logic operation with the *destination* operand being inverted.
- `Copy = 3`：Copy logic operation (keeps the *source* value as-is).
- `AndInverted = 4`：AND logic operation with the *source* operand being inverted.
- `NoOp = 5`：No-op logic operation (keeps the *destination* value as-is).
- `Xor = 6`：Exclusive or (XOR) logic operation.
- `Or = 7`：OR logic operation.
- `Nor = 8`：Not-OR (NOR) logic operation.
- `Equivalent = 9`：Not-XOR (XNOR) logic operation.
- `Invert = 10`：Invert logic operation.
- `OrReverse = 11`：OR logic operation with the *destination* operand being inverted.
- `CopyInverted = 12`：NOT logic operation (inverts the value).
- `OrInverted = 13`：OR logic operation with the *source* operand being inverted.
- `Nand = 14`：Not-AND (NAND) logic operation.
- `Set = 15`：SET logic operation (result is always `1`).
- `Max = 16`：Represents the size of the `RenderingDevice.LogicOperation` enum.

## MemoryType

命名空间 `Godot`；enum

### 枚举值

- `Textures = 0`：Memory taken by textures.
- `Buffers = 1`：Memory taken by buffers.
- `Total = 2`：Total memory taken.

## PipelineDynamicStateFlags

命名空间 `Godot`；enum

### 枚举值

- `LineWidth = 1`：Allows dynamically changing the width of rendering lines.
- `DepthBias = 2`：Allows dynamically changing the depth bias.
- `BlendConstants = 4`
- `DepthBounds = 8`
- `StencilCompareMask = 16`
- `StencilWriteMask = 32`
- `StencilReference = 64`

## PipelineSpecializationConstantType

命名空间 `Godot`；enum

### 枚举值

- `Bool = 0`：Boolean specialization constant.
- `Int = 1`：Integer specialization constant.
- `Float = 2`：Floating-point specialization constant.

## PolygonCullMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not use polygon front face or backface culling.
- `Front = 1`：Use polygon frontface culling (faces pointing towards the camera are hidden).
- `Back = 2`：Use polygon backface culling (faces pointing away from the camera are hidden).

## PolygonFrontFace

命名空间 `Godot`；enum

### 枚举值

- `Clockwise = 0`：Clockwise winding order to determine which face of a polygon is its front face.
- `CounterClockwise = 1`：Counter-clockwise winding order to determine which face of a polygon is its front face.

## RenderPrimitive

命名空间 `Godot`；enum

### 枚举值

- `Points = 0`：Point rendering primitive (with constant size, regardless of distance from camera).
- `Lines = 1`：Line list rendering primitive.
- `LinesWithAdjacency = 2`：[Line list rendering primitive with adjacency.](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#drawing-line-lists-with-adjacency)
- `Linestrips = 3`：Line strip rendering primitive.
- `LinestripsWithAdjacency = 4`：[Line strip rendering primitive with adjacency.](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#drawing-line-strips-with-adjacency)
- `Triangles = 5`：Triangle list rendering primitive.
- `TrianglesWithAdjacency = 6`：[Triangle list rendering primitive with adjacency.](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#drawing-triangle-lists-with-adjacency)
- `TriangleStrips = 7`：Triangle strip rendering primitive.
- `TriangleStripsWithAjacency = 8`：[Triangle strip rendering primitive with adjacency.](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#drawing-triangle-strips-with-adjacency)
- `TriangleStripsWithRestartIndex = 9`：Triangle strip rendering primitive with *primitive restart* enabled.
- `TesselationPatch = 10`：Tessellation patch rendering primitive.
- `Max = 11`：Represents the size of the `RenderingDevice.RenderPrimitive` enum.

## SamplerBorderColor

命名空间 `Godot`；enum

### 枚举值

- `FloatTransparentBlack = 0`：Return a floating-point transparent black color when sampling outside the `[0.0, 1.0]` range.
- `IntTransparentBlack = 1`：Return an integer transparent black color when sampling outside the `[0.0, 1.0]` range.
- `FloatOpaqueBlack = 2`：Return a floating-point opaque black color when sampling outside the `[0.0, 1.0]` range.
- `IntOpaqueBlack = 3`：Return an integer opaque black color when sampling outside the `[0.0, 1.0]` range.
- `FloatOpaqueWhite = 4`：Return a floating-point opaque white color when sampling outside the `[0.0, 1.0]` range.
- `IntOpaqueWhite = 5`：Return an integer opaque white color when sampling outside the `[0.0, 1.0]` range.
- `Max = 6`：Represents the size of the `RenderingDevice.SamplerBorderColor` enum.

## SamplerFilter

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：Nearest-neighbor sampler filtering.
- `Linear = 1`：Bilinear sampler filtering.

## SamplerRepeatMode

命名空间 `Godot`；enum

### 枚举值

- `Repeat = 0`：Sample with repeating enabled.
- `MirroredRepeat = 1`：Sample with mirrored repeating enabled.
- `ClampToEdge = 2`：Sample with repeating disabled.
- `ClampToBorder = 3`：Sample with repeating disabled.
- `MirrorClampToEdge = 4`：Sample with mirrored repeating enabled, but only once.
- `Max = 5`：Represents the size of the `RenderingDevice.SamplerRepeatMode` enum.

## ShaderLanguage

命名空间 `Godot`；enum

### 枚举值

- `Glsl = 0`：Khronos' GLSL shading language (used natively by OpenGL and Vulkan).
- `Hlsl = 1`：Microsoft's High-Level Shading Language (used natively by Direct3D, but can also be used in Vulkan).

## ShaderStage

命名空间 `Godot`；enum

### 枚举值

- `Vertex = 0`：Vertex shader stage.
- `Fragment = 1`：Fragment shader stage (called "pixel shader" in Direct3D).
- `TesselationControl = 2`：Tessellation control shader stage.
- `TesselationEvaluation = 3`：Tessellation evaluation shader stage.
- `Compute = 4`：Compute shader stage.
- `Raygen = 5`：Ray generation shader stage.
- `AnyHit = 6`：Any hit shader stage.
- `ClosestHit = 7`：Closest hit shader stage.
- `Miss = 8`：Miss shader stage.
- `Intersection = 9`：Intersection shader stage.
- `Max = 10`：Represents the size of the `RenderingDevice.ShaderStage` enum.
- `VertexBit = 1`：Vertex shader stage bit (see also `Vertex`).
- `FragmentBit = 2`：Fragment shader stage bit (see also `Fragment`).
- `TesselationControlBit = 4`：Tessellation control shader stage bit (see also `TesselationControl`).
- `TesselationEvaluationBit = 8`：Tessellation evaluation shader stage bit (see also `TesselationEvaluation`).
- `ComputeBit = 16`：Compute shader stage bit (see also `Compute`).
- `RaygenBit = 32`：Ray generation shader stage bit (see also `Raygen`).
- `AnyHitBit = 64`：Any hit shader stage bit (see also `AnyHit`).
- `ClosestHitBit = 128`：Closest hit shader stage bit (see also `ClosestHit`).
- `MissBit = 256`：Miss shader stage bit (see also `Miss`).
- `IntersectionBit = 512`：Intersection shader stage bit (see also `Intersection`).

## StencilOperation

命名空间 `Godot`；enum

### 枚举值

- `Keep = 0`：Keep the current stencil value.
- `Zero = 1`：Set the stencil value to `0`.
- `Replace = 2`：Replace the existing stencil value with the new one.
- `IncrementAndClamp = 3`：Increment the existing stencil value and clamp to the maximum representable unsigned value if reached.
- `DecrementAndClamp = 4`：Decrement the existing stencil value and clamp to the minimum value if reached.
- `Invert = 5`：Bitwise-invert the existing stencil value.
- `IncrementAndWrap = 6`：Increment the stencil value and wrap around to `0` if reaching the maximum representable unsigned.
- `DecrementAndWrap = 7`：Decrement the stencil value and wrap around to the maximum representable unsigned if reaching the minimum.
- `Max = 8`：Represents the size of the `RenderingDevice.StencilOperation` enum.

## StorageBufferUsage

命名空间 `Godot`；enum

### 枚举值

- `Indirect = 1`

## TextureSamples

命名空间 `Godot`；enum

### 枚举值

- `Samples1 = 0`：Perform 1 texture sample (this is the fastest but lowest-quality for antialiasing).
- `Samples2 = 1`：Perform 2 texture samples.
- `Samples4 = 2`：Perform 4 texture samples.
- `Samples8 = 3`：Perform 8 texture samples.
- `Samples16 = 4`：Perform 16 texture samples.
- `Samples32 = 5`：Perform 32 texture samples.
- `Samples64 = 6`：Perform 64 texture samples (this is the slowest but highest-quality for antialiasing).
- `Max = 7`：Represents the size of the `RenderingDevice.TextureSamples` enum.

## TextureSliceType

命名空间 `Godot`；enum

### 枚举值

- `Slice2D = 0`：2-dimensional texture slice.
- `Cubemap = 1`：Cubemap texture slice.
- `Slice3D = 2`：3-dimensional texture slice.

## TextureSwizzle

命名空间 `Godot`；enum

### 枚举值

- `Identity = 0`：Return the sampled value as-is.
- `Zero = 1`：Always return `0.0` when sampling.
- `One = 2`：Always return `1.0` when sampling.
- `R = 3`：Sample the red color channel.
- `G = 4`：Sample the green color channel.
- `B = 5`：Sample the blue color channel.
- `A = 6`：Sample the alpha channel.
- `Max = 7`：Represents the size of the `RenderingDevice.TextureSwizzle` enum.

## TextureType

命名空间 `Godot`；enum

### 枚举值

- `Type1D = 0`：1-dimensional texture.
- `Type2D = 1`：2-dimensional texture.
- `Type3D = 2`：3-dimensional texture.
- `Cube = 3`：[`Cubemap`](Cubemap.md) texture.
- `Type1DArray = 4`：Array of 1-dimensional textures.
- `Type2DArray = 5`：Array of 2-dimensional textures.
- `CubeArray = 6`：Array of [`Cubemap`](Cubemap.md) textures.
- `Max = 7`：Represents the size of the `RenderingDevice.TextureType` enum.

## TextureUsageBits

命名空间 `Godot`；enum

### 枚举值

- `SamplingBit = 1`：Texture can be sampled.
- `ColorAttachmentBit = 2`：Texture can be used as a color attachment in a framebuffer.
- `DepthStencilAttachmentBit = 4`：Texture can be used as a depth/stencil attachment in a framebuffer.
- `DepthResolveAttachmentBit = 4096`：Texture can be used as a depth/stencil resolve attachment in a framebuffer.
- `StorageBit = 8`：Texture can be used as a [storage image](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#descriptorsets-storageimage).
- `StorageAtomicBit = 16`：Texture can be used as a [storage image](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#descriptorsets-storageimage) with support for atomic operations.
- `CpuReadBit = 32`：Texture can be read back on the CPU using [`RenderingDevice.TextureGetData`](RenderingDevice.md) faster than without this bit, since it is always kept in the system memory.
- `CanUpdateBit = 64`：Texture can be updated using [`RenderingDevice.TextureUpdate`](RenderingDevice.md).
- `CanCopyFromBit = 128`：Texture can be a source for [`RenderingDevice.TextureCopy`](RenderingDevice.md).
- `CanCopyToBit = 256`：Texture can be a destination for [`RenderingDevice.TextureCopy`](RenderingDevice.md).
- `InputAttachmentBit = 512`：Texture can be used as a [input attachment](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#descriptorsets-inputattachment) in a framebuffer.

## UniformType

命名空间 `Godot`；enum

### 枚举值

- `Sampler = 0`：Sampler uniform.
- `SamplerWithTexture = 1`：Sampler uniform with a texture.
- `Texture = 2`：Texture uniform.
- `Image = 3`：Image uniform.
- `TextureBuffer = 4`：Texture buffer uniform.
- `SamplerWithTextureBuffer = 5`：Sampler uniform with a texture buffer.
- `ImageBuffer = 6`：Image buffer uniform.
- `UniformBuffer = 7`：Uniform buffer uniform.
- `StorageBuffer = 8`：[Storage buffer](https://vkguide.dev/docs/chapter-4/storage_buffers/) uniform.
- `InputAttachment = 9`：Input attachment uniform.
- `UniformBufferDynamic = 10`：Same as UNIFORM_TYPE_UNIFORM_BUFFER but for buffers created with BUFFER_CREATION_DYNAMIC_PERSISTENT_BIT.
- `StorageBufferDynamic = 11`：Same as UNIFORM_TYPE_STORAGE_BUFFER but for buffers created with BUFFER_CREATION_DYNAMIC_PERSISTENT_BIT.
- `AccelerationStructure = 12`：Acceleration structure uniform.
- `Max = 13`：Represents the size of the `RenderingDevice.UniformType` enum.

## VertexFrequency

命名空间 `Godot`；enum

### 枚举值

- `Vertex = 0`：Vertex attribute addressing is a function of the vertex.
- `Instance = 1`：Vertex attribute addressing is a function of the instance index.
