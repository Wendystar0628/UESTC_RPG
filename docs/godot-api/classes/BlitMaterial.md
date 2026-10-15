<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BlitMaterial

命名空间 `Godot`；class；继承 [`Material`](Material.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A material resource that can be used by DrawableTextures when processing blit calls to draw.

## 构造函数

### BlitMaterial

```csharp
public BlitMaterial()
```

## 属性

### BlendMode

```csharp
public BlitMaterial.BlendModeEnum BlendMode { get; set; }
```

引擎名：`blend_mode`

The manner in which the newly blitted texture is blended with the original DrawableTexture.

## 方法

### GetBlendMode

```csharp
public BlitMaterial.BlendModeEnum GetBlendMode()
```

引擎名：`get_blend_mode`

### SetBlendMode

```csharp
public void SetBlendMode(BlitMaterial.BlendModeEnum blendMode)
```

引擎名：`set_blend_mode`

## BlendModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Mix = 0`：Mix blending mode.
- `Add = 1`：Additive blending mode.
- `Sub = 2`：Subtractive blending mode.
- `Mul = 3`：Multiplicative blending mode.
- `Disabled = 4`：No blending mode, direct color copy.
