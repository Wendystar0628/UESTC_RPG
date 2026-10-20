<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectHardLimiter

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "limiter" disallows audio signals from exceeding a given volume threshold level in dB. Hard limiters predict volume peaks, and will smoothly apply gain reduction when a peak crosses the ceiling threshold level to prevent clipping. It preserves the waveform and prevents it from crossing the ceiling threshold level. Adding one in the Master bus is recommended as a safety measure to prevent sudden volume peaks from occurring, and to prevent distortion caused by clipping, when the volume exceeds 0 dB.

If clipping is desired, consider [`AudioEffectDistortion.ModeEnum.Clip`](AudioEffectDistortion.md).

## 构造函数

### AudioEffectHardLimiter

```csharp
public AudioEffectHardLimiter()
```

## 属性

### CeilingDb

```csharp
public float CeilingDb { get; set; }
```

引擎名：`ceiling_db`

The waveform's maximum allowed value, in dB. This value can range from -24 to 0.

The default value of -0.3 prevents potential inter-sample peaks (ISP) from crossing over 0 dB, which can cause slight distortion on some older hardware.

### PreGainDb

```csharp
public float PreGainDb { get; set; }
```

引擎名：`pre_gain_db`

Gain before limiting, in dB. Value can range from -24 to 24.

### Release

```csharp
public float Release { get; set; }
```

引擎名：`release`

Time it takes in seconds for the gain reduction to fully release. Value can range from 0.01 to 3.

## 方法

### GetCeilingDb

```csharp
public float GetCeilingDb()
```

引擎名：`get_ceiling_db`

### GetPreGainDb

```csharp
public float GetPreGainDb()
```

引擎名：`get_pre_gain_db`

### GetRelease

```csharp
public float GetRelease()
```

引擎名：`get_release`

### SetCeilingDb

```csharp
public void SetCeilingDb(float ceiling)
```

引擎名：`set_ceiling_db`

### SetPreGainDb

```csharp
public void SetPreGainDb(float preGain)
```

引擎名：`set_pre_gain_db`

### SetRelease

```csharp
public void SetRelease(float release)
```

引擎名：`set_release`
