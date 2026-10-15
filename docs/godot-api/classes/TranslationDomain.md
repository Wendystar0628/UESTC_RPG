<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TranslationDomain

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`TranslationDomain` is a self-contained collection of [`Translation`](Translation.md) resources. Translations can be added to or removed from it.

If you're working with the main translation domain, it is more convenient to use the wrap methods on [`TranslationServer`](TranslationServer.md).

## 构造函数

### TranslationDomain

```csharp
public TranslationDomain()
```

## 属性

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

If `true`, translation is enabled. Otherwise, `Translate()` and `TranslatePlural()` will return the input message unchanged regardless of the current locale.

### PseudolocalizationAccentsEnabled

```csharp
public bool PseudolocalizationAccentsEnabled { get; set; }
```

引擎名：`pseudolocalization_accents_enabled`

Replace all characters with their accented variants during pseudolocalization.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

### PseudolocalizationDoubleVowelsEnabled

```csharp
public bool PseudolocalizationDoubleVowelsEnabled { get; set; }
```

引擎名：`pseudolocalization_double_vowels_enabled`

Double vowels in strings during pseudolocalization to simulate the lengthening of text due to localization.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

### PseudolocalizationEnabled

```csharp
public bool PseudolocalizationEnabled { get; set; }
```

引擎名：`pseudolocalization_enabled`

If `true`, enables pseudolocalization for the project. This can be used to spot untranslatable strings or layout issues that may occur once the project is localized to languages that have longer strings than the source language.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

### PseudolocalizationExpansionRatio

```csharp
public float PseudolocalizationExpansionRatio { get; set; }
```

引擎名：`pseudolocalization_expansion_ratio`

The expansion ratio to use during pseudolocalization. A value of `0.3` is sufficient for most practical purposes, and will increase the length of each string by 30%.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

### PseudolocalizationFakeBidiEnabled

```csharp
public bool PseudolocalizationFakeBidiEnabled { get; set; }
```

引擎名：`pseudolocalization_fake_bidi_enabled`

If `true`, emulate bidirectional (right-to-left) text when pseudolocalization is enabled. This can be used to spot issues with RTL layout and UI mirroring that will crop up if the project is localized to RTL languages such as Arabic or Hebrew.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

### PseudolocalizationOverrideEnabled

```csharp
public bool PseudolocalizationOverrideEnabled { get; set; }
```

引擎名：`pseudolocalization_override_enabled`

Replace all characters in the string with `*`. Useful for finding non-localizable strings.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

### PseudolocalizationPrefix

```csharp
public string PseudolocalizationPrefix { get; set; }
```

引擎名：`pseudolocalization_prefix`

Prefix that will be prepended to the pseudolocalized string.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

### PseudolocalizationSkipPlaceholdersEnabled

```csharp
public bool PseudolocalizationSkipPlaceholdersEnabled { get; set; }
```

引擎名：`pseudolocalization_skip_placeholders_enabled`

Skip placeholders for string formatting like `%s` or `%f` during pseudolocalization. Useful to identify strings which need additional control characters to display correctly.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

### PseudolocalizationSuffix

```csharp
public string PseudolocalizationSuffix { get; set; }
```

引擎名：`pseudolocalization_suffix`

Suffix that will be appended to the pseudolocalized string.

**Note:** Updating this property does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) notification manually after you have finished modifying pseudolocalization related options.

## 方法

### AddTranslation

```csharp
public void AddTranslation(Translation translation)
```

引擎名：`add_translation`

Adds a translation.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Removes all translations.

### FindTranslations

```csharp
public Array<Translation> FindTranslations(string locale, bool exact)
```

引擎名：`find_translations`

Returns the [`Translation`](Translation.md) instances that match `locale` (see [`TranslationServer.CompareLocales`](TranslationServer.md)). If `exact` is `true`, only instances whose locale exactly equals `locale` will be returned.

### GetLocaleOverride

```csharp
public string GetLocaleOverride()
```

引擎名：`get_locale_override`

Returns the locale override of the domain. Returns an empty string if locale override is disabled.

### GetPseudolocalizationExpansionRatio

```csharp
public float GetPseudolocalizationExpansionRatio()
```

引擎名：`get_pseudolocalization_expansion_ratio`

### GetPseudolocalizationPrefix

```csharp
public string GetPseudolocalizationPrefix()
```

引擎名：`get_pseudolocalization_prefix`

### GetPseudolocalizationSuffix

```csharp
public string GetPseudolocalizationSuffix()
```

引擎名：`get_pseudolocalization_suffix`

### GetTranslationObject

```csharp
public Translation GetTranslationObject(string locale)
```

引擎名：`get_translation_object`

Returns the [`Translation`](Translation.md) instance that best matches `locale`. Returns `null` if there are no matches.

### GetTranslations

```csharp
public Array<Translation> GetTranslations()
```

引擎名：`get_translations`

Returns all available [`Translation`](Translation.md) instances as added by `AddTranslation()`.

### HasTranslation

```csharp
public bool HasTranslation(Translation translation)
```

引擎名：`has_translation`

Returns `true` if this translation domain contains the given `translation`.

### HasTranslationForLocale

```csharp
public bool HasTranslationForLocale(string locale, bool exact)
```

引擎名：`has_translation_for_locale`

Returns `true` if there are any [`Translation`](Translation.md) instances that match `locale` (see [`TranslationServer.CompareLocales`](TranslationServer.md)). If `exact` is `true`, only instances whose locale exactly equals `locale` are considered.

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### IsPseudolocalizationAccentsEnabled

```csharp
public bool IsPseudolocalizationAccentsEnabled()
```

引擎名：`is_pseudolocalization_accents_enabled`

### IsPseudolocalizationDoubleVowelsEnabled

```csharp
public bool IsPseudolocalizationDoubleVowelsEnabled()
```

引擎名：`is_pseudolocalization_double_vowels_enabled`

### IsPseudolocalizationEnabled

```csharp
public bool IsPseudolocalizationEnabled()
```

引擎名：`is_pseudolocalization_enabled`

### IsPseudolocalizationFakeBidiEnabled

```csharp
public bool IsPseudolocalizationFakeBidiEnabled()
```

引擎名：`is_pseudolocalization_fake_bidi_enabled`

### IsPseudolocalizationOverrideEnabled

```csharp
public bool IsPseudolocalizationOverrideEnabled()
```

引擎名：`is_pseudolocalization_override_enabled`

### IsPseudolocalizationSkipPlaceholdersEnabled

```csharp
public bool IsPseudolocalizationSkipPlaceholdersEnabled()
```

引擎名：`is_pseudolocalization_skip_placeholders_enabled`

### Pseudolocalize

```csharp
public StringName Pseudolocalize(StringName message)
```

引擎名：`pseudolocalize`

Returns the pseudolocalized string based on the `message` passed in.

### RemoveTranslation

```csharp
public void RemoveTranslation(Translation translation)
```

引擎名：`remove_translation`

Removes the given translation.

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetLocaleOverride

```csharp
public void SetLocaleOverride(string locale)
```

引擎名：`set_locale_override`

Sets the locale override of the domain.

If `locale` is an empty string, locale override is disabled. Otherwise, `locale` will be standardized to match known locales (e.g. `en-US` would be matched to `en_US`).

**Note:** Calling this method does not automatically update texts in the scene tree. Please propagate the [`MainLoop.NotificationTranslationChanged`](MainLoop.md) signal manually.

### SetPseudolocalizationAccentsEnabled

```csharp
public void SetPseudolocalizationAccentsEnabled(bool enabled)
```

引擎名：`set_pseudolocalization_accents_enabled`

### SetPseudolocalizationDoubleVowelsEnabled

```csharp
public void SetPseudolocalizationDoubleVowelsEnabled(bool enabled)
```

引擎名：`set_pseudolocalization_double_vowels_enabled`

### SetPseudolocalizationEnabled

```csharp
public void SetPseudolocalizationEnabled(bool enabled)
```

引擎名：`set_pseudolocalization_enabled`

### SetPseudolocalizationExpansionRatio

```csharp
public void SetPseudolocalizationExpansionRatio(float ratio)
```

引擎名：`set_pseudolocalization_expansion_ratio`

### SetPseudolocalizationFakeBidiEnabled

```csharp
public void SetPseudolocalizationFakeBidiEnabled(bool enabled)
```

引擎名：`set_pseudolocalization_fake_bidi_enabled`

### SetPseudolocalizationOverrideEnabled

```csharp
public void SetPseudolocalizationOverrideEnabled(bool enabled)
```

引擎名：`set_pseudolocalization_override_enabled`

### SetPseudolocalizationPrefix

```csharp
public void SetPseudolocalizationPrefix(string prefix)
```

引擎名：`set_pseudolocalization_prefix`

### SetPseudolocalizationSkipPlaceholdersEnabled

```csharp
public void SetPseudolocalizationSkipPlaceholdersEnabled(bool enabled)
```

引擎名：`set_pseudolocalization_skip_placeholders_enabled`

### SetPseudolocalizationSuffix

```csharp
public void SetPseudolocalizationSuffix(string suffix)
```

引擎名：`set_pseudolocalization_suffix`

### Translate

```csharp
public StringName Translate(StringName message, StringName context = null)
```

引擎名：`translate`

Returns the current locale's translation for the given message and context.

### TranslatePlural

```csharp
public StringName TranslatePlural(StringName message, StringName messagePlural, int n, StringName context = null)
```

引擎名：`translate_plural`

Returns the current locale's translation for the given message, plural message and context.

The number `n` is the number or quantity of the plural object. It will be used to guide the translation system to fetch the correct plural form for the selected language.
