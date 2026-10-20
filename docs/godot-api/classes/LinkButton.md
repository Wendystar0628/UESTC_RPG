<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LinkButton

命名空间 `Godot`；class；继承 [`BaseButton`](BaseButton.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A button that represents a link. This type of button is primarily used for interactions that cause a context change (like linking to a web page).

See also [`BaseButton`](BaseButton.md) which contains common properties and methods associated with this node.

## 构造函数

### LinkButton

```csharp
public LinkButton()
```

## 属性

### EllipsisChar

```csharp
public string EllipsisChar { get; set; }
```

引擎名：`ellipsis_char`

Ellipsis character used for text clipping.

### Language

```csharp
public string Language { get; set; }
```

引擎名：`language`

Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

### StructuredTextBidiOverride

```csharp
public TextServer.StructuredTextParser StructuredTextBidiOverride { get; set; }
```

引擎名：`structured_text_bidi_override`

Set BiDi algorithm override for the structured text.

### StructuredTextBidiOverrideOptions

```csharp
public Array StructuredTextBidiOverrideOptions { get; set; }
```

引擎名：`structured_text_bidi_override_options`

Set additional options for BiDi override.

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

### Underline

```csharp
public LinkButton.UnderlineMode Underline { get; set; }
```

引擎名：`underline`

The underline mode to use for the text.

### Uri

```csharp
public string Uri { get; set; }
```

引擎名：`uri`

The [URI](https://en.wikipedia.org/wiki/Uniform_Resource_Identifier) for this `LinkButton`. If set to a valid URI, pressing the button opens the URI using the operating system's default program for the protocol (via [`OS.ShellOpen`](OS.md)). HTTP and HTTPS URLs open the default web browser.

```csharp
Uri = "https://godotengine.org"; // Opens the URL in the default web browser.
Uri = "C:\SomeFolder"; // Opens the file explorer at the given path.
Uri = "C:\SomeImage.png"; // Opens the given image in the default viewing app.

```

## 方法

### GetEllipsisChar

```csharp
public string GetEllipsisChar()
```

引擎名：`get_ellipsis_char`

### GetLanguage

```csharp
public string GetLanguage()
```

引擎名：`get_language`

### GetStructuredTextBidiOverride

```csharp
public TextServer.StructuredTextParser GetStructuredTextBidiOverride()
```

引擎名：`get_structured_text_bidi_override`

### GetStructuredTextBidiOverrideOptions

```csharp
public Array GetStructuredTextBidiOverrideOptions()
```

引擎名：`get_structured_text_bidi_override_options`

### GetText

```csharp
public string GetText()
```

引擎名：`get_text`

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

### GetUnderlineMode

```csharp
public LinkButton.UnderlineMode GetUnderlineMode()
```

引擎名：`get_underline_mode`

### GetUri

```csharp
public string GetUri()
```

引擎名：`get_uri`

### SetEllipsisChar

```csharp
public void SetEllipsisChar(string char)
```

引擎名：`set_ellipsis_char`

### SetLanguage

```csharp
public void SetLanguage(string language)
```

引擎名：`set_language`

### SetStructuredTextBidiOverride

```csharp
public void SetStructuredTextBidiOverride(TextServer.StructuredTextParser parser)
```

引擎名：`set_structured_text_bidi_override`

### SetStructuredTextBidiOverrideOptions

```csharp
public void SetStructuredTextBidiOverrideOptions(Array args)
```

引擎名：`set_structured_text_bidi_override_options`

### SetText

```csharp
public void SetText(string text)
```

引擎名：`set_text`

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

### SetUnderlineMode

```csharp
public void SetUnderlineMode(LinkButton.UnderlineMode underlineMode)
```

引擎名：`set_underline_mode`

### SetUri

```csharp
public void SetUri(string uri)
```

引擎名：`set_uri`

## UnderlineMode

命名空间 `Godot`；enum

### 枚举值

- `Always = 0`：The LinkButton will always show an underline at the bottom of its text.
- `OnHover = 1`：The LinkButton will show an underline at the bottom of its text when the mouse cursor is over it.
- `Never = 2`：The LinkButton will never show an underline at the bottom of its text.
