<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPlaylist

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An audio stream that can play back sub-streams in sequence. Streams can be added to the Playlist with `SetListStream()`, and shuffled with `Shuffle`.

## 构造函数

### AudioStreamPlaylist

```csharp
public AudioStreamPlaylist()
```

## 属性

### FadeTime

```csharp
public float FadeTime { get; set; }
```

引擎名：`fade_time`

Fade time used when a stream ends, when going to the next one. Streams are expected to have an extra bit of audio after the end to help with fading.

### Loop

```csharp
public bool Loop { get; set; }
```

引擎名：`loop`

If `true`, the playlist will loop, otherwise the playlist will end when the last stream is finished.

### Shuffle

```csharp
public bool Shuffle { get; set; }
```

引擎名：`shuffle`

If `true`, the playlist will shuffle each time playback starts and each time it loops.

### Stream0

```csharp
public AudioStream Stream0 { get; set; }
```

### Stream1

```csharp
public AudioStream Stream1 { get; set; }
```

### Stream10

```csharp
public AudioStream Stream10 { get; set; }
```

### Stream11

```csharp
public AudioStream Stream11 { get; set; }
```

### Stream12

```csharp
public AudioStream Stream12 { get; set; }
```

### Stream13

```csharp
public AudioStream Stream13 { get; set; }
```

### Stream14

```csharp
public AudioStream Stream14 { get; set; }
```

### Stream15

```csharp
public AudioStream Stream15 { get; set; }
```

### Stream16

```csharp
public AudioStream Stream16 { get; set; }
```

### Stream17

```csharp
public AudioStream Stream17 { get; set; }
```

### Stream18

```csharp
public AudioStream Stream18 { get; set; }
```

### Stream19

```csharp
public AudioStream Stream19 { get; set; }
```

### Stream2

```csharp
public AudioStream Stream2 { get; set; }
```

### Stream20

```csharp
public AudioStream Stream20 { get; set; }
```

### Stream21

```csharp
public AudioStream Stream21 { get; set; }
```

### Stream22

```csharp
public AudioStream Stream22 { get; set; }
```

### Stream23

```csharp
public AudioStream Stream23 { get; set; }
```

### Stream24

```csharp
public AudioStream Stream24 { get; set; }
```

### Stream25

```csharp
public AudioStream Stream25 { get; set; }
```

### Stream26

```csharp
public AudioStream Stream26 { get; set; }
```

### Stream27

```csharp
public AudioStream Stream27 { get; set; }
```

### Stream28

```csharp
public AudioStream Stream28 { get; set; }
```

### Stream29

```csharp
public AudioStream Stream29 { get; set; }
```

### Stream3

```csharp
public AudioStream Stream3 { get; set; }
```

### Stream30

```csharp
public AudioStream Stream30 { get; set; }
```

### Stream31

```csharp
public AudioStream Stream31 { get; set; }
```

### Stream32

```csharp
public AudioStream Stream32 { get; set; }
```

### Stream33

```csharp
public AudioStream Stream33 { get; set; }
```

### Stream34

```csharp
public AudioStream Stream34 { get; set; }
```

### Stream35

```csharp
public AudioStream Stream35 { get; set; }
```

### Stream36

```csharp
public AudioStream Stream36 { get; set; }
```

### Stream37

```csharp
public AudioStream Stream37 { get; set; }
```

### Stream38

```csharp
public AudioStream Stream38 { get; set; }
```

### Stream39

```csharp
public AudioStream Stream39 { get; set; }
```

### Stream4

```csharp
public AudioStream Stream4 { get; set; }
```

### Stream40

```csharp
public AudioStream Stream40 { get; set; }
```

### Stream41

```csharp
public AudioStream Stream41 { get; set; }
```

### Stream42

```csharp
public AudioStream Stream42 { get; set; }
```

### Stream43

```csharp
public AudioStream Stream43 { get; set; }
```

### Stream44

```csharp
public AudioStream Stream44 { get; set; }
```

### Stream45

```csharp
public AudioStream Stream45 { get; set; }
```

### Stream46

```csharp
public AudioStream Stream46 { get; set; }
```

### Stream47

```csharp
public AudioStream Stream47 { get; set; }
```

### Stream48

```csharp
public AudioStream Stream48 { get; set; }
```

### Stream49

```csharp
public AudioStream Stream49 { get; set; }
```

### Stream5

```csharp
public AudioStream Stream5 { get; set; }
```

### Stream50

```csharp
public AudioStream Stream50 { get; set; }
```

### Stream51

```csharp
public AudioStream Stream51 { get; set; }
```

### Stream52

```csharp
public AudioStream Stream52 { get; set; }
```

### Stream53

```csharp
public AudioStream Stream53 { get; set; }
```

### Stream54

```csharp
public AudioStream Stream54 { get; set; }
```

### Stream55

```csharp
public AudioStream Stream55 { get; set; }
```

### Stream56

```csharp
public AudioStream Stream56 { get; set; }
```

### Stream57

```csharp
public AudioStream Stream57 { get; set; }
```

### Stream58

```csharp
public AudioStream Stream58 { get; set; }
```

### Stream59

```csharp
public AudioStream Stream59 { get; set; }
```

### Stream6

```csharp
public AudioStream Stream6 { get; set; }
```

### Stream60

```csharp
public AudioStream Stream60 { get; set; }
```

### Stream61

```csharp
public AudioStream Stream61 { get; set; }
```

### Stream62

```csharp
public AudioStream Stream62 { get; set; }
```

### Stream63

```csharp
public AudioStream Stream63 { get; set; }
```

### Stream7

```csharp
public AudioStream Stream7 { get; set; }
```

### Stream8

```csharp
public AudioStream Stream8 { get; set; }
```

### Stream9

```csharp
public AudioStream Stream9 { get; set; }
```

### StreamCount

```csharp
public int StreamCount { get; set; }
```

引擎名：`stream_count`

Amount of streams in the playlist.

## 方法

### GetBpm

```csharp
public double GetBpm()
```

引擎名：`get_bpm`

Returns the BPM of the playlist, which can vary depending on the clip being played.

### GetFadeTime

```csharp
public float GetFadeTime()
```

引擎名：`get_fade_time`

### GetListStream

```csharp
public AudioStream GetListStream(int streamIndex)
```

引擎名：`get_list_stream`

Returns the stream at playback position index.

### GetShuffle

```csharp
public bool GetShuffle()
```

引擎名：`get_shuffle`

### GetStreamCount

```csharp
public int GetStreamCount()
```

引擎名：`get_stream_count`

### HasLoop

```csharp
public bool HasLoop()
```

引擎名：`has_loop`

### SetFadeTime

```csharp
public void SetFadeTime(float dec)
```

引擎名：`set_fade_time`

### SetListStream

```csharp
public void SetListStream(int streamIndex, AudioStream audioStream)
```

引擎名：`set_list_stream`

Sets the stream at playback position index.

### SetLoop

```csharp
public void SetLoop(bool loop)
```

引擎名：`set_loop`

### SetShuffle

```csharp
public void SetShuffle(bool shuffle)
```

引擎名：`set_shuffle`

### SetStreamCount

```csharp
public void SetStreamCount(int streamCount)
```

引擎名：`set_stream_count`

## 常量

### MaxStreams

```csharp
public const long MaxStreams = 64
```

Maximum amount of streams supported in the playlist.
