<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AssemblyHasScriptsAttribute

命名空间 `Godot`；class

Attribute that determines that the assembly contains Godot scripts and, optionally, the
collection of types that implement scripts; otherwise, retrieving the types requires lookup.

## 构造函数

### AssemblyHasScriptsAttribute

```csharp
public AssemblyHasScriptsAttribute()
```

Constructs a new AssemblyHasScriptsAttribute instance
that requires lookup to get the Godot scripts.

### AssemblyHasScriptsAttribute

```csharp
public AssemblyHasScriptsAttribute(Type[] scriptTypes)
```

Constructs a new AssemblyHasScriptsAttribute instance
that includes the Godot script types and requires no lookup.

参数：
- `scriptTypes`：The collection of types that implement a Godot script.

## 属性

### RequiresLookup

```csharp
public bool RequiresLookup { get; }
```

If the Godot scripts contained in the assembly require lookup
and can't rely on `ScriptTypes`.

### ScriptTypes

```csharp
public Type[] ScriptTypes { get; }
```

The collection of types that implement a Godot script.
