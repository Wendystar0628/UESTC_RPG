<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeIf

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This visual shader node has six input ports:

- Port **1** and **2** provide the two floating-point numbers `a` and `b` that will be compared.

- Port **3** is the tolerance, which allows similar floating-point numbers to be considered equal.

- Ports **4**, **5**, and **6** are the possible outputs, returned if `a == b`, `a > b`, or `a < b` respectively.

## 构造函数

### VisualShaderNodeIf

```csharp
public VisualShaderNodeIf()
```
