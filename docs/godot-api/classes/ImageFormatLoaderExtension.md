<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ImageFormatLoaderExtension

命名空间 `Godot`；class；继承 [`ImageFormatLoader`](ImageFormatLoader.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The engine supports multiple image formats out of the box (PNG, SVG, JPEG, WebP to name a few), but you can choose to implement support for additional image formats by extending this class.

Be sure to respect the documented return types and values. You should create an instance of it, and call `AddFormatLoader()` to register that loader during the initialization phase.

## 构造函数

### ImageFormatLoaderExtension

```csharp
public ImageFormatLoaderExtension()
```

## 方法

### AddFormatLoader

```csharp
public void AddFormatLoader()
```

引擎名：`add_format_loader`

Add this format loader to the engine, allowing it to recognize the file extensions returned by `_GetRecognizedExtensions()`.

### RemoveFormatLoader

```csharp
public void RemoveFormatLoader()
```

引擎名：`remove_format_loader`

Remove this format loader from the engine.

### _GetRecognizedExtensions

```csharp
public virtual string[] _GetRecognizedExtensions()
```

引擎名：`_get_recognized_extensions`

Returns the list of file extensions for this image format. Files with the given extensions will be treated as image file and loaded using this class.

### _LoadImage

```csharp
public virtual Error _LoadImage(Image image, FileAccess fileaccess, ImageFormatLoader.LoaderFlags flags, float scale)
```

引擎名：`_load_image`

Loads the content of `fileaccess` into the provided `image`.
