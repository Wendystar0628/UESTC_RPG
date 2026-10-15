<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicalSkyMaterial

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The `PhysicalSkyMaterial` uses the Preetham analytic daylight model to draw a sky based on physical properties. This results in a substantially more realistic sky than the [`ProceduralSkyMaterial`](ProceduralSkyMaterial.md), but it is slightly slower and less flexible.

The `PhysicalSkyMaterial` only supports one sun. The color, energy, and direction of the sun are taken from the first [`DirectionalLight3D`](DirectionalLight3D.md) in the scene tree.

## 构造函数

### PhysicalSkyMaterial

```csharp
public PhysicalSkyMaterial()
```

## 属性

### EnergyMultiplier

```csharp
public float EnergyMultiplier { get; set; }
```

引擎名：`energy_multiplier`

The sky's overall brightness multiplier. Higher values result in a brighter sky.

### GroundColor

```csharp
public Color GroundColor { get; set; }
```

引擎名：`ground_color`

Modulates the [`Color`](Color.md) on the bottom half of the sky to represent the ground.

### MieCoefficient

```csharp
public float MieCoefficient { get; set; }
```

引擎名：`mie_coefficient`

Controls the strength of [Mie scattering](https://en.wikipedia.org/wiki/Mie_scattering) for the sky. Mie scattering results from light colliding with larger particles (like water). On earth, Mie scattering results in a whitish color around the sun and horizon.

### MieColor

```csharp
public Color MieColor { get; set; }
```

引擎名：`mie_color`

Controls the [`Color`](Color.md) of the [Mie scattering](https://en.wikipedia.org/wiki/Mie_scattering) effect. While not physically accurate, this allows for the creation of alien-looking planets.

### MieEccentricity

```csharp
public float MieEccentricity { get; set; }
```

引擎名：`mie_eccentricity`

Controls the direction of the [Mie scattering](https://en.wikipedia.org/wiki/Mie_scattering). A value of `1` means that when light hits a particle it's passing through straight forward. A value of `-1` means that all light is scatter backwards.

### NightSky

```csharp
public Texture2D NightSky { get; set; }
```

引擎名：`night_sky`

[`Texture2D`](Texture2D.md) for the night sky. This is added to the sky, so if it is bright enough, it may be visible during the day.

### RayleighCoefficient

```csharp
public float RayleighCoefficient { get; set; }
```

引擎名：`rayleigh_coefficient`

Controls the strength of the [Rayleigh scattering](https://en.wikipedia.org/wiki/Rayleigh_scattering). Rayleigh scattering results from light colliding with small particles. It is responsible for the blue color of the sky.

### RayleighColor

```csharp
public Color RayleighColor { get; set; }
```

引擎名：`rayleigh_color`

Controls the [`Color`](Color.md) of the [Rayleigh scattering](https://en.wikipedia.org/wiki/Rayleigh_scattering). While not physically accurate, this allows for the creation of alien-looking planets. For example, setting this to a red [`Color`](Color.md) results in a Mars-looking atmosphere with a corresponding blue sunset.

### SunDiskScale

```csharp
public float SunDiskScale { get; set; }
```

引擎名：`sun_disk_scale`

Sets the size of the sun disk. Default value is based on Sol's perceived size from Earth.

### Turbidity

```csharp
public float Turbidity { get; set; }
```

引擎名：`turbidity`

Sets the thickness of the atmosphere. High turbidity creates a foggy-looking atmosphere, while a low turbidity results in a clearer atmosphere.

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

### GetGroundColor

```csharp
public Color GetGroundColor()
```

引擎名：`get_ground_color`

### GetMieCoefficient

```csharp
public float GetMieCoefficient()
```

引擎名：`get_mie_coefficient`

### GetMieColor

```csharp
public Color GetMieColor()
```

引擎名：`get_mie_color`

### GetMieEccentricity

```csharp
public float GetMieEccentricity()
```

引擎名：`get_mie_eccentricity`

### GetNightSky

```csharp
public Texture2D GetNightSky()
```

引擎名：`get_night_sky`

### GetRayleighCoefficient

```csharp
public float GetRayleighCoefficient()
```

引擎名：`get_rayleigh_coefficient`

### GetRayleighColor

```csharp
public Color GetRayleighColor()
```

引擎名：`get_rayleigh_color`

### GetSunDiskScale

```csharp
public float GetSunDiskScale()
```

引擎名：`get_sun_disk_scale`

### GetTurbidity

```csharp
public float GetTurbidity()
```

引擎名：`get_turbidity`

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

### SetGroundColor

```csharp
public void SetGroundColor(Color color)
```

引擎名：`set_ground_color`

### SetMieCoefficient

```csharp
public void SetMieCoefficient(float mie)
```

引擎名：`set_mie_coefficient`

### SetMieColor

```csharp
public void SetMieColor(Color color)
```

引擎名：`set_mie_color`

### SetMieEccentricity

```csharp
public void SetMieEccentricity(float eccentricity)
```

引擎名：`set_mie_eccentricity`

### SetNightSky

```csharp
public void SetNightSky(Texture2D nightSky)
```

引擎名：`set_night_sky`

### SetRayleighCoefficient

```csharp
public void SetRayleighCoefficient(float rayleigh)
```

引擎名：`set_rayleigh_coefficient`

### SetRayleighColor

```csharp
public void SetRayleighColor(Color color)
```

引擎名：`set_rayleigh_color`

### SetSunDiskScale

```csharp
public void SetSunDiskScale(float scale)
```

引擎名：`set_sun_disk_scale`

### SetTurbidity

```csharp
public void SetTurbidity(float turbidity)
```

引擎名：`set_turbidity`

### SetUseDebanding

```csharp
public void SetUseDebanding(bool useDebanding)
```

引擎名：`set_use_debanding`
