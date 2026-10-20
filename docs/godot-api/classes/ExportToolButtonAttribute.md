<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ExportToolButtonAttribute

命名空间 `Godot`；class

Exports the annotated [`Callable`](Callable.md) as a clickable button.

## 构造函数

### ExportToolButtonAttribute

```csharp
public ExportToolButtonAttribute(string text)
```

Exports the annotated [`Callable`](Callable.md) as a clickable button.

参数：
- `text`：The label of the button.

## 属性

### Icon

```csharp
public string Icon { get; set; }
```

If defined, used to fetch an icon for the button via [`Control.GetThemeIcon`](Control.md),
from the

```csharp
EditorIcons
```

theme type.

### Text

```csharp
public string Text { get; }
```

The label of the button.
