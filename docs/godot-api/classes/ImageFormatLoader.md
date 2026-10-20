<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ImageFormatLoader

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The engine supports multiple image formats out of the box (PNG, SVG, JPEG, WebP to name a few), but you can choose to implement support for additional image formats by extending [`ImageFormatLoaderExtension`](ImageFormatLoaderExtension.md).

## LoaderFlags

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Default loading behavior.
- `ForceLinear = 1`：If set, the image is converted from sRGB to linear encoding.
- `ConvertColors = 2`：If set, a predefined color map is applied to the image.
