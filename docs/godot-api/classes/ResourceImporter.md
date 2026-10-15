<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceImporter

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This is the base class for Godot's resource importers. To implement your own resource importers using editor plugins, see `EditorImportPlugin`.

## 方法

### _GetBuildDependencies

```csharp
public virtual string[] _GetBuildDependencies(string path)
```

引擎名：`_get_build_dependencies`

Called when the engine compilation profile editor wants to check what build options an imported resource needs. For example, `ResourceImporterDynamicFont` has a property called `ResourceImporterDynamicFont.multichannel_signed_distance_field`, that depends on the engine to be build with the "msdfgen" module. If that resource happened to be a custom one, it would be handled like this:

```csharp
func _get_build_dependencies(path):
var resource = load(path)
var dependencies = PackedStringArray()

if resource.multichannel_signed_distance_field:
dependencies.push_back("module_msdfgen_enabled")

return dependencies

```

## ImportOrder

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：The default import order.
- `Scene = 100`：The import order for scenes, which ensures scenes are imported *after* all other core resources such as textures.
