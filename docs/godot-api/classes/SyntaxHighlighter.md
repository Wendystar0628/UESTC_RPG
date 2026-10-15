<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SyntaxHighlighter

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for syntax highlighters. Provides syntax highlighting data to a [`TextEdit`](TextEdit.md). The associated [`TextEdit`](TextEdit.md) will call into the `SyntaxHighlighter` on an as-needed basis.

**Note:** A `SyntaxHighlighter` instance should not be used across multiple [`TextEdit`](TextEdit.md) nodes.

## 构造函数

### SyntaxHighlighter

```csharp
public SyntaxHighlighter()
```

## 方法

### ClearHighlightingCache

```csharp
public void ClearHighlightingCache()
```

引擎名：`clear_highlighting_cache`

Clears all cached syntax highlighting data.

Then calls overridable method `_ClearHighlightingCache()`.

### GetLineSyntaxHighlighting

```csharp
public Dictionary GetLineSyntaxHighlighting(int line)
```

引擎名：`get_line_syntax_highlighting`

Returns the syntax highlighting data for the line at index `line`. If the line is not cached, calls `_GetLineSyntaxHighlighting()` first to calculate the data.

Each entry is a column number containing a nested [`Collections.Dictionary`](Collections.Dictionary.md). The column number denotes the start of a region, the region will end if another region is found, or at the end of the line. The nested [`Collections.Dictionary`](Collections.Dictionary.md) contains the data for that region. Currently only the key `"color"` is supported.

**Example:** Possible return value. This means columns `0` to `4` should be red, and columns `5` to the end of the line should be green:

```csharp
{
0: {
"color": Color(1, 0, 0)
},
5: {
"color": Color(0, 1, 0)
}
}

```

### GetTextEdit

```csharp
public TextEdit GetTextEdit()
```

引擎名：`get_text_edit`

Returns the associated [`TextEdit`](TextEdit.md) node.

### UpdateCache

```csharp
public void UpdateCache()
```

引擎名：`update_cache`

Clears then updates the `SyntaxHighlighter` caches. Override `_UpdateCache()` for a callback.

**Note:** This is called automatically when the associated [`TextEdit`](TextEdit.md) node, updates its own cache.

### _ClearHighlightingCache

```csharp
public virtual void _ClearHighlightingCache()
```

引擎名：`_clear_highlighting_cache`

Virtual method which can be overridden to clear any local caches.

### _GetLineSyntaxHighlighting

```csharp
public virtual Dictionary _GetLineSyntaxHighlighting(int line)
```

引擎名：`_get_line_syntax_highlighting`

Virtual method which can be overridden to return syntax highlighting data.

See `GetLineSyntaxHighlighting()` for more details.

### _UpdateCache

```csharp
public virtual void _UpdateCache()
```

引擎名：`_update_cache`

Virtual method which can be overridden to update any local caches.
