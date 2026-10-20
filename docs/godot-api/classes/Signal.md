<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Signal

命名空间 `Godot`；struct

Represents a signal defined in an object.

## 构造函数

### Signal

```csharp
public Signal(GodotObject owner, StringName name)
```

Creates a new `Signal` with the name `name`
in the specified `owner`.

参数：
- `owner`：Object that contains the signal.
- `name`：Name of the signal.

## 属性

### Name

```csharp
public StringName Name { get; }
```

Name of the signal.

### Owner

```csharp
public GodotObject Owner { get; }
```

Object that contains the signal.

## 方法

### GetAwaiter

```csharp
public IAwaiter<Variant[]> GetAwaiter()
```
