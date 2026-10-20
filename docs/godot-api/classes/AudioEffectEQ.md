<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectEQ

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An "equalizer" gives you control over the gain of frequencies in the entire spectrum, by allowing their adjustment through bands. A band is a point in the frequency spectrum, and each band means a division of the spectrum that can be adjusted.

Use equalizers to compensate for existing deficiencies in the audio, make room for other elements, or remove undesirable frequencies. AudioEffectEQs are useful on the Master bus to balance the entire mix or give it more character. They are also useful when a game is run on a mobile device, to adjust the mix to that kind of speakers (it can be disabled when headphones are plugged in).

## 构造函数

### AudioEffectEQ

```csharp
public AudioEffectEQ()
```

## 方法

### GetBandCount

```csharp
public int GetBandCount()
```

引擎名：`get_band_count`

Returns the number of bands of the equalizer.

### GetBandGainDb

```csharp
public float GetBandGainDb(int bandIdx)
```

引擎名：`get_band_gain_db`

Returns the band's gain at the specified index, in dB.

### SetBandGainDb

```csharp
public void SetBandGainDb(int bandIdx, float volumeDb)
```

引擎名：`set_band_gain_db`

Sets band's gain at the specified index, in dB.
