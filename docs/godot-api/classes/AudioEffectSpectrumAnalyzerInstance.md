<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectSpectrumAnalyzerInstance

命名空间 `Godot`；class；继承 [`AudioEffectInstance`](AudioEffectInstance.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The runtime part of an [`AudioEffectSpectrumAnalyzer`](AudioEffectSpectrumAnalyzer.md), which can be used to query the magnitude of a frequency range on its host bus.

An instance of this class can be obtained with [`AudioServer.GetBusEffectInstance`](AudioServer.md).

## 方法

### GetMagnitudeForFrequencyRange

```csharp
public Vector2 GetMagnitudeForFrequencyRange(float fromHz, float toHz, AudioEffectSpectrumAnalyzerInstance.MagnitudeMode mode = AudioEffectSpectrumAnalyzerInstance.MagnitudeMode.Max)
```

引擎名：`get_magnitude_for_frequency_range`

Returns the magnitude of the frequencies from `fromHz` to `toHz` in linear energy as a Vector2. The `x` component of the return value represents the left stereo channel, and `y` represents the right channel.

`mode` determines how the frequency range will be processed.

## MagnitudeMode

命名空间 `Godot`；enum

### 枚举值

- `Average = 0`：Use the average value across the frequency range as magnitude.
- `Max = 1`：Use the maximum value of the frequency range as magnitude.
