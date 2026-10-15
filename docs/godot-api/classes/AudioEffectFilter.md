<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectFilter

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "filter" controls the gain of frequencies, using `CutoffHz` as a frequency threshold. Filters can help to give room for each sound, and create interesting effects.

There are different types of filter that inherit this class:

Shelf filters: [`AudioEffectLowShelfFilter`](AudioEffectLowShelfFilter.md) and [`AudioEffectHighShelfFilter`](AudioEffectHighShelfFilter.md)

Band-pass and notch filters: [`AudioEffectBandPassFilter`](AudioEffectBandPassFilter.md), [`AudioEffectBandLimitFilter`](AudioEffectBandLimitFilter.md), and [`AudioEffectNotchFilter`](AudioEffectNotchFilter.md)

Low/high-pass filters: [`AudioEffectLowPassFilter`](AudioEffectLowPassFilter.md) and [`AudioEffectHighPassFilter`](AudioEffectHighPassFilter.md)

## 构造函数

### AudioEffectFilter

```csharp
public AudioEffectFilter()
```

## 属性

### CutoffHz

```csharp
public float CutoffHz { get; set; }
```

引擎名：`cutoff_hz`

Frequency threshold for the filter, in Hz. Value can range from 1 to 20500.

### Db

```csharp
public AudioEffectFilter.FilterDB Db { get; set; }
```

引擎名：`db`

Steepness of the cutoff curve in dB per octave (twice the frequency above `CutoffHz`, or half the frequency below `CutoffHz`), also known as the "order" of the filter. Higher orders have a more aggressive cutoff.

### Gain

```csharp
public float Gain { get; set; }
```

引擎名：`gain`

Gain of the frequencies affected by the filter. This property is only available for [`AudioEffectLowShelfFilter`](AudioEffectLowShelfFilter.md) and [`AudioEffectHighShelfFilter`](AudioEffectHighShelfFilter.md). Value can range from 0 to 4.

### Resonance

```csharp
public float Resonance { get; set; }
```

引擎名：`resonance`

Gain at or directly next to the `CutoffHz` frequency threshold. Value can range from 0 to 1.

Its exact behavior depends on the selected filter type:

- For shelf filters, it accentuates or masks the order by increasing frequencies right next to the `CutoffHz` frequency and decreasing frequencies on the opposite side.

- For the band-pass and notch filters, it widens or narrows the filter at the `CutoffHz` frequency threshold.

- For low/high-pass filters, it increases or decreases frequencies at the `CutoffHz` frequency threshold.

## 方法

### GetCutoff

```csharp
public float GetCutoff()
```

引擎名：`get_cutoff`

### GetDb

```csharp
public AudioEffectFilter.FilterDB GetDb()
```

引擎名：`get_db`

### GetGain

```csharp
public float GetGain()
```

引擎名：`get_gain`

### GetResonance

```csharp
public float GetResonance()
```

引擎名：`get_resonance`

### SetCutoff

```csharp
public void SetCutoff(float freq)
```

引擎名：`set_cutoff`

### SetDb

```csharp
public void SetDb(AudioEffectFilter.FilterDB amount)
```

引擎名：`set_db`

### SetGain

```csharp
public void SetGain(float amount)
```

引擎名：`set_gain`

### SetResonance

```csharp
public void SetResonance(float amount)
```

引擎名：`set_resonance`

## FilterDB

命名空间 `Godot`；enum

### 枚举值

- `Filter6Db = 0`：Cutting off at 6 dB per octave.
- `Filter12Db = 1`：Cutting off at 12 dB per octave.
- `Filter18Db = 2`：Cutting off at 18 dB per octave.
- `Filter24Db = 3`：Cutting off at 24 dB per octave.
