<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# X509Certificate

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The X509Certificate class represents an X509 certificate. Certificates can be loaded and saved like any other [`Resource`](Resource.md).

They can be used as the server certificate in [`StreamPeerTls.AcceptStream`](StreamPeerTls.md) (along with the proper [`CryptoKey`](CryptoKey.md)), and to specify the only certificate that should be accepted when connecting to a TLS server via [`StreamPeerTls.ConnectToStream`](StreamPeerTls.md).

## 构造函数

### X509Certificate

```csharp
public X509Certificate()
```

## 方法

### Load

```csharp
public Error Load(string path)
```

引擎名：`load`

Loads a certificate from `path` ("*.crt" file).

### LoadFromString

```csharp
public Error LoadFromString(string string)
```

引擎名：`load_from_string`

Loads a certificate from the given `string`.

### Save

```csharp
public Error Save(string path)
```

引擎名：`save`

Saves a certificate to the given `path` (should be a "*.crt" file).

### SaveToString

```csharp
public string SaveToString()
```

引擎名：`save_to_string`

Returns a string representation of the certificate, or an empty string if the certificate is invalid.
