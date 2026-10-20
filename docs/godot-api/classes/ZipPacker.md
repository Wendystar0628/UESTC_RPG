<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ZipPacker

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class implements a writer that allows storing the multiple blobs in a ZIP archive. See also [`ZipReader`](ZipReader.md) and [`PckPacker`](PckPacker.md).

```csharp
# Create a ZIP archive with a single file at its root.
func write_zip_file():
var writer = ZIPPacker.new()
var err = writer.open("user://archive.zip")
if err != OK:
return err
writer.start_file("hello.txt")
writer.write_file("Hello World".to_utf8_buffer())
writer.close_file()

writer.close()
return OK

```

## 构造函数

### ZipPacker

```csharp
public ZipPacker()
```

## 属性

### CompressionLevel

```csharp
public int CompressionLevel { get; set; }
```

The compression level used when `StartFile()` is called. Use [`ZipPacker.CompressionLevelEnum`](ZipPacker.md) as a reference.

## 方法

### AddDirectory

```csharp
public Error AddDirectory(string path, FileAccess.UnixPermissionFlags permissions = FileAccess.UnixPermissionFlags., ulong modifiedTime = 0)
```

Adds directory to the archive. If `modifiedTime` is set to `0`, current system time is used.

**Note:** Directories are automatically created when `StartFile()` is called, use this function before adding files to create directories with custom permissions and modification time.

### Close

```csharp
public Error Close()
```

Closes the underlying resources used by this instance.

### CloseFile

```csharp
public Error CloseFile()
```

Stops writing to a file within the archive.

It will fail if there is no open file.

### GetCompressionLevel

```csharp
public int GetCompressionLevel()
```

### Open

```csharp
public Error Open(string path, ZipPacker.ZipAppend append = ZipPacker.ZipAppend.Create)
```

Opens a zip file for writing at the given path using the specified write mode.

This must be called before everything else.

### SetCompressionLevel

```csharp
public void SetCompressionLevel(int compressionLevel)
```

### StartFile

```csharp
public Error StartFile(string path)
```

Starts writing to a file within the archive. Only one file can be written at the same time. If `modifiedTime` is set to `0`, current system time is used.

Must be called after `Open()`.

### StartFile

```csharp
public Error StartFile(string path, FileAccess.UnixPermissionFlags permissions = FileAccess.UnixPermissionFlags., ulong modifiedTime = 0)
```

Starts writing to a file within the archive. Only one file can be written at the same time. If `modifiedTime` is set to `0`, current system time is used.

Must be called after `Open()`.

### WriteFile

```csharp
public Error WriteFile(ReadOnlySpan<byte> data)
```

Write the given `data` to the file.

Needs to be called after `StartFile()`.

### WriteFile

```csharp
public Error WriteFile(byte[] data)
```

Write the given `data` to the file.

Needs to be called after `StartFile()`.

## CompressionLevelEnum

命名空间 `Godot`；enum

### 枚举值

- `Default = -1`：Start a file with the default Deflate compression level (`6`).
- `None = 0`：Start a file with no compression.
- `Fast = 1`：Start a file with the fastest Deflate compression level (`1`).
- `Best = 9`：Start a file with the best Deflate compression level (`9`).

## ZipAppend

命名空间 `Godot`；enum

### 枚举值

- `Create = 0`：Create a new zip archive at the given path.
- `Createafter = 1`：Append a new zip archive to the end of the already existing file at the given path.
- `Addinzip = 2`：Add new files to the existing zip archive at the given path.
