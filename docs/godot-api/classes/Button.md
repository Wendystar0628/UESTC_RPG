<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Button

命名空间 `Godot`；class；继承 [`BaseButton`](BaseButton.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Button` is the standard themed button. It can contain text and an icon, and it will display them according to the current [`Theme`](Theme.md).

**Example:** Create a button and connect a method that will be called when the button is pressed:

```csharp
public override void _Ready()
{
var button = new Button();
button.Text = "Click me";
button.Pressed += ButtonPressed;
AddChild(button);
}

private void ButtonPressed()
{
GD.Print("Hello world!");
}

```

See also [`BaseButton`](BaseButton.md) which contains common properties and methods associated with this node.

**Note:** Buttons support multitouch via touch input, allowing multiple buttons to be pressed at the same time. Otherwise, mouse input is used, limiting interaction to one button press at a time.

## 构造函数

### Button

```csharp
public Button()
```

## 属性

### Alignment

```csharp
public HorizontalAlignment Alignment { get; set; }
```

引擎名：`alignment`

Text alignment policy for the button's text.

### AutowrapMode

```csharp
public TextServer.AutowrapMode AutowrapMode { get; set; }
```

引擎名：`autowrap_mode`

If set to something other than [`TextServer.AutowrapMode.Off`](TextServer.md), the text gets wrapped inside the node's bounding rectangle.

### AutowrapTrimFlags

```csharp
public TextServer.LineBreakFlag AutowrapTrimFlags { get; set; }
```

引擎名：`autowrap_trim_flags`

Autowrap space trimming flags. See [`TextServer.LineBreakFlag.TrimStartEdgeSpaces`](TextServer.md) and [`TextServer.LineBreakFlag.TrimEndEdgeSpaces`](TextServer.md) for more info.

### ClipText

```csharp
public bool ClipText { get; set; }
```

引擎名：`clip_text`

If `true`, text that is too large to fit the button is clipped horizontally. If `false`, the button will always be wide enough to hold the text. The text is not vertically clipped, and the button's height is not affected by this property.

### ExpandIcon

```csharp
public bool ExpandIcon { get; set; }
```

引擎名：`expand_icon`

When enabled, the button's icon will expand/shrink to fit the button's size while keeping its aspect. See also `icon_max_width`.

### Flat

```csharp
public bool Flat { get; set; }
```

引擎名：`flat`

Flat buttons don't display decoration.

### Icon

```csharp
public Texture2D Icon { get; set; }
```

引擎名：`icon`

Button's icon, if text is present the icon will be placed before the text.

To edit margin and spacing of the icon, use `h_separation` theme property and `content_margin_*` properties of the used [`StyleBox`](StyleBox.md)es.

### IconAlignment

```csharp
public HorizontalAlignment IconAlignment { get; set; }
```

引擎名：`icon_alignment`

Specifies if the icon should be aligned horizontally to the left, right, or center of a button. Uses the same [`HorizontalAlignment`](HorizontalAlignment.md) constants as the text alignment. If centered horizontally and vertically, text will draw on top of the icon.

### Language

```csharp
public string Language { get; set; }
```

引擎名：`language`

Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

### Text

```csharp
public string Text { get; set; }
```

引擎名：`text`

The button's text that will be displayed inside the button's area.

### TextDirection

```csharp
public Control.TextDirection TextDirection { get; set; }
```

引擎名：`text_direction`

Base text writing direction.

### TextOverrunBehavior

```csharp
public TextServer.OverrunBehavior TextOverrunBehavior { get; set; }
```

引擎名：`text_overrun_behavior`

Sets the clipping behavior when the text exceeds the node's bounding rectangle.

### VerticalIconAlignment

```csharp
public VerticalAlignment VerticalIconAlignment { get; set; }
```

引擎名：`vertical_icon_alignment`

Specifies if the icon should be aligned vertically to the top, bottom, or center of a button. Uses the same [`VerticalAlignment`](VerticalAlignment.md) constants as the text alignment. If centered horizontally and vertically, text will draw on top of the icon.

## 方法

### GetAutowrapMode

```csharp
public TextServer.AutowrapMode GetAutowrapMode()
```

引擎名：`get_autowrap_mode`

### GetAutowrapTrimFlags

```csharp
public TextServer.LineBreakFlag GetAutowrapTrimFlags()
```

引擎名：`get_autowrap_trim_flags`

### GetButtonIcon

```csharp
public Texture2D GetButtonIcon()
```

引擎名：`get_button_icon`

### GetClipText

```csharp
public bool GetClipText()
```

引擎名：`get_clip_text`

### GetIconAlignment

```csharp
public HorizontalAlignment GetIconAlignment()
```

引擎名：`get_icon_alignment`

### GetLanguage

```csharp
public string GetLanguage()
```

引擎名：`get_language`

### GetText

```csharp
public string GetText()
```

引擎名：`get_text`

### GetTextAlignment

```csharp
public HorizontalAlignment GetTextAlignment()
```

引擎名：`get_text_alignment`

### GetTextDirection

```csharp
public Control.TextDirection GetTextDirection()
```

引擎名：`get_text_direction`

### GetTextOverrunBehavior

```csharp
public TextServer.OverrunBehavior GetTextOverrunBehavior()
```

引擎名：`get_text_overrun_behavior`

### GetVerticalIconAlignment

```csharp
public VerticalAlignment GetVerticalIconAlignment()
```

引擎名：`get_vertical_icon_alignment`

### IsExpandIcon

```csharp
public bool IsExpandIcon()
```

引擎名：`is_expand_icon`

### IsFlat

```csharp
public bool IsFlat()
```

引擎名：`is_flat`

### SetAutowrapMode

```csharp
public void SetAutowrapMode(TextServer.AutowrapMode autowrapMode)
```

引擎名：`set_autowrap_mode`

### SetAutowrapTrimFlags

```csharp
public void SetAutowrapTrimFlags(TextServer.LineBreakFlag autowrapTrimFlags)
```

引擎名：`set_autowrap_trim_flags`

### SetButtonIcon

```csharp
public void SetButtonIcon(Texture2D texture)
```

引擎名：`set_button_icon`

### SetClipText

```csharp
public void SetClipText(bool enabled)
```

引擎名：`set_clip_text`

### SetExpandIcon

```csharp
public void SetExpandIcon(bool enabled)
```

引擎名：`set_expand_icon`

### SetFlat

```csharp
public void SetFlat(bool enabled)
```

引擎名：`set_flat`

### SetIconAlignment

```csharp
public void SetIconAlignment(HorizontalAlignment iconAlignment)
```

引擎名：`set_icon_alignment`

### SetLanguage

```csharp
public void SetLanguage(string language)
```

引擎名：`set_language`

### SetText

```csharp
public void SetText(string text)
```

引擎名：`set_text`

### SetTextAlignment

```csharp
public void SetTextAlignment(HorizontalAlignment alignment)
```

引擎名：`set_text_alignment`

### SetTextDirection

```csharp
public void SetTextDirection(Control.TextDirection direction)
```

引擎名：`set_text_direction`

### SetTextOverrunBehavior

```csharp
public void SetTextOverrunBehavior(TextServer.OverrunBehavior overrunBehavior)
```

引擎名：`set_text_overrun_behavior`

### SetVerticalIconAlignment

```csharp
public void SetVerticalIconAlignment(VerticalAlignment verticalIconAlignment)
```

引擎名：`set_vertical_icon_alignment`
