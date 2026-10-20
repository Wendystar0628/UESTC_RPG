<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ThemeDBInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

This singleton provides access to static information about [`Theme`](Theme.md) resources used by the engine and by your projects. You can fetch the default engine theme, as well as your project configured theme.

[`ThemeDB`](ThemeDB.md) also contains fallback values for theme properties.

## 属性

### FallbackBaseScale

```csharp
public float FallbackBaseScale { get; set; }
```

The fallback base scale factor of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

See also [`Theme.DefaultBaseScale`](Theme.md).

### FallbackFont

```csharp
public Font FallbackFont { get; set; }
```

The fallback font of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

See also [`Theme.DefaultFont`](Theme.md).

### FallbackFontSize

```csharp
public int FallbackFontSize { get; set; }
```

The fallback font size of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

See also [`Theme.DefaultFontSize`](Theme.md).

### FallbackIcon

```csharp
public Texture2D FallbackIcon { get; set; }
```

The fallback icon of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

### FallbackStylebox

```csharp
public StyleBox FallbackStylebox { get; set; }
```

The fallback stylebox of every [`Control`](Control.md) node and [`Theme`](Theme.md) resource. Used when no other value is available to the control.

## 方法

### GetDefaultTheme

```csharp
public Theme GetDefaultTheme()
```

Returns a reference to the default engine [`Theme`](Theme.md). This theme resource is responsible for the out-of-the-box look of [`Control`](Control.md) nodes and cannot be overridden.

### GetFallbackBaseScale

```csharp
public float GetFallbackBaseScale()
```

### GetFallbackFont

```csharp
public Font GetFallbackFont()
```

### GetFallbackFontSize

```csharp
public int GetFallbackFontSize()
```

### GetFallbackIcon

```csharp
public Texture2D GetFallbackIcon()
```

### GetFallbackStylebox

```csharp
public StyleBox GetFallbackStylebox()
```

### GetProjectTheme

```csharp
public Theme GetProjectTheme()
```

Returns a reference to the custom project [`Theme`](Theme.md). This theme resources allows to override the default engine theme for every control node in the project.

To set the project theme, see `ProjectSettings.gui/theme/custom`.

### SetFallbackBaseScale

```csharp
public void SetFallbackBaseScale(float baseScale)
```

### SetFallbackFont

```csharp
public void SetFallbackFont(Font font)
```

### SetFallbackFontSize

```csharp
public void SetFallbackFontSize(int fontSize)
```

### SetFallbackIcon

```csharp
public void SetFallbackIcon(Texture2D icon)
```

### SetFallbackStylebox

```csharp
public void SetFallbackStylebox(StyleBox stylebox)
```

## 事件（信号）

### FallbackChanged

```csharp
public event Action FallbackChanged
```

Emitted when one of the fallback values had been changed. Use it to refresh the look of controls that may rely on the fallback theme items.
