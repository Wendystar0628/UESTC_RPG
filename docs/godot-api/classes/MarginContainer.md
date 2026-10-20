<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MarginContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`MarginContainer` adds an adjustable margin on each side of its child controls. The margins are added around all children, not around each individual one. To control the `MarginContainer`'s margins, use the `margin_*` theme properties listed below.

**Note:** The margin sizes are theme overrides, not normal properties. This is an example of how to change them in code:

```csharp
// This code sample assumes the current script is extending MarginContainer.
int marginValue = 100;
AddThemeConstantOverride("margin_top", marginValue);
AddThemeConstantOverride("margin_left", marginValue);
AddThemeConstantOverride("margin_bottom", marginValue);
AddThemeConstantOverride("margin_right", marginValue);

```

## 构造函数

### MarginContainer

```csharp
public MarginContainer()
```
