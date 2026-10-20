<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WebRtcPeerConnectionExtension

命名空间 `Godot`；class；继承 [`WebRtcPeerConnection`](WebRtcPeerConnection.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### WebRtcPeerConnectionExtension

```csharp
public WebRtcPeerConnectionExtension()
```

## 方法

### _AddIceCandidate

```csharp
public virtual Error _AddIceCandidate(string sdpMidName, int sdpMlineIndex, string sdpName)
```

### _Close

```csharp
public virtual void _Close()
```

### _CreateDataChannel

```csharp
public virtual WebRtcDataChannel _CreateDataChannel(string label, Dictionary config)
```

### _CreateOffer

```csharp
public virtual Error _CreateOffer()
```

### _GetConnectionState

```csharp
public virtual WebRtcPeerConnection.ConnectionState _GetConnectionState()
```

### _GetGatheringState

```csharp
public virtual WebRtcPeerConnection.GatheringState _GetGatheringState()
```

### _GetSignalingState

```csharp
public virtual WebRtcPeerConnection.SignalingState _GetSignalingState()
```

### _Initialize

```csharp
public virtual Error _Initialize(Dictionary config)
```

### _Poll

```csharp
public virtual Error _Poll()
```

### _SetLocalDescription

```csharp
public virtual Error _SetLocalDescription(string type, string sdp)
```

### _SetRemoteDescription

```csharp
public virtual Error _SetRemoteDescription(string type, string sdp)
```
