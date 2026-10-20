<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPlaybackResampled

命名空间 `Godot`；class；继承 [`AudioStreamPlayback`](AudioStreamPlayback.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Playback class used to mix an [`AudioStream`](AudioStream.md)'s audio samples to [`AudioServer.GetMixRate`](AudioServer.md) using cubic interpolation.

## 构造函数

### AudioStreamPlaybackResampled

```csharp
public AudioStreamPlaybackResampled()
```

## 方法

### BeginResample

```csharp
public void BeginResample()
```

引擎名：`begin_resample`

Called when an [`AudioStream`](AudioStream.md) is played. Clears the cubic interpolation history and starts mixing by calling `_mix_resampled`.

### _GetStreamSamplingRate

```csharp
public virtual float _GetStreamSamplingRate()
```

引擎名：`_get_stream_sampling_rate`

Returns an [`AudioStream`](AudioStream.md)'s sample rate, in Hz. Used to perform resampling.
