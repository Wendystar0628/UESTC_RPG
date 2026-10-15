<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LabelSettings

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`LabelSettings` is a resource that provides common settings to customize the text in a [`Label`](Label.md). It will take priority over the properties defined in [`Control.Theme`](Control.md). The resource can be shared between multiple labels and changed on the fly, so it's convenient and flexible way to setup text style.

## 构造函数

### LabelSettings

```csharp
public LabelSettings()
```

## 属性

### Font

```csharp
public Font Font { get; set; }
```

引擎名：`font`

[`Font`](Font.md) used for the text.

### FontColor

```csharp
public Color FontColor { get; set; }
```

引擎名：`font_color`

Color of the text.

### FontSize

```csharp
public int FontSize { get; set; }
```

引擎名：`font_size`

Size of the text.

### LineSpacing

```csharp
public float LineSpacing { get; set; }
```

引擎名：`line_spacing`

Additional vertical spacing between lines (in pixels), spacing is added to line descent. This value can be negative.

### OutlineColor

```csharp
public Color OutlineColor { get; set; }
```

引擎名：`outline_color`

The color of the outline.

### OutlineSize

```csharp
public int OutlineSize { get; set; }
```

引擎名：`outline_size`

Text outline size.

### ParagraphSpacing

```csharp
public float ParagraphSpacing { get; set; }
```

引擎名：`paragraph_spacing`

Vertical space between paragraphs. Added on top of `LineSpacing`.

### ShadowColor

```csharp
public Color ShadowColor { get; set; }
```

引擎名：`shadow_color`

Color of the shadow effect. If alpha is `0`, no shadow will be drawn.

### ShadowOffset

```csharp
public Vector2 ShadowOffset { get; set; }
```

引擎名：`shadow_offset`

Offset of the shadow effect, in pixels.

### ShadowSize

```csharp
public int ShadowSize { get; set; }
```

引擎名：`shadow_size`

Size of the shadow effect.

### StackedOutlineCount

```csharp
public int StackedOutlineCount { get; set; }
```

引擎名：`stacked_outline_count`

The number of stacked outlines.

### StackedShadowCount

```csharp
public int StackedShadowCount { get; set; }
```

引擎名：`stacked_shadow_count`

The number of stacked shadows.

## 方法

### AddStackedOutline

```csharp
public void AddStackedOutline(int index = -1)
```

引擎名：`add_stacked_outline`

Adds a new stacked outline to the label at the given `index`. If `index` is `-1`, the new stacked outline will be added at the end of the list.

### AddStackedShadow

```csharp
public void AddStackedShadow(int index = -1)
```

引擎名：`add_stacked_shadow`

Adds a new stacked shadow to the label at the given `index`. If `index` is `-1`, the new stacked shadow will be added at the end of the list.

### GetFont

```csharp
public Font GetFont()
```

引擎名：`get_font`

### GetFontColor

```csharp
public Color GetFontColor()
```

引擎名：`get_font_color`

### GetFontSize

```csharp
public int GetFontSize()
```

引擎名：`get_font_size`

### GetLineSpacing

```csharp
public float GetLineSpacing()
```

引擎名：`get_line_spacing`

### GetOutlineColor

```csharp
public Color GetOutlineColor()
```

引擎名：`get_outline_color`

### GetOutlineSize

```csharp
public int GetOutlineSize()
```

引擎名：`get_outline_size`

### GetParagraphSpacing

```csharp
public float GetParagraphSpacing()
```

引擎名：`get_paragraph_spacing`

### GetShadowColor

```csharp
public Color GetShadowColor()
```

引擎名：`get_shadow_color`

### GetShadowOffset

```csharp
public Vector2 GetShadowOffset()
```

引擎名：`get_shadow_offset`

### GetShadowSize

```csharp
public int GetShadowSize()
```

引擎名：`get_shadow_size`

### GetStackedOutlineColor

```csharp
public Color GetStackedOutlineColor(int index)
```

引擎名：`get_stacked_outline_color`

Returns the color of the stacked outline at `index`.

### GetStackedOutlineCount

```csharp
public int GetStackedOutlineCount()
```

引擎名：`get_stacked_outline_count`

### GetStackedOutlineSize

```csharp
public int GetStackedOutlineSize(int index)
```

引擎名：`get_stacked_outline_size`

Returns the size of the stacked outline at `index`.

### GetStackedShadowColor

```csharp
public Color GetStackedShadowColor(int index)
```

引擎名：`get_stacked_shadow_color`

Returns the color of the stacked shadow at `index`.

### GetStackedShadowCount

```csharp
public int GetStackedShadowCount()
```

引擎名：`get_stacked_shadow_count`

### GetStackedShadowOffset

```csharp
public Vector2 GetStackedShadowOffset(int index)
```

引擎名：`get_stacked_shadow_offset`

Returns the offset of the stacked shadow at `index`.

### GetStackedShadowOutlineSize

```csharp
public int GetStackedShadowOutlineSize(int index)
```

引擎名：`get_stacked_shadow_outline_size`

Returns the outline size of the stacked shadow at `index`.

### MoveStackedOutline

```csharp
public void MoveStackedOutline(int fromIndex, int toPosition)
```

引擎名：`move_stacked_outline`

Moves the stacked outline at index `fromIndex` to the given position `toPosition` in the array.

### MoveStackedShadow

```csharp
public void MoveStackedShadow(int fromIndex, int toPosition)
```

引擎名：`move_stacked_shadow`

Moves the stacked shadow at index `fromIndex` to the given position `toPosition` in the array.

### RemoveStackedOutline

```csharp
public void RemoveStackedOutline(int index)
```

引擎名：`remove_stacked_outline`

Removes the stacked outline at index `index`.

### RemoveStackedShadow

```csharp
public void RemoveStackedShadow(int index)
```

引擎名：`remove_stacked_shadow`

Removes the stacked shadow at index `index`.

### SetFont

```csharp
public void SetFont(Font font)
```

引擎名：`set_font`

### SetFontColor

```csharp
public void SetFontColor(Color color)
```

引擎名：`set_font_color`

### SetFontSize

```csharp
public void SetFontSize(int size)
```

引擎名：`set_font_size`

### SetLineSpacing

```csharp
public void SetLineSpacing(float spacing)
```

引擎名：`set_line_spacing`

### SetOutlineColor

```csharp
public void SetOutlineColor(Color color)
```

引擎名：`set_outline_color`

### SetOutlineSize

```csharp
public void SetOutlineSize(int size)
```

引擎名：`set_outline_size`

### SetParagraphSpacing

```csharp
public void SetParagraphSpacing(float spacing)
```

引擎名：`set_paragraph_spacing`

### SetShadowColor

```csharp
public void SetShadowColor(Color color)
```

引擎名：`set_shadow_color`

### SetShadowOffset

```csharp
public void SetShadowOffset(Vector2 offset)
```

引擎名：`set_shadow_offset`

### SetShadowSize

```csharp
public void SetShadowSize(int size)
```

引擎名：`set_shadow_size`

### SetStackedOutlineColor

```csharp
public void SetStackedOutlineColor(int index, Color color)
```

引擎名：`set_stacked_outline_color`

Sets the color of the stacked outline identified by the given `index` to `color`.

### SetStackedOutlineCount

```csharp
public void SetStackedOutlineCount(int count)
```

引擎名：`set_stacked_outline_count`

### SetStackedOutlineSize

```csharp
public void SetStackedOutlineSize(int index, int size)
```

引擎名：`set_stacked_outline_size`

Sets the size of the stacked outline identified by the given `index` to `size`.

### SetStackedShadowColor

```csharp
public void SetStackedShadowColor(int index, Color color)
```

引擎名：`set_stacked_shadow_color`

Sets the color of the stacked shadow identified by the given `index` to `color`.

### SetStackedShadowCount

```csharp
public void SetStackedShadowCount(int count)
```

引擎名：`set_stacked_shadow_count`

### SetStackedShadowOffset

```csharp
public void SetStackedShadowOffset(int index, Vector2 offset)
```

引擎名：`set_stacked_shadow_offset`

Sets the offset of the stacked shadow identified by the given `index` to `offset`.

### SetStackedShadowOutlineSize

```csharp
public void SetStackedShadowOutlineSize(int index, int size)
```

引擎名：`set_stacked_shadow_outline_size`

Sets the outline size of the stacked shadow identified by the given `index` to `size`.
