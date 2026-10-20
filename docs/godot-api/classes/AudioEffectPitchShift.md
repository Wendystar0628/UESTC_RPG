<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectPitchShift

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Allows modulation of pitch without modifying speed. All frequencies can be raised or lowered with minimal effect on transients.

## 构造函数

### AudioEffectPitchShift

```csharp
public AudioEffectPitchShift()
```

## 属性

### FftSize

```csharp
public AudioEffectPitchShift.FftSizeEnum FftSize { get; set; }
```

引擎名：`fft_size`

The size of the [Fast Fourier transform](https://en.wikipedia.org/wiki/Fast_Fourier_transform) buffer. Higher values smooth out the effect over time, but have greater latency. The effects of this higher latency are especially noticeable on audio signals that have sudden amplitude changes.

### Oversampling

```csharp
public int Oversampling { get; set; }
```

引擎名：`oversampling`

The oversampling factor to use. Higher values result in better quality, but are more demanding on the CPU and may cause audio cracking if the CPU can't keep up.

### PitchScale

```csharp
public float PitchScale { get; set; }
```

引擎名：`pitch_scale`

The pitch scale to use. `1.0` is the default pitch and plays sounds unaffected. `PitchScale` can range from 0 (infinitely low pitch, inaudible) to 16 (16 times higher than the initial pitch).

## 方法

### GetFftSize

```csharp
public AudioEffectPitchShift.FftSizeEnum GetFftSize()
```

引擎名：`get_fft_size`

### GetOversampling

```csharp
public int GetOversampling()
```

引擎名：`get_oversampling`

### GetPitchScale

```csharp
public float GetPitchScale()
```

引擎名：`get_pitch_scale`

### SetFftSize

```csharp
public void SetFftSize(AudioEffectPitchShift.FftSizeEnum size)
```

引擎名：`set_fft_size`

### SetOversampling

```csharp
public void SetOversampling(int amount)
```

引擎名：`set_oversampling`

### SetPitchScale

```csharp
public void SetPitchScale(float rate)
```

引擎名：`set_pitch_scale`

## FftSizeEnum

命名空间 `Godot`；enum

### 枚举值

- `Size256 = 0`：Use a buffer of 256 samples for the Fast Fourier transform.
- `Size512 = 1`：Use a buffer of 512 samples for the Fast Fourier transform.
- `Size1024 = 2`：Use a buffer of 1024 samples for the Fast Fourier transform.
- `Size2048 = 3`：Use a buffer of 2048 samples for the Fast Fourier transform.
- `Size4096 = 4`：Use a buffer of 4096 samples for the Fast Fourier transform.
- `Max = 5`：Represents the size of the `AudioEffectPitchShift.FftSizeEnum` enum.
