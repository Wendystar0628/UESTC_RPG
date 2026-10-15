<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PlaceholderCubemap

命名空间 `Godot`；class；继承 [`PlaceholderTextureLayered`](PlaceholderTextureLayered.md) → [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class replaces a [`Cubemap`](Cubemap.md) or a [`Cubemap`](Cubemap.md)-derived class in 2 conditions:

- In dedicated server mode, where the image data shouldn't affect game logic. This allows reducing the exported PCK's size significantly.

- When the [`Cubemap`](Cubemap.md)-derived class is missing, for example when using a different engine version.

**Note:** This class is not intended for rendering or for use in shaders. Operations like calculating UV are not guaranteed to work.

## 构造函数

### PlaceholderCubemap

```csharp
public PlaceholderCubemap()
```
