<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventFromWindow

命名空间 `Godot`；class；继承 [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

InputEventFromWindow represents events specifically received by windows. This includes mouse events, keyboard events in focused windows or touch screen actions.

## 属性

### WindowId

```csharp
public long WindowId { get; set; }
```

引擎名：`window_id`

The ID of a [`Window`](Window.md) that received this event.

## 方法

### GetWindowId

```csharp
public long GetWindowId()
```

引擎名：`get_window_id`

### SetWindowId

```csharp
public void SetWindowId(long id)
```

引擎名：`set_window_id`
