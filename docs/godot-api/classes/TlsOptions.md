<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TlsOptions

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

TLSOptions abstracts the configuration options for the [`StreamPeerTls`](StreamPeerTls.md) and [`PacketPeerDtls`](PacketPeerDtls.md) classes.

Objects of this class cannot be instantiated directly, and one of the static methods `Client()`, `ClientUnsafe()`, or `Server()` should be used instead.

## 方法

### Client

```csharp
public static TlsOptions Client(X509Certificate trustedChain = null, string commonNameOverride = "")
```

Creates a TLS client configuration which validates certificates and their common names (fully qualified domain names).

You can specify a custom `trustedChain` of certification authorities (the default CA list will be used if `null`), and optionally provide a `commonNameOverride` if you expect the certificate to have a common name other than the server FQDN.

**Note:** On the Web platform, TLS verification is always enforced against the CA list of the web browser. This is considered a security feature.

### ClientUnsafe

```csharp
public static TlsOptions ClientUnsafe(X509Certificate trustedChain = null)
```

Creates an **unsafe** TLS client configuration where certificate validation is optional. You can optionally provide a valid `trustedChain`, but the common name of the certificates will never be checked. Using this configuration for purposes other than testing **is not recommended**.

**Note:** On the Web platform, TLS verification is always enforced against the CA list of the web browser. This is considered a security feature.

### GetCommonNameOverride

```csharp
public string GetCommonNameOverride()
```

Returns the common name (domain name) override specified when creating with `Client()`.

### GetOwnCertificate

```csharp
public X509Certificate GetOwnCertificate()
```

Returns the [`X509Certificate`](X509Certificate.md) specified when creating with `Server()`.

### GetPrivateKey

```csharp
public CryptoKey GetPrivateKey()
```

Returns the [`CryptoKey`](CryptoKey.md) specified when creating with `Server()`.

### GetTrustedCaChain

```csharp
public X509Certificate GetTrustedCaChain()
```

Returns the CA [`X509Certificate`](X509Certificate.md) chain specified when creating with `Client()` or `ClientUnsafe()`.

### IsServer

```csharp
public bool IsServer()
```

Returns `true` if created with `Server()`, `false` otherwise.

### IsUnsafeClient

```csharp
public bool IsUnsafeClient()
```

Returns `true` if created with `ClientUnsafe()`, `false` otherwise.

### Server

```csharp
public static TlsOptions Server(CryptoKey key, X509Certificate certificate)
```

Creates a TLS server configuration using the provided `key` and `certificate`.

**Note:** The `certificate` should include the full certificate chain up to the signing CA (certificates file can be concatenated using a general purpose text editor).
