<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectPhaser

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "phaser" effect creates a copy of the original audio that phase-rotates differently across the entire frequency spectrum, with the use of a series of all-pass filter stages (6 in this effect). This copy modulates with a low-frequency oscillator and combines with the original audio, resulting in peaks and troughs that sweep across the spectrum.

This effect can be used to create a "glassy" or "bubbly" sound.

## 构造函数

### AudioEffectPhaser

```csharp
public AudioEffectPhaser()
```

## 属性

### Depth

```csharp
public float Depth { get; set; }
```

引擎名：`depth`

Intensity of the effect. Value can range from 0.1 to 4.0.

### Feedback

```csharp
public float Feedback { get; set; }
```

引擎名：`feedback`

The volume ratio of the filtered audio that is fed back to the all-pass filters. The higher the value, the sharper and louder the peak filters created by the effect. Value can range from 0.1 to 0.9.

### RangeMaxHz

```csharp
public float RangeMaxHz { get; set; }
```

引擎名：`range_max_hz`

Determines the maximum frequency affected by the low-frequency oscillator modulations, in Hz. Value can range from 10 to 10000.

### RangeMinHz

```csharp
public float RangeMinHz { get; set; }
```

引擎名：`range_min_hz`

Determines the minimum frequency affected by the low-frequency oscillator modulations, in Hz. Value can range from 10 to 10000.

### RateHz

```csharp
public float RateHz { get; set; }
```

引擎名：`rate_hz`

Adjusts the rate in Hz at which the effect sweeps up and down across the frequency range. Value can range from 0.01 to 20.

## 方法

### GetDepth

```csharp
public float GetDepth()
```

引擎名：`get_depth`

### GetFeedback

```csharp
public float GetFeedback()
```

引擎名：`get_feedback`

### GetRangeMaxHz

```csharp
public float GetRangeMaxHz()
```

引擎名：`get_range_max_hz`

### GetRangeMinHz

```csharp
public float GetRangeMinHz()
```

引擎名：`get_range_min_hz`

### GetRateHz

```csharp
public float GetRateHz()
```

引擎名：`get_rate_hz`

### SetDepth

```csharp
public void SetDepth(float depth)
```

引擎名：`set_depth`

### SetFeedback

```csharp
public void SetFeedback(float fbk)
```

引擎名：`set_feedback`

### SetRangeMaxHz

```csharp
public void SetRangeMaxHz(float hz)
```

引擎名：`set_range_max_hz`

### SetRangeMinHz

```csharp
public void SetRangeMinHz(float hz)
```

引擎名：`set_range_min_hz`

### SetRateHz

```csharp
public void SetRateHz(float hz)
```

引擎名：`set_rate_hz`
