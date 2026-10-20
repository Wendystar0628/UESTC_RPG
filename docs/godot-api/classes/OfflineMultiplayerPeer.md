<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OfflineMultiplayerPeer

命名空间 `Godot`；class；继承 [`MultiplayerPeer`](MultiplayerPeer.md) → [`PacketPeer`](PacketPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This is the default [`MultiplayerApi.MultiplayerPeer`](MultiplayerApi.md) for the [`Node.Multiplayer`](Node.md). It mimics the behavior of a server with no peers connected.

This means that the [`SceneTree`](SceneTree.md) will act as the multiplayer authority by default. Calls to [`MultiplayerApi.IsServer`](MultiplayerApi.md) will return `true`, and calls to [`MultiplayerApi.GetUniqueId`](MultiplayerApi.md) will return [`MultiplayerPeer.TargetPeerServer`](MultiplayerPeer.md).

## 构造函数

### OfflineMultiplayerPeer

```csharp
public OfflineMultiplayerPeer()
```
