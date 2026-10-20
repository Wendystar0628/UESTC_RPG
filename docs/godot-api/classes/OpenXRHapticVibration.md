<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRHapticVibration

命名空间 `Godot`；class；继承 [`OpenXRHapticBase`](OpenXRHapticBase.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This haptic feedback resource makes it possible to define a vibration based haptic feedback pulse that can be triggered through actions in the OpenXR action map.

## 构造函数

### OpenXRHapticVibration

```csharp
public OpenXRHapticVibration()
```

## 属性

### Amplitude

```csharp
public float Amplitude { get; set; }
```

引擎名：`amplitude`

The amplitude of the pulse between `0.0` and `1.0`.

### Duration

```csharp
public long Duration { get; set; }
```

引擎名：`duration`

The duration of the pulse in nanoseconds. Use `-1` for a minimum duration pulse for the current XR runtime.

### Frequency

```csharp
public float Frequency { get; set; }
```

引擎名：`frequency`

The frequency of the pulse in Hz. `0.0` will let the XR runtime chose an optimal frequency for the device used.

## 方法

### GetAmplitude

```csharp
public float GetAmplitude()
```

引擎名：`get_amplitude`

### GetDuration

```csharp
public long GetDuration()
```

引擎名：`get_duration`

### GetFrequency

```csharp
public float GetFrequency()
```

引擎名：`get_frequency`

### SetAmplitude

```csharp
public void SetAmplitude(float amplitude)
```

引擎名：`set_amplitude`

### SetDuration

```csharp
public void SetDuration(long duration)
```

引擎名：`set_duration`

### SetFrequency

```csharp
public void SetFrequency(float frequency)
```

引擎名：`set_frequency`
