<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StyleBoxLine

命名空间 `Godot`；class；继承 [`StyleBox`](StyleBox.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A [`StyleBox`](StyleBox.md) that displays a single line of a given color and thickness. The line can be either horizontal or vertical. Useful for separators.

## 构造函数

### StyleBoxLine

```csharp
public StyleBoxLine()
```

## 属性

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

The line's color.

### GrowBegin

```csharp
public float GrowBegin { get; set; }
```

引擎名：`grow_begin`

The number of pixels the line will extend before the `StyleBoxLine`'s bounds. If set to a negative value, the line will begin inside the `StyleBoxLine`'s bounds.

### GrowEnd

```csharp
public float GrowEnd { get; set; }
```

引擎名：`grow_end`

The number of pixels the line will extend past the `StyleBoxLine`'s bounds. If set to a negative value, the line will end inside the `StyleBoxLine`'s bounds.

### Thickness

```csharp
public int Thickness { get; set; }
```

引擎名：`thickness`

The line's thickness in pixels.

### Vertical

```csharp
public bool Vertical { get; set; }
```

引擎名：`vertical`

If `true`, the line will be vertical. If `false`, the line will be horizontal.

## 方法

### GetColor

```csharp
public Color GetColor()
```

引擎名：`get_color`

### GetGrowBegin

```csharp
public float GetGrowBegin()
```

引擎名：`get_grow_begin`

### GetGrowEnd

```csharp
public float GetGrowEnd()
```

引擎名：`get_grow_end`

### GetThickness

```csharp
public int GetThickness()
```

引擎名：`get_thickness`

### IsVertical

```csharp
public bool IsVertical()
```

引擎名：`is_vertical`

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`

### SetGrowBegin

```csharp
public void SetGrowBegin(float offset)
```

引擎名：`set_grow_begin`

### SetGrowEnd

```csharp
public void SetGrowEnd(float offset)
```

引擎名：`set_grow_end`

### SetThickness

```csharp
public void SetThickness(int thickness)
```

引擎名：`set_thickness`

### SetVertical

```csharp
public void SetVertical(bool vertical)
```

引擎名：`set_vertical`
