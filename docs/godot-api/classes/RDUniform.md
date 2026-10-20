<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDUniform

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDUniform

```csharp
public RDUniform()
```

## 属性

### Binding

```csharp
public int Binding { get; set; }
```

引擎名：`binding`

The uniform's binding.

### UniformType

```csharp
public RenderingDevice.UniformType UniformType { get; set; }
```

引擎名：`uniform_type`

The uniform's data type.

### _Ids

```csharp
public Array<Rid> _Ids { get; set; }
```

## 方法

### AddId

```csharp
public void AddId(Rid id)
```

引擎名：`add_id`

Binds the given id to the uniform. The data associated with the id is then used when the uniform is passed to a shader.

### ClearIds

```csharp
public void ClearIds()
```

引擎名：`clear_ids`

Unbinds all ids currently bound to the uniform.

### GetBinding

```csharp
public int GetBinding()
```

引擎名：`get_binding`

### GetIds

```csharp
public Array<Rid> GetIds()
```

引擎名：`get_ids`

Returns an array of all ids currently bound to the uniform.

### GetUniformType

```csharp
public RenderingDevice.UniformType GetUniformType()
```

引擎名：`get_uniform_type`

### SetBinding

```csharp
public void SetBinding(int pMember)
```

引擎名：`set_binding`

### SetUniformType

```csharp
public void SetUniformType(RenderingDevice.UniformType pMember)
```

引擎名：`set_uniform_type`
