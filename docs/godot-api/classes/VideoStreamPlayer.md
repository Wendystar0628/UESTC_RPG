<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VideoStreamPlayer

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A control used for playback of [`VideoStream`](VideoStream.md) resources.

Supported video formats are [Ogg Theora](https://www.theora.org/) (`.ogv`, [`VideoStreamTheora`](VideoStreamTheora.md)) and any format exposed via a GDExtension plugin.

**Warning:** On Web, video playback *will* perform poorly due to missing architecture-specific assembly optimizations.

## 构造函数

### VideoStreamPlayer

```csharp
public VideoStreamPlayer()
```

## 属性

### AudioTrack

```csharp
public int AudioTrack { get; set; }
```

引擎名：`audio_track`

The embedded audio track to play.

### Autoplay

```csharp
public bool Autoplay { get; set; }
```

引擎名：`autoplay`

If `true`, playback starts when the scene loads.

### BufferingMsec

```csharp
public int BufferingMsec { get; set; }
```

引擎名：`buffering_msec`

Amount of time in milliseconds to store in buffer while playing.

### Bus

```csharp
public StringName Bus { get; set; }
```

引擎名：`bus`

Audio bus to use for sound playback.

### Expand

```csharp
public bool Expand { get; set; }
```

引擎名：`expand`

If `true`, the video scales to the control size. Otherwise, the control minimum size will be automatically adjusted to match the video stream's dimensions.

### Loop

```csharp
public bool Loop { get; set; }
```

引擎名：`loop`

If `true`, the video restarts when it reaches its end.

### Paused

```csharp
public bool Paused { get; set; }
```

引擎名：`paused`

If `true`, the video is paused.

### SpeedScale

```csharp
public float SpeedScale { get; set; }
```

引擎名：`speed_scale`

The stream's current speed scale. `1.0` is the normal speed, while `2.0` is double speed and `0.5` is half speed. A speed scale of `0.0` pauses the video, similar to setting `Paused` to `true`.

### Stream

```csharp
public VideoStream Stream { get; set; }
```

引擎名：`stream`

The assigned video stream. See description for supported formats.

### StreamPosition

```csharp
public double StreamPosition { get; set; }
```

引擎名：`stream_position`

The current position of the stream, in seconds.

### Volume

```csharp
public float Volume { get; set; }
```

引擎名：`volume`

Audio volume as a linear value.

### VolumeDb

```csharp
public float VolumeDb { get; set; }
```

引擎名：`volume_db`

Audio volume in dB.

## 方法

### GetAudioTrack

```csharp
public int GetAudioTrack()
```

引擎名：`get_audio_track`

### GetBufferingMsec

```csharp
public int GetBufferingMsec()
```

引擎名：`get_buffering_msec`

### GetBus

```csharp
public StringName GetBus()
```

引擎名：`get_bus`

### GetSpeedScale

```csharp
public float GetSpeedScale()
```

引擎名：`get_speed_scale`

### GetStream

```csharp
public VideoStream GetStream()
```

引擎名：`get_stream`

### GetStreamLength

```csharp
public double GetStreamLength()
```

引擎名：`get_stream_length`

The length of the current stream, in seconds.

### GetStreamName

```csharp
public string GetStreamName()
```

引擎名：`get_stream_name`

Returns the video stream's name, or `"<No Stream>"` if no video stream is assigned.

### GetStreamPosition

```csharp
public double GetStreamPosition()
```

引擎名：`get_stream_position`

### GetVideoTexture

```csharp
public Texture2D GetVideoTexture()
```

引擎名：`get_video_texture`

Returns the current frame as a [`Texture2D`](Texture2D.md).

### GetVolume

```csharp
public float GetVolume()
```

引擎名：`get_volume`

### GetVolumeDb

```csharp
public float GetVolumeDb()
```

引擎名：`get_volume_db`

### HasAutoplay

```csharp
public bool HasAutoplay()
```

引擎名：`has_autoplay`

### HasExpand

```csharp
public bool HasExpand()
```

引擎名：`has_expand`

### HasLoop

```csharp
public bool HasLoop()
```

引擎名：`has_loop`

### IsPaused

```csharp
public bool IsPaused()
```

引擎名：`is_paused`

### IsPlaying

```csharp
public bool IsPlaying()
```

引擎名：`is_playing`

Returns `true` if the video is playing.

**Note:** The video is still considered playing if paused during playback.

### Play

```csharp
public void Play()
```

引擎名：`play`

Starts the video playback from the beginning. If the video is paused, this will not unpause the video.

### SetAudioTrack

```csharp
public void SetAudioTrack(int track)
```

引擎名：`set_audio_track`

### SetAutoplay

```csharp
public void SetAutoplay(bool enabled)
```

引擎名：`set_autoplay`

### SetBufferingMsec

```csharp
public void SetBufferingMsec(int msec)
```

引擎名：`set_buffering_msec`

### SetBus

```csharp
public void SetBus(StringName bus)
```

引擎名：`set_bus`

### SetExpand

```csharp
public void SetExpand(bool enable)
```

引擎名：`set_expand`

### SetLoop

```csharp
public void SetLoop(bool loop)
```

引擎名：`set_loop`

### SetPaused

```csharp
public void SetPaused(bool paused)
```

引擎名：`set_paused`

### SetSpeedScale

```csharp
public void SetSpeedScale(float speedScale)
```

引擎名：`set_speed_scale`

### SetStream

```csharp
public void SetStream(VideoStream stream)
```

引擎名：`set_stream`

### SetStreamPosition

```csharp
public void SetStreamPosition(double position)
```

引擎名：`set_stream_position`

### SetVolume

```csharp
public void SetVolume(float volume)
```

引擎名：`set_volume`

### SetVolumeDb

```csharp
public void SetVolumeDb(float db)
```

引擎名：`set_volume_db`

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops the video playback and sets the stream position to 0.

**Note:** Although the stream position will be set to 0, the first frame of the video stream won't become the current frame.

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

引擎名：`finished`

Emitted when playback is finished.
