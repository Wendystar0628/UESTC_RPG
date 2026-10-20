<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StreamPeerGZip

命名空间 `Godot`；class；继承 [`StreamPeer`](StreamPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class allows to compress or decompress data using GZIP/deflate in a streaming fashion. This is particularly useful when compressing or decompressing files that have to be sent through the network without needing to allocate them all in memory.

After starting the stream via `StartCompression()` (or `StartDecompression()`), calling [`StreamPeer.PutPartialData`](StreamPeer.md) on this stream will compress (or decompress) the data, writing it to the internal buffer. Calling [`StreamPeer.GetAvailableBytes`](StreamPeer.md) will return the pending bytes in the internal buffer, and [`StreamPeer.GetPartialData`](StreamPeer.md) will retrieve the compressed (or decompressed) bytes from it. When the stream is over, you must call `Finish()` to ensure the internal buffer is properly flushed (make sure to call [`StreamPeer.GetAvailableBytes`](StreamPeer.md) on last time to check if more data needs to be read after that).

## 构造函数

### StreamPeerGZip

```csharp
public StreamPeerGZip()
```

## 方法

### Clear

```csharp
public void Clear()
```

Clears this stream, resetting the internal state.

### Finish

```csharp
public Error Finish()
```

Finalizes the stream, compressing any buffered chunk left.

You must call it only when you are compressing.

### StartCompression

```csharp
public Error StartCompression(bool useDeflate = false, int bufferSize = 65535)
```

Start the stream in compression mode with the given `bufferSize`, if `useDeflate` is `true` uses deflate instead of GZIP.

### StartDecompression

```csharp
public Error StartDecompression(bool useDeflate = false, int bufferSize = 65535)
```

Start the stream in decompression mode with the given `bufferSize`, if `useDeflate` is `true` uses deflate instead of GZIP.
