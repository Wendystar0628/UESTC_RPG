<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPlayer

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `AudioStreamPlayer` node plays an audio stream non-positionally. It is ideal for user interfaces, menus, or background music.

To use this node, `Stream` needs to be set to a valid [`AudioStream`](AudioStream.md) resource. Playing more than one sound at the same time is also supported, see `MaxPolyphony`.

If you need to play audio at a specific position, use [`AudioStreamPlayer2D`](AudioStreamPlayer2D.md) or [`AudioStreamPlayer3D`](AudioStreamPlayer3D.md) instead.

## 构造函数

### AudioStreamPlayer

```csharp
public AudioStreamPlayer()
```

## 属性

### Autoplay

```csharp
public bool Autoplay { get; set; }
```

引擎名：`autoplay`

If `true`, this node calls `Play()` when entering the tree.

### Bus

```csharp
public StringName Bus { get; set; }
```

引擎名：`bus`

The target bus name. All sounds from this node will be playing on this bus.

**Note:** At runtime, if no bus with the given name exists, all sounds will fall back on `"Master"`. See also [`AudioServer.GetBusName`](AudioServer.md).

### MaxPolyphony

```csharp
public int MaxPolyphony { get; set; }
```

引擎名：`max_polyphony`

The maximum number of sounds this node can play at the same time. Calling `Play()` after this value is reached will cut off the oldest sounds.

### MixTarget

```csharp
public AudioStreamPlayer.MixTargetEnum MixTarget { get; set; }
```

引擎名：`mix_target`

The mix target channels. Has no effect when two speakers or less are detected (see [`AudioServer.SpeakerMode`](AudioServer.md)).

### PitchScale

```csharp
public float PitchScale { get; set; }
```

引擎名：`pitch_scale`

The audio's pitch and tempo, as a multiplier of the `Stream`'s sample rate. A value of `2.0` doubles the audio's pitch, while a value of `0.5` halves the pitch.

### PlaybackType

```csharp
public AudioServer.PlaybackType PlaybackType { get; set; }
```

引擎名：`playback_type`

The playback type of the stream player. If set other than to the default value, it will force that playback type.

### Playing

```csharp
public bool Playing { get; set; }
```

引擎名：`playing`

If `true`, this node is playing sounds. Setting this property has the same effect as `Play()` and `Stop()`.

### Stream

```csharp
public AudioStream Stream { get; set; }
```

引擎名：`stream`

The [`AudioStream`](AudioStream.md) resource to be played. Setting this property stops all currently playing sounds. If left empty, the `AudioStreamPlayer` does not work.

### StreamPaused

```csharp
public bool StreamPaused { get; set; }
```

引擎名：`stream_paused`

If `true`, the sounds are paused. Setting `StreamPaused` to `false` resumes all sounds.

**Note:** This property is automatically changed when exiting or entering the tree, or this node is paused (see [`Node.ProcessMode`](Node.md)).

### VolumeDb

```csharp
public float VolumeDb { get; set; }
```

引擎名：`volume_db`

Volume of sound, in decibels. This is an offset of the `Stream`'s volume.

**Note:** To convert between decibel and linear energy (like most volume sliders do), use `VolumeLinear`, or `Mathf.DbToLinear` and `Mathf.LinearToDb`.

### VolumeLinear

```csharp
public float VolumeLinear { get; set; }
```

引擎名：`volume_linear`

Volume of sound, as a linear value.

**Note:** This member modifies `VolumeDb` for convenience. The returned value is equivalent to the result of `Mathf.DbToLinear` on `VolumeDb`. Setting this member is equivalent to setting `VolumeDb` to the result of `Mathf.LinearToDb` on a value.

## 方法

### GetBus

```csharp
public StringName GetBus()
```

引擎名：`get_bus`

### GetMaxPolyphony

```csharp
public int GetMaxPolyphony()
```

引擎名：`get_max_polyphony`

### GetMixTarget

```csharp
public AudioStreamPlayer.MixTargetEnum GetMixTarget()
```

引擎名：`get_mix_target`

### GetPitchScale

```csharp
public float GetPitchScale()
```

引擎名：`get_pitch_scale`

### GetPlaybackPosition

```csharp
public float GetPlaybackPosition()
```

引擎名：`get_playback_position`

Returns the position in the [`AudioStream`](AudioStream.md) of the latest sound, in seconds. Returns `0.0` if no sounds are playing.

**Note:** The position is not always accurate, as the [`AudioServer`](AudioServer.md) does not mix audio every processed frame. To get more accurate results, add [`AudioServer.GetTimeSinceLastMix`](AudioServer.md) to the returned position.

**Note:** This method always returns `0.0` if the `Stream` is an [`AudioStreamInteractive`](AudioStreamInteractive.md), since it can have multiple clips playing at once.

### GetPlaybackType

```csharp
public AudioServer.PlaybackType GetPlaybackType()
```

引擎名：`get_playback_type`

### GetStream

```csharp
public AudioStream GetStream()
```

引擎名：`get_stream`

### GetStreamPaused

```csharp
public bool GetStreamPaused()
```

引擎名：`get_stream_paused`

### GetStreamPlayback

```csharp
public AudioStreamPlayback GetStreamPlayback()
```

引擎名：`get_stream_playback`

Returns the latest [`AudioStreamPlayback`](AudioStreamPlayback.md) of this node, usually the most recently created by `Play()`. If no sounds are playing, this method fails and returns an empty playback.

### GetVolumeDb

```csharp
public float GetVolumeDb()
```

引擎名：`get_volume_db`

### GetVolumeLinear

```csharp
public float GetVolumeLinear()
```

引擎名：`get_volume_linear`

### HasStreamPlayback

```csharp
public bool HasStreamPlayback()
```

引擎名：`has_stream_playback`

Returns `true` if any sound is active, even if `StreamPaused` is set to `true`. See also `Playing` and `GetStreamPlayback()`.

### IsAutoplayEnabled

```csharp
public bool IsAutoplayEnabled()
```

引擎名：`is_autoplay_enabled`

### IsPlaying

```csharp
public bool IsPlaying()
```

引擎名：`is_playing`

### Play

```csharp
public void Play(float fromPosition = 0f)
```

引擎名：`play`

Plays a sound from the beginning, or the given `fromPosition` in seconds.

### Seek

```csharp
public void Seek(float toPosition)
```

引擎名：`seek`

Restarts all sounds to be played from the given `toPosition`, in seconds. Does nothing if no sounds are playing.

### SetAutoplay

```csharp
public void SetAutoplay(bool enable)
```

引擎名：`set_autoplay`

### SetBus

```csharp
public void SetBus(StringName bus)
```

引擎名：`set_bus`

### SetMaxPolyphony

```csharp
public void SetMaxPolyphony(int maxPolyphony)
```

引擎名：`set_max_polyphony`

### SetMixTarget

```csharp
public void SetMixTarget(AudioStreamPlayer.MixTargetEnum mixTarget)
```

引擎名：`set_mix_target`

### SetPitchScale

```csharp
public void SetPitchScale(float pitchScale)
```

引擎名：`set_pitch_scale`

### SetPlaybackType

```csharp
public void SetPlaybackType(AudioServer.PlaybackType playbackType)
```

引擎名：`set_playback_type`

### SetPlaying

```csharp
public void SetPlaying(bool enable)
```

引擎名：`set_playing`

### SetStream

```csharp
public void SetStream(AudioStream stream)
```

引擎名：`set_stream`

### SetStreamPaused

```csharp
public void SetStreamPaused(bool pause)
```

引擎名：`set_stream_paused`

### SetVolumeDb

```csharp
public void SetVolumeDb(float volumeDb)
```

引擎名：`set_volume_db`

### SetVolumeLinear

```csharp
public void SetVolumeLinear(float volumeLinear)
```

引擎名：`set_volume_linear`

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops all sounds from this node.

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

引擎名：`finished`

Emitted when a sound finishes playing without interruptions. This signal is *not* emitted when calling `Stop()`, or when exiting the tree while sounds are playing.

## MixTargetEnum

命名空间 `Godot`；enum

### 枚举值

- `Stereo = 0`：The audio will be played only on the first channel.
- `Surround = 1`：The audio will be played on all surround channels.
- `Center = 2`：The audio will be played on the second channel, which is usually the center.
