<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PacketPeerUdp

命名空间 `Godot`；class；继承 [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

UDP packet peer. Can be used to send and receive raw UDP packets as well as [`Variant`](Variant.md)s.

**Example:** Send a packet:

```csharp
var peer = PacketPeerUDP.new()

# Optionally, you can select the local port used to send the packet.
peer.bind(4444)

peer.set_dest_address("1.1.1.1", 4433)
peer.put_packet("hello".to_utf8_buffer())

```

**Example:** Listen for packets:

```csharp
var peer

func _ready():
peer = PacketPeerUDP.new()
peer.bind(4433)

func _process(_delta):
if peer.get_available_packet_count() > 0:
var array_bytes = peer.get_packet()
var packet_string = array_bytes.get_string_from_ascii()
print("Received message: ", packet_string)

```

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 构造函数

### PacketPeerUdp

```csharp
public PacketPeerUdp()
```

## 方法

### Bind

```csharp
public Error Bind(int port, string bindAddress = "*", int recvBufSize = 65536)
```

Binds this `PacketPeerUdp` to the specified `port` and `bindAddress` with a buffer size `recvBufSize`, allowing it to receive incoming packets.

If `bindAddress` is set to `"*"` (default), the peer will be bound on all available addresses (both IPv4 and IPv6).

If `bindAddress` is set to `"0.0.0.0"` (for IPv4) or `"::"` (for IPv6), the peer will be bound to all available addresses matching that IP type.

If `bindAddress` is set to any valid address (e.g. `"192.168.1.101"`, `"::1"`, etc.), the peer will only be bound to the interface with that address (or fail if no interface with the given address exists).

### Close

```csharp
public void Close()
```

Closes the `PacketPeerUdp`'s underlying UDP socket.

### ConnectToHost

```csharp
public Error ConnectToHost(string host, int port)
```

Calling this method connects this UDP peer to the given `host`/`port` pair. UDP is in reality connectionless, so this option only means that incoming packets from different addresses are automatically discarded, and that outgoing packets are always sent to the connected address (future calls to `SetDestAddress()` are not allowed). This method does not send any data to the remote peer, to do that, use [`PacketPeer.PutVar`](PacketPeer.md) or [`PacketPeer.PutPacket`](PacketPeer.md) as usual. See also [`UdpServer`](UdpServer.md).

**Note:** Connecting to the remote peer does not help to protect from malicious attacks like IP spoofing, etc. Think about using an encryption technique like TLS or DTLS if you feel like your application is transferring sensitive information.

### GetLocalPort

```csharp
public int GetLocalPort()
```

Returns the local port to which this peer is bound.

### GetPacketIP

```csharp
public string GetPacketIP()
```

Returns the IP of the remote peer that sent the last packet(that was received with [`PacketPeer.GetPacket`](PacketPeer.md) or [`PacketPeer.GetVar`](PacketPeer.md)).

### GetPacketPort

```csharp
public int GetPacketPort()
```

Returns the port of the remote peer that sent the last packet(that was received with [`PacketPeer.GetPacket`](PacketPeer.md) or [`PacketPeer.GetVar`](PacketPeer.md)).

### IsBound

```csharp
public bool IsBound()
```

Returns whether this `PacketPeerUdp` is bound to an address and can receive packets.

### IsSocketConnected

```csharp
public bool IsSocketConnected()
```

Returns `true` if the UDP socket is open and has been connected to a remote address. See `ConnectToHost()`.

### JoinMulticastGroup

```csharp
public Error JoinMulticastGroup(string multicastAddress, string interfaceName)
```

Joins the multicast group specified by `multicastAddress` using the interface identified by `interfaceName`.

You can join the same multicast group with multiple interfaces. Use [`IP.GetLocalInterfaces`](IP.md) to know which are available.

**Note:** Some Android devices might require the `CHANGE_WIFI_MULTICAST_STATE` permission for multicast to work.

### LeaveMulticastGroup

```csharp
public Error LeaveMulticastGroup(string multicastAddress, string interfaceName)
```

Removes the interface identified by `interfaceName` from the multicast group specified by `multicastAddress`.

### SetBroadcastEnabled

```csharp
public void SetBroadcastEnabled(bool enabled)
```

Enable or disable sending of broadcast packets (e.g. `set_dest_address("255.255.255.255", 4343)`. This option is disabled by default.

**Note:** Some Android devices might require the `CHANGE_WIFI_MULTICAST_STATE` permission and this option to be enabled to receive broadcast packets too.

### SetDestAddress

```csharp
public Error SetDestAddress(string host, int port)
```

Sets the destination address and port for sending packets and variables. A hostname will be resolved using DNS if needed.

**Note:** `SetBroadcastEnabled()` must be enabled before sending packets to a broadcast address (e.g. `255.255.255.255`).

### Wait

```csharp
public Error Wait()
```

Waits for a packet to arrive on the bound address. See `Bind()`.

**Note:** `Wait()` can't be interrupted once it has been called. This can be worked around by allowing the other party to send a specific "death pill" packet like this:

```csharp
var socket = new PacketPeerUdp();
// Server
socket.SetDestAddress("127.0.0.1", 789);
socket.PutPacket("Time to stop".ToAsciiBuffer());

// Client
while (socket.Wait() == OK)
{
string data = socket.GetPacket().GetStringFromASCII();
if (data == "Time to stop")
{
return;
}
}

```
