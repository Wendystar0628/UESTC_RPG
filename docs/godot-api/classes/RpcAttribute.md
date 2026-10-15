<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RpcAttribute

命名空间 `Godot`；class

Attribute that changes the RPC mode for the annotated `method` to the given `Mode`,
optionally specifying the `TransferMode` and `TransferChannel` (on supported peers).
See [`MultiplayerApi.RpcMode`](MultiplayerApi.md) and [`MultiplayerPeer.TransferModeEnum`](MultiplayerPeer.md).
By default, methods are not exposed to networking (and RPCs).

## 构造函数

### RpcAttribute

```csharp
public RpcAttribute(MultiplayerApi.RpcMode mode = MultiplayerApi.RpcMode.Authority)
```

Constructs a `RpcAttribute` instance.

参数：
- `mode`：The RPC mode to use.

## 属性

### CallLocal

```csharp
public bool CallLocal { get; set; }
```

If the method will also be called locally; otherwise, it is only called remotely.

### Mode

```csharp
public MultiplayerApi.RpcMode Mode { get; }
```

RPC mode for the annotated method.

### TransferChannel

```csharp
public int TransferChannel { get; set; }
```

Transfer channel for the annotated mode.

### TransferMode

```csharp
public MultiplayerPeer.TransferModeEnum TransferMode { get; set; }
```

Transfer mode for the annotated method.
