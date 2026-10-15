<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRStructureBase

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing OpenXR structure data that is passed when calling into OpenXR APIs.

## 构造函数

### OpenXRStructureBase

```csharp
public OpenXRStructureBase()
```

## 属性

### Next

```csharp
public OpenXRStructureBase Next { get; set; }
```

引擎名：`next`

Setting another structure object here chains these structures together to extend the API functionality. Consult the OpenXR documentation for which structures can be used with a given API call.

## 方法

### GetNext

```csharp
public OpenXRStructureBase GetNext()
```

引擎名：`get_next`

### GetStructureType

```csharp
public ulong GetStructureType()
```

引擎名：`get_structure_type`

Returns the structure type (OpenXR `XrStructureType`) used for this structure.

### SetNext

```csharp
public void SetNext(OpenXRStructureBase entity)
```

引擎名：`set_next`

### _GetHeader

```csharp
public virtual ulong _GetHeader(ulong next)
```

引擎名：`_get_header`
