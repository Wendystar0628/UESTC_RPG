<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectBandPassFilter

命名空间 `Godot`；class；继承 [`AudioEffectFilter`](AudioEffectFilter.md) → [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A "band-pass" filter allows the frequencies at [`AudioEffectFilter.CutoffHz`](AudioEffectFilter.md) to pass unchanged, and attenuates frequencies outside the frequency threshold. It is the opposite of [`AudioEffectBandLimitFilter`](AudioEffectBandLimitFilter.md) and [`AudioEffectNotchFilter`](AudioEffectNotchFilter.md).

This filter can be used to emulate sounds coming from weak speakers.

## 构造函数

### AudioEffectBandPassFilter

```csharp
public AudioEffectBandPassFilter()
```
