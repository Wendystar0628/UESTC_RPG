<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FogMaterial

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A [`Material`](Material.md) resource that can be used by [`FogVolume`](FogVolume.md)s to draw volumetric effects.

If you need more advanced effects, use a custom [fog shader]($DOCS_URL/tutorials/shaders/shader_reference/fog_shader.html).

## 构造函数

### FogMaterial

```csharp
public FogMaterial()
```

## 属性

### Albedo

```csharp
public Color Albedo { get; set; }
```

引擎名：`albedo`

The single-scattering [`Color`](Color.md) of the [`FogVolume`](FogVolume.md). Internally, `Albedo` is converted into single-scattering, which is additively blended with other [`FogVolume`](FogVolume.md)s and the [`Environment.VolumetricFogAlbedo`](Environment.md).

### Density

```csharp
public float Density { get; set; }
```

引擎名：`density`

The density of the [`FogVolume`](FogVolume.md). Denser objects are more opaque, but may suffer from under-sampling artifacts that look like stripes. Negative values can be used to subtract fog from other [`FogVolume`](FogVolume.md)s or global volumetric fog.

**Note:** Due to limited precision, `Density` values between `-0.001` and `0.001` (exclusive) act like `0.0`. This does not apply to [`Environment.VolumetricFogDensity`](Environment.md).

### DensityTexture

```csharp
public Texture3D DensityTexture { get; set; }
```

引擎名：`density_texture`

The 3D texture that is used to scale the `Density` of the [`FogVolume`](FogVolume.md). This can be used to vary fog density within the [`FogVolume`](FogVolume.md) with any kind of static pattern. For animated effects, consider using a custom [fog shader]($DOCS_URL/tutorials/shaders/shader_reference/fog_shader.html).

### EdgeFade

```csharp
public float EdgeFade { get; set; }
```

引擎名：`edge_fade`

The hardness of the edges of the [`FogVolume`](FogVolume.md). A higher value will result in softer edges, while a lower value will result in harder edges.

### Emission

```csharp
public Color Emission { get; set; }
```

引擎名：`emission`

The [`Color`](Color.md) of the light emitted by the [`FogVolume`](FogVolume.md). Emitted light will not cast light or shadows on other objects, but can be useful for modulating the [`Color`](Color.md) of the [`FogVolume`](FogVolume.md) independently from light sources.

### HeightFalloff

```csharp
public float HeightFalloff { get; set; }
```

引擎名：`height_falloff`

The rate by which the height-based fog decreases in density as height increases in world space. A high falloff will result in a sharp transition, while a low falloff will result in a smoother transition. A value of `0.0` results in uniform-density fog. The height threshold is determined by the height of the associated [`FogVolume`](FogVolume.md).

## 方法

### GetAlbedo

```csharp
public Color GetAlbedo()
```

引擎名：`get_albedo`

### GetDensity

```csharp
public float GetDensity()
```

引擎名：`get_density`

### GetDensityTexture

```csharp
public Texture3D GetDensityTexture()
```

引擎名：`get_density_texture`

### GetEdgeFade

```csharp
public float GetEdgeFade()
```

引擎名：`get_edge_fade`

### GetEmission

```csharp
public Color GetEmission()
```

引擎名：`get_emission`

### GetHeightFalloff

```csharp
public float GetHeightFalloff()
```

引擎名：`get_height_falloff`

### SetAlbedo

```csharp
public void SetAlbedo(Color albedo)
```

引擎名：`set_albedo`

### SetDensity

```csharp
public void SetDensity(float density)
```

引擎名：`set_density`

### SetDensityTexture

```csharp
public void SetDensityTexture(Texture3D densityTexture)
```

引擎名：`set_density_texture`

### SetEdgeFade

```csharp
public void SetEdgeFade(float edgeFade)
```

引擎名：`set_edge_fade`

### SetEmission

```csharp
public void SetEmission(Color emission)
```

引擎名：`set_emission`

### SetHeightFalloff

```csharp
public void SetHeightFalloff(float heightFalloff)
```

引擎名：`set_height_falloff`
