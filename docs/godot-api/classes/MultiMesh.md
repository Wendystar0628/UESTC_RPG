<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiMesh

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

MultiMesh provides low-level mesh instancing. Drawing thousands of [`MeshInstance3D`](MeshInstance3D.md) nodes can be slow, since each object is submitted to the GPU then drawn individually.

MultiMesh is much faster as it can draw thousands of instances with a single draw call, resulting in less API overhead.

As a drawback, if the instances are too far away from each other, performance may be reduced as every single instance will always render (they are spatially indexed as one, for the whole object).

Since instances may have any behavior, the AABB used for visibility must be provided by the user.

**Note:** A MultiMesh is a single object, therefore the same maximum lights per object restriction applies. This means, that once the maximum lights are consumed by one or more instances, the rest of the MultiMesh instances will **not** receive any lighting.

**Note:** Blend Shapes will be ignored if used in a MultiMesh.

## 构造函数

### MultiMesh

```csharp
public MultiMesh()
```

## 属性

### Buffer

```csharp
public float[] Buffer { get; set; }
```

引擎名：`buffer`

### ColorArray

```csharp
public Color[] ColorArray { get; set; }
```

引擎名：`color_array`

Array containing each [`Color`](Color.md) used by all instances of this mesh.

### CustomAabb

```csharp
public Aabb CustomAabb { get; set; }
```

引擎名：`custom_aabb`

Custom AABB for this MultiMesh resource. Setting this manually prevents costly runtime AABB recalculations.

### CustomDataArray

```csharp
public Color[] CustomDataArray { get; set; }
```

引擎名：`custom_data_array`

Array containing each custom data value used by all instances of this mesh, as a [`Color`](Color.md)[].

### InstanceCount

```csharp
public int InstanceCount { get; set; }
```

引擎名：`instance_count`

Number of instances that will get drawn. This clears and (re)sizes the buffers. Setting data format or flags afterwards will have no effect.

By default, all instances are drawn but you can limit this with `VisibleInstanceCount`.

### Mesh

```csharp
public Mesh Mesh { get; set; }
```

引擎名：`mesh`

[`Mesh`](Mesh.md) resource to be instanced.

The looks of the individual instances can be modified using `SetInstanceColor()` and `SetInstanceCustomData()`.

### PhysicsInterpolationQuality

```csharp
public MultiMesh.PhysicsInterpolationQualityEnum PhysicsInterpolationQuality { get; set; }
```

引擎名：`physics_interpolation_quality`

Choose whether to use an interpolation method that favors speed or quality.

When using low physics tick rates (typically below 20) or high rates of object rotation, you may get better results from the high quality setting.

**Note:** Fast quality does not equate to low quality. Except in the special cases mentioned above, the quality should be comparable to high quality.

### Transform2DArray

```csharp
public Vector2[] Transform2DArray { get; set; }
```

Array containing each [`Transform2D`](Transform2D.md) value used by all instances of this mesh, as a [`Vector2`](Vector2.md)[]. Each transform is divided into 3 [`Vector2`](Vector2.md) values corresponding to the transforms' `x`, `y`, and `origin`.

### TransformArray

```csharp
public Vector3[] TransformArray { get; set; }
```

引擎名：`transform_array`

Array containing each [`Transform3D`](Transform3D.md) value used by all instances of this mesh, as a [`Vector3`](Vector3.md)[]. Each transform is divided into 4 [`Vector3`](Vector3.md) values corresponding to the transforms' `x`, `y`, `z`, and `origin`.

### TransformFormat

```csharp
public MultiMesh.TransformFormatEnum TransformFormat { get; set; }
```

引擎名：`transform_format`

Format of transform used to transform mesh, either 2D or 3D.

### UseColors

```csharp
public bool UseColors { get; set; }
```

引擎名：`use_colors`

If `true`, the `MultiMesh` will use color data (see `SetInstanceColor()`). Can only be set when `InstanceCount` is `0` or less. This means that you need to call this method before setting the instance count, or temporarily reset it to `0`.

### UseCustomData

```csharp
public bool UseCustomData { get; set; }
```

引擎名：`use_custom_data`

If `true`, the `MultiMesh` will use custom data (see `SetInstanceCustomData()`). Can only be set when `InstanceCount` is `0` or less. This means that you need to call this method before setting the instance count, or temporarily reset it to `0`.

### VisibleInstanceCount

```csharp
public int VisibleInstanceCount { get; set; }
```

引擎名：`visible_instance_count`

Limits the number of instances drawn, -1 draws all instances. Changing this does not change the sizes of the buffers.

## 方法

### GetAabb

```csharp
public Aabb GetAabb()
```

引擎名：`get_aabb`

Returns the visibility axis-aligned bounding box in local space.

### GetBuffer

```csharp
public float[] GetBuffer()
```

引擎名：`get_buffer`

### GetCustomAabb

```csharp
public Aabb GetCustomAabb()
```

引擎名：`get_custom_aabb`

### GetInstanceColor

```csharp
public Color GetInstanceColor(int instance)
```

引擎名：`get_instance_color`

Gets a specific instance's color multiplier.

### GetInstanceCount

```csharp
public int GetInstanceCount()
```

引擎名：`get_instance_count`

### GetInstanceCustomData

```csharp
public Color GetInstanceCustomData(int instance)
```

引擎名：`get_instance_custom_data`

Returns the custom data that has been set for a specific instance.

### GetInstanceTransform

```csharp
public Transform3D GetInstanceTransform(int instance)
```

引擎名：`get_instance_transform`

Returns the [`Transform3D`](Transform3D.md) of a specific instance.

### GetInstanceTransform2D

```csharp
public Transform2D GetInstanceTransform2D(int instance)
```

Returns the [`Transform2D`](Transform2D.md) of a specific instance.

### GetMesh

```csharp
public Mesh GetMesh()
```

引擎名：`get_mesh`

### GetPhysicsInterpolationQuality

```csharp
public MultiMesh.PhysicsInterpolationQualityEnum GetPhysicsInterpolationQuality()
```

引擎名：`get_physics_interpolation_quality`

### GetTransformFormat

```csharp
public MultiMesh.TransformFormatEnum GetTransformFormat()
```

引擎名：`get_transform_format`

### GetVisibleInstanceCount

```csharp
public int GetVisibleInstanceCount()
```

引擎名：`get_visible_instance_count`

### IsUsingColors

```csharp
public bool IsUsingColors()
```

引擎名：`is_using_colors`

### IsUsingCustomData

```csharp
public bool IsUsingCustomData()
```

引擎名：`is_using_custom_data`

### ResetInstancePhysicsInterpolation

```csharp
public void ResetInstancePhysicsInterpolation(int instance)
```

引擎名：`reset_instance_physics_interpolation`

When using *physics interpolation*, this function allows you to prevent interpolation on an instance in the current physics tick.

This allows you to move instances instantaneously, and should usually be used when initially placing an instance such as a bullet to prevent graphical glitches.

### ResetInstancesPhysicsInterpolation

```csharp
public void ResetInstancesPhysicsInterpolation()
```

引擎名：`reset_instances_physics_interpolation`

When using *physics interpolation*, this function allows you to prevent interpolation for all instances in the current physics tick.

This allows you to move all instances instantaneously, and should usually be used when initially placing instances to prevent graphical glitches.

### SetBuffer

```csharp
public void SetBuffer(ReadOnlySpan<float> buffer)
```

引擎名：`set_buffer`

### SetBuffer

```csharp
public void SetBuffer(float[] buffer)
```

引擎名：`set_buffer`

### SetBufferInterpolated

```csharp
public void SetBufferInterpolated(ReadOnlySpan<float> bufferCurr, ReadOnlySpan<float> bufferPrev)
```

引擎名：`set_buffer_interpolated`

An alternative to setting the `Buffer` property, which can be used with *physics interpolation*. This method takes two arrays, and can set the data for the current and previous tick in one go. The renderer will automatically interpolate the data at each frame.

This is useful for situations where the order of instances may change from physics tick to tick, such as particle systems.

When the order of instances is coherent, the simpler alternative of setting `Buffer` can still be used with interpolation.

### SetBufferInterpolated

```csharp
public void SetBufferInterpolated(float[] bufferCurr, float[] bufferPrev)
```

引擎名：`set_buffer_interpolated`

An alternative to setting the `Buffer` property, which can be used with *physics interpolation*. This method takes two arrays, and can set the data for the current and previous tick in one go. The renderer will automatically interpolate the data at each frame.

This is useful for situations where the order of instances may change from physics tick to tick, such as particle systems.

When the order of instances is coherent, the simpler alternative of setting `Buffer` can still be used with interpolation.

### SetCustomAabb

```csharp
public void SetCustomAabb(Aabb aabb)
```

引擎名：`set_custom_aabb`

### SetInstanceColor

```csharp
public void SetInstanceColor(int instance, Color color)
```

引擎名：`set_instance_color`

Sets the color of a specific instance by *multiplying* the mesh's existing vertex colors. This allows for different color tinting per instance.

**Note:** Each component is stored in 32 bits in the Forward+ and Mobile rendering methods, but is packed into 16 bits in the Compatibility rendering method.

For the color to take effect, ensure that `UseColors` is `true` on the `MultiMesh` and [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) is `true` on the material. If you intend to set an absolute color instead of tinting, make sure the material's albedo color is set to pure white (`Color(1, 1, 1)`).

### SetInstanceCount

```csharp
public void SetInstanceCount(int count)
```

引擎名：`set_instance_count`

### SetInstanceCustomData

```csharp
public void SetInstanceCustomData(int instance, Color customData)
```

引擎名：`set_instance_custom_data`

Sets custom data for a specific instance. `customData` is a [`Color`](Color.md) type only to contain 4 floating-point numbers.

**Note:** Each number is stored in 32 bits in the Forward+ and Mobile rendering methods, but is packed into 16 bits in the Compatibility rendering method.

For the custom data to be used, ensure that `UseCustomData` is `true`.

This custom instance data has to be manually accessed in your custom shader using `INSTANCE_CUSTOM`.

### SetInstanceTransform

```csharp
public void SetInstanceTransform(int instance, Transform3D transform)
```

引擎名：`set_instance_transform`

Sets the [`Transform3D`](Transform3D.md) for a specific instance.

### SetInstanceTransform2D

```csharp
public void SetInstanceTransform2D(int instance, Transform2D transform)
```

Sets the [`Transform2D`](Transform2D.md) for a specific instance.

### SetMesh

```csharp
public void SetMesh(Mesh mesh)
```

引擎名：`set_mesh`

### SetPhysicsInterpolationQuality

```csharp
public void SetPhysicsInterpolationQuality(MultiMesh.PhysicsInterpolationQualityEnum quality)
```

引擎名：`set_physics_interpolation_quality`

### SetTransformFormat

```csharp
public void SetTransformFormat(MultiMesh.TransformFormatEnum format)
```

引擎名：`set_transform_format`

### SetUseColors

```csharp
public void SetUseColors(bool enable)
```

引擎名：`set_use_colors`

### SetUseCustomData

```csharp
public void SetUseCustomData(bool enable)
```

引擎名：`set_use_custom_data`

### SetVisibleInstanceCount

```csharp
public void SetVisibleInstanceCount(int count)
```

引擎名：`set_visible_instance_count`

## PhysicsInterpolationQualityEnum

命名空间 `Godot`；enum

### 枚举值

- `Fast = 0`：Always interpolate using Basis lerping, which can produce warping artifacts in some situations.
- `High = 1`：Attempt to interpolate using Basis slerping (spherical linear interpolation) where possible, otherwise fall back to lerping.

## TransformFormatEnum

命名空间 `Godot`；enum

### 枚举值

- `Transform2D = 0`：Use this when using 2D transforms.
- `Transform3D = 1`：Use this when using 3D transforms.
