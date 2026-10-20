<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRRenderModelExtension

命名空间 `Godot`；class；继承 [`OpenXRExtensionWrapper`](OpenXRExtensionWrapper.md) → [`GodotObject`](GodotObject.md)

This class implements the OpenXR Render Model Extension, if enabled it will maintain a list of active render models and provides an interface to the render model data.

## 构造函数

### OpenXRRenderModelExtension

```csharp
public OpenXRRenderModelExtension()
```

## 方法

### IsActive

```csharp
public bool IsActive()
```

引擎名：`is_active`

Returns `true` if OpenXR's render model extension is supported and enabled.

**Note:** This only returns a valid value after OpenXR has been initialized.

### RenderModelCreate

```csharp
public Rid RenderModelCreate(ulong renderModelId)
```

引擎名：`render_model_create`

Creates a render model object within OpenXR using a render model id.

**Note:** This function is exposed for dependent OpenXR extensions that provide render model ids to be used with the render model extension.

### RenderModelDestroy

```csharp
public void RenderModelDestroy(Rid renderModel)
```

引擎名：`render_model_destroy`

Destroys a render model object within OpenXR that was previously created with `RenderModelCreate()`.

**Note:** This function is exposed for dependent OpenXR extensions that provide render model ids to be used with the render model extension.

### RenderModelGetAll

```csharp
public Array<Rid> RenderModelGetAll()
```

引擎名：`render_model_get_all`

Returns an array of all currently active render models registered with this extension.

### RenderModelGetAnimatableNodeCount

```csharp
public uint RenderModelGetAnimatableNodeCount(Rid renderModel)
```

引擎名：`render_model_get_animatable_node_count`

Returns the number of animatable nodes this render model has.

### RenderModelGetAnimatableNodeName

```csharp
public string RenderModelGetAnimatableNodeName(Rid renderModel, uint index)
```

引擎名：`render_model_get_animatable_node_name`

Returns the name of the given animatable node.

### RenderModelGetAnimatableNodeTransform

```csharp
public Transform3D RenderModelGetAnimatableNodeTransform(Rid renderModel, uint index)
```

引擎名：`render_model_get_animatable_node_transform`

Returns the current local transform for an animatable node. This is updated every frame.

### RenderModelGetConfidence

```csharp
public XRPose.TrackingConfidenceEnum RenderModelGetConfidence(Rid renderModel)
```

引擎名：`render_model_get_confidence`

Returns the tracking confidence of the tracking data for the render model.

### RenderModelGetRootTransform

```csharp
public Transform3D RenderModelGetRootTransform(Rid renderModel)
```

引擎名：`render_model_get_root_transform`

Returns the root transform of a render model. This is the tracked position relative to our [`XROrigin3D`](XROrigin3D.md) node.

### RenderModelGetSubactionPaths

```csharp
public string[] RenderModelGetSubactionPaths(Rid renderModel)
```

引擎名：`render_model_get_subaction_paths`

Returns a list of active subaction paths for this `renderModel`.

**Note:** If different devices are bound to your actions than available in suggested interaction bindings, this information shows paths related to the interaction bindings being mimicked by that device.

### RenderModelGetTopLevelPath

```csharp
public string RenderModelGetTopLevelPath(Rid renderModel)
```

引擎名：`render_model_get_top_level_path`

Returns the top level path associated with this `renderModel`. If provided this identifies whether the render model is associated with the player's hands or other body part.

### RenderModelIsAnimatableNodeVisible

```csharp
public bool RenderModelIsAnimatableNodeVisible(Rid renderModel, uint index)
```

引擎名：`render_model_is_animatable_node_visible`

Returns `true` if this animatable node should be visible.

### RenderModelNewSceneInstance

```csharp
public Node3D RenderModelNewSceneInstance(Rid renderModel)
```

引擎名：`render_model_new_scene_instance`

Returns an instance of a subscene that contains all [`MeshInstance3D`](MeshInstance3D.md) nodes that allow you to visualize the render model.

## 事件（信号）

### RenderModelAdded

```csharp
public event OpenXRRenderModelExtension.RenderModelAddedEventHandler RenderModelAdded
```

引擎名：`render_model_added`

Emitted when a new render model is added.

### RenderModelRemoved

```csharp
public event OpenXRRenderModelExtension.RenderModelRemovedEventHandler RenderModelRemoved
```

引擎名：`render_model_removed`

Emitted when a render model is removed.

### RenderModelTopLevelPathChanged

```csharp
public event OpenXRRenderModelExtension.RenderModelTopLevelPathChangedEventHandler RenderModelTopLevelPathChanged
```

引擎名：`render_model_top_level_path_changed`

Emitted when the top level path associated with a render model changed.

## RenderModelAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRRenderModelExtension.RenderModelAdded`](OpenXRRenderModelExtension.md) event of a [`OpenXRRenderModelExtension`](OpenXRRenderModelExtension.md) class.

### 签名

#### Invoke

```csharp
delegate void RenderModelAddedEventHandler(Rid renderModel)
```

## RenderModelRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRRenderModelExtension.RenderModelRemoved`](OpenXRRenderModelExtension.md) event of a [`OpenXRRenderModelExtension`](OpenXRRenderModelExtension.md) class.

### 签名

#### Invoke

```csharp
delegate void RenderModelRemovedEventHandler(Rid renderModel)
```

## RenderModelTopLevelPathChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRRenderModelExtension.RenderModelTopLevelPathChanged`](OpenXRRenderModelExtension.md) event of a [`OpenXRRenderModelExtension`](OpenXRRenderModelExtension.md) class.

### 签名

#### Invoke

```csharp
delegate void RenderModelTopLevelPathChangedEventHandler(Rid renderModel)
```
