<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GDScript

命名空间 `Godot`；class；继承 [`Script`](Script.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A script implemented in the GDScript programming language, saved with the `.gd` extension. The script extends the functionality of all objects that instantiate it.

Calling `New()` creates a new instance of the script. [`GodotObject.SetScript`](GodotObject.md) extends an existing object, if that object's class matches one of the script's base classes.

If you are looking for GDScript's built-in functions, see `@GDScript` instead.

## 构造函数

### GDScript

```csharp
public GDScript()
```

## 方法

### New

```csharp
public Variant New(ReadOnlySpan<Variant> args)
```

引擎名：`new`

Returns a new instance of the script.

```csharp
var MyClass = load("myclass.gd")
var instance = MyClass.new()
print(instance.get_script() == MyClass) # Prints true

```

### New

```csharp
public Variant New(params Variant[] args)
```

引擎名：`new`

Returns a new instance of the script.

```csharp
var MyClass = load("myclass.gd")
var instance = MyClass.new()
print(instance.get_script() == MyClass) # Prints true

```
