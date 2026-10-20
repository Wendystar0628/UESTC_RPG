<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CompressedTextureLayered

命名空间 `Godot`；class；继承 [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for [`CompressedTexture2DArray`](CompressedTexture2DArray.md) and [`CompressedTexture3D`](CompressedTexture3D.md). Cannot be used directly, but contains all the functions necessary for accessing the derived resource types. See also [`TextureLayered`](TextureLayered.md).

## 属性

### LoadPath

```csharp
public string LoadPath { get; set; }
```

引擎名：`load_path`

The path the texture should be loaded from.

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

Loads the texture at `path`.
