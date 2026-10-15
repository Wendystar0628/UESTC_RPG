<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRRenderModel

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node will display an OpenXR render model by accessing the associated GLTF and processes all animation data (if supported by the XR runtime).

Render models were introduced to allow showing the correct model for the controller (or other device) the user has in hand, since the OpenXR action map does not provide information about the hardware used by the user. Note that while the controller (or device) can be somewhat inferred by the bound action map profile, this is a dangerous approach as the user may be using hardware not known at time of development and OpenXR will simply simulate an available interaction profile.

## 构造函数

### OpenXRRenderModel

```csharp
public OpenXRRenderModel()
```

## 属性

### RenderModel

```csharp
public Rid RenderModel { get; set; }
```

引擎名：`render_model`

The render model RID for the render model to load, as returned by [`OpenXRRenderModelExtension.RenderModelCreate`](OpenXRRenderModelExtension.md) or [`OpenXRRenderModelExtension.RenderModelGetAll`](OpenXRRenderModelExtension.md).

## 方法

### GetRenderModel

```csharp
public Rid GetRenderModel()
```

引擎名：`get_render_model`

### GetTopLevelPath

```csharp
public string GetTopLevelPath()
```

引擎名：`get_top_level_path`

Returns the top level path related to this render model.

### SetRenderModel

```csharp
public void SetRenderModel(Rid renderModel)
```

引擎名：`set_render_model`

## 事件（信号）

### RenderModelTopLevelPathChanged

```csharp
public event Action RenderModelTopLevelPathChanged
```

引擎名：`render_model_top_level_path_changed`

Emitted when the top level path of this render model has changed.
