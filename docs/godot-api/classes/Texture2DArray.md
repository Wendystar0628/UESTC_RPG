<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Texture2DArray

命名空间 `Godot`；class；继承 [`ImageTextureLayered`](ImageTextureLayered.md) → [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A Texture2DArray is different from a Texture3D: The Texture2DArray does not support trilinear interpolation between the [`Image`](Image.md)s, i.e. no blending. See also [`Cubemap`](Cubemap.md) and [`CubemapArray`](CubemapArray.md), which are texture arrays with specialized cubemap functions.

A Texture2DArray is also different from an [`AtlasTexture`](AtlasTexture.md): In a Texture2DArray, all images are treated separately. In an atlas, the regions (i.e. the single images) can be of different sizes. Furthermore, you usually need to add a padding around the regions, to prevent accidental UV mapping to more than one region. The same goes for mipmapping: Mipmap chains are handled separately for each layer. In an atlas, the slicing has to be done manually in the fragment shader.

To create such a texture file yourself, reimport your image files using the Godot Editor import presets. To create a Texture2DArray from code, use [`ImageTextureLayered.CreateFromImages`](ImageTextureLayered.md) on an instance of the Texture2DArray class.

## 构造函数

### Texture2DArray

```csharp
public Texture2DArray()
```

## 方法

### CreatePlaceholder

```csharp
public Resource CreatePlaceholder()
```

引擎名：`create_placeholder`

Creates a placeholder version of this resource ([`PlaceholderTexture2DArray`](PlaceholderTexture2DArray.md)).
