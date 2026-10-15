<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# IP

命名空间 `Godot`；static class

IP contains support functions for the Internet Protocol (IP). TCP/IP support is in different classes (see [`StreamPeerTcp`](StreamPeerTcp.md) and [`TcpServer`](TcpServer.md)). IP provides DNS hostname resolution support, both blocking and threaded.

## 属性

### Singleton

```csharp
public static IPInstance Singleton { get; }
```

## 方法

### ClearCache

```csharp
public static void ClearCache(string hostname = "")
```

引擎名：`clear_cache`

Removes all of a `hostname`'s cached references. If no `hostname` is given, all cached IP addresses are removed.

### EraseResolveItem

```csharp
public static void EraseResolveItem(int id)
```

引擎名：`erase_resolve_item`

Removes a given item `id` from the queue. This should be used to free a queue after it has completed to enable more queries to happen.

### GetLocalAddresses

```csharp
public static string[] GetLocalAddresses()
```

引擎名：`get_local_addresses`

Returns all the user's current IPv4 and IPv6 addresses as an array.

### GetLocalInterfaces

```csharp
public static Array<Dictionary> GetLocalInterfaces()
```

引擎名：`get_local_interfaces`

Returns all network adapters as an array.

Each adapter is a dictionary of the form:

```csharp
{
"index": "1", # Interface index.
"name": "eth0", # Interface name.
"friendly": "Ethernet One", # A friendly name (might be empty).
"addresses": ["192.168.1.101"], # An array of IP addresses associated to this interface.
}

```

### GetResolveItemAddress

```csharp
public static string GetResolveItemAddress(int id)
```

引擎名：`get_resolve_item_address`

Returns a queued hostname's IP address, given its queue `id`. Returns an empty string on error or if resolution hasn't happened yet (see `GetResolveItemStatus()`).

### GetResolveItemAddresses

```csharp
public static Array GetResolveItemAddresses(int id)
```

引擎名：`get_resolve_item_addresses`

Returns resolved addresses, or an empty array if an error happened or resolution didn't happen yet (see `GetResolveItemStatus()`).

### GetResolveItemStatus

```csharp
public static IP.ResolverStatus GetResolveItemStatus(int id)
```

引擎名：`get_resolve_item_status`

Returns a queued hostname's status as a [`IP.ResolverStatus`](IP.md) constant, given its queue `id`.

### ResolveHostname

```csharp
public static string ResolveHostname(string host, IP.Type iPType = IP.Type.Any)
```

引擎名：`resolve_hostname`

Returns a given hostname's IPv4 or IPv6 address when resolved (blocking-type method). The address type returned depends on the [`IP.Type`](IP.md) constant given as `iPType`.

### ResolveHostnameAddresses

```csharp
public static string[] ResolveHostnameAddresses(string host, IP.Type iPType = IP.Type.Any)
```

引擎名：`resolve_hostname_addresses`

Resolves a given hostname in a blocking way. Addresses are returned as an [`Collections.Array`](Collections.Array.md) of IPv4 or IPv6 addresses depending on `iPType`.

### ResolveHostnameQueueItem

```csharp
public static int ResolveHostnameQueueItem(string host, IP.Type iPType = IP.Type.Any)
```

引擎名：`resolve_hostname_queue_item`

Creates a queue item to resolve a hostname to an IPv4 or IPv6 address depending on the [`IP.Type`](IP.md) constant given as `iPType`. Returns the queue ID if successful, or `ResolverInvalidId` on error.

## 常量

### ResolverInvalidId

```csharp
public const long ResolverInvalidId = -1
```

Invalid ID constant. Returned if `ResolverMaxQueries` is exceeded.

### ResolverMaxQueries

```csharp
public const long ResolverMaxQueries = 256
```

Maximum number of concurrent DNS resolver queries allowed, `ResolverInvalidId` is returned if exceeded.

## ResolverStatus

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：DNS hostname resolver status: No status.
- `Waiting = 1`：DNS hostname resolver status: Waiting.
- `Done = 2`：DNS hostname resolver status: Done.
- `Error = 3`：DNS hostname resolver status: Error.

## Type

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Address type: None.
- `Ipv4 = 1`：Address type: Internet protocol version 4 (IPv4).
- `Ipv6 = 2`：Address type: Internet protocol version 6 (IPv6).
- `Any = 3`：Address type: Any.
