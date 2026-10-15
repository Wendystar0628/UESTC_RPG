<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ImageTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A [`Texture2D`](Texture2D.md) based on an [`Image`](Image.md). For an image to be displayed, an `ImageTexture` has to be created from it using the `CreateFromImage()` method:

```csharp
var image = Image.load_from_file("res://icon.svg")
var texture = ImageTexture.create_from_image(image)
$Sprite2D.texture = texture

```

This way, textures can be created at run-time by loading images both from within the editor and externally.

**Warning:** Prefer to load imported textures with `@GDScript.load` over loading them from within the filesystem dynamically with [`Image.Load`](Image.md), as it may not work in exported projects:

```csharp
var texture = load("res://icon.svg")
$Sprite2D.texture = texture

```

This is because images have to be imported as a [`CompressedTexture2D`](CompressedTexture2D.md) first to be loaded with `@GDScript.load`. If you'd still like to load an image file just like any other [`Resource`](Resource.md), import it as an [`Image`](Image.md) resource instead, and then load it normally using the `@GDScript.load` method.

**Note:** The image can be retrieved from an imported texture using the [`Texture2D.GetImage`](Texture2D.md) method, which returns a copy of the image:

```csharp
var texture = load("res://icon.svg")
var image = texture.get_image()

```

An `ImageTexture` is not meant to be operated from within the editor interface directly, and is mostly useful for rendering images on screen dynamically via code. If you need to generate images procedurally from within the editor, consider saving and importing images as custom texture resources implementing a new `EditorImportPlugin`.

**Note:** The maximum texture size is 16384×16384 pixels due to graphics hardware limitations.

## 构造函数

### ImageTexture

```csharp
public ImageTexture()
```

## 属性

### Image

```csharp
public Image Image { get; set; }
```

引擎名：`image`

## 方法

### CreateFromImage

```csharp
public static ImageTexture CreateFromImage(Image image)
```

引擎名：`create_from_image`

Creates a new `ImageTexture` and initializes it by allocating and setting the data from an [`Image`](Image.md).

### SetImage

```csharp
public void SetImage(Image image)
```

引擎名：`set_image`

Replaces the texture's data with a new [`Image`](Image.md). This will re-allocate new memory for the texture.

If you want to update the image, but don't need to change its parameters (format, size), use `Update()` instead for better performance.

### SetSizeOverride

```csharp
public void SetSizeOverride(Vector2I size)
```

引擎名：`set_size_override`

Resizes the texture to the specified dimensions.

### Update

```csharp
public void Update(Image image)
```

引擎名：`update`

Replaces the texture's data with a new [`Image`](Image.md).

**Note:** The texture has to be created using `CreateFromImage()` or initialized first with the `SetImage()` method before it can be updated. The new image dimensions, format, and mipmaps configuration should match the existing texture's image configuration.

Use this method over `SetImage()` if you need to update the texture frequently, which is faster than allocating additional memory for a new texture each time.
