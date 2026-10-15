<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamGenerator

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`AudioStreamGenerator` is a type of audio stream that does not play back sounds on its own; instead, it expects a script to generate audio data for it. See also [`AudioStreamGeneratorPlayback`](AudioStreamGeneratorPlayback.md).

Here's a sample on how to use it to generate a sine wave:

```csharp
[Export] public AudioStreamPlayer Player { get; set; }

private AudioStreamGeneratorPlayback _playback; // Will hold the AudioStreamGeneratorPlayback.
private float _sampleHz;
private float _pulseHz = 440.0f; // The frequency of the sound wave.
private double phase = 0.0;

public override void _Ready()
{
if (Player.Stream is AudioStreamGenerator generator) // Type as a generator to access MixRate.
{
_sampleHz = generator.MixRate;
Player.Play();
_playback = (AudioStreamGeneratorPlayback)Player.GetStreamPlayback();
FillBuffer();
}
}

public void FillBuffer()
{
float increment = _pulseHz / _sampleHz;
int framesAvailable = _playback.GetFramesAvailable();

for (int i = 0; i < framesAvailable; i++)
{
_playback.PushFrame(Vector2.One * (float)Mathf.Sin(phase * Mathf.Tau));
phase = Mathf.PosMod(phase + increment, 1.0);
}
}

```

In the example above, the "AudioStreamPlayer" node must use an `AudioStreamGenerator` as its stream. The `fill_buffer` function provides audio data for approximating a sine wave.

See also [`AudioEffectSpectrumAnalyzer`](AudioEffectSpectrumAnalyzer.md) for performing real-time audio spectrum analysis.

**Note:** Due to performance constraints, this class is best used from C# or from a compiled language via GDExtension. If you still want to use this class from GDScript, consider using a lower `MixRate` such as 11,025 Hz or 22,050 Hz.

## 构造函数

### AudioStreamGenerator

```csharp
public AudioStreamGenerator()
```

## 属性

### BufferLength

```csharp
public float BufferLength { get; set; }
```

引擎名：`buffer_length`

The length of the buffer to generate (in seconds). Lower values result in less latency, but require the script to generate audio data faster, resulting in increased CPU usage and more risk for audio cracking if the CPU can't keep up.

### MixRate

```csharp
public float MixRate { get; set; }
```

引擎名：`mix_rate`

The sample rate to use (in Hz). Higher values are more demanding for the CPU to generate, but result in better quality.

In games, common sample rates in use are `11025`, `16000`, `22050`, `32000`, `44100`, and `48000`.

According to the [Nyquist-Shannon sampling theorem](https://en.wikipedia.org/wiki/Nyquist%E2%80%93Shannon_sampling_theorem), there is no quality difference to human hearing when going past 40,000 Hz (since most humans can only hear up to ~20,000 Hz, often less). If you are generating lower-pitched sounds such as voices, lower sample rates such as `32000` or `22050` may be usable with no loss in quality.

**Note:** `AudioStreamGenerator` is not automatically resampling input data, to produce expected result `MixRateMode` should match the sampling rate of input data.

**Note:** If you are using [`AudioEffectCapture`](AudioEffectCapture.md) as the source of your data, set `MixRateMode` to [`AudioStreamGenerator.AudioStreamGeneratorMixRate.Input`](AudioStreamGenerator.md) or [`AudioStreamGenerator.AudioStreamGeneratorMixRate.Output`](AudioStreamGenerator.md) to automatically match current [`AudioServer`](AudioServer.md) mixing rate.

### MixRateMode

```csharp
public AudioStreamGenerator.AudioStreamGeneratorMixRate MixRateMode { get; set; }
```

引擎名：`mix_rate_mode`

Mixing rate mode. If set to [`AudioStreamGenerator.AudioStreamGeneratorMixRate.Custom`](AudioStreamGenerator.md), `MixRate` is used, otherwise current [`AudioServer`](AudioServer.md) mixing rate is used.

## 方法

### GetBufferLength

```csharp
public float GetBufferLength()
```

引擎名：`get_buffer_length`

### GetMixRate

```csharp
public float GetMixRate()
```

引擎名：`get_mix_rate`

### GetMixRateMode

```csharp
public AudioStreamGenerator.AudioStreamGeneratorMixRate GetMixRateMode()
```

引擎名：`get_mix_rate_mode`

### SetBufferLength

```csharp
public void SetBufferLength(float seconds)
```

引擎名：`set_buffer_length`

### SetMixRate

```csharp
public void SetMixRate(float hz)
```

引擎名：`set_mix_rate`

### SetMixRateMode

```csharp
public void SetMixRateMode(AudioStreamGenerator.AudioStreamGeneratorMixRate mode)
```

引擎名：`set_mix_rate_mode`

## AudioStreamGeneratorMixRate

命名空间 `Godot`；enum

### 枚举值

- `Output = 0`：Current [`AudioServer`](AudioServer.md) output mixing rate.
- `Input = 1`：Current [`AudioServer`](AudioServer.md) input mixing rate.
- `Custom = 2`：Custom mixing rate, specified by [`AudioStreamGenerator.MixRate`](AudioStreamGenerator.md).
- `Max = 3`：Maximum value for the mixing rate mode enum.
