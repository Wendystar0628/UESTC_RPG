<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CompressedTexture3D

命名空间 `Godot`；class；继承 [`Texture3D`](Texture3D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`CompressedTexture3D` is the VRAM-compressed counterpart of [`ImageTexture3D`](ImageTexture3D.md). The file extension for `CompressedTexture3D` files is `.ctex3d`. This file format is internal to Godot; it is created by importing other image formats with the import system.

`CompressedTexture3D` uses VRAM compression, which allows to reduce memory usage on the GPU when rendering the texture. This also improves loading times, as VRAM-compressed textures are faster to load compared to textures using lossless compression. VRAM compression can exhibit noticeable artifacts and is intended to be used for 3D rendering, not 2D.

See [`Texture3D`](Texture3D.md) for a general description of 3D textures.

## 构造函数

### CompressedTexture3D

```csharp
public CompressedTexture3D()
```

## 属性

### LoadPath

```csharp
public string LoadPath { get; set; }
```

引擎名：`load_path`

The `CompressedTexture3D`'s file path to a `.ctex3d` file.

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
