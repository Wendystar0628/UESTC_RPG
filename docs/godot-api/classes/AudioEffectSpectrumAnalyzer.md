<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectSpectrumAnalyzer

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Calculates a Fourier Transform of the audio signal. This effect does not alter the audio. Can be used for creating real-time audio visualizations, like a spectrogram.

This resource configures an [`AudioEffectSpectrumAnalyzerInstance`](AudioEffectSpectrumAnalyzerInstance.md), which performs the actual analysis at runtime. An instance should be obtained with [`AudioServer.GetBusEffectInstance`](AudioServer.md) to make use of this effect.

## 构造函数

### AudioEffectSpectrumAnalyzer

```csharp
public AudioEffectSpectrumAnalyzer()
```

## 属性

### BufferLength

```csharp
public float BufferLength { get; set; }
```

引擎名：`buffer_length`

The length of the buffer to keep, in seconds. Higher values keep data around for longer, but require more memory. Value can range from 0.1 to 4.

### FftSize

```csharp
public AudioEffectSpectrumAnalyzer.FftSizeEnum FftSize { get; set; }
```

引擎名：`fft_size`

The size of the [Fast Fourier transform](https://en.wikipedia.org/wiki/Fast_Fourier_transform) buffer. Higher values smooth out the spectrum analysis over time, but have greater latency. The effects of this higher latency are especially noticeable with sudden amplitude changes.

## 方法

### GetBufferLength

```csharp
public float GetBufferLength()
```

引擎名：`get_buffer_length`

### GetFftSize

```csharp
public AudioEffectSpectrumAnalyzer.FftSizeEnum GetFftSize()
```

引擎名：`get_fft_size`

### GetTapBackPos

```csharp
public float GetTapBackPos()
```

### SetBufferLength

```csharp
public void SetBufferLength(float seconds)
```

引擎名：`set_buffer_length`

### SetFftSize

```csharp
public void SetFftSize(AudioEffectSpectrumAnalyzer.FftSizeEnum size)
```

引擎名：`set_fft_size`

### SetTapBackPos

```csharp
public void SetTapBackPos(float seconds)
```

## FftSizeEnum

命名空间 `Godot`；enum

### 枚举值

- `Size256 = 0`：Use a buffer of 256 samples for the Fast Fourier transform.
- `Size512 = 1`：Use a buffer of 512 samples for the Fast Fourier transform.
- `Size1024 = 2`：Use a buffer of 1024 samples for the Fast Fourier transform.
- `Size2048 = 3`：Use a buffer of 2048 samples for the Fast Fourier transform.
- `Size4096 = 4`：Use a buffer of 4096 samples for the Fast Fourier transform.
- `Max = 5`：Represents the size of the `AudioEffectSpectrumAnalyzer.FftSizeEnum` enum.
