<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PacketPeer

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

PacketPeer is an abstraction and base class for packet-based protocols (such as UDP). It provides an API for sending and receiving packets both as raw data or variables. This makes it easy to transfer data over a protocol, without having to encode data as low-level bytes or having to worry about network ordering.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 属性

### EncodeBufferMaxSize

```csharp
public int EncodeBufferMaxSize { get; set; }
```

引擎名：`encode_buffer_max_size`

Maximum buffer size allowed when encoding [`Variant`](Variant.md)s. Raise this value to support heavier memory allocations.

The `PutVar()` method allocates memory on the stack, and the buffer used will grow automatically to the closest power of two to match the size of the [`Variant`](Variant.md). If the [`Variant`](Variant.md) is bigger than `EncodeBufferMaxSize`, the method will error out with [`Error.OutOfMemory`](Error.md).

## 方法

### GetAvailablePacketCount

```csharp
public int GetAvailablePacketCount()
```

引擎名：`get_available_packet_count`

Returns the number of packets currently available in the ring-buffer.

### GetEncodeBufferMaxSize

```csharp
public int GetEncodeBufferMaxSize()
```

引擎名：`get_encode_buffer_max_size`

### GetPacket

```csharp
public byte[] GetPacket()
```

引擎名：`get_packet`

Gets a raw packet.

### GetPacketError

```csharp
public Error GetPacketError()
```

引擎名：`get_packet_error`

Returns the error state of the last packet received (via `GetPacket()` and `GetVar()`).

### GetVar

```csharp
public Variant GetVar(bool allowObjects = false)
```

引擎名：`get_var`

Gets a Variant. If `allowObjects` is `true`, decoding objects is allowed.

Internally, this uses the same decoding mechanism as the `GD.BytesToVar` method.

**Warning:** Deserialized objects can contain code which gets executed. Do not use this option if the serialized object comes from untrusted sources to avoid potential security threats such as remote code execution.

### PutPacket

```csharp
public Error PutPacket(ReadOnlySpan<byte> buffer)
```

引擎名：`put_packet`

Sends a raw packet.

### PutPacket

```csharp
public Error PutPacket(byte[] buffer)
```

引擎名：`put_packet`

Sends a raw packet.

### PutVar

```csharp
public Error PutVar(Variant var, bool fullObjects = false)
```

引擎名：`put_var`

Sends a [`Variant`](Variant.md) as a packet. If `fullObjects` is `true`, encoding objects is allowed (and can potentially include code).

Internally, this uses the same encoding mechanism as the `GD.VarToBytes` method.

### SetEncodeBufferMaxSize

```csharp
public void SetEncodeBufferMaxSize(int maxSize)
```

引擎名：`set_encode_buffer_max_size`
