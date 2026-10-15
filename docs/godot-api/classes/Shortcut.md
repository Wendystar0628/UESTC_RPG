<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Shortcut

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Shortcuts (also known as hotkeys) are containers of [`InputEvent`](InputEvent.md) resources. They are commonly used to interact with a [`Control`](Control.md) element from an [`InputEvent`](InputEvent.md).

One shortcut can contain multiple [`InputEvent`](InputEvent.md) resources, making it possible to trigger one action with multiple different inputs.

**Example:** Capture the Ctrl + S shortcut using a `Shortcut` resource:

```csharp
using Godot;

public partial class MyNode : Node
{
private readonly Shortcut _saveShortcut = new Shortcut();

public override void _Ready()
{
InputEventKey keyEvent = new InputEventKey
{
Keycode = Key.S,
CtrlPressed = true,
CommandOrControlAutoremap = true, // Swaps Ctrl for Command on Mac.
};

_saveShortcut.Events = [keyEvent];
}

public override void _Input(InputEvent @event)
{
if (@event is InputEventKey keyEvent &&
_saveShortcut.MatchesEvent(@event) &&
keyEvent.Pressed && !keyEvent.Echo)
{
GD.Print("Save shortcut pressed!");
GetViewport().SetInputAsHandled();
}
}
}

```

## 构造函数

### Shortcut

```csharp
public Shortcut()
```

## 属性

### Events

```csharp
public Array Events { get; set; }
```

引擎名：`events`

The shortcut's [`InputEvent`](InputEvent.md) array.

Generally the [`InputEvent`](InputEvent.md) used is an [`InputEventKey`](InputEventKey.md), though it can be any [`InputEvent`](InputEvent.md), including an [`InputEventAction`](InputEventAction.md).

## 方法

### GetAsText

```csharp
public string GetAsText()
```

引擎名：`get_as_text`

Returns the shortcut's first valid [`InputEvent`](InputEvent.md) as a `System.String`.

### GetEvents

```csharp
public Array GetEvents()
```

引擎名：`get_events`

### HasValidEvent

```csharp
public bool HasValidEvent()
```

引擎名：`has_valid_event`

Returns whether `Events` contains an [`InputEvent`](InputEvent.md) which is valid.

### MatchesEvent

```csharp
public bool MatchesEvent(InputEvent event)
```

引擎名：`matches_event`

Returns whether any [`InputEvent`](InputEvent.md) in `Events` equals `event`. This uses [`InputEvent.IsMatch`](InputEvent.md) to compare events.

### SetEvents

```csharp
public void SetEvents(Array events)
```

引擎名：`set_events`
