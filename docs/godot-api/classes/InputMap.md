<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputMap

命名空间 `Godot`；static class

Manages all [`InputEventAction`](InputEventAction.md) which can be created/modified from the project settings menu **Project > Project Settings > Input Map** or in code with `AddAction()` and `ActionAddEvent()`. See [`Node._Input()`](Node.md).

## 属性

### Singleton

```csharp
public static InputMapInstance Singleton { get; }
```

## 方法

### ActionAddEvent

```csharp
public static void ActionAddEvent(StringName action, InputEvent event)
```

引擎名：`action_add_event`

Adds an [`InputEvent`](InputEvent.md) to an action. This [`InputEvent`](InputEvent.md) will trigger the action.

### ActionEraseEvent

```csharp
public static void ActionEraseEvent(StringName action, InputEvent event)
```

引擎名：`action_erase_event`

Removes an [`InputEvent`](InputEvent.md) from an action.

### ActionEraseEvents

```csharp
public static void ActionEraseEvents(StringName action)
```

引擎名：`action_erase_events`

Removes all events from an action.

### ActionGetDeadzone

```csharp
public static float ActionGetDeadzone(StringName action)
```

引擎名：`action_get_deadzone`

Returns a deadzone value for the action.

### ActionGetEvents

```csharp
public static Array<InputEvent> ActionGetEvents(StringName action)
```

引擎名：`action_get_events`

Returns an array of [`InputEvent`](InputEvent.md)s associated with a given action.

**Note:** When used in the editor (e.g. a tool script or `EditorPlugin`), this method will return events for the editor action. If you want to access your project's input binds from the editor, read the `input/*` settings from [`ProjectSettings`](ProjectSettings.md).

### ActionHasEvent

```csharp
public static bool ActionHasEvent(StringName action, InputEvent event)
```

引擎名：`action_has_event`

Returns `true` if the action has the given [`InputEvent`](InputEvent.md) associated with it.

### ActionSetDeadzone

```csharp
public static void ActionSetDeadzone(StringName action, float deadzone)
```

引擎名：`action_set_deadzone`

Sets a deadzone value for the action.

### AddAction

```csharp
public static void AddAction(StringName action, float deadzone = 0.2f)
```

引擎名：`add_action`

Adds an empty action to the `InputMap` with a configurable `deadzone`.

An [`InputEvent`](InputEvent.md) can then be added to this action with `ActionAddEvent()`.

### EraseAction

```csharp
public static void EraseAction(StringName action)
```

引擎名：`erase_action`

Removes an action from the `InputMap`.

### EventIsAction

```csharp
public static bool EventIsAction(InputEvent event, StringName action, bool exactMatch = false)
```

引擎名：`event_is_action`

Returns `true` if the given event is part of an existing action. This method ignores keyboard modifiers if the given [`InputEvent`](InputEvent.md) is not pressed (for proper release detection). See `ActionHasEvent()` if you don't want this behavior.

If `exactMatch` is `false`, it ignores additional input modifiers for [`InputEventKey`](InputEventKey.md) and [`InputEventMouseButton`](InputEventMouseButton.md) events, and the direction for [`InputEventJoypadMotion`](InputEventJoypadMotion.md) events.

### GetActionDescription

```csharp
public static string GetActionDescription(StringName action)
```

引擎名：`get_action_description`

Returns the human-readable description of the given action.

### GetActions

```csharp
public static Array<StringName> GetActions()
```

引擎名：`get_actions`

Returns an array of all actions in the `InputMap`.

### HasAction

```csharp
public static bool HasAction(StringName action)
```

引擎名：`has_action`

Returns `true` if the `InputMap` has a registered action with the given name.

### LoadFromProjectSettings

```csharp
public static void LoadFromProjectSettings()
```

引擎名：`load_from_project_settings`

Clears all [`InputEventAction`](InputEventAction.md) in the `InputMap` and load it anew from [`ProjectSettings`](ProjectSettings.md).

## 事件（信号）

### ProjectSettingsLoaded

```csharp
public event Action ProjectSettingsLoaded
```

引擎名：`project_settings_loaded`

Emitted when the [`ProjectSettings`](ProjectSettings.md) `InputMap` has been loaded.
