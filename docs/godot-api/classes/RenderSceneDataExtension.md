<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderSceneDataExtension

命名空间 `Godot`；class；继承 [`RenderSceneData`](RenderSceneData.md) → [`GodotObject`](GodotObject.md)

This class allows for a RenderSceneData implementation to be made in GDExtension.

## 构造函数

### RenderSceneDataExtension

```csharp
public RenderSceneDataExtension()
```

## 方法

### _GetCamProjection

```csharp
public virtual Projection _GetCamProjection()
```

引擎名：`_get_cam_projection`

Implement this in GDExtension to return the camera [`Projection`](Projection.md).

### _GetCamTransform

```csharp
public virtual Transform3D _GetCamTransform()
```

引擎名：`_get_cam_transform`

Implement this in GDExtension to return the camera [`Transform3D`](Transform3D.md).

### _GetUniformBuffer

```csharp
public virtual Rid _GetUniformBuffer()
```

引擎名：`_get_uniform_buffer`

Implement this in GDExtension to return the [`Rid`](Rid.md) of the uniform buffer containing the scene data as a UBO.

### _GetViewCount

```csharp
public virtual uint _GetViewCount()
```

引擎名：`_get_view_count`

Implement this in GDExtension to return the view count.

### _GetViewEyeOffset

```csharp
public virtual Vector3 _GetViewEyeOffset(uint view)
```

引擎名：`_get_view_eye_offset`

Implement this in GDExtension to return the eye offset for the given `view`.

### _GetViewProjection

```csharp
public virtual Projection _GetViewProjection(uint view)
```

引擎名：`_get_view_projection`

Implement this in GDExtension to return the view [`Projection`](Projection.md) for the given `view`.
