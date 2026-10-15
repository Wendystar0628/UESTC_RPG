<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDAccelerationStructureInstance

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`RDAccelerationStructureInstance` describes an instance of a Bottom-Level Acceleration Structure (BLAS) used in the [`RenderingDevice.TlasBuild`](RenderingDevice.md) method.

## 构造函数

### RDAccelerationStructureInstance

```csharp
public RDAccelerationStructureInstance()
```

## 属性

### Blas

```csharp
public Rid Blas { get; set; }
```

引擎名：`blas`

The BLAS referenced by this instance. If `null`, the instance is treated as a placeholder but still contributes to `gl_InstanceIndex` in GLSL.

### Flags

```csharp
public RenderingDevice.AccelerationStructureInstanceFlagBits Flags { get; set; }
```

引擎名：`flags`

Flags for the instance.

### HitSbtRange

```csharp
public long HitSbtRange { get; set; }
```

引擎名：`hit_sbt_range`

Hit shader binding table range used for this instance, allocated using the [`RenderingDevice.HitSbtRangeAlloc`](RenderingDevice.md) method.

### Id

```csharp
public uint Id { get; set; }
```

引擎名：`id`

Custom instance ID that can be accessed in GLSL using `gl_InstanceCustomIndexEXT`.

### Mask

```csharp
public byte Mask { get; set; }
```

引擎名：`mask`

Visibility mask used to control which rays can intersect this instance.

### Transform

```csharp
public Transform3D Transform { get; set; }
```

引擎名：`transform`

Transform applied to the referenced BLAS for this instance.

## 方法

### GetBlas

```csharp
public Rid GetBlas()
```

引擎名：`get_blas`

### GetFlags

```csharp
public RenderingDevice.AccelerationStructureInstanceFlagBits GetFlags()
```

引擎名：`get_flags`

### GetHitSbtRange

```csharp
public long GetHitSbtRange()
```

引擎名：`get_hit_sbt_range`

### GetId

```csharp
public uint GetId()
```

引擎名：`get_id`

### GetMask

```csharp
public byte GetMask()
```

引擎名：`get_mask`

### GetTransform

```csharp
public Transform3D GetTransform()
```

引擎名：`get_transform`

### SetBlas

```csharp
public void SetBlas(Rid pMember)
```

引擎名：`set_blas`

### SetFlags

```csharp
public void SetFlags(RenderingDevice.AccelerationStructureInstanceFlagBits pMember)
```

引擎名：`set_flags`

### SetHitSbtRange

```csharp
public void SetHitSbtRange(long pMember)
```

引擎名：`set_hit_sbt_range`

### SetId

```csharp
public void SetId(uint pMember)
```

引擎名：`set_id`

### SetMask

```csharp
public void SetMask(byte pMember)
```

引擎名：`set_mask`

### SetTransform

```csharp
public void SetTransform(Transform3D pMember)
```

引擎名：`set_transform`
