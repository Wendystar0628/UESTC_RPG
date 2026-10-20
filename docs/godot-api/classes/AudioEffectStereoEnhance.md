<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectStereoEnhance

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Adjusts gain of the left and right channels, and makes mono sounds stereo through phase shifting.

## 构造函数

### AudioEffectStereoEnhance

```csharp
public AudioEffectStereoEnhance()
```

## 属性

### PanPullout

```csharp
public float PanPullout { get; set; }
```

引擎名：`pan_pullout`

Gain of the side channels, if they exist. A value of 0 will downmix stereo to mono. Value can range from 0 to 4.

### Surround

```csharp
public float Surround { get; set; }
```

引擎名：`surround`

Widens the stereo image through phase shifting in conjunction with `TimePulloutMs`. Just pans sound to the left channel if `TimePulloutMs` is 0. Value can range from 0 to 1.

### TimePulloutMs

```csharp
public float TimePulloutMs { get; set; }
```

引擎名：`time_pullout_ms`

Widens the stereo image through phase shifting in conjunction with `Surround`. Just delays the right channel if `Surround` is 0. Value is in milliseconds, and can range from 0 to 50.

## 方法

### GetPanPullout

```csharp
public float GetPanPullout()
```

引擎名：`get_pan_pullout`

### GetSurround

```csharp
public float GetSurround()
```

引擎名：`get_surround`

### GetTimePullout

```csharp
public float GetTimePullout()
```

引擎名：`get_time_pullout`

### SetPanPullout

```csharp
public void SetPanPullout(float amount)
```

引擎名：`set_pan_pullout`

### SetSurround

```csharp
public void SetSurround(float amount)
```

引擎名：`set_surround`

### SetTimePullout

```csharp
public void SetTimePullout(float amount)
```

引擎名：`set_time_pullout`
