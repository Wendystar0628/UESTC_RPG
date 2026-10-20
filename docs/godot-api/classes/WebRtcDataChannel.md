<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WebRtcDataChannel

命名空间 `Godot`；class；继承 [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 属性

### WriteMode

```csharp
public WebRtcDataChannel.WriteModeEnum WriteMode { get; set; }
```

The transfer mode to use when sending outgoing packet. Either text or binary.

## 方法

### Close

```csharp
public void Close()
```

Closes this data channel, notifying the other peer.

### GetBufferedAmount

```csharp
public int GetBufferedAmount()
```

Returns the number of bytes currently queued to be sent over this channel.

### GetId

```csharp
public int GetId()
```

Returns the ID assigned to this channel during creation (or auto-assigned during negotiation).

If the channel is not negotiated out-of-band the ID will only be available after the connection is established (will return `65535` until then).

### GetLabel

```csharp
public string GetLabel()
```

Returns the label assigned to this channel during creation.

### GetMaxPacketLifeTime

```csharp
public int GetMaxPacketLifeTime()
```

Returns the `maxPacketLifeTime` value assigned to this channel during creation.

Will be `65535` if not specified.

### GetMaxRetransmits

```csharp
public int GetMaxRetransmits()
```

Returns the `maxRetransmits` value assigned to this channel during creation.

Will be `65535` if not specified.

### GetProtocol

```csharp
public string GetProtocol()
```

Returns the sub-protocol assigned to this channel during creation. An empty string if not specified.

### GetReadyState

```csharp
public WebRtcDataChannel.ChannelState GetReadyState()
```

Returns the current state of this channel.

### GetWriteMode

```csharp
public WebRtcDataChannel.WriteModeEnum GetWriteMode()
```

### IsNegotiated

```csharp
public bool IsNegotiated()
```

Returns `true` if this channel was created with out-of-band configuration.

### IsOrdered

```csharp
public bool IsOrdered()
```

Returns `true` if this channel was created with ordering enabled (default).

### Poll

```csharp
public Error Poll()
```

Reserved, but not used for now.

### SetWriteMode

```csharp
public void SetWriteMode(WebRtcDataChannel.WriteModeEnum writeMode)
```

### WasStringPacket

```csharp
public bool WasStringPacket()
```

Returns `true` if the last received packet was transferred as text. See `WriteMode`.

## ChannelState

命名空间 `Godot`；enum

### 枚举值

- `Connecting = 0`：The channel was created, but it's still trying to connect.
- `Open = 1`：The channel is currently open, and data can flow over it.
- `Closing = 2`：The channel is being closed, no new messages will be accepted, but those already in queue will be flushed.
- `Closed = 3`：The channel was closed, or connection failed.

## WriteModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Text = 0`：Tells the channel to send data over this channel as text.
- `Binary = 1`：Tells the channel to send data over this channel as binary.
