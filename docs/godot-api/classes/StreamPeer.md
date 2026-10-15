<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StreamPeer

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

StreamPeer is an abstract base class mostly used for stream-based protocols (such as TCP). It provides an API for sending and receiving data through streams as raw data or strings.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

## 属性

### BigEndian

```csharp
public bool BigEndian { get; set; }
```

引擎名：`big_endian`

If `true`, this `StreamPeer` will using big-endian format for encoding and decoding.

## 方法

### Get16

```csharp
public short Get16()
```

Gets a signed 16-bit value from the stream.

### Get32

```csharp
public int Get32()
```

Gets a signed 32-bit value from the stream.

### Get64

```csharp
public long Get64()
```

Gets a signed 64-bit value from the stream.

### Get8

```csharp
public sbyte Get8()
```

Gets a signed byte from the stream.

### GetAvailableBytes

```csharp
public int GetAvailableBytes()
```

引擎名：`get_available_bytes`

Returns the number of bytes this `StreamPeer` has available.

### GetData

```csharp
public Array GetData(int bytes)
```

引擎名：`get_data`

Returns a chunk data with the received bytes, as an [`Collections.Array`](Collections.Array.md) containing two elements: an [`Error`](Error.md) constant and a `System.Byte`[]. `bytes` is the number of bytes to be received. If not enough bytes are available, the function will block until the desired amount is received.

### GetDouble

```csharp
public double GetDouble()
```

引擎名：`get_double`

Gets a double-precision float from the stream.

### GetFloat

```csharp
public float GetFloat()
```

引擎名：`get_float`

Gets a single-precision float from the stream.

### GetHalf

```csharp
public float GetHalf()
```

引擎名：`get_half`

Gets a half-precision float from the stream.

### GetPartialData

```csharp
public Array GetPartialData(int bytes)
```

引擎名：`get_partial_data`

Returns a chunk data with the received bytes, as an [`Collections.Array`](Collections.Array.md) containing two elements: an [`Error`](Error.md) constant and a `System.Byte`[]. `bytes` is the number of bytes to be received. If not enough bytes are available, the function will return how many were actually received.

### GetString

```csharp
public string GetString(int bytes = -1)
```

引擎名：`get_string`

Gets an ASCII string with byte-length `bytes` from the stream. If `bytes` is negative (default) the length will be read from the stream using the reverse process of `PutString()`.

### GetU16

```csharp
public ushort GetU16()
```

引擎名：`get_u16`

Gets an unsigned 16-bit value from the stream.

### GetU32

```csharp
public uint GetU32()
```

引擎名：`get_u32`

Gets an unsigned 32-bit value from the stream.

### GetU64

```csharp
public ulong GetU64()
```

引擎名：`get_u64`

Gets an unsigned 64-bit value from the stream.

### GetU8

```csharp
public byte GetU8()
```

引擎名：`get_u8`

Gets an unsigned byte from the stream.

### GetUtf8String

```csharp
public string GetUtf8String(int bytes = -1)
```

引擎名：`get_utf8_string`

Gets a UTF-8 string with byte-length `bytes` from the stream (this decodes the string sent as UTF-8). If `bytes` is negative (default) the length will be read from the stream using the reverse process of `PutUtf8String()`.

### GetVar

```csharp
public Variant GetVar(bool allowObjects = false)
```

引擎名：`get_var`

Gets a Variant from the stream. If `allowObjects` is `true`, decoding objects is allowed.

Internally, this uses the same decoding mechanism as the `GD.BytesToVar` method.

**Warning:** Deserialized objects can contain code which gets executed. Do not use this option if the serialized object comes from untrusted sources to avoid potential security threats such as remote code execution.

### IsBigEndianEnabled

```csharp
public bool IsBigEndianEnabled()
```

引擎名：`is_big_endian_enabled`

### Put16

```csharp
public void Put16(short value)
```

Puts a signed 16-bit value into the stream.

### Put32

```csharp
public void Put32(int value)
```

Puts a signed 32-bit value into the stream.

### Put64

```csharp
public void Put64(long value)
```

Puts a signed 64-bit value into the stream.

### Put8

```csharp
public void Put8(sbyte value)
```

Puts a signed byte into the stream.

### PutData

```csharp
public Error PutData(ReadOnlySpan<byte> data)
```

引擎名：`put_data`

Sends a chunk of data through the connection, blocking if necessary until the data is done sending. This function returns an [`Error`](Error.md) code.

### PutData

```csharp
public Error PutData(byte[] data)
```

引擎名：`put_data`

Sends a chunk of data through the connection, blocking if necessary until the data is done sending. This function returns an [`Error`](Error.md) code.

### PutDouble

```csharp
public void PutDouble(double value)
```

引擎名：`put_double`

Puts a double-precision float into the stream.

### PutFloat

```csharp
public void PutFloat(float value)
```

引擎名：`put_float`

Puts a single-precision float into the stream.

### PutHalf

```csharp
public void PutHalf(float value)
```

引擎名：`put_half`

Puts a half-precision float into the stream.

### PutPartialData

```csharp
public Array PutPartialData(ReadOnlySpan<byte> data)
```

引擎名：`put_partial_data`

Sends a chunk of data through the connection. If all the data could not be sent at once, only part of it will. This function returns two values, an [`Error`](Error.md) code and an integer, describing how much data was actually sent.

### PutPartialData

```csharp
public Array PutPartialData(byte[] data)
```

引擎名：`put_partial_data`

Sends a chunk of data through the connection. If all the data could not be sent at once, only part of it will. This function returns two values, an [`Error`](Error.md) code and an integer, describing how much data was actually sent.

### PutString

```csharp
public void PutString(string value)
```

引擎名：`put_string`

Puts a zero-terminated ASCII string into the stream prepended by a 32-bit unsigned integer representing its size.

**Note:** To put an ASCII string without prepending its size, you can use `PutData()`:

```csharp
PutData("Hello World".ToAsciiBuffer());

```

### PutU16

```csharp
public void PutU16(ushort value)
```

引擎名：`put_u16`

Puts an unsigned 16-bit value into the stream.

### PutU32

```csharp
public void PutU32(uint value)
```

引擎名：`put_u32`

Puts an unsigned 32-bit value into the stream.

### PutU64

```csharp
public void PutU64(ulong value)
```

引擎名：`put_u64`

Puts an unsigned 64-bit value into the stream.

### PutU8

```csharp
public void PutU8(byte value)
```

引擎名：`put_u8`

Puts an unsigned byte into the stream.

### PutUtf8String

```csharp
public void PutUtf8String(string value)
```

引擎名：`put_utf8_string`

Puts a zero-terminated UTF-8 string into the stream prepended by a 32 bits unsigned integer representing its size.

**Note:** To put a UTF-8 string without prepending its size, you can use `PutData()`:

```csharp
PutData("Hello World".ToUtf8Buffer());

```

### PutVar

```csharp
public void PutVar(Variant value, bool fullObjects = false)
```

引擎名：`put_var`

Puts a Variant into the stream. If `fullObjects` is `true` encoding objects is allowed (and can potentially include code).

Internally, this uses the same encoding mechanism as the `GD.VarToBytes` method.

### SetBigEndian

```csharp
public void SetBigEndian(bool enable)
```

引擎名：`set_big_endian`
