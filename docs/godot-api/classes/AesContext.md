<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AesContext

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class holds the context information required for encryption and decryption operations with AES (Advanced Encryption Standard). Both AES-ECB and AES-CBC modes are supported.

```csharp
using Godot;
using System.Diagnostics;

public partial class MyNode : Node
{
private AesContext _aes = new AesContext();

public override void _Ready()
{
string key = "My secret key!!!"; // Key must be either 16 or 32 bytes.
string data = "My secret text!!"; // Data size must be multiple of 16 bytes, apply padding if needed.
// Encrypt ECB
_aes.Start(AesContext.Mode.EcbEncrypt, key.ToUtf8Buffer());
byte[] encrypted = _aes.Update(data.ToUtf8Buffer());
_aes.Finish();
// Decrypt ECB
_aes.Start(AesContext.Mode.EcbDecrypt, key.ToUtf8Buffer());
byte[] decrypted = _aes.Update(encrypted);
_aes.Finish();
// Check ECB
Debug.Assert(decrypted == data.ToUtf8Buffer());

string iv = "My secret iv!!!!"; // IV must be of exactly 16 bytes.
// Encrypt CBC
_aes.Start(AesContext.Mode.EcbEncrypt, key.ToUtf8Buffer(), iv.ToUtf8Buffer());
encrypted = _aes.Update(data.ToUtf8Buffer());
_aes.Finish();
// Decrypt CBC
_aes.Start(AesContext.Mode.EcbDecrypt, key.ToUtf8Buffer(), iv.ToUtf8Buffer());
decrypted = _aes.Update(encrypted);
_aes.Finish();
// Check CBC
Debug.Assert(decrypted == data.ToUtf8Buffer());
}
}

```

## 构造函数

### AesContext

```csharp
public AesContext()
```

## 方法

### Finish

```csharp
public void Finish()
```

Close this AES context so it can be started again. See `Start()`.

### GetIVState

```csharp
public byte[] GetIVState()
```

Get the current IV state for this context (IV gets updated when calling `Update()`). You normally don't need this function.

**Note:** This function only makes sense when the context is started with [`AesContext.Mode.CbcEncrypt`](AesContext.md) or [`AesContext.Mode.CbcDecrypt`](AesContext.md).

### Start

```csharp
public Error Start(AesContext.Mode mode, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iV)
```

Start the AES context in the given `mode`. A `key` of either 16 or 32 bytes must always be provided, while an `iV` (initialization vector) of exactly 16 bytes, is only needed when `mode` is either [`AesContext.Mode.CbcEncrypt`](AesContext.md) or [`AesContext.Mode.CbcDecrypt`](AesContext.md).

### Start

```csharp
public Error Start(AesContext.Mode mode, byte[] key, byte[] iV = null)
```

Start the AES context in the given `mode`. A `key` of either 16 or 32 bytes must always be provided, while an `iV` (initialization vector) of exactly 16 bytes, is only needed when `mode` is either [`AesContext.Mode.CbcEncrypt`](AesContext.md) or [`AesContext.Mode.CbcDecrypt`](AesContext.md).

参数：
- `iV`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### Update

```csharp
public byte[] Update(ReadOnlySpan<byte> src)
```

Run the desired operation for this AES context. Will return a `System.Byte`[] containing the result of encrypting (or decrypting) the given `src`. See `Start()` for mode of operation.

**Note:** The size of `src` must be a multiple of 16. Apply some padding if needed.

### Update

```csharp
public byte[] Update(byte[] src)
```

Run the desired operation for this AES context. Will return a `System.Byte`[] containing the result of encrypting (or decrypting) the given `src`. See `Start()` for mode of operation.

**Note:** The size of `src` must be a multiple of 16. Apply some padding if needed.

## Mode

命名空间 `Godot`；enum

### 枚举值

- `EcbEncrypt = 0`：AES electronic codebook encryption mode.
- `EcbDecrypt = 1`：AES electronic codebook decryption mode.
- `CbcEncrypt = 2`：AES cipher block chaining encryption mode.
- `CbcDecrypt = 3`：AES cipher block chaining decryption mode.
- `Max = 4`：Maximum value for the mode enum.
