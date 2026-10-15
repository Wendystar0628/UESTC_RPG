<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ProgressBar

命名空间 `Godot`；class；继承 [`Range`](Range.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A control used for visual representation of a percentage. Shows the fill percentage in the center. Can also be used to show indeterminate progress. For more fill modes, use [`TextureProgressBar`](TextureProgressBar.md) instead.

## 构造函数

### ProgressBar

```csharp
public ProgressBar()
```

## 属性

### EditorPreviewIndeterminate

```csharp
public bool EditorPreviewIndeterminate { get; set; }
```

引擎名：`editor_preview_indeterminate`

If `false`, the `Indeterminate` animation will be paused in the editor.

### FillMode

```csharp
public int FillMode { get; set; }
```

引擎名：`fill_mode`

The fill direction. See [`ProgressBar.FillModeEnum`](ProgressBar.md) for possible values.

### Indeterminate

```csharp
public bool Indeterminate { get; set; }
```

引擎名：`indeterminate`

When set to `true`, the progress bar indicates that something is happening with an animation, but does not show the fill percentage or value.

### ShowPercentage

```csharp
public bool ShowPercentage { get; set; }
```

引擎名：`show_percentage`

If `true`, the fill percentage is displayed on the bar.

## 方法

### GetFillMode

```csharp
public int GetFillMode()
```

引擎名：`get_fill_mode`

### IsEditorPreviewIndeterminateEnabled

```csharp
public bool IsEditorPreviewIndeterminateEnabled()
```

引擎名：`is_editor_preview_indeterminate_enabled`

### IsIndeterminate

```csharp
public bool IsIndeterminate()
```

引擎名：`is_indeterminate`

### IsPercentageShown

```csharp
public bool IsPercentageShown()
```

引擎名：`is_percentage_shown`

### SetEditorPreviewIndeterminate

```csharp
public void SetEditorPreviewIndeterminate(bool previewIndeterminate)
```

引擎名：`set_editor_preview_indeterminate`

### SetFillMode

```csharp
public void SetFillMode(int mode)
```

引擎名：`set_fill_mode`

### SetIndeterminate

```csharp
public void SetIndeterminate(bool indeterminate)
```

引擎名：`set_indeterminate`

### SetShowPercentage

```csharp
public void SetShowPercentage(bool visible)
```

引擎名：`set_show_percentage`

## FillModeEnum

命名空间 `Godot`；enum

### 枚举值

- `BeginToEnd = 0`：The progress bar fills from begin to end horizontally, according to the language direction.
- `EndToBegin = 1`：The progress bar fills from end to begin horizontally, according to the language direction.
- `TopToBottom = 2`：The progress fills from top to bottom.
- `BottomToTop = 3`：The progress fills from bottom to top.
