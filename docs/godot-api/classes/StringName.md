<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StringName

命名空间 `Godot`；class

StringNames are immutable strings designed for general-purpose representation of unique names.
StringName ensures that only one instance of a given name exists (so two StringNames with the
same value are the same object).
Comparing them is much faster than with regular strings, because only the pointers are compared,
not the whole strings.

## 构造函数

### StringName

```csharp
public StringName()
```

Constructs an empty `StringName`.

### StringName

```csharp
public StringName(string name)
```

Constructs a `StringName` from the given `name` string.

参数：
- `name`：String to construct the `StringName` from.

## 属性

### IsEmpty

```csharp
public bool IsEmpty { get; }
```

引擎名：`is_empty`

Check whether this `StringName` is empty.

返回：If the `StringName` is empty.

## 方法

### Dispose

```csharp
public void Dispose()
```

Disposes of this `StringName`.

### Dispose

```csharp
public void Dispose(bool disposing)
```

### Equals

```csharp
public bool Equals(StringName other)
```

### Equals

```csharp
public bool Equals(in godot_string_name other)
```

### Equals

```csharp
public virtual bool Equals(object obj)
```

### GetHashCode

```csharp
public virtual int GetHashCode()
```

### ToString

```csharp
public virtual string ToString()
```

Converts this `StringName` to a string.

返回：A string representation of this `StringName`.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(StringName left, StringName right)
```

### op_Equality

```csharp
public static bool op_Equality(StringName left, in godot_string_name right)
```

### op_Equality

```csharp
public static bool op_Equality(in godot_string_name left, StringName right)
```

### op_Implicit

```csharp
public static StringName op_Implicit(string from)
```

Converts a string to a `StringName`.

参数：
- `from`：The string to convert.

### op_Implicit

```csharp
public static string op_Implicit(StringName from)
```

Converts a `StringName` to a string.

参数：
- `from`：The `StringName` to convert.

### op_Inequality

```csharp
public static bool op_Inequality(StringName left, StringName right)
```

### op_Inequality

```csharp
public static bool op_Inequality(StringName left, in godot_string_name right)
```

### op_Inequality

```csharp
public static bool op_Inequality(in godot_string_name left, StringName right)
```
