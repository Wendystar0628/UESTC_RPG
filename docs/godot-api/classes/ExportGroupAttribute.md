<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ExportGroupAttribute

命名空间 `Godot`；class

Define a new group for the following exported properties. This helps to organize properties in the Inspector dock.

## 构造函数

### ExportGroupAttribute

```csharp
public ExportGroupAttribute(string name, string prefix = "")
```

Define a new group for the following exported properties.

参数：
- `name`：The name of the group.
- `prefix`：If provided, the group would make group to only consider properties that have this prefix.

## 属性

### Name

```csharp
public string Name { get; }
```

Name of the group.

### Prefix

```csharp
public string Prefix { get; }
```

If provided, the prefix that all properties must have to be considered part of the group.
