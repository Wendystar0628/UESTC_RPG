<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamGeneratorPlayback

命名空间 `Godot`；class；继承 [`AudioStreamPlaybackResampled`](AudioStreamPlaybackResampled.md) → [`AudioStreamPlayback`](AudioStreamPlayback.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is meant to be used with [`AudioStreamGenerator`](AudioStreamGenerator.md) to play back the generated audio in real-time.

## 方法

### CanPushBuffer

```csharp
public bool CanPushBuffer(int amount)
```

引擎名：`can_push_buffer`

Returns `true` if a buffer of the size `amount` can be pushed to the audio sample data buffer without overflowing it, `false` otherwise.

### ClearBuffer

```csharp
public void ClearBuffer()
```

引擎名：`clear_buffer`

Clears the audio sample data buffer.

### GetFramesAvailable

```csharp
public int GetFramesAvailable()
```

引擎名：`get_frames_available`

Returns the number of frames that can be pushed to the audio sample data buffer without overflowing it. If the result is `0`, the buffer is full.

### GetSkips

```csharp
public int GetSkips()
```

引擎名：`get_skips`

Returns the number of times the playback skipped due to a buffer underrun in the audio sample data. This value is reset at the start of the playback.

### PushBuffer

```csharp
public bool PushBuffer(ReadOnlySpan<Vector2> frames)
```

引擎名：`push_buffer`

Pushes several audio data frames to the buffer. This is usually more efficient than `PushFrame()` in C# and compiled languages via GDExtension, but `PushBuffer()` may be *less* efficient in GDScript.

### PushBuffer

```csharp
public bool PushBuffer(Vector2[] frames)
```

引擎名：`push_buffer`

Pushes several audio data frames to the buffer. This is usually more efficient than `PushFrame()` in C# and compiled languages via GDExtension, but `PushBuffer()` may be *less* efficient in GDScript.

### PushFrame

```csharp
public bool PushFrame(Vector2 frame)
```

引擎名：`push_frame`

Pushes a single audio data frame to the buffer. This is usually less efficient than `PushBuffer()` in C# and compiled languages via GDExtension, but `PushFrame()` may be *more* efficient in GDScript.
