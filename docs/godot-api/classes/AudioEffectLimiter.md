<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectLimiter

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "limiter" is an audio effect designed to stop audio signals from exceeding a specified volume threshold level, and usually works by decreasing the volume or soft-clipping the audio. Adding one in the Master bus is always recommended to prevent clipping when the volume goes above 0 dB.

Soft clipping starts to decrease the peaks a little below the volume threshold level and progressively increases its effect as the input volume increases such that the threshold level is never exceeded.

If hard clipping is desired, consider [`AudioEffectDistortion.ModeEnum.Clip`](AudioEffectDistortion.md).

## 构造函数

### AudioEffectLimiter

```csharp
public AudioEffectLimiter()
```

## 属性

### CeilingDb

```csharp
public float CeilingDb { get; set; }
```

引擎名：`ceiling_db`

The waveform's maximum allowed value, in dB. Value can range from -20 to -0.1.

### SoftClipDb

```csharp
public float SoftClipDb { get; set; }
```

引擎名：`soft_clip_db`

Modifies the volume of the limited waves, in dB. Value can range from 0 to 6.

### SoftClipRatio

```csharp
public float SoftClipRatio { get; set; }
```

引擎名：`soft_clip_ratio`

This property has no effect on the audio. Use [`AudioEffectHardLimiter`](AudioEffectHardLimiter.md) instead, as this Limiter effect is deprecated.

### ThresholdDb

```csharp
public float ThresholdDb { get; set; }
```

引擎名：`threshold_db`

The volume threshold level from which the limiter begins to be active, in dB. Value can range from -30 to 0.

## 方法

### GetCeilingDb

```csharp
public float GetCeilingDb()
```

引擎名：`get_ceiling_db`

### GetSoftClipDb

```csharp
public float GetSoftClipDb()
```

引擎名：`get_soft_clip_db`

### GetSoftClipRatio

```csharp
public float GetSoftClipRatio()
```

引擎名：`get_soft_clip_ratio`

### GetThresholdDb

```csharp
public float GetThresholdDb()
```

引擎名：`get_threshold_db`

### SetCeilingDb

```csharp
public void SetCeilingDb(float ceiling)
```

引擎名：`set_ceiling_db`

### SetSoftClipDb

```csharp
public void SetSoftClipDb(float softClip)
```

引擎名：`set_soft_clip_db`

### SetSoftClipRatio

```csharp
public void SetSoftClipRatio(float softClip)
```

引擎名：`set_soft_clip_ratio`

### SetThresholdDb

```csharp
public void SetThresholdDb(float threshold)
```

引擎名：`set_threshold_db`
