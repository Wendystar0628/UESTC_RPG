<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ClassDB

命名空间 `Godot`；static class

Provides access to metadata stored for every available engine class.

**Note:** Script-defined classes with `class_name` are not part of `ClassDB`, so they will not return reflection data such as a method or property list. However, [`GDExtension`](GDExtension.md)-defined classes *are* part of `ClassDB`, so they will return reflection data.

## 属性

### Singleton

```csharp
public static ClassDBInstance Singleton { get; }
```

## 方法

### CanInstantiate

```csharp
public static bool CanInstantiate(StringName class)
```

引擎名：`can_instantiate`

Returns `true` if objects can be instantiated from the specified `class`, otherwise returns `false`.

### ClassCallStatic

```csharp
public static Variant ClassCallStatic(StringName class, StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`class_call_static`

Calls a static method on a class.

### ClassCallStatic

```csharp
public static Variant ClassCallStatic(StringName class, StringName method, params Variant[] args)
```

引擎名：`class_call_static`

Calls a static method on a class.

### ClassExists

```csharp
public static bool ClassExists(StringName class)
```

引擎名：`class_exists`

Returns whether the specified `class` is available or not.

### ClassGetApiType

```csharp
public static ClassDB.ApiType ClassGetApiType(StringName class)
```

引擎名：`class_get_api_type`

Returns the API type of the specified `class`.

### ClassGetEnumConstants

```csharp
public static string[] ClassGetEnumConstants(StringName class, StringName enum, bool noInheritance = false)
```

引擎名：`class_get_enum_constants`

Returns an array with all the keys in `enum` of `class` or its ancestry.

### ClassGetEnumList

```csharp
public static string[] ClassGetEnumList(StringName class, bool noInheritance = false)
```

引擎名：`class_get_enum_list`

Returns an array with all the enums of `class` or its ancestry.

### ClassGetIntegerConstant

```csharp
public static long ClassGetIntegerConstant(StringName class, StringName name)
```

引擎名：`class_get_integer_constant`

Returns the value of the integer constant `name` of `class` or its ancestry. Always returns 0 when the constant could not be found.

### ClassGetIntegerConstantEnum

```csharp
public static StringName ClassGetIntegerConstantEnum(StringName class, StringName name, bool noInheritance = false)
```

引擎名：`class_get_integer_constant_enum`

Returns which enum the integer constant `name` of `class` or its ancestry belongs to.

### ClassGetIntegerConstantList

```csharp
public static string[] ClassGetIntegerConstantList(StringName class, bool noInheritance = false)
```

引擎名：`class_get_integer_constant_list`

Returns an array with the names all the integer constants of `class` or its ancestry.

### ClassGetMethodArgumentCount

```csharp
public static int ClassGetMethodArgumentCount(StringName class, StringName method, bool noInheritance = false)
```

引擎名：`class_get_method_argument_count`

Returns the number of arguments of the method `method` of `class` or its ancestry if `noInheritance` is `false`.

### ClassGetMethodList

```csharp
public static Array<Dictionary> ClassGetMethodList(StringName class, bool noInheritance = false)
```

引擎名：`class_get_method_list`

Returns an array with all the methods of `class` or its ancestry if `noInheritance` is `false`. Every element of the array is a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `args`, `default_args`, `flags`, `id`, `name`, `return: (class_name, hint, hint_string, name, type, usage)`.

**Note:** In exported release builds the debug info is not available, so the returned dictionaries will contain only method names.

### ClassGetProperty

```csharp
public static Variant ClassGetProperty(GodotObject object, StringName property)
```

引擎名：`class_get_property`

Returns the value of `property` of `object` or its ancestry.

### ClassGetPropertyDefaultValue

```csharp
public static Variant ClassGetPropertyDefaultValue(StringName class, StringName property)
```

引擎名：`class_get_property_default_value`

Returns the default value of `property` of `class` or its ancestor classes.

### ClassGetPropertyGetter

```csharp
public static StringName ClassGetPropertyGetter(StringName class, StringName property)
```

引擎名：`class_get_property_getter`

Returns the getter method name of `property` of `class`.

### ClassGetPropertyList

```csharp
public static Array<Dictionary> ClassGetPropertyList(StringName class, bool noInheritance = false)
```

引擎名：`class_get_property_list`

Returns an array with all the properties of `class` or its ancestry if `noInheritance` is `false`.

### ClassGetPropertySetter

```csharp
public static StringName ClassGetPropertySetter(StringName class, StringName property)
```

引擎名：`class_get_property_setter`

Returns the setter method name of `property` of `class`.

### ClassGetSignal

```csharp
public static Dictionary ClassGetSignal(StringName class, StringName signal)
```

引擎名：`class_get_signal`

Returns the `signal` data of `class` or its ancestry. The returned value is a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `args`, `default_args`, `flags`, `id`, `name`, `return: (class_name, hint, hint_string, name, type, usage)`.

### ClassGetSignalList

```csharp
public static Array<Dictionary> ClassGetSignalList(StringName class, bool noInheritance = false)
```

引擎名：`class_get_signal_list`

Returns an array with all the signals of `class` or its ancestry if `noInheritance` is `false`. Every element of the array is a [`Collections.Dictionary`](Collections.Dictionary.md) as described in `ClassGetSignal()`.

### ClassHasEnum

```csharp
public static bool ClassHasEnum(StringName class, StringName name, bool noInheritance = false)
```

引擎名：`class_has_enum`

Returns whether `class` or its ancestry has an enum called `name` or not.

### ClassHasIntegerConstant

```csharp
public static bool ClassHasIntegerConstant(StringName class, StringName name)
```

引擎名：`class_has_integer_constant`

Returns whether `class` or its ancestry has an integer constant called `name` or not.

### ClassHasMethod

```csharp
public static bool ClassHasMethod(StringName class, StringName method, bool noInheritance = false)
```

引擎名：`class_has_method`

Returns whether `class` (or its ancestry if `noInheritance` is `false`) has a method called `method` or not.

### ClassHasSignal

```csharp
public static bool ClassHasSignal(StringName class, StringName signal)
```

引擎名：`class_has_signal`

Returns whether `class` or its ancestry has a signal called `signal` or not.

### ClassSetProperty

```csharp
public static Error ClassSetProperty(GodotObject object, StringName property, Variant value)
```

引擎名：`class_set_property`

Sets `property` value of `object` to `value`.

### GetClassList

```csharp
public static string[] GetClassList()
```

引擎名：`get_class_list`

Returns the names of all engine classes available.

**Note:** Script-defined classes with `class_name` are not included in this list. Use [`ProjectSettings.GetGlobalClassList`](ProjectSettings.md) to get a list of script-defined classes instead.

### GetInheritersFromClass

```csharp
public static string[] GetInheritersFromClass(StringName class)
```

引擎名：`get_inheriters_from_class`

Returns the names of all engine classes that directly or indirectly inherit from `class`.

### GetParentClass

```csharp
public static StringName GetParentClass(StringName class)
```

引擎名：`get_parent_class`

Returns the parent class of `class`.

### Instantiate

```csharp
public static Variant Instantiate(StringName class)
```

引擎名：`instantiate`

Creates an instance of `class`.

### IsClassEnabled

```csharp
public static bool IsClassEnabled(StringName class)
```

引擎名：`is_class_enabled`

Returns whether this `class` is enabled or not.

### IsClassEnumBitfield

```csharp
public static bool IsClassEnumBitfield(StringName class, StringName enum, bool noInheritance = false)
```

引擎名：`is_class_enum_bitfield`

Returns whether `class` (or its ancestor classes if `noInheritance` is `false`) has an enum called `enum` that is a bitfield.

### IsParentClass

```csharp
public static bool IsParentClass(StringName class, StringName inherits)
```

引擎名：`is_parent_class`

Returns whether `inherits` is an ancestor of `class` or not.

## ApiType

命名空间 `Godot`；enum

### 枚举值

- `Core = 0`：Native Core class type.
- `Editor = 1`：Native Editor class type.
- `Extension = 2`：GDExtension class type.
- `EditorExtension = 3`：GDExtension Editor class type.
- `None = 4`：Unknown class type.
