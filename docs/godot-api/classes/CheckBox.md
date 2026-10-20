<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CheckBox

命名空间 `Godot`；class；继承 [`Button`](Button.md) → [`BaseButton`](BaseButton.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`CheckBox` allows the user to choose one of only two possible options. It's similar to [`CheckButton`](CheckButton.md) in functionality, but it has a different appearance. To follow established UX patterns, it's recommended to use `CheckBox` when toggling it has **no** immediate effect on something. For example, it could be used when toggling it will only do something once a confirmation button is pressed.

See also [`BaseButton`](BaseButton.md) which contains common properties and methods associated with this node.

When [`BaseButton.ButtonGroup`](BaseButton.md) specifies a [`ButtonGroup`](ButtonGroup.md), `CheckBox` changes its appearance to that of a radio button and uses the various `radio_*` theme properties.

## 构造函数

### CheckBox

```csharp
public CheckBox()
```
