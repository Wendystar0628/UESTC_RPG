<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Camera3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Camera3D` is a special node that displays what is visible from its current location. Cameras register themselves in the nearest [`Viewport`](Viewport.md) node (when ascending the tree). Only one camera can be active per viewport. If no viewport is available ascending the tree, the camera will register in the global viewport. In other words, a camera just provides 3D display capabilities to a [`Viewport`](Viewport.md), and, without one, a scene registered in that [`Viewport`](Viewport.md) (or higher viewports) can't be displayed.

## 构造函数

### Camera3D

```csharp
public Camera3D()
```

## 属性

### Attributes

```csharp
public CameraAttributes Attributes { get; set; }
```

引擎名：`attributes`

The [`CameraAttributes`](CameraAttributes.md) to use for this camera.

### Compositor

```csharp
public Compositor Compositor { get; set; }
```

引擎名：`compositor`

The [`Compositor`](Compositor.md) to use for this camera.

### CullMask

```csharp
public uint CullMask { get; set; }
```

引擎名：`cull_mask`

The culling mask that describes which [`VisualInstance3D.Layers`](VisualInstance3D.md) are rendered by this camera. By default, all 20 user-visible layers are rendered.

**Note:** Since the `CullMask` allows for 32 layers to be stored in total, there are an additional 12 layers that are only used internally by the engine and aren't exposed in the editor. Setting `CullMask` using a script allows you to toggle those reserved layers, which can be useful for editor plugins.

To adjust `CullMask` more easily using a script, use `GetCullMaskValue()` and `SetCullMaskValue()`.

**Note:** [`VoxelGI`](VoxelGI.md), SDFGI and [`LightmapGI`](LightmapGI.md) will always take all layers into account to determine what contributes to global illumination. If this is an issue, set [`GeometryInstance3D.GIMode`](GeometryInstance3D.md) to [`GeometryInstance3D.GIModeEnum.Disabled`](GeometryInstance3D.md) for meshes and [`Light3D.LightBakeMode`](Light3D.md) to [`Light3D.BakeMode.Disabled`](Light3D.md) for lights to exclude them from global illumination.

### Current

```csharp
public bool Current { get; set; }
```

引擎名：`current`

If `true`, the ancestor [`Viewport`](Viewport.md) is currently using this camera.

If multiple cameras are in the scene, one will always be made current. For example, if two `Camera3D` nodes are present in the scene and only one is current, setting one camera's `Current` to `false` will cause the other camera to be made current.

### DopplerTracking

```csharp
public Camera3D.DopplerTrackingEnum DopplerTracking { get; set; }
```

引擎名：`doppler_tracking`

If not [`Camera3D.DopplerTrackingEnum.Disabled`](Camera3D.md), this camera will simulate the [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) for objects changed in particular `_process` methods.

**Note:** The Doppler effect will only be heard on [`AudioStreamPlayer3D`](AudioStreamPlayer3D.md)s if [`AudioStreamPlayer3D.DopplerTracking`](AudioStreamPlayer3D.md) is not set to [`AudioStreamPlayer3D.DopplerTrackingEnum.Disabled`](AudioStreamPlayer3D.md).

### Environment

```csharp
public Environment Environment { get; set; }
```

引擎名：`environment`

The [`Environment`](Environment.md) to use for this camera.

### Far

```csharp
public float Far { get; set; }
```

引擎名：`far`

The distance to the far culling boundary for this camera relative to its local Z axis. Higher values allow the camera to see further away, while decreasing `Far` can improve performance if it results in objects being partially or fully culled.

### Fov

```csharp
public float Fov { get; set; }
```

引擎名：`fov`

The camera's field of view angle (in degrees). Only applicable in perspective mode. Since `KeepAspect` locks one axis, `Fov` sets the other axis' field of view angle.

For reference, the default vertical field of view value (`75.0`) is equivalent to a horizontal FOV of:

- ~91.31 degrees in a 4:3 viewport

- ~101.67 degrees in a 16:10 viewport

- ~107.51 degrees in a 16:9 viewport

- ~121.63 degrees in a 21:9 viewport

### FrustumOffset

```csharp
public Vector2 FrustumOffset { get; set; }
```

引擎名：`frustum_offset`

The camera's frustum offset. This can be changed from the default to create "tilted frustum" effects such as [Y-shearing](https://zdoom.org/wiki/Y-shearing).

**Note:** Only effective if `Projection` is [`Camera3D.ProjectionType.Frustum`](Camera3D.md).

### HOffset

```csharp
public float HOffset { get; set; }
```

引擎名：`h_offset`

The horizontal (X) offset of the camera viewport.

### KeepAspect

```csharp
public Camera3D.KeepAspectEnum KeepAspect { get; set; }
```

引擎名：`keep_aspect`

The axis to lock during `Fov`/`Size` adjustments. Can be either [`Camera3D.KeepAspectEnum.Width`](Camera3D.md) or [`Camera3D.KeepAspectEnum.Height`](Camera3D.md).

### Near

```csharp
public float Near { get; set; }
```

引擎名：`near`

The distance to the near culling boundary for this camera relative to its local Z axis. Lower values allow the camera to see objects more up close to its origin, at the cost of lower precision across the *entire* range. Values lower than the default can lead to increased Z-fighting.

### Projection

```csharp
public Camera3D.ProjectionType Projection { get; set; }
```

引擎名：`projection`

The camera's projection mode. In [`Camera3D.ProjectionType.Perspective`](Camera3D.md) mode, objects' Z distance from the camera's local space scales their perceived size.

### Size

```csharp
public float Size { get; set; }
```

引擎名：`size`

The camera's size in meters measured as the diameter of the width or height, depending on `KeepAspect`. Only applicable in orthogonal and frustum modes.

### VOffset

```csharp
public float VOffset { get; set; }
```

引擎名：`v_offset`

The vertical (Y) offset of the camera viewport.

## 方法

### ClearCurrent

```csharp
public void ClearCurrent(bool enableNext = true)
```

引擎名：`clear_current`

If this is the current camera, remove it from being current. If `enableNext` is `true`, request to make the next camera current, if any.

### GetAttributes

```csharp
public CameraAttributes GetAttributes()
```

引擎名：`get_attributes`

### GetCameraProjection

```csharp
public Projection GetCameraProjection()
```

引擎名：`get_camera_projection`

Returns the projection matrix that this camera uses to render to its associated viewport. The camera must be part of the scene tree to function.

### GetCameraRid

```csharp
public Rid GetCameraRid()
```

引擎名：`get_camera_rid`

Returns the camera's RID from the [`RenderingServer`](RenderingServer.md).

### GetCameraTransform

```csharp
public Transform3D GetCameraTransform()
```

引擎名：`get_camera_transform`

Returns the transform of the camera plus the vertical (`VOffset`) and horizontal (`HOffset`) offsets; and any other adjustments made to the position and orientation of the camera by subclassed cameras such as [`XRCamera3D`](XRCamera3D.md).

### GetCompositor

```csharp
public Compositor GetCompositor()
```

引擎名：`get_compositor`

### GetCullMask

```csharp
public uint GetCullMask()
```

引擎名：`get_cull_mask`

### GetCullMaskValue

```csharp
public bool GetCullMaskValue(int layerNumber)
```

引擎名：`get_cull_mask_value`

Returns whether or not the specified layer of the `CullMask` is enabled, given a `layerNumber` between 1 and 20.

### GetDopplerTracking

```csharp
public Camera3D.DopplerTrackingEnum GetDopplerTracking()
```

引擎名：`get_doppler_tracking`

### GetEnvironment

```csharp
public Environment GetEnvironment()
```

引擎名：`get_environment`

### GetFar

```csharp
public float GetFar()
```

引擎名：`get_far`

### GetFov

```csharp
public float GetFov()
```

引擎名：`get_fov`

### GetFrustum

```csharp
public Array<Plane> GetFrustum()
```

引擎名：`get_frustum`

Returns the camera's frustum planes in world space units as an array of [`Plane`](Plane.md)s in the following order: near, far, left, top, right, bottom. Not to be confused with `FrustumOffset`.

### GetFrustumOffset

```csharp
public Vector2 GetFrustumOffset()
```

引擎名：`get_frustum_offset`

### GetHOffset

```csharp
public float GetHOffset()
```

引擎名：`get_h_offset`

### GetKeepAspectMode

```csharp
public Camera3D.KeepAspectEnum GetKeepAspectMode()
```

引擎名：`get_keep_aspect_mode`

### GetNear

```csharp
public float GetNear()
```

引擎名：`get_near`

### GetProjection

```csharp
public Camera3D.ProjectionType GetProjection()
```

引擎名：`get_projection`

### GetPyramidShapeRid

```csharp
public Rid GetPyramidShapeRid()
```

引擎名：`get_pyramid_shape_rid`

Returns the RID of a pyramid shape encompassing the camera's view frustum, ignoring the camera's near plane. The tip of the pyramid represents the position of the camera.

### GetSize

```csharp
public float GetSize()
```

引擎名：`get_size`

### GetVOffset

```csharp
public float GetVOffset()
```

引擎名：`get_v_offset`

### IsCurrent

```csharp
public bool IsCurrent()
```

引擎名：`is_current`

### IsPositionBehind

```csharp
public bool IsPositionBehind(Vector3 worldPoint)
```

引擎名：`is_position_behind`

Returns `true` if the given position is behind the camera (the blue part of the linked diagram). [See this diagram](https://raw.githubusercontent.com/godotengine/godot-docs/master/img/camera3d_position_frustum.png) for an overview of position query methods.

**Note:** A position which returns `false` may still be outside the camera's field of view.

### IsPositionInFrustum

```csharp
public bool IsPositionInFrustum(Vector3 worldPoint)
```

引擎名：`is_position_in_frustum`

Returns `true` if the given position is inside the camera's frustum (the green part of the linked diagram). [See this diagram](https://raw.githubusercontent.com/godotengine/godot-docs/master/img/camera3d_position_frustum.png) for an overview of position query methods.

### MakeCurrent

```csharp
public void MakeCurrent()
```

引擎名：`make_current`

Makes this camera the current camera for the [`Viewport`](Viewport.md) (see class description). If the camera node is outside the scene tree, it will attempt to become current once it's added.

### ProjectLocalRayNormal

```csharp
public Vector3 ProjectLocalRayNormal(Vector2 screenPoint)
```

引擎名：`project_local_ray_normal`

Returns a normal vector from the screen point location directed along the camera. Orthogonal cameras are normalized. Perspective cameras account for perspective, screen width/height, etc.

### ProjectPosition

```csharp
public Vector3 ProjectPosition(Vector2 screenPoint, float zDepth)
```

引擎名：`project_position`

Returns the 3D point in world space that maps to the given 2D coordinate in the [`Viewport`](Viewport.md) rectangle on a plane that is the given `zDepth` distance into the scene away from the camera.

### ProjectRayNormal

```csharp
public Vector3 ProjectRayNormal(Vector2 screenPoint)
```

引擎名：`project_ray_normal`

Returns a normal vector in world space, that is the result of projecting a point on the [`Viewport`](Viewport.md) rectangle by the inverse camera projection. This is useful for casting rays in the form of (origin, normal) for object intersection or picking.

### ProjectRayOrigin

```csharp
public Vector3 ProjectRayOrigin(Vector2 screenPoint)
```

引擎名：`project_ray_origin`

Returns a 3D position in world space, that is the result of projecting a point on the [`Viewport`](Viewport.md) rectangle by the inverse camera projection. This is useful for casting rays in the form of (origin, normal) for object intersection or picking.

### SetAttributes

```csharp
public void SetAttributes(CameraAttributes env)
```

引擎名：`set_attributes`

### SetCompositor

```csharp
public void SetCompositor(Compositor compositor)
```

引擎名：`set_compositor`

### SetCullMask

```csharp
public void SetCullMask(uint mask)
```

引擎名：`set_cull_mask`

### SetCullMaskValue

```csharp
public void SetCullMaskValue(int layerNumber, bool value)
```

引擎名：`set_cull_mask_value`

Based on `value`, enables or disables the specified layer in the `CullMask`, given a `layerNumber` between 1 and 20.

### SetCurrent

```csharp
public void SetCurrent(bool enabled)
```

引擎名：`set_current`

### SetDopplerTracking

```csharp
public void SetDopplerTracking(Camera3D.DopplerTrackingEnum mode)
```

引擎名：`set_doppler_tracking`

### SetEnvironment

```csharp
public void SetEnvironment(Environment env)
```

引擎名：`set_environment`

### SetFar

```csharp
public void SetFar(float far)
```

引擎名：`set_far`

### SetFov

```csharp
public void SetFov(float fov)
```

引擎名：`set_fov`

### SetFrustum

```csharp
public void SetFrustum(float size, Vector2 offset, float zNear, float zFar)
```

引擎名：`set_frustum`

Sets the camera projection to frustum mode (see [`Camera3D.ProjectionType.Frustum`](Camera3D.md)), by specifying a `size`, an `offset`, and the `zNear` and `zFar` clip planes in world space units. The `size` parameter represents the size of the near plane, either its width or height depending on the value of `KeepAspect`. See also `FrustumOffset`.

### SetFrustumOffset

```csharp
public void SetFrustumOffset(Vector2 offset)
```

引擎名：`set_frustum_offset`

### SetHOffset

```csharp
public void SetHOffset(float offset)
```

引擎名：`set_h_offset`

### SetKeepAspectMode

```csharp
public void SetKeepAspectMode(Camera3D.KeepAspectEnum mode)
```

引擎名：`set_keep_aspect_mode`

### SetNear

```csharp
public void SetNear(float near)
```

引擎名：`set_near`

### SetOrthogonal

```csharp
public void SetOrthogonal(float size, float zNear, float zFar)
```

引擎名：`set_orthogonal`

Sets the camera projection to orthogonal mode (see [`Camera3D.ProjectionType.Orthogonal`](Camera3D.md)), by specifying a `size`, and the `zNear` and `zFar` clip planes in world space units.

As a hint, 3D games that look 2D often use this projection, with `size` specified in pixels.

### SetPerspective

```csharp
public void SetPerspective(float fov, float zNear, float zFar)
```

引擎名：`set_perspective`

Sets the camera projection to perspective mode (see [`Camera3D.ProjectionType.Perspective`](Camera3D.md)), by specifying a `fov` (field of view) angle in degrees, and the `zNear` and `zFar` clip planes in world space units.

### SetProjection

```csharp
public void SetProjection(Camera3D.ProjectionType mode)
```

引擎名：`set_projection`

### SetSize

```csharp
public void SetSize(float size)
```

引擎名：`set_size`

### SetVOffset

```csharp
public void SetVOffset(float offset)
```

引擎名：`set_v_offset`

### UnprojectPosition

```csharp
public Vector2 UnprojectPosition(Vector3 worldPoint)
```

引擎名：`unproject_position`

Returns the 2D coordinate in the [`Viewport`](Viewport.md) rectangle that maps to the given 3D point in world space.

**Note:** When using this to position GUI elements over a 3D viewport, use `IsPositionBehind()` to prevent them from appearing if the 3D point is behind the camera:

```csharp
# This code block is part of a script that inherits from Node3D.
# `control` is a reference to a node inheriting from Control.
control.visible = not get_viewport().get_camera_3d().is_position_behind(global_transform.origin)
control.position = get_viewport().get_camera_3d().unproject_position(global_transform.origin)

```

## DopplerTrackingEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) simulation (default).
- `IdleStep = 1`：Simulate [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) by tracking positions of objects that are changed in `_process`.
- `PhysicsStep = 2`：Simulate [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) by tracking positions of objects that are changed in `_physics_process`.

## KeepAspectEnum

命名空间 `Godot`；enum

### 枚举值

- `Width = 0`：Preserves the horizontal aspect ratio; also known as Vert- scaling.
- `Height = 1`：Preserves the vertical aspect ratio; also known as Hor+ scaling.

## ProjectionType

命名空间 `Godot`；enum

### 枚举值

- `Perspective = 0`：Perspective projection.
- `Orthogonal = 1`：Orthogonal projection, also known as orthographic projection.
- `Frustum = 2`：Frustum projection.
