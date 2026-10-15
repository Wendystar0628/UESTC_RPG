<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OggPacketSequence

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A sequence of Ogg packets.

## 构造函数

### OggPacketSequence

```csharp
public OggPacketSequence()
```

## 属性

### GranulePositions

```csharp
public long[] GranulePositions { get; set; }
```

引擎名：`granule_positions`

Contains the granule positions for each page in this packet sequence.

### PacketData

```csharp
public Array<Array> PacketData { get; set; }
```

引擎名：`packet_data`

Contains the raw packets that make up this OggPacketSequence.

### SamplingRate

```csharp
public float SamplingRate { get; set; }
```

引擎名：`sampling_rate`

Holds sample rate information about this sequence. Must be set by another class that actually understands the codec.

## 方法

### GetLength

```csharp
public float GetLength()
```

引擎名：`get_length`

The length of this stream, in seconds.

### GetPacketData

```csharp
public Array<Array> GetPacketData()
```

引擎名：`get_packet_data`

### GetPacketGranulePositions

```csharp
public long[] GetPacketGranulePositions()
```

引擎名：`get_packet_granule_positions`

### GetSamplingRate

```csharp
public float GetSamplingRate()
```

引擎名：`get_sampling_rate`

### SetPacketData

```csharp
public void SetPacketData(Array<Array> packetData)
```

引擎名：`set_packet_data`

### SetPacketGranulePositions

```csharp
public void SetPacketGranulePositions(ReadOnlySpan<long> granulePositions)
```

引擎名：`set_packet_granule_positions`

### SetPacketGranulePositions

```csharp
public void SetPacketGranulePositions(long[] granulePositions)
```

引擎名：`set_packet_granule_positions`

### SetSamplingRate

```csharp
public void SetSamplingRate(float samplingRate)
```

引擎名：`set_sampling_rate`
