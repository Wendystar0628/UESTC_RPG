<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderData

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Abstract render data object, exists for the duration of rendering a single viewport. See also [`RenderDataRD`](RenderDataRD.md), [`RenderSceneData`](RenderSceneData.md), and [`RenderSceneDataRD`](RenderSceneDataRD.md).

**Note:** This is an internal rendering server object. Do not instantiate this class from a script.

## 方法

### GetCameraAttributes

```csharp
public Rid GetCameraAttributes()
```

引擎名：`get_camera_attributes`

Returns the [`Rid`](Rid.md) of the camera attributes object in the [`RenderingServer`](RenderingServer.md) being used to render this viewport.

### GetEnvironment

```csharp
public Rid GetEnvironment()
```

引擎名：`get_environment`

Returns the [`Rid`](Rid.md) of the environment object in the [`RenderingServer`](RenderingServer.md) being used to render this viewport.

### GetRenderSceneBuffers

```csharp
public RenderSceneBuffers GetRenderSceneBuffers()
```

引擎名：`get_render_scene_buffers`

Returns the [`RenderSceneBuffers`](RenderSceneBuffers.md) object managing the scene buffers for rendering this viewport.

### GetRenderSceneData

```csharp
public RenderSceneData GetRenderSceneData()
```

引擎名：`get_render_scene_data`

Returns the [`RenderSceneData`](RenderSceneData.md) object managing this frames scene data.
