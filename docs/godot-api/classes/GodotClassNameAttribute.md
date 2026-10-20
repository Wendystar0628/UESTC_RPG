<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GodotClassNameAttribute

命名空间 `Godot`；class

Attribute that specifies the engine class name when it's not the same
as the generated C# class name. This allows introspection code to find
the name associated with the class. If the attribute is not present,
the C# class name can be used instead.

## 构造函数

### GodotClassNameAttribute

```csharp
public GodotClassNameAttribute(string name)
```

Specify the name that represents the original engine class.

参数：
- `name`：Name of the original engine class.

## 属性

### Name

```csharp
public string Name { get; }
```

Original engine class name.
