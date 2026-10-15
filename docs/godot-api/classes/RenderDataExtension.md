<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderDataExtension

命名空间 `Godot`；class；继承 [`RenderData`](RenderData.md) → [`GodotObject`](GodotObject.md)

This class allows for a RenderData implementation to be made in GDExtension.

## 构造函数

### RenderDataExtension

```csharp
public RenderDataExtension()
```

## 方法

### _GetCameraAttributes

```csharp
public virtual Rid _GetCameraAttributes()
```

引擎名：`_get_camera_attributes`

Implement this in GDExtension to return the [`Rid`](Rid.md) for the implementation's camera attributes object.

### _GetEnvironment

```csharp
public virtual Rid _GetEnvironment()
```

引擎名：`_get_environment`

Implement this in GDExtension to return the [`Rid`](Rid.md) of the implementation's environment object.

### _GetRenderSceneBuffers

```csharp
public virtual RenderSceneBuffers _GetRenderSceneBuffers()
```

引擎名：`_get_render_scene_buffers`

Implement this in GDExtension to return the implementation's [`RenderSceneBuffers`](RenderSceneBuffers.md) object.

### _GetRenderSceneData

```csharp
public virtual RenderSceneData _GetRenderSceneData()
```

引擎名：`_get_render_scene_data`

Implement this in GDExtension to return the implementation's [`RenderSceneDataExtension`](RenderSceneDataExtension.md) object.
