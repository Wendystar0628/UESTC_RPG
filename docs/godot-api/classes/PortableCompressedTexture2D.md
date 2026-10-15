<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PortableCompressedTexture2D

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class allows storing compressed textures as self contained (not imported) resources.

For 2D usage (compressed on disk, uncompressed on VRAM), the lossy and lossless modes are recommended. For 3D usage (compressed on VRAM) it depends on the target platform.

If you intend to only use desktop, S3TC or BPTC are recommended. For only mobile, ETC2 is recommended.

For portable, self contained 3D textures that work on both desktop and mobile, Basis Universal is recommended (although it has a small quality cost and longer compression time as a tradeoff).

This resource is intended to be created from code.

## 构造函数

### PortableCompressedTexture2D

```csharp
public PortableCompressedTexture2D()
```

## 属性

### KeepCompressedBuffer

```csharp
public bool KeepCompressedBuffer { get; set; }
```

引擎名：`keep_compressed_buffer`

If `true`, when running in the editor, this texture will keep the source-compressed data in memory, allowing the data to persist after loading. Otherwise, the source-compressed data is lost after loading and the texture can't be re-saved.

**Note:** This property must be set before `CreateFromImage()` for this to work.

### SizeOverride

```csharp
public Vector2 SizeOverride { get; set; }
```

引擎名：`size_override`

Allows overriding the texture's size (for 2D only).

### _Data

```csharp
public byte[] _Data { get; set; }
```

## 方法

### CreateFromImage

```csharp
public void CreateFromImage(Image image, PortableCompressedTexture2D.CompressionMode compressionMode, bool normalMap = false, float lossyQuality = 0.8f)
```

引擎名：`create_from_image`

Initializes the compressed texture from a base image. The compression mode must be provided.

`normalMap` is recommended to ensure optimum quality if this image will be used as a normal map.

If lossy compression is requested, the quality setting can optionally be provided. This maps to Lossy WebP compression quality.

### GetCompressionMode

```csharp
public PortableCompressedTexture2D.CompressionMode GetCompressionMode()
```

引擎名：`get_compression_mode`

Return the compression mode used (valid after initialized).

### GetSizeOverride

```csharp
public Vector2 GetSizeOverride()
```

引擎名：`get_size_override`

### IsKeepingAllCompressedBuffers

```csharp
public static bool IsKeepingAllCompressedBuffers()
```

引擎名：`is_keeping_all_compressed_buffers`

Returns `true` if the flag is overridden for all textures of this type.

### IsKeepingCompressedBuffer

```csharp
public bool IsKeepingCompressedBuffer()
```

引擎名：`is_keeping_compressed_buffer`

### SetBasisuCompressorParams

```csharp
public void SetBasisuCompressorParams(int uastcLevel, float rdoQualityLoss)
```

引擎名：`set_basisu_compressor_params`

Sets the compressor parameters for Basis Universal compression. See also the settings in `ResourceImporterTexture`.

**Note:** This method must be called before `CreateFromImage()` for this to work.

### SetKeepAllCompressedBuffers

```csharp
public static void SetKeepAllCompressedBuffers(bool keep)
```

引擎名：`set_keep_all_compressed_buffers`

If `keep` is `true`, overrides the flag globally for all textures of this type. This is used primarily by the editor.

### SetKeepCompressedBuffer

```csharp
public void SetKeepCompressedBuffer(bool keep)
```

引擎名：`set_keep_compressed_buffer`

### SetSizeOverride

```csharp
public void SetSizeOverride(Vector2 size)
```

引擎名：`set_size_override`

## CompressionMode

命名空间 `Godot`；enum

### 枚举值

- `Lossless = 0`
- `Lossy = 1`
- `BasisUniversal = 2`
- `S3Tc = 3`
- `Etc2 = 4`
- `Bptc = 5`
- `Astc = 6`
