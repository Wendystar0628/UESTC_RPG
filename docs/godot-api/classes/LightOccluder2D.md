<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LightOccluder2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Occludes light cast by a Light2D, casting shadows. The LightOccluder2D must be provided with an [`OccluderPolygon2D`](OccluderPolygon2D.md) in order for the shadow to be computed.

## 构造函数

### LightOccluder2D

```csharp
public LightOccluder2D()
```

## 属性

### Occluder

```csharp
public OccluderPolygon2D Occluder { get; set; }
```

引擎名：`occluder`

The [`OccluderPolygon2D`](OccluderPolygon2D.md) used to compute the shadow.

### OccluderLightMask

```csharp
public int OccluderLightMask { get; set; }
```

引擎名：`occluder_light_mask`

The LightOccluder2D's occluder light mask. The LightOccluder2D will cast shadows only from Light2D(s) that have the same light mask(s).

### SdfCollision

```csharp
public bool SdfCollision { get; set; }
```

引擎名：`sdf_collision`

If enabled, the occluder will be part of a real-time generated signed distance field that can be used in custom shaders.

## 方法

### GetOccluderLightMask

```csharp
public int GetOccluderLightMask()
```

引擎名：`get_occluder_light_mask`

### GetOccluderPolygon

```csharp
public OccluderPolygon2D GetOccluderPolygon()
```

引擎名：`get_occluder_polygon`

### IsSetAsSdfCollision

```csharp
public bool IsSetAsSdfCollision()
```

引擎名：`is_set_as_sdf_collision`

### SetAsSdfCollision

```csharp
public void SetAsSdfCollision(bool enable)
```

引擎名：`set_as_sdf_collision`

### SetOccluderLightMask

```csharp
public void SetOccluderLightMask(int mask)
```

引擎名：`set_occluder_light_mask`

### SetOccluderPolygon

```csharp
public void SetOccluderPolygon(OccluderPolygon2D polygon)
```

引擎名：`set_occluder_polygon`
