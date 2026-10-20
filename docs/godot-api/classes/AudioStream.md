<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStream

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for audio streams. Audio streams are used for sound effects and music playback, and support WAV (via [`AudioStreamWav`](AudioStreamWav.md)), Ogg (via [`AudioStreamOggVorbis`](AudioStreamOggVorbis.md)), and MP3 (via [`AudioStreamMP3`](AudioStreamMP3.md)) file formats.

## 构造函数

### AudioStream

```csharp
public AudioStream()
```

## 方法

### CanBeSampled

```csharp
public bool CanBeSampled()
```

引擎名：`can_be_sampled`

Returns if the current `AudioStream` can be used as a sample. Only static streams can be sampled.

### GenerateSample

```csharp
public AudioSample GenerateSample()
```

引擎名：`generate_sample`

Generates an [`AudioSample`](AudioSample.md) based on the current stream.

### GetLength

```csharp
public double GetLength()
```

引擎名：`get_length`

Returns the length of the audio stream in seconds. If this stream is an [`AudioStreamRandomizer`](AudioStreamRandomizer.md), returns the length of the last played stream. If this stream has an indefinite length (such as for [`AudioStreamGenerator`](AudioStreamGenerator.md) and [`AudioStreamMicrophone`](AudioStreamMicrophone.md)), returns `0.0`.

### InstantiatePlayback

```csharp
public AudioStreamPlayback InstantiatePlayback()
```

引擎名：`instantiate_playback`

Returns a newly created [`AudioStreamPlayback`](AudioStreamPlayback.md) intended to play this audio stream. Useful for when you want to extend `_InstantiatePlayback()` but call `InstantiatePlayback()` from an internally held AudioStream subresource. An example of this can be found in the source code for `AudioStreamRandomPitch::instantiate_playback`.

### IsMetaStream

```csharp
public bool IsMetaStream()
```

引擎名：`is_meta_stream`

Returns `true` if the stream is a collection of other streams, `false` otherwise.

### IsMonophonic

```csharp
public bool IsMonophonic()
```

引擎名：`is_monophonic`

Returns `true` if this audio stream only supports one channel (*monophony*), or `false` if the audio stream supports two or more channels (*polyphony*).

### _GetBarBeats

```csharp
public virtual int _GetBarBeats()
```

引擎名：`_get_bar_beats`

Override this method to return the bar beats of this stream.

### _GetBeatCount

```csharp
public virtual int _GetBeatCount()
```

引擎名：`_get_beat_count`

Overridable method. Should return the total number of beats of this audio stream. Used by the engine to determine the position of every beat.

Ideally, the returned value should be based off the stream's sample rate ([`AudioStreamWav.MixRate`](AudioStreamWav.md), for example).

### _GetBpm

```csharp
public virtual double _GetBpm()
```

引擎名：`_get_bpm`

Overridable method. Should return the tempo of this audio stream, in beats per minute (BPM). Used by the engine to determine the position of every beat.

Ideally, the returned value should be based off the stream's sample rate ([`AudioStreamWav.MixRate`](AudioStreamWav.md), for example).

### _GetLength

```csharp
public virtual double _GetLength()
```

引擎名：`_get_length`

Override this method to customize the returned value of `GetLength()`. Should return the length of this audio stream, in seconds.

### _GetParameterList

```csharp
public virtual Array<Dictionary> _GetParameterList()
```

引擎名：`_get_parameter_list`

Return the controllable parameters of this stream. This array contains dictionaries with a property info description format (see [`GodotObject.GetPropertyList`](GodotObject.md)). Additionally, the default value for this parameter must be added tho each dictionary in "default_value" field.

### _GetStreamName

```csharp
public virtual string _GetStreamName()
```

引擎名：`_get_stream_name`

Override this method to customize the name assigned to this audio stream. Unused by the engine.

### _GetTags

```csharp
public virtual Dictionary _GetTags()
```

引擎名：`_get_tags`

Override this method to customize the tags for this audio stream. Should return a [`Collections.Dictionary`](Collections.Dictionary.md) of strings with the tag as the key and its content as the value.

Commonly used tags include `title`, `artist`, `album`, `tracknumber`, and `date`.

### _HasLoop

```csharp
public virtual bool _HasLoop()
```

引擎名：`_has_loop`

Override this method to return `true` if this stream has a loop.

### _InstantiatePlayback

```csharp
public virtual AudioStreamPlayback _InstantiatePlayback()
```

引擎名：`_instantiate_playback`

Override this method to customize the returned value of `InstantiatePlayback()`. Should return a new [`AudioStreamPlayback`](AudioStreamPlayback.md) created when the stream is played (such as by an [`AudioStreamPlayer`](AudioStreamPlayer.md)).

### _IsMonophonic

```csharp
public virtual bool _IsMonophonic()
```

引擎名：`_is_monophonic`

Override this method to customize the returned value of `IsMonophonic()`. Should return `true` if this audio stream only supports one channel.

## 事件（信号）

### ParameterListChanged

```csharp
public event Action ParameterListChanged
```

引擎名：`parameter_list_changed`

Signal to be emitted to notify when the parameter list changed.
