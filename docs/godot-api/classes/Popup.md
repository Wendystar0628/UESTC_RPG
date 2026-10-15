<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Popup

命名空间 `Godot`；class；继承 [`Window`](Window.md) → [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Popup` is a base class for contextual windows and panels with fixed position. It's a modal by default (see [`Window.PopupWindow`](Window.md)) and provides methods for implementing custom popup behavior.

**Note:** `Popup` is invisible by default. To make it visible, call one of the `popup_*` methods from [`Window`](Window.md) on the node, such as [`Window.PopupCenteredClamped`](Window.md).

## 构造函数

### Popup

```csharp
public Popup()
```

## 事件（信号）

### PopupHide

```csharp
public event Action PopupHide
```

引擎名：`popup_hide`

Emitted when the popup is hidden.
