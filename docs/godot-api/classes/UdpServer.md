<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# UdpServer

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A simple server that opens a UDP socket and returns connected [`PacketPeerUdp`](PacketPeerUdp.md) upon receiving new packets. See also [`PacketPeerUdp.ConnectToHost`](PacketPeerUdp.md).

After starting the server (`Listen()`), you will need to `Poll()` it at regular intervals (e.g. inside [`Node._Process()`](Node.md)) for it to process new packets, delivering them to the appropriate [`PacketPeerUdp`](PacketPeerUdp.md), and taking new connections.

Below a small example of how it can be used:

```csharp
// ServerNode.cs
using Godot;
using System.Collections.Generic;

public partial class ServerNode : Node
{
private UdpServer _server = new UdpServer();
private List<PacketPeerUdp> _peers  = new List<PacketPeerUdp>();

public override void _Ready()
{
_server.Listen(4242);
}

public override void _Process(double delta)
{
_server.Poll(); // Important!
if (_server.IsConnectionAvailable())
{
PacketPeerUdp peer = _server.TakeConnection();
byte[] packet = peer.GetPacket();
GD.Print($"Accepted Peer: {peer.GetPacketIP()}:{peer.GetPacketPort()}");
GD.Print($"Received Data: {packet.GetStringFromUtf8()}");
// Reply so it knows we received the message.
peer.PutPacket(packet);
// Keep a reference so we can keep contacting the remote peer.
_peers.Add(peer);
}
foreach (var peer in _peers)
{
// Do something with the peers.
}
}
}

```

```csharp
// ClientNode.cs
using Godot;

public partial class ClientNode : Node
{
private PacketPeerUdp _udp = new PacketPeerUdp();
private bool _connected = false;

public override void _Ready()
{
_udp.ConnectToHost("127.0.0.1", 4242);
}

public override void _Process(double delta)
{
if (!_connected)
{
// Try to contact server
_udp.PutPacket("The Answer Is..42!".ToUtf8Buffer());
}
if (_udp.GetAvailablePacketCount() > 0)
{
GD.Print($"Connected: {_udp.GetPacket().GetStringFromUtf8()}");
_connected = true;
}
}
}

```

## 构造函数

### UdpServer

```csharp
public UdpServer()
```

## 属性

### MaxPendingConnections

```csharp
public int MaxPendingConnections { get; set; }
```

Define the maximum number of pending connections, during `Poll()`, any new pending connection exceeding that value will be automatically dropped. Setting this value to `0` effectively prevents any new pending connection to be accepted (e.g. when all your players have connected).

## 方法

### GetLocalPort

```csharp
public int GetLocalPort()
```

Returns the local port this server is listening to.

### GetMaxPendingConnections

```csharp
public int GetMaxPendingConnections()
```

### IsConnectionAvailable

```csharp
public bool IsConnectionAvailable()
```

Returns `true` if a packet with a new address/port combination was received on the socket.

### IsListening

```csharp
public bool IsListening()
```

Returns `true` if the socket is open and listening on a port.

### Listen

```csharp
public Error Listen(ushort port, string bindAddress = "*")
```

Starts the server by opening a UDP socket listening on the given `port`. You can optionally specify a `bindAddress` to only listen for packets sent to that address. See also [`PacketPeerUdp.Bind`](PacketPeerUdp.md).

### Poll

```csharp
public Error Poll()
```

Call this method at regular intervals (e.g. inside [`Node._Process()`](Node.md)) to process new packets. Any packet from a known address/port pair will be delivered to the appropriate [`PacketPeerUdp`](PacketPeerUdp.md), while any packet received from an unknown address/port pair will be added as a pending connection (see `IsConnectionAvailable()` and `TakeConnection()`). The maximum number of pending connections is defined via `MaxPendingConnections`.

### SetMaxPendingConnections

```csharp
public void SetMaxPendingConnections(int maxPendingConnections)
```

### Stop

```csharp
public void Stop()
```

Stops the server, closing the UDP socket if open. Will close all connected [`PacketPeerUdp`](PacketPeerUdp.md) accepted via `TakeConnection()` (remote peers will not be notified).

### TakeConnection

```csharp
public PacketPeerUdp TakeConnection()
```

Returns the first pending connection (connected to the appropriate address/port). Will return `null` if no new connection is available. See also `IsConnectionAvailable()`, [`PacketPeerUdp.ConnectToHost`](PacketPeerUdp.md).
