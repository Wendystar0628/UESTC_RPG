<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamRandomizer

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Picks a random AudioStream from the pool, depending on the playback mode, and applies random pitch shifting and volume shifting during playback.

## 构造函数

### AudioStreamRandomizer

```csharp
public AudioStreamRandomizer()
```

## 属性

### PlaybackMode

```csharp
public AudioStreamRandomizer.PlaybackModeEnum PlaybackMode { get; set; }
```

引擎名：`playback_mode`

Controls how this AudioStreamRandomizer picks which AudioStream to play next.

### RandomPitch

```csharp
public float RandomPitch { get; set; }
```

引擎名：`random_pitch`

The largest possible frequency multiplier of the random pitch variation. Pitch will be randomly chosen within a range of `1.0 / random_pitch` and `random_pitch`. A value of `1.0` means no variation. A value of `2.0` means pitch will be randomized between double and half.

**Note:** Setting this property also sets `RandomPitchSemitones`.

### RandomPitchSemitones

```csharp
public float RandomPitchSemitones { get; set; }
```

引擎名：`random_pitch_semitones`

The largest possible distance, in semitones, of the random pitch variation. A value of `0.0` means no variation.

**Note:** Setting this property also sets `RandomPitch`.

### RandomVolumeOffsetDb

```csharp
public float RandomVolumeOffsetDb { get; set; }
```

引擎名：`random_volume_offset_db`

The intensity of random volume variation. Volume will be increased or decreased by a random value up to `random_volume_offset_db`. A value of `0.0` means no variation. A value of `3.0` means volume will be randomized between `-3.0 dB` and `+3.0 dB`.

### StreamsCount

```csharp
public int StreamsCount { get; set; }
```

引擎名：`streams_count`

The number of streams in the stream pool.

## 方法

### AddStream

```csharp
public void AddStream(int index, AudioStream stream, float weight = 1f)
```

引擎名：`add_stream`

Insert a stream at the specified index. If the index is less than zero, the insertion occurs at the end of the underlying pool.

### GetPlaybackMode

```csharp
public AudioStreamRandomizer.PlaybackModeEnum GetPlaybackMode()
```

引擎名：`get_playback_mode`

### GetRandomPitch

```csharp
public float GetRandomPitch()
```

引擎名：`get_random_pitch`

### GetRandomPitchSemitones

```csharp
public float GetRandomPitchSemitones()
```

引擎名：`get_random_pitch_semitones`

### GetRandomVolumeOffsetDb

```csharp
public float GetRandomVolumeOffsetDb()
```

引擎名：`get_random_volume_offset_db`

### GetStream

```csharp
public AudioStream GetStream(int index)
```

引擎名：`get_stream`

Returns the stream at the specified index.

### GetStreamProbabilityWeight

```csharp
public float GetStreamProbabilityWeight(int index)
```

引擎名：`get_stream_probability_weight`

Returns the probability weight associated with the stream at the given index.

### GetStreamsCount

```csharp
public int GetStreamsCount()
```

引擎名：`get_streams_count`

### MoveStream

```csharp
public void MoveStream(int indexFrom, int indexTo)
```

引擎名：`move_stream`

Move a stream from one index to another.

### RemoveStream

```csharp
public void RemoveStream(int index)
```

引擎名：`remove_stream`

Remove the stream at the specified index.

### SetPlaybackMode

```csharp
public void SetPlaybackMode(AudioStreamRandomizer.PlaybackModeEnum mode)
```

引擎名：`set_playback_mode`

### SetRandomPitch

```csharp
public void SetRandomPitch(float scale)
```

引擎名：`set_random_pitch`

### SetRandomPitchSemitones

```csharp
public void SetRandomPitchSemitones(float semitones)
```

引擎名：`set_random_pitch_semitones`

### SetRandomVolumeOffsetDb

```csharp
public void SetRandomVolumeOffsetDb(float dbOffset)
```

引擎名：`set_random_volume_offset_db`

### SetStream

```csharp
public void SetStream(int index, AudioStream stream)
```

引擎名：`set_stream`

Set the AudioStream at the specified index.

### SetStreamProbabilityWeight

```csharp
public void SetStreamProbabilityWeight(int index, float weight)
```

引擎名：`set_stream_probability_weight`

Set the probability weight of the stream at the specified index. The higher this value, the more likely that the randomizer will choose this stream during random playback modes.

### SetStreamsCount

```csharp
public void SetStreamsCount(int count)
```

引擎名：`set_streams_count`

## PlaybackModeEnum

命名空间 `Godot`；enum

### 枚举值

- `RandomNoRepeats = 0`：Pick a stream at random according to the probability weights chosen for each stream, but avoid playing the same stream twice in a row whenever possible.
- `Random = 1`：Pick a stream at random according to the probability weights chosen for each stream.
- `Sequential = 2`：Play streams in the order they appear in the stream pool.
