<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorSyntaxHighlighter

命名空间 `Godot`；class；继承 [`SyntaxHighlighter`](SyntaxHighlighter.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Base class that all [`SyntaxHighlighter`](SyntaxHighlighter.md)s used by the [`ScriptEditor`](ScriptEditor.md) extend from.

Add a syntax highlighter to an individual script by calling [`ScriptEditorBase.AddSyntaxHighlighter`](ScriptEditorBase.md). To apply to all scripts on open, call [`ScriptEditor.RegisterSyntaxHighlighter`](ScriptEditor.md).

## 构造函数

### EditorSyntaxHighlighter

```csharp
public EditorSyntaxHighlighter()
```

## 方法

### _Create

```csharp
public virtual EditorSyntaxHighlighter _Create()
```

引擎名：`_create`

Virtual method which creates a new instance of the syntax highlighter.

### _GetName

```csharp
public virtual string _GetName()
```

引擎名：`_get_name`

Virtual method which can be overridden to return the syntax highlighter name.

### _GetSupportedLanguages

```csharp
public virtual string[] _GetSupportedLanguages()
```

引擎名：`_get_supported_languages`

Virtual method which can be overridden to return the supported language names.
