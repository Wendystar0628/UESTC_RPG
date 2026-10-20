<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialComponentData

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Object for storing OpenXR spatial entity component data.

## 构造函数

### OpenXRSpatialComponentData

```csharp
public OpenXRSpatialComponentData()
```

## 方法

### GetComponentType

```csharp
public long GetComponentType()
```

引擎名：`get_component_type`

Gets this `OpenXRSpatialComponentData`'s `XrSpatialComponentTypeEXT`.

### SetCapacity

```csharp
public void SetCapacity(uint capacity)
```

引擎名：`set_capacity`

Sets the expected capacity as provided by the spatial entities query system. Buffers should be initialized with the correct storage.

### _GetComponentType

```csharp
public virtual ulong _GetComponentType()
```

引擎名：`_get_component_type`

Return the component type for the component we store data for.

### _GetStructureData

```csharp
public virtual ulong _GetStructureData(ulong next)
```

引擎名：`_get_structure_data`

Return a pointer to the structure data that will be submitted along with the snapshot query. This pointer must remain valid as long as this object is instantiated.

### _SetCapacity

```csharp
public virtual void _SetCapacity(uint capacity)
```

引擎名：`_set_capacity`

Sets the expected capacity as provided by the spatial entities query system. Buffers should be initialized with the correct storage.
