<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# HashingContext

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The HashingContext class provides an interface for computing cryptographic hashes over multiple iterations. Useful for computing hashes of big files (so you don't have to load them all in memory), network streams, and data streams in general (so you don't have to hold buffers).

The [`HashingContext.HashType`](HashingContext.md) enum shows the supported hashing algorithms.

```csharp
public const int ChunkSize = 1024;

public void HashFile(string path)
{
// Check that file exists.
if (!FileAccess.FileExists(path))
{
return;
}
// Start an SHA-256 context.
var ctx = new HashingContext();
ctx.Start(HashingContext.HashType.Sha256);
// Open the file to hash.
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
// Update the context after reading each chunk.
while (file.GetPosition() < file.GetLength())
{
int remaining = (int)(file.GetLength() - file.GetPosition());
ctx.Update(file.GetBuffer(Mathf.Min(remaining, ChunkSize)));
}
// Get the computed hash.
byte[] res = ctx.Finish();
// Print the result as hex string and array.
GD.PrintT(res.HexEncode(), (Variant)res);
}

```

## 构造函数

### HashingContext

```csharp
public HashingContext()
```

## 方法

### Finish

```csharp
public byte[] Finish()
```

引擎名：`finish`

Closes the current context, and return the computed hash.

### Start

```csharp
public Error Start(HashingContext.HashType type)
```

引擎名：`start`

Starts a new hash computation of the given `type` (e.g. [`HashingContext.HashType.Sha256`](HashingContext.md) to start computation of an SHA-256).

### Update

```csharp
public Error Update(ReadOnlySpan<byte> chunk)
```

引擎名：`update`

Updates the computation with the given `chunk` of data.

### Update

```csharp
public Error Update(byte[] chunk)
```

引擎名：`update`

Updates the computation with the given `chunk` of data.

## HashType

命名空间 `Godot`；enum

### 枚举值

- `Md5 = 0`：Hashing algorithm: MD5.
- `Sha1 = 1`：Hashing algorithm: SHA-1.
- `Sha256 = 2`：Hashing algorithm: SHA-256.
