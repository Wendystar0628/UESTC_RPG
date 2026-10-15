<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MarshallsInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Provides data transformation and encoding utility functions.

## 方法

### Base64ToRaw

```csharp
public byte[] Base64ToRaw(string base64Str)
```

Returns a decoded `System.Byte`[] corresponding to the Base64-encoded string `base64Str`.

### Base64ToUtf8

```csharp
public string Base64ToUtf8(string base64Str)
```

Returns a decoded string corresponding to the Base64-encoded string `base64Str`.

### Base64ToVariant

```csharp
public Variant Base64ToVariant(string base64Str, bool allowObjects = false)
```

Returns a decoded [`Variant`](Variant.md) corresponding to the Base64-encoded string `base64Str`. If `allowObjects` is `true`, decoding objects is allowed.

Internally, this uses the same decoding mechanism as the `GD.BytesToVar` method.

**Warning:** Deserialized objects can contain code which gets executed. Do not use this option if the serialized object comes from untrusted sources to avoid potential security threats such as remote code execution.

### RawToBase64

```csharp
public string RawToBase64(ReadOnlySpan<byte> array)
```

Returns a Base64-encoded string of a given `System.Byte`[].

### RawToBase64

```csharp
public string RawToBase64(byte[] array)
```

Returns a Base64-encoded string of a given `System.Byte`[].

### Utf8ToBase64

```csharp
public string Utf8ToBase64(string utf8Str)
```

Returns a Base64-encoded string of the UTF-8 string `utf8Str`.

### VariantToBase64

```csharp
public string VariantToBase64(Variant variant, bool fullObjects = false)
```

Returns a Base64-encoded string of the [`Variant`](Variant.md) `variant`. If `fullObjects` is `true`, encoding objects is allowed (and can potentially include code).

Internally, this uses the same encoding mechanism as the `GD.VarToBytes` method.
