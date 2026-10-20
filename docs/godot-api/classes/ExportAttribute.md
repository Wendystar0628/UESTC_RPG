<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ExportAttribute

命名空间 `Godot`；class

Exports the annotated member as a property of the Godot Object.

## 构造函数

### ExportAttribute

```csharp
public ExportAttribute(PropertyHint hint = PropertyHint.None, string hintString = "")
```

Constructs a new ExportAttribute Instance.

参数：
- `hint`：The hint for the exported property.
- `hintString`：A string that may contain additional metadata for the hint.

## 属性

### Hint

```csharp
public PropertyHint Hint { get; }
```

Optional hint that determines how the property should be handled by the editor.

### HintString

```csharp
public string HintString { get; }
```

Optional string that can contain additional metadata for the `Hint`.
