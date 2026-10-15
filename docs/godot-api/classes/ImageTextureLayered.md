<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ImageTextureLayered

命名空间 `Godot`；class；继承 [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for [`Texture2DArray`](Texture2DArray.md), [`Cubemap`](Cubemap.md) and [`CubemapArray`](CubemapArray.md). Cannot be used directly, but contains all the functions necessary for accessing the derived resource types. See also [`Texture3D`](Texture3D.md).

## 属性

### _Images

```csharp
public Array<Image> _Images { get; set; }
```

## 方法

### CreateFromImages

```csharp
public Error CreateFromImages(Array<Image> images)
```

引擎名：`create_from_images`

Creates an `ImageTextureLayered` from an array of [`Image`](Image.md)s. See [`Image.Create`](Image.md) for the expected data format. The first image decides the width, height, image format and mipmapping setting. The other images *must* have the same width, height, image format and mipmapping setting.

Each [`Image`](Image.md) represents one `layer`.

```csharp
# Fill in an array of Images with different colors.
var images = []
const LAYERS = 6
for i in LAYERS:
var image = Image.create_empty(128, 128, false, Image.FORMAT_RGB8)
if i % 3 == 0:
image.fill(Color.RED)
elif i % 3 == 1:
image.fill(Color.GREEN)
else:
image.fill(Color.BLUE)
images.push_back(image)

# Create and save a 2D texture array. The array of images must have at least 1 Image.
var texture_2d_array = Texture2DArray.new()
texture_2d_array.create_from_images(images)
ResourceSaver.save(texture_2d_array, "res://texture_2d_array.res", ResourceSaver.FLAG_COMPRESS)

# Create and save a cubemap. The array of images must have exactly 6 Images.
# The cubemap's images are specified in this order: X+, X-, Y+, Y-, Z+, Z-
# (in Godot's coordinate system, so Y+ is "up" and Z- is "forward").
var cubemap = Cubemap.new()
cubemap.create_from_images(images)
ResourceSaver.save(cubemap, "res://cubemap.res", ResourceSaver.FLAG_COMPRESS)

# Create and save a cubemap array. The array of images must have a multiple of 6 Images.
# Each cubemap's images are specified in this order: X+, X-, Y+, Y-, Z+, Z-
# (in Godot's coordinate system, so Y+ is "up" and Z- is "forward").
var cubemap_array = CubemapArray.new()
cubemap_array.create_from_images(images)
ResourceSaver.save(cubemap_array, "res://cubemap_array.res", ResourceSaver.FLAG_COMPRESS)

```

### UpdateLayer

```csharp
public void UpdateLayer(Image image, int layer)
```

引擎名：`update_layer`

Replaces the existing [`Image`](Image.md) data at the given `layer` with this new image.

The given [`Image`](Image.md) must have the same width, height, image format, and mipmapping flag as the rest of the referenced images.

If the image format is unsupported, it will be decompressed and converted to a similar and supported [`Image.Format`](Image.md).

The update is immediate: it's synchronized with drawing.
