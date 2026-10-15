<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FastNoiseLite

命名空间 `Godot`；class；继承 [`Noise`](Noise.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class generates noise using the FastNoiseLite library, which is a collection of several noise algorithms including Cellular, Perlin, Value, and more.

Most generated noise values are in the range of `[-1, 1]`, but not always. Some of the cellular noise algorithms return results above `1`.

## 构造函数

### FastNoiseLite

```csharp
public FastNoiseLite()
```

## 属性

### CellularDistanceFunction

```csharp
public FastNoiseLite.CellularDistanceFunctionEnum CellularDistanceFunction { get; set; }
```

引擎名：`cellular_distance_function`

Determines how the distance to the nearest/second-nearest point is computed.

### CellularJitter

```csharp
public float CellularJitter { get; set; }
```

引擎名：`cellular_jitter`

Maximum distance a point can move off of its grid position. Set to `0` for an even grid.

### CellularReturnType

```csharp
public FastNoiseLite.CellularReturnTypeEnum CellularReturnType { get; set; }
```

引擎名：`cellular_return_type`

Return type from cellular noise calculations.

### DomainWarpAmplitude

```csharp
public float DomainWarpAmplitude { get; set; }
```

引擎名：`domain_warp_amplitude`

Sets the maximum warp distance from the origin.

### DomainWarpEnabled

```csharp
public bool DomainWarpEnabled { get; set; }
```

引擎名：`domain_warp_enabled`

If enabled, another FastNoiseLite instance is used to warp the space, resulting in a distortion of the noise.

### DomainWarpFractalGain

```csharp
public float DomainWarpFractalGain { get; set; }
```

引擎名：`domain_warp_fractal_gain`

Determines the strength of each subsequent layer of the noise which is used to warp the space.

A low value places more emphasis on the lower frequency base layers, while a high value puts more emphasis on the higher frequency layers.

### DomainWarpFractalLacunarity

```csharp
public float DomainWarpFractalLacunarity { get; set; }
```

引擎名：`domain_warp_fractal_lacunarity`

The change in frequency between octaves, also known as "lacunarity", of the fractal noise which warps the space. Increasing this value results in higher octaves, producing noise with finer details and a rougher appearance.

### DomainWarpFractalOctaves

```csharp
public int DomainWarpFractalOctaves { get; set; }
```

引擎名：`domain_warp_fractal_octaves`

The number of noise layers that are sampled to get the final value for the fractal noise which warps the space.

### DomainWarpFractalType

```csharp
public FastNoiseLite.DomainWarpFractalTypeEnum DomainWarpFractalType { get; set; }
```

引擎名：`domain_warp_fractal_type`

The method for combining octaves into a fractal which is used to warp the space.

### DomainWarpFrequency

```csharp
public float DomainWarpFrequency { get; set; }
```

引擎名：`domain_warp_frequency`

Frequency of the noise which warps the space. Low frequency results in smooth noise while high frequency results in rougher, more granular noise.

### DomainWarpType

```csharp
public FastNoiseLite.DomainWarpTypeEnum DomainWarpType { get; set; }
```

引擎名：`domain_warp_type`

The warp algorithm.

### FractalGain

```csharp
public float FractalGain { get; set; }
```

引擎名：`fractal_gain`

Determines the strength of each subsequent layer of noise in fractal noise.

A low value places more emphasis on the lower frequency base layers, while a high value puts more emphasis on the higher frequency layers.

### FractalLacunarity

```csharp
public float FractalLacunarity { get; set; }
```

引擎名：`fractal_lacunarity`

Frequency multiplier between subsequent octaves. Increasing this value results in higher octaves producing noise with finer details and a rougher appearance.

### FractalOctaves

```csharp
public int FractalOctaves { get; set; }
```

引擎名：`fractal_octaves`

The number of noise layers that are sampled to get the final value for fractal noise types.

### FractalPingPongStrength

```csharp
public float FractalPingPongStrength { get; set; }
```

引擎名：`fractal_ping_pong_strength`

Sets the strength of the fractal ping pong type.

### FractalType

```csharp
public FastNoiseLite.FractalTypeEnum FractalType { get; set; }
```

引擎名：`fractal_type`

The method for combining octaves into a fractal.

### FractalWeightedStrength

```csharp
public float FractalWeightedStrength { get; set; }
```

引擎名：`fractal_weighted_strength`

Higher weighting means higher octaves have less impact if lower octaves have a large impact.

### Frequency

```csharp
public float Frequency { get; set; }
```

引擎名：`frequency`

The frequency for all noise types. Low frequency results in smooth noise while high frequency results in rougher, more granular noise.

### NoiseType

```csharp
public FastNoiseLite.NoiseTypeEnum NoiseType { get; set; }
```

引擎名：`noise_type`

The noise algorithm used.

### Offset

```csharp
public Vector3 Offset { get; set; }
```

引擎名：`offset`

Translate the noise input coordinates by the given [`Vector3`](Vector3.md).

### Seed

```csharp
public int Seed { get; set; }
```

引擎名：`seed`

The random number seed for all noise types.

## 方法

### GetCellularDistanceFunction

```csharp
public FastNoiseLite.CellularDistanceFunctionEnum GetCellularDistanceFunction()
```

引擎名：`get_cellular_distance_function`

### GetCellularJitter

```csharp
public float GetCellularJitter()
```

引擎名：`get_cellular_jitter`

### GetCellularReturnType

```csharp
public FastNoiseLite.CellularReturnTypeEnum GetCellularReturnType()
```

引擎名：`get_cellular_return_type`

### GetDomainWarpAmplitude

```csharp
public float GetDomainWarpAmplitude()
```

引擎名：`get_domain_warp_amplitude`

### GetDomainWarpFractalGain

```csharp
public float GetDomainWarpFractalGain()
```

引擎名：`get_domain_warp_fractal_gain`

### GetDomainWarpFractalLacunarity

```csharp
public float GetDomainWarpFractalLacunarity()
```

引擎名：`get_domain_warp_fractal_lacunarity`

### GetDomainWarpFractalOctaves

```csharp
public int GetDomainWarpFractalOctaves()
```

引擎名：`get_domain_warp_fractal_octaves`

### GetDomainWarpFractalType

```csharp
public FastNoiseLite.DomainWarpFractalTypeEnum GetDomainWarpFractalType()
```

引擎名：`get_domain_warp_fractal_type`

### GetDomainWarpFrequency

```csharp
public float GetDomainWarpFrequency()
```

引擎名：`get_domain_warp_frequency`

### GetDomainWarpType

```csharp
public FastNoiseLite.DomainWarpTypeEnum GetDomainWarpType()
```

引擎名：`get_domain_warp_type`

### GetFractalGain

```csharp
public float GetFractalGain()
```

引擎名：`get_fractal_gain`

### GetFractalLacunarity

```csharp
public float GetFractalLacunarity()
```

引擎名：`get_fractal_lacunarity`

### GetFractalOctaves

```csharp
public int GetFractalOctaves()
```

引擎名：`get_fractal_octaves`

### GetFractalPingPongStrength

```csharp
public float GetFractalPingPongStrength()
```

引擎名：`get_fractal_ping_pong_strength`

### GetFractalType

```csharp
public FastNoiseLite.FractalTypeEnum GetFractalType()
```

引擎名：`get_fractal_type`

### GetFractalWeightedStrength

```csharp
public float GetFractalWeightedStrength()
```

引擎名：`get_fractal_weighted_strength`

### GetFrequency

```csharp
public float GetFrequency()
```

引擎名：`get_frequency`

### GetNoiseType

```csharp
public FastNoiseLite.NoiseTypeEnum GetNoiseType()
```

引擎名：`get_noise_type`

### GetOffset

```csharp
public Vector3 GetOffset()
```

引擎名：`get_offset`

### GetSeed

```csharp
public int GetSeed()
```

引擎名：`get_seed`

### IsDomainWarpEnabled

```csharp
public bool IsDomainWarpEnabled()
```

引擎名：`is_domain_warp_enabled`

### SetCellularDistanceFunction

```csharp
public void SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunctionEnum func)
```

引擎名：`set_cellular_distance_function`

### SetCellularJitter

```csharp
public void SetCellularJitter(float jitter)
```

引擎名：`set_cellular_jitter`

### SetCellularReturnType

```csharp
public void SetCellularReturnType(FastNoiseLite.CellularReturnTypeEnum ret)
```

引擎名：`set_cellular_return_type`

### SetDomainWarpAmplitude

```csharp
public void SetDomainWarpAmplitude(float domainWarpAmplitude)
```

引擎名：`set_domain_warp_amplitude`

### SetDomainWarpEnabled

```csharp
public void SetDomainWarpEnabled(bool domainWarpEnabled)
```

引擎名：`set_domain_warp_enabled`

### SetDomainWarpFractalGain

```csharp
public void SetDomainWarpFractalGain(float domainWarpGain)
```

引擎名：`set_domain_warp_fractal_gain`

### SetDomainWarpFractalLacunarity

```csharp
public void SetDomainWarpFractalLacunarity(float domainWarpLacunarity)
```

引擎名：`set_domain_warp_fractal_lacunarity`

### SetDomainWarpFractalOctaves

```csharp
public void SetDomainWarpFractalOctaves(int domainWarpOctaveCount)
```

引擎名：`set_domain_warp_fractal_octaves`

### SetDomainWarpFractalType

```csharp
public void SetDomainWarpFractalType(FastNoiseLite.DomainWarpFractalTypeEnum domainWarpFractalType)
```

引擎名：`set_domain_warp_fractal_type`

### SetDomainWarpFrequency

```csharp
public void SetDomainWarpFrequency(float domainWarpFrequency)
```

引擎名：`set_domain_warp_frequency`

### SetDomainWarpType

```csharp
public void SetDomainWarpType(FastNoiseLite.DomainWarpTypeEnum domainWarpType)
```

引擎名：`set_domain_warp_type`

### SetFractalGain

```csharp
public void SetFractalGain(float gain)
```

引擎名：`set_fractal_gain`

### SetFractalLacunarity

```csharp
public void SetFractalLacunarity(float lacunarity)
```

引擎名：`set_fractal_lacunarity`

### SetFractalOctaves

```csharp
public void SetFractalOctaves(int octaveCount)
```

引擎名：`set_fractal_octaves`

### SetFractalPingPongStrength

```csharp
public void SetFractalPingPongStrength(float pingPongStrength)
```

引擎名：`set_fractal_ping_pong_strength`

### SetFractalType

```csharp
public void SetFractalType(FastNoiseLite.FractalTypeEnum type)
```

引擎名：`set_fractal_type`

### SetFractalWeightedStrength

```csharp
public void SetFractalWeightedStrength(float weightedStrength)
```

引擎名：`set_fractal_weighted_strength`

### SetFrequency

```csharp
public void SetFrequency(float freq)
```

引擎名：`set_frequency`

### SetNoiseType

```csharp
public void SetNoiseType(FastNoiseLite.NoiseTypeEnum type)
```

引擎名：`set_noise_type`

### SetOffset

```csharp
public void SetOffset(Vector3 offset)
```

引擎名：`set_offset`

### SetSeed

```csharp
public void SetSeed(int seed)
```

引擎名：`set_seed`

## CellularDistanceFunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Euclidean = 0`：Euclidean distance to the nearest point.
- `EuclideanSquared = 1`：Squared Euclidean distance to the nearest point.
- `Manhattan = 2`：Manhattan distance (taxicab metric) to the nearest point.
- `Hybrid = 3`：Blend of `Euclidean` and `Manhattan` to give curved cell boundaries.

## CellularReturnTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `CellValue = 0`：The cellular distance function will return the same value for all points within a cell.
- `Distance = 1`：The cellular distance function will return a value determined by the distance to the nearest point.
- `Distance2 = 2`：The cellular distance function returns the distance to the second-nearest point.
- `Distance2Add = 3`：The distance to the nearest point is added to the distance to the second-nearest point.
- `Distance2Sub = 4`：The distance to the nearest point is subtracted from the distance to the second-nearest point.
- `Distance2Mul = 5`：The distance to the nearest point is multiplied with the distance to the second-nearest point.
- `Distance2Div = 6`：The distance to the nearest point is divided by the distance to the second-nearest point.

## DomainWarpFractalTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No fractal noise for warping the space.
- `Progressive = 1`：Warping the space progressively, octave for octave, resulting in a more "liquified" distortion.
- `Independent = 2`：Warping the space independently for each octave, resulting in a more chaotic distortion.

## DomainWarpTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Simplex = 0`：The domain is warped using the simplex noise algorithm.
- `SimplexReduced = 1`：The domain is warped using a simplified version of the simplex noise algorithm.
- `BasicGrid = 2`：The domain is warped using a simple noise grid (not as smooth as the other methods, but more performant).

## FractalTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No fractal noise.
- `Fbm = 1`：Method using Fractional Brownian Motion to combine octaves into a fractal.
- `Ridged = 2`：Method of combining octaves into a fractal resulting in a "ridged" look.
- `PingPong = 3`：Method of combining octaves into a fractal with a ping pong effect.

## NoiseTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Value = 5`：A lattice of points are assigned random values then interpolated based on neighboring values.
- `ValueCubic = 4`：Similar to value noise (`Value`), but slower.
- `Perlin = 3`：A lattice of random gradients.
- `Cellular = 2`：Cellular includes both Worley noise and Voronoi diagrams which creates various regions of the same value.
- `Simplex = 0`：As opposed to `Perlin`, gradients exist in a simplex lattice rather than a grid lattice, avoiding directional artifacts.
- `SimplexSmooth = 1`：Modified, higher quality version of `Simplex`, but slower.
