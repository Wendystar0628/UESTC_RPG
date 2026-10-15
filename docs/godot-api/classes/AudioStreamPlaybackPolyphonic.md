<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPlaybackPolyphonic

命名空间 `Godot`；class；继承 [`AudioStreamPlayback`](AudioStreamPlayback.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Playback instance for [`AudioStreamPolyphonic`](AudioStreamPolyphonic.md). After setting the `stream` property of [`AudioStreamPlayer`](AudioStreamPlayer.md), [`AudioStreamPlayer2D`](AudioStreamPlayer2D.md), or [`AudioStreamPlayer3D`](AudioStreamPlayer3D.md), the playback instance can be obtained by calling [`AudioStreamPlayer.GetStreamPlayback`](AudioStreamPlayer.md), [`AudioStreamPlayer2D.GetStreamPlayback`](AudioStreamPlayer2D.md) or [`AudioStreamPlayer3D.GetStreamPlayback`](AudioStreamPlayer3D.md) methods.

## 方法

### IsStreamPlaying

```csharp
public bool IsStreamPlaying(long stream)
```

引擎名：`is_stream_playing`

Returns `true` if the stream associated with the given integer ID is still playing. Check `PlayStream()` for information on when this ID becomes invalid.

### PlayStream

```csharp
public long PlayStream(AudioStream stream, float fromOffset = 0f, float volumeDb = 0f, float pitchScale = 1f, AudioServer.PlaybackType playbackType = AudioServer.PlaybackType.Default, StringName bus = null)
```

引擎名：`play_stream`

Play an [`AudioStream`](AudioStream.md) at a given offset, volume, pitch scale, playback type, and bus. Playback starts immediately.

The return value is a unique integer ID that is associated to this playback stream and which can be used to control it.

This ID becomes invalid when the stream ends (if it does not loop), when the `AudioStreamPlaybackPolyphonic` is stopped, or when `StopStream()` is called.

This function returns `InvalidId` if the amount of streams currently playing equals [`AudioStreamPolyphonic.Polyphony`](AudioStreamPolyphonic.md). If you need a higher amount of maximum polyphony, raise this value.

### PlayStream

```csharp
public long PlayStream(AudioStream stream, float fromOffset, float volumeDb, float pitchScale)
```

引擎名：`play_stream`

Play an [`AudioStream`](AudioStream.md) at a given offset, volume, pitch scale, playback type, and bus. Playback starts immediately.

The return value is a unique integer ID that is associated to this playback stream and which can be used to control it.

This ID becomes invalid when the stream ends (if it does not loop), when the `AudioStreamPlaybackPolyphonic` is stopped, or when `StopStream()` is called.

This function returns `InvalidId` if the amount of streams currently playing equals [`AudioStreamPolyphonic.Polyphony`](AudioStreamPolyphonic.md). If you need a higher amount of maximum polyphony, raise this value.

### SetStreamPitchScale

```csharp
public void SetStreamPitchScale(long stream, float pitchScale)
```

引擎名：`set_stream_pitch_scale`

Change the stream pitch scale. The `stream` argument is an integer ID returned by `PlayStream()`.

### SetStreamVolume

```csharp
public void SetStreamVolume(long stream, float volumeDb)
```

引擎名：`set_stream_volume`

Change the stream volume (in db). The `stream` argument is an integer ID returned by `PlayStream()`.

### StopStream

```csharp
public void StopStream(long stream)
```

引擎名：`stop_stream`

Stop a stream. The `stream` argument is an integer ID returned by `PlayStream()`, which becomes invalid after calling this function.

## 常量

### InvalidId

```csharp
public const long InvalidId = -1
```

Returned by `PlayStream()` in case it could not allocate a stream for playback.
