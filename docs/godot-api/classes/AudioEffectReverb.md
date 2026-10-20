<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectReverb

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "reverb" effect plays the input audio back continuously, decaying over a period of time. It simulates sounds in different kinds of spaces, ranging from small rooms, to big caverns.

See also [`AudioEffectDelay`](AudioEffectDelay.md) for a non-blurry type of echo.

## 构造函数

### AudioEffectReverb

```csharp
public AudioEffectReverb()
```

## 属性

### Damping

```csharp
public float Damping { get; set; }
```

引擎名：`damping`

Defines how reflective the imaginary room's walls are. The more reflective, the more high frequency content the reverb has. Value can range from 0 to 1.

### Dry

```csharp
public float Dry { get; set; }
```

引擎名：`dry`

The volume ratio of the original audio. At 0, only the modified audio is outputted. Value can range from 0 to 1.

### Hipass

```csharp
public float Hipass { get; set; }
```

引擎名：`hipass`

High-pass filter allows frequencies higher than a certain cutoff threshold and attenuates frequencies lower than the cutoff threshold. Value can range from 0 to 1.

### PredelayFeedback

```csharp
public float PredelayFeedback { get; set; }
```

引擎名：`predelay_feedback`

Gain of early reflection copies. At higher values, early reflection copies are louder and ring out for longer. Value can range from 0 to 1.

### PredelayMsec

```csharp
public float PredelayMsec { get; set; }
```

引擎名：`predelay_msec`

Time between the original audio and the early reflections of the reverb signal, in milliseconds. Value can range from 20 to 500.

### RoomSize

```csharp
public float RoomSize { get; set; }
```

引擎名：`room_size`

Dimensions of simulated room. Bigger means more echoes. Value can range from 0 to 1.

### Spread

```csharp
public float Spread { get; set; }
```

引擎名：`spread`

Widens or narrows the stereo image of the reverb tail. At 1, it fully widens. Value can range from 0 to 1.

### Wet

```csharp
public float Wet { get; set; }
```

引擎名：`wet`

The volume ratio of the modified audio. At 0, only the original audio is outputted. Value can range from 0 to 1.

## 方法

### GetDamping

```csharp
public float GetDamping()
```

引擎名：`get_damping`

### GetDry

```csharp
public float GetDry()
```

引擎名：`get_dry`

### GetHpf

```csharp
public float GetHpf()
```

引擎名：`get_hpf`

### GetPredelayFeedback

```csharp
public float GetPredelayFeedback()
```

引擎名：`get_predelay_feedback`

### GetPredelayMsec

```csharp
public float GetPredelayMsec()
```

引擎名：`get_predelay_msec`

### GetRoomSize

```csharp
public float GetRoomSize()
```

引擎名：`get_room_size`

### GetSpread

```csharp
public float GetSpread()
```

引擎名：`get_spread`

### GetWet

```csharp
public float GetWet()
```

引擎名：`get_wet`

### SetDamping

```csharp
public void SetDamping(float amount)
```

引擎名：`set_damping`

### SetDry

```csharp
public void SetDry(float amount)
```

引擎名：`set_dry`

### SetHpf

```csharp
public void SetHpf(float amount)
```

引擎名：`set_hpf`

### SetPredelayFeedback

```csharp
public void SetPredelayFeedback(float feedback)
```

引擎名：`set_predelay_feedback`

### SetPredelayMsec

```csharp
public void SetPredelayMsec(float msec)
```

引擎名：`set_predelay_msec`

### SetRoomSize

```csharp
public void SetRoomSize(float size)
```

引擎名：`set_room_size`

### SetSpread

```csharp
public void SetSpread(float amount)
```

引擎名：`set_spread`

### SetWet

```csharp
public void SetWet(float amount)
```

引擎名：`set_wet`
