<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PckPacker

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The `PckPacker` is used to create packages that can be loaded into a running project using [`ProjectSettings.LoadResourcePack`](ProjectSettings.md).

```csharp
var packer = new PckPacker();
packer.PckStart("test.pck");
packer.AddFile("res://text.txt", "text.txt");
packer.Flush();

```

The above `PckPacker` creates package `test.pck`, then adds a file named `text.txt` at the root of the package.

**Note:** PCK is Godot's own pack file format. To create ZIP archives that can be read by any program, use [`ZipPacker`](ZipPacker.md) instead.

## 构造函数

### PckPacker

```csharp
public PckPacker()
```

## 方法

### AddFile

```csharp
public Error AddFile(string targetPath, string sourcePath, bool encrypt = false)
```

Adds the `sourcePath` file to the current PCK package at the `targetPath` internal path. The `res://` prefix for `targetPath` is optional and stripped internally. File content is immediately written to the PCK.

### AddFileFromBuffer

```csharp
public Error AddFileFromBuffer(string targetPath, ReadOnlySpan<byte> data, bool encrypt)
```

Adds the `data` to the current PCK package at the `targetPath` internal path. The `res://` prefix for `targetPath` is optional and stripped internally. File content is immediately written to the PCK.

### AddFileFromBuffer

```csharp
public Error AddFileFromBuffer(string targetPath, byte[] data, bool encrypt = false)
```

Adds the `data` to the current PCK package at the `targetPath` internal path. The `res://` prefix for `targetPath` is optional and stripped internally. File content is immediately written to the PCK.

### AddFileRemoval

```csharp
public Error AddFileRemoval(string targetPath)
```

Registers a file removal of the `targetPath` internal path to the PCK. This is mainly used for patches. If the file at this path has been loaded from a previous PCK, it will be removed. The `res://` prefix for `targetPath` is optional and stripped internally.

### Flush

```csharp
public Error Flush(bool verbose = false)
```

Writes the file directory and closes the PCK. If `verbose` is `true`, a list of files added will be printed to the console for easier debugging.

**Note:** `PckPacker` will automatically flush when it's freed, which happens when it goes out of scope or when it gets assigned with `null`. In C# the reference must be disposed after use, either with the `using` statement or by calling the `Dispose` method directly.

### PckStart

```csharp
public Error PckStart(string pckPath, int alignment = 32, string key = "0000000000000000000000000000000000000000000000000000000000000000", bool encryptDirectory = false)
```

Creates a new PCK file at the file path `pckPath`. The `.pck` file extension isn't added automatically, so it should be part of `pckPath` (even though it's not required).
