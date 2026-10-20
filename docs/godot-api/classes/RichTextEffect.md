<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RichTextEffect

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A custom effect for a [`RichTextLabel`](RichTextLabel.md), which can be loaded in the [`RichTextLabel`](RichTextLabel.md) inspector or using [`RichTextLabel.InstallEffect`](RichTextLabel.md).

**Note:** For a `RichTextEffect` to be usable, a BBCode tag must be defined as a member variable called `bbcode` in the script.

```csharp
// The RichTextEffect will be usable like this: `[example]Some text[/example]`
string bbcode = "example";

```

**Note:** As soon as a [`RichTextLabel`](RichTextLabel.md) contains at least one `RichTextEffect`, it will continuously process the effect unless the project is paused. This may impact battery life negatively.

## 构造函数

### RichTextEffect

```csharp
public RichTextEffect()
```

## 方法

### _ProcessCustomFX

```csharp
public virtual bool _ProcessCustomFX(CharFXTransform charFX)
```

引擎名：`_process_custom_fx`

Override this method to modify properties in `charFX`. The method must return `true` if the character could be transformed successfully. If the method returns `false`, it will skip transformation to avoid displaying broken text.
