<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GDScriptLanguageProtocolInstance

命名空间 `Godot`；class；继承 [`JsonRpc`](JsonRpc.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Provides access to certain features that are implemented in the language server.

**Note:** This class is not a language server client that can be used to access LSP functionality. It only provides access to a limited set of features that is implemented using the same technical foundation as the language server.

## 方法

### GetTextDocument

```csharp
public GDScriptTextDocument GetTextDocument()
```

Returns the language server's [`GDScriptTextDocument`](GDScriptTextDocument.md) instance.

### GetWorkspace

```csharp
public GDScriptWorkspace GetWorkspace()
```

Returns the language server's [`GDScriptWorkspace`](GDScriptWorkspace.md) instance.

### Initialize

```csharp
public Variant Initialize(Dictionary params)
```

### Initialized

```csharp
public void Initialized(Variant params)
```

### IsInitialized

```csharp
public bool IsInitialized()
```

Returns `true` if the language server was initialized by a language server client, `false` otherwise.

### IsSmartResolveEnabled

```csharp
public bool IsSmartResolveEnabled()
```

Returns `true` if the language server is providing the smart resolve feature, `false` otherwise. The feature can be configured through the editor settings.

### NotifyClient

```csharp
public void NotifyClient(string method, Variant params = default, int clientId = -1)
```

### OnClientConnected

```csharp
public Error OnClientConnected()
```

### OnClientDisconnected

```csharp
public void OnClientDisconnected(int clientId)
```
