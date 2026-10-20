<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventShortcut

命名空间 `Godot`；class；继承 [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

InputEventShortcut is a special event that can be received in [`Node._Input()`](Node.md), [`Node._ShortcutInput()`](Node.md), and [`Node._UnhandledInput()`](Node.md). It is typically sent by the editor's Command Palette to trigger actions, but can also be sent manually using [`Viewport.PushInput`](Viewport.md).

## 构造函数

### InputEventShortcut

```csharp
public InputEventShortcut()
```

## 属性

### Shortcut

```csharp
public Shortcut Shortcut { get; set; }
```

引擎名：`shortcut`

The [`Shortcut`](Shortcut.md) represented by this event. Its [`Shortcut.MatchesEvent`](Shortcut.md) method will always return `true` for this event.

## 方法

### GetShortcut

```csharp
public Shortcut GetShortcut()
```

引擎名：`get_shortcut`

### SetShortcut

```csharp
public void SetShortcut(Shortcut shortcut)
```

引擎名：`set_shortcut`
