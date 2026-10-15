<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Upnp

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class can be used to discover compatible [`UpnpDevice`](UpnpDevice.md)s on the local network and execute commands on them, like managing port mappings (for port forwarding/NAT traversal) and querying the local and remote network IP address. Note that methods on this class are synchronous and block the calling thread.

To forward a specific port (here `7777`, note both `Discover()` and `AddPortMapping()` can return errors that should be checked):

```csharp
var upnp = UPNP.new()
upnp.discover()
upnp.add_port_mapping(7777)

```

To close a specific port (e.g. after you have finished using it):

```csharp
upnp.delete_port_mapping(port)

```

**Note:** UPnP discovery blocks the current thread. To perform discovery without blocking the main thread, use [`GodotThread`](GodotThread.md)s like this:

```csharp
# Emitted when UPnP port mapping setup is completed (regardless of success or failure).
signal upnp_completed(error)

# Replace this with your own server port number between 1024 and 65535.
const SERVER_PORT = 3928
var thread = null

func _upnp_setup(server_port):
# UPNP queries take some time.
var upnp = UPNP.new()
var err = upnp.discover()

if err != UPNP.UPNP_RESULT_SUCCESS:
push_error(str(err))
upnp_completed.emit(err)
return

if upnp.get_gateway() and upnp.get_gateway().is_valid_gateway():
upnp.add_port_mapping(server_port, server_port, ProjectSettings.get_setting("application/config/name"), "UDP")
upnp.add_port_mapping(server_port, server_port, ProjectSettings.get_setting("application/config/name"), "TCP")
upnp_completed.emit(UPNP.UPNP_RESULT_SUCCESS)

func _ready():
thread = Thread.new()
thread.start(_upnp_setup.bind(SERVER_PORT))

func _exit_tree():
# Wait for thread finish here to handle game exit while the thread is running.
thread.wait_to_finish()

```

**Terminology:** In the context of UPnP networking, "gateway" (or "internet gateway device", short IGD) refers to network devices that allow computers in the local network to access the internet ("wide area network", WAN). These gateways are often also called "routers".

**Pitfalls:**

- As explained above, these calls are blocking and shouldn't be run on the main thread, especially as they can block for multiple seconds at a time. Use threading!

- Networking is physical and messy. Packets get lost in transit or get filtered, addresses, free ports and assigned mappings change, and devices may leave or join the network at any time. Be mindful of this, be diligent when checking and handling errors, and handle these gracefully if you can: add clear error UI, timeouts and re-try handling.

- Port mappings may change (and be removed) at any time, and the remote/external IP address of the gateway can change likewise. You should consider re-querying the external IP and try to update/refresh the port mapping periodically (for example, every 5 minutes and on networking failures).

- Not all devices support UPnP, and some users disable UPnP support. You need to handle this (e.g. documenting and requiring the user to manually forward ports, or adding alternative methods of NAT traversal, like a relay/mirror server, or NAT hole punching, STUN/TURN, etc.).

- Consider what happens on mapping conflicts. Maybe multiple users on the same network would like to play your game at the same time, or maybe another application uses the same port. Make the port configurable, and optimally choose a port automatically (re-trying with a different port on failure).

**Further reading:** If you want to know more about UPnP (and the Internet Gateway Device (IGD) and Port Control Protocol (PCP) specifically), [Wikipedia](https://en.wikipedia.org/wiki/Universal_Plug_and_Play) is a good first stop, the specification can be found at the [Open Connectivity Foundation](https://openconnectivity.org/developer/specifications/upnp-resources/upnp/) and Godot's implementation is based on the [MiniUPnP client](https://github.com/miniupnp/miniupnp).

## 构造函数

### Upnp

```csharp
public Upnp()
```

## 属性

### DiscoverIpv6

```csharp
public bool DiscoverIpv6 { get; set; }
```

If `true`, IPv6 is used for [`UpnpDevice`](UpnpDevice.md) discovery.

### DiscoverLocalPort

```csharp
public int DiscoverLocalPort { get; set; }
```

If `0`, the local port to use for discovery is chosen automatically by the system. If `1`, discovery will be done from the source port 1900 (same as destination port). Otherwise, the value will be used as the port.

### DiscoverMulticastIf

```csharp
public string DiscoverMulticastIf { get; set; }
```

Multicast interface to use for discovery. Uses the default multicast interface if empty.

## 方法

### AddDevice

```csharp
public void AddDevice(UpnpDevice device)
```

Adds the given [`UpnpDevice`](UpnpDevice.md) to the list of discovered devices.

### AddPortMapping

```csharp
public int AddPortMapping(int port, int portInternal = 0, string desc = "", string proto = "UDP", int duration = 0)
```

Adds a mapping to forward the external `port` (between 1 and 65535, although recommended to use port 1024 or above) on the default gateway (see `GetGateway()`) to the `portInternal` on the local machine for the given protocol `proto` (either `"TCP"` or `"UDP"`, with UDP being the default). If a port mapping for the given port and protocol combination already exists on that gateway device, this method tries to overwrite it. If that is not desired, you can retrieve the gateway manually with `GetGateway()` and call `AddPortMapping()` on it, if any. Note that forwarding a well-known port (below 1024) with UPnP may fail depending on the device.

Depending on the gateway device, if a mapping for that port already exists, it will either be updated or it will refuse this command due to that conflict, especially if the existing mapping for that port wasn't created via UPnP or points to a different network address (or device) than this one.

If `portInternal` is `0` (the default), the same port number is used for both the external and the internal port (the `port` value).

The description (`desc`) is shown in some routers management UIs and can be used to point out which application added the mapping.

The mapping's lease `duration` can be limited by specifying a duration in seconds. The default of `0` means no duration, i.e. a permanent lease and notably some devices only support these permanent leases. Note that whether permanent or not, this is only a request and the gateway may still decide at any point to remove the mapping (which usually happens on a reboot of the gateway, when its external IP address changes, or on some models when it detects a port mapping has become inactive, i.e. had no traffic for multiple minutes). If not `0` (permanent), the allowed range according to spec is between `120` (2 minutes) and `86400` seconds (24 hours).

See [`Upnp.UpnpResult`](Upnp.md) for possible return values.

### ClearDevices

```csharp
public void ClearDevices()
```

Clears the list of discovered devices.

### DeletePortMapping

```csharp
public int DeletePortMapping(int port, string proto = "UDP")
```

Deletes the port mapping for the given port and protocol combination on the default gateway (see `GetGateway()`) if one exists. `port` must be a valid port between 1 and 65535, `proto` can be either `"TCP"` or `"UDP"`. May be refused for mappings pointing to addresses other than this one, for well-known ports (below 1024), or for mappings not added via UPnP. See [`Upnp.UpnpResult`](Upnp.md) for possible return values.

### Discover

```csharp
public int Discover(int timeout = 2000, int ttl = 2, string deviceFilter = "InternetGatewayDevice")
```

Discovers local [`UpnpDevice`](UpnpDevice.md)s. Clears the list of previously discovered devices.

Filters for IGD (InternetGatewayDevice) type devices by default, as those manage port forwarding. `timeout` is the time to wait for responses in milliseconds. `ttl` is the time-to-live; only touch this if you know what you're doing.

See [`Upnp.UpnpResult`](Upnp.md) for possible return values.

### GetDevice

```csharp
public UpnpDevice GetDevice(int index)
```

Returns the [`UpnpDevice`](UpnpDevice.md) at the given `index`.

### GetDeviceCount

```csharp
public int GetDeviceCount()
```

Returns the number of discovered [`UpnpDevice`](UpnpDevice.md)s.

### GetDiscoverLocalPort

```csharp
public int GetDiscoverLocalPort()
```

### GetDiscoverMulticastIf

```csharp
public string GetDiscoverMulticastIf()
```

### GetGateway

```csharp
public UpnpDevice GetGateway()
```

Returns the default gateway. That is the first discovered [`UpnpDevice`](UpnpDevice.md) that is also a valid IGD (InternetGatewayDevice).

### IsDiscoverIpv6

```csharp
public bool IsDiscoverIpv6()
```

### QueryExternalAddress

```csharp
public string QueryExternalAddress()
```

Returns the external [`IP`](IP.md) address of the default gateway (see `GetGateway()`) as string. Returns an empty string on error.

### RemoveDevice

```csharp
public void RemoveDevice(int index)
```

Removes the device at `index` from the list of discovered devices.

### SetDevice

```csharp
public void SetDevice(int index, UpnpDevice device)
```

Sets the device at `index` from the list of discovered devices to `device`.

### SetDiscoverIpv6

```csharp
public void SetDiscoverIpv6(bool ipv6)
```

### SetDiscoverLocalPort

```csharp
public void SetDiscoverLocalPort(int port)
```

### SetDiscoverMulticastIf

```csharp
public void SetDiscoverMulticastIf(string mIf)
```

## UpnpResult

命名空间 `Godot`；enum

### 枚举值

- `Success = 0`：UPNP command or discovery was successful.
- `NotAuthorized = 1`：Not authorized to use the command on the [`UpnpDevice`](UpnpDevice.md).
- `PortMappingNotFound = 2`：No port mapping was found for the given port, protocol combination on the given [`UpnpDevice`](UpnpDevice.md).
- `InconsistentParameters = 3`：Inconsistent parameters.
- `NoSuchEntryInArray = 4`：No such entry in array.
- `ActionFailed = 5`：The action failed.
- `SrcIPWildcardNotPermitted = 6`：The [`UpnpDevice`](UpnpDevice.md) does not allow wildcard values for the source IP address.
- `ExtPortWildcardNotPermitted = 7`：The [`UpnpDevice`](UpnpDevice.md) does not allow wildcard values for the external port.
- `IntPortWildcardNotPermitted = 8`：The [`UpnpDevice`](UpnpDevice.md) does not allow wildcard values for the internal port.
- `RemoteHostMustBeWildcard = 9`：The remote host value must be a wildcard.
- `ExtPortMustBeWildcard = 10`：The external port value must be a wildcard.
- `NoPortMapsAvailable = 11`：No port maps are available.
- `ConflictWithOtherMechanism = 12`：Conflict with other mechanism.
- `ConflictWithOtherMapping = 13`：Conflict with an existing port mapping.
- `SamePortValuesRequired = 14`：External and internal port values must be the same.
- `OnlyPermanentLeaseSupported = 15`：Only permanent leases are supported.
- `InvalidGateway = 16`：Invalid gateway.
- `InvalidPort = 17`：Invalid port.
- `InvalidProtocol = 18`：Invalid protocol.
- `InvalidDuration = 19`：Invalid duration.
- `InvalidArgs = 20`：Invalid arguments.
- `InvalidResponse = 21`：Invalid response.
- `InvalidParam = 22`：Invalid parameter.
- `HttpError = 23`：HTTP error.
- `SocketError = 24`：Socket error.
- `MemAllocError = 25`：Error allocating memory.
- `NoGateway = 26`：No gateway available.
- `NoDevices = 27`：No devices available.
- `UnknownError = 28`：Unknown error.
