<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FileAccess

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class can be used to permanently store data in the user device's file system and to read from it. This is useful for storing game save data or player configuration files.

**Example:** How to write and read from a file. The file named `"save_game.dat"` will be stored in the user data folder, as specified in the [Data paths]($DOCS_URL/tutorials/io/data_paths.html) documentation:

```csharp
public void SaveToFile(string content)
{
using var file = FileAccess.Open("user://save_game.dat", FileAccess.ModeFlags.Write);
file.StoreString(content);
}

public string LoadFromFile()
{
using var file = FileAccess.Open("user://save_game.dat", FileAccess.ModeFlags.Read);
string content = file.GetAsText();
return content;
}

```

A `FileAccess` instance has its own file cursor, which is the position in bytes in the file where the next read/write operation will occur. Functions such as `Get8()`, `Get16()`, `Store8()`, and `Store16()` will move the file cursor forward by the number of bytes read/written. The file cursor can be moved to a specific position using `Seek()` or `SeekEnd()`, and its position can be retrieved using `GetPosition()`.

A `FileAccess` instance will close its file when the instance is freed. Since it inherits [`RefCounted`](RefCounted.md), this happens automatically when it is no longer in use. `Close()` can be called to close it earlier. In C#, the reference must be disposed manually, which can be done with the `using` statement or by calling the `Dispose` method directly.

**Note:** To access project resources once exported, it is recommended to use [`ResourceLoader`](ResourceLoader.md) instead of `FileAccess`, as some files are converted to engine-specific formats and their original source files might not be present in the exported PCK package. If using `FileAccess`, make sure the file is included in the export by changing its import mode to **Keep File (exported as is)** in the Import dock, or, for files where this option is not available, change the non-resource export filter in the Export dialog to include the file's extension (e.g. `*.txt`).

**Note:** Files are automatically closed only if the process exits "normally" (such as by clicking the window manager's close button or pressing Alt + F4). If you stop the project execution by pressing F8 while the project is running, the file won't be closed as the game process will be killed. You can work around this by calling `Flush()` at regular intervals.

## 属性

### BigEndian

```csharp
public bool BigEndian { get; set; }
```

引擎名：`big_endian`

If `true`, the file is read with big-endian [endianness](https://en.wikipedia.org/wiki/Endianness). If `false`, the file is read with little-endian endianness. If in doubt, leave this to `false` as most files are written with little-endian endianness.

**Note:** This is always reset to system endianness, which is little-endian on all supported platforms, whenever you open the file. Therefore, you must set `BigEndian` *after* opening the file, not before.

## 方法

### Close

```csharp
public void Close()
```

引擎名：`close`

Closes the currently opened file and prevents subsequent read/write operations. Use `Flush()` to persist the data to disk without closing the file.

**Note:** `FileAccess` will automatically close when it's freed, which happens when it goes out of scope or when it gets assigned with `null`. In C# the reference must be disposed after we are done using it, this can be done with the `using` statement or calling the `Dispose` method directly.

### CreateTemp

```csharp
public static FileAccess CreateTemp(FileAccess.ModeFlags modeFlags, string prefix = "", string extension = "", bool keep = false)
```

引擎名：`create_temp`

Creates a temporary file. This file will be freed when the returned `FileAccess` is freed.

If `prefix` is not empty, it will be prefixed to the file name, separated by a `-`.

If `extension` is not empty, it will be appended to the temporary file name.

If `keep` is `true`, the file is not deleted when the returned `FileAccess` is freed.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

### CreateTemp

```csharp
public static FileAccess CreateTemp(int modeFlags, string prefix, string extension, bool keep)
```

引擎名：`create_temp`

Creates a temporary file. This file will be freed when the returned `FileAccess` is freed.

If `prefix` is not empty, it will be prefixed to the file name, separated by a `-`.

If `extension` is not empty, it will be appended to the temporary file name.

If `keep` is `true`, the file is not deleted when the returned `FileAccess` is freed.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

### EofReached

```csharp
public bool EofReached()
```

引擎名：`eof_reached`

Returns `true` if the file cursor has already read past the end of the file.

**Note:** `eof_reached() == false` cannot be used to check whether there is more data available. To loop while there is more data available, use:

```csharp
while (file.GetPosition() < file.GetLength())
{
// Read data
}

```

### FileExists

```csharp
public static bool FileExists(string path)
```

引擎名：`file_exists`

Returns `true` if the file exists in the given path.

**Note:** Many resources types are imported (e.g. textures or sound files), and their source asset will not be included in the exported game, as only the imported version is used. See [`ResourceLoader.Exists`](ResourceLoader.md) for an alternative approach that takes resource remapping into account.

For a non-static, relative equivalent, use [`DirAccess.FileExists`](DirAccess.md).

### Flush

```csharp
public void Flush()
```

引擎名：`flush`

Writes the file's buffer to disk. Flushing is automatically performed when the file is closed. This means you don't need to call `Flush()` manually before closing a file. Still, calling `Flush()` can be used to ensure the data is safe even if the project crashes instead of being closed gracefully.

**Note:** Only call `Flush()` when you actually need it. Otherwise, it will decrease performance due to constant disk writes.

### Get16

```csharp
public ushort Get16()
```

Returns the next 16 bits from the file as an integer. This advances the file cursor by 2 bytes. See `Store16()` for details on what values can be stored and retrieved this way.

### Get32

```csharp
public uint Get32()
```

Returns the next 32 bits from the file as an integer. This advances the file cursor by 4 bytes. See `Store32()` for details on what values can be stored and retrieved this way.

### Get64

```csharp
public ulong Get64()
```

Returns the next 64 bits from the file as an integer. This advances the file cursor by 8 bytes. See `Store64()` for details on what values can be stored and retrieved this way.

### Get8

```csharp
public byte Get8()
```

Returns the next 8 bits from the file as an integer. This advances the file cursor by 1 byte. See `Store8()` for details on what values can be stored and retrieved this way.

### GetAccessTime

```csharp
public static ulong GetAccessTime(string file)
```

引擎名：`get_access_time`

Returns the last time the `file` was accessed in Unix timestamp format, or `0` on error. This Unix timestamp can be converted to another format using the [`Time`](Time.md) singleton.

### GetAsText

```csharp
public string GetAsText()
```

引擎名：`get_as_text`

Returns the whole file as a `System.String`. Text is interpreted as being UTF-8 encoded. This ignores the file cursor and does not affect it.

### GetAsText

```csharp
public string GetAsText(bool skipCr)
```

引擎名：`get_as_text`

Returns the whole file as a `System.String`. Text is interpreted as being UTF-8 encoded. This ignores the file cursor and does not affect it.

### GetBuffer

```csharp
public byte[] GetBuffer(long length)
```

引擎名：`get_buffer`

Returns next `length` bytes of the file as a `System.Byte`[]. This advances the file cursor by `length` bytes.

### GetCsvLine

```csharp
public string[] GetCsvLine(string delim = ",")
```

引擎名：`get_csv_line`

Returns the next value of the file in CSV (Comma-Separated Values) format. You can pass a different delimiter `delim` to use other than the default `","` (comma). This delimiter must be one-character long, and cannot be a double quotation mark.

Text is interpreted as being UTF-8 encoded. Text values must be enclosed in double quotes if they include the delimiter character. Double quotes within a text value can be escaped by doubling their occurrence. This advances the file cursor to after the newline character at the end of the line.

For example, the following CSV lines are valid and will be properly parsed as two strings each:

```csharp
Alice,"Hello, Bob!"
Bob,Alice! What a surprise!
Alice,"I thought you'd reply with ""Hello, world""."

```

Note how the second line can omit the enclosing quotes as it does not include the delimiter. However it *could* very well use quotes, it was only written without for demonstration purposes. The third line must use `""` for each quotation mark that needs to be interpreted as such instead of the end of a text value.

### GetDouble

```csharp
public double GetDouble()
```

引擎名：`get_double`

Returns the next 64 bits from the file as a floating-point number. This advances the file cursor by 8 bytes.

### GetError

```csharp
public Error GetError()
```

引擎名：`get_error`

Returns the last error that happened when trying to perform operations. Compare with the `ERR_FILE_*` constants from [`Error`](Error.md).

### GetExtendedAttribute

```csharp
public static byte[] GetExtendedAttribute(string file, string attributeName)
```

引擎名：`get_extended_attribute`

Reads the file extended attribute with name `attributeName` as a byte array.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems.

**Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included.

**Note:** On Windows, alternate data streams are used to store extended attributes.

### GetExtendedAttributeString

```csharp
public static string GetExtendedAttributeString(string file, string attributeName)
```

引擎名：`get_extended_attribute_string`

Reads the file extended attribute with name `attributeName` as a UTF-8 encoded string.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems.

**Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included.

**Note:** On Windows, alternate data streams are used to store extended attributes.

### GetExtendedAttributesList

```csharp
public static string[] GetExtendedAttributesList(string file)
```

引擎名：`get_extended_attributes_list`

Returns a list of file extended attributes.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems.

**Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included.

**Note:** On Windows, alternate data streams are used to store extended attributes.

### GetFileAsBytes

```csharp
public static byte[] GetFileAsBytes(string path)
```

引擎名：`get_file_as_bytes`

Returns the whole `path` file contents as a `System.Byte`[] without any decoding.

Returns an empty `System.Byte`[] if an error occurred while opening the file. You can use `GetOpenError()` to check the error that occurred.

### GetFileAsString

```csharp
public static string GetFileAsString(string path)
```

引擎名：`get_file_as_string`

Returns the whole `path` file contents as a `System.String`. Text is interpreted as being UTF-8 encoded.

Returns an empty `System.String` if an error occurred while opening the file. You can use `GetOpenError()` to check the error that occurred.

### GetFloat

```csharp
public float GetFloat()
```

引擎名：`get_float`

Returns the next 32 bits from the file as a floating-point number. This advances the file cursor by 4 bytes.

### GetHalf

```csharp
public float GetHalf()
```

引擎名：`get_half`

Returns the next 16 bits from the file as a half-precision floating-point number. This advances the file cursor by 2 bytes.

### GetHiddenAttribute

```csharp
public static bool GetHiddenAttribute(string file)
```

引擎名：`get_hidden_attribute`

Returns `true` if the **hidden** attribute is set on the file at the given path.

**Note:** This method is implemented on iOS, BSD, macOS, and Windows.

### GetLength

```csharp
public ulong GetLength()
```

引擎名：`get_length`

Returns the size of the file in bytes. For a pipe, returns the number of bytes available for reading from the pipe.

### GetLine

```csharp
public string GetLine()
```

引擎名：`get_line`

Returns the next line of the file as a `System.String`. The returned string doesn't include newline (`\n`) or carriage return (`\r`) characters, but does include any other leading or trailing whitespace. This advances the file cursor to after the newline character at the end of the line.

Text is interpreted as being UTF-8 encoded.

### GetMd5

```csharp
public static string GetMd5(string path)
```

引擎名：`get_md5`

Returns an MD5 String representing the file at the given path or an empty `System.String` on failure.

### GetModifiedTime

```csharp
public static ulong GetModifiedTime(string file)
```

引擎名：`get_modified_time`

Returns the last time the `file` was modified in Unix timestamp format, or `0` on error. This Unix timestamp can be converted to another format using the [`Time`](Time.md) singleton.

### GetOpenError

```csharp
public static Error GetOpenError()
```

引擎名：`get_open_error`

Returns the result of the last `Open()` call in the current thread.

### GetPascalString

```csharp
public string GetPascalString()
```

引擎名：`get_pascal_string`

Returns a `System.String` saved in Pascal format from the file, meaning that the length of the string is explicitly stored at the start. See `StorePascalString()`. This may include newline characters. The file cursor is advanced after the bytes read.

Text is interpreted as being UTF-8 encoded.

### GetPath

```csharp
public string GetPath()
```

引擎名：`get_path`

Returns the path as a `System.String` for the current open file.

### GetPathAbsolute

```csharp
public string GetPathAbsolute()
```

引擎名：`get_path_absolute`

Returns the absolute path as a `System.String` for the current open file.

### GetPosition

```csharp
public ulong GetPosition()
```

引擎名：`get_position`

Returns the file cursor's position in bytes from the beginning of the file. This is the file reading/writing cursor set by `Seek()` or `SeekEnd()` and advanced by read/write operations.

### GetReadOnlyAttribute

```csharp
public static bool GetReadOnlyAttribute(string file)
```

引擎名：`get_read_only_attribute`

Returns `true` if the **read only** attribute is set on the file at the given path.

**Note:** This method is implemented on iOS, BSD, macOS, and Windows.

### GetReal

```csharp
public float GetReal()
```

引擎名：`get_real`

Returns the next bits from the file as a floating-point number. This advances the file cursor by either 4 or 8 bytes, depending on the precision used by the Godot build that saved the file.

If the file was saved by a Godot build compiled with the `precision=single` option (the default), the number of read bits for that file is 32. Otherwise, if compiled with the `precision=double` option, the number of read bits is 64.

### GetSha256

```csharp
public static string GetSha256(string path)
```

引擎名：`get_sha256`

Returns an SHA-256 `System.String` representing the file at the given path or an empty `System.String` on failure.

### GetSize

```csharp
public static long GetSize(string file)
```

引擎名：`get_size`

Returns the size of the file at the given path, in bytes, or `-1` on error.

### GetUnixPermissions

```csharp
public static FileAccess.UnixPermissionFlags GetUnixPermissions(string file)
```

引擎名：`get_unix_permissions`

Returns the UNIX permissions of the file at the given path.

**Note:** This method is implemented on iOS, Linux/BSD, and macOS.

### GetVar

```csharp
public Variant GetVar(bool allowObjects = false)
```

引擎名：`get_var`

Returns the next [`Variant`](Variant.md) value from the file. If `allowObjects` is `true`, decoding objects is allowed. This advances the file cursor by the number of bytes read.

Internally, this uses the same decoding mechanism as the `GD.BytesToVar` method, as described in the [Binary serialization API]($DOCS_URL/tutorials/io/binary_serialization_api.html) documentation.

**Warning:** Deserialized objects can contain code which gets executed. Do not use this option if the serialized object comes from untrusted sources to avoid potential security threats such as remote code execution.

### IsBigEndian

```csharp
public bool IsBigEndian()
```

引擎名：`is_big_endian`

### IsOpen

```csharp
public bool IsOpen()
```

引擎名：`is_open`

Returns `true` if the file is currently opened.

### Open

```csharp
public static FileAccess Open(string path, FileAccess.ModeFlags flags)
```

引擎名：`open`

Creates a new `FileAccess` object and opens the file for writing or reading, depending on the flags.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

### OpenCompressed

```csharp
public static FileAccess OpenCompressed(string path, FileAccess.ModeFlags modeFlags, FileAccess.CompressionMode compressionMode = FileAccess.CompressionMode.Fastlz)
```

引擎名：`open_compressed`

Creates a new `FileAccess` object and opens a compressed file for reading or writing.

**Note:** `OpenCompressed()` can only read files that were saved by Godot, not third-party compression formats. See [GitHub issue #28999](https://github.com/godotengine/godot/issues/28999) for a workaround.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

### OpenEncrypted

```csharp
public static FileAccess OpenEncrypted(string path, FileAccess.ModeFlags modeFlags, ReadOnlySpan<byte> key)
```

引擎名：`open_encrypted`

Creates a new `FileAccess` object and opens an encrypted file in write or read mode. You need to pass a binary key to encrypt/decrypt it.

**Note:** The provided key must be 32 bytes long.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

### OpenEncrypted

```csharp
public static FileAccess OpenEncrypted(string path, FileAccess.ModeFlags modeFlags, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iV)
```

引擎名：`open_encrypted`

Creates a new `FileAccess` object and opens an encrypted file in write or read mode. You need to pass a binary key to encrypt/decrypt it.

**Note:** The provided key must be 32 bytes long.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

### OpenEncrypted

```csharp
public static FileAccess OpenEncrypted(string path, FileAccess.ModeFlags modeFlags, byte[] key)
```

引擎名：`open_encrypted`

Creates a new `FileAccess` object and opens an encrypted file in write or read mode. You need to pass a binary key to encrypt/decrypt it.

**Note:** The provided key must be 32 bytes long.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

### OpenEncrypted

```csharp
public static FileAccess OpenEncrypted(string path, FileAccess.ModeFlags modeFlags, byte[] key, byte[] iV = null)
```

引擎名：`open_encrypted`

Creates a new `FileAccess` object and opens an encrypted file in write or read mode. You need to pass a binary key to encrypt/decrypt it.

**Note:** The provided key must be 32 bytes long.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

参数：
- `iV`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### OpenEncryptedWithPass

```csharp
public static FileAccess OpenEncryptedWithPass(string path, FileAccess.ModeFlags modeFlags, string pass)
```

引擎名：`open_encrypted_with_pass`

Creates a new `FileAccess` object and opens an encrypted file in write or read mode. You need to pass a password to encrypt/decrypt it.

Returns `null` if opening the file failed. You can use `GetOpenError()` to check the error that occurred.

### RemoveExtendedAttribute

```csharp
public static Error RemoveExtendedAttribute(string file, string attributeName)
```

引擎名：`remove_extended_attribute`

Removes file extended attribute with name `attributeName`.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems.

**Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included.

**Note:** On Windows, alternate data streams are used to store extended attributes.

### Resize

```csharp
public Error Resize(long length)
```

引擎名：`resize`

Resizes the file to a specified length. The file must be open in a mode that permits writing. If the file is extended, NUL characters are appended. If the file is truncated, all data from the end file to the original length of the file is lost.

### Seek

```csharp
public void Seek(ulong position)
```

引擎名：`seek`

Sets the file cursor to the specified position in bytes, from the beginning of the file. This changes the value returned by `GetPosition()`.

### SeekEnd

```csharp
public void SeekEnd(long position = 0)
```

引擎名：`seek_end`

Sets the file cursor to the specified position in bytes, from the end of the file. This changes the value returned by `GetPosition()`.

**Note:** This is an offset, so you should use negative numbers otherwise the file cursor will move past the end of the file.

### SetBigEndian

```csharp
public void SetBigEndian(bool bigEndian)
```

引擎名：`set_big_endian`

### SetExtendedAttribute

```csharp
public static Error SetExtendedAttribute(string file, string attributeName, ReadOnlySpan<byte> data)
```

引擎名：`set_extended_attribute`

Writes file extended attribute with name `attributeName` as a byte array.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems.

**Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included.

**Note:** On Windows, alternate data streams are used to store extended attributes.

### SetExtendedAttribute

```csharp
public static Error SetExtendedAttribute(string file, string attributeName, byte[] data)
```

引擎名：`set_extended_attribute`

Writes file extended attribute with name `attributeName` as a byte array.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems.

**Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included.

**Note:** On Windows, alternate data streams are used to store extended attributes.

### SetExtendedAttributeString

```csharp
public static Error SetExtendedAttributeString(string file, string attributeName, string data)
```

引擎名：`set_extended_attribute_string`

Writes file extended attribute with name `attributeName` as a UTF-8 encoded string.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Extended attributes support depends on the file system. Attributes will be lost when the file is moved between incompatible file systems.

**Note:** On Linux, only "user" namespace attributes are accessible, namespace prefix should not be included.

**Note:** On Windows, alternate data streams are used to store extended attributes.

### SetHiddenAttribute

```csharp
public static Error SetHiddenAttribute(string file, bool hidden)
```

引擎名：`set_hidden_attribute`

Sets file **hidden** attribute.

**Note:** This method is implemented on iOS, BSD, macOS, and Windows.

### SetReadOnlyAttribute

```csharp
public static Error SetReadOnlyAttribute(string file, bool ro)
```

引擎名：`set_read_only_attribute`

Sets file **read only** attribute.

**Note:** This method is implemented on iOS, BSD, macOS, and Windows.

### SetUnixPermissions

```csharp
public static Error SetUnixPermissions(string file, FileAccess.UnixPermissionFlags permissions)
```

引擎名：`set_unix_permissions`

Sets file UNIX permissions.

**Note:** This method is implemented on iOS, Linux/BSD, and macOS.

### Store16

```csharp
public bool Store16(ushort value)
```

Stores an integer as 16 bits in the file. This advances the file cursor by 2 bytes. Returns `true` if the operation is successful.

**Note:** The `value` should lie in the interval `[0, 2^16 - 1]`. Any other value will overflow and wrap around.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

To store a signed integer, use `Store64()` or store a signed integer from the interval `[-2^15, 2^15 - 1]` (i.e. keeping one bit for the signedness) and compute its sign manually when reading. For example:

```csharp
public override void _Ready()
{
using var f = FileAccess.Open("user://file.dat", FileAccess.ModeFlags.WriteRead);
f.Store16(unchecked((ushort)-42)); // This wraps around and stores 65494 (2^16 - 42).
f.Store16(121); // In bounds, will store 121.
f.Seek(0); // Go back to start to read the stored value.
ushort read1 = f.Get16(); // 65494
ushort read2 = f.Get16(); // 121
short converted1 = (short)read1; // -42
short converted2 = (short)read2; // 121
}

```

### Store32

```csharp
public bool Store32(uint value)
```

Stores an integer as 32 bits in the file. This advances the file cursor by 4 bytes. Returns `true` if the operation is successful.

**Note:** The `value` should lie in the interval `[0, 2^32 - 1]`. Any other value will overflow and wrap around.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

To store a signed integer, use `Store64()`, or convert it manually (see `Store16()` for an example).

### Store64

```csharp
public bool Store64(ulong value)
```

Stores an integer as 64 bits in the file. This advances the file cursor by 8 bytes. Returns `true` if the operation is successful.

**Note:** The `value` must lie in the interval `[-2^63, 2^63 - 1]` (i.e. be a valid `System.Int32` value).

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### Store8

```csharp
public bool Store8(byte value)
```

Stores an integer as 8 bits in the file. This advances the file cursor by 1 byte. Returns `true` if the operation is successful.

**Note:** The `value` should lie in the interval `[0, 255]`. Any other value will overflow and wrap around.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

To store a signed integer, use `Store64()`, or convert it manually (see `Store16()` for an example).

### StoreBuffer

```csharp
public bool StoreBuffer(ReadOnlySpan<byte> buffer)
```

引擎名：`store_buffer`

Stores the given array of bytes in the file. This advances the file cursor by the number of bytes written. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreBuffer

```csharp
public bool StoreBuffer(byte[] buffer)
```

引擎名：`store_buffer`

Stores the given array of bytes in the file. This advances the file cursor by the number of bytes written. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreCsvLine

```csharp
public bool StoreCsvLine(ReadOnlySpan<string> values, string delim)
```

引擎名：`store_csv_line`

Stores the given `System.String`[] in the file as a line formatted in the CSV (Comma-Separated Values) format. You can pass a different delimiter `delim` to use other than the default `","` (comma). This delimiter must be one-character long.

Text will be encoded as UTF-8. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreCsvLine

```csharp
public bool StoreCsvLine(string[] values, string delim = ",")
```

引擎名：`store_csv_line`

Stores the given `System.String`[] in the file as a line formatted in the CSV (Comma-Separated Values) format. You can pass a different delimiter `delim` to use other than the default `","` (comma). This delimiter must be one-character long.

Text will be encoded as UTF-8. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreDouble

```csharp
public bool StoreDouble(double value)
```

引擎名：`store_double`

Stores a floating-point number as 64 bits in the file. This advances the file cursor by 8 bytes. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreFloat

```csharp
public bool StoreFloat(float value)
```

引擎名：`store_float`

Stores a floating-point number as 32 bits in the file. This advances the file cursor by 4 bytes. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreHalf

```csharp
public bool StoreHalf(float value)
```

引擎名：`store_half`

Stores a half-precision floating-point number as 16 bits in the file. This advances the file cursor by 2 bytes. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreLine

```csharp
public bool StoreLine(string line)
```

引擎名：`store_line`

Stores `line` in the file followed by a newline character (`\n`), encoding the text as UTF-8. This advances the file cursor by the length of the line, after the newline character. The amount of bytes written depends on the UTF-8 encoded bytes, which may be different from `String.length` which counts the number of UTF-32 codepoints. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StorePascalString

```csharp
public bool StorePascalString(string string)
```

引擎名：`store_pascal_string`

Stores the given `System.String` as a line in the file in Pascal format (i.e. also store the length of the string). Text will be encoded as UTF-8. This advances the file cursor by the number of bytes written depending on the UTF-8 encoded bytes, which may be different from `String.length` which counts the number of UTF-32 codepoints. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreReal

```csharp
public bool StoreReal(float value)
```

引擎名：`store_real`

Stores a floating-point number in the file. This advances the file cursor by either 4 or 8 bytes, depending on the precision used by the current Godot build.

If using a Godot build compiled with the `precision=single` option (the default), this method will save a 32-bit float. Otherwise, if compiled with the `precision=double` option, this will save a 64-bit float. Returns `true` if the operation is successful.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreString

```csharp
public bool StoreString(string string)
```

引擎名：`store_string`

Stores `string` in the file without a newline character (`\n`), encoding the text as UTF-8. This advances the file cursor by the length of the string in UTF-8 encoded bytes, which may be different from `String.length` which counts the number of UTF-32 codepoints. Returns `true` if the operation is successful.

**Note:** This method is intended to be used to write text files. The string is stored as a UTF-8 encoded buffer without string length or terminating zero, which means that it can't be loaded back easily. If you want to store a retrievable string in a binary file, consider using `StorePascalString()` instead. For retrieving strings from a text file, you can use `get_buffer(length).get_string_from_utf8()` (if you know the length) or `GetAsText()`.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

### StoreVar

```csharp
public bool StoreVar(Variant value, bool fullObjects = false)
```

引擎名：`store_var`

Stores any Variant value in the file. If `fullObjects` is `true`, encoding objects is allowed (and can potentially include code). This advances the file cursor by the number of bytes written. Returns `true` if the operation is successful.

Internally, this uses the same encoding mechanism as the `GD.VarToBytes` method, as described in the [Binary serialization API]($DOCS_URL/tutorials/io/binary_serialization_api.html) documentation.

**Note:** Not all properties are included. Only properties that are configured with the [`PropertyUsageFlags.Storage`](PropertyUsageFlags.md) flag set will be serialized. You can add a new usage flag to a property by overriding the [`GodotObject.GetPropertyList`](GodotObject.md) method in your class. You can also check how property usage is configured by calling [`GodotObject.GetPropertyList`](GodotObject.md). See [`PropertyUsageFlags`](PropertyUsageFlags.md) for the possible usage flags.

**Note:** If an error occurs, the resulting value of the file position indicator is indeterminate.

## CompressionMode

命名空间 `Godot`；enum

### 枚举值

- `Fastlz = 0`：Uses the [FastLZ](https://fastlz.org/) compression method.
- `Deflate = 1`：Uses the [DEFLATE](https://en.wikipedia.org/wiki/DEFLATE) compression method.
- `Zstd = 2`：Uses the [Zstandard](https://facebook.github.io/zstd/) compression method.
- `GZip = 3`：Uses the [gzip](https://www.gzip.org/) compression method.
- `Brotli = 4`：Uses the [brotli](https://github.com/google/brotli) compression method (only decompression is supported).

## ModeFlags

命名空间 `Godot`；enum

### 枚举值

- `Read = 1`：Opens the file for read operations.
- `Write = 2`：Opens the file for write operations.
- `ReadWrite = 3`：Opens the file for read and write operations.
- `WriteRead = 7`：Opens the file for read and write operations.

## UnixPermissionFlags

命名空间 `Godot`；enum

### 枚举值

- `ReadOwner = 256`：Read for owner bit.
- `WriteOwner = 128`：Write for owner bit.
- `ExecuteOwner = 64`：Execute for owner bit.
- `ReadGroup = 32`：Read for group bit.
- `WriteGroup = 16`：Write for group bit.
- `ExecuteGroup = 8`：Execute for group bit.
- `ReadOther = 4`：Read for other bit.
- `WriteOther = 2`：Write for other bit.
- `ExecuteOther = 1`：Execute for other bit.
- `SetUserId = 2048`：Set user id on execution bit.
- `SetGroupId = 1024`：Set group id on execution bit.
- `RestrictedDelete = 512`：Restricted deletion (sticky) bit.
