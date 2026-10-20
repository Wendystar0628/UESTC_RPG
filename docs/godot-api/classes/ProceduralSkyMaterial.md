<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ProceduralSkyMaterial

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`ProceduralSkyMaterial` provides a way to create an effective background quickly by defining procedural parameters for the sun, the sky and the ground. The sky and ground are defined by a main color, a color at the horizon, and an easing curve to interpolate between them. Suns are described by a position in the sky, a color, and a max angle from the sun at which the easing curve ends. The max angle therefore defines the size of the sun in the sky.

`ProceduralSkyMaterial` supports up to 4 suns, using the color, and energy, direction, and angular distance of the first four [`DirectionalLight3D`](DirectionalLight3D.md) nodes in the scene. This means that the suns are defined individually by the properties of their corresponding [`DirectionalLight3D`](DirectionalLight3D.md)s and globally by `SunAngleMax` and `SunCurve`.

`ProceduralSkyMaterial` uses a lightweight shader to draw the sky and is therefore suited for real-time updates. This makes it a great option for a sky that is simple and computationally cheap, but unrealistic. If you need a more realistic procedural option, use [`PhysicalSkyMaterial`](PhysicalSkyMaterial.md).

## 构造函数

### ProceduralSkyMaterial

```csharp
public ProceduralSkyMaterial()
```

## 属性

### EnergyMultiplier

```csharp
public float EnergyMultiplier { get; set; }
```

引擎名：`energy_multiplier`

The sky's overall brightness multiplier. Higher values result in a brighter sky.

### GroundBottomColor

```csharp
public Color GroundBottomColor { get; set; }
```

引擎名：`ground_bottom_color`

Color of the ground at the bottom. Blends with `GroundHorizonColor`.

### GroundCurve

```csharp
public float GroundCurve { get; set; }
```

引擎名：`ground_curve`

How quickly the `GroundHorizonColor` fades into the `GroundBottomColor`.

### GroundEnergyMultiplier

```csharp
public float GroundEnergyMultiplier { get; set; }
```

引擎名：`ground_energy_multiplier`

Multiplier for ground color. A higher value will make the ground brighter.

### GroundHorizonColor

```csharp
public Color GroundHorizonColor { get; set; }
```

引擎名：`ground_horizon_color`

Color of the ground at the horizon. Blends with `GroundBottomColor`.

### SkyCover

```csharp
public Texture2D SkyCover { get; set; }
```

引擎名：`sky_cover`

The sky cover texture to use. This texture must use an equirectangular projection (similar to [`PanoramaSkyMaterial`](PanoramaSkyMaterial.md)). The texture's colors will be *added* to the existing sky color, and will be multiplied by `SkyEnergyMultiplier` and `SkyCoverModulate`. This is mainly suited to displaying stars at night, but it can also be used to display clouds at day or night (with a non-physically-accurate look).

### SkyCoverModulate

```csharp
public Color SkyCoverModulate { get; set; }
```

引擎名：`sky_cover_modulate`

The tint to apply to the `SkyCover` texture. This can be used to change the sky cover's colors or opacity independently of the sky energy, which is useful for day/night or weather transitions. Only effective if a texture is defined in `SkyCover`.

### SkyCurve

```csharp
public float SkyCurve { get; set; }
```

引擎名：`sky_curve`

How quickly the `SkyHorizonColor` fades into the `SkyTopColor`.

### SkyEnergyMultiplier

```csharp
public float SkyEnergyMultiplier { get; set; }
```

引擎名：`sky_energy_multiplier`

Multiplier for sky color. A higher value will make the sky brighter.

### SkyHorizonColor

```csharp
public Color SkyHorizonColor { get; set; }
```

引擎名：`sky_horizon_color`

Color of the sky at the horizon. Blends with `SkyTopColor`.

### SkyTopColor

```csharp
public Color SkyTopColor { get; set; }
```

引擎名：`sky_top_color`

Color of the sky at the top. Blends with `SkyHorizonColor`.

### SunAngleMax

```csharp
public float SunAngleMax { get; set; }
```

引擎名：`sun_angle_max`

Distance from center of sun where it fades out completely.

### SunCurve

```csharp
public float SunCurve { get; set; }
```

引擎名：`sun_curve`

How quickly the sun fades away between the edge of the sun disk and `SunAngleMax`.

### UseDebanding

```csharp
public bool UseDebanding { get; set; }
```

引擎名：`use_debanding`

If `true`, enables debanding. Debanding adds a small amount of noise which helps reduce banding that appears from the smooth changes in color in the sky.

## 方法

### GetEnergyMultiplier

```csharp
public float GetEnergyMultiplier()
```

引擎名：`get_energy_multiplier`

### GetGroundBottomColor

```csharp
public Color GetGroundBottomColor()
```

引擎名：`get_ground_bottom_color`

### GetGroundCurve

```csharp
public float GetGroundCurve()
```

引擎名：`get_ground_curve`

### GetGroundEnergyMultiplier

```csharp
public float GetGroundEnergyMultiplier()
```

引擎名：`get_ground_energy_multiplier`

### GetGroundHorizonColor

```csharp
public Color GetGroundHorizonColor()
```

引擎名：`get_ground_horizon_color`

### GetSkyCover

```csharp
public Texture2D GetSkyCover()
```

引擎名：`get_sky_cover`

### GetSkyCoverModulate

```csharp
public Color GetSkyCoverModulate()
```

引擎名：`get_sky_cover_modulate`

### GetSkyCurve

```csharp
public float GetSkyCurve()
```

引擎名：`get_sky_curve`

### GetSkyEnergyMultiplier

```csharp
public float GetSkyEnergyMultiplier()
```

引擎名：`get_sky_energy_multiplier`

### GetSkyHorizonColor

```csharp
public Color GetSkyHorizonColor()
```

引擎名：`get_sky_horizon_color`

### GetSkyTopColor

```csharp
public Color GetSkyTopColor()
```

引擎名：`get_sky_top_color`

### GetSunAngleMax

```csharp
public float GetSunAngleMax()
```

引擎名：`get_sun_angle_max`

### GetSunCurve

```csharp
public float GetSunCurve()
```

引擎名：`get_sun_curve`

### GetUseDebanding

```csharp
public bool GetUseDebanding()
```

引擎名：`get_use_debanding`

### SetEnergyMultiplier

```csharp
public void SetEnergyMultiplier(float multiplier)
```

引擎名：`set_energy_multiplier`

### SetGroundBottomColor

```csharp
public void SetGroundBottomColor(Color color)
```

引擎名：`set_ground_bottom_color`

### SetGroundCurve

```csharp
public void SetGroundCurve(float curve)
```

引擎名：`set_ground_curve`

### SetGroundEnergyMultiplier

```csharp
public void SetGroundEnergyMultiplier(float energy)
```

引擎名：`set_ground_energy_multiplier`

### SetGroundHorizonColor

```csharp
public void SetGroundHorizonColor(Color color)
```

引擎名：`set_ground_horizon_color`

### SetSkyCover

```csharp
public void SetSkyCover(Texture2D skyCover)
```

引擎名：`set_sky_cover`

### SetSkyCoverModulate

```csharp
public void SetSkyCoverModulate(Color color)
```

引擎名：`set_sky_cover_modulate`

### SetSkyCurve

```csharp
public void SetSkyCurve(float curve)
```

引擎名：`set_sky_curve`

### SetSkyEnergyMultiplier

```csharp
public void SetSkyEnergyMultiplier(float multiplier)
```

引擎名：`set_sky_energy_multiplier`

### SetSkyHorizonColor

```csharp
public void SetSkyHorizonColor(Color color)
```

引擎名：`set_sky_horizon_color`

### SetSkyTopColor

```csharp
public void SetSkyTopColor(Color color)
```

引擎名：`set_sky_top_color`

### SetSunAngleMax

```csharp
public void SetSunAngleMax(float degrees)
```

引擎名：`set_sun_angle_max`

### SetSunCurve

```csharp
public void SetSunCurve(float curve)
```

引擎名：`set_sun_curve`

### SetUseDebanding

```csharp
public void SetUseDebanding(bool useDebanding)
```

引擎名：`set_use_debanding`
