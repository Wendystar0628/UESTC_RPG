<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialCapabilityConfigurationAprilTag

命名空间 `Godot`；class；继承 [`OpenXRSpatialCapabilityConfigurationBaseHeader`](OpenXRSpatialCapabilityConfigurationBaseHeader.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Configuration header for April tag markers. Pass this to [`OpenXRSpatialEntityExtension.CreateSpatialContext`](OpenXRSpatialEntityExtension.md) to create a spatial context that can detect April tags.

## 构造函数

### OpenXRSpatialCapabilityConfigurationAprilTag

```csharp
public OpenXRSpatialCapabilityConfigurationAprilTag()
```

## 属性

### AprilDict

```csharp
public OpenXRSpatialCapabilityConfigurationAprilTag.AprilTagDict AprilDict { get; set; }
```

引擎名：`april_dict`

Dictionary to use to decode April tags.

**Note:** Must be set before using this configuration to create a spatial context.

## 方法

### GetAprilDict

```csharp
public OpenXRSpatialCapabilityConfigurationAprilTag.AprilTagDict GetAprilDict()
```

引擎名：`get_april_dict`

### GetEnabledComponents

```csharp
public long[] GetEnabledComponents()
```

引擎名：`get_enabled_components`

Returns the components enabled by this configuration.

**Note:** Only valid after this configuration was used to create a spatial context.

### SetAprilDict

```csharp
public void SetAprilDict(OpenXRSpatialCapabilityConfigurationAprilTag.AprilTagDict aprilDict)
```

引擎名：`set_april_dict`

## AprilTagDict

命名空间 `Godot`；enum

### 枚举值

- `Dict16H5 = 1`：4 by 4 bits, minimum Hamming distance between any two codes = 5, 30 codes.
- `Dict25H9 = 2`：5 by 5 bits, minimum Hamming distance between any two codes = 9, 35 codes.
- `Dict36H10 = 3`：6 by 6 bits, minimum Hamming distance between any two codes = 10, 2320 codes.
- `Dict36H11 = 4`：6 by 6 bits, minimum Hamming distance between any two codes = 11, 587 codes.
