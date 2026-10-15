<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ReferenceRect

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A rectangular box that displays only a colored border around its rectangle (see [`Control.GetRect`](Control.md)). It can be used to visualize the extents of a [`Control`](Control.md) node, for testing purposes.

## 构造函数

### ReferenceRect

```csharp
public ReferenceRect()
```

## 属性

### BorderColor

```csharp
public Color BorderColor { get; set; }
```

引擎名：`border_color`

Sets the border color of the `ReferenceRect`.

### BorderWidth

```csharp
public float BorderWidth { get; set; }
```

引擎名：`border_width`

Sets the border width of the `ReferenceRect`. The border grows both inwards and outwards with respect to the rectangle box.

### EditorOnly

```csharp
public bool EditorOnly { get; set; }
```

引擎名：`editor_only`

If `true`, the `ReferenceRect` will only be visible while in editor. Otherwise, `ReferenceRect` will be visible in the running project.

## 方法

### GetBorderColor

```csharp
public Color GetBorderColor()
```

引擎名：`get_border_color`

### GetBorderWidth

```csharp
public float GetBorderWidth()
```

引擎名：`get_border_width`

### GetEditorOnly

```csharp
public bool GetEditorOnly()
```

引擎名：`get_editor_only`

### SetBorderColor

```csharp
public void SetBorderColor(Color color)
```

引擎名：`set_border_color`

### SetBorderWidth

```csharp
public void SetBorderWidth(float width)
```

引擎名：`set_border_width`

### SetEditorOnly

```csharp
public void SetEditorOnly(bool enabled)
```

引擎名：`set_editor_only`
