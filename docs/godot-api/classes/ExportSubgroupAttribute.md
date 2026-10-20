<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ExportSubgroupAttribute

命名空间 `Godot`；class

Define a new subgroup for the following exported properties. This helps to organize properties in the Inspector dock.

## 构造函数

### ExportSubgroupAttribute

```csharp
public ExportSubgroupAttribute(string name, string prefix = "")
```

Define a new subgroup for the following exported properties. This helps to organize properties in the Inspector dock.

参数：
- `name`：The name of the subgroup.
- `prefix`：If provided, the subgroup would make group to only consider properties that have this prefix.

## 属性

### Name

```csharp
public string Name { get; }
```

Name of the subgroup.

### Prefix

```csharp
public string Prefix { get; }
```

If provided, the prefix that all properties must have to be considered part of the subgroup.
