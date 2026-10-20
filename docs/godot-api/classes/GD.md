<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GD

命名空间 `Godot`；static class

Godot's global functions.

## 方法

### BytesToVar

```csharp
public static Variant BytesToVar(Span<byte> bytes)
```

Decodes a byte array back to a [`Variant`](Variant.md) value, without decoding objects.
Note: If you need object deserialization, see `BytesToVarWithObjects()`.

参数：
- `bytes`：Byte array that will be decoded to a [`Variant`](Variant.md).

返回：The decoded [`Variant`](Variant.md).

### BytesToVarWithObjects

```csharp
public static Variant BytesToVarWithObjects(Span<byte> bytes)
```

Decodes a byte array back to a [`Variant`](Variant.md) value. Decoding objects is allowed.
Warning: Deserialized object can contain code which gets executed. Do not use this
option if the serialized object comes from untrusted sources to avoid potential security
threats (remote code execution).

参数：
- `bytes`：Byte array that will be decoded to a [`Variant`](Variant.md).

返回：The decoded [`Variant`](Variant.md).

### Compress

```csharp
public static byte[] Compress(byte[] instance, FileAccess.CompressionMode compressionMode = FileAccess.CompressionMode.Fastlz)
```

Returns a new byte array with the data compressed.

参数：
- `instance`：The byte array to compress.
- `compressionMode`：The compression mode, one of [`FileAccess.CompressionMode`](FileAccess.md)

返回：The compressed byte array.

### Convert

```csharp
public static Variant Convert(Variant what, Variant.Type type)
```

Converts `what` to `type` in the best way possible.
The `type` parameter uses the [`Variant.Type`](Variant.md) values.

返回：The `Variant` converted to the given `type`.

### Decompress

```csharp
public static byte[] Decompress(byte[] instance, long bufferSize, FileAccess.CompressionMode compressionMode = FileAccess.CompressionMode.Fastlz)
```

Returns a new byte array with the data decompressed.

Note: Decompression is not guaranteed to work with data not compressed by Godot, for example if data compressed with the deflate compression mode lacks a checksum or header.

参数：
- `instance`：The byte array to decompress.
- `bufferSize`：The size of the uncompressed data.
- `compressionMode`：The compression mode, one of [`FileAccess.CompressionMode`](FileAccess.md)

返回：The decompressed byte array.

### DecompressDynamic

```csharp
public static byte[] DecompressDynamic(byte[] instance, long maxOutputSize, FileAccess.CompressionMode compressionMode = FileAccess.CompressionMode.Fastlz)
```

Returns a new byte array with the data decompressed. **This method only accepts brotli, gzip, and deflate compression modes**.

This method is potentially slower than `Decompress()`, as it may have to re-allocate its output buffer multiple times while decompressing, whereas `Decompress()` knows it's output buffer size from the beginning.

GZIP has a maximal compression ratio of 1032:1, meaning it's very possible for a small compressed payload to decompress to a potentially very large output. To guard against this, you may provide a maximum size this function is allowed to allocate in bytes via [param max_output_size]. Passing -1 will allow for unbounded output. If any positive value is passed, and the decompression exceeds that amount in bytes, then an error will be returned.

Note: Decompression is not guaranteed to work with data not compressed by Godot, for example if data compressed with the deflate compression mode lacks a checksum or header.

参数：
- `instance`：The byte array to decompress.
- `maxOutputSize`：The maximum size this function is allowed to allocate in bytes.
- `compressionMode`：The compression mode, one of [`FileAccess.CompressionMode`](FileAccess.md)

返回：The decompressed byte array.

### Hash

```csharp
public static int Hash(Variant var)
```

Returns the integer hash of the passed `var`.

参数：
- `var`：Variable that will be hashed.

返回：Hash of the variable passed.

### IsEmpty

```csharp
public static bool IsEmpty(Color[] instance)
```

Returns true if this Color array is empty or doesn't exist.

参数：
- `instance`：The Color array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(Vector2I[] instance)
```

Returns true if this Vector2I array is empty or doesn't exist.

参数：
- `instance`：The Vector2I array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(Vector2[] instance)
```

Returns true if this Vector2 array is empty or doesn't exist.

参数：
- `instance`：The Vector2 array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(Vector3I[] instance)
```

Returns true if this Vector3I array is empty or doesn't exist.

参数：
- `instance`：The Vector3I array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(Vector3[] instance)
```

Returns true if this Vector3 array is empty or doesn't exist.

参数：
- `instance`：The Vector3 array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(Vector4I[] instance)
```

Returns true if this Vector4I array is empty or doesn't exist.

参数：
- `instance`：The Vector4I array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(Vector4[] instance)
```

Returns true if this Vector4 array is empty or doesn't exist.

参数：
- `instance`：The Vector4 array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(byte[] instance)
```

Returns true if this byte array is empty or doesn't exist.

参数：
- `instance`：The byte array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(double[] instance)
```

Returns true if this double array is empty or doesn't exist.

参数：
- `instance`：The double array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(float[] instance)
```

Returns true if this float array is empty or doesn't exist.

参数：
- `instance`：The float array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(int[] instance)
```

Returns true if this int array is empty or doesn't exist.

参数：
- `instance`：The int array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(long[] instance)
```

Returns true if this long array is empty or doesn't exist.

参数：
- `instance`：The long array check.

返回：Whether or not the array is empty.

### IsEmpty

```csharp
public static bool IsEmpty(string[] instance)
```

Returns true if this string array is empty or doesn't exist.

参数：
- `instance`：The string array check.

返回：Whether or not the array is empty.

### Join

```csharp
public static string Join(Color[] instance, string delimiter = ", ")
```

Converts this Color array to a string delimited by the given string.

参数：
- `instance`：The Color array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(Vector2I[] instance, string delimiter = ", ")
```

Converts this Vector2I array to a string delimited by the given string.

参数：
- `instance`：The Vector2I array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(Vector2[] instance, string delimiter = ", ")
```

Converts this Vector2 array to a string delimited by the given string.

参数：
- `instance`：The Vector2 array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(Vector3I[] instance, string delimiter = ", ")
```

Converts this Vector3I array to a string delimited by the given string.

参数：
- `instance`：The Vector3I array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(Vector3[] instance, string delimiter = ", ")
```

Converts this Vector3 array to a string delimited by the given string.

参数：
- `instance`：The Vector3 array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(Vector4I[] instance, string delimiter = ", ")
```

Converts this Vector4I array to a string delimited by the given string.

参数：
- `instance`：The Vector4I array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(Vector4[] instance, string delimiter = ", ")
```

Converts this Vector4 array to a string delimited by the given string.

参数：
- `instance`：The Vector4 array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(byte[] instance, string delimiter = ", ")
```

Converts this byte array to a string delimited by the given string.

参数：
- `instance`：The byte array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(double[] instance, string delimiter = ", ")
```

Converts this double array to a string delimited by the given string.

参数：
- `instance`：The double array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(float[] instance, string delimiter = ", ")
```

Converts this float array to a string delimited by the given string.

参数：
- `instance`：The float array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(int[] instance, string delimiter = ", ")
```

Converts this int array to a string delimited by the given string.

参数：
- `instance`：The int array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(long[] instance, string delimiter = ", ")
```

Converts this long array to a string delimited by the given string.

参数：
- `instance`：The long array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Join

```csharp
public static string Join(string[] instance, string delimiter = ", ")
```

Converts this string array to a string delimited by the given string.

参数：
- `instance`：The string array to convert.
- `delimiter`：The delimiter to use between items.

返回：A single string with all items.

### Load

```csharp
public static Resource Load(string path)
```

Loads a resource from the filesystem located at `path`.
The resource is loaded on the method call (unless it's referenced already
elsewhere, e.g. in another script or in the scene), which might cause slight delay,
especially when loading scenes. To avoid unnecessary delays when loading something
multiple times, either store the resource in a variable.

Note: Resource paths can be obtained by right-clicking on a resource in the FileSystem
dock and choosing "Copy Path" or by dragging the file from the FileSystem dock into the script.

Important: The path must be absolute, a local path will just return `null`.
This method is a simplified version of [`ResourceLoader.Load`](ResourceLoader.md), which can be used
for more advanced scenarios.

参数：
- `path`：Path of the [`Resource`](Resource.md) to load.

返回：The loaded [`Resource`](Resource.md).

### Load

```csharp
public static T Load<T>(string path)
```

Loads a resource from the filesystem located at `path`.
The resource is loaded on the method call (unless it's referenced already
elsewhere, e.g. in another script or in the scene), which might cause slight delay,
especially when loading scenes. To avoid unnecessary delays when loading something
multiple times, either store the resource in a variable.

Note: Resource paths can be obtained by right-clicking on a resource in the FileSystem
dock and choosing "Copy Path" or by dragging the file from the FileSystem dock into the script.

Important: The path must be absolute, a local path will just return `null`.
This method is a simplified version of [`ResourceLoader.Load`](ResourceLoader.md), which can be used
for more advanced scenarios.

参数：
- `path`：Path of the [`Resource`](Resource.md) to load.

### Print

```csharp
public static void Print(params object[] what)
```

Converts one or more arguments of any type to string in the best way possible
and prints them to the console.

Note: Consider using `PushError()` and `PushWarning()`
to print error and warning messages instead of `Print()`.
This distinguishes them from print messages used for debugging purposes,
while also displaying a stack trace when an error or warning is printed.

参数：
- `what`：Arguments that will be printed.

### Print

```csharp
public static void Print(string what)
```

Prints a message to the console.

Note: Consider using `PushError()` and `PushWarning()`
to print error and warning messages instead of `Print()`.
This distinguishes them from print messages used for debugging purposes,
while also displaying a stack trace when an error or warning is printed.

参数：
- `what`：Message that will be printed.

### PrintErr

```csharp
public static void PrintErr(params object[] what)
```

Prints one or more arguments to strings in the best way possible to standard error line.

参数：
- `what`：Arguments that will be printed.

### PrintErr

```csharp
public static void PrintErr(string what)
```

Prints a message to standard error line.

参数：
- `what`：Message that will be printed.

### PrintRaw

```csharp
public static void PrintRaw(params object[] what)
```

Prints one or more arguments to strings in the best way possible to the OS terminal.
Unlike `Print()`, no newline is added at the end.

参数：
- `what`：Arguments that will be printed.

### PrintRaw

```csharp
public static void PrintRaw(string what)
```

Prints a message to the OS terminal.
Unlike `Print()`, no newline is added at the end.

参数：
- `what`：Message that will be printed.

### PrintRich

```csharp
public static void PrintRich(params object[] what)
```

Converts one or more arguments of any type to string in the best way possible
and prints them to the console.
The following BBCode tags are supported: b, i, u, s, indent, code, url, center,
right, color, bgcolor, fgcolor.
Color tags only support named colors such as `red`, not hexadecimal color codes.
Unsupported tags will be left as-is in standard output.
When printing to standard output, the supported subset of BBCode is converted to
ANSI escape codes for the terminal emulator to display. Displaying ANSI escape codes
is currently only supported on Linux and macOS. Support for ANSI escape codes may vary
across terminal emulators, especially for italic and strikethrough.

Note: Consider using `PushError()` and `PushWarning()`
to print error and warning messages instead of `Print()` or
`PrintRich()`.
This distinguishes them from print messages used for debugging purposes,
while also displaying a stack trace when an error or warning is printed.

参数：
- `what`：Arguments that will be printed.

### PrintRich

```csharp
public static void PrintRich(string what)
```

Prints a message to the console.
The following BBCode tags are supported: b, i, u, s, indent, code, url, center,
right, color, bgcolor, fgcolor.
Color tags only support named colors such as `red`, not hexadecimal color codes.
Unsupported tags will be left as-is in standard output.
When printing to standard output, the supported subset of BBCode is converted to
ANSI escape codes for the terminal emulator to display. Displaying ANSI escape codes
is currently only supported on Linux and macOS. Support for ANSI escape codes may vary
across terminal emulators, especially for italic and strikethrough.

Note: Consider using `PushError()` and `PushWarning()`
to print error and warning messages instead of `Print()` or
`PrintRich()`.
This distinguishes them from print messages used for debugging purposes,
while also displaying a stack trace when an error or warning is printed.

参数：
- `what`：Message that will be printed.

### PrintS

```csharp
public static void PrintS(params object[] what)
```

Prints one or more arguments to the console with a space between each argument.

参数：
- `what`：Arguments that will be printed.

### PrintT

```csharp
public static void PrintT(params object[] what)
```

Prints one or more arguments to the console with a tab between each argument.

参数：
- `what`：Arguments that will be printed.

### PushError

```csharp
public static void PushError(params object[] what)
```

Pushes an error message to Godot's built-in debugger and to the OS terminal.

Note: Errors printed this way will not pause project execution.

参数：
- `what`：Arguments that form the error message.

### PushError

```csharp
public static void PushError(string message)
```

Pushes an error message to Godot's built-in debugger and to the OS terminal.

Note: Errors printed this way will not pause project execution.

参数：
- `message`：Error message.

### PushWarning

```csharp
public static void PushWarning(params object[] what)
```

Pushes a warning message to Godot's built-in debugger and to the OS terminal.

参数：
- `what`：Arguments that form the warning message.

### PushWarning

```csharp
public static void PushWarning(string message)
```

Pushes a warning message to Godot's built-in debugger and to the OS terminal.

参数：
- `message`：Warning message.

### RandFromSeed

```csharp
public static uint RandFromSeed(ref ulong seed)
```

Given a `seed`, returns a randomized `uint`
value. The `seed` may be modified.
Passing the same `seed` consistently returns the same value.

Note: "Seed" here refers to the internal state of the pseudo random number
generator, currently implemented as a 64 bit integer.

参数：
- `seed`：Seed to use to generate the random number.
If a different seed is used, its value will be modified.

返回：A random `uint` number.

### RandRange

```csharp
public static double RandRange(double from, double to)
```

Returns a random floating point value between `from`
and `to` (inclusive).

返回：A random `double` number inside the given range.

### RandRange

```csharp
public static int RandRange(int from, int to)
```

Returns a random signed 32-bit integer between `from`
and `to` (inclusive). If `to` is lesser than
`from`, they are swapped.

返回：A random `int` number inside the given range.

### Randf

```csharp
public static float Randf()
```

Returns a random floating point value between `0.0` and `1.0` (inclusive).

返回：A random `float` number.

### Randfn

```csharp
public static double Randfn(double mean, double deviation)
```

Returns a normally-distributed pseudo-random floating point value
using Box-Muller transform with the specified
and a standard `deviation`.
This is also called Gaussian distribution.

返回：A random normally-distributed `float` number.

### Randi

```csharp
public static uint Randi()
```

Returns a random unsigned 32-bit integer.
Use remainder to obtain a random value in the interval `[0, N - 1]`
(where N is smaller than 2^32).

返回：A random `uint` number.

### Randomize

```csharp
public static void Randomize()
```

Randomizes the seed (or the internal state) of the random number generator.
The current implementation uses a number based on the device's time.

Note: This method is called automatically when the project is run.
If you need to fix the seed to have consistent, reproducible results,
use `Seed()` to initialize the random number generator.

### Range

```csharp
public static IEnumerable<int> Range(int end)
```

Returns a `System.Collections.Generic.IEnumerable` that iterates from
`0` (inclusive) to `end` (exclusive)
in steps of `1`.

参数：
- `end`：The last index.

### Range

```csharp
public static IEnumerable<int> Range(int start, int end)
```

Returns a `System.Collections.Generic.IEnumerable` that iterates from
`start` (inclusive) to `end` (exclusive)
in steps of `1`.

参数：
- `start`：The first index.
- `end`：The last index.

### Range

```csharp
public static IEnumerable<int> Range(int start, int end, int step)
```

Returns a `System.Collections.Generic.IEnumerable` that iterates from
`start` (inclusive) to `end` (exclusive)
in steps of `step`.
The argument `step` can be negative, but not `0`.

参数：
- `start`：The first index.
- `end`：The last index.
- `step`：The amount by which to increment the index on each iteration.

### Seed

```csharp
public static void Seed(ulong seed)
```

Sets seed for the random number generator to `seed`.
Setting the seed manually can ensure consistent, repeatable results for
most random functions.

参数：
- `seed`：Seed that will be used.

### StrToVar

```csharp
public static Variant StrToVar(string str)
```

Converts a formatted string that was returned by `VarToStr()`
to the original value.

参数：
- `str`：String that will be converted to Variant.

返回：The decoded `Variant`.

### Stringify

```csharp
public static string Stringify(Color[] instance)
```

Converts this Color array to a string with brackets.

参数：
- `instance`：The Color array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(Vector2I[] instance)
```

Converts this Vector2I array to a string with brackets.

参数：
- `instance`：The Vector2I array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(Vector2[] instance)
```

Converts this Vector2 array to a string with brackets.

参数：
- `instance`：The Vector2 array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(Vector3I[] instance)
```

Converts this Vector3I array to a string with brackets.

参数：
- `instance`：The Vector3I array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(Vector3[] instance)
```

Converts this Vector3 array to a string with brackets.

参数：
- `instance`：The Vector3 array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(Vector4I[] instance)
```

Converts this Vector4I array to a string with brackets.

参数：
- `instance`：The Vector4I array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(Vector4[] instance)
```

Converts this Vector4 array to a string with brackets.

参数：
- `instance`：The Vector4 array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(byte[] instance)
```

Converts this byte array to a string with brackets.

参数：
- `instance`：The byte array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(double[] instance)
```

Converts this double array to a string with brackets.

参数：
- `instance`：The double array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(float[] instance)
```

Converts this float array to a string with brackets.

参数：
- `instance`：The float array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(int[] instance)
```

Converts this int array to a string with brackets.

参数：
- `instance`：The int array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(long[] instance)
```

Converts this long array to a string with brackets.

参数：
- `instance`：The long array to convert.

返回：A single string with all items.

### Stringify

```csharp
public static string Stringify(string[] instance)
```

Converts this string array to a string with brackets.

参数：
- `instance`：The string array to convert.

返回：A single string with all items.

### TypeToVariantType

```csharp
public static Variant.Type TypeToVariantType(Type type)
```

Get the [`Variant.Type`](Variant.md) that corresponds for the given `System.Type`.

返回：The [`Variant.Type`](Variant.md) for the given `type`.

### VarToBytes

```csharp
public static byte[] VarToBytes(Variant var)
```

Encodes a [`Variant`](Variant.md) value to a byte array, without encoding objects.
Deserialization can be done with `BytesToVar()`.
Note: If you need object serialization, see `VarToBytesWithObjects()`.

参数：
- `var`：[`Variant`](Variant.md) that will be encoded.

返回：The [`Variant`](Variant.md) encoded as an array of bytes.

### VarToBytesWithObjects

```csharp
public static byte[] VarToBytesWithObjects(Variant var)
```

Encodes a [`Variant`](Variant.md). Encoding objects is allowed (and can potentially
include executable code). Deserialization can be done with `BytesToVarWithObjects()`.

参数：
- `var`：[`Variant`](Variant.md) that will be encoded.

返回：The [`Variant`](Variant.md) encoded as an array of bytes.

### VarToStr

```csharp
public static string VarToStr(Variant var)
```

Converts a [`Variant`](Variant.md) `var` to a formatted string that
can later be parsed using `StrToVar()`.

参数：
- `var`：Variant that will be converted to string.

返回：The [`Variant`](Variant.md) encoded as a string.

## 常量

### Int16Max

```csharp
public const long Int16Max = 32767
```

Maximum value of a 16-bit signed integer.

### Int16Min

```csharp
public const long Int16Min = -32768
```

Minimum value of a 16-bit signed integer.

### Int32Max

```csharp
public const long Int32Max = 2147483647
```

Maximum value of a 32-bit signed integer.

### Int32Min

```csharp
public const long Int32Min = -2147483648
```

Minimum value of a 32-bit signed integer.

### Int64Max

```csharp
public const long Int64Max = 9223372036854775807
```

Maximum value of a 64-bit signed integer.

### Int64Min

```csharp
public const long Int64Min = -9223372036854775808
```

Minimum value of a 64-bit signed integer.

### Int8Max

```csharp
public const long Int8Max = 127
```

Maximum value of an 8-bit signed integer.

### Int8Min

```csharp
public const long Int8Min = -128
```

Minimum value of an 8-bit signed integer.

### Uint16Max

```csharp
public const long Uint16Max = 65535
```

Maximum value of a 16-bit unsigned integer.

### Uint32Max

```csharp
public const long Uint32Max = 4294967295
```

Maximum value of a 32-bit unsigned integer.

### Uint8Max

```csharp
public const long Uint8Max = 255
```

Maximum value of an 8-bit unsigned integer.
