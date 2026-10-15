<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDTextureView

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDTextureView

```csharp
public RDTextureView()
```

## 属性

### FormatOverride

```csharp
public RenderingDevice.DataFormat FormatOverride { get; set; }
```

引擎名：`format_override`

Optional override for the data format to return sampled values in. The corresponding [`RDTextureFormat`](RDTextureFormat.md) must have had this added as a shareable format. The default value of [`RenderingDevice.DataFormat.Max`](RenderingDevice.md) does not override the format.

### SwizzleA

```csharp
public RenderingDevice.TextureSwizzle SwizzleA { get; set; }
```

引擎名：`swizzle_a`

The channel to sample when sampling the alpha channel.

### SwizzleB

```csharp
public RenderingDevice.TextureSwizzle SwizzleB { get; set; }
```

引擎名：`swizzle_b`

The channel to sample when sampling the blue color channel.

### SwizzleG

```csharp
public RenderingDevice.TextureSwizzle SwizzleG { get; set; }
```

引擎名：`swizzle_g`

The channel to sample when sampling the green color channel.

### SwizzleR

```csharp
public RenderingDevice.TextureSwizzle SwizzleR { get; set; }
```

引擎名：`swizzle_r`

The channel to sample when sampling the red color channel.

## 方法

### GetFormatOverride

```csharp
public RenderingDevice.DataFormat GetFormatOverride()
```

引擎名：`get_format_override`

### GetSwizzleA

```csharp
public RenderingDevice.TextureSwizzle GetSwizzleA()
```

引擎名：`get_swizzle_a`

### GetSwizzleB

```csharp
public RenderingDevice.TextureSwizzle GetSwizzleB()
```

引擎名：`get_swizzle_b`

### GetSwizzleG

```csharp
public RenderingDevice.TextureSwizzle GetSwizzleG()
```

引擎名：`get_swizzle_g`

### GetSwizzleR

```csharp
public RenderingDevice.TextureSwizzle GetSwizzleR()
```

引擎名：`get_swizzle_r`

### SetFormatOverride

```csharp
public void SetFormatOverride(RenderingDevice.DataFormat pMember)
```

引擎名：`set_format_override`

### SetSwizzleA

```csharp
public void SetSwizzleA(RenderingDevice.TextureSwizzle pMember)
```

引擎名：`set_swizzle_a`

### SetSwizzleB

```csharp
public void SetSwizzleB(RenderingDevice.TextureSwizzle pMember)
```

引擎名：`set_swizzle_b`

### SetSwizzleG

```csharp
public void SetSwizzleG(RenderingDevice.TextureSwizzle pMember)
```

引擎名：`set_swizzle_g`

### SetSwizzleR

```csharp
public void SetSwizzleR(RenderingDevice.TextureSwizzle pMember)
```

引擎名：`set_swizzle_r`
