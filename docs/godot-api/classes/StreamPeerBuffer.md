<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StreamPeerBuffer

命名空间 `Godot`；class；继承 [`StreamPeer`](StreamPeer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A data buffer stream peer that uses a byte array as the stream. This object can be used to handle binary data from network sessions. To handle binary data stored in files, [`FileAccess`](FileAccess.md) can be used directly.

A `StreamPeerBuffer` object keeps an internal cursor which is the offset in bytes to the start of the buffer. Get and put operations are performed at the cursor position and will move the cursor accordingly.

## 构造函数

### StreamPeerBuffer

```csharp
public StreamPeerBuffer()
```

## 属性

### DataArray

```csharp
public byte[] DataArray { get; set; }
```

引擎名：`data_array`

The underlying data buffer. Setting this value resets the cursor.

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the `DataArray` and resets the cursor.

### Duplicate

```csharp
public StreamPeerBuffer Duplicate()
```

引擎名：`duplicate`

Returns a new `StreamPeerBuffer` with the same `DataArray` content.

### GetDataArray

```csharp
public byte[] GetDataArray()
```

引擎名：`get_data_array`

### GetPosition

```csharp
public int GetPosition()
```

引擎名：`get_position`

Returns the current cursor position.

### GetSize

```csharp
public int GetSize()
```

引擎名：`get_size`

Returns the size of `DataArray`.

### Resize

```csharp
public void Resize(int size)
```

引擎名：`resize`

Resizes the `DataArray`. This *doesn't* update the cursor.

### Seek

```csharp
public void Seek(int position)
```

引擎名：`seek`

Moves the cursor to the specified position. `position` must be a valid index of `DataArray`.

### SetDataArray

```csharp
public void SetDataArray(ReadOnlySpan<byte> data)
```

引擎名：`set_data_array`

### SetDataArray

```csharp
public void SetDataArray(byte[] data)
```

引擎名：`set_data_array`
