<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GDScriptTextDocument

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Provides language server functionality related to documents.

## 构造函数

### GDScriptTextDocument

```csharp
public GDScriptTextDocument()
```

## 方法

### CodeLens

```csharp
public Array CodeLens(Dictionary params)
```

### ColorPresentation

```csharp
public Array ColorPresentation(Dictionary params)
```

### Completion

```csharp
public Array Completion(Dictionary params)
```

引擎名：`completion`

### Declaration

```csharp
public Variant Declaration(Dictionary params)
```

引擎名：`declaration`

### Definition

```csharp
public Array Definition(Dictionary params)
```

引擎名：`definition`

### DidChange

```csharp
public void DidChange(Variant params)
```

### DidClose

```csharp
public void DidClose(Variant params)
```

### DidOpen

```csharp
public void DidOpen(Variant params)
```

### DidSave

```csharp
public void DidSave(Variant params)
```

### DocumentLink

```csharp
public Array DocumentLink(Dictionary params)
```

### DocumentSymbol

```csharp
public Array DocumentSymbol(Dictionary params)
```

### FoldingRange

```csharp
public Array FoldingRange(Dictionary params)
```

### Hover

```csharp
public Variant Hover(Dictionary params)
```

引擎名：`hover`

### NativeSymbol

```csharp
public Variant NativeSymbol(Dictionary params)
```

### PrepareRename

```csharp
public Variant PrepareRename(Dictionary params)
```

### References

```csharp
public Array References(Dictionary params)
```

引擎名：`references`

### Rename

```csharp
public Dictionary Rename(Dictionary params)
```

引擎名：`rename`

### Resolve

```csharp
public Dictionary Resolve(Dictionary params)
```

引擎名：`resolve`

### ShowNativeSymbolInEditor

```csharp
public void ShowNativeSymbolInEditor(string symbolId)
```

引擎名：`show_native_symbol_in_editor`

### SignatureHelp

```csharp
public Variant SignatureHelp(Dictionary params)
```

### WillSaveWaitUntil

```csharp
public void WillSaveWaitUntil(Variant params)
```
