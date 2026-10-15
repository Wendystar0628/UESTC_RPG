<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AreaLight3D

命名空间 `Godot`；class；继承 [`Light3D`](Light3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An area light is a type of [`Light3D`](Light3D.md) node that emits light over a two-dimensional area, in the shape of a rectangle. The light is attenuated throughout the distance. This attenuation can be configured by changing the energy, `AreaAttenuation`, and `AreaRange`.

Light is emitted in the -Z direction of the node's global basis. For an unrotated light, this means that the light is emitted forwards, illuminating the front side of a 3D model (see `Vector3.Forward` and `Vector3.ModelFront`).

Area lights can cast soft shadows using PCSS, which you can control by tweaking the size parameter. The shadow map is drawn from the center of the light.

**Note:** Area lights have limited support in the Mobile and Compatibility renderers. In the Mobile renderer, the size of the penumbra doesn't vary as it should with PCSS. In Compatibility, area lights cannot cast shadows.

**Warning:** Shadows cast by an area light may look incorrect if the object casting shadows doesn't have enough subdivisions and it's very close to the area light. This is the same limitation as the Dual Paraboloid shadow mode on an [`OmniLight3D`](OmniLight3D.md).

**Performance:** Area lights are more demanding on the GPU compared to omni and spot lights. In Forward+, there is an additional GPU cost on *all* rendered objects as soon as one area light is present in the view frustum (due to the nature of clustered lighting). Consider using them only for cinematics or when targeting high-end devices.

## 构造函数

### AreaLight3D

```csharp
public AreaLight3D()
```

## 属性

### AreaAttenuation

```csharp
public float AreaAttenuation { get; set; }
```

引擎名：`area_attenuation`

Controls the distance attenuation function for this area light.

A value of `0.0` will maintain a constant brightness through most of the range, but will smoothly attenuate the light at the edge of the range. Use a value of `2.0` for physically accurate lights as it results in the proper inverse square attenuation.

**Note:** Setting attenuation to `2.0` or higher may result in distant objects receiving minimal light, even when within range. For example, with a range of `4096`, an object at `100` units is attenuated by a factor of `0.0001`. With a default brightness of `1`, the light would not be visible at that distance.

**Note:** Using negative values or values higher than `10.0` may lead to unexpected results.

### AreaNormalizeEnergy

```csharp
public bool AreaNormalizeEnergy { get; set; }
```

引擎名：`area_normalize_energy`

Defines whether the energy is normalized (divided) by the surface area of the light. If set to `true`, changing the size does not affect the total energy output, and does not dramatically alter the brightness of the scene.

### AreaRange

```csharp
public float AreaRange { get; set; }
```

引擎名：`area_range`

The range of the area in meters. This determines the maximum distance from any point on the area at which the area can still emit light.

### AreaSize

```csharp
public Vector2 AreaSize { get; set; }
```

引擎名：`area_size`

The extents (width and height) of the area in meters.

### AreaTexture

```csharp
public Texture2D AreaTexture { get; set; }
```

引擎名：`area_texture`

An optional texture to use as a light source. Changing the texture at runtime might impact performance, as it needs to be drawn to the area light atlas with filtered mipmaps.

If no texture is assigned, the area light emits uniform light across its surface.

**Note:** Area light textures are only supported in the Forward+ and Mobile rendering methods, not Compatibility. To reduce the performance impact of switching textures at runtime, make sure each dimension of an area texture is either a multiple of 128 pixels, or a power of two. This removes the need for a scaling pass, which slows down texture changes. The textures don't necessarily have to be square to be optimal. Examples of optimal texture sizes include 32x64, 128x128, and 256x384.

## 方法

### GetAreaSize

```csharp
public Vector2 GetAreaSize()
```

引擎名：`get_area_size`

### GetAreaTexture

```csharp
public Texture2D GetAreaTexture()
```

引擎名：`get_area_texture`

### IsAreaNormalizingEnergy

```csharp
public bool IsAreaNormalizingEnergy()
```

引擎名：`is_area_normalizing_energy`

### SetAreaNormalizeEnergy

```csharp
public void SetAreaNormalizeEnergy(bool enable)
```

引擎名：`set_area_normalize_energy`

### SetAreaSize

```csharp
public void SetAreaSize(Vector2 areaSize)
```

引擎名：`set_area_size`

### SetAreaTexture

```csharp
public void SetAreaTexture(Texture2D texture)
```

引擎名：`set_area_texture`
