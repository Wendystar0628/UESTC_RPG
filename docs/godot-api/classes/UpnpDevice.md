<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# UpnpDevice

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Universal Plug and Play (UPnP) device. See [`Upnp`](Upnp.md) for UPnP discovery and utility functions. Provides low-level access to UPNP control commands. Allows to manage port mappings (port forwarding) and to query network information of the device (like local and external IP address and status). Note that methods on this class are synchronous and block the calling thread.

## 构造函数

### UpnpDevice

```csharp
public UpnpDevice()
```

## 属性

### DescriptionUrl

```csharp
public string DescriptionUrl { get; set; }
```

URL to the device description.

### IgdControlUrl

```csharp
public string IgdControlUrl { get; set; }
```

IDG control URL.

### IgdOurAddr

```csharp
public string IgdOurAddr { get; set; }
```

Address of the local machine in the network connecting it to this `UpnpDevice`.

### IgdServiceType

```csharp
public string IgdServiceType { get; set; }
```

IGD service type.

### IgdStatus

```csharp
public UpnpDevice.IgdStatusEnum IgdStatus { get; set; }
```

IGD status.

### ServiceType

```csharp
public string ServiceType { get; set; }
```

Service type.

## 方法

### AddPortMapping

```csharp
public int AddPortMapping(int port, int portInternal = 0, string desc = "", string proto = "UDP", int duration = 0)
```

Adds a port mapping to forward the given external port on this `UpnpDevice` for the given protocol to the local machine. See [`Upnp.AddPortMapping`](Upnp.md).

### DeletePortMapping

```csharp
public int DeletePortMapping(int port, string proto = "UDP")
```

Deletes the port mapping identified by the given port and protocol combination on this device. See [`Upnp.DeletePortMapping`](Upnp.md).

### GetDescriptionUrl

```csharp
public string GetDescriptionUrl()
```

### GetIgdControlUrl

```csharp
public string GetIgdControlUrl()
```

### GetIgdOurAddr

```csharp
public string GetIgdOurAddr()
```

### GetIgdServiceType

```csharp
public string GetIgdServiceType()
```

### GetIgdStatus

```csharp
public UpnpDevice.IgdStatusEnum GetIgdStatus()
```

### GetServiceType

```csharp
public string GetServiceType()
```

### IsValidGateway

```csharp
public bool IsValidGateway()
```

Returns `true` if this is a valid IGD (InternetGatewayDevice) which potentially supports port forwarding.

### QueryExternalAddress

```csharp
public string QueryExternalAddress()
```

Returns the external IP address of this `UpnpDevice` or an empty string.

### SetDescriptionUrl

```csharp
public void SetDescriptionUrl(string url)
```

### SetIgdControlUrl

```csharp
public void SetIgdControlUrl(string url)
```

### SetIgdOurAddr

```csharp
public void SetIgdOurAddr(string addr)
```

### SetIgdServiceType

```csharp
public void SetIgdServiceType(string type)
```

### SetIgdStatus

```csharp
public void SetIgdStatus(UpnpDevice.IgdStatusEnum status)
```

### SetServiceType

```csharp
public void SetServiceType(string type)
```

## IgdStatusEnum

命名空间 `Godot`；enum

### 枚举值

- `Ok = 0`：OK.
- `HttpError = 1`：HTTP error.
- `HttpEmpty = 2`：Empty HTTP response.
- `NoUrls = 3`：Returned response contained no URLs.
- `NoIgd = 4`：Not a valid IGD.
- `Disconnected = 5`：Disconnected.
- `UnknownDevice = 6`：Unknown device.
- `InvalidControl = 7`：Invalid control.
- `MallocError = 8`：Memory allocation error.
- `UnknownError = 9`：Unknown error.
