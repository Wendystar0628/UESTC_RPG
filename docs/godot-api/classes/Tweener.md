<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Tweener

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Tweeners are objects that perform a specific animating task, e.g. interpolating a property or calling a method at a given time. A `Tweener` can't be created manually, you need to use a dedicated method from [`Tween`](Tween.md).

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

引擎名：`finished`

Emitted when the `Tweener` has just finished its job or became invalid (e.g. due to a freed object).
