<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialCapabilityConfigurationPlaneTracking

命名空间 `Godot`；class；继承 [`OpenXRSpatialCapabilityConfigurationBaseHeader`](OpenXRSpatialCapabilityConfigurationBaseHeader.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Configuration header for plane tracking. Pass this to [`OpenXRSpatialEntityExtension.CreateSpatialContext`](OpenXRSpatialEntityExtension.md) to create a spatial context with plane tracking capabilities.

## 构造函数

### OpenXRSpatialCapabilityConfigurationPlaneTracking

```csharp
public OpenXRSpatialCapabilityConfigurationPlaneTracking()
```

## 方法

### GetEnabledComponents

```csharp
public long[] GetEnabledComponents()
```

引擎名：`get_enabled_components`

Returns the components enabled by this configuration.

**Note:** Only valid after this configuration was used to create a spatial context.

### SupportsLabels

```csharp
public bool SupportsLabels()
```

引擎名：`supports_labels`

Returns `true` if we support the plane semantic label component (only valid after the OpenXR session has started). You can query these using the [`OpenXRSpatialComponentPlaneSemanticLabelList`](OpenXRSpatialComponentPlaneSemanticLabelList.md) data object.

### SupportsMesh2D

```csharp
public bool SupportsMesh2D()
```

Returns `true` if we support the mesh 2D component (only valid after the OpenXR session has started). You can query these using the [`OpenXRSpatialComponentMesh2DList`](OpenXRSpatialComponentMesh2DList.md) data object.

### SupportsPolygons

```csharp
public bool SupportsPolygons()
```

引擎名：`supports_polygons`

Returns `true` if we support the polygon 2D component (only valid after the OpenXR session has started). You can query these using the [`OpenXRSpatialComponentPolygon2DList`](OpenXRSpatialComponentPolygon2DList.md) data object.
