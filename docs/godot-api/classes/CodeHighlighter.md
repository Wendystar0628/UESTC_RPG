<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CodeHighlighter

命名空间 `Godot`；class；继承 [`SyntaxHighlighter`](SyntaxHighlighter.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By adjusting various properties of this resource, you can change the colors of strings, comments, numbers, and other text patterns inside a [`TextEdit`](TextEdit.md) control.

## 构造函数

### CodeHighlighter

```csharp
public CodeHighlighter()
```

## 属性

### ColorRegions

```csharp
public Dictionary ColorRegions { get; set; }
```

引擎名：`color_regions`

Sets the color regions. All existing regions will be removed. The [`Collections.Dictionary`](Collections.Dictionary.md) key is the region start and end key, separated by a space. The value is the region color.

### FunctionColor

```csharp
public Color FunctionColor { get; set; }
```

引擎名：`function_color`

Sets color for functions. A function is a non-keyword string followed by a '('.

### KeywordColors

```csharp
public Dictionary KeywordColors { get; set; }
```

引擎名：`keyword_colors`

Sets the keyword colors. All existing keywords will be removed. The [`Collections.Dictionary`](Collections.Dictionary.md) key is the keyword. The value is the keyword color.

### MemberKeywordColors

```csharp
public Dictionary MemberKeywordColors { get; set; }
```

引擎名：`member_keyword_colors`

Sets the member keyword colors. All existing member keyword will be removed. The [`Collections.Dictionary`](Collections.Dictionary.md) key is the member keyword. The value is the member keyword color.

### MemberVariableColor

```csharp
public Color MemberVariableColor { get; set; }
```

引擎名：`member_variable_color`

Sets color for member variables. A member variable is non-keyword, non-function string proceeded with a '.'.

### NumberColor

```csharp
public Color NumberColor { get; set; }
```

引擎名：`number_color`

Sets the color for numbers.

### SymbolColor

```csharp
public Color SymbolColor { get; set; }
```

引擎名：`symbol_color`

Sets the color for symbols.

## 方法

### AddColorRegion

```csharp
public void AddColorRegion(string startKey, string endKey, Color color, bool lineOnly = false)
```

引擎名：`add_color_region`

Adds a color region (such as for comments or strings) from `startKey` to `endKey`. Both keys should be symbols, and `startKey` must not be shared with other delimiters.

If `lineOnly` is `true` or `endKey` is an empty `System.String`, the region does not carry over to the next line.

### AddKeywordColor

```csharp
public void AddKeywordColor(string keyword, Color color)
```

引擎名：`add_keyword_color`

Sets the color for a keyword.

The keyword cannot contain any symbols except '_'.

### AddMemberKeywordColor

```csharp
public void AddMemberKeywordColor(string memberKeyword, Color color)
```

引擎名：`add_member_keyword_color`

Sets the color for a member keyword.

The member keyword cannot contain any symbols except '_'.

It will not be highlighted if preceded by a '.'.

### ClearColorRegions

```csharp
public void ClearColorRegions()
```

引擎名：`clear_color_regions`

Removes all color regions.

### ClearKeywordColors

```csharp
public void ClearKeywordColors()
```

引擎名：`clear_keyword_colors`

Removes all keywords.

### ClearMemberKeywordColors

```csharp
public void ClearMemberKeywordColors()
```

引擎名：`clear_member_keyword_colors`

Removes all member keywords.

### GetColorRegions

```csharp
public Dictionary GetColorRegions()
```

引擎名：`get_color_regions`

### GetFunctionColor

```csharp
public Color GetFunctionColor()
```

引擎名：`get_function_color`

### GetKeywordColor

```csharp
public Color GetKeywordColor(string keyword)
```

引擎名：`get_keyword_color`

Returns the color for a keyword.

### GetKeywordColors

```csharp
public Dictionary GetKeywordColors()
```

引擎名：`get_keyword_colors`

### GetMemberKeywordColor

```csharp
public Color GetMemberKeywordColor(string memberKeyword)
```

引擎名：`get_member_keyword_color`

Returns the color for a member keyword.

### GetMemberKeywordColors

```csharp
public Dictionary GetMemberKeywordColors()
```

引擎名：`get_member_keyword_colors`

### GetMemberVariableColor

```csharp
public Color GetMemberVariableColor()
```

引擎名：`get_member_variable_color`

### GetNumberColor

```csharp
public Color GetNumberColor()
```

引擎名：`get_number_color`

### GetSymbolColor

```csharp
public Color GetSymbolColor()
```

引擎名：`get_symbol_color`

### HasColorRegion

```csharp
public bool HasColorRegion(string startKey)
```

引擎名：`has_color_region`

Returns `true` if the start key exists, else `false`.

### HasKeywordColor

```csharp
public bool HasKeywordColor(string keyword)
```

引擎名：`has_keyword_color`

Returns `true` if the keyword exists, else `false`.

### HasMemberKeywordColor

```csharp
public bool HasMemberKeywordColor(string memberKeyword)
```

引擎名：`has_member_keyword_color`

Returns `true` if the member keyword exists, else `false`.

### RemoveColorRegion

```csharp
public void RemoveColorRegion(string startKey)
```

引擎名：`remove_color_region`

Removes the color region that uses that start key.

### RemoveKeywordColor

```csharp
public void RemoveKeywordColor(string keyword)
```

引擎名：`remove_keyword_color`

Removes the keyword.

### RemoveMemberKeywordColor

```csharp
public void RemoveMemberKeywordColor(string memberKeyword)
```

引擎名：`remove_member_keyword_color`

Removes the member keyword.

### SetColorRegions

```csharp
public void SetColorRegions(Dictionary colorRegions)
```

引擎名：`set_color_regions`

### SetFunctionColor

```csharp
public void SetFunctionColor(Color color)
```

引擎名：`set_function_color`

### SetKeywordColors

```csharp
public void SetKeywordColors(Dictionary keywords)
```

引擎名：`set_keyword_colors`

### SetMemberKeywordColors

```csharp
public void SetMemberKeywordColors(Dictionary memberKeyword)
```

引擎名：`set_member_keyword_colors`

### SetMemberVariableColor

```csharp
public void SetMemberVariableColor(Color color)
```

引擎名：`set_member_variable_color`

### SetNumberColor

```csharp
public void SetNumberColor(Color color)
```

引擎名：`set_number_color`

### SetSymbolColor

```csharp
public void SetSymbolColor(Color color)
```

引擎名：`set_symbol_color`
