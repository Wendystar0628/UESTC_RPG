<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Translation

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`Translation` maps a collection of strings to their individual translations, and also provides convenience methods for pluralization.

A `Translation` consists of messages. A message is identified by its context and untranslated string. Unlike [gettext](https://www.gnu.org/software/gettext/), using an empty context string in Godot means not using any context.

## 构造函数

### Translation

```csharp
public Translation()
```

## 属性

### Locale

```csharp
public string Locale { get; set; }
```

引擎名：`locale`

The locale of the translation.

### Messages

```csharp
public Dictionary Messages { get; set; }
```

引擎名：`messages`

### PluralRulesOverride

```csharp
public string PluralRulesOverride { get; set; }
```

引擎名：`plural_rules_override`

The plural rules string to enforce. See [GNU gettext](https://www.gnu.org/software/gettext/manual/html_node/Plural-forms.html) for examples and more info.

If empty or invalid, default plural rules from [`TranslationServer.GetPluralRules`](TranslationServer.md) are used. The English plural rules are used as a fallback.

## 方法

### AddMessage

```csharp
public void AddMessage(StringName srcMessage, StringName xlatedMessage, StringName context = null)
```

引擎名：`add_message`

Adds a message if nonexistent, followed by its translation.

An additional context could be used to specify the translation context or differentiate polysemic words.

### AddPluralMessage

```csharp
public void AddPluralMessage(StringName srcMessage, ReadOnlySpan<string> xlatedMessages, StringName context)
```

引擎名：`add_plural_message`

Adds a message involving plural translation if nonexistent, followed by its translation.

An additional context could be used to specify the translation context or differentiate polysemic words.

### AddPluralMessage

```csharp
public void AddPluralMessage(StringName srcMessage, string[] xlatedMessages, StringName context = null)
```

引擎名：`add_plural_message`

Adds a message involving plural translation if nonexistent, followed by its translation.

An additional context could be used to specify the translation context or differentiate polysemic words.

### EraseMessage

```csharp
public void EraseMessage(StringName srcMessage, StringName context = null)
```

引擎名：`erase_message`

Erases a message.

### GetLocale

```csharp
public string GetLocale()
```

引擎名：`get_locale`

### GetMessage

```csharp
public StringName GetMessage(StringName srcMessage, StringName context = null)
```

引擎名：`get_message`

Returns a message's translation.

### GetMessageCount

```csharp
public int GetMessageCount()
```

引擎名：`get_message_count`

Returns the number of existing messages.

### GetMessageList

```csharp
public string[] GetMessageList()
```

引擎名：`get_message_list`

Returns the keys of all messages, that is, the context and untranslated strings of each message.

**Note:** If a message does not use a context, the corresponding element is the untranslated string. Otherwise, the corresponding element is the context and untranslated string separated by the EOT character (`U+0004`). This is done for compatibility purposes.

```csharp
for key in translation.get_message_list():
var p = key.find("\u0004")
if p == -1:
var untranslated = key
print("Message %s" % untranslated)
else:
var context = key.substr(0, p)
var untranslated = key.substr(p + 1)
print("Message %s with context %s" % [untranslated, context])

```

### GetPluralMessage

```csharp
public StringName GetPluralMessage(StringName srcMessage, StringName srcPluralMessage, int n, StringName context = null)
```

引擎名：`get_plural_message`

Returns a message's translation involving plurals.

The number `n` is the number or quantity of the plural object. It will be used to guide the translation system to fetch the correct plural form for the selected language.

**Note:** Plurals are only supported in [gettext-based translations (PO)]($DOCS_URL/tutorials/i18n/localization_using_gettext.html), not CSV.

### GetPluralRulesOverride

```csharp
public string GetPluralRulesOverride()
```

引擎名：`get_plural_rules_override`

### GetTranslatedMessageList

```csharp
public string[] GetTranslatedMessageList()
```

引擎名：`get_translated_message_list`

Returns all the translated strings.

### SetLocale

```csharp
public void SetLocale(string locale)
```

引擎名：`set_locale`

### SetPluralRulesOverride

```csharp
public void SetPluralRulesOverride(string rules)
```

引擎名：`set_plural_rules_override`

### _GetMessage

```csharp
public virtual StringName _GetMessage(StringName srcMessage, StringName context)
```

引擎名：`_get_message`

Virtual method to override `GetMessage()`.

### _GetPluralMessage

```csharp
public virtual StringName _GetPluralMessage(StringName srcMessage, StringName srcPluralMessage, int n, StringName context)
```

引擎名：`_get_plural_message`

Virtual method to override `GetPluralMessage()`.
