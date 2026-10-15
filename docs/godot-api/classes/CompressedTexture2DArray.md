<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CompressedTexture2DArray

命名空间 `Godot`；class；继承 [`CompressedTextureLayered`](CompressedTextureLayered.md) → [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A texture array that is loaded from a `.ctexarray` file. This file format is internal to Godot; it is created by importing other image formats with the import system. `CompressedTexture2DArray` can use one of 4 compression methods:

- Lossless (WebP or PNG, uncompressed on the GPU)

- Lossy (WebP, uncompressed on the GPU)

- VRAM Compressed (compressed on the GPU)

- VRAM Uncompressed (uncompressed on the GPU)

- Basis Universal (compressed on the GPU. Lower file sizes than VRAM Compressed, but slower to compress and lower quality than VRAM Compressed)

Only **VRAM Compressed** actually reduces the memory usage on the GPU. The **Lossless** and **Lossy** compression methods will reduce the required storage on disk, but they will not reduce memory usage on the GPU as the texture is sent to the GPU uncompressed.

Using **VRAM Compressed** also improves loading times, as VRAM-compressed textures are faster to load compared to textures using lossless or lossy compression. VRAM compression can exhibit noticeable artifacts and is intended to be used for 3D rendering, not 2D.

See [`Texture2DArray`](Texture2DArray.md) for a general description of texture arrays.

## 构造函数

### CompressedTexture2DArray

```csharp
public CompressedTexture2DArray()
```
