<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TranslationServer

命名空间 `Godot`；static class

The translation server is the API backend that manages all language translations.

Translations are stored in [`TranslationDomain`](TranslationDomain.md)s, which can be accessed by name. The most commonly used translation domain is the main translation domain. It always exists and can be accessed using an empty [`StringName`](StringName.md). The translation server provides wrapper methods for accessing the main translation domain directly, without having to fetch the translation domain first. Custom translation domains are mainly for advanced usages like editor plugins. Names starting with `godot.` are reserved for engine internals.

## 属性

### PseudolocalizationEnabled

```csharp
public static bool PseudolocalizationEnabled { get; set; }
```

引擎名：`pseudolocalization_enabled`

If `true`, enables the use of pseudolocalization on the main translation domain. See `ProjectSettings.internationalization/pseudolocalization/use_pseudolocalization` for details.

### Singleton

```csharp
public static TranslationServerInstance Singleton { get; }
```

## 方法

### AddTranslation

```csharp
public static void AddTranslation(Translation translation)
```

引擎名：`add_translation`

Adds a translation to the main translation domain.

### Clear

```csharp
public static void Clear()
```

引擎名：`clear`

Removes all translations from the main translation domain.

### CompareLocales

```csharp
public static int CompareLocales(string localeA, string localeB)
```

引擎名：`compare_locales`

Compares two locales and returns a similarity score between `0` (no match) and `10` (full match).

### FindTranslations

```csharp
public static Array<Translation> FindTranslations(string locale, bool exact)
```

引擎名：`find_translations`

Returns the [`Translation`](Translation.md) instances in the main translation domain that match `locale` (see `CompareLocales()`). If `exact` is `true`, only instances whose locale exactly equals `locale` will be returned.

### FormatNumber

```csharp
public static string FormatNumber(string number, string locale)
```

引擎名：`format_number`

Converts a number from Western Arabic (0..9) to the numeral system used in the given `locale`.

### GetAllCountries

```csharp
public static string[] GetAllCountries()
```

引擎名：`get_all_countries`

Returns an array of known country codes.

### GetAllLanguages

```csharp
public static string[] GetAllLanguages()
```

引擎名：`get_all_languages`

Returns array of known language codes.

### GetAllScripts

```csharp
public static string[] GetAllScripts()
```

引擎名：`get_all_scripts`

Returns an array of known script codes.

### GetCountryName

```csharp
public static string GetCountryName(string country)
```

引擎名：`get_country_name`

Returns a readable country name for the `country` code.

### GetLanguageName

```csharp
public static string GetLanguageName(string language)
```

引擎名：`get_language_name`

Returns a readable language name for the `language` code.

### GetLoadedLocales

```csharp
public static string[] GetLoadedLocales()
```

引擎名：`get_loaded_locales`

Returns an array of all loaded locales of the project.

### GetLocale

```csharp
public static string GetLocale()
```

引擎名：`get_locale`

Returns the current locale of the project.

See also [`OS.GetLocale`](OS.md) and [`OS.GetLocaleLanguage`](OS.md) to query the locale of the user system.

### GetLocaleName

```csharp
public static string GetLocaleName(string locale)
```

引擎名：`get_locale_name`

Returns a locale's language and its variant (e.g. `"en_US"` would return `"English (United States)"`).

### GetOrAddDomain

```csharp
public static TranslationDomain GetOrAddDomain(StringName domain)
```

引擎名：`get_or_add_domain`

Returns the translation domain with the specified name. An empty translation domain will be created and added if it does not exist.

### GetPercentSign

```csharp
public static string GetPercentSign(string locale)
```

引擎名：`get_percent_sign`

Returns the percent sign used in the given `locale`.

### GetPluralRules

```csharp
public static string GetPluralRules(string locale)
```

引擎名：`get_plural_rules`

Returns the default plural rules for the `locale`.

### GetScriptName

```csharp
public static string GetScriptName(string script)
```

引擎名：`get_script_name`

Returns a readable script name for the `script` code.

### GetToolLocale

```csharp
public static string GetToolLocale()
```

引擎名：`get_tool_locale`

Returns the current locale of the editor.

**Note:** When called from an exported project returns the same value as `GetLocale()`.

### GetTranslationObject

```csharp
public static Translation GetTranslationObject(string locale)
```

引擎名：`get_translation_object`

Returns the [`Translation`](Translation.md) instance that best matches `locale` in the main translation domain. Returns `null` if there are no matches.

### GetTranslations

```csharp
public static Array<Translation> GetTranslations()
```

引擎名：`get_translations`

Returns all available [`Translation`](Translation.md) instances in the main translation domain as added by `AddTranslation()`.

### HasDomain

```csharp
public static bool HasDomain(StringName domain)
```

引擎名：`has_domain`

Returns `true` if a translation domain with the specified name exists.

### HasTranslation

```csharp
public static bool HasTranslation(Translation translation)
```

引擎名：`has_translation`

Returns `true` if the main translation domain contains the given `translation`.

### HasTranslationForLocale

```csharp
public static bool HasTranslationForLocale(string locale, bool exact)
```

引擎名：`has_translation_for_locale`

Returns `true` if there are any [`Translation`](Translation.md) instances in the main translation domain that match `locale` (see `CompareLocales()`). If `exact` is `true`, only instances whose locale exactly equals `locale` are considered.

### IsPseudolocalizationEnabled

```csharp
public static bool IsPseudolocalizationEnabled()
```

引擎名：`is_pseudolocalization_enabled`

### ParseNumber

```csharp
public static string ParseNumber(string number, string locale)
```

引擎名：`parse_number`

Converts `number` from the numeral system used in the given `locale` to Western Arabic (0..9).

### Pseudolocalize

```csharp
public static StringName Pseudolocalize(StringName message)
```

引擎名：`pseudolocalize`

Returns the pseudolocalized string based on the `message` passed in.

**Note:** This method always uses the main translation domain.

### ReloadPseudolocalization

```csharp
public static void ReloadPseudolocalization()
```

引擎名：`reload_pseudolocalization`

Reparses the pseudolocalization options and reloads the translation for the main translation domain.

### RemoveDomain

```csharp
public static void RemoveDomain(StringName domain)
```

引擎名：`remove_domain`

Removes the translation domain with the specified name.

**Note:** Trying to remove the main translation domain is an error.

### RemoveTranslation

```csharp
public static void RemoveTranslation(Translation translation)
```

引擎名：`remove_translation`

Removes the given translation from the main translation domain.

### SetLocale

```csharp
public static void SetLocale(string locale)
```

引擎名：`set_locale`

Sets the locale of the project. The `locale` string will be standardized to match known locales (e.g. `en-US` would be matched to `en_US`).

If translations have been loaded beforehand for the new locale, they will be applied.

### SetPseudolocalizationEnabled

```csharp
public static void SetPseudolocalizationEnabled(bool enabled)
```

引擎名：`set_pseudolocalization_enabled`

### StandardizeLocale

```csharp
public static string StandardizeLocale(string locale)
```

引擎名：`standardize_locale`

Returns a `locale` string standardized to match known locales (e.g. `en-US` would be matched to `en_US`). If `addDefaults` is `true`, the locale may have a default script or country added.

### StandardizeLocale

```csharp
public static string StandardizeLocale(string locale, bool addDefaults = false)
```

引擎名：`standardize_locale`

Returns a `locale` string standardized to match known locales (e.g. `en-US` would be matched to `en_US`). If `addDefaults` is `true`, the locale may have a default script or country added.

### Translate

```csharp
public static StringName Translate(StringName message, StringName context = null)
```

引擎名：`translate`

Returns the current locale's translation for the given message and context.

**Note:** This method always uses the main translation domain.

### TranslatePlural

```csharp
public static StringName TranslatePlural(StringName message, StringName pluralMessage, int n, StringName context = null)
```

引擎名：`translate_plural`

Returns the current locale's translation for the given message, plural message and context.

The number `n` is the number or quantity of the plural object. It will be used to guide the translation system to fetch the correct plural form for the selected language.

**Note:** This method always uses the main translation domain.
