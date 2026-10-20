<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StringExtensions

命名空间 `Godot`；static class

Extension methods to manipulate strings.

## 方法

### Bigrams

```csharp
public static string[] Bigrams(string instance)
```

Returns the bigrams (pairs of consecutive letters) of this string.

参数：
- `instance`：The string that will be used.

返回：The bigrams of this string.

### BinToInt

```csharp
public static int BinToInt(string instance)
```

Converts a string containing a binary number into an integer.
Binary strings can either be prefixed with `0b` or not,
and they can also start with a `-` before the optional prefix.

参数：
- `instance`：The string to convert.

返回：The converted string.

### CEscape

```csharp
public static string CEscape(string instance)
```

Returns a copy of the string with special characters escaped using the C language standard.

参数：
- `instance`：The string to escape.

返回：The escaped string.

### CUnescape

```csharp
public static string CUnescape(string instance)
```

Returns a copy of the string with escaped characters replaced by their meanings
according to the C language standard.

参数：
- `instance`：The string to unescape.

返回：The unescaped string.

### Capitalize

```csharp
public static string Capitalize(string instance)
```

Changes the case of some letters. Replace underscores with spaces, convert all letters
to lowercase then capitalize first and every letter following the space character.
For `capitalize camelCase mixed_with_underscores` it will return
`Capitalize Camelcase Mixed With Underscores`.

参数：
- `instance`：The string to capitalize.

返回：The capitalized string.

### CasecmpTo

```csharp
public static int CasecmpTo(string instance, string to)
```

Performs a case-sensitive comparison to another string and returns an integer that indicates their relative position in the sort order.

参数：
- `instance`：The string to compare.
- `to`：The other string to compare.

返回：An integer that indicates the lexical relationship between the two comparands.

### CompareTo

```csharp
public static int CompareTo(string instance, string to, bool caseSensitive = true)
```

Performs a comparison to another string and returns an integer that indicates their relative position in the sort order.

参数：
- `instance`：The string to compare.
- `to`：The other string to compare.
- `caseSensitive`：If `true`, the comparison will be case sensitive.

返回：An integer that indicates the lexical relationship between the two comparands.

### Count

```csharp
public static int Count(string instance, string what, int from = 0, int to = 0, bool caseSensitive = true)
```

Returns the number of occurrences of substring `what` in the string.

参数：
- `instance`：The string where the substring will be searched.
- `what`：The substring that will be counted.
- `from`：Index to start searching from.
- `to`：Index to stop searching at.
- `caseSensitive`：If the search is case sensitive.

返回：Number of occurrences of the substring in the string.

### CountN

```csharp
public static int CountN(string instance, string what, int from = 0, int to = 0)
```

Returns the number of occurrences of substring `what` (ignoring case)
between `from` and `to` positions. If `from`
and `to` equals 0 the whole string will be used. If only `to`
equals 0 the remained substring will be used.

参数：
- `instance`：The string where the substring will be searched.
- `what`：The substring that will be counted.
- `from`：Index to start searching from.
- `to`：Index to stop searching at.

返回：Number of occurrences of the substring in the string.

### Dedent

```csharp
public static string Dedent(string instance)
```

Returns a copy of the string with indentation (leading tabs and spaces) removed.
See also `Indent()` to add indentation.

参数：
- `instance`：The string to remove the indentation from.

返回：The string with the indentation removed.

### Find

```csharp
public static int Find(string instance, char what, int from = 0, bool caseSensitive = true)
```

Find the first occurrence of a char. Optionally, the search starting position can be passed.

参数：
- `instance`：The string that will be searched.
- `what`：The substring to find.
- `from`：The search starting position.
- `caseSensitive`：If `true`, the search is case sensitive.

返回：The first instance of the char, or -1 if not found.

### Find

```csharp
public static int Find(string instance, string what, int from = 0, bool caseSensitive = true)
```

Returns the index of the first occurrence of the specified string in this instance,
or `-1`. Optionally, the starting search index can be specified, continuing
to the end of the string.
Note: If you just want to know whether a string contains a substring, use the
`System.String.Contains()` method.

参数：
- `instance`：The string that will be searched.
- `what`：The substring to find.
- `from`：The search starting position.
- `caseSensitive`：If `true`, the search is case sensitive.

返回：The starting position of the substring, or -1 if not found.

### FindN

```csharp
public static int FindN(string instance, string what, int from = 0)
```

Returns the index of the first case-insensitive occurrence of the specified string in this instance,
or `-1`. Optionally, the starting search index can be specified, continuing
to the end of the string.

参数：
- `instance`：The string that will be searched.
- `what`：The substring to find.
- `from`：The search starting position.

返回：The starting position of the substring, or -1 if not found.

### GetBaseDir

```csharp
public static string GetBaseDir(string instance)
```

If the string is a path to a file, return the base directory.

参数：
- `instance`：The path to a file.

返回：The base directory.

### GetBaseName

```csharp
public static string GetBaseName(string instance)
```

If the string is a path to a file, return the path to the file without the extension.

参数：
- `instance`：The path to a file.

返回：The path to the file without the extension.

### GetExtension

```csharp
public static string GetExtension(string instance)
```

Returns the extension without the leading period character (`.`)
if the string is a valid file name or path. If the string does not contain
an extension, returns an empty string instead.

参数：
- `instance`：The path to a file.

返回：The extension of the file or an empty string.

### GetFile

```csharp
public static string GetFile(string instance)
```

If the string is a path to a file, return the file and ignore the base directory.

参数：
- `instance`：The path to a file.

返回：The file name.

### GetStringFromAscii

```csharp
public static string GetStringFromAscii(byte[] bytes)
```

Converts ASCII encoded array to string.
Fast alternative to `GetStringFromUtf8()` if the
content is ASCII-only. Unlike the UTF-8 function this function
maps every byte to a character in the array. Multibyte sequences
will not be interpreted correctly. For parsing user input always
use `GetStringFromUtf8()`.

参数：
- `bytes`：A byte array of ASCII characters (on the range of 0-127).

返回：A string created from the bytes.

### GetStringFromUtf16

```csharp
public static string GetStringFromUtf16(byte[] bytes)
```

Converts UTF-16 encoded array to string using the little endian byte order.

参数：
- `bytes`：A byte array of UTF-16 characters.

返回：A string created from the bytes.

### GetStringFromUtf32

```csharp
public static string GetStringFromUtf32(byte[] bytes)
```

Converts UTF-32 encoded array to string using the little endian byte order.

参数：
- `bytes`：A byte array of UTF-32 characters.

返回：A string created from the bytes.

### GetStringFromUtf8

```csharp
public static string GetStringFromUtf8(byte[] bytes)
```

Converts UTF-8 encoded array to string.
Slower than `GetStringFromAscii()` but supports UTF-8
encoded data. Use this function if you are unsure about the
source of the data. For user input this function
should always be preferred.

参数：
- `bytes`：A byte array of UTF-8 characters (a character may take up multiple bytes).

返回：A string created from the bytes.

### Hash

```csharp
public static uint Hash(string instance)
```

Hash the string and return a 32 bits unsigned integer.

参数：
- `instance`：The string to hash.

返回：The calculated hash of the string.

### HexDecode

```csharp
public static byte[] HexDecode(string instance)
```

Decodes a hexadecimal string.

参数：
- `instance`：The hexadecimal string.

返回：The byte array representation of this string.

### HexEncode

```csharp
public static string HexEncode(byte[] bytes)
```

Returns a hexadecimal representation of this byte array as a string.

参数：
- `bytes`：The byte array to encode.

返回：The hexadecimal representation of this byte array.

### HexToInt

```csharp
public static int HexToInt(string instance)
```

Converts a string containing a hexadecimal number into an integer.
Hexadecimal strings can either be prefixed with `0x` or not,
and they can also start with a `-` before the optional prefix.

参数：
- `instance`：The string to convert.

返回：The converted string.

### Indent

```csharp
public static string Indent(string instance, string prefix)
```

Returns a copy of the string with lines indented with `prefix`.
For example, the string can be indented with two tabs using `"\t\t"`,
or four spaces using `"    "`. The prefix can be any string so it can
also be used to comment out strings with e.g. `"//`.
See also `Dedent()` to remove indentation.
Note: Empty lines are kept empty.

参数：
- `instance`：The string to add indentation to.
- `prefix`：The string to use as indentation.

返回：The string with indentation added.

### IsAbsolutePath

```csharp
public static bool IsAbsolutePath(string instance)
```

Returns `true` if the string is a path to a file or
directory and its starting point is explicitly defined. This includes
`res://`, `user://`, `C:\`, `/`, etc.

参数：
- `instance`：The string to check.

返回：If the string is an absolute path.

### IsRelativePath

```csharp
public static bool IsRelativePath(string instance)
```

Returns `true` if the string is a path to a file or
directory and its starting point is implicitly defined within the
context it is being used. The starting point may refer to the current
directory (`./`), or the current [`Node`](Node.md).

参数：
- `instance`：The string to check.

返回：If the string is a relative path.

### IsSubsequenceOf

```csharp
public static bool IsSubsequenceOf(string instance, string text, bool caseSensitive = true)
```

Check whether this string is a subsequence of the given string.

参数：
- `instance`：The subsequence to search.
- `text`：The string that contains the subsequence.
- `caseSensitive`：If `true`, the check is case sensitive.

返回：If the string is a subsequence of the given string.

### IsSubsequenceOfN

```csharp
public static bool IsSubsequenceOfN(string instance, string text)
```

Check whether this string is a subsequence of the given string, ignoring case differences.

参数：
- `instance`：The subsequence to search.
- `text`：The string that contains the subsequence.

返回：If the string is a subsequence of the given string.

### IsValidFileName

```csharp
public static bool IsValidFileName(string instance)
```

Returns `true` if this string is free from characters that
aren't allowed in file names.

参数：
- `instance`：The string to check.

返回：If the string contains a valid file name.

### IsValidFloat

```csharp
public static bool IsValidFloat(string instance)
```

Returns `true` if this string contains a valid `float`.
This is inclusive of integers, and also supports exponents.

参数：
- `instance`：The string to check.

返回：If the string contains a valid floating point number.

### IsValidHexNumber

```csharp
public static bool IsValidHexNumber(string instance, bool withPrefix = false)
```

Returns `true` if this string contains a valid hexadecimal number.
If `withPrefix` is `true`, then a validity of the
hexadecimal number is determined by `0x` prefix, for instance: `0xDEADC0DE`.

参数：
- `instance`：The string to check.
- `withPrefix`：If the string must contain the `0x` prefix to be valid.

返回：If the string contains a valid hexadecimal number.

### IsValidHtmlColor

```csharp
public static bool IsValidHtmlColor(string instance)
```

Returns `true` if this string contains a valid color in hexadecimal
HTML notation. Other HTML notations such as named colors or `hsl()` aren't
considered valid by this method and will return `false`.

参数：
- `instance`：The string to check.

返回：If the string contains a valid HTML color.

### IsValidIPAddress

```csharp
public static bool IsValidIPAddress(string instance)
```

Returns `true` if this string contains only a well-formatted
IPv4 or IPv6 address. This method considers reserved IP addresses such as
`0.0.0.0` as valid.

参数：
- `instance`：The string to check.

返回：If the string contains a valid IP address.

### IsValidIdentifier

```csharp
public static bool IsValidIdentifier(string instance)
```

Returns `true` if this string is a valid identifier.
A valid identifier may contain only letters, digits and underscores (`_`)
and the first character may not be a digit.

参数：
- `instance`：The string to check.

返回：If the string contains a valid identifier.

### IsValidInt

```csharp
public static bool IsValidInt(string instance)
```

Returns `true` if this string contains a valid `int`.

参数：
- `instance`：The string to check.

返回：If the string contains a valid integer.

### JSONEscape

```csharp
public static string JSONEscape(string instance)
```

Returns a copy of the string with special characters escaped using the JSON standard.

参数：
- `instance`：The string to escape.

返回：The escaped string.

### Left

```csharp
public static string Left(string instance, int pos)
```

Returns an amount of characters from the left of the string.

参数：
- `instance`：The original string.
- `pos`：The position in the string where the left side ends.

返回：The left side of the string from the given position.

### Match

```csharp
public static bool Match(string instance, string expr, bool caseSensitive = true)
```

Do a simple case sensitive expression match, using ? and * wildcards.

参数：
- `instance`：The string to check.
- `expr`：Expression to check.
- `caseSensitive`：If `true`, the check will be case sensitive.

返回：If the expression has any matches.

### MatchN

```csharp
public static bool MatchN(string instance, string expr)
```

Do a simple case insensitive expression match, using ? and * wildcards.

参数：
- `instance`：The string to check.
- `expr`：Expression to check.

返回：If the expression has any matches.

### Md5Buffer

```csharp
public static byte[] Md5Buffer(string instance)
```

Returns the MD5 hash of the string as an array of bytes.

参数：
- `instance`：The string to hash.

返回：The MD5 hash of the string.

### Md5Text

```csharp
public static string Md5Text(string instance)
```

Returns the MD5 hash of the string as a string.

参数：
- `instance`：The string to hash.

返回：The MD5 hash of the string.

### NocasecmpTo

```csharp
public static int NocasecmpTo(string instance, string to)
```

Performs a case-insensitive comparison to another string and returns an integer that indicates their relative position in the sort order.

参数：
- `instance`：The string to compare.
- `to`：The other string to compare.

返回：An integer that indicates the lexical relationship between the two comparands.

### PadDecimals

```csharp
public static string PadDecimals(string instance, int digits)
```

Format a number to have an exact number of `digits`
after the decimal point.

参数：
- `instance`：The string to pad.
- `digits`：Amount of digits after the decimal point.

返回：The string padded with zeroes.

### PadZeros

```csharp
public static string PadZeros(string instance, int digits)
```

Format a number to have an exact number of `digits`
before the decimal point.

参数：
- `instance`：The string to pad.
- `digits`：Amount of digits before the decimal point.

返回：The string padded with zeroes.

### PathJoin

```csharp
public static string PathJoin(string instance, string file)
```

If the string is a path, this concatenates `file`
at the end of the string as a subpath.
E.g. `"this/is".PathJoin("path") == "this/is/path"`.

参数：
- `instance`：The path that will be concatenated.
- `file`：File name to concatenate with the path.

返回：The concatenated path with the given file name.

### RFind

```csharp
public static int RFind(string instance, string what, int from = -1, bool caseSensitive = true)
```

Returns the index of the last occurrence of the specified string in this instance,
or `-1`. Optionally, the starting search index can be specified, continuing to
the beginning of the string.

参数：
- `instance`：The string that will be searched.
- `what`：The substring to search in the string.
- `from`：The position at which to start searching.
- `caseSensitive`：If `true`, the search is case sensitive.

返回：The position at which the substring was found, or -1 if not found.

### RFindN

```csharp
public static int RFindN(string instance, string what, int from = -1)
```

Returns the index of the last case-insensitive occurrence of the specified string in this instance,
or `-1`. Optionally, the starting search index can be specified, continuing to
the beginning of the string.

参数：
- `instance`：The string that will be searched.
- `what`：The substring to search in the string.
- `from`：The position at which to start searching.

返回：The position at which the substring was found, or -1 if not found.

### ReplaceN

```csharp
public static string ReplaceN(string instance, string what, string forwhat)
```

Replace occurrences of a substring for different ones inside the string, but search case-insensitive.

参数：
- `instance`：The string to modify.
- `what`：The substring to be replaced in the string.
- `forwhat`：The substring that replaces `what`.

返回：The string with the substring occurrences replaced.

### Right

```csharp
public static string Right(string instance, int pos)
```

Returns the right side of the string from a given position.

参数：
- `instance`：The original string.
- `pos`：The position in the string from which the right side starts.

返回：The right side of the string from the given position.

### Sha1Buffer

```csharp
public static byte[] Sha1Buffer(string instance)
```

Returns the SHA-1 hash of the string as an array of bytes.

参数：
- `instance`：The string to hash.

返回：The SHA-1 hash of the string.

### Sha1Text

```csharp
public static string Sha1Text(string instance)
```

Returns the SHA-1 hash of the string as a string.

参数：
- `instance`：The string to hash.

返回：The SHA-1 hash of the string.

### Sha256Buffer

```csharp
public static byte[] Sha256Buffer(string instance)
```

Returns the SHA-256 hash of the string as an array of bytes.

参数：
- `instance`：The string to hash.

返回：The SHA-256 hash of the string.

### Sha256Text

```csharp
public static string Sha256Text(string instance)
```

Returns the SHA-256 hash of the string as a string.

参数：
- `instance`：The string to hash.

返回：The SHA-256 hash of the string.

### Similarity

```csharp
public static float Similarity(string instance, string text)
```

Returns the similarity index of the text compared to this string.
1 means totally similar and 0 means totally dissimilar.

参数：
- `instance`：The string to compare.
- `text`：The other string to compare.

返回：The similarity index.

### SimplifyPath

```csharp
public static string SimplifyPath(string instance)
```

Returns a simplified canonical path.

### Split

```csharp
public static string[] Split(string instance, string divisor, bool allowEmpty = true)
```

Split the string by a divisor string, return an array of the substrings.
Example "One,Two,Three" will return ["One","Two","Three"] if split by ",".

参数：
- `instance`：The string to split.
- `divisor`：The divisor string that splits the string.
- `allowEmpty`：If `true`, the array may include empty strings.

返回：The array of strings split from the string.

### SplitFloats

```csharp
public static float[] SplitFloats(string instance, string divisor, bool allowEmpty = true)
```

Split the string in floats by using a divisor string, return an array of the substrings.
Example "1,2.5,3" will return [1,2.5,3] if split by ",".

参数：
- `instance`：The string to split.
- `divisor`：The divisor string that splits the string.
- `allowEmpty`：If `true`, the array may include empty floats.

返回：The array of floats split from the string.

### StripEdges

```csharp
public static string StripEdges(string instance, bool left = true, bool right = true)
```

Returns a copy of the string stripped of any non-printable character
(including tabulations, spaces and line breaks) at the beginning and the end.
The optional arguments are used to toggle stripping on the left and right
edges respectively.

参数：
- `instance`：The string to strip.
- `left`：If the left side should be stripped.
- `right`：If the right side should be stripped.

返回：The string stripped of any non-printable characters.

### StripEscapes

```csharp
public static string StripEscapes(string instance)
```

Returns a copy of the string stripped of any escape character.
These include all non-printable control characters of the first page
of the ASCII table (< 32), such as tabulation (`\t`) and
newline (`\n` and `\r`) characters, but not spaces.

参数：
- `instance`：The string to strip.

返回：The string stripped of any escape characters.

### Substr

```csharp
public static string Substr(string instance, int from, int len)
```

Returns part of the string from the position `from`, with length `len`.

参数：
- `instance`：The string to slice.
- `from`：The position in the string that the part starts from.
- `len`：The length of the returned part.

返回：Part of the string from the position `from`, with length `len`.

### ToAsciiBuffer

```csharp
public static byte[] ToAsciiBuffer(string instance)
```

Converts the String (which is a character array) to PackedByteArray (which is an array of bytes).
The conversion is faster compared to `ToUtf8Buffer()`,
as this method assumes that all the characters in the String are ASCII characters.

参数：
- `instance`：The string to convert.

返回：The string as ASCII encoded bytes.

### ToCamelCase

```csharp
public static string ToCamelCase(string instance)
```

Returns the string converted to `camelCase`.

参数：
- `instance`：The string to convert.

返回：The converted string.

### ToFloat

```csharp
public static float ToFloat(string instance)
```

Converts a string, containing a decimal number, into a `float`.

参数：
- `instance`：The string to convert.

返回：The number representation of the string.

### ToInt

```csharp
public static int ToInt(string instance)
```

Converts a string, containing an integer number, into an `int`.

参数：
- `instance`：The string to convert.

返回：The number representation of the string.

### ToKebabCase

```csharp
public static string ToKebabCase(string instance)
```

Returns the string converted to `kebab-case`.

参数：
- `instance`：The string to convert.

返回：The converted string.

### ToPascalCase

```csharp
public static string ToPascalCase(string instance)
```

Returns the string converted to `PascalCase`.

参数：
- `instance`：The string to convert.

返回：The converted string.

### ToSnakeCase

```csharp
public static string ToSnakeCase(string instance)
```

Returns the string converted to `snake_case`.

参数：
- `instance`：The string to convert.

返回：The converted string.

### ToUtf16Buffer

```csharp
public static byte[] ToUtf16Buffer(string instance)
```

Converts the string (which is an array of characters) to a UTF-16 encoded array of bytes.

参数：
- `instance`：The string to convert.

返回：The string as UTF-16 encoded bytes.

### ToUtf32Buffer

```csharp
public static byte[] ToUtf32Buffer(string instance)
```

Converts the string (which is an array of characters) to a UTF-32 encoded array of bytes.

参数：
- `instance`：The string to convert.

返回：The string as UTF-32 encoded bytes.

### ToUtf8Buffer

```csharp
public static byte[] ToUtf8Buffer(string instance)
```

Converts the string (which is an array of characters) to a UTF-8 encoded array of bytes.
The conversion is a bit slower than `ToAsciiBuffer()`,
but supports all UTF-8 characters. Therefore, you should prefer this function
over `ToAsciiBuffer()`.

参数：
- `instance`：The string to convert.

返回：The string as UTF-8 encoded bytes.

### TrimPrefix

```csharp
public static string TrimPrefix(string instance, string prefix)
```

Removes a given string from the start if it starts with it or leaves the string unchanged.

参数：
- `instance`：The string to remove the prefix from.
- `prefix`：The string to remove from the start.

返回：A copy of the string with the prefix string removed from the start.

### TrimSuffix

```csharp
public static string TrimSuffix(string instance, string suffix)
```

Removes a given string from the end if it ends with it or leaves the string unchanged.

参数：
- `instance`：The string to remove the suffix from.
- `suffix`：The string to remove from the end.

返回：A copy of the string with the suffix string removed from the end.

### URIDecode

```csharp
public static string URIDecode(string instance)
```

Decodes a string in URL encoded format. This is meant to
decode parameters in a URL when receiving an HTTP request.
This mostly wraps around `System.Uri.UnescapeDataString()`,
but also handles `+`.
See `URIEncode()` for encoding.

参数：
- `instance`：The string to decode.

返回：The unescaped string.

### URIEncode

```csharp
public static string URIEncode(string instance)
```

Encodes a string to URL friendly format. This is meant to
encode parameters in a URL when sending an HTTP request.
This wraps around `System.Uri.EscapeDataString()`.
See `URIDecode()` for decoding.

参数：
- `instance`：The string to encode.

返回：The escaped string.

### ValidateNodeName

```csharp
public static string ValidateNodeName(string instance)
```

Removes any characters from the string that are prohibited in
[`Node`](Node.md) names (`.` `:` `@` `/` `"`).

参数：
- `instance`：The string to sanitize.

返回：The string sanitized as a valid node name.

### XMLEscape

```csharp
public static string XMLEscape(string instance)
```

Returns a copy of the string with special characters escaped using the XML standard.

参数：
- `instance`：The string to escape.

返回：The escaped string.

### XMLUnescape

```csharp
public static string XMLUnescape(string instance)
```

Returns a copy of the string with escaped characters replaced by their meanings
according to the XML standard.

参数：
- `instance`：The string to unescape.

返回：The unescaped string.
