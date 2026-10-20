<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OptimizedTranslation

命名空间 `Godot`；class；继承 [`Translation`](Translation.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An optimized translation. Uses real-time compressed translations, which results in very small dictionaries.

This class does not store the untranslated strings for optimization purposes. Therefore, [`Translation.GetMessageList`](Translation.md) always returns an empty array, and [`Translation.GetMessageCount`](Translation.md) always returns `0`.

## 构造函数

### OptimizedTranslation

```csharp
public OptimizedTranslation()
```

## 方法

### Generate

```csharp
public bool Generate(Translation from)
```

引擎名：`generate`

Generates and sets an optimized translation from the given [`Translation`](Translation.md) resource. Returns `true` if successful.

**Note:** Messages in `from` should not use context or plural forms.

**Note:** This method is intended to be used in the editor. It does nothing when called from an exported project.
