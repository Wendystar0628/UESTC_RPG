<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderSceneBuffers

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Abstract scene buffers object, created for each viewport for which 3D rendering is done. It manages any additional buffers used during rendering and will discard buffers when the viewport is resized. See also [`RenderSceneBuffersRD`](RenderSceneBuffersRD.md).

**Note:** This is an internal rendering server object. Do not instantiate this class from a script.

## 方法

### Configure

```csharp
public void Configure(RenderSceneBuffersConfiguration config)
```

引擎名：`configure`

This method is called by the rendering server when the associated viewport's configuration is changed. It will discard the old buffers and recreate the internal buffers used.
