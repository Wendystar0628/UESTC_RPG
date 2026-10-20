<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PlaceholderCubemapArray

命名空间 `Godot`；class；继承 [`PlaceholderTextureLayered`](PlaceholderTextureLayered.md) → [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class replaces a [`CubemapArray`](CubemapArray.md) or a [`CubemapArray`](CubemapArray.md)-derived class in 2 conditions:

- In dedicated server mode, where the image data shouldn't affect game logic. This allows reducing the exported PCK's size significantly.

- When the [`CubemapArray`](CubemapArray.md)-derived class is missing, for example when using a different engine version.

**Note:** This class is not intended for rendering or for use in shaders. Operations like calculating UV are not guaranteed to work.

## 构造函数

### PlaceholderCubemapArray

```csharp
public PlaceholderCubemapArray()
```
