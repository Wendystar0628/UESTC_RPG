<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ColorPalette

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The `ColorPalette` resource is designed to store and manage a collection of colors. This resource is useful in scenarios where a predefined set of colors is required, such as for creating themes, designing user interfaces, or managing game assets. The built-in [`ColorPicker`](ColorPicker.md) control can also make use of `ColorPalette` without additional code.

## 构造函数

### ColorPalette

```csharp
public ColorPalette()
```

## 属性

### Colors

```csharp
public Color[] Colors { get; set; }
```

引擎名：`colors`

A [`Color`](Color.md)[] containing the colors in the palette.

## 方法

### GetColors

```csharp
public Color[] GetColors()
```

引擎名：`get_colors`

### SetColors

```csharp
public void SetColors(Color[] colors)
```

引擎名：`set_colors`

### SetColors

```csharp
public void SetColors(ReadOnlySpan<Color> colors)
```

引擎名：`set_colors`
