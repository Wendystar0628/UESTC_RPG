<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Lightmapper

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class should be extended by custom lightmapper classes. Lightmappers can then be used with [`LightmapGI`](LightmapGI.md) to provide fast baked global illumination in 3D.

Godot contains a built-in GPU-based lightmapper [`LightmapperRD`](LightmapperRD.md) that uses compute shaders, but custom lightmappers can be implemented by C++ modules.
