<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectCompressor

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "compressor" decreases the volume of sounds when it exceeds a certain volume threshold level.

A compressor can have many uses in a mix:

- To compress the whole volume in the Master bus (although an [`AudioEffectHardLimiter`](AudioEffectHardLimiter.md) is probably better).

- To ensure balance of voice audio clips.

- To sidechain, using another bus as a trigger. This decreases the volume of the bus it is attached to, by using the volume from another audio bus for threshold detection. This technique is common in video game mixing to decrease the volume of music and SFX while voices are being heard. This effect is also known as "ducking".

- To accentuate transients by using a long attack, letting sounds exceed the volume threshold level for a short period before compressing them. This can be used to make SFX more punchy.

## 构造函数

### AudioEffectCompressor

```csharp
public AudioEffectCompressor()
```

## 属性

### AttackUs

```csharp
public float AttackUs { get; set; }
```

引擎名：`attack_us`

Compressor's reaction time when the audio exceeds the volume threshold level, in microseconds. Value can range from 20 to 2000.

### Gain

```csharp
public float Gain { get; set; }
```

引擎名：`gain`

Gain of the audio signal, in dB. Value can range from -20 to 20.

### Mix

```csharp
public float Mix { get; set; }
```

引擎名：`mix`

Balance between the original audio and the compressed audio. Value can range from 0 (totally dry) to 1 (totally wet).

### Ratio

```csharp
public float Ratio { get; set; }
```

引擎名：`ratio`

Amount of compression applied to the audio once it passes the volume threshold level. The higher the ratio, the stronger the compression applied to audio signals that pass the volume threshold level. Value can range from 1 to 48.

### ReleaseMs

```csharp
public float ReleaseMs { get; set; }
```

引擎名：`release_ms`

Compressor's delay time to stop decreasing the volume after the it falls below the volume threshold level, in milliseconds. Value can range from 20 to 2000.

### Sidechain

```csharp
public StringName Sidechain { get; set; }
```

引擎名：`sidechain`

Audio bus to use for the volume threshold detection.

### Threshold

```csharp
public float Threshold { get; set; }
```

引擎名：`threshold`

The volume level above which compression is applied to the audio, in dB. Value can range from -60 to 0.

## 方法

### GetAttackUs

```csharp
public float GetAttackUs()
```

引擎名：`get_attack_us`

### GetGain

```csharp
public float GetGain()
```

引擎名：`get_gain`

### GetMix

```csharp
public float GetMix()
```

引擎名：`get_mix`

### GetRatio

```csharp
public float GetRatio()
```

引擎名：`get_ratio`

### GetReleaseMs

```csharp
public float GetReleaseMs()
```

引擎名：`get_release_ms`

### GetSidechain

```csharp
public StringName GetSidechain()
```

引擎名：`get_sidechain`

### GetThreshold

```csharp
public float GetThreshold()
```

引擎名：`get_threshold`

### SetAttackUs

```csharp
public void SetAttackUs(float attackUs)
```

引擎名：`set_attack_us`

### SetGain

```csharp
public void SetGain(float gain)
```

引擎名：`set_gain`

### SetMix

```csharp
public void SetMix(float mix)
```

引擎名：`set_mix`

### SetRatio

```csharp
public void SetRatio(float ratio)
```

引擎名：`set_ratio`

### SetReleaseMs

```csharp
public void SetReleaseMs(float releaseMs)
```

引擎名：`set_release_ms`

### SetSidechain

```csharp
public void SetSidechain(StringName sidechain)
```

引擎名：`set_sidechain`

### SetThreshold

```csharp
public void SetThreshold(float threshold)
```

引擎名：`set_threshold`
