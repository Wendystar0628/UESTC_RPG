<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDHitGroup

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Defines a hit group for use with [`RenderingDevice.RaytracingPipelineCreate`](RenderingDevice.md).

A hit group combines shaders that are executed when a ray intersects geometry. It may include a closest-hit shader, any-hit shader, and intersection shader.

Hit groups are referenced by index when populating hit shader binding tables using [`RenderingDevice.HitSbtRangeUpdate`](RenderingDevice.md).

## 构造函数

### RDHitGroup

```csharp
public RDHitGroup()
```

## 属性

### AnyHitShader

```csharp
public RDPipelineShader AnyHitShader { get; set; }
```

引擎名：`any_hit_shader`

Any-hit shader for this hit group. Executed for each potential intersection. Can be `null`.

### ClosestHitShader

```csharp
public RDPipelineShader ClosestHitShader { get; set; }
```

引擎名：`closest_hit_shader`

Closest-hit shader for this hit group. Executed for the closest intersection. Can be `null`.

### IntersectionShader

```csharp
public RDPipelineShader IntersectionShader { get; set; }
```

引擎名：`intersection_shader`

Intersection shader for this hit group. Required for non-triangle geometry. Must be `null` when using for triangle geometry.

## 方法

### GetAnyHitShader

```csharp
public RDPipelineShader GetAnyHitShader()
```

引擎名：`get_any_hit_shader`

### GetClosestHitShader

```csharp
public RDPipelineShader GetClosestHitShader()
```

引擎名：`get_closest_hit_shader`

### GetIntersectionShader

```csharp
public RDPipelineShader GetIntersectionShader()
```

引擎名：`get_intersection_shader`

### SetAnyHitShader

```csharp
public void SetAnyHitShader(RDPipelineShader pMember)
```

引擎名：`set_any_hit_shader`

### SetClosestHitShader

```csharp
public void SetClosestHitShader(RDPipelineShader pMember)
```

引擎名：`set_closest_hit_shader`

### SetIntersectionShader

```csharp
public void SetIntersectionShader(RDPipelineShader pMember)
```

引擎名：`set_intersection_shader`
