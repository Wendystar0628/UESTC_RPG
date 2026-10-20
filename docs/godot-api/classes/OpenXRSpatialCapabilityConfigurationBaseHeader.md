<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialCapabilityConfigurationBaseHeader

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Wrapper base class for OpenXR Spatial Capability Configuration headers. This class needs to be implemented for each capability configuration structure usable within OpenXR's spatial entities system.

## 构造函数

### OpenXRSpatialCapabilityConfigurationBaseHeader

```csharp
public OpenXRSpatialCapabilityConfigurationBaseHeader()
```

## 方法

### GetConfiguration

```csharp
public ulong GetConfiguration()
```

引擎名：`get_configuration`

Gets a pointer to the `XrSpatialCapabilityConfigurationBaseHeaderEXT` struct.

**Note:** This method is intended to be used from GDExtensions.

### HasValidConfiguration

```csharp
public bool HasValidConfiguration()
```

引擎名：`has_valid_configuration`

Returns `true` if this object contains a valid configuration that can be used when calling [`OpenXRSpatialEntityExtension.CreateSpatialContext`](OpenXRSpatialEntityExtension.md).

### _GetConfiguration

```csharp
public virtual ulong _GetConfiguration()
```

引擎名：`_get_configuration`

Return a pointer (encoded as an `int64_t`) to a struct holding the spatial capability configuration data. The memory for this struct should remain accessible as long as this object remains instantiated.

### _HasValidConfiguration

```csharp
public virtual bool _HasValidConfiguration()
```

引擎名：`_has_valid_configuration`

Return `true` if this object contains a valid configuration that can be retrieved when calling `_GetConfiguration()`.
