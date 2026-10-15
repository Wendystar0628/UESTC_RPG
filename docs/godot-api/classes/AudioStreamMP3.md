<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamMP3

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

MP3 audio stream driver. See `Data` if you want to load an MP3 file at run-time. More info can be found in `ResourceImporterMP3`.

**Note:** This class can optionally support legacy MP1 and MP2 formats, provided that the engine is compiled with the `minimp3_extra_formats=yes` SCons option. These extra formats are not enabled by default.

## 构造函数

### AudioStreamMP3

```csharp
public AudioStreamMP3()
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

### Data

```csharp
public byte[] Data { get; set; }
```

引擎名：`data`

Contains the audio data in bytes.

You can load a file without having to import it beforehand using the code snippet below. Keep in mind that this snippet loads the whole file into memory and may not be ideal for huge files (hundreds of megabytes or more).

```csharp
public AudioStreamMP3 LoadMP3(string path)
{
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
var sound = new AudioStreamMP3();
sound.Data = file.GetBuffer(file.GetLength());
return sound;
}

```

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

### GetData

```csharp
public byte[] GetData()
```

引擎名：`get_data`

### GetLoopOffset

```csharp
public double GetLoopOffset()
```

引擎名：`get_loop_offset`

### HasLoop

```csharp
public bool HasLoop()
```

引擎名：`has_loop`

### LoadFromBuffer

```csharp
public static AudioStreamMP3 LoadFromBuffer(ReadOnlySpan<byte> streamData)
```

引擎名：`load_from_buffer`

Creates a new `AudioStreamMP3` instance from the given buffer. The buffer must contain MP3 data.

### LoadFromBuffer

```csharp
public static AudioStreamMP3 LoadFromBuffer(byte[] streamData)
```

引擎名：`load_from_buffer`

Creates a new `AudioStreamMP3` instance from the given buffer. The buffer must contain MP3 data.

### LoadFromFile

```csharp
public static AudioStreamMP3 LoadFromFile(string path)
```

引擎名：`load_from_file`

Creates a new `AudioStreamMP3` instance from the given file path. The file must be in MP3 format.

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

### SetData

```csharp
public void SetData(ReadOnlySpan<byte> data)
```

引擎名：`set_data`

### SetData

```csharp
public void SetData(byte[] data)
```

引擎名：`set_data`

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
