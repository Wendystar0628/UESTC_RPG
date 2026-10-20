<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ClassDBInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Provides access to metadata stored for every available engine class.

**Note:** Script-defined classes with `class_name` are not part of [`ClassDB`](ClassDB.md), so they will not return reflection data such as a method or property list. However, [`GDExtension`](GDExtension.md)-defined classes *are* part of [`ClassDB`](ClassDB.md), so they will return reflection data.

## 方法

### CanInstantiate

```csharp
public bool CanInstantiate(StringName class)
```

Returns `true` if objects can be instantiated from the specified `class`, otherwise returns `false`.

### ClassCallStatic

```csharp
public Variant ClassCallStatic(StringName class, StringName method, ReadOnlySpan<Variant> args)
```

Calls a static method on a class.

### ClassCallStatic

```csharp
public Variant ClassCallStatic(StringName class, StringName method, params Variant[] args)
```

Calls a static method on a class.

### ClassExists

```csharp
public bool ClassExists(StringName class)
```

Returns whether the specified `class` is available or not.

### ClassGetApiType

```csharp
public ClassDB.ApiType ClassGetApiType(StringName class)
```

Returns the API type of the specified `class`.

### ClassGetEnumConstants

```csharp
public string[] ClassGetEnumConstants(StringName class, StringName enum, bool noInheritance = false)
```

Returns an array with all the keys in `enum` of `class` or its ancestry.

### ClassGetEnumList

```csharp
public string[] ClassGetEnumList(StringName class, bool noInheritance = false)
```

Returns an array with all the enums of `class` or its ancestry.

### ClassGetIntegerConstant

```csharp
public long ClassGetIntegerConstant(StringName class, StringName name)
```

Returns the value of the integer constant `name` of `class` or its ancestry. Always returns 0 when the constant could not be found.

### ClassGetIntegerConstantEnum

```csharp
public StringName ClassGetIntegerConstantEnum(StringName class, StringName name, bool noInheritance = false)
```

Returns which enum the integer constant `name` of `class` or its ancestry belongs to.

### ClassGetIntegerConstantList

```csharp
public string[] ClassGetIntegerConstantList(StringName class, bool noInheritance = false)
```

Returns an array with the names all the integer constants of `class` or its ancestry.

### ClassGetMethodArgumentCount

```csharp
public int ClassGetMethodArgumentCount(StringName class, StringName method, bool noInheritance = false)
```

Returns the number of arguments of the method `method` of `class` or its ancestry if `noInheritance` is `false`.

### ClassGetMethodList

```csharp
public Array<Dictionary> ClassGetMethodList(StringName class, bool noInheritance = false)
```

Returns an array with all the methods of `class` or its ancestry if `noInheritance` is `false`. Every element of the array is a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `args`, `default_args`, `flags`, `id`, `name`, `return: (class_name, hint, hint_string, name, type, usage)`.

**Note:** In exported release builds the debug info is not available, so the returned dictionaries will contain only method names.

### ClassGetProperty

```csharp
public Variant ClassGetProperty(GodotObject object, StringName property)
```

Returns the value of `property` of `object` or its ancestry.

### ClassGetPropertyDefaultValue

```csharp
public Variant ClassGetPropertyDefaultValue(StringName class, StringName property)
```

Returns the default value of `property` of `class` or its ancestor classes.

### ClassGetPropertyGetter

```csharp
public StringName ClassGetPropertyGetter(StringName class, StringName property)
```

Returns the getter method name of `property` of `class`.

### ClassGetPropertyList

```csharp
public Array<Dictionary> ClassGetPropertyList(StringName class, bool noInheritance = false)
```

Returns an array with all the properties of `class` or its ancestry if `noInheritance` is `false`.

### ClassGetPropertySetter

```csharp
public StringName ClassGetPropertySetter(StringName class, StringName property)
```

Returns the setter method name of `property` of `class`.

### ClassGetSignal

```csharp
public Dictionary ClassGetSignal(StringName class, StringName signal)
```

Returns the `signal` data of `class` or its ancestry. The returned value is a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys: `args`, `default_args`, `flags`, `id`, `name`, `return: (class_name, hint, hint_string, name, type, usage)`.

### ClassGetSignalList

```csharp
public Array<Dictionary> ClassGetSignalList(StringName class, bool noInheritance = false)
```

Returns an array with all the signals of `class` or its ancestry if `noInheritance` is `false`. Every element of the array is a [`Collections.Dictionary`](Collections.Dictionary.md) as described in `ClassGetSignal()`.

### ClassHasEnum

```csharp
public bool ClassHasEnum(StringName class, StringName name, bool noInheritance = false)
```

Returns whether `class` or its ancestry has an enum called `name` or not.

### ClassHasIntegerConstant

```csharp
public bool ClassHasIntegerConstant(StringName class, StringName name)
```

Returns whether `class` or its ancestry has an integer constant called `name` or not.

### ClassHasMethod

```csharp
public bool ClassHasMethod(StringName class, StringName method, bool noInheritance = false)
```

Returns whether `class` (or its ancestry if `noInheritance` is `false`) has a method called `method` or not.

### ClassHasSignal

```csharp
public bool ClassHasSignal(StringName class, StringName signal)
```

Returns whether `class` or its ancestry has a signal called `signal` or not.

### ClassSetProperty

```csharp
public Error ClassSetProperty(GodotObject object, StringName property, Variant value)
```

Sets `property` value of `object` to `value`.

### GetClassList

```csharp
public string[] GetClassList()
```

Returns the names of all engine classes available.

**Note:** Script-defined classes with `class_name` are not included in this list. Use [`ProjectSettings.GetGlobalClassList`](ProjectSettings.md) to get a list of script-defined classes instead.

### GetInheritersFromClass

```csharp
public string[] GetInheritersFromClass(StringName class)
```

Returns the names of all engine classes that directly or indirectly inherit from `class`.

### GetParentClass

```csharp
public StringName GetParentClass(StringName class)
```

Returns the parent class of `class`.

### Instantiate

```csharp
public Variant Instantiate(StringName class)
```

Creates an instance of `class`.

### IsClassEnabled

```csharp
public bool IsClassEnabled(StringName class)
```

Returns whether this `class` is enabled or not.

### IsClassEnumBitfield

```csharp
public bool IsClassEnumBitfield(StringName class, StringName enum, bool noInheritance = false)
```

Returns whether `class` (or its ancestor classes if `noInheritance` is `false`) has an enum called `enum` that is a bitfield.

### IsParentClass

```csharp
public bool IsParentClass(StringName class, StringName inherits)
```

Returns whether `inherits` is an ancestor of `class` or not.
