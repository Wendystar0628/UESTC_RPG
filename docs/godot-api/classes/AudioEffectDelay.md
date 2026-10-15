<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectDelay

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "delay" effect plays the input audio signal back after a period of time. Each repetition is called a "delay tap" or simply "tap". Delay taps may be played back multiple times to create the sound of a repeating, decaying echo. Delay effects range from a subtle echo to a pronounced blending of previous sounds with new sounds.

See also [`AudioEffectReverb`](AudioEffectReverb.md) for a blurry, continuous echo.

## 构造函数

### AudioEffectDelay

```csharp
public AudioEffectDelay()
```

## 属性

### Dry

```csharp
public float Dry { get; set; }
```

引擎名：`dry`

The volume ratio of the original audio. Value can range from 0 to 1.

### FeedbackActive

```csharp
public bool FeedbackActive { get; set; }
```

引擎名：`feedback_active`

If `true`, feedback is enabled, repeating taps after they are played.

### FeedbackDelayMs

```csharp
public float FeedbackDelayMs { get; set; }
```

引擎名：`feedback_delay_ms`

Feedback delay time in milliseconds. Value can range from 0 to 1500.

### FeedbackLevelDb

```csharp
public float FeedbackLevelDb { get; set; }
```

引擎名：`feedback_level_db`

Gain for feedback, in dB. Value can range from -60 to 0.

### FeedbackLowpass

```csharp
public float FeedbackLowpass { get; set; }
```

引擎名：`feedback_lowpass`

Low-pass filter for feedback, in Hz. Frequencies above this value are filtered out. Value can range from 1 to 16000.

### Tap1Active

```csharp
public bool Tap1Active { get; set; }
```

引擎名：`tap1_active`

If `true`, the first tap will be enabled.

### Tap1DelayMs

```csharp
public float Tap1DelayMs { get; set; }
```

引擎名：`tap1_delay_ms`

First tap delay time in milliseconds, compared to the original audio. Value can range from 0 to 1500.

### Tap1LevelDb

```csharp
public float Tap1LevelDb { get; set; }
```

引擎名：`tap1_level_db`

Gain for the first tap, in dB. Value can range from -60 to 0.

### Tap1Pan

```csharp
public float Tap1Pan { get; set; }
```

引擎名：`tap1_pan`

Pan position for the first tap. Negative values pan the sound to the left, positive pan to the right. Value can range from -1 to 1.

### Tap2Active

```csharp
public bool Tap2Active { get; set; }
```

引擎名：`tap2_active`

If `true`, the second tap will be enabled.

### Tap2DelayMs

```csharp
public float Tap2DelayMs { get; set; }
```

引擎名：`tap2_delay_ms`

Second tap delay time in milliseconds, compared to the original audio. Value can range from 0 to 1500.

### Tap2LevelDb

```csharp
public float Tap2LevelDb { get; set; }
```

引擎名：`tap2_level_db`

Gain for the second tap, in dB. Value can range from -60 to 0.

### Tap2Pan

```csharp
public float Tap2Pan { get; set; }
```

引擎名：`tap2_pan`

Pan position for the second tap. Negative values pan the sound to the left, positive pan to the right. Value can range from -1 to 1.

## 方法

### GetDry

```csharp
public float GetDry()
```

引擎名：`get_dry`

### GetFeedbackDelayMs

```csharp
public float GetFeedbackDelayMs()
```

引擎名：`get_feedback_delay_ms`

### GetFeedbackLevelDb

```csharp
public float GetFeedbackLevelDb()
```

引擎名：`get_feedback_level_db`

### GetFeedbackLowpass

```csharp
public float GetFeedbackLowpass()
```

引擎名：`get_feedback_lowpass`

### GetTap1DelayMs

```csharp
public float GetTap1DelayMs()
```

引擎名：`get_tap1_delay_ms`

### GetTap1LevelDb

```csharp
public float GetTap1LevelDb()
```

引擎名：`get_tap1_level_db`

### GetTap1Pan

```csharp
public float GetTap1Pan()
```

引擎名：`get_tap1_pan`

### GetTap2DelayMs

```csharp
public float GetTap2DelayMs()
```

引擎名：`get_tap2_delay_ms`

### GetTap2LevelDb

```csharp
public float GetTap2LevelDb()
```

引擎名：`get_tap2_level_db`

### GetTap2Pan

```csharp
public float GetTap2Pan()
```

引擎名：`get_tap2_pan`

### IsFeedbackActive

```csharp
public bool IsFeedbackActive()
```

引擎名：`is_feedback_active`

### IsTap1Active

```csharp
public bool IsTap1Active()
```

引擎名：`is_tap1_active`

### IsTap2Active

```csharp
public bool IsTap2Active()
```

引擎名：`is_tap2_active`

### SetDry

```csharp
public void SetDry(float amount)
```

引擎名：`set_dry`

### SetFeedbackActive

```csharp
public void SetFeedbackActive(bool amount)
```

引擎名：`set_feedback_active`

### SetFeedbackDelayMs

```csharp
public void SetFeedbackDelayMs(float amount)
```

引擎名：`set_feedback_delay_ms`

### SetFeedbackLevelDb

```csharp
public void SetFeedbackLevelDb(float amount)
```

引擎名：`set_feedback_level_db`

### SetFeedbackLowpass

```csharp
public void SetFeedbackLowpass(float amount)
```

引擎名：`set_feedback_lowpass`

### SetTap1Active

```csharp
public void SetTap1Active(bool amount)
```

引擎名：`set_tap1_active`

### SetTap1DelayMs

```csharp
public void SetTap1DelayMs(float amount)
```

引擎名：`set_tap1_delay_ms`

### SetTap1LevelDb

```csharp
public void SetTap1LevelDb(float amount)
```

引擎名：`set_tap1_level_db`

### SetTap1Pan

```csharp
public void SetTap1Pan(float amount)
```

引擎名：`set_tap1_pan`

### SetTap2Active

```csharp
public void SetTap2Active(bool amount)
```

引擎名：`set_tap2_active`

### SetTap2DelayMs

```csharp
public void SetTap2DelayMs(float amount)
```

引擎名：`set_tap2_delay_ms`

### SetTap2LevelDb

```csharp
public void SetTap2LevelDb(float amount)
```

引擎名：`set_tap2_level_db`

### SetTap2Pan

```csharp
public void SetTap2Pan(float amount)
```

引擎名：`set_tap2_pan`
