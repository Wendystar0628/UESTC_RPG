<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Slider

命名空间 `Godot`；class；继承 [`Range`](Range.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Abstract base class for sliders, used to adjust a value by moving a grabber along a horizontal or vertical axis. Sliders are [`Range`](Range.md)-based controls.

## 属性

### Editable

```csharp
public bool Editable { get; set; }
```

引擎名：`editable`

If `true`, the slider can be interacted with. If `false`, the value can be changed only by code.

### Scrollable

```csharp
public bool Scrollable { get; set; }
```

引擎名：`scrollable`

If `true`, the value can be changed using the mouse wheel.

### TickCount

```csharp
public int TickCount { get; set; }
```

引擎名：`tick_count`

Number of ticks displayed on the slider, including border ticks. Ticks are uniformly-distributed value markers.

### TicksOnBorders

```csharp
public bool TicksOnBorders { get; set; }
```

引擎名：`ticks_on_borders`

If `true`, the slider will display ticks for minimum and maximum values.

### TicksPosition

```csharp
public Slider.TickPosition TicksPosition { get; set; }
```

引擎名：`ticks_position`

Sets the position of the ticks. See [`Slider.TickPosition`](Slider.md) for details.

## 方法

### GetTicks

```csharp
public int GetTicks()
```

引擎名：`get_ticks`

### GetTicksOnBorders

```csharp
public bool GetTicksOnBorders()
```

引擎名：`get_ticks_on_borders`

### GetTicksPosition

```csharp
public Slider.TickPosition GetTicksPosition()
```

引擎名：`get_ticks_position`

### IsEditable

```csharp
public bool IsEditable()
```

引擎名：`is_editable`

### IsScrollable

```csharp
public bool IsScrollable()
```

引擎名：`is_scrollable`

### SetEditable

```csharp
public void SetEditable(bool editable)
```

引擎名：`set_editable`

### SetScrollable

```csharp
public void SetScrollable(bool scrollable)
```

引擎名：`set_scrollable`

### SetTicks

```csharp
public void SetTicks(int count)
```

引擎名：`set_ticks`

### SetTicksOnBorders

```csharp
public void SetTicksOnBorders(bool ticksOnBorder)
```

引擎名：`set_ticks_on_borders`

### SetTicksPosition

```csharp
public void SetTicksPosition(Slider.TickPosition ticksOnBorder)
```

引擎名：`set_ticks_position`

## 事件（信号）

### DragEnded

```csharp
public event Slider.DragEndedEventHandler DragEnded
```

引擎名：`drag_ended`

Emitted when the grabber stops being dragged. If `valueChanged` is `true`, [`Range.Value`](Range.md) is different from the value when the dragging was started.

### DragStarted

```csharp
public event Action DragStarted
```

引擎名：`drag_started`

Emitted when the grabber starts being dragged. This is emitted before the corresponding [`Range.ValueChanged`](Range.md) signal.

## DragEndedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Slider.DragEnded`](Slider.md) event of a [`Slider`](Slider.md) class.

### 签名

#### Invoke

```csharp
delegate void DragEndedEventHandler(bool valueChanged)
```

## TickPosition

命名空间 `Godot`；enum

### 枚举值

- `BottomRight = 0`：Places the ticks at the bottom of the [`HSlider`](HSlider.md), or right of the [`VSlider`](VSlider.md).
- `TopLeft = 1`：Places the ticks at the top of the [`HSlider`](HSlider.md), or left of the [`VSlider`](VSlider.md).
- `Both = 2`：Places the ticks at the both sides of the slider.
- `Center = 3`：Places the ticks at the center of the slider.
