<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRApiExtension

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`OpenXRApiExtension` makes OpenXR available for GDExtension. It provides the OpenXR API to GDExtension through the `GetInstanceProcAddr()` method, and the OpenXR instance through `GetInstance()`.

It also provides methods for querying the status of OpenXR initialization, and helper methods for ease of use of the API with GDExtension.

## 构造函数

### OpenXRApiExtension

```csharp
public OpenXRApiExtension()
```

## 方法

### ActionGetHandle

```csharp
public ulong ActionGetHandle(Rid action)
```

Returns the corresponding `XrAction` OpenXR handle for the given action RID.

### BeginDebugLabelRegion

```csharp
public void BeginDebugLabelRegion(string labelName)
```

Begins a new debug label region, this label will be reported in debug messages for any calls following this until `EndDebugLabelRegion()` is called. Debug labels can be stacked.

### CanRender

```csharp
public bool CanRender()
```

Returns `true` if OpenXR is initialized for rendering with an XR viewport.

### EndDebugLabelRegion

```csharp
public void EndDebugLabelRegion()
```

Marks the end of a debug label region. Removes the latest debug label region added by calling `BeginDebugLabelRegion()`.

### FindAction

```csharp
public Rid FindAction(string name, Rid actionSet)
```

Returns the [`Rid`](Rid.md) corresponding to an `Action` of a matching name, optionally limited to a specified action set.

### GetErrorString

```csharp
public string GetErrorString(ulong result)
```

Returns an error string for the given [XrResult](https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrResult.html).

### GetHandTracker

```csharp
public ulong GetHandTracker(int handIndex)
```

Returns the corresponding `XRHandTrackerEXT` handle for the given hand index value.

### GetInstance

```csharp
public ulong GetInstance()
```

Returns the [XrInstance](https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrInstance.html) created during the initialization of the OpenXR API.

### GetInstanceProcAddr

```csharp
public ulong GetInstanceProcAddr(string name)
```

Returns the function pointer of the OpenXR function with the specified name, cast to an integer. If the function with the given name does not exist, the method returns `0`.

**Note:** `openxr/util.h` contains utility macros for acquiring OpenXR functions, e.g. `GDEXTENSION_INIT_XR_FUNC_V(xrCreateAction)`.

### GetNextFrameTime

```csharp
public long GetNextFrameTime()
```

Returns the predicted display timing for the next frame.

### GetOpenxrVersion

```csharp
public ulong GetOpenxrVersion()
```

Returns the version of OpenXR that was initialized. Only valid after the OpenXR instance has been created. See [XR_MAKE_VERSION](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_MAKE_VERSION) for how the version is calculated.

### GetPlaySpace

```csharp
public ulong GetPlaySpace()
```

Returns the play space, which is an [XrSpace](https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrSpace.html) cast to an integer.

### GetPredictedDisplayTime

```csharp
public long GetPredictedDisplayTime()
```

Returns the predicted display timing for the current frame.

### GetProjectionLayer

```csharp
public ulong GetProjectionLayer()
```

Returns a pointer to the render state's `XrCompositionLayerProjection` struct.

**Note:** This method should only be called from the rendering thread.

### GetRenderStateZFar

```csharp
public double GetRenderStateZFar()
```

Returns the far boundary value of the camera frustum.

**Note:** This is only accessible in the render thread.

### GetRenderStateZNear

```csharp
public double GetRenderStateZNear()
```

Returns the near boundary value of the camera frustum.

**Note:** This is only accessible in the render thread.

### GetSession

```csharp
public ulong GetSession()
```

Returns the OpenXR session, which is an [XrSession](https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrSession.html) cast to an integer.

### GetSupportedSwapchainFormats

```csharp
public long[] GetSupportedSwapchainFormats()
```

Returns an array of supported swapchain formats.

### GetSwapchainFormatName

```csharp
public string GetSwapchainFormatName(long swapchainFormat)
```

Returns the name of the specified swapchain format.

### GetSystemId

```csharp
public ulong GetSystemId()
```

Returns the ID of the system, which is an [XrSystemId](https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrSystemId.html) cast to an integer.

### GetViewConfiguration

```csharp
public ulong GetViewConfiguration()
```

Returns the view configuration type, which is an [XrViewConfigurationType](https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrViewConfigurationType.html) cast to an integer.

### GetViewCount

```csharp
public uint GetViewCount()
```

Returns the number of views. It is usually two, one for each eye, but may differ with different view configurations.

### InsertDebugLabel

```csharp
public void InsertDebugLabel(string labelName)
```

Inserts a debug label, this label is reported in any debug message resulting from the OpenXR calls that follows, until any of `BeginDebugLabelRegion()`, `EndDebugLabelRegion()`, or `InsertDebugLabel()` is called.

### IsEnvironmentBlendModeAlphaSupported

```csharp
public OpenXRApiExtension.OpenXRAlphaBlendModeSupport IsEnvironmentBlendModeAlphaSupported()
```

Returns [`OpenXRApiExtension.OpenXRAlphaBlendModeSupport`](OpenXRApiExtension.md) denoting if [`XRInterface.EnvironmentBlendModeEnum.AlphaBlend`](XRInterface.md) is really supported, emulated or not supported at all.

### IsInitialized

```csharp
public bool IsInitialized()
```

Returns `true` if OpenXR is initialized.

### IsRunning

```csharp
public bool IsRunning()
```

Returns `true` if OpenXR is running ([xrBeginSession](https://registry.khronos.org/OpenXR/specs/1.0/man/html/xrBeginSession.html) was successfully called and the swapchains were created).

### OpenxrIsEnabled

```csharp
public static bool OpenxrIsEnabled(bool checkRunInEditor)
```

Returns `true` if OpenXR is enabled.

### OpenxrSwapchainAcquire

```csharp
public void OpenxrSwapchainAcquire(ulong swapchain)
```

Acquires the image of the provided swapchain.

### OpenxrSwapchainCreate

```csharp
public ulong OpenxrSwapchainCreate(ulong createFlags, ulong usageFlags, long swapchainFormat, uint width, uint height, uint sampleCount, uint arraySize)
```

Returns a pointer to a new swapchain created using the provided parameters.

### OpenxrSwapchainFree

```csharp
public void OpenxrSwapchainFree(ulong swapchain)
```

Destroys the provided swapchain and frees it from memory.

### OpenxrSwapchainGetImage

```csharp
public Rid OpenxrSwapchainGetImage(ulong swapchain)
```

Returns the RID of the provided swapchain's image.

### OpenxrSwapchainGetSwapchain

```csharp
public ulong OpenxrSwapchainGetSwapchain(ulong swapchain)
```

Returns the `XrSwapchain` handle of the provided swapchain.

### OpenxrSwapchainRelease

```csharp
public void OpenxrSwapchainRelease(ulong swapchain)
```

Releases the image of the provided swapchain.

### RegisterCompositionLayerProvider

```csharp
public void RegisterCompositionLayerProvider(OpenXRExtensionWrapper extension)
```

Registers the given extension as a composition layer provider.

**Note:** This cannot be called after the OpenXR session has started. However, it can be called in [`OpenXRExtensionWrapper._OnSessionCreated()`](OpenXRExtensionWrapper.md).

### RegisterCompositionLayerProvider

```csharp
public void RegisterCompositionLayerProvider(OpenXRExtensionWrapperExtension extension)
```

Registers the given extension as a composition layer provider.

**Note:** This cannot be called after the OpenXR session has started. However, it can be called in [`OpenXRExtensionWrapper._OnSessionCreated()`](OpenXRExtensionWrapper.md).

### RegisterFrameInfoExtension

```csharp
public void RegisterFrameInfoExtension(OpenXRExtensionWrapper extension)
```

Registers the given extension as modifying frame info via the `OpenXRExtensionWrapper._set_frame_wait_info_and_get_next_pointer`, `OpenXRExtensionWrapper._set_view_locate_info_and_get_next_pointer`, or `OpenXRExtensionWrapper._set_frame_end_info_and_get_next_pointer` virtual methods.

**Note:** This cannot be called after the OpenXR session has started. However, it can be called in [`OpenXRExtensionWrapper._OnSessionCreated()`](OpenXRExtensionWrapper.md).

### RegisterProjectionLayerExtension

```csharp
public void RegisterProjectionLayerExtension(OpenXRExtensionWrapper extension)
```

Registers the given extension as modifying `XrCompositionLayerProjection` via the `OpenXRExtensionWrapper._set_projection_layer_and_get_next_pointer` virtual method.

**Note:** This cannot be called after the OpenXR session has started. However, it can be called in [`OpenXRExtensionWrapper._OnSessionCreated()`](OpenXRExtensionWrapper.md).

### RegisterProjectionViewsExtension

```csharp
public void RegisterProjectionViewsExtension(OpenXRExtensionWrapper extension)
```

Registers the given extension as a provider of additional data structures to projections views.

**Note:** This cannot be called after the OpenXR session has started. However, it can be called in [`OpenXRExtensionWrapper._OnSessionCreated()`](OpenXRExtensionWrapper.md).

### RegisterProjectionViewsExtension

```csharp
public void RegisterProjectionViewsExtension(OpenXRExtensionWrapperExtension extension)
```

Registers the given extension as a provider of additional data structures to projections views.

**Note:** This cannot be called after the OpenXR session has started. However, it can be called in [`OpenXRExtensionWrapper._OnSessionCreated()`](OpenXRExtensionWrapper.md).

### SetEmulateEnvironmentBlendModeAlphaBlend

```csharp
public void SetEmulateEnvironmentBlendModeAlphaBlend(bool enabled)
```

If set to `true`, an OpenXR extension is loaded which is capable of emulating the [`XRInterface.EnvironmentBlendModeEnum.AlphaBlend`](XRInterface.md) blend mode.

### SetObjectName

```csharp
public void SetObjectName(long objectType, ulong objectHandle, string objectName)
```

Set the object name of an OpenXR object, used for debug output. `objectType` must be a valid OpenXR `XrObjectType` enum and `objectHandle` must be a valid OpenXR object handle.

### SetRenderRegion

```csharp
public void SetRenderRegion(Rect2I renderRegion)
```

Sets the render region to `renderRegion`, overriding the normal render target's rect.

### SetVelocityDepthTexture

```csharp
public void SetVelocityDepthTexture(Rid renderTarget)
```

Sets the render target of the velocity depth texture.

### SetVelocityTargetSize

```csharp
public void SetVelocityTargetSize(Vector2I targetSize)
```

Sets the target size of the velocity and velocity depth textures.

### SetVelocityTexture

```csharp
public void SetVelocityTexture(Rid renderTarget)
```

Sets the render target of the velocity texture.

### UnregisterCompositionLayerProvider

```csharp
public void UnregisterCompositionLayerProvider(OpenXRExtensionWrapper extension)
```

Unregisters the given extension as a composition layer provider.

**Note:** This cannot be called while the OpenXR session is still running.

### UnregisterCompositionLayerProvider

```csharp
public void UnregisterCompositionLayerProvider(OpenXRExtensionWrapperExtension extension)
```

Unregisters the given extension as a composition layer provider.

**Note:** This cannot be called while the OpenXR session is still running.

### UnregisterFrameInfoExtension

```csharp
public void UnregisterFrameInfoExtension(OpenXRExtensionWrapper extension)
```

Unregisters the given extension as modifying frame info.

**Note:** This cannot be called while the OpenXR session is still running.

### UnregisterProjectionLayerExtension

```csharp
public void UnregisterProjectionLayerExtension(OpenXRExtensionWrapper extension)
```

Unregisters the given extension as modifying `XrCompositionLayerProjection`.

**Note:** This cannot be called while the OpenXR session is still running.

### UnregisterProjectionViewsExtension

```csharp
public void UnregisterProjectionViewsExtension(OpenXRExtensionWrapper extension)
```

Unregisters the given extension as a provider of additional data structures to projections views.

**Note:** This cannot be called while the OpenXR session is still running.

### UnregisterProjectionViewsExtension

```csharp
public void UnregisterProjectionViewsExtension(OpenXRExtensionWrapperExtension extension)
```

Unregisters the given extension as a provider of additional data structures to projections views.

**Note:** This cannot be called while the OpenXR session is still running.

### UpdateMainSwapchainSize

```csharp
public void UpdateMainSwapchainSize()
```

Request the recommended resolution from the OpenXR runtime and update the main swapchain size if it has changed.

### XRResult

```csharp
public bool XRResult(ulong result, string format, Array args)
```

Returns `true` if the provided [XrResult](https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrResult.html) (cast to an integer) is successful. Otherwise returns `false` and prints the [XrResult](https://registry.khronos.org/OpenXR/specs/1.0/man/html/XrResult.html) converted to a string, with the specified additional information.

## OpenXRAlphaBlendModeSupport

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Means that [`XRInterface.EnvironmentBlendModeEnum.AlphaBlend`](XRInterface.md) isn't supported at all.
- `Real = 1`：Means that [`XRInterface.EnvironmentBlendModeEnum.AlphaBlend`](XRInterface.md) is really supported.
- `Emulating = 2`：Means that [`XRInterface.EnvironmentBlendModeEnum.AlphaBlend`](XRInterface.md) is emulated.
