<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Cubemap

命名空间 `Godot`；class；继承 [`ImageTextureLayered`](ImageTextureLayered.md) → [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A cubemap is made of 6 textures organized in layers. They are typically used for faking reflections in 3D rendering (see [`ReflectionProbe`](ReflectionProbe.md)). It can be used to make an object look as if it's reflecting its surroundings. This usually delivers much better performance than other reflection methods.

This resource is typically used as a uniform in custom shaders. Few core Godot methods make use of `Cubemap` resources.

To create such a texture file yourself, reimport your image files using the Godot Editor import presets. To create a Cubemap from code, use [`ImageTextureLayered.CreateFromImages`](ImageTextureLayered.md) on an instance of the Cubemap class.

The expected image order is X+, X-, Y+, Y-, Z+, Z- (in Godot's coordinate system, so Y+ is "up" and Z- is "forward"). You can use one of the following templates as a base:

- [2×3 cubemap template (default layout option)](https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/assets_pipeline/img/cubemap_template_2x3.webp)

- [3×2 cubemap template](https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/assets_pipeline/img/cubemap_template_3x2.webp)

- [1×6 cubemap template](https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/assets_pipeline/img/cubemap_template_1x6.webp)

- [6×1 cubemap template](https://raw.githubusercontent.com/godotengine/godot-docs/master/tutorials/assets_pipeline/img/cubemap_template_6x1.webp)

**Note:** Godot doesn't support using cubemaps in a [`PanoramaSkyMaterial`](PanoramaSkyMaterial.md). To use a cubemap as a skybox, convert the default [`PanoramaSkyMaterial`](PanoramaSkyMaterial.md) to a [`ShaderMaterial`](ShaderMaterial.md) using the **Convert to ShaderMaterial** resource dropdown option, then replace its code with the following:

```csharp
shader_type sky;

uniform samplerCube source_panorama : filter_linear, source_color, hint_default_black;
uniform float exposure : hint_range(0, 128) = 1.0;

void sky() {
// If importing a cubemap from another engine, you may need to flip one of the `EYEDIR` components below
// by replacing it with `-EYEDIR`.
vec3 eyedir = vec3(EYEDIR.x, EYEDIR.y, EYEDIR.z);
COLOR = texture(source_panorama, eyedir).rgb * exposure;
}

```

After replacing the shader code and saving, specify the imported Cubemap resource in the Shader Parameters section of the ShaderMaterial in the inspector.

Alternatively, you can use [this tool](https://danilw.github.io/GLSL-howto/cubemap_to_panorama_js/cubemap_to_panorama.html) to convert a cubemap to an equirectangular sky map and use [`PanoramaSkyMaterial`](PanoramaSkyMaterial.md) as usual.

## 构造函数

### Cubemap

```csharp
public Cubemap()
```

## 方法

### CreatePlaceholder

```csharp
public Resource CreatePlaceholder()
```

引擎名：`create_placeholder`

Creates a placeholder version of this resource ([`PlaceholderCubemap`](PlaceholderCubemap.md)).
