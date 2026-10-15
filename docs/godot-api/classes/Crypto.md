<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Crypto

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The Crypto class provides access to advanced cryptographic functionalities.

Currently, this includes asymmetric key encryption/decryption, signing/verification, and generating cryptographically secure random bytes, RSA keys, HMAC digests, and self-signed [`X509Certificate`](X509Certificate.md)s.

```csharp
using Godot;
using System.Diagnostics;

Crypto crypto = new Crypto();

// Generate new RSA key.
CryptoKey key = crypto.GenerateRsa(4096);

// Generate new self-signed certificate with the given key.
X509Certificate cert = crypto.GenerateSelfSignedCertificate(key, "CN=mydomain.com,O=My Game Company,C=IT");

// Save key and certificate in the user folder.
key.Save("user://generated.key");
cert.Save("user://generated.crt");

// Encryption
string data = "Some data";
byte[] encrypted = crypto.Encrypt(key, data.ToUtf8Buffer());

// Decryption
byte[] decrypted = crypto.Decrypt(key, encrypted);

// Signing
byte[] signature = crypto.Sign(HashingContext.HashType.Sha256, Data.Sha256Buffer(), key);

// Verifying
bool verified = crypto.Verify(HashingContext.HashType.Sha256, Data.Sha256Buffer(), signature, key);

// Checks
Debug.Assert(verified);
Debug.Assert(data.ToUtf8Buffer() == decrypted);

```

## 构造函数

### Crypto

```csharp
public Crypto()
```

## 方法

### ConstantTimeCompare

```csharp
public bool ConstantTimeCompare(ReadOnlySpan<byte> trusted, ReadOnlySpan<byte> received)
```

引擎名：`constant_time_compare`

Compares two `System.Byte`[]s for equality without leaking timing information in order to prevent timing attacks.

See [this blog post](https://paragonie.com/blog/2015/11/preventing-timing-attacks-on-string-comparison-with-double-hmac-strategy) for more information.

### ConstantTimeCompare

```csharp
public bool ConstantTimeCompare(byte[] trusted, byte[] received)
```

引擎名：`constant_time_compare`

Compares two `System.Byte`[]s for equality without leaking timing information in order to prevent timing attacks.

See [this blog post](https://paragonie.com/blog/2015/11/preventing-timing-attacks-on-string-comparison-with-double-hmac-strategy) for more information.

### Decrypt

```csharp
public byte[] Decrypt(CryptoKey key, ReadOnlySpan<byte> ciphertext)
```

引擎名：`decrypt`

Decrypt the given `ciphertext` with the provided private `key`.

**Note:** The maximum size of accepted ciphertext is limited by the key size.

### Decrypt

```csharp
public byte[] Decrypt(CryptoKey key, byte[] ciphertext)
```

引擎名：`decrypt`

Decrypt the given `ciphertext` with the provided private `key`.

**Note:** The maximum size of accepted ciphertext is limited by the key size.

### Encrypt

```csharp
public byte[] Encrypt(CryptoKey key, ReadOnlySpan<byte> plaintext)
```

引擎名：`encrypt`

Encrypt the given `plaintext` with the provided public `key`.

**Note:** The maximum size of accepted plaintext is limited by the key size.

### Encrypt

```csharp
public byte[] Encrypt(CryptoKey key, byte[] plaintext)
```

引擎名：`encrypt`

Encrypt the given `plaintext` with the provided public `key`.

**Note:** The maximum size of accepted plaintext is limited by the key size.

### GenerateRandomBytes

```csharp
public byte[] GenerateRandomBytes(int size)
```

引擎名：`generate_random_bytes`

Generates a `System.Byte`[] of cryptographically secure random bytes with given `size`.

### GenerateRsa

```csharp
public CryptoKey GenerateRsa(int size)
```

引擎名：`generate_rsa`

Generates an RSA [`CryptoKey`](CryptoKey.md) that can be used for creating self-signed certificates and passed to [`StreamPeerTls.AcceptStream`](StreamPeerTls.md).

### GenerateSelfSignedCertificate

```csharp
public X509Certificate GenerateSelfSignedCertificate(CryptoKey key, string issuerName = "CN=myserver,O=myorganisation,C=IT", string notBefore = "20140101000000", string notAfter = "20340101000000")
```

引擎名：`generate_self_signed_certificate`

Generates a self-signed [`X509Certificate`](X509Certificate.md) from the given [`CryptoKey`](CryptoKey.md) and `issuerName`. The certificate validity will be defined by `notBefore` and `notAfter` (first valid date and last valid date). The `issuerName` must contain at least "CN=" (common name, i.e. the domain name), "O=" (organization, i.e. your company name), "C=" (country, i.e. 2 lettered ISO-3166 code of the country the organization is based in).

A small example to generate an RSA key and an X509 self-signed certificate.

```csharp
var crypto = new Crypto();
// Generate 4096 bits RSA key.
CryptoKey key = crypto.GenerateRsa(4096);
// Generate self-signed certificate using the given key.
X509Certificate cert = crypto.GenerateSelfSignedCertificate(key, "CN=mydomain.com,O=My Game Company,C=IT");

```

### HmacDigest

```csharp
public byte[] HmacDigest(HashingContext.HashType hashType, ReadOnlySpan<byte> key, ReadOnlySpan<byte> msg)
```

引擎名：`hmac_digest`

Generates an [HMAC](https://en.wikipedia.org/wiki/HMAC) digest of `msg` using `key`. The `hashType` parameter is the hashing algorithm that is used for the inner and outer hashes.

Currently, only [`HashingContext.HashType.Sha256`](HashingContext.md) and [`HashingContext.HashType.Sha1`](HashingContext.md) are supported.

### HmacDigest

```csharp
public byte[] HmacDigest(HashingContext.HashType hashType, byte[] key, byte[] msg)
```

引擎名：`hmac_digest`

Generates an [HMAC](https://en.wikipedia.org/wiki/HMAC) digest of `msg` using `key`. The `hashType` parameter is the hashing algorithm that is used for the inner and outer hashes.

Currently, only [`HashingContext.HashType.Sha256`](HashingContext.md) and [`HashingContext.HashType.Sha1`](HashingContext.md) are supported.

### Sign

```csharp
public byte[] Sign(HashingContext.HashType hashType, ReadOnlySpan<byte> hash, CryptoKey key)
```

引擎名：`sign`

Sign a given `hash` of type `hashType` with the provided private `key`.

### Sign

```csharp
public byte[] Sign(HashingContext.HashType hashType, byte[] hash, CryptoKey key)
```

引擎名：`sign`

Sign a given `hash` of type `hashType` with the provided private `key`.

### Verify

```csharp
public bool Verify(HashingContext.HashType hashType, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, CryptoKey key)
```

引擎名：`verify`

Verify that a given `signature` for `hash` of type `hashType` against the provided public `key`.

### Verify

```csharp
public bool Verify(HashingContext.HashType hashType, byte[] hash, byte[] signature, CryptoKey key)
```

引擎名：`verify`

Verify that a given `signature` for `hash` of type `hashType` against the provided public `key`.
