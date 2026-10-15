<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PacketPeerStream

命名空间 `Godot`；class；继承 [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

PacketStreamPeer provides a wrapper for working using packets over a stream. This allows for using packet based code with StreamPeers. PacketPeerStream implements a custom protocol over the StreamPeer, so the user should not read or write to the wrapped StreamPeer directly.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 构造函数

### PacketPeerStream

```csharp
public PacketPeerStream()
```

## 属性

### InputBufferMaxSize

```csharp
public int InputBufferMaxSize { get; set; }
```

引擎名：`input_buffer_max_size`

### OutputBufferMaxSize

```csharp
public int OutputBufferMaxSize { get; set; }
```

引擎名：`output_buffer_max_size`

### StreamPeer

```csharp
public StreamPeer StreamPeer { get; set; }
```

引擎名：`stream_peer`

The wrapped [`StreamPeer`](StreamPeer.md) object.

## 方法

### GetInputBufferMaxSize

```csharp
public int GetInputBufferMaxSize()
```

引擎名：`get_input_buffer_max_size`

### GetOutputBufferMaxSize

```csharp
public int GetOutputBufferMaxSize()
```

引擎名：`get_output_buffer_max_size`

### GetStreamPeer

```csharp
public StreamPeer GetStreamPeer()
```

引擎名：`get_stream_peer`

### SetInputBufferMaxSize

```csharp
public void SetInputBufferMaxSize(int maxSizeBytes)
```

引擎名：`set_input_buffer_max_size`

### SetOutputBufferMaxSize

```csharp
public void SetOutputBufferMaxSize(int maxSizeBytes)
```

引擎名：`set_output_buffer_max_size`

### SetStreamPeer

```csharp
public void SetStreamPeer(StreamPeer peer)
```

引擎名：`set_stream_peer`
