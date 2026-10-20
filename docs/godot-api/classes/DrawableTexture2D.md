<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DrawableTexture2D

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D texture that can be modified via blit calls, copying from a target texture to itself. Primarily intended to be managed in code, a user must call `Setup()` to initialize the state before drawing. Each `BlitRect()` call takes at least a rectangle, the area to draw to, and another texture, what to be drawn. The draw calls use a Texture_Blit Shader to process and calculate the result, pixel by pixel. Users can supply their own ShaderMaterial with custom Texture_Blit shaders for more complex behaviors.

## 构造函数

### DrawableTexture2D

```csharp
public DrawableTexture2D()
```

## 方法

### BlitRect

```csharp
public void BlitRect(Rect2I rect, Texture2D source, Color? modulate = null, int mipmap = 0, Material material = null)
```

引擎名：`blit_rect`

Draws to given `rect` on this texture by copying from the given `source`. A `modulate` color can be passed in for the shader to use, but defaults to White. The `mipmap` value can specify a draw to a lower mipmap level. The `material` parameter can take a ShaderMaterial with a TextureBlit Shader for custom drawing behavior.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### BlitRectMulti

```csharp
public void BlitRectMulti(Rect2I rect, Array<Texture2D> sources, Array<DrawableTexture2D> extraTargets, Color? modulate = null, int mipmap = 0, Material material = null)
```

引擎名：`blit_rect_multi`

Draws to the given `rect` on this texture, as well as on up to 3 DrawableTexture `extraTargets`. All `extraTargets` must be the same size and DrawableFormat as the original target, otherwise the Shader may fail. Expects up to 4 Texture `sources`, but will replace missing `sources` with default Black Textures.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### GenerateMipmaps

```csharp
public void GenerateMipmaps()
```

引擎名：`generate_mipmaps`

Re-calculates the mipmaps for this texture on demand.

### GetUseMipmaps

```csharp
public bool GetUseMipmaps()
```

引擎名：`get_use_mipmaps`

Returns `true` if mipmaps are set to be used on this DrawableTexture.

### SetFormat

```csharp
public void SetFormat(DrawableTexture2D.DrawableFormat format)
```

引擎名：`set_format`

Sets the format of this DrawableTexture.

### SetUseMipmaps

```csharp
public void SetUseMipmaps(bool mipmaps)
```

引擎名：`set_use_mipmaps`

Sets if mipmaps should be used on this DrawableTexture.

### Setup

```csharp
public void Setup(int width, int height, DrawableTexture2D.DrawableFormat format, Color? color = null, bool useMipmaps = false)
```

引擎名：`setup`

Initializes the DrawableTexture to a White texture of the given `width`, `height`, and `format`.

参数：
- `color`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

## DrawableFormat

命名空间 `Godot`；enum

### 枚举值

- `Rgba8 = 0`：OpenGL texture format RGBA with four components, each with a bitdepth of 8.
- `Rgba8Srgb = 1`：OpenGL texture format RGBA with four components, each with a bitdepth of 8.
- `Rgbah = 2`：OpenGL texture format GL_RGBA16F where there are four components, each a 16-bit "half-precision" floating-point value.
- `Rgbaf = 3`：OpenGL texture format GL_RGBA32F where there are four components, each a 32-bit floating-point value.
