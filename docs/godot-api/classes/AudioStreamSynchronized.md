<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamSynchronized

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This is a stream that can be fitted with sub-streams, which will be played in-sync. The streams begin at exactly the same time when play is pressed, and will end when the last of them ends. If one of the sub-streams loops, then playback will continue.

## 构造函数

### AudioStreamSynchronized

```csharp
public AudioStreamSynchronized()
```

## 属性

### StreamCount

```csharp
public int StreamCount { get; set; }
```

引擎名：`stream_count`

Set the total amount of streams that will be played back synchronized.

## 方法

### GetStreamCount

```csharp
public int GetStreamCount()
```

引擎名：`get_stream_count`

### GetSyncStream

```csharp
public AudioStream GetSyncStream(int streamIndex)
```

引擎名：`get_sync_stream`

Get one of the synchronized streams, by index.

### GetSyncStreamVolume

```csharp
public float GetSyncStreamVolume(int streamIndex)
```

引擎名：`get_sync_stream_volume`

Get the volume of one of the synchronized streams, by index.

### SetStreamCount

```csharp
public void SetStreamCount(int streamCount)
```

引擎名：`set_stream_count`

### SetSyncStream

```csharp
public void SetSyncStream(int streamIndex, AudioStream audioStream)
```

引擎名：`set_sync_stream`

Set one of the synchronized streams, by index.

### SetSyncStreamVolume

```csharp
public void SetSyncStreamVolume(int streamIndex, float volumeDb)
```

引擎名：`set_sync_stream_volume`

Set the volume of one of the synchronized streams, by index.

## 常量

### MaxStreams

```csharp
public const long MaxStreams = 32
```

Maximum amount of streams that can be synchronized.
