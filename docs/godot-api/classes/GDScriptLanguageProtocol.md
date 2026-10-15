<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GDScriptLanguageProtocol

命名空间 `Godot`；static class；仅编辑器（GodotSharpEditor）

Provides access to certain features that are implemented in the language server.

**Note:** This class is not a language server client that can be used to access LSP functionality. It only provides access to a limited set of features that is implemented using the same technical foundation as the language server.

## 属性

### Singleton

```csharp
public static GDScriptLanguageProtocolInstance Singleton { get; }
```

## 方法

### GetTextDocument

```csharp
public static GDScriptTextDocument GetTextDocument()
```

引擎名：`get_text_document`

Returns the language server's [`GDScriptTextDocument`](GDScriptTextDocument.md) instance.

### GetWorkspace

```csharp
public static GDScriptWorkspace GetWorkspace()
```

引擎名：`get_workspace`

Returns the language server's [`GDScriptWorkspace`](GDScriptWorkspace.md) instance.

### Initialize

```csharp
public static Variant Initialize(Dictionary params)
```

引擎名：`initialize`

### Initialized

```csharp
public static void Initialized(Variant params)
```

引擎名：`initialized`

### IsInitialized

```csharp
public static bool IsInitialized()
```

引擎名：`is_initialized`

Returns `true` if the language server was initialized by a language server client, `false` otherwise.

### IsSmartResolveEnabled

```csharp
public static bool IsSmartResolveEnabled()
```

引擎名：`is_smart_resolve_enabled`

Returns `true` if the language server is providing the smart resolve feature, `false` otherwise. The feature can be configured through the editor settings.

### NotifyClient

```csharp
public static void NotifyClient(string method, Variant params = default, int clientId = -1)
```

引擎名：`notify_client`

### OnClientConnected

```csharp
public static Error OnClientConnected()
```

引擎名：`on_client_connected`

### OnClientDisconnected

```csharp
public static void OnClientDisconnected(int clientId)
```

引擎名：`on_client_disconnected`
