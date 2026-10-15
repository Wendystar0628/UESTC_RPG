<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPlayback

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Can play, loop, pause a scroll through audio. See [`AudioStream`](AudioStream.md) and [`AudioStreamOggVorbis`](AudioStreamOggVorbis.md) for usage.

## 构造函数

### AudioStreamPlayback

```csharp
public AudioStreamPlayback()
```

## 方法

### GetLoopCount

```csharp
public int GetLoopCount()
```

引擎名：`get_loop_count`

Returns the number of times the stream has looped.

### GetPlaybackPosition

```csharp
public double GetPlaybackPosition()
```

引擎名：`get_playback_position`

Returns the current position in the stream, in seconds.

### GetSamplePlayback

```csharp
public AudioSamplePlayback GetSamplePlayback()
```

引擎名：`get_sample_playback`

Returns the [`AudioSamplePlayback`](AudioSamplePlayback.md) associated with this `AudioStreamPlayback` for playing back the audio sample of this stream.

### IsPlaying

```csharp
public bool IsPlaying()
```

引擎名：`is_playing`

Returns `true` if the stream is playing.

### MixAudio

```csharp
public Vector2[] MixAudio(float rateScale, int frames)
```

引擎名：`mix_audio`

Mixes up to `frames` of audio from the stream from the current position, at a rate of `rateScale`, advancing the stream.

Returns a [`Vector2`](Vector2.md)[] where each element holds the left and right channel volume levels of each frame.

**Note:** Can return fewer frames than requested, make sure to use the size of the return value.

### Seek

```csharp
public void Seek(double time = 0)
```

引擎名：`seek`

Seeks the stream at the given `time`, in seconds.

### SetSamplePlayback

```csharp
public void SetSamplePlayback(AudioSamplePlayback playbackSample)
```

引擎名：`set_sample_playback`

Associates [`AudioSamplePlayback`](AudioSamplePlayback.md) to this `AudioStreamPlayback` for playing back the audio sample of this stream.

### Start

```csharp
public void Start(double fromPos = 0)
```

引擎名：`start`

Starts the stream from the given `fromPos`, in seconds.

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops the stream.

### _GetLoopCount

```csharp
public virtual int _GetLoopCount()
```

引擎名：`_get_loop_count`

Overridable method. Should return how many times this audio stream has looped. Most built-in playbacks always return `0`.

### _GetParameter

```csharp
public virtual Variant _GetParameter(StringName name)
```

引擎名：`_get_parameter`

Return the current value of a playback parameter by name (see [`AudioStream._GetParameterList()`](AudioStream.md)).

### _GetPlaybackPosition

```csharp
public virtual double _GetPlaybackPosition()
```

引擎名：`_get_playback_position`

Overridable method. Should return the current progress along the audio stream, in seconds.

### _IsPlaying

```csharp
public virtual bool _IsPlaying()
```

引擎名：`_is_playing`

Overridable method. Should return `true` if this playback is active and playing its audio stream.

### _Seek

```csharp
public virtual void _Seek(double position)
```

引擎名：`_seek`

Override this method to customize what happens when seeking this audio stream at the given `position`, such as by calling [`AudioStreamPlayer.Seek`](AudioStreamPlayer.md).

### _SetParameter

```csharp
public virtual void _SetParameter(StringName name, Variant value)
```

引擎名：`_set_parameter`

Set the current value of a playback parameter by name (see [`AudioStream._GetParameterList()`](AudioStream.md)).

### _Start

```csharp
public virtual void _Start(double fromPos)
```

引擎名：`_start`

Override this method to customize what happens when the playback starts at the given position, such as by calling [`AudioStreamPlayer.Play`](AudioStreamPlayer.md).

### _Stop

```csharp
public virtual void _Stop()
```

引擎名：`_stop`

Override this method to customize what happens when the playback is stopped, such as by calling [`AudioStreamPlayer.Stop`](AudioStreamPlayer.md).

### _TagUsedStreams

```csharp
public virtual void _TagUsedStreams()
```

引擎名：`_tag_used_streams`

Overridable method. Called whenever the audio stream is mixed if the playback is active and [`AudioServer.SetEnableTaggingUsedAudioStreams`](AudioServer.md) has been set to `true`. Editor plugins may use this method to "tag" the current position along the audio stream and display it in a preview.
