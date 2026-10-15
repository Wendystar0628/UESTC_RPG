<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TranslationServerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The translation server is the API backend that manages all language translations.

Translations are stored in [`TranslationDomain`](TranslationDomain.md)s, which can be accessed by name. The most commonly used translation domain is the main translation domain. It always exists and can be accessed using an empty [`StringName`](StringName.md). The translation server provides wrapper methods for accessing the main translation domain directly, without having to fetch the translation domain first. Custom translation domains are mainly for advanced usages like editor plugins. Names starting with `godot.` are reserved for engine internals.

## 属性

### PseudolocalizationEnabled

```csharp
public bool PseudolocalizationEnabled { get; set; }
```

If `true`, enables the use of pseudolocalization on the main translation domain. See `ProjectSettings.internationalization/pseudolocalization/use_pseudolocalization` for details.

## 方法

### AddTranslation

```csharp
public void AddTranslation(Translation translation)
```

Adds a translation to the main translation domain.

### Clear

```csharp
public void Clear()
```

Removes all translations from the main translation domain.

### CompareLocales

```csharp
public int CompareLocales(string localeA, string localeB)
```

Compares two locales and returns a similarity score between `0` (no match) and `10` (full match).

### FindTranslations

```csharp
public Array<Translation> FindTranslations(string locale, bool exact)
```

Returns the [`Translation`](Translation.md) instances in the main translation domain that match `locale` (see `CompareLocales()`). If `exact` is `true`, only instances whose locale exactly equals `locale` will be returned.

### FormatNumber

```csharp
public string FormatNumber(string number, string locale)
```

Converts a number from Western Arabic (0..9) to the numeral system used in the given `locale`.

### GetAllCountries

```csharp
public string[] GetAllCountries()
```

Returns an array of known country codes.

### GetAllLanguages

```csharp
public string[] GetAllLanguages()
```

Returns array of known language codes.

### GetAllScripts

```csharp
public string[] GetAllScripts()
```

Returns an array of known script codes.

### GetCountryName

```csharp
public string GetCountryName(string country)
```

Returns a readable country name for the `country` code.

### GetLanguageName

```csharp
public string GetLanguageName(string language)
```

Returns a readable language name for the `language` code.

### GetLoadedLocales

```csharp
public string[] GetLoadedLocales()
```

Returns an array of all loaded locales of the project.

### GetLocale

```csharp
public string GetLocale()
```

Returns the current locale of the project.

See also [`OS.GetLocale`](OS.md) and [`OS.GetLocaleLanguage`](OS.md) to query the locale of the user system.

### GetLocaleName

```csharp
public string GetLocaleName(string locale)
```

Returns a locale's language and its variant (e.g. `"en_US"` would return `"English (United States)"`).

### GetOrAddDomain

```csharp
public TranslationDomain GetOrAddDomain(StringName domain)
```

Returns the translation domain with the specified name. An empty translation domain will be created and added if it does not exist.

### GetPercentSign

```csharp
public string GetPercentSign(string locale)
```

Returns the percent sign used in the given `locale`.

### GetPluralRules

```csharp
public string GetPluralRules(string locale)
```

Returns the default plural rules for the `locale`.

### GetScriptName

```csharp
public string GetScriptName(string script)
```

Returns a readable script name for the `script` code.

### GetToolLocale

```csharp
public string GetToolLocale()
```

Returns the current locale of the editor.

**Note:** When called from an exported project returns the same value as `GetLocale()`.

### GetTranslationObject

```csharp
public Translation GetTranslationObject(string locale)
```

Returns the [`Translation`](Translation.md) instance that best matches `locale` in the main translation domain. Returns `null` if there are no matches.

### GetTranslations

```csharp
public Array<Translation> GetTranslations()
```

Returns all available [`Translation`](Translation.md) instances in the main translation domain as added by `AddTranslation()`.

### HasDomain

```csharp
public bool HasDomain(StringName domain)
```

Returns `true` if a translation domain with the specified name exists.

### HasTranslation

```csharp
public bool HasTranslation(Translation translation)
```

Returns `true` if the main translation domain contains the given `translation`.

### HasTranslationForLocale

```csharp
public bool HasTranslationForLocale(string locale, bool exact)
```

Returns `true` if there are any [`Translation`](Translation.md) instances in the main translation domain that match `locale` (see `CompareLocales()`). If `exact` is `true`, only instances whose locale exactly equals `locale` are considered.

### IsPseudolocalizationEnabled

```csharp
public bool IsPseudolocalizationEnabled()
```

### ParseNumber

```csharp
public string ParseNumber(string number, string locale)
```

Converts `number` from the numeral system used in the given `locale` to Western Arabic (0..9).

### Pseudolocalize

```csharp
public StringName Pseudolocalize(StringName message)
```

Returns the pseudolocalized string based on the `message` passed in.

**Note:** This method always uses the main translation domain.

### ReloadPseudolocalization

```csharp
public void ReloadPseudolocalization()
```

Reparses the pseudolocalization options and reloads the translation for the main translation domain.

### RemoveDomain

```csharp
public void RemoveDomain(StringName domain)
```

Removes the translation domain with the specified name.

**Note:** Trying to remove the main translation domain is an error.

### RemoveTranslation

```csharp
public void RemoveTranslation(Translation translation)
```

Removes the given translation from the main translation domain.

### SetLocale

```csharp
public void SetLocale(string locale)
```

Sets the locale of the project. The `locale` string will be standardized to match known locales (e.g. `en-US` would be matched to `en_US`).

If translations have been loaded beforehand for the new locale, they will be applied.

### SetPseudolocalizationEnabled

```csharp
public void SetPseudolocalizationEnabled(bool enabled)
```

### StandardizeLocale

```csharp
public string StandardizeLocale(string locale)
```

Returns a `locale` string standardized to match known locales (e.g. `en-US` would be matched to `en_US`). If `addDefaults` is `true`, the locale may have a default script or country added.

### StandardizeLocale

```csharp
public string StandardizeLocale(string locale, bool addDefaults = false)
```

Returns a `locale` string standardized to match known locales (e.g. `en-US` would be matched to `en_US`). If `addDefaults` is `true`, the locale may have a default script or country added.

### Translate

```csharp
public StringName Translate(StringName message, StringName context = null)
```

Returns the current locale's translation for the given message and context.

**Note:** This method always uses the main translation domain.

### TranslatePlural

```csharp
public StringName TranslatePlural(StringName message, StringName pluralMessage, int n, StringName context = null)
```

Returns the current locale's translation for the given message, plural message and context.

The number `n` is the number or quantity of the plural object. It will be used to guide the translation system to fetch the correct plural form for the selected language.

**Note:** This method always uses the main translation domain.
