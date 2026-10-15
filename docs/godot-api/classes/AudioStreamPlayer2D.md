<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPlayer2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Plays audio that is attenuated with distance to the listener.

By default, audio is heard from the screen center. This can be changed by adding an [`AudioListener2D`](AudioListener2D.md) node to the scene and enabling it by calling [`AudioListener2D.MakeCurrent`](AudioListener2D.md) on it.

See also [`AudioStreamPlayer`](AudioStreamPlayer.md) to play a sound non-positionally.

**Note:** Hiding an `AudioStreamPlayer2D` node does not disable its audio output. To temporarily disable an `AudioStreamPlayer2D`'s audio output, set `VolumeDb` to a very low value like `-100` (which isn't audible to human hearing).

## 构造函数

### AudioStreamPlayer2D

```csharp
public AudioStreamPlayer2D()
```

## 属性

### AreaMask

```csharp
public uint AreaMask { get; set; }
```

引擎名：`area_mask`

Determines which [`Area2D`](Area2D.md) layers affect the sound for reverb and audio bus effects. Areas can be used to redirect [`AudioStream`](AudioStream.md)s so that they play in a certain audio bus. An example of how you might use this is making a "water" area so that sounds played in the water are redirected through an audio bus to make them sound like they are being played underwater.

### Attenuation

```csharp
public float Attenuation { get; set; }
```

引擎名：`attenuation`

The volume is attenuated over distance with this as an exponent.

### Autoplay

```csharp
public bool Autoplay { get; set; }
```

引擎名：`autoplay`

If `true`, audio plays when added to scene tree.

### Bus

```csharp
public StringName Bus { get; set; }
```

引擎名：`bus`

Bus on which this audio is playing.

**Note:** When setting this property, keep in mind that no validation is performed to see if the given name matches an existing bus. This is because audio bus layouts might be loaded after this property is set. If this given name can't be resolved at runtime, it will fall back to `"Master"`.

### MaxDistance

```csharp
public float MaxDistance { get; set; }
```

引擎名：`max_distance`

Maximum distance from which audio is still hearable.

### MaxPolyphony

```csharp
public int MaxPolyphony { get; set; }
```

引擎名：`max_polyphony`

The maximum number of sounds this node can play at the same time. Playing additional sounds after this value is reached will cut off the oldest sounds.

### PanningStrength

```csharp
public float PanningStrength { get; set; }
```

引擎名：`panning_strength`

Scales the panning strength for this node by multiplying the base `ProjectSettings.audio/general/2d_panning_strength` with this factor. Higher values will pan audio from left to right more dramatically than lower values.

### PitchScale

```csharp
public float PitchScale { get; set; }
```

引擎名：`pitch_scale`

The pitch and the tempo of the audio, as a multiplier of the audio sample's sample rate.

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

If `true`, audio is playing or is queued to be played (see `Play()`).

### Stream

```csharp
public AudioStream Stream { get; set; }
```

引擎名：`stream`

The [`AudioStream`](AudioStream.md) object to be played.

### StreamPaused

```csharp
public bool StreamPaused { get; set; }
```

引擎名：`stream_paused`

If `true`, the playback is paused. You can resume it by setting `StreamPaused` to `false`.

### VolumeDb

```csharp
public float VolumeDb { get; set; }
```

引擎名：`volume_db`

Base volume before attenuation, in decibels.

### VolumeLinear

```csharp
public float VolumeLinear { get; set; }
```

引擎名：`volume_linear`

Base volume before attenuation, as a linear value.

**Note:** This member modifies `VolumeDb` for convenience. The returned value is equivalent to the result of `Mathf.DbToLinear` on `VolumeDb`. Setting this member is equivalent to setting `VolumeDb` to the result of `Mathf.LinearToDb` on a value.

## 方法

### GetAreaMask

```csharp
public uint GetAreaMask()
```

引擎名：`get_area_mask`

### GetAttenuation

```csharp
public float GetAttenuation()
```

引擎名：`get_attenuation`

### GetBus

```csharp
public StringName GetBus()
```

引擎名：`get_bus`

### GetMaxDistance

```csharp
public float GetMaxDistance()
```

引擎名：`get_max_distance`

### GetMaxPolyphony

```csharp
public int GetMaxPolyphony()
```

引擎名：`get_max_polyphony`

### GetPanningStrength

```csharp
public float GetPanningStrength()
```

引擎名：`get_panning_strength`

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

Returns the position in the [`AudioStream`](AudioStream.md).

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

Returns the [`AudioStreamPlayback`](AudioStreamPlayback.md) object associated with this `AudioStreamPlayer2D`.

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

Returns whether the [`AudioStreamPlayer`](AudioStreamPlayer.md) can return the [`AudioStreamPlayback`](AudioStreamPlayback.md) object or not.

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

Queues the audio to play on the next physics frame, from the given position `fromPosition`, in seconds.

### Seek

```csharp
public void Seek(float toPosition)
```

引擎名：`seek`

Sets the position from which audio will be played, in seconds.

### SetAreaMask

```csharp
public void SetAreaMask(uint mask)
```

引擎名：`set_area_mask`

### SetAttenuation

```csharp
public void SetAttenuation(float curve)
```

引擎名：`set_attenuation`

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

### SetMaxDistance

```csharp
public void SetMaxDistance(float pixels)
```

引擎名：`set_max_distance`

### SetMaxPolyphony

```csharp
public void SetMaxPolyphony(int maxPolyphony)
```

引擎名：`set_max_polyphony`

### SetPanningStrength

```csharp
public void SetPanningStrength(float panningStrength)
```

引擎名：`set_panning_strength`

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

Stops the audio.

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

引擎名：`finished`

Emitted when the audio stops playing.
