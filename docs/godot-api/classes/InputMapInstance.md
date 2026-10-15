<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputMapInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Manages all [`InputEventAction`](InputEventAction.md) which can be created/modified from the project settings menu **Project > Project Settings > Input Map** or in code with `AddAction()` and `ActionAddEvent()`. See [`Node._Input()`](Node.md).

## 方法

### ActionAddEvent

```csharp
public void ActionAddEvent(StringName action, InputEvent event)
```

Adds an [`InputEvent`](InputEvent.md) to an action. This [`InputEvent`](InputEvent.md) will trigger the action.

### ActionEraseEvent

```csharp
public void ActionEraseEvent(StringName action, InputEvent event)
```

Removes an [`InputEvent`](InputEvent.md) from an action.

### ActionEraseEvents

```csharp
public void ActionEraseEvents(StringName action)
```

Removes all events from an action.

### ActionGetDeadzone

```csharp
public float ActionGetDeadzone(StringName action)
```

Returns a deadzone value for the action.

### ActionGetEvents

```csharp
public Array<InputEvent> ActionGetEvents(StringName action)
```

Returns an array of [`InputEvent`](InputEvent.md)s associated with a given action.

**Note:** When used in the editor (e.g. a tool script or `EditorPlugin`), this method will return events for the editor action. If you want to access your project's input binds from the editor, read the `input/*` settings from [`ProjectSettings`](ProjectSettings.md).

### ActionHasEvent

```csharp
public bool ActionHasEvent(StringName action, InputEvent event)
```

Returns `true` if the action has the given [`InputEvent`](InputEvent.md) associated with it.

### ActionSetDeadzone

```csharp
public void ActionSetDeadzone(StringName action, float deadzone)
```

Sets a deadzone value for the action.

### AddAction

```csharp
public void AddAction(StringName action, float deadzone = 0.2f)
```

Adds an empty action to the [`InputMap`](InputMap.md) with a configurable `deadzone`.

An [`InputEvent`](InputEvent.md) can then be added to this action with `ActionAddEvent()`.

### EraseAction

```csharp
public void EraseAction(StringName action)
```

Removes an action from the [`InputMap`](InputMap.md).

### EventIsAction

```csharp
public bool EventIsAction(InputEvent event, StringName action, bool exactMatch = false)
```

Returns `true` if the given event is part of an existing action. This method ignores keyboard modifiers if the given [`InputEvent`](InputEvent.md) is not pressed (for proper release detection). See `ActionHasEvent()` if you don't want this behavior.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

### GetActionDescription

```csharp
public string GetActionDescription(StringName action)
```

Returns the human-readable description of the given action.

### GetActions

```csharp
public Array<StringName> GetActions()
```

Returns an array of all actions in the [`InputMap`](InputMap.md).

### HasAction

```csharp
public bool HasAction(StringName action)
```

Returns `true` if the [`InputMap`](InputMap.md) has a registered action with the given name.

### LoadFromProjectSettings

```csharp
public void LoadFromProjectSettings()
```

Clears all [`InputEventAction`](InputEventAction.md) in the [`InputMap`](InputMap.md) and load it anew from [`ProjectSettings`](ProjectSettings.md).

## 事件（信号）

### ProjectSettingsLoaded

```csharp
public event Action ProjectSettingsLoaded
```

Emitted when the [`ProjectSettings`](ProjectSettings.md) [`InputMap`](InputMap.md) has been loaded.
