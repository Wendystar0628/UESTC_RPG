<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CompressedTexture2D

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A texture that is loaded from a `.ctex` file. This file format is internal to Godot; it is created by importing other image formats with the import system. `CompressedTexture2D` can use one of 4 compression methods (including a lack of any compression):

- Lossless (WebP or PNG, uncompressed on the GPU)

- Lossy (WebP, uncompressed on the GPU)

- VRAM Compressed (compressed on the GPU)

- VRAM Uncompressed (uncompressed on the GPU)

- Basis Universal (compressed on the GPU. Lower file sizes than VRAM Compressed, but slower to compress and lower quality than VRAM Compressed)

Only **VRAM Compressed** actually reduces the memory usage on the GPU. The **Lossless** and **Lossy** compression methods will reduce the required storage on disk, but they will not reduce memory usage on the GPU as the texture is sent to the GPU uncompressed.

Using **VRAM Compressed** also improves loading times, as VRAM-compressed textures are faster to load compared to textures using lossless or lossy compression. VRAM compression can exhibit noticeable artifacts and is intended to be used for 3D rendering, not 2D.

## 构造函数

### CompressedTexture2D

```csharp
public CompressedTexture2D()
```

## 属性

### LoadPath

```csharp
public string LoadPath { get; set; }
```

引擎名：`load_path`

The `CompressedTexture2D`'s file path to a `.ctex` file.

## 方法

### GetLoadPath

```csharp
public string GetLoadPath()
```

引擎名：`get_load_path`

### Load

```csharp
public Error Load(string path)
```

引擎名：`load`

Loads the texture from the specified `path`.
