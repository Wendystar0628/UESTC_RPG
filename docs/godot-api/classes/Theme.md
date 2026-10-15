<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Theme

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource used for styling/skinning [`Control`](Control.md) and [`Window`](Window.md) nodes. While individual controls can be styled using their local theme overrides (see [`Control.AddThemeColorOverride`](Control.md)), theme resources allow you to store and apply the same settings across all controls sharing the same type (e.g. style all [`Button`](Button.md)s the same). One theme resource can be used for the entire project, but you can also set a separate theme resource to a branch of control nodes. A theme resource assigned to a control applies to the control itself, as well as all of its direct and indirect children (as long as a chain of controls is uninterrupted).

Use `ProjectSettings.gui/theme/custom` to set up a project-scope theme that will be available to every control in your project.

Use [`Control.Theme`](Control.md) of any control node to set up a theme that will be available to that control and all of its direct and indirect children.

## 构造函数

### Theme

```csharp
public Theme()
```

## 属性

### DefaultBaseScale

```csharp
public float DefaultBaseScale { get; set; }
```

引擎名：`default_base_scale`

The default base scale factor of this theme resource. Used by some controls to scale their visual properties based on the global scale factor. If this value is set to `0.0`, the global scale factor is used (see [`ThemeDB.FallbackBaseScale`](ThemeDB.md)).

Use `HasDefaultBaseScale()` to check if this value is valid.

### DefaultFont

```csharp
public Font DefaultFont { get; set; }
```

引擎名：`default_font`

The default font of this theme resource. Used as the default value when trying to fetch a font resource that doesn't exist in this theme or is in invalid state. If the default font is also missing or invalid, the engine fallback value is used (see [`ThemeDB.FallbackFont`](ThemeDB.md)).

Use `HasDefaultFont()` to check if this value is valid.

### DefaultFontSize

```csharp
public int DefaultFontSize { get; set; }
```

引擎名：`default_font_size`

The default font size of this theme resource. Used as the default value when trying to fetch a font size value that doesn't exist in this theme or is in invalid state. If the default font size is also missing or invalid, the engine fallback value is used (see [`ThemeDB.FallbackFontSize`](ThemeDB.md)).

Values below `1` are invalid and can be used to unset the property. Use `HasDefaultFontSize()` to check if this value is valid.

## 方法

### AddType

```csharp
public void AddType(StringName themeType)
```

引擎名：`add_type`

Adds an empty theme type for every valid data type.

**Note:** Empty types are not saved with the theme. This method only exists to perform in-memory changes to the resource. Use available `set_*` methods to add theme items.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Removes all the theme properties defined on the theme resource.

### ClearColor

```csharp
public void ClearColor(StringName name, StringName themeType)
```

引擎名：`clear_color`

Removes the [`Color`](Color.md) property defined by `name` and `themeType`, if it exists.

Fails if it doesn't exist. Use `HasColor()` to check for existence.

### ClearConstant

```csharp
public void ClearConstant(StringName name, StringName themeType)
```

引擎名：`clear_constant`

Removes the constant property defined by `name` and `themeType`, if it exists.

Fails if it doesn't exist. Use `HasConstant()` to check for existence.

### ClearFont

```csharp
public void ClearFont(StringName name, StringName themeType)
```

引擎名：`clear_font`

Removes the [`Font`](Font.md) property defined by `name` and `themeType`, if it exists.

Fails if it doesn't exist. Use `HasFont()` to check for existence.

### ClearFontSize

```csharp
public void ClearFontSize(StringName name, StringName themeType)
```

引擎名：`clear_font_size`

Removes the font size property defined by `name` and `themeType`, if it exists.

Fails if it doesn't exist. Use `HasFontSize()` to check for existence.

### ClearIcon

```csharp
public void ClearIcon(StringName name, StringName themeType)
```

引擎名：`clear_icon`

Removes the icon property defined by `name` and `themeType`, if it exists.

Fails if it doesn't exist. Use `HasIcon()` to check for existence.

### ClearStylebox

```csharp
public void ClearStylebox(StringName name, StringName themeType)
```

引擎名：`clear_stylebox`

Removes the [`StyleBox`](StyleBox.md) property defined by `name` and `themeType`, if it exists.

Fails if it doesn't exist. Use `HasStylebox()` to check for existence.

### ClearThemeItem

```csharp
public void ClearThemeItem(Theme.DataType dataType, StringName name, StringName themeType)
```

引擎名：`clear_theme_item`

Removes the theme property of `dataType` defined by `name` and `themeType`, if it exists.

Fails if it doesn't exist. Use `HasThemeItem()` to check for existence.

**Note:** This method is analogous to calling the corresponding data type specific method, but can be used for more generalized logic.

### ClearTypeVariation

```csharp
public void ClearTypeVariation(StringName themeType)
```

引擎名：`clear_type_variation`

Unmarks `themeType` as being a variation of another theme type. See `SetTypeVariation()`.

### GetColor

```csharp
public Color GetColor(StringName name, StringName themeType)
```

引擎名：`get_color`

Returns the [`Color`](Color.md) property defined by `name` and `themeType`, if it exists.

Returns the default color value if the property doesn't exist. Use `HasColor()` to check for existence.

### GetColorList

```csharp
public string[] GetColorList(string themeType)
```

引擎名：`get_color_list`

Returns a list of names for [`Color`](Color.md) properties defined with `themeType`. Use `GetColorTypeList()` to get a list of possible theme type names.

### GetColorTypeList

```csharp
public string[] GetColorTypeList()
```

引擎名：`get_color_type_list`

Returns a list of all unique theme type names for [`Color`](Color.md) properties. Use `GetTypeList()` to get a list of all unique theme types.

### GetConstant

```csharp
public int GetConstant(StringName name, StringName themeType)
```

引擎名：`get_constant`

Returns the constant property defined by `name` and `themeType`, if it exists.

Returns `0` if the property doesn't exist. Use `HasConstant()` to check for existence.

### GetConstantList

```csharp
public string[] GetConstantList(string themeType)
```

引擎名：`get_constant_list`

Returns a list of names for constant properties defined with `themeType`. Use `GetConstantTypeList()` to get a list of possible theme type names.

### GetConstantTypeList

```csharp
public string[] GetConstantTypeList()
```

引擎名：`get_constant_type_list`

Returns a list of all unique theme type names for constant properties. Use `GetTypeList()` to get a list of all unique theme types.

### GetDefaultBaseScale

```csharp
public float GetDefaultBaseScale()
```

引擎名：`get_default_base_scale`

### GetDefaultFont

```csharp
public Font GetDefaultFont()
```

引擎名：`get_default_font`

### GetDefaultFontSize

```csharp
public int GetDefaultFontSize()
```

引擎名：`get_default_font_size`

### GetFont

```csharp
public Font GetFont(StringName name, StringName themeType)
```

引擎名：`get_font`

Returns the [`Font`](Font.md) property defined by `name` and `themeType`, if it exists.

Returns the default theme font if the property doesn't exist and the default theme font is set up (see `DefaultFont`). Use `HasFont()` to check for existence of the property and `HasDefaultFont()` to check for existence of the default theme font.

Returns the engine fallback font value, if neither exist (see [`ThemeDB.FallbackFont`](ThemeDB.md)).

### GetFontList

```csharp
public string[] GetFontList(string themeType)
```

引擎名：`get_font_list`

Returns a list of names for [`Font`](Font.md) properties defined with `themeType`. Use `GetFontTypeList()` to get a list of possible theme type names.

### GetFontSize

```csharp
public int GetFontSize(StringName name, StringName themeType)
```

引擎名：`get_font_size`

Returns the font size property defined by `name` and `themeType`, if it exists.

Returns the default theme font size if the property doesn't exist and the default theme font size is set up (see `DefaultFontSize`). Use `HasFontSize()` to check for existence of the property and `HasDefaultFontSize()` to check for existence of the default theme font.

Returns the engine fallback font size value, if neither exist (see [`ThemeDB.FallbackFontSize`](ThemeDB.md)).

### GetFontSizeList

```csharp
public string[] GetFontSizeList(string themeType)
```

引擎名：`get_font_size_list`

Returns a list of names for font size properties defined with `themeType`. Use `GetFontSizeTypeList()` to get a list of possible theme type names.

### GetFontSizeTypeList

```csharp
public string[] GetFontSizeTypeList()
```

引擎名：`get_font_size_type_list`

Returns a list of all unique theme type names for font size properties. Use `GetTypeList()` to get a list of all unique theme types.

### GetFontTypeList

```csharp
public string[] GetFontTypeList()
```

引擎名：`get_font_type_list`

Returns a list of all unique theme type names for [`Font`](Font.md) properties. Use `GetTypeList()` to get a list of all unique theme types.

### GetIcon

```csharp
public Texture2D GetIcon(StringName name, StringName themeType)
```

引擎名：`get_icon`

Returns the icon property defined by `name` and `themeType`, if it exists.

Returns the engine fallback icon value if the property doesn't exist (see [`ThemeDB.FallbackIcon`](ThemeDB.md)). Use `HasIcon()` to check for existence.

### GetIconList

```csharp
public string[] GetIconList(string themeType)
```

引擎名：`get_icon_list`

Returns a list of names for icon properties defined with `themeType`. Use `GetIconTypeList()` to get a list of possible theme type names.

### GetIconTypeList

```csharp
public string[] GetIconTypeList()
```

引擎名：`get_icon_type_list`

Returns a list of all unique theme type names for icon properties. Use `GetTypeList()` to get a list of all unique theme types.

### GetStylebox

```csharp
public StyleBox GetStylebox(StringName name, StringName themeType)
```

引擎名：`get_stylebox`

Returns the [`StyleBox`](StyleBox.md) property defined by `name` and `themeType`, if it exists.

Returns the engine fallback stylebox value if the property doesn't exist (see [`ThemeDB.FallbackStylebox`](ThemeDB.md)). Use `HasStylebox()` to check for existence.

### GetStyleboxList

```csharp
public string[] GetStyleboxList(string themeType)
```

引擎名：`get_stylebox_list`

Returns a list of names for [`StyleBox`](StyleBox.md) properties defined with `themeType`. Use `GetStyleboxTypeList()` to get a list of possible theme type names.

### GetStyleboxTypeList

```csharp
public string[] GetStyleboxTypeList()
```

引擎名：`get_stylebox_type_list`

Returns a list of all unique theme type names for [`StyleBox`](StyleBox.md) properties. Use `GetTypeList()` to get a list of all unique theme types.

### GetThemeItem

```csharp
public Variant GetThemeItem(Theme.DataType dataType, StringName name, StringName themeType)
```

引擎名：`get_theme_item`

Returns the theme property of `dataType` defined by `name` and `themeType`, if it exists.

Returns the engine fallback value if the property doesn't exist (see [`ThemeDB`](ThemeDB.md)). Use `HasThemeItem()` to check for existence.

**Note:** This method is analogous to calling the corresponding data type specific method, but can be used for more generalized logic.

### GetThemeItemList

```csharp
public string[] GetThemeItemList(Theme.DataType dataType, string themeType)
```

引擎名：`get_theme_item_list`

Returns a list of names for properties of `dataType` defined with `themeType`. Use `GetThemeItemTypeList()` to get a list of possible theme type names.

**Note:** This method is analogous to calling the corresponding data type specific method, but can be used for more generalized logic.

### GetThemeItemTypeList

```csharp
public string[] GetThemeItemTypeList(Theme.DataType dataType)
```

引擎名：`get_theme_item_type_list`

Returns a list of all unique theme type names for `dataType` properties. Use `GetTypeList()` to get a list of all unique theme types.

**Note:** This method is analogous to calling the corresponding data type specific method, but can be used for more generalized logic.

### GetTypeList

```csharp
public string[] GetTypeList()
```

引擎名：`get_type_list`

Returns a list of all unique theme type names. Use the appropriate `get_*_type_list` method to get a list of unique theme types for a single data type.

### GetTypeVariationBase

```csharp
public StringName GetTypeVariationBase(StringName themeType)
```

引擎名：`get_type_variation_base`

Returns the name of the base theme type if `themeType` is a valid variation type. Returns an empty string otherwise.

### GetTypeVariationList

```csharp
public string[] GetTypeVariationList(StringName baseType)
```

引擎名：`get_type_variation_list`

Returns a list of all type variations for the given `baseType`.

### HasColor

```csharp
public bool HasColor(StringName name, StringName themeType)
```

引擎名：`has_color`

Returns `true` if the [`Color`](Color.md) property defined by `name` and `themeType` exists.

Returns `false` if it doesn't exist. Use `SetColor()` to define it.

### HasConstant

```csharp
public bool HasConstant(StringName name, StringName themeType)
```

引擎名：`has_constant`

Returns `true` if the constant property defined by `name` and `themeType` exists.

Returns `false` if it doesn't exist. Use `SetConstant()` to define it.

### HasDefaultBaseScale

```csharp
public bool HasDefaultBaseScale()
```

引擎名：`has_default_base_scale`

Returns `true` if `DefaultBaseScale` has a valid value.

Returns `false` if it doesn't. The value must be greater than `0.0` to be considered valid.

### HasDefaultFont

```csharp
public bool HasDefaultFont()
```

引擎名：`has_default_font`

Returns `true` if `DefaultFont` has a valid value.

Returns `false` if it doesn't.

### HasDefaultFontSize

```csharp
public bool HasDefaultFontSize()
```

引擎名：`has_default_font_size`

Returns `true` if `DefaultFontSize` has a valid value.

Returns `false` if it doesn't. The value must be greater than `0` to be considered valid.

### HasFont

```csharp
public bool HasFont(StringName name, StringName themeType)
```

引擎名：`has_font`

Returns `true` if the [`Font`](Font.md) property defined by `name` and `themeType` exists, or if the default theme font is set up (see `HasDefaultFont()`).

Returns `false` if neither exist. Use `SetFont()` to define the property.

### HasFontSize

```csharp
public bool HasFontSize(StringName name, StringName themeType)
```

引擎名：`has_font_size`

Returns `true` if the font size property defined by `name` and `themeType` exists, or if the default theme font size is set up (see `HasDefaultFontSize()`).

Returns `false` if neither exist. Use `SetFontSize()` to define the property.

### HasIcon

```csharp
public bool HasIcon(StringName name, StringName themeType)
```

引擎名：`has_icon`

Returns `true` if the icon property defined by `name` and `themeType` exists.

Returns `false` if it doesn't exist. Use `SetIcon()` to define it.

### HasStylebox

```csharp
public bool HasStylebox(StringName name, StringName themeType)
```

引擎名：`has_stylebox`

Returns `true` if the [`StyleBox`](StyleBox.md) property defined by `name` and `themeType` exists.

Returns `false` if it doesn't exist. Use `SetStylebox()` to define it.

### HasThemeItem

```csharp
public bool HasThemeItem(Theme.DataType dataType, StringName name, StringName themeType)
```

引擎名：`has_theme_item`

Returns `true` if the theme property of `dataType` defined by `name` and `themeType` exists.

Returns `false` if it doesn't exist. Use `SetThemeItem()` to define it.

**Note:** This method is analogous to calling the corresponding data type specific method, but can be used for more generalized logic.

### IsTypeVariation

```csharp
public bool IsTypeVariation(StringName themeType, StringName baseType)
```

引擎名：`is_type_variation`

Returns `true` if `themeType` is marked as a variation of `baseType`.

### MergeWith

```csharp
public void MergeWith(Theme other)
```

引擎名：`merge_with`

Adds missing and overrides existing definitions with values from the `other` theme resource.

**Note:** This modifies the current theme. If you want to merge two themes together without modifying either one, create a new empty theme and merge the other two into it one after another.

### RemoveType

```csharp
public void RemoveType(StringName themeType)
```

引擎名：`remove_type`

Removes the theme type, gracefully discarding defined theme items. If the type is a variation, this information is also erased. If the type is a base for type variations, those variations lose their base.

### RenameColor

```csharp
public void RenameColor(StringName oldName, StringName name, StringName themeType)
```

引擎名：`rename_color`

Renames the [`Color`](Color.md) property defined by `oldName` and `themeType` to `name`, if it exists.

Fails if it doesn't exist, or if a similar property with the new name already exists. Use `HasColor()` to check for existence, and `ClearColor()` to remove the existing property.

### RenameConstant

```csharp
public void RenameConstant(StringName oldName, StringName name, StringName themeType)
```

引擎名：`rename_constant`

Renames the constant property defined by `oldName` and `themeType` to `name`, if it exists.

Fails if it doesn't exist, or if a similar property with the new name already exists. Use `HasConstant()` to check for existence, and `ClearConstant()` to remove the existing property.

### RenameFont

```csharp
public void RenameFont(StringName oldName, StringName name, StringName themeType)
```

引擎名：`rename_font`

Renames the [`Font`](Font.md) property defined by `oldName` and `themeType` to `name`, if it exists.

Fails if it doesn't exist, or if a similar property with the new name already exists. Use `HasFont()` to check for existence, and `ClearFont()` to remove the existing property.

### RenameFontSize

```csharp
public void RenameFontSize(StringName oldName, StringName name, StringName themeType)
```

引擎名：`rename_font_size`

Renames the font size property defined by `oldName` and `themeType` to `name`, if it exists.

Fails if it doesn't exist, or if a similar property with the new name already exists. Use `HasFontSize()` to check for existence, and `ClearFontSize()` to remove the existing property.

### RenameIcon

```csharp
public void RenameIcon(StringName oldName, StringName name, StringName themeType)
```

引擎名：`rename_icon`

Renames the icon property defined by `oldName` and `themeType` to `name`, if it exists.

Fails if it doesn't exist, or if a similar property with the new name already exists. Use `HasIcon()` to check for existence, and `ClearIcon()` to remove the existing property.

### RenameStylebox

```csharp
public void RenameStylebox(StringName oldName, StringName name, StringName themeType)
```

引擎名：`rename_stylebox`

Renames the [`StyleBox`](StyleBox.md) property defined by `oldName` and `themeType` to `name`, if it exists.

Fails if it doesn't exist, or if a similar property with the new name already exists. Use `HasStylebox()` to check for existence, and `ClearStylebox()` to remove the existing property.

### RenameThemeItem

```csharp
public void RenameThemeItem(Theme.DataType dataType, StringName oldName, StringName name, StringName themeType)
```

引擎名：`rename_theme_item`

Renames the theme property of `dataType` defined by `oldName` and `themeType` to `name`, if it exists.

Fails if it doesn't exist, or if a similar property with the new name already exists. Use `HasThemeItem()` to check for existence, and `ClearThemeItem()` to remove the existing property.

**Note:** This method is analogous to calling the corresponding data type specific method, but can be used for more generalized logic.

### RenameType

```csharp
public void RenameType(StringName oldThemeType, StringName themeType)
```

引擎名：`rename_type`

Renames the theme type `oldThemeType` to `themeType`, if the old type exists and the new one doesn't exist.

**Note:** Renaming a theme type to an empty name or a variation to a type associated with a built-in class removes type variation connections in a way that cannot be undone by reversing the rename alone.

### SetColor

```csharp
public void SetColor(StringName name, StringName themeType, Color color)
```

引擎名：`set_color`

Creates or changes the value of the [`Color`](Color.md) property defined by `name` and `themeType`. Use `ClearColor()` to remove the property.

### SetConstant

```csharp
public void SetConstant(StringName name, StringName themeType, int constant)
```

引擎名：`set_constant`

Creates or changes the value of the constant property defined by `name` and `themeType`. Use `ClearConstant()` to remove the property.

### SetDefaultBaseScale

```csharp
public void SetDefaultBaseScale(float baseScale)
```

引擎名：`set_default_base_scale`

### SetDefaultFont

```csharp
public void SetDefaultFont(Font font)
```

引擎名：`set_default_font`

### SetDefaultFontSize

```csharp
public void SetDefaultFontSize(int fontSize)
```

引擎名：`set_default_font_size`

### SetFont

```csharp
public void SetFont(StringName name, StringName themeType, Font font)
```

引擎名：`set_font`

Creates or changes the value of the [`Font`](Font.md) property defined by `name` and `themeType`. Use `ClearFont()` to remove the property.

### SetFontSize

```csharp
public void SetFontSize(StringName name, StringName themeType, int fontSize)
```

引擎名：`set_font_size`

Creates or changes the value of the font size property defined by `name` and `themeType`. Use `ClearFontSize()` to remove the property.

### SetIcon

```csharp
public void SetIcon(StringName name, StringName themeType, Texture2D texture)
```

引擎名：`set_icon`

Creates or changes the value of the icon property defined by `name` and `themeType`. Use `ClearIcon()` to remove the property.

### SetStylebox

```csharp
public void SetStylebox(StringName name, StringName themeType, StyleBox texture)
```

引擎名：`set_stylebox`

Creates or changes the value of the [`StyleBox`](StyleBox.md) property defined by `name` and `themeType`. Use `ClearStylebox()` to remove the property.

### SetThemeItem

```csharp
public void SetThemeItem(Theme.DataType dataType, StringName name, StringName themeType, Variant value)
```

引擎名：`set_theme_item`

Creates or changes the value of the theme property of `dataType` defined by `name` and `themeType`. Use `ClearThemeItem()` to remove the property.

Fails if the `value` type is not accepted by `dataType`.

**Note:** This method is analogous to calling the corresponding data type specific method, but can be used for more generalized logic.

### SetTypeVariation

```csharp
public void SetTypeVariation(StringName themeType, StringName baseType)
```

引擎名：`set_type_variation`

Marks `themeType` as a variation of `baseType`.

This adds `themeType` as a suggested option for [`Control.ThemeTypeVariation`](Control.md) on a [`Control`](Control.md) that is of the `baseType` class.

Variations can also be nested, i.e. `baseType` can be another variation. If a chain of variations ends with a `baseType` matching the class of the [`Control`](Control.md), the whole chain is going to be suggested as options.

**Note:** Suggestions only show up if this theme resource is set as the project default theme. See `ProjectSettings.gui/theme/custom`.

## DataType

命名空间 `Godot`；enum

### 枚举值

- `Color = 0`：Theme's [`Color`](Color.md) item type.
- `Constant = 1`：Theme's constant item type.
- `Font = 2`：Theme's [`Font`](Font.md) item type.
- `FontSize = 3`：Theme's font size item type.
- `Icon = 4`：Theme's icon [`Texture2D`](Texture2D.md) item type.
- `Stylebox = 5`：Theme's [`StyleBox`](StyleBox.md) item type.
- `Max = 6`：Maximum value for the DataType enum.
