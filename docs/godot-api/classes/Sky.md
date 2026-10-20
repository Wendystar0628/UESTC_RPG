<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Sky

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The `Sky` class uses a [`Material`](Material.md) to render a 3D environment's background and the light it emits by updating the reflection/radiance cubemaps.

## 构造函数

### Sky

```csharp
public Sky()
```

## 属性

### ProcessMode

```csharp
public Sky.ProcessModeEnum ProcessMode { get; set; }
```

引擎名：`process_mode`

The method for generating the radiance map from the sky. The radiance map is a cubemap with increasingly blurry versions of the sky corresponding to different levels of roughness. Radiance maps can be expensive to calculate.

### RadianceSize

```csharp
public Sky.RadianceSizeEnum RadianceSize { get; set; }
```

引擎名：`radiance_size`

The `Sky`'s radiance map size. The higher the radiance map size, the more detailed the lighting from the `Sky` will be.

**Note:** Some hardware will have trouble with higher radiance sizes, especially [`Sky.RadianceSizeEnum.Size512`](Sky.md) and above. Only use such high values on high-end hardware.

### SkyMaterial

```csharp
public Material SkyMaterial { get; set; }
```

引擎名：`sky_material`

[`Material`](Material.md) used to draw the background. Can be [`PanoramaSkyMaterial`](PanoramaSkyMaterial.md), [`ProceduralSkyMaterial`](ProceduralSkyMaterial.md), [`PhysicalSkyMaterial`](PhysicalSkyMaterial.md), or even a [`ShaderMaterial`](ShaderMaterial.md) if you want to use your own custom shader.

## 方法

### GetMaterial

```csharp
public Material GetMaterial()
```

引擎名：`get_material`

### GetProcessMode

```csharp
public Sky.ProcessModeEnum GetProcessMode()
```

引擎名：`get_process_mode`

### GetRadianceSize

```csharp
public Sky.RadianceSizeEnum GetRadianceSize()
```

引擎名：`get_radiance_size`

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

引擎名：`set_material`

### SetProcessMode

```csharp
public void SetProcessMode(Sky.ProcessModeEnum mode)
```

引擎名：`set_process_mode`

### SetRadianceSize

```csharp
public void SetRadianceSize(Sky.RadianceSizeEnum size)
```

引擎名：`set_radiance_size`

## ProcessModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Automatic = 0`：Automatically selects the appropriate process mode based on your sky shader.
- `Quality = 1`：Uses high quality importance sampling to process the radiance map.
- `Incremental = 2`：Uses the same high quality importance sampling to process the radiance map as `Quality`, but updates over several frames.
- `Realtime = 3`：Uses the fast filtering algorithm to process the radiance map.

## RadianceSizeEnum

命名空间 `Godot`；enum

### 枚举值

- `Size32 = 0`：Radiance texture size is 32×32 pixels.
- `Size64 = 1`：Radiance texture size is 64×64 pixels.
- `Size128 = 2`：Radiance texture size is 128×128 pixels.
- `Size256 = 3`：Radiance texture size is 256×256 pixels.
- `Size512 = 4`：Radiance texture size is 512×512 pixels.
- `Size1024 = 5`：Radiance texture size is 1024×1024 pixels.
- `Size2048 = 6`：Radiance texture size is 2048×2048 pixels.
- `Max = 7`：Represents the size of the `Sky.RadianceSizeEnum` enum.
