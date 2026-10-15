<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRSpatialCapabilityConfigurationQrCode

命名空间 `Godot`；class；继承 [`OpenXRSpatialCapabilityConfigurationBaseHeader`](OpenXRSpatialCapabilityConfigurationBaseHeader.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Configuration header for micro QR code markers. Pass this to [`OpenXRSpatialEntityExtension.CreateSpatialContext`](OpenXRSpatialEntityExtension.md) to create a spatial context that can detect micro QR code markers.

## 构造函数

### OpenXRSpatialCapabilityConfigurationQrCode

```csharp
public OpenXRSpatialCapabilityConfigurationQrCode()
```

## 方法

### GetEnabledComponents

```csharp
public long[] GetEnabledComponents()
```

引擎名：`get_enabled_components`

Returns the components enabled by this configuration.

**Note:** Only valid after this configuration was used to create a spatial context.
