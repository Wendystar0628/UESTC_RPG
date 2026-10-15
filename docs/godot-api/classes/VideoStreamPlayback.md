<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VideoStreamPlayback

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is intended to be overridden by video decoder extensions with custom implementations of [`VideoStream`](VideoStream.md).

## 构造函数

### VideoStreamPlayback

```csharp
public VideoStreamPlayback()
```

## 方法

### MixAudio

```csharp
public int MixAudio(int numFrames, ReadOnlySpan<float> buffer, int offset)
```

引擎名：`mix_audio`

Render `numFrames` audio frames (of `_GetChannels()` floats each) from `buffer`, starting from index `offset` in the array. Returns the number of audio frames rendered, or -1 on error.

### MixAudio

```csharp
public int MixAudio(int numFrames, float[] buffer = null, int offset = 0)
```

引擎名：`mix_audio`

Render `numFrames` audio frames (of `_GetChannels()` floats each) from `buffer`, starting from index `offset` in the array. Returns the number of audio frames rendered, or -1 on error.

参数：
- `buffer`：If the parameter is null, then the default value is `Array.Empty<float>()`.

### _GetChannels

```csharp
public virtual int _GetChannels()
```

引擎名：`_get_channels`

Returns the number of audio channels.

### _GetLength

```csharp
public virtual double _GetLength()
```

引擎名：`_get_length`

Returns the video duration in seconds, if known, or 0 if unknown.

### _GetMixRate

```csharp
public virtual int _GetMixRate()
```

引擎名：`_get_mix_rate`

Returns the audio sample rate used for mixing.

### _GetPlaybackPosition

```csharp
public virtual double _GetPlaybackPosition()
```

引擎名：`_get_playback_position`

Return the current playback timestamp. Called in response to the [`VideoStreamPlayer.StreamPosition`](VideoStreamPlayer.md) getter.

### _GetTexture

```csharp
public virtual Texture2D _GetTexture()
```

引擎名：`_get_texture`

Allocates a [`Texture2D`](Texture2D.md) in which decoded video frames will be drawn.

### _IsPaused

```csharp
public virtual bool _IsPaused()
```

引擎名：`_is_paused`

Returns the paused status, as set by `_SetPaused()`.

### _IsPlaying

```csharp
public virtual bool _IsPlaying()
```

引擎名：`_is_playing`

Returns the playback state, as determined by calls to `_Play()` and `_Stop()`.

### _Play

```csharp
public virtual void _Play()
```

引擎名：`_play`

Called in response to [`VideoStreamPlayer.Autoplay`](VideoStreamPlayer.md) or [`VideoStreamPlayer.Play`](VideoStreamPlayer.md). Note that manual playback may also invoke `_Stop()` multiple times before this method is called. `_IsPlaying()` should return `true` once playing.

### _Seek

```csharp
public virtual void _Seek(double time)
```

引擎名：`_seek`

Seeks to `time` seconds. Called in response to the [`VideoStreamPlayer.StreamPosition`](VideoStreamPlayer.md) setter.

### _SetAudioTrack

```csharp
public virtual void _SetAudioTrack(int idx)
```

引擎名：`_set_audio_track`

Select the audio track `idx`. Called when playback starts, and in response to the [`VideoStreamPlayer.AudioTrack`](VideoStreamPlayer.md) setter.

### _SetPaused

```csharp
public virtual void _SetPaused(bool paused)
```

引擎名：`_set_paused`

Set the paused status of video playback. `_IsPaused()` must return `paused`. Called in response to the [`VideoStreamPlayer.Paused`](VideoStreamPlayer.md) setter.

### _Stop

```csharp
public virtual void _Stop()
```

引擎名：`_stop`

Stops playback. May be called multiple times before `_Play()`, or in response to [`VideoStreamPlayer.Stop`](VideoStreamPlayer.md). `_IsPlaying()` should return `false` once stopped.

### _Update

```csharp
public virtual void _Update(double delta)
```

引擎名：`_update`

Ticks video playback for `delta` seconds. Called every frame as long as both `_IsPaused()` and `_IsPlaying()` return `true`.
