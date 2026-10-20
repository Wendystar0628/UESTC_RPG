<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectDistortion

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "distortion" effect modifies the waveform via a nonlinear mathematical function (see available ones in [`AudioEffectDistortion.ModeEnum`](AudioEffectDistortion.md)), based on the amplitude of the waveform's samples.

**Note:** In a nonlinear function, an input sample at *x* amplitude value, will either have its amplitude increased or decreased to a *y* value, based on the function value at *x*, which is why even at the same `Drive`, the output sound will vary depending on the input's volume. To change the volume while maintaining the output waveform, use `PostGain`.

In this effect, each type is a different nonlinear function. The different types available are: clip, atan, lofi (bitcrush), overdrive, and waveshape. Every distortion type available here is symmetric: negative amplitude values are affected the same way as positive ones.

Although distortion will always change frequency content, usually by introducing high harmonics, different distortion types offer a range of sound qualities; from "soft" and "warm", to "crunchy" and "abrasive".

For games, it can help simulate sound coming from some saturated device or speaker very efficiently. It can also help the audio stand out in a mix, by introducing higher frequencies and increasing the volume.

**Note:** Although usually imperceptible, an enabled distortion effect still changes the sound even when `Drive` is set to 0. This is not a bug. If this behavior is undesirable, consider disabling the effect using [`AudioServer.SetBusEffectEnabled`](AudioServer.md).

## 构造函数

### AudioEffectDistortion

```csharp
public AudioEffectDistortion()
```

## 属性

### Drive

```csharp
public float Drive { get; set; }
```

引擎名：`drive`

Distortion intensity. Controls how much of the input audio is affected by the distortion curve by moving from a linear function to a nonlinear one. Value can range from 0 to 1.

### KeepHfHz

```csharp
public float KeepHfHz { get; set; }
```

引擎名：`keep_hf_hz`

High-pass filter, in Hz. Frequencies higher than this value will not be affected by the distortion. Value can range from 1 to 20000.

### Mode

```csharp
public AudioEffectDistortion.ModeEnum Mode { get; set; }
```

引擎名：`mode`

Distortion type. Changes the nonlinear function used to distort the waveform. See [`AudioEffectDistortion.ModeEnum`](AudioEffectDistortion.md).

### PostGain

```csharp
public float PostGain { get; set; }
```

引擎名：`post_gain`

Gain after the effect, in dB. Value can range from -80 to 24.

### PreGain

```csharp
public float PreGain { get; set; }
```

引擎名：`pre_gain`

Gain before the effect, in dB. Value can range from -60 to 60.

## 方法

### GetDrive

```csharp
public float GetDrive()
```

引擎名：`get_drive`

### GetKeepHfHz

```csharp
public float GetKeepHfHz()
```

引擎名：`get_keep_hf_hz`

### GetMode

```csharp
public AudioEffectDistortion.ModeEnum GetMode()
```

引擎名：`get_mode`

### GetPostGain

```csharp
public float GetPostGain()
```

引擎名：`get_post_gain`

### GetPreGain

```csharp
public float GetPreGain()
```

引擎名：`get_pre_gain`

### SetDrive

```csharp
public void SetDrive(float drive)
```

引擎名：`set_drive`

### SetKeepHfHz

```csharp
public void SetKeepHfHz(float keepHfHz)
```

引擎名：`set_keep_hf_hz`

### SetMode

```csharp
public void SetMode(AudioEffectDistortion.ModeEnum mode)
```

引擎名：`set_mode`

### SetPostGain

```csharp
public void SetPostGain(float postGain)
```

引擎名：`set_post_gain`

### SetPreGain

```csharp
public void SetPreGain(float preGain)
```

引擎名：`set_pre_gain`

## ModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Clip = 0`：Flattens the waveform at 0 dB in a sharp manner.
- `Atan = 1`：Flattens the waveform in a smooth manner, following an arctangent curve.
- `Lofi = 2`：Decreases audio bit depth to achieve a low-resolution audio signal, going from 16-bit to 2-bit.
- `Overdrive = 3`：Emulates the warm distortion produced by a field effect transistor, which is commonly used in solid-state musical instrument amplifiers.
- `Waveshape = 4`：Flattens the waveform in a smooth manner, until it reaches a sharp peak at `drive = 1`, following a generic absolute sigmoid function.
