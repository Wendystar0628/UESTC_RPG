<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRBindingModifierEditor

命名空间 `Godot`；class；继承 [`PanelContainer`](PanelContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This is the default binding modifier editor used in the OpenXR action map.

## 构造函数

### OpenXRBindingModifierEditor

```csharp
public OpenXRBindingModifierEditor()
```

## 方法

### GetBindingModifier

```csharp
public OpenXRBindingModifier GetBindingModifier()
```

引擎名：`get_binding_modifier`

Returns the [`OpenXRBindingModifier`](OpenXRBindingModifier.md) currently being edited.

### Setup

```csharp
public void Setup(OpenXRActionMap actionMap, OpenXRBindingModifier bindingModifier)
```

引擎名：`setup`

Setup this editor for the provided `actionMap` and `bindingModifier`.

## 事件（信号）

### BindingModifierRemoved

```csharp
public event OpenXRBindingModifierEditor.BindingModifierRemovedEventHandler BindingModifierRemoved
```

引擎名：`binding_modifier_removed`

Signal emitted when the user presses the delete binding modifier button for this modifier.

## BindingModifierRemovedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`OpenXRBindingModifierEditor.BindingModifierRemoved`](OpenXRBindingModifierEditor.md) event of a [`OpenXRBindingModifierEditor`](OpenXRBindingModifierEditor.md) class.

### 签名

#### Invoke

```csharp
delegate void BindingModifierRemovedEventHandler(GodotObject bindingModifierEditor)
```
