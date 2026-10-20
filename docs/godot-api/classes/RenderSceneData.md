<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderSceneData

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Abstract scene data object, exists for the duration of rendering a single viewport. See also [`RenderSceneDataRD`](RenderSceneDataRD.md), [`RenderData`](RenderData.md), and [`RenderDataRD`](RenderDataRD.md).

**Note:** This is an internal rendering server object. Do not instantiate this class from a script.

## 方法

### GetCamProjection

```csharp
public Projection GetCamProjection()
```

引擎名：`get_cam_projection`

Returns the camera projection used to render this frame.

**Note:** If more than one view is rendered, this will return a combined projection.

### GetCamTransform

```csharp
public Transform3D GetCamTransform()
```

引擎名：`get_cam_transform`

Returns the camera transform used to render this frame.

**Note:** If more than one view is rendered, this will return a centered transform.

### GetUniformBuffer

```csharp
public Rid GetUniformBuffer()
```

引擎名：`get_uniform_buffer`

Return the [`Rid`](Rid.md) of the uniform buffer containing the scene data as a UBO.

### GetViewCount

```csharp
public uint GetViewCount()
```

引擎名：`get_view_count`

Returns the number of views being rendered.

### GetViewEyeOffset

```csharp
public Vector3 GetViewEyeOffset(uint view)
```

引擎名：`get_view_eye_offset`

Returns the eye offset per view used to render this frame. This is the offset between our camera transform and the eye transform.

### GetViewProjection

```csharp
public Projection GetViewProjection(uint view)
```

引擎名：`get_view_projection`

Returns the view projection per view used to render this frame.

**Note:** If a single view is rendered, this returns the camera projection. If more than one view is rendered, this will return a projection for the given view including the eye offset.
