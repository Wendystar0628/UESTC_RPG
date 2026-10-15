<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ScrollBar

命名空间 `Godot`；class；继承 [`Range`](Range.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Abstract base class for scrollbars, typically used to navigate through content that extends beyond the visible area of a control. Scrollbars are [`Range`](Range.md)-based controls.

## 属性

### CustomStep

```csharp
public float CustomStep { get; set; }
```

引擎名：`custom_step`

Overrides the step used when clicking increment and decrement buttons or when using arrow keys when the `ScrollBar` is focused.

## 方法

### GetCustomStep

```csharp
public float GetCustomStep()
```

引擎名：`get_custom_step`

### SetCustomStep

```csharp
public void SetCustomStep(float step)
```

引擎名：`set_custom_step`

## 事件（信号）

### Scrolling

```csharp
public event Action Scrolling
```

引擎名：`scrolling`

Emitted when the scrollbar is being scrolled.
