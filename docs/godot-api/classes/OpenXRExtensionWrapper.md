<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRExtensionWrapper

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

`OpenXRExtensionWrapper` allows implementing OpenXR extensions with GDExtension. The extension should be registered with `RegisterExtensionWrapper()`.

When [`OpenXRInterface`](OpenXRInterface.md) is initialized as the primary interface and any [`Viewport`](Viewport.md) has [`Viewport.UseXR`](Viewport.md) set to `true`, OpenXR will become involved in Godot's rendering process. If `ProjectSettings.rendering/driver/threads/thread_model` is set to "Separate", Godot's renderer will run on its own thread, and special care must be taken in all `OpenXRExtensionWrapper`s in order to prevent crashes or unexpected behavior. Some virtual methods will be called on the render thread, and any data they access should not be directly written to on the main thread. This is to prevent two potential issues:

1. Changes intended for the next frame, taking effect on the current frame. When using the "Separate" thread model, the main thread will immediately start working on the next frame while the render thread may still be rendering the current frame. If the main thread changes anything used by the render thread directly, the change could end up being used one frame earlier than intended.

2. Reading and writing to the same data at the same time from different threads can lead to the render thread using data in an invalid state.

In most cases, the solution is to use [`RenderingServer.CallOnRenderThread`](RenderingServer.md) to schedule [`Callable`](Callable.md)s to write to any data used on the render thread. When using the "Separate" thread model, these [`Callable`](Callable.md)s will run after the renderer finishes the current frame and before it starts rendering the next frame. When not using this mode, they'll run immediately, so it's recommended to always use [`RenderingServer.CallOnRenderThread`](RenderingServer.md) in these cases, which will allow your code to do the right thing regardless of the thread model.

Any virtual methods that run on the render thread will be noted below.

## 构造函数

### OpenXRExtensionWrapper

```csharp
public OpenXRExtensionWrapper()
```

## 方法

### GetOpenxrApi

```csharp
public OpenXRApiExtension GetOpenxrApi()
```

引擎名：`get_openxr_api`

Returns the created [`OpenXRApiExtension`](OpenXRApiExtension.md), which can be used to access the OpenXR API.

### RegisterExtensionWrapper

```csharp
public void RegisterExtensionWrapper()
```

引擎名：`register_extension_wrapper`

Registers the extension. This should happen at core module initialization level.

**Note:** This cannot be called once OpenXR has been initialized.

### _GetCompositionLayer

```csharp
public virtual ulong _GetCompositionLayer(int index)
```

引擎名：`_get_composition_layer`

Returns a pointer to an `XrCompositionLayerBaseHeader` struct to provide the given composition layer.

This will only be called if the extension previously registered itself with [`OpenXRApiExtension.RegisterCompositionLayerProvider`](OpenXRApiExtension.md).

**Note:** This virtual method will be called on the render thread. Additionally, the data it returns will be used shortly after this method is called, so it needs to remain valid until the next time `_OnPreRender()` runs.

### _GetCompositionLayerCount

```csharp
public virtual int _GetCompositionLayerCount()
```

引擎名：`_get_composition_layer_count`

Returns the number of composition layers this extension wrapper provides via `_GetCompositionLayer()`.

This will only be called if the extension previously registered itself with [`OpenXRApiExtension.RegisterCompositionLayerProvider`](OpenXRApiExtension.md).

**Note:** This virtual method will be called on the render thread. Additionally, the data it returns will be used shortly after this method is called, so it needs to remain valid until the next time `_OnPreRender()` runs.

### _GetCompositionLayerOrder

```csharp
public virtual int _GetCompositionLayerOrder(int index)
```

引擎名：`_get_composition_layer_order`

Returns an integer that will be used to sort the given composition layer provided via `_GetCompositionLayer()`. Lower numbers will move the layer to the front of the list, and higher numbers to the end. The default projection layer has an order of `0`, so layers provided by this method should probably be above or below (but not exactly) `0`.

This will only be called if the extension previously registered itself with [`OpenXRApiExtension.RegisterCompositionLayerProvider`](OpenXRApiExtension.md).

**Note:** This virtual method will be called on the render thread. Additionally, the data it returns will be used shortly after this method is called, so it needs to remain valid until the next time `_OnPreRender()` runs.

### _GetRequestedExtensions

```csharp
public virtual Dictionary _GetRequestedExtensions(ulong xRVersion)
```

引擎名：`_get_requested_extensions`

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) of OpenXR extensions related to this extension. `xRVersion` specifies the OpenXR version we're instantiating. This will be zero if the editor requests this list to flag supported features. The [`Collections.Dictionary`](Collections.Dictionary.md) should contain the name of the extension, mapped to a `bool *` cast to an integer:

- If the `bool *` is a `nullptr` this extension is mandatory.

- If the `bool *` points to a boolean, the boolean will be updated to `true` if the extension is enabled.

### _GetSuggestedTrackerNames

```csharp
public virtual string[] _GetSuggestedTrackerNames()
```

引擎名：`_get_suggested_tracker_names`

Returns a `System.String`[] of positional tracker names that are used within the extension wrapper.

### _GetViewportCompositionLayerExtensionProperties

```csharp
public virtual Array<Dictionary> _GetViewportCompositionLayerExtensionProperties()
```

引擎名：`_get_viewport_composition_layer_extension_properties`

Gets an array of [`Collections.Dictionary`](Collections.Dictionary.md)s that represent properties, just like [`GodotObject.GetPropertyList`](GodotObject.md), that will be added to [`OpenXRCompositionLayer`](OpenXRCompositionLayer.md) nodes.

**Note:** This virtual method will be called on the render thread.

### _GetViewportCompositionLayerExtensionPropertyDefaults

```csharp
public virtual Dictionary _GetViewportCompositionLayerExtensionPropertyDefaults()
```

引擎名：`_get_viewport_composition_layer_extension_property_defaults`

Gets a [`Collections.Dictionary`](Collections.Dictionary.md) containing the default values for the properties returned by `_GetViewportCompositionLayerExtensionProperties()`.

### _OnBeforeInstanceCreated

```csharp
public virtual void _OnBeforeInstanceCreated()
```

引擎名：`_on_before_instance_created`

Called before the OpenXR instance is created.

**Note:** This virtual method will be called on the main thread, however, it will be called *before* OpenXR becomes involved in rendering, so it is safe to write to data that will be used by the render thread.

### _OnInstanceCreated

```csharp
public virtual void _OnInstanceCreated(ulong instance)
```

引擎名：`_on_instance_created`

Called right after the OpenXR instance is created.

**Note:** This virtual method will be called on the main thread, however, it will be called *before* OpenXR becomes involved in rendering, so it is safe to write to data that will be used by the render thread.

### _OnInstanceDestroyed

```csharp
public virtual void _OnInstanceDestroyed()
```

引擎名：`_on_instance_destroyed`

Called right before the OpenXR instance is destroyed.

**Note:** This virtual method will be called on the main thread, however, it will be called *after* OpenXR is done being involved in rendering, so it is safe to write to data that was used by the render thread.

### _OnMainSwapchainsCreated

```csharp
public virtual void _OnMainSwapchainsCreated()
```

引擎名：`_on_main_swapchains_created`

Called right after the main swapchains are (re)created.

**Note:** This virtual method will be called on the render thread.

### _OnPostDrawViewport

```csharp
public virtual void _OnPostDrawViewport(Rid viewport)
```

引擎名：`_on_post_draw_viewport`

Called right after the given viewport is rendered.

**Note:** The draw commands might only be queued at this point, not executed.

**Note:** This virtual method will be called on the render thread.

### _OnPreDrawViewport

```csharp
public virtual void _OnPreDrawViewport(Rid viewport)
```

引擎名：`_on_pre_draw_viewport`

Called right before the given viewport is rendered.

**Note:** This virtual method will be called on the render thread.

### _OnPreRender

```csharp
public virtual void _OnPreRender()
```

引擎名：`_on_pre_render`

Called right before the XR viewports begin their rendering step.

**Note:** This virtual method will be called on the render thread.

### _OnProcess

```csharp
public virtual void _OnProcess()
```

引擎名：`_on_process`

Called as part of the OpenXR process handling. This happens right before general and physics processing steps of the main loop. During this step controller data is queried and made available to game logic.

### _OnRegisterMetadata

```csharp
public virtual void _OnRegisterMetadata(OpenXRInteractionProfileMetadata interactionProfileMetadata)
```

引擎名：`_on_register_metadata`

Allows extensions to register additional controller metadata. This function is called even when the OpenXR API is not constructed as the metadata needs to be available to the editor.

Extensions should also provide metadata regardless of whether they are supported on the host system. The controller data is used to setup action maps for users who may have access to the relevant hardware.

### _OnSessionCreated

```csharp
public virtual void _OnSessionCreated(ulong session)
```

引擎名：`_on_session_created`

Called right after the OpenXR session is created.

**Note:** This virtual method will be called on the main thread, however, it will be called *before* OpenXR becomes involved in rendering, so it is safe to write to data that will be used by the render thread.

### _OnSessionDestroyed

```csharp
public virtual void _OnSessionDestroyed()
```

引擎名：`_on_session_destroyed`

Called right before the OpenXR session is destroyed.

**Note:** This virtual method will be called on the main thread, however, it will be called *after* OpenXR is done being involved in rendering, so it is safe to write to data that was used by the render thread.

### _OnStateExiting

```csharp
public virtual void _OnStateExiting()
```

引擎名：`_on_state_exiting`

Called when the OpenXR session state is changed to exiting.

### _OnStateFocused

```csharp
public virtual void _OnStateFocused()
```

引擎名：`_on_state_focused`

Called when the OpenXR session state is changed to focused. This state is the active state when the game runs.

### _OnStateIdle

```csharp
public virtual void _OnStateIdle()
```

引擎名：`_on_state_idle`

Called when the OpenXR session state is changed to idle.

### _OnStateLossPending

```csharp
public virtual void _OnStateLossPending()
```

引擎名：`_on_state_loss_pending`

Called when the OpenXR session state is changed to loss pending.

### _OnStateReady

```csharp
public virtual void _OnStateReady()
```

引擎名：`_on_state_ready`

Called when the OpenXR session state is changed to ready. This means OpenXR is ready to set up the session.

### _OnStateStopping

```csharp
public virtual void _OnStateStopping()
```

引擎名：`_on_state_stopping`

Called when the OpenXR session state is changed to stopping.

### _OnStateSynchronized

```csharp
public virtual void _OnStateSynchronized()
```

引擎名：`_on_state_synchronized`

Called when the OpenXR session state is changed to synchronized. OpenXR also returns to this state when the application loses focus.

### _OnStateVisible

```csharp
public virtual void _OnStateVisible()
```

引擎名：`_on_state_visible`

Called when the OpenXR session state is changed to visible. This means OpenXR is now ready to receive frames.

### _OnSyncActions

```csharp
public virtual void _OnSyncActions()
```

引擎名：`_on_sync_actions`

Called when OpenXR has performed its action sync.

### _PrepareViewConfiguration

```csharp
public virtual void _PrepareViewConfiguration(int viewCount)
```

引擎名：`_prepare_view_configuration`

Called before `_set_view_configuration_and_get_next_pointer` to allow the extension to reserve data for the given number of views.

### _PrintViewConfigurationInfo

```csharp
public virtual void _PrintViewConfigurationInfo(int view)
```

引擎名：`_print_view_configuration_info`

Called to allow an extension to print additional information about its view configuration, if applicable. This will only be called if verbose output is enabled.
