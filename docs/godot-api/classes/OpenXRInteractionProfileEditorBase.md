<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRInteractionProfileEditorBase

命名空间 `Godot`；class；继承 [`HBoxContainer`](HBoxContainer.md) → [`BoxContainer`](BoxContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This is a base class for interaction profile editors used by the OpenXR action map editor. It can be used to create bespoke editors for specific interaction profiles.

## 方法

### Setup

```csharp
public void Setup(OpenXRActionMap actionMap, OpenXRInteractionProfile interactionProfile)
```

引擎名：`setup`

Setup this editor for the provided `actionMap` and `interactionProfile`.
