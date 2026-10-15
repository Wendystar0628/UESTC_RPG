<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialCapabilityConfigurationAruco

命名空间 `Godot`；class；继承 [`OpenXRSpatialCapabilityConfigurationBaseHeader`](OpenXRSpatialCapabilityConfigurationBaseHeader.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Configuration header for Aruco markers. Pass this to [`OpenXRSpatialEntityExtension.CreateSpatialContext`](OpenXRSpatialEntityExtension.md) to create a spatial context that can detect Aruco markers.

## 构造函数

### OpenXRSpatialCapabilityConfigurationAruco

```csharp
public OpenXRSpatialCapabilityConfigurationAruco()
```

## 属性

### ArucoDict

```csharp
public OpenXRSpatialCapabilityConfigurationAruco.ArucoDictEnum ArucoDict { get; set; }
```

引擎名：`aruco_dict`

Dictionary to use to decode Aruco markers.

**Note:** Must be set before using this configuration to create a spatial context.

## 方法

### GetArucoDict

```csharp
public OpenXRSpatialCapabilityConfigurationAruco.ArucoDictEnum GetArucoDict()
```

引擎名：`get_aruco_dict`

### GetEnabledComponents

```csharp
public long[] GetEnabledComponents()
```

引擎名：`get_enabled_components`

Returns the components enabled by this configuration.

**Note:** Only valid after this configuration was used to create a spatial context.

### SetArucoDict

```csharp
public void SetArucoDict(OpenXRSpatialCapabilityConfigurationAruco.ArucoDictEnum arucoDict)
```

引擎名：`set_aruco_dict`

## ArucoDictEnum

命名空间 `Godot`；enum

### 枚举值

- `Dict4X450 = 1`：4 by 4 pixel Aruco marker dictionary with 50 IDs.
- `Dict4X4100 = 2`：4 by 4 pixel Aruco marker dictionary with 100 IDs.
- `Dict4X4250 = 3`：4 by 4 pixel Aruco marker dictionary with 250 IDs.
- `Dict4X41000 = 4`：4 by 4 pixel Aruco marker dictionary with 1000 IDs.
- `Dict5X550 = 5`：5 by 5 pixel Aruco marker dictionary with 50 IDs.
- `Dict5X5100 = 6`：5 by 5 pixel Aruco marker dictionary with 100 IDs.
- `Dict5X5250 = 7`：5 by 5 pixel Aruco marker dictionary with 250 IDs.
- `Dict5X51000 = 8`：5 by 5 pixel Aruco marker dictionary with 1000 IDs.
- `Dict6X650 = 9`：6 by 6 pixel Aruco marker dictionary with 50 IDs.
- `Dict6X6100 = 10`：6 by 6 pixel Aruco marker dictionary with 100 IDs.
- `Dict6X6250 = 11`：6 by 6 pixel Aruco marker dictionary with 250 IDs.
- `Dict6X61000 = 12`：6 by 6 pixel Aruco marker dictionary with 1000 IDs.
- `Dict7X750 = 13`：7 by 7 pixel Aruco marker dictionary with 50 IDs.
- `Dict7X7100 = 14`：7 by 7 pixel Aruco marker dictionary with 100 IDs.
- `Dict7X7250 = 15`：7 by 7 pixel Aruco marker dictionary with 250 IDs.
- `Dict7X71000 = 16`：7 by 7 pixel Aruco marker dictionary with 1000 IDs.
