<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceImporterDynamicFont

命名空间 `Godot`；class；继承 [`ResourceImporter`](ResourceImporter.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Unlike bitmap fonts, dynamic fonts can be resized to any size and still look crisp. Dynamic fonts also optionally support MSDF font rendering, which allows for run-time scale changes with no re-rasterization cost.

While WOFF and especially WOFF2 tend to result in smaller file sizes, there is no universally "better" font format. In most situations, it's recommended to use the font format that was shipped on the font developer's website.

See also [`ResourceImporterBMFont`](ResourceImporterBMFont.md) and [`ResourceImporterImageFont`](ResourceImporterImageFont.md).

## 构造函数

### ResourceImporterDynamicFont

```csharp
public ResourceImporterDynamicFont()
```
