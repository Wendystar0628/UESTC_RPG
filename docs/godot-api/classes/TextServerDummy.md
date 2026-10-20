<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextServerDummy

命名空间 `Godot`；class；继承 [`TextServerExtension`](TextServerExtension.md) → [`TextServer`](TextServer.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A dummy [`TextServer`](TextServer.md) interface that doesn't do anything. Useful for freeing up memory when rendering text is not needed, as text servers are resource-intensive. It can also be used for performance comparisons in complex GUIs to check the impact of text rendering.

A dummy text server is always available at the start of a project. Here's how to access it:

```csharp
var dummy_text_server = TextServerManager.find_interface("Dummy")
if dummy_text_server != null:
TextServerManager.set_primary_interface(dummy_text_server)
# If the other text servers are unneeded, they can be removed:
for i in TextServerManager.get_interface_count():
var text_server = TextServerManager.get_interface(i)
if text_server != dummy_text_server:
TextServerManager.remove_interface(text_server)

```

The command line argument `--text-driver Dummy` (case-sensitive) can be used to force the "Dummy" [`TextServer`](TextServer.md) on any project.

## 构造函数

### TextServerDummy

```csharp
public TextServerDummy()
```
