<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# IPInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

IP contains support functions for the Internet Protocol (IP). TCP/IP support is in different classes (see [`StreamPeerTcp`](StreamPeerTcp.md) and [`TcpServer`](TcpServer.md)). IP provides DNS hostname resolution support, both blocking and threaded.

## 方法

### ClearCache

```csharp
public void ClearCache(string hostname = "")
```

Removes all of a `hostname`'s cached references. If no `hostname` is given, all cached IP addresses are removed.

### EraseResolveItem

```csharp
public void EraseResolveItem(int id)
```

Removes a given item `id` from the queue. This should be used to free a queue after it has completed to enable more queries to happen.

### GetLocalAddresses

```csharp
public string[] GetLocalAddresses()
```

Returns all the user's current IPv4 and IPv6 addresses as an array.

### GetLocalInterfaces

```csharp
public Array<Dictionary> GetLocalInterfaces()
```

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
public string GetResolveItemAddress(int id)
```

Returns a queued hostname's IP address, given its queue `id`. Returns an empty string on error or if resolution hasn't happened yet (see `GetResolveItemStatus()`).

### GetResolveItemAddresses

```csharp
public Array GetResolveItemAddresses(int id)
```

Returns resolved addresses, or an empty array if an error happened or resolution didn't happen yet (see `GetResolveItemStatus()`).

### GetResolveItemStatus

```csharp
public IP.ResolverStatus GetResolveItemStatus(int id)
```

Returns a queued hostname's status as a [`IP.ResolverStatus`](IP.md) constant, given its queue `id`.

### ResolveHostname

```csharp
public string ResolveHostname(string host, IP.Type iPType = IP.Type.Any)
```

Returns a given hostname's IPv4 or IPv6 address when resolved (blocking-type method). The address type returned depends on the [`IP.Type`](IP.md) constant given as `iPType`.

### ResolveHostnameAddresses

```csharp
public string[] ResolveHostnameAddresses(string host, IP.Type iPType = IP.Type.Any)
```

Resolves a given hostname in a blocking way. Addresses are returned as an [`Collections.Array`](Collections.Array.md) of IPv4 or IPv6 addresses depending on `iPType`.

### ResolveHostnameQueueItem

```csharp
public int ResolveHostnameQueueItem(string host, IP.Type iPType = IP.Type.Any)
```

Creates a queue item to resolve a hostname to an IPv4 or IPv6 address depending on the [`IP.Type`](IP.md) constant given as `iPType`. Returns the queue ID if successful, or [`IP.ResolverInvalidId`](IP.md) on error.
