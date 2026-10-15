<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DtlsServer

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is used to store the state of a DTLS server. Upon `Setup()` it converts connected [`PacketPeerUdp`](PacketPeerUdp.md) to [`PacketPeerDtls`](PacketPeerDtls.md) accepting them via `TakeConnection()` as DTLS clients. Under the hood, this class is used to store the DTLS state and cookies of the server. The reason of why the state and cookies are needed is outside of the scope of this documentation.

Below a small example of how to use it:

```csharp
// ServerNode.cs
using Godot;

public partial class ServerNode : Node
{
private DtlsServer _dtls = new DtlsServer();
private UdpServer _server = new UdpServer();
private Godot.Collections.Array<PacketPeerDtls> _peers = [];

public override void _Ready()
{
_server.Listen(4242);
var key = GD.Load<CryptoKey>("key.key"); // Your private key.
var cert = GD.Load<X509Certificate>("cert.crt"); // Your X509 certificate.
_dtls.Setup(TlsOptions.Server(key, cert));
}

public override void _Process(double delta)
{
while (_server.IsConnectionAvailable())
{
PacketPeerUdp peer = _server.TakeConnection();
PacketPeerDtls dtlsPeer = _dtls.TakeConnection(peer);
if (dtlsPeer.GetStatus() != PacketPeerDtls.Status.Handshaking)
{
continue; // It is normal that 50% of the connections fails due to cookie exchange.
}
GD.Print("Peer connected!");
_peers.Add(dtlsPeer);
}

foreach (var p in _peers)
{
p.Poll(); // Must poll to update the state.
if (p.GetStatus() == PacketPeerDtls.Status.Connected)
{
while (p.GetAvailablePacketCount() > 0)
{
GD.Print($"Received Message From Client: {p.GetPacket().GetStringFromUtf8()}");
p.PutPacket("Hello DTLS Client".ToUtf8Buffer());
}
}
}
}
}

```

```csharp
// ClientNode.cs
using Godot;
using System.Text;

public partial class ClientNode : Node
{
private PacketPeerDtls _dtls = new PacketPeerDtls();
private PacketPeerUdp _udp = new PacketPeerUdp();
private bool _connected = false;

public override void _Ready()
{
_udp.ConnectToHost("127.0.0.1", 4242);
_dtls.ConnectToPeer(_udp, validateCerts: false); // Use true in production for certificate validation!
}

public override void _Process(double delta)
{
_dtls.Poll();
if (_dtls.GetStatus() == PacketPeerDtls.Status.Connected)
{
if (!_connected)
{
// Try to contact server
_dtls.PutPacket("The Answer Is..42!".ToUtf8Buffer());
}
while (_dtls.GetAvailablePacketCount() > 0)
{
GD.Print($"Connected: {_dtls.GetPacket().GetStringFromUtf8()}");
_connected = true;
}
}
}
}

```

## 构造函数

### DtlsServer

```csharp
public DtlsServer()
```

## 方法

### Setup

```csharp
public Error Setup(TlsOptions serverOptions)
```

Setup the DTLS server to use the given `serverOptions`. See [`TlsOptions.Server`](TlsOptions.md).

### TakeConnection

```csharp
public PacketPeerDtls TakeConnection(PacketPeerUdp udpPeer)
```

Try to initiate the DTLS handshake with the given `udpPeer` which must be already connected (see [`PacketPeerUdp.ConnectToHost`](PacketPeerUdp.md)).

**Note:** You must check that the state of the return PacketPeerUDP is [`PacketPeerDtls.Status.Handshaking`](PacketPeerDtls.md), as it is normal that 50% of the new connections will be invalid due to cookie exchange.
