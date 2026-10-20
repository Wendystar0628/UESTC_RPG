<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDAttachmentFormat

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDAttachmentFormat

```csharp
public RDAttachmentFormat()
```

## 属性

### Format

```csharp
public RenderingDevice.DataFormat Format { get; set; }
```

引擎名：`format`

The attachment's data format.

### Samples

```csharp
public RenderingDevice.TextureSamples Samples { get; set; }
```

引擎名：`samples`

The number of samples used when sampling the attachment.

### UsageFlags

```csharp
public uint UsageFlags { get; set; }
```

引擎名：`usage_flags`

The attachment's usage flags, which determine what can be done with it.

## 方法

### GetFormat

```csharp
public RenderingDevice.DataFormat GetFormat()
```

引擎名：`get_format`

### GetSamples

```csharp
public RenderingDevice.TextureSamples GetSamples()
```

引擎名：`get_samples`

### GetUsageFlags

```csharp
public uint GetUsageFlags()
```

引擎名：`get_usage_flags`

### SetFormat

```csharp
public void SetFormat(RenderingDevice.DataFormat pMember)
```

引擎名：`set_format`

### SetSamples

```csharp
public void SetSamples(RenderingDevice.TextureSamples pMember)
```

引擎名：`set_samples`

### SetUsageFlags

```csharp
public void SetUsageFlags(uint pMember)
```

引擎名：`set_usage_flags`
