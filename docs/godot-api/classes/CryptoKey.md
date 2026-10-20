<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CryptoKey

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The CryptoKey class represents a cryptographic key. Keys can be loaded and saved like any other [`Resource`](Resource.md).

They can be used to generate a self-signed [`X509Certificate`](X509Certificate.md) via [`Crypto.GenerateSelfSignedCertificate`](Crypto.md) and as private key in [`StreamPeerTls.AcceptStream`](StreamPeerTls.md) along with the appropriate certificate.

## 构造函数

### CryptoKey

```csharp
public CryptoKey()
```

## 方法

### IsPublicOnly

```csharp
public bool IsPublicOnly()
```

引擎名：`is_public_only`

Returns `true` if this CryptoKey only has the public part, and not the private one.

### Load

```csharp
public Error Load(string path, bool publicOnly = false)
```

引擎名：`load`

Loads a key from `path`. If `publicOnly` is `true`, only the public key will be loaded.

**Note:** `path` should be a "*.pub" file if `publicOnly` is `true`, a "*.key" file otherwise.

### LoadFromString

```csharp
public Error LoadFromString(string stringKey, bool publicOnly = false)
```

引擎名：`load_from_string`

Loads a key from the given `stringKey`. If `publicOnly` is `true`, only the public key will be loaded.

### Save

```csharp
public Error Save(string path, bool publicOnly = false)
```

引擎名：`save`

Saves a key to the given `path`. If `publicOnly` is `true`, only the public key will be saved.

**Note:** `path` should be a "*.pub" file if `publicOnly` is `true`, a "*.key" file otherwise.

### SaveToString

```csharp
public string SaveToString(bool publicOnly = false)
```

引擎名：`save_to_string`

Returns a string containing the key in PEM format. If `publicOnly` is `true`, only the public key will be included.
