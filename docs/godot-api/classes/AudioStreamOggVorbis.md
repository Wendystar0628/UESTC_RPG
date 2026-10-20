<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamOggVorbis

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The AudioStreamOggVorbis class is a specialized [`AudioStream`](AudioStream.md) for handling Ogg Vorbis file formats. It offers functionality for loading and playing back Ogg Vorbis files, as well as managing looping and other playback properties. More info can be found in `ResourceImporterOggVorbis`.

This class is part of the audio stream system, which also supports WAV files through the [`AudioStreamWav`](AudioStreamWav.md) class, and MP3 files through the [`AudioStreamMP3`](AudioStreamMP3.md) class.

## 构造函数

### AudioStreamOggVorbis

```csharp
public AudioStreamOggVorbis()
```

## 属性

### BarBeats

```csharp
public int BarBeats { get; set; }
```

引擎名：`bar_beats`

The number of beats within a single bar in the audio track.

### BeatCount

```csharp
public int BeatCount { get; set; }
```

引擎名：`beat_count`

The length of the audio track, in beats. The actual duration of the audio file might be longer than what is indicated by this property. It defines the end of the audio for looping, [`AudioStreamPlaylist`](AudioStreamPlaylist.md), and [`AudioStreamInteractive`](AudioStreamInteractive.md).

### Bpm

```csharp
public double Bpm { get; set; }
```

引擎名：`bpm`

The tempo of the audio track, measured in beats per minute.

### Loop

```csharp
public bool Loop { get; set; }
```

引擎名：`loop`

If `true`, the stream will play again from the specified `LoopOffset` once it reaches the end of the audio track, or once it reaches the end of the last beat according to the amount specified in `BeatCount`. Useful for ambient sounds and background music.

### LoopOffset

```csharp
public double LoopOffset { get; set; }
```

引擎名：`loop_offset`

Time in seconds at which the stream starts after being looped.

### PacketSequence

```csharp
public OggPacketSequence PacketSequence { get; set; }
```

引擎名：`packet_sequence`

Contains the raw Ogg data for this stream.

### Tags

```csharp
public Dictionary Tags { get; set; }
```

引擎名：`tags`

Contains user-defined tags if found in the Ogg Vorbis data.

Commonly used tags include `title`, `artist`, `album`, `tracknumber`, and `date` (`date` does not have a standard date format).

**Note:** No tag is *guaranteed* to be present in every file, so make sure to account for the keys not always existing.

## 方法

### GetBarBeats

```csharp
public int GetBarBeats()
```

引擎名：`get_bar_beats`

### GetBeatCount

```csharp
public int GetBeatCount()
```

引擎名：`get_beat_count`

### GetBpm

```csharp
public double GetBpm()
```

引擎名：`get_bpm`

### GetLoopOffset

```csharp
public double GetLoopOffset()
```

引擎名：`get_loop_offset`

### GetPacketSequence

```csharp
public OggPacketSequence GetPacketSequence()
```

引擎名：`get_packet_sequence`

### GetTags

```csharp
public Dictionary GetTags()
```

引擎名：`get_tags`

### HasLoop

```csharp
public bool HasLoop()
```

引擎名：`has_loop`

### LoadFromBuffer

```csharp
public static AudioStreamOggVorbis LoadFromBuffer(ReadOnlySpan<byte> streamData)
```

引擎名：`load_from_buffer`

Creates a new `AudioStreamOggVorbis` instance from the given buffer. The buffer must contain Ogg Vorbis data.

### LoadFromBuffer

```csharp
public static AudioStreamOggVorbis LoadFromBuffer(byte[] streamData)
```

引擎名：`load_from_buffer`

Creates a new `AudioStreamOggVorbis` instance from the given buffer. The buffer must contain Ogg Vorbis data.

### LoadFromFile

```csharp
public static AudioStreamOggVorbis LoadFromFile(string path)
```

引擎名：`load_from_file`

Creates a new `AudioStreamOggVorbis` instance from the given file path. The file must be in Ogg Vorbis format.

### SetBarBeats

```csharp
public void SetBarBeats(int count)
```

引擎名：`set_bar_beats`

### SetBeatCount

```csharp
public void SetBeatCount(int count)
```

引擎名：`set_beat_count`

### SetBpm

```csharp
public void SetBpm(double bpm)
```

引擎名：`set_bpm`

### SetLoop

```csharp
public void SetLoop(bool enable)
```

引擎名：`set_loop`

### SetLoopOffset

```csharp
public void SetLoopOffset(double seconds)
```

引擎名：`set_loop_offset`

### SetPacketSequence

```csharp
public void SetPacketSequence(OggPacketSequence packetSequence)
```

引擎名：`set_packet_sequence`

### SetTags

```csharp
public void SetTags(Dictionary tags)
```

引擎名：`set_tags`
