<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectCapture

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Copies all audio frames, also known as "samples" or "audio samples", from the attached audio bus into its internal ring buffer. This effect does not alter the audio. Can be used for storing real-time audio data for playback, and for creating real-time audio visualizations, like an oscilloscope.

Application code should consume these audio frames from this ring buffer using `GetBuffer()` and process it as needed, for example to capture data from an [`AudioStreamMicrophone`](AudioStreamMicrophone.md), implement application-defined effects, or to transmit audio over the network. When capturing audio data from a microphone, the format of the samples will be stereo 32-bit floating-point PCM.

Unlike [`AudioEffectRecord`](AudioEffectRecord.md), this effect only returns the raw audio samples instead of encoding them into an [`AudioStream`](AudioStream.md).

## 构造函数

### AudioEffectCapture

```csharp
public AudioEffectCapture()
```

## 属性

### BufferLength

```csharp
public float BufferLength { get; set; }
```

引擎名：`buffer_length`

Length of the internal ring buffer, in seconds. Higher values keep data around for longer, but require more memory. Value can range from 0.01 to 10.

**Note:** Setting the buffer length will have no effect if already initialized.

## 方法

### CanGetBuffer

```csharp
public bool CanGetBuffer(int frames)
```

引擎名：`can_get_buffer`

Returns `true` if at least `frames` samples are available to read in the internal ring buffer.

### ClearBuffer

```csharp
public void ClearBuffer()
```

引擎名：`clear_buffer`

Clears the internal ring buffer.

**Note:** Calling this during a capture can cause the loss of samples which causes popping in the playback.

### GetBuffer

```csharp
public Vector2[] GetBuffer(int frames)
```

引擎名：`get_buffer`

Gets the next `frames` samples from the internal ring buffer.

Returns a [`Vector2`](Vector2.md)[] containing exactly `frames` samples if available, or an empty [`Vector2`](Vector2.md)[] if insufficient data was available.

The samples are signed floating-point PCM between `-1` and `1`. You will have to scale them if you want to use them as 8 or 16-bit integer samples. (`v = 0x7fff * samples[0].x`)

### GetBufferLength

```csharp
public float GetBufferLength()
```

引擎名：`get_buffer_length`

### GetBufferLengthFrames

```csharp
public int GetBufferLengthFrames()
```

引擎名：`get_buffer_length_frames`

Returns the total size of the internal ring buffer in number of samples.

### GetDiscardedFrames

```csharp
public long GetDiscardedFrames()
```

引擎名：`get_discarded_frames`

Returns the number of samples discarded from the audio bus due to full buffer.

### GetFramesAvailable

```csharp
public int GetFramesAvailable()
```

引擎名：`get_frames_available`

Returns the number of samples available to read using `GetBuffer()`.

### GetPushedFrames

```csharp
public long GetPushedFrames()
```

引擎名：`get_pushed_frames`

Returns the number of samples inserted from the audio bus.

### SetBufferLength

```csharp
public void SetBufferLength(float bufferLengthSeconds)
```

引擎名：`set_buffer_length`
