<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ThemeDB

命名空间 `Godot`；static class

This singleton provides access to static information about [`Theme`](Theme.md) resources used by the engine and by your projects. You can fetch the default engine theme, as well as your project configured theme.

`ThemeDB` also contains fallback values for theme properties.

## 属性

### FallbackBaseScale

```csharp
public static float FallbackBaseScale { get; set; }
```

引擎名：`fallback_base_scale`

The fallback base scale factor of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

See also [`Theme.DefaultBaseScale`](Theme.md).

### FallbackFont

```csharp
public static Font FallbackFont { get; set; }
```

引擎名：`fallback_font`

The fallback font of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

See also [`Theme.DefaultFont`](Theme.md).

### FallbackFontSize

```csharp
public static int FallbackFontSize { get; set; }
```

引擎名：`fallback_font_size`

The fallback font size of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

See also [`Theme.DefaultFontSize`](Theme.md).

### FallbackIcon

```csharp
public static Texture2D FallbackIcon { get; set; }
```

引擎名：`fallback_icon`

The fallback icon of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

### FallbackStylebox

```csharp
public static StyleBox FallbackStylebox { get; set; }
```

引擎名：`fallback_stylebox`

The fallback stylebox of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

### Singleton

```csharp
public static ThemeDBInstance Singleton { get; }
```

## 方法

### GetDefaultTheme

```csharp
public static Theme GetDefaultTheme()
```

引擎名：`get_default_theme`

Returns a reference to the default engine [`Theme`](Theme.md). This theme resource is responsible for the out-of-the-box look of [`Control`](Control.md) nodes and cannot be overridden.

### GetFallbackBaseScale

```csharp
public static float GetFallbackBaseScale()
```

引擎名：`get_fallback_base_scale`

### GetFallbackFont

```csharp
public static Font GetFallbackFont()
```

引擎名：`get_fallback_font`

### GetFallbackFontSize

```csharp
public static int GetFallbackFontSize()
```

引擎名：`get_fallback_font_size`

### GetFallbackIcon

```csharp
public static Texture2D GetFallbackIcon()
```

引擎名：`get_fallback_icon`

### GetFallbackStylebox

```csharp
public static StyleBox GetFallbackStylebox()
```

引擎名：`get_fallback_stylebox`

### GetProjectTheme

```csharp
public static Theme GetProjectTheme()
```

引擎名：`get_project_theme`

Returns a reference to the custom project [`Theme`](Theme.md). This theme resources allows to override the default engine theme for every control node in the project.

To set the project theme, see `ProjectSettings.gui/theme/custom`.

### SetFallbackBaseScale

```csharp
public static void SetFallbackBaseScale(float baseScale)
```

引擎名：`set_fallback_base_scale`

### SetFallbackFont

```csharp
public static void SetFallbackFont(Font font)
```

引擎名：`set_fallback_font`

### SetFallbackFontSize

```csharp
public static void SetFallbackFontSize(int fontSize)
```

引擎名：`set_fallback_font_size`

### SetFallbackIcon

```csharp
public static void SetFallbackIcon(Texture2D icon)
```

引擎名：`set_fallback_icon`

### SetFallbackStylebox

```csharp
public static void SetFallbackStylebox(StyleBox stylebox)
```

引擎名：`set_fallback_stylebox`

## 事件（信号）

### FallbackChanged

```csharp
public event Action FallbackChanged
```

引擎名：`fallback_changed`

Emitted when one of the fallback values had been changed. Use it to refresh the look of controls that may rely on the fallback theme items.
