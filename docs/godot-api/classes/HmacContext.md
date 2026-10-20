<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# HmacContext

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The HMACContext class is useful for advanced HMAC use cases, such as streaming the message as it supports creating the message over time rather than providing it all at once.

```csharp
using Godot;
using System.Diagnostics;

public partial class MyNode : Node
{
private HmacContext _ctx = new HmacContext();

public override void _Ready()
{
byte[] key = "supersecret".ToUtf8Buffer();
Error err = _ctx.Start(HashingContext.HashType.Sha256, key);
Debug.Assert(err == Error.Ok);
byte[] msg1 = "this is ".ToUtf8Buffer();
byte[] msg2 = "super duper secret".ToUtf8Buffer();
err = _ctx.Update(msg1);
Debug.Assert(err == Error.Ok);
err = _ctx.Update(msg2);
Debug.Assert(err == Error.Ok);
byte[] hmac = _ctx.Finish();
GD.Print(hmac.HexEncode());
}
}

```

## 构造函数

### HmacContext

```csharp
public HmacContext()
```

## 方法

### Finish

```csharp
public byte[] Finish()
```

Returns the resulting HMAC. If the HMAC failed, an empty `System.Byte`[] is returned.

### Start

```csharp
public Error Start(HashingContext.HashType hashType, ReadOnlySpan<byte> key)
```

Initializes the HMACContext. This method cannot be called again on the same HMACContext until `Finish()` has been called.

### Start

```csharp
public Error Start(HashingContext.HashType hashType, byte[] key)
```

Initializes the HMACContext. This method cannot be called again on the same HMACContext until `Finish()` has been called.

### Update

```csharp
public Error Update(ReadOnlySpan<byte> data)
```

Updates the message to be HMACed. This can be called multiple times before `Finish()` is called to append `data` to the message, but cannot be called until `Start()` has been called.

### Update

```csharp
public Error Update(byte[] data)
```

Updates the message to be HMACed. This can be called multiple times before `Finish()` is called to append `data` to the message, but cannot be called until `Start()` has been called.
