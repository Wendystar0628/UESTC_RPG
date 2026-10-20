<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventAction

命名空间 `Godot`；class；继承 [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Contains a generic action which can be targeted from several types of inputs. Actions and their events can be set in the **Input Map** tab in **Project > Project Settings**, or with the [`InputMap`](InputMap.md) class.

**Note:** Unlike the other [`InputEvent`](InputEvent.md) subclasses which map to unique physical events, this virtual one is not emitted by the engine. This class is useful to emit actions manually with [`Input.ParseInputEvent`](Input.md), which are then received in [`Node._Input()`](Node.md). To check if a physical event matches an action from the Input Map, use [`InputEvent.IsAction`](InputEvent.md) and [`InputEvent.IsActionPressed`](InputEvent.md).

## 构造函数

### InputEventAction

```csharp
public InputEventAction()
```

## 属性

### Action

```csharp
public StringName Action { get; set; }
```

引擎名：`action`

The action's name. This is usually the name of an existing action in the [`InputMap`](InputMap.md) which you want this custom event to match.

### EventIndex

```csharp
public int EventIndex { get; set; }
```

引擎名：`event_index`

The real event index in action this event corresponds to (from events defined for this action in the [`InputMap`](InputMap.md)). If `-1`, a unique ID will be used and actions pressed with this ID will need to be released with another `InputEventAction`.

### Pressed

```csharp
public bool Pressed { get; set; }
```

引擎名：`pressed`

If `true`, the action's state is pressed. If `false`, the action's state is released.

### Strength

```csharp
public float Strength { get; set; }
```

引擎名：`strength`

The action's strength between 0 and 1. This value is considered as equal to 0 if pressed is `false`. The event strength allows faking analog joypad motion events, by specifying how strongly the joypad axis is bent or pressed.

## 方法

### GetAction

```csharp
public StringName GetAction()
```

引擎名：`get_action`

### GetEventIndex

```csharp
public int GetEventIndex()
```

引擎名：`get_event_index`

### GetStrength

```csharp
public float GetStrength()
```

引擎名：`get_strength`

### SetAction

```csharp
public void SetAction(StringName action)
```

引擎名：`set_action`

### SetEventIndex

```csharp
public void SetEventIndex(int index)
```

引擎名：`set_event_index`

### SetPressed

```csharp
public void SetPressed(bool pressed)
```

引擎名：`set_pressed`

### SetStrength

```csharp
public void SetStrength(float strength)
```

引擎名：`set_strength`
