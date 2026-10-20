<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectChorus

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "chorus" effect creates multiple copies of the original audio (called "voices") with variations in pitch, and layers on top of the original, giving the impression that the sound comes from multiple sources. This creates spectral and spatial movement.

Each voice is played a short period of time after the original audio, controlled by `delay`. An internal low-frequency oscillator (LFO) controls their pitch, and `depth` controls the LFO's maximum amount.

In the real world, this kind of effect is found in pianos, choirs, and instrument ensembles.

This effect can also be used to widen mono audio and make digital sounds have a more natural or analog quality.

## 构造函数

### AudioEffectChorus

```csharp
public AudioEffectChorus()
```

## 属性

### Dry

```csharp
public float Dry { get; set; }
```

引擎名：`dry`

The volume ratio of the original audio. Value can range from 0 to 1.

### VoiceCount

```csharp
public int VoiceCount { get; set; }
```

引擎名：`voice_count`

The number of voices in the effect. Value can range from 1 to 4.

### Wet

```csharp
public float Wet { get; set; }
```

引擎名：`wet`

The volume ratio of all voices. Value can range from 0 to 1.

## 方法

### GetDry

```csharp
public float GetDry()
```

引擎名：`get_dry`

### GetVoiceCount

```csharp
public int GetVoiceCount()
```

引擎名：`get_voice_count`

### GetVoiceCutoffHz

```csharp
public float GetVoiceCutoffHz(int voiceIdx)
```

引擎名：`get_voice_cutoff_hz`

Returns the frequency threshold of a given `voiceIdx`'s low-pass filter in Hz. Frequencies above this value are removed from the voice.

### GetVoiceDelayMs

```csharp
public float GetVoiceDelayMs(int voiceIdx)
```

引擎名：`get_voice_delay_ms`

Returns the delay of a given `voiceIdx` in milliseconds, compared to the original audio.

### GetVoiceDepthMs

```csharp
public float GetVoiceDepthMs(int voiceIdx)
```

引擎名：`get_voice_depth_ms`

Returns the depth of a given `voiceIdx`'s low-frequency oscillator in milliseconds.

### GetVoiceLevelDb

```csharp
public float GetVoiceLevelDb(int voiceIdx)
```

引擎名：`get_voice_level_db`

Returns the gain of a given `voiceIdx` in dB.

### GetVoicePan

```csharp
public float GetVoicePan(int voiceIdx)
```

引擎名：`get_voice_pan`

Returns the pan position of a given `voiceIdx`. Negative values mean the left channel, positive mean the right.

### GetVoiceRateHz

```csharp
public float GetVoiceRateHz(int voiceIdx)
```

引擎名：`get_voice_rate_hz`

Returns the rate of a given `voiceIdx`'s low-frequency oscillator in Hz.

### GetWet

```csharp
public float GetWet()
```

引擎名：`get_wet`

### SetDry

```csharp
public void SetDry(float amount)
```

引擎名：`set_dry`

### SetVoiceCount

```csharp
public void SetVoiceCount(int voices)
```

引擎名：`set_voice_count`

### SetVoiceCutoffHz

```csharp
public void SetVoiceCutoffHz(int voiceIdx, float cutoffHz)
```

引擎名：`set_voice_cutoff_hz`

Sets the frequency threshold of a given `voiceIdx`'s low-pass filter in Hz. Frequencies above `cutoffHz` are removed from `voiceIdx`. Value can range from 1 to 20500.

### SetVoiceDelayMs

```csharp
public void SetVoiceDelayMs(int voiceIdx, float delayMs)
```

引擎名：`set_voice_delay_ms`

Sets the delay of a given `voiceIdx` in milliseconds, compared to the original audio. Value can range from 0 to 50.

### SetVoiceDepthMs

```csharp
public void SetVoiceDepthMs(int voiceIdx, float depthMs)
```

引擎名：`set_voice_depth_ms`

Sets the depth of a given `voiceIdx`'s low-frequency oscillator in milliseconds. Value can range from 0 to 20.

### SetVoiceLevelDb

```csharp
public void SetVoiceLevelDb(int voiceIdx, float levelDb)
```

引擎名：`set_voice_level_db`

Sets the gain of a given `voiceIdx` in dB. Value can range from -60 to 24.

### SetVoicePan

```csharp
public void SetVoicePan(int voiceIdx, float pan)
```

引擎名：`set_voice_pan`

Sets the pan position of a given `voiceIdx`. Negative values pan the sound to the left, positive pan to the right. Value can range from -1 to 1.

### SetVoiceRateHz

```csharp
public void SetVoiceRateHz(int voiceIdx, float rateHz)
```

引擎名：`set_voice_rate_hz`

Sets the rate of a given `voiceIdx`'s low-frequency oscillator in Hz. Value can range from 0.1 to 20.

### SetWet

```csharp
public void SetWet(float amount)
```

引擎名：`set_wet`
