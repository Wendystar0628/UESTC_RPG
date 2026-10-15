<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PanoramaSkyMaterial

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource referenced in a [`Sky`](Sky.md) that is used to draw a background. `PanoramaSkyMaterial` functions similar to skyboxes in other engines, except it uses an equirectangular sky map instead of a [`Cubemap`](Cubemap.md).

Using an HDR panorama is strongly recommended for accurate, high-quality reflections. Godot supports the Radiance HDR (`.hdr`) and OpenEXR (`.exr`) image formats for this purpose.

You can use [this tool](https://danilw.github.io/GLSL-howto/cubemap_to_panorama_js/cubemap_to_panorama.html) to convert a cubemap to an equirectangular sky map.

## 构造函数

### PanoramaSkyMaterial

```csharp
public PanoramaSkyMaterial()
```

## 属性

### EnergyMultiplier

```csharp
public float EnergyMultiplier { get; set; }
```

引擎名：`energy_multiplier`

The sky's overall brightness multiplier. Higher values result in a brighter sky.

### Filter

```csharp
public bool Filter { get; set; }
```

引擎名：`filter`

A boolean value to determine if the background texture should be filtered or not.

### Panorama

```csharp
public Texture2D Panorama { get; set; }
```

引擎名：`panorama`

[`Texture2D`](Texture2D.md) to be applied to the `PanoramaSkyMaterial`.

## 方法

### GetEnergyMultiplier

```csharp
public float GetEnergyMultiplier()
```

引擎名：`get_energy_multiplier`

### GetPanorama

```csharp
public Texture2D GetPanorama()
```

引擎名：`get_panorama`

### IsFilteringEnabled

```csharp
public bool IsFilteringEnabled()
```

引擎名：`is_filtering_enabled`

### SetEnergyMultiplier

```csharp
public void SetEnergyMultiplier(float multiplier)
```

引擎名：`set_energy_multiplier`

### SetFilteringEnabled

```csharp
public void SetFilteringEnabled(bool enabled)
```

引擎名：`set_filtering_enabled`

### SetPanorama

```csharp
public void SetPanorama(Texture2D texture)
```

引擎名：`set_panorama`
