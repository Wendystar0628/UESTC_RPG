<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorExportPlatformWeb

命名空间 `Godot`；class；继承 [`EditorExportPlatform`](EditorExportPlatform.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

The Web exporter customizes how a web build is handled. In the editor's "Export" window, it is created when adding a new "Web" preset.

**Note:** Godot on Web is rendered inside a `<canvas>` tag. Normally, the canvas cannot be positioned or resized manually, but otherwise acts as the main [`Window`](Window.md) of the application.

## 构造函数

### EditorExportPlatformWeb

```csharp
public EditorExportPlatformWeb()
```
